using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// Verwittigt de interne rol die verantwoordelijk is voor de mijlpaal. Er is (nog) geen algemene
/// rol→gebruiker-mapping in de app: Projectleider en Verkoper volgen de directe koppeling op het project,
/// Boekhouder (rol Boekhouding), CEO / CFO en Projectontwikkelaar (rol Bestuurder) gaan naar alle actieve gebruikers met die rol (zie RolOntvangers). Voor overige
/// rollen kan een expliciet e-mailadres in <c>ActieParametersJson</c> ("email") meegegeven worden.
/// </summary>
public class VerwittigRolAction : ITrajectTriggerAction
{
    public int Actie => (int)TriggerActie.VerwittigRol;

    private readonly cpmRunningContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration? _config;

    public VerwittigRolAction(cpmRunningContext db, IEmailSender emailSender, IConfiguration? config = null)
    {
        _db = db;
        _emailSender = emailSender;
        _config = config;
    }

    public async Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default)
    {
        var parameters = TriggerParamHelper.Parse(trigger.ActieParametersJson);
        var expliciteEmail = TriggerParamHelper.GetString(parameters, "email");
        var rol = TriggerParamHelper.GetInt(parameters, "rol") ?? mijlpaal.VerantwoordelijkeRol;

        string? aanduiding = rol.HasValue ? ((InterneRol)rol.Value).GetDisplayName() : "verantwoordelijke";

        var ontvangers = new List<string>();
        if (!string.IsNullOrWhiteSpace(expliciteEmail))
            ontvangers.Add(expliciteEmail.Trim());
        else if (rol is int r)
            ontvangers = await RolOntvangers.ZoekAsync(_db, (InterneRol)r, project, ct);

        if (ontvangers.Count == 0)
            return TriggerActieResultaat.Skip($"Geen e-mailadres gekend voor rol '{aanduiding}': geen gebruiker met die rol (of projectkoppeling) en geen expliciet e-mailadres opgegeven.");

        var (subject, body) = await TrajectEmailBuilder.MaakAsync(_db, _config?["App:BaseUrl"], "Actie nodig", "Actie nodig van " + aanduiding, mijlpaal, trigger, project, ct);
        foreach (var email in ontvangers)
            await _emailSender.SendEmailAsync(email, subject, body);
        return TriggerActieResultaat.Success($"E-mail verzonden naar {string.Join(", ", ontvangers)} (rol: {aanduiding}).");
    }
}
