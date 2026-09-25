-- =============================================
-- Migratie: 054_OndertekeningPerLink
-- Datum: 2026-09-25
-- Omschrijving: Klant ondertekent een wijzigingsopdracht via een persoonlijke link + code per e-mail,
--   zonder CPM-login (Controllers/SigningController). Bouwt voort op 049_DocumentenModel (uitvoeren NA 049).
--     ProjectDocs.ChangeOrderId          document is de PDF van deze wijzigingsopdracht (FK ChangeOrder.ID)
--     DocumentSignatures.*               per ondertekenaar: e-mail, gehashte link-token + geldigheid, gehashte
--                                        e-mailcode (+ pogingen/limieten) en het bewijsdossier bij het tekenen
--                                        (ingetypte naam, IP, user-agent, akkoordtekst, hash van het PDF, referentie)
--   Van de token en de code wordt enkel een SHA-256 bewaard: een databaselek geeft dus geen bruikbare links.
--   Strikt additief + idempotent.
-- =============================================

------------------------------------------------------------
-- 1. ProjectDocs.ChangeOrderId
------------------------------------------------------------
IF COL_LENGTH('dbo.ProjectDocs', 'ChangeOrderId') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [ChangeOrderId] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProjectDocs_ChangeOrder')
    ALTER TABLE [dbo].[ProjectDocs] ADD CONSTRAINT [FK_ProjectDocs_ChangeOrder]
        FOREIGN KEY ([ChangeOrderId]) REFERENCES [dbo].[ChangeOrder]([ID]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProjectDocs_ChangeOrderId' AND object_id = OBJECT_ID(N'[dbo].[ProjectDocs]'))
    CREATE NONCLUSTERED INDEX [IX_ProjectDocs_ChangeOrderId] ON [dbo].[ProjectDocs] ([ChangeOrderId]) WHERE [ChangeOrderId] IS NOT NULL;
PRINT 'ProjectDocs.ChangeOrderId klaar.';
GO

------------------------------------------------------------
-- 2. DocumentSignatures: link, code en bewijs
------------------------------------------------------------
IF COL_LENGTH('dbo.DocumentSignatures', 'SignerEmail') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [SignerEmail] NVARCHAR(254) NULL;
IF COL_LENGTH('dbo.DocumentSignatures', 'NotifyEmail') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [NotifyEmail] NVARCHAR(254) NULL;       -- wie een melding krijgt na het tekenen
IF COL_LENGTH('dbo.DocumentSignatures', 'TokenHash') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [TokenHash] NVARCHAR(64) NULL;          -- SHA-256 (hex) van de link-token
IF COL_LENGTH('dbo.DocumentSignatures', 'TokenExpiresOn') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [TokenExpiresOn] DATETIME2(7) NULL;
IF COL_LENGTH('dbo.DocumentSignatures', 'CodeHash') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [CodeHash] NVARCHAR(64) NULL;           -- SHA-256 van de e-mailcode
IF COL_LENGTH('dbo.DocumentSignatures', 'CodeExpiresOn') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [CodeExpiresOn] DATETIME2(7) NULL;
IF COL_LENGTH('dbo.DocumentSignatures', 'CodeAttempts') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [CodeAttempts] INT NOT NULL CONSTRAINT [DF_DocumentSignatures_CodeAttempts] DEFAULT 0;
IF COL_LENGTH('dbo.DocumentSignatures', 'CodeSentCount') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [CodeSentCount] INT NOT NULL CONSTRAINT [DF_DocumentSignatures_CodeSentCount] DEFAULT 0;
IF COL_LENGTH('dbo.DocumentSignatures', 'CodeWindowStart') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [CodeWindowStart] DATETIME2(7) NULL;   -- begin van het uur waarin CodeSentCount telt
IF COL_LENGTH('dbo.DocumentSignatures', 'SignedName') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [SignedName] NVARCHAR(150) NULL;        -- naam zoals de ondertekenaar die typte
IF COL_LENGTH('dbo.DocumentSignatures', 'SignedIp') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [SignedIp] NVARCHAR(64) NULL;
IF COL_LENGTH('dbo.DocumentSignatures', 'SignedUserAgent') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [SignedUserAgent] NVARCHAR(400) NULL;
IF COL_LENGTH('dbo.DocumentSignatures', 'ConsentText') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [ConsentText] NVARCHAR(1000) NULL;      -- exacte akkoordtekst die getoond werd
IF COL_LENGTH('dbo.DocumentSignatures', 'DocumentHash') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [DocumentHash] NVARCHAR(64) NULL;       -- SHA-256 van het PDF dat ter ondertekening ging
IF COL_LENGTH('dbo.DocumentSignatures', 'EvidenceRef') IS NULL
    ALTER TABLE [dbo].[DocumentSignatures] ADD [EvidenceRef] NVARCHAR(40) NULL;        -- referentie op het ondertekende document
PRINT 'DocumentSignatures uitgebreid.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_DocumentSignatures_TokenHash' AND object_id = OBJECT_ID(N'[dbo].[DocumentSignatures]'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_DocumentSignatures_TokenHash] ON [dbo].[DocumentSignatures] ([TokenHash]) WHERE [TokenHash] IS NOT NULL;
PRINT 'Migratie 054_OndertekeningPerLink voltooid.';
GO
