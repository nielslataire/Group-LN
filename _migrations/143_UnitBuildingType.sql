-- =============================================
-- Migratie: 143_UnitBuildingType
-- Omschrijving: Woningen krijgen een bebouwingstype onder het subtype (Projecten/EditUnit):
--   1 = open, 2 = halfopen, 3 = gesloten. NULL = niet ingevuld / niet van toepassing.
--   De publieke projectpagina (WWWCOPRO projects/detail) toont dit onder de naam van de eenheid.
--   Additief en idempotent.
-- =============================================

IF COL_LENGTH('dbo.Units', 'BuildingType') IS NULL
BEGIN
    ALTER TABLE [dbo].[Units] ADD [BuildingType] INT NULL;
    PRINT 'Kolom Units.BuildingType toegevoegd.';
END
ELSE PRINT 'Kolom Units.BuildingType bestaat al, overgeslagen.';
GO
