<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>@ViewData("Title")</title>
    <link href="http://fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800|Shadows+Into+Light" rel="stylesheet" type="text/css">

    @Styles.Render("~/Vendor/Admin/css")
    @Styles.Render("~/Theme/Admin/css")
    @Scripts.Render("~/bundles/admin/modernizr")
</head>

<body>
    <div>
        @RenderBody()
    </div>
    @Scripts.Render("~/bundles/admin/vendor")
    @Scripts.Render("~/bundles/admin/theme")
    @RenderSection("scripts", required:=False)
</body>
</html>
