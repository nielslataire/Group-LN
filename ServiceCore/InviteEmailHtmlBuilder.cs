using System.Net;
using System.Text;

namespace ServiceCore;

public static class InviteEmailHtmlBuilder
{
    private const string Green       = "#0a5a3b";
    private const string GreenDark   = "#08482e";
    private const string GreenTint   = "#f3f8f5";
    private const string GreenBorder = "#d2e3d8";
    private const string Border      = "#e3e7ee";
    private const string BorderSoft  = "#eef1f6";
    private const string TextMain    = "#1f2937";
    private const string TextSoft    = "#4a5566";
    private const string Muted       = "#8892a4";
    private const string Font        = "'Poppins','Segoe UI',Helvetica,Arial,sans-serif";
    private const string AmberBg     = "#fffbeb";
    private const string AmberBorder = "#f59e0b";
    private const string AmberText   = "#78350f";

    public static string BuildInviteEmail(
        string recipientName,
        string recipientEmail,
        string loginUrl,
        string? redeemUrl,
        DateTime sentDate,
        string companyName = "Group LN")
    {
        var sb = new StringBuilder();
        AppendHeader(sb, companyName, sentDate);

        // ── Greeting ─────────────────────────────────────────────────────────
        sb.Append($@"
<p style=""margin:0 0 14px;font-size:15px;color:{TextMain};font-family:{Font};"">Beste <strong style=""color:{GreenDark};font-weight:600;"">{HtmlEnc(recipientName)}</strong>,</p>
<p style=""margin:0 0 14px;font-size:13.5px;color:{TextMain};line-height:1.65;font-family:{Font};"">{HtmlEnc(companyName)} werkt met een online <strong>aannemersportaal</strong>. Daar volgt u eenvoudig uw werven op: u ziet uw <strong>openstaande punten</strong>, kunt deze <strong>zelf beheren</strong> en bijkomende info rechtstreeks doorsturen &mdash; zonder telkens te mailen.</p>
<p style=""margin:0 0 28px;font-size:13.5px;color:{TextMain};line-height:1.65;font-family:{Font};"">Aanmelden is eenvoudig en veilig: u logt in met uw <strong>Microsoft-account</strong> dat gekoppeld is aan uw e-mailadres <strong>{HtmlEnc(recipientEmail)}</strong>. U heeft dus geen nieuw wachtwoord aan te maken of te onthouden.</p>");

        // ── CTA card ──────────────────────────────────────────────────────────
        sb.Append($@"
<!-- CTA CARD -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid {GreenBorder};border-radius:10px;border-collapse:separate;mso-table-lspace:0pt;mso-table-rspace:0pt;"">
  <tr>
    <td bgcolor=""{GreenTint}"" style=""background:{GreenTint};padding:32px 36px;border-radius:10px;text-align:center;"">

      <!-- Shield pill -->
      <table cellpadding=""0"" cellspacing=""0"" align=""center"" style=""border-collapse:separate;mso-table-lspace:0pt;mso-table-rspace:0pt;margin-bottom:18px;"">
        <tr>
          <td bgcolor=""#ffffff"" style=""background:#ffffff;border:1px solid {GreenBorder};border-radius:20px;padding:5px 16px;text-align:center;"">
            <img src=""https://cpm.groupln.be/Img/icons/shield.png"" width=""14"" height=""14"" alt="""" style=""vertical-align:middle;margin-right:4px;"" /><span style=""font-size:11px;font-weight:700;color:{Green};font-family:{Font};"">Beveiligde aanmelding</span>
          </td>
        </tr>
      </table>

      <div style=""font-size:20px;font-weight:700;color:{TextMain};margin-bottom:8px;font-family:{Font};line-height:1.3;"">Activeer uw toegang in &eacute;&eacute;n klik</div>
      <div style=""font-size:13px;color:{TextSoft};line-height:1.6;margin-bottom:24px;font-family:{Font};"">Klik op de knop hieronder en meld u aan met uw Microsoft-account.<br/>Uw toegang wordt meteen geactiveerd.</div>

      {BuildLoginButtonHtml(redeemUrl ?? loginUrl)}

      <div style=""margin-top:14px;font-size:11.5px;color:{Muted};font-family:{Font};"">Inloggen via uw Microsoft-account &nbsp;&middot;&nbsp; {HtmlEnc(recipientEmail)}</div>
    </td>
  </tr>
</table>");

        AppendSpacer(sb, 24);

        // ── Steps ─────────────────────────────────────────────────────────────
        sb.Append($@"
<!-- STEPS -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid {Border};border-radius:8px;border-collapse:separate;mso-table-lspace:0pt;mso-table-rspace:0pt;"">
  <tr>
    <td bgcolor=""{GreenTint}"" style=""background:{GreenTint};padding:10px 18px;border-radius:8px 8px 0 0;border-bottom:1px solid {GreenBorder};"">
      <span style=""font-size:10px;font-weight:700;color:{Green};text-transform:uppercase;letter-spacing:.8px;font-family:{Font};"">Zo werkt het &mdash; in 3 stappen</span>
    </td>
  </tr>
  <tr>
    <td bgcolor=""#ffffff"" style=""background:#ffffff;padding:16px 18px;border-bottom:1px solid {BorderSoft};"">
      {BuildStepHtml(1,
          "Klik op &ldquo;Aanmelden met Microsoft&rdquo;",
          "U wordt doorgestuurd naar de beveiligde aanmeldpagina van Microsoft.")}
    </td>
  </tr>
  <tr>
    <td bgcolor=""#ffffff"" style=""background:#ffffff;padding:16px 18px;border-bottom:1px solid {BorderSoft};"">
      {BuildStepHtml(2,
          "Log in met uw e-mailadres",
          $"Gebruik het Microsoft-account gekoppeld aan <strong>{HtmlEnc(recipientEmail)}</strong>. Heeft u nog geen Microsoft-account met dit adres? Dan maakt u er gratis &eacute;&eacute;n aan in enkele klikken.")}
    </td>
  </tr>
  <tr>
    <td bgcolor=""#ffffff"" style=""background:#ffffff;padding:16px 18px;border-radius:0 0 8px 8px;"">
      {BuildStepHtml(3,
          "U heeft toegang tot het portaal",
          "Klaar. U komt rechtstreeks in het aannemersportaal terecht en kunt meteen aan de slag.")}
    </td>
  </tr>
</table>");

        AppendSpacer(sb, 24);

        // ── Features 2×2 ──────────────────────────────────────────────────────
        sb.Append($@"
<!-- FEATURES -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid {Border};border-radius:8px;border-collapse:separate;mso-table-lspace:0pt;mso-table-rspace:0pt;"">
  <tr>
    <td colspan=""2"" bgcolor=""#fafbfc"" style=""background:#fafbfc;padding:10px 18px;border-radius:8px 8px 0 0;border-bottom:1px solid {Border};"">
      <span style=""font-size:10px;font-weight:700;color:{TextMain};text-transform:uppercase;letter-spacing:.7px;font-family:{Font};"">Wat u in het portaal kunt doen</span>
    </td>
  </tr>
  <tr valign=""top"">
    <td width=""50%"" bgcolor=""#ffffff"" style=""background:#ffffff;padding:16px 18px;border-right:1px solid {BorderSoft};border-bottom:1px solid {BorderSoft};"">
      {BuildFeatureHtml("https://cpm.groupln.be/Img/icons/list-ul.png", "Uw punten bekijken", "Bekijk per werf al uw openstaande en afgewerkte punten op &eacute;&eacute;n plek.")}
    </td>
    <td width=""50%"" bgcolor=""#ffffff"" style=""background:#ffffff;padding:16px 18px;border-bottom:1px solid {BorderSoft};"">
      {BuildFeatureHtml("https://cpm.groupln.be/Img/icons/edit.png", "Zelf beheren", "Pas de status aan of geef een geplande uitvoeringsdatum in.")}
    </td>
  </tr>
  <tr valign=""top"">
    <td width=""50%"" bgcolor=""#ffffff"" style=""background:#ffffff;padding:16px 18px;border-right:1px solid {BorderSoft};border-radius:0 0 0 8px;"">
      {BuildFeatureHtml("https://cpm.groupln.be/Img/icons/photo-album.png", "Extra info doorsturen", "Voeg een opmerking of foto toe als bewijs van uitvoering.")}
    </td>
    <td width=""50%"" bgcolor=""#ffffff"" style=""background:#ffffff;padding:16px 18px;border-radius:0 0 8px 0;"">
      {BuildFeatureHtml("https://cpm.groupln.be/Img/icons/bell.png", "Op de hoogte blijven", "Ontvang een melding zodra er nieuwe punten voor u klaarstaan.")}
    </td>
  </tr>
</table>");

        AppendSpacer(sb, 24);

        // ── "Werkt de knop niet?" ─────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(redeemUrl))
        {
            sb.Append($@"
<!-- REDEEM LINK -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid {Border};border-radius:8px;border-collapse:separate;mso-table-lspace:0pt;mso-table-rspace:0pt;"">
  <tr>
    <td bgcolor=""#fafbfc"" style=""background:#fafbfc;padding:12px 18px;border-radius:8px 8px 0 0;border-bottom:1px solid {Border};"">
      <span style=""font-size:12px;font-weight:700;color:{TextMain};font-family:{Font};"">&#128279;&nbsp; Werkt de knop niet?</span>
    </td>
  </tr>
  <tr>
    <td bgcolor=""#f4f6f8"" style=""background:#f4f6f8;padding:12px 16px;border-radius:0 0 8px 8px;font-size:11px;color:{TextSoft};word-break:break-all;font-family:Courier New,Courier,monospace;"">
      <a href=""{HtmlEnc(redeemUrl)}"" style=""color:{Green};text-decoration:none;word-break:break-all;"">{HtmlEnc(redeemUrl)}</a>
    </td>
  </tr>
</table>");

            AppendSpacer(sb, 16);
        }

        // ── Warning box ───────────────────────────────────────────────────────
        sb.Append($@"
<!-- WARNING BOX -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;mso-table-lspace:0pt;mso-table-rspace:0pt;"">
  <tr>
    <td bgcolor=""{AmberBg}"" style=""background:{AmberBg};border-left:4px solid {AmberBorder};padding:12px 14px;border-radius:0 6px 6px 0;"">
      <span style=""font-size:12px;color:{AmberText};line-height:1.6;font-family:{Font};""><strong>Goed om te weten:</strong> deze uitnodiging blijft geldig en is persoonlijk. Meld u aan met het e-mailadres waarop u deze mail ontving &mdash; anders krijgt u geen toegang. Aanmelden heeft u maar &eacute;&eacute;n keer te doen; daarna logt u steeds met uw Microsoft-account in.</span>
    </td>
  </tr>
</table>");

        AppendSpacer(sb, 24);

        // ── Closing ───────────────────────────────────────────────────────────
        sb.Append($@"
<p style=""margin:0 0 22px;font-size:13px;color:{TextSoft};line-height:1.65;font-family:{Font};"">Heeft u vragen of lukt het aanmelden niet? Neem gerust contact op met uw projectleider &mdash; wij helpen u graag verder.</p>
<p style=""margin:0 0 4px;font-size:13px;color:{TextSoft};font-family:{Font};"">Met vriendelijke groeten,</p>
<p style=""margin:0;font-size:13px;color:{TextMain};font-family:{Font};""><strong style=""color:{GreenDark};"">Het team van {HtmlEnc(companyName)}</strong></p>");

        AppendFooter(sb, sentDate);
        return sb.ToString();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string BuildLoginButtonHtml(string href)
    {
        var safeHref = HtmlEnc(href);
        return $@"
<!--[if mso]>
<v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word""
  href=""{safeHref}"" style=""height:44px;v-text-anchor:middle;width:268px;"" arcsize=""12%"" stroke=""f"" fillcolor=""{Green}"">
  <w:anchorlock/>
  <center style=""color:#ffffff;font-family:Arial,sans-serif;font-size:14px;font-weight:bold;"">Aanmelden met Microsoft</center>
</v:roundrect>
<![endif]-->
<!--[if !mso]><!-- -->
<a href=""{safeHref}"" style=""background:{Green};color:#ffffff;display:inline-block;font-family:{Font};font-size:14px;font-weight:700;line-height:44px;text-align:center;text-decoration:none;width:268px;border-radius:7px;white-space:nowrap;"">Aanmelden met Microsoft</a>
<!--<![endif]-->";
    }

    private static string BuildStepHtml(int number, string title, string description)
    {
        return $@"
<table cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;mso-table-lspace:0pt;mso-table-rspace:0pt;"">
  <tr valign=""top"">
    <td width=""32"" style=""width:32px;padding-top:1px;"">
      {BuildNumberCircleHtml(number)}
    </td>
    <td style=""padding-left:12px;"">
      <div style=""font-size:13px;font-weight:700;color:{TextMain};margin-bottom:4px;font-family:{Font};"">{title}</div>
      <div style=""font-size:12px;color:{TextSoft};line-height:1.6;font-family:{Font};"">{description}</div>
    </td>
  </tr>
</table>";
    }

    private static string BuildNumberCircleHtml(int number)
    {
        return $@"
<!--[if mso]>
<v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word""
  style=""height:26px;v-text-anchor:middle;width:26px;"" arcsize=""50%"" stroke=""f"" fillcolor=""{Green}"">
  <w:anchorlock/>
  <center style=""color:#ffffff;font-family:Arial,sans-serif;font-size:12px;font-weight:bold;"">{number}</center>
</v:roundrect>
<![endif]-->
<!--[if !mso]><!-- -->
<span style=""display:inline-block;background:{Green};color:#ffffff;width:26px;height:26px;border-radius:13px;text-align:center;font-size:12px;font-weight:700;line-height:26px;font-family:{Font};"">{number}</span>
<!--<![endif]-->";
    }

    private static string BuildFeatureHtml(string iconUrl, string title, string description)
    {
        return $@"
<table cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;mso-table-lspace:0pt;mso-table-rspace:0pt;"">
  <tr valign=""top"">
    <td width=""36"" style=""width:36px;"">
      <div style=""width:30px;background:{GreenTint};border-radius:6px;text-align:center;padding:5px 0;font-size:0;line-height:0;""><img src=""{iconUrl}"" width=""20"" height=""20"" alt="""" style=""display:inline-block;"" /></div>
    </td>
    <td style=""padding-left:10px;"">
      <div style=""font-size:12.5px;font-weight:700;color:{TextMain};margin-bottom:3px;font-family:{Font};"">{title}</div>
      <div style=""font-size:11.5px;color:{TextSoft};line-height:1.55;font-family:{Font};"">{description}</div>
    </td>
  </tr>
</table>";
    }

    private static void AppendHeader(StringBuilder sb, string companyName, DateTime sentDate)
    {
        sb.Append($@"<!doctype html>
<html lang=""nl"">
<head>
<meta charset=""UTF-8"">
<title>Uitnodiging &mdash; {HtmlEnc(companyName)} Aannemersportaal</title>
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
<meta name=""x-apple-disable-message-reformatting"">
<style>
  table {{ border-spacing:0; }}
  td, th {{ border-collapse:collapse; }}
</style>
</head>
<body style=""margin:0;padding:0;background:#f4f6f8;"">

<!-- OUTER WRAPPER -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" bgcolor=""#f4f6f8"" style=""background:#f4f6f8;"">
  <tr>
    <td align=""center"" style=""padding:28px 16px 48px;"">

      <!-- CARD (700px) -->
      <table role=""presentation"" width=""700"" cellpadding=""0"" cellspacing=""0"" border=""0"" align=""center""
             style=""width:700px;border-collapse:collapse;mso-table-lspace:0pt;mso-table-rspace:0pt;"">

        <!-- BRAND BAR -->
        <tr>
          <td bgcolor=""{Green}"" style=""background:{Green};padding:26px 36px;border-radius:10px 10px 0 0;"">
            <div style=""font-size:10px;font-weight:700;color:#ffffff;text-transform:uppercase;letter-spacing:1px;margin-bottom:10px;font-family:{Font};""><img src=""https://cpm.groupln.be/Img/icons/key.png"" width=""14"" height=""14"" alt="""" style=""vertical-align:middle;margin-right:4px;"" />&nbsp; Uitnodiging aannemersportaal</div>
            <div style=""font-size:24px;font-weight:700;color:#ffffff;letter-spacing:.2px;margin-bottom:6px;font-family:{Font};"">Welkom bij het portaal van {HtmlEnc(companyName)}</div>
            <div style=""font-size:13px;color:#ffffff;font-family:{Font};"">U bent uitgenodigd om toegang te krijgen tot het aannemersportaal</div>
          </td>
        </tr>

        <!-- CARD BODY -->
        <tr>
          <td bgcolor=""#ffffff"" style=""background:#ffffff;padding:34px 38px;border-radius:0 0 10px 10px;font-family:{Font};-webkit-font-smoothing:antialiased;"">
");
    }

    private static void AppendFooter(StringBuilder sb, DateTime sentDate)
    {
        sb.Append($@"
          </td>
        </tr>
      </table><!-- /CARD -->

      <!-- FOOTER -->
      <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
             style=""border-collapse:collapse;mso-table-lspace:0pt;mso-table-rspace:0pt;"">
        <tr><td height=""22"" style=""height:22px;font-size:0;line-height:22px;mso-line-height-rule:exactly;"">&nbsp;</td></tr>
      </table>
      <table role=""presentation"" width=""700"" cellpadding=""0"" cellspacing=""0"" border=""0"" align=""center""
             style=""width:700px;margin-top:16px;border-collapse:collapse;mso-table-lspace:0pt;mso-table-rspace:0pt;"">
        <tr>
          <td align=""center"" style=""font-size:11px;color:{Muted};line-height:1.6;font-family:{Font};"">
            Group LN &nbsp;&middot;&nbsp; U ontvangt dit bericht omdat een beheerder u heeft uitgenodigd &nbsp;&middot;&nbsp; {sentDate:dd/MM/yyyy HH:mm}
          </td>
        </tr>
      </table>

    </td>
  </tr>
</table><!-- /OUTER WRAPPER -->

</body>
</html>");
    }

    private static void AppendSpacer(StringBuilder sb, int height)
    {
        sb.Append($@"
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
       style=""border-collapse:collapse;mso-table-lspace:0pt;mso-table-rspace:0pt;"">
  <tr><td height=""{height}"" style=""height:{height}px;font-size:0;line-height:{height}px;mso-line-height-rule:exactly;"">&nbsp;</td></tr>
</table>");
    }

    private static string HtmlEnc(string? s) => WebUtility.HtmlEncode(s ?? "");
}
