-- =============================================
-- Migratie: 050_ProjectRegieUurTariefVastleggen
-- Datum: 2026-09-25
-- Omschrijving: Regie-uren van een coördinatieproject (Projecten/DetailCoordinatie):
--   Het uurtarief per medewerker staat op ProjectHourlyRate (per project) en werd tot nu overal live
--   opgezocht — ook voor prestaties die al gefactureerd zijn. Wie het tarief van een medewerker
--   wijzigde, wijzigde dus ook stil de bedragen van reeds gefactureerde prestaties op het scherm en
--   in de prestatielijst. Omdat het tarief nu "ten allen tijde" mag wijzigen voor wat nog niet
--   gefactureerd is, wordt het tarief op het moment van factureren vastgelegd:
--   1. ProjectRegieUur.HourlyRateInvoiced DECIMAL(9,2) NULL — het tarief waarmee de prestatie
--      gefactureerd werd. NULL = nog niet gefactureerd (dan geldt het actuele projecttarief).
--   2. Backfill: reeds gefactureerde prestaties (InvoiceId IS NOT NULL) krijgen het huidige
--      projecttarief van hun medewerker. Dat is exact wat het scherm tot nu toonde, dus er verandert
--      niets zichtbaars; wie een afwijkend historisch tarief weet, past de kolom achteraf aan.
--   Strikt additief + idempotent.
-- =============================================

IF COL_LENGTH('dbo.ProjectRegieUur', 'HourlyRateInvoiced') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectRegieUur] ADD [HourlyRateInvoiced] DECIMAL(9,2) NULL;
    PRINT 'Kolom ProjectRegieUur.HourlyRateInvoiced toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectRegieUur.HourlyRateInvoiced bestaat al, overgeslagen.';
GO

-- Backfill (enkel waar nog leeg, dus veilig om opnieuw te draaien)
UPDATE r
SET r.HourlyRateInvoiced = h.HourlyRate
FROM [dbo].[ProjectRegieUur] r
JOIN [dbo].[ProjectHourlyRate] h ON h.ProjectId = r.ProjectId AND h.UserId = r.UserId
WHERE r.InvoiceId IS NOT NULL
  AND r.HourlyRateInvoiced IS NULL;
PRINT 'Tarief van reeds gefactureerde regie-uren vastgelegd (aantal rijen: ' + CAST(@@ROWCOUNT AS VARCHAR(20)) + ').';
GO

PRINT 'Migratie 050_ProjectRegieUurTariefVastleggen voltooid.';
