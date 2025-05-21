@code
    Dim model As ModelStateDictionary = ViewData.ModelState


End Code
@If (Not model.IsValid) Then
    @<text>
                <div class="alert alert-danger alert-dismissible" role="alert">
                    <button type="button" class="close" data-dismiss="alert" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                    <strong><i class="fa fa-warning"></i>Opgelet!</strong> Gelieve volgende velden verder aan te vullen :
                    <ul>
                        @For Each ModelError In model.SelectMany(Function(KeyValuePair) KeyValuePair.Value.Errors)
                            @<li>@ModelError.ErrorMessage</li>
                        Next
                    </ul>
                </div>
    </text>
End If
