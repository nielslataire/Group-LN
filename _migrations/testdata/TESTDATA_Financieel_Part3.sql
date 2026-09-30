-- ============================================================================================
-- TESTDATA_Financieel_Part3.sql — extra scenario's bovenop Part 1 + Part 2
-- ============================================================================================
-- Testdata, geen schema-migratie. Dekt de twee gevallen die op 2026-09-30 bij Betalingsschijven
-- (PaymentStagesV2) gevraagd zijn:
--   - Account E (WO_Test_AkteNietVerleden): eenheid VERKOCHT (ClientAccountID gezet, DateDeedOfSale
--     bewust NULL) maar de UnitConstructionValue-rij voor "TEST Woning 21% BTW" heeft nog geen
--     ValueSold — akte nog niet verleden/aktedatum nog niet ingevuld. Zit in DEZELFDE groep als
--     Account A/B, dus komt er als extra kolom bij op de bestaande kaart en toont "—" in elke
--     schijf-rij (PaymentStagesV2.PaymentStagesV2: state "not-invoicable").
--   - Lot A se eerste schijf ("TEST - Na het uitzetten") wordt als al GEFACTUREERD gezet — een
--     Invoices + InvoicesDetails-rij (LineType='Stages'), toont het groene vinkje i.p.v. de goud-bol.
--
-- Idempotent-check: als Account E al bestaat, doet dit script niets.
-- ============================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM ClientAccount WHERE Name = N'WO_Test_AkteNietVerleden')
BEGIN
    PRINT 'Bestaat al — niets gedaan.';
    RETURN;
END

DECLARE @ProjectId INT = (SELECT ProjectID FROM Project WHERE ProjectName = N'TEST Financieel - Facturatie');
IF @ProjectId IS NULL
BEGIN
    PRINT 'TEST Financieel - Facturatie niet gevonden — voer eerst TESTDATA_Financieel.sql uit.';
    RETURN;
END

DECLARE @GroupId INT = (SELECT Id FROM InvoicingPaymentGroup WHERE ProjectId = @ProjectId AND Name = N'TEST Woning 21% BTW');
DECLARE @AccountA INT = (SELECT Id FROM ClientAccount WHERE Name = N'WO_Test_HoofdEigenaar');
DECLARE @UnitA INT = (SELECT Id FROM Units WHERE ProjectId = @ProjectId AND Name = N'TEST Lot A');
DECLARE @Stage1 INT = (SELECT Id FROM InvoicingPaymentStages WHERE GroupId = @GroupId AND Name = N'TEST - Na het uitzetten');
IF @GroupId IS NULL OR @AccountA IS NULL OR @UnitA IS NULL OR @Stage1 IS NULL
BEGIN
    PRINT 'Basisdata (groep/account/eenheid/schijf) niet gevonden — voer eerst TESTDATA_Financieel.sql uit.';
    RETURN;
END

BEGIN TRANSACTION;

-- 1) Account E: verkocht, maar akte nog niet verleden.
DECLARE @AccountE INT, @UnitE INT;
INSERT INTO ClientAccount (Name, Forename, Salutation, Street, Housenumber, PostalCodeID, DateSalesAgreement, DateDeedOfSale, OwnerTypeId)
VALUES (N'WO_Test_AkteNietVerleden', N'Anna', N'Mevr', N'Testlaan', '5', 2576, '2026-01-01', NULL, 4);
SET @AccountE = SCOPE_IDENTITY();

INSERT INTO Units (Name, TypeId, ProjectId, ClientAccountID, ConstructionValueSold, IsLink, IsOption)
VALUES (N'TEST Lot E', 2, @ProjectId, @AccountE, 240000, 0, 0);
SET @UnitE = SCOPE_IDENTITY();

INSERT INTO UnitConstructionValue (Value, ValueSold, PaymentGroupId, UnitId)
VALUES (240000, NULL, @GroupId, @UnitE);

-- 2) Lot A se eerste schijf: al gefactureerd. ClientId_ClientAccount is een BEREKENDE kolom
--    ("case when ClientType=1 then ClientId end", zie cpmRunningContext.cs) — je zet dus ClientType=1
--    + ClientId, niet die kolom zelf (die naam bestaat ook niet letterlijk in de databank).
DECLARE @InvoiceId INT;
INSERT INTO Invoices (Filename, Date, ClientType, ClientId, ClientName, IssuerCompanyId, ProjectId, StatusId, Prepaid)
VALUES (NULL, '2026-09-15', 1, @AccountA, N'WO_Test_HoofdEigenaar', 4, @ProjectId, 2, 0);
SET @InvoiceId = SCOPE_IDENTITY();

INSERT INTO InvoicesDetails (InvoiceId, PaymentStageId, UnitId, Text, VatPercentage, Price, LineType)
VALUES (@InvoiceId, @Stage1, @UnitA, N'TEST - Na het uitzetten', 21, 20000, N'Stages');

COMMIT TRANSACTION;

PRINT 'Klaar.';
SELECT @AccountE AS AccountE, @InvoiceId AS InvoiceId;

-- ============================================================================================
-- Verwachte resultaat op /Projecten/PaymentStagesV2?projectid=<ProjectId van "TEST Financieel -
-- Facturatie">:
--   Lot A — "TEST - Na het uitzetten": groen vinkje (gefactureerd) i.p.v. de goud-bol.
--   Lot E — nieuwe kolom op de kaart "TEST Woning 21% BTW": elke schijf-rij toont "—" met tooltip
--           "Akte nog niet verleden" (geen bol, niet aanklikbaar), net als "nog niet verkocht" maar
--           met een andere tooltip.
-- ============================================================================================

-- ── Opruimen ──────────────────────────────────────────────────────────────────────────────────
-- BEGIN TRANSACTION;
-- DELETE FROM InvoicesDetails WHERE InvoiceId IN (
--     SELECT Id FROM Invoices WHERE Date = '2026-09-15' AND Filename IS NULL AND StatusId = 2 AND ClientType = 1
--         AND ClientId = (SELECT Id FROM ClientAccount WHERE Name = N'WO_Test_HoofdEigenaar'));
-- DELETE FROM Invoices WHERE Date = '2026-09-15' AND Filename IS NULL AND StatusId = 2 AND ClientType = 1
--     AND ClientId = (SELECT Id FROM ClientAccount WHERE Name = N'WO_Test_HoofdEigenaar');
-- DELETE ucv FROM UnitConstructionValue ucv JOIN Units u ON u.Id = ucv.UnitId JOIN ClientAccount ca ON ca.Id = u.ClientAccountID WHERE ca.Name = N'WO_Test_AkteNietVerleden';
-- DELETE u FROM Units u JOIN ClientAccount ca ON ca.Id = u.ClientAccountID WHERE ca.Name = N'WO_Test_AkteNietVerleden';
-- DELETE FROM ClientAccount WHERE Name = N'WO_Test_AkteNietVerleden';
-- COMMIT TRANSACTION;
