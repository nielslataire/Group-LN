-- =============================================
-- Migratie: UserPinnedProject
-- Datum: 2026-09-04
-- Omschrijving:
--   Laat een gebruiker een project "vastzetten" op het projectleider-
--   dashboard (Mijn Werven), ook als het project niet aan hem/haar is
--   toegewezen (Project.AspNetUserID komt niet overeen met de gebruiker).
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserPinnedProject' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[UserPinnedProject] (
        [Id]        INT       IDENTITY(1,1) NOT NULL,
        [UserId]    INT       NOT NULL,
        [ProjectId] INT       NOT NULL,
        [PinnedAt]  DATETIME2 NOT NULL CONSTRAINT [DF_UserPinnedProject_PinnedAt] DEFAULT (GETDATE()),

        CONSTRAINT [PK_UserPinnedProject]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [FK_UserPinnedProject_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([ID])
            ON DELETE CASCADE,

        CONSTRAINT [FK_UserPinnedProject_Project]
            FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project] ([ProjectID])
            ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [UX_UserPinnedProject_User_Project]
        ON [dbo].[UserPinnedProject] ([UserId], [ProjectId]);

    PRINT 'Tabel UserPinnedProject aangemaakt.';
END
ELSE
    PRINT 'Tabel UserPinnedProject bestaat al, overgeslagen.';
