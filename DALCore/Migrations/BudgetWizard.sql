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

-- ── 4. BudgetOppervlaktes ─────────────────────────────────────────────────────

CREATE TABLE [dbo].[BudgetOppervlaktes]
(
    [Id]                        INT             NOT NULL IDENTITY(1,1),
    [BudgetVersieId]            INT             NOT NULL,
    [EenheidNaam]               NVARCHAR(100)   NOT NULL,
    [UnitGroupTypeId]           INT             NULL,
    [UnitTypeId]                INT             NULL,
    [SortOrder]                 INT             NOT NULL CONSTRAINT [DF_BudgetOpp_SortOrder]              DEFAULT 0,
    [BewoonbareOpp]             DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_BewoonbareOpp]          DEFAULT 0,
    [Tuin]                      DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_Tuin]                   DEFAULT 0,
    [TerrasPrefab]              DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_TerrasPrefab]           DEFAULT 0,
    [TerrasGelijkvloers]        DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_TerrasGelijkvloers]     DEFAULT 0,
    [Dakterras]                 DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_Dakterras]              DEFAULT 0,
    [GaragesParkingsBovenGr]    DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_GaragesParkingsBGR]     DEFAULT 0,
    [GarBergOndergronds]        DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_GarBergOndergronds]     DEFAULT 0,
    [BergGelijkvloers]          DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_BergGelijkvloers]       DEFAULT 0,
    [Carports]                  DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_Carports]               DEFAULT 0,
    [DoorritGVL]                DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_DoorritGVL]             DEFAULT 0,
    [Zolder]                    DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_Zolder]                 DEFAULT 0,
    [GemeenschappelijkeDelen]   DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_GemeenschappDelen]      DEFAULT 0,
    [Wegenis]                   DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_Wegenis]                DEFAULT 0,
    [Grondopp]                  DECIMAL(8,2)    NOT NULL CONSTRAINT [DF_BudgetOpp_Grondopp]               DEFAULT 0,

    CONSTRAINT [PK_BudgetOppervlaktes]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_BudgetOpp_BudgetVersie]
        FOREIGN KEY ([BudgetVersieId])
        REFERENCES [dbo].[BudgetVersie] ([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_BudgetOpp_UnitGroupType]
        FOREIGN KEY ([UnitGroupTypeId])
        REFERENCES [dbo].[UnitGroupTypes] ([Id])
        ON DELETE SET NULL,

    CONSTRAINT [FK_BudgetOpp_UnitType]
        FOREIGN KEY ([UnitTypeId])
        REFERENCES [dbo].[UnitTypes] ([Id])
        ON DELETE SET NULL
);

GO

CREATE NONCLUSTERED INDEX [IX_BudgetOpp_BudgetVersieId]
    ON [dbo].[BudgetOppervlaktes] ([BudgetVersieId] ASC, [SortOrder] ASC);

GO

-- ── 5. BudgetSanitair ────────────────────────────────────────────────────────

CREATE TABLE [dbo].[BudgetSanitair]
(
    [Id]                    INT             NOT NULL IDENTITY(1,1),
    [BudgetVersieId]        INT             NOT NULL,
    [EenheidNaam]           NVARCHAR(100)   NOT NULL,
    [UnitTypeId]            INT             NULL,
    [SortOrder]             INT             NOT NULL CONSTRAINT [DF_BudgetSanitair_SortOrder]              DEFAULT 0,
    [Badkamer]              INT             NOT NULL CONSTRAINT [DF_BudgetSanitair_Badkamer]              DEFAULT 0,
    [ToiletInBadkamer]      INT             NOT NULL CONSTRAINT [DF_BudgetSanitair_ToiletInBadkamer]      DEFAULT 0,
    [AfzonderlijkToilet]    INT             NOT NULL CONSTRAINT [DF_BudgetSanitair_AfzonderlijkToilet]    DEFAULT 0,
    [DoucheInBadkamer]      INT             NOT NULL CONSTRAINT [DF_BudgetSanitair_DoucheInBadkamer]      DEFAULT 0,
    [Douchekamer]           INT             NOT NULL CONSTRAINT [DF_BudgetSanitair_Douchekamer]           DEFAULT 0,

    CONSTRAINT [PK_BudgetSanitair]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_BudgetSanitair_BudgetVersie]
        FOREIGN KEY ([BudgetVersieId])
        REFERENCES [dbo].[BudgetVersie] ([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_BudgetSanitair_UnitType]
        FOREIGN KEY ([UnitTypeId])
        REFERENCES [dbo].[UnitTypes] ([Id])
        ON DELETE SET NULL
);

