@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Projecten - Vakantiedagen"
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
                    <h2 class="panel-title">Verlofdagen</h2>
                </header>
                <div class="panel-body ">

                    <div id="vacationcalendar" style="line-height:1.42857143">
                    </div>
                </div>
            </div>
        </div>
      

</div>
@section scripts
    <script>
     
        function getMyData() {

            var myData = [];
            $.ajax({
                url: '@Url.Action("GetVacationDays", "Admin")',
                async: false,
                cache: false,
                traditional: true,
                success: function (response) {
                    for (var i = 0; i < response.length; i++) {
                        myData.push({
                            id: response[i].id,
                            title: "verlofdag",
                            startDate: new Date(response[i].year, response[i].month - 1, response[i].day),
                            endDate: new Date(response[i].year, response[i].month - 1, response[i].day),
                            //color: response[i].color,
                        });
                       
                    }
                }
            });
            return myData;
        }
        $(document).ready(function () {
            $('#vacationcalendar').calendar({
                dataSource: getMyData(),
                style: "custom",
                language: "nl",
                disabledWeekDays: [0,6],
                customDataSourceRenderer: function (element, date) {
                    $(element).css('background-color', '#009336');
                        $(element).css('color', 'white');
                        $(element).css('border-radius', '0px');
                },
                clickDay: function (e) {
                   
                    var dataSource = $('#vacationcalendar').data('calendar').getDataSource();
                    if (e.events.length > 0) {
                        for (ev in e.events) {
                            for (var i in dataSource) {
                                if (dataSource[i].id == e.events[ev].id) {
                                   
                                    $.ajax({
                                        url: '@Url.Action("DeleteVacationDay", "Admin")',
                                        type: "POST",
                                        async: false,
                                        cache: false,
                                        traditional: true,
                                        data: { id: e.events[ev].id},
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
                            url: '@Url.Action("AddVacationDay", "Admin")',
                            type: "POST",
                            async:false,
                            cache: false,
                            traditional: true,
                            data: { dag: e.date.toLocaleDateString().replace(/\u200E/g, '') },
                            success: function (response) {
                                dataSource.push({
                                    id: response,
                                    title: 'verlofdag',
                                    startDate: e.date,
                                    endDate: e.date,
                                });
                                
                            },
                            
                        });
                        
                    }
                    
                    $('#vacationcalendar').data('calendar').setDataSource(dataSource);
                    
                }
                  
            });
        });
        </script>
    end section

