using System.Security.Cryptography;
using CPMCore.Models.Projecten;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;

namespace CPMCore.Services;

/// <summary>Bouwt het PDF van een wijzigingsopdracht (dezelfde view als Projecten/ChangeOrderPDF) als bytes — zodat het
/// ter ondertekening verstuurd én, na het tekenen, opnieuw met het handtekeningblok bewaard kan worden.</summary>
public class ChangeOrderPdfService
{
    private readonly IProjectService _projects;
    private readonly IClientService _clients;

    public ChangeOrderPdfService(IProjectService projects, IClientService clients)
    {
        _projects = projects;
        _clients = clients;
    }

    public record PdfResult(byte[] Bytes, string FileName, string Hash);

    public async Task<PdfResult?> BuildAsync(ActionContext context, int changeOrderId, bool pendingDigitalSigning, List<SignatureEvidence>? evidence = null)
    {
        var model = new ProjectChangeOrderExportModel();
        var co = _projects.GetChangeOrder(changeOrderId);
        if (!co.Success) return null;
        model.ChangeOrder = co.Values.FirstOrDefault()!;
        if (model.ChangeOrder == null) return null;

        var project = _projects.GetProjectByID(model.ChangeOrder.ProjectId);
        if (project.Success) model.Project = project.Values.FirstOrDefault()!;
        var sales = _projects.GetSalesSettings(model.Project.Id);
        if (sales.Success) model.ProjectSalesSettings = sales.Values.FirstOrDefault()!;
        var client = _clients.GetClientAccountById(model.ChangeOrder.ClientAccountID);
        if (client.Success) model.ClientAccount = client.Values.FirstOrDefault()!;
        model.Units = _clients.GetClientAccountUnitsNameById(model.ChangeOrder.ClientAccountID);
        model.Signatures = evidence ?? new List<SignatureEvidence>();
        model.DigitalSigningPending = pendingDigitalSigning;

        var pdf = new ViewAsPdf("~/Views/Projecten/ChangeOrderPDF.cshtml", model)
        {
            PageOrientation = Orientation.Portrait,
            PageMargins = new Margins(10, 5, 0, 5),
            PageSize = Rotativa.AspNetCore.Options.Size.A4
        };
        var bytes = await pdf.BuildFile(context);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var name = $"Wijzigingsopdracht - {model.Project.Name} {DateTime.Now:yyyyMMdd}_{model.ChangeOrder.Id}.pdf";
        return new PdfResult(bytes, name, hash);
    }
}
