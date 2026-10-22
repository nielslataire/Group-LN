-- ============================================================================================
-- TESTDATA_Verzekeringen_Project79_Opruimen.sql — haalt de testdata van TESTDATA_Verzekeringen_Project79.sql weer weg
-- ============================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

-- Attesten (de eenheden-koppelingen gaan mee via ON DELETE CASCADE, de rijen eerst voor de duidelijkheid)
DELETE FROM dbo.InsuranceAttestUnit WHERE AttestId IN (SELECT Id FROM dbo.InsuranceAttest WHERE ProjectId = 79 AND Note = N'TESTDATA verzekeringen');
DELETE FROM dbo.InsuranceAttest WHERE ProjectId = 79 AND Note = N'TESTDATA verzekeringen';

-- Polissen en de contractactiviteit van de makelaar
DECLARE @caIds TABLE (Id INT);
INSERT INTO @caIds SELECT ContractActivityID FROM dbo.Insurances WHERE ProjectId = 79 AND Note = N'TESTDATA verzekeringen' AND ContractActivityID IS NOT NULL;
DELETE FROM dbo.Insurances WHERE ProjectId = 79 AND Note = N'TESTDATA verzekeringen';
DELETE FROM dbo.ContractActivity WHERE Id IN (SELECT Id FROM @caIds);

-- Contracten (en hun activiteiten)
DELETE FROM dbo.ContractActivity WHERE ContractID IN (SELECT ID FROM dbo.Contract WHERE ProjectID = 79 AND ContractSentNote = N'TESTDATA verzekeringen');
DELETE FROM dbo.Contract WHERE ProjectID = 79 AND ContractSentNote = N'TESTDATA verzekeringen';

-- Documentkoppelingen en eenheden van de testwoningen
DELETE FROM dbo.DocumentLinks WHERE UnitId IN (SELECT Id FROM dbo.Units WHERE ProjectId = 79 AND Name LIKE N'TEST Woning _');
DELETE FROM dbo.Units WHERE ProjectId = 79 AND Name LIKE N'TEST Woning _';

-- Testverzekeraars (enkel als niets ze nog gebruikt)
DELETE FROM dbo.InsuranceCompanies WHERE Name IN (N'TEST Verzekeraar A', N'TEST Verzekeraar B') AND Id NOT IN (SELECT InsuranceCompanyID FROM dbo.Insurances WHERE InsuranceCompanyID IS NOT NULL);

-- Data van het project die het script zelf zette (enkel als ze nog de testwaarde hebben)
UPDATE dbo.Project SET DeliveryDate = NULL WHERE ProjectId = 79 AND DeliveryDate = '2026-08-15';
UPDATE dbo.Project SET DeliveryDateDef = NULL WHERE ProjectId = 79 AND DeliveryDateDef = '2027-08-15';
UPDATE dbo.Project SET StartDateConstruction = NULL WHERE ProjectId = 79 AND StartDateConstruction = '2025-04-25';

COMMIT;
PRINT 'Testdata verzekeringen project 79 opgeruimd.';
