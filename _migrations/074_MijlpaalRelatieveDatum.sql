-- =============================================
-- Migratie: 074_MijlpaalRelatieveDatum
-- Datum: 2026-10-07
-- Omschrijving: Design-handoff 30e ("Mijlpaal toevoegen" - "OF RELATIEF + 30 d. na vorige mijlpaal"):
--   een trajectmijlpaal kan haar streefdatum afleiden uit een andere mijlpaal van hetzelfde traject.
--   RelatiefAnkerMijlpaalId = de mijlpaal waarna geteld wordt (geen FK: een verwijderde ankermijlpaal
--   laat de huidige streefdatum gewoon staan), RelatiefOffsetDagen = aantal dagen erna (mag 0 zijn).
--   De afgeleide datum wordt in Mijlpaal.Doeldatum bewaard (opslaan + TrajectRecalculationService).
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Mijlpaal') AND name = 'RelatiefAnkerMijlpaalId')
BEGIN
    ALTER TABLE [dbo].[Mijlpaal] ADD [RelatiefAnkerMijlpaalId] INT NULL;
    PRINT 'Kolom RelatiefAnkerMijlpaalId toegevoegd aan Mijlpaal.';
END
ELSE PRINT 'Kolom RelatiefAnkerMijlpaalId bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Mijlpaal') AND name = 'RelatiefOffsetDagen')
BEGIN
    ALTER TABLE [dbo].[Mijlpaal] ADD [RelatiefOffsetDagen] INT NULL;
    PRINT 'Kolom RelatiefOffsetDagen toegevoegd aan Mijlpaal.';
END
ELSE PRINT 'Kolom RelatiefOffsetDagen bestaat al, overgeslagen.';
GO
