-- Migration: AddProjectGroupColumns
-- Voegt ProjectGroup/unit-tracking toe aan MarketAsset en SaleStatus aan MarketListingSnapshot
-- EF migration: 20260607172917_AddProjectGroupColumns

-- MarketListingSnapshot: SaleStatus (nullable int)
ALTER TABLE [dbo].[MarketListingSnapshot]
    ADD [SaleStatus] INT NULL;

-- MarketAsset: project- en unit-kolommen
ALTER TABLE [dbo].[MarketAsset]
    ADD [IsProjectGroup]      BIT           NOT NULL DEFAULT 0,
        [ParentMarketAssetId] BIGINT        NULL,
        [ProjectExternalId]   NVARCHAR(50)  NULL,
        [UnitExternalId]      NVARCHAR(50)  NULL,
        [SaleStatus]          INT           NULL;

-- FK: unit → parent project asset (self-join)
ALTER TABLE [dbo].[MarketAsset]
    ADD CONSTRAINT [FK_MarketAsset_MarketAsset_ParentMarketAssetId]
    FOREIGN KEY ([ParentMarketAssetId])
    REFERENCES [dbo].[MarketAsset]([Id])
    ON DELETE NO ACTION;

-- Indexen
CREATE INDEX [IX_MarketAsset_ParentMarketAssetId]
    ON [dbo].[MarketAsset] ([ParentMarketAssetId]);

CREATE INDEX [IX_MarketAsset_ProjectExternalId]
    ON [dbo].[MarketAsset] ([ProjectExternalId]);

-- ── Rollback ──────────────────────────────────────────────────────────────
-- ALTER TABLE [dbo].[MarketAsset] DROP CONSTRAINT [FK_MarketAsset_MarketAsset_ParentMarketAssetId];
-- DROP INDEX [IX_MarketAsset_ParentMarketAssetId] ON [dbo].[MarketAsset];
-- DROP INDEX [IX_MarketAsset_ProjectExternalId]   ON [dbo].[MarketAsset];
-- ALTER TABLE [dbo].[MarketAsset] DROP COLUMN [IsProjectGroup], [ParentMarketAssetId], [ProjectExternalId], [UnitExternalId], [SaleStatus];
-- ALTER TABLE [dbo].[MarketListingSnapshot] DROP COLUMN [SaleStatus];
