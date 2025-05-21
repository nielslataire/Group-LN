<div class="input-group">
    <span class="input-group-addon">m²</span>
    @Html.TextBox("", ViewData.TemplateInfo.FormattedModelValue, New With {.Class = "form-control Surfacemask", .placeHolder = ViewData.ModelMetadata.DisplayName, .data_a_sep = ".", .data_a_dec = ","})
</div>

<script>

        jQuery(function ($) {
            $('.Surfacemask').autoNumeric('init');  //autoNumeric with defaults
            
        });

</script>