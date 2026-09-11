-- =============================================
-- Migratie: 035_TrajectTriggers
-- Datum: 2026-09-11
-- Omschrijving: Trigger-laag van de trajectopvolging (increment 3).
--   - TrajectSjabloonMijlpaalTrigger: trigger-definitie op een sjabloon-mijlpaal
--   - MijlpaalTrigger: concrete trigger op een mijlpaal-instantie
--   - MijlpaalTriggerRun: audit-log per uitvoering (spiegelt IssueNotificationRun)
--   Strikt additief + idempotent.
--
--   TriggerEvent: 0=BijBereiken 1=BijOverschrijding 2=XDagenVoorDoeldatum
--       3=BijStatuswijziging 4=BijFaseAfronding
--   TriggerActie: 0=VerwittigRol 1=VerwittigGebruiker 2=MaakTaak 3=MaakDossier
--       4=ZetProjectVlag 5=ZetProjectStatus 6=PlanHerinnering
--       7=DeblokkeerVolgendeFase 8=StuurDossierAanvraagMail
--   TriggerRunStatus: 0=Geslaagd 1=Mislukt 2=Overgeslagen
-- =============================================

------------------------------------------------------------
-- 1. TrajectSjabloonMijlpaalTrigger
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[TrajectSjabloonMijlpaalTrigger]'))
BEGIN
    CREATE TABLE [dbo].[TrajectSjabloonMijlpaalTrigger] (
        [Id]                      INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [TrajectSjabloonMijlpaalId] INT           NOT NULL,
        [TriggerEvent]            INT             NOT NULL DEFAULT 0,
        [TriggerActie]            INT             NOT NULL DEFAULT 0,
        [OffsetDagen]             INT             NULL,
        [ActieParametersJson]     NVARCHAR(MAX)   NULL,
        [MagProjectWijzigen]      BIT             NOT NULL DEFAULT 0,
        [IsActief]                BIT             NOT NULL DEFAULT 1,
        [Omschrijving]            NVARCHAR(300)   NULL,

        CONSTRAINT [FK_SjabloonMijlpaalTrigger_Mijlpaal]
            FOREIGN KEY ([TrajectSjabloonMijlpaalId]) REFERENCES [dbo].[TrajectSjabloonMijlpaal]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_SjabloonMijlpaalTrigger_MijlpaalId]
        ON [dbo].[TrajectSjabloonMijlpaalTrigger] ([TrajectSjabloonMijlpaalId]);

    PRINT 'Tabel TrajectSjabloonMijlpaalTrigger aangemaakt.';
END
ELSE
    PRINT 'Tabel TrajectSjabloonMijlpaalTrigger bestaat al, overgeslagen.';

------------------------------------------------------------
-- 2. MijlpaalTrigger
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[MijlpaalTrigger]'))
BEGIN
    CREATE TABLE [dbo].[MijlpaalTrigger] (
        [Id]                  INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [MijlpaalId]          INT             NOT NULL,
        [SjabloonTriggerId]   INT             NULL,
        [TriggerEvent]        INT             NOT NULL DEFAULT 0,
        [TriggerActie]        INT             NOT NULL DEFAULT 0,
        [OffsetDagen]         INT             NULL,
        [ActieParametersJson] NVARCHAR(MAX)   NULL,
        [MagProjectWijzigen]  BIT             NOT NULL DEFAULT 0,
        [IsActief]            BIT             NOT NULL DEFAULT 1,
        [LaatstGevuurdOp]     DATETIME2(7)    NULL,

        CONSTRAINT [FK_MijlpaalTrigger_Mijlpaal]
            FOREIGN KEY ([MijlpaalId]) REFERENCES [dbo].[Mijlpaal]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MijlpaalTrigger_SjabloonTrigger]
            FOREIGN KEY ([SjabloonTriggerId]) REFERENCES [dbo].[TrajectSjabloonMijlpaalTrigger]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_MijlpaalTrigger_MijlpaalId]
        ON [dbo].[MijlpaalTrigger] ([MijlpaalId]);

    PRINT 'Tabel MijlpaalTrigger aangemaakt.';
END
ELSE
    PRINT 'Tabel MijlpaalTrigger bestaat al, overgeslagen.';

------------------------------------------------------------
-- 3. MijlpaalTriggerRun (audit-log)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[MijlpaalTriggerRun]'))
BEGIN
    CREATE TABLE [dbo].[MijlpaalTriggerRun] (
        [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [MijlpaalTriggerId] INT             NOT NULL,
        [MijlpaalId]        INT             NOT NULL,
        [Uitgevoerd]        DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [Status]            INT             NOT NULL DEFAULT 0,
        [Resultaat]         NVARCHAR(MAX)   NULL,
        [Fout]              NVARCHAR(MAX)   NULL,

        CONSTRAINT [FK_MijlpaalTriggerRun_Trigger]
            FOREIGN KEY ([MijlpaalTriggerId]) REFERENCES [dbo].[MijlpaalTrigger]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_MijlpaalTriggerRun_TriggerId]
        ON [dbo].[MijlpaalTriggerRun] ([MijlpaalTriggerId]);
    CREATE NONCLUSTERED INDEX [IX_MijlpaalTriggerRun_Uitgevoerd]
        ON [dbo].[MijlpaalTriggerRun] ([Uitgevoerd] DESC);

    PRINT 'Tabel MijlpaalTriggerRun aangemaakt.';
END
ELSE
    PRINT 'Tabel MijlpaalTriggerRun bestaat al, overgeslagen.';

PRINT 'Migratie 035_TrajectTriggers voltooid.';
