-- =============================================
-- Migratie: 090_ProjectWebsiteLigging
-- Datum: 2026-10-16
-- Omschrijving: Blok "Ligging" op de publieke projectpagina: titel, tekst, gemeente (voor "Wil je weten of ... bij je past?"), het kaartpunt van het
--   project, puntjes (titel + tekst), voorzieningen in de buurt (badges op de kaart) en belangrijke plaatsen met reistijd.
--   ProjectWebsite.LocationTitle / LocationText / LocationMunicipality / LocationLat / LocationLng
--   ProjectWebsiteGuideItem : puntjes (bv. "Dorpsleven") met titel + tekst.
--   ProjectWebsitePoi       : voorzieningen (categorie, naam, afstand, tijd, positie) voor de kaart.
--   ProjectWebsiteTravel    : belangrijke plaatsen met een reistijd (bv. "Aalst" - "12 min").
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'LocationTitle')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [LocationTitle] NVARCHAR(250) NULL;
    PRINT 'Kolom LocationTitle toegevoegd.';
END
ELSE PRINT 'Kolom LocationTitle bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'LocationText')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [LocationText] NVARCHAR(MAX) NULL;
    PRINT 'Kolom LocationText toegevoegd.';
END
ELSE PRINT 'Kolom LocationText bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'LocationMunicipality')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [LocationMunicipality] NVARCHAR(120) NULL;
    PRINT 'Kolom LocationMunicipality toegevoegd.';
END
ELSE PRINT 'Kolom LocationMunicipality bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'LocationLat')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [LocationLat] DECIMAL(9,6) NULL;
    PRINT 'Kolom LocationLat toegevoegd.';
END
ELSE PRINT 'Kolom LocationLat bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'LocationLng')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [LocationLng] DECIMAL(9,6) NULL;
    PRINT 'Kolom LocationLng toegevoegd.';
END
ELSE PRINT 'Kolom LocationLng bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectWebsiteGuideItem', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsiteGuideItem] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [Title]      NVARCHAR(120)  NOT NULL,
        [Text]       NVARCHAR(600)  NULL,
        [SortOrder]  INT            NOT NULL CONSTRAINT DF_ProjectWebsiteGuideItem_Sort DEFAULT 0,
        CONSTRAINT PK_ProjectWebsiteGuideItem PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsiteGuideItem_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsiteGuideItem_Project_Sort] ON [dbo].[ProjectWebsiteGuideItem]([ProjectId], [SortOrder]);
    PRINT 'Tabel ProjectWebsiteGuideItem aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsiteGuideItem bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectWebsitePoi', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsitePoi] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [Category]   VARCHAR(12)    NOT NULL,
        [Name]       NVARCHAR(150)  NOT NULL,
        [Lat]        DECIMAL(9,6)   NOT NULL,
        [Lng]        DECIMAL(9,6)   NOT NULL,
        [Distance]   NVARCHAR(30)   NULL,
        [TimeText]   NVARCHAR(40)   NULL,
        [SortOrder]  INT            NOT NULL CONSTRAINT DF_ProjectWebsitePoi_Sort DEFAULT 0,
        CONSTRAINT PK_ProjectWebsitePoi PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsitePoi_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsitePoi_Project_Sort] ON [dbo].[ProjectWebsitePoi]([ProjectId], [SortOrder]);
    PRINT 'Tabel ProjectWebsitePoi aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsitePoi bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectWebsiteTravel', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsiteTravel] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [Place]      NVARCHAR(80)   NOT NULL,
        [Duration]   NVARCHAR(40)   NOT NULL,
        [SortOrder]  INT            NOT NULL CONSTRAINT DF_ProjectWebsiteTravel_Sort DEFAULT 0,
        CONSTRAINT PK_ProjectWebsiteTravel PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsiteTravel_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsiteTravel_Project_Sort] ON [dbo].[ProjectWebsiteTravel]([ProjectId], [SortOrder]);
    PRINT 'Tabel ProjectWebsiteTravel aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsiteTravel bestaat al, overgeslagen.';
GO
