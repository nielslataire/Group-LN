-- =============================================
-- Migratie: 140_ChangeOrderLeveranciersgroepen
-- Datum: 2026-11-11
-- Omschrijving: Offerte / wijzigingsopdracht met MEERDERE leveranciers (design-handoff 28t en 28u).
--   De leverancier verhuist van het document naar een GROEP regels:
--   1. Tabel ChangeOrderGroup: per groep leverancier, titel op de offerte (tussenkop), contract (ContractActivity),
--      bron-offerte (bestand + referentie) en "via ons gefactureerd" (de schakelaar hoort bij de leverancier).
--   2. ChangeOrderDetail.GroupId (NULL-baar, FK naar ChangeOrderGroup): de groep waartoe een regel behoort.
--   3. Bestaande documenten worden eenmalig naar EEN groep omgezet (hun leverancier/contract, bron en facturatie
--      van vandaag) en hun regels aan die groep gehangen: er verandert niets zichtbaars.
--   De oude kolommen op ChangeOrder (ContractActivityId, QuoteSupplierReference, QuoteSourcePath, QuoteSourceFileName,
--   Invoiceable) blijven bestaan en spiegelen de EERSTE groep, zodat alles wat er nog op leunt (lijst, nummering,
--   ondertekening, facturatie, legacy-schermen) ongewijzigd blijft werken.
--   Idempotent: tweemaal draaien doet niets extra.
-- =============================================

IF OBJECT_ID('dbo.ChangeOrderGroup', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChangeOrderGroup]
    (
        [Id]                     INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ChangeOrderGroup] PRIMARY KEY,
        [ChangeOrderId]          INT NOT NULL,
        [SortOrder]              INT NOT NULL CONSTRAINT [DF_ChangeOrderGroup_SortOrder] DEFAULT (0),
        [SupplierCompanyId]      INT NULL,
        [Title]                  NVARCHAR(150) NULL,
        [ContractActivityId]     INT NULL,
        [QuoteSupplierReference] NVARCHAR(100) NULL,
        [QuoteSourcePath]        NVARCHAR(300) NULL,
        [QuoteSourceFileName]    NVARCHAR(260) NULL,
        [InvoicedByUs]           BIT NOT NULL CONSTRAINT [DF_ChangeOrderGroup_InvoicedByUs] DEFAULT (1),
        CONSTRAINT [FK_ChangeOrderGroup_ChangeOrder] FOREIGN KEY ([ChangeOrderId]) REFERENCES [dbo].[ChangeOrder] ([ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChangeOrderGroup_Company] FOREIGN KEY ([SupplierCompanyId]) REFERENCES [dbo].[CompanyInfo] ([CompanyId]),
        CONSTRAINT [FK_ChangeOrderGroup_ContractActivity] FOREIGN KEY ([ContractActivityId]) REFERENCES [dbo].[ContractActivity] ([Id])
    );
    CREATE INDEX [IX_ChangeOrderGroup_ChangeOrderId] ON [dbo].[ChangeOrderGroup] ([ChangeOrderId]);
    PRINT 'Tabel ChangeOrderGroup aangemaakt.';
END
ELSE PRINT 'Tabel ChangeOrderGroup bestaat al, overgeslagen.';
GO

IF COL_LENGTH('dbo.ChangeOrderDetail', 'GroupId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChangeOrderDetail] ADD [GroupId] INT NULL;
    PRINT 'Kolom ChangeOrderDetail.GroupId toegevoegd.';
END
GO

-- NO ACTION (geen cascade): een document verwijderen laat zijn groepen én regels samen vervallen via ChangeOrder.
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChangeOrderDetail_ChangeOrderGroup')
BEGIN
    ALTER TABLE [dbo].[ChangeOrderDetail] WITH CHECK ADD CONSTRAINT [FK_ChangeOrderDetail_ChangeOrderGroup]
        FOREIGN KEY ([GroupId]) REFERENCES [dbo].[ChangeOrderGroup] ([Id]);
    CREATE INDEX [IX_ChangeOrderDetail_GroupId] ON [dbo].[ChangeOrderDetail] ([GroupId]);
    PRINT 'FK_ChangeOrderDetail_ChangeOrderGroup toegevoegd.';
END
GO

-- Eenmalige omzetting: elk bestaand document met regels of een leverancier krijgt precies één groep.
INSERT INTO [dbo].[ChangeOrderGroup]
    ([ChangeOrderId], [SortOrder], [SupplierCompanyId], [Title], [ContractActivityId],
     [QuoteSupplierReference], [QuoteSourcePath], [QuoteSourceFileName], [InvoicedByUs])
SELECT co.[ID], 0, k.[CompanyId], NULL, co.[ContractActivityID],
       co.[QuoteSupplierReference], co.[QuoteSourcePath], co.[QuoteSourceFileName], ISNULL(co.[Invoiceable], 1)
FROM [dbo].[ChangeOrder] co
LEFT JOIN [dbo].[ContractActivity] a ON a.[Id] = co.[ContractActivityID]
LEFT JOIN [dbo].[Contract] k ON k.[Id] = a.[ContractId]
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ChangeOrderGroup] g WHERE g.[ChangeOrderId] = co.[ID])
  AND (co.[ContractActivityID] IS NOT NULL
       OR EXISTS (SELECT 1 FROM [dbo].[ChangeOrderDetail] d WHERE d.[ChangeOrderID] = co.[ID]));
PRINT 'Bestaande documenten naar een groep omgezet.';
GO

UPDATE d SET d.[GroupId] = g.[Id]
FROM [dbo].[ChangeOrderDetail] d
JOIN [dbo].[ChangeOrderGroup] g ON g.[ChangeOrderId] = d.[ChangeOrderID]
WHERE d.[GroupId] IS NULL;
GO
