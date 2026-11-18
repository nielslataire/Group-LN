-- =============================================
-- Migratie: 147_PlanningSjabloonBeheer
-- Omschrijving: Beheerpagina's van de planningssjablonen (design-handoff "CRM Planningssjablonen"):
--   * PlanningSjabloon.ReageertOp      - waarop het sjabloon later automatisch voorgesteld wordt (null = niets, handmatig toepassen)
--   * PlanningSjabloon.ModifiedByUserId - wie het sjabloon het laatst wijzigde (kolom "Gewijzigd" in de lijst)
--   * BouwPlanning.SjabloonId          - uit welk sjabloon een planning gemaakt is (kolom "Gebruikt in"; een sjabloon in gebruik kan niet verwijderd worden)
--   Enkel nieuwe, nullable kolommen. Additief en idempotent. Draai vóór app-start.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSjabloon]') AND name = N'ReageertOp')
BEGIN
    ALTER TABLE [dbo].[PlanningSjabloon] ADD [ReageertOp] NVARCHAR(40) NULL;
    PRINT 'Kolom PlanningSjabloon.ReageertOp toegevoegd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSjabloon]') AND name = N'ModifiedByUserId')
BEGIN
    ALTER TABLE [dbo].[PlanningSjabloon] ADD [ModifiedByUserId] NVARCHAR(450) NULL;
    PRINT 'Kolom PlanningSjabloon.ModifiedByUserId toegevoegd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BouwPlanning]') AND name = N'SjabloonId')
BEGIN
    ALTER TABLE [dbo].[BouwPlanning] ADD [SjabloonId] INT NULL;
    PRINT 'Kolom BouwPlanning.SjabloonId toegevoegd.';
END
GO

-- Apart blok: de index verwijst naar de kolom, die in het blok hierboven pas aangemaakt wordt.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[BouwPlanning]') AND name = N'IX_BouwPlanning_Sjabloon')
BEGIN
    CREATE INDEX [IX_BouwPlanning_Sjabloon] ON [dbo].[BouwPlanning] ([SjabloonId]) WHERE [SjabloonId] IS NOT NULL;
    PRINT 'Index IX_BouwPlanning_Sjabloon aangemaakt.';
END
GO
