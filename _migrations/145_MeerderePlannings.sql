-- =============================================
-- Migratie: 145_MeerderePlannings
-- Omschrijving: Meerdere plannings per project (hoofdplanning + één planning per bouwdeel).
--   Nieuwe tabel BouwPlanning (een planning van een project; optioneel gekoppeld aan een bouwdeel = InvoicingPaymentGroup,
--   met een eigen startdatum) en PlanningSectie.BouwPlanningId (de planning waarin een fase zit).
--   Bestaande fases komen in de hoofdplanning van hun project. Planningen zonder bouwdeel zijn de hoofdplanning.
--   Additief en idempotent; de huidige CPMCore blijft werken (BouwPlanningId is nullable, null = hoofdplanning).
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[BouwPlanning]'))
BEGIN
    CREATE TABLE [dbo].[BouwPlanning] (
        [Id]               INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjectId]        INT           NOT NULL,
        [Naam]             NVARCHAR(200) NOT NULL,
        -- Hoofdplanning: geldt voor het hele project, zonder bouwdeel. Per project hoogstens één.
        [IsHoofd]          BIT           NOT NULL CONSTRAINT [DF_BouwPlanning_IsHoofd] DEFAULT 0,
        -- Bouwdeel (woning of gebouw) = betalingsgroep van het project; null bij de hoofdplanning.
        [PaymentGroupId]   INT           NULL,
        -- Eerste dag waarop deze planning mag starten; null = aanvangsdatum van het project.
        [StartDatum]       DATE          NULL,
        [Volgorde]         INT           NOT NULL CONSTRAINT [DF_BouwPlanning_Volgorde] DEFAULT 0,
        [IsActief]         BIT           NOT NULL CONSTRAINT [DF_BouwPlanning_IsActief] DEFAULT 1,
        [CreatedDate]      DATETIME2     NOT NULL CONSTRAINT [DF_BouwPlanning_Created] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_BouwPlanning_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]),
        CONSTRAINT [FK_BouwPlanning_PaymentGroup] FOREIGN KEY ([PaymentGroupId]) REFERENCES [dbo].[InvoicingPaymentGroup]([Id])
    );
    CREATE INDEX [IX_BouwPlanning_Project] ON [dbo].[BouwPlanning] ([ProjectId]);
    PRINT 'Tabel BouwPlanning aangemaakt.';
END
ELSE PRINT 'Tabel BouwPlanning bestaat al, overgeslagen.';
GO

-- Een bouwdeel heeft hoogstens één planning.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BouwPlanning_PaymentGroup' AND object_id = OBJECT_ID(N'[dbo].[BouwPlanning]'))
BEGIN
    CREATE UNIQUE INDEX [UX_BouwPlanning_PaymentGroup] ON [dbo].[BouwPlanning] ([PaymentGroupId]) WHERE [PaymentGroupId] IS NOT NULL;
    PRINT 'Index UX_BouwPlanning_PaymentGroup aangemaakt.';
END
GO

-- Een project heeft hoogstens één hoofdplanning.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BouwPlanning_Hoofd' AND object_id = OBJECT_ID(N'[dbo].[BouwPlanning]'))
BEGIN
    CREATE UNIQUE INDEX [UX_BouwPlanning_Hoofd] ON [dbo].[BouwPlanning] ([ProjectId]) WHERE [IsHoofd] = 1;
    PRINT 'Index UX_BouwPlanning_Hoofd aangemaakt.';
END
GO

IF COL_LENGTH('dbo.PlanningSectie', 'BouwPlanningId') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlanningSectie] ADD [BouwPlanningId] INT NULL;
    PRINT 'Kolom PlanningSectie.BouwPlanningId toegevoegd.';
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PlanningSectie_BouwPlanning')
BEGIN
    ALTER TABLE [dbo].[PlanningSectie] ADD CONSTRAINT [FK_PlanningSectie_BouwPlanning]
        FOREIGN KEY ([BouwPlanningId]) REFERENCES [dbo].[BouwPlanning]([Id]);
    PRINT 'FK_PlanningSectie_BouwPlanning toegevoegd.';
END
GO

-- Bestaande fases: elk project met fases krijgt een hoofdplanning waarin ze komen.
INSERT INTO [dbo].[BouwPlanning] ([ProjectId], [Naam], [IsHoofd], [Volgorde])
SELECT DISTINCT s.[ProjectId], N'Hoofdplanning', 1, 0
FROM [dbo].[PlanningSectie] s
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[BouwPlanning] b WHERE b.[ProjectId] = s.[ProjectId] AND b.[IsHoofd] = 1);
GO
UPDATE s SET s.[BouwPlanningId] = b.[Id]
FROM [dbo].[PlanningSectie] s
JOIN [dbo].[BouwPlanning] b ON b.[ProjectId] = s.[ProjectId] AND b.[IsHoofd] = 1
WHERE s.[BouwPlanningId] IS NULL;
GO
PRINT 'Migratie 145 klaar.';
GO
