-- =============================================
-- Migratie: 150_PlanningUitvoerder
-- Omschrijving: Uitvoerder (aannemer) bij een planningsfase en een planningstaak.
--   * PlanningSectie.UitvoerderCompanyId (nullable)  - CompanyInfo.CompanyId
--   * PlanningTaak.UitvoerderCompanyId   (nullable)  - taken zonder eigen uitvoerder nemen die van hun fase over
--   Enkel nieuwe, nullable kolommen. Additief en idempotent. Draai vóór app-start.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSectie]') AND name = N'UitvoerderCompanyId')
BEGIN
    ALTER TABLE [dbo].[PlanningSectie] ADD [UitvoerderCompanyId] INT NULL;
    PRINT 'Kolom PlanningSectie.UitvoerderCompanyId toegevoegd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningTaak]') AND name = N'UitvoerderCompanyId')
BEGIN
    ALTER TABLE [dbo].[PlanningTaak] ADD [UitvoerderCompanyId] INT NULL;
    PRINT 'Kolom PlanningTaak.UitvoerderCompanyId toegevoegd.';
END
GO
