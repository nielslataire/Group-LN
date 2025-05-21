@modeltype ProjectSalesSelectForPriceModel


    @Using Html.BeginForm("SelectForPrice", "Projecten", FormMethod.Post, New With {.id = "FormSelectForPrice", .class = "form-horizontal mb-lg"})
    @<text>
        @Html.HiddenFor(Function(m) m.ProjectId)
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Kies de gewenste eenheden om een berekening te maken :</h2>
            </header>
            <div class="panel-body">
                <div class="form-group">
                    <label class="col-md-3 control-label" for="lstSelectForPriceUnits">Eenheden</label>
                    <div class="col-md-9">
                        @Html.DropDownListFor(Function(m) m.SelectedUnits, New SelectList(Model.Units, "ID", "Display", "Group", Model.SelectedUnits, Model.Units, Nothing), New With {.class = "form-control populate", .multiple = "", .data_plugin_selecttwo = "", .id = "lstSelectForPriceUnits"})                       
                    </div>
                </div>
            </div>

            <footer class="panel-footer">
                <div class="row">
                    <div class="col-md-12 text-right">
                        <a class="btn btn-primary btn-block openCalculate" href="#modalcalculateprice" id="btnOpenCalculate" data-id="@Model.ProjectId">Berekening openen</a>
                        <button class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                    </div>
                </div>
            </footer>
        </section>
    </text>
    End Using

<script>
    $('#btnOpenCalculate').click(function () {
        //var url = "/Projecten/ModalCalculatePrice"; // the url to the controller
        //var id = $(this).attr('data-id'); // the id that's given to each button in the list
        //$.get(url + '?projectid=' + id, function (data) {
        //    $('#calculate-price-container').html(data);
        //});
        $.ajax({
            url: '/Projecten/ModalCalculatePrice',
            data: {
                projectid: @Model.ProjectId,
                unitids: $('#lstSelectForPriceUnits').val()
            },
            cache: false,
            traditional: true,
            type: 'POST',
            success: function (data) {
                $('#calculate-price-container').html(data);
            },

        });

    });
    $(document).ready(function () {

        $('#lstSelectForPriceUnits').select2({
            placeholder: 'Selecteer Eenheid',
            width: 'resolve',

        });
        $('.openCalculate').magnificPopup({
            type: 'inline',
            src: 'openCalculate',
        });
    });
</script>


