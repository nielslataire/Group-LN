-- =============================================
-- Migratie: 049_DocumentenModel
-- Datum: 2026-09-25
-- Omschrijving: Documentenmodule (design-handoff 17a-17e) — "één document, veel plaatsen".
--   Het bestaande ProjectDocs blijft de bron (de publieke site WWWCOPRO leest daaruit:
--   ProjectId + ClientAccountId IS NULL + Type = 1 (Verkoop) + Filename) en wordt UITGEBREID; niets
--   wordt hernoemd of verwijderd. Nieuw:
--     DocumentFolders    vaste mappen per bedrijf (Plannen, Verkoop, Contracten, ...)
--     DocumentRevisions  bestandsversies van een document (A, B, C ...), ook door klant/leverancier
--                        aangeleverd via een portaal (UploadedByKind)
--     DocumentLinks      koppeling document -> eenheid / klant / leverancier (0-n), met delen-in-portaal
--     DocumentSignatures ondertekening per partij (itsme of manueel), voor contracten
--     DocumentRequests   verwachte / aangevraagde documenten ("Ontbreekt", "Aangevraagd") zonder bestand
--     DocumentTemplates  sjablonen die per eenheid/project de verwachte documenten aanmaken
--   ProjectDocs krijgt: FolderId, DocumentNumber, AuthoredBy, Status, ExpiresOn, ShareAllBuyers,
--   Perceel, AwardedDocumentId-achtige koppeling (RelatedDocumentId), CurrentRevisionId, audit.
--   Backfill: elk bestaand ProjectDocs-record krijgt een map (uit Type), een revisie "A" (goedgekeurd,
--   huidig) en — als ClientAccountId gevuld is — een klantkoppeling. Status = Goedgekeurd zodat er
--   niets uit het zicht verdwijnt.
--   Strikt additief + idempotent (opnieuw uitvoeren is veilig).
-- Status (ProjectDocs.Status / DocumentStatus): 0 Concept, 1 Ter goedkeuring, 2 Goedgekeurd,
--   3 Getekend, 4 Ter ondertekening, 5 Ingediend, 6 Gegund, 7 Niet gegund.
-- =============================================

------------------------------------------------------------
-- 1. DocumentFolders
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[DocumentFolders]'))
BEGIN
    CREATE TABLE [dbo].[DocumentFolders] (
        [Id]        INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [Code]      NVARCHAR(30)  NOT NULL,
        [Name]      NVARCHAR(100) NOT NULL,
        [Icon]      NVARCHAR(40)  NULL,
        [SortOrder] INT           NOT NULL DEFAULT 0,
        -- Kolomset/groepering van de map (17c-17e): default | keuringen | contracten | offertes
        [ViewKind]  NVARCHAR(20)  NOT NULL DEFAULT 'default',
        [IsActive]  BIT           NOT NULL DEFAULT 1
    );
    CREATE UNIQUE NONCLUSTERED INDEX [UX_DocumentFolders_Code] ON [dbo].[DocumentFolders] ([Code]);
    PRINT 'Tabel DocumentFolders aangemaakt.';
END
ELSE
    PRINT 'Tabel DocumentFolders bestaat al, overgeslagen.';
GO


-- Vaste mappen (idempotent op Code)
;WITH src AS (
    SELECT * FROM (VALUES
        (N'plannen',         N'Plannen',                 N'plan',       10, N'default'),
        (N'verkoop',         N'Verkoop',                 N'tag',        20, N'default'),
        (N'contracten',      N'Contracten',              N'signature',  30, N'contracten'),
        (N'offertes',        N'Offertes & bestellingen', N'coins',      40, N'offertes'),
        (N'keuringen',       N'Keuringen & attesten',    N'certificate',50, N'keuringen'),
        (N'verzekeringen',   N'Verzekeringen',           N'shield',     60, N'default'),
        (N'correspondentie', N'Correspondentie',         N'mail',       70, N'default'),
        (N'overige',         N'Overige',                 N'folder',     99, N'default')
    ) AS v([Code],[Name],[Icon],[SortOrder],[ViewKind])
)
INSERT INTO [dbo].[DocumentFolders] ([Code],[Name],[Icon],[SortOrder],[ViewKind])
SELECT s.[Code], s.[Name], s.[Icon], s.[SortOrder], s.[ViewKind]
FROM src s
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[DocumentFolders] f WHERE f.[Code] = s.[Code]);
PRINT 'DocumentFolders geseed.';
GO


