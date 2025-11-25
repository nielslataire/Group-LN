@Code
    Dim requestType As String = If(String.IsNullOrWhiteSpace(ViewBag.RequestType), "planaanvraag", ViewBag.RequestType)
    Dim requestTitle As String = If(String.IsNullOrWhiteSpace(ViewBag.RequestTitle), "Nieuwe planaanvraag", ViewBag.RequestTitle)
End Code
To: niels.lataire@groupln.be
From: info@bouwenconstructie.be
Subject: Website BCO - @requestType
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
                    <tr>
                        <td style="padding:18px 22px 10px 22px;">
                            <table width="100%" cellspacing="0" cellpadding="0" border="0">
                                <tr>
                                    <td style="width:220px;" valign="middle">
                                        <a href="http://www.bouwenconstructie.be" target="_blank" style="text-decoration:none; color:#009336;">
                                            <img src="http://www.groupln.be/content/img/logo-bco.gif" alt="BCO - Bouw en constructie" height="54" style="display:block; border:0;" />
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
                            <div style="font-size:26px; font-weight:700; line-height:32px;">@requestTitle</div>
                            @If Not String.IsNullOrWhiteSpace(ViewBag.Project) Then
                                @<div style="margin-top:6px; font-size:16px; line-height:22px;">@ViewBag.Project</div>
                            End If
                            @If Not String.IsNullOrWhiteSpace(ViewBag.Unit) Then
                                @<div style="margin-top:4px; font-size:14px; line-height:20px; opacity:0.9;">Eenheid: @ViewBag.Unit</div>
                            End If
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:22px 22px 8px 22px; color:#4a4a4a; font-size:14px; line-height:22px;">
                            <div style="margin-bottom:10px; font-size:16px; font-weight:600; color:#222;">Aanvraaggegevens</div>
                            <table width="100%" cellspacing="0" cellpadding="0" border="0" style="border-collapse:collapse;">
                                @If Not String.IsNullOrWhiteSpace(requestType) Then
                                    @<tr>
                                        <td style="padding:8px 6px; color:#777; width:34%;">Type aanvraag</td>
                                        <td style="padding:8px 6px; color:#222;">@requestType</td>
                                    </tr>
                                End If
                                @If Not String.IsNullOrWhiteSpace(ViewBag.Project) Then
                                    @<tr>
                                        <td style="padding:8px 6px; color:#777; width:34%;">Project</td>
                                        <td style="padding:8px 6px; color:#222;">@ViewBag.Project</td>
                                    </tr>
                                End If
                                @If Not String.IsNullOrWhiteSpace(ViewBag.Unit) Then
                                    @<tr>
                                        <td style="padding:8px 6px; color:#777; width:34%;">Eenheid</td>
                                        <td style="padding:8px 6px; color:#222;">@ViewBag.Unit</td>
                                    </tr>
                                End If
                                @If Not String.IsNullOrWhiteSpace(ViewBag.Name) Then
                                    @<tr>
                                        <td style="padding:8px 6px; color:#777; width:34%;">Naam</td>
                                        <td style="padding:8px 6px; color:#222;">@ViewBag.Name</td>
                                    </tr>
                                End If
                                @If Not String.IsNullOrWhiteSpace(ViewBag.Firstname) Then
                                    @<tr>
                                        <td style="padding:8px 6px; color:#777; width:34%;">Voornaam</td>
                                        <td style="padding:8px 6px; color:#222;">@ViewBag.Firstname</td>
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
                                @If Not String.IsNullOrWhiteSpace(ViewBag.Question) Then
                                    @<tr>
                                        <td style="padding:8px 6px; color:#777; width:34%;">Vraag</td>
                                        <td style="padding:8px 6px; color:#222;">@ViewBag.Question</td>
                                    </tr>
                                End If
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