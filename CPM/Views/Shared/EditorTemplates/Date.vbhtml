@Modeltype Date?

<div Class="input-group">
    <span Class="input-group-addon">
        <i Class="fa fa-calendar"></i>
    </span>
    @Html.TextBox("", ViewData.TemplateInfo.FormattedModelValue, New With {.class = "form-control datepick", .data_plugin_datepicker = "", .type = "text", .autocomplete = "off"})
</div>
<Script>
    $(document).ready(function () {
        $('.datepick').datepicker({
            language: 'nl-BE',
            format: 'dd/mm/yyyy',
            autoclose: true,
        });
    });
</script>
