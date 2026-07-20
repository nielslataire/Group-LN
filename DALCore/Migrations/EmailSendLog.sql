-- ============================================================
-- EmailSendLog — historiek van verzonden template-mails naar projectcontacten
-- Uitvoeren op de CPM-database vóór deployment.
-- ============================================================

CREATE TABLE [dbo].[EmailSendLog]
(
    [Id]                  INT            NOT NULL IDENTITY(1,1),
    [ProjectId]           INT            NOT NULL,
    [ContactEmail]        NVARCHAR(320)  NOT NULL,
    [ContactNaam]         NVARCHAR(300)  NULL,
    [EmailTemplateId]     INT            NULL,
    [TemplateNaam]        NVARCHAR(200)  NULL,
    [Onderwerp]           NVARCHAR(300)  NULL,
    [VerzondenDoorUserId] INT            NOT NULL,
    [VerzondenDoorNaam]   NVARCHAR(300)  NULL,
    [VerzondenOp]         DATETIME2(7)   NOT NULL CONSTRAINT [DF_EmailSendLog_VerzondenOp] DEFAULT (SYSDATETIME()),

    CONSTRAINT [PK_EmailSendLog]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_EmailSendLog_EmailTemplate]
        FOREIGN KEY ([EmailTemplateId]) REFERENCES [dbo].[EmailTemplate]([Id])
        ON DELETE SET NULL,

    CONSTRAINT [FK_EmailSendLog_Users]
        FOREIGN KEY ([VerzondenDoorUserId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_EmailSendLog_ProjectId_ContactEmail]
    ON [dbo].[EmailSendLog] ([ProjectId], [ContactEmail]);
GO
