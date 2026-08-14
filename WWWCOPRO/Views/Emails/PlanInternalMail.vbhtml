
<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
</head>

<body style="margin:0; padding:0; background-color:#f0f1f4;">
    <table width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#f0f1f4; padding-top:20px; padding-bottom:20px;">
        <tr>
            <td align="center">

                <table width="640" cellspacing="0" cellpadding="0" border="0" style="background-color:#ffffff; border-radius:6px; overflow:hidden; font-family:Arial, Helvetica, sans-serif;">

                    <!-- HEADER -->
                    <tr>
                        <td style="padding:18px 22px 10px 22px;">
                            <table width="100%">
                                <tr>
                                    <td style="width:220px;">
                                        <a href="https://www.groupln.be" target="_blank">
                                            <img src="https://www.groupln.be/content/img/logo-default.png"
                                                 alt="Group LN" height="54" style="display:block; border:0;" />
                                        </a>
                                    </td>

                                    <td align="right" style="color:#8a8a8a; font-size:11px; font-weight:bold; letter-spacing:0.5px; text-transform:uppercase;">
                                        Interne melding
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- GREEN TITLE BAR -->
                    <tr>
                        <td style="background-color:#009336; color:#ffffff; padding:24px 22px 18px 22px; text-align:center;">

                            <div style="font-size:26px; font-weight:700; line-height:32px;">
                                @Model.RequestTitle
                            </div>

<div style="margin-top:6px; font-size:16px; line-height:22px;">
                                    @Model.Project
                                </div>

                            
<div style="margin-top:4px; font-size:14px; line-height:20px; opacity:0.9;">
                                    Eenheid: @Model.Unit
                                </div>


                        </td>
                    </tr>

                    <!-- CONTENT BLOCK -->
                    <tr>
                        <td style="padding:22px 22px 8px 22px; color:#4a4a4a; font-size:14px; line-height:22px;">

                            <div style="margin-bottom:10px; font-size:16px; font-weight:600; color:#222;">
                                Aanvraaggegevens
                            </div>

                            <table width="100%" style="border-collapse:collapse;">

<tr>
                                    <td style="padding:8px 6px; color:#777; width:34%;">Type aanvraag</td>
                                    <td style="padding:8px 6px; color:#222;">@Model.RequestType</td>
                                </tr>
<tr>
                                        <td style="padding:8px 6px; color:#777;">Project</td>
                                        <td style="padding:8px 6px; color:#222;">@Model.Project</td>
                                    </tr>
<tr>
                                        <td style="padding:8px 6px; color:#777;">Eenheid</td>
                                        <td style="padding:8px 6px; color:#222;">@Model.Unit</td>
                                    </tr>
<tr>
                                        <td style="padding:8px 6px; color:#777;">Naam</td>
                                        <td style="padding:8px 6px; color:#222;">@Model.Name</td>
                                    </tr>
<tr>
                                        <td style="padding:8px 6px; color:#777;">Voornaam</td>
                                        <td style="padding:8px 6px; color:#222;">@Model.Firstname</td>
                                    </tr>


                                <tr>
                                    <td style="padding:8px 6px; color:#777;">Email</td>
                                    <td style="padding:8px 6px; color:#222;">@Model.To</td>
                                </tr>

                                    <tr>
                                        <td style="padding:8px 6px; color:#777;">Telefoon</td>
                                        <td style="padding:8px 6px; color:#222;">@Model.Phone</td>
                                    </tr>

<tr>
                                        <td style="padding:8px 6px; color:#777;">Vraag</td>
                                        <td style="padding:8px 6px; color:#222;">@Model.Question</td>
                                    </tr>

                            </table>

                        </td>
                    </tr>

                    <!-- FOOTER -->
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
