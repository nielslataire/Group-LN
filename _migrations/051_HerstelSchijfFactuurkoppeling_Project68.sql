-- =============================================
-- Herstelscript: 051_HerstelSchijfFactuurkoppeling_Project68
-- Datum: 2026-09-25
-- Omschrijving: Project 68 heeft contractschijven gefactureerd (factuur 1033), maar die schijven staan
--   op Projecten/DetailCoordinatie niet meer als gefactureerd.
--
--   Oorzaak: ProjectenController.CoordinatieInstellingen (POST) gaf bij het opslaan geen Id mee per
--   schijf. ProjectService.SaveContractSlices behandelde daardoor elke schijf als nieuw: alle
--   bestaande schijven werden verwijderd en opnieuw aangemaakt — zonder ProjectContractSlice.InvoiceId.
--   Elke keer dat de coördinatie-instellingen werden opgeslagen (bv. om een uurtarief aan te passen)
--   raakte de factuurkoppeling dus kwijt. Die bug is in de code hersteld; dit script zet de koppeling
--   voor project 68 terug.
--
--   Werkwijze: de lijnen op factuur 1033 heten 'Bij de aanvang van de vloerplaat 1ste verdieping - 5%' enz.
--   (geen 'Projectcoördinatie – …' zoals MakeCoordSliceInvoices ze schrijft; de tekst is dus aangepast of de
--   factuur is elders gemaakt). Per schijf zonder koppeling zoeken we daarom een lijn waarin de omschrijving van
--   de schijf voorkomt, en koppelen enkel bij PRECIES ÉÉN treffer.
--   LES uit de eerste run: een terugval op "hetzelfde bedrag" is fout gegaan — twee schijven van 10 %
--   (dakbedekking, buitenschrijnwerk) kregen de lijn 'vloerplaat gelijkvloers – 10%' omdat het bedrag gelijk is.
--   Bedrag is dus GEEN criterium meer. Zit er een verkeerde koppeling uit die eerste run in, draai dan eerst 052.
--   Enkel schijven van dit project, enkel waar InvoiceId nog NULL is — veilig om opnieuw te draaien.
--   Wat na STAP 1 overblijft koppel je in de pagina zelf: schijf → ⋯ → Factuur koppelen.
-- =============================================

DECLARE @projectId INT = 68;
DECLARE @invoiceId INT = 1033;

-- STAP 0: de echte lijnen van de factuur
SELECT d.Id AS FactuurlijnId, d.LineType, d.Text, d.Quantity, d.UnitPrice, d.Price, d.VatPercentage
FROM [dbo].[InvoicesDetails] d
WHERE d.InvoiceId = @invoiceId
ORDER BY d.Id;

-- STAP 1: voorbeeld — welke schijf krijgt welke factuurlijn (enkel op tekst)
SELECT s.Id AS SliceId, s.SortOrder, s.Description, s.Percentage,
       d.Id AS FactuurlijnId, d.Text AS Factuurlijn, d.Price
FROM [dbo].[ProjectContractSlice] s
JOIN [dbo].[InvoicesDetails] d
  ON d.InvoiceId = @invoiceId
 AND CHARINDEX(s.Description, d.Text) > 0
WHERE s.ProjectId = @projectId
  AND s.InvoiceId IS NULL
ORDER BY s.SortOrder, s.Id;

-- STAP 2: uitvoeren (enkel schijven met precies één lijn waarin hun omschrijving voorkomt)
UPDATE s
SET s.InvoiceId = @invoiceId
FROM [dbo].[ProjectContractSlice] s
WHERE s.ProjectId = @projectId
  AND s.InvoiceId IS NULL
  AND (
        SELECT COUNT(*)
        FROM [dbo].[InvoicesDetails] d
        WHERE d.InvoiceId = @invoiceId
          AND CHARINDEX(s.Description, d.Text) > 0
      ) = 1;
PRINT 'Schijven van project ' + CAST(@projectId AS VARCHAR(10)) + ' opnieuw gekoppeld aan factuur ' + CAST(@invoiceId AS VARCHAR(10)) + ' (aantal: ' + CAST(@@ROWCOUNT AS VARCHAR(20)) + ').';

-- Controle
SELECT s.Id, s.SortOrder, s.Description, s.Percentage, s.InvoiceId
FROM [dbo].[ProjectContractSlice] s
WHERE s.ProjectId = @projectId
ORDER BY s.SortOrder, s.Id;
