-- =============================================
-- Migratie: 097_PuntenPlanSoortNiveauVersie
-- Datum: 2026-10-13
-- Omschrijving: Punten, plan kiezen / nieuw plan (design-handoff 40p/40q): een uitvoeringsplan heeft een soort (grondplan, gevel, doorsnede,
--   situering, technisch), een niveau (kelder, gelijkvloers, verdieping …) en een versie (zelfde naam in dezelfde eenheid/het project = nieuwe versie).
--   UnitExecutionPlan.Soort  : NULL = niet opgegeven.
--   UnitExecutionPlan.Niveau : NULL = niet opgegeven.
--   UnitExecutionPlan.Versie : 1 voor bestaande plannen.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UnitExecutionPlan') AND name = 'Soort')
BEGIN
    ALTER TABLE [dbo].[UnitExecutionPlan] ADD [Soort] NVARCHAR(30) NULL;
    PRINT 'Kolom Soort toegevoegd aan UnitExecutionPlan.';
END
ELSE PRINT 'Kolom Soort bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UnitExecutionPlan') AND name = 'Niveau')
BEGIN
    ALTER TABLE [dbo].[UnitExecutionPlan] ADD [Niveau] NVARCHAR(60) NULL;
    PRINT 'Kolom Niveau toegevoegd aan UnitExecutionPlan.';
END
ELSE PRINT 'Kolom Niveau bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UnitExecutionPlan') AND name = 'Versie')
BEGIN
    ALTER TABLE [dbo].[UnitExecutionPlan] ADD [Versie] INT NOT NULL CONSTRAINT DF_UnitExecutionPlan_Versie DEFAULT 1;
    PRINT 'Kolom Versie toegevoegd aan UnitExecutionPlan.';
END
ELSE PRINT 'Kolom Versie bestaat al, overgeslagen.';
GO