------------------------------------------------------------
-- 2. ProjectDocs uitbreiden (allemaal nullable of met default -> bestaande code blijft werken)
------------------------------------------------------------
IF COL_LENGTH('dbo.ProjectDocs', 'FolderId') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [FolderId] INT NULL;
IF COL_LENGTH('dbo.ProjectDocs', 'DocumentNumber') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [DocumentNumber] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.ProjectDocs', 'AuthoredBy') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [AuthoredBy] NVARCHAR(150) NULL;
IF COL_LENGTH('dbo.ProjectDocs', 'Status') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [Status] TINYINT NOT NULL CONSTRAINT [DF_ProjectDocs_Status] DEFAULT 2;
IF COL_LENGTH('dbo.ProjectDocs', 'ExpiresOn') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [ExpiresOn] DATE NULL;
IF COL_LENGTH('dbo.ProjectDocs', 'ShareAllBuyers') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [ShareAllBuyers] BIT NOT NULL CONSTRAINT [DF_ProjectDocs_ShareAllBuyers] DEFAULT 0;
IF COL_LENGTH('dbo.ProjectDocs', 'Perceel') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [Perceel] NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.ProjectDocs', 'RelatedDocumentId') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [RelatedDocumentId] INT NULL;
IF COL_LENGTH('dbo.ProjectDocs', 'CurrentRevisionId') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [CurrentRevisionId] INT NULL;
IF COL_LENGTH('dbo.ProjectDocs', 'CreatedDate') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [CreatedDate] DATETIME2(7) NULL;
IF COL_LENGTH('dbo.ProjectDocs', 'CreatedByUserId') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [CreatedByUserId] NVARCHAR(128) NULL;
IF COL_LENGTH('dbo.ProjectDocs', 'ModifiedDate') IS NULL
    ALTER TABLE [dbo].[ProjectDocs] ADD [ModifiedDate] DATETIME2(7) NULL;
PRINT 'ProjectDocs uitgebreid.';
GO


IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProjectDocs_DocumentFolders')
    ALTER TABLE [dbo].[ProjectDocs] ADD CONSTRAINT [FK_ProjectDocs_DocumentFolders]
        FOREIGN KEY ([FolderId]) REFERENCES [dbo].[DocumentFolders]([Id]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProjectDocs_RelatedDocument')
    ALTER TABLE [dbo].[ProjectDocs] ADD CONSTRAINT [FK_ProjectDocs_RelatedDocument]
        FOREIGN KEY ([RelatedDocumentId]) REFERENCES [dbo].[ProjectDocs]([Id]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProjectDocs_ProjectId_FolderId' AND object_id = OBJECT_ID(N'[dbo].[ProjectDocs]'))
    CREATE NONCLUSTERED INDEX [IX_ProjectDocs_ProjectId_FolderId] ON [dbo].[ProjectDocs] ([ProjectId], [FolderId]);

------------------------------------------------------------
-- 3. DocumentRevisions
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[DocumentRevisions]'))
BEGIN
    CREATE TABLE [dbo].[DocumentRevisions] (
        [Id]                 INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [DocumentId]         INT            NOT NULL,
        [RevisionNo]         INT            NOT NULL,             -- 1, 2, 3 ... (letter A, B, C ... afgeleid)
        [Filename]           NVARCHAR(200)  NOT NULL,             -- naam in de "docs"-storage
        [OriginalFilename]   NVARCHAR(260)  NULL,
        [SizeBytes]          BIGINT         NULL,
        [Status]             TINYINT        NOT NULL DEFAULT 2,   -- 0 Concept, 1 Ter goedkeuring, 2 Goedgekeurd, 3 Vervangen/afgewezen
        [Note]               NVARCHAR(500)  NULL,                 -- reden / omschrijving van de revisie
        [Amount]             DECIMAL(18,2)  NULL,                 -- offertes: bedrag excl. btw
        [UploadedByKind]     TINYINT        NOT NULL DEFAULT 0,   -- 0 intern, 1 klant (portaal), 2 leverancier (portaal), 3 extern/notaris
        [UploadedByUserId]   NVARCHAR(128)  NULL,
        [UploadedByName]     NVARCHAR(150)  NULL,
        [UploadedDate]       DATETIME2(7)   NOT NULL DEFAULT SYSUTCDATETIME(),
        [ApprovedByUserId]   NVARCHAR(128)  NULL,
        [ApprovedByName]     NVARCHAR(150)  NULL,
        [ApprovedDate]       DATETIME2(7)   NULL,
        CONSTRAINT [FK_DocumentRevisions_ProjectDocs]
            FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[ProjectDocs]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_DocumentRevisions_DocumentId] ON [dbo].[DocumentRevisions] ([DocumentId], [RevisionNo]);
    PRINT 'Tabel DocumentRevisions aangemaakt.';
END
ELSE
    PRINT 'Tabel DocumentRevisions bestaat al, overgeslagen.';
GO


IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProjectDocs_CurrentRevision')
    ALTER TABLE [dbo].[ProjectDocs] ADD CONSTRAINT [FK_ProjectDocs_CurrentRevision]
        FOREIGN KEY ([CurrentRevisionId]) REFERENCES [dbo].[DocumentRevisions]([Id]);
GO


------------------------------------------------------------
-- 4. DocumentLinks
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[DocumentLinks]'))
BEGIN
    CREATE TABLE [dbo].[DocumentLinks] (
        [Id]              INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [DocumentId]      INT           NOT NULL,
        -- Precies één van de drie is gevuld
        [UnitId]          INT           NULL,
        [ClientAccountId] INT           NULL,
        [CompanyId]       INT           NULL,                       -- leverancier (CompanyInfo)
        [SharedInPortal]  BIT           NOT NULL DEFAULT 0,         -- zichtbaar in het portaal van deze koppeling
        [SeenDate]        DATETIME2(7)  NULL,                       -- laatst gezien in het portaal
        [SeenByName]      NVARCHAR(150) NULL,
        [CreatedDate]     DATETIME2(7)  NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_DocumentLinks_ProjectDocs] FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[ProjectDocs]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentLinks_Units]       FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id]),
        CONSTRAINT [FK_DocumentLinks_ClientAccount] FOREIGN KEY ([ClientAccountId]) REFERENCES [dbo].[ClientAccount]([Id]),
        CONSTRAINT [FK_DocumentLinks_CompanyInfo] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[CompanyInfo]([CompanyId]),
        CONSTRAINT [CK_DocumentLinks_OneTarget] CHECK (
            (CASE WHEN [UnitId] IS NULL THEN 0 ELSE 1 END
           + CASE WHEN [ClientAccountId] IS NULL THEN 0 ELSE 1 END
           + CASE WHEN [CompanyId] IS NULL THEN 0 ELSE 1 END) = 1)
    );
    CREATE NONCLUSTERED INDEX [IX_DocumentLinks_DocumentId] ON [dbo].[DocumentLinks] ([DocumentId]);
    CREATE NONCLUSTERED INDEX [IX_DocumentLinks_UnitId] ON [dbo].[DocumentLinks] ([UnitId]) WHERE [UnitId] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_DocumentLinks_ClientAccountId] ON [dbo].[DocumentLinks] ([ClientAccountId]) WHERE [ClientAccountId] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_DocumentLinks_CompanyId] ON [dbo].[DocumentLinks] ([CompanyId]) WHERE [CompanyId] IS NOT NULL;
    PRINT 'Tabel DocumentLinks aangemaakt.';
END
ELSE
    PRINT 'Tabel DocumentLinks bestaat al, overgeslagen.';
GO


