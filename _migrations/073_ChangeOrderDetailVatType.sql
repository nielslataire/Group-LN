-- =============================================
-- Migratie: 073_ChangeOrderDetailVatType
-- Datum: 2026-10-06
-- Omschrijving: Btw-code (Vattype van het facturatiebedrijf) per regel van een offerte/wijzigingsopdracht.
--   ChangeOrderDetail.VatPercentage blijft het toegepaste percentage (afgeleid van de code); VatTypeId bewaart
--   welke code gekozen werd (standaard de code van de betalingsgroep van de klant). NULL bij bestaande regels.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrderDetail') AND name = 'VatTypeId')
BEGIN
    ALTER TABLE [dbo].[ChangeOrderDetail] ADD [VatTypeId] INT NULL;
    PRINT 'Kolom VatTypeId toegevoegd aan ChangeOrderDetail.';
END
ELSE PRINT 'Kolom VatTypeId bestaat al, overgeslagen.';
GO
