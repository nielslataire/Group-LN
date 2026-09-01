-- =============================================
-- Migratie: VacatureVideo
-- Datum: 2026-09-01
-- Omschrijving: Voegt aan Vacature een optionele video toe (URL naar het
--               bestand in de Storage API) plus een posterafbeelding die
--               als eerste beeld dient vóór het afspelen.
-- =============================================

IF COL_LENGTH('dbo.Vacature', 'VideoBestand') IS NULL
BEGIN
    ALTER TABLE [dbo].[Vacature] ADD [VideoBestand] NVARCHAR(500) NULL;
    PRINT 'Kolom Vacature.VideoBestand toegevoegd.';
END
ELSE
    PRINT 'Kolom Vacature.VideoBestand bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.Vacature', 'VideoPosterBestand') IS NULL
BEGIN
    ALTER TABLE [dbo].[Vacature] ADD [VideoPosterBestand] NVARCHAR(500) NULL;
    PRINT 'Kolom Vacature.VideoPosterBestand toegevoegd.';
END
ELSE
    PRINT 'Kolom Vacature.VideoPosterBestand bestaat al, overgeslagen.';
