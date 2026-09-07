-- =============================================
-- Migratie: ProjectVoortgangBedragen
-- Datum: 2026-09-07
-- Omschrijving:
--   De voortgangberekening (ProjectVoortgangService) rekent al met de
--   project-totalen begroot/gecontracteerd/gefactureerd, maar bewaarde enkel
--   de afgeleide percentages. Deze 3 kolommen bewaren ook de onderliggende
--   bedragen zodat de "Voortgang & budget"-kaart op Projecten/Detail ze kan
--   tonen zonder de nacalculatie opnieuw te moeten opbouwen.
-- =============================================

IF COL_LENGTH('dbo.ProjectVoortgang', 'TotaalBegroot') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectVoortgang] ADD [TotaalBegroot] DECIMAL(18, 2) NULL;
    PRINT 'Kolom ProjectVoortgang.TotaalBegroot toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectVoortgang.TotaalBegroot bestaat al, overgeslagen.';
GO

IF COL_LENGTH('dbo.ProjectVoortgang', 'TotaalGecontracteerd') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectVoortgang] ADD [TotaalGecontracteerd] DECIMAL(18, 2) NULL;
    PRINT 'Kolom ProjectVoortgang.TotaalGecontracteerd toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectVoortgang.TotaalGecontracteerd bestaat al, overgeslagen.';
GO

IF COL_LENGTH('dbo.ProjectVoortgang', 'TotaalGefactureerd') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectVoortgang] ADD [TotaalGefactureerd] DECIMAL(18, 2) NULL;
    PRINT 'Kolom ProjectVoortgang.TotaalGefactureerd toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectVoortgang.TotaalGefactureerd bestaat al, overgeslagen.';
GO
