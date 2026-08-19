Imports System.Configuration
Imports System.Net
Imports System.Net.Mail
Imports System.Threading

''' Centrale mailverzending voor alle formulieren (Contact, Solliciteren, SendDoc, SendMail,
''' Inschrijving, Newsletter) — gebruikt overal dezelfde bewezen-werkende Office365-relay
''' (credentials via environment variables met Web.config-fallback), met één automatische
''' herpoging bij een transiënte verbindings-/TLS-fout (de SmtpClient-verbinding wordt
''' bovendien correct gesloten via Using, wat voorheen nergens gebeurde).
Public Class SmtpMailHelper
    Public Shared Sub SendWithRetry(msg As MailMessage, Optional maxAttempts As Integer = 2)
        Dim lastException As Exception = Nothing

        For attempt As Integer = 1 To maxAttempts
            Try
                ' ServicePointManager.SecurityProtocol is een process-brede static — zonder deze
                ' expliciete zet hangt de effectieve TLS-versie af van toeval (of een ándere,
                ' ongerelateerde aanroep zoals ReCaptchaValidator of een bestandsdownload deze al
                ' naar Tls12 gezet heeft in dezelfde AppDomain). Dat verklaarde waarom sommige
                ' formulieren wél en andere niét betrouwbaar verstuurden.
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12

                Using smtp As New SmtpClient("smtp.office365.com", 587)
                    smtp.EnableSsl = True
                    smtp.Credentials = New Net.NetworkCredential(
                        If(Environment.GetEnvironmentVariable("SmtpUser"), ConfigurationManager.AppSettings("SmtpUser")),
                        If(Environment.GetEnvironmentVariable("SmtpPassword"), ConfigurationManager.AppSettings("SmtpPassword")))
                    smtp.Send(msg)
                End Using
                Return
            Catch ex As Exception
                lastException = ex
                If attempt < maxAttempts Then
                    Thread.Sleep(800)
                End If
            End Try
        Next

        Throw lastException
    End Sub
End Class
