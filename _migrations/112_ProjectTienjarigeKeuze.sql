-- =============================================
-- Migratie: 112_ProjectTienjarigeKeuze
-- Datum: 2026-10-23
-- Omschrijving: Verzekeringen: de keuze voor de tienjarige aansprakelijkheid geldt voor het hele project i.p.v. per aannemer.
--   Project.TienjarigeBron: NULL = per aannemer afgeleid uit de werken, 1 = via projectpolis (globale polis van de ontwikkelaar, ook bij woningen).
--     Een contract met een eigen keuze (Contract.TienjarigeBron, migratie 111) blijft die houden.
--   Project.TienjarigeOpmerking: vrije opmerking, bv. "polis volgt eind november van de notaris". Staat bij het aandachtspunt als de polis nog ontbreekt.
--   Strikt additief + idempotent. Draai 110 en 111 eerst.
-- =============================================
IF COL_LENGTH('dbo.Project', 'TienjarigeBron') IS NULL
BEGIN
    ALTER TABLE [dbo].[Project] ADD [TienjarigeBron] INT NULL;
    PRINT 'Kolom Project.TienjarigeBron toegevoegd.';
END
ELSE PRINT 'Kolom Project.TienjarigeBron bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.Project', 'TienjarigeOpmerking') IS NULL
BEGIN
    ALTER TABLE [dbo].[Project] ADD [TienjarigeOpmerking] NVARCHAR(300) NULL;
    PRINT 'Kolom Project.TienjarigeOpmerking toegevoegd.';
END
ELSE PRINT 'Kolom Project.TienjarigeOpmerking bestaat al, overgeslagen.';
GO
