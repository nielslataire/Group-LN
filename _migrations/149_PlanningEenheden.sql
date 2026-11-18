-- =============================================
-- Migratie: 149_PlanningEenheden
-- Omschrijving: Een planning hoort bij een of meer eenheden (woningen/gebouw) in plaats van bij een betalingsgroep.
--   * Nieuwe tabel BouwPlanningEenheid (BouwPlanningId, UnitId); een eenheid zit in hoogstens één planning (unieke index op UnitId).
--   * Bestaande plannings die aan een betalingsgroep hingen krijgen alle eenheden van die groep als eenheden.
--   * BouwPlanning.PaymentGroupId blijft bestaan maar wordt niet meer gebruikt. Niets wordt verwijderd.
--   Additief en idempotent. Draai vóór app-start.
-- =============================================

IF OBJECT_ID(N'[dbo].[BouwPlanningEenheid]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BouwPlanningEenheid] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_BouwPlanningEenheid] PRIMARY KEY,
        [BouwPlanningId] INT NOT NULL,
        [UnitId] INT NOT NULL,
        CONSTRAINT [FK_BouwPlanningEenheid_BouwPlanning] FOREIGN KEY ([BouwPlanningId]) REFERENCES [dbo].[BouwPlanning]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BouwPlanningEenheid_Unit] FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id])
    );
    PRINT 'Tabel BouwPlanningEenheid aangemaakt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BouwPlanningEenheid_Unit' AND object_id = OBJECT_ID(N'[dbo].[BouwPlanningEenheid]'))
    CREATE UNIQUE INDEX [UX_BouwPlanningEenheid_Unit] ON [dbo].[BouwPlanningEenheid]([UnitId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BouwPlanningEenheid_Planning' AND object_id = OBJECT_ID(N'[dbo].[BouwPlanningEenheid]'))
    CREATE INDEX [IX_BouwPlanningEenheid_Planning] ON [dbo].[BouwPlanningEenheid]([BouwPlanningId]);
GO

-- Bestaande plannings van een betalingsgroep: neem de eenheden van die groep over.
INSERT INTO [dbo].[BouwPlanningEenheid] ([BouwPlanningId], [UnitId])
SELECT bp.[Id], u.[Id]
FROM [dbo].[BouwPlanning] bp
JOIN [dbo].[Units] u ON u.[PaymentGroupId] = bp.[PaymentGroupId]
WHERE bp.[PaymentGroupId] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[BouwPlanningEenheid] e WHERE e.[UnitId] = u.[Id]);
GO
