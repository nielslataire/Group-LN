-- =============================================
-- Migratie: IncommingInvoiceKind
-- Datum: 2026-10-10
-- Omschrijving:
--   - IncommingInvoices.InvoiceKind : TINYINT NULL, 0 = Vordering (default/leeg), 1 = Eindvordering,
--                                      2 = Creditnota. Design-handoff 42c/42e punt 07.
--   Bewust GEEN hergebruik van de bestaande IncommingInvoices.DocumentType-kolom: die wordt al
--   gebruikt door de Octopus-/verrijkingspijplijn met een eigen waardenverzameling ("Invoice"/
--   "CreditNote", uit UBL/Azure-herkenning) — een tweede, andere betekenis in dezelfde kolom zou de
--   twee door elkaar halen. NULL/0 = Vordering, dus elke bestaande en elke nieuw ingevoerde factuur
--   via het legacy Add/Edit-scherm (dat dit veld niet kent) blijft gewoon "Vordering".
-- =============================================

IF COL_LENGTH('dbo.IncommingInvoices', 'InvoiceKind') IS NULL
BEGIN
    ALTER TABLE [dbo].[IncommingInvoices] ADD [InvoiceKind] TINYINT NULL;
    PRINT 'Kolom IncommingInvoices.InvoiceKind toegevoegd.';
END
ELSE
    PRINT 'Kolom IncommingInvoices.InvoiceKind bestaat al, overgeslagen.';
