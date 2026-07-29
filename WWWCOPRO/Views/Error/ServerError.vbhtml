@Imports BO
@Code
    Layout = Nothing
    Dim pageTitle As String = "Er ging iets mis | Group LN"
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
            color: #B5533E;
            margin: 0;
        }

        .error-divider {
            width: 56px;
            height: 3px;
            background: #B5533E;
            border: none;
            margin: 18px auto 22px;
        }

        .error-kicker {
            display: block;
            text-transform: uppercase;
            letter-spacing: 2px;
            font-size: 13px;
            font-weight: 700;
            color: #00532D;
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
            margin-bottom: 24px;
        }

        .error-btn {
            display: inline-block;
            text-decoration: none;
            text-transform: uppercase;
            letter-spacing: 1px;
            font-size: 13px;
            font-weight: 700;
            padding: 14px 26px;
            border-radius: 3px;
            cursor: pointer;
            font-family: inherit;
            transition: background 0.15s ease, color 0.15s ease;
        }

        .error-btn-solid {
            background: #00532D;
            color: #fff;
            border: 1px solid #00532D;
        }

        .error-btn-solid:hover {
            background: #006638;
            border-color: #006638;
            color: #fff;
        }

        .error-btn-outline {
            background: transparent;
            color: #00532D;
            border: 1px solid #00532D;
        }

        .error-btn-outline:hover {
            background: rgba(0, 83, 45, 0.06);
            color: #00532D;
        }

        .error-notice {
            display: inline-flex;
            align-items: center;
            gap: 10px;
            background: #fff;
            border: 1px solid rgba(0, 83, 45, 0.15);
            border-radius: 40px;
            padding: 12px 22px;
            font-size: 14px;
            color: #5a6b58;
            margin-bottom: 32px;
        }

        .error-notice i {
            color: #7A9E6E;
        }

        .error-hr {
            border: none;
            border-top: 1px solid rgba(0, 83, 45, 0.12);
            margin: 0 0 24px;
        }

        .error-contact {
            display: flex;
            gap: 32px;
            justify-content: center;
            flex-wrap: wrap;
            list-style: none;
            margin: 0;
            padding: 0;
        }

        .error-contact a {
            color: #2C3B2A;
            text-decoration: none;
            font-size: 14px;
        }

        .error-contact a:hover {
            color: #00532D;
        }

        .error-contact i {
            margin-right: 6px;
            color: #00532D;
        }
    </style>
</head>
<body>
    <div class="error-page">
        <p class="error-code">500</p>
        <hr class="error-divider" />
        <span class="error-kicker">Er ging iets mis</span>
        <h1 class="error-heading">Onze server ondervindt een probleem</h1>
        <p class="error-text">Er is iets misgelopen bij het laden van deze pagina. Dit ligt niet aan u &mdash; probeer de pagina te vernieuwen, of kom later nog eens terug. Ons team is automatisch op de hoogte gebracht.</p>
        <div class="error-actions">
            <button type="button" class="error-btn error-btn-solid" onclick="location.reload()">Pagina vernieuwen</button>
            <a class="error-btn error-btn-outline" href="@Url.Action("Index", "Home")">Naar de startpagina</a>
        </div>
        <div class="error-notice">
            <i class="bx bx-info-circle"></i>
            <span>Blijft dit probleem zich voordoen? Neem gerust contact op.</span>
        </div>
        <hr class="error-hr" />
        <ul class="error-contact">
            <li><a href="tel:+3292164950"><i class="bx bx-phone"></i>+32 (0)9 216 49 50</a></li>
            <li><a href="mailto:info@groupln.be"><i class="bx bx-envelope"></i>info@groupln.be</a></li>
        </ul>
    </div>
</body>
</html>
