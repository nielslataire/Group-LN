-- ============================================================================================
-- TESTDATA_Verzekeringen_Project79.sql — voorbeelddata voor Verzekeringen (design-handoff punt 43) op project 79 "TEST Financieel - Facturatie"
-- ============================================================================================
-- Geen schema-migratie, enkel testdata voor db_ab5fbb_testdb. Vereist: migratie 110 en 111 zijn gedraaid.
-- Alles is gemarkeerd (polissen en attesten: Note 'TESTDATA verzekeringen'; contracten: ContractSentNote 'TESTDATA verzekeringen';
-- eenheden: naam 'TEST Woning 1' t/m 'TEST Woning 4'; verzekeraars: naam 'TEST Verzekeraar A/B') en gaat weg met TESTDATA_Verzekeringen_Project79_Opruimen.sql.
-- Bevat: een ABR-polis via een makelaar (termijn voorbij, verlenging actief, waarde lager dan een definitief budget zodra er een is),
-- een BA-polis die binnen enkele maanden vervalt, een beëindigde brandpolis voor het bestaande gebouw (opgelost aandachtspunt), een aangevraagde polis,
-- een ontbrekende brandverzekering voor de onverkochte eenheden (de voorlopige oplevering is dan al bereikt), 5 aannemerscontracten met en zonder attesten (tienjarige per woning en BA) en 4 woningen
-- (2 verkocht aan de klanten van TESTDATA_Financieel.sql, 2 onverkocht). Heeft project 79 nog geen opleveringsdata of startdatum werf,
-- dan zet het script ze (VO 15/08/2026, DO 15/08/2027, start 25/04/2025) en het opruimscript haalt ze weer weg.
-- Geen polisdocument en geen attestbestand: de koppeling van attesten aan kopers (Documenten) toon je met een eigen upload via het scherm.
-- ============================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF NOT EXISTS (SELECT 1 FROM dbo.Project WHERE ProjectId = 79)
BEGIN PRINT 'Project 79 bestaat niet, niets gedaan.'; RETURN; END
IF EXISTS (SELECT 1 FROM dbo.Insurances WHERE ProjectId = 79 AND Note = N'TESTDATA verzekeringen')
BEGIN PRINT 'Testdata bestaat al, niets gedaan.'; RETURN; END

BEGIN TRAN;

-- Opleveringsdata en start werf, enkel als ze nog leeg zijn
UPDATE dbo.Project SET DeliveryDate = '2026-08-15' WHERE ProjectId = 79 AND DeliveryDate IS NULL;
UPDATE dbo.Project SET DeliveryDateDef = '2027-08-15' WHERE ProjectId = 79 AND DeliveryDateDef IS NULL;
UPDATE dbo.Project SET StartDateConstruction = '2025-04-25' WHERE ProjectId = 79 AND StartDateConstruction IS NULL;

-- Verzekeraars (bestaande gebruiken, anders twee testverzekeraars)
IF (SELECT COUNT(*) FROM dbo.InsuranceCompanies) < 2
BEGIN
    INSERT INTO dbo.InsuranceCompanies (Name, PostcodeId, Straat, Huisnummer) VALUES (N'TEST Verzekeraar A', 2608, N'Testlaan', N'1');
    INSERT INTO dbo.InsuranceCompanies (Name, PostcodeId, Straat, Huisnummer) VALUES (N'TEST Verzekeraar B', 2608, N'Testlaan', N'2');
END
DECLARE @vzA INT = (SELECT TOP 1 Id FROM dbo.InsuranceCompanies ORDER BY Id);
DECLARE @vzB INT = (SELECT TOP 1 Id FROM dbo.InsuranceCompanies WHERE Id <> @vzA ORDER BY Id);

