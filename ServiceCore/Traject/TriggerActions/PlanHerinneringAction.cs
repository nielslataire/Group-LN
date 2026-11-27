using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// Stuurt een herinneringsmail — typisch gekoppeld aan het <c>XDagenVoorDoeldatum</c>-event.
/// Probeert eerst de toegewezen gebruiker, dan de verantwoordelijke rol (zie RolOntvangers: projectkoppeling of gebruikersrol);
/// vindt het niemand, dan wordt de actie overgeslagen.
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

        var ontvangers = new List<string>();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var mail = await _db.Users.Where(u => u.UserId == userId).Select(u => u.Email).FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(mail)) ontvangers.Add(mail.Trim());
        }

        if (ontvangers.Count == 0 && mijlpaal.VerantwoordelijkeRol is int r)
            ontvangers = await RolOntvangers.ZoekAsync(_db, (InterneRol)r, project, ct);

        if (ontvangers.Count == 0)
            return TriggerActieResultaat.Skip("Geen gebruiker/rol met bekend e-mailadres om te herinneren.");

        var subject = $"Herinnering — '{mijlpaal.Naam}' nadert ({project.ProjectName})";
        var body = TrajectEmailBuilder.Build(project.ProjectName, mijlpaal, "nadert en vraagt binnenkort actie");
        foreach (var email in ontvangers)
            await _emailSender.SendEmailAsync(email, subject, body);
        return TriggerActieResultaat.Success($"Herinnering verzonden naar {string.Join(", ", ontvangers)}.");
    }
}
