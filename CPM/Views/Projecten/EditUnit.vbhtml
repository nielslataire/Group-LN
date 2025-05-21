@modeltype EditUnitModel 
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.css" />
    <link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
    <link rel="stylesheet" href="~/Content/theme-blog.css">

End Section
<script src="~/scripts/autoNumeric/autoNumeric.js"></script>

<div class="row">
    <div class="col-xs-12">
        <!-- start: page -->
        <section class="content-with-menu media-gallery">
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
                                            @Html.Partial("DetailMenu", Model.ProjectId)
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </menu>
                <div class="inner-body mg-main">

                    @Using Html.BeginForm("EditUnit", "Projecten", FormMethod.Post, New With {.id = "FormAddUnit", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
                        @Html.AntiForgeryToken()
                        @<text>
                            @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
                            @Html.HiddenFor(Function(m) m.Unit.Id, New With {.id = "unitid"})
                            @Html.HiddenFor(Function(m) m.Unit.ClientAccountId)
                            @Html.HiddenFor(Function(m) m.Unit.IsLink)
                            @*@Html.HiddenFor(Function(m) m.Unit.LinkedUnits)*@
    <div class="tabs tabs-primary ">
                        <ul class="nav nav-tabs nav-justified">
                            <li class="active">
                                <a href="#algemeen" data-toggle="tab" class="text-center">Algemeen</a>
                            </li>
                            <li>
                                <a href="#indeling" data-toggle="tab" class="text-center">Indeling</a>
                            </li>
                        </ul>
                        <div class="tab-content">
                            <div id="algemeen" class="tab-pane active">

                                <div class="form-group">
                                    <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.Name)</label>
                                    <div class="col-md-9">
                                        @Html.TextBoxFor(Function(m) m.Unit.Name, New With {.placeholder = "Naam", .class = "form-control", .id = "txtTitle"})
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="col-md-3 control-label" for="w4-street">@Html.LabelFor(Function(m) m.Unit.Street)</label>
                                    <div class="col-md-5">
                                        @Html.TextBoxFor(Function(m) m.Unit.Street, New With {.placeholder = "vb. Klaverdries", .class = "form-control"})
                                    </div>
                                    <div class="col-md-2">
                                        @Html.TextBoxFor(Function(m) m.Unit.HouseNumber, New With {.placeholder = "Nr", .class = "form-control"})
                                    </div>
                                    <div class="col-md-2">
                                        @Html.TextBoxFor(Function(m) m.Unit.BusNumber, New With {.placeholder = "Bus", .class = "form-control"})
                                    </div>
                                </div>
                                <div class="form-group" id="Prekad">
                                    <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.PreKad)</label>
                                    <div class="col-md-9">
                                        @Html.TextBoxFor(Function(m) m.Unit.PreKad, New With {.placeholder = "Naam", .class = "form-control", .id = "txtPrekad"})
                                    </div>
                                </div>
                                <div class="form-group" id="Landshare">
                                    <label class="col-md-3 control-label" for="txtTitle">@Model.ProjectLandShare sten</label>
                                    @Html.HiddenFor(Function(m) m.ProjectLandShare)
                                    <div class="col-md-3">
                                        @Html.TextBoxFor(Function(m) m.Unit.Landshare, New With {.placeholder = "verdeling basisakte", .class = "form-control", .id = "txtLandshare"})
                                    </div>
                                    <label class="col-md-1 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.Surface)</label>
                                    <div class="col-md-2">
                                        @Html.EditorFor(Function(m) m.Unit.Surface)
                                    </div>
                                    <label class="col-md-1 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.GroundSurface)</label>
                                    <div class="col-md-2">
                                        @Html.EditorFor(Function(m) m.Unit.GroundSurface)
                                    </div>
                                </div>
                                <div class="form-group" id="GroupType">
                                    <label class="col-md-3 control-label" for="txtTitle">Hoofdtype</label>
                                    <div class="col-md-3">
                                        @Html.DropDownListFor(Function(m) m.SelectedGroupType, New SelectList(Model.GroupTypes, "ID", "Name", Model.SelectedGroupType), "Selecteer ....", New With {.class = "form-control populate", .id = "lstGroupTypes"})
                                    </div>
                                    <label class="col-md-1 control-label" for="txtTitle">Subtype</label>
                                    <div class="col-md-5">
                                        @Html.DropDownListFor(Function(m) m.SelectedType, New SelectList(Model.Types, "ID", "Name", Model.SelectedType), "Selecteer ....", New With {.class = "form-control populate", .id = "lstTypes"})
                                    </div>
                                </div>

                                <div class="form-group" id="Level">
                                    <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.Level)</label>
                                    <div class="col-md-9">

                                        @Html.TextBoxFor(Function(m) m.Unit.Level, New With {.type = "number", .class = "form-control", .id = "txtLevel"})

                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="col-md-3 control-label">Hoofdeenheid</label>
                                    <div class="col-md-9">
                                        @Html.DropDownListFor(Function(m) m.Unit.AttachedUnitsId, New SelectList(Model.AttachableUnits, "ID", "Display", "Group"), "-- Geen hoofdeenheid --", New With {.Class = "form-control populate", .data_plugin_selecttwo = "", .allowClear = True, .id = "lstAttachableUnits"})
                                    </div>
                                </div>
                                <div class="form-group" id="Units">
                                    <label class="col-md-3 control-label">Entiteiten toevoegen</label>
                                    <div class="col-md-9">
                                        @Html.ListBoxFor(Function(m) m.SelectedUnits, New SelectList(Model.Units, "ID", "Display", "Group", Model.SelectedUnits, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstUnits"})
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="col-md-3 control-label">Plan</label>
                                    <div class="col-md-9">
                                        @if Model.Unit.Plan Is Nothing Then
                                            @<text>
                                                <div class="fileinput fileinput-new input-group" data-provides="fileinput">
                                                    <div class="form-control" data-trigger="fileinput"><i class="fa fa-file-pdf-o fileinput-exists"></i> <span class="fileinput-filename"></span></div>
                                                    <span class="input-group-addon btn btn-default btn-file"><span class="fileinput-new">Selecteren</span><span class="fileinput-exists">Wijzigen</span><input type="file" name="file" id="file" accept=".pdf"></span>
                                                    <a href="#" class="input-group-addon btn btn-default fileinput-exists" data-dismiss="fileinput">Verwijderen</a>
                                                </div>
                                            </text>
                                        Else
                                            @<text>

                                                <div Class="fileinput fileinput-exists input-group" data-provides="fileinput" data-name="Unit.Plan">
                                                    @Html.HiddenFor(Function(m) m.Unit.Plan, New With {.name = "myimage"})
                                                    <div class="form-control" data-trigger="fileinput"><i class="fa fa-file-pdf-o fileinput-exists"></i> <span class="fileinput-filename">@Model.Unit.Plan</span></div>
                                                    @*<div Class="fileinput-new thumbnail" style="width: 200px; height: 150px;"><img src="http://www.placehold.it/200x150/EFEFEF/AAAAAA&text=no+image" /></div>*@
                                                    @*<div Class="fileinput-preview fileinput-exists thumbnail" style="max-width: 200px; max-height: 150px; line-height: 20px;">
                                  <img src = "@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("PlanWebURL") & "Plans/" & Model.Unit.Plan)" >
                                </div>*@

                                                    <span class="input-group-addon btn btn-default btn-file"><span class="fileinput-new">Selecteren</span><span class="fileinput-exists">Wijzigen</span><input type="file" accept=".pdf"></span>
                                                    <a href="#" class="input-group-addon btn btn-default fileinput-exists" data-dismiss="fileinput">Verwijderen</a>

                                                </div>
                                            </text>
                                        End If


                                    </div>
                                </div>
                                <div class="form-group" id="PaymentGroup">
                                    <label class="col-md-3 control-label" for="txtTitle">Betalingsschijven</label>
                                    <div class="col-md-9">
                                        @Html.DropDownListFor(Function(m) m.SelectedPaymentGroup, New SelectList(Model.PaymentGroups, "ID", "Display", Model.SelectedPaymentGroup), "Selecteer ....", New With {.class = "form-control populate", .id = "lstGroupTypes"})
                                    </div>

                                </div>
                                @*<div Class="form-group">
                                    <Label Class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.ConstructionValue)</Label>
                                    <div class="col-md-3">
                                        @Html.EditorFor(Function(m) m.Unit.ConstructionValue)
                                    </div>
                                    <label class="col-md-2 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.ConstructionValue) verkocht</label>
                                    <div class="col-md-4">
                                        @Html.EditorFor(Function(m) m.Unit.ConstructionValueSold)
                                    </div>
                                </div>*@
                                <div class="form-group">
                                    <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.LandValue) </label>
                                    <div Class="col-md-3">
                                        @Html.EditorFor(Function(m) m.Unit.LandValue)
                                    </div>
                                    <label class="col-md-2 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.Unit.LandValue) verkocht</label>
                                    <div class="col-md-4">
                                        @Html.EditorFor(Function(m) m.Unit.LandValueSold)
                                    </div>
                                </div>

                                <div class="form-group">
                                        <label class="col-md-3 control-label" for="txtTitle">
                                            Bouwwaardes
                                            </label>
                                    <div class="col-md-9">
                                        <table class="table table-no-more mb-none">
                                            <thead>
                                                <tr>
                                                    <th>Bouwwaarde</th>
                                                    <th>Bouwwaarde verkocht</th>
                                                    <th>Omschrijving</th>
                                                    <th>Betalingsgroep</th>
                                                    <th></th>
                                                </tr>
                                            </thead>
                                            <tbody id="ConstructionValueRows">
                                                @For Each item In Model.ConstructionValues
                                                    Html.RenderPartial("_ConstructionValueEditorRow", item)
                                                Next
                                            </tbody>
                                        </table>


                                        <div class="form-group mt-md">
                                            <div class="col-md-3">
                                                <button class="btn btn-default btn-block " type="button" id="btnAddConstructionValue"><i class="fa fa-plus mr-md"></i>Bouwwaarde toevoegen</button>
                                            </div>
                                        </div>                          
                                    </div>
                                        
                                   
                                        
                                    </div>
                                </div>
                            <div id="indeling" class="tab-pane">
                                <table class="table table-no-more mb-none">
                                    <thead>
                                        <tr>
                                            <th>Type</th>
                                            <th>Aantal</th>
                                            <th>Opp.</th>
                                            <th>Opmerking</th>
                                            <th></th>
                                        </tr>
                                    </thead>
                                    <tbody id="RoomRows">
                                        @For Each item In Model.Rooms
                                            Html.RenderPartial("_RoomEditorRow", item)
                                        Next
                                    </tbody>
                                </table>
                                <div class="row justify-content-center mt-4">
                                    <div class="col-xl-9 text-end">
                                        <a href="#" class="ecommerce-attribute-add-new btn btn-primary btn-px-4 btn-py-2" id="btnAddRoom">+ Ruimte toevoegen</a>
                                    </div>
                                </div>
                                @*<div class="form-group mt-md">
                                    <div class="col-md-3">
                                        <button class="btn btn-default btn-block " type="button" id="btnAddRoom"><i class="fa fa-plus mr-md"></i>Ruimte toevoegen</button>
                                    </div>
                                </div>*@
                            </div>

                    </div>
                    <div class="row">
                        <div class="col-md-12 text-right">
                            <button class="btn btn-primary btn-block ">Opslaan</button>
                            <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                        </div>
                    </div>
                            
                        </text>
                    End Using


                </div>




            </div>
        </section>



        <!-- end: page -->

    </div>
