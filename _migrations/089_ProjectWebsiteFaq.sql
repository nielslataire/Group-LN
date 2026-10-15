-- =============================================
-- Migratie: 089_ProjectWebsiteFaq (vragen + aankoopstappen)
-- Datum: 2026-10-16
-- Omschrijving: Blok "Vragen" (FAQ) op de publieke projectpagina: een variabel aantal vragen met een antwoord (opgemaakte tekst, ook links),
--   met volgorde. De antwoorden worden bij het opslaan in CPMCore opgeschoond (enkel eenvoudige opmaak en links).
--   ProjectWebsiteFaq : vraag (titel) + antwoord (HTML) per project.
--   Blok "Zo verloopt de aankoop" aanpasbaar per project:
--   ProjectWebsite.BuyTitle  : titel van het blok (leeg = standaardtitel).
--   ProjectWebsiteBuyStep    : stappen (titel + een zin) met volgorde. Geen rijen = de standaardstappen.
--   Strikt additief + idempotent.
-- =============================================
IF OBJECT_ID('dbo.ProjectWebsiteFaq', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsiteFaq] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [Question]   NVARCHAR(300)  NOT NULL,
        [Answer]     NVARCHAR(MAX)  NOT NULL,
        [SortOrder]  INT            NOT NULL CONSTRAINT DF_ProjectWebsiteFaq_Sort DEFAULT 0,
        CONSTRAINT PK_ProjectWebsiteFaq PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsiteFaq_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsiteFaq_Project_Sort] ON [dbo].[ProjectWebsiteFaq]([ProjectId], [SortOrder]);
    PRINT 'Tabel ProjectWebsiteFaq aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsiteFaq bestaat al, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'BuyTitle')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [BuyTitle] NVARCHAR(250) NULL;
    PRINT 'Kolom BuyTitle toegevoegd.';
END
ELSE PRINT 'Kolom BuyTitle bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectWebsiteBuyStep', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsiteBuyStep] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [Title]      NVARCHAR(120)  NOT NULL,
        [Text]       NVARCHAR(400)  NULL,
        [SortOrder]  INT            NOT NULL CONSTRAINT DF_ProjectWebsiteBuyStep_Sort DEFAULT 0,
        CONSTRAINT PK_ProjectWebsiteBuyStep PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsiteBuyStep_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsiteBuyStep_Project_Sort] ON [dbo].[ProjectWebsiteBuyStep]([ProjectId], [SortOrder]);
    PRINT 'Tabel ProjectWebsiteBuyStep aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsiteBuyStep bestaat al, overgeslagen.';
GO
