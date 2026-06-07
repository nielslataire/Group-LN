-- Migration: AddExtendedPropertyColumns
-- EF migration: 20260607192651_AddExtendedPropertyColumns
-- Voegt alle ontbrekende property-velden toe aan MarketAsset en MarketListingSnapshot

-- ── MarketListingSnapshot ─────────────────────────────────────────────────────
ALTER TABLE [dbo].[MarketListingSnapshot]
    ADD [MaxPrice]       DECIMAL(14,2) NULL,
        [TerraceArea]    DECIMAL(10,2) NULL,
        [GardenArea]     DECIMAL(10,2) NULL,
        [Floor]          INT           NULL,
        [ShowerCount]    INT           NULL,
        [ToiletCount]    INT           NULL,
        [EnergyFeatures] NVARCHAR(500) NULL;

-- ── MarketAsset ───────────────────────────────────────────────────────────────
ALTER TABLE [dbo].[MarketAsset]
    ADD [Floor]            INT           NULL,
        [GarageCount]      INT           NULL,
        [TerraceArea]      DECIMAL(10,2) NULL,
        [GardenArea]       DECIMAL(10,2) NULL,
        [ShowerCount]      INT           NULL,
        [ToiletCount]      INT           NULL,
        [MaxPrice]         DECIMAL(14,2) NULL,
        [EnergyFeatures]   NVARCHAR(500) NULL,
        [DeveloperName]    NVARCHAR(200) NULL,
        [DeveloperWebsite] NVARCHAR(500) NULL,
        [DeveloperPhone]   NVARCHAR(50)  NULL;

-- ── Rollback ──────────────────────────────────────────────────────────────────
-- ALTER TABLE [dbo].[MarketListingSnapshot] DROP COLUMN [MaxPrice],[TerraceArea],[GardenArea],[Floor],[ShowerCount],[ToiletCount],[EnergyFeatures];
-- ALTER TABLE [dbo].[MarketAsset] DROP COLUMN [Floor],[GarageCount],[TerraceArea],[GardenArea],[ShowerCount],[ToiletCount],[MaxPrice],[EnergyFeatures],[DeveloperName],[DeveloperWebsite],[DeveloperPhone];