-- Woningen: een eenheidstype onder een groep met "woon" in de naam (anders type 2 zoals TESTDATA_Financieel.sql)
DECLARE @woonType INT = (SELECT TOP 1 t.Id FROM dbo.UnitTypes t JOIN dbo.UnitGroupTypes g ON g.Id = t.GroupId WHERE g.Name LIKE N'%woon%' ORDER BY t.Id);
IF @woonType IS NULL SET @woonType = 2;
DECLARE @klantA INT = (SELECT TOP 1 Id FROM dbo.ClientAccount WHERE Name = N'WO_Test_HoofdEigenaar');
DECLARE @klantB INT = (SELECT TOP 1 Id FROM dbo.ClientAccount WHERE Name = N'WO_Test_MedeEigenaar');
DECLARE @w1 INT, @w2 INT, @w3 INT, @w4 INT;
INSERT INTO dbo.Units (Name, TypeId, ProjectId, ClientAccountID, ConstructionValueSold, IsLink, IsOption) VALUES (N'TEST Woning 1', @woonType, 79, @klantA, 250000, 0, 0); SET @w1 = SCOPE_IDENTITY();
INSERT INTO dbo.Units (Name, TypeId, ProjectId, ClientAccountID, ConstructionValueSold, IsLink, IsOption) VALUES (N'TEST Woning 2', @woonType, 79, @klantB, 260000, 0, 0); SET @w2 = SCOPE_IDENTITY();
INSERT INTO dbo.Units (Name, TypeId, ProjectId, ClientAccountID, ConstructionValueSold, IsLink, IsOption) VALUES (N'TEST Woning 3', @woonType, 79, NULL, NULL, 0, 0); SET @w3 = SCOPE_IDENTITY();
INSERT INTO dbo.Units (Name, TypeId, ProjectId, ClientAccountID, ConstructionValueSold, IsLink, IsOption) VALUES (N'TEST Woning 4', @woonType, 79, NULL, NULL, 0, 0); SET @w4 = SCOPE_IDENTITY();

-- Contracten met aannemers (TienjarigeBron: 0 via de aannemer, 2 niet vereist, NULL niet bepaald)
DECLARE @cRuw INT, @cDak INT, @cStab INT, @cElek INT, @cArch INT, @cMak INT, @caMak INT;
INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, GuaranteeType, ContractSigned, ContractName, ContractSentNote, TienjarigeBron)
VALUES (79, 4, 0, 21, 30, 1, 1, N'Ruwbouw (test)', N'TESTDATA verzekeringen', 0); SET @cRuw = SCOPE_IDENTITY();
INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@cRuw, 37, 200000.00);
INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, GuaranteeType, ContractSigned, ContractName, ContractSentNote, TienjarigeBron)
VALUES (79, 7, 0, 21, 30, 1, 1, N'Dakwerken (test)', N'TESTDATA verzekeringen', 0); SET @cDak = SCOPE_IDENTITY();
INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@cDak, 194, 120000.00);
INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, GuaranteeType, ContractSigned, ContractName, ContractSentNote, TienjarigeBron)
VALUES (79, 9, 0, 21, 30, 1, 1, N'Stabiliteit (test)', N'TESTDATA verzekeringen', 0); SET @cStab = SCOPE_IDENTITY();
INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@cStab, 39, 30000.00);
INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, GuaranteeType, ContractSigned, ContractName, ContractSentNote, TienjarigeBron)
VALUES (79, 12, 0, 21, 30, 1, 1, N'Elektriciteit (test)', N'TESTDATA verzekeringen', 2); SET @cElek = SCOPE_IDENTITY();
INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@cElek, 241, 23115.00);
INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, GuaranteeType, ContractSigned, ContractName, ContractSentNote, TienjarigeBron)
VALUES (79, 13, 0, 21, 30, 1, 1, N'Architect (test)', N'TESTDATA verzekeringen', 0); SET @cArch = SCOPE_IDENTITY();
INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@cArch, 46, 40000.00);

-- Makelaar: contract met de activiteit 142 "Verzekeringen" (zo hangt een polis via een makelaar aan het project)
INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, GuaranteeType, ContractSigned, ContractName, ContractSentNote)
VALUES (79, 4, 0, 21, 30, 1, 1, N'Makelaar verzekeringen (test)', N'TESTDATA verzekeringen'); SET @cMak = SCOPE_IDENTITY();
INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@cMak, 142, NULL); SET @caMak = SCOPE_IDENTITY();

-- Polissen
-- 1. ABR via de makelaar: termijn 15 maanden + 8 verlenging + 12 maanden waarborgperiode
INSERT INTO dbo.Insurances (ContractActivityID, ProjectId, InsuranceCompanyID, Type, Polisnummer, Startdate, Period, ExtensionPeriod, GuaranteePeriod, InsuredValue, Premium, Note,
                            IsRequested, StartAtDelivery, RemindStart, RemindSale, RemindExpiry)
