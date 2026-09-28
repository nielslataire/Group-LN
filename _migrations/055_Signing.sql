-- =============================================
-- Migratie: 055_Signing
-- Datum: 2026-09-27
-- Omschrijving: Elektronisch ondertekenen (ONDERTEKENEN_VOORSTEL.md §3). Acht tabellen:
--     SigningPolicy          beleid per documenttype (seed: ChangeOrder)
--     SigningCase            ondertekeningsdossier (aggregate root)
--     SigningDocument        onveranderlijke bestanden (bytes in SQL + spiegel in Storage API)
--     SigningParty           ondertekenaars (snapshots van naam/e-mail)
--     SigningAccessToken     persoonlijke links (alleen SHA-256 van het token)
--     SigningVerification    OTP-cycli (alleen HMAC van de code)
--     SigningEvent           audit trail — APPEND-ONLY via trigger + hash-ketting
--     ClientContactChangeLog wijzigingslog e-mail/gsm van klantcontacten (gemaskeerd)
--   Alle tijdstippen UTC (sysutcdatetime()). Idempotent + additief, zelfde conventie als de rest
--   van deze map. EF-config: DALCore/Models/cpmRunningContext.Signing.cs.
-- =============================================

-- ---- 1. SigningPolicy ------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SigningPolicy' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[SigningPolicy] (
        [Id]                   INT            IDENTITY(1,1) NOT NULL,
        [DocumentType]         NVARCHAR(50)   NOT NULL,
        [DisplayName]          NVARCHAR(100)  NOT NULL,
        [SignatureMethod]      NVARCHAR(50)   NOT NULL CONSTRAINT [DF_SigningPolicy_SignatureMethod] DEFAULT ('internal-ses'),
        [SigningRule]          INT            NOT NULL CONSTRAINT [DF_SigningPolicy_SigningRule] DEFAULT (0),
        [OtpRequired]          BIT            NOT NULL CONSTRAINT [DF_SigningPolicy_OtpRequired] DEFAULT (1),
        [VerificationMethod]   NVARCHAR(50)   NULL,
        [OtpValiditySeconds]   INT            NOT NULL CONSTRAINT [DF_SigningPolicy_OtpValiditySeconds] DEFAULT (600),
        [OtpMaxAttempts]       INT            NOT NULL CONSTRAINT [DF_SigningPolicy_OtpMaxAttempts] DEFAULT (5),
        [LinkValidityDays]     INT            NOT NULL CONSTRAINT [DF_SigningPolicy_LinkValidityDays] DEFAULT (30),
        [ReminderAfterDays]    INT            NULL,
        [ReminderRepeatDays]   INT            NULL,
        [MaxReminders]         INT            NOT NULL CONSTRAINT [DF_SigningPolicy_MaxReminders] DEFAULT (3),
        [ConsentText]          NVARCHAR(MAX)  NOT NULL,
        [RetentionDays]        INT            NULL,
        [IsActive]             BIT            NOT NULL CONSTRAINT [DF_SigningPolicy_IsActive] DEFAULT (1),
        [CreatedAt]            DATETIME2(7)   NOT NULL CONSTRAINT [DF_SigningPolicy_CreatedAt] DEFAULT (sysutcdatetime()),
        [UpdatedAt]            DATETIME2(7)   NOT NULL CONSTRAINT [DF_SigningPolicy_UpdatedAt] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_SigningPolicy] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE INDEX [UX_SigningPolicy_DocumentType] ON [dbo].[SigningPolicy]([DocumentType]);
    PRINT 'Tabel SigningPolicy aangemaakt.';
END
ELSE
    PRINT 'Tabel SigningPolicy bestaat al, overgeslagen.';
GO

-- ---- 2. SigningDocument (vóór SigningCase: die verwijst er naar) -------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SigningDocument' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[SigningDocument] (
        [Id]               INT            IDENTITY(1,1) NOT NULL,
        [SigningCaseId]    INT            NOT NULL,
        [SigningPartyId]   INT            NULL,
        [Kind]             INT            NOT NULL,
        [FileName]         NVARCHAR(260)  NOT NULL,
        [ContentType]      NVARCHAR(100)  NOT NULL,
        [ByteLength]       BIGINT         NOT NULL,
        [Sha256]           CHAR(64)       NOT NULL,
        [StorageFileName]  NVARCHAR(260)  NULL,
        [Content]          VARBINARY(MAX) NULL,
        [CreatedAt]        DATETIME2(7)   NOT NULL CONSTRAINT [DF_SigningDocument_CreatedAt] DEFAULT (sysutcdatetime()),
        [CreatedByUserId]  INT            NULL,
        CONSTRAINT [PK_SigningDocument] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE INDEX [IX_SigningDocument_Case_Kind] ON [dbo].[SigningDocument]([SigningCaseId], [Kind]);
    CREATE INDEX [IX_SigningDocument_Sha256]    ON [dbo].[SigningDocument]([Sha256]);
    PRINT 'Tabel SigningDocument aangemaakt.';
