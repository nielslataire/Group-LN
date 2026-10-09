-- =============================================
-- Migratie: 086_ProjectWebsiteBlokken
-- Datum: 2026-10-09
-- Omschrijving: Blokken van de publieke projectpagina beheerbaar per project: volgorde + zichtbaarheid, blok "Verhaal"
--   (titel, tekst, beelden met tekst, brochure-blok aan/uit) en blok "Architectuur" (boventitel, titel, tekst,
--   quotes, details met foto).
--   ProjectWebsite : BlocksJson (volgorde + zichtbaarheid), ShowBrochure, StoryTitle, StoryText,
--                    ArchEyebrow, ArchTitle, ArchText.
--   ProjectWebsiteStoryItem : beeld + tekst, variabel aantal, met volgorde.
--   ProjectWebsiteQuote     : quote (tekst + persoon), variabel aantal, met volgorde.
--   ProjectWebsiteDetail    : detail (foto + titel + tekst), variabel aantal, met volgorde.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'BlocksJson')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [BlocksJson] NVARCHAR(MAX) NULL;
    PRINT 'Kolom BlocksJson toegevoegd.';
END
ELSE PRINT 'Kolom BlocksJson bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'ShowBrochure')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [ShowBrochure] BIT NOT NULL CONSTRAINT DF_ProjectWebsite_ShowBrochure DEFAULT 0;
    PRINT 'Kolom ShowBrochure toegevoegd.';
END
ELSE PRINT 'Kolom ShowBrochure bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'StoryTitle')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [StoryTitle] NVARCHAR(250) NULL;
    PRINT 'Kolom StoryTitle toegevoegd.';
END
ELSE PRINT 'Kolom StoryTitle bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'StoryText')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [StoryText] NVARCHAR(MAX) NULL;
    PRINT 'Kolom StoryText toegevoegd.';
END
ELSE PRINT 'Kolom StoryText bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'ArchEyebrow')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [ArchEyebrow] NVARCHAR(200) NULL;
    PRINT 'Kolom ArchEyebrow toegevoegd.';
END
ELSE PRINT 'Kolom ArchEyebrow bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'ArchTitle')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [ArchTitle] NVARCHAR(250) NULL;
    PRINT 'Kolom ArchTitle toegevoegd.';
END
ELSE PRINT 'Kolom ArchTitle bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'ArchText')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [ArchText] NVARCHAR(MAX) NULL;
    PRINT 'Kolom ArchText toegevoegd.';
END
ELSE PRINT 'Kolom ArchText bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectWebsiteStoryItem', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsiteStoryItem] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [ImageName]  NVARCHAR(260)  NULL,
        [Text]       NVARCHAR(1000) NULL,
        [SortOrder]  INT            NOT NULL CONSTRAINT DF_ProjectWebsiteStoryItem_Sort DEFAULT 0,
        CONSTRAINT PK_ProjectWebsiteStoryItem PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsiteStoryItem_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsiteStoryItem_Project_Sort] ON [dbo].[ProjectWebsiteStoryItem]([ProjectId], [SortOrder]);
    PRINT 'Tabel ProjectWebsiteStoryItem aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsiteStoryItem bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectWebsiteQuote', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsiteQuote] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [Text]       NVARCHAR(600)  NOT NULL,
        [Person]     NVARCHAR(200)  NULL,
        [SortOrder]  INT            NOT NULL CONSTRAINT DF_ProjectWebsiteQuote_Sort DEFAULT 0,
        CONSTRAINT PK_ProjectWebsiteQuote PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsiteQuote_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsiteQuote_Project_Sort] ON [dbo].[ProjectWebsiteQuote]([ProjectId], [SortOrder]);
    PRINT 'Tabel ProjectWebsiteQuote aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsiteQuote bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectWebsiteDetail', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsiteDetail] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [ImageName]  NVARCHAR(260)  NULL,
        [Title]      NVARCHAR(200)  NOT NULL,
        [Text]       NVARCHAR(1000) NULL,
        [SortOrder]  INT            NOT NULL CONSTRAINT DF_ProjectWebsiteDetail_Sort DEFAULT 0,
        CONSTRAINT PK_ProjectWebsiteDetail PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsiteDetail_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsiteDetail_Project_Sort] ON [dbo].[ProjectWebsiteDetail]([ProjectId], [SortOrder]);
    PRINT 'Tabel ProjectWebsiteDetail aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsiteDetail bestaat al, overgeslagen.';
GO
