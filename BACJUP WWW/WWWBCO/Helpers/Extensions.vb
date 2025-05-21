Public Module Extensions
    <System.Runtime.CompilerServices.Extension> _
    Public Function TrimTo(s As String, size As Integer) As String
        If s Is Nothing OrElse s.Length < size Then
            Return s
        End If
        Dim inextspace As Integer = s.LastIndexOf(" ", size)
        Return String.Format("{0}...", s.Substring(0, If((inextspace > 0), inextspace, size)).Trim())


        'If s.Length > size Then s = String.Format("{0}{1}", s.Substring(0, size), "...")
        'Return s
    End Function
    <System.Runtime.CompilerServices.Extension> _
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
End Module
