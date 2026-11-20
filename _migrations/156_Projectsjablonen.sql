-- =============================================
-- Migratie: 156_Projectsjablonen
-- Omschrijving: Projectsjablonen (stap 4): een bundel van een planningssjabloon, een trajectsjabloon en een schijvensjabloon met
--   standaardantwoorden (verdiepingen, kelder, dak). Toegepast vanaf de planningpagina; het sjabloon past zich aan de antwoorden aan.
--   Schijven in een schijvensjabloon kunnen nu ook een voorwaarde hebben (kelder of dak), zoals de blokken van een planningssjabloon.
--   * Nieuwe tabel ProjectSjabloon (verwijzingen zonder foreign key: een verwijderd sjabloon laat een lege keuze achter).
--   * Nieuwe nullable kolommen SchijfSjabloonSchijf.VoorwaardeVariabele en VoorwaardeWaarde.
--   Additief en idempotent. Draai vóór app-start (na 155).
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ProjectSjabloon]'))
BEGIN
    CREATE TABLE [dbo].[ProjectSjabloon] (
        [Id]                   INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [Naam]                 NVARCHAR(200)  NOT NULL,
        [Beschrijving]         NVARCHAR(1000) NULL,
        [PlanningSjabloonId]   INT            NULL,
        [TrajectSjabloonId]    INT            NULL,
        [SchijfSjabloonId]     INT            NULL,
        [StandaardVerdiepingen] INT           NOT NULL CONSTRAINT [DF_ProjectSjabloon_Verd] DEFAULT 1,
        [StandaardKelder]      BIT            NOT NULL CONSTRAINT [DF_ProjectSjabloon_Kelder] DEFAULT 0,
        -- "hellend", "plat" of leeg
        [StandaardDak]         NVARCHAR(20)   NULL,
        [IsActief]             BIT            NOT NULL CONSTRAINT [DF_ProjectSjabloon_IsActief] DEFAULT 1,
        [CreatedDate]          DATETIME2      NOT NULL CONSTRAINT [DF_ProjectSjabloon_Created] DEFAULT SYSUTCDATETIME(),
        [ModifiedDate]         DATETIME2      NULL,
        [CreatedByUserId]      NVARCHAR(450)  NULL,
        [ModifiedByUserId]     NVARCHAR(450)  NULL
    );
    PRINT 'Tabel ProjectSjabloon aangemaakt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SchijfSjabloonSchijf]') AND name = N'VoorwaardeVariabele')
BEGIN
    ALTER TABLE [dbo].[SchijfSjabloonSchijf] ADD [VoorwaardeVariabele] NVARCHAR(20) NULL;
    PRINT 'Kolom SchijfSjabloonSchijf.VoorwaardeVariabele toegevoegd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SchijfSjabloonSchijf]') AND name = N'VoorwaardeWaarde')
BEGIN
    ALTER TABLE [dbo].[SchijfSjabloonSchijf] ADD [VoorwaardeWaarde] NVARCHAR(20) NULL;
    PRINT 'Kolom SchijfSjabloonSchijf.VoorwaardeWaarde toegevoegd.';
END
GO
