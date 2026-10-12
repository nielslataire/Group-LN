-- =============================================
-- Migratie: 090_BudgetOppervlaktesPoorten
-- Datum: 2026-10-12
-- Omschrijving: Budgetwizard stap 2 - aantal garagepoorten per rij (eenheid). Een woning met ingebouwde garage krijgt hier het aantal poorten
--   (geen oppervlakte nodig); een apart bijgebouwde garage krijgt daarnaast zijn oppervlakte in de kolom "Gar. bgr.". Een rij met een garage-subtype
--   en 0 poorten telt nog steeds voor 1 poort (bestaande budgetten veranderen niet).
--   BudgetOppervlaktes.Poorten : aantal poorten van deze rij (standaard 0).
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetOppervlaktes') AND name = 'Poorten')
BEGIN
    ALTER TABLE [dbo].[BudgetOppervlaktes] ADD [Poorten] INT NOT NULL CONSTRAINT DF_BudgetOppervlaktes_Poorten DEFAULT 0;
    PRINT 'Kolom Poorten toegevoegd aan BudgetOppervlaktes.';
END
ELSE PRINT 'Kolom Poorten bestaat al, overgeslagen.';
GO
