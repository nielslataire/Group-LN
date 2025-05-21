To: niels.lataire@groupln.be
From: info@bouwenconstructie.be
Subject: Website BCO : @ViewBag.title
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
        <p>@ViewBag.ContactName heeft volgende vraag :</p>
        <p>@ViewBag.Message</p>
        <p>Telefoon / GSM : @ViewBag.Phone</p>

      
    </div>

   
</body>
</html>


