<div class="input-group">
    @Html.TextBox("", ViewData.TemplateInfo.FormattedModelValue, New With {.Class = "form-control Currencymask", .placeHolder = ViewData.ModelMetadata.DisplayName, .data_a_sep = ".", .data_a_dec = ","})
    <span class="input-group-addon">%</span>
</div>

<script>

        jQuery(function ($) {
            $('.Currencymask').autoNumeric('init');  //autoNumeric with defaults
            
        });

</script>