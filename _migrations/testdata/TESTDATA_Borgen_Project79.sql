-- ============================================================================================
-- TESTDATA_Borgen_Project79.sql — voorbeelddata voor Nacalculatie > Borgen en inhoudingen (21/10/2026)
-- ============================================================================================
-- Geen schema-migratie, enkel testdata voor db_ab5fbb_testdb, project 79 "TEST Financieel - Facturatie".
-- Vereist: migraties 105, 106 en 108 zijn gedraaid. Alles is gemarkeerd (contracten: ContractSentNote
-- 'TESTDATA borg', facturen: Notes 'TESTDATA borg') en gaat weg met TESTDATA_Borgen_Project79_Opruimen.sql.
-- Bevat: 4 contracten (financiële waarborg 5 % met 50/50, 3 % met 100 % bij VO, 5 % met standaard 50/50, bankwaarborg),
-- facturen met borg, een eindvordering met extra inhouding van 2.000 (al deels verrekend met een creditnota,
-- een tweede creditnota nog vrij), een deelvrijgave, borg zonder contract, 3 open punten, handmatig budget
-- en opleveringsdata op het project (VO 14/07/2026 bereikt, DO 14/07/2027).
-- ============================================================================================
-- Voorbeelddata voor de helpbeelden van Nacalculatie > Borgen (project 79 "TEST Financieel - Facturatie").
-- Alles is gemarkeerd: contracten via ContractSentNote = 'TESTDATA borg', facturen via Notes = 'TESTDATA borg'.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF EXISTS (SELECT 1 FROM dbo.Contract WHERE ProjectID = 79 AND ContractSentNote = N'TESTDATA borg')
BEGIN PRINT 'Testdata bestaat al, niets gedaan.'; RETURN; END

BEGIN TRAN;

-- Opleveringsdata van het project (VO bereikt, DO in de toekomst)
UPDATE dbo.Project SET DeliveryDate = '2026-07-14', DeliveryDateDef = '2027-07-14' WHERE ProjectId = 79;

DECLARE @aBouw1 INT = 37, @aBouw2 INT = 39, @aDak INT = 194, @aElek INT = 241, @aBinnen INT = 85, @aAlg INT = 46;

-- Budget (geen definitief budget: handmatige regels)
INSERT INTO dbo.ProjectBudget (ProjectID, ActivityID, Price) VALUES
 (79, @aBouw1, 120000.00), (79, @aBouw2, 208225.48), (79, @aDak, 59363.00), (79, @aElek, 27397.00), (79, @aBinnen, 144481.00), (79, @aAlg, 6000.00);

-- Contracten
DECLARE @c1 INT, @c2 INT, @c3 INT, @c4 INT, @ca1a INT, @ca1b INT, @ca2 INT, @ca3 INT, @ca4 INT;
INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, CashDiscountPercentage, CashDiscountPaymentTerm, GuaranteeType, GuaranteePercentage, GuaranteeReleaseVOPercentage, ContractSigned, ContractName, ContractSentNote)
VALUES (79, 4, 1, 21, 30, 2, 10, 2, 5, 50, 1, N'Ruwbouw', N'TESTDATA borg');
SET @c1 = SCOPE_IDENTITY();
INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, GuaranteeType, GuaranteePercentage, GuaranteeReleaseVOPercentage, ContractSigned, ContractName, ContractSentNote)
VALUES (79, 7, 0, 21, 30, 2, 3, 100, 1, N'Dakwerken', N'TESTDATA borg');
SET @c2 = SCOPE_IDENTITY();
INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, GuaranteeType, GuaranteePercentage, ContractSigned, ContractName, ContractSentNote)
VALUES (79, 9, 0, 21, 30, 2, 5, 1, N'Technieken elektro', N'TESTDATA borg');
SET @c3 = SCOPE_IDENTITY();
INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, GuaranteeType, GuaranteePercentage, ContractSigned, ContractName, ContractSentNote)
VALUES (79, 12, 0, 21, 30, 3, 5, 1, N'Binnenafwerking', N'TESTDATA borg');
SET @c4 = SCOPE_IDENTITY();

INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@c1, @aBouw1, 200000.00); SET @ca1a = SCOPE_IDENTITY();
INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@c1, @aBouw2, 136388.19); SET @ca1b = SCOPE_IDENTITY();
INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@c2, @aDak, 120000.00); SET @ca2 = SCOPE_IDENTITY();
INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@c3, @aElek, 23115.00); SET @ca3 = SCOPE_IDENTITY();
INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@c4, @aBinnen, 80000.00); SET @ca4 = SCOPE_IDENTITY();

-- Facturen contract 1 (Ruwbouw, borg 5 %)
DECLARE @i INT;
INSERT INTO dbo.IncommingInvoices (ProjectId, ContractId, ExternalId, Date, Price, DueDate, InvoiceKind, BorgInhouden, BorgPercentage, BorgBedrag, Notes)
VALUES (79, @c1, N'2025 20', '2025-06-02', 51410.68, '2025-07-02', 0, 1, 5, 2570.53, N'TESTDATA borg');
SET @i = SCOPE_IDENTITY();
INSERT INTO dbo.IncommingInvoiceDetail (IncommingInvoiceID, Description, Price, ContractActID, Type) VALUES (@i, N'VS 1', 51410.68, @ca1a, 1);

INSERT INTO dbo.IncommingInvoices (ProjectId, ContractId, ExternalId, Date, Price, DueDate, InvoiceKind, BorgInhouden, BorgPercentage, BorgBedrag, Notes)
VALUES (79, @c1, N'2025 31', '2025-09-17', 31876.16, '2025-10-17', 0, 1, 5, 1593.81, N'TESTDATA borg');
SET @i = SCOPE_IDENTITY();
INSERT INTO dbo.IncommingInvoiceDetail (IncommingInvoiceID, Description, Price, ContractActID, Type) VALUES (@i, N'VS 2', 31876.16, @ca1a, 1);

-- Eindvordering met extra inhouding van 2.000 (creditnota te ontvangen) en meerwerk
DECLARE @iEind INT;
INSERT INTO dbo.IncommingInvoices (ProjectId, ContractId, ExternalId, Date, Price, DueDate, InvoiceKind, BorgInhouden, BorgPercentage, BorgBedrag, Notes)
VALUES (79, @c1, N'202630', '2026-09-08', 29434.56, '2026-10-08', 1, 1, 5, 1371.73, N'TESTDATA borg');
SET @iEind = SCOPE_IDENTITY();
INSERT INTO dbo.IncommingInvoiceDetail (IncommingInvoiceID, Description, Price, ContractActID, Type) VALUES
 (@iEind, N'Eindvordering', 26224.92, @ca1b, 1),
 (@iEind, N'Extra kelderraam', 1667.75, @ca1b, 2),
 (@iEind, N'Extra werfafsluiting', 1541.89, @ca1b, 2);
DECLARE @inh INT;
INSERT INTO dbo.IncommingInvoiceInhouding (IncommingInvoiceId, Bedrag, Reden, Toelichting, CreatedBy)
VALUES (@iEind, 2000.00, 1, N'Werfafsluiting dubbel aangerekend', N'testdata');
SET @inh = SCOPE_IDENTITY();

-- Creditnota's: CN-2026-017 is al deels verrekend, CN-2026-021 is nog vrij
DECLARE @cn1 INT, @cn2 INT;
INSERT INTO dbo.IncommingInvoices (ProjectId, ContractId, ExternalId, Date, Price, InvoiceKind, BorgInhouden, Notes)
VALUES (79, @c1, N'CN-2026-017', '2026-10-02', 1200.00, 2, 0, N'TESTDATA borg');
SET @cn1 = SCOPE_IDENTITY();
INSERT INTO dbo.IncommingInvoiceDetail (IncommingInvoiceID, Description, Price, ContractActID, Type) VALUES (@cn1, N'Creditnota werfafsluiting', 1200.00, @ca1b, 1);
INSERT INTO dbo.IncommingInvoices (ProjectId, ContractId, ExternalId, Date, Price, InvoiceKind, BorgInhouden, Notes)
VALUES (79, @c1, N'CN-2026-021', '2026-10-18', 500.00, 2, 0, N'TESTDATA borg');
SET @cn2 = SCOPE_IDENTITY();
INSERT INTO dbo.IncommingInvoiceDetail (IncommingInvoiceID, Description, Price, ContractActID, Type) VALUES (@cn2, N'Creditnota', 500.00, @ca1b, 1);
INSERT INTO dbo.IncommingInvoiceInhoudingAfhandeling (InhoudingId, Datum, Soort, Bedrag, BorgBedrag, CreditnotaInvoiceId, Opmerking, CreatedBy)
VALUES (@inh, '2026-10-02', 2, 1200.00, 0, @cn1, NULL, N'testdata');

