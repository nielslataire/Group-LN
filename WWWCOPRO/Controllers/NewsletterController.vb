Imports System.Web.Mvc
Imports System.Configuration

Namespace Controllers
    Public Class NewsletterController
        Inherits Controller

        ' GET: Newsletter
        Function Index() As ActionResult
            Dim model As New NewsletterModel
            Return View(model)
        End Function
        '<HttpPostAttribute>
        '<ValidateInput(False)>
        <HttpPost>
        Function Index(EmailTo As String) As ActionResult
            Dim errors As New ArrayList
            'if not valid then there where errors (required property not filled in or such) so return to show them
            'For Each key In ModelState.Keys
            '    If ModelState(key).Errors.Count > 0 Then
            '        errors(key) = ModelState(key).Errors()
            '    End If
            'Next

            'If (Not ModelState.IsValid) Then Return "Inschrijven mislukt, probeer later opnieuw!"
            If (ModelState.IsValid) Then

                Try
                    ViewBag.To = EmailTo
                    ViewBag.ContactName = ""
                    ViewBag.Title = "Nieuwsbrief"
                    ViewBag.Message = "Nieuwsbrief"

                    Dim internalHtml As String = ViewRenderHelper.RenderViewToString(Me.ControllerContext, "~/Views/Emails/InternalMail.vbhtml", Nothing)

                    Dim msg As New Net.Mail.MailMessage()
                    msg.To.Add("niels.lataire@groupln.be")
                    msg.From = New Net.Mail.MailAddress("info@groupln.be")
                    msg.Subject = "Website Group LN : Nieuwsbrief"
                    msg.Body = internalHtml
                    msg.IsBodyHtml = True

                    SmtpMailHelper.SendWithRetry(msg)
                Catch
                End Try

                Return Json(New With {.success = True})
            Else
                Return PartialView("_ValidationSummary", ModelState)
            End If
        End Function
        Public Sub AddMessage(ByVal messagetype As String, ByVal message As String, ByVal messagetitle As String)
            TempData("Message") = message
            TempData("MessageType") = messagetype
            TempData("MessageTitle") = messagetitle
        End Sub
    End Class

End Namespace