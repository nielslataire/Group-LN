@Modeltype PostalcodeModel

<div class="col-md-6">
    @Html.DropDownListFor(Function(m) m.CountryId, New SelectList(Model.Countries, "ID", "Display", Model.CountryId), New With {.class = "form-control populate"})
</div>


<div class="col-md-6">
    @Html.HiddenFor(Function(m) m.PostalCodeId, New With {.class = "form-control"})
    @Html.HiddenFor(Function(m) m.CountryId)
</div>



<script>
    //selecteren van een land
    var i@(ViewData.TemplateInfo.HtmlFieldPrefix & "_CountryId") = jQuery('@("#" & ViewData.TemplateInfo.HtmlFieldPrefix & "_CountryId option:selected")').val();
    $('@("#" & ViewData.TemplateInfo.HtmlFieldPrefix & "_CountryId")').on('change', function () {

        i@(ViewData.TemplateInfo.HtmlFieldPrefix & "_CountryId") = this.value;
    });

    $(document).ready(function () {
  
        $('@("#" & ViewData.TemplateInfo.HtmlFieldPrefix & "_PostalCodeId")').select2({
            placeholder: 'Gemeente',
            minimumInputLength: 3,
            width: 'resolve',
            ajax: {

                url: '@Url.Action("GetPostcodesByCountry", "Shared")',
                cache: false,
                
                traditional: true,
                type: 'POST',
                data: function (term) {
                    return {
                        term: term,
                        CountryId: i@(ViewData.TemplateInfo.HtmlFieldPrefix & "_CountryId"),
                    };
                },

                results: function (data, page) {
                    return { results: data };
                },

            },
            initSelection: function (element, callback) {

                if (@ViewData("PostcodeId") > 0){

                    var data = {id:@ViewData("PostcodeId"), text: '@(ViewData("Postcode")) - @(ViewData("Gemeente"))' };
                    callback(data);

                }

            }

        });
    });
</script>