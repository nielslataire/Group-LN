-- =============================================
-- Migratie: 123_OutlookMailVoorstel
-- Datum: 2026-10-28
-- Omschrijving: Outlook-sync voor Mijn taken (spec #2, ticket #12): bij een mail die "Nog te koppelen" is, bewaart CPM een voorstel voor het project
--   (VoorgesteldProjectId). De gebruiker bevestigt of kiest zelf; er wordt nooit stil naar een gok gekoppeld.
--   Strikt additief + idempotent. Draai dit script na 122 en VOOR je de nieuwe versie van de applicatie opstart, anders mislukken de Inbox
--   en de Outlook-sync met "Invalid column name 'VoorgesteldProjectId'".
-- =============================================
IF OBJECT_ID('dbo.OutlookMail', 'U') IS NOT NULL AND COL_LENGTH('dbo.OutlookMail', 'VoorgesteldProjectId') IS NULL
BEGIN
    ALTER TABLE [dbo].[OutlookMail] ADD [VoorgesteldProjectId] INT NULL;
    PRINT 'Kolom OutlookMail.VoorgesteldProjectId toegevoegd.';
END
ELSE PRINT 'Kolom OutlookMail.VoorgesteldProjectId bestaat al (of de tabel ontbreekt), overgeslagen.';
GO

IF OBJECT_ID('dbo.FK_OutlookMail_Voorstel', 'F') IS NULL AND COL_LENGTH('dbo.OutlookMail', 'VoorgesteldProjectId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[OutlookMail] ADD CONSTRAINT [FK_OutlookMail_Voorstel] FOREIGN KEY ([VoorgesteldProjectId]) REFERENCES [dbo].[Project]([ProjectId]) ON DELETE SET NULL;
    PRINT 'Foreign key FK_OutlookMail_Voorstel toegevoegd.';
END
ELSE PRINT 'Foreign key FK_OutlookMail_Voorstel bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.OutlookMail', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OutlookMail_User_Conversation' AND object_id = OBJECT_ID('dbo.OutlookMail'))
BEGIN
    CREATE INDEX [IX_OutlookMail_User_Conversation] ON [dbo].[OutlookMail]([UserId], [ConversationId]) WHERE [ConversationId] IS NOT NULL;
    PRINT 'Index IX_OutlookMail_User_Conversation aangemaakt.';
END
ELSE PRINT 'Index IX_OutlookMail_User_Conversation bestaat al, overgeslagen.';
GO
