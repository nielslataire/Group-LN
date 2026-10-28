-- =============================================
-- Migratie: 122_OutlookMail
-- Datum: 2026-10-28
-- Omschrijving: Outlook-sync voor Mijn taken (spec #2, ticket #5): gesynchroniseerde mailberichten en geblokkeerde afzenders.
--   OutlookMail: één rij per mail per gebruiker, ENKEL metadata: richting (in/uit), Graph-id en internet-message-id (dedup), conversatie-id, afzender,
--   ontvangers, onderwerp, ontvangstdatum, een voorvertoning van maximaal 300 tekens en de deeplink naar Outlook, plus de koppeling met project, klant
--   (ClientAccount) of leverancier (CompanyInfo) en de status (nieuw, afgehandeld, te-koppelen). Geen bijlagen en geen volledige mailtekst.
--   OutlookGeblokkeerdeAfzender: afzenders die een gebruiker blijvend negeert; de sync slaat ze over.
--   Strikt additief + idempotent. Draai dit script na 121 en VOOR je de nieuwe versie van de applicatie opstart, anders mislukken de Inbox van Mijn taken
--   en de Outlook-sync met "Invalid object name 'OutlookMail'".
-- =============================================
IF OBJECT_ID('dbo.OutlookMail', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OutlookMail]
    (
        [Id]                  INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OutlookMail] PRIMARY KEY,
        [UserId]              INT NOT NULL,
        [Richting]            NVARCHAR(3) NOT NULL,
        [GraphId]             NVARCHAR(200) NOT NULL,
        [InternetMessageId]   NVARCHAR(500) NULL,
        [ConversationId]      NVARCHAR(200) NULL,
        [Afzender]            NVARCHAR(320) NULL,
        [AfzenderNaam]        NVARCHAR(200) NULL,
        [Ontvangers]          NVARCHAR(2000) NULL,
        [Onderwerp]           NVARCHAR(500) NULL,
        [OntvangenOp]         DATETIME2 NOT NULL,
        [Voorvertoning]       NVARCHAR(300) NULL,
        [WebLink]             NVARCHAR(1000) NULL,
        [ProjectId]           INT NULL,
        [KlantId]             INT NULL,
        [LeverancierId]       INT NULL,
        [Status]              NVARCHAR(20) NOT NULL CONSTRAINT [DF_OutlookMail_Status] DEFAULT ('nieuw'),
        [IsPrive]             BIT NOT NULL CONSTRAINT [DF_OutlookMail_IsPrive] DEFAULT (0),
        [VerwijderdInOutlook] BIT NOT NULL CONSTRAINT [DF_OutlookMail_Verwijderd] DEFAULT (0),
        [AangemaaktOp]        DATETIME2 NOT NULL CONSTRAINT [DF_OutlookMail_AangemaaktOp] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_OutlookMail_Users]    FOREIGN KEY ([UserId])        REFERENCES [dbo].[Users]([Id]),
        -- Verdwijnt het project, de klant of de leverancier, dan blijft de mail bestaan zonder koppeling (komt terug bij "Nog te koppelen").
        CONSTRAINT [FK_OutlookMail_Project]  FOREIGN KEY ([ProjectId])     REFERENCES [dbo].[Project]([ProjectId]) ON DELETE SET NULL,
        CONSTRAINT [FK_OutlookMail_Klant]    FOREIGN KEY ([KlantId])       REFERENCES [dbo].[ClientAccount]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_OutlookMail_Leverancier] FOREIGN KEY ([LeverancierId]) REFERENCES [dbo].[CompanyInfo]([CompanyId]) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX [UQ_OutlookMail_User_GraphId] ON [dbo].[OutlookMail]([UserId], [GraphId]);
    CREATE UNIQUE INDEX [UQ_OutlookMail_User_InternetMessageId] ON [dbo].[OutlookMail]([UserId], [InternetMessageId]) WHERE [InternetMessageId] IS NOT NULL;
    CREATE INDEX [IX_OutlookMail_User_Status] ON [dbo].[OutlookMail]([UserId], [Richting], [Status]);
    CREATE INDEX [IX_OutlookMail_Project] ON [dbo].[OutlookMail]([ProjectId]) WHERE [ProjectId] IS NOT NULL;
    PRINT 'Tabel OutlookMail aangemaakt.';
END
ELSE PRINT 'Tabel OutlookMail bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.OutlookGeblokkeerdeAfzender', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OutlookGeblokkeerdeAfzender]
    (
        [Id]           INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OutlookGeblokkeerdeAfzender] PRIMARY KEY,
        [UserId]       INT NOT NULL,
        [Email]        NVARCHAR(320) NOT NULL,
        [AangemaaktOp] DATETIME2 NOT NULL CONSTRAINT [DF_OutlookGeblokkeerdeAfzender_AangemaaktOp] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_OutlookGeblokkeerdeAfzender_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE UNIQUE INDEX [UQ_OutlookGeblokkeerdeAfzender_User_Email] ON [dbo].[OutlookGeblokkeerdeAfzender]([UserId], [Email]);
    PRINT 'Tabel OutlookGeblokkeerdeAfzender aangemaakt.';
END
ELSE PRINT 'Tabel OutlookGeblokkeerdeAfzender bestaat al, overgeslagen.';
GO
