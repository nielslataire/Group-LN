-- =============================================
-- Migratie: 042_NutsAansluitingWerkstroomDatums
-- Datum: 2026-09-19
-- Omschrijving: Vijf getypeerde werkstroomdatums op ProjectNutsAansluiting, rechtstreeks
--   invulbaar op het aanmaak-/bewerkformulier i.p.v. enkel via de generieke
--   ProjectDossierSubstap-checklist (die blijft ernaast bestaan en wordt door de app nu
--   automatisch gespiegeld vanuit deze datums, zodat de trajectbindingen
--   DossierSubstap/AlleGekoppeldeDossierSubstappen/AlleNutsaanvragenSubstap gewoon
--   blijven werken ongeacht welke UI de gebruiker gebruikt):
--     - VerwachteOfferteDatum : planning, geen bijhorende checklist-stap (louter een streefdatum)
--     - OfferteOntvangenOp    -> substap OFFERTE_ONTVANGEN
--     - OfferteGoedgekeurdOp  -> substap OFFERTE_GOEDGEKEURD
--     - UitvoeringGevraagdOp  -> substap UITVOERINGSDATUM_DOORGEGEVEN
--     - UitgevoerdOp          -> substap UITGEVOERD
--   (AanvraagVerstuurdOp bestaat al sinds migratie 037 en dekt "aanvraagdatum".)
--   Strikt additief + idempotent. Geen schemawijziging aan bestaande kolommen.
-- =============================================

IF COL_LENGTH('dbo.ProjectNutsAansluiting', 'VerwachteOfferteDatum') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectNutsAansluiting] ADD [VerwachteOfferteDatum] DATE NULL;
    PRINT 'Kolom ProjectNutsAansluiting.VerwachteOfferteDatum toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectNutsAansluiting.VerwachteOfferteDatum bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.ProjectNutsAansluiting', 'OfferteOntvangenOp') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectNutsAansluiting] ADD [OfferteOntvangenOp] DATE NULL;
    PRINT 'Kolom ProjectNutsAansluiting.OfferteOntvangenOp toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectNutsAansluiting.OfferteOntvangenOp bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.ProjectNutsAansluiting', 'OfferteGoedgekeurdOp') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectNutsAansluiting] ADD [OfferteGoedgekeurdOp] DATE NULL;
    PRINT 'Kolom ProjectNutsAansluiting.OfferteGoedgekeurdOp toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectNutsAansluiting.OfferteGoedgekeurdOp bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.ProjectNutsAansluiting', 'UitvoeringGevraagdOp') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectNutsAansluiting] ADD [UitvoeringGevraagdOp] DATE NULL;
    PRINT 'Kolom ProjectNutsAansluiting.UitvoeringGevraagdOp toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectNutsAansluiting.UitvoeringGevraagdOp bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.ProjectNutsAansluiting', 'UitgevoerdOp') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProjectNutsAansluiting] ADD [UitgevoerdOp] DATE NULL;
    PRINT 'Kolom ProjectNutsAansluiting.UitgevoerdOp toegevoegd.';
END
ELSE
    PRINT 'Kolom ProjectNutsAansluiting.UitgevoerdOp bestaat al, overgeslagen.';

PRINT 'Migratie 042_NutsAansluitingWerkstroomDatums voltooid.';
