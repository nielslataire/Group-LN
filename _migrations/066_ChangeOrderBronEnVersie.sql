-- =============================================
-- Migratie: 066_ChangeOrderBronEnVersie
-- Datum: 2026-10-02
-- Omschrijving: "Offertes & wijzigingen" gl-v2, design-handoff punt 28/29 (het scherm per stap + de
--   drie routes naar het opmaakscherm). Twee dingen die het scherm toont maar nog nergens bewaard
--   werden:
--   1) het originele offertebestand van de leverancier (28a, kaart "Offerte leverancier": voorbeeld
--      + bijlage) — tot nu toe werden in 20c enkel de bijgesneden regio's bewaard, niet het bestand
--      zelf;
--   2) de herkomst van een kopie of een nieuwe versie (29b "Kopie": "Bron = de oorspronkelijke WO";
--      28c/28h "Versie 2 maken") — een verwijzing naar de ChangeOrder waarvan deze rij vertrok.
--   Bewust GEEN foreign key op SourceChangeOrderId: de bron is louter informatief (de kaart "Bron"),
--   en een FK zou het verwijderen van de oorspronkelijke WO blokkeren. Strikt additief + idempotent.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'QuoteSourcePath')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [QuoteSourcePath] NVARCHAR(300) NULL;
    PRINT 'Kolom QuoteSourcePath toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Kolom QuoteSourcePath bestaat al, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'QuoteSourceFileName')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [QuoteSourceFileName] NVARCHAR(260) NULL;
    PRINT 'Kolom QuoteSourceFileName toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Kolom QuoteSourceFileName bestaat al, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'SourceChangeOrderId')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [SourceChangeOrderId] INT NULL;
    PRINT 'Kolom SourceChangeOrderId toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Kolom SourceChangeOrderId bestaat al, overgeslagen.';

-- 1 = kopie (zelfde werk voor een andere eenheid), 2 = nieuwe versie (vervangt de bron).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'SourceKind')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [SourceKind] TINYINT NULL;
    PRINT 'Kolom SourceKind toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Kolom SourceKind bestaat al, overgeslagen.';
