-- =============================================
-- Migratie: 157_PlanningSjabloonTaakVoorwaarde
-- Omschrijving: een voorwaarde per taak in een planningssjabloon (naast de voorwaarde per blok), zodat één blok per bouwlaag
--   taken kan bevatten die enkel op het gelijkvloers, enkel zonder kelder of enkel bij terrassen of luifels voorkomen.
--   * PlanningSjabloonTaak.Voorwaarde - lijst "variabele:waarde" gescheiden door komma's, bv. "laag:eerste,kelder:nee".
--     kelder:ja|nee · dak:hellend|plat · terras:ja|nee · laag:eerste|laatste|boven. Leeg = altijd.
--   Enkel een nieuwe, nullable kolom. Additief en idempotent. Draai vóór app-start.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSjabloonTaak]') AND name = N'Voorwaarde')
BEGIN
    ALTER TABLE [dbo].[PlanningSjabloonTaak] ADD [Voorwaarde] NVARCHAR(200) NULL;
    PRINT 'Kolom PlanningSjabloonTaak.Voorwaarde toegevoegd.';
END
GO
