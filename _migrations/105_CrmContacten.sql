-- =============================================
-- Migratie: 105_CrmContacten
-- Datum: 2026-10-22
-- Omschrijving: Contacten van een project als echt opvolgsysteem (design-handoff punt 44, CRM Contacten).
--   Tot nu toe waren contacten enkel rijen in ContactRequests (formulieren van de website, interne acties, statusupdates) die per e-mail
--   gegroepeerd werden. Nieuw:
--   CrmContact       : één rij per persoon of koppel per project (status, bron, toegewezen aan, volgende actie, interesse, budget, toestemming).
--   CrmContactUnit   : de eenheden waarvoor iemand interesse heeft.
--   CrmContactEvent  : de tijdlijn "Opvolging" (formulier, notitie, gesprek, e-mail, bezichtiging, optie, statuswijziging).
--   CrmContactList   : opgeslagen lijsten (een naam + de filters; de lijst blijft automatisch bij, 44d).
--   CrmExportLog     : elke export wordt gelogd (wie, wanneer, welke lijst, hoeveel contacten, formaat).
--   De bestaande rijen in ContactRequests blijven ongewijzigd; CPM importeert ze zelf (ook nieuwe website-formulieren) de eerste keer dat
--   de contactenpagina opent. CrmContactEvent.LegacyRequestId onthoudt welke rij al verwerkt is.
--   Status: 0 Nieuw, 1 Contact, 2 Bezichtiging, 3 Optie, 4 Verkocht, 5 Afgevallen. Strikt additief + idempotent.
-- =============================================
IF OBJECT_ID('dbo.CrmContact', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CrmContact] (
        [Id]                 INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CrmContact] PRIMARY KEY,
        [ProjectId]          INT NOT NULL,
        [FirstName]          NVARCHAR(100) NULL,
        [LastName]           NVARCHAR(150) NULL,
        [PartnerName]        NVARCHAR(150) NULL,
        [Email]              NVARCHAR(200) NULL,
        [Phone]              NVARCHAR(50) NULL,
        [Street]             NVARCHAR(200) NULL,
        [PostalCode]         NVARCHAR(20) NULL,
        [City]               NVARCHAR(100) NULL,
        [Source]             NVARCHAR(20) NOT NULL CONSTRAINT [DF_CrmContact_Source] DEFAULT (N'Website'),
        [BrokerName]         NVARCHAR(150) NULL,
        [SeenVia]            NVARCHAR(200) NULL,
        [Status]             INT NOT NULL CONSTRAINT [DF_CrmContact_Status] DEFAULT (0),
        [StatusReason]       NVARCHAR(300) NULL,
        [AssignedUserId]     INT NULL,
        [AssignedName]       NVARCHAR(150) NULL,
        [Budget]             NVARCHAR(100) NULL,
        [Financing]          NVARCHAR(300) NULL,
        [OwnHome]            NVARCHAR(300) NULL,
        [Wish]               NVARCHAR(300) NULL,
        [ConsentContact]     BIT NOT NULL CONSTRAINT [DF_CrmContact_ConsentContact] DEFAULT (1),
        [ConsentNewsletter]  BIT NOT NULL CONSTRAINT [DF_CrmContact_ConsentNewsletter] DEFAULT (0),
        [ConsentAt]          DATETIME2(0) NULL,
        [NextActionText]     NVARCHAR(200) NULL,
        [NextActionDate]     DATE NULL,
        [NextActionUserId]   INT NULL,
        [NextActionName]     NVARCHAR(150) NULL,
        [VisitAt]            DATETIME2(0) NULL,
        [VisitUnit]          NVARCHAR(100) NULL,
        [OptionUntil]        DATE NULL,
        [OptionUnit]         NVARCHAR(100) NULL,
        [CreatedAt]          DATETIME2(0) NOT NULL CONSTRAINT [DF_CrmContact_CreatedAt] DEFAULT (sysutcdatetime()),
        [CreatedBy]          NVARCHAR(150) NULL,
        [LastActivityAt]     DATETIME2(0) NULL,
        CONSTRAINT [FK_CrmContact_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectId]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_CrmContact_Project_Status] ON [dbo].[CrmContact] ([ProjectId], [Status]);
    CREATE INDEX [IX_CrmContact_Project_Email] ON [dbo].[CrmContact] ([ProjectId], [Email]);
    PRINT 'Tabel CrmContact aangemaakt.';
