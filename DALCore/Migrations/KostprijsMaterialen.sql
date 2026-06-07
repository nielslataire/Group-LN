CREATE TABLE [dbo].[KmIndexType] (
    [Id]   INT          IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(20) NOT NULL,
    [Naam] NVARCHAR(100) NOT NULL,
    CONSTRAINT [PK_KmIndexType] PRIMARY KEY ([Id])
);
INSERT INTO [dbo].[KmIndexType] ([Code],[Naam]) VALUES
    ('I2021','I-index (materialen)'),
    ('S2021','S-index (lonen)'),
    ('GW','Gewogen index (I×0.40 + S×0.40 + 0.20)'),
    ('LK','Loonkost'),
    ('NVT','Niet van toepassing');
CREATE TABLE [dbo].[KostprijsCategorie] (
    [Id]        INT           IDENTITY(1,1) NOT NULL,
    [Naam]      NVARCHAR(100) NOT NULL,
    [LotNummer] INT           NULL,
    [Volgorde]  INT           NOT NULL DEFAULT(0),
    CONSTRAINT [PK_KostprijsCategorie] PRIMARY KEY ([Id])
);
CREATE TABLE [dbo].[KostprijsMateriaal] (
    [Id]                INT            IDENTITY(1,1) NOT NULL,
    [CategorieId]       INT            NOT NULL,
    [Naam]              NVARCHAR(150)  NOT NULL,
    [Eenheid]           NVARCHAR(20)   NOT NULL,
    [Toepassingsgebied] NVARCHAR(50)   NULL,
    [KmIndexTypeId]     INT            NOT NULL,
    [ReferentiePrijs]   DECIMAL(19,4)  NOT NULL DEFAULT(0),
    [ReferentieDatum]   DATE           NOT NULL,
    [Volgorde]          INT            NOT NULL DEFAULT(0),
    CONSTRAINT [PK_KostprijsMateriaal]           PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KostprijsMateriaal_Categorie] FOREIGN KEY ([CategorieId])   REFERENCES [dbo].[KostprijsCategorie]([Id]),
    CONSTRAINT [FK_KostprijsMateriaal_IndexType] FOREIGN KEY ([KmIndexTypeId]) REFERENCES [dbo].[KmIndexType]([Id])
);
CREATE TABLE [dbo].[ProjectKostprijs] (
    [Id]                   INT            IDENTITY(1,1) NOT NULL,
    [ProjectId]            INT            NOT NULL,
    [KostprijsMateriaalId] INT            NOT NULL,
    [CategorieNaam]        NVARCHAR(100)  NOT NULL,
    [LotNummer]            INT            NULL,
    [Naam]                 NVARCHAR(150)  NOT NULL,
    [Eenheid]              NVARCHAR(20)   NOT NULL,
    [IndexTypeCode]        NVARCHAR(20)   NOT NULL,
    [Prijs]                DECIMAL(19,4)  NOT NULL DEFAULT(0),
    [ReferentieDatum]      DATE           NOT NULL,
    [SnapshotDatum]        DATETIME2      NOT NULL DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [PK_ProjectKostprijs]           PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectKostprijs_Project]   FOREIGN KEY ([ProjectId])            REFERENCES [dbo].[Project]([ProjectID]),
    CONSTRAINT [FK_ProjectKostprijs_Materiaal] FOREIGN KEY ([KostprijsMateriaalId]) REFERENCES [dbo].[KostprijsMateriaal]([Id])
);