</div>

<div id="ModalDeleteUnit" class="modal-block modal-block-warning mfp-hide">
    <div id="delete-unit-container"></div>
                                                                        </div>
<div id="ModalEditNews" class="modal-block modal-block-primary mfp-hide">
    <div id="edit-news-container"></div>
                                                                        </div>

@section scripts
<script src="~/vendor/admin/jquery-maskedinput/jquery.maskedinput.js"></script>
<script>
    var test=@(Model.Unit.IsLink.ToString().ToLower());
    if(test){
        $('#Prekad').hide();
        $('#Landshare').hide();
        //$('#GroupType').hide();
        //$('#SubType').hide();
        //$('#Level').hide();
        $('#Units').show();
    }
    else{
        $('#Prekad').show();
        $('#Landshare').show();
        //$('#GroupType').show();
        //$('#SubType').show();
        //$('#Level').show();
        $('#Units').hide();
    }
    //$('#Type').on('change', function () {
    //    var val = $(this).val();
    //    if (val == 2) {

    //        $('#Prekad').hide();
    //        $('#Landshare').hide();
    //        $('#GroupType').hide();
    //        $('#SubType').hide();
    //        $('#Level').hide();
    //        $('#Units').show();
    //    }
    //    if (val == 1) {
    //        $('#Prekad').show();
    //        $('#Landshare').show();
    //        $('#GroupType').show();
    //        $('#SubType').show();
    //        $('#Level').show();
    //        $('#Units').hide();
    //    }
    //});
    $("#btnAddRoom").click(function() {
        $.ajax({
            url: "BlankRoomRow",
            data: { unitid: @Model.Unit.Id},
            cache: false,
            traditional: true,
            type: 'POST',
            success: function(html) { 
                $("#RoomRows").append(html); 
                
            }
        });
        return false;
    });
    $("#btnAddConstructionValue").click(function() {
        $.ajax({
            url: "BlankConstructionValueRow",
            data: { unitid: @Model.Unit.Id, projectid:@Model.Unit.ProjectId},
            cache: false,
            traditional: true,
            type: 'POST',
            success: function(html) { 
                $("#ConstructionValueRows").append(html); 
                
            }
        });
        return false;
    });
    $(document).on('click', 'a.deleteRow', function () { // <-- changes
        $(this).closest('tr').remove();
        return false;
    });
    $(document).on('click', 'a.deleteConstructionRow', function () { // <-- changes
        $(this).closest('tr').remove();
        return false;
    });
        $('#lstGroupTypes').on('change', function () {

            var val = $(this).val();
            if(val){

                var subitems = "";
                $.getJSON("@Url.Action("GetSubType", "Projecten")", { id: val }, function (data) {
                    $.each(data,function(index, item){
                        subitems+="<option value='"+item.Value+"'>"+item.Text+"</option>"
                    });
                    $('#lstTypes').html(subitems);
                    $('#lstTypes').attr("disabled", false);
                });
            }
            else {

                $('#lstTypes').attr("disabled", true);
            }

        });

    $('.deleteUnit').click(function () {
        var url = "/Projecten/ModalDeleteUnit"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#delete-unit-container').html(data);
        });
    });
    $('.editNews').click(function () {
        var url = "/Projecten/ModalEditNews"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#edit-news-container').html(data);
        });
    });

    $('.facebookNews').click(function () {
        $.ajax({
            url: '/Projecten/PlaceFacebookNews',
data: { newsid: $(this).attr('data-id') },
            cache: false,
            traditional: true,
            type: 'POST',
            success: function (result) {
                //$('#txtCountryIsoCode').val(result);
            },

        });
    });
    $(window).load(function () {
        @If Not TempData("Message") Is Nothing Then
        @<text>

        new PNotify({
            title:      '@TempData("MessageTitle")',
text:       '@TempData("Message")',
type:       '@TempData("MessageType")'
        });
        </text>
          End If
    });


    $(document).ready(function () {
        $('.deleteUnit').magnificPopup({
            type: 'inline',
src: 'deleteUnit',
        });
        $('.editNews').magnificPopup({
            type: 'inline',
src: 'editNews',
        });
        $('#addLevel').magnificPopup({
            type: 'inline',
src: 'addLevel',
        });

    });

</script>


<script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
<script src="~/vendor/admin/bootstrap-jasny/jasny-bootstrap.js"></script>
<script src="~/Scripts/admin/pages/examples.mediagallery.js" ></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
<script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>
<script src="~/vendor/admin/jquery-datatables/media/js/jquery.dataTables.js"></script>
<script src="~/vendor/admin/jquery-datatables-bs3/assets/js/datatables.js"></script>
<script src="~/scripts/admin/tables/examples.datatables.editable.js"></script>

                                                                            end section

