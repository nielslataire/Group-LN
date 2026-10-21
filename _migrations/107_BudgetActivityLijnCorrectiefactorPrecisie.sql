-- =============================================
-- Migratie: 107_BudgetActivityLijnCorrectiefactorPrecisie
-- Datum: 2026-10-21
-- Omschrijving: Budgetstap 6 (Activiteiten): de correctie per activiteit is een factor (1 = 100 %). De kolom was decimal(4,2): een correctie van 95,5 % (factor 0,955)
--   werd als 0,96 bewaard en kwam terug als 96 %. Nu decimal(9,4). Breder maken van een bestaande kolom, geen gegevens gaan verloren; de default 1 blijft.
--   Idempotent (enkel als de kolom nog precisie 4 heeft).
-- =============================================
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetActivityLijnen') AND name = 'Correctiefactor' AND precision = 4)
BEGIN
    DECLARE @dc SYSNAME = (SELECT dc.name FROM sys.default_constraints dc
                             JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                            WHERE dc.parent_object_id = OBJECT_ID('dbo.BudgetActivityLijnen') AND c.name = 'Correctiefactor');
    IF @dc IS NOT NULL EXEC('ALTER TABLE [dbo].[BudgetActivityLijnen] DROP CONSTRAINT [' + @dc + ']');
    ALTER TABLE [dbo].[BudgetActivityLijnen] ALTER COLUMN [Correctiefactor] DECIMAL(9,4) NOT NULL;
    ALTER TABLE [dbo].[BudgetActivityLijnen] ADD CONSTRAINT [DF_BudgetActivityLijnen_Correctiefactor] DEFAULT (1) FOR [Correctiefactor];
    PRINT 'Kolom BudgetActivityLijnen.Correctiefactor is nu decimal(9,4).';
END
ELSE PRINT 'Kolom BudgetActivityLijnen.Correctiefactor heeft al de nieuwe precisie, overgeslagen.';
GO
