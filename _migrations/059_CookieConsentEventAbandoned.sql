-- =============================================
-- Migratie: 059_CookieConsentEventAbandoned
-- Datum: 2026-09-29
-- Omschrijving: Vierde gebeurtenistype 'Abandoned' ("Verlaten") voor de cookiebanner-telling
--   (zie 033_CookieConsentEvents). Tot nu was "geen keuze" een restwaarde (Shown - Accepted -
--   Rejected) en dus vervuild door bots, aarzelaars over meerdere sessies en nieuwe tabbladen.
--   WWWCOPRO stuurt nu een echte gebeurtenis wanneer een bezoeker de site verlaat terwijl de
--   banner nog open staat (cookie-consent.js), en filtert bots server-side
--   (CookieConsentController.vb). Enkel de CHECK-constraint wordt verruimd: geen datawijziging,
--   geen kolomwijziging. Idempotent (opnieuw uitvoeren is veilig).
-- =============================================

IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = N'CK_CookieConsentEvent_EventType'
             AND parent_object_id = OBJECT_ID(N'[dbo].[CookieConsentEvent]')
             AND definition NOT LIKE N'%Abandoned%')
BEGIN
    ALTER TABLE [dbo].[CookieConsentEvent] DROP CONSTRAINT [CK_CookieConsentEvent_EventType];
    PRINT 'Oude CK_CookieConsentEvent_EventType verwijderd.';
END

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_CookieConsentEvent_EventType'
                 AND parent_object_id = OBJECT_ID(N'[dbo].[CookieConsentEvent]'))
BEGIN
    ALTER TABLE [dbo].[CookieConsentEvent] WITH CHECK
        ADD CONSTRAINT [CK_CookieConsentEvent_EventType]
        CHECK ([EventType] IN (N'Shown', N'Accepted', N'Rejected', N'Abandoned'));
    PRINT 'CK_CookieConsentEvent_EventType aangemaakt met Abandoned.';
END
ELSE
    PRINT 'CK_CookieConsentEvent_EventType bevat Abandoned al, overgeslagen.';
GO

PRINT 'Migratie 059_CookieConsentEventAbandoned voltooid.';
