-- =============================================
-- Migratie: 131_ChangeOrderProjectEnBijlagen
-- Datum: 2026-11-05
-- Omschrijving: Offertes en wijzigingsopdrachten hoeven niet meer aan een contract te hangen.
--   1. ChangeOrder.ProjectId (NOT NULL, FK naar Project): het project waartoe het document behoort. Tot nu toe volgde het project uit
--      ContractActivity -> Contract -> Project; zonder contract is er een eigen koppeling nodig. Bestaande rijen worden gevuld via hun contract.
--   2. ChangeOrder.ContractActivityID wordt NULL-baar. Het project toont een waarschuwing zolang een offerte of wijzigingsopdracht nog geen contract heeft.
--   3. Tabel ChangeOrderAttachment: bijlagen bij een offerte (plan, fiche, ...) die mee met de offertemail naar de klant gaan.
--   Idempotent: tweemaal draaien doet niets extra.
-- =============================================

-- 1. ProjectId
IF COL_LENGTH('dbo.ChangeOrder', 'ProjectId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [ProjectId] INT NULL;
    PRINT 'ChangeOrder.ProjectId toegevoegd.';
END
GO

UPDATE co SET co.ProjectId = k.ProjectId
FROM [dbo].[ChangeOrder] co
JOIN [dbo].[ContractActivity] a ON a.Id = co.ContractActivityID
JOIN [dbo].[Contract] k ON k.Id = a.ContractId
WHERE co.ProjectId IS NULL;
GO

IF EXISTS (SELECT 1 FROM [dbo].[ChangeOrder] WHERE [ProjectId] IS NULL)
    PRINT 'LET OP: er zijn ChangeOrder-rijen zonder project; ProjectId blijft NULL-baar tot die opgelost zijn.';
ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'ProjectId' AND is_nullable = 1)
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ALTER COLUMN [ProjectId] INT NOT NULL;
    PRINT 'ChangeOrder.ProjectId is nu NOT NULL.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChangeOrder_Project')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] WITH CHECK ADD CONSTRAINT [FK_ChangeOrder_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project] ([ProjectID]);
    PRINT 'FK_ChangeOrder_Project toegevoegd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChangeOrder_ProjectId' AND object_id = OBJECT_ID('dbo.ChangeOrder'))
    CREATE NONCLUSTERED INDEX [IX_ChangeOrder_ProjectId] ON [dbo].[ChangeOrder] ([ProjectId]);
GO

-- 2. ContractActivityID NULL-baar
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'ContractActivityID' AND is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ALTER COLUMN [ContractActivityID] INT NULL;
    PRINT 'ChangeOrder.ContractActivityID is nu NULL-baar.';
END
GO

-- 3. Bijlagen
IF OBJECT_ID('dbo.ChangeOrderAttachment', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChangeOrderAttachment]
    (
        [Id]            INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ChangeOrderAttachment] PRIMARY KEY,
        [ChangeOrderId] INT NOT NULL,
        [FileName]      NVARCHAR(260) NOT NULL,
        [StoragePath]   NVARCHAR(300) NOT NULL,
        [ContentType]   NVARCHAR(100) NULL,
        [SizeBytes]     BIGINT NOT NULL CONSTRAINT [DF_ChangeOrderAttachment_Size] DEFAULT (0),
        [UploadedAt]    DATETIME2 NOT NULL CONSTRAINT [DF_ChangeOrderAttachment_UploadedAt] DEFAULT (SYSDATETIME()),
        CONSTRAINT [FK_ChangeOrderAttachment_ChangeOrder] FOREIGN KEY ([ChangeOrderId]) REFERENCES [dbo].[ChangeOrder] ([ID]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ChangeOrderAttachment_ChangeOrderId] ON [dbo].[ChangeOrderAttachment] ([ChangeOrderId]);
    PRINT 'Tabel ChangeOrderAttachment aangemaakt.';
END
ELSE PRINT 'Tabel ChangeOrderAttachment bestaat al, overgeslagen.';
GO
