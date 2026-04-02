using CPMCore.Service;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ServiceCore.Issues;

namespace CPMCore.Services;

/// <summary>
/// Dagelijkse digestmail voor werfleiders:
///  - nieuwe opmerkingen toegevoegd door aannemers (action=6, enkel met tekst)
///  - punten die klaar voor controle zijn gezet (action=3, newValueJson="3")
/// Maximaal één run per kalenderdag (bewaakt via IssueNotificationRun in de DB).
/// </summary>
public class ContractorPortalDigestService : IContractorPortalDigestService
{
    private readonly cpmRunningContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    private const int ActionCommentAdded      = 6;
    private const int ActionStatusChanged     = 3;
    private const int StatusWaitingInspection = 3;
    private const int RunTypeDigest           = 3; // used in IssueNotificationRun

    public ContractorPortalDigestService(
        cpmRunningContext db,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var now   = DateTime.UtcNow;
        var today = now.Date;

        // Run at most once per calendar day — checked against the DB so restarts don't re-trigger
        var alreadyRanToday = await _db.IssueNotificationRun
            .AnyAsync(r => r.RunType == RunTypeDigest && r.StartedDate >= today, ct);
        if (alreadyRanToday)
            return;

        // Log start immediately so parallel restarts don't double-send
        var runEntry = new IssueNotificationRun
        {
            ScheduleId   = null,
            RunType      = RunTypeDigest,
            TriggeredBy  = "digest",
            StartedDate  = now,
            Status       = 0 // Running
        };
        _db.IssueNotificationRun.Add(runEntry);
        await _db.SaveChangesAsync(ct);

        try
        {
            var emailsSent = await SendDigestAsync(now, ct);
            runEntry.Status     = 1; // Success
            runEntry.EmailsSent = emailsSent;
        }
        catch (Exception ex)
        {
            runEntry.Status       = 2; // Error
            runEntry.ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            runEntry.FinishedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<int> SendDigestAsync(DateTime now, CancellationToken ct)
    {
        // Look at the previous 24 hours of history
        var since = now.AddHours(-24);

        // Only CommentAdded entries that have actual text, and WaitingInspection status changes
        var historyEntries = await _db.ConstructionIssueHistory
            .AsNoTracking()
            .Where(h => h.Timestamp >= since && (
                (h.Action == ActionCommentAdded && h.Comment != null && h.Comment != "") ||
                (h.Action == ActionStatusChanged && h.NewValueJson == StatusWaitingInspection.ToString())
            ))
            .ToListAsync(ct);

        if (!historyEntries.Any())
            return 0;

        // Resolve issues (include UnitId for display)
        var issueIds = historyEntries.Select(h => h.IssueId).Distinct().ToList();
        var issues = await _db.ConstructionIssue
            .AsNoTracking()
            .Where(i => issueIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Title, i.ProjectId, i.UnitId })
            .ToDictionaryAsync(i => i.Id, ct);

        // Resolve unit names
        var unitIds = issues.Values.Where(i => i.UnitId.HasValue).Select(i => i.UnitId!.Value).Distinct().ToList();
        var unitNames = unitIds.Any()
            ? await _db.Units.AsNoTracking()
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Name ?? "", ct)
            : new Dictionary<int, string>();

        var projectIds = issues.Values.Select(i => i.ProjectId).Distinct().ToList();

        // Resolve projects with site manager
        var projects = await _db.Project
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.ProjectId))
            .Select(p => new { p.ProjectId, p.ProjectName, p.AspNetUserId })
            .ToDictionaryAsync(p => p.ProjectId, ct);

        var managerUserIds = projects.Values
            .Where(p => p.AspNetUserId != null)
            .Select(p => p.AspNetUserId!)
            .Distinct()
            .ToList();

        var managers = await _db.Users
            .AsNoTracking()
            .Where(u => managerUserIds.Contains(u.UserId) && u.Email != null)
            .Select(u => new { u.UserId, u.Voornaam, u.Familienaam, u.Email })
            .ToDictionaryAsync(u => u.UserId!, ct);

        // Resolve comment authors
        var authorIds = historyEntries
            .Where(h => h.Action == ActionCommentAdded && h.UserId != null)
            .Select(h => h.UserId!).Distinct().ToList();
        var authors = authorIds.Any()
            ? await _db.Users.AsNoTracking()
                .Where(u => authorIds.Contains(u.UserId))
                .Select(u => new { u.UserId, Name = (u.Voornaam + " " + u.Familienaam).Trim() })
                .ToDictionaryAsync(u => u.UserId!, u => u.Name, ct)
            : new Dictionary<string, string>();

        var appBaseUrl = (_configuration["App:BaseUrl"] ?? "https://cpm.groupln.be").TrimEnd('/');

        // Group items per site manager
        var digestPerManager = new Dictionary<string, List<DigestItem>>();

        foreach (var h in historyEntries)
        {
            if (!issues.TryGetValue(h.IssueId, out var issue)) continue;
            if (!projects.TryGetValue(issue.ProjectId, out var project)) continue;
            if (project.AspNetUserId == null) continue;

            var mgrId = project.AspNetUserId;
            if (!digestPerManager.ContainsKey(mgrId))
                digestPerManager[mgrId] = new List<DigestItem>();

            var unitName = issue.UnitId.HasValue && unitNames.TryGetValue(issue.UnitId.Value, out var un) ? un : null;

            if (h.Action == ActionCommentAdded)
            {
                var authorName = h.UserId != null && authors.TryGetValue(h.UserId, out var n) ? n : "Aannemer";
                digestPerManager[mgrId].Add(new DigestItem(
                    IsComment: true,
                    IssueId: h.IssueId,
                    IssueTitle: issue.Title ?? $"Punt {h.IssueId}",
                    ProjectId: issue.ProjectId,
                    ProjectName: project.ProjectName ?? $"Project {issue.ProjectId}",
                    Detail: $"{authorName}: {h.Comment}",
                    UnitName: unitName
                ));
            }
            else
            {
                digestPerManager[mgrId].Add(new DigestItem(
                    IsComment: false,
                    IssueId: h.IssueId,
                    IssueTitle: issue.Title ?? $"Punt {h.IssueId}",
                    ProjectId: issue.ProjectId,
                    ProjectName: project.ProjectName ?? $"Project {issue.ProjectId}",
                    Detail: "Aannemer heeft dit punt klaar voor controle gemeld.",
                    UnitName: unitName
                ));
            }
        }

        var emailsSent = 0;

        // Send one email per manager
        foreach (var (mgrUserId, items) in digestPerManager)
        {
            if (!managers.TryGetValue(mgrUserId, out var mgr)) continue;
            if (string.IsNullOrWhiteSpace(mgr.Email)) continue;

            var commentItems = items
                .Where(i => i.IsComment)
                .Select(i => new IssueEmailHtmlBuilder.DigestItem(
                    i.IssueTitle,
                    i.ProjectName,
                    i.Detail,
                    $"{appBaseUrl}/Projecten/Punten/{i.ProjectId}#{i.IssueId}",
                    i.UnitName))
                .ToList();
            var waitingItems = items
                .Where(i => !i.IsComment)
                .Select(i => new IssueEmailHtmlBuilder.DigestItem(
                    i.IssueTitle,
                    i.ProjectName,
                    "Aannemer heeft dit punt klaar voor controle gemeld.",
                    $"{appBaseUrl}/Projecten/Punten/{i.ProjectId}#{i.IssueId}",
                    i.UnitName))
                .ToList();

            var sections = new List<IssueEmailHtmlBuilder.DigestSection>();
            if (commentItems.Any()) sections.Add(new IssueEmailHtmlBuilder.DigestSection("Nieuwe opmerkingen van aannemers", commentItems));
            if (waitingItems.Any()) sections.Add(new IssueEmailHtmlBuilder.DigestSection("Punten klaar voor controle", waitingItems));

            if (!sections.Any()) continue;

            var subject = $"Dagelijks overzicht aannemersportaal – {now:dd/MM/yyyy}";
            var html    = IssueEmailHtmlBuilder.BuildDigestEmail(mgr.Voornaam ?? "Werfleider", sections, now);
            await _emailSender.SendEmailAsync(mgr.Email, subject, html);
            emailsSent++;
        }

        return emailsSent;
    }

    private record DigestItem(
        bool IsComment,
        int IssueId,
        string IssueTitle,
        int ProjectId,
        string ProjectName,
        string Detail,
        string? UnitName);
}
