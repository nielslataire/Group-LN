-- =============================================
-- Migratie: 096_BudgetGrondTweedeZone
-- Datum: 2026-10-13
-- Omschrijving: Een woning kan grond in twee zones hebben (bv. woongebied + agrarisch) met een verschillende waarde.
--   BudgetOppervlaktes.GrondoppAgrarisch      : tweede grondoppervlakte (m2) naast Grondopp (woongebied); standaard 0.
--   BudgetParams.GrondAgrarischPerc           : waarde van de tweede zone t.o.v. de eerste (fractie; NULL = 20 %), enkel voor de verdeling van de grondkost over de loten.
--   BudgetVerkoopLijnen.GrondAgrPrijsPerM2      : eigen EUR/m2 voor de tweede zone van deze woning (stap 8).
--   BudgetVerkoopLijnen.GrondwaardeAgrarisch    : waarde van de tweede zone (= prijs x oppervlakte); Grondwaarde blijft de eerste zone.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetOppervlaktes') AND name = 'GrondoppAgrarisch')
BEGIN
    ALTER TABLE [dbo].[BudgetOppervlaktes] ADD [GrondoppAgrarisch] DECIMAL(8,2) NOT NULL CONSTRAINT DF_BudgetOppervlaktes_GrondoppAgrarisch DEFAULT 0;
    PRINT 'Kolom GrondoppAgrarisch toegevoegd aan BudgetOppervlaktes.';
END
ELSE PRINT 'Kolom GrondoppAgrarisch bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetParams') AND name = 'GrondAgrarischPerc')
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [GrondAgrarischPerc] DECIMAL(9,6) NULL;
    PRINT 'Kolom GrondAgrarischPerc toegevoegd aan BudgetParams.';
END
ELSE PRINT 'Kolom GrondAgrarischPerc bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVerkoopLijnen') AND name = 'GrondAgrPrijsPerM2')
BEGIN
    ALTER TABLE [dbo].[BudgetVerkoopLijnen] ADD [GrondAgrPrijsPerM2] DECIMAL(18,2) NULL;
    PRINT 'Kolom GrondAgrPrijsPerM2 toegevoegd aan BudgetVerkoopLijnen.';
END
ELSE PRINT 'Kolom GrondAgrPrijsPerM2 bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVerkoopLijnen') AND name = 'GrondwaardeAgrarisch')
BEGIN
    ALTER TABLE [dbo].[BudgetVerkoopLijnen] ADD [GrondwaardeAgrarisch] DECIMAL(18,2) NULL;
    PRINT 'Kolom GrondwaardeAgrarisch toegevoegd aan BudgetVerkoopLijnen.';
END
ELSE PRINT 'Kolom GrondwaardeAgrarisch bestaat al, overgeslagen.';
GO
