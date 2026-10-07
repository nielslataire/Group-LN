-- =============================================
-- Migratie: 075_BudgetVerkoopPrijsPerM2
-- Datum: 2026-10-07
-- Omschrijving: Verkoopprijzen manueel per m² op stap 8 van de budgetwizard (zie BUDGET_VOORTGANG.md).
--   BudgetVerkoopLijnen: BouwPrijsPerM2 / GrondPrijsPerM2 = de ingestelde €/m² (bouw op gereduceerde oppervlakte,
--   grond op grondoppervlakte); PrijsBron = waar de prijs vandaan komt (1 voorstel, 2 markt, 3 referentiecode,
--   4 manueel €/m², 5 manueel bedrag). NULL bij bestaande lijnen (= bedrag zoals vastgelegd).
--   BudgetPrijsReferentie: Datum en Bron, zodat een manueel opgezochte referentie herleidbaar blijft.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVerkoopLijnen') AND name = 'BouwPrijsPerM2')
BEGIN
    ALTER TABLE [dbo].[BudgetVerkoopLijnen] ADD [BouwPrijsPerM2] DECIMAL(18,2) NULL;
    PRINT 'Kolom BouwPrijsPerM2 toegevoegd aan BudgetVerkoopLijnen.';
END
ELSE PRINT 'Kolom BouwPrijsPerM2 bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVerkoopLijnen') AND name = 'GrondPrijsPerM2')
BEGIN
    ALTER TABLE [dbo].[BudgetVerkoopLijnen] ADD [GrondPrijsPerM2] DECIMAL(18,2) NULL;
    PRINT 'Kolom GrondPrijsPerM2 toegevoegd aan BudgetVerkoopLijnen.';
END
ELSE PRINT 'Kolom GrondPrijsPerM2 bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVerkoopLijnen') AND name = 'PrijsBron')
BEGIN
    ALTER TABLE [dbo].[BudgetVerkoopLijnen] ADD [PrijsBron] TINYINT NULL;
    PRINT 'Kolom PrijsBron toegevoegd aan BudgetVerkoopLijnen.';
END
ELSE PRINT 'Kolom PrijsBron bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetPrijsReferentie') AND name = 'Datum')
BEGIN
    ALTER TABLE [dbo].[BudgetPrijsReferentie] ADD [Datum] DATE NULL;
    PRINT 'Kolom Datum toegevoegd aan BudgetPrijsReferentie.';
END
ELSE PRINT 'Kolom Datum bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetPrijsReferentie') AND name = 'Bron')
BEGIN
    ALTER TABLE [dbo].[BudgetPrijsReferentie] ADD [Bron] NVARCHAR(200) NULL;
    PRINT 'Kolom Bron toegevoegd aan BudgetPrijsReferentie.';
END
ELSE PRINT 'Kolom Bron bestaat al, overgeslagen.';
GO
