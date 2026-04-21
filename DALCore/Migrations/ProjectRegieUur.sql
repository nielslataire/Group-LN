-- ============================================================
-- ProjectRegieUur — regie-uren per coördinatieproject
-- Uitvoeren op de CPM-database vóór deployment.
-- ============================================================

CREATE TABLE [dbo].[ProjectRegieUur]
(
    [Id]          INT             NOT NULL IDENTITY(1,1),
    [ProjectId]   INT             NOT NULL,
    [UserId]      NVARCHAR(128)   NOT NULL,
    [Date]        DATE            NOT NULL,
    [Hours]       DECIMAL(6, 2)   NOT NULL,
    [WithTravel]  BIT             NOT NULL CONSTRAINT [DF_ProjectRegieUur_WithTravel] DEFAULT (0),
    [CreatedAt]   DATETIME2(7)    NOT NULL CONSTRAINT [DF_ProjectRegieUur_CreatedAt]  DEFAULT (SYSUTCDATETIME()),

    -- Facturatie: NULL = nog niet gefactureerd, gevuld = gefactureerd op die factuur
    [InvoiceId]   INT             NULL,

    CONSTRAINT [PK_ProjectRegieUur]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_ProjectRegieUur_Project]
        FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID])
        ON DELETE CASCADE,

    CONSTRAINT [FK_ProjectRegieUur_Users]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserID]),

    CONSTRAINT [FK_ProjectRegieUur_Invoice]
        FOREIGN KEY ([InvoiceId]) REFERENCES [dbo].[Invoices]([Id])
        ON DELETE SET NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_ProjectRegieUur_ProjectId]
    ON [dbo].[ProjectRegieUur] ([ProjectId]);
GO

CREATE NONCLUSTERED INDEX [IX_ProjectRegieUur_InvoiceId]
    ON [dbo].[ProjectRegieUur] ([InvoiceId])
    WHERE [InvoiceId] IS NOT NULL;
GO
