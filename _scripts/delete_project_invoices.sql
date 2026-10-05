-- =====================================================================================================
-- Verwijdert ALLE verkoopfacturen van één project (testfase). Vul @ProjectId in.
--
-- HOE GEBRUIKEN
--   1. Neem EERST een back-up (of draai dit op een kopie).
--   2. Zet @ProjectId. Draai het hele script. Standaard @DoCommit = 0: alles gebeurt in één transactie en wordt
--      TERUGGEDRAAID, je ziet enkel wat er zou gebeuren. Controleer deel 1 en de tellingen, zet daarna
--      @DoCommit = 1 en draai opnieuw.
--   3. Facturen die al in Octopus geboekt zijn (OctopusBookedAt / OctopusDocumentSequenceNr gevuld) blokkeren
--      het script. Zet @IncludeOctopusBooked = 1 enkel als dat bewust is: in Octopus blijven ze dan staan en
--      moet je ze daar zelf verwijderen/crediteren.
--
-- WAT HET DOET
--   Per factuur (Invoices.ProjectId = @ProjectId) verwijdert het: factuurregels (InvoicesDetails), bijlagen,
--   aanmaningen, e-maillog, PDF-archief, UBL, relaties (credit/vervangen) en betalingstoewijzingen
--   (PaymentAllocations; de betalingen zelf blijven bestaan, ze zijn dan niet meer toegewezen).
--   Ontkoppelt (laat bestaan): ProjectContractSlice, ProjectRegieUur, Units.LandValueInvoiceId en
--   Invoices.ReplacementOfId van facturen buiten de selectie.
--   Zet de "gefactureerd"-vlag van wijzigingsopdrachtregels (ChangeOrderDetail.Invoiced) terug op 0 voor
--   regels die op een verwijderde factuurregel stonden, zodat ze weer te factureren zijn. Schijven en
--   "bereikt"-markeringen volgen de factuurregels en worden vanzelf weer "te factureren".
--
-- WAT HET NIET DOET
--   Verwijdert geen bestanden op schijf (factuur-PDF's, bijlagen) en herstelt geen factuurnummerreeks:
--   gebruikte nummers worden niet hergebruikt. Raakt Octopus niet aan.
-- =====================================================================================================
SET NOCOUNT ON;
DECLARE @ProjectId INT = 0;               -- << VUL IN
DECLARE @DoCommit BIT = 0;                -- 0 = enkel tonen + terugdraaien, 1 = echt uitvoeren
DECLARE @IncludeOctopusBooked BIT = 0;    -- 1 = ook in Octopus geboekte facturen meenemen

IF @ProjectId <= 0 THROW 50010, 'Vul eerst @ProjectId in.', 1;

IF OBJECT_ID('tempdb..#inv') IS NOT NULL DROP TABLE #inv;
SELECT Id INTO #inv FROM dbo.Invoices WHERE ProjectId = @ProjectId;

-- ── DEEL 1: voorbeeld ──────────────────────────────────────────────────────────────────────────────
PRINT '--- Facturen die verwijderd worden ---';
SELECT i.Id, i.Filename, i.[Date], i.ClientName, i.StatusId,
       CASE WHEN i.OctopusBookedAt IS NOT NULL OR i.OctopusDocumentSequenceNr IS NOT NULL THEN 'JA' ELSE '' END AS InOctopusGeboekt
FROM dbo.Invoices i WHERE i.Id IN (SELECT Id FROM #inv) ORDER BY i.[Date], i.Id;

PRINT '--- Tellingen per tabel ---';
SELECT (SELECT COUNT(*) FROM #inv) AS Facturen,
       (SELECT COUNT(*) FROM dbo.InvoicesDetails WHERE InvoiceId IN (SELECT Id FROM #inv)) AS Regels,
       (SELECT COUNT(*) FROM dbo.InvoiceAttachments WHERE InvoiceId IN (SELECT Id FROM #inv)) AS Bijlagen,
       (SELECT COUNT(*) FROM dbo.InvoiceDunning WHERE InvoiceId IN (SELECT Id FROM #inv)) AS Aanmaningen,
       (SELECT COUNT(*) FROM dbo.InvoiceEmailLog WHERE InvoiceId IN (SELECT Id FROM #inv)) AS Emails,
       (SELECT COUNT(*) FROM dbo.PaymentAllocations WHERE InvoiceId IN (SELECT Id FROM #inv)) AS Betalingstoewijzingen,
       (SELECT COUNT(*) FROM dbo.InvoiceRelations WHERE ParentInvoiceId IN (SELECT Id FROM #inv) OR ChildInvoiceId IN (SELECT Id FROM #inv)) AS Relaties,
       (SELECT COUNT(*) FROM dbo.ConnectionAdvanceApplication WHERE InvoiceLineId IN (SELECT Id FROM dbo.InvoicesDetails WHERE InvoiceId IN (SELECT Id FROM #inv))) AS Voorschotverrekeningen;

PRINT '--- Alle foreign keys naar Invoices/InvoicesDetails (controle of dit script niets mist) ---';
SELECT fk.name AS ForeignKey, OBJECT_NAME(fk.parent_object_id) AS Tabel, OBJECT_NAME(fk.referenced_object_id) AS VerwijstNaar
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.referenced_object_id) IN ('Invoices','InvoicesDetails')
ORDER BY VerwijstNaar, Tabel;

-- ── DEEL 2: uitvoeren (in één transactie) ──────────────────────────────────────────────────────────
BEGIN TRY
    BEGIN TRAN;

    IF @IncludeOctopusBooked = 0 AND EXISTS (SELECT 1 FROM dbo.Invoices WHERE Id IN (SELECT Id FROM #inv) AND (OctopusBookedAt IS NOT NULL OR OctopusDocumentSequenceNr IS NOT NULL))
        THROW 50011, 'Er zijn facturen die al in Octopus geboekt zijn. Zet @IncludeOctopusBooked = 1 als je die bewust wilt meenemen. Niets verwijderd.', 1;

    -- wijzigingsopdrachtregels weer factureerbaar maken
    UPDATE cd SET cd.Invoiced = 0
    FROM dbo.ChangeOrderDetail cd
    WHERE cd.Id IN (SELECT ChangeOrderDetailId FROM dbo.InvoicesDetails WHERE InvoiceId IN (SELECT Id FROM #inv) AND ChangeOrderDetailId IS NOT NULL);
    PRINT CONCAT('Wijzigingsopdrachtregels terug op niet-gefactureerd: ', @@ROWCOUNT);

    UPDATE dbo.ProjectContractSlice SET InvoiceId = NULL WHERE InvoiceId IN (SELECT Id FROM #inv);
    PRINT CONCAT('ProjectContractSlice ontkoppeld: ', @@ROWCOUNT);
    UPDATE dbo.ProjectRegieUur SET InvoiceId = NULL WHERE InvoiceId IN (SELECT Id FROM #inv);
    PRINT CONCAT('ProjectRegieUur ontkoppeld: ', @@ROWCOUNT);
    UPDATE dbo.Units SET LandValueInvoiceId = NULL WHERE LandValueInvoiceId IN (SELECT Id FROM #inv);
    PRINT CONCAT('Eenheden (grondwaardefactuur) ontkoppeld: ', @@ROWCOUNT);
    UPDATE dbo.Invoices SET ReplacementOfId = NULL WHERE ReplacementOfId IN (SELECT Id FROM #inv) AND Id NOT IN (SELECT Id FROM #inv);
    -- binnen de selectie: onderlinge verwijzingen eerst loskoppelen
    UPDATE dbo.Invoices SET ReplacementOfId = NULL WHERE Id IN (SELECT Id FROM #inv) AND ReplacementOfId IS NOT NULL;

    DELETE FROM dbo.InvoiceRelations WHERE ParentInvoiceId IN (SELECT Id FROM #inv) OR ChildInvoiceId IN (SELECT Id FROM #inv);
    DELETE FROM dbo.PaymentAllocations WHERE InvoiceId IN (SELECT Id FROM #inv);
    PRINT CONCAT('Betalingstoewijzingen verwijderd: ', @@ROWCOUNT);
    DELETE FROM dbo.InvoiceAttachments WHERE InvoiceId IN (SELECT Id FROM #inv);
    DELETE FROM dbo.InvoiceDunning WHERE InvoiceId IN (SELECT Id FROM #inv);
    DELETE FROM dbo.InvoiceEmailLog WHERE InvoiceId IN (SELECT Id FROM #inv);
    DELETE FROM dbo.InvoicePdfArchive WHERE InvoiceId IN (SELECT Id FROM #inv);
    DELETE FROM dbo.InvoiceUbl WHERE InvoiceId IN (SELECT Id FROM #inv);
    -- Aansluitingsvoorschotten die op deze factuurregels verrekend zijn (FK_CAA_Line): verrekening vervalt.
    DELETE FROM dbo.ConnectionAdvanceApplication WHERE InvoiceLineId IN (SELECT Id FROM dbo.InvoicesDetails WHERE InvoiceId IN (SELECT Id FROM #inv));
    PRINT CONCAT('Voorschotverrekeningen verwijderd: ', @@ROWCOUNT);
    DELETE FROM dbo.InvoicesDetails WHERE InvoiceId IN (SELECT Id FROM #inv);
    PRINT CONCAT('Factuurregels verwijderd: ', @@ROWCOUNT);
    DELETE FROM dbo.Invoices WHERE Id IN (SELECT Id FROM #inv);
    PRINT CONCAT('Facturen verwijderd: ', @@ROWCOUNT);

    SELECT COUNT(*) AS NogAanwezig FROM dbo.Invoices WHERE ProjectId = @ProjectId;   -- moet 0 zijn

    IF @DoCommit = 1
    BEGIN
        COMMIT TRAN;
        PRINT 'KLAAR: gecommit.';
    END
    ELSE
    BEGIN
        ROLLBACK TRAN;
        PRINT 'DROOGLOOP: teruggedraaid. Zet @DoCommit = 1 om echt uit te voeren.';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT CONCAT('FOUT, alles teruggedraaid: ', ERROR_MESSAGE());
    THROW;
END CATCH;
