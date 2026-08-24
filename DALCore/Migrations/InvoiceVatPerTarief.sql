-- ─────────────────────────────────────────────────────────────────────────────
-- Btw-berekening per tarief op de maatstaf van heffing
--
-- Voorheen: vwInvoiceTotals sommeerde Price * VatPercentage/100 per lijn zonder
-- afronding, en de applicatie rondde daarnaast per lijn af en telde op. Dat gaf
-- verschillen van 1 cent (bv. factuur 0030.08.2026: 269,45 i.p.v. 269,44) en
-- week af van de Octopus-boeking en de EPC-QR-code.
--
-- Nu: netto per lijn = Price - DiscountAmount; btw = ROUND(som netto per
-- btw-tarief × tarief, 2) — zelfde methode als InvoiceVatCalculator in
-- ServiceCore. T-SQL ROUND rondt .5 weg van nul, net als MidpointRounding.AwayFromZero.
--
-- Dit script wijzigt geen opgeslagen gegevens: de view berekent totalen
-- on-the-fly uit de detaillijnen.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER VIEW [dbo].[vwInvoiceTotals] AS
SELECT  i.Id,
        t.LinesNet,
        t.LinesVat,
        ISNULL(i.RoundingAmount, 0) AS Rounding,
        t.LinesNet + t.LinesVat + ISNULL(i.RoundingAmount, 0) AS GrossTotal
FROM dbo.Invoices i
LEFT JOIN (
    SELECT  r.InvoiceId,
            SUM(r.Net) AS LinesNet,
            SUM(r.Vat) AS LinesVat
    FROM (
        -- maatstaf en btw per btw-tarief
        SELECT  d.InvoiceId,
                SUM(d.Price - ISNULL(d.DiscountAmount, 0)) AS Net,
                ROUND(SUM(d.Price - ISNULL(d.DiscountAmount, 0)) * ISNULL(d.VatPercentage, 0) / 100, 2) AS Vat
        FROM dbo.InvoicesDetails d
        GROUP BY d.InvoiceId, d.VatPercentage
    ) r
    GROUP BY r.InvoiceId
) t ON t.InvoiceId = i.Id;

GO
