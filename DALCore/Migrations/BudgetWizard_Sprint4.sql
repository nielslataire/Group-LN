-- ============================================================
-- Budget Wizard Sprint 4
-- BudgetParams, BudgetVerkoopLijnen, ABEXIndex, BudgetPrijsReferentie
-- Gebruik IF NOT EXISTS zodat het script opnieuw uitvoerbaar is.
-- ============================================================

-- ── BudgetParams ──────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'BudgetParams' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE [dbo].[BudgetParams]
    (
        [Id]                            INT             NOT NULL IDENTITY(1,1),
        [BudgetVersieId]                INT             NOT NULL,
        [ProjectcoordinatiePerc]        DECIMAL(8,4)    NOT NULL CONSTRAINT [DF_BudgetParams_ProjcoorPerc]    DEFAULT 0.0525,
        [ArchitectPerc]                 DECIMAL(8,4)    NULL,
        [VeiligheidscoordEPBPerc]       DECIMAL(8,4)    NULL,
        [VentVerslaggeverForfait]       DECIMAL(18,2)   NULL,
        [StudieIRPerc]                  DECIMAL(8,4)    NULL,
        [OpmetingSonderingForfait]      DECIMAL(18,2)   NULL,
        [DecennaleGeslRuwbouwPerc]      DECIMAL(8,4)    NULL,
        [ABRPlaatsbeschrPerc]           DECIMAL(8,4)    NULL,
        [InfrastructuurForfait]         DECIMAL(18,2)   NULL,
        [LiftPrijsPerStuk]              DECIMAL(18,2)   NULL,
        [WetBreynePerc]                 DECIMAL(8,4)    NOT NULL CONSTRAINT [DF_BudgetParams_WetBreynePerc]   DEFAULT 0.01,
        [WetBreyneMaanden]              INT             NULL,
        [StraightloanGebouwPerc]        DECIMAL(8,4)    NOT NULL CONSTRAINT [DF_BudgetParams_SlGebouwPerc]   DEFAULT 0.0125,
        [StraightloanGebouwMaanden]     INT             NULL,
        [StraightloanGrondPerc]         DECIMAL(8,4)    NOT NULL CONSTRAINT [DF_BudgetParams_SlGrondPerc]    DEFAULT 0.0125,
        [StraightloanGrondMaanden]      INT             NULL,
        [AankoopprijsGrond]             DECIMAL(18,2)   NULL,
        [OnvoorzienPerc]                DECIMAL(8,4)    NOT NULL CONSTRAINT [DF_BudgetParams_OnvoorzienPerc] DEFAULT 0.02,
        [PubliciteitForfait]            DECIMAL(18,2)   NULL,

        CONSTRAINT [PK_BudgetParams]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [UQ_BudgetParams_VersieId]
            UNIQUE ([BudgetVersieId]),

        CONSTRAINT [FK_BudgetParams_BudgetVersie]
            FOREIGN KEY ([BudgetVersieId])
            REFERENCES [dbo].[BudgetVersie] ([Id])
            ON DELETE CASCADE
    );
END

GO

-- ── BudgetVerkoopLijnen ───────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'BudgetVerkoopLijnen' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE [dbo].[BudgetVerkoopLijnen]
    (
        [Id]            INT             NOT NULL IDENTITY(1,1),
        [BudgetVersieId] INT            NOT NULL,
        [EenheidNaam]   NVARCHAR(100)   NULL,
        [UnitId]        INT             NULL,
        [CodeBouw]      INT             NULL,
        [CodeGrond]     INT             NULL,
        [OppTuin]       DECIMAL(10,2)   NULL,
        [OppTerras]     DECIMAL(10,2)   NULL,
        [OppDakterras]  DECIMAL(10,2)   NULL,
        [IsRuil]        BIT             NOT NULL CONSTRAINT [DF_BudgetVerkLijn_IsRuil]    DEFAULT 0,
        [ExtraForfait]  DECIMAL(18,2)   NULL,
        [SortOrder]     INT             NOT NULL CONSTRAINT [DF_BudgetVerkLijn_SortOrder] DEFAULT 0,

        CONSTRAINT [PK_BudgetVerkoopLijnen]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [FK_BudgetVerkLijn_BudgetVersie]
            FOREIGN KEY ([BudgetVersieId])
            REFERENCES [dbo].[BudgetVersie] ([Id])
            ON DELETE CASCADE,

        CONSTRAINT [FK_BudgetVerkLijn_Units]
            FOREIGN KEY ([UnitId])
            REFERENCES [dbo].[Units] ([ID])
            ON DELETE SET NULL
    );

    CREATE NONCLUSTERED INDEX [IX_BudgetVerkLijn_VersieId]
        ON [dbo].[BudgetVerkoopLijnen] ([BudgetVersieId] ASC, [SortOrder] ASC);
END

GO

-- ── ABEXIndex ─────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ABEXIndex' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE [dbo].[ABEXIndex]
    (
        [Id]            INT             NOT NULL IDENTITY(1,1),
        [Jaar]          INT             NOT NULL,
        [Kwartaal]      INT             NULL,
        [IndexWaarde]   DECIMAL(10,2)   NOT NULL,
        [IsActief]      BIT             NOT NULL CONSTRAINT [DF_ABEXIndex_IsActief] DEFAULT 0,

        CONSTRAINT [PK_ABEXIndex]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [UQ_ABEXIndex_JaarKwartaal]
            UNIQUE ([Jaar], [Kwartaal])
    );

    INSERT INTO [dbo].[ABEXIndex] ([Jaar], [Kwartaal], [IndexWaarde], [IsActief])
    VALUES
        (2022, NULL, 847.27, 0),
        (2023, NULL, 902.14, 0),
        (2024, NULL, 931.50, 0),
        (2025, NULL, 958.00, 1);
END

GO

-- ── BudgetPrijsReferentie ─────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'BudgetPrijsReferentie' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE [dbo].[BudgetPrijsReferentie]
    (
        [Id]            INT             NOT NULL IDENTITY(1,1),
        [ProjectId]     INT             NULL,
        [PrijsType]     NVARCHAR(10)    NOT NULL,
        [Code]          INT             NOT NULL,
        [PrijsPerM2]    DECIMAL(18,2)   NOT NULL,
        [Omschrijving]  NVARCHAR(200)   NULL,

        CONSTRAINT [PK_BudgetPrijsReferentie]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [CK_BudgetPrijsRef_PrijsType]
            CHECK ([PrijsType] IN (N'Bouw', N'Grond')),

        CONSTRAINT [FK_BudgetPrijsRef_Project]
            FOREIGN KEY ([ProjectId])
            REFERENCES [dbo].[Project] ([ProjectId])
            ON DELETE NO ACTION
    );
END

GO
