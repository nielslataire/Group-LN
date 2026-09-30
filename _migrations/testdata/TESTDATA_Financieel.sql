-- ============================================================================================
-- TESTDATA_Financieel.sql — testproject voor de gl-v2 Facturatie-pagina (InvoicingV2)
-- ============================================================================================
-- Dit is GEEN schema-migratie (hoort dus niet in _migrations/NNN_*.sql) — enkel testdata voor
-- db_ab5fbb_testdb. Bouwt één nieuw project "TEST Financieel - Facturatie" met:
--   - Account A (WO_Test_HoofdEigenaar): 1 eenheid, 2 open schijven, 1 GEBLOKKEERDE WO
--     (Invoiceable=1, DateAgreement=NULL — wacht op ondertekening, nog geen dossier gestart).
--   - Account B (WO_Test_MedeEigenaar): 1 eenheid, 2 open schijven, mede-eigenaar op 40%
--     (hoofdaccount dus 60%), 1 KLARE WO (Invoiceable=1, DateAgreement gezet — al ondertekend).
-- Referentie-data (IssuerCompany 4, PostalCode 2608, Activity 168, Company 1247, ProjectType 1,
-- ContractType 1) is hergebruikt van bestaande, al werkende rijen (project 69 "Verkaveling
-- Keerstraat" resp. change order 37) — nagekeken via read-only SELECT, niet geraden.
--
-- Idempotent-check: als het project al bestaat, doet dit script niets (géén tweede kopie).
-- Onderaan staat, in commentaar, hoe je alles weer opruimt.
-- ============================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM Project WHERE ProjectName = 'TEST Financieel - Facturatie')
BEGIN
    PRINT 'Bestaat al — niets gedaan. Zie de opruim-sectie onderaan als je opnieuw wil beginnen.';
    RETURN;
END

BEGIN TRANSACTION;

DECLARE @ProjectId INT, @AccountA INT, @AccountB INT, @CoOwnerB INT,
        @UnitA INT, @UnitB INT, @GroupId INT, @Stage1 INT, @Stage2 INT,
        @ContractId INT, @ContractActivityId INT, @CoOpen INT, @CoBlocked INT;

-- 1) Project ------------------------------------------------------------------------------
INSERT INTO Project (ProjectName, Slug, PostalCodeID, StatusID, ProjectType, ContractType,
                      IssuerCompanyIdBuilder, IsCoordinationProject, IsPublished, IsOnlyCoordinationProject)
VALUES ('TEST Financieel - Facturatie', 'test-financieel-facturatie', 2608, 2, 1, 1,
        4, 0, 0, 0);
SET @ProjectId = SCOPE_IDENTITY();

-- 2) Klantenaccounts (donor: ClientAccount 1768 se opbouw) --------------------------------
INSERT INTO ClientAccount (Name, Forename, Salutation, Street, Housenumber, PostalCodeID,
                            DateSalesAgreement, DateDeedOfSale, OwnerTypeId)
VALUES (N'WO_Test_HoofdEigenaar', N'Jan', N'Dhr', N'Testlaan', '1', 2576,
        '2026-01-01', '2026-01-15', 4);
SET @AccountA = SCOPE_IDENTITY();

INSERT INTO ClientAccount (Name, Forename, Salutation, Street, Housenumber, PostalCodeID,
                            DateSalesAgreement, DateDeedOfSale, OwnerTypeId)
VALUES (N'WO_Test_MedeEigenaar', N'Piet', N'Dhr', N'Testlaan', '2', 2576,
        '2026-01-01', '2026-01-15', 4);
SET @AccountB = SCOPE_IDENTITY();

-- Mede-eigenaar op account B: 40% — hoofdaccount krijgt dus automatisch 60% (100 - Σ mede-eig.%).
INSERT INTO ClientContacts (Name, Forename, Salutation, IsCoOwner, CoOwnerPercentage, ClientAccountID)
VALUES (N'WO_Test_MedeEigenaarContact', N'Marie', N'Mevr', 1, 40, @AccountB);
SET @CoOwnerB = SCOPE_IDENTITY();

-- 3) Eenheden (donor: Units 418, TypeId=2 "Lot") ------------------------------------------
INSERT INTO Units (Name, TypeId, ProjectId, ClientAccountID, ConstructionValueSold, IsLink, IsOption)
VALUES (N'TEST Lot A', 2, @ProjectId, @AccountA, 200000, 0, 0);
SET @UnitA = SCOPE_IDENTITY();

INSERT INTO Units (Name, TypeId, ProjectId, ClientAccountID, ConstructionValueSold, IsLink, IsOption)
VALUES (N'TEST Lot B', 2, @ProjectId, @AccountB, 300000, 0, 0);
SET @UnitB = SCOPE_IDENTITY();

-- 4) Betalingsgroep + 2 open, factureerbare schijven (donor: groep 51/stage 310) ----------
INSERT INTO InvoicingPaymentGroup (Name, ProjectId, VatPercentage)
VALUES (N'TEST Woning 21% BTW', @ProjectId, 21);
SET @GroupId = SCOPE_IDENTITY();

INSERT INTO InvoicingPaymentStages (Name, Percentage, Invoicable, GroupId, VatPercentage)
VALUES (N'TEST - Na het uitzetten', 10, 1, @GroupId, 21);
SET @Stage1 = SCOPE_IDENTITY();

INSERT INTO InvoicingPaymentStages (Name, Percentage, Invoicable, GroupId, VatPercentage)
VALUES (N'TEST - Na de funderingen', 20, 1, @GroupId, 21);
SET @Stage2 = SCOPE_IDENTITY();

