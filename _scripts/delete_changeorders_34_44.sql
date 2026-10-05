-- =====================================================================================================
-- Eenmalig: offertes/wijzigingsopdrachten 34 t/m 44 verwijderen (opdracht Niels, okt. 2026).
--
-- HOE GEBRUIKEN
--   1. Neem EERST een back-up (of draai dit op een kopie).
--   2. Draai het hele script. Het staat standaard op @DoCommit = 0: alles gebeurt in één transactie en wordt
--      TERUGGEDRAAID, je ziet enkel wat er zou gebeuren. Controleer de resultaten van deel 1 en de tellingen
--      onderaan. Zet daarna @DoCommit = 1 en draai opnieuw om het echt te doen.
--
-- WAT HET DOET
--   Verwijdert de ChangeOrder-rijen met Id 34..44 samen met hun regels (ChangeOrderDetail) en betaalschijven
--   (ChangeOrderPaymentTerm). Ontkoppelt (laat bestaan): inkoopfactuurregels (IncommingInvoiceDetail),
--   documenten (ProjectDocs) en afgeleide wijzigingsopdrachten buiten de lijst (SourceChangeOrderId).
--   Weigert (THROW + rollback) als er verkoopfactuurregels aan de regels/betaalschijven hangen: een
--   gefactureerde post verwijder je niet zomaar.
--
-- WAT HET BEWUST NIET DOET
--   De ondertekeningstrail (SigningCase / SigningEvent / SigningParty / SigningDocument) blijft onaangeroerd.
--   SigningEvent is een onwijzigbaar auditlogboek (een trigger weigert DELETE, de FK heeft geen cascade); dat
--   omzeilen door de trigger uit te zetten doet dit script niet. De dossiers verwijzen via
--   DocumentType + SourceEntityId naar de offerte/WO zonder echte FK, dus ze blokkeren dit verwijderen niet.
--   Ze blijven als "verweesd" bewijs bestaan. Deel 1 toont ze; staat er een lopend dossier (Status 0/1)
--   tussen, trek dat dan eerst in via de applicatie vóór je dit script commit.
-- =====================================================================================================
SET NOCOUNT ON;
DECLARE @DoCommit BIT = 0;   -- 0 = enkel tonen + terugdraaien, 1 = echt uitvoeren

IF OBJECT_ID('tempdb..#ids') IS NOT NULL DROP TABLE #ids;
CREATE TABLE #ids (Id INT PRIMARY KEY);
INSERT INTO #ids (Id) SELECT n FROM (VALUES (34),(35),(36),(37),(38),(39),(40),(41),(42),(43),(44)) v(n);

-- ── DEEL 1: voorbeeld ──────────────────────────────────────────────────────────────────────────────
PRINT '--- Rijen die verwijderd worden (ontbrekende Id''s bestaan niet) ---';
SELECT i.Id AS GevraagdId, c.Id AS Gevonden, c.IsQuote, c.ClientAccountId, c.SourceChangeOrderId
FROM #ids i LEFT JOIN dbo.ChangeOrder c ON c.Id = i.Id ORDER BY i.Id;