END
ELSE
    PRINT 'Tabel SigningDocument bestaat al, overgeslagen.';
GO

-- ---- 3. SigningCase ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SigningCase' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[SigningCase] (
        [Id]                     INT              IDENTITY(1,1) NOT NULL,
        [PublicVerificationId]   UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SigningCase_PublicVerificationId] DEFAULT (newid()),
        [DocumentType]           NVARCHAR(50)     NOT NULL,
        [SourceEntityId]         INT              NOT NULL,
        [ProjectId]              INT              NULL,
        [ClientAccountId]        INT              NULL,
        [Title]                  NVARCHAR(300)    NOT NULL,
        [DocumentNumber]         NVARCHAR(50)     NULL,
        [Summary]                NVARCHAR(MAX)    NULL,
        [AmountExclVat]          DECIMAL(18,2)    NULL,
        [VatAmount]              DECIMAL(18,2)    NULL,
        [AmountInclVat]          DECIMAL(18,2)    NULL,
        [Status]                 INT              NOT NULL CONSTRAINT [DF_SigningCase_Status] DEFAULT (0),
        [SigningRule]            INT              NOT NULL CONSTRAINT [DF_SigningCase_SigningRule] DEFAULT (0),
        [SignatureMethod]        NVARCHAR(50)     NOT NULL,
        [ProviderCaseRef]        NVARCHAR(200)    NULL,
        [OtpRequired]            BIT              NOT NULL CONSTRAINT [DF_SigningCase_OtpRequired] DEFAULT (1),
        [VerificationMethod]     NVARCHAR(50)     NULL,
        [OtpValiditySeconds]     INT              NOT NULL CONSTRAINT [DF_SigningCase_OtpValiditySeconds] DEFAULT (600),
        [OtpMaxAttempts]         INT              NOT NULL CONSTRAINT [DF_SigningCase_OtpMaxAttempts] DEFAULT (5),
        [ConsentTextSnapshot]    NVARCHAR(MAX)    NULL,
        [OriginalDocumentId]     INT              NULL,
        [FinalDocumentId]        INT              NULL,
        [AuditReportDocumentId]  INT              NULL,
        [CreatedByUserId]        INT              NOT NULL,
        [CreatedAt]              DATETIME2(7)     NOT NULL CONSTRAINT [DF_SigningCase_CreatedAt] DEFAULT (sysutcdatetime()),
        [OpenedAt]               DATETIME2(7)     NULL,
        [ExpiresAt]              DATETIME2(7)     NULL,
        [CompletedAt]            DATETIME2(7)     NULL,
        [ClosedAt]               DATETIME2(7)     NULL,
        [ClosedByUserId]         INT              NULL,
        [CloseReason]            NVARCHAR(1000)   NULL,
        [SupersededByCaseId]     INT              NULL,
        [RetentionUntil]         DATETIME2(7)     NULL,
        [RetentionScrubbedAt]    DATETIME2(7)     NULL,
        [RowVersion]             ROWVERSION       NOT NULL,
        CONSTRAINT [PK_SigningCase] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_SigningCase_Project]             FOREIGN KEY ([ProjectId])             REFERENCES [dbo].[Project]([ProjectId]),
        CONSTRAINT [FK_SigningCase_ClientAccount]       FOREIGN KEY ([ClientAccountId])       REFERENCES [dbo].[ClientAccount]([Id]),
        CONSTRAINT [FK_SigningCase_OriginalDocument]    FOREIGN KEY ([OriginalDocumentId])    REFERENCES [dbo].[SigningDocument]([Id]),
        CONSTRAINT [FK_SigningCase_FinalDocument]       FOREIGN KEY ([FinalDocumentId])       REFERENCES [dbo].[SigningDocument]([Id]),
        CONSTRAINT [FK_SigningCase_AuditReportDocument] FOREIGN KEY ([AuditReportDocumentId]) REFERENCES [dbo].[SigningDocument]([Id])
    );
    CREATE UNIQUE INDEX [UX_SigningCase_PublicVerificationId] ON [dbo].[SigningCase]([PublicVerificationId]);
    CREATE INDEX [IX_SigningCase_Source]          ON [dbo].[SigningCase]([DocumentType], [SourceEntityId], [Status]);
    CREATE INDEX [IX_SigningCase_ProjectId]       ON [dbo].[SigningCase]([ProjectId]);
    CREATE INDEX [IX_SigningCase_ClientAccountId] ON [dbo].[SigningCase]([ClientAccountId]);
    CREATE INDEX [IX_SigningCase_Status]          ON [dbo].[SigningCase]([Status]);
    CREATE INDEX [IX_SigningCase_ExpiresAt]       ON [dbo].[SigningCase]([ExpiresAt]);
    -- Hoogstens één lopend dossier (Draft=0/Open=1) per brondocument.
    CREATE UNIQUE INDEX [UX_SigningCase_ActivePerSource] ON [dbo].[SigningCase]([DocumentType], [SourceEntityId]) WHERE [Status] IN (0, 1);
    PRINT 'Tabel SigningCase aangemaakt.';
