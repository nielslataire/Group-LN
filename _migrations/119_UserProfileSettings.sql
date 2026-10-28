-- =============================================
-- Migratie: 119_UserProfileSettings
-- Datum: 2026-10-26
-- Omschrijving: Account > Mijn profiel (design-handoff "CRM Mijn profiel"): persoonlijke instellingen per gebruiker in een eigen tabel,
--   zodat Users ongemoeid blijft. Eén rij per gebruiker (aangemaakt bij het eerste opslaan):
--   vast nummer, werkdagen (bitmask: ma=1 di=2 wo=4 do=8 vr=16 za=32 zo=64) en werkuren, afwezigheid (van/tot, back-up, opties als bitmask),
--   meldingen (JSON met per gebeurtenis in-CRM/e-mail), dagelijkse samenvatting, stille uren en de deadline-herinnering.
--   Strikt additief + idempotent. Draai dit script VOOR je de nieuwe versie van de applicatie opstart, anders mislukt de pagina Mijn profiel
--   met "Invalid object name 'UserProfileSettings'".
-- =============================================
IF OBJECT_ID('dbo.UserProfileSettings', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserProfileSettings]
    (
        [Id]                   INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_UserProfileSettings] PRIMARY KEY,
        [UserId]               INT NOT NULL,
        [VastNummer]           NVARCHAR(50) NULL,
        [WerkDagen]            INT NOT NULL CONSTRAINT [DF_UserProfileSettings_WerkDagen] DEFAULT (31),
        [WerkVan]              NVARCHAR(5) NOT NULL CONSTRAINT [DF_UserProfileSettings_WerkVan] DEFAULT ('08:00'),
        [WerkTot]              NVARCHAR(5) NOT NULL CONSTRAINT [DF_UserProfileSettings_WerkTot] DEFAULT ('17:00'),
        [PauzeVan]             NVARCHAR(5) NULL CONSTRAINT [DF_UserProfileSettings_PauzeVan] DEFAULT ('12:00'),
        [PauzeTot]             NVARCHAR(5) NULL CONSTRAINT [DF_UserProfileSettings_PauzeTot] DEFAULT ('12:45'),
        [AfwezigAan]           BIT NOT NULL CONSTRAINT [DF_UserProfileSettings_AfwezigAan] DEFAULT (0),
        [AfwezigVan]           DATE NULL,
        [AfwezigTot]           DATE NULL,
        [BackupUserId]         INT NULL,
        [AfwezigOpties]        INT NOT NULL CONSTRAINT [DF_UserProfileSettings_AfwezigOpties] DEFAULT (3),
        [MeldingenJson]        NVARCHAR(MAX) NULL,
        [DagSamenvatting]      BIT NOT NULL CONSTRAINT [DF_UserProfileSettings_DagSamenvatting] DEFAULT (1),
        [SamenvattingTijd]     NVARCHAR(5) NOT NULL CONSTRAINT [DF_UserProfileSettings_SamenvattingTijd] DEFAULT ('07:30'),
        [StilleUren]           BIT NOT NULL CONSTRAINT [DF_UserProfileSettings_StilleUren] DEFAULT (1),
        [DringendDoorlaten]    BIT NOT NULL CONSTRAINT [DF_UserProfileSettings_DringendDoorlaten] DEFAULT (1),
        [DeadlineHerinnering]  NVARCHAR(4) NOT NULL CONSTRAINT [DF_UserProfileSettings_DeadlineHerinnering] DEFAULT ('3d'),
        [GewijzigdOp]          DATETIME2 NOT NULL CONSTRAINT [DF_UserProfileSettings_GewijzigdOp] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_UserProfileSettings_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_UserProfileSettings_Backup] FOREIGN KEY ([BackupUserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE UNIQUE INDEX [UQ_UserProfileSettings_UserId] ON [dbo].[UserProfileSettings]([UserId]);
    PRINT 'Tabel UserProfileSettings aangemaakt.';
END
ELSE PRINT 'Tabel UserProfileSettings bestaat al, overgeslagen.';
GO
