-- =============================================
-- Migratie: 154_PlanningCodes
-- Omschrijving: taken en fases die uit een planningssjabloon komen onthouden de code van het sjabloon (bv. RUWBOUW_KLAAR).
--   Een mijlpaal in het traject met dezelfde code hangt dan automatisch aan die taak of fase, in beide richtingen
--   (planningssjabloon toepassen op een project met traject, of traject toepassen op een project met planning).
--   * Nieuwe nullable kolommen PlanningTaak.Code en PlanningSectie.Code.
--   Additief en idempotent. Draai vóór app-start.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningTaak]') AND name = N'Code')
BEGIN
    ALTER TABLE [dbo].[PlanningTaak] ADD [Code] NVARCHAR(100) NULL;
    PRINT 'Kolom PlanningTaak.Code toegevoegd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSectie]') AND name = N'Code')
BEGIN
    ALTER TABLE [dbo].[PlanningSectie] ADD [Code] NVARCHAR(100) NULL;
    PRINT 'Kolom PlanningSectie.Code toegevoegd.';
END
GO
