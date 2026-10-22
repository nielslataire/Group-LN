-- =============================================
-- Migratie: 110_Verzekeringen
-- Datum: 2026-10-22
-- Omschrijving: Verzekeringen van een project (design-handoff punt 43, CRM Verzekeringen): polissen met verzekerde waarde, premie en gebouw,
--   attesten van aannemers (tienjarige per woning en BA) en automatische aandachtspunten.
--   1. Insurances.ContractActivityID wordt NULL-baar: een polis die rechtstreeks bij de maatschappij loopt (zonder makelaar) heeft geen
--      contract bij een makelaar. De unieke index UQ_Insurances_ContractActivity wordt een gefilterde unieke index (enkel waar ingevuld),
--      zodat er nog altijd maximaal één verzekering per contractactiviteit is. Bestaande rijen blijven ongewijzigd.
--   2. Insurances krijgt extra kolommen (alle optioneel of met een standaardwaarde): ProjectId (nu ook voor polissen zonder contract; wordt
--      voor bestaande rijen ingevuld via contract -> project), Building, AppliesTo, UnitId, InsuredValue, Premium, Note, IsRequested,
--      StartAtDelivery, DurationYears, EndReason, RemindStart/RemindSale/RemindExpiry, DocumentId.
--      Type: 1 ABR, 2 Brand, 3 Tienjarige, 4 BA (nieuw), 5 Andere (nieuw). Building: 0 bestaand gebouw, 1 nieuwbouw.
--   3. InsuranceAttest: per contract en soort (0 tienjarige, 1 BA) wat de aannemer heeft afgegeven.
--      Status: 0 ontbreekt, 1 ontvangen, 2 niet vereist. Een contract zonder rij is "ontbreekt" (tienjarige, enkel als de werken onder de wet
--      vallen) of "niet ontvangen" (BA, aanbevolen); de rij ontstaat pas als iemand iets vastlegt of opvraagt.
--   Geen FK op ProjectId/UnitId/DocumentId (zou een tweede cascade-pad naar Project geven). Idempotent.
-- =============================================

-- 1. ContractActivityID NULL-baar + gefilterde unieke index
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Insurances') AND name = N'ContractActivityID' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Insurances_ContractActivity' AND parent_object_id = OBJECT_ID(N'dbo.Insurances'))
        ALTER TABLE [dbo].[Insurances] DROP CONSTRAINT [UQ_Insurances_ContractActivity];
    ELSE IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Insurances_ContractActivity' AND object_id = OBJECT_ID(N'dbo.Insurances'))
        DROP INDEX [UQ_Insurances_ContractActivity] ON [dbo].[Insurances];
    ALTER TABLE [dbo].[Insurances] ALTER COLUMN [ContractActivityID] INT NULL;
    PRINT 'Insurances.ContractActivityID is nu NULL-baar.';
END
ELSE PRINT 'Insurances.ContractActivityID was al NULL-baar, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Insurances_ContractActivity' AND object_id = OBJECT_ID(N'dbo.Insurances'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Insurances_ContractActivity] ON [dbo].[Insurances] ([ContractActivityID]) WHERE [ContractActivityID] IS NOT NULL;
    PRINT 'Gefilterde unieke index UQ_Insurances_ContractActivity aangemaakt.';
END
ELSE PRINT 'Index UQ_Insurances_ContractActivity bestaat al, overgeslagen.';
GO

