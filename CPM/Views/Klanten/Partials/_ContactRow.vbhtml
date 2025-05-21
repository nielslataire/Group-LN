@modeltype BO.ClientContactBO

<div class="contactRow">
    @Using (Html.BeginCollectionItem("contacts"))
        @<text>


            <hr />

            <div class="form-group">
                <div class="col-md-1  col-md-offset-2"> 
                        @Html.EnumDropDownListFor(Function(m) m.Salutation, New With {.class = "form-control", .id = "lstSalutation"})
                </div>
                <div class="col-md-4">
                    @Html.TextBoxFor(Function(m) m.Name, New With {.placeholder = "Naam", .class = "form-control", .id = "txtName"})
                </div>
                <div class="col-md-4">
                    @Html.TextBoxFor(Function(m) m.Firstname, New With {.placeholder = "Voornaam", .class = "form-control", .id = "txtForeName"})
                </div>
                <div class="col-md-1">
                    <a href="" data-toggle="tooltip" data-placement="top" title="" data-original-title="Contact verwijderen" class="deleteContact"><i class="fa fa-remove "></i></a>

                </div>
            </div>
           
            <div class="form-group">
                <div Class="col-md-5  col-md-offset-2">
                    <div class="input-group">
                        <span class="input-group-addon">
                            <i class="fa fa-at"></i>
                        </span>
                        @Html.TextBoxFor(Function(m) m.Email, New With {.placeholder = "Email", .class = "form-control", .id = "txtEmail", .type = "email"})
                    </div>
                </div>
                <div class="col-md-2">
                    @Html.EditorFor(Function(m) m.Phone)
                </div>
                <div Class="col-md-2">

                       @Html.EditorFor(Function(m) m.Cellphone)

                </div>

            </div>

        </text>
    End Using
</div>

