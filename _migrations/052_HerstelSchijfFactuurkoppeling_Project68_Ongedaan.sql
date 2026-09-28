-- =============================================
-- Herstelscript: 052_HerstelSchijfFactuurkoppeling_Project68_Ongedaan
-- Datum: 2026-09-25
-- Omschrijving: Zet de FOUTE koppelingen terug die de eerste run van 051 maakte.
--   Die versie van 051 viel bij een schijf zonder tekstmatch terug op "hetzelfde bedrag". Dakbedekking en
--   buitenschrijnwerk (10 % = € 4.800) kregen zo de factuurlijn "vloerplaat gelijkvloers – 10%" toegewezen,
--   terwijl ze niet gefactureerd zijn.
--
--   Regel: een schijf van project 68 die aan factuur 1033 hangt maar waarvan de omschrijving in GEEN ENKELE
--   lijn van die factuur voorkomt, is niet echt gefactureerd → InvoiceId terug op NULL.
--   Draai daarna 051 opnieuw (die is nu enkel op tekst gebaseerd) om de schijven die wél gefactureerd zijn
--   maar nog niet gekoppeld (vloerplaat 1ste en 2de verdieping) alsnog te koppelen.
-- =============================================

DECLARE @projectId INT = 68;
DECLARE @invoiceId INT = 1033;

-- Voorbeeld: welke schijven worden losgekoppeld
SELECT s.Id, s.SortOrder, s.Description, s.Percentage, s.InvoiceId
FROM [dbo].[ProjectContractSlice] s
WHERE s.ProjectId = @projectId
  AND s.InvoiceId = @invoiceId
  AND NOT EXISTS (SELECT 1 FROM [dbo].[InvoicesDetails] d WHERE d.InvoiceId = @invoiceId AND CHARINDEX(s.Description, d.Text) > 0)
ORDER BY s.SortOrder, s.Id;

UPDATE s
SET s.InvoiceId = NULL
FROM [dbo].[ProjectContractSlice] s
WHERE s.ProjectId = @projectId
  AND s.InvoiceId = @invoiceId
  AND NOT EXISTS (SELECT 1 FROM [dbo].[InvoicesDetails] d WHERE d.InvoiceId = @invoiceId AND CHARINDEX(s.Description, d.Text) > 0);
PRINT 'Foute koppelingen verwijderd (aantal: ' + CAST(@@ROWCOUNT AS VARCHAR(20)) + ').';
