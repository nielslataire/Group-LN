-- =============================================
-- Migratie: 033_CookieConsentEvents
-- Datum: 2026-09-11
-- Omschrijving: Anonieme telling van de cookiebanner-uitkomst op de publieke
--   website (WWWCOPRO) — hoeveel bezoekers de banner te zien krijgen, ze
--   aanvaarden of weigeren. Geen IP, geen cookie-/sessie-id, geen koppeling
--   aan een bezoeker: puur een gebeurtenisteller per keuzemoment.
--   Reden: Google Analytics/GTM laadt pas ná toestemming (Consent Mode),
--   dus daarmee kan je nooit meten wie weigert of niet klikt — deze tabel
--   meet dat wél, los van GA.
--   WWWCOPRO schrijft er rechtstreeks in via SQL (buiten zijn legacy
--   BO-laag, zelfde stijl als HomeController.GetHomeHeroFeatured()).
--   CPMCore (Instellingen) leest en aggregeert via EF.
--   Strikt additief + idempotent.
-- =============================================

IF OBJECT_ID(N'[dbo].[CookieConsentEvent]', 'U') IS NULL
BEGIN
    EXEC(N'CREATE TABLE [dbo].[CookieConsentEvent]
    (
        [Id]             INT           NOT NULL IDENTITY(1,1),
        [EventType]      NVARCHAR(20)  NOT NULL,
        [OccurredAtUtc]  DATETIME2(7)  NOT NULL CONSTRAINT [DF_CookieConsentEvent_OccurredAtUtc] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_CookieConsentEvent]
            PRIMARY KEY CLUSTERED ([Id]),

        CONSTRAINT [CK_CookieConsentEvent_EventType]
            CHECK ([EventType] IN (N''Shown'', N''Accepted'', N''Rejected''))
    );');
    PRINT 'Tabel CookieConsentEvent aangemaakt.';
END
ELSE
    PRINT 'Tabel CookieConsentEvent bestaat al, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CookieConsentEvent_OccurredAtUtc' AND object_id = OBJECT_ID(N'[dbo].[CookieConsentEvent]'))
BEGIN
    EXEC(N'CREATE NONCLUSTERED INDEX [IX_CookieConsentEvent_OccurredAtUtc]
        ON [dbo].[CookieConsentEvent] ([OccurredAtUtc]) INCLUDE ([EventType]);');
    PRINT 'Index IX_CookieConsentEvent_OccurredAtUtc aangemaakt.';
END
ELSE
    PRINT 'Index IX_CookieConsentEvent_OccurredAtUtc bestaat al, overgeslagen.';

PRINT 'Migratie 033_CookieConsentEvents voltooid.';
