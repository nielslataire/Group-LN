-- =============================================
-- Migratie: 133_TerugkerendeFacturen
-- Datum: 2026-11-06
-- Omschrijving: Terugkerende facturen (design-handoff punt 49 en 50).
--   RecurringInvoiceSchedule       een schema: klant, ritme, lijnen-instellingen, wat het CRM bij elke factuur doet, indexering.
--   RecurringInvoiceScheduleLine   de lijnen van het schema (omschrijving met variabelen, aantal, prijs, btw, enkel de eerste, indexeren).
--   RecurringInvoiceEvent          de geschiedenis van een schema (aangemaakt, concept gemaakt, gepauzeerd, ...).
--   RecurringInvoiceSettings       standaardinstellingen per facturatiebedrijf (zijpaneel Instellingen op het Terugkerend-overzicht).
--   Invoices.RecurringScheduleId / RecurringOccurrence / RecurringLabel / RecurringPeriodText
--                                  welke factuur uit welk schema komt (het ↻-label in de lijst met facturen).
--   Alleen toevoegen: bestaande facturen blijven ongewijzigd. Idempotent: tweemaal draaien doet niets extra.
--   LET OP: draai dit VÓÓR de app opnieuw start. De app leest de nieuwe kolommen op Invoices bij elke factuurquery.
-- =============================================

IF OBJECT_ID('dbo.RecurringInvoiceSchedule', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RecurringInvoiceSchedule]
    (
        [Id]                    INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_RecurringInvoiceSchedule] PRIMARY KEY,
        [IssuerCompanyId]       INT            NOT NULL,
        [Status]                TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_Status] DEFAULT (0),      -- 0 concept, 1 actief, 2 gepauzeerd, 3 stopgezet, 4 afgelopen
        [PartyType]             TINYINT        NOT NULL,                                                         -- 1 ClientAccount, 2 ClientContact, 3 Supplier (zoals InvoicePartyType)
        [PartyId]               INT            NOT NULL,
        [ProjectId]             INT            NULL,
        [SupplierContractId]    INT            NULL,
        [IssuerBankAccountId]   INT            NULL,
        [PaymentTermId]         INT            NULL,
        [VatTypeId]             INT            NULL,
        [IsPrepaid]             BIT            NOT NULL CONSTRAINT [DF_RecInvSchedule_Prepaid] DEFAULT (0),
        [HeaderDescription]     NVARCHAR(MAX)  NULL,
        [DetailDescription]     NVARCHAR(MAX)  NULL,
        [FooterDescription]     NVARCHAR(MAX)  NULL,
        -- ritme
        [Frequency]             TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_Freq] DEFAULT (2),         -- 1 week, 2 maand, 3 kwartaal, 4 jaar
        [DayMode]               TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_DayMode] DEFAULT (1),      -- 1 vaste dag, 2 laatste dag van de maand
        [DayOfMonth]            TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_Day] DEFAULT (1),          -- 1-31 (bij een week: 1 = maandag ... 5 = vrijdag)
        [FirstDate]             DATE           NOT NULL,
        [PeriodMode]            TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_Period] DEFAULT (1),       -- 1 lopende periode, 2 vorige periode
        [EndMode]               TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_EndMode] DEFAULT (0),      -- 0 nooit, 1 na aantal, 2 op datum
        [EndCount]              INT            NULL,
        [EndDate]               DATE           NULL,
        -- wat het CRM telkens doet
        [OnCreateMode]          TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_OnCreate] DEFAULT (1),     -- 1 klaarzetten ter controle, 2 automatisch nummeren en versturen
        [DraftLeadDays]         TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_Lead] DEFAULT (3),
        [WeekendRule]           TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_Weekend] DEFAULT (1),      -- 1 volgende werkdag, 2 vorige werkdag, 3 ongewijzigd
        [ResumeMode]            TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_Resume] DEFAULT (1),       -- 1 telkens vragen, 2 gemiste periodes overslaan
        [WarnUnpaid]            BIT            NOT NULL CONSTRAINT [DF_RecInvSchedule_Warn] DEFAULT (1),
        -- indexering
        [IndexationEnabled]     BIT            NOT NULL CONSTRAINT [DF_RecInvSchedule_IdxOn] DEFAULT (0),
        [IndexationType]        TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_IdxType] DEFAULT (1),      -- 1 gezondheidsindex, 2 consumptieprijzen, 3 vast percentage
        [IndexationWhen]        TINYINT        NOT NULL CONSTRAINT [DF_RecInvSchedule_IdxWhen] DEFAULT (1),      -- 1 op 1 januari, 2 op de verjaardag van de start, 3 op een andere datum
        [IndexationBaseDate]    DATE           NULL,                                                             -- startdatum voor indexering (bv. ingangsdatum van het huurcontract); leeg = eerste factuur
        [IndexationDay]         TINYINT        NULL,                                                             -- bij IndexationWhen = 3: dag
        [IndexationMonth]       TINYINT        NULL,                                                             -- bij IndexationWhen = 3: maand
        [IndexationPercent]     DECIMAL(9,4)   NULL,                                                             -- het percentage dat toegepast wordt
        [IndexationConfirm]     BIT            NOT NULL CONSTRAINT [DF_RecInvSchedule_IdxConfirm] DEFAULT (1),
        [IndexationAckCount]    INT            NOT NULL CONSTRAINT [DF_RecInvSchedule_IdxAck] DEFAULT (0),       -- aantal indexaties dat al bevestigd is
        -- voortgang
        [OccurrenceIndex]       INT            NOT NULL CONSTRAINT [DF_RecInvSchedule_OccIdx] DEFAULT (0),       -- k-de factuurdatum van het patroon die nog moet komen
        [NextOccurrence]        DATE           NULL,                                                             -- datum van de volgende factuur (leeg = klaar of nog niet actief)
        [LastOccurrence]        DATE           NULL,
        [CreatedCount]          INT            NOT NULL CONSTRAINT [DF_RecInvSchedule_Count] DEFAULT (0),
        [LastInvoiceId]         INT            NULL,
        [PausedAt]              DATETIME2      NULL,
        [StoppedAt]             DATETIME2      NULL,
        [CreatedAt]             DATETIME2      NOT NULL CONSTRAINT [DF_RecInvSchedule_Created] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]             NVARCHAR(200)  NULL,
        [UpdatedAt]             DATETIME2      NULL
    );
    CREATE INDEX [IX_RecInvSchedule_Issuer_Status] ON [dbo].[RecurringInvoiceSchedule] ([IssuerCompanyId], [Status]);
    PRINT 'RecurringInvoiceSchedule aangemaakt.';
