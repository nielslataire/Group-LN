using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>Verwittigt een specifieke interne gebruiker (uit <c>ActieParametersJson.userId</c> of <c>Mijlpaal.VerantwoordelijkeUserId</c>).</summary>
public class VerwittigGebruikerAction : ITrajectTriggerAction
{
    public int Actie => (int)TriggerActie.VerwittigGebruiker;

    private readonly cpmRunningContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration? _config;

    public VerwittigGebruikerAction(cpmRunningContext db, IEmailSender emailSender, IConfiguration? config = null)
    {
        _db = db;
        _emailSender = emailSender;
        _config = config;
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

        var (subject, body) = await TrajectEmailBuilder.MaakAsync(_db, _config?["App:BaseUrl"], "Actie nodig", "Actie nodig", mijlpaal, trigger, project, ct);
        await _emailSender.SendEmailAsync(user.Email, subject, body);
        return TriggerActieResultaat.Success($"E-mail verzonden naar {user.Email}.");
    }
}
