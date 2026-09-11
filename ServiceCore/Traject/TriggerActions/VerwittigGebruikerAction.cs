using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>Verwittigt een specifieke interne gebruiker (uit <c>ActieParametersJson.userId</c> of <c>Mijlpaal.VerantwoordelijkeUserId</c>).</summary>
public class VerwittigGebruikerAction : ITrajectTriggerAction
{
    public int Actie => (int)TriggerActie.VerwittigGebruiker;

    private readonly cpmRunningContext _db;
    private readonly IEmailSender _emailSender;

    public VerwittigGebruikerAction(cpmRunningContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    public async Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default)
    {
        var parameters = TriggerParamHelper.Parse(trigger.ActieParametersJson);
        var userId = TriggerParamHelper.GetString(parameters, "userId") ?? mijlpaal.VerantwoordelijkeUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return TriggerActieResultaat.Skip("Geen gebruiker toegewezen aan deze mijlpaal.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return TriggerActieResultaat.Skip($"Gebruiker '{userId}' niet gevonden of heeft geen e-mailadres.");

        var subject = $"Mijlpaal '{mijlpaal.Naam}' — {project.ProjectName}";
        var body = TrajectEmailBuilder.Build(project.ProjectName, mijlpaal, "is aan jou toegewezen");
        await _emailSender.SendEmailAsync(user.Email, subject, body);
        return TriggerActieResultaat.Success($"E-mail verzonden naar {user.Email}.");
    }
}
