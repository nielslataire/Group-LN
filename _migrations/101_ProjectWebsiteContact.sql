-- =============================================
-- Migratie: 101_ProjectWebsiteContact
-- Omschrijving: Eigen titel en tekst voor het contactformulier op de publieke projectpagina (v4).
--   ProjectWebsite.ContactTitle / ContactText. Leeg = standaardtitel/-tekst. Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'ContactTitle')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [ContactTitle] NVARCHAR(250) NULL;
    PRINT 'Kolom ContactTitle toegevoegd.';
END
ELSE PRINT 'Kolom ContactTitle bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'ContactText')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [ContactText] NVARCHAR(600) NULL;
    PRINT 'Kolom ContactText toegevoegd.';
END
ELSE PRINT 'Kolom ContactText bestaat al, overgeslagen.';
GO