END
GO

IF OBJECT_ID('dbo.RecurringInvoiceScheduleLine', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RecurringInvoiceScheduleLine]
    (
        [Id]           INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_RecurringInvoiceScheduleLine] PRIMARY KEY,
        [ScheduleId]   INT            NOT NULL,
        [SortOrder]    INT            NOT NULL CONSTRAINT [DF_RecInvLine_Sort] DEFAULT (0),
        [Text]         NVARCHAR(200)  NOT NULL,
        [Quantity]     DECIMAL(18,3)  NOT NULL CONSTRAINT [DF_RecInvLine_Qty] DEFAULT (1),
        [UnitPrice]    DECIMAL(18,4)  NOT NULL CONSTRAINT [DF_RecInvLine_Price] DEFAULT (0),
        [VatTypeId]    INT            NULL,
        [VatPercentage] DECIMAL(9,4)  NOT NULL CONSTRAINT [DF_RecInvLine_Vat] DEFAULT (0),
        [FirstOnly]    BIT            NOT NULL CONSTRAINT [DF_RecInvLine_First] DEFAULT (0),                     -- enkel op de eerste factuur
        [Indexable]    BIT            NOT NULL CONSTRAINT [DF_RecInvLine_Idx] DEFAULT (0),
        CONSTRAINT [FK_RecInvLine_Schedule] FOREIGN KEY ([ScheduleId]) REFERENCES [dbo].[RecurringInvoiceSchedule] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_RecInvLine_Schedule] ON [dbo].[RecurringInvoiceScheduleLine] ([ScheduleId], [SortOrder]);
    PRINT 'RecurringInvoiceScheduleLine aangemaakt.';
END
GO