VALUES (@caMak, 79, @vzA, 1, N'81.218.177-0000', '2025-04-25', 15, 8, 12, 1150000.00, 6200.00, N'TESTDATA verzekeringen', 0, 0, 1, 1, 1);
-- 2. BA-uitbating rechtstreeks, vervalt over ongeveer 70 dagen
INSERT INTO dbo.Insurances (ContractActivityID, ProjectId, InsuranceCompanyID, Type, Polisnummer, Startdate, Enddate, InsuredValue, Note, IsRequested, StartAtDelivery, RemindStart, RemindSale, RemindExpiry)
VALUES (NULL, 79, @vzB, 4, N'BA-2025-0912', '2025-01-01', DATEADD(DAY, 71, CAST(GETDATE() AS DATE)), 2500000.00, N'TESTDATA verzekeringen', 0, 0, 1, 1, 1);
-- 3. Brand bestaand gebouw, beëindigd na de afbraak (opgelost aandachtspunt)
INSERT INTO dbo.Insurances (ContractActivityID, ProjectId, InsuranceCompanyID, Type, Building, Polisnummer, Startdate, Enddate, EndReason, InsuredValue, Note, IsRequested, StartAtDelivery, RemindStart, RemindSale, RemindExpiry)
VALUES (NULL, 79, @vzA, 2, 0, N'BR-77.20.118', '2025-01-01', '2025-05-14', N'Opgezegd na de afbraak op 14/05/2025, geen dekking meer nodig.', 320000.00, N'TESTDATA verzekeringen', 0, 0, 1, 1, 1);
-- 4. Aangevraagd, zonder polisnummer: een werfverzekering die over 20 dagen start
INSERT INTO dbo.Insurances (ContractActivityID, ProjectId, InsuranceCompanyID, Type, AppliesTo, Startdate, Note, IsRequested, StartAtDelivery, RemindStart, RemindSale, RemindExpiry)
VALUES (NULL, 79, @vzB, 5, N'werfzone', DATEADD(DAY, 20, CAST(GETDATE() AS DATE)), N'TESTDATA verzekeringen', 1, 0, 1, 1, 1);

-- Attesten van aannemers (Note als marker)
-- Ruwbouw: tienjarige ontbreekt (opgevraagd op 12/09/2026), BA geldig tot 31/12/2026
INSERT INTO dbo.InsuranceAttest (ProjectId, ContractId, Kind, Status, LastRequestedAt, LastRequestedBy, Note) VALUES (79, @cRuw, 0, 0, '2026-09-12T09:00:00', N'testdata', N'TESTDATA verzekeringen');
INSERT INTO dbo.InsuranceAttest (ProjectId, ContractId, Kind, Status, ValidUntil, Insurer, PolicyNumber, ReceivedAt, Note) VALUES (79, @cRuw, 1, 1, '2026-12-31', N'Baloise', N'BA-RW-001', '2026-01-10', N'TESTDATA verzekeringen');
-- Dakwerken en Stabiliteit: nog niets ontvangen; Stabiliteit heeft een BA die binnen 30 dagen vervalt
INSERT INTO dbo.InsuranceAttest (ProjectId, ContractId, Kind, Status, ValidUntil, Insurer, PolicyNumber, ReceivedAt, Note) VALUES (79, @cStab, 1, 1, DATEADD(DAY, 30, CAST(GETDATE() AS DATE)), N'AXA', N'BA-ST-114', '2025-11-02', N'TESTDATA verzekeringen');
-- Elektriciteit: tienjarige niet vereist (via het contract), BA geldig
INSERT INTO dbo.InsuranceAttest (ProjectId, ContractId, Kind, Status, ValidUntil, Insurer, PolicyNumber, ReceivedAt, Note) VALUES (79, @cElek, 1, 1, '2027-03-31', N'Protect', N'BA-EL-077', '2026-03-01', N'TESTDATA verzekeringen');
-- Architect: tienjarige ontvangen voor woning 1 en 2 (zonder bestand), BA doorlopend
DECLARE @atArch INT;
INSERT INTO dbo.InsuranceAttest (ProjectId, ContractId, Kind, Status, UnitsRequired, UnitsReceived, Insurer, PolicyNumber, ReceivedAt, Note)
VALUES (79, @cArch, 0, 1, 4, 2, N'Protect', N'TJ-2025-44810', '2026-02-20', N'TESTDATA verzekeringen');
SET @atArch = SCOPE_IDENTITY();
INSERT INTO dbo.InsuranceAttestUnit (AttestId, UnitId) VALUES (@atArch, @w1), (@atArch, @w2);
INSERT INTO dbo.InsuranceAttest (ProjectId, ContractId, Kind, Status, Insurer, PolicyNumber, ReceivedAt, Note) VALUES (79, @cArch, 1, 1, N'Protect', N'BA-AR-310', '2026-02-20', N'TESTDATA verzekeringen');

COMMIT;
SELECT 'ok' AS resultaat, @cRuw AS ruwbouw, @cDak AS dak, @cStab AS stabiliteit, @cElek AS elektriciteit, @cArch AS architect, @cMak AS makelaar, @w1 AS woning1;
