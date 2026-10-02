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
            IsQuote = co.IsQuote,
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
            Lines = co.ChangeOrderDetail.OrderBy(d => d.SortOrder ?? int.MaxValue).ThenBy(d => d.Id).Select(d => new ChangeOrderPdfLine
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

    /// <summary>Zelfde regel als ClientAccountBO.DisplayName (migratie 057): achternaam eerst, dan
    /// voornaam indien gekend, anders bedrijfsnaam.</summary>
    public static string DisplayName(ClientAccount? client)
    {
        if (client is null) return "";
        if (string.IsNullOrWhiteSpace(client.Name)) return client.CompanyName ?? "";
        return string.IsNullOrWhiteSpace(client.Forename) ? client.Name : client.Name + " " + client.Forename;
    }

    public byte[] Render(ChangeOrderPdfModel model)
    {
        var logoPath = Path.Combine(_env.WebRootPath, "Img", "groupln-logo.png");
        byte[]? logo = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;
        var fontFamily = GroupLnFonts.EnsureAvenirRegistered(_env, _logger);
        return new ChangeOrderDocument(model, logo, fontFamily).GeneratePdf();
    }

    public static string FileName(ChangeOrderPdfModel model)
    {
        var safe = string.Concat((model.ProjectName ?? "Project").Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).Trim();
        return $"{(model.IsQuote ? "Offerte" : "Wijzigingsopdracht")}_{safe}_{model.Date:yyyyMMdd}_{model.Id}.pdf";
    }
}
