-- =============================================
-- Migratie: 092_BudgetParamsOpmetingSondering
-- Datum: 2026-10-13
-- Omschrijving: Budgetwizard stap 7 - opmeting en sondering als twee aparte percentages (op de bouwkost) i.p.v. een gezamenlijk forfait.
--   BudgetParams.OpmetingPerc   : percentage opmeting als fractie (NULL = standaard uit Instellingen > Bouwkost %, rij "Opmeting").
--   BudgetParams.SonderingPerc  : percentage sondering als fractie (NULL = standaard uit Instellingen > Bouwkost %, rij "Sonderingen").
--   De oude kolom OpmetingSonderingForfait blijft bestaan maar telt niet meer mee.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetParams') AND name = 'OpmetingPerc')
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [OpmetingPerc] DECIMAL(9,6) NULL;
    PRINT 'Kolom OpmetingPerc toegevoegd aan BudgetParams.';
END
ELSE PRINT 'Kolom OpmetingPerc bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetParams') AND name = 'SonderingPerc')
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [SonderingPerc] DECIMAL(9,6) NULL;
    PRINT 'Kolom SonderingPerc toegevoegd aan BudgetParams.';
END
ELSE PRINT 'Kolom SonderingPerc bestaat al, overgeslagen.';
GO
