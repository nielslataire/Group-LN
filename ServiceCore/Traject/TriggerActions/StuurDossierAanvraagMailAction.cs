using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>Stuurt een aanvraagmail naar het externe contact van het aan de mijlpaal gekoppelde dossier.</summary>
public class StuurDossierAanvraagMailAction : ITrajectTriggerAction
{
    public int Actie => (int)TriggerActie.StuurDossierAanvraagMail;

    private readonly cpmRunningContext _db;
    private readonly IEmailSender _emailSender;

    public StuurDossierAanvraagMailAction(cpmRunningContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    public async Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default)
    {
        if (mijlpaal.DossierId is not int dossierId)
            return TriggerActieResultaat.Skip("Mijlpaal is aan geen enkel dossier gekoppeld.");

        var dossier = await _db.ProjectDossier.FirstOrDefaultAsync(d => d.Id == dossierId, ct);
        if (dossier == null) return TriggerActieResultaat.Skip("Gekoppeld dossier niet gevonden.");
        if (string.IsNullOrWhiteSpace(dossier.ExterneContactEmail))
            return TriggerActieResultaat.Skip("Dossier heeft geen extern e-mailadres.");

        var subject = $"Aanvraag — {dossier.Titel} ({project.ProjectName})";
        var body = TrajectEmailBuilder.Build(project.ProjectName, mijlpaal, $"vereist een aanvraag voor dossier '{dossier.Titel}'");
        await _emailSender.SendEmailAsync(dossier.ExterneContactEmail, subject, body);

        if (dossier.Status == (int)DossierStatus.Nieuw)
            dossier.Status = (int)DossierStatus.Aangevraagd;
        if (dossier.AanvraagDatum == null)
            dossier.AanvraagDatum = DateOnly.FromDateTime(DateTime.Today);

        _db.ProjectDossierGebeurtenis.Add(new ProjectDossierGebeurtenis
        {
            ProjectDossierId = dossier.Id,
            Type = (int)DossierGebeurtenisType.AanvraagVerstuurd,
            Titel = "Aanvraagmail verstuurd",
            Tekst = $"Naar {dossier.ExterneContactEmail}",
            UserId = triggeredBy,
            Datum = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        });

        return TriggerActieResultaat.Success($"Aanvraagmail verstuurd naar {dossier.ExterneContactEmail}.");
    }
}
