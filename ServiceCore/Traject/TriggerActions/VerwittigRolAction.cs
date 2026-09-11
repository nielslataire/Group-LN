using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// Verwittigt de interne rol die verantwoordelijk is voor de mijlpaal. Er is (nog) geen algemene
/// rol→gebruiker-mapping in de app; enkel de rollen met een directe koppeling op het project
/// (Projectleider → werfleider, Verkoper → verkoopsverantwoordelijke) worden herkend. Voor overige
/// rollen kan een expliciet e-mailadres in <c>ActieParametersJson</c> ("email") meegegeven worden.
/// </summary>
public class VerwittigRolAction : ITrajectTriggerAction
{
    public int Actie => (int)TriggerActie.VerwittigRol;

    private readonly cpmRunningContext _db;
    private readonly IEmailSender _emailSender;

    public VerwittigRolAction(cpmRunningContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    public async Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default)
    {
        var parameters = TriggerParamHelper.Parse(trigger.ActieParametersJson);
        var expliciteEmail = TriggerParamHelper.GetString(parameters, "email");
        var rol = TriggerParamHelper.GetInt(parameters, "rol") ?? mijlpaal.VerantwoordelijkeRol;

        string? email = expliciteEmail;
        string? aanduiding = rol.HasValue ? ((InterneRol)rol.Value).GetDisplayName() : "verantwoordelijke";

        if (string.IsNullOrWhiteSpace(email) && rol is int r)
        {
            string? userId = (InterneRol)r switch
            {
                InterneRol.Projectleider => project.AspNetUserId,
                InterneRol.Verkoper => project.SalesResponsibleAspNetUserId,
                _ => null
            };
            if (!string.IsNullOrWhiteSpace(userId))
                email = await _db.Users.Where(u => u.UserId == userId).Select(u => u.Email).FirstOrDefaultAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(email))
            return TriggerActieResultaat.Skip($"Geen e-mailadres gekend voor rol '{aanduiding}' — geen directe koppeling en geen expliciet e-mailadres opgegeven.");

        var subject = $"Mijlpaal '{mijlpaal.Naam}' — {project.ProjectName}";
        var body = TrajectEmailBuilder.Build(project.ProjectName, mijlpaal, "vraagt de aandacht van " + aanduiding);
        await _emailSender.SendEmailAsync(email!, subject, body);
        return TriggerActieResultaat.Success($"E-mail verzonden naar {email} (rol: {aanduiding}).");
    }
}