END
ELSE
    PRINT 'Tabel SigningCase bestaat al, overgeslagen.';
GO

-- SigningDocument → SigningCase (cascade; pas mogelijk nu SigningCase bestaat)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SigningDocument_SigningCase')
BEGIN
    ALTER TABLE [dbo].[SigningDocument] WITH CHECK
        ADD CONSTRAINT [FK_SigningDocument_SigningCase] FOREIGN KEY ([SigningCaseId]) REFERENCES [dbo].[SigningCase]([Id]) ON DELETE CASCADE;
    PRINT 'FK_SigningDocument_SigningCase toegevoegd.';
END
GO

-- ---- 4. SigningParty -----------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SigningParty' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[SigningParty] (
        [Id]                         INT              IDENTITY(1,1) NOT NULL,
        [SigningCaseId]              INT              NOT NULL,
        [SortOrder]                  INT              NOT NULL CONSTRAINT [DF_SigningParty_SortOrder] DEFAULT (0),
        [PartyType]                  INT              NOT NULL,
        [SourceRefId]                INT              NULL,
        [DisplayName]                NVARCHAR(200)    NOT NULL,
        [Email]                      NVARCHAR(256)    NULL,
        [PhoneMasked]                NVARCHAR(40)     NULL,
        [Capacity]                   NVARCHAR(200)    NULL,
        [Status]                     INT              NOT NULL CONSTRAINT [DF_SigningParty_Status] DEFAULT (0),
        [PartyVerificationId]        UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SigningParty_PartyVerificationId] DEFAULT (newid()),
        [InvitedAt]                  DATETIME2(7)     NULL,
        [LastReminderAt]             DATETIME2(7)     NULL,
        [ReminderCount]              INT              NOT NULL CONSTRAINT [DF_SigningParty_ReminderCount] DEFAULT (0),
        [FirstOpenedAt]              DATETIME2(7)     NULL,
        [VerifiedAt]                 DATETIME2(7)     NULL,
        [ConsentAcceptedAt]          DATETIME2(7)     NULL,
        [ConsentTextSnapshot]        NVARCHAR(MAX)    NULL,
        [SignedAt]                   DATETIME2(7)     NULL,
        [SignedIp]                   NVARCHAR(45)     NULL,
        [SignedUserAgent]            NVARCHAR(500)    NULL,
        [SignIdempotencyKey]         NVARCHAR(100)    NULL,
        [SignatureImageDocumentId]   INT              NULL,
        [DeclinedAt]                 DATETIME2(7)     NULL,
        [DeclineReason]              NVARCHAR(1000)   NULL,
        [ProviderPartyRef]           NVARCHAR(200)    NULL,
        [RowVersion]                 ROWVERSION       NOT NULL,
        CONSTRAINT [PK_SigningParty] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_SigningParty_SigningCase]            FOREIGN KEY ([SigningCaseId])            REFERENCES [dbo].[SigningCase]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SigningParty_SignatureImageDocument] FOREIGN KEY ([SignatureImageDocumentId]) REFERENCES [dbo].[SigningDocument]([Id])
    );
    CREATE INDEX [IX_SigningParty_Case_Sort] ON [dbo].[SigningParty]([SigningCaseId], [SortOrder]);
    CREATE UNIQUE INDEX [UX_SigningParty_PartyVerificationId] ON [dbo].[SigningParty]([PartyVerificationId]);
    CREATE INDEX [IX_SigningParty_Status] ON [dbo].[SigningParty]([Status]);
    PRINT 'Tabel SigningParty aangemaakt.';
