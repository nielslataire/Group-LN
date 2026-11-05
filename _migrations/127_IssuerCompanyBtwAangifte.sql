-- =============================================
-- Migratie: 127_IssuerCompanyBtwAangifte
-- Datum: 2026-11-05
-- Omschrijving: Per facturatiebedrijf instellen hoe vaak de btw-aangifte ingediend wordt, voor de kaart/KPI "Btw-aangifte"
--   op het dashboard Boekhouding. Nieuwe kolom IssuerCompany.VatReturnFrequency: 'Monthly' (maandelijks), 'Quarterly' (per kwartaal)
--   of NULL (nog niet ingesteld: het dashboard toont dan "Instellen" i.p.v. een datum).
--   Strikt additief + idempotent. Draai dit script VOOR je de nieuwe versie van CPMCore opstart,
--   anders mislukken de facturatiebedrijf-schermen en het dashboard met "Invalid column name 'VatReturnFrequency'".
-- =============================================
IF COL_LENGTH('dbo.IssuerCompany', 'VatReturnFrequency') IS NULL
BEGIN
    ALTER TABLE [dbo].[IssuerCompany] ADD [VatReturnFrequency] NVARCHAR(10) NULL;
    PRINT 'Kolom IssuerCompany.VatReturnFrequency toegevoegd.';
END
ELSE PRINT 'Kolom IssuerCompany.VatReturnFrequency bestaat al, overgeslagen.';
GO
