-- =============================================
-- Migratie: 072_ChangeOrderSubject
-- Datum: 2026-10-06
-- Omschrijving: "Onderwerp" van een offerte/wijzigingsopdracht: de ene onderwerpregel boven de inleiding
--   op de gl-v2-PDF (design-handoff 35d). De bestaande "Omschrijving voor de klant" blijft de inleiding.
--   NULL = geen onderwerp. Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'Subject')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [Subject] NVARCHAR(150) NULL;
    PRINT 'Kolom Subject toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Kolom Subject bestaat al, overgeslagen.';
GO
