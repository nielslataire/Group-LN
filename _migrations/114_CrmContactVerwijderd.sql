-- =============================================
-- Migratie: 114_CrmContactVerwijderd
-- Datum: 2026-10-23
-- Omschrijving: Een verwijderd contact kwam terug omdat de contactenpagina de oude aanvragen (ContactRequests) opnieuw overneemt zodra de bijhorende
--   tijdlijn-berichten weg zijn. CrmContactLegacySkip onthoudt welke aanvragen bewust verwijderd zijn, zodat ze niet opnieuw ingelezen worden.
--   Strikt additief + idempotent.
-- =============================================
IF OBJECT_ID('dbo.CrmContactLegacySkip', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CrmContactLegacySkip] (
        [LegacyRequestId] INT NOT NULL CONSTRAINT [PK_CrmContactLegacySkip] PRIMARY KEY,
        [ProjectId]       INT NOT NULL,
        [DeletedAt]       DATETIME2(0) NOT NULL CONSTRAINT [DF_CrmContactLegacySkip_DeletedAt] DEFAULT (sysutcdatetime())
    );
    CREATE INDEX [IX_CrmContactLegacySkip_Project] ON [dbo].[CrmContactLegacySkip] ([ProjectId]);
    PRINT 'Tabel CrmContactLegacySkip aangemaakt.';
END
ELSE PRINT 'Tabel CrmContactLegacySkip bestaat al, overgeslagen.';
GO