PRINT '--- Regels en betaalschijven die mee verdwijnen ---';
SELECT (SELECT COUNT(*) FROM dbo.ChangeOrderDetail d WHERE d.ChangeOrderId IN (SELECT Id FROM #ids)) AS Regels,
       (SELECT COUNT(*) FROM dbo.ChangeOrderPaymentTerm t WHERE t.ChangeOrderId IN (SELECT Id FROM #ids)) AS Betaalschijven;

PRINT '--- BLOKKEERT: verkoopfactuurregels aan deze regels/betaalschijven (moet leeg zijn) ---';
SELECT inv.Id AS InvoicesDetailsId, inv.ChangeOrderDetailId, inv.ChangeOrderPaymentTermId
FROM dbo.InvoicesDetails inv
WHERE inv.ChangeOrderDetailId IN (SELECT d.Id FROM dbo.ChangeOrderDetail d WHERE d.ChangeOrderId IN (SELECT Id FROM #ids))
   OR inv.ChangeOrderPaymentTermId IN (SELECT t.Id FROM dbo.ChangeOrderPaymentTerm t WHERE t.ChangeOrderId IN (SELECT Id FROM #ids));

PRINT '--- Wordt ontkoppeld (blijft bestaan): inkoopfactuurregels ---';
SELECT Id, ChangeOrderId FROM dbo.IncommingInvoiceDetail WHERE ChangeOrderId IN (SELECT Id FROM #ids);

PRINT '--- Wordt ontkoppeld (blijft bestaan): documenten ---';
SELECT Id, ChangeOrderId FROM dbo.ProjectDocs WHERE ChangeOrderId IN (SELECT Id FROM #ids);

PRINT '--- Wordt ontkoppeld: afgeleide wijzigingsopdrachten buiten de lijst ---';
SELECT Id, SourceChangeOrderId FROM dbo.ChangeOrder WHERE SourceChangeOrderId IN (SELECT Id FROM #ids) AND Id NOT IN (SELECT Id FROM #ids);

PRINT '--- Ondertekeningsdossiers die BLIJVEN (controleer DocumentType; Status 0/1 = lopend, eerst intrekken) ---';
SELECT Id, DocumentType, SourceEntityId, Status, Title FROM dbo.SigningCase WHERE SourceEntityId IN (SELECT Id FROM #ids) ORDER BY SourceEntityId;

PRINT '--- Alle foreign keys naar ChangeOrder/-Detail/-PaymentTerm (voor het geval er een tabel ontbreekt in dit script) ---';
SELECT fk.name AS ForeignKey, OBJECT_NAME(fk.parent_object_id) AS Tabel, OBJECT_NAME(fk.referenced_object_id) AS VerwijstNaar
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.referenced_object_id) IN ('ChangeOrder','ChangeOrderDetail','ChangeOrderPaymentTerm')
ORDER BY VerwijstNaar, Tabel;

-- ── DEEL 2: uitvoeren (in één transactie) ──────────────────────────────────────────────────────────
BEGIN TRY
    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.InvoicesDetails inv
               WHERE inv.ChangeOrderDetailId IN (SELECT d.Id FROM dbo.ChangeOrderDetail d WHERE d.ChangeOrderId IN (SELECT Id FROM #ids))
                  OR inv.ChangeOrderPaymentTermId IN (SELECT t.Id FROM dbo.ChangeOrderPaymentTerm t WHERE t.ChangeOrderId IN (SELECT Id FROM #ids)))
        THROW 50001, 'Er hangen verkoopfactuurregels aan deze offertes/wijzigingsopdrachten. Niets verwijderd.', 1;

    UPDATE dbo.IncommingInvoiceDetail SET ChangeOrderId = NULL WHERE ChangeOrderId IN (SELECT Id FROM #ids);
    PRINT CONCAT('Inkoopfactuurregels ontkoppeld: ', @@ROWCOUNT);

    UPDATE dbo.ProjectDocs SET ChangeOrderId = NULL WHERE ChangeOrderId IN (SELECT Id FROM #ids);
    PRINT CONCAT('Documenten ontkoppeld: ', @@ROWCOUNT);

    UPDATE dbo.ChangeOrder SET SourceChangeOrderId = NULL
    WHERE SourceChangeOrderId IN (SELECT Id FROM #ids) AND Id NOT IN (SELECT Id FROM #ids);
    PRINT CONCAT('Afgeleide wijzigingsopdrachten ontkoppeld: ', @@ROWCOUNT);

    -- "Vervangen door"-verwijzing (versies), enkel als de kolom bestaat.
    IF COL_LENGTH('dbo.ChangeOrder', 'ReplacedByChangeOrderId') IS NOT NULL
    BEGIN
        EXEC (N'UPDATE dbo.ChangeOrder SET ReplacedByChangeOrderId = NULL WHERE ReplacedByChangeOrderId IN (SELECT Id FROM #ids) AND Id NOT IN (SELECT Id FROM #ids);');
    END

    DELETE FROM dbo.ChangeOrderPaymentTerm WHERE ChangeOrderId IN (SELECT Id FROM #ids);
    PRINT CONCAT('Betaalschijven verwijderd: ', @@ROWCOUNT);
    DELETE FROM dbo.ChangeOrderDetail WHERE ChangeOrderId IN (SELECT Id FROM #ids);
    PRINT CONCAT('Regels verwijderd: ', @@ROWCOUNT);
    DELETE FROM dbo.ChangeOrder WHERE Id IN (SELECT Id FROM #ids);
    PRINT CONCAT('Offertes/wijzigingsopdrachten verwijderd: ', @@ROWCOUNT);

    SELECT COUNT(*) AS NogAanwezig FROM dbo.ChangeOrder WHERE Id IN (SELECT Id FROM #ids);   -- moet 0 zijn

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
