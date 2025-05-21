@modeltype ActivitiesModel 
@Code
    ViewData("Title") = "Activiteiten beheren"
End Code
@section PageStyle
<link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />


end section
<div class="row">
    <div class="col-md-6">
        <section class="panel">
            <div id="activity-container">
                <header class="panel-heading">
                 
                    <h2 class="panel-title">Activiteiten</h2>

                </header>
                <div class="panel-body">
                    <form class="form-horizontal form-bordered" method="get">
                        <div class="form-group">
                            <label class="col-md-3 control-label" for="ActivityName">Selecteren </label>
                            @Html.TextBoxFor(Function(m) m.SelectedActivity.ID, New With {.style = "display:none;", .class = "form-control", .disabled = "", .id = "txtSelActId"})
                            <div class="col-md-9">
                                @Html.DropDownListFor(Function(m) m.SelectedActivityID, New SelectList(Model.SelectListActivities, "ID", "Display", "Group", Nothing, Nothing), New With {.class = "form-control populate", .onChange = "lstSelectActivityChange()", .data_plugin_selecttwo = "", .id = "lstSelectActivity"})
                            
                            </div>
                        </div>
                        <div id="activity-detail-container">
                            @Html.Partial("ActivityDetail", Model)
                        </div>
                    </form>
                </div>
                <footer class="panel-footer">

                    <button class="btn btn-default" id="startEdit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" disabled=""><i class="fa fa-edit"></i></button>
                    <button type="reset" class="btn btn-default" id="addEdit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Toevoegen" ><i class="fa fa-plus"></i></button>
                    <button type="reset" class="btn btn-primary" id="saveEdit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Opslaan" disabled=""><i class="fa fa-save"></i></button>
                    <a href="#modaldeleteactivity" data-toggle="tooltip" data-placement="top" title="" data-original-title="verwijderen" class="btn btn-danger" id="removeEdit" disabled=""><i class="fa fa-remove"></i></a>

                    @*<button type="reset" class="btn btn-danger" id="removeEdit" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" disabled=""><i class="fa fa-remove"></i></button>*@

                </footer>
            </div>

        </section>
    </div>
    <div class="col-md-6">
        <section class="panel">
            <div id="activity-container">
                <header class="panel-heading">

                    <h2 class="panel-title">Groepen</h2>

                </header>
                <div class="panel-body">
                    <form class="form-horizontal form-bordered" method="get">
                        <div class="form-group">
                            <label class="col-md-3 control-label" for="ActivityName">Selecteren </label>
                            @Html.TextBoxFor(Function(m) m.SelectedActivityGroup.ID, New With {.style = "display:none;", .class = "form-control", .disabled = "", .id = "txtSelGroupID"})
                            <div class="col-md-9">
                                @Html.DropDownListFor(Function(m) m.SelectedActivityGroupId, New SelectList(Model.SelectListGroups, "ID", "Display"), New With {.class = "form-control populate", .onChange = "lstSelectGroupChange()", .data_plugin_selecttwo = "", .id = "lstSelectGroup"})
                            </div>

                        </div>
                        <div id="group-detail-container">
                            @Html.Partial("GroupDetail", Model)
                        </div>
                    </form>
                </div>
                <footer class="panel-footer">

                    <button class="btn btn-default" id="startEditGroup" data-toggle="tooltip" data-placement="top" title="" data-original-title="Bewerken" disabled=""><i class="fa fa-edit"></i></button>
                    <button type="reset" class="btn btn-default" id="addEditGroup" data-toggle="tooltip" data-placement="top" title="" data-original-title="Toevoegen"><i class="fa fa-plus"></i></button>
                    <button type="reset" class="btn btn-primary" id="saveEditGroup" data-toggle="tooltip" data-placement="top" title="" data-original-title="Opslaan" disabled=""><i class="fa fa-save"></i></button>

                    <a href="#modaldeleteactivitygroup" data-toggle="tooltip" data-placement="top" title="" data-original-title="verwijderen" class="btn btn-danger" id="removeEditGroup" disabled=""><i class="fa fa-remove"></i></a>

                </footer>
            </div>

        </section>
    </div>
    </div>
    <div id="modaldeleteactivity" class="modal-block modal-block-warning mfp-hide">
        <div id="delete-activity-container"></div>
    </div>
    <div id="modaldeleteactivitygroup" class="modal-block modal-block-warning mfp-hide">
        <div id="delete-activitygroup-container"></div>
    </div>
@section scripts



