-- =============================================
-- Migratie: 103_VerkoopcontractenInVerkoop
-- Datum: 2026-10-16
-- Omschrijving: Verkoopcontracten (één eenheid + één koper) horen in de documentmap "Verkoop", gegroepeerd per klant/eenheid, met dezelfde
--   ondertekening en dezelfde regel "volledig getekend = eenheid naar Verkocht" als de map "Contracten". "Contracten" blijft bestaan voor
--   de andere contracten (leveranciers/aannemers).
--   1. De map 'verkoop' krijgt ViewKind 'contracten' (kolomset/groepering/ondertekening/Verkocht-effect volgen de ViewKind, niet de mapcode).
--   2. Bestaande documenten in 'contracten' met precies één eenheid- en precies één klantkoppeling (en geen wijzigingsopdracht) gaan naar 'verkoop'.
--   Draai dit NA 102_WijzigingsopdrachtenNaarOffertes. Strikt idempotent; status, revisies en koppelingen blijven ongewijzigd.
-- =============================================
DECLARE @contracten INT = (SELECT TOP 1 [Id] FROM [dbo].[DocumentFolders] WHERE [Code] = N'contracten');
DECLARE @verkoop    INT = (SELECT TOP 1 [Id] FROM [dbo].[DocumentFolders] WHERE [Code] = N'verkoop');

IF @contracten IS NULL OR @verkoop IS NULL
    PRINT 'Map contracten of verkoop niet gevonden, niets gewijzigd.';
ELSE
BEGIN
    UPDATE [dbo].[DocumentFolders] SET [ViewKind] = N'contracten' WHERE [Id] = @verkoop AND [ViewKind] <> N'contracten';
    PRINT CONCAT('Map Verkoop op ViewKind contracten gezet: ', @@ROWCOUNT);

    UPDATE d
       SET d.[FolderId] = @verkoop
      FROM [dbo].[ProjectDocs] d
     WHERE d.[FolderId] = @contracten
       AND d.[ChangeOrderId] IS NULL
       AND (SELECT COUNT(*) FROM [dbo].[DocumentLinks] l WHERE l.[DocumentId] = d.[Id] AND l.[UnitId] IS NOT NULL) = 1
       AND (SELECT COUNT(*) FROM [dbo].[DocumentLinks] l WHERE l.[DocumentId] = d.[Id] AND l.[ClientAccountId] IS NOT NULL) = 1;
    PRINT CONCAT('Verkoopcontracten verplaatst naar Verkoop: ', @@ROWCOUNT);
END
GO
