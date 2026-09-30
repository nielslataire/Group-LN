-- =============================================
-- Migratie: 061_BlogBlokFotoEnKnop
-- Datum: 2026-09-29
-- Omschrijving: Twee nieuwe inhoudsblok-types voor blog/detail (BlogArtikelBlok.BlokType,
--   zie 010_Blog.sql/011_BlogFaq.sql):
--   - 'foto': losse foto over de volledige kolombreedte (gebruikt het bestaande
--     FotoBestand; Titel wordt hergebruikt als optioneel bijschrift).
--   - 'knop': een centrale call-to-action-knop, met eigen knoptekst en URL.
--   Enkel de twee nieuwe kolommen KnopTekst/KnopUrl zijn nieuw; BlokType blijft een vrij
--   NVARCHAR-veld zonder CHECK-constraint (zie 010/011), dus de nieuwe waarden 'foto' en
--   'knop' vergen geen schema-wijziging voor BlokType zelf. Strikt additief + idempotent.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BlogArtikelBlok') AND name = 'KnopTekst')
BEGIN
    ALTER TABLE [dbo].[BlogArtikelBlok]
        ADD [KnopTekst] NVARCHAR(100) NULL;
    PRINT 'Kolom KnopTekst toegevoegd aan BlogArtikelBlok.';
END
ELSE
    PRINT 'Kolom KnopTekst bestaat al op BlogArtikelBlok, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BlogArtikelBlok') AND name = 'KnopUrl')
BEGIN
    ALTER TABLE [dbo].[BlogArtikelBlok]
        ADD [KnopUrl] NVARCHAR(500) NULL;
    PRINT 'Kolom KnopUrl toegevoegd aan BlogArtikelBlok.';
END
ELSE
    PRINT 'Kolom KnopUrl bestaat al op BlogArtikelBlok, overgeslagen.';

PRINT 'Migratie 061_BlogBlokFotoEnKnop voltooid.';
