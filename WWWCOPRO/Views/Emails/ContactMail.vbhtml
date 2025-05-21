To: @ViewBag.To
From: info@groupln.be
Subject: Group LN - uw contactvraag
<!DOCTYPE html>
<html>
<head>
    <!-- Web Fonts  -->

    <link href="http://www.copro-bouwteam.be/vendor/bootstrap/css/bootstrap.min.css" rel="stylesheet">
    <link href="http://www.copro-bouwteam.be/vendor/font-awesome/css/font-awesome.min.css" rel="stylesheet">
    <link href="http://www.copro-bouwteam.be/content/theme.css" rel="stylesheet">
    <link href="http://www.copro-bouwteam.be/content/theme-elements.css" rel="stylesheet">
    <link href="http://www.copro-bouwteam.be/content/custom.css" rel="stylesheet">
    <link href="http://www.copro-bouwteam.be/content/skins/skin-corporate-3.css" rel="stylesheet">
  


</head>
<body>
    <div class="body" style="font-family:Arial, Helvetica, sans-serif">
        <p>Beste @ViewBag.ContactName,</p>

        <p>
            Wij hebben uw vraag in goede orde ontvangen, en nemen zo spoedig mogelijk contact met u op betreffende uw vraag.
            Hieronder vindt u ter bevestiging de vraag die u gesteld heeft:
        </p>
        <table>
            <tr><td class="text-color-primary" style="font-family:Arial, Helvetica, sans-serif">Uw onderwerp : </td><td style="width:50px;font-family:Arial, Helvetica, sans-serif"></td><td style="font-family:Arial, Helvetica, sans-serif">@ViewBag.Title</td></tr>
            <tr><td class="text-color-primary " style="font-family:Arial, Helvetica, sans-serif">Uw vraag : </td><td style="width:50px;font-family:Arial, Helvetica, sans-serif"></td><td style="font-family:Arial, Helvetica, sans-serif">@ViewBag.Message</td></tr>
        </table>
        <br />
        <br />
        <table>
            <tr>
                <td>
                    <h5 class="heading-primary" style="font-family:Arial, Helvetica, sans-serif;font-size:10pt;">Group LN</h5>
                    <p style="font-family:Arial, Helvetica, sans-serif;font-size:8pt;">Klaverdries 53, 9031 Drongen<br />Telefoon : +32 (0)9 223 03 27<br />Email : <a href="mailto:info@groupln.be">info@groupln.be</a></p>
                </td>
            </tr>
            <tr>
                <td><img alt="Copro" height="80" src="http://www.groupln.be/Content/img/logo-default.png"></td>
                <td width="50">&nbsp;</td>
                
            </tr>
        </table>
    </div>

   
</body>
</html>


