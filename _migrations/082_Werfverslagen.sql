-- =============================================
-- Migratie: 082_Werfverslagen
-- Datum: 2026-10-09
-- Omschrijving: Verslagen bij Punten (design-handoff 40i/40l): werfverslagen en opleveringen bundelen de punten van één bezoek.
--   Werfverslag       : kop (type, naam, datum, uur, aanwezigen als JSON, weer, opmerkingen, volgend bezoek, status Concept/Verstuurd,
--                       hernomen van het vorige verslag).
--   WerfverslagPunt   : punt in een verslag (nieuw tijdens het bezoek, of overgenomen als openstaand) met wat je ter plaatse zag
--                       (Blijft open / Opgelost / Afgesloten) en een opmerking.
--   Strikt additief + idempotent.
-- =============================================
IF OBJECT_ID('dbo.Werfverslag', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Werfverslag] (
        [Id]               INT IDENTITY(1,1) NOT NULL,
        [ProjectId]        INT            NOT NULL,
        [VerslagType]      TINYINT        NOT NULL CONSTRAINT DF_Werfverslag_Type DEFAULT 0,
        [Nummer]           INT            NOT NULL CONSTRAINT DF_Werfverslag_Nummer DEFAULT 1,
        [Naam]             NVARCHAR(150)  NOT NULL,
        [Datum]            DATE           NOT NULL,
        [Uur]              TIME(0)        NULL,
        [AanwezigenJson]   NVARCHAR(MAX)  NULL,
        [Weer]             NVARCHAR(100)  NULL,
        [Opmerkingen]      NVARCHAR(MAX)  NULL,
        [VolgendBezoek]    DATETIME2(0)   NULL,
        [Status]           TINYINT        NOT NULL CONSTRAINT DF_Werfverslag_Status DEFAULT 0,
        [HernomenVanId]    INT            NULL,
        [CreatedAt]        DATETIME2(0)   NOT NULL CONSTRAINT DF_Werfverslag_CreatedAt DEFAULT SYSUTCDATETIME(),
        [CreatedBy]        NVARCHAR(100)  NULL,
        [VerzondenOp]      DATETIME2(0)   NULL,
        [VerzondenDoor]    NVARCHAR(100)  NULL,
        CONSTRAINT PK_Werfverslag PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_Werfverslag_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE,
        CONSTRAINT FK_Werfverslag_Vorig FOREIGN KEY ([HernomenVanId]) REFERENCES [dbo].[Werfverslag]([Id])
    );
    CREATE NONCLUSTERED INDEX IX_Werfverslag_Project ON [dbo].[Werfverslag]([ProjectId], [Datum] DESC);
    PRINT 'Tabel Werfverslag aangemaakt.';
END
ELSE PRINT 'Tabel Werfverslag bestaat al, overgeslagen.';
GO
IF OBJECT_ID('dbo.WerfverslagPunt', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WerfverslagPunt] (
        [Id]          INT IDENTITY(1,1) NOT NULL,
        [VerslagId]   INT           NOT NULL,
        [IssueId]     INT           NOT NULL,
        [IsNieuw]     BIT           NOT NULL CONSTRAINT DF_WerfverslagPunt_IsNieuw DEFAULT 0,
        [TerPlaatse]  TINYINT       NULL,
        [Opmerking]   NVARCHAR(300) NULL,
        CONSTRAINT PK_WerfverslagPunt PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_WerfverslagPunt UNIQUE ([VerslagId], [IssueId]),
        CONSTRAINT FK_WerfverslagPunt_Verslag FOREIGN KEY ([VerslagId]) REFERENCES [dbo].[Werfverslag]([Id]) ON DELETE CASCADE,
        CONSTRAINT FK_WerfverslagPunt_Issue FOREIGN KEY ([IssueId]) REFERENCES [dbo].[ConstructionIssue]([Id])
    );
    CREATE NONCLUSTERED INDEX IX_WerfverslagPunt_Issue ON [dbo].[WerfverslagPunt]([IssueId]);
    PRINT 'Tabel WerfverslagPunt aangemaakt.';
END
ELSE PRINT 'Tabel WerfverslagPunt bestaat al, overgeslagen.';
GO