-- 2. Extra kolommen
IF COL_LENGTH('dbo.Insurances', 'ProjectId') IS NULL ALTER TABLE [dbo].[Insurances] ADD [ProjectId] INT NULL;
IF COL_LENGTH('dbo.Insurances', 'Building') IS NULL ALTER TABLE [dbo].[Insurances] ADD [Building] INT NULL;
IF COL_LENGTH('dbo.Insurances', 'AppliesTo') IS NULL ALTER TABLE [dbo].[Insurances] ADD [AppliesTo] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.Insurances', 'UnitId') IS NULL ALTER TABLE [dbo].[Insurances] ADD [UnitId] INT NULL;
IF COL_LENGTH('dbo.Insurances', 'InsuredValue') IS NULL ALTER TABLE [dbo].[Insurances] ADD [InsuredValue] DECIMAL(18,2) NULL;
IF COL_LENGTH('dbo.Insurances', 'Premium') IS NULL ALTER TABLE [dbo].[Insurances] ADD [Premium] DECIMAL(18,2) NULL;
IF COL_LENGTH('dbo.Insurances', 'Note') IS NULL ALTER TABLE [dbo].[Insurances] ADD [Note] NVARCHAR(300) NULL;
IF COL_LENGTH('dbo.Insurances', 'IsRequested') IS NULL ALTER TABLE [dbo].[Insurances] ADD [IsRequested] BIT NOT NULL CONSTRAINT [DF_Insurances_IsRequested] DEFAULT (0);
IF COL_LENGTH('dbo.Insurances', 'StartAtDelivery') IS NULL ALTER TABLE [dbo].[Insurances] ADD [StartAtDelivery] BIT NOT NULL CONSTRAINT [DF_Insurances_StartAtDelivery] DEFAULT (0);
IF COL_LENGTH('dbo.Insurances', 'DurationYears') IS NULL ALTER TABLE [dbo].[Insurances] ADD [DurationYears] INT NULL;
IF COL_LENGTH('dbo.Insurances', 'EndReason') IS NULL ALTER TABLE [dbo].[Insurances] ADD [EndReason] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.Insurances', 'RemindStart') IS NULL ALTER TABLE [dbo].[Insurances] ADD [RemindStart] BIT NOT NULL CONSTRAINT [DF_Insurances_RemindStart] DEFAULT (1);
IF COL_LENGTH('dbo.Insurances', 'RemindSale') IS NULL ALTER TABLE [dbo].[Insurances] ADD [RemindSale] BIT NOT NULL CONSTRAINT [DF_Insurances_RemindSale] DEFAULT (1);
IF COL_LENGTH('dbo.Insurances', 'RemindExpiry') IS NULL ALTER TABLE [dbo].[Insurances] ADD [RemindExpiry] BIT NOT NULL CONSTRAINT [DF_Insurances_RemindExpiry] DEFAULT (1);
IF COL_LENGTH('dbo.Insurances', 'DocumentId') IS NULL ALTER TABLE [dbo].[Insurances] ADD [DocumentId] INT NULL;
PRINT 'Kolommen op Insurances gecontroleerd.';
GO

-- ProjectId voor bestaande polissen (via contractactiviteit -> contract -> project)
UPDATE i SET i.[ProjectId] = c.[ProjectId]
  FROM [dbo].[Insurances] i
  JOIN [dbo].[ContractActivity] ca ON ca.[Id] = i.[ContractActivityID]
  JOIN [dbo].[Contract] c ON c.[Id] = ca.[ContractId]
 WHERE i.[ProjectId] IS NULL;
PRINT CONCAT('ProjectId ingevuld voor bestaande polissen: ', @@ROWCOUNT);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Insurances_ProjectId' AND object_id = OBJECT_ID(N'dbo.Insurances'))
    CREATE NONCLUSTERED INDEX [IX_Insurances_ProjectId] ON [dbo].[Insurances] ([ProjectId]);
GO

-- 3. Attesten van aannemers
IF OBJECT_ID('dbo.InsuranceAttest', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InsuranceAttest] (
        [Id]              INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_InsuranceAttest] PRIMARY KEY,
        [ProjectId]       INT NOT NULL,
        [ContractId]      INT NOT NULL,
        [Kind]            INT NOT NULL,
        [Status]          INT NOT NULL CONSTRAINT [DF_InsuranceAttest_Status] DEFAULT (0),
        [UnitsRequired]   INT NULL,
        [UnitsReceived]   INT NULL,
        [ValidUntil]      DATE NULL,
        [Insurer]         NVARCHAR(150) NULL,
        [PolicyNumber]    NVARCHAR(100) NULL,
        [Note]            NVARCHAR(300) NULL,
        [DocumentId]      INT NULL,
        [ReceivedAt]      DATE NULL,
        [LastRequestedAt] DATETIME2(0) NULL,
        [LastRequestedBy] NVARCHAR(150) NULL,
        [CreatedAt]       DATETIME2(0) NOT NULL CONSTRAINT [DF_InsuranceAttest_CreatedAt] DEFAULT (sysutcdatetime()),
        [UpdatedAt]       DATETIME2(0) NULL,
        CONSTRAINT [FK_InsuranceAttest_Contract] FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contract]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [UX_InsuranceAttest_Contract_Kind] ON [dbo].[InsuranceAttest] ([ContractId], [Kind]);
    CREATE INDEX [IX_InsuranceAttest_Project] ON [dbo].[InsuranceAttest] ([ProjectId]);
    PRINT 'Tabel InsuranceAttest aangemaakt.';
END
ELSE PRINT 'Tabel InsuranceAttest bestaat al, overgeslagen.';
GO
