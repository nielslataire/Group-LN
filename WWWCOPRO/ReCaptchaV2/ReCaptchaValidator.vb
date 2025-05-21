Imports System.Net.Http
Imports Newtonsoft.Json

Public Class ReCaptchaValidator
    Public Shared Function IsValid(ByVal captchaResponse As String) As ReCaptchaValidationResult
        If String.IsNullOrWhiteSpace(captchaResponse) Then
            Return New ReCaptchaValidationResult() With {
            .Success = False
        }
        End If

        Dim client As HttpClient = New HttpClient()
        client.BaseAddress = New Uri("https://www.google.com")
        Dim values = New List(Of KeyValuePair(Of String, String))()
        values.Add(New KeyValuePair(Of String, String)("secret", "6LfcFIoUAAAAAHpw4NtwOLATcDqnJ53Lu16lg-sO"))
        values.Add(New KeyValuePair(Of String, String)("response", captchaResponse))
        Dim content As FormUrlEncodedContent = New FormUrlEncodedContent(values)
        Dim response As HttpResponseMessage = client.PostAsync("/recaptcha/api/siteverify", content).Result
        Dim verificationResponse As String = response.Content.ReadAsStringAsync().Result
        Dim verificationResult = JsonConvert.DeserializeObject(Of ReCaptchaValidationResult)(verificationResponse)
        Return verificationResult
    End Function
End Class
