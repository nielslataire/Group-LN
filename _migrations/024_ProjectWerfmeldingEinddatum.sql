-- =============================================
-- Migratie: ProjectWerfmeldingEinddatum
-- Datum: 2026-09-08
-- Omschrijving:
--   Project.WerfmeldingEndDate : einddatum (geldigheid) van de werfmelding.
--   Wordt op Projecten/Edit (tab "Algemeen") ingevuld naast de bestaande
--   WerfmeldingDate/WerfmeldingDossier en voedt de "Aandacht vereist"-kaart
--   op Projecten/Detail: een project in uitvoering zonder werfmelding of met
--   een verlopen einddatum levert een dringende actie op.
-- =============================================

IF COL_LENGTH('dbo.Project', 'WerfmeldingEndDate') IS NULL
BEGIN
    ALTER TABLE [dbo].[Project] ADD [WerfmeldingEndDate] DATE NULL;
    PRINT 'Kolom Project.WerfmeldingEndDate toegevoegd.';
END
ELSE
    PRINT 'Kolom Project.WerfmeldingEndDate bestaat al, overgeslagen.';
