-- =============================================
-- Migratie: 081_PuntenPlannenAlgemeen
-- Datum: 2026-10-09
-- Omschrijving: Punten, "Op plan" (design-handoff 40b): plannen die niet bij één eenheid horen (inplantingsplan, gevels …).
--   UnitExecutionPlan.ProjectId : het project van een algemeen plan.
--   UnitExecutionPlan.UnitId    : wordt NULL-baar (NULL = algemeen plan). Bestaande rijen blijven ongewijzigd.
--   Idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UnitExecutionPlan') AND name = 'ProjectId')
BEGIN
    ALTER TABLE [dbo].[UnitExecutionPlan] ADD [ProjectId] INT NULL;
    PRINT 'Kolom ProjectId toegevoegd aan UnitExecutionPlan.';
END
ELSE PRINT 'Kolom ProjectId bestaat al, overgeslagen.';
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UnitExecutionPlan') AND name = 'UnitId' AND is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[UnitExecutionPlan] ALTER COLUMN [UnitId] INT NULL;
    PRINT 'UnitExecutionPlan.UnitId is nu NULL-baar.';
END
ELSE PRINT 'UnitExecutionPlan.UnitId was al NULL-baar, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UnitExecutionPlan_ProjectId' AND object_id = OBJECT_ID('dbo.UnitExecutionPlan'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UnitExecutionPlan_ProjectId] ON [dbo].[UnitExecutionPlan]([ProjectId]) WHERE [ProjectId] IS NOT NULL;
    PRINT 'Index IX_UnitExecutionPlan_ProjectId aangemaakt.';
END
GO
