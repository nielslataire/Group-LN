-- =====================================================================================================
-- ENKEL LEZEN (geen enkele wijziging): welke verkoopfacturen horen bij een project, en wat hangt eraan?
-- Vul @ProjectId in (0 = overzicht van alle projecten met het aantal facturen).
-- =====================================================================================================
SET NOCOUNT ON;
DECLARE @ProjectId INT = 0;   -- << VUL IN

IF @ProjectId = 0
BEGIN
    SELECT p.ProjectId, p.ProjectName, COUNT(i.Id) AS Facturen,
           SUM(CASE WHEN i.OctopusBookedAt IS NOT NULL OR i.OctopusDocumentSequenceNr IS NOT NULL THEN 1 ELSE 0 END) AS InOctopusGeboekt
    FROM dbo.Project p LEFT JOIN dbo.Invoices i ON i.ProjectId = p.ProjectId
    GROUP BY p.ProjectId, p.ProjectName
    HAVING COUNT(i.Id) > 0
    ORDER BY p.ProjectName;
    RETURN;
END

-- 1. De facturen zelf (met totaal van de regels, Octopus-status en wat eraan hangt)
SELECT i.Id, i.Filename, i.[Date], i.ClientName, i.StatusId, i.Prepaid,
       (SELECT SUM(ISNULL(d.Quantity, 1) * ISNULL(d.UnitPrice, d.Price)) FROM dbo.InvoicesDetails d WHERE d.InvoiceId = i.Id) AS TotaalExclBtwOngeveer,
       (SELECT COUNT(*) FROM dbo.InvoicesDetails d WHERE d.InvoiceId = i.Id) AS Regels,
       CASE WHEN i.OctopusBookedAt IS NOT NULL OR i.OctopusDocumentSequenceNr IS NOT NULL THEN 'JA' ELSE '' END AS InOctopusGeboekt,
       i.OctopusBookedAt, i.OctopusDocumentSequenceNr, i.OctopusDeliveryState,
       (SELECT COUNT(*) FROM dbo.PaymentAllocations a WHERE a.InvoiceId = i.Id) AS Betalingstoewijzingen,
       (SELECT SUM(a.Amount) FROM dbo.PaymentAllocations a WHERE a.InvoiceId = i.Id) AS BetaaldBedrag,
       (SELECT COUNT(*) FROM dbo.InvoiceEmailLog e WHERE e.InvoiceId = i.Id) AS VerzondenEmails,
       (SELECT COUNT(*) FROM dbo.InvoiceDunning n WHERE n.InvoiceId = i.Id) AS Aanmaningen,
       (SELECT COUNT(*) FROM dbo.InvoiceRelations r WHERE r.ParentInvoiceId = i.Id OR r.ChildInvoiceId = i.Id) AS CreditOfRelaties,
       i.ReplacementOfId
FROM dbo.Invoices i
WHERE i.ProjectId = @ProjectId
ORDER BY i.[Date], i.Id;

-- 2. Waar de regels voor gefactureerd zijn (type regel)
SELECT d.LineType, COUNT(*) AS Regels
FROM dbo.InvoicesDetails d JOIN dbo.Invoices i ON i.Id = d.InvoiceId
WHERE i.ProjectId = @ProjectId GROUP BY d.LineType ORDER BY Regels DESC;

-- 3. Gekoppeld aan andere gegevens die bij verwijderen zouden moeten worden ontkoppeld
SELECT 'ProjectContractSlice' AS Tabel, COUNT(*) AS Aantal FROM dbo.ProjectContractSlice WHERE InvoiceId IN (SELECT Id FROM dbo.Invoices WHERE ProjectId = @ProjectId)
UNION ALL SELECT 'ProjectRegieUur', COUNT(*) FROM dbo.ProjectRegieUur WHERE InvoiceId IN (SELECT Id FROM dbo.Invoices WHERE ProjectId = @ProjectId)
UNION ALL SELECT 'Units (grondwaardefactuur)', COUNT(*) FROM dbo.Units WHERE LandValueInvoiceId IN (SELECT Id FROM dbo.Invoices WHERE ProjectId = @ProjectId)
UNION ALL SELECT 'ConnectionAdvanceApplication (voorschotverrekening op factuurregel)', COUNT(*) FROM dbo.ConnectionAdvanceApplication WHERE InvoiceLineId IN (SELECT d.Id FROM dbo.InvoicesDetails d JOIN dbo.Invoices i ON i.Id = d.InvoiceId WHERE i.ProjectId = @ProjectId)
UNION ALL SELECT 'Wijzigingsopdrachtregels die als gefactureerd gelden', COUNT(*) FROM dbo.ChangeOrderDetail
          WHERE Id IN (SELECT ChangeOrderDetailId FROM dbo.InvoicesDetails WHERE ChangeOrderDetailId IS NOT NULL AND InvoiceId IN (SELECT Id FROM dbo.Invoices WHERE ProjectId = @ProjectId));

-- 4. Alle tabellen met een foreign key naar Invoices/InvoicesDetails (om te zien of er iets ontbreekt)
SELECT fk.name AS ForeignKey, OBJECT_NAME(fk.parent_object_id) AS Tabel, OBJECT_NAME(fk.referenced_object_id) AS VerwijstNaar
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.referenced_object_id) IN ('Invoices', 'InvoicesDetails')
ORDER BY VerwijstNaar, Tabel;