-- 5) Bouwwaarde per eenheid, gekoppeld aan de nieuwe groep (dit was de ontbrekende Include-bug
--    in GetProjectInvoicableUnits die op 2026-09-29 gefixt is — zonder deze rij blijft elke
--    schijf op €0 staan). ---------------------------------------------------------------
INSERT INTO UnitConstructionValue (Value, ValueSold, PaymentGroupId, UnitId)
VALUES (200000, 200000, @GroupId, @UnitA);
INSERT INTO UnitConstructionValue (Value, ValueSold, PaymentGroupId, UnitId)
VALUES (300000, 300000, @GroupId, @UnitB);

-- 6) Contract + ContractActivity (container-lijn voor de WO's, donor: ContractActivity 205,
--    ActivityID 168, Contract.CompanyID 1247 — beide bestaande, geldige rijen). ------------
INSERT INTO Contract (ProjectID, CompanyID, VatPercentage, CashDiscount)
VALUES (@ProjectId, 1247, 21, 0);
SET @ContractId = SCOPE_IDENTITY();

INSERT INTO ContractActivity (ContractID, ActivityID, Price, Description)
VALUES (@ContractId, 168, 5000, N'TEST — meerwerken/minwerken');
SET @ContractActivityId = SCOPE_IDENTITY();

-- 7) Twee wijzigingsopdrachten: één klaar om te factureren (getekend), één geblokkeerd
--    (nog niet getekend — verschijnt in het tabblad "Geblokkeerd" met een link naar
--    "Ondertekening starten"; een SigningCase erbij zetten laat dit script bewust niet doen,
--    dat test je best live door effectief op die knop te klikken). ------------------------
INSERT INTO ChangeOrder (ClientAccountID, Description, Date, ExpirationDate, Invoiceable, DateAgreement, ContractActivityID)
VALUES (@AccountB, N'TEST — meerwerk, klaar om te factureren', '2026-09-01', '2026-12-31', 1, '2026-09-15', @ContractActivityId);
SET @CoOpen = SCOPE_IDENTITY();
INSERT INTO ChangeOrderDetail (ChangeOrderID, Description, Number, Price, Commission, Invoicable, VatPercentage)
VALUES (@CoOpen, N'TEST — extra stopcontacten', 1, 850, 0, 1, 21);

INSERT INTO ChangeOrder (ClientAccountID, Description, Date, ExpirationDate, Invoiceable, DateAgreement, ContractActivityID)
VALUES (@AccountA, N'TEST — meerwerk, wacht op handtekening', '2026-09-01', '2026-12-31', 1, NULL, @ContractActivityId);
SET @CoBlocked = SCOPE_IDENTITY();
INSERT INTO ChangeOrderDetail (ChangeOrderID, Description, Number, Price, Commission, Invoicable, VatPercentage)
VALUES (@CoBlocked, N'TEST — verplaatsing binnendeur', 1, 425, 0, 1, 21);

COMMIT TRANSACTION;

PRINT 'Klaar. Nieuw ProjectId:';
SELECT @ProjectId AS NewProjectId;

-- ============================================================================================
-- Verwachte resultaat op /Projecten/InvoicingV2?projectid=<NewProjectId>:
--   Te factureren: 2 accounts, elk 2 open schijven (Lot A: €20.000+€40.000, Lot B: €30.000+
--                  €60.000) + de al-getekende WO (€850) op account B (mede-eigenaar 40/60%).
--   Geblokkeerd:   1 WO (€425) op account A, "Nog niet aangeboden om te tekenen".
--   Gefactureerd:  leeg (dit script maakt bewust geen facturen aan).
-- ============================================================================================

-- ── Opruimen (uncomment en @ProjectId invullen, of vervang door de NewProjectId hierboven) ──
-- BEGIN TRANSACTION;
-- DECLARE @DelProjectId INT = (SELECT ProjectID FROM Project WHERE ProjectName = 'TEST Financieel - Facturatie');
-- DELETE cod FROM ChangeOrderDetail cod JOIN ChangeOrder co ON co.ID = cod.ChangeOrderID
--     JOIN ContractActivity ca ON ca.Id = co.ContractActivityID JOIN Contract c ON c.ID = ca.ContractID WHERE c.ProjectID = @DelProjectId;
-- DELETE co FROM ChangeOrder co JOIN ContractActivity ca ON ca.Id = co.ContractActivityID JOIN Contract c ON c.ID = ca.ContractID WHERE c.ProjectID = @DelProjectId;
-- DELETE ca FROM ContractActivity ca JOIN Contract c ON c.ID = ca.ContractID WHERE c.ProjectID = @DelProjectId;
-- DELETE FROM Contract WHERE ProjectID = @DelProjectId;
-- DELETE ucv FROM UnitConstructionValue ucv JOIN Units u ON u.Id = ucv.UnitId WHERE u.ProjectId = @DelProjectId;
-- DELETE s FROM InvoicingPaymentStages s JOIN InvoicingPaymentGroup g ON g.Id = s.GroupId WHERE g.ProjectId = @DelProjectId;
-- DELETE FROM InvoicingPaymentGroup WHERE ProjectId = @DelProjectId;
-- DELETE FROM Units WHERE ProjectId = @DelProjectId;
-- DELETE cc FROM ClientContacts cc JOIN ClientAccount ca ON ca.Id = cc.ClientAccountID WHERE ca.Name IN (N'WO_Test_HoofdEigenaar', N'WO_Test_MedeEigenaar');
-- DELETE FROM ClientAccount WHERE Name IN (N'WO_Test_HoofdEigenaar', N'WO_Test_MedeEigenaar');
-- DELETE FROM Project WHERE ProjectID = @DelProjectId;
-- COMMIT TRANSACTION;
