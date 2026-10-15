-- =============================================
-- Migratie: 095_BudgetParamsVerkoopkosten
-- Datum: 2026-10-13
-- Omschrijving: Budgetwizard stap 7 (kaart "Verkoopkosten") en stap 9 (kaart "Verkoop t.o.v. kostprijs"):
--   BudgetParams.MakelaarsCommissiePerc : commissie als fractie van de verkoopprijs per eenheid (NULL = standaard uit Bouwkost %, rij "makelaarscommissie").
--   BudgetParams.OverigeVerkoopkosten   : forfait per project (EPC, keuring, ...) in euro.
--   De btw op de commissie is vast 21 % (niet bewaard). Standaardrij 'makelaarscommissie' (3,00) in Instellingen > Kostprijsmaterialen > Bouwkost % (groep 'Verkoop').
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetParams') AND name = 'MakelaarsCommissiePerc')
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [MakelaarsCommissiePerc] DECIMAL(9,6) NULL;
    PRINT 'Kolom MakelaarsCommissiePerc toegevoegd aan BudgetParams.';
END
ELSE PRINT 'Kolom MakelaarsCommissiePerc bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetParams') AND name = 'OverigeVerkoopkosten')
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [OverigeVerkoopkosten] DECIMAL(18,2) NULL;
    PRINT 'Kolom OverigeVerkoopkosten toegevoegd aan BudgetParams.';
END
ELSE PRINT 'Kolom OverigeVerkoopkosten bestaat al, overgeslagen.';
GO

DECLARE @groepId INT = (SELECT TOP 1 Id FROM [dbo].[BouwkostPercentageGroep] WHERE Naam = 'Verkoop' ORDER BY Id);
IF @groepId IS NULL
BEGIN
    DECLARE @volgorde INT = ISNULL((SELECT MAX(Volgorde) FROM [dbo].[BouwkostPercentageGroep]), 0) + 10;
    INSERT INTO [dbo].[BouwkostPercentageGroep] ([Naam], [Volgorde]) VALUES ('Verkoop', @volgorde);
    SET @groepId = SCOPE_IDENTITY();
END
INSERT INTO [dbo].[BouwkostPercentage] ([GroepId], [Naam], [Percentage], [Volgorde], [Sleutel])
SELECT @groepId, 'Makelaarscommissie (op verkoopprijs)', CAST(3.00 AS DECIMAL(7,4)), 3, 'makelaarscommissie'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[BouwkostPercentage] p WHERE p.Sleutel = 'makelaarscommissie');
PRINT 'Standaardrij makelaarscommissie gecontroleerd/toegevoegd.';
GO
