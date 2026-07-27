-- ============================================================
-- MarktanalyseZoekProfiel + MarktanalyseZoekActie
-- Opgeslagen zoekprofielen en zoekhistoriek voor "Vergelijkbare panden".
-- Uitvoeren op de CPM-database vóór deployment.
-- ============================================================

CREATE TABLE [dbo].[MarktanalyseZoekProfiel]
(
    [Id]                INT             NOT NULL IDENTITY(1,1),
    [UserId]            INT             NOT NULL,
    [Naam]              NVARCHAR(200)   NOT NULL,
    [ZoekgebiedTab]     NVARCHAR(20)    NOT NULL,
    [GemeenteIdsJson]   NVARCHAR(MAX)   NULL,
    [RondAdresPostcode] NVARCHAR(20)    NULL,
    [RondAdresLat]      FLOAT           NULL,
    [RondAdresLng]      FLOAT           NULL,
    [RondAdresStraal]   INT             NOT NULL CONSTRAINT [DF_MarktanalyseZoekProfiel_RondAdresStraal] DEFAULT (1000),
    [Type]              NVARCHAR(20)    NOT NULL,
    [Oppervlakte]       DECIMAL(18, 2)  NULL,
    [Tolerantie]        INT             NOT NULL CONSTRAINT [DF_MarktanalyseZoekProfiel_Tolerantie] DEFAULT (10),
    [PrijsMin]          DECIMAL(18, 2)  NULL,
    [PrijsMax]          DECIMAL(18, 2)  NULL,
    [Slaapkamers]       INT             NULL,
    [Status]            NVARCHAR(20)    NOT NULL CONSTRAINT [DF_MarktanalyseZoekProfiel_Status] DEFAULT ('Alles'),
    [CreatedDate]       DATETIME2(7)    NOT NULL CONSTRAINT [DF_MarktanalyseZoekProfiel_CreatedDate] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_MarktanalyseZoekProfiel]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_MarktanalyseZoekProfiel_Users]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_MarktanalyseZoekProfiel_UserId]
    ON [dbo].[MarktanalyseZoekProfiel] ([UserId]);
GO

CREATE TABLE [dbo].[MarktanalyseZoekActie]
(
    [Id]                INT             NOT NULL IDENTITY(1,1),
    [UserId]            INT             NOT NULL,
    [ZoekgebiedTab]     NVARCHAR(20)    NOT NULL,
    [GemeenteIdsJson]   NVARCHAR(MAX)   NULL,
    [RondAdresPostcode] NVARCHAR(20)    NULL,
    [RondAdresLat]      FLOAT           NULL,
    [RondAdresLng]      FLOAT           NULL,
    [RondAdresStraal]   INT             NOT NULL CONSTRAINT [DF_MarktanalyseZoekActie_RondAdresStraal] DEFAULT (1000),
    [Type]              NVARCHAR(20)    NOT NULL,
    [Oppervlakte]       DECIMAL(18, 2)  NULL,
    [Tolerantie]        INT             NOT NULL CONSTRAINT [DF_MarktanalyseZoekActie_Tolerantie] DEFAULT (10),
    [PrijsMin]          DECIMAL(18, 2)  NULL,
    [PrijsMax]          DECIMAL(18, 2)  NULL,
    [Slaapkamers]       INT             NULL,
    [Status]            NVARCHAR(20)    NOT NULL CONSTRAINT [DF_MarktanalyseZoekActie_Status] DEFAULT ('Alles'),
    [AantalResultaten]  INT             NOT NULL CONSTRAINT [DF_MarktanalyseZoekActie_AantalResultaten] DEFAULT (0),
    [UitgevoerdOp]      DATETIME2(7)    NOT NULL CONSTRAINT [DF_MarktanalyseZoekActie_UitgevoerdOp] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_MarktanalyseZoekActie]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_MarktanalyseZoekActie_Users]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_MarktanalyseZoekActie_UserId_UitgevoerdOp]
    ON [dbo].[MarktanalyseZoekActie] ([UserId], [UitgevoerdOp] DESC);
GO
