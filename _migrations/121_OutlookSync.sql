-- =============================================
-- Migratie: 121_OutlookSync
-- Datum: 2026-10-28
-- Omschrijving: Outlook-sync voor Mijn taken (spec #2, ticket #6): per gebruiker de opt-in en de syncstatus. Eén rij per gebruiker
--   (aangemaakt bij het eerste aanzetten): Aan (standaard uit), AangezetOp (mails komen enkel vanaf dat moment), LaatsteSyncOp, Status
--   (wacht, ok, geen-toegang, geen-mailbox, fout), LaatsteFout (technische boodschap, geen mail- of agenda-inhoud), FoutenOpRij en
--   DeltaTokensJson (Graph-deltatoken per map, gebruikt door de mailsync). Er staat geen mail- of agenda-inhoud in.
--   Strikt additief + idempotent. Draai dit script VOOR je de nieuwe versie van de applicatie opstart, anders mislukken Mijn profiel
--   en de achtergrondservice met "Invalid object name 'OutlookSyncStatus'".
-- =============================================
IF OBJECT_ID('dbo.OutlookSyncStatus', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OutlookSyncStatus]
    (
        [Id]               INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OutlookSyncStatus] PRIMARY KEY,
        [UserId]           INT NOT NULL,
        [Aan]              BIT NOT NULL CONSTRAINT [DF_OutlookSyncStatus_Aan] DEFAULT (0),
        [AangezetOp]       DATETIME2 NULL,
        [LaatsteSyncOp]    DATETIME2 NULL,
        [Status]           NVARCHAR(20) NOT NULL CONSTRAINT [DF_OutlookSyncStatus_Status] DEFAULT ('wacht'),
        [LaatsteFout]      NVARCHAR(500) NULL,
        [FoutenOpRij]      INT NOT NULL CONSTRAINT [DF_OutlookSyncStatus_FoutenOpRij] DEFAULT (0),
        [DeltaTokensJson]  NVARCHAR(MAX) NULL,
        [GewijzigdOp]      DATETIME2 NOT NULL CONSTRAINT [DF_OutlookSyncStatus_GewijzigdOp] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_OutlookSyncStatus_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE UNIQUE INDEX [UQ_OutlookSyncStatus_UserId] ON [dbo].[OutlookSyncStatus]([UserId]);
    PRINT 'Tabel OutlookSyncStatus aangemaakt.';
END
ELSE PRINT 'Tabel OutlookSyncStatus bestaat al, overgeslagen.';
GO
