Imports System.Globalization
Imports System.Text.RegularExpressions

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

    Public Function StripHtml(html As String) As String
        If String.IsNullOrWhiteSpace(html) Then Return String.Empty
        Dim stripped = Regex.Replace(html, "<[^>]+>", " ")
        stripped = System.Net.WebUtility.HtmlDecode(stripped)
        stripped = Regex.Replace(stripped, "\s+", " ").Trim()
        Return stripped
    End Function

    Public Function TruncateMetaDescription(text As String, Optional maxLength As Integer = 155) As String
        If String.IsNullOrWhiteSpace(text) Then Return String.Empty
        Dim clean = StripHtml(text)
        If clean.Length <= maxLength Then Return clean
        Dim cut = clean.LastIndexOf(" "c, maxLength)
        If cut < 1 Then cut = maxLength
        Return clean.Substring(0, cut).TrimEnd() & "…"
    End Function

    Public Function BuildProjectSeoTitle(projectName As String, gemeente As String) As String
        If String.IsNullOrWhiteSpace(projectName) Then Return "Group LN"
        If String.IsNullOrWhiteSpace(gemeente) Then
            Return projectName & " | Group LN"
        End If
        Return projectName & " | Nieuwbouwproject in " & gemeente & " | Group LN"
    End Function

    Public Function BuildProjectSeoDescription(projectName As String, gemeente As String, street As String, commercialTextNL As String, Optional maxLength As Integer = 155) As String
        If Not String.IsNullOrWhiteSpace(commercialTextNL) Then
            Dim fromText = TruncateMetaDescription(commercialTextNL, maxLength)
            If Not String.IsNullOrWhiteSpace(fromText) Then Return fromText
        End If
        Dim parts As New List(Of String)
        If Not String.IsNullOrWhiteSpace(projectName) Then parts.Add(projectName)
        If Not String.IsNullOrWhiteSpace(street) Then parts.Add(street)
        If Not String.IsNullOrWhiteSpace(gemeente) Then parts.Add(gemeente)
        Dim desc = "Ontdek nieuwbouwproject " & String.Join(", ", parts) & " van Group LN."
        Return TruncateMetaDescription(desc, maxLength)
    End Function
End Module
