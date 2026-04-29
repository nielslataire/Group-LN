-- ============================================================
-- Verrijkings- en leerlagentabellen voor het Documentencentrum
--
-- IncomingInvoiceEnrichment  : uitkomst verrijkingspipeline per factuur
-- IncomingInvoiceWarning     : individuele waarschuwingen per factuur
-- InvoiceSupplierRule        : door gebruiker opgeslagen leverancierregels
-- InvoiceProjectAlias        : alternatieve namen/codes per project
-- ============================================================

-- ── 1. IncomingInvoiceEnrichment ─────────────────────────────────────────────

CREATE TABLE [dbo].[IncomingInvoiceEnrichment]
(
    [Id]                            INT            NOT NULL IDENTITY(1,1),
    [IncomingInvoiceId]             INT            NOT NULL,

    [EnrichedAt]                    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),

    -- Documentclassificatie
    [DocumentClassification]        NVARCHAR(50)   NULL,

    -- Leverancier-matching
    [SupplierMatchConfidence]       NVARCHAR(20)   NULL,
    [SupplierMatchReason]           NVARCHAR(500)  NULL,
    [SupplierIban]                  NVARCHAR(50)   NULL,

    -- Projectvoorstel
    [SuggestedProjectId]            INT            NULL,
    [SuggestedProjectName]          NVARCHAR(250)  NULL,
    [SuggestedProjectScore]         INT            NOT NULL DEFAULT 0,
    [SuggestedProjectConfidence]    NVARCHAR(20)   NULL,
    [SuggestedProjectMatchReason]   NVARCHAR(500)  NULL,
    [SuggestedProjectMatchedFields] NVARCHAR(200)  NULL,
    [AltProjectCandidatesJson]      NVARCHAR(MAX)  NULL,

    -- Contractvoorstel
    [SuggestedContractId]           INT            NULL,
    [SuggestedContractName]         NVARCHAR(250)  NULL,
    [SuggestedContractScore]        INT            NOT NULL DEFAULT 0,
    [SuggestedContractConfidence]   NVARCHAR(20)   NULL,
    [AltContractCandidatesJson]     NVARCHAR(MAX)  NULL,

    -- Kostenrekening
    [SuggestedAccountingCode]       NVARCHAR(100)  NULL,
    [SuggestedAccountingDescription] NVARCHAR(250) NULL,
    [SuggestedAccountingSource]     NVARCHAR(50)   NULL,
    [IsGeneralCostSuggested]        BIT            NOT NULL DEFAULT 0,

    -- Reason flags (bitmask van InvoiceReasonFlags)
    [ReasonFlags]                   BIGINT         NOT NULL DEFAULT 0,

    -- Duplicaatcontrole
    [IsDuplicateSuspected]          BIT            NOT NULL DEFAULT 0,
    [DuplicateSuspectInvoiceId]     INT            NULL,

    -- Volledig extractieresultaat (JSON)
    [ExtractionResultJson]          NVARCHAR(MAX)  NULL,

    -- Gebruikersbeslissingen
    [UserReviewedAt]                DATETIME2      NULL,
    [UserReviewedBy]                NVARCHAR(200)  NULL,
    [UserConfirmedProjectId]        INT            NULL,
    [UserConfirmedContractId]       INT            NULL,
    [UserConfirmedAccountingCode]   NVARCHAR(100)  NULL,
    [UserNotes]                     NVARCHAR(2000) NULL,

    CONSTRAINT [PK_IncomingInvoiceEnrichment]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_IncomingInvoiceEnrichment_Invoice]
        FOREIGN KEY ([IncomingInvoiceId])
        REFERENCES [dbo].[IncommingInvoices] ([ID])
        ON DELETE CASCADE,

    CONSTRAINT [FK_IncomingInvoiceEnrichment_SuggestedProject]
        FOREIGN KEY ([SuggestedProjectId])
        REFERENCES [dbo].[Project] ([ProjectId])
        ON DELETE NO ACTION,

    CONSTRAINT [FK_IncomingInvoiceEnrichment_ConfirmedProject]
        FOREIGN KEY ([UserConfirmedProjectId])
        REFERENCES [dbo].[Project] ([ProjectId])
        ON DELETE NO ACTION
);

GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_IncomingInvoiceEnrichment_InvoiceId]
    ON [dbo].[IncomingInvoiceEnrichment] ([IncomingInvoiceId] ASC);

GO

-- ── 2. IncomingInvoiceWarning ────────────────────────────────────────────────

