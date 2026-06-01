-- =============================================
-- Migratie: Project Media Management
-- Datum: 2026-06-01
-- Omschrijving: Voegt ProjectMediaSection tabel toe en
--               breidt ProjectPictures uit met secties,
--               zichtbaarheid, sortering en video-ondersteuning
-- =============================================

-- ---- 1. ProjectMediaSection ----------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProjectMediaSection' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ProjectMediaSection] (
        [Id]          INT            IDENTITY(1,1) NOT NULL,
        [ProjectId]   INT            NOT NULL,
        [Name]        NVARCHAR(100)  NOT NULL,
        [Description] NVARCHAR(200)  NULL,
        [SortOrder]   INT            NOT NULL CONSTRAINT [DF_ProjectMediaSection_SortOrder]  DEFAULT (0),
        [IsPublic]    BIT            NOT NULL CONSTRAINT [DF_ProjectMediaSection_IsPublic]   DEFAULT (1),

        CONSTRAINT [PK_ProjectMediaSection]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [FK_ProjectMediaSection_Project]
            FOREIGN KEY ([ProjectId])
            REFERENCES [dbo].[Project] ([ProjectId])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_ProjectMediaSection_ProjectId]
        ON [dbo].[ProjectMediaSection] ([ProjectId]);

    PRINT 'Tabel ProjectMediaSection aangemaakt.';
END
ELSE
    PRINT 'Tabel ProjectMediaSection bestaat al, overgeslagen.';

-- ---- 2. Kolommen toevoegen aan ProjectPictures ----

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectPictures') AND name = 'SectionId')
BEGIN
    ALTER TABLE [dbo].[ProjectPictures]
        ADD [SectionId] INT NULL
        CONSTRAINT [FK_ProjectPictures_Section]
            FOREIGN KEY REFERENCES [dbo].[ProjectMediaSection]([Id])
            ON DELETE SET NULL;
    PRINT 'Kolom SectionId toegevoegd aan ProjectPictures.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectPictures') AND name = 'IsPublic')
BEGIN
    ALTER TABLE [dbo].[ProjectPictures]
        ADD [IsPublic] BIT NOT NULL CONSTRAINT [DF_ProjectPictures_IsPublic] DEFAULT (1);
    PRINT 'Kolom IsPublic toegevoegd aan ProjectPictures.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectPictures') AND name = 'SortOrder')
BEGIN
    ALTER TABLE [dbo].[ProjectPictures]
        ADD [SortOrder] INT NOT NULL CONSTRAINT [DF_ProjectPictures_SortOrder] DEFAULT (0);
    PRINT 'Kolom SortOrder toegevoegd aan ProjectPictures.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectPictures') AND name = 'MediaType')
BEGIN
    ALTER TABLE [dbo].[ProjectPictures]
        ADD [MediaType] INT NOT NULL CONSTRAINT [DF_ProjectPictures_MediaType] DEFAULT (0);
    -- 0 = Photo, 1 = Video
    PRINT 'Kolom MediaType toegevoegd aan ProjectPictures.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectPictures') AND name = 'FileSizeBytes')
BEGIN
    ALTER TABLE [dbo].[ProjectPictures]
        ADD [FileSizeBytes] BIGINT NULL;
    PRINT 'Kolom FileSizeBytes toegevoegd aan ProjectPictures.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectPictures') AND name = 'WidthPx')
BEGIN
    ALTER TABLE [dbo].[ProjectPictures]
        ADD [WidthPx] INT NULL;
    PRINT 'Kolom WidthPx toegevoegd aan ProjectPictures.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectPictures') AND name = 'HeightPx')
BEGIN
    ALTER TABLE [dbo].[ProjectPictures]
        ADD [HeightPx] INT NULL;
    PRINT 'Kolom HeightPx toegevoegd aan ProjectPictures.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectPictures') AND name = 'DurationSeconds')
BEGIN
    ALTER TABLE [dbo].[ProjectPictures]
        ADD [DurationSeconds] FLOAT NULL;
    PRINT 'Kolom DurationSeconds toegevoegd aan ProjectPictures.';
END

CREATE INDEX [IX_ProjectPictures_SectionId]
    ON [dbo].[ProjectPictures] ([SectionId])
    WHERE [SectionId] IS NOT NULL;

-- ---- 3. Standaard secties aanmaken voor bestaande projecten ----
-- Elk project krijgt 5 secties: Galerij, Exterieur, Interieur, Voortgang, Plannen
-- (Hoofdmedia is geen sectie maar de Type=Hoofdfoto foto)

INSERT INTO [dbo].[ProjectMediaSection] ([ProjectId], [Name], [Description], [SortOrder], [IsPublic])
SELECT p.[ProjectId], 'Galerij', 'Hoofdgalerij op de publieke pagina', 0, 1
FROM [dbo].[Project] p
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[ProjectMediaSection] s
    WHERE s.[ProjectId] = p.[ProjectId] AND s.[Name] = 'Galerij'
);

INSERT INTO [dbo].[ProjectMediaSection] ([ProjectId], [Name], [Description], [SortOrder], [IsPublic])
SELECT p.[ProjectId], 'Exterieur', 'Buitenbeelden — voorgevel, achtergevel, omgeving', 1, 1
FROM [dbo].[Project] p
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[ProjectMediaSection] s
    WHERE s.[ProjectId] = p.[ProjectId] AND s.[Name] = 'Exterieur'
);

INSERT INTO [dbo].[ProjectMediaSection] ([ProjectId], [Name], [Description], [SortOrder], [IsPublic])
SELECT p.[ProjectId], 'Interieur', 'Binnenbeelden per ruimte', 2, 1
FROM [dbo].[Project] p
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[ProjectMediaSection] s
    WHERE s.[ProjectId] = p.[ProjectId] AND s.[Name] = 'Interieur'
);

INSERT INTO [dbo].[ProjectMediaSection] ([ProjectId], [Name], [Description], [SortOrder], [IsPublic])
SELECT p.[ProjectId], 'Voortgang', 'Werfopvolging en voortgangsbeelden', 3, 1
FROM [dbo].[Project] p
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[ProjectMediaSection] s
    WHERE s.[ProjectId] = p.[ProjectId] AND s.[Name] = 'Voortgang'
);

INSERT INTO [dbo].[ProjectMediaSection] ([ProjectId], [Name], [Description], [SortOrder], [IsPublic])
SELECT p.[ProjectId], 'Plannen', 'Grondplannen en technische tekeningen', 4, 0
FROM [dbo].[Project] p
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[ProjectMediaSection] s
    WHERE s.[ProjectId] = p.[ProjectId] AND s.[Name] = 'Plannen'
);

PRINT 'Standaard secties aangemaakt voor bestaande projecten.';