END
ELSE
    PRINT 'Tabel SigningParty bestaat al, overgeslagen.';
GO

-- ---- 5. SigningAccessToken -----------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SigningAccessToken' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[SigningAccessToken] (
        [Id]               INT              IDENTITY(1,1) NOT NULL,
        [SigningPartyId]   INT              NOT NULL,
        [TokenHash]        CHAR(64)         NOT NULL,
        [Purpose]          INT              NOT NULL CONSTRAINT [DF_SigningAccessToken_Purpose] DEFAULT (0),
        [CreatedAt]        DATETIME2(7)     NOT NULL CONSTRAINT [DF_SigningAccessToken_CreatedAt] DEFAULT (sysutcdatetime()),
        [CreatedByUserId]  INT              NULL,
        [ExpiresAt]        DATETIME2(7)     NOT NULL,
        [FirstUsedAt]      DATETIME2(7)     NULL,
        [LastUsedAt]       DATETIME2(7)     NULL,
        [UseCount]         INT              NOT NULL CONSTRAINT [DF_SigningAccessToken_UseCount] DEFAULT (0),
        [RevokedAt]        DATETIME2(7)     NULL,
        [RevokedReason]    NVARCHAR(500)    NULL,
        [SessionId]        UNIQUEIDENTIFIER NULL,
        [SessionIssuedAt]  DATETIME2(7)     NULL,
        CONSTRAINT [PK_SigningAccessToken] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_SigningAccessToken_SigningParty] FOREIGN KEY ([SigningPartyId]) REFERENCES [dbo].[SigningParty]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [UX_SigningAccessToken_TokenHash] ON [dbo].[SigningAccessToken]([TokenHash]);
    CREATE INDEX [IX_SigningAccessToken_Party]     ON [dbo].[SigningAccessToken]([SigningPartyId]);
    CREATE INDEX [IX_SigningAccessToken_SessionId] ON [dbo].[SigningAccessToken]([SessionId]);
    PRINT 'Tabel SigningAccessToken aangemaakt.';
END
ELSE
    PRINT 'Tabel SigningAccessToken bestaat al, overgeslagen.';
GO

-- ---- 6. SigningVerification ----------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SigningVerification' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[SigningVerification] (
        [Id]                  INT              IDENTITY(1,1) NOT NULL,
        [SigningPartyId]      INT              NOT NULL,
        [Method]              NVARCHAR(50)     NOT NULL,
        [ChannelKey]          NVARCHAR(50)     NOT NULL,
        [ProviderKey]         NVARCHAR(50)     NULL,
        [DestinationMasked]   NVARCHAR(100)    NULL,
        [CodeHmac]            CHAR(64)         NOT NULL,
        [SessionId]           UNIQUEIDENTIFIER NULL,
        [RequestedAt]         DATETIME2(7)     NOT NULL CONSTRAINT [DF_SigningVerification_RequestedAt] DEFAULT (sysutcdatetime()),
        [RequestedIp]         NVARCHAR(45)     NULL,
        [ExpiresAt]           DATETIME2(7)     NOT NULL,
        [AttemptCount]        INT              NOT NULL CONSTRAINT [DF_SigningVerification_AttemptCount] DEFAULT (0),
        [MaxAttempts]         INT              NOT NULL CONSTRAINT [DF_SigningVerification_MaxAttempts] DEFAULT (5),
        [Status]              INT              NOT NULL CONSTRAINT [DF_SigningVerification_Status] DEFAULT (0),
        [ProviderMessageId]   NVARCHAR(200)    NULL,
        [ProviderStatus]      NVARCHAR(100)    NULL,
        [ProviderAcceptedAt]  DATETIME2(7)     NULL,
        [DeliveredAt]         DATETIME2(7)     NULL,
        [VerifiedAt]          DATETIME2(7)     NULL,
        [VerifiedIp]          NVARCHAR(45)     NULL,
        [ConsumedAt]          DATETIME2(7)     NULL,
        CONSTRAINT [PK_SigningVerification] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_SigningVerification_SigningParty] FOREIGN KEY ([SigningPartyId]) REFERENCES [dbo].[SigningParty]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_SigningVerification_Party_Status]   ON [dbo].[SigningVerification]([SigningPartyId], [Status]);
    CREATE INDEX [IX_SigningVerification_ProviderMsgId] ON [dbo].[SigningVerification]([ProviderMessageId]);
    PRINT 'Tabel SigningVerification aangemaakt.';
