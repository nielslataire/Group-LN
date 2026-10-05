-- =============================================
-- Migratie: 071_ChangeOrderSavedAt
-- Datum: 2026-10-05
-- Omschrijving: "Opgeslagen op dd/MM/yyyy HH:mm" op het scherm van een offerte/wijzigingsopdracht
--   (ChangeOrderDetailV2). SavedAt wordt gezet telkens het scherm wordt opgeslagen. NULL bij rijen die
--   nog nooit via het scherm zijn opgeslagen. Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'SavedAt')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [SavedAt] DATETIME2(0) NULL;
    PRINT 'Kolom SavedAt toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Kolom SavedAt bestaat al, overgeslagen.';
GO