IF OBJECT_ID('dbo.RecurringInvoiceEvent', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RecurringInvoiceEvent]
    (
        [Id]         INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_RecurringInvoiceEvent] PRIMARY KEY,
        [ScheduleId] INT            NOT NULL,
        [At]         DATETIME2      NOT NULL CONSTRAINT [DF_RecInvEvent_At] DEFAULT (SYSUTCDATETIME()),
        [Text]       NVARCHAR(300)  NOT NULL,
        [Detail]     NVARCHAR(300)  NULL,
        CONSTRAINT [FK_RecInvEvent_Schedule] FOREIGN KEY ([ScheduleId]) REFERENCES [dbo].[RecurringInvoiceSchedule] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_RecInvEvent_Schedule] ON [dbo].[RecurringInvoiceEvent] ([ScheduleId], [At] DESC);
    PRINT 'RecurringInvoiceEvent aangemaakt.';
END
GO

IF OBJECT_ID('dbo.RecurringInvoiceSettings', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RecurringInvoiceSettings]
    (
        [IssuerCompanyId]     INT            NOT NULL CONSTRAINT [PK_RecurringInvoiceSettings] PRIMARY KEY,
        [OnCreateMode]        TINYINT        NOT NULL CONSTRAINT [DF_RecInvSet_OnCreate] DEFAULT (1),
        [DraftLeadDays]       TINYINT        NOT NULL CONSTRAINT [DF_RecInvSet_Lead] DEFAULT (3),
        [WeekendRule]         TINYINT        NOT NULL CONSTRAINT [DF_RecInvSet_Weekend] DEFAULT (1),
        [NotifyUsers]         NVARCHAR(1000) NULL,                                                               -- kommagescheiden e-mailadressen van wie een melding krijgt als een concept klaarstaat
        [IndexationType]      TINYINT        NOT NULL CONSTRAINT [DF_RecInvSet_IdxType] DEFAULT (1),
        [IndexationWhen]      TINYINT        NOT NULL CONSTRAINT [DF_RecInvSet_IdxWhen] DEFAULT (1),
        [IndexationConfirm]   BIT            NOT NULL CONSTRAINT [DF_RecInvSet_IdxConfirm] DEFAULT (1),
        [ResumeMode]          TINYINT        NOT NULL CONSTRAINT [DF_RecInvSet_Resume] DEFAULT (1),
        [WarnUnpaid]          BIT            NOT NULL CONSTRAINT [DF_RecInvSet_Warn] DEFAULT (1),
        [UpdatedAt]           DATETIME2      NULL
    );
    PRINT 'RecurringInvoiceSettings aangemaakt.';
END
GO

IF COL_LENGTH('dbo.Invoices', 'RecurringScheduleId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Invoices] ADD [RecurringScheduleId] INT NULL;
    PRINT 'Invoices.RecurringScheduleId toegevoegd.';
END
GO

IF COL_LENGTH('dbo.Invoices', 'RecurringOccurrence') IS NULL
BEGIN
    ALTER TABLE [dbo].[Invoices] ADD [RecurringOccurrence] DATE NULL;
    PRINT 'Invoices.RecurringOccurrence toegevoegd.';
END
GO

IF COL_LENGTH('dbo.Invoices', 'RecurringLabel') IS NULL
BEGIN
    ALTER TABLE [dbo].[Invoices] ADD [RecurringLabel] NVARCHAR(80) NULL;
    PRINT 'Invoices.RecurringLabel toegevoegd.';
END
GO

IF COL_LENGTH('dbo.Invoices', 'RecurringPeriodText') IS NULL
BEGIN
    ALTER TABLE [dbo].[Invoices] ADD [RecurringPeriodText] NVARCHAR(40) NULL;
    PRINT 'Invoices.RecurringPeriodText toegevoegd.';
END
GO

-- Nooit twee facturen voor dezelfde factuurdatum van hetzelfde schema (dubbel aanmaken bij twee gelijktijdige runs).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Invoices_RecurringSchedule_Occurrence' AND object_id = OBJECT_ID('dbo.Invoices'))
BEGIN
    CREATE UNIQUE INDEX [UQ_Invoices_RecurringSchedule_Occurrence] ON [dbo].[Invoices] ([RecurringScheduleId], [RecurringOccurrence])
        WHERE [RecurringScheduleId] IS NOT NULL AND [RecurringOccurrence] IS NOT NULL;
    PRINT 'Unieke index UQ_Invoices_RecurringSchedule_Occurrence aangemaakt.';
END
GO
