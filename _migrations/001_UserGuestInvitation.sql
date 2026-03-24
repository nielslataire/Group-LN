-- =============================================
-- Migratie: B2B Gastuitnodiging tabellen
-- Datum: 2026-03-24
-- Omschrijving: Voegt UserGuestInvitation en
--               UserGuestInvitationAudit toe
-- =============================================

-- ---- 1. UserGuestInvitation ----------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserGuestInvitation' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[UserGuestInvitation] (
        [Id]                   INT            IDENTITY(1,1) NOT NULL,
        [UserId]               INT            NOT NULL,
        [Provider]             NVARCHAR(50)   NOT NULL CONSTRAINT [DF_UserGuestInvitation_Provider]  DEFAULT ('MicrosoftEntra'),
        [ExternalObjectId]     NVARCHAR(128)  NULL,
        [ExternalTenantId]     NVARCHAR(128)  NULL,
        [ExternalEmail]        NVARCHAR(256)  NOT NULL,
        [UserType]             NVARCHAR(20)   NOT NULL CONSTRAINT [DF_UserGuestInvitation_UserType]  DEFAULT ('Guest'),
        [InvitationStatus]     NVARCHAR(30)   NOT NULL CONSTRAINT [DF_UserGuestInvitation_Status]   DEFAULT ('Draft'),
        [InvitationSentAt]     DATETIME2(7)   NULL,
        [InvitationRedeemedAt] DATETIME2(7)   NULL,
        [LastLoginAt]          DATETIME2(7)   NULL,
        [InviteRedeemUrl]      NVARCHAR(2048) NULL,
        [InvitedByUserId]      INT            NULL,
        [CreatedAt]            DATETIME2(7)   NOT NULL CONSTRAINT [DF_UserGuestInvitation_CreatedAt] DEFAULT (GETUTCDATE()),
        [UpdatedAt]            DATETIME2(7)   NOT NULL CONSTRAINT [DF_UserGuestInvitation_UpdatedAt] DEFAULT (GETUTCDATE()),

        CONSTRAINT [PK_UserGuestInvitation]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [FK_UserGuestInvitation_Users]
            FOREIGN KEY ([UserId])
            REFERENCES [dbo].[Users] ([Id])
            ON DELETE CASCADE,

        CONSTRAINT [FK_UserGuestInvitation_InvitedByUser]
            FOREIGN KEY ([InvitedByUserId])
            REFERENCES [dbo].[Users] ([Id])
    );

    CREATE INDEX [IX_UserGuestInvitation_UserId]
        ON [dbo].[UserGuestInvitation] ([UserId]);

    CREATE INDEX [IX_UserGuestInvitation_ExternalObjectId]
        ON [dbo].[UserGuestInvitation] ([ExternalObjectId])
        WHERE [ExternalObjectId] IS NOT NULL;

    CREATE INDEX [IX_UserGuestInvitation_ExternalEmail]
        ON [dbo].[UserGuestInvitation] ([ExternalEmail]);

    PRINT 'Tabel UserGuestInvitation aangemaakt.';
END
ELSE
    PRINT 'Tabel UserGuestInvitation bestaat al, overgeslagen.';

-- ---- 2. UserGuestInvitationAudit -----------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserGuestInvitationAudit' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[UserGuestInvitationAudit] (
        [Id]          INT            IDENTITY(1,1) NOT NULL,
        [UserId]      INT            NOT NULL,
        [Action]      NVARCHAR(50)   NOT NULL,
        [Details]     NVARCHAR(1000) NULL,
        [PerformedBy] INT            NULL,
        [PerformedAt] DATETIME2(7)   NOT NULL CONSTRAINT [DF_UserGuestInvitationAudit_PerformedAt] DEFAULT (GETUTCDATE()),

        CONSTRAINT [PK_UserGuestInvitationAudit]
            PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE INDEX [IX_UserGuestInvitationAudit_UserId]
        ON [dbo].[UserGuestInvitationAudit] ([UserId]);

    PRINT 'Tabel UserGuestInvitationAudit aangemaakt.';
END
ELSE
    PRINT 'Tabel UserGuestInvitationAudit bestaat al, overgeslagen.';
