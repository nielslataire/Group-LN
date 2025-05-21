@modeltype ModalPrintInvoiceListModel
<link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
<link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />
<link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
    @Using Html.BeginForm("PrintInvoiceList", "Projecten", FormMethod.Post, New With {.id = "FormPrintInvoiceList", .class = "form-horizontal mb-lg"})
    @<text>
@Html.HiddenFor(Function(m) m.ProjectId)
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Facturatie overzicht klant afdrukken</h2>
            </header>
            <div class="panel-body">
                @Html.DropDownListFor(Function(m) m.SelectedClient, New SelectList(Model.Client, "ID", "Display", Model.SelectedClient), New With {.class = "form-control populate", .data_plugin_selecttwo = "", .id = "lstClients"})

            </div>

            <footer Class="panel-footer">
                <div Class="row">
                    <div Class="col-md-12 text-right">
                        <Button Class="btn btn-primary btn-block">Afdrukken</Button>
                        <Button Class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                    </div>
                </div>
            </footer>
        </section>
    </text>
    End Using
<script>
    $(document).ready(function () {
        //select2 postcode tabblad 1
        $("#lstClients").select2({
            dropdownParent: $('#modalprintinvoicelist')
        });
    });
    $('#modalprintinvoicelist').on('shown.bs.modal', function () {
        $('#lstClients').select2('focus');
    })

</script>