------------------------------------------------------------
-- 5. DocumentSignatures
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[DocumentSignatures]'))
BEGIN
    CREATE TABLE [dbo].[DocumentSignatures] (
        [Id]              INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [DocumentId]      INT           NOT NULL,
        [RevisionId]      INT           NULL,
        [SignOrder]       INT           NOT NULL DEFAULT 0,
        [Name]            NVARCHAR(150) NOT NULL,
        [Role]            NVARCHAR(50)  NULL,                       -- koper, verkoper, notaris ...
        [ClientAccountId] INT           NULL,
        [UserId]          NVARCHAR(128) NULL,
        [Status]          TINYINT       NOT NULL DEFAULT 0,         -- 0 wacht, 1 geopend, 2 getekend
        [Method]          NVARCHAR(20)  NULL,                       -- itsme | manueel
        [SentDate]        DATETIME2(7)  NULL,
        [OpenedDate]      DATETIME2(7)  NULL,
        [SignedDate]      DATETIME2(7)  NULL,
        [ReminderDate]    DATETIME2(7)  NULL,
        CONSTRAINT [FK_DocumentSignatures_ProjectDocs] FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[ProjectDocs]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentSignatures_Revision]    FOREIGN KEY ([RevisionId]) REFERENCES [dbo].[DocumentRevisions]([Id]),
        CONSTRAINT [FK_DocumentSignatures_ClientAccount] FOREIGN KEY ([ClientAccountId]) REFERENCES [dbo].[ClientAccount]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_DocumentSignatures_DocumentId] ON [dbo].[DocumentSignatures] ([DocumentId], [SignOrder]);
    PRINT 'Tabel DocumentSignatures aangemaakt.';
END
ELSE
    PRINT 'Tabel DocumentSignatures bestaat al, overgeslagen.';
GO


------------------------------------------------------------
-- 6. DocumentTemplates (verwachte documenten per projecttype / eenheid)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[DocumentTemplates]'))
BEGIN
    CREATE TABLE [dbo].[DocumentTemplates] (
        [Id]              INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [Code]            NVARCHAR(40)  NOT NULL,
        [Name]            NVARCHAR(200) NOT NULL,
        [FolderId]        INT           NOT NULL,
        [ProjectType]     INT           NULL,                       -- BOCore.ProjectType; NULL = alle projecttypes
        [PerUnit]         BIT           NOT NULL DEFAULT 1,         -- 1 = één per (hoofd)eenheid, 0 = één per project
        [ResponsibleRole] NVARCHAR(100) NULL,                       -- "Elektricien", "EPB-verslaggever" ...
        [DueLabel]        NVARCHAR(100) NULL,                       -- "verwacht bij oplevering"
        [ExpiryYears]     INT           NULL,
        [ReminderDays]    INT           NULL,
        [LegacyDocType]   INT           NULL,                       -- ProjectDocType voor de oude Type-kolom
        [SortOrder]       INT           NOT NULL DEFAULT 0,
        [IsActive]        BIT           NOT NULL DEFAULT 1,
        CONSTRAINT [FK_DocumentTemplates_Folder] FOREIGN KEY ([FolderId]) REFERENCES [dbo].[DocumentFolders]([Id])
    );
    CREATE UNIQUE NONCLUSTERED INDEX [UX_DocumentTemplates_Code] ON [dbo].[DocumentTemplates] ([Code]);
    PRINT 'Tabel DocumentTemplates aangemaakt.';
END
ELSE
    PRINT 'Tabel DocumentTemplates bestaat al, overgeslagen.';
GO


