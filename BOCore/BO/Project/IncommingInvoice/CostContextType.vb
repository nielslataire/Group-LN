Public NotInheritable Class CostContextType
    Public Const Unknown As String = "Unknown"
    Public Const ProjectCost As String = "ProjectCost"
    Public Const GeneralCompanyCost As String = "GeneralCompanyCost"

    Public Shared Function Label(contextType As String) As String
        Select Case contextType
            Case ProjectCost : Return "Projectkost"
            Case GeneralCompanyCost : Return "Algemene kost"
            Case Else : Return "Onzeker"
        End Select
    End Function

    Public Shared Function BadgeClass(contextType As String) As String
        Select Case contextType
            Case ProjectCost : Return "dc-badge-project"
            Case GeneralCompanyCost : Return "dc-badge-general"
            Case Else : Return "dc-badge-unknown"
        End Select
    End Function

    Public Shared Function Icon(contextType As String) As String
        Select Case contextType
            Case ProjectCost : Return "bx-building-house"
            Case GeneralCompanyCost : Return "bx-briefcase"
            Case Else : Return "bx-question-mark"
        End Select
    End Function
End Class
