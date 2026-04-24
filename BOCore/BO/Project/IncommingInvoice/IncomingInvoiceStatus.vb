Public NotInheritable Class IncomingInvoiceStatus
    Public Const [New] As Byte = 0
    Public Const PendingApproval As Byte = 1
    Public Const Approved As Byte = 2
    Public Const Rejected As Byte = 3
    Public Const Booked As Byte = 4
    Public Const Paid As Byte = 5
    Public Const Duplicate As Byte = 10

    Public Shared Function Label(statusId As Byte) As String
        Select Case statusId
            Case [New] : Return "Nieuw"
            Case PendingApproval : Return "Te keuren"
            Case Approved : Return "Goedgekeurd"
            Case Rejected : Return "Afgekeurd"
            Case Booked : Return "Geboekt"
            Case Paid : Return "Betaald"
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
            Case Duplicate : Return "badge-dark"
            Case Else : Return "badge-light"
        End Select
    End Function
End Class
