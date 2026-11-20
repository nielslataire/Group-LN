-- =============================================
-- Migratie: 155_Schijvensjablonen
-- Omschrijving: Schijvensjablonen (stap 2 van het projectsjabloon): een standaardlijst betalingsschijven met een naam, een vast
--   percentage of een gewicht, een eventuele herhaling per verdieping en de code van de taak of fase in het planningssjabloon waaraan
--   de schijf hangt. Toegepast in het scherm van een betalingsgroep.
--   * Nieuwe tabellen SchijfSjabloon en SchijfSjabloonSchijf.
--   * Nieuwe nullable kolommen InvoicingPaymentStages.PlanningCode en PlanningVolgnr: de schijf onthoudt aan welke code (en bij een
--     herhaling per verdieping welke verdieping) in de planning ze hangt, zodat ze automatisch aan de juiste taak gekoppeld wordt.
--   Additief en idempotent. Draai vóór app-start.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[SchijfSjabloon]'))
BEGIN
    CREATE TABLE [dbo].[SchijfSjabloon] (
        [Id]                INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [Naam]              NVARCHAR(200)  NOT NULL,
        [Beschrijving]      NVARCHAR(1000) NULL,
        -- Waarschuwing (geen blokkade) als een schijf na het uitrekenen onder dit percentage zakt.
        [MinimumPercentage] DECIMAL(9,4)   NOT NULL CONSTRAINT [DF_SchijfSjabloon_Min] DEFAULT 5,
        [IsActief]          BIT            NOT NULL CONSTRAINT [DF_SchijfSjabloon_IsActief] DEFAULT 1,
        [CreatedDate]       DATETIME2      NOT NULL CONSTRAINT [DF_SchijfSjabloon_Created] DEFAULT SYSUTCDATETIME(),
        [ModifiedDate]      DATETIME2      NULL,
        [CreatedByUserId]   NVARCHAR(450)  NULL,
        [ModifiedByUserId]  NVARCHAR(450)  NULL
    );
    PRINT 'Tabel SchijfSjabloon aangemaakt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[SchijfSjabloonSchijf]'))
BEGIN
    CREATE TABLE [dbo].[SchijfSjabloonSchijf] (
        [Id]             INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [SjabloonId]     INT           NOT NULL,
        [Volgorde]       INT           NOT NULL CONSTRAINT [DF_SchijfSjabloonSchijf_Volgorde] DEFAULT 0,
        -- Mag {verdieping} bevatten ("Bij het gieten van de vloerplaat {verdieping}" wordt "... 1ste verdieping").
        [Naam]           NVARCHAR(250) NOT NULL,
        -- Code van de taak of fase in het planningssjabloon waaraan deze schijf hangt (optioneel).
        [PlanningCode]   NVARCHAR(100) NULL,
        -- Vast percentage (komt eerst van de 100 % af). Leeg = het percentage volgt uit het gewicht.
        [VastPercentage] DECIMAL(9,4)  NULL,
        [Gewicht]        DECIMAL(9,4)  NOT NULL CONSTRAINT [DF_SchijfSjabloonSchijf_Gewicht] DEFAULT 1,
        -- Herhaald per verdieping: het percentage of gewicht wordt gelijk verdeeld over de verdiepingen.
        [PerVerdieping]  BIT           NOT NULL CONSTRAINT [DF_SchijfSjabloonSchijf_PerVerd] DEFAULT 0,
        CONSTRAINT [FK_SchijfSjabloonSchijf_Sjabloon] FOREIGN KEY ([SjabloonId]) REFERENCES [dbo].[SchijfSjabloon]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_SchijfSjabloonSchijf_Sjabloon] ON [dbo].[SchijfSjabloonSchijf] ([SjabloonId]);
    PRINT 'Tabel SchijfSjabloonSchijf aangemaakt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InvoicingPaymentStages]') AND name = N'PlanningCode')
BEGIN
    ALTER TABLE [dbo].[InvoicingPaymentStages] ADD [PlanningCode] NVARCHAR(100) NULL;
    PRINT 'Kolom InvoicingPaymentStages.PlanningCode toegevoegd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InvoicingPaymentStages]') AND name = N'PlanningVolgnr')
BEGIN
    ALTER TABLE [dbo].[InvoicingPaymentStages] ADD [PlanningVolgnr] INT NULL;
    PRINT 'Kolom InvoicingPaymentStages.PlanningVolgnr toegevoegd.';
END
GO
