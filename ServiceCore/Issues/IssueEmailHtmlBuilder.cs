using DALCore.Models;
using System.Net;
using System.Text;

namespace ServiceCore.Issues;

public static class IssueEmailHtmlBuilder
{
    private const string Primary      = "#0a5a3b";
    private const string PrimaryTint  = "#f3f8f5";
    private const string HeaderSoft   = "#a8cfb8";
    private const string DangerColor  = "#b9243a";
    private const string DangerBg     = "#fdeaee";
    private const string BorderColor  = "#e3e7ee";

    // ── Full issue-table email (used by Herinnering + SendSelected) ──────────

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

        var introPara1 = string.IsNullOrWhiteSpace(intro)
            ? $"Hieronder vindt u een overzicht van de <strong>openstaande werfpunten</strong> op de werf <strong>{HtmlEnc(projectName)}</strong> die nog op uw opvolging wachten. Gelieve deze punten <strong>zo snel mogelijk</strong> op te lossen zodat de werf vlot kan worden afgewerkt."
            : intro;

        var sb = new StringBuilder();
        AppendHeader(sb, projectName, sentDate);

        // Greeting + intro paragraphs
        sb.Append($@"
            <!-- GREETING -->
            <p style=""font-size:15px;color:#1f2937;margin:0 0 10px;"">Beste <strong>{HtmlEnc(recipientName)}</strong>,</p>
            <p style=""font-size:14px;color:#374151;margin:0 0 10px;line-height:1.7;"">{introPara1}</p>
            <p style=""font-size:14px;color:#374151;margin:0 0 20px;line-height:1.7;"">Heeft u een punt reeds opgelost, of heeft u er een geplande uitvoeringsdatum voor? Laat het ons op &eacute;&eacute;n van de volgende manieren weten:</p>");

        // Optional comment block
        if (!string.IsNullOrWhiteSpace(optionalComment))
        {
            sb.Append($@"
            <div style=""background:{PrimaryTint};border-left:3px solid {Primary};padding:10px 14px;margin-bottom:20px;font-size:13px;color:#374151;"">
              <strong>Opmerking:</strong><br/>{HtmlEnc(optionalComment)}
            </div>");
        }

        // Respond-box — title + two equal-height cards
        sb.Append($@"
            <!-- RESPOND BOX -->
            <div style=""background:{PrimaryTint};border:1px solid {BorderColor};border-radius:8px;padding:18px 20px;margin-bottom:28px;"">
              <p style=""font-size:12.5px;font-weight:600;color:{Primary};text-transform:uppercase;letter-spacing:0.5px;margin:0 0 14px;"">Twee manieren om te reageren</p>
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                <tr valign=""top"">
                  <!-- Card 1: Via werfleider -->
                  <td width=""48%"" style=""padding-right:8px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border:1px solid {BorderColor};border-radius:8px;height:100%;"">
                      <tr>
                        <td style=""padding:14px 16px;"" valign=""top"">
                          <table cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:8px;"">
                            <tr valign=""middle"">
                              <td>
                                <table cellpadding=""0"" cellspacing=""0"">
                                  <tr><td style=""background:{Primary};color:#ffffff;border-radius:50%;width:24px;height:24px;text-align:center;font-size:12px;font-weight:700;line-height:24px;"">1</td></tr>
                                </table>
                              </td>
                              <td style=""padding-left:8px;font-size:13px;font-weight:700;color:#1f2937;"">Via uw werfleider</td>
                            </tr>
                          </table>
                          <p style=""font-size:12px;color:#374151;line-height:1.55;margin:0;"">Stuur een korte mail naar uw werfleider met de status of de geplande datum per punt &mdash; wij verwerken het dan voor u.</p>
                        </td>
                      </tr>
                    </table>
                  </td>
                  <!-- Card 2: Via portaal -->
                  <td width=""48%"" style=""padding-left:8px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border:1px solid {BorderColor};border-radius:8px;height:100%;"">
                      <tr>
                        <td style=""padding:14px 16px;"" valign=""top"">
                          <table cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:8px;"">
                            <tr valign=""middle"">
                              <td>
                                <table cellpadding=""0"" cellspacing=""0"">
                                  <tr><td style=""background:{Primary};color:#ffffff;border-radius:50%;width:24px;height:24px;text-align:center;font-size:12px;font-weight:700;line-height:24px;"">2</td></tr>
                                </table>
                              </td>
                              <td style=""padding-left:8px;font-size:13px;font-weight:700;color:#1f2937;"">Via het aannemersportaal</td>
                            </tr>
                          </table>
                          <p style=""font-size:12px;color:#374151;line-height:1.55;margin:0;"">Pas de status en planning rechtstreeks zelf aan. U ontving hiervoor in een aparte mail een <strong>uitnodiging om u aan te melden</strong> op het portaal &mdash; registreer u eerst via die link, daarna kunt u inloggen.</p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </div>");

        // Issues table
        sb.Append($@"
            <!-- ISSUE TABLE -->
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:24px;"">
              <tr>
                <td style=""background:{Primary};border-radius:6px 6px 0 0;padding:10px 16px;"">
                  <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                    <tr valign=""middle"">
                      <td>
                        <span style=""color:#ffffff;font-size:13px;font-weight:700;"">{HtmlEnc(projectName)}</span>
                      </td>
                      <td align=""right"">
                        <span style=""background:rgba(255,255,255,0.18);color:#ffffff;font-size:11px;font-weight:600;padding:3px 10px;border-radius:10px;white-space:nowrap;"">{issuesForEmail.Count} openstaande punt{(issuesForEmail.Count == 1 ? "" : "en")}</span>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
              <tr>
                <td style=""border:1px solid {BorderColor};border-top:none;border-radius:0 0 6px 6px;padding:0;"">
                  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;"">
                    <thead>
                      <tr style=""background:#f9fafb;"">
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid {BorderColor};width:36px;"">#</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid {BorderColor};"">Omschrijving</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid {BorderColor};width:120px;"">Eenheid / Locatie</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid {BorderColor};width:110px;"">Status</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid {BorderColor};width:90px;"">Deadline</th>
                      </tr>
                    </thead>
                    <tbody>");

        var rowIdx = 1;
        foreach (var issue in issuesForEmail)
        {
            var rowBg         = rowIdx % 2 == 0 ? "#f9fafb" : "#ffffff";
            var statusLabel   = GetStatusLabel(issue.Status);
            var statusColor   = GetStatusColor(issue.Status);
            var deadline      = issue.DueDate.HasValue ? issue.DueDate.Value.ToString("dd/MM/yyyy") : "–";
            var priorityBadge = GetPriorityBadgeHtml(issue.Priority);
            var title         = HtmlEnc(issue.Title ?? "(zonder titel)");
            var unitName      = issue.UnitId.HasValue && unitNames.TryGetValue(issue.UnitId.Value, out var un) ? HtmlEnc(un) : null;
            var roomOrZone    = !string.IsNullOrWhiteSpace(issue.RoomOrZone) ? HtmlEnc(issue.RoomOrZone) : null;
            var locationCell  = BuildLocationCellHtml(unitName, roomOrZone);

            sb.Append($@"
                      <tr style=""background:{rowBg};"">
                        <td style=""padding:9px 12px;font-size:12px;color:#6b7280;border-bottom:1px solid #f3f4f6;"">{rowIdx}</td>
                        <td style=""padding:9px 12px;font-size:12px;color:#1f2937;border-bottom:1px solid #f3f4f6;"">{title}{priorityBadge}</td>
                        <td style=""padding:9px 12px;font-size:11px;color:#374151;border-bottom:1px solid #f3f4f6;"">{locationCell}</td>
                        <td style=""padding:9px 12px;border-bottom:1px solid #f3f4f6;"">
                          <span style=""background:{statusColor.Bg};color:{statusColor.Text};font-size:10px;font-weight:600;padding:3px 10px;border-radius:11px;white-space:nowrap;display:inline-block;"">&#9679; {statusLabel}</span>
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
            </table>");

        // Attachment block (dashed border, download button if URL available)
        var pdfFileName = $"Openstaande punten &ndash; {HtmlEnc(projectName)}.pdf";
        var downloadCell = string.IsNullOrWhiteSpace(pdfDownloadUrl)
            ? ""
            : $@"<td align=""right"" style=""padding-left:12px;white-space:nowrap;"">
                   <table cellpadding=""0"" cellspacing=""0"">
                     <tr>
                       <td style=""border:1px solid {BorderColor};border-radius:6px;padding:7px 16px;"">
                         <a href=""{HtmlEnc(pdfDownloadUrl)}"" style=""color:#1f2937;text-decoration:none;font-size:12px;font-weight:600;white-space:nowrap;"">Downloaden</a>
                       </td>
                     </tr>
                   </table>
                 </td>";

        sb.Append($@"
            <!-- ATTACHMENT -->
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px dashed {BorderColor};border-radius:8px;margin-bottom:24px;"">
              <tr valign=""middle"">
                <td style=""padding:14px 16px;"">
                  <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                    <tr valign=""middle"">
                      <td style=""width:40px;"">
                        <div style=""background:{DangerBg};border-radius:8px;width:36px;height:36px;text-align:center;line-height:36px;font-size:18px;"">&#128196;</div>
                      </td>
                      <td style=""padding-left:12px;"">
                        <div style=""font-size:13px;font-weight:600;color:#1f2937;"">{pdfFileName}</div>
                        <div style=""font-size:11px;color:#6b7280;margin-top:2px;"">PDF-rapport &middot; in bijlage</div>
                      </td>
                      {downloadCell}
                    </tr>
                  </table>
                </td>
              </tr>
            </table>");

        // CTA buttons
        var hasPortal     = !string.IsNullOrWhiteSpace(portalLoginUrl);
        var hasWerfleider = !string.IsNullOrWhiteSpace(werfleiderEmail);

        if (hasPortal || hasWerfleider)
        {
            sb.Append(@"
            <!-- CTA BUTTONS -->
            <table cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:8px;"">
              <tr valign=""middle"">");

            if (hasPortal)
            {
                sb.Append($@"
                <td style=""padding-right:10px;"">
                  <table cellpadding=""0"" cellspacing=""0"">
                    <tr>
                      <td style=""background:{Primary};border-radius:6px;padding:11px 22px;"">
                        <a href=""{HtmlEnc(portalLoginUrl)}"" style=""color:#ffffff;text-decoration:none;font-size:13px;font-weight:600;white-space:nowrap;"">Login op het aannemersportaal</a>
                      </td>
                    </tr>
                  </table>
                </td>");
            }

            if (hasWerfleider)
            {
                sb.Append($@"
                <td>
                  <table cellpadding=""0"" cellspacing=""0"">
                    <tr>
                      <td style=""border:1px solid {BorderColor};border-radius:6px;padding:10px 20px;"">
                        <a href=""mailto:{HtmlEnc(werfleiderEmail)}"" style=""color:#1f2937;text-decoration:none;font-size:13px;font-weight:600;white-space:nowrap;"">&#128139; Mail uw werfleider</a>
                      </td>
                    </tr>
                  </table>
                </td>");
            }

            sb.Append(@"
              </tr>
            </table>");

            if (hasPortal)
            {
                sb.Append(@"
            <p style=""font-size:11px;color:#6b7280;margin:0 0 20px;line-height:1.6;"">Nog niet aangemeld? Volg eerst de uitnodigingslink uit de afzonderlijke registratiemail om uw account te activeren.</p>");
            }
        }

        // Info note
        sb.Append($@"
            <!-- INFO NOTE -->
            <div style=""background:{PrimaryTint};border:1px solid {BorderColor};border-radius:6px;padding:10px 14px;margin-bottom:20px;"">
              <p style=""font-size:12px;color:#374151;margin:0;"">&#8505; Gelieve bij vragen contact op te nemen met uw projectleider. Wij helpen u graag verder bij het opvolgen of het inplannen van de werken.</p>
            </div>");

        // Closing + signature
        sb.Append($@"
            <!-- CLOSING -->
            <p style=""font-size:13px;color:#374151;margin:0 0 4px;"">Alvast bedankt voor uw snelle opvolging.</p>
            <p style=""font-size:13px;color:#374151;margin:0;"">Met vriendelijke groeten,<br/><strong style=""color:{Primary};"">Het team van Group LN</strong></p>");

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
                  <span style=""color:{PrimaryTint};font-size:11px;margin-left:8px;"">({section.Items.Count} item{(section.Items.Count == 1 ? "" : "s")})</span>
                </td>
              </tr>
              <tr>
                <td style=""border:1px solid {BorderColor};border-top:none;border-radius:0 0 6px 6px;padding:0;"">
                  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;"">");

            var rowIdx = 0;
            foreach (var item in section.Items)
            {
                var rowBg = rowIdx % 2 == 0 ? "#ffffff" : "#f9fafb";
                sb.Append($@"
                  <tr style=""background:{rowBg};"">
                    <td style=""padding:10px 14px;border-bottom:1px solid #f3f4f6;"">
                      <div style=""font-size:13px;font-weight:600;color:#1f2937;margin-bottom:2px;"">{HtmlEnc(item.IssueTitle)}</div>
                      <div style=""font-size:11px;color:#6b7280;margin-bottom:2px;"">{HtmlEnc(item.ProjectName)}{(string.IsNullOrWhiteSpace(item.UnitName) ? "" : $" &nbsp;&middot;&nbsp; <span style='color:#374151;font-weight:600;'>{HtmlEnc(item.UnitName)}</span>")}</div>
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

<!-- WRAPPER -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"">
  <tr>
    <td align=""center"" style=""padding:28px 16px;"">
      <table width=""620"" cellpadding=""0"" cellspacing=""0""
             style=""border-radius:10px;border:1px solid {BorderColor};border-collapse:separate;"">

        <!-- GREEN HEADER (top of card) -->
        <tr>
          <td bgcolor=""{Primary}"" style=""background:{Primary};border-radius:10px 10px 0 0;padding:24px 32px;"">
            <div style=""font-size:22px;font-weight:700;color:#ffffff;letter-spacing:0.5px;"">Group LN</div>
            <div style=""font-size:12px;color:{HeaderSoft};font-weight:400;margin-top:5px;"">
              {HtmlEnc(subtitle)} &nbsp;&#9679;&nbsp; {sentDate:dd MMMM yyyy}
            </div>
          </td>
        </tr>

        <!-- WHITE CONTENT -->
        <tr>
          <td bgcolor=""#ffffff"" style=""background:#ffffff;border-radius:0 0 10px 10px;padding:28px 32px;"">
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
      Group LN &nbsp;&middot;&nbsp; Automatisch gegenereerd bericht &nbsp;&middot;&nbsp; {sentDate:dd/MM/yyyy HH:mm} UTC
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
        0 => (DangerBg, DangerColor),
        1 => ("#fef3c7", "#92400e"),
        2 => ("#dbeafe", "#1e40af"),
        3 => ("#f3f4f6", "#374151"),
        7 => ("#fce7f3", "#9d174d"),
        _ => ("#f3f4f6", "#374151")
    };

    public static string GetPriorityBadgeHtml(int priority) => priority switch
    {
        2 => " &nbsp;<span style=\"background:#fff3cd;color:#856404;font-size:9px;font-weight:700;padding:1px 5px;border-radius:8px;\">DRINGEND</span>",
        3 => " &nbsp;<span style=\"background:#fdeaee;color:#b9243a;font-size:9px;font-weight:700;padding:1px 5px;border-radius:8px;\">KRITIEK</span>",
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
