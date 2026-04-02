using DALCore.Models;
using System.Net;
using System.Text;

namespace ServiceCore.Issues;

/// <summary>
/// Shared HTML email builder — produces the branded "Herinnering werfpunten" shell.
/// All emails use the same table-based layout with Group LN header/footer.
/// </summary>
public static class IssueEmailHtmlBuilder
{
    private const string Primary      = "#0a5a3b";
    private const string PrimaryLight = "#d4ede4";

    // ── Full issue-table email (used by Herinnering + SendSelected) ──────────

    public static string BuildIssueTableEmail(
        string recipientName,
        string projectName,
        IList<ConstructionIssue> issues,
        Dictionary<int, string> unitNames,
        string intro,
        DateTime sentDate,
        string? optionalComment = null,
        string? portalLoginUrl = null)
    {
        var issuesForEmail = issues
            .OrderBy(x => x.Priority == 3 ? 0 : x.Priority == 2 ? 1 : 2)
            .ThenBy(x => x.DueDate)
            .ToList();

        var sb = new StringBuilder();
        AppendHeader(sb, projectName, sentDate);

        // greeting + intro
        sb.Append($@"
            <!-- GREETING -->
            <p style=""font-size:15px;color:#1f2937;margin:0 0 8px;"">Beste <strong>{HtmlEnc(recipientName)}</strong>,</p>
            <p style=""font-size:14px;color:#374151;margin:0 0 24px;line-height:1.6;"">{intro}</p>");

        // optional comment block
        if (!string.IsNullOrWhiteSpace(optionalComment))
        {
            sb.Append($@"
            <div style=""background:#f0fdf4;border-left:3px solid {Primary};padding:10px 14px;margin-bottom:20px;font-size:13px;color:#374151;"">
              <strong>Opmerking:</strong><br/>{HtmlEnc(optionalComment)}
            </div>");
        }

        // issues table
        sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:28px;"">
              <tr>
                <td style=""background:{Primary};border-radius:6px 6px 0 0;padding:10px 14px;"">
                  <span style=""color:#ffffff;font-size:13px;font-weight:bold;"">{HtmlEnc(projectName)}</span>
                  <span style=""color:{PrimaryLight};font-size:11px;margin-left:8px;"">({issuesForEmail.Count} punt{(issuesForEmail.Count == 1 ? "" : "en")})</span>
                </td>
              </tr>
              <tr>
                <td style=""border:1px solid #dde3e9;border-top:none;border-radius:0 0 6px 6px;padding:0;"">
                  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;"">
                    <thead>
                      <tr style=""background:#f9fafb;"">
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;width:36px;"">#</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;"">Omschrijving</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;width:120px;"">Eenheid / Locatie</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;width:110px;"">Status</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;width:90px;"">Deadline</th>
                      </tr>
                    </thead>
                    <tbody>");

        var rowIdx = 1;
        foreach (var issue in issuesForEmail)
        {
            var rowBg        = rowIdx % 2 == 0 ? "#f9fafb" : "#ffffff";
            var statusLabel  = GetStatusLabel(issue.Status);
            var statusColor  = GetStatusColor(issue.Status);
            var deadline     = issue.DueDate.HasValue ? issue.DueDate.Value.ToString("dd/MM/yyyy") : "–";
            var priorityBadge = GetPriorityBadgeHtml(issue.Priority);
            var title        = HtmlEnc(issue.Title ?? "(zonder titel)");
            var unitName     = issue.UnitId.HasValue && unitNames.TryGetValue(issue.UnitId.Value, out var un) ? HtmlEnc(un) : null;
            var roomOrZone   = !string.IsNullOrWhiteSpace(issue.RoomOrZone) ? HtmlEnc(issue.RoomOrZone) : null;
            var locationCell = BuildLocationCellHtml(unitName, roomOrZone);

            sb.Append($@"
                      <tr style=""background:{rowBg};"">
                        <td style=""padding:9px 12px;font-size:12px;color:#6b7280;border-bottom:1px solid #f3f4f6;"">{rowIdx}</td>
                        <td style=""padding:9px 12px;font-size:12px;color:#1f2937;border-bottom:1px solid #f3f4f6;"">{title}{priorityBadge}</td>
                        <td style=""padding:9px 12px;font-size:11px;color:#374151;border-bottom:1px solid #f3f4f6;"">{locationCell}</td>
                        <td style=""padding:9px 12px;border-bottom:1px solid #f3f4f6;"">
                          <span style=""background:{statusColor.Bg};color:{statusColor.Text};font-size:10px;font-weight:600;padding:3px 10px;border-radius:10px;white-space:nowrap;display:inline-block;"">{statusLabel}</span>
                        </td>
                        <td style=""padding:9px 12px;font-size:12px;color:#374151;border-bottom:1px solid #f3f4f6;"">{deadline}</td>
                      </tr>");
            rowIdx++;
        }

        sb.Append(@"
                    </tbody>
                  </table>
                </td>
              </tr>
            </table>

            <!-- CLOSING -->
            <p style=""font-size:13px;color:#6b7280;margin:16px 0 0;line-height:1.6;"">
              De openstaande punten zijn ook beschikbaar als PDF-rapport in bijlage.<br/>
              Gelieve bij vragen contact op te nemen met uw projectleider.
            </p>");

        if (!string.IsNullOrWhiteSpace(portalLoginUrl))
        {
            sb.Append($@"
            <!-- PORTAL BUTTON -->
            <table cellpadding=""0"" cellspacing=""0"" style=""margin-top:20px;"">
              <tr>
                <td style=""background:{Primary};border-radius:6px;padding:10px 24px;"">
                  <a href=""{HtmlEnc(portalLoginUrl)}"" style=""color:#ffffff;text-decoration:none;font-size:13px;font-weight:600;white-space:nowrap;"">
                    <span style=""margin-right:6px;"">&#128274;</span> Login op ons portaal
                  </a>
                </td>
              </tr>
            </table>");
        }

        AppendFooter(sb, sentDate);
        return sb.ToString();
    }

    // ── Digest email (used by ContractorPortalDigestService) ─────────────────

    public static string BuildDigestEmail(
        string managerName,
        List<DigestSection> sections,
        DateTime sentDate)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, "Dagelijks overzicht aannemersportaal", sentDate);

        sb.Append($@"
            <p style=""font-size:15px;color:#1f2937;margin:0 0 8px;"">Dag <strong>{HtmlEnc(managerName)}</strong>,</p>
            <p style=""font-size:14px;color:#374151;margin:0 0 24px;line-height:1.6;"">Hieronder een overzicht van de recente activiteit op uw projecten via het aannemersportaal.</p>");

        foreach (var section in sections)
        {
            sb.Append($@"
            <!-- SECTION: {HtmlEnc(section.Title)} -->
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:24px;"">
              <tr>
                <td style=""background:{Primary};border-radius:6px 6px 0 0;padding:10px 14px;"">
                  <span style=""color:#ffffff;font-size:13px;font-weight:bold;"">{HtmlEnc(section.Title)}</span>
                  <span style=""color:{PrimaryLight};font-size:11px;margin-left:8px;"">({section.Items.Count} item{(section.Items.Count == 1 ? "" : "s")})</span>
                </td>
              </tr>
              <tr>
                <td style=""border:1px solid #dde3e9;border-top:none;border-radius:0 0 6px 6px;padding:0;"">
                  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;"">");

            var rowIdx = 0;
            foreach (var item in section.Items)
            {
                var rowBg = rowIdx % 2 == 0 ? "#ffffff" : "#f9fafb";
                sb.Append($@"
                  <tr style=""background:{rowBg};"">
                    <td style=""padding:10px 14px;border-bottom:1px solid #f3f4f6;"">
                      <div style=""font-size:13px;font-weight:600;color:#1f2937;margin-bottom:2px;"">{HtmlEnc(item.IssueTitle)}</div>
                      <div style=""font-size:11px;color:#6b7280;margin-bottom:2px;"">{HtmlEnc(item.ProjectName)}{(string.IsNullOrWhiteSpace(item.UnitName) ? "" : $" &nbsp;·&nbsp; <span style='color:#374151;font-weight:600;'>{HtmlEnc(item.UnitName)}</span>")}</div>
                      <div style=""font-size:12px;color:#374151;margin-bottom:8px;"">{HtmlEnc(item.Detail)}</div>
                      <table cellpadding=""0"" cellspacing=""0"" style=""display:inline-table;border-collapse:separate;"">
                        <tr>
                          <td style=""background:{Primary};border-radius:4px;padding:7px 16px;"">
                            <a href=""{HtmlEnc(item.LinkUrl)}"" style=""color:#ffffff;text-decoration:none;font-size:11px;font-weight:600;white-space:nowrap;"">Bekijk punt</a>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>");
                rowIdx++;
            }

            sb.Append(@"
                  </table>
                </td>
              </tr>
            </table>");
        }

        AppendFooter(sb, sentDate);
        return sb.ToString();
    }

    // ── Shared header / footer ────────────────────────────────────────────────

    private static void AppendHeader(StringBuilder sb, string subtitle, DateTime sentDate)
    {
        sb.Append($@"<!DOCTYPE html>
<html lang=""nl"">
<head>
  <meta charset=""utf-8""/>
  <meta name=""viewport"" content=""width=device-width,initial-scale=1""/>
  <title>Werfpunten – Group LN</title>
</head>
<body style=""margin:0;padding:0;background:#f4f6f8;font-family:Arial,Helvetica,sans-serif;"">

<!-- HEADER -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" bgcolor=""{Primary}"">
  <tr>
    <td style=""padding:24px 32px;"">
      <div style=""font-size:22px;font-weight:bold;color:#ffffff;letter-spacing:1px;"">Group LN</div>
      <div style=""font-size:12px;color:{PrimaryLight};margin-top:4px;"">{HtmlEnc(subtitle)} · {sentDate:dd MMMM yyyy}</div>
    </td>
  </tr>
</table>

<!-- WRAPPER -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"">
  <tr>
    <td align=""center"" style=""padding:24px 16px;"">
      <table width=""620"" cellpadding=""0"" cellspacing=""0"" bgcolor=""#ffffff""
             style=""border-radius:8px;border:1px solid #dde3e9;"">
        <tr>
          <td style=""padding:28px 32px;"">
");
    }

    private static void AppendFooter(StringBuilder sb, DateTime sentDate)
    {
        sb.Append($@"
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>

<!-- FOOTER -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"">
  <tr>
    <td align=""center"" style=""padding:16px;color:#9ca3af;font-size:11px;"">
      Group LN &nbsp;·&nbsp; Automatisch gegenereerd bericht &nbsp;·&nbsp; {sentDate:dd/MM/yyyy HH:mm} UTC
    </td>
  </tr>
</table>

</body>
</html>");
    }

    // ── Status helpers ────────────────────────────────────────────────────────

    public static string GetStatusLabel(int status) => status switch
    {
        0 => "Open",
        1 => "Toegewezen",
        2 => "Gepland",
        3 => "Wacht op controle",
        7 => "Heropend",
        _ => "Open"
    };

    public static (string Bg, string Text) GetStatusColor(int status) => status switch
    {
        0 => ("#fee2e2", "#b91c1c"),
        1 => ("#fef3c7", "#92400e"),
        2 => ("#dbeafe", "#1e40af"),
        3 => ("#f3f4f6", "#374151"),   // grey for Wacht op controle
        7 => ("#fce7f3", "#9d174d"),
        _ => ("#f3f4f6", "#374151")
    };

    public static string GetPriorityBadgeHtml(int priority) => priority switch
    {
        2 => " &nbsp;<span style=\"background:#fff3cd;color:#856404;font-size:9px;font-weight:700;padding:1px 5px;border-radius:8px;\">DRINGEND</span>",
        3 => " &nbsp;<span style=\"background:#fee2e2;color:#b91c1c;font-size:9px;font-weight:700;padding:1px 5px;border-radius:8px;\">KRITIEK</span>",
        _ => string.Empty
    };

    private static string BuildLocationCellHtml(string? unitName, string? roomOrZone)
    {
        if (unitName == null && roomOrZone == null)
            return "<span style=\"color:#9ca3af;\">–</span>";
        var sb = new StringBuilder();
        if (unitName != null)
            sb.Append($"<span style=\"font-weight:600;\">{unitName}</span>");
        if (roomOrZone != null)
        {
            if (unitName != null) sb.Append("<br/>");
            sb.Append($"<span style=\"color:#6b7280;\">{roomOrZone}</span>");
        }
        return sb.ToString();
    }

    private static string HtmlEnc(string? s) => WebUtility.HtmlEncode(s ?? "");

    // ── Digest section model ─────────────────────────────────────────────────

    public record DigestItem(string IssueTitle, string ProjectName, string Detail, string LinkUrl, string? UnitName = null);
    public record DigestSection(string Title, List<DigestItem> Items);
}
