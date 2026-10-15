-- =============================================
-- Migratie: 093_BudgetParamsRegistratierechten (registratierechten + publiciteit %)
-- Datum: 2026-10-13
-- Omschrijving: Budgetwizard stap 7, kaart "Grondkost": registratierechten als percentage op de aankoopprijs van de grond.
--   BudgetParams.RegistratierechtenPerc : fractie (NULL = standaard uit Instellingen > Bouwkost %, rij "Registratierechten"; bestaat die niet: 12 %).
--   De notariskosten (ereloon) worden berekend volgens het notarisschaal (zie ServiceCore/Budget/NotarisTarief.cs) en worden niet bewaard.
--   BudgetParams.PubliciteitPerc        : fractie van de totale verkoopprijs (NULL = standaard uit Bouwkost %, rij "Publiciteit", 0,04 %). Vervangt het forfait PubliciteitForfait (kolom blijft, telt niet meer mee).
--   Standaardrijen in Instellingen > Kostprijsmaterialen > Bouwkost % (groep 'Overige kosten'): 'registratierechten' (12,00) en 'publiciteit' (0,04), enkel als de sleutel nog niet bestaat.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetParams') AND name = 'RegistratierechtenPerc')
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [RegistratierechtenPerc] DECIMAL(9,6) NULL;
    PRINT 'Kolom RegistratierechtenPerc toegevoegd aan BudgetParams.';
END
ELSE PRINT 'Kolom RegistratierechtenPerc bestaat al, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetParams') AND name = 'PubliciteitPerc')
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [PubliciteitPerc] DECIMAL(9,6) NULL;
    PRINT 'Kolom PubliciteitPerc toegevoegd aan BudgetParams.';
END
ELSE PRINT 'Kolom PubliciteitPerc bestaat al, overgeslagen.';
GO

DECLARE @groepId INT = (SELECT TOP 1 Id FROM [dbo].[BouwkostPercentageGroep] WHERE Naam = 'Overige kosten' ORDER BY Id);
IF @groepId IS NULL
BEGIN
    DECLARE @volgorde INT = ISNULL((SELECT MAX(Volgorde) FROM [dbo].[BouwkostPercentageGroep]), 0) + 10;
    INSERT INTO [dbo].[BouwkostPercentageGroep] ([Naam], [Volgorde]) VALUES ('Overige kosten', @volgorde);
    SET @groepId = SCOPE_IDENTITY();
    PRINT 'Groep ''Overige kosten'' aangemaakt.';
END

INSERT INTO [dbo].[BouwkostPercentage] ([GroepId], [Naam], [Percentage], [Volgorde], [Sleutel])
SELECT @groepId, v.Naam, v.Perc, v.Volgorde, v.Sleutel
FROM (VALUES
        ('Registratierechten (op aankoopprijs grond)', CAST(12.00 AS DECIMAL(7,4)), 1, 'registratierechten'),
        ('Publiciteit (op totale verkoopprijs)',       CAST(0.04  AS DECIMAL(7,4)), 2, 'publiciteit')
     ) AS v(Naam, Perc, Volgorde, Sleutel)
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[BouwkostPercentage] p WHERE p.Sleutel = v.Sleutel)
  AND NOT EXISTS (SELECT 1 FROM [dbo].[BouwkostPercentage] p WHERE v.Sleutel = 'registratierechten' AND p.Naam LIKE 'Registratierechten%');
PRINT 'Standaardrijen registratierechten/publiciteit gecontroleerd/toegevoegd.';
GO
