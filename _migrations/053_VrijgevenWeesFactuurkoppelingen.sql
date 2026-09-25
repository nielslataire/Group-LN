-- =============================================
-- Herstelscript: 053_VrijgevenWeesFactuurkoppelingen
-- Datum: 2026-09-25
-- Omschrijving: Coördinatieprojecten (Projecten/DetailCoordinatie): een verwijderde concept-/proformafactuur
--   maakte de schijven en regie-uren die erop stonden niet meer vrij (InvoiceCommandService.DeleteAsync raakte
--   die koppelingen niet aan). Ze bleven "gefactureerd" op een factuur die niet meer bestaat.
--   De code is aangepast: DeleteAsync maakt ze nu los, en de pagina ruimt bij het laden zulke wees-koppelingen
--   zelf op. Dit script doet hetzelfde eenmalig voor alle projecten. Voorbeeld eerst, dan uitvoeren.
--   Strikt beperkt tot koppelingen naar een factuur die niet (meer) bestaat — idempotent.
-- =============================================

-- Voorbeeld
SELECT 'Schijf' AS Soort, s.Id, s.ProjectId, s.Description AS Omschrijving, s.InvoiceId
FROM [dbo].[ProjectContractSlice] s
WHERE s.InvoiceId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Invoices] i WHERE i.Id = s.InvoiceId)
UNION ALL
SELECT 'Prestatie', r.Id, r.ProjectId, CONVERT(nvarchar(255), r.[Date]), r.InvoiceId
FROM [dbo].[ProjectRegieUur] r
WHERE r.InvoiceId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Invoices] i WHERE i.Id = r.InvoiceId);

UPDATE s SET s.InvoiceId = NULL
FROM [dbo].[ProjectContractSlice] s
WHERE s.InvoiceId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Invoices] i WHERE i.Id = s.InvoiceId);
PRINT 'Schijven vrijgegeven: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

UPDATE r SET r.InvoiceId = NULL, r.HourlyRateInvoiced = NULL
FROM [dbo].[ProjectRegieUur] r
WHERE r.InvoiceId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Invoices] i WHERE i.Id = r.InvoiceId);
PRINT 'Prestaties vrijgegeven: ' + CAST(@@ROWCOUNT AS VARCHAR(20));
