To: niels.lataire@groupln.be
From: @ViewBag.To
Subject: Website Group LN : @ViewBag.Title
<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
</head>
<body style="margin:0; padding:0; background-color:#f0f1f4; font-family:Arial, Helvetica, sans-serif;">
    <table width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#f0f1f4; padding-top:20px; padding-bottom:20px;">
        <tr>
            <td align="center">
                <table width="640" cellspacing="0" cellpadding="0" border="0" style="background-color:#ffffff; border-radius:6px; overflow:hidden; font-family:Arial, Helvetica, sans-serif;">
                    <tr>
                        <td style="padding:18px 22px 10px 22px;">
                            <table width="100%" cellspacing="0" cellpadding="0" border="0">
                                <tr>
                                    <td style="width:220px;" valign="middle">
                                        <a href="https://www.groupln.be" target="_blank" style="text-decoration:none; color:#009336;">
                                            <img src="https://www.copro-bouwteam.be/content/img/logo-default.png" alt="Group LN" height="54" style="display:block; border:0;" />
                                        </a>
                                    </td>
                                    <td align="right" style="color:#8a8a8a; font-size:11px; font-weight:bold; letter-spacing:0.5px; text-transform:uppercase;">
                                        Interne melding
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style="background-color:#009336; color:#ffffff; padding:24px 22px 18px 22px; text-align:center;">
                            <div style="font-size:24px; font-weight:700; line-height:30px;">Nieuwe contactaanvraag</div>
                            @If Not String.IsNullOrWhiteSpace(ViewBag.ContactName) Then
                                @<div style="margin-top:6px; font-size:16px; line-height:22px;">Van @ViewBag.ContactName</div>
                            End If
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:22px 22px 14px 22px; color:#4a4a4a; font-size:14px; line-height:22px;">
                            <div style="margin-bottom:10px; font-size:16px; font-weight:600; color:#222;">Aanvraaggegevens</div>
                            <table width="100%" cellspacing="0" cellpadding="0" border="0" style="border-collapse:collapse;">
                                @If Not String.IsNullOrWhiteSpace(ViewBag.ContactName) Then
                                    @<tr>
                                        <td style="padding:8px 6px; color:#777; width:34%;">Naam</td>
                                        <td style="padding:8px 6px; color:#222;">@ViewBag.ContactName</td>
                                    </tr>
                                End If
                                <tr>
                                    <td style="padding:8px 6px; color:#777; width:34%;">Email</td>
                                    <td style="padding:8px 6px; color:#222;">@ViewBag.To</td>
                                </tr>
                                @If Not String.IsNullOrWhiteSpace(ViewBag.Phone) Then
                                    @<tr>
                                        <td style="padding:8px 6px; color:#777; width:34%;">Telefoon</td>
                                        <td style="padding:8px 6px; color:#222;">@ViewBag.Phone</td>
                                    </tr>
                                End If
                                <tr>
                                    <td style="padding:8px 6px; color:#777; width:34%;">Onderwerp</td>
                                    <td style="padding:8px 6px; color:#222;">@ViewBag.Title</td>
                                </tr>
                                <tr>
                                    <td style="padding:8px 6px; color:#777; width:34%; vertical-align:top;">Vraag</td>
                                    <td style="padding:8px 6px; color:#222;">@ViewBag.Message</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:18px 22px 24px 22px; color:#9a9a9a; font-size:12px; line-height:18px; text-align:center; border-top:1px solid #e6e6e6;">
                            Deze melding werd automatisch verstuurd vanuit de website.
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>