-- Startsjabloon "nieuwbouw woonproject" (ProjectType 1). Vrij aanpasbaar door de gebruiker.
;WITH src AS (
    SELECT * FROM (VALUES
        (N'epc',      N'EPC-certificaat',                     N'keuringen', 1, 1, N'EPB-verslaggever', N'verwacht bij oplevering',           10, NULL, 5,  10),
        (N'elek',     N'Keuringsverslag elektriciteit',       N'keuringen', 1, 1, N'Elektricien',      N'verwacht bij oplevering',           25, 60,   6,  20),
        (N'pid',      N'Post-interventiedossier',             N'keuringen', 1, 1, N'Veiligheidscoördinator', N'verwacht bij voorlopige oplevering', NULL, NULL, 14, 30),
        (N'gas',      N'Gaskeuring',                          N'keuringen', 1, 1, N'Installateur',     N'verwacht bij oplevering',           NULL, NULL, 8,  40),
        (N'epbstart', N'EPB-startverklaring',                 N'keuringen', 1, 0, N'EPB-verslaggever', N'verwacht bij start',                NULL, NULL, 10, 50)
    ) AS v([Code],[Name],[Folder],[ProjectType],[PerUnit],[Role],[Due],[ExpiryYears],[ReminderDays],[LegacyDocType],[SortOrder])
)
INSERT INTO [dbo].[DocumentTemplates] ([Code],[Name],[FolderId],[ProjectType],[PerUnit],[ResponsibleRole],[DueLabel],[ExpiryYears],[ReminderDays],[LegacyDocType],[SortOrder])
SELECT s.[Code], s.[Name], f.[Id], s.[ProjectType], s.[PerUnit], s.[Role], s.[Due], s.[ExpiryYears], s.[ReminderDays], s.[LegacyDocType], s.[SortOrder]
FROM src s
JOIN [dbo].[DocumentFolders] f ON f.[Code] = s.[Folder]
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[DocumentTemplates] t WHERE t.[Code] = s.[Code]);
PRINT 'DocumentTemplates geseed.';
GO


------------------------------------------------------------
-- 7. DocumentRequests (verwacht / aangevraagd, nog zonder bestand)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[DocumentRequests]'))
BEGIN
    CREATE TABLE [dbo].[DocumentRequests] (
        [Id]                   INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjectId]            INT            NOT NULL,
        [FolderId]             INT            NOT NULL,
        [TemplateId]           INT            NULL,
        [Name]                 NVARCHAR(200)  NOT NULL,
        [UnitId]               INT            NULL,                 -- NULL = geldt voor het hele project
        [Perceel]              NVARCHAR(100)  NULL,
        -- Wie moet het aanleveren
        [ResponsibleKind]      TINYINT        NOT NULL DEFAULT 0,   -- 0 intern, 1 leverancier, 2 klant, 3 extern
        [ResponsibleCompanyId] INT            NULL,
        [ResponsibleClientAccountId] INT      NULL,
        [ResponsibleName]      NVARCHAR(150)  NULL,                 -- vrije tekst (of weergavenaam van de contactpersoon)
        [ResponsibleRole]      NVARCHAR(100)  NULL,
        [DueDate]              DATE           NULL,
        [DueLabel]             NVARCHAR(100)  NULL,
        [ReminderDaysBefore]   INT            NULL,
        [ExpiryYears]          INT            NULL,
        [LegacyDocType]        INT            NULL,
        [Status]               TINYINT        NOT NULL DEFAULT 0,   -- 0 ontbreekt, 1 aangevraagd, 2 ontvangen, 3 niet vereist
        [RequestedDate]        DATETIME2(7)   NULL,
        [RequestedByUserId]    NVARCHAR(128)  NULL,
        [RequestedByName]      NVARCHAR(150)  NULL,
        [SeenInPortalDate]     DATETIME2(7)   NULL,
        [SeenByName]           NVARCHAR(150)  NULL,
        [LastReminderDate]     DATETIME2(7)   NULL,
        [ShareWithBuyerAfterApproval] BIT     NOT NULL DEFAULT 1,
        [FulfilledDocumentId]  INT            NULL,
        [Note]                 NVARCHAR(500)  NULL,
        [CreatedDate]          DATETIME2(7)   NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_DocumentRequests_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentRequests_Folder]  FOREIGN KEY ([FolderId]) REFERENCES [dbo].[DocumentFolders]([Id]),
        CONSTRAINT [FK_DocumentRequests_Template] FOREIGN KEY ([TemplateId]) REFERENCES [dbo].[DocumentTemplates]([Id]),
        CONSTRAINT [FK_DocumentRequests_Units]   FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id]),
        CONSTRAINT [FK_DocumentRequests_Company] FOREIGN KEY ([ResponsibleCompanyId]) REFERENCES [dbo].[CompanyInfo]([CompanyId]),
        CONSTRAINT [FK_DocumentRequests_ClientAccount] FOREIGN KEY ([ResponsibleClientAccountId]) REFERENCES [dbo].[ClientAccount]([Id]),
        CONSTRAINT [FK_DocumentRequests_FulfilledDoc] FOREIGN KEY ([FulfilledDocumentId]) REFERENCES [dbo].[ProjectDocs]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_DocumentRequests_ProjectId] ON [dbo].[DocumentRequests] ([ProjectId], [Status]);
    CREATE NONCLUSTERED INDEX [IX_DocumentRequests_CompanyId] ON [dbo].[DocumentRequests] ([ResponsibleCompanyId]) WHERE [ResponsibleCompanyId] IS NOT NULL;
    -- Eén verwacht document per sjabloon per eenheid (idempotent genereren)
    CREATE UNIQUE NONCLUSTERED INDEX [UX_DocumentRequests_Template_Unit]
        ON [dbo].[DocumentRequests] ([ProjectId], [TemplateId], [UnitId]) WHERE [TemplateId] IS NOT NULL AND [UnitId] IS NOT NULL;
    PRINT 'Tabel DocumentRequests aangemaakt.';
