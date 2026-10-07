''' <summary>
''' Herkomst van de vastgelegde prijs op een verkooplijn van de budgetwizard (stap 8).
''' Waarde bewaard in BudgetVerkoopLijnen.PrijsBron (migratie 075).
''' </summary>
Public Enum VerkoopPrijsBron As Byte
    ''' <summary>Verkoopvoorstel: kostprijs + marge.</summary>
    Voorstel = 1
    ''' <summary>Marktreferentie: mediaan €/m² van vergelijkbare units.</summary>
    Markt = 2
    ''' <summary>Referentiecode uit de prijsreferentietabel (€/m² × oppervlakte).</summary>
    Referentiecode = 3
    ''' <summary>Manueel ingestelde €/m² (bedrag afgeleid).</summary>
    ManueelPerM2 = 4
    ''' <summary>Manueel ingesteld bedrag (€/m² afgeleid).</summary>
    ManueelBedrag = 5
End Enum
