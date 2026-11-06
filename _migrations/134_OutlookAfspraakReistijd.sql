-- =============================================
-- Migratie: 134_OutlookAfspraakReistijd
-- Datum: 2026-11-06
-- Omschrijving: Outlook-sync voor Mijn taken (spec #2, ticket #8): de koppeling tussen een afspraak en haar blok "Reistijd".
--   Bij Nieuwe afspraak maakt CPM naast de afspraak een apart bezet blok voor de reistijd. Deze tabel onthoudt welk Graph-event dat blok is,
--   zodat het blok meeverhuist als de afspraak in CPM of in Outlook verplaatst of hernoemd wordt, en meegaat bij verwijderen.
--   Eén rij per afspraak per medewerker; ook voor persoonlijke afspraken (zonder project). Geen kopie van agenda-inhoud: enkel de twee event-id's en de lengte.
--   Strikt additief + idempotent. Draai dit script VOOR je de nieuwe versie van de applicatie opstart, anders mislukken Mijn taken
--   (Vandaag en Week) met "Invalid object name 'OutlookAfspraakReistijd'".
-- =============================================
IF OBJECT_ID('dbo.OutlookAfspraakReistijd', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OutlookAfspraakReistijd]
    (
        [Id]           INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OutlookAfspraakReistijd] PRIMARY KEY,
        [UserId]       INT NOT NULL,
        [EventId]      NVARCHAR(400) NOT NULL,   -- de afspraak zelf
        [BlokEventId]  NVARCHAR(400) NOT NULL,   -- het blok Reistijd dat er vlak voor eindigt
        [Minuten]      INT NOT NULL,             -- lengte van het blok bij het aanmaken (reistijd + marge)
        [GemaaktOp]    DATETIME2 NOT NULL CONSTRAINT [DF_OutlookAfspraakReistijd_GemaaktOp] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [FK_OutlookAfspraakReistijd_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE UNIQUE INDEX [UQ_OutlookAfspraakReistijd_User_Event] ON [dbo].[OutlookAfspraakReistijd]([UserId], [EventId]);
    PRINT 'Tabel OutlookAfspraakReistijd aangemaakt.';
END
ELSE PRINT 'Tabel OutlookAfspraakReistijd bestaat al, overgeslagen.';
GO
