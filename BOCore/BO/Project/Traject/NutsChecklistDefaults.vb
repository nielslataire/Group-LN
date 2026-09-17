''' <summary>
''' Standaard checklist voor een Nutsaansluiting-dossier — dezelfde generieke
''' ProjectDossierSubstap-mechanica als VergunningChecklistDefaults, toegepast op het
''' nutsaanvraagtraject (aanvraag, offerte, uitvoering, keuring, overdracht).
''' </summary>
Public Module NutsChecklistDefaults

    Public ReadOnly Stappen As VergunningChecklistStapBO() = {
        New VergunningChecklistStapBO("AANVRAAG", "Aanvraag verstuurd", 10),
        New VergunningChecklistStapBO("OFFERTE_ONTVANGEN", "Offerte ontvangen", 20),
        New VergunningChecklistStapBO("OFFERTE_GOEDGEKEURD", "Offerte goedgekeurd", 30),
        New VergunningChecklistStapBO("UITVOERINGSDATUM_DOORGEGEVEN", "Uitvoeringsdatum doorgegeven", 40),
        New VergunningChecklistStapBO("UITGEVOERD", "Aansluiting uitgevoerd", 50),
        New VergunningChecklistStapBO("GEKEURD", "Keuring goedgekeurd", 60),
        New VergunningChecklistStapBO("OVERGEDRAGEN", "Overgedragen aan netbeheerder / actief", 70)
    }

End Module
