-- =============================================
-- Migratie: 029_TrajectMijlpalen
-- Datum: 2026-09-10
-- Omschrijving: Instantie-laag van de trajectopvolging.
--   Projecttraject (1:1 met Project) -> ProjecttrajectFase -> Mijlpaal,
--   met MijlpaalHistoriek (audit) en MijlpaalAfhankelijkheid.
--   Strikt additief + idempotent: enkel CREATE TABLE / CREATE INDEX.
--   Sjabloon-laag + FK Projecttraject.TrajectSjabloonId volgen in 030.
--   Na uitvoeren: DAL-entities bijwerken (EF Core Power Tools re-scaffold
--   of met de hand) + ConfigureTrajectEntities in cpmRunningContext.
-- =============================================

------------------------------------------------------------
-- 1. Projecttraject
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[Projecttraject]'))
BEGIN
    CREATE TABLE [dbo].[Projecttraject] (
        [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjectId]         INT             NOT NULL,
        [TrajectSjabloonId] INT             NULL,
        [Naam]              NVARCHAR(200)   NOT NULL,
        [Status]            INT             NOT NULL DEFAULT 0,
        [GestartOp]         DATE            NULL,
        [AfgerondOp]        DATE            NULL,
        [HerberekendOp]     DATETIME2(7)    NULL,
        [Waarschuwingen]    NVARCHAR(MAX)   NULL,
        [CreatedDate]       DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId]   NVARCHAR(128)   NULL,
        [ModifiedDate]      DATETIME2(7)    NULL,
        [ModifiedByUserId]  NVARCHAR(128)   NULL,

        CONSTRAINT [FK_Projecttraject_Project]
            FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE,
        CONSTRAINT [UX_Projecttraject_ProjectId] UNIQUE ([ProjectId])
    );
    PRINT 'Tabel Projecttraject aangemaakt.';
END
ELSE
    PRINT 'Tabel Projecttraject bestaat al, overgeslagen.';

------------------------------------------------------------
-- 2. ProjecttrajectFase
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ProjecttrajectFase]'))
BEGIN
    CREATE TABLE [dbo].[ProjecttrajectFase] (
        [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjecttrajectId]  INT             NOT NULL,
        [SjabloonFaseId]    INT             NULL,
        [Naam]              NVARCHAR(200)   NOT NULL,
        [Code]              NVARCHAR(50)    NULL,
        [Volgorde]          INT             NOT NULL DEFAULT 0,
        [Status]            INT             NOT NULL DEFAULT 0,
        [StartGepland]      DATE            NULL,
        [StartWerkelijk]    DATE            NULL,
        [EindGepland]       DATE            NULL,
        [EindWerkelijk]     DATE            NULL,
        [IsVergrendeld]     BIT             NOT NULL DEFAULT 0,
        [KleurCode]         NVARCHAR(20)    NULL,

        CONSTRAINT [FK_ProjecttrajectFase_Projecttraject]
            FOREIGN KEY ([ProjecttrajectId]) REFERENCES [dbo].[Projecttraject]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_ProjecttrajectFase_ProjecttrajectId]
        ON [dbo].[ProjecttrajectFase] ([ProjecttrajectId]);

    PRINT 'Tabel ProjecttrajectFase aangemaakt.';
END
ELSE
    PRINT 'Tabel ProjecttrajectFase bestaat al, overgeslagen.';

