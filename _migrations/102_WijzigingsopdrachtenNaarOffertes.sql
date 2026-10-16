-- =============================================
-- Migratie: 102_WijzigingsopdrachtenNaarOffertes
-- Datum: 2026-10-16
-- Omschrijving: Wijzigingsopdrachten (ProjectDocs met ChangeOrderId) kwamen in de documentmap "Contracten" terecht. Ze horen bij
--   "Offertes & bestellingen" (Code 'offertes'). De code maakt nieuwe documenten voortaan in die map; deze migratie verplaatst de bestaande.
--   Enkel documenten die nu in 'contracten' staan worden verplaatst; status, revisies en koppelingen blijven ongewijzigd.
--   Strikt additief + idempotent (een tweede run vindt niets meer om te verplaatsen).
-- =============================================
DECLARE @contracten INT = (SELECT TOP 1 [Id] FROM [dbo].[DocumentFolders] WHERE [Code] = N'contracten');
DECLARE @offertes   INT = (SELECT TOP 1 [Id] FROM [dbo].[DocumentFolders] WHERE [Code] = N'offertes');

IF @contracten IS NULL OR @offertes IS NULL
    PRINT 'Map contracten of offertes niet gevonden, niets verplaatst.';
ELSE
BEGIN
    UPDATE [dbo].[ProjectDocs]
       SET [FolderId] = @offertes
     WHERE [FolderId] = @contracten AND [ChangeOrderId] IS NOT NULL;
    PRINT CONCAT('Wijzigingsopdrachten verplaatst naar Offertes & bestellingen: ', @@ROWCOUNT);
END
GO
