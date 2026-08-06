-- ============================================================
-- HomeHeroProject — uitgelicht project op de WWWCOPRO home-hero
-- (kicker/titel/tekst/knoptekst, verwijst naar 1 te-koop-project).
-- Uitvoeren op de CPM-database vóór deployment.
-- ============================================================

CREATE TABLE [dbo].[HomeHeroProject]
(
    [Id]                    INT            NOT NULL IDENTITY(1,1),
    [ProjectId]             INT            NOT NULL,
    [Kicker]                NVARCHAR(200)  NULL,
    [Titel]                 NVARCHAR(300)  NULL,
    [Tekst]                 NVARCHAR(MAX)  NULL,
    [ProjectTitelOverride]  NVARCHAR(300)  NULL,
    [GewijzigdOp]           DATETIME2(7)   NOT NULL CONSTRAINT [DF_HomeHeroProject_GewijzigdOp] DEFAULT (SYSDATETIME()),

    CONSTRAINT [PK_HomeHeroProject]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_HomeHeroProject_Project]
        FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID])
);
GO
