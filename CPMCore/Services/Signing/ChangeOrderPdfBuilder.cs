using System.Globalization;
using BOCore;
using CPMCore.Documents;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Drawing;
using QuestPDF.Fluent;

namespace CPMCore.Services.Signing;

/// <summary>
/// Eén plek die een wijzigingsopdracht uit de database haalt en er de QuestPDF
/// (<see cref="ChangeOrderDocument"/>) van maakt. Gebruikt door de legacy afdrukactie
/// (<c>ProjectenController.ChangeOrderPDF</c>) én door <see cref="ChangeOrderSigningSource"/>, zodat
/// de klant op papier en in het ondertekendossier letterlijk hetzelfde document krijgt. Leest via
/// <see cref="cpmRunningContext"/> (zoals de nieuwere services), niet via de BO-laag: het document
/// heeft velden nodig (postcode van het project, contacten, eenheden per project) die de
/// <c>ChangeOrderBO</c>-vertaling niet meedraagt.
/// </summary>
public sealed class ChangeOrderPdfBuilder
{
    private static readonly object FontLock = new();
    private static bool _fontsRegistered;

    private readonly cpmRunningContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ChangeOrderPdfBuilder> _logger;

    public ChangeOrderPdfBuilder(cpmRunningContext db, IWebHostEnvironment env, ILogger<ChangeOrderPdfBuilder> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    /// <summary>De wijzigingsopdracht met alles wat de PDF en het ondertekendossier nodig hebben; null als ze niet bestaat.</summary>
    public async Task<ChangeOrderPdfModel?> LoadAsync(int changeOrderId, CancellationToken ct = default)
    {
        var co = await _db.ChangeOrder.AsNoTracking()
            .Include(c => c.ChangeOrderDetail)
            .Include(c => c.ClientAccount)
            .Include(c => c.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Project).ThenInclude(p => p.PostalCode)
            .FirstOrDefaultAsync(c => c.Id == changeOrderId, ct);
        if (co is null) return null;

        var project = co.ContractActivity?.Contract?.Project;
        var projectId = project?.ProjectId ?? 0;

        // Zelfde regel als ClientService.GetClientAccountUnitsNameById, maar beperkt tot dit project.
        var units = await _db.Units.AsNoTracking()
            .Where(u => u.ClientAccountId == co.ClientAccountId && u.ProjectId == projectId && u.Type != null && u.Type.Selectable == true)
            .OrderBy(u => u.Type!.GroupId).ThenBy(u => u.Name)
            .Select(u => new { TypeName = u.Type!.Name, u.Name })
            .ToListAsync(ct);

        var vat = await _db.ProjectSalesSettings.AsNoTracking()
            .Where(s => s.Projectid == projectId)
            .Select(s => s.Vatpercentage)
            .FirstOrDefaultAsync(ct) ?? 0m;

        var client = co.ClientAccount;
        string? salutation = null;
        if (client is not null && Enum.TryParse<Salutation>(client.Salutation, out var sal))
            salutation = sal.GetDisplayName();

        return new ChangeOrderPdfModel
        {
            Id = co.Id,
            ProjectId = projectId,
            ClientAccountId = co.ClientAccountId,
            Date = co.Date,
            ExpirationDate = co.ExpirationDate,
            ProjectName = project?.ProjectName ?? "",
            ProjectAddressLine = project is null ? null : string.Join(" ", new[] { project.Street, project.Number }.Where(s => !string.IsNullOrWhiteSpace(s))),
            ProjectCityLine = project?.PostalCode is null ? null : string.Join(" ", new[] { project.PostalCode.Postcode, project.PostalCode.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s))),
            ClientSalutation = salutation,
            ClientName = DisplayName(client),
            ClientEmail = client?.Email,
            UnitsLine = string.Join(" - ", units.Select(u => $"{u.TypeName} {u.Name}".Trim())),
            Description = co.Description ?? "",
            CommentHtml = co.Comment,
            Conditions = co.ChangeOrderConditions,
            VatPercentage = vat,
            Lines = co.ChangeOrderDetail.OrderBy(d => d.Id).Select(d => new ChangeOrderPdfLine
            {
                Description = d.Description,
                UnitLabel = d.MeasurementUnit.HasValue && Enum.IsDefined(typeof(MeasurementUnit), d.MeasurementUnit.Value) ? ((MeasurementUnit)d.MeasurementUnit.Value).GetDisplayName() : null,
                TypeLabel = d.MeasurementType.HasValue && Enum.IsDefined(typeof(MeasurementType), d.MeasurementType.Value) ? ((MeasurementType)d.MeasurementType.Value).GetDisplayName() : null,
                Number = d.Number,
                Price = d.Price,
                CommissionPercentage = d.Commission,
            }).ToList(),
        };
    }

    /// <summary>Zelfde regel als ClientAccountBO.DisplayName: naam, anders bedrijfsnaam.</summary>
    public static string DisplayName(ClientAccount? client)
        => client is null ? "" : (!string.IsNullOrWhiteSpace(client.Name) ? client.Name : client.CompanyName ?? "");

    public byte[] Render(ChangeOrderPdfModel model)
    {
        var logoPath = Path.Combine(_env.WebRootPath, "Img", "groupln-logo.png");
        byte[]? logo = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;
        var fontFamily = EnsureFontsRegistered();
        return new ChangeOrderDocument(model, logo, fontFamily).GeneratePdf();
    }

    public static string FileName(ChangeOrderPdfModel model)
    {
        var safe = string.Concat((model.ProjectName ?? "Project").Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).Trim();
        return $"Wijzigingsopdracht_{safe}_{model.Date:yyyyMMdd}_{model.Id}.pdf";
    }

    /// <summary>Avenir één keer per proces registreren (QuestPDF's FontManager is globaal); de
    /// bestaande afdrukacties doen dit per aanroep, hier gebeurt het ook vanuit een achtergrondpad
    /// zonder controller. Valt terug op het standaardlettertype als de TTF's ontbreken.</summary>
    private string? EnsureFontsRegistered()
    {
        lock (FontLock)
        {
            if (_fontsRegistered) return "Avenir";
            var fontsRoot = Path.Combine(_env.WebRootPath, "fonts");
            var any = false;
            try
            {
                foreach (var f in new[]
                {
                    "Avenir-Roman.ttf", "Avenir-Medium.ttf", "Avenir-Heavy.ttf", "Avenir-Black.ttf",
                    "Avenir-Oblique.ttf", "Avenir-MediumOblique.ttf", "Avenir-HeavyOblique.ttf", "Avenir-BlackOblique.ttf"
                })
                {
                    var fp = Path.Combine(fontsRoot, f);
                    if (!File.Exists(fp)) continue;
                    using var stream = File.OpenRead(fp);
                    FontManager.RegisterFont(stream);
                    any = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Avenir-fonts konden niet geregistreerd worden; de PDF valt terug op het standaardlettertype.");
            }
            _fontsRegistered = true;
            return any ? "Avenir" : null;
        }
    }
}
