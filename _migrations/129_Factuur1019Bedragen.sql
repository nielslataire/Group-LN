-- =============================================
-- Migratie: 129_Factuur1019Bedragen
-- Datum: 2026-11-05
-- Omschrijving: Oude factuur 1019 (0020.09.2025, Dhondt - Nelis, Lot 3, project Verkaveling Ketenhoekstraat) heeft 9 schijfregels (schijf 249 t.e.m. 257) zonder bedrag
--   en zonder btw-percentage, waardoor haar totaal NULL is. Het bedrag is af te leiden uit de verkoopwaarde van de eenheid en het percentage van elke schijf:
--   ValueSold 405.000 x (5+20+15+10+10+10+5+5+5 = 85 %) = 344.250,00 netto. Dat sluit aan op de latere facturen van dezelfde eenheid (1037: schijf 258 en 259,
--   1048: schijf 260, elk 20.250,00 = 5 %), samen 100 %.
--   BTW: de schijven staan op 0 % ingesteld, maar de twee latere facturen van deze eenheid gebruiken 21 %. Dit script gebruikt 21 % via @Btw: pas die waarde aan
--   (bv. 6) vóór je het draait als dit niet klopt. Daarna krijgt de factuur, net als de andere oude facturen (migratie 128), een betaling ter grootte van het totaal.
--   Idempotent: enkel regels met Price IS NULL worden ingevuld, de betaling wordt maar één keer aangemaakt. Draai eerst migratie 128.
-- =============================================
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

DECLARE @InvoiceId INT = 1019;
DECLARE @Btw DECIMAL(5,2) = 21;
DECLARE @Aantal INT;

UPDATE d
SET d.Price = ROUND(cv.ValueSold * s.Percentage / 100, 2),
    d.VatPercentage = @Btw
FROM dbo.InvoicesDetails d
JOIN dbo.InvoicingPaymentStages s ON s.Id = d.PaymentStageId
JOIN dbo.UnitConstructionValue cv ON cv.UnitId = d.UnitId AND cv.PaymentGroupId = s.GroupId
WHERE d.InvoiceId = @InvoiceId AND d.Price IS NULL AND cv.ValueSold > 0;
SET @Aantal = @@ROWCOUNT;
PRINT CONCAT('Regels ingevuld: ', @Aantal);

INSERT INTO dbo.Payments (CompanyID, ReceivedDate, Amount, CurrencyCode, Method, Notes)
SELECT i.IssuerCompanyId, i.Date, t.GrossTotal, N'EUR', N'Historisch', CONCAT(N'Oude factuur ', i.Id, N': als betaald overgenomen (migratie 128)')
FROM dbo.Invoices i
JOIN dbo.vwInvoiceTotals t ON t.Id = i.Id
WHERE i.Id = @InvoiceId AND i.IssuerCompanyId IS NOT NULL AND ISNULL(t.GrossTotal, 0) > 0
  AND NOT EXISTS (SELECT 1 FROM dbo.Payments x WHERE x.Notes = CONCAT(N'Oude factuur ', i.Id, N': als betaald overgenomen (migratie 128)'));
SET @Aantal = @@ROWCOUNT;
PRINT CONCAT('Betalingen aangemaakt: ', @Aantal);

INSERT INTO dbo.PaymentAllocations (PaymentId, InvoiceId, Amount, CreatedAt)
SELECT pay.Id, @InvoiceId, pay.Amount, SYSUTCDATETIME()
FROM dbo.Payments pay
WHERE pay.Notes = CONCAT(N'Oude factuur ', @InvoiceId, N': als betaald overgenomen (migratie 128)')
  AND NOT EXISTS (SELECT 1 FROM dbo.PaymentAllocations a WHERE a.PaymentId = pay.Id AND a.InvoiceId = @InvoiceId);
SET @Aantal = @@ROWCOUNT;
PRINT CONCAT('Toewijzingen aangemaakt: ', @Aantal);

-- Controle: netto 344.250,00, bruto bij 21 % = 416.542,50, openstaand 0.
SELECT i.Id, t.LinesNet, t.GrossTotal, b.Paid, b.Balance
FROM dbo.Invoices i
JOIN dbo.vwInvoiceTotals t ON t.Id = i.Id
JOIN dbo.vwInvoiceBalance b ON b.Id = i.Id
WHERE i.Id = @InvoiceId;

COMMIT TRANSACTION;
GO