------------------------------------------------------------
-- 3. Mijlpaal
--    ProjecttrajectFaseId: NO ACTION om multiple-cascade-path te vermijden
--    (traject -> fase -> mijlpaal EN traject -> mijlpaal).
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[Mijlpaal]'))
BEGIN
    CREATE TABLE [dbo].[Mijlpaal] (
        [Id]                          INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjecttrajectId]            INT             NOT NULL,
        [ProjecttrajectFaseId]        INT             NULL,
        [SjabloonMijlpaalId]          INT             NULL,
        [Code]                        NVARCHAR(50)    NULL,
        [Naam]                        NVARCHAR(200)   NOT NULL,
        [Volgorde]                    INT             NOT NULL DEFAULT 0,
        [MijlpaalType]                INT             NOT NULL DEFAULT 0,
        [Status]                      INT             NOT NULL DEFAULT 0,
        [Doeldatum]                   DATE            NULL,
        [DoeldatumBerekend]           DATE            NULL,
        [WerkelijkeDatum]             DATE            NULL,
        [VerantwoordelijkeRol]        INT             NULL,
        [VerantwoordelijkePartijType] INT             NULL,
        [VerantwoordelijkePartijId]   INT             NULL,
        [VerantwoordelijkeUserId]     NVARCHAR(128)   NULL,
        [BronBinding]                 INT             NULL,
        [BronParam]                   NVARCHAR(100)   NULL,
        [BronRefId]                   INT             NULL,
        [DossierId]                   INT             NULL,
        [IsVerplicht]                 BIT             NOT NULL DEFAULT 1,
        [Opmerking]                   NVARCHAR(MAX)   NULL,
        [LastComputedDate]            DATETIME2(7)    NULL,
        [CreatedDate]                 DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId]             NVARCHAR(128)   NULL,
        [ModifiedDate]                DATETIME2(7)    NULL,
        [ModifiedByUserId]            NVARCHAR(128)   NULL,

        CONSTRAINT [FK_Mijlpaal_Projecttraject]
            FOREIGN KEY ([ProjecttrajectId]) REFERENCES [dbo].[Projecttraject]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Mijlpaal_ProjecttrajectFase]
            FOREIGN KEY ([ProjecttrajectFaseId]) REFERENCES [dbo].[ProjecttrajectFase]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Mijlpaal_ProjecttrajectId]
        ON [dbo].[Mijlpaal] ([ProjecttrajectId]);
    CREATE NONCLUSTERED INDEX [IX_Mijlpaal_Projecttraject_Status]
        ON [dbo].[Mijlpaal] ([ProjecttrajectId], [Status]);
    CREATE NONCLUSTERED INDEX [IX_Mijlpaal_ProjecttrajectFaseId]
        ON [dbo].[Mijlpaal] ([ProjecttrajectFaseId]);
    CREATE NONCLUSTERED INDEX [IX_Mijlpaal_Doeldatum]
        ON [dbo].[Mijlpaal] ([Doeldatum]);

    PRINT 'Tabel Mijlpaal aangemaakt.';
END
ELSE
    PRINT 'Tabel Mijlpaal bestaat al, overgeslagen.';

------------------------------------------------------------
-- 4. MijlpaalHistoriek  (audit; spiegelt ConstructionIssueHistory)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[MijlpaalHistoriek]'))
BEGIN
    CREATE TABLE [dbo].[MijlpaalHistoriek] (
        [Id]            INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [MijlpaalId]    INT             NOT NULL,
        [Actie]         INT             NOT NULL,
        [UserId]        NVARCHAR(128)   NULL,
        [Timestamp]     DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [OldValueJson]  NVARCHAR(MAX)   NULL,
        [NewValueJson]  NVARCHAR(MAX)   NULL,
        [Opmerking]     NVARCHAR(MAX)   NULL,

        CONSTRAINT [FK_MijlpaalHistoriek_Mijlpaal]
            FOREIGN KEY ([MijlpaalId]) REFERENCES [dbo].[Mijlpaal]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_MijlpaalHistoriek_MijlpaalId]
        ON [dbo].[MijlpaalHistoriek] ([MijlpaalId]);

    PRINT 'Tabel MijlpaalHistoriek aangemaakt.';
END
ELSE
    PRINT 'Tabel MijlpaalHistoriek bestaat al, overgeslagen.';

------------------------------------------------------------
-- 5. MijlpaalAfhankelijkheid
--    VereistMijlpaalId: NO ACTION (vermijd cascade-cyclus op dezelfde tabel)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[MijlpaalAfhankelijkheid]'))
BEGIN
    CREATE TABLE [dbo].[MijlpaalAfhankelijkheid] (
        [Id]                INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [MijlpaalId]        INT NOT NULL,
        [VereistMijlpaalId] INT NOT NULL,
        [Type]              INT NOT NULL DEFAULT 0,

        CONSTRAINT [FK_MijlpaalAfhankelijkheid_Mijlpaal]
            FOREIGN KEY ([MijlpaalId]) REFERENCES [dbo].[Mijlpaal]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MijlpaalAfhankelijkheid_Vereist]
            FOREIGN KEY ([VereistMijlpaalId]) REFERENCES [dbo].[Mijlpaal]([Id]),
        CONSTRAINT [UX_MijlpaalAfhankelijkheid] UNIQUE ([MijlpaalId], [VereistMijlpaalId])
    );

    PRINT 'Tabel MijlpaalAfhankelijkheid aangemaakt.';
END
ELSE
    PRINT 'Tabel MijlpaalAfhankelijkheid bestaat al, overgeslagen.';

PRINT 'Migratie 029_TrajectMijlpalen voltooid.';
