-- =============================================
-- Migratie: 047_BudgetParamsVerkoopMarges
-- Datum: 2026-09-25
-- Omschrijving: Verkoopvoorstel per eenheid in de budgetwizard (stap 8 Verkoop / stap 9 Resultaat):
--   1. BudgetParams.DoelMargePerc  DECIMAL(8,4) NULL — doelmarge op de bouwkost (fractie, 0.15 = 15 %).
--      BudgetParams.GrondMargePerc DECIMAL(8,4) NULL — marge op de grondkost (fractie).
--      NULL = "niet overschreven per budget" → de standaard uit Instellingen > Bouwkost % wordt
--      gevolgd, exact hetzelfde mechanisme als ArchitectPerc/StudieIRPerc (zie migratie 027).
--   2. Standaardrijen in BouwkostPercentage met Sleutel 'doelmarge' (15,00) en 'grondmarge' (10,00)
--      in een nieuwe groep 'Verkoop'. Percentages daar staan in procentpunten; de applicatie deelt door 100.
--   Verdeelsleutels (gereduceerde oppervlakte voor bouwwaarde, grondoppervlakte voor grondwaarde)
--   zijn geen data maar code: BudgetOppervlaktesBO.OppGereduceerd en VerkoopVoorstelService.
--   Strikt additief + idempotent.
-- =============================================

IF COL_LENGTH('dbo.BudgetParams', 'DoelMargePerc') IS NULL
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [DoelMargePerc] DECIMAL(8,4) NULL;
    PRINT 'Kolom BudgetParams.DoelMargePerc toegevoegd.';
END
ELSE
    PRINT 'Kolom BudgetParams.DoelMargePerc bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.BudgetParams', 'GrondMargePerc') IS NULL
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [GrondMargePerc] DECIMAL(8,4) NULL;
    PRINT 'Kolom BudgetParams.GrondMargePerc toegevoegd.';
END
ELSE
    PRINT 'Kolom BudgetParams.GrondMargePerc bestaat al, overgeslagen.';
GO

-- Groep 'Verkoop' voor de standaardmarges (Instellingen > Kostprijsmaterialen > Bouwkost %)
DECLARE @groepId INT =
    (SELECT TOP 1 Id FROM [dbo].[BouwkostPercentageGroep] WHERE Naam = 'Verkoop' ORDER BY Id);

IF @groepId IS NULL
BEGIN
    DECLARE @volgorde INT = ISNULL((SELECT MAX(Volgorde) FROM [dbo].[BouwkostPercentageGroep]), 0) + 10;
    INSERT INTO [dbo].[BouwkostPercentageGroep] ([Naam], [Volgorde]) VALUES ('Verkoop', @volgorde);
    SET @groepId = SCOPE_IDENTITY();
    PRINT 'Groep BouwkostPercentageGroep ''Verkoop'' aangemaakt.';
END

INSERT INTO [dbo].[BouwkostPercentage] ([GroepId], [Naam], [Percentage], [Volgorde], [Sleutel])
SELECT @groepId, v.Naam, v.Perc, v.Volgorde, v.Sleutel
FROM (VALUES
        ('Doelmarge op bouwkost', CAST(15.00 AS DECIMAL(7,4)), 1, 'doelmarge'),
        ('Marge op grondkost',    CAST(10.00 AS DECIMAL(7,4)), 2, 'grondmarge')
     ) AS v(Naam, Perc, Volgorde, Sleutel)
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[BouwkostPercentage] p WHERE p.Sleutel = v.Sleutel
);
PRINT 'Standaardmarges (doelmarge/grondmarge) gecontroleerd/toegevoegd.';
GO

PRINT 'Migratie 047_BudgetParamsVerkoopMarges voltooid.';
