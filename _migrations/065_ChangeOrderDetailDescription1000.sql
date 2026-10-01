-- =============================================
-- Migratie: 065_ChangeOrderDetailDescription1000
-- Datum: 2026-10-01
-- Omschrijving: ChangeOrderDetail.Description van nvarchar(250) naar nvarchar(1000). "Offerte inlezen"
--   (design-handoff 20c) vouwt de specificatierijen van een leveranciersofferte (kleur, afmetingen,
--   motortype, referentie …) in de omschrijving van de artikelregel — dat past niet in 250 tekens en
--   zou bij opslaan op een afkappingsfout stuklopen. Kolom VERBREDEN is niet-destructief (bestaande
--   waarden blijven ongewijzigd); geen gegevens worden verplaatst of verwijderd. Idempotent: enkel als
--   de kolom nu smaller is dan 1000.
-- =============================================

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ChangeOrderDetail') AND name = 'Description'
      AND max_length >= 0 AND max_length < 2000   -- nvarchar: max_length telt in bytes (2 per teken); -1 = MAX
)
BEGIN
    ALTER TABLE [dbo].[ChangeOrderDetail] ALTER COLUMN [Description] NVARCHAR(1000) NOT NULL;
    PRINT 'ChangeOrderDetail.Description verbreed naar nvarchar(1000).';
END
ELSE
    PRINT 'ChangeOrderDetail.Description is al breed genoeg, overgeslagen.';

PRINT 'Migratie 065_ChangeOrderDetailDescription1000 voltooid.';
