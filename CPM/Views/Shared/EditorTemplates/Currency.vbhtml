<div class="input-group">
    <span class="input-group-addon">€</span>
    @Html.TextBox("", ViewData.TemplateInfo.FormattedModelValue, New With {.Class = "form-control Currencymask", .placeHolder = ViewData.ModelMetadata.DisplayName, .data_a_sep = ".", .data_a_dec = ","})
</div>

<script>

        jQuery(function ($) {
            $('.Currencymask').autoNumeric('init');  //autoNumeric with defaults
            
        });

</script>