END
ELSE
    PRINT 'Tabel SigningVerification bestaat al, overgeslagen.';
GO

-- ---- 7. SigningEvent (append-only) ---------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SigningEvent' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[SigningEvent] (
        [Id]               BIGINT         IDENTITY(1,1) NOT NULL,
        [SigningCaseId]    INT            NOT NULL,
        [SigningPartyId]   INT            NULL,
        [EventType]        NVARCHAR(60)   NOT NULL,
        [OccurredAtUtc]    DATETIME2(7)   NOT NULL,
        [ActorType]        INT            NOT NULL,
        [ActorUserId]      INT            NULL,
        [ActorLabel]       NVARCHAR(200)  NULL,
        [Ip]               NVARCHAR(45)   NULL,
        [IpHash]           CHAR(64)       NULL,
        [UserAgent]        NVARCHAR(500)  NULL,
        [UserAgentHash]    CHAR(64)       NULL,
        [DocumentSha256]   CHAR(64)       NULL,
        [DataJson]         NVARCHAR(MAX)  NULL,
        [PrevEventHash]    CHAR(64)       NULL,
        [EventHash]        CHAR(64)       NOT NULL,
        CONSTRAINT [PK_SigningEvent] PRIMARY KEY CLUSTERED ([Id] ASC),
        -- Geen ON DELETE CASCADE: events overleven bewust; de trigger hieronder weigert DELETE sowieso.
        CONSTRAINT [FK_SigningEvent_SigningCase]  FOREIGN KEY ([SigningCaseId])  REFERENCES [dbo].[SigningCase]([Id]),
        CONSTRAINT [FK_SigningEvent_SigningParty] FOREIGN KEY ([SigningPartyId]) REFERENCES [dbo].[SigningParty]([Id])
    );
    CREATE INDEX [IX_SigningEvent_Case_Id]   ON [dbo].[SigningEvent]([SigningCaseId], [Id]);
    CREATE INDEX [IX_SigningEvent_Party]     ON [dbo].[SigningEvent]([SigningPartyId]);
    CREATE INDEX [IX_SigningEvent_EventType] ON [dbo].[SigningEvent]([EventType]);
    PRINT 'Tabel SigningEvent aangemaakt.';
END
ELSE
    PRINT 'Tabel SigningEvent bestaat al, overgeslagen.';
GO

