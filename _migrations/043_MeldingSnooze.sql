-- =============================================
-- Migratie: 043_MeldingSnooze
-- Datum: 2026-09-21
-- Omschrijving: Server-side snooze-opslag voor het gl-v2 dashboard-meldingenscherm
--   (design-handoff 7b). Vervangt de eerdere client-side (localStorage) snooze-implementatie in
--   de legacy _DashboardProjectleider.cshtml-meldingencentrum voor gl-v2-pagina's — nu per
--   gebruiker echt bewaard, overleeft een herlaad/ander toestel.
--
--   De onderliggende "meldingen" (verzekeringswaarschuwingen, projectinfo, aannemer-comments)
--   zijn zelf GEEN echte entiteiten met een stabiele, uniforme Id over de drie bronnen heen
--   (WarningBO.ID betekent per bron iets anders — zie HomeController/_DashboardProjectleider).
--   [MeldingKey] is daarom een SHA2_256-hash (hex, 64 tekens) van "ProjectId|Category|Tekst" —
--   dezelfde de-facto samengestelde sleutel die de bestaande client-side snooze al gebruikte,
--   nu hash-gebaseerd i.p.v. Tekst zelf op te slaan (kan willekeurig lang zijn).
--   Strikt additief + idempotent.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[MeldingSnooze]'))
BEGIN
    CREATE TABLE [dbo].[MeldingSnooze] (
        [Id]             INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [UserId]         INT             NOT NULL,
        [MeldingKey]     CHAR(64)        NOT NULL,
        [SnoozedUntil]   DATETIME2(7)    NOT NULL,
        [CreatedDate]    DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [UQ_MeldingSnooze_User_Key] UNIQUE ([UserId], [MeldingKey])
    );

    CREATE NONCLUSTERED INDEX [IX_MeldingSnooze_UserId_SnoozedUntil] ON [dbo].[MeldingSnooze] ([UserId], [SnoozedUntil]);

    PRINT 'Tabel MeldingSnooze aangemaakt.';
END
ELSE
    PRINT 'Tabel MeldingSnooze bestaat al, overgeslagen.';

PRINT 'Migratie 043_MeldingSnooze voltooid.';
