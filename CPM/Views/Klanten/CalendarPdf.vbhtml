@Imports BO
@ModelType ClientCalendarModel
@Code
    Layout = Nothing
End Code
@section PageStyle

End Section


<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Kalender Weerverlet</title>
  
    <!-- Web Fonts  -->
    <link href="//fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800" rel="stylesheet" type="text/css">

    <!-- Vendor CSS -->
    <link rel="stylesheet" href="~/vendor/bootstrap/css/bootstrap.css" />

    <!-- Invoice Print Style -->
    @*<link rel="stylesheet" href="~/Content/Admin/invoice-print.css"  />*@
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-year-calendar/bootstrap-year-calendar.css" />
    <style> 
        html{-webkit-print-color-adjust:exact;}
        thead{display:table-header-group;}
        tfoot{display:table-row-group;}
        tr{page-break-inside:avoid;}
        .breakhere { page-break-after: always }
    </style>
    <script src="~/vendor/admin/jquery/jquery.js"></script>
    <script src="~/vendor/admin/bootstrap-year-calendar/bootstrap-year-calendar-print.js"></script>
    <script src="~/vendor/admin/bootstrap-year-calendar/bootstrap-year-calendar.nl.js" charset="UTF-8"></script>
    <script>

        function getMyData() {

            var myData = [];
            @For Each item In Model.Days
                @<text>
                myData.push({
                    id: @item.Id,
                    title: '@item.Title',
                    startDate: new Date(@item.Year, @item.Month - 1, @item.Day),
                    endDate: new Date(@item.Year, @item.Month - 1, @item.Day),
                    color: '@item.Color',
                });
                </text>
            Next
            return myData;
        };
        $(document).ready(function () {
            for (i = @Model.Startdate.Year; i <= @Date.Now.Year(); i++) {


                $('#calendar_' + i).calendar({
                    dataSource: getMyData(),
                    style: "custom",
                    language: "nl",
                    disabledWeekDays: [0, 6],
                    displayDisabledDataSource:true,
                    customDataSourceRenderer: function (element, date, events) {
                        for (j in events) {

                            //$(element).css('background-color', events[j].color);
                            $(element).attr('style', 'background-color: ' + events[j].color +' !important; color:white !important; border-radius:0px');
                        };
                        //$(element).css('color', 'white');
                        //$(element).css('border-radius', '0px');
                    },
                    //data: { dag: e.date.toLocaleDateString(), weatherstationid: $('#lstWeatherStations').val(), type: 0 },
                });
                $('#calendar_'+i).data('calendar').setYear(i);

                if (i == @Model.Startdate.Year) {
                    $('#calendar_'+i).data('calendar').setMinDate(new Date(@Model.Startdate.Month +'/'+ @Model.Startdate.Day + '/' + @Model.Startdate.Year));
            } else {
                                $('#calendar_'+i).data('calendar').setMinDate(new Date('1/1/' + i));
        }
                if (i == @Date.Now.Year()) {
                                    $('#calendar_'+i).data('calendar').setMaxDate(new Date(@Date.Now.Month +'/'+ @Date.Now.Day + '/' + @Date.Now.Year));
        } else {
            $('#calendar_'+i).data('calendar').setMaxDate(new Date('12/31/' + i));
        }

        }


        });
    </script>
</head>
<body>
    <div class="invoice">
        <header class="clearfix">
            <div class="row">
                <div class="col-sm-12 mt-md text-center">
                    <h2 Class="h2 m-none text-dark text-weight-bold">Kalender weerverlet</h2>
                    </div>
                </div>
                <div class="row">
                    <div class="col-xs-6 mt-md">
                        <h4 class="mt-none mb-sm text-dark text-weight-bold">
                            @if Model.Client.CompanyName Is Nothing Then
                                 @Model.Client.Salutation.GetDisplayName().ToUpper()
                            End If
                            @Html.Raw(" ") @Model.Client.DisplayName.ToUpper()
                        </h4>
                        <div class="col-xs-1" style="background-color:#777;line-height:1.42857143;">
                            &nbsp;
                        </div>
                        <div class="col-xs-11">
                            vakantiedagen bouw
                        </div>
                        <div class="col-xs-1" style="background-color:red;line-height:1.42857143;">
                            &nbsp;
                        </div>
                        <div class="col-xs-11">
                            windverlet
                        </div>
                        <div class="col-xs-1" style="background-color:#009336;line-height:1.42857143;">
                            &nbsp;
                        </div>
                        <div class="col-xs-11">
                            regen/vorstverlet
                        </div>
                        </div>
                    <div class="col-xs-6 text-right text-dark mt-md mb-md">

                        @Html.ValueFor(Function(m) m.Client.Street) @Html.ValueFor(Function(m) m.Client.Housenumber)
                        @If Model.Client.Busnumber IsNot Nothing Then
                @<text>
                    / @Html.ValueFor(Function(m) m.Client.Busnumber)
                </text>
                        End If
                        <br />
                        @Html.ValueFor(Function(m) m.Client.Postalcode.Postcode) @Html.ValueFor(Function(m) m.Client.Postalcode.Gemeente).ToString.ToUpper <br />
                        @Html.ValueFor(Function(m) m.Client.Postalcode.Country.Name)

                        <br />
                        @if Model.Client.VATnumber IsNot Nothing Then
                @<text>

                    <strong> BTW-nummer : </strong>@Html.ValueFor(Function(m) m.Client.VATnumber)<br />
                </text>
                        End If
                    <strong> Aktedatum :                 </strong>@Html.DisplayFor(Function(m) m.Client.DateDeedOfSale)<br />
                    <strong> Startdatum werken :                 </strong>@Html.DisplayFor(Function(m) m.Client.StartDateConstruction)<br />
                        @If Model.Client.ExecutionDays = 0 Then
                            @<text>
                        <strong> Uitvoeringstermijn :                    </strong>@Html.DisplayFor(Function(m) m.ExecutionDays) werkbare dagen<br />
                        </text>
                        Else
                            @<text>
                                <strong> Uitvoeringstermijn :                    </strong>@Html.DisplayFor(Function(m) m.Client.ExecutionDays) werkbare dagen<br />
                            </text>
                        End If
                        <br />

                    </div>
                </div>
</header>
        <div>
            @For year As Integer = Model.Startdate.Year To Date.Now().Year

                @<text>
                    <div id="calendar_@year" style="line-height:1.42857143;">
                    </div>

                </text>
                If Not year = Date.Now.Year() Then
                    @<text>

                    <P CLASS="breakhere">
                    </text>
                End If

            Next

</div>
</div>
</body>
</html>



