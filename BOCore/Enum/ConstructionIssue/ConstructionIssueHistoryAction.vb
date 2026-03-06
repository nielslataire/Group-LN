Imports System.ComponentModel.DataAnnotations

Public Enum ConstructionIssueHistoryAction As Integer

    <Display(Name:="Aangemaakt")>
    Created = 0

    <Display(Name:="Gewijzigd")>
    Updated = 1

    <Display(Name:="Toegewezen")>
    Assigned = 2

    <Display(Name:="Status gewijzigd")>
    StatusChanged = 3

    <Display(Name:="Bijlage toegevoegd")>
    AttachmentAdded = 4

    <Display(Name:="Bijlage verwijderd")>
    AttachmentRemoved = 5

    <Display(Name:="Commentaar toegevoegd")>
    CommentAdded = 6

    <Display(Name:="Prioriteit gewijzigd")>
    PriorityChanged = 7

    <Display(Name:="Bijlage verwijderd")>
    SentToResponsible = 8

    <Display(Name:="Commentaar toegevoegd")>
    ReminderSent = 9

End Enum