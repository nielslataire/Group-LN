using DALCore.Models;
using System.Net;
using System.Text;

namespace ServiceCore.Issues;

public static class IssueEmailHtmlBuilder
{
    private const string Green       = "#0a5a3b";
    private const string GreenDark   = "#08482e";
    private const string GreenTint   = "#f3f8f5";
    private const string GreenBorder = "#d2e3d8";
    private const string GreenAcc    = "#3a7455";
    private const string DangerColor = "#b9243a";
    private const string DangerBg    = "#fdeaee";
    private const string Border      = "#e3e7ee";
    private const string BorderSoft  = "#eef1f6";
    private const string TextMain    = "#1f2937";
    private const string TextSoft    = "#4a5566";
    private const string Muted       = "#8892a4";
    private const string Font        = "-apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif";

    // ── Full issue-table email ────────────────────────────────────────────────

    public static string BuildIssueTableEmail(
        string recipientName,
        string projectName,
        IList<ConstructionIssue> issues,
        Dictionary<int, string> unitNames,
        string? intro,
        DateTime sentDate,
        string? optionalComment = null,
        string? portalLoginUrl = null,
        string? pdfDownloadUrl = null,
        string? werfleiderEmail = null)
    {
        var issuesForEmail = issues
            .OrderBy(x => x.Priority == 3 ? 0 : x.Priority == 2 ? 1 : 2)
            .ThenBy(x => x.DueDate)
            .ToList();

        bool isEvening = !string.IsNullOrWhiteSpace(intro);

        var introPara1 = string.IsNullOrWhiteSpace(intro)
            ? $"Hieronder vindt u een overzicht van de <strong>openstaande werfpunten</strong> op de werf <strong>{HtmlEnc(projectName)}</strong> die nog op uw opvolging wachten. Gelieve deze punten <strong>zo snel mogelijk</strong> op te lossen zodat de werf vlot kan worden afgewerkt."
            : intro;

        var countLabel = isEvening
            ? $"{issuesForEmail.Count} nieuw of gewijzigd punt{(issuesForEmail.Count == 1 ? "" : "en")}"
            : $"{issuesForEmail.Count} openstaande punt{(issuesForEmail.Count == 1 ? "" : "en")}";

        var sb = new StringBuilder();
        AppendHeader(sb, projectName, sentDate);

        // ── Greeting ──────────────────────────────────────────────────────────
        sb.Append($@"
<p style=""margin:0 0 14px;font-size:15px;color:{TextMain};font-family:{Font};"">Beste <strong style=""color:{GreenDark};font-weight:600;"">{HtmlEnc(recipientName)}</strong>,</p>
<p style=""margin:0 0 10px;font-size:13.5px;color:{Green};line-height:1.65;font-family:{Font};"">{introPara1}</p>
<p style=""margin:0 0 0;font-size:13.5px;color:{Green};line-height:1.65;font-family:{Font};"">Heeft u een punt reeds opgelost, of heeft u er een geplande uitvoeringsdatum voor? Laat het ons op &eacute;&eacute;n van de volgende manieren weten:</p>");

        // ── Optional comment ──────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(optionalComment))
        {
            sb.Append($@"
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:14px;"">
  <tr>
    <td bgcolor=""{GreenTint}"" style=""background:{GreenTint};border-left:3px solid {Green};padding:10px 14px;font-size:13px;color:{TextMain};font-family:{Font};"">
      <strong>Opmerking:</strong><br/>{HtmlEnc(optionalComment)}
    </td>
  </tr>
</table>");
        }

        // ── Respond box ───────────────────────────────────────────────────────
        sb.Append($@"
<!-- RESPOND BOX -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:22px;margin-bottom:26px;border-radius:8px;border-collapse:separate;border:1px solid {GreenBorder};"">
  <tr>
    <td bgcolor=""{GreenTint}"" style=""background:{GreenTint};padding:16px 18px;border-radius:8px;"">
      <p style=""margin:0 0 12px;font-size:11.5px;font-weight:700;color:{GreenDark};text-transform:uppercase;letter-spacing:.7px;font-family:{Font};"">Twee manieren om te reageren</p>
      <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
        <tr valign=""top"">
          <td width=""49%"" style=""padding-right:6px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-radius:6px;border-collapse:separate;border:1px solid #dbe5df;"">
              <tr>
                <td bgcolor=""#ffffff"" style=""background:#ffffff;padding:14px;border-radius:6px;"" valign=""top"">
                  <table cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:9px;"">
                    <tr valign=""middle"">
                      <td bgcolor=""{Green}"" style=""background:{Green};color:#ffffff;width:26px;height:26px;border-radius:13px;text-align:center;font-size:12px;font-weight:700;line-height:26px;font-family:{Font};"">1</td>
                      <td style=""padding-left:9px;font-size:13px;font-weight:700;color:{TextMain};font-family:{Font};"">Via uw werfleider</td>
                    </tr>
                  </table>
                  <p style=""margin:0;font-size:12.5px;color:{TextSoft};line-height:1.55;font-family:{Font};"">Stuur een korte mail naar uw werfleider met de status of de geplande datum per punt &mdash; wij verwerken het dan voor u.</p>
                </td>
              </tr>
            </table>
          </td>
          <td width=""49%"" style=""padding-left:6px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-radius:6px;border-collapse:separate;border:1px solid #dbe5df;"">
              <tr>
                <td bgcolor=""#ffffff"" style=""background:#ffffff;padding:14px;border-radius:6px;"" valign=""top"">
                  <table cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:9px;"">
                    <tr valign=""middle"">
                      <td bgcolor=""{Green}"" style=""background:{Green};color:#ffffff;width:26px;height:26px;border-radius:13px;text-align:center;font-size:12px;font-weight:700;line-height:26px;font-family:{Font};"">2</td>
                      <td style=""padding-left:9px;font-size:13px;font-weight:700;color:{TextMain};font-family:{Font};"">Via het aannemersportaal</td>
                    </tr>
                  </table>
                  <p style=""margin:0;font-size:12.5px;color:{TextSoft};line-height:1.55;font-family:{Font};"">Pas de status en planning rechtstreeks zelf aan. U ontving hiervoor in een aparte mail een <strong>uitnodiging om u aan te melden</strong> op het portaal &mdash; registreer u eerst via die link, daarna kunt u inloggen.</p>
                </td>
              </tr>
            </table>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>");

        // ── Werf block — single merged table ──────────────────────────────────
        sb.Append($@"
<!-- WERF BLOCK -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:26px;border:1px solid {Border};border-radius:8px;border-collapse:separate;"">
  <tr bgcolor=""{Green}"">
    <td colspan=""5"" bgcolor=""{Green}"" style=""background:{Green};padding:12px 18px;border-radius:8px 8px 0 0;"">
      <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
        <tr valign=""middle"">
          <td style=""color:#ffffff;font-size:14px;font-weight:600;font-family:{Font};"">&#127968; {HtmlEnc(projectName)}</td>
          <td align=""right"" style=""white-space:nowrap;"">
            <span style=""background:rgba(255,255,255,.2);color:#ffffff;font-size:11px;font-weight:500;padding:3px 11px;border-radius:12px;font-family:{Font};"">{countLabel}</span>
          </td>
        </tr>
      </table>
    </td>
  </tr>
  <tr bgcolor=""#fafbfc"" style=""background:#fafbfc;"">
    <th style=""padding:10px 14px;text-align:left;font-size:10px;font-weight:700;color:{Muted};text-transform:uppercase;letter-spacing:.6px;border-bottom:1px solid {Border};width:36px;font-family:{Font};"">#</th>
    <th style=""padding:10px 14px;text-align:left;font-size:10px;font-weight:700;color:{Muted};text-transform:uppercase;letter-spacing:.6px;border-bottom:1px solid {Border};font-family:{Font};"">Omschrijving</th>
    <th style=""padding:10px 14px;text-align:left;font-size:10px;font-weight:700;color:{Muted};text-transform:uppercase;letter-spacing:.6px;border-bottom:1px solid {Border};font-family:{Font};"">Eenheid / Locatie</th>
    <th style=""padding:10px 14px;text-align:left;font-size:10px;font-weight:700;color:{Muted};text-transform:uppercase;letter-spacing:.6px;border-bottom:1px solid {Border};font-family:{Font};"">Status</th>
    <th style=""padding:10px 14px;text-align:left;font-size:10px;font-weight:700;color:{Muted};text-transform:uppercase;letter-spacing:.6px;border-bottom:1px solid {Border};font-family:{Font};"">Deadline</th>
  </tr>");

        var rowIdx = 1;
        foreach (var issue in issuesForEmail)
        {
            var isLast        = rowIdx == issuesForEmail.Count;
            var borderBottom  = isLast ? "" : $"border-bottom:1px solid {BorderSoft};";
            var deadline      = issue.DueDate.HasValue ? issue.DueDate.Value.ToString("dd/MM/yyyy") : "&ndash;";
            var priorityBadge = GetPriorityBadgeHtml(issue.Priority);
            var title         = HtmlEnc(issue.Title ?? "(zonder titel)");
            var unitName      = issue.UnitId.HasValue && unitNames.TryGetValue(issue.UnitId.Value, out var un) ? HtmlEnc(un) : null;
            var roomOrZone    = !string.IsNullOrWhiteSpace(issue.RoomOrZone) ? HtmlEnc(issue.RoomOrZone) : null;
            var locationHtml  = BuildLocationCellHtml(unitName, roomOrZone);
            var statusPill    = BuildStatusPillHtml(issue.Status);

            sb.Append($@"
  <tr bgcolor=""#ffffff"">
    <td style=""padding:13px 14px;{borderBottom}color:{Muted};font-size:12px;font-weight:500;font-family:{Font};"">{rowIdx}</td>
    <td style=""padding:13px 14px;{borderBottom}font-size:13px;font-weight:600;color:{Green};font-family:{Font};"">{title}{priorityBadge}</td>
    <td style=""padding:13px 14px;{borderBottom}font-family:{Font};"">{locationHtml}</td>
    <td style=""padding:13px 14px;{borderBottom}"">{statusPill}</td>
    <td style=""padding:13px 14px;{borderBottom}color:{Muted};font-size:12.5px;font-family:{Font};"">{deadline}</td>
  </tr>");
            rowIdx++;
        }

        sb.Append(@"
</table><!-- /werf block -->");

        // ── Attachment ────────────────────────────────────────────────────────
        var pdfFileName  = $"Openstaande punten &ndash; {HtmlEnc(projectName)}.pdf";
        var downloadCell = string.IsNullOrWhiteSpace(pdfDownloadUrl) ? "" :
            $@"<td align=""right"" style=""padding:12px 14px;white-space:nowrap;"">
              <table cellpadding=""0"" cellspacing=""0"" style=""border:1px solid {Border};border-radius:6px;border-collapse:separate;"">
                <tr><td bgcolor=""#ffffff"" style=""background:#ffffff;padding:7px 14px;border-radius:6px;"">
                  <a href=""{HtmlEnc(pdfDownloadUrl)}"" style=""color:{TextMain};text-decoration:none;font-size:12.5px;font-weight:600;white-space:nowrap;font-family:{Font};"">Downloaden</a>
                </td></tr>
              </table>
            </td>";

        sb.Append($@"
<!-- ATTACHMENT -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:24px;border:1px dashed #c6d1c9;border-radius:8px;border-collapse:separate;"">
  <tr valign=""middle"">
    <td bgcolor=""#fafdfb"" style=""background:#fafdfb;padding:12px 12px 12px 14px;width:52px;border-radius:8px 0 0 8px;"">
      <div style=""width:38px;height:38px;background:{DangerBg};border-radius:6px;text-align:center;line-height:38px;font-size:20px;"">&#128196;</div>
    </td>
    <td bgcolor=""#fafdfb"" style=""background:#fafdfb;padding:12px 6px;"">
      <div style=""font-size:13px;font-weight:600;color:{TextMain};font-family:{Font};"">{pdfFileName}</div>
      <div style=""font-size:11px;color:{Muted};margin-top:2px;font-family:{Font};"">PDF-rapport &middot; in bijlage</div>
    </td>
    {downloadCell}
  </tr>
</table>");

        // ── CTA buttons ───────────────────────────────────────────────────────
        var hasPortal     = !string.IsNullOrWhiteSpace(portalLoginUrl);
        var hasWerfleider = !string.IsNullOrWhiteSpace(werfleiderEmail);

        if (hasPortal || hasWerfleider)
        {
            sb.Append(@"
<!-- CTA BUTTONS -->
<table cellpadding=""0"" cellspacing=""0"" style=""margin-top:22px;"">
  <tr valign=""middle"">");

            if (hasPortal)
                sb.Append($@"
    <td style=""padding-right:10px;"">
      <table cellpadding=""0"" cellspacing=""0"" style=""border-radius:7px;border-collapse:separate;"">
        <tr>
          <td bgcolor=""{Green}"" style=""background:{Green};padding:12px 22px;border-radius:7px;"">
            <a href=""{HtmlEnc(portalLoginUrl)}"" style=""color:#ffffff;text-decoration:none;font-size:13.5px;font-weight:600;white-space:nowrap;font-family:{Font};"">Login op het aannemersportaal</a>
          </td>
        </tr>
      </table>
    </td>");

            if (hasWerfleider)
                sb.Append($@"
    <td>
      <table cellpadding=""0"" cellspacing=""0"" style=""border:1px solid {Border};border-radius:7px;border-collapse:separate;"">
        <tr>
          <td bgcolor=""#ffffff"" style=""background:#ffffff;padding:11px 22px;border-radius:7px;"">
            <a href=""mailto:{HtmlEnc(werfleiderEmail)}"" style=""color:{GreenDark};text-decoration:none;font-size:13.5px;font-weight:600;white-space:nowrap;font-family:{Font};"">&#9993; Mail uw werfleider</a>
          </td>
        </tr>
      </table>
    </td>");

            sb.Append(@"
  </tr>
</table>");

            // "Nog niet aangemeld?" callout
            if (hasPortal)
                sb.Append($@"
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:12px;"">
  <tr>
    <td bgcolor=""{GreenTint}"" style=""background:{GreenTint};border-left:4px solid {Green};padding:12px 14px;border-radius:0 6px 6px 0;"">
      <table cellpadding=""0"" cellspacing=""0"">
        <tr valign=""top"">
          <td style=""padding-right:8px;color:{GreenAcc};font-size:16px;line-height:1;"">&#8505;</td>
          <td style=""font-size:12px;color:{TextSoft};line-height:1.6;font-family:{Font};""><strong style=""color:{TextMain};"">Nog niet aangemeld?</strong> Volg eerst de uitnodigingslink uit de afzonderlijke registratiemail om uw account te activeren &mdash; daarna kunt u op het portaal inloggen.</td>
        </tr>
      </table>
    </td>
  </tr>
</table>");
        }

        // ── Info note ─────────────────────────────────────────────────────────
        sb.Append($@"
<!-- INFO NOTE -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:16px;border:1px solid {GreenBorder};border-radius:6px;border-collapse:separate;"">
  <tr>
    <td bgcolor=""{GreenTint}"" style=""background:{GreenTint};padding:12px 14px;border-radius:6px;"">
      <table cellpadding=""0"" cellspacing=""0"">
        <tr valign=""top"">
          <td style=""padding-right:8px;color:{GreenAcc};font-size:16px;line-height:1;"">&#8505;</td>
          <td style=""font-size:12px;color:{TextSoft};line-height:1.6;font-family:{Font};"">Gelieve bij vragen contact op te nemen met uw projectleider. Wij helpen u graag verder bij het opvolgen of het inplannen van de werken.</td>
        </tr>
      </table>
    </td>
  </tr>
</table>");

        // ── Closing ───────────────────────────────────────────────────────────
        sb.Append($@"
<p style=""margin:22px 0 4px;font-size:13px;color:{TextSoft};font-family:{Font};"">Alvast bedankt voor uw snelle opvolging.</p>
<p style=""margin:0;font-size:13px;color:{TextMain};font-family:{Font};"">Met vriendelijke groeten,<br/><strong style=""color:{GreenDark};"">Het team van Group LN</strong></p>");

        AppendFooter(sb, sentDate);
        return sb.ToString();
    }

    // ── Digest email ──────────────────────────────────────────────────────────

    public static string BuildDigestEmail(
        string managerName,
        List<DigestSection> sections,
        DateTime sentDate)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, "Dagelijks overzicht aannemersportaal", sentDate);

        sb.Append($@"
<p style=""margin:0 0 8px;font-size:15px;color:{TextMain};font-family:{Font};"">Dag <strong style=""color:{GreenDark};"">{HtmlEnc(managerName)}</strong>,</p>
<p style=""margin:0 0 24px;font-size:13.5px;color:{TextSoft};line-height:1.65;font-family:{Font};"">Hieronder een overzicht van de recente activiteit op uw projecten via het aannemersportaal.</p>");

        foreach (var section in sections)
        {
            sb.Append($@"
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:24px;border:1px solid {Border};border-radius:8px;border-collapse:separate;"">
  <tr>
    <td colspan=""2"" bgcolor=""{Green}"" style=""background:{Green};padding:10px 16px;border-radius:8px 8px 0 0;"">
      <span style=""color:#ffffff;font-size:13px;font-weight:700;font-family:{Font};"">{HtmlEnc(section.Title)}</span>
      <span style=""color:rgba(255,255,255,.7);font-size:11px;margin-left:8px;font-family:{Font};"">({section.Items.Count} item{(section.Items.Count == 1 ? "" : "s")})</span>
    </td>
  </tr>");

            var idx = 0;
            foreach (var item in section.Items)
            {
                var bg = idx % 2 == 0 ? "#ffffff" : "#fafbfc";
                sb.Append($@"
  <tr>
    <td bgcolor=""{bg}"" style=""background:{bg};padding:12px 16px;border-bottom:1px solid {BorderSoft};"">
      <div style=""font-size:13px;font-weight:600;color:{TextMain};margin-bottom:2px;font-family:{Font};"">{HtmlEnc(item.IssueTitle)}</div>
      <div style=""font-size:11px;color:{Muted};margin-bottom:4px;font-family:{Font};"">{HtmlEnc(item.ProjectName)}{(string.IsNullOrWhiteSpace(item.UnitName) ? "" : $" &middot; <strong style='color:{TextMain};'>{HtmlEnc(item.UnitName)}</strong>")}</div>
      <div style=""font-size:12px;color:{TextSoft};margin-bottom:10px;font-family:{Font};"">{HtmlEnc(item.Detail)}</div>
      <table cellpadding=""0"" cellspacing=""0"" style=""border-radius:5px;border-collapse:separate;"">
        <tr>
          <td bgcolor=""{Green}"" style=""background:{Green};padding:7px 16px;border-radius:5px;"">
            <a href=""{HtmlEnc(item.LinkUrl)}"" style=""color:#ffffff;text-decoration:none;font-size:12px;font-weight:600;white-space:nowrap;font-family:{Font};"">Bekijk punt</a>
          </td>
        </tr>
      </table>
    </td>
  </tr>");
                idx++;
            }

            sb.Append(@"
</table>");
        }

        AppendFooter(sb, sentDate);
        return sb.ToString();
    }

    // ── Header / footer — table-based for Outlook compatibility ──────────────

    private static void AppendHeader(StringBuilder sb, string subtitle, DateTime sentDate)
    {
        sb.Append($@"<!doctype html>
<html lang=""nl"">
<head>
<meta charset=""UTF-8"">
<title>Werfpunten &mdash; Group LN</title>
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
</head>
<body style=""margin:0;padding:0;background:#f4f6f8;"">

<!-- OUTER WRAPPER -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" bgcolor=""#f4f6f8"" style=""background:#f4f6f8;"">
  <tr>
    <td align=""center"" style=""padding:28px 16px 48px;"">

      <!-- CARD (fixed 620px width = centered card in all email clients) -->
      <table width=""620"" cellpadding=""0"" cellspacing=""0"" style=""border-radius:10px;border-collapse:separate;"">

        <!-- BRAND BAR -->
        <tr>
          <td bgcolor=""{Green}"" style=""background:{Green};padding:22px 32px;border-radius:10px 10px 0 0;"">
            <div style=""font-size:22px;font-weight:700;color:#ffffff;letter-spacing:.2px;margin-bottom:4px;font-family:{Font};"">Group LN</div>
            <div style=""font-size:12px;color:rgba(255,255,255,.85);font-family:{Font};"">
              {HtmlEnc(subtitle)} &nbsp;&middot;&nbsp; {sentDate:dd MMMM yyyy}
            </div>
          </td>
        </tr>

        <!-- CARD BODY -->
        <tr>
          <td bgcolor=""#ffffff"" style=""background:#ffffff;padding:32px 36px 32px;border-radius:0 0 10px 10px;"">
");
    }

    private static void AppendFooter(StringBuilder sb, DateTime sentDate)
    {
        sb.Append($@"
          </td>
        </tr>
      </table><!-- /CARD -->

      <!-- FOOTER -->
      <table width=""620"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:16px;"">
        <tr>
          <td align=""center"" style=""font-size:11px;color:{Muted};line-height:1.6;font-family:{Font};"">
            Group LN &nbsp;&middot;&nbsp; Automatisch gegenereerd bericht &nbsp;&middot;&nbsp; {sentDate:dd/MM/yyyy HH:mm} UTC
          </td>
        </tr>
      </table>

    </td>
  </tr>
</table><!-- /OUTER WRAPPER -->

</body>
</html>");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public static string GetStatusLabel(int status) => status switch
    {
        0 => "Open",
        1 => "Toegewezen",
        2 => "Gepland",
        3 => "Wacht op controle",
        7 => "Heropend",
        _ => "Open"
    };

    private static string BuildStatusPillHtml(int status)
    {
        var (bg, color) = status switch
        {
            0 => (DangerBg, DangerColor),
            1 => ("#fff8df", "#b67800"),
            2 => ("#dbeafe", "#1e40af"),
            3 => ("#f3f4f6", "#374151"),
            7 => ("#fce7f3", "#9d174d"),
            _ => (DangerBg, DangerColor)
        };
        var label = GetStatusLabel(status);
        return $"<span style=\"display:inline-block;background:{bg};color:{color};padding:3px 10px;border-radius:11px;font-size:10.5px;font-weight:700;white-space:nowrap;font-family:{Font};\"><span style=\"display:inline-block;width:6px;height:6px;border-radius:50%;background:{color};vertical-align:middle;margin-right:5px;margin-bottom:1px;\"></span>{label}</span>";
    }

    public static (string Bg, string Text) GetStatusColor(int status) => status switch
    {
        0 => (DangerBg, DangerColor),
        1 => ("#fff8df", "#b67800"),
        2 => ("#dbeafe", "#1e40af"),
        3 => ("#f3f4f6", "#374151"),
        7 => ("#fce7f3", "#9d174d"),
        _ => ("#f3f4f6", "#374151")
    };

    public static string GetPriorityBadgeHtml(int priority) => priority switch
    {
        2 => $" &nbsp;<span style=\"background:#fff8df;color:#b67800;font-size:9px;font-weight:700;padding:2px 6px;border-radius:8px;font-family:{Font};\">DRINGEND</span>",
        3 => $" &nbsp;<span style=\"background:{DangerBg};color:{DangerColor};font-size:9px;font-weight:700;padding:2px 6px;border-radius:8px;font-family:{Font};\">KRITIEK</span>",
        _ => string.Empty
    };

    private static string BuildLocationCellHtml(string? unitName, string? roomOrZone)
    {
        if (unitName == null && roomOrZone == null)
            return $"<span style=\"color:{Muted};\">–</span>";
        var sb = new StringBuilder();
        if (unitName != null)
            sb.Append($"<span style=\"font-size:13px;font-weight:600;color:{TextMain};display:block;line-height:1.35;\">{unitName}</span>");
        if (roomOrZone != null)
            sb.Append($"<span style=\"font-size:11px;color:{Muted};display:block;margin-top:1px;\">{roomOrZone}</span>");
        return sb.ToString();
    }

    private static string HtmlEnc(string? s) => WebUtility.HtmlEncode(s ?? "");

    // ── Models ────────────────────────────────────────────────────────────────

    public record DigestItem(string IssueTitle, string ProjectName, string Detail, string LinkUrl, string? UnitName = null);
    public record DigestSection(string Title, List<DigestItem> Items);
}
