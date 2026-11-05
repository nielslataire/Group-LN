-- =============================================
-- Migratie: 128_OudeFacturenNormaliseren
-- Datum: 2026-11-05
-- Omschrijving: De facturen uit de oude werkwijze (dbo.Invoices met StatusId NULL, zonder facturatiebedrijf en meestal zonder project, regels zonder LineType)
--   horen niet bij de nieuwe lijsten, de klantpagina en de berekening "reeds gefactureerd". Dit script brengt ze op dezelfde vorm als nieuwe facturen:
--     1. regels met een betalingsschijf krijgen LineType = 'Stages' (daardoor staat de schijf van die eenheid als gefactureerd, dus niet meer onder "Klaar om te factureren")
--     2. de factuur krijgt ProjectId (via de eenheid of de schijf van haar regels; elke oude factuur heeft er precies één) en IssuerCompanyId
--        (facturatiebedrijf bouwheer van het project; heeft het project er geen, dan @FallbackIssuerId = BCO, id 4, zo gekozen door Niels op 05/11/2026)
--     3. de factuur krijgt StatusId = 5 (Betaald) en een betaling (dbo.Payments + dbo.PaymentAllocations) ter grootte van het factuurtotaal, zodat het
--        saldo 0 is en ze niet als openstaand of vervallen meetellen. Betaaldatum = factuurdatum (de echte datum is niet bekend); Methode 'Historisch'.
--   Enkel rijen met StatusId IS NULL AND IssuerCompanyId IS NULL worden aangepast (een oude factuur die al een ProjectId had, houdt dat), dus het script is idempotent en raakt nieuwe facturen niet.
--   Geen schemawijziging; draai het voor of na de app-start. Eerst een kopie van de database nemen is verstandig: dit zijn UPDATE's op ~944 facturen.
-- =============================================
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

DECLARE @FallbackIssuerId INT = 4; -- BCO

IF OBJECT_ID('tempdb..#Legacy') IS NOT NULL DROP TABLE #Legacy;

-- Elke oude factuur met precies één afgeleid project (via eenheid of schijf van de regels).
SELECT i.Id AS InvoiceId,
       MIN(COALESCE(u.ProjectId, g.ProjectId)) AS ProjectId
INTO #Legacy
FROM dbo.Invoices i
JOIN dbo.InvoicesDetails d ON d.InvoiceId = i.Id
LEFT JOIN dbo.Units u ON u.Id = d.UnitId
LEFT JOIN dbo.InvoicingPaymentStages s ON s.Id = d.PaymentStageId
LEFT JOIN dbo.InvoicingPaymentGroup g ON g.Id = s.GroupId
WHERE i.StatusId IS NULL AND i.IssuerCompanyId IS NULL
GROUP BY i.Id
HAVING COUNT(DISTINCT COALESCE(u.ProjectId, g.ProjectId)) = 1
   AND SUM(CASE WHEN COALESCE(u.ProjectId, g.ProjectId) IS NULL THEN 1 ELSE 0 END) = 0;

DECLARE @Aantal INT = (SELECT COUNT(*) FROM #Legacy);
PRINT CONCAT('Oude facturen om te normaliseren: ', @Aantal);

-- 1. Regels met een schijf als schijfregel markeren.
UPDATE d
SET d.LineType = 'Stages'
FROM dbo.InvoicesDetails d
JOIN #Legacy l ON l.InvoiceId = d.InvoiceId
WHERE d.LineType IS NULL AND d.PaymentStageId IS NOT NULL;
SET @Aantal = @@ROWCOUNT;
PRINT CONCAT('Regels op LineType Stages gezet: ', @Aantal);

-- 2. Betalingen aanmaken (eenmalig per factuur, herkenbaar aan de notitie) voor het factuurtotaal.
INSERT INTO dbo.Payments (CompanyID, ReceivedDate, Amount, CurrencyCode, Method, Notes)
SELECT COALESCE(p.IssuerCompanyIdBuilder, @FallbackIssuerId),
       i.Date,
       ISNULL(t.GrossTotal, 0),
       N'EUR',
       N'Historisch',
       CONCAT(N'Oude factuur ', i.Id, N': als betaald overgenomen (migratie 128)')
FROM #Legacy l
JOIN dbo.Invoices i ON i.Id = l.InvoiceId
JOIN dbo.Project p ON p.ProjectId = l.ProjectId
LEFT JOIN dbo.vwInvoiceTotals t ON t.Id = i.Id
WHERE ISNULL(t.GrossTotal, 0) > 0
  AND NOT EXISTS (SELECT 1 FROM dbo.Payments x WHERE x.Notes = CONCAT(N'Oude factuur ', i.Id, N': als betaald overgenomen (migratie 128)'));
SET @Aantal = @@ROWCOUNT;
PRINT CONCAT('Betalingen aangemaakt: ', @Aantal);

INSERT INTO dbo.PaymentAllocations (PaymentId, InvoiceId, Amount, CreatedAt)
SELECT pay.Id, l.InvoiceId, pay.Amount, SYSUTCDATETIME()
FROM #Legacy l
JOIN dbo.Payments pay ON pay.Notes = CONCAT(N'Oude factuur ', l.InvoiceId, N': als betaald overgenomen (migratie 128)')
WHERE NOT EXISTS (SELECT 1 FROM dbo.PaymentAllocations a WHERE a.PaymentId = pay.Id AND a.InvoiceId = l.InvoiceId);
SET @Aantal = @@ROWCOUNT;
PRINT CONCAT('Toewijzingen aangemaakt: ', @Aantal);

-- 3. Factuur zelf: project, facturatiebedrijf en status Betaald.
UPDATE i
SET i.ProjectId = ISNULL(i.ProjectId, l.ProjectId),
    i.IssuerCompanyId = COALESCE(p.IssuerCompanyIdBuilder, @FallbackIssuerId),
    i.StatusId = 5
FROM dbo.Invoices i
JOIN #Legacy l ON l.InvoiceId = i.Id
JOIN dbo.Project p ON p.ProjectId = l.ProjectId
WHERE i.StatusId IS NULL AND i.IssuerCompanyId IS NULL;
SET @Aantal = @@ROWCOUNT;
PRINT CONCAT('Facturen bijgewerkt: ', @Aantal);

-- Controle: de som van alle oude facturen en wat er nog openstaat (moet 0 zijn).
SELECT COUNT(*) AS AantalOudeFacturen,
       SUM(b.GrossTotal) AS TotaalGefactureerd,
       SUM(b.Balance) AS NogOpenstaand
FROM dbo.Invoices i
JOIN dbo.vwInvoiceBalance b ON b.Id = i.Id
WHERE i.StatusId = 5 AND i.Id IN (SELECT InvoiceId FROM #Legacy);

DROP TABLE #Legacy;

-- Het script bewaart de wijzigingen meteen (COMMIT). Wil je de controle eerst bekijken: vervang de volgende regel door ROLLBACK TRANSACTION, kijk na, en draai het daarna opnieuw met COMMIT.
COMMIT TRANSACTION;
GO
