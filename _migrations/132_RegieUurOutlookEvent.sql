-- =============================================
-- Migratie: 132_RegieUurOutlookEvent
-- Datum: 2026-11-06
-- Omschrijving: Regie-uren die uit een Outlook-afspraak komen (Mijn taken > Overzicht > Uren goedkeuren, spec #2, ticket #9).
--   ProjectRegieUur.OutlookEventId   Graph-id van de afspraak; samen met UserId de dedup-sleutel (nooit twee uren voor dezelfde afspraak).
--   ProjectRegieUur.OutlookStart/-Einde  begin en einde van de afspraak op het moment van goedkeuren, om een latere wijziging te herkennen.
--   Alleen toevoegen: bestaande uren en de factureerflow blijven ongewijzigd. Idempotent: tweemaal draaien doet niets extra.
-- =============================================

IF COL_LENGTH('dbo.ProjectRegieUur', 'OutlookEventId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectRegieUur] ADD [OutlookEventId] NVARCHAR(300) NULL;
    PRINT 'ProjectRegieUur.OutlookEventId toegevoegd.';
END
GO

IF COL_LENGTH('dbo.ProjectRegieUur', 'OutlookStart') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectRegieUur] ADD [OutlookStart] DATETIME2 NULL;
    PRINT 'ProjectRegieUur.OutlookStart toegevoegd.';
END
GO

IF COL_LENGTH('dbo.ProjectRegieUur', 'OutlookEinde') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectRegieUur] ADD [OutlookEinde] DATETIME2 NULL;
    PRINT 'ProjectRegieUur.OutlookEinde toegevoegd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_ProjectRegieUur_User_OutlookEvent' AND object_id = OBJECT_ID('dbo.ProjectRegieUur'))
BEGIN
    CREATE UNIQUE INDEX [UQ_ProjectRegieUur_User_OutlookEvent] ON [dbo].[ProjectRegieUur] ([UserId], [OutlookEventId]) WHERE [OutlookEventId] IS NOT NULL;
    PRINT 'Unieke index UQ_ProjectRegieUur_User_OutlookEvent aangemaakt.';
END
GO
