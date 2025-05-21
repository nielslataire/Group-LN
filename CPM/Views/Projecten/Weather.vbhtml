@modeltype BWDModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Projecten - Weerverlet"
End Code
@section PageStyle
<link rel="stylesheet" href="~/vendor/admin/bootstrap-year-calendar/bootstrap-year-calendar.css" />
End Section

<script src="~/vendor/admin/bootstrap-year-calendar/bootstrap-year-calendar.js"></script>
<script src="~/vendor/admin/bootstrap-year-calendar/bootstrap-year-calendar.nl.js"  charset="UTF-8"></script>
<div class="row">
    <div class="col-xs-12 ">
        <div class="panel">
            <header class="panel-heading">
                <h4 class="panel-title">Selecteer het weerstation :</h4>
            </header>
            <div class="panel-body ">
                @Html.DropDownListFor(Function(m) m.SelectedWeatherStation, New SelectList(Model.WeatherStations, "ID", "Display", Model.SelectedWeatherStation), New With {.class = "form-control populate", .id = "lstWeatherStations"})
            </div>
            </div>
        </div>
     
</div>
<div class="row">

        <div class="col-xs-6 ">
            <div class="panel">
                <header class="panel-heading">
                    <h2 class="panel-title">Regen en vorst verlet</h2>
                </header>
                <div class="panel-body ">

                    <div id="raincalendar" style="line-height:1.42857143">
                    </div>
                </div>
            </div>
        </div>
        <div class="col-xs-6">
            <div class="panel">
                <header class="panel-heading">
                    <h2 class="panel-title">Wind verlet</h2>
                </header>
                <div class="panel-body ">

                    <div id="windcalendar" style="line-height:1.42857143">
                    </div>
                </div>
            </div>
        </div>

</div>
@section scripts
    <script>
        $('#lstWeatherStations').on('change', function () {
            $('#raincalendar').data('calendar').setDataSource(getMyData(0, this.value, 2016));
            $('#windcalendar').data('calendar').setDataSource(getMyData(1, this.value, 2016));
        });
        function getMyData(itype, iwsid, iyear) {

            var myData = [];
            $.ajax({
                url: '@Url.Action("GetBadWeatherDays", "Projecten")',
                async: false,
                cache: false,
                traditional: true,
                data: { type: itype, weatherstationid: iwsid, year: iyear },
                success: function (response) {

                    for (var i = 0; i < response.length; i++) {
                        myData.push({
                            id: response[i].id,
                            title: response[i].title,
                            startDate: new Date(response[i].year, response[i].month - 1, response[i].day),
                            endDate: new Date(response[i].year, response[i].month - 1, response[i].day),
                            color: response[i].color,
                        });

                    }
                }
            });
            return myData;
        };
        function getVacationDays() {

            var myData = [];
            $.ajax({
                url: '@Url.Action("GetVacationDays", "Projecten")',
                async: false,
                cache: false,
                traditional: true,
                success: function (response) {
                    for (var i = 0; i < response.length; i++) {
                        myData.push(new Date(response[i].year, response[i].month - 1, response[i].day))
                    }
                }
            });
            return myData;
        };
        $(document).ready(function () {
            $('#raincalendar').calendar({
                dataSource: getMyData(0, $('#lstWeatherStations').val(), 2016),
                style: "custom",
                language: "nl",
                disabledWeekDays: [0, 6],
                disabledDays: getVacationDays(),
                displayDisabledDataSource:true,
                customDataSourceRenderer: function (element, date, events) {
                    for (i in events) {
                        $(element).css('background-color', events[i].color);
                        //if (events[i].color === "#777") {
                        //    $(element).unbind();
                        //    $(element).unbind("clickDay");
                        //}
                    };
                        $(element).css('color', 'white');
                        $(element).css('border-radius', '0px');
                },
                //data: { dag: e.date.toLocaleDateString(), weatherstationid: $('#lstWeatherStations').val(), type: 0 },
                clickDay: function (e) {
                   
                    var dataSource = $('#raincalendar').data('calendar').getDataSource();
                    if (e.events.length > 0) {
                        
                        for (ev in e.events) {
                            for (var i in dataSource) {
                                if (dataSource[i].id == e.events[ev].id) {
                                   
                                    $.ajax({
                                        url: '@Url.Action("DeleteBadWeatherDay", "Projecten")',
                                        type: "POST",
                                        async: false,
                                        cache: false,
                                        traditional: true,
                                        data: { id: e.events[ev].id},
                                        success: function (response) {
                                            dataSource.splice(i, 1); 
                                        },
                                    });
                                    
                                }
                            }
                        }
                    
                    }
                    else {
                        var newid = 0
                        $.ajax({
                            url: '@Url.Action("AddBadWeatherDay", "Projecten")',
                            type: "POST",
                            async:false,
                            cache: false,
                            traditional: true,
                            data: { dag: e.date.toLocaleString().replace(/\u200E/g, ''), weatherstationid: $('#lstWeatherStations').val(), type: 0 },
                            success: function (response) {
                                dataSource.push({
                                    id: response,
                                    title: 'regen',
                                    startDate: e.date,
                                    endDate: e.date,
                                    color: "#009336",
                                });
                                
                            },
                            
                        });
                        
                    }
                    
                    $('#raincalendar').data('calendar').setDataSource(dataSource);
                    
                }
                  
            });

            $('#windcalendar').calendar({
                dataSource: getMyData(1, $('#lstWeatherStations').val(), 2016),
                style: "custom",
                language: "nl",
                disabledWeekDays: [0, 6],
                disabledDays: getVacationDays(),
                displayDisabledDataSource: true,
                customDataSourceRenderer: function (element, date, events) {
                    for (i in events) {
                        $(element).css('background-color', events[i].color);
                    };
                    $(element).css('color', 'white');
                    $(element).css('border-radius', '0px');
                },
                clickDay: function (e) {

                    var dataSource = $('#windcalendar').data('calendar').getDataSource();
                    if (e.events.length > 0) {
                        for (ev in e.events) {
                            for (var i in dataSource) {
                                if (dataSource[i].id == e.events[ev].id) {

                                    $.ajax({
                                        url: '@Url.Action("DeleteBadWeatherDay", "Projecten")',
                                        type: "POST",
                                        async: false,
                                        cache: false,
                                        traditional: true,
                                        data: { id: e.events[ev].id },
                                        success: function (response) {
                                            dataSource.splice(i, 1);
                                        },

                                    });
                                    break;
                                }
                            }
                        }

                    }
                    else {
                        var newid = 0
                        $.ajax({
                            url: '@Url.Action("AddBadWeatherDay", "Projecten")',
                            type: "POST",
                            async: false,
                            cache: false,
                            traditional: true,
                            data: { dag: e.date.toLocaleString().replace(/\u200E/g, ''), weatherstationid: $('#lstWeatherStations').val(), type: 1 },
                            success: function (response) {
                                dataSource.push({
                                    id: response,
                                    title: 'wind',
                                    startDate: e.date,
                                    endDate: e.date,
                                    color: "#009336",
                                });

                            },

                        });

                    }

                    $('#windcalendar').data('calendar').setDataSource(dataSource);

                }

            });
            $('#raincalendar').data('calendar').setMaxDate(new Date());
            $('#windcalendar').data('calendar').setMaxDate(new Date());
            //$('#simpliest-usage').datepicker({
            //    multidate: true,
            //    format: 'dd/mm/yyyy',
            //    daysOfWeekDisabled: '0,6',
            //    disableTouchKeyboard: true,
            //    language:'nl-BE',
            //});
        });
        </script>
    end section

