-- =============================================
-- Migratie: 100_PuntRondgang
-- Datum: 2026-10-14
-- Omschrijving: Punten, fase 6 (design-handoff 41d): een rondgang (opname + foto's) wordt op de server verwerkt (spraak naar tekst, punten herkennen)
--   en blijft bewaard tot de voorstellen bewaard worden of 14 dagen verstreken zijn. Overleeft zo een herstart van de applicatie.
--   PuntRondgang     : één rij per rondgang (status 0 wacht, 1 bezig, 2 klaar, 3 fout), transcript en voorstellen (JSON), verwijzing naar de opname.
--   PuntRondgangFoto : de foto's van de rondgang met het tijdstip in de opname. De bestanden zelf staan in de bestandsopslag (map "docs").
--   Strikt additief + idempotent.
-- =============================================
IF OBJECT_ID('dbo.PuntRondgang', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PuntRondgang] (
        [Id]              INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PuntRondgang] PRIMARY KEY,
        [ProjectId]       INT NOT NULL,
        [UserId]          NVARCHAR(450) NULL,
        [CreatedAt]       DATETIME2(0) NOT NULL CONSTRAINT [DF_PuntRondgang_CreatedAt] DEFAULT (sysutcdatetime()),
        [DurationSeconds] FLOAT NOT NULL CONSTRAINT [DF_PuntRondgang_Duration] DEFAULT (0),
        [VerslagId]       INT NULL,
        [DefaultUnitId]   INT NULL,
        [Status]          INT NOT NULL CONSTRAINT [DF_PuntRondgang_Status] DEFAULT (0),
        [Step]            NVARCHAR(60) NULL,
        [Percent]         INT NOT NULL CONSTRAINT [DF_PuntRondgang_Percent] DEFAULT (0),
        [Attempts]        INT NOT NULL CONSTRAINT [DF_PuntRondgang_Attempts] DEFAULT (0),
        [StartedAt]       DATETIME2(0) NULL,
        [AudioFile]       NVARCHAR(300) NULL,
        [AudioType]       NVARCHAR(100) NULL,
        [Transcript]      NVARCHAR(MAX) NULL,
        [VoorstellenJson] NVARCHAR(MAX) NULL,
        [Error]           NVARCHAR(500) NULL,
        CONSTRAINT [FK_PuntRondgang_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectId]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_PuntRondgang_Status_CreatedAt] ON [dbo].[PuntRondgang] ([Status], [CreatedAt]);
    CREATE INDEX [IX_PuntRondgang_Project_User] ON [dbo].[PuntRondgang] ([ProjectId], [UserId]);
    PRINT 'Tabel PuntRondgang aangemaakt.';
END
ELSE PRINT 'Tabel PuntRondgang bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.PuntRondgangFoto', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PuntRondgangFoto] (
        [Id]         INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PuntRondgangFoto] PRIMARY KEY,
        [RondgangId] INT NOT NULL,
        [Volgnr]     INT NOT NULL,
        [AtSeconds]  FLOAT NOT NULL CONSTRAINT [DF_PuntRondgangFoto_At] DEFAULT (0),
        [FileName]   NVARCHAR(300) NOT NULL,
        CONSTRAINT [FK_PuntRondgangFoto_Rondgang] FOREIGN KEY ([RondgangId]) REFERENCES [dbo].[PuntRondgang]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_PuntRondgangFoto_Rondgang_Volgnr] ON [dbo].[PuntRondgangFoto] ([RondgangId], [Volgnr]);
    PRINT 'Tabel PuntRondgangFoto aangemaakt.';
END
ELSE PRINT 'Tabel PuntRondgangFoto bestaat al, overgeslagen.';
GO
