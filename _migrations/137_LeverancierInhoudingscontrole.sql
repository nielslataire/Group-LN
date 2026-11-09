-- =============================================
-- Migratie: 137_LeverancierInhoudingscontrole
-- Datum: 2026-11-09
-- Omschrijving: Controle inhoudingsplicht (art. 30bis/30ter, sociale en fiscale schulden) van leveranciers via de RSZ-webservice
--   BillRetainment. Eén rij per gecontroleerde onderneming; de factuurpagina toont de laatste controle van die factuur, de controlelijst
--   (BatchId) groepeert een handmatige controle van meerdere ondernemingen vóór een betaling.
--   Bewijs: RetainmentCheckReference (consultatiereferentie), TraceId (BelGov-Trace-Id van de RSZ) en RawResponse (de ongewijzigde RSZ-respons).
--   SupplierRetainmentCheck.IncomingInvoiceId verwijst naar IncommingInvoices.Id (bewust zonder FK: een controle blijft als bewijs bestaan).
--   Strikt additief + idempotent (ook veilig als een eerdere versie van dit script al gedraaid is). Draai dit VÓÓR de nieuwe versie van de applicatie opstart.
-- =============================================
IF OBJECT_ID('dbo.SupplierRetainmentCheck', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SupplierRetainmentCheck]
    (
        [Id]                          INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_SupplierRetainmentCheck] PRIMARY KEY,
        [IncomingInvoiceId]           INT           NULL,
        [BatchId]                     UNIQUEIDENTIFIER NULL,    -- controlelijst waartoe deze controle behoort
        [SupplierName]                NVARCHAR(200) NULL,       -- naam zoals de gebruiker/de factuur hem gaf
        [EnterpriseIdentifier]        NVARCHAR(50)  NOT NULL,   -- wat we naar de RSZ stuurden (ondernemingsnummer, btw-nummer of buitenlandse id)
        [EnterpriseNumber]            NVARCHAR(20)  NULL,       -- ondernemingsnummer zoals de RSZ het teruggeeft
        [Environment]                 NVARCHAR(20)  NOT NULL,   -- Simulation of Production
        [CheckedAt]                   DATETIME2     NOT NULL CONSTRAINT [DF_SupplierRetainmentCheck_CheckedAt] DEFAULT (SYSUTCDATETIME()),
        [CheckedBy]                   NVARCHAR(200) NULL,
        [RetainmentCheckReference]    NVARCHAR(50)  NULL,       -- consultatiereferentie van de RSZ (bewijs + invoer voor paymentPreparations)
        [TraceId]                     NVARCHAR(100) NULL,       -- BelGov-Trace-Id van het antwoord, voor audit en supportvragen
        [FiscalSituation]             NVARCHAR(20)  NULL,       -- obligation / noObligation / notInDomain
        [FiscalWithholdingPercentage] TINYINT       NULL,
        [FiscalValidUntil]            DATETIME2     NULL,
        [FiscalAttestId]              BIGINT        NULL,
        [SocialSituation]             NVARCHAR(20)  NULL,
        [SocialWithholdingPercentage] TINYINT       NULL,
        [SocialValidUntil]            DATETIME2     NULL,
        [RawResponse]                 NVARCHAR(MAX) NULL,       -- het ongewijzigde resultaat van de RSZ voor deze onderneming (JSON)
        [ErrorMessage]                NVARCHAR(500) NULL        -- ingevuld als de bevraging mislukte; de rest blijft dan leeg
    );
    PRINT 'SupplierRetainmentCheck aangemaakt.';
END
GO

IF COL_LENGTH('dbo.SupplierRetainmentCheck', 'BatchId') IS NULL
    ALTER TABLE [dbo].[SupplierRetainmentCheck] ADD [BatchId] UNIQUEIDENTIFIER NULL;
GO
IF COL_LENGTH('dbo.SupplierRetainmentCheck', 'SupplierName') IS NULL
    ALTER TABLE [dbo].[SupplierRetainmentCheck] ADD [SupplierName] NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.SupplierRetainmentCheck', 'TraceId') IS NULL
    ALTER TABLE [dbo].[SupplierRetainmentCheck] ADD [TraceId] NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.SupplierRetainmentCheck', 'RawResponse') IS NULL
    ALTER TABLE [dbo].[SupplierRetainmentCheck] ADD [RawResponse] NVARCHAR(MAX) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierRetainmentCheck_Invoice' AND object_id = OBJECT_ID('dbo.SupplierRetainmentCheck'))
    CREATE INDEX [IX_SupplierRetainmentCheck_Invoice] ON [dbo].[SupplierRetainmentCheck] ([IncomingInvoiceId], [CheckedAt] DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierRetainmentCheck_Enterprise' AND object_id = OBJECT_ID('dbo.SupplierRetainmentCheck'))
    CREATE INDEX [IX_SupplierRetainmentCheck_Enterprise] ON [dbo].[SupplierRetainmentCheck] ([EnterpriseIdentifier], [CheckedAt] DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierRetainmentCheck_Batch' AND object_id = OBJECT_ID('dbo.SupplierRetainmentCheck'))
    CREATE INDEX [IX_SupplierRetainmentCheck_Batch] ON [dbo].[SupplierRetainmentCheck] ([BatchId]);
GO
