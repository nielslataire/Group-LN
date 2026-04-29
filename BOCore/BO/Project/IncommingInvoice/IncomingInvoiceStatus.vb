Public NotInheritable Class IncomingInvoiceStatus
    ' Bestaande statussen (waarden ongewijzigd)
    Public Const [New] As Byte = 0
    Public Const PendingApproval As Byte = 1
    Public Const Approved As Byte = 2
    Public Const Rejected As Byte = 3
    Public Const Booked As Byte = 4
    Public Const Paid As Byte = 5
    Public Const Duplicate As Byte = 10

    ' Verrijkings-pipeline statussen
    Public Const Enriched As Byte = 6          ' Verrijkt, hoog vertrouwen — klaar voor snelle bevestiging
    Public Const NeedsReview As Byte = 7       ' Verrijkt, maar met waarschuwingen/onzekerheden
    Public Const ManualProcessing As Byte = 9  ' Vereist handmatige verwerking buiten het systeem

    Public Shared Function Label(statusId As Byte) As String
        Select Case statusId
            Case [New] : Return "Nieuw"
            Case PendingApproval : Return "Te keuren"
            Case Approved : Return "Goedgekeurd"
            Case Rejected : Return "Afgekeurd"
            Case Booked : Return "Geboekt"
            Case Paid : Return "Betaald"
            Case Enriched : Return "Verrijkt"
            Case NeedsReview : Return "Vraagt aandacht"
            Case ManualProcessing : Return "Handmatig"
            Case Duplicate : Return "Duplicaat"
            Case Else : Return "Onbekend"
        End Select
    End Function

    Public Shared Function BadgeClass(statusId As Byte) As String
        Select Case statusId
            Case [New] : Return "badge-info"
            Case PendingApproval : Return "badge-warning"
            Case Approved : Return "badge-success"
            Case Rejected : Return "badge-danger"
            Case Booked : Return "badge-primary"
            Case Paid : Return "badge-secondary"
            Case Enriched : Return "badge-success"
            Case NeedsReview : Return "badge-warning"
            Case ManualProcessing : Return "badge-secondary"
            Case Duplicate : Return "badge-dark"
            Case Else : Return "badge-light"
        End Select
    End Function

    ''' <summary>Statussen die aanvullende actie vragen van de gebruiker.</summary>
    Public Shared Function RequiresUserAction(statusId As Byte) As Boolean
        Return statusId = [New] OrElse statusId = Enriched OrElse statusId = NeedsReview OrElse statusId = PendingApproval
    End Function
End Class
