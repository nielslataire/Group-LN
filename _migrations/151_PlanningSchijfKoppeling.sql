-- =============================================
-- Migratie: 151_PlanningSchijfKoppeling
-- Omschrijving: koppelt een betalingsschijf (InvoicingPaymentStages) per planning aan een taak of fase van die planning.
--   Een eenheid bereikt de schijf dan via de taak/fase in de planning van die eenheid ("schijf bereikt, markeren?").
--   * Nieuwe tabel PlanningSchijfKoppeling; per planning hoogstens één koppeling per schijf (unieke index).
--   * BouwPlanningId, TaakId en SectieId zijn gewone kolommen zonder foreign key (een verwijderde taak laat een
--     verweesde koppeling achter die genegeerd en bij het volgende koppelen opgeruimd wordt). Wel FK naar de schijf (cascade).
--   Additief en idempotent. Draai vóór app-start.
-- =============================================

IF OBJECT_ID(N'[dbo].[PlanningSchijfKoppeling]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PlanningSchijfKoppeling] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PlanningSchijfKoppeling] PRIMARY KEY,
        [PaymentStageId] INT NOT NULL,
        [BouwPlanningId] INT NOT NULL,   -- 0 = de hoofdplanning als die nog niet in de database staat
        [TaakId] INT NULL,
        [SectieId] INT NULL,
        CONSTRAINT [FK_PlanningSchijfKoppeling_Stage] FOREIGN KEY ([PaymentStageId]) REFERENCES [dbo].[InvoicingPaymentStages]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabel PlanningSchijfKoppeling aangemaakt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PlanningSchijfKoppeling_Planning_Stage' AND object_id = OBJECT_ID(N'[dbo].[PlanningSchijfKoppeling]'))
    CREATE UNIQUE INDEX [UX_PlanningSchijfKoppeling_Planning_Stage] ON [dbo].[PlanningSchijfKoppeling]([BouwPlanningId], [PaymentStageId]);
GO
