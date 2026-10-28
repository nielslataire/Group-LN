-- =============================================
-- Migratie: 124_OutlookAfspraak
-- Datum: 2026-10-28
-- Omschrijving: Outlook-sync voor Mijn taken (spec #2, ticket #7): het koppelrecord tussen een afspraak uit de Outlook-agenda en een project.
--   Eén rij per afspraak per medewerker: het Graph-event-id, de medewerker (UserId), het project en het moment van koppelen.
--   De afspraakdetails (titel, tijd, deelnemers, plaats) blijven live uit Graph komen: er staat GEEN kopie van agenda-inhoud in deze tabel.
--   Een koppeling blijft onthouden; verdwijnt het project, dan verdwijnt het koppelrecord mee (ON DELETE CASCADE).
--   Strikt additief + idempotent. Draai dit script VOOR je de nieuwe versie van de applicatie opstart, anders mislukken Mijn taken
--   (Vandaag en Week) met "Invalid object name 'OutlookAfspraak'".
-- =============================================
IF OBJECT_ID('dbo.OutlookAfspraak', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OutlookAfspraak]
    (
        [Id]          INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OutlookAfspraak] PRIMARY KEY,
        [UserId]      INT NOT NULL,
        [EventId]     NVARCHAR(400) NOT NULL,
        [ProjectId]   INT NOT NULL,
        [GekoppeldOp] DATETIME2 NOT NULL CONSTRAINT [DF_OutlookAfspraak_GekoppeldOp] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_OutlookAfspraak_Users]   FOREIGN KEY ([UserId])    REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_OutlookAfspraak_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectId]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [UQ_OutlookAfspraak_User_Event] ON [dbo].[OutlookAfspraak]([UserId], [EventId]);
    CREATE INDEX [IX_OutlookAfspraak_Project] ON [dbo].[OutlookAfspraak]([ProjectId]);
    PRINT 'Tabel OutlookAfspraak aangemaakt.';
END
ELSE PRINT 'Tabel OutlookAfspraak bestaat al, overgeslagen.';
GO
