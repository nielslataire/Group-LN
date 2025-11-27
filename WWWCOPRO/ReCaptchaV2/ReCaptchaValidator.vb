Imports System.Globalization
Imports System.Net.Http
Imports Newtonsoft.Json

Public Class ReCaptchaValidator
    Public Shared Function ValidateV3(ByVal captchaResponse As String, ByVal expectedAction As String, ByVal minimumScore As Double) As ReCaptchaValidationResult
        Dim validationResult As New ReCaptchaValidationResult() With {
            .Success = False,
            .ErrorCodes = New List(Of String)()
        }

        If String.IsNullOrWhiteSpace(captchaResponse) Then
            validationResult.ErrorCodes.Add("missing-input-response")
            Return validationResult
        End If

        Dim secretKey As String = ConfigurationManager.AppSettings("ReCaptchaV3SecretKey")
        If String.IsNullOrWhiteSpace(secretKey) Then
            validationResult.ErrorCodes.Add("missing-secret-key")
            Return validationResult
        End If

        Using client As HttpClient = New HttpClient()
            client.BaseAddress = New Uri("https://www.google.com")
            Dim values = New List(Of KeyValuePair(Of String, String))() From {
                New KeyValuePair(Of String, String)("secret", secretKey),
                New KeyValuePair(Of String, String)("response", captchaResponse)
            }
            Dim content As FormUrlEncodedContent = New FormUrlEncodedContent(values)
            Dim response As HttpResponseMessage = client.PostAsync("/recaptcha/api/siteverify", content).Result
            Dim verificationResponse As String = response.Content.ReadAsStringAsync().Result
            Dim verificationResult = JsonConvert.DeserializeObject(Of ReCaptchaValidationResult)(verificationResponse)

            If verificationResult Is Nothing Then
                validationResult.ErrorCodes.Add("invalid-response")
                Return validationResult
            End If

            If verificationResult.ErrorCodes Is Nothing Then
                verificationResult.ErrorCodes = New List(Of String)()
            End If

            If Not verificationResult.Success Then
                Return verificationResult
            End If

            If Not String.IsNullOrWhiteSpace(expectedAction) AndAlso Not String.Equals(verificationResult.Action, expectedAction, StringComparison.OrdinalIgnoreCase) Then
                verificationResult.Success = False
                verificationResult.ErrorCodes.Add("invalid-action")
                Return verificationResult
            End If

            If verificationResult.Score < minimumScore Then
                verificationResult.Success = False
                verificationResult.ErrorCodes.Add(String.Format(CultureInfo.InvariantCulture, "score-below-threshold ({0:0.00})", verificationResult.Score))
                Return verificationResult
            End If

            Return verificationResult
        End Using
    End Function
End Class