-- Een deelvrijgave bij contract 1
INSERT INTO dbo.ContractBorgVrijgave (ContractId, Fase, Datum, Bedrag, Opmerking, CreatedBy)
VALUES (@c1, 3, '2026-07-20', 1500.00, N'deelvrijgave bij de voorlopige oplevering', N'testdata');

-- Contract 2 (Dakwerken, borg 3 %, 100 % bij VO)
INSERT INTO dbo.IncommingInvoices (ProjectId, ContractId, ExternalId, Date, Price, DueDate, InvoiceKind, BorgInhouden, BorgPercentage, BorgBedrag, Notes)
VALUES (79, @c2, N'D-2026-004', '2026-03-10', 129333.33, '2026-04-09', 0, 1, 3, 3880.00, N'TESTDATA borg');
SET @i = SCOPE_IDENTITY();
INSERT INTO dbo.IncommingInvoiceDetail (IncommingInvoiceID, Description, Price, ContractActID, Type) VALUES (@i, N'Dakbedekking', 129333.33, @ca2, 1);

-- Contract 3 (Technieken, borg 5 %, 50/50)
INSERT INTO dbo.IncommingInvoices (ProjectId, ContractId, ExternalId, Date, Price, DueDate, InvoiceKind, BorgInhouden, BorgPercentage, BorgBedrag, Notes)
VALUES (79, @c3, N'2026-118', '2026-08-22', 7670.00, '2026-09-21', 0, 1, 5, 383.50, N'TESTDATA borg');
SET @i = SCOPE_IDENTITY();
INSERT INTO dbo.IncommingInvoiceDetail (IncommingInvoiceID, Description, Price, ContractActID, Type) VALUES (@i, N'Elektrische installatie', 7670.00, @ca3, 1);
INSERT INTO dbo.IncommingInvoices (ProjectId, ContractId, ExternalId, Date, Price, DueDate, InvoiceKind, BorgInhouden, BorgPercentage, BorgBedrag, Notes)
VALUES (79, @c3, N'2026-131', '2026-09-30', 8400.00, '2026-10-30', 0, 1, 5, 420.00, N'TESTDATA borg');
SET @i = SCOPE_IDENTITY();
INSERT INTO dbo.IncommingInvoiceDetail (IncommingInvoiceID, Description, Price, ContractActID, Type) VALUES (@i, N'Elektrische installatie, tweede schijf', 8400.00, @ca3, 1);

-- Borg zonder contract (leverancier 13, 100 % bij VO)
INSERT INTO dbo.IncommingInvoices (ProjectId, CompanyId, ExternalId, Date, Price, InvoiceKind, BorgInhouden, BorgPercentage, BorgBedrag, BorgVoPercentage, Notes)
VALUES (79, 13, N'T-0412', '2026-05-12', 8250.00, 0, 1, 5, 412.50, 100, N'TESTDATA borg');
SET @i = SCOPE_IDENTITY();
INSERT INTO dbo.IncommingInvoiceDetail (IncommingInvoiceID, Description, Price, ActID, Type) VALUES (@i, N'Diverse werken', 8250.00, @aAlg, 4);

-- Open punten voor Dakwerken (leverancier 7)
INSERT INTO dbo.ConstructionIssue (ProjectId, Title, LocationText, CategoryId, IssueType, IssuePhase, Priority, Status, ResponsiblePartyType, ResponsiblePartyId)
VALUES (79, N'Voeg te breed boven raam', N'Dak', 1, 0, 3, 1, 10, 0, 7),
       (79, N'Waterdichting kelder', N'Kelder', 1, 0, 3, 1, 10, 0, 7),
       (79, N'Leiding door brandwand', N'Technische ruimte', 1, 0, 3, 1, 10, 0, 7);

COMMIT;
SELECT 'ok' AS resultaat, @c1 AS contract1, @c2 AS contract2, @c3 AS contract3, @c4 AS contract4, @iEind AS eindvordering;
