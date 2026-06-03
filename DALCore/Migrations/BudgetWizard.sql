-- ============================================================
-- Budget Wizard tabellen
--
-- BudgetMaster  : budget scenario per project (bv. "8 Woningen")
-- BudgetVersie  : versies van een BudgetMaster
-- BudgetGegevens: algemene parameters voor één budget versie (stap 1 wizard)
-- ============================================================

-- ── 1. BudgetMaster ──────────────────────────────────────────────────────────

CREATE TABLE [dbo].[BudgetMaster]
(
    [Id]                INT             NOT NULL IDENTITY(1,1),
    [ProjectId]         INT             NOT NULL,
    [Naam]              NVARCHAR(200)   NOT NULL,
    [Omschrijving]      NVARCHAR(500)   NULL,
    [IsActief]          BIT             NOT NULL CONSTRAINT [DF_BudgetMaster_IsActief] DEFAULT 1,
    [IsGearchiveerd]    BIT             NOT NULL CONSTRAINT [DF_BudgetMaster_IsGearchiveerd] DEFAULT 0,
    [CreatedAt]         DATETIME2       NOT NULL CONSTRAINT [DF_BudgetMaster_CreatedAt] DEFAULT GETDATE(),
    [CreatedByUserId]   INT             NULL,

    CONSTRAINT [PK_BudgetMaster]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_BudgetMaster_Project]
        FOREIGN KEY ([ProjectId])
        REFERENCES [dbo].[Project] ([ProjectId])
        ON DELETE CASCADE
);

GO

CREATE NONCLUSTERED INDEX [IX_BudgetMaster_ProjectId]
    ON [dbo].[BudgetMaster] ([ProjectId] ASC);

GO

-- ── 2. BudgetVersie ──────────────────────────────────────────────────────────

CREATE TABLE [dbo].[BudgetVersie]
(
    [Id]                INT             NOT NULL IDENTITY(1,1),
    [BudgetMasterId]    INT             NOT NULL,
    [ProjectId]         INT             NOT NULL,
    [Versienummer]      INT             NOT NULL,
    [VersieNaam]        NVARCHAR(200)   NULL,
    [Status]            NVARCHAR(20)    NOT NULL CONSTRAINT [DF_BudgetVersie_Status] DEFAULT 'Concept',
    [IsHuidig]          BIT             NOT NULL CONSTRAINT [DF_BudgetVersie_IsHuidig] DEFAULT 0,
    [Notitie]           NVARCHAR(1000)  NULL,
    [CreatedAt]         DATETIME2       NOT NULL CONSTRAINT [DF_BudgetVersie_CreatedAt] DEFAULT GETDATE(),
    [CreatedByUserId]   INT             NULL,

    CONSTRAINT [PK_BudgetVersie]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_BudgetVersie_BudgetMaster]
        FOREIGN KEY ([BudgetMasterId])
        REFERENCES [dbo].[BudgetMaster] ([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_BudgetVersie_Project]
        FOREIGN KEY ([ProjectId])
        REFERENCES [dbo].[Project] ([ProjectId])
        ON DELETE NO ACTION
);

GO

CREATE NONCLUSTERED INDEX [IX_BudgetVersie_BudgetMasterId]
    ON [dbo].[BudgetVersie] ([BudgetMasterId] ASC);

CREATE NONCLUSTERED INDEX [IX_BudgetVersie_ProjectId]
    ON [dbo].[BudgetVersie] ([ProjectId] ASC);

GO

-- ── 3. BudgetGegevens ────────────────────────────────────────────────────────

CREATE TABLE [dbo].[BudgetGegevens]
(
    [Id]                            INT             NOT NULL IDENTITY(1,1),
    [BudgetVersieId]                INT             NOT NULL,
    [Naam]                          NVARCHAR(200)   NULL,
    [Adres]                         NVARCHAR(300)   NULL,
    [BouwheerCompanyId]             INT             NULL,
    [AantalLiften]                  INT             NOT NULL CONSTRAINT [DF_BudgetGegevens_AantalLiften] DEFAULT 0,
    [AantalBinnentrappen]           INT             NOT NULL CONSTRAINT [DF_BudgetGegevens_AantalBinnentrappen] DEFAULT 0,
    [AantalBovengrondseVerdiepingen] INT            NOT NULL CONSTRAINT [DF_BudgetGegevens_AantalBovengrondseVerdiepingen] DEFAULT 0,
    [AantalVerdiepingenOndergronds] INT             NOT NULL CONSTRAINT [DF_BudgetGegevens_AantalVerdiepingenOndergronds] DEFAULT 0,
    [TypePoorten]                   NVARCHAR(100)   NULL,
    [TypeDak]                       NVARCHAR(50)    NULL,
    [GevelLeienSidings]             INT             NOT NULL CONSTRAINT [DF_BudgetGegevens_GevelLeienSidings] DEFAULT 0,
    [OppFunderingen]                DECIMAL(10,2)   NULL,
    [M3Grondwerk]                   DECIMAL(10,2)   NULL,
    [LmBerlinerwanden]              DECIMAL(10,2)   NULL,
    [LmSecanpalen]                  DECIMAL(10,2)   NULL,
    [NacalcBasisprijs]              DECIMAL(8,2)    NULL,
    [NacalcBasisJaar]               INT             NULL,
    [ABEXBasisIndex]                DECIMAL(8,4)    NULL,
    [ABEXHuidigIndex]               DECIMAL(8,4)    NULL,
    [GevelMetselwerkPrijsPerM2]     DECIMAL(8,2)    NULL CONSTRAINT [DF_BudgetGegevens_GevelMetselwerk] DEFAULT 165,
    [GipswerkenPrijsPerM2]          DECIMAL(8,2)    NULL CONSTRAINT [DF_BudgetGegevens_Gipswerken] DEFAULT 2759,
    [UpdatedAt]                     DATETIME2       NULL,

    CONSTRAINT [PK_BudgetGegevens]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [UQ_BudgetGegevens_BudgetVersieId]
        UNIQUE ([BudgetVersieId]),

    CONSTRAINT [FK_BudgetGegevens_BudgetVersie]
        FOREIGN KEY ([BudgetVersieId])
        REFERENCES [dbo].[BudgetVersie] ([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_BudgetGegevens_CompanyInfo]
        FOREIGN KEY ([BouwheerCompanyId])
        REFERENCES [dbo].[CompanyInfo] ([CompanyID])
        ON DELETE SET NULL
);

GO

CREATE NONCLUSTERED INDEX [IX_BudgetGegevens_BudgetVersieId]
    ON [dbo].[BudgetGegevens] ([BudgetVersieId] ASC);

GO
