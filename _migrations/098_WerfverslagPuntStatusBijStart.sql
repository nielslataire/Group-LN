-- =============================================
-- Migratie: 098_WerfverslagPuntStatusBijStart
-- Datum: 2026-10-14
-- Omschrijving: Werfverslag-PDF (design-handoff 35l): bij openstaande punten staat "vorig verslag" (status toen) naast "nu".
--   WerfverslagPunt.StatusBijStart : status van het punt op het moment dat het verslag gestart (hernomen) werd; NULL = onbekend/nieuw punt.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.WerfverslagPunt') AND name = 'StatusBijStart')
BEGIN
    ALTER TABLE [dbo].[WerfverslagPunt] ADD [StatusBijStart] INT NULL;
    PRINT 'Kolom StatusBijStart toegevoegd aan WerfverslagPunt.';
END
ELSE PRINT 'Kolom StatusBijStart bestaat al, overgeslagen.';
GO
