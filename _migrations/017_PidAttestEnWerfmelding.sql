-- =============================================
-- Migratie: PidAttestEnWerfmelding
-- Datum: 2026-09-02
-- Omschrijving:
--   - Contract.PidAttest        : attesten voor het postinterventiedossier ontvangen (ja/nee)
--   - Project.WerfmeldingDate   : datum waarop de werfmelding werd ingediend
--   - Project.WerfmeldingDossier: dossiernummer van de werfmelding
-- =============================================

IF COL_LENGTH('dbo.Contract', 'PidAttest') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [PidAttest] BIT NOT NULL
        CONSTRAINT [DF_Contract_PidAttest] DEFAULT (0);
    PRINT 'Kolom Contract.PidAttest toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.PidAttest bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.Project', 'WerfmeldingDate') IS NULL
BEGIN
    ALTER TABLE [dbo].[Project] ADD [WerfmeldingDate] DATE NULL;
    PRINT 'Kolom Project.WerfmeldingDate toegevoegd.';
END
ELSE
    PRINT 'Kolom Project.WerfmeldingDate bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.Project', 'WerfmeldingDossier') IS NULL
BEGIN
    ALTER TABLE [dbo].[Project] ADD [WerfmeldingDossier] NVARCHAR(100) NULL;
    PRINT 'Kolom Project.WerfmeldingDossier toegevoegd.';
END
ELSE
    PRINT 'Kolom Project.WerfmeldingDossier bestaat al, overgeslagen.';
