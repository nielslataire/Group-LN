-- ============================================================================================
-- TESTDATA_Financieel_Part2.sql — extra scenario's bovenop TESTDATA_Financieel.sql
-- ============================================================================================
-- Testdata, geen schema-migratie. Vult het bestaande testproject "TEST Financieel - Facturatie"
-- (ProjectID 79) aan met twee accounts die de nieuwe functionaliteit van 2026-09-30 dekken:
--   - WO_Test_NietsTeFactureren: een eenheid zonder betalingsgroep-koppeling (net als Lot 3/Lot 4
--     in "Verkaveling Keerstraat") — komt op de pagina met "Niets te factureren".
--   - WO_Test_NogNietBereikt: een NIEUWE betalingsgroep van 3 schijven waarvan de laatste
--     (structureel de eindafrekening) nog Invoicable=0 staat — toont het derde balkje
--     ("nog niet vrijgegeven") en bewijst dat er nog geen eindafrekening-schijf aangeboden wordt
--     zolang die laatste schijf niet vrijgegeven is.
-- De twee bestaande accounts (WO_Test_HoofdEigenaar/-MedeEigenaar, ClientAccount 1867/1868)
-- dekken al: een geblokkeerde (niet-ondertekende) WO die de eindafrekening tegenhoudt, een
-- ondertekende WO die er verplicht wordt bijgetrokken, en de 60/40-mede-eigenaarsplitsing.
--
-- Idempotent-check: als WO_Test_NietsTeFactureren al bestaat, doet dit script niets.
-- ============================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM ClientAccount WHERE Name = N'WO_Test_NietsTeFactureren')
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

BEGIN TRANSACTION;

DECLARE @AccountC INT, @AccountD INT, @UnitC INT, @UnitD INT, @GroupD INT, @StageD1 INT, @StageD2 INT, @StageD3 INT;

-- 1) Account C: eenheid zonder betalingsgroep-koppeling op de bouwwaarde (PaymentGroupId = NULL,
--    zelfde patroon als bestaande "losse" eenheden elders in de databank) — telt dus nergens in mee
--    en het account krijgt een lege "Niets te factureren"-kaart.
INSERT INTO ClientAccount (Name, Forename, Salutation, Street, Housenumber, PostalCodeID, DateSalesAgreement, DateDeedOfSale, OwnerTypeId)
VALUES (N'WO_Test_NietsTeFactureren', N'Karel', N'Dhr', N'Testlaan', '3', 2576, '2026-01-01', '2026-01-15', 4);
SET @AccountC = SCOPE_IDENTITY();

INSERT INTO Units (Name, TypeId, ProjectId, ClientAccountID, ConstructionValueSold, IsLink, IsOption)
VALUES (N'TEST Lot C', 2, @ProjectId, @AccountC, 220000, 0, 0);
SET @UnitC = SCOPE_IDENTITY();

INSERT INTO UnitConstructionValue (Value, ValueSold, PaymentGroupId, UnitId)
VALUES (220000, 220000, NULL, @UnitC);

-- 2) Account D: nieuwe groep met 3 schijven, de laatste (de facto de eindafrekening) nog niet
--    vrijgegeven (Invoicable = 0) — toont het "nog niet vrijgegeven"-segment en bewijst dat er nu
--    geen enkele schijf als eindafrekening aangeboden wordt.
INSERT INTO ClientAccount (Name, Forename, Salutation, Street, Housenumber, PostalCodeID, DateSalesAgreement, DateDeedOfSale, OwnerTypeId)
VALUES (N'WO_Test_NogNietBereikt', N'Sofie', N'Mevr', N'Testlaan', '4', 2576, '2026-01-01', '2026-01-15', 4);
SET @AccountD = SCOPE_IDENTITY();

INSERT INTO Units (Name, TypeId, ProjectId, ClientAccountID, ConstructionValueSold, IsLink, IsOption)
VALUES (N'TEST Lot D', 2, @ProjectId, @AccountD, 250000, 0, 0);
SET @UnitD = SCOPE_IDENTITY();

INSERT INTO InvoicingPaymentGroup (Name, ProjectId, VatPercentage)
VALUES (N'TEST Woning 21% BTW - D', @ProjectId, 21);
SET @GroupD = SCOPE_IDENTITY();

INSERT INTO InvoicingPaymentStages (Name, Percentage, Invoicable, GroupId, VatPercentage) VALUES (N'TEST - Na het uitzetten', 15, 1, @GroupD, 21);
SET @StageD1 = SCOPE_IDENTITY();
INSERT INTO InvoicingPaymentStages (Name, Percentage, Invoicable, GroupId, VatPercentage) VALUES (N'TEST - Na de ruwbouw', 25, 1, @GroupD, 21);
SET @StageD2 = SCOPE_IDENTITY();
INSERT INTO InvoicingPaymentStages (Name, Percentage, Invoicable, GroupId, VatPercentage) VALUES (N'TEST - Bij de oplevering (eindafrekening)', 10, 0, @GroupD, 21);
SET @StageD3 = SCOPE_IDENTITY();

INSERT INTO UnitConstructionValue (Value, ValueSold, PaymentGroupId, UnitId)
VALUES (250000, 250000, @GroupD, @UnitD);

COMMIT TRANSACTION;

PRINT 'Klaar.';
SELECT @AccountC AS AccountC, @AccountD AS AccountD;

-- ============================================================================================
-- Verwachte resultaat op /Projecten/InvoicingV2?projectid=79 (na Part 1 + Part 2):
--   Te factureren: nu 3 accounts met open posten (Jan, Piet, Sofie) + 1 zonder (Karel, "Niets te
--                  factureren"). Sofie's kaart toont 2 open schijven (€37.500 + €62.500) en een
--                  verloopbalk met een grijs "nog niet vrijgegeven"-stukje (10%) — geen van haar
--                  rijen heeft het EINDAFREKENING-label, want de echte laatste schijf staat nog dicht.
--   Eindafrekening-test: vink bij Jan (HoofdEigenaar) zijn laatste schijf ("Na de funderingen") aan
--                  → moet geweigerd worden met een toast, want WO-41 is nog niet ondertekend. Los dat
--                  op (of test bij Piet, waar WO-40 al ondertekend is) → bij Piet trekt het aanvinken
--                  van zijn laatste schijf WO-40 automatisch en niet-uitvinkbaar mee aan.
-- ============================================================================================

-- ── Opruimen ──────────────────────────────────────────────────────────────────────────────────
-- BEGIN TRANSACTION;
-- DELETE ucv FROM UnitConstructionValue ucv JOIN Units u ON u.Id = ucv.UnitId JOIN ClientAccount ca ON ca.Id = u.ClientAccountID WHERE ca.Name IN (N'WO_Test_NietsTeFactureren', N'WO_Test_NogNietBereikt');
-- DELETE s FROM InvoicingPaymentStages s WHERE s.GroupId IN (SELECT Id FROM InvoicingPaymentGroup WHERE Name = N'TEST Woning 21% BTW - D');
-- DELETE FROM InvoicingPaymentGroup WHERE Name = N'TEST Woning 21% BTW - D';
-- DELETE u FROM Units u JOIN ClientAccount ca ON ca.Id = u.ClientAccountID WHERE ca.Name IN (N'WO_Test_NietsTeFactureren', N'WO_Test_NogNietBereikt');
-- DELETE FROM ClientAccount WHERE Name IN (N'WO_Test_NietsTeFactureren', N'WO_Test_NogNietBereikt');
-- COMMIT TRANSACTION;
