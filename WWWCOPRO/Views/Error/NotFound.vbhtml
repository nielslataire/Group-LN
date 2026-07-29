@Imports BO
@Code
    Layout = Nothing
    Dim pageTitle As String = "Pagina niet gevonden | Group LN"
End Code
<!DOCTYPE html>
<html lang="nl">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="robots" content="noindex, follow" />
    <title>@pageTitle</title>
    <link rel="icon" href="@Url.Content("~/content/img/favicon.ico")" type="image/x-icon" />
    <link href="https://fonts.googleapis.com/css2?family=Open+Sans:wght@400;600;700&family=Playfair+Display:wght@500;600&display=swap" rel="stylesheet" type="text/css">
    <link href="https://unpkg.com/boxicons@2.1.4/css/boxicons.min.css" rel="stylesheet" />
    <style>
        html, body {
            height: 100%;
            margin: 0;
        }

        body {
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            background: #F2F5EF;
            font-family: 'Open Sans', Arial, sans-serif;
            padding: 24px;
            box-sizing: border-box;
        }

        .error-page {
            text-align: center;
            max-width: 560px;
        }

        .error-code {
            font-family: 'Playfair Display', Georgia, serif;
            font-size: 96px;
            line-height: 1;
            color: #00532D;
            margin: 0;
        }

        .error-divider {
            width: 56px;
            height: 3px;
            background: #C9A96E;
            border: none;
            margin: 18px auto 22px;
        }

        .error-kicker {
            display: block;
            text-transform: uppercase;
            letter-spacing: 2px;
            font-size: 13px;
            font-weight: 700;
            color: #C9A96E;
            margin-bottom: 14px;
        }

        .error-heading {
            font-family: 'Playfair Display', Georgia, serif;
            font-weight: 600;
            font-size: 30px;
            color: #2C3B2A;
            margin: 0 0 16px;
        }

        .error-text {
            color: #5a6b58;
            font-size: 15px;
            line-height: 1.7;
            margin: 0 0 32px;
        }

        .error-actions {
            display: flex;
            gap: 16px;
            justify-content: center;
            flex-wrap: wrap;
            margin-bottom: 36px;
        }

        .error-btn {
            display: inline-block;
            background: #00532D;
            color: #fff;
            text-decoration: none;
            text-transform: uppercase;
            letter-spacing: 1px;
            font-size: 13px;
            font-weight: 700;
            padding: 14px 26px;
            border-radius: 3px;
            transition: background 0.15s ease;
        }

        .error-btn:hover {
            background: #006638;
            color: #fff;
        }

        .error-hr {
            border: none;
            border-top: 1px solid rgba(0, 83, 45, 0.12);
            margin: 0 0 24px;
        }

        .error-links {
            display: flex;
            gap: 28px;
            justify-content: center;
            flex-wrap: wrap;
            list-style: none;
            margin: 0;
            padding: 0;
        }

        .error-links a {
            color: #2c322e;
            font-weight: 600;
            text-decoration: none;
            font-size: 14px;
        }

        .error-links a:hover {
            color: #00532D;
        }

        .error-links i {
            margin-right: 6px;
            color: #00532D;
        }
    </style>
</head>
<body>
    <div class="error-page">
        <p class="error-code">404</p>
        <hr class="error-divider" />
        <span class="error-kicker">Pagina niet gevonden</span>
        <h1 class="error-heading">Deze pagina bestaat niet (meer)</h1>
        <p class="error-text">Het lijkt erop dat de pagina die u zoekt verplaatst of verwijderd is, of dat het adres niet juist is. Probeer een van onderstaande paden, of ga terug naar de startpagina.</p>
        <div class="error-actions">
            <a class="error-btn" href="@Url.Action("Index", "Home")">Naar de startpagina</a>
            <a class="error-btn" href="@Url.Action("Index", "Projects", New With {.id = UrlParameter.Optional, .Type = ProjectType.Woonproject})">Bekijk woonprojecten</a>
        </div>
        <hr class="error-hr" />
        <ul class="error-links">
            <li><a href="@Url.Action("Index", "References", New With {.id = UrlParameter.Optional})"><i class="bx bx-buildings"></i>Realisaties</a></li>
            <li><a href="@Url.Action("Index", "Blog")"><i class="bx bx-news"></i>Blog</a></li>
            <li><a href="@Url.Action("Index", "Contact")"><i class="bx bx-phone"></i>Contacteer ons</a></li>
        </ul>
    </div>
</body>
</html>
