Imports Newtonsoft.Json

Public Class ReCaptchaValidationResult
    Public Property Success As Boolean
    Public Property HostName As String
    <JsonProperty("challenge_ts")>
    Public Property TimeStamp As String
    <JsonProperty("error-codes")>
    Public Property ErrorCodes As List(Of String)
End Class
