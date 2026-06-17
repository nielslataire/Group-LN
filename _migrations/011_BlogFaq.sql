-- =============================================
-- Migratie: BlogArtikelFaq
-- Datum: 2026-06-17
-- Omschrijving: Voegt BlogArtikelFaq tabel toe voor FAQ-blokken per blogartikel.
--               Elk FAQ-item heeft een vraag en een antwoord (geen foto, geen type).
--               Voegt tevens BlokType kolom toe aan BlogArtikelBlok indien nog ontbrekend.
-- =============================================

-- ---- 1. BlokType kolom op BlogArtikelBlok (indien ontbrekend) ----
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BlogArtikelBlok') AND name = 'BlokType')
BEGIN
    ALTER TABLE [dbo].[BlogArtikelBlok]
        ADD [BlokType] NVARCHAR(50) NOT NULL CONSTRAINT [DF_BlogArtikelBlok_BlokType] DEFAULT ('tekst');
    PRINT 'Kolom BlokType toegevoegd aan BlogArtikelBlok.';
END
ELSE
    PRINT 'Kolom BlokType bestaat al op BlogArtikelBlok, overgeslagen.';

-- ---- 2. BlogArtikelFaq ----
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BlogArtikelFaq' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[BlogArtikelFaq] (
        [Id]        INT            IDENTITY(1,1) NOT NULL,
        [ArtikelId] INT            NOT NULL,
        [SortOrder] INT            NOT NULL CONSTRAINT [DF_BlogArtikelFaq_SortOrder] DEFAULT (0),
        [Vraag]     NVARCHAR(500)  NOT NULL,
        [Antwoord]  NVARCHAR(MAX)  NULL,

        CONSTRAINT [PK_BlogArtikelFaq]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [FK_BlogArtikelFaq_BlogArtikel]
            FOREIGN KEY ([ArtikelId])
            REFERENCES [dbo].[BlogArtikel] ([Id])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_BlogArtikelFaq_ArtikelId]
        ON [dbo].[BlogArtikelFaq] ([ArtikelId]);

    PRINT 'Tabel BlogArtikelFaq aangemaakt.';
END
ELSE
    PRINT 'Tabel BlogArtikelFaq bestaat al, overgeslagen.';
