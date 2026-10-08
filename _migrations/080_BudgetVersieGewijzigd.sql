-- =============================================
-- Migratie: 080_BudgetVersieGewijzigd
-- Datum: 2026-10-08
-- Omschrijving: BudgetVersie.GewijzigdOp = tijdstip van de laatste opgeslagen wijziging in de budgetwizard
--   (wordt door BudgetVersieVergrendeldFilter bijgewerkt na elke geslaagde schrijvende actie). NULL = nooit gewijzigd sinds aanmaak
--   (het overzicht valt dan terug op CreatedAt). Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVersie') AND name = 'GewijzigdOp')
BEGIN
    ALTER TABLE [dbo].[BudgetVersie] ADD [GewijzigdOp] DATETIME2(0) NULL;
    PRINT 'Kolom GewijzigdOp toegevoegd aan BudgetVersie.';
END
ELSE PRINT 'Kolom GewijzigdOp bestaat al, overgeslagen.';
GO
-- TotaalKosten: bewaarde totale kostprijs voor het overzicht. NULL = nog niet berekend of verouderd (wordt bij elke wijziging
-- in de wizard door BudgetVersieVergrendeldFilter op NULL gezet en door het overzicht lui opnieuw berekend en bewaard).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVersie') AND name = 'TotaalKosten')
BEGIN
    ALTER TABLE [dbo].[BudgetVersie] ADD [TotaalKosten] DECIMAL(18,2) NULL;
    PRINT 'Kolom TotaalKosten toegevoegd aan BudgetVersie.';
END
ELSE PRINT 'Kolom TotaalKosten bestaat al, overgeslagen.';
GO
