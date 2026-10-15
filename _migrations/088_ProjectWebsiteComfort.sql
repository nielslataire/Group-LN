-- =============================================
-- Migratie: 088_ProjectWebsiteComfort
-- Datum: 2026-10-16
-- Omschrijving: Blok "Comfort en energie" op de publieke projectpagina (WWWCOPRO): titel, subtitel en een variabel aantal puntjes
--   (titel + tekst). (De blokken "Documenten" en het lastenboek-blokje vragen geen nieuwe velden: ze lezen de bestaande verkoopdocumenten.)
--   ProjectWebsite.ComfortTitle    : titel van het blok.
--   ProjectWebsite.ComfortSubtitle : subtitel / korte inleiding onder de titel.
--   ProjectWebsiteComfortItem      : puntjes (titel + tekst) met volgorde.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'ComfortTitle')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [ComfortTitle] NVARCHAR(250) NULL;
    PRINT 'Kolom ComfortTitle toegevoegd.';
END
ELSE PRINT 'Kolom ComfortTitle bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'ComfortSubtitle')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [ComfortSubtitle] NVARCHAR(600) NULL;
    PRINT 'Kolom ComfortSubtitle toegevoegd.';
END
ELSE PRINT 'Kolom ComfortSubtitle bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectWebsiteComfortItem', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsiteComfortItem] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [Title]      NVARCHAR(120)  NOT NULL,
        [Text]       NVARCHAR(500)  NULL,
        [SortOrder]  INT            NOT NULL CONSTRAINT DF_ProjectWebsiteComfortItem_Sort DEFAULT 0,
        CONSTRAINT PK_ProjectWebsiteComfortItem PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsiteComfortItem_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsiteComfortItem_Project_Sort] ON [dbo].[ProjectWebsiteComfortItem]([ProjectId], [SortOrder]);
    PRINT 'Tabel ProjectWebsiteComfortItem aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsiteComfortItem bestaat al, overgeslagen.';
GO
