@Modeltype HomeModel


@section PageStyle
        <link href="~/vendor/admin/jstree/themes/default/style.css" rel="stylesheet" />
        <link rel="stylesheet" href="~/vendor/admin/pnotify/pnotify.custom.css" />
        <link rel="stylesheet" href="~/vendor/admin/select2/select2.css" />
        <link rel="stylesheet" href="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css" />
        <link rel="stylesheet" href="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css" />
    end section
<form class="search nav-form">
    @Html.HiddenFor(Function(m) m.SelectedSearch.ID, New With {.id = "txtGeneralSearch"})
</form>
@section scripts
    <script>
        $(document).ready(function () {

            $("#txtGeneralSearch").select2({

                minimumInputLength: 3,  // minimumInputLength for sending ajax request to server
                width: 'resolve',   // to adjust proper width of select2 wrapped elements
                placeholder: "Zoeken ....",
                ajax: {

                    url: 'GetGeneralSearchList',
                    cache: false,
                    traditional: true,
                    type: 'POST',
                    data: function (term) {
                        return {
                            term: term,
                        };
                    },

                    results: function (data, page) {
                        return { results: data };
                    },

                },
            });
        });
        $("#txtGeneralSearch").on("change", function () {


            $.ajax({
                url: 'SelectSearchItem',
                data: { id: $("#txtGeneralSearch").select2('data').id },
                cache: false,
                traditional: true,
                type: 'POST',
                success: function (result) {
                    window.location.href = result;
                },

            });
        });
    </script>
<script src="~/vendor/admin/select2/select2.js"></script>
<script src="~/vendor/admin/select2/select2_locale_nl.js"></script>
<script src="~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js"></script>
<script src="~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js"></script>
end section