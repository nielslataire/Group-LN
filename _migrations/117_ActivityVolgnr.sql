-- =============================================
-- Migratie: 117_ActivityVolgnr
-- Datum: 2026-10-24
-- Omschrijving: Instellingen > Activiteiten (design-handoff punt 51a): elke activiteit heeft een eigen volgnummer binnen haar
--   activiteitgroep; samen met het lot van de groep is dat het lotnummer, bv. lot 5 + volgnr 11 = "5.11".
--   Activity.Volgnr : INT NULL. Bestaande activiteiten blijven zonder nummer: je vult ze zelf aan op de pagina Activiteiten (tot dan staat er bv. "5.–").
--   Strikt additief + idempotent. LET OP: draai dit script VOOR je de nieuwe versie van de applicatie opstart, anders mislukt alles dat Activity
--   leest (budget, contracten, facturen) met "Invalid column name 'Volgnr'".
-- =============================================
IF COL_LENGTH('dbo.Activity', 'Volgnr') IS NULL
BEGIN
    ALTER TABLE [dbo].[Activity] ADD [Volgnr] INT NULL;
    PRINT 'Kolom Activity.Volgnr toegevoegd.';
END
ELSE PRINT 'Kolom Activity.Volgnr bestaat al, overgeslagen.';
GO
