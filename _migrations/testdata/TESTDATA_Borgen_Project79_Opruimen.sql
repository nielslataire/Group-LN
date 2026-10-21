-- ============================================================================================
-- TESTDATA_Borgen_Project79_Opruimen.sql — haalt de testdata van TESTDATA_Borgen_Project79.sql weer weg
-- ============================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;
DELETE FROM dbo.IncommingInvoiceInhoudingAfhandeling WHERE InhoudingId IN (SELECT h.Id FROM dbo.IncommingInvoiceInhouding h JOIN dbo.IncommingInvoices i ON i.Id = h.IncommingInvoiceId WHERE i.ProjectId = 79 AND i.Notes = N'TESTDATA borg');
DELETE FROM dbo.IncommingInvoiceInhouding WHERE IncommingInvoiceId IN (SELECT Id FROM dbo.IncommingInvoices WHERE ProjectId = 79 AND Notes = N'TESTDATA borg');
DELETE d FROM dbo.IncommingInvoiceDetail d JOIN dbo.IncommingInvoices i ON i.Id = d.IncommingInvoiceID WHERE i.ProjectId = 79 AND i.Notes = N'TESTDATA borg';
DELETE FROM dbo.IncommingInvoices WHERE ProjectId = 79 AND Notes = N'TESTDATA borg';
DELETE FROM dbo.ContractBorgVrijgave WHERE ContractId IN (SELECT ID FROM dbo.Contract WHERE ProjectID = 79 AND ContractSentNote = N'TESTDATA borg');
DELETE FROM dbo.ContractActivity WHERE ContractID IN (SELECT ID FROM dbo.Contract WHERE ProjectID = 79 AND ContractSentNote = N'TESTDATA borg');
DELETE FROM dbo.Contract WHERE ProjectID = 79 AND ContractSentNote = N'TESTDATA borg';
DELETE FROM dbo.ProjectBudget WHERE ProjectID = 79;  -- project 79 had geen budgetregels vóór deze testdata
DELETE FROM dbo.ConstructionIssue WHERE ProjectId = 79 AND ResponsiblePartyId = 7 AND Title IN (N'Voeg te breed boven raam', N'Waterdichting kelder', N'Leiding door brandwand');
UPDATE dbo.Project SET DeliveryDate = NULL, DeliveryDateDef = NULL WHERE ProjectId = 79;  -- waren NULL
COMMIT;
PRINT 'Testdata borgen project 79 opgeruimd.';
