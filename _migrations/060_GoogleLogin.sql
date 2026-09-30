-- =============================================
-- Migratie: 060_GoogleLogin
-- Datum: 2026-09-29
-- Omschrijving: Aannemers (en andere gasten) kunnen naast Microsoft Entra ook met een Google-account
--   op het portaal inloggen. De app praat daarvoor rechtstreeks met Google (OpenID Connect), dus
--   buiten Entra om: Entra B2B kan Google-accounts op een eigen domein (Google Workspace) niet
--   federeren en dwong die aannemers een Microsoft-account aan te maken, wat mislukte zodra hun
--   domein al in een Entra-tenant gekend was.
--   Users.EntraObjectId blijft de Entra-koppeling; GoogleSubjectId is de stabiele Google "sub"-claim
--   (max. 255 tekens volgens Google, in de praktijk ~21 cijfers). Eerste Google-login koppelt op het
--   uitgenodigde e-mailadres en vult deze kolom, daarna wordt enkel nog op GoogleSubjectId gematcht.
--   UserGuestInvitation.Provider (bestond al, default 'MicrosoftEntra') krijgt voortaan 'Google' bij
--   een Google-login; geen schemawijziging nodig.
--   Strikt additief + idempotent (opnieuw uitvoeren is veilig).
-- =============================================

IF COL_LENGTH('dbo.Users', 'GoogleSubjectId') IS NULL
    ALTER TABLE [dbo].[Users] ADD [GoogleSubjectId] NVARCHAR(255) NULL;
PRINT 'Users uitgebreid (GoogleSubjectId).';
GO

-- Uniek per gebruiker, maar NULL toegelaten voor iedereen zonder Google-login (gefilterde index).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_GoogleSubjectId' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_Users_GoogleSubjectId]
        ON [dbo].[Users] ([GoogleSubjectId])
        WHERE [GoogleSubjectId] IS NOT NULL;
PRINT 'Index UX_Users_GoogleSubjectId aanwezig.';
GO

PRINT 'Migratie 060_GoogleLogin voltooid.';
