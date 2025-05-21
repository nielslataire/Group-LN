@Modeltype String

<div class="input-group">
    <span class="input-group-addon">
        <i class="fa fa-mobile"></i>
    </span>
    @Html.TextBox("", ViewData.TemplateInfo.FormattedModelValue, New With {.class = "form-control Cellphonemask", .placeHolder = ViewData.ModelMetadata.DisplayName, .data_plugin_masked_input = "", .data_input_mask = "9999/99.99.99"})
</div>

<script>
    jQuery(function ($) {
       $(".Cellphonemask").mask("9999/99.99.99");
    });
</script>