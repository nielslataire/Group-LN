@modeltype DetailUnitsModel
@Code
    Layout = "~/Views/Shared/_Layout.vbhtml"
    ViewData("Title") = "Project - " & Model.ProjectName
End Code
@section PageStyle
    <link rel="stylesheet" href="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />
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
                        @Using Html.BeginForm("AddUnit", "Projecten", FormMethod.Post, New With {.id = "FormAddUnit", .class = "form-horizontal mb-lg", .enctype = "multipart/form-data"})
                        @Html.AntiForgeryToken()
                        @<text>
                            @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "projectid"})
                            <section class="panel">
                                <header class="panel-heading">
                                    <h2 class="panel-title">Eenheid toevoegen :</h2>
                                </header>
                                <div class="panel-body">
                                                                      
                                    <div class="form-group">
                                        <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.AddUnit.Name)</label>
                                        <div class="col-md-9">
                                            @Html.TextBoxFor(Function(m) m.AddUnit.Name, New With {.placeholder = "Naam", .class = "form-control", .id = "txtTitle"})
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-md-3 control-label" for="w4-street">@Html.LabelFor(Function(m) m.AddUnit.Street)</label>
                                        <div class="col-md-5">
                                            @Html.TextBoxFor(Function(m) m.AddUnit.Street, New With {.placeholder = "vb. Klaverdries", .class = "form-control"})
                                        </div>
                                        <div class="col-md-2">
                                            @Html.TextBoxFor(Function(m) m.AddUnit.HouseNumber, New With {.placeholder = "Nr", .class = "form-control"})
                                        </div>
                                        <div class="col-md-2">
                                            @Html.TextBoxFor(Function(m) m.AddUnit.BusNumber, New With {.placeholder = "Bus", .class = "form-control"})
                                        </div>
                                    </div>
                                    <div class="form-group" id="Prekad">
                                        <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.AddUnit.PreKad)</label>
                                        <div class="col-md-9">
                                            @Html.TextBoxFor(Function(m) m.AddUnit.PreKad, New With {.placeholder = "Naam", .class = "form-control", .id = "txtPrekad"})
                                        </div>
                                    </div>
                                    <div class="form-group" id="Landshare">
                                        <label class="col-md-3 control-label" for="txtTitle">@Model.ProjectLandShare sten</label>
                                        @Html.HiddenFor(Function(m) m.ProjectLandShare)
                                        <div class="col-md-9">
                                            @Html.TextBoxFor(Function(m) m.AddUnit.Landshare, New With {.placeholder = "verdeling basisakte", .class = "form-control", .id = "txtLandshare"})
                                        </div>
                                    </div>
                                    <div class="form-group" id="GroupType">
                                        <label class="col-md-3 control-label" for="txtTitle">Hoofdtype</label>
                                        <div class="col-md-9">
                                            @Html.DropDownListFor(Function(m) m.SelectedGroupType, New SelectList(Model.GroupTypes, "ID", "Name", Model.SelectedGroupType), "Selecteer ....", New With {.class = "form-control populate", .id = "lstGroupTypes"})
                                        </div>
                                    </div>
                                    <div class="form-group" id="SubType">
                                        <label class="col-md-3 control-label" for="txtTitle">Subtype</label>
                                        <div class="col-md-9">
                                            @Html.DropDownListFor(Function(m) m.SelectedType, New SelectList(Model.Types, "ID", "Name", Model.SelectedType), "Selecteer ....", New With {.class = "form-control populate", .id = "lstTypes"})
                                        </div>
                                    </div>
                                    <div class="form-group" id="Level">
                                        <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.AddUnit.Level)</label>
                                        <div class="col-md-9">
                                            @Html.TextBoxFor(Function(m) m.AddUnit.Level, New With {.type = "number", .class = "form-control", .id = "txtLandshare"})
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="col-md-3 control-label">Hoofdeenheid</label>
                                        <div class="col-md-9">
                                            @Html.DropDownListFor(Function(m) m.AddUnit.AttachedUnitsId, New SelectList(Model.AttachableUnits, "ID", "Display", "Group"), "-- Geen hoofdeenheid --", New With {.class = "form-control populate", .data_plugin_selecttwo = "", .id = "lstAttachableUnits"})
                                        </div>
                                    </div>
                                    @*<div class="form-group" id="Units">
                                        <label class="col-md-3 control-label">Entiteiten toevoegen</label>
                                        <div class="col-md-9">
                                            @Html.ListBoxFor(Function(m) m.SelectedUnits, New SelectList(Model.Units, "ID", "Display", "Group", Model.SelectedUnits, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstUnits"})
                                        </div>
                                    </div>*@
                                    @*<div class="form-group">
                                        <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.AddUnit.ConstructionValue)</label>
                                        <div class="col-md-9">
                                            @Html.EditorFor(Function(m) m.AddUnit.ConstructionValue)
                                        </div>
                                       
                                    </div>*@
                                    <div class="form-group">
                                        <label class="col-md-3 control-label" for="txtTitle">@Html.LabelFor(Function(m) m.AddUnit.LandValue) </label>
                                        <div Class="col-md-9">
                                            @Html.EditorFor(Function(m) m.AddUnit.LandValue)
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
                                                        <th>Constructiewaarde</th>
                                                        <th>Constructiewaare verkocht</th>
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
                                <footer class="panel-footer">
                                    <div class="row">
                                        <div class="col-md-12 text-right">
                                            <button class="btn btn-primary btn-block ">Toevoegen</button>
                                            <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                                        </div>
                                    </div>
                                </footer>
                            </section>
                        </text>
                        End Using

                    @Html.Partial("Units", Model)

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
<div id="ModalAddLink" class="modal-block modal-block-primary mfp-hide">
    <div id="add-link-container"></div>
</div>
@section scripts

<script>
    //$('#Type').on('change', function () {
    //    var val = $(this).val();
    //    if (val == 2) {
            
    //        $('#Prekad').hide();
    //        $('#Landshare').hide();
    //        //$('#GroupType').hide();
    //        //$('#SubType').hide();
    //        $('#Level').hide();
    //        $('#Units').show();
    //    }
    //    if (val == 1) {
    //        $('#Prekad').show();
    //        $('#Landshare').show();
    //        //$('#GroupType').show();
    //        //$('#SubType').show();
    //        $('#Level').show();
    //        $('#Units').hide();
    //    }
    //});
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
    $('.addLink').click(function () {
        var url = "/Projecten/ModalAddUnitLink"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#add-link-container').html(data);
        });
    });
    $('.editNews').click(function () {
        var url = "/Projecten/ModalEditNews"; // the url to the controller
        var id = $(this).attr('data-id'); // the id that's given to each button in the list
        $.get(url + '/' + id, function (data) {
            $('#edit-news-container').html(data);
        });
    });
    $("#btnAddConstructionValue").click(function() {
        $.ajax({
            url: "BlankConstructionValueRow",
            data: { unitid:0, projectid:@Model.ProjectId},
            cache: false,
            traditional: true,
            type: 'POST',
            success: function(html) { 
                $("#ConstructionValueRows").append(html); 
                
            }
        });
        return false;
    });
    $(document).on('click', 'a.deleteConstructionRow', function () { // <-- changes
        $(this).closest('tr').remove();
        return false;
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
        $('#Units').hide();
        @If Not TempData("Message") Is Nothing Then
        @<text>

        new PNotify({
            title:      '@TempData("MessageTitle")',
            text:       '@TempData("Message")',
            type:       '@TempData("MessageType")'
        });
        </text>                   End If
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
        $('.addLink').magnificPopup({
            type: 'inline',
            src: 'addLink',
        });
    });
    
</script>


<script src="~/vendor/admin/isotope/jquery.isotope.js"></script>
<script src="~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js"></script>
<script src="~/Scripts/admin/pages/examples.mediagallery.js" ></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
<script src="~/vendor/admin/select2/select2.js"></script>
<script src="~/vendor/admin/select2/select2_locale_nl.js"></script>
<script src="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js"></script>
<script src="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js"></script>
<script src="~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js"></script>


end section