END
ELSE PRINT 'Tabel CrmContact bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.CrmContactUnit', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CrmContactUnit] (
        [ContactId] INT NOT NULL,
        [UnitId]    INT NOT NULL,
        CONSTRAINT [PK_CrmContactUnit] PRIMARY KEY ([ContactId], [UnitId]),
        CONSTRAINT [FK_CrmContactUnit_Contact] FOREIGN KEY ([ContactId]) REFERENCES [dbo].[CrmContact]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CrmContactUnit_Unit] FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id])
    );
    PRINT 'Tabel CrmContactUnit aangemaakt.';
END
ELSE PRINT 'Tabel CrmContactUnit bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.CrmContactEvent', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CrmContactEvent] (
        [Id]              INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CrmContactEvent] PRIMARY KEY,
        [ContactId]       INT NOT NULL,
        [Kind]            NVARCHAR(20) NOT NULL,
        [OccurredAt]      DATETIME2(0) NOT NULL CONSTRAINT [DF_CrmContactEvent_OccurredAt] DEFAULT (sysutcdatetime()),
        [UserName]        NVARCHAR(150) NULL,
        [Title]           NVARCHAR(300) NULL,
        [Text]            NVARCHAR(MAX) NULL,
        [DurationMin]     INT NULL,
        [IsAuto]          BIT NOT NULL CONSTRAINT [DF_CrmContactEvent_IsAuto] DEFAULT (0),
        [LegacyRequestId] INT NULL,
        CONSTRAINT [FK_CrmContactEvent_Contact] FOREIGN KEY ([ContactId]) REFERENCES [dbo].[CrmContact]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_CrmContactEvent_Contact_Occurred] ON [dbo].[CrmContactEvent] ([ContactId], [OccurredAt]);
    CREATE INDEX [IX_CrmContactEvent_Legacy] ON [dbo].[CrmContactEvent] ([LegacyRequestId]) WHERE [LegacyRequestId] IS NOT NULL;
    PRINT 'Tabel CrmContactEvent aangemaakt.';
END
ELSE PRINT 'Tabel CrmContactEvent bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.CrmContactList', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CrmContactList] (
        [Id]          INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CrmContactList] PRIMARY KEY,
        [ProjectId]   INT NOT NULL,
        [Name]        NVARCHAR(150) NOT NULL,
        [FiltersJson] NVARCHAR(MAX) NULL,
        [CreatedBy]   NVARCHAR(150) NULL,
        [CreatedAt]   DATETIME2(0) NOT NULL CONSTRAINT [DF_CrmContactList_CreatedAt] DEFAULT (sysutcdatetime()),
        CONSTRAINT [FK_CrmContactList_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectId]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_CrmContactList_Project] ON [dbo].[CrmContactList] ([ProjectId]);
    PRINT 'Tabel CrmContactList aangemaakt.';
END
ELSE PRINT 'Tabel CrmContactList bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.CrmExportLog', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CrmExportLog] (
        [Id]           INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CrmExportLog] PRIMARY KEY,
        [ProjectId]    INT NOT NULL,
        [UserName]     NVARCHAR(150) NULL,
        [ListName]     NVARCHAR(150) NULL,
        [Format]       NVARCHAR(10) NOT NULL,
        [ContactCount] INT NOT NULL,
        [Skipped]      INT NOT NULL CONSTRAINT [DF_CrmExportLog_Skipped] DEFAULT (0),
        [ExportedAt]   DATETIME2(0) NOT NULL CONSTRAINT [DF_CrmExportLog_ExportedAt] DEFAULT (sysutcdatetime()),
        CONSTRAINT [FK_CrmExportLog_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectId]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_CrmExportLog_Project] ON [dbo].[CrmExportLog] ([ProjectId], [ExportedAt]);
    PRINT 'Tabel CrmExportLog aangemaakt.';
END
ELSE PRINT 'Tabel CrmExportLog bestaat al, overgeslagen.';
GO
