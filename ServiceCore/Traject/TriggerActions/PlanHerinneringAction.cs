using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// Stuurt een herinneringsmail — typisch gekoppeld aan het <c>XDagenVoorDoeldatum</c>-event.
/// Probeert eerst de toegewezen gebruiker, dan de verantwoordelijke rol (enkel Projectleider/Verkoper
/// hebben een directe koppeling), anders overgeslagen.
/// </summary>
public class PlanHerinneringAction : ITrajectTriggerAction
{
    public int Actie => (int)TriggerActie.PlanHerinnering;

    private readonly cpmRunningContext _db;
    private readonly IEmailSender _emailSender;

    public PlanHerinneringAction(cpmRunningContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    public async Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default)
    {
        var parameters = TriggerParamHelper.Parse(trigger.ActieParametersJson);
        var userId = TriggerParamHelper.GetString(parameters, "userId") ?? mijlpaal.VerantwoordelijkeUserId;

        string? email = null;
        if (!string.IsNullOrWhiteSpace(userId))
            email = await _db.Users.Where(u => u.UserId == userId).Select(u => u.Email).FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(email) && mijlpaal.VerantwoordelijkeRol is int r)
        {
            string? roleUserId = (InterneRol)r switch
            {
                InterneRol.Projectleider => project.AspNetUserId,
                InterneRol.Verkoper => project.SalesResponsibleAspNetUserId,
                _ => null
            };
            if (!string.IsNullOrWhiteSpace(roleUserId))
                email = await _db.Users.Where(u => u.UserId == roleUserId).Select(u => u.Email).FirstOrDefaultAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(email))
            return TriggerActieResultaat.Skip("Geen gebruiker/rol met bekend e-mailadres om te herinneren.");

        var subject = $"Herinnering — '{mijlpaal.Naam}' nadert ({project.ProjectName})";
        var body = TrajectEmailBuilder.Build(project.ProjectName, mijlpaal, "nadert en vraagt binnenkort actie");
        await _emailSender.SendEmailAsync(email!, subject, body);
        return TriggerActieResultaat.Success($"Herinnering verzonden naar {email}.");
    }
}
