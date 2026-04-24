-- ============================================================
-- Documentencentrum: uitbreiden van de bestaande IncommingInvoices-tabel
-- en aanmaken van de nieuwe IncomingInvoiceAttachments-tabel.
--
-- Bestaande kolommen (ongewijzigd):
--   Id, Date (= factuurdatum), Price (= totaalbedrag incl. BTW),
--   ExternalId (= factuurreferentie), CompanyId (= leverancier FK naar CompanyInfo),
--   ProjectId, ContractId
-- ============================================================

-- 1. ProjectId nullable maken (was NOT NULL — facturen zonder project zijn nu mogelijk)
ALTER TABLE [dbo].[IncommingInvoices]
    ALTER COLUMN [ProjectId] INT NULL;

GO

-- 2. Documentencentrum-kolommen toevoegen
ALTER TABLE [dbo].[IncommingInvoices]
    ADD
    -- Facturatiebedrijf (onze onderneming)
    [IssuerCompanyId]            INT            NULL,

    -- Leverancier (aanvullend op bestaand CompanyId)
    [SupplierName]               NVARCHAR(250)  NULL,
    [SupplierVatNumber]          NVARCHAR(50)   NULL,
    [OctopusSupplierRelationKey] INT            NULL,

    -- Factuurdocument (aanvullend op bestaand Date/Price/ExternalId)
    [DueDate]                    DATE           NULL,
    [VatAmount]                  DECIMAL(18,4)  NULL,
    [NetAmount]                  DECIMAL(18,4)  NULL,
    [CurrencyCode]               NVARCHAR(10)   NOT NULL CONSTRAINT [DF_IncommingInvoices_CurrencyCode]    DEFAULT ('EUR'),

    -- Classificatie
    [StatusId]                   TINYINT        NOT NULL CONSTRAINT [DF_IncommingInvoices_StatusId]        DEFAULT (0),
    [DocumentType]               NVARCHAR(50)   NOT NULL CONSTRAINT [DF_IncommingInvoices_DocumentType]    DEFAULT ('Invoice'),
    [Source]                     NVARCHAR(50)   NOT NULL CONSTRAINT [DF_IncommingInvoices_Source]          DEFAULT ('Manual'),
    [Notes]                      NVARCHAR(2000) NULL,

    -- Octopus sync metadata
    [OctopusExternalId]          NVARCHAR(200)  NULL,
    [SyncedAt]                   DATETIME2      NULL,
    [SyncError]                  NVARCHAR(2000) NULL,

    -- Future-proof: Octopus booking-velden (fase 2)
    [OctopusBookyearId]          INT            NULL,
    [OctopusJournalKey]          NVARCHAR(50)   NULL,
    [OctopusDocumentSequenceNr]  INT            NULL,
    [OctopusBookedAt]            DATETIME2      NULL,
    [OctopusBookedBy]            NVARCHAR(200)  NULL,
    [OctopusBookingStatus]       NVARCHAR(100)  NULL,

    -- Audit
    [CreatedAt]                  DATETIME2      NULL,
    [UpdatedAt]                  DATETIME2      NULL;

GO

-- 3. FK voor IssuerCompanyId
ALTER TABLE [dbo].[IncommingInvoices]
    ADD CONSTRAINT [FK_IncommingInvoices_IssuerCompany]
    FOREIGN KEY ([IssuerCompanyId])
    REFERENCES [dbo].[IssuerCompany]([Id]);

GO

-- 4. Indexes voor documentencentrum-queries
CREATE NONCLUSTERED INDEX [IX_IncommingInvoices_DuplicateCheck]
    ON [dbo].[IncommingInvoices] ([IssuerCompanyId], [SupplierVatNumber], [ExternalId], [Date]);

CREATE NONCLUSTERED INDEX [IX_IncommingInvoices_OctopusExternalId]
    ON [dbo].[IncommingInvoices] ([OctopusExternalId])
    WHERE [OctopusExternalId] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_IncommingInvoices_IssuerStatus]
    ON [dbo].[IncommingInvoices] ([IssuerCompanyId], [StatusId]);

CREATE NONCLUSTERED INDEX [IX_IncommingInvoices_IssuerSource]
    ON [dbo].[IncommingInvoices] ([IssuerCompanyId], [Source]);

GO

-- 5. Per-bedrijf aankoopdagboek prefix (overschrijft globale appsettings.json instelling)
ALTER TABLE [dbo].[IssuerCompany]
    ADD [OctopusPurchaseJournalKeyPrefix] NVARCHAR(20) NULL;

GO

-- 6. Nieuwe bijlagentabel
CREATE TABLE [dbo].[IncomingInvoiceAttachments] (
    [Id]                  INT             IDENTITY(1,1) NOT NULL,
    [IncomingInvoiceId]   INT             NOT NULL,
    [FileName]            NVARCHAR(500)   NOT NULL,
    [ContentType]         NVARCHAR(200)   NULL,
    [AttachmentType]      NVARCHAR(50)    NOT NULL CONSTRAINT [DF_IncomingInvoiceAttachments_Type]      DEFAULT ('PDF'),
    [Content]             VARBINARY(MAX)  NULL,
    [FilePath]            NVARCHAR(1000)  NULL,
    [FileSizeBytes]       BIGINT          NULL,
    [CreatedAt]           DATETIME2       NOT NULL CONSTRAINT [DF_IncomingInvoiceAttachments_CreatedAt] DEFAULT (GETUTCDATE()),

    CONSTRAINT [PK_IncomingInvoiceAttachments] PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_IncomingInvoiceAttachments_Invoice]
        FOREIGN KEY ([IncomingInvoiceId])
        REFERENCES [dbo].[IncommingInvoices]([Id])
        ON DELETE CASCADE
);

GO
