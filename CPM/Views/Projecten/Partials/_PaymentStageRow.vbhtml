@imports BO
@modeltype ProjectPaymentStageBO 

<div class="form-group">
    @Using (Html.BeginCollectionItem("Stages"))
    @<text>
        @Html.HiddenFor(Function(m) m.Id)
        @Html.HiddenFor(Function(m) m.GroupId)
        @Html.HiddenFor(Function(m) m.Invoicable)
        <div class="col-md-2 control-label">
            @Html.LabelFor(Function(m) m.Name)
        </div>
        <div class="col-md-7">
            @Html.TextBoxFor(Function(m) m.Name, New With {.class = "form-control"})
        </div>
        <div class="col-md-2">
            @Html.EditorFor(Function(m) m.Percentage, New With {.class = "form-control percentage"})
        </div>
     
        <div class="col-md-1">
            <a href="#" data-toggle="tooltip" data-placement="top" title="" data-original-title="Verwijderen" Class="deleteRow"><i Class="fa fa-remove"></i></a>
        </div>
    </text> End Using
</div>
