@imports bo
@modeltype ChangeOrderBO

<table class="table table-no-border table-striped mb-none">
    <tr>
        <th> Omschrijving</th>
        <th> Eenheid</th>
        <th></th>
        <th> Aantal</th>
        <th> Eenheidsprijs</th>
        <th> Commissie</th>
        <th> Totaalprijs</th>
        @If Not Model.DateAgreement Is Nothing AndAlso Model.Invoiceable = True Then @<text><th> Te Fact.</th></text> End if
    </tr>
    @For Each item As ChangeOrderDetailBO In Model.Details
        @<text>
    <tr>
        <td>@Html.DisplayFor(Function(m) item.Description) </td>
        <td>@item.MeasurementType.GetDisplayName()</td>
        <td>@item.MeasurementUnit.GetDisplayName()</td>
        <td>@Html.DisplayFor(Function(m) item.Number)</td>
        <td>@Html.DisplayFor(Function(m) item.Price)</td>
        <td>@Html.DisplayFor(Function(m) item.Commision)</td>
        <td>@String.Format("{0:C}", item.Totaal)</td>
        @If Not Model.DateAgreement Is Nothing AndAlso Model.Invoiceable = True Then @<text><td>@Html.CheckBoxFor(Function(m) item.Invoicable, New With {.data_id = item.Id, .data_groupid = item.ChangeOrderID, .class = "chkInvoicable"}).DisableIf(Function() item.Invoiced = True)</td></text>end If
    </tr>
    </text>
    Next
</table> 
<script>    
 $('.chkInvoicable').change(function () {
     if ($('.chkInvoicable:checkbox').data("groupid", $(this).attr('data-groupid')).length == $('.chkInvoicable:checkbox:checked').data("groupid", $(this).attr('data-groupid')).length) {
         
         $('.chkbox:checkbox').data("groupid", $(this).attr('data-groupid')).each(function () {
             $(this).prop("indeterminate", false);
             $(this).prop("checked", true);

         });
     } else if ($('.chkInvoicable:checkbox:checked').data("groupid", $(this).attr('data-groupid')).length == 0) {
         $('.chkbox:checkbox').data("groupid", $(this).attr('data-groupid')).each(function () {
             $(this).prop("indeterminate", false);
             $(this).prop("checked", false);
         });
     } else {
         $('.chkbox:checkbox').data("groupid", $(this).attr('data-groupid')).each(function () {
             $(this).prop("indeterminate", true);
         });
        
     };
        $.ajax({
            url: '/Projecten/ChangeOrderDetailInvoicable',
            data: { CODetailid: $(this).attr('data-id'), value: $(this).prop('checked') },
            cache: false,
            traditional: true,
            type: 'POST',
            success: function (result) {

            },

        });
       
          
 });
</script>