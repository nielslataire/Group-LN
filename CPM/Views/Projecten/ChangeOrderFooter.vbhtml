@modeltype String
<!DOCTYPE html>
<html>
<head>
    <link rel="stylesheet" href="~/vendor/bootstrap/css/bootstrap.css" />
    <link rel="stylesheet" href="~/Content/Admin/invoice-print.css" />
</head>
<body>
    <div class="invoice"> 
        <div class="col-sm-12 mt-md">
            <section id="pnlList">
                <div>
                    <table class="table table-bordered">
                        <tr>
                            <td style="font-size:16px;text-align:center;" width="40%">VOORWAARDEN</td>
                            <td style="font-size:16px;text-align:center;" width="30%">DATUM</td>
                            <td style="font-size:16px;text-align:center;" width="30%">HANDTEKENING VOOR AKKOORD</td>
                        </tr>
                        <tr style="height:75px;">
                            <td style="font-size:12px;text-align:center;">@Model</td>
                            <td style="font-size:12px;text-align:center;"></td>
                            <td style="font-size:12px;text-align:center;"></td>
                        </tr>
                    </table>
                </div>
            </section>
        </div>
        </div>
</body>
</html>