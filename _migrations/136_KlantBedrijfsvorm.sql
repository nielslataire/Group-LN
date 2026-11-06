-- =============================================
-- Migratie: 136_KlantBedrijfsvorm
-- Datum: 2026-11-06
-- Omschrijving: Bedrijfsvorm (BV, NV, VZW, ...) bij klanten die een onderneming zijn, met dezelfde lijst als bij leveranciers (CompanyLegalForm).
--   ClientAccount.CompanyLegalFormId   nullable; leeg = niet ingevuld (alle bestaande klanten). Strikt additief + idempotent.
--   Draai dit script VÓÓR de nieuwe versie van de applicatie opstart: de app leest de kolom bij elke klantenquery.
-- =============================================
IF COL_LENGTH('dbo.ClientAccount', 'CompanyLegalFormId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ClientAccount] ADD [CompanyLegalFormId] INT NULL;
    PRINT 'ClientAccount.CompanyLegalFormId toegevoegd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClientAccount_CompanyLegalForm')
BEGIN
    ALTER TABLE [dbo].[ClientAccount] ADD CONSTRAINT [FK_ClientAccount_CompanyLegalForm] FOREIGN KEY ([CompanyLegalFormId]) REFERENCES [dbo].[CompanyLegalForm] ([Id]);
    PRINT 'FK_ClientAccount_CompanyLegalForm aangemaakt.';
END
GO