GO

CREATE NONCLUSTERED INDEX [IX_BudgetSanitair_BudgetVersieId]
    ON [dbo].[BudgetSanitair] ([BudgetVersieId] ASC, [SortOrder] ASC);

GO

-- ── 6. BudgetGevelElementen ───────────────────────────────────────────────────

CREATE TABLE [dbo].[BudgetGevelElementen]
(
    [Id]            INT             NOT NULL IDENTITY(1,1),
    [BudgetVersieId] INT            NOT NULL,
    [ElementType]   NVARCHAR(30)    NOT NULL,
    [EenheidNaam]   NVARCHAR(100)   NULL,
    [Beschrijving]  NVARCHAR(200)   NULL,
    [Aantal]        DECIMAL(6,2)    NOT NULL CONSTRAINT [DF_BudgetGevel_Aantal]   DEFAULT 1,
    [Breedte]       DECIMAL(8,3)    NULL,
    [Hoogte]        DECIMAL(8,3)    NULL,
    [Lengte]        DECIMAL(8,3)    NULL,
    [SortOrder]     INT             NOT NULL CONSTRAINT [DF_BudgetGevel_SortOrder] DEFAULT 0,

    CONSTRAINT [PK_BudgetGevelElementen]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_BudgetGevel_BudgetVersie]
        FOREIGN KEY ([BudgetVersieId])
        REFERENCES [dbo].[BudgetVersie] ([Id])
        ON DELETE CASCADE
);

GO

CREATE NONCLUSTERED INDEX [IX_BudgetGevel_VersieType]
    ON [dbo].[BudgetGevelElementen] ([BudgetVersieId] ASC, [ElementType] ASC, [SortOrder] ASC);

GO

-- ── 7. BudgetActivityLijnen ───────────────────────────────────────────────────

CREATE TABLE [dbo].[BudgetActivityLijnen]
(
    [Id]                            INT             NOT NULL IDENTITY(1,1),
    [BudgetVersieId]                INT             NOT NULL,
    [ActivityId]                    INT             NOT NULL,
    [NacalcPrijsPerEenheid]         DECIMAL(10,2)   NULL,
    [NacalcBronProjectId]           INT             NULL,
    [Correctiefactor]               DECIMAL(4,2)    NOT NULL CONSTRAINT [DF_BudgetActLijn_Correctiefactor]   DEFAULT 1,
    [AlternatievePrijsPerEenheid]   DECIMAL(10,2)   NULL,
    [IsManueel]                     BIT             NOT NULL CONSTRAINT [DF_BudgetActLijn_IsManueel]          DEFAULT 0,
    [VerhogingsPerc]                DECIMAL(5,2)    NOT NULL CONSTRAINT [DF_BudgetActLijn_VerhogingsPerc]     DEFAULT 0,
    [Omschrijving]                  NVARCHAR(200)   NULL,

    CONSTRAINT [PK_BudgetActivityLijnen]
        PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT [FK_BudgetActLijn_BudgetVersie]
        FOREIGN KEY ([BudgetVersieId])
        REFERENCES [dbo].[BudgetVersie] ([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_BudgetActLijn_Activity]
        FOREIGN KEY ([ActivityId])
        REFERENCES [dbo].[Activity] ([ActivityId])
        ON DELETE NO ACTION,

    CONSTRAINT [FK_BudgetActLijn_NacalcProject]
        FOREIGN KEY ([NacalcBronProjectId])
        REFERENCES [dbo].[Project] ([ProjectId])
        ON DELETE NO ACTION
);

GO

CREATE NONCLUSTERED INDEX [IX_BudgetActLijn_VersieActivity]
    ON [dbo].[BudgetActivityLijnen] ([BudgetVersieId] ASC, [ActivityId] ASC);

GO