END
ELSE
    PRINT 'Tabel DocumentRequests bestaat al, overgeslagen.';
GO


------------------------------------------------------------
-- 8. Backfill van bestaande ProjectDocs
------------------------------------------------------------
-- 8a. Map uit het oude Type (nog niet gevulde FolderId)
UPDATE d SET d.[FolderId] = f.[Id]
FROM [dbo].[ProjectDocs] d
JOIN [dbo].[DocumentFolders] f ON f.[Code] =
    CASE
        WHEN d.[Type] = 1 THEN 'verkoop'
        WHEN d.[Type] = 16 THEN 'plannen'
        WHEN d.[Type] IN (2,3,4,5,6,7,8,9,10,11,12,13,14,15,17) THEN 'keuringen'
        ELSE 'overige'
    END
WHERE d.[FolderId] IS NULL;
PRINT 'Backfill ProjectDocs.FolderId klaar.';
GO


-- 8b. Eén revisie "A" per bestaand document (goedgekeurd, huidig)
INSERT INTO [dbo].[DocumentRevisions] ([DocumentId],[RevisionNo],[Filename],[Status],[UploadedByKind],[UploadedDate],[ApprovedDate])
SELECT d.[Id], 1, d.[Filename], 2, 0,
       COALESCE(CONVERT(DATETIME2(7), d.[Date]), SYSUTCDATETIME()),
       COALESCE(CONVERT(DATETIME2(7), d.[Date]), SYSUTCDATETIME())
FROM [dbo].[ProjectDocs] d
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[DocumentRevisions] r WHERE r.[DocumentId] = d.[Id]);

UPDATE d SET d.[CurrentRevisionId] = r.[Id]
FROM [dbo].[ProjectDocs] d
JOIN [dbo].[DocumentRevisions] r ON r.[DocumentId] = d.[Id] AND r.[RevisionNo] = 1
WHERE d.[CurrentRevisionId] IS NULL;
PRINT 'Backfill revisies klaar.';
GO


-- 8c. Klantdocumenten -> klantkoppeling (de oude ClientAccountId-kolom blijft ongewijzigd)
INSERT INTO [dbo].[DocumentLinks] ([DocumentId],[ClientAccountId],[SharedInPortal])
SELECT d.[Id], d.[ClientAccountId], 0
FROM [dbo].[ProjectDocs] d
WHERE d.[ClientAccountId] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[DocumentLinks] l WHERE l.[DocumentId] = d.[Id] AND l.[ClientAccountId] = d.[ClientAccountId]);
PRINT 'Backfill klantkoppelingen klaar.';
GO


PRINT 'Migratie 049_DocumentenModel voltooid.';
