-- =============================================
-- Migratie: 148_ProjectSlugHistoriek
-- Omschrijving: Oude slugs van een project, zodat een oude link op de website permanent doorverwijst
--   naar de huidige slug (de slug wordt bij elk opslaan van Projecten/Edit opnieuw gemaakt uit naam + gemeente).
--   * dbo.ProjectSlugHistory: een oude slug hoort bij maximaal een project (unieke index op Slug).
--   Enkel een nieuwe tabel. Additief en idempotent. Draai vóór app-start (CPMCore schrijft erin, WWWCOPRO leest eruit;
--   ontbreekt de tabel, dan werken beide gewoon verder zonder doorverwijzing).
-- =============================================

IF OBJECT_ID(N'[dbo].[ProjectSlugHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectSlugHistory]
    (
        [Id]        INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ProjectSlugHistory] PRIMARY KEY,
        [ProjectId] INT NOT NULL,
        [Slug]      NVARCHAR(200) NOT NULL,
        [ChangedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ProjectSlugHistory_ChangedAt] DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Tabel ProjectSlugHistory aangemaakt.';
END
GO

-- Apart blok: de indexen verwijzen naar kolommen van de tabel hierboven.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProjectSlugHistory]') AND name = N'UX_ProjectSlugHistory_Slug')
BEGIN
    CREATE UNIQUE INDEX [UX_ProjectSlugHistory_Slug] ON [dbo].[ProjectSlugHistory] ([Slug]);
    PRINT 'Index UX_ProjectSlugHistory_Slug aangemaakt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ProjectSlugHistory]') AND name = N'IX_ProjectSlugHistory_Project')
BEGIN
    CREATE INDEX [IX_ProjectSlugHistory_Project] ON [dbo].[ProjectSlugHistory] ([ProjectId]);
    PRINT 'Index IX_ProjectSlugHistory_Project aangemaakt.';
END
GO
