-- =============================================
-- Migratie: 125_InschrijvingWebsite
-- Datum: 2026-10-29
-- Omschrijving: Website-status "Inschrijving" (SalesDisplayStatus = 4) met een eigen titel en tekst voor de inschrijvingspagina.
--   Twee nieuwe kolommen op ProjectSalesSettings: InschrijvingTitle en InschrijvingText.
--   Zolang de status niet op "Inschrijving" staat, blijven titel en tekst onzichtbaar op de site en toont het project de gewone detailpagina.
--   Strikt additief + idempotent. Draai dit script VOOR je de nieuwe versie van CPMCore of WWWCOPRO opstart,
--   anders mislukken de verkoopinstellingen en de projectpagina's met "Invalid column name 'InschrijvingTitle'".
-- =============================================
IF COL_LENGTH('dbo.ProjectSalesSettings', 'InschrijvingTitle') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectSalesSettings] ADD [InschrijvingTitle] NVARCHAR(200) NULL;
    PRINT 'Kolom ProjectSalesSettings.InschrijvingTitle toegevoegd.';
END
ELSE PRINT 'Kolom ProjectSalesSettings.InschrijvingTitle bestaat al, overgeslagen.';
GO

IF COL_LENGTH('dbo.ProjectSalesSettings', 'InschrijvingText') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectSalesSettings] ADD [InschrijvingText] NVARCHAR(MAX) NULL;
    PRINT 'Kolom ProjectSalesSettings.InschrijvingText toegevoegd.';
END
ELSE PRINT 'Kolom ProjectSalesSettings.InschrijvingText bestaat al, overgeslagen.';
GO
