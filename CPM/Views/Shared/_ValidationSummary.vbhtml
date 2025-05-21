@code
    Dim model As ModelStateDictionary = ViewData.ModelState


End Code
@If (Not model.IsValid) Then
    @<div class="validation-message">
        <ul>
            @For Each ModelError In model.SelectMany(Function(KeyValuePair) KeyValuePair.Value.Errors)
                @<li><label class="error" style="display: inline-block;">@ModelError.ErrorMessage</label></li>
            Next

        </ul>
    </div>

End If
