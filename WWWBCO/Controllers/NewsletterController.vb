Imports System.Web.Mvc

Imports Postal
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

                Dim internalemail As Object = New Email("InternalMail")
                internalemail.[To] = EmailTo
                internalemail.ContactName = ""
                internalemail.Title = "Nieuwsbrief"
                internalemail.Message = "Nieuwsbrief"
                internalemail.Send()

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