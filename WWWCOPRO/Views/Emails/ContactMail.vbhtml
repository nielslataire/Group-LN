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
                                            <img src="https://www.groupln.be/content/img/logo-default.png" alt="Group LN" height="54" style="display:block; border:0;" />
                                        </a>
                                    </td>
                                    <td align="right" style="color:#8a8a8a; font-size:11px; font-weight:bold; letter-spacing:0.5px; text-transform:uppercase;">
                                        Bevestiging contactaanvraag
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style="background-color:#009336; color:#ffffff; padding:24px 22px 18px 22px; text-align:center;">
                            <div style="font-size:24px; font-weight:700; line-height:30px;">Bedankt voor uw bericht</div>
                            <div style="margin-top:6px; font-size:16px; line-height:22px;">Wij nemen zo spoedig mogelijk contact met u op.</div>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:22px 22px 14px 22px; color:#4a4a4a; font-size:14px; line-height:22px;">
                            <p style="margin:0 0 12px 0; font-size:15px; color:#222;">Beste @ViewBag.ContactName,</p>
                            <p style="margin:0 0 16px 0;">Wij bevestigen de ontvangst van uw vraag. Hieronder vindt u een overzicht van wat u ons bezorgde.</p>
                            <div style="margin-bottom:10px; font-size:16px; font-weight:600; color:#222;">Uw gegevens</div>
                            <table width="100%" cellspacing="0" cellpadding="0" border="0" style="border-collapse:collapse;">
                                <tr>
                                    <td style="padding:8px 6px; color:#777; width:34%;">Naam</td>
                                    <td style="padding:8px 6px; color:#222;">@ViewBag.ContactName</td>
                                </tr>
                                <tr>
                                    <td style="padding:8px 6px; color:#777; width:34%;">Email</td>
                                    <td style="padding:8px 6px; color:#222;">@ViewBag.To</td>
                                </tr>
                                <tr>
                                    <td style="padding:8px 6px; color:#777; width:34%;">Onderwerp</td>
                                    <td style="padding:8px 6px; color:#222;">@ViewBag.Title</td>
                                </tr>
                                <tr>
                                    <td style="padding:8px 6px; color:#777; width:34%; vertical-align:top;">Uw vraag</td>
                                    <td style="padding:8px 6px; color:#222;">@ViewBag.Message</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:18px 22px 24px 22px; color:#9a9a9a; font-size:12px; line-height:18px; text-align:center; border-top:1px solid #e6e6e6;">
                            Dit bericht werd automatisch verstuurd vanuit de website. Gelieve niet te antwoorden op deze email.
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>