CREATE TABLE [dbo].[IncomingInvoiceWarning]
(
    [Id]                INT            NOT NULL IDENTITY(1,1),
    [IncomingInvoiceId] INT            NOT NULL,
    [WarningCode]       NVARCHAR(100)  NOT NULL,
    [WarningMessage]    NVARCHAR(1000) NULL,
    [Severity]          NVARCHAR(20)   NOT NULL DEFAULT 'Warning',
    [Source]            NVARCHAR(50)   NULL,
    [IsResolved]        BIT            NOT NULL DEFAULT 0,
    [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_IncomingInvoiceWarning]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_IncomingInvoiceWarning_Invoice]
        FOREIGN KEY ([IncomingInvoiceId])
        REFERENCES [dbo].[IncommingInvoices] ([ID])
        ON DELETE CASCADE
);

GO

CREATE NONCLUSTERED INDEX [IX_IncomingInvoiceWarning_InvoiceId]
    ON [dbo].[IncomingInvoiceWarning] ([IncomingInvoiceId] ASC);

GO

-- ── 3. InvoiceSupplierRule ───────────────────────────────────────────────────

CREATE TABLE [dbo].[InvoiceSupplierRule]
(
    [Id]                    INT            NOT NULL IDENTITY(1,1),
    [IssuerCompanyId]       INT            NULL,
    [CompanyId]             INT            NULL,
    [SupplierVatNumber]     NVARCHAR(50)   NULL,
    [SupplierNameContains]  NVARCHAR(250)  NULL,
    [RuleType]              NVARCHAR(100)  NOT NULL,
    [DefaultProjectId]      INT            NULL,
    [DefaultContractId]     INT            NULL,
    [DefaultAccountingCode] NVARCHAR(100)  NULL,
    [DefaultVatCode]        NVARCHAR(50)   NULL,
    [IsGeneralCost]         BIT            NOT NULL DEFAULT 0,
    [RequiresManualReview]  BIT            NOT NULL DEFAULT 0,
    [RequiresProject]       BIT            NOT NULL DEFAULT 0,
    [Notes]                 NVARCHAR(1000) NULL,
    [CreatedAt]             DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy]             NVARCHAR(200)  NULL,
    [IsActive]              BIT            NOT NULL DEFAULT 1,

    CONSTRAINT [PK_InvoiceSupplierRule]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_InvoiceSupplierRule_Company]
        FOREIGN KEY ([CompanyId])
        REFERENCES [dbo].[CompanyInfo] ([Id])
        ON DELETE SET NULL,

    CONSTRAINT [FK_InvoiceSupplierRule_IssuerCompany]
        FOREIGN KEY ([IssuerCompanyId])
        REFERENCES [dbo].[IssuerCompany] ([Id])
        ON DELETE SET NULL,

    CONSTRAINT [FK_InvoiceSupplierRule_Project]
        FOREIGN KEY ([DefaultProjectId])
        REFERENCES [dbo].[Project] ([ProjectId])
        ON DELETE SET NULL
);

GO

CREATE NONCLUSTERED INDEX [IX_InvoiceSupplierRule_Supplier]
    ON [dbo].[InvoiceSupplierRule] ([IssuerCompanyId] ASC, [CompanyId] ASC);

CREATE NONCLUSTERED INDEX [IX_InvoiceSupplierRule_Vat]
    ON [dbo].[InvoiceSupplierRule] ([SupplierVatNumber] ASC)
    WHERE [SupplierVatNumber] IS NOT NULL;

GO

-- ── 4. InvoiceProjectAlias ───────────────────────────────────────────────────

CREATE TABLE [dbo].[InvoiceProjectAlias]
(
    [Id]        INT            NOT NULL IDENTITY(1,1),
    [ProjectId] INT            NOT NULL,
    [Alias]     NVARCHAR(250)  NOT NULL,
    [AliasType] NVARCHAR(50)   NULL,
    [CreatedAt] DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(200)  NULL,
    [IsActive]  BIT            NOT NULL DEFAULT 1,

    CONSTRAINT [PK_InvoiceProjectAlias]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_InvoiceProjectAlias_Project]
        FOREIGN KEY ([ProjectId])
        REFERENCES [dbo].[Project] ([ProjectId])
        ON DELETE CASCADE
);

GO

CREATE NONCLUSTERED INDEX [IX_InvoiceProjectAlias_Project]
    ON [dbo].[InvoiceProjectAlias] ([ProjectId] ASC);

GO
