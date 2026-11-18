-- =============================================
-- Migratie: 146_PlanningSjablonen
-- Omschrijving: Planningssjablonen (stap 1 van het projectsjabloon). Een sjabloon bestaat uit blokken (fases), die altijd,
--   voorwaardelijk (kelder, dak) of per verdieping herhaald worden, met taken (duur, code, estafette) en voorgangers via codes.
--   Vier nieuwe tabellen; raakt geen bestaande tabel. Additief en idempotent.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSjabloon]'))
BEGIN
    CREATE TABLE [dbo].[PlanningSjabloon] (
        [Id]           INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [Naam]         NVARCHAR(200)  NOT NULL,
        [Beschrijving] NVARCHAR(1000) NULL,
        [IsActief]     BIT            NOT NULL CONSTRAINT [DF_PlanningSjabloon_IsActief] DEFAULT 1,
        [CreatedDate]  DATETIME2      NOT NULL CONSTRAINT [DF_PlanningSjabloon_Created] DEFAULT SYSUTCDATETIME(),
        [ModifiedDate] DATETIME2      NULL,
        [CreatedByUserId] NVARCHAR(450) NULL
    );
    PRINT 'Tabel PlanningSjabloon aangemaakt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSjabloonBlok]'))
BEGIN
    CREATE TABLE [dbo].[PlanningSjabloonBlok] (
        [Id]                   INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [SjabloonId]           INT           NOT NULL,
        [Code]                 NVARCHAR(60)  NULL,
        [Naam]                 NVARCHAR(200) NOT NULL,
        [Volgorde]             INT           NOT NULL CONSTRAINT [DF_PlanningSjabloonBlok_Volgorde] DEFAULT 0,
        -- 0 = altijd, 1 = herhaald per verdieping
        [Herhaling]            TINYINT       NOT NULL CONSTRAINT [DF_PlanningSjabloonBlok_Herhaling] DEFAULT 0,
        -- Bij herhaling per verdieping: de eerste taak van verdieping n wacht op de taken van verdieping n-1.
        [VolgtVorigeVerdieping] BIT          NOT NULL CONSTRAINT [DF_PlanningSjabloonBlok_Volgt] DEFAULT 0,
        -- Voorwaarde: enkel opnemen als de variabele (kelder of dak) deze waarde heeft (ja, hellend, plat). Beide null = altijd.
        [VoorwaardeVariabele]  NVARCHAR(20)  NULL,
        [VoorwaardeWaarde]     NVARCHAR(20)  NULL,
        CONSTRAINT [FK_PlanningSjabloonBlok_Sjabloon] FOREIGN KEY ([SjabloonId]) REFERENCES [dbo].[PlanningSjabloon]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_PlanningSjabloonBlok_Sjabloon] ON [dbo].[PlanningSjabloonBlok] ([SjabloonId]);
    PRINT 'Tabel PlanningSjabloonBlok aangemaakt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSjabloonTaak]'))
BEGIN
    CREATE TABLE [dbo].[PlanningSjabloonTaak] (
        [Id]            INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [BlokId]        INT            NOT NULL,
        [Code]          NVARCHAR(60)   NULL,
        [Naam]          NVARCHAR(200)  NOT NULL,
        [Beschrijving]  NVARCHAR(MAX)  NULL,
        [Duur]          INT            NOT NULL CONSTRAINT [DF_PlanningSjabloonTaak_Duur] DEFAULT 1,
        [Volgorde]      INT            NOT NULL CONSTRAINT [DF_PlanningSjabloonTaak_Volgorde] DEFAULT 0,
        -- Estafette: bij meerdere bouwdelen start deze taak pas als dezelfde taak in het vorige bouwdeel klaar is (+ wachttijd).
        [Estafette]     BIT            NOT NULL CONSTRAINT [DF_PlanningSjabloonTaak_Estafette] DEFAULT 0,
        [EstafetteWacht] INT           NOT NULL CONSTRAINT [DF_PlanningSjabloonTaak_EstWacht] DEFAULT 0,
        CONSTRAINT [FK_PlanningSjabloonTaak_Blok] FOREIGN KEY ([BlokId]) REFERENCES [dbo].[PlanningSjabloonBlok]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_PlanningSjabloonTaak_Blok] ON [dbo].[PlanningSjabloonTaak] ([BlokId]);
    PRINT 'Tabel PlanningSjabloonTaak aangemaakt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSjabloonVoorganger]'))
BEGIN
    CREATE TABLE [dbo].[PlanningSjabloonVoorganger] (
        [Id]            INT          NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [TaakId]        INT          NOT NULL,
        -- Code van een taak of blok uit hetzelfde sjabloon.
        [VoorgangerCode] NVARCHAR(60) NOT NULL,
        [Type]          TINYINT      NOT NULL CONSTRAINT [DF_PlanningSjabloonVoorganger_Type] DEFAULT 0,
        [WachtDagen]    INT          NOT NULL CONSTRAINT [DF_PlanningSjabloonVoorganger_Wacht] DEFAULT 0,
        CONSTRAINT [FK_PlanningSjabloonVoorganger_Taak] FOREIGN KEY ([TaakId]) REFERENCES [dbo].[PlanningSjabloonTaak]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_PlanningSjabloonVoorganger_Taak] ON [dbo].[PlanningSjabloonVoorganger] ([TaakId]);
    PRINT 'Tabel PlanningSjabloonVoorganger aangemaakt.';
END
GO
