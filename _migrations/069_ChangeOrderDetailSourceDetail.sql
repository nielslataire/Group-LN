-- =============================================
-- Migratie: 069_ChangeOrderDetailSourceDetail
-- Datum: 2026-10-02
-- Omschrijving: "Offertes & wijzigingen" gl-v2 — de tussenstap "offerte aan de klant" (beslissing
--   Niels 2026-10-02): een offerte wordt opgemaakt en per mail verzonden; "Omzetten" maakt er een
--   NIEUWE wijzigingsopdracht van (de offerte blijft ongewijzigd bestaan als bron). In die
--   wijzigingsopdracht liggen de prijzen van de overgenomen regels vast: enkel het aantal en de
--   omschrijving zijn nog aanpasbaar, de regel mag verwijderd worden, en nieuwe regels krijgen een
--   vrije prijs. SourceDetailId wijst van een overgenomen WO-regel naar de offerteregel waar ze
--   vandaan komt — gevuld = prijs vergrendeld, NULL = eigen regel met vrije prijs (ook alle
--   bestaande regels). Bewust geen foreign key: louter herkomst, en een FK zou het verwijderen van
--   een offerteregel blokkeren. Strikt additief + idempotent.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrderDetail') AND name = 'SourceDetailId')
BEGIN
    ALTER TABLE [dbo].[ChangeOrderDetail] ADD [SourceDetailId] INT NULL;
    PRINT 'Kolom SourceDetailId toegevoegd aan ChangeOrderDetail.';
END
ELSE PRINT 'Kolom SourceDetailId bestaat al, overgeslagen.';
