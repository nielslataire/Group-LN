-- =============================================
-- Migratie: 116_VacationDaysKind
-- Datum: 2026-10-24
-- Omschrijving: Instellingen > Verlofdagen (design-handoff punt 50). Een verlofdag is nu bouwverlof of een feestdag.
--   VacationDays.Kind : 0 = bouwverlof (ook alle bestaande rijen en de verlofdagen van een project), 1 = feestdag.
--   Strikt additief + idempotent. LET OP: draai dit script VOOR je de nieuwe versie van de applicatie opstart, anders mislukken alle
--   plekken die VacationDays lezen (weerverlet, planning, deadlines) met "Invalid column name 'Kind'".
-- =============================================
IF COL_LENGTH('dbo.VacationDays', 'Kind') IS NULL
BEGIN
    ALTER TABLE [dbo].[VacationDays] ADD [Kind] TINYINT NOT NULL CONSTRAINT [DF_VacationDays_Kind] DEFAULT (0);
    PRINT 'Kolom VacationDays.Kind toegevoegd.';
END
ELSE PRINT 'Kolom VacationDays.Kind bestaat al, overgeslagen.';
GO
