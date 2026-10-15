-- =============================================
-- Migratie: 087_ProjectPicturesVarianten
-- Datum: 2026-10-15
-- Omschrijving: Foto's die via de nieuwe upload (CPMCore -> Storage /api/assets/photo) zijn opgeladen hebben WebP-varianten op breedte
--   (pictures/w320 ... pictures/w2560) en een master-JPEG (pictures/original). Oudere foto's hebben die niet; de publieke site
--   gebruikt srcset enkel voor foto's met HasVariants = 1 en valt anders terug op de bestaande map "pictures".
--   ProjectPictures.HasVariants : 1 = varianten op breedte bestaan voor deze foto. Bestaande rijen blijven 0.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectPictures') AND name = 'HasVariants')
BEGIN
    ALTER TABLE [dbo].[ProjectPictures] ADD [HasVariants] BIT NOT NULL CONSTRAINT DF_ProjectPictures_HasVariants DEFAULT 0;
    PRINT 'Kolom HasVariants toegevoegd aan ProjectPictures.';
END
ELSE PRINT 'Kolom HasVariants bestaat al, overgeslagen.';
GO
