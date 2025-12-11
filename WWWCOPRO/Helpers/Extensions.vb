Imports System.Globalization

Public Module Extensions
    Private ReadOnly EuroCultureInfo As CultureInfo = CultureInfo.CreateSpecificCulture("nl-BE")

    Sub New()
        EuroCultureInfo.NumberFormat.CurrencySymbol = "€"
        EuroCultureInfo.NumberFormat.CurrencyDecimalDigits = 2
    End Sub
    <System.Runtime.CompilerServices.Extension>
    Public Function TrimTo(s As String, size As Integer) As String
        If s Is Nothing OrElse s.Length < size Then
            Return s
        End If
        Dim inextspace As Integer = s.LastIndexOf(" ", size)
        Return String.Format("{0}...", s.Substring(0, If((inextspace > 0), inextspace, size)).Trim())


        'If s.Length > size Then s = String.Format("{0}{1}", s.Substring(0, size), "...")
        'Return s
    End Function
    <System.Runtime.CompilerServices.Extension>
    Public Function GenerateSlug(phrase As String) As String
        Dim str As String = RemoveAccent(phrase).ToLower()
        str = Regex.Replace(str, "[^a-z0-9\s-]", "")
        str = Regex.Replace(str, "\s+", " ").Trim()
        str.Substring(0, If(str.Length <= 45, str.Length, 45)).Trim()
        str = Regex.Replace(str, "\s", "-")
        Return str
    End Function
    Public Function RemoveAccent(txt As String) As String
        Dim bytes As Byte() = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt)
        Return System.Text.Encoding.ASCII.GetString(bytes)

    End Function
    <System.Runtime.CompilerServices.Extension>
    Public Function ToEuroCurrency(amount As Decimal) As String
        Return String.Format(EuroCultureInfo, "{0:C2}", amount)
    End Function

    <System.Runtime.CompilerServices.Extension>
    Public Function ToEuroCurrency(amount As Decimal?) As String
        If Not amount.HasValue Then Return String.Empty
        Return ToEuroCurrency(amount.Value)
    End Function
End Module
