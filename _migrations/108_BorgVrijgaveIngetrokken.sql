-- =============================================
-- Migratie: 108_BorgVrijgaveIngetrokken
-- Datum: 2026-10-21
-- Omschrijving: Nacalculatie, Borgen: een foute vrijgave moet gecorrigeerd kunnen worden, met bewaring van het spoor.
--   ContractBorgVrijgave.IngetrokkenOp/IngetrokkenDoor              : een ingetrokken vrijgave telt niet meer mee (de rij blijft staan).
--   IncommingInvoiceInhoudingAfhandeling.IngetrokkenOp/IngetrokkenDoor : idem voor een afhandeling van een extra inhouding.
--   De code filtert ingetrokken rijen standaard weg (EF query filter). Strikt additief + idempotent. Draai dit NA 105.
-- =============================================

IF COL_LENGTH('dbo.ContractBorgVrijgave', 'IngetrokkenOp') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContractBorgVrijgave] ADD [IngetrokkenOp] DATETIME2(0) NULL;
    PRINT 'Kolom ContractBorgVrijgave.IngetrokkenOp toegevoegd.';
END
ELSE PRINT 'Kolom ContractBorgVrijgave.IngetrokkenOp bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.ContractBorgVrijgave', 'IngetrokkenDoor') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContractBorgVrijgave] ADD [IngetrokkenDoor] NVARCHAR(100) NULL;
    PRINT 'Kolom ContractBorgVrijgave.IngetrokkenDoor toegevoegd.';
END
ELSE PRINT 'Kolom ContractBorgVrijgave.IngetrokkenDoor bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.IncommingInvoiceInhoudingAfhandeling', 'IngetrokkenOp') IS NULL
BEGIN
    ALTER TABLE [dbo].[IncommingInvoiceInhoudingAfhandeling] ADD [IngetrokkenOp] DATETIME2(0) NULL;
    PRINT 'Kolom IncommingInvoiceInhoudingAfhandeling.IngetrokkenOp toegevoegd.';
END
ELSE PRINT 'Kolom IncommingInvoiceInhoudingAfhandeling.IngetrokkenOp bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.IncommingInvoiceInhoudingAfhandeling', 'IngetrokkenDoor') IS NULL
BEGIN
    ALTER TABLE [dbo].[IncommingInvoiceInhoudingAfhandeling] ADD [IngetrokkenDoor] NVARCHAR(100) NULL;
    PRINT 'Kolom IncommingInvoiceInhoudingAfhandeling.IngetrokkenDoor toegevoegd.';
END
ELSE PRINT 'Kolom IncommingInvoiceInhoudingAfhandeling.IngetrokkenDoor bestaat al, overgeslagen.';
GO
