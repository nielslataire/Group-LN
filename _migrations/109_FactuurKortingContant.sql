-- =============================================
-- Migratie: 109_FactuurKortingContant
-- Datum: 2026-10-22
-- Omschrijving: Nacalculatie, factuur zonder contract (design-handoff 42k): korting contant die je zelf invult wordt bij de factuur bewaard.
--   IncommingInvoices.KortingPercentage : % korting contant (NULL = geen).
--   IncommingInvoices.KortingDagen      : aantal dagen na de factuurdatum waarbinnen de korting geldt.
--   Enkel voor facturen zonder contract; met een contract komt de korting uit het contract. Strikt additief + idempotent.
-- =============================================
IF COL_LENGTH('dbo.IncommingInvoices', 'KortingPercentage') IS NULL
BEGIN
    ALTER TABLE [dbo].[IncommingInvoices] ADD [KortingPercentage] DECIMAL(5,2) NULL;
    PRINT 'Kolom IncommingInvoices.KortingPercentage toegevoegd.';
END
ELSE PRINT 'Kolom IncommingInvoices.KortingPercentage bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.IncommingInvoices', 'KortingDagen') IS NULL
BEGIN
    ALTER TABLE [dbo].[IncommingInvoices] ADD [KortingDagen] INT NULL;
    PRINT 'Kolom IncommingInvoices.KortingDagen toegevoegd.';
END
ELSE PRINT 'Kolom IncommingInvoices.KortingDagen bestaat al, overgeslagen.';
GO
