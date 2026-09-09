-- =============================================
-- Migratie: Insurances.Polisnummer
-- Datum: 2026-09-09
-- Omschrijving:
--   Polisnummer van een verzekering, ingevuld op de "Verzekering toevoegen/
--   bewerken"-modal op Projecten/DetailInsurances en getoond in de kolom
--   "Polisnummer" van de verzekeringentabel.
-- =============================================

IF COL_LENGTH('dbo.Insurances', 'Polisnummer') IS NULL
BEGIN
    ALTER TABLE [dbo].[Insurances] ADD [Polisnummer] NVARCHAR(100) NULL;
    PRINT 'Kolom Insurances.Polisnummer toegevoegd.';
END
ELSE
    PRINT 'Kolom Insurances.Polisnummer bestaat al, overgeslagen.';
