-- =============================================
-- Migratie: 120_MijnTakenAssistent
-- Datum: 2026-10-26
-- Omschrijving: Mijn taken als persoonlijke assistent (design-handoff punt 2, "CRM Mijn taken en Deadlines"): vastleggen (inbox),
--   verwerken (volgende actie, plannen, delegeren, ooit), plannen (blok in dag/week), doen (timer) en terugblikken (overzicht, wekelijkse review).
--   1. ProjectTaak krijgt extra kolommen (alle NULL of met standaard, bestaande taken blijven ongewijzigd):
--      Context ('@bureau'), SchattingMinuten, GeplandStart + GeplandMinuten (blok in de agenda), IsInbox (vastgelegd, nog te verwerken),
--      InboxBron/InboxMeta (waar het vandaan komt), Ooit ("ooit/misschien"), WachtOp + WachtOpDatum (gedelegeerd, jij volgt op).
--   2. TaakTijd: gewerkte tijd per gebruiker (een lopende timer = rij zonder Einde).
--   3. TaakWeekReview: afgevinkte punten van de wekelijkse review per gebruiker en week.
--   Strikt additief + idempotent. Draai dit script VOOR je de nieuwe versie van de applicatie opstart, anders mislukt alles dat ProjectTaak leest
--   (Mijn taken, trajecten, dossiers, punten) met "Invalid column name 'Context'".
-- =============================================
IF COL_LENGTH('dbo.ProjectTaak', 'Context') IS NULL
    ALTER TABLE [dbo].[ProjectTaak] ADD [Context] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.ProjectTaak', 'SchattingMinuten') IS NULL
    ALTER TABLE [dbo].[ProjectTaak] ADD [SchattingMinuten] INT NULL;
IF COL_LENGTH('dbo.ProjectTaak', 'GeplandStart') IS NULL
    ALTER TABLE [dbo].[ProjectTaak] ADD [GeplandStart] DATETIME2 NULL;
IF COL_LENGTH('dbo.ProjectTaak', 'GeplandMinuten') IS NULL
    ALTER TABLE [dbo].[ProjectTaak] ADD [GeplandMinuten] INT NULL;
IF COL_LENGTH('dbo.ProjectTaak', 'IsInbox') IS NULL
    ALTER TABLE [dbo].[ProjectTaak] ADD [IsInbox] BIT NOT NULL CONSTRAINT [DF_ProjectTaak_IsInbox] DEFAULT (0);
IF COL_LENGTH('dbo.ProjectTaak', 'InboxBron') IS NULL
    ALTER TABLE [dbo].[ProjectTaak] ADD [InboxBron] NVARCHAR(30) NULL;
IF COL_LENGTH('dbo.ProjectTaak', 'InboxMeta') IS NULL
    ALTER TABLE [dbo].[ProjectTaak] ADD [InboxMeta] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.ProjectTaak', 'Ooit') IS NULL
    ALTER TABLE [dbo].[ProjectTaak] ADD [Ooit] BIT NOT NULL CONSTRAINT [DF_ProjectTaak_Ooit] DEFAULT (0);
IF COL_LENGTH('dbo.ProjectTaak', 'WachtOp') IS NULL
    ALTER TABLE [dbo].[ProjectTaak] ADD [WachtOp] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.ProjectTaak', 'WachtOpDatum') IS NULL
    ALTER TABLE [dbo].[ProjectTaak] ADD [WachtOpDatum] DATE NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProjectTaak_GeplandStart' AND object_id = OBJECT_ID('dbo.ProjectTaak'))
    CREATE INDEX [IX_ProjectTaak_GeplandStart] ON [dbo].[ProjectTaak]([GeplandStart]);
GO

IF OBJECT_ID('dbo.TaakTijd', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TaakTijd]
    (
        [Id]        INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_TaakTijd] PRIMARY KEY,
        [UserId]    INT NOT NULL,
        [TaakId]    INT NULL,
        [ProjectId] INT NULL,
        [Titel]     NVARCHAR(200) NOT NULL,
        [Context]   NVARCHAR(50) NULL,
        [Start]     DATETIME2 NOT NULL,
        [Einde]     DATETIME2 NULL,
        CONSTRAINT [FK_TaakTijd_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_TaakTijd_ProjectTaak] FOREIGN KEY ([TaakId]) REFERENCES [dbo].[ProjectTaak]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_TaakTijd_User_Start] ON [dbo].[TaakTijd]([UserId], [Start]);
    PRINT 'Tabel TaakTijd aangemaakt.';
END
ELSE PRINT 'Tabel TaakTijd bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.TaakWeekReview', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TaakWeekReview]
    (
        [Id]        INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_TaakWeekReview] PRIMARY KEY,
        [UserId]    INT NOT NULL,
        [WeekStart] DATE NOT NULL,
        [Afgevinkt] INT NOT NULL CONSTRAINT [DF_TaakWeekReview_Afgevinkt] DEFAULT (0),
        CONSTRAINT [FK_TaakWeekReview_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE UNIQUE INDEX [UQ_TaakWeekReview_User_Week] ON [dbo].[TaakWeekReview]([UserId], [WeekStart]);
    PRINT 'Tabel TaakWeekReview aangemaakt.';
END
ELSE PRINT 'Tabel TaakWeekReview bestaat al, overgeslagen.';
GO
