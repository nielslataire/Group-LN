-- =============================================
-- Migratie: 048_BudgetVerkoopLijnenWaarden
-- Datum: 2026-09-25
-- Omschrijving: Verkoopvoorstel increment 3 — het voorstel landt in de verkooplijnen (stap 8) en
--   kan doorgezet worden naar de Units van het project.
--     BudgetVerkoopLijnen.Grondwaarde DECIMAL(18,2) NULL — overgenomen/bijgestuurde grondwaarde per eenheid
--     BudgetVerkoopLijnen.Bouwwaarde  DECIMAL(18,2) NULL — idem bouwwaarde
--     BudgetVerkoopLijnen.Vraagprijs  DECIMAL(18,2) NULL — de vraagprijs die de gebruiker vastlegt
--                                                          (standaard = aanbevolen prijs uit het voorstel)
--   UnitId (bestond al, was nooit gevuld) koppelt de lijn aan een Unit voor "Doorzetten naar units":
--   Units.LandValue ← Grondwaarde, basis-bouwwaarde (UnitConstructionValue zonder FinishingOptionId) ← Bouwwaarde.
--   Strikt additief + idempotent.
-- =============================================

IF COL_LENGTH('dbo.BudgetVerkoopLijnen', 'Grondwaarde') IS NULL
BEGIN
    ALTER TABLE [dbo].[BudgetVerkoopLijnen] ADD [Grondwaarde] DECIMAL(18,2) NULL;
    PRINT 'Kolom BudgetVerkoopLijnen.Grondwaarde toegevoegd.';
END
ELSE
    PRINT 'Kolom BudgetVerkoopLijnen.Grondwaarde bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.BudgetVerkoopLijnen', 'Bouwwaarde') IS NULL
BEGIN
    ALTER TABLE [dbo].[BudgetVerkoopLijnen] ADD [Bouwwaarde] DECIMAL(18,2) NULL;
    PRINT 'Kolom BudgetVerkoopLijnen.Bouwwaarde toegevoegd.';
END
ELSE
    PRINT 'Kolom BudgetVerkoopLijnen.Bouwwaarde bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.BudgetVerkoopLijnen', 'Vraagprijs') IS NULL
BEGIN
    ALTER TABLE [dbo].[BudgetVerkoopLijnen] ADD [Vraagprijs] DECIMAL(18,2) NULL;
    PRINT 'Kolom BudgetVerkoopLijnen.Vraagprijs toegevoegd.';
END
ELSE
    PRINT 'Kolom BudgetVerkoopLijnen.Vraagprijs bestaat al, overgeslagen.';

PRINT 'Migratie 048_BudgetVerkoopLijnenWaarden voltooid.';
