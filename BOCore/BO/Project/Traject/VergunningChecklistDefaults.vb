''' <summary>Eén stap in de standaard-checklist van een Omgevingsvergunning-dossier.</summary>
Public Class VergunningChecklistStapBO
    Public Property Code As String
    Public Property Naam As String
    Public Property Volgorde As Integer

    Public Sub New(code As String, naam As String, volgorde As Integer)
        Me.Code = code
        Me.Naam = naam
        Me.Volgorde = volgorde
    End Sub
End Class

''' <summary>
''' Standaard vergunningschecklist + de mapping van bekende sjabloon-mijlpaal-Codes naar de
''' bijhorende checklist-stap. Enige bron van waarheid — gebruikt door
''' <c>ProjectDossierService</c> (auto-koppeling bij dossieraanmaak/-sync) en door de
''' trajectsjabloon-admin-UI (BronParam-keuzelijst voor de <c>DossierSubstap</c>-binding).
''' </summary>
Public Module VergunningChecklistDefaults

    Public ReadOnly Stappen As VergunningChecklistStapBO() = {
        New VergunningChecklistStapBO("INGEDIEND", "Ingediend", 10),
        New VergunningChecklistStapBO("VOLLEDIG_VERKLAARD", "Volledig en ontvankelijk verklaard", 20),
        New VergunningChecklistStapBO("OPENBAAR_ONDERZOEK", "Openbaar onderzoek afgerond", 30),
        New VergunningChecklistStapBO("ADVIEZEN", "Adviezen ontvangen", 40),
        New VergunningChecklistStapBO("COLLEGEBESLISSING", "Beslissing college van B&W", 50),
        New VergunningChecklistStapBO("BEROEPSTERMIJN", "Beroepstermijn verstreken", 60),
        New VergunningChecklistStapBO("DEFINITIEF", "Vergunning definitief", 70)
    }

    ''' <summary>Bekende sjabloon-mijlpaal-Codes voor de vergunningsstappen -> substap-Code, voor automatisch koppelen.</summary>
    Public ReadOnly MijlpaalNaarSubstap As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
        {"VERGUNNING_INGEDIEND", "INGEDIEND"},
        {"VERGUNNING_VOLLEDIG", "VOLLEDIG_VERKLAARD"},
        {"OPENBAAR_ONDERZOEK", "OPENBAAR_ONDERZOEK"},
        {"VERGUNNING_VERLEEND", "COLLEGEBESLISSING"},
        {"VERGUNNING_DEFINITIEF", "DEFINITIEF"}
    }

End Module
