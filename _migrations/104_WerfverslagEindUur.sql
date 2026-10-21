-- =============================================
-- Migratie: 104_WerfverslagEindUur
-- Datum: 2026-10-21
-- Omschrijving: Werfverslag (design-handoff 35k): bij de datum staat "16:29 - 17:40" (begin en einde van het bezoek).
--   Werfverslag.EindUur : einduur van het bezoek (nullable). Strikt additief + idempotent.
--   "Verontschuldigd" heeft geen kolom nodig: het is een vlag in Werfverslag.AanwezigenJson ("excused").
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Werfverslag') AND name = 'EindUur')
BEGIN
    ALTER TABLE [dbo].[Werfverslag] ADD [EindUur] TIME(0) NULL;
    PRINT 'Kolom EindUur toegevoegd aan Werfverslag.';
END
ELSE PRINT 'Kolom EindUur bestaat al, overgeslagen.';
GO