<script>
    $(window).load(function () {

        @If Not TempData("Message") Is Nothing Then
@<text>

        new PNotify({
            title: '@TempData("MessageTitle")',
            text: '@TempData("Message")',
            type: '@TempData("MessageType")'
        });
        </text>
        End If
    });
    
    $('#removeEdit').click(function () {
        var url = "/Admin/DeleteActivityModal"; // the url to the controller
        var id = $('#txtActId').val();
        $.get(url + '/' + id, function (data) {
            $('#delete-activity-container').html(data);
        });
    });
    $('#removeEditGroup').click(function () {
        var url = "/Admin/DeleteGroupModal"; // the url to the controller
        var id = $('#txtGroupId').val();
        $.get(url + '/' + id , function (data) {
            $('#delete-activitygroup-container').html(data);
        });
    });
    function lstSelectActivityChange() {
        var myselect = document.getElementById("lstSelectActivity");
        var val = myselect.options[myselect.selectedIndex].value;
        var url = "/Admin/DetailActivity"; // the url to the controller
        $.get(url + '/' + val, function (data) {
            $('#activity-detail-container').html(data);
        });
        $('#startEdit').removeAttr("disabled");
        $('#removeEdit').removeAttr("disabled");
    };
    function lstSelectGroupChange() {
        var myselect = document.getElementById("lstSelectGroup");
        var val = myselect.options[myselect.selectedIndex].value;
        var url = "/Admin/DetailGroup"; // the url to the controller
        $.get(url + '/' + val, function (data) {
            $('#group-detail-container').html(data);
        });
        $('#startEditGroup').removeAttr("disabled");
        $('#removeEditGroup').removeAttr("disabled");
    };
    //buttons activities
    $('#startEdit').click(function () {
        $('#txtActName').removeAttr("disabled");
        $('#lstActivityGroups').removeAttr("disabled");
        $('#saveEdit').removeAttr("disabled");
        $('#startEdit').attr("disabled", true);
        $('#lstSelectActivity').attr("disabled", true);
    });
    $('#saveEdit').click(function () {
        var iGroupID = jQuery("#lstActivityGroups option:selected").val();
        $.ajax({
            url: 'SaveActivity',
            data: { actId: $('#txtActId').val(), actName: $('#txtActName').val(), actGroupID: iGroupID },
            cache: false,
            traditional: true,
            type: 'POST',
            success: function (result) {
                window.location.href = '/Admin/Activiteiten';

            },
        });
    });
    //$('#removeEdit').click(function () {
    //    var iGroupID = jQuery("#lstActivityGroups option:selected").val();
    //    $.ajax({
    //        url: 'DeleteActivity',
    //        data: { actId: $('#txtActId').val(), actName: $('#txtActName').val(), actGroupID: iGroupID },
    //        cache: false,
    //        traditional: true,
    //        type: 'POST',
    //        success: function (result) {
    //            window.location.href = "/Admin/Activiteiten";

    //        },
    //    });
    //    $('#txtActName').attr("disabled", true);
    //    $('#lstActivityGroups').attr("disabled", true);
    //    $('#lstSelectActivity').attr("disabled", true);
    //    $('#startEdit').removeAttr("disabled");

    //});
    $('#addEdit').click(function () {
        $('#txtActId').val('0');
        $('#txtActName').val('');
        $('#txtActGroup').val('');
        $('#txtActName').removeAttr("disabled");
        $('#lstActivityGroups').removeAttr("disabled");
        $('#removeEdit').attr("disabled", true);
        $('#startEdit').attr("disabled", true);
        $('#lstSelectActivity').attr("disabled", true);
        $('#saveEdit').removeAttr("disabled");
    });
    //buttons groups
    $('#startEditGroup').click(function () {
        $('#txtGroupName').removeAttr("disabled");
        $('#txtGroupLot').removeAttr("disabled");
        $('#saveEditGroup').removeAttr("disabled");
        $('#startEditGroup').attr("disabled", true);
        $('#lstSelectGroup').attr("disabled", true);
    });
    $('#saveEditGroup').click(function () {
        $.ajax({
            url: 'SaveGroup',
            data: { groupId: $('#txtGroupId').val(), groupName: $('#txtGroupName').val(), groupLot: $('#txtGroupLot').val() },
            cache: false,
            traditional: true,
            type: 'POST',
            success: function (result) {
                window.location.href = '/Admin/Activiteiten';

            },
        });
    });
    //$('#removeEditGroup').click(function () {
    //    $.ajax({
    //        url: 'DeleteGroup',
    //        data: { groupId: $('#txtGroupId').val(), groupName: $('#txtGroupName').val(), groupLot: $('#txtGroupLot').val() },
    //        cache: false,
    //        traditional: true,
    //        type: 'POST',
    //        success: function (result) {
    //            window.location.href = "/Admin/Activiteiten";

    //        },
    //    });
    //    $('#txtGroupName').attr("disabled", true);
    //    $('#txtGroupLot').attr("disabled", true);
    //    $('#lstSelectGroup').attr("disabled", true);
    //    $('#startEditGroup').removeAttr("disabled");

    //});
    $('#addEditGroup').click(function () {
        $('#txtGroupId').val('0');
        $('#txtGroupName').val('');
        $('#txtGroupLot').val('');
        $('#txtGroupName').removeAttr("disabled");
        $('#txtGroupLot').removeAttr("disabled");
        $('#removeEditGroup').attr("disabled", true);
        $('#startEditGroup').attr("disabled", true);
        $('#lstSelectGroup').attr("disabled", true);
        $('#saveEditGroup').removeAttr("disabled");
    });

    $(document).ready(function () {

        $('#removeEdit').magnificPopup({
            type: 'inline',
            src: 'removeEdit',
        });
        $('#removeEditGroup').magnificPopup({
            type: 'inline',
            src: 'removeEditGroup',
        });
        $("#lstSelectActivity").select2({
            placeholder: "Selecteer een activiteit",
        });
    });
</script>

<script src="~/vendor/admin/pnotify/pnotify.custom.js"></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
end section