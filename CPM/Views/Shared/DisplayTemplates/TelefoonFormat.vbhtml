@Modeltype String

@If Model IsNot Nothing Then
    Dim value = String.Format("{0:###/##.##.## x}{1}", Model.Substring(0, 8), Model.Substring(8))
    Dim phonedigits As Int64
    If Not String.IsNullOrEmpty(value) AndAlso Not String.IsNullOrWhiteSpace(value) Then
        If Int64.TryParse(value.ToString(), phonedigits) Then
            Dim cleanphonedigits As String = phonedigits.ToString()
            Dim digitcount As Integer = cleanphonedigits.Length
            If digitcount = 8 Then
                @<text>@String.Format("{0:'+32 '##' '##' '##' '##}", phonedigits)</text>
ElseIf digitcount = 9 Then
    @<text>@String.Format("{0:'+32 '###' '##' '##' '##}", phonedigits)</text>
ElseIf digitcount > 9 Then
@<text>@String.Format("{0:'+32 '##' '##' '##' '## x#########}", phonedigits)</text>

Else
@<text>@cleanphonedigits </text>
End If
End If
End If
End If
