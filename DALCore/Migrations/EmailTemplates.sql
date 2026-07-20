-- ============================================================
-- EmailTemplate — herbruikbare e-mailtemplates (onderwerp + tekst)
-- Uitvoeren op de CPM-database vóór deployment.
-- ============================================================

CREATE TABLE [dbo].[EmailTemplate]
(
    [Id]            INT            NOT NULL IDENTITY(1,1),
    [Naam]          NVARCHAR(200)  NOT NULL,
    [Onderwerp]     NVARCHAR(300)  NOT NULL,
    [BodyHtml]      NVARCHAR(MAX)  NULL,
    [IsActief]      BIT            NOT NULL CONSTRAINT [DF_EmailTemplate_IsActief] DEFAULT (1),
    [AangemaaktOp]  DATETIME2(7)   NOT NULL CONSTRAINT [DF_EmailTemplate_AangemaaktOp] DEFAULT (SYSDATETIME()),
    [GewijzigdOp]   DATETIME2(7)   NOT NULL CONSTRAINT [DF_EmailTemplate_GewijzigdOp]  DEFAULT (SYSDATETIME()),

    CONSTRAINT [PK_EmailTemplate]
        PRIMARY KEY CLUSTERED ([Id])
);
GO