-- Append-only: de audit trail mag nooit gewijzigd of verwijderd worden, ook niet door een
-- rechtstreekse UPDATE/DELETE. De retentiescrub (§8) is de ENIGE toegelaten wijziging en werkt
-- uitsluitend op de kolommen Ip en UserAgent (die niet in de hash-ketting zitten); alles anders
-- wordt geweigerd. Zie ONDERTEKENEN_VOORSTEL.md §3.7.
IF NOT EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_SigningEvent_AppendOnly')
BEGIN
    EXEC('
    CREATE TRIGGER [dbo].[TR_SigningEvent_AppendOnly] ON [dbo].[SigningEvent]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
        BEGIN
            THROW 51001, ''SigningEvent is append-only: verwijderen is niet toegestaan.'', 1;
        END
        -- UPDATE: enkel Ip/UserAgent mogen naar NULL (retentiescrub); alle andere kolommen moeten gelijk blijven.
        IF EXISTS (
            SELECT 1
            FROM inserted i
            JOIN deleted d ON d.Id = i.Id
            WHERE i.SigningCaseId <> d.SigningCaseId
               OR ISNULL(i.SigningPartyId, -1) <> ISNULL(d.SigningPartyId, -1)
               OR i.EventType <> d.EventType
               OR i.OccurredAtUtc <> d.OccurredAtUtc
               OR i.ActorType <> d.ActorType
               OR ISNULL(i.ActorUserId, -1) <> ISNULL(d.ActorUserId, -1)
               OR ISNULL(i.ActorLabel, '''') <> ISNULL(d.ActorLabel, '''')
               OR ISNULL(i.IpHash, '''') <> ISNULL(d.IpHash, '''')
               OR ISNULL(i.UserAgentHash, '''') <> ISNULL(d.UserAgentHash, '''')
               OR ISNULL(i.DocumentSha256, '''') <> ISNULL(d.DocumentSha256, '''')
               OR ISNULL(i.DataJson, '''') <> ISNULL(d.DataJson, '''')
               OR ISNULL(i.PrevEventHash, '''') <> ISNULL(d.PrevEventHash, '''')
               OR i.EventHash <> d.EventHash
               OR (i.Ip IS NOT NULL AND ISNULL(i.Ip, '''') <> ISNULL(d.Ip, ''''))
               OR (i.UserAgent IS NOT NULL AND ISNULL(i.UserAgent, '''') <> ISNULL(d.UserAgent, ''''))
        )
        BEGIN
            THROW 51002, ''SigningEvent is append-only: enkel Ip/UserAgent mogen (naar NULL) gescrubd worden.'', 1;
        END
        UPDATE e SET e.Ip = i.Ip, e.UserAgent = i.UserAgent
        FROM [dbo].[SigningEvent] e JOIN inserted i ON i.Id = e.Id;
    END');
    PRINT 'Trigger TR_SigningEvent_AppendOnly aangemaakt.';
END
ELSE
    PRINT 'Trigger TR_SigningEvent_AppendOnly bestaat al, overgeslagen.';
GO

-- ---- 8. ClientContactChangeLog -------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ClientContactChangeLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ClientContactChangeLog] (
        [Id]               INT            IDENTITY(1,1) NOT NULL,
        [EntityType]       NVARCHAR(30)   NOT NULL,
        [EntityId]         INT            NOT NULL,
        [ClientAccountId]  INT            NOT NULL,
        [Field]            NVARCHAR(30)   NOT NULL,
        [OldValueMasked]   NVARCHAR(100)  NULL,
        [NewValueMasked]   NVARCHAR(100)  NULL,
        [ChangedByUserId]  INT            NULL,
        [ChangedAt]        DATETIME2(7)   NOT NULL CONSTRAINT [DF_ClientContactChangeLog_ChangedAt] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_ClientContactChangeLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE INDEX [IX_ClientContactChangeLog_Account_At] ON [dbo].[ClientContactChangeLog]([ClientAccountId], [ChangedAt]);
    CREATE INDEX [IX_ClientContactChangeLog_Entity]     ON [dbo].[ClientContactChangeLog]([EntityType], [EntityId]);
    PRINT 'Tabel ClientContactChangeLog aangemaakt.';
END
ELSE
    PRINT 'Tabel ClientContactChangeLog bestaat al, overgeslagen.';
GO

-- ---- 9. Seed: beleid voor wijzigingsopdrachten -------------------------------
-- De akkoordtekst hier is de standaard uit de opdracht; ze is beleid (bewerkbaar), geen code.
-- Laat ze juridisch nakijken vóór de eerste echte verzending (ONDERTEKENEN_VOORSTEL.md §9.10).
IF NOT EXISTS (SELECT 1 FROM [dbo].[SigningPolicy] WHERE [DocumentType] = 'ChangeOrder')
BEGIN
    INSERT INTO [dbo].[SigningPolicy]
        ([DocumentType], [DisplayName], [SignatureMethod], [SigningRule], [OtpRequired], [VerificationMethod],
         [OtpValiditySeconds], [OtpMaxAttempts], [LinkValidityDays], [ReminderAfterDays], [ReminderRepeatDays], [MaxReminders],
         [ConsentText], [RetentionDays], [IsActive])
    VALUES
        ('ChangeOrder', N'Wijzigingsopdracht', 'internal-ses', 0, 1, 'EmailOtp',
         600, 5, 30, 3, 7, 3,
         N'Ik bevestig dat ik dit document en de bijbehorende gegevens heb gelezen en ga akkoord met de inhoud, de uitvoering van de beschreven werken en de vermelde prijs.',
         NULL, 1);
    PRINT 'SigningPolicy ChangeOrder toegevoegd.';
END
ELSE
    PRINT 'SigningPolicy ChangeOrder bestaat al, overgeslagen.';
GO

-- ---- Controle ----
SELECT t.name AS Tabel, (SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id = t.object_id AND i.index_id > 0) AS Indexen
FROM   sys.tables t
WHERE  t.name IN ('SigningPolicy','SigningCase','SigningDocument','SigningParty','SigningAccessToken','SigningVerification','SigningEvent','ClientContactChangeLog')
ORDER BY t.name;
GO
