-- =============================================
-- Migratie: Planning secties en taken
-- Datum: 2026-03-30
-- Omschrijving: Maakt tabellen PlanningSectie en
--               PlanningTaak aan voor taak-gebaseerde
--               voortgangsberekening per project.
--               Als taken bestaan voor een sectie
--               gekoppeld aan een activiteitengroep,
--               vervangt het gewogen taak-voortgang
--               de gefactureerde cappingmethode.
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSectie]')
)
BEGIN
    CREATE TABLE [dbo].[PlanningSectie] (
        [Id]                INT             NOT NULL IDENTITY(1,1)  PRIMARY KEY,
        [ProjectId]         INT             NOT NULL,
        [ActivityGroupId]   INT             NULL,
        [Naam]              NVARCHAR(200)   NOT NULL,
        [Volgorde]          INT             NOT NULL DEFAULT 0,
        [IsActief]          BIT             NOT NULL DEFAULT 1,

        CONSTRAINT [FK_PlanningSectie_Project]
            FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]),
        CONSTRAINT [FK_PlanningSectie_ActivityGroup]
            FOREIGN KEY ([ActivityGroupId]) REFERENCES [dbo].[ActivityGroup]([GroupID])
    );

    CREATE INDEX [IX_PlanningSectie_ProjectId]
        ON [dbo].[PlanningSectie] ([ProjectId]);

    PRINT 'Tabel PlanningSectie aangemaakt.';
END
ELSE
    PRINT 'Tabel PlanningSectie bestaat al, overgeslagen.';

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE object_id = OBJECT_ID(N'[dbo].[PlanningTaak]')
)
BEGIN
    CREATE TABLE [dbo].[PlanningTaak] (
        [Id]                INT             NOT NULL IDENTITY(1,1)  PRIMARY KEY,
        [PlanningSectieId]  INT             NOT NULL,
        [Naam]              NVARCHAR(300)   NOT NULL,
        [Beschrijving]      NVARCHAR(1000)  NULL,
        [Startdatum]        DATE            NULL,
        [Einddatum]         DATE            NULL,
        [AfgerondOp]        DATE            NULL,
        [IsAfgerond]        BIT             NOT NULL DEFAULT 0,
        [VoortgangPct]      INT             NOT NULL DEFAULT 0,
        [Volgorde]          INT             NOT NULL DEFAULT 0,

        CONSTRAINT [FK_PlanningTaak_PlanningSectie]
            FOREIGN KEY ([PlanningSectieId]) REFERENCES [dbo].[PlanningSectie]([Id])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_PlanningTaak_PlanningSectieId]
        ON [dbo].[PlanningTaak] ([PlanningSectieId]);

    PRINT 'Tabel PlanningTaak aangemaakt.';
END
ELSE
    PRINT 'Tabel PlanningTaak bestaat al, overgeslagen.';
