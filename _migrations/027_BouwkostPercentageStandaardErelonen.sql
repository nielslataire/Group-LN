-- =============================================
-- Migratie: BouwkostPercentage.Sleutel + standaard-erelonen
-- Datum: 2026-09-09
-- Omschrijving:
--   'Sleutel' markeert vaste, systeem-beheerde percentagerijen die niet
--   hernoemd of verwijderd mogen worden en die als standaard dienen voor
--   het tabblad Budget > Parameters:
--     projectcoordinatie -> BudgetParams.ProjectcoordinatiePerc
--     architect          -> BudgetParams.ArchitectPerc
--     ingenieur          -> BudgetParams.StudieIRPerc   (label 'Ingenieur')
--   Elk bedrag op Parameters = percentage x totale (gecorrigeerde) bouwkost
--   uit de budgetactiviteiten.
-- =============================================

IF COL_LENGTH('dbo.BouwkostPercentage', 'Sleutel') IS NULL
BEGIN
    ALTER TABLE [dbo].[BouwkostPercentage] ADD [Sleutel] NVARCHAR(50) NULL;
    PRINT 'Kolom BouwkostPercentage.Sleutel toegevoegd.';
END
ELSE
    PRINT 'Kolom BouwkostPercentage.Sleutel bestaat al, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BouwkostPercentage_Sleutel')
BEGIN
    CREATE UNIQUE INDEX [UX_BouwkostPercentage_Sleutel]
        ON [dbo].[BouwkostPercentage] ([Sleutel])
        WHERE [Sleutel] IS NOT NULL;
    PRINT 'Index UX_BouwkostPercentage_Sleutel aangemaakt.';
END
GO

-- Groep waarin de standaardrijen komen: bestaande 'Erelonen', anders eerste groep, anders nieuw aanmaken.
DECLARE @groepId INT =
    (SELECT TOP 1 Id FROM [dbo].[BouwkostPercentageGroep] WHERE Naam = 'Erelonen' ORDER BY Id);

IF @groepId IS NULL
    SET @groepId = (SELECT TOP 1 Id FROM [dbo].[BouwkostPercentageGroep] ORDER BY Volgorde, Id);

IF @groepId IS NULL
BEGIN
    INSERT INTO [dbo].[BouwkostPercentageGroep] ([Naam], [Volgorde]) VALUES ('Erelonen', 10);
    SET @groepId = SCOPE_IDENTITY();
END

-- Standaardrijen alleen toevoegen als de sleutel nog niet bestaat.
INSERT INTO [dbo].[BouwkostPercentage] ([GroepId], [Naam], [Percentage], [Volgorde], [Sleutel])
SELECT @groepId, v.Naam, v.Perc, v.Volgorde, v.Sleutel
FROM (VALUES
        ('Projectcoördinatie', CAST(5.25 AS DECIMAL(7,4)), 1, 'projectcoordinatie'),
        ('Architect',          CAST(0.00 AS DECIMAL(7,4)), 2, 'architect'),
        ('Ingenieur',          CAST(0.00 AS DECIMAL(7,4)), 3, 'ingenieur')
     ) AS v(Naam, Perc, Volgorde, Sleutel)
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[BouwkostPercentage] p WHERE p.Sleutel = v.Sleutel
);
PRINT 'Standaard-erelonen (projectcoordinatie/architect/ingenieur) gecontroleerd/toegevoegd.';
GO
