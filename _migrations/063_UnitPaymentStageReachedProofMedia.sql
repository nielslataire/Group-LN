-- =============================================
-- Migratie: 063_UnitPaymentStageReachedProofMedia
-- Datum: 2026-09-30
-- Omschrijving: Bewijs bij "schijf bereikt aanduiden" (21h) is volgens de wireframe een keuze uit de
--   project-Media ("Werffoto's kiezen uit Media" — ProjectPictures), niet een document uit ProjectDocs.
--   Migratie 062 had daarvoor per ongeluk ProofDocId (FK -> ProjectDocs) voorzien; die kolom blijft
--   ongebruikt/orphaned staan (additief-only, geen kolommen wijzigen/verwijderen) en er komt een
--   nieuwe, correcte ProofMediaId (FK -> ProjectPictures) naast. Strikt additief + idempotent.
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.UnitPaymentStageReached') AND name = 'ProofMediaId'
)
BEGIN
    ALTER TABLE [dbo].[UnitPaymentStageReached] ADD [ProofMediaId] INT NULL;
    ALTER TABLE [dbo].[UnitPaymentStageReached]
        ADD CONSTRAINT [FK_UnitPaymentStageReached_ProofMedia] FOREIGN KEY ([ProofMediaId]) REFERENCES [dbo].[ProjectPictures]([Id]);
    PRINT 'Kolom ProofMediaId toegevoegd aan UnitPaymentStageReached.';
END
ELSE
    PRINT 'Kolom ProofMediaId bestaat al, overgeslagen.';

PRINT 'Migratie 063_UnitPaymentStageReachedProofMedia voltooid.';
