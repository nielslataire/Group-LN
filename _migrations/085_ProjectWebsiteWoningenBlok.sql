-- =============================================
-- Migratie: 085_ProjectWebsiteWoningenBlok
-- Datum: 2026-10-09
-- Omschrijving: Titel en omschrijving van het blok "De woningen" op de publieke projectpagina instelbaar per project.
--   ProjectWebsite.HomesTitle : titel van het blok (leeg = "De woningen").
--   ProjectWebsite.HomesIntro : omschrijving onder de titel (leeg = standaardtekst).
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'HomesTitle')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [HomesTitle] NVARCHAR(200) NULL;
    PRINT 'Kolom HomesTitle toegevoegd aan ProjectWebsite.';
END
ELSE PRINT 'Kolom HomesTitle bestaat al, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'HomesIntro')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [HomesIntro] NVARCHAR(MAX) NULL;
    PRINT 'Kolom HomesIntro toegevoegd aan ProjectWebsite.';
END
ELSE PRINT 'Kolom HomesIntro bestaat al, overgeslagen.';
GO
