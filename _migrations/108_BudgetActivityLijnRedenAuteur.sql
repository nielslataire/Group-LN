-- =============================================
-- Migratie: 108_BudgetActivityLijnRedenAuteur
-- Datum: 2026-10-21
-- Omschrijving: Budgetstap 6 (design-handoff 39g): bij een correctie ≠ 100 % hoort een reden (BudgetActivityLijnen.Omschrijving) met wie ze invulde en wanneer
--   ("Niels Lataire · 14/10/2026 10:32 · zichtbaar in het interne budget").
--   BudgetActivityLijnen.OmschrijvingDoor : naam van wie de reden voor het laatst wijzigde. NULL bij oudere redenen.
--   BudgetActivityLijnen.OmschrijvingOp   : tijdstip van die wijziging (UTC). NULL bij oudere redenen.
--   Strikt additief + idempotent.
-- =============================================
IF COL_LENGTH('dbo.BudgetActivityLijnen', 'OmschrijvingDoor') IS NULL
BEGIN
    ALTER TABLE [dbo].[BudgetActivityLijnen] ADD [OmschrijvingDoor] NVARCHAR(100) NULL;
    PRINT 'Kolom BudgetActivityLijnen.OmschrijvingDoor toegevoegd.';
END
ELSE PRINT 'Kolom BudgetActivityLijnen.OmschrijvingDoor bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.BudgetActivityLijnen', 'OmschrijvingOp') IS NULL
BEGIN
    ALTER TABLE [dbo].[BudgetActivityLijnen] ADD [OmschrijvingOp] DATETIME2(0) NULL;
    PRINT 'Kolom BudgetActivityLijnen.OmschrijvingOp toegevoegd.';
END
ELSE PRINT 'Kolom BudgetActivityLijnen.OmschrijvingOp bestaat al, overgeslagen.';
GO
