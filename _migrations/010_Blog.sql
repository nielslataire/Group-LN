-- =============================================
-- Migratie: Blog Articles
-- Datum: 2026-06-11
-- Omschrijving: Voegt BlogArtikel en BlogArtikelBlok tabellen toe
--               voor de publieke blog/nieuws pagina van de website.
--               Elk artikel heeft een foto, titel, preview-tekst en datum.
--               De inhoud bestaat uit blokken met titel, rich-text en optionele foto.
-- =============================================

-- ---- 1. BlogArtikel ----------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BlogArtikel' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[BlogArtikel] (
        [Id]           INT            IDENTITY(1,1) NOT NULL,
        [Titel]        NVARCHAR(250)  NOT NULL,
        [Slug]         NVARCHAR(300)  NOT NULL,
        [PreviewTekst] NVARCHAR(500)  NULL,
        [FotoBestand]  NVARCHAR(500)  NULL,
        [Datum]        DATETIME2      NOT NULL CONSTRAINT [DF_BlogArtikel_Datum] DEFAULT (GETDATE()),
        [IsGepubliceerd] BIT          NOT NULL CONSTRAINT [DF_BlogArtikel_IsGepubliceerd] DEFAULT (0),
        [SortOrder]    INT            NOT NULL CONSTRAINT [DF_BlogArtikel_SortOrder] DEFAULT (0),
        [AangemaaktOp] DATETIME2      NOT NULL CONSTRAINT [DF_BlogArtikel_AangemaaktOp] DEFAULT (GETDATE()),
        [GewijzigdOp]  DATETIME2      NOT NULL CONSTRAINT [DF_BlogArtikel_GewijzigdOp]  DEFAULT (GETDATE()),

        CONSTRAINT [PK_BlogArtikel]
            PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE UNIQUE INDEX [UX_BlogArtikel_Slug]
        ON [dbo].[BlogArtikel] ([Slug]);

    PRINT 'Tabel BlogArtikel aangemaakt.';
END
ELSE
    PRINT 'Tabel BlogArtikel bestaat al, overgeslagen.';

-- ---- 2. BlogArtikelBlok ----------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BlogArtikelBlok' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[BlogArtikelBlok] (
        [Id]           INT            IDENTITY(1,1) NOT NULL,
        [ArtikelId]    INT            NOT NULL,
        [SortOrder]    INT            NOT NULL CONSTRAINT [DF_BlogArtikelBlok_SortOrder] DEFAULT (0),
        [Titel]        NVARCHAR(250)  NULL,
        [RijkeTekst]   NVARCHAR(MAX)  NULL,
        [FotoBestand]  NVARCHAR(500)  NULL,

        CONSTRAINT [PK_BlogArtikelBlok]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [FK_BlogArtikelBlok_BlogArtikel]
            FOREIGN KEY ([ArtikelId])
            REFERENCES [dbo].[BlogArtikel] ([Id])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_BlogArtikelBlok_ArtikelId]
        ON [dbo].[BlogArtikelBlok] ([ArtikelId]);

    PRINT 'Tabel BlogArtikelBlok aangemaakt.';
END
ELSE
    PRINT 'Tabel BlogArtikelBlok bestaat al, overgeslagen.';

-- ---- 3. DetailTitel en DetailTitelTekst toevoegen aan BlogArtikel ----
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BlogArtikel') AND name = 'DetailTitel')
BEGIN
    ALTER TABLE [dbo].[BlogArtikel]
        ADD [DetailTitel] NVARCHAR(250) NULL;
    PRINT 'Kolom DetailTitel toegevoegd aan BlogArtikel.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BlogArtikel') AND name = 'DetailTitelTekst')
BEGIN
    ALTER TABLE [dbo].[BlogArtikel]
        ADD [DetailTitelTekst] NVARCHAR(1000) NULL;
    PRINT 'Kolom DetailTitelTekst toegevoegd aan BlogArtikel.';
END
