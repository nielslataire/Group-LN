-- ============================================================
-- UserEmailSignature — persoonlijke e-mailhandtekening per gebruiker
-- Uitvoeren op de CPM-database vóór deployment.
-- ============================================================

CREATE TABLE [dbo].[UserEmailSignature]
(
    [Id]              INT            NOT NULL IDENTITY(1,1),
    [UserId]          INT            NOT NULL,
    [SignatureHtml]   NVARCHAR(MAX)  NULL,
    -- 'Visual' = laatst bewerkt via de Quill-editor, 'Html' = laatst bewerkt/geplakt als ruwe HTML-bron.
    -- Bepaalt welk tabblad standaard actief is en voorkomt dat geplakte HTML (bv. Outlook-handtekeningen
    -- met tabellen) automatisch door de Quill-editor vereenvoudigd wordt bij het laden van de pagina.
    [SignatureFormat] NVARCHAR(20)   NOT NULL CONSTRAINT [DF_UserEmailSignature_SignatureFormat] DEFAULT ('Visual'),
    [GewijzigdOp]     DATETIME2(7)   NOT NULL CONSTRAINT [DF_UserEmailSignature_GewijzigdOp] DEFAULT (SYSDATETIME()),

    CONSTRAINT [PK_UserEmailSignature]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_UserEmailSignature_Users]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
        ON DELETE CASCADE,

    -- Eén handtekening per gebruiker
    CONSTRAINT [UQ_UserEmailSignature_UserId]
        UNIQUE ([UserId])
);
GO
