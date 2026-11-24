-- Alleen lezen: lijst van activiteitgroepen (loten) met hun activiteiten.
-- Kopieer het resultaat (Results to Text of Grid > Alles kopiëren met headers) naar Claude.
SELECT
    g.Lot,
    g.Name              AS Groep,
    a.Volgnr,
    CASE WHEN g.Lot IS NOT NULL AND a.Volgnr IS NOT NULL
         THEN CONCAT(CAST(g.Lot AS int), '.', a.Volgnr) END AS Lotnummer,
    a.Omschrijving      AS Activiteit,
    (SELECT COUNT(*) FROM ContractActivity ca WHERE ca.ActivityId = a.ActivityId) AS InContracten
FROM ActivityGroup g
LEFT JOIN Activity a ON a.GroupId = g.GroupId
ORDER BY g.Lot, g.Name, a.Volgnr, a.Omschrijving;
