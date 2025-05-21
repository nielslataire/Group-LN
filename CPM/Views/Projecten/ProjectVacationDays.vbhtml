@modeltype ProjectVacationDaysModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
<link rel="stylesheet" href="~/vendor/admin/bootstrap-year-calendar/bootstrap-year-calendar.css" />
End Section

<script src="~/vendor/admin/bootstrap-year-calendar/bootstrap-year-calendar.js"></script>
<script src="~/vendor/admin/bootstrap-year-calendar/bootstrap-year-calendar.nl.js"  charset="UTF-8"></script>

<div class="row">
    <div class="col-xs-12">
        <!-- start: page -->
        <section class="content-with-menu content-with-menu-has-toolbar">
            <div class="content-with-menu-container">
                <div class="inner-menu-toggle">
                    <a href="#" class="inner-menu-expand" data-open="inner-menu">
                        Toon Menu <i class="fa fa-chevron-right"></i>
                    </a>
                </div>

                <menu id="content-menu" class="inner-menu" role="menu">
                    <div class="nano">
                        <div class="nano-content">

                            <div class="inner-menu-toggle-inside">
                                <a href="#" class="inner-menu-collapse">
                                    <i class="fa fa-chevron-up visible-xs-inline"></i><i class="fa fa-chevron-left hidden-xs-inline"></i> Verberg Menu
                                </a>
                                <a href="#" class="inner-menu-expand" data-open="inner-menu">
                                    Toon Menu <i class="fa fa-chevron-down"></i>
                                </a>
                            </div>
                            <div class="inner-menu-content">
                                <div class="sidebar-widget m-none">
                                    <div class="widget-header clearfix">
                                        <a href="#Project" data-toggle="tab"> <h5 class="title pull-left mt-xs">Projectmenu</h5></a>
                                    </div>
                                    <div class="widget-content">
                                        <ul class="mg-folders">
                                            @Html.Partial("DetailMenu", Model.ProjectID)
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </menu>
                <div class="inner-body mg-main">


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



                </div>
            </div>
        </section>



    </div>
</div>

@section scripts
    <script>

       function getMyData() {

            var myData = [];
            $.ajax({
                url: '@Url.Action("GetProjectVacationDays", "Projecten")',
                async: false,
                cache: false,
                traditional: true,
                data: { projectid: @Model.ProjectID },
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
            $('#vacationcalendar').calendar({
                dataSource: getMyData(),
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

                clickDay: function (e) {

                    var dataSource = $('#vacationcalendar').data('calendar').getDataSource();
                    if (e.events.length > 0) {

                        for (ev in e.events) {
                            for (var i in dataSource) {
                                if (dataSource[i].id == e.events[ev].id) {

                                    $.ajax({
                                        url: '@Url.Action("DeleteProjectVacationDay", "Projecten")',
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
                            url: '@Url.Action("AddProjectVacationDay", "Projecten")',
                            type: "POST",
                            async:false,
                            cache: false,
                            traditional: true,
                            data: { dag: e.date.toLocaleDateString().replace(/\u200E/g, ''), projectid: @Model.ProjectID  },
                            success: function (response) {
                                dataSource.push({
                                    id: response,
                                    title: 'verlofdag project',
                                    startDate: e.date,
                                    endDate: e.date,
                                    color: "#009336",
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

