-- =============================================
-- Migratie: BudgetGegevens.AantalAanTeBouwenBuren
-- Datum: 2026-09-09
-- Omschrijving:
--   Nieuw aantal-veld op tab "Gegevens" van de budgetwizard:
--   het aantal aan te bouwen buren (aanpalende constructies waartegen
--   gebouwd wordt). Beschikbaar als parameter @aantal_aan_te_bouwen_buren
--   in de bewerkbare budgetformules (categorie "Aantallen").
-- =============================================

IF COL_LENGTH('dbo.BudgetGegevens', 'AantalAanTeBouwenBuren') IS NULL
BEGIN
    ALTER TABLE [dbo].[BudgetGegevens] ADD [AantalAanTeBouwenBuren] INT NULL;
    PRINT 'Kolom BudgetGegevens.AantalAanTeBouwenBuren toegevoegd.';
END
ELSE
    PRINT 'Kolom BudgetGegevens.AantalAanTeBouwenBuren bestaat al, overgeslagen.';
