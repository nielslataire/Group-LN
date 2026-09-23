-- =============================================
-- Migratie: 045_ProjectPicturesDetailVelden
-- Datum: 2026-09-23
-- Omschrijving: Nieuwe kolommen op ProjectPictures voor het gl-v2 mediadetailpaneel
--   (design-handoff punt 15b "Detailpaneel — dezelfde opbouw voor foto en video"):
--     - AltText                  NVARCHAR(300) NULL  — alt-tekst, enkel getoond/bewerkt bij foto's
--     - Subtitle                 NVARCHAR(500) NULL  — ondertitel/beschrijving, enkel bij video's
--     - UnitId                   INT           NULL  — koppeling naar Units (FK, ON DELETE SET NULL,
--                                                       zelfde soort optionele koppeling als SectionId)
--     - AutoPlayMuted            BIT NOT NULL DEFAULT (1) — "automatisch afspelen, zonder geluid",
--                                                       enkel relevant/getoond bij video's
--     - PosterTimestampSeconds   FLOAT         NULL  — tijdstip (seconden) waarop "Posterbeeld =
--                                                       huidig frame" werd vastgelegd, enkel video's
--   "Titel" uit 15b is GEEN nieuwe kolom — dat is het bestaande ProjectPictures.Caption veld.
--   Strikt additief + idempotent, zelfde COL_LENGTH/sys.columns-conventie als elke migratie hier.
-- =============================================

IF COL_LENGTH('dbo.ProjectPictures', 'AltText') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectPictures] ADD [AltText] NVARCHAR(300) NULL;
    PRINT 'Kolom ProjectPictures.AltText toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectPictures.AltText bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.ProjectPictures', 'Subtitle') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectPictures] ADD [Subtitle] NVARCHAR(500) NULL;
    PRINT 'Kolom ProjectPictures.Subtitle toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectPictures.Subtitle bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.ProjectPictures', 'AutoPlayMuted') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectPictures] ADD [AutoPlayMuted] BIT NOT NULL CONSTRAINT DF_ProjectPictures_AutoPlayMuted DEFAULT (1);
    PRINT 'Kolom ProjectPictures.AutoPlayMuted toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectPictures.AutoPlayMuted bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.ProjectPictures', 'PosterTimestampSeconds') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectPictures] ADD [PosterTimestampSeconds] FLOAT NULL;
    PRINT 'Kolom ProjectPictures.PosterTimestampSeconds toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectPictures.PosterTimestampSeconds bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.ProjectPictures', 'UnitId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectPictures]
        ADD [UnitId] INT NULL
        CONSTRAINT [FK_ProjectPictures_Unit]
            FOREIGN KEY REFERENCES [dbo].[Units]([Id])
            ON DELETE SET NULL;
    PRINT 'Kolom ProjectPictures.UnitId toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectPictures.UnitId bestaat al, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProjectPictures_UnitId' AND object_id = OBJECT_ID('dbo.ProjectPictures'))
BEGIN
    CREATE INDEX [IX_ProjectPictures_UnitId]
        ON [dbo].[ProjectPictures] ([UnitId])
        WHERE [UnitId] IS NOT NULL;
    PRINT 'Index IX_ProjectPictures_UnitId aangemaakt.';
END
ELSE
    PRINT 'Index IX_ProjectPictures_UnitId bestaat al, overgeslagen.';

PRINT 'Migratie 045_ProjectPicturesDetailVelden voltooid.';
