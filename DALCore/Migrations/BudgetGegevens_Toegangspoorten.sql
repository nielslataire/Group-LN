-- Voeg AantalToegangspoorten toe aan BudgetGegevens
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.BudgetGegevens') AND name = N'AantalToegangspoorten')
    ALTER TABLE [dbo].[BudgetGegevens]
        ADD [AantalToegangspoorten] INT NULL;
