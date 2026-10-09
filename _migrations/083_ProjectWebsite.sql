-- =============================================
-- Migratie: 083_ProjectWebsite
-- Datum: 2026-10-09
-- Omschrijving: Website-inhoud per project voor de publieke projectpagina (WWWCOPRO, ontwerp "Project Detail v4 Split").
--   Eigen tabellen, los van Project, zodat de brede Project-tabel niet groeit en de publieke site
--   met één kleine opzoeking per project (PK-lookup + index op ProjectId) alles ophaalt.
--   ProjectWebsite     : 1 rij per project - locatie (eyebrow), titel, subtitel, inleidende tekst.
--   ProjectWebsiteKpi  : n rijen per project - eigen kerncijfers (titel + tekst) met volgorde.
--   Strikt additief + idempotent. Geen data-migratie: bestaande projecten blijven ongewijzigd
--   (geen rij = de website valt terug op de bestaande commerciële titel/tekst).
-- =============================================
IF OBJECT_ID('dbo.ProjectWebsite', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsite] (
        [ProjectId]   INT            NOT NULL,
        [Location]    NVARCHAR(200)  NULL,
        [Title]       NVARCHAR(200)  NULL,
        [Subtitle]    NVARCHAR(250)  NULL,
        [IntroText]   NVARCHAR(MAX)  NULL,
        [UpdatedOn]   DATETIME2(0)   NOT NULL CONSTRAINT DF_ProjectWebsite_UpdatedOn DEFAULT SYSDATETIME(),
        CONSTRAINT PK_ProjectWebsite PRIMARY KEY CLUSTERED ([ProjectId]),
        CONSTRAINT FK_ProjectWebsite_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    PRINT 'Tabel ProjectWebsite aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsite bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectWebsiteKpi', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsiteKpi] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [Title]      NVARCHAR(80)   NOT NULL,
        [Text]       NVARCHAR(200)  NOT NULL,
        [SortOrder]  INT            NOT NULL CONSTRAINT DF_ProjectWebsiteKpi_SortOrder DEFAULT 0,
        CONSTRAINT PK_ProjectWebsiteKpi PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsiteKpi_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    PRINT 'Tabel ProjectWebsiteKpi aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsiteKpi bestaat al, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProjectWebsiteKpi_Project_Sort' AND object_id = OBJECT_ID('dbo.ProjectWebsiteKpi'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsiteKpi_Project_Sort] ON [dbo].[ProjectWebsiteKpi]([ProjectId], [SortOrder]) INCLUDE ([Title], [Text]);
    PRINT 'Index IX_ProjectWebsiteKpi_Project_Sort aangemaakt.';
END
ELSE PRINT 'Index IX_ProjectWebsiteKpi_Project_Sort bestaat al, overgeslagen.';
GO
