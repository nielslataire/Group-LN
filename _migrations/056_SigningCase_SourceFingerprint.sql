-- =============================================
-- Migratie: 056_SigningCase_SourceFingerprint
-- Datum: 2026-09-27
-- Omschrijving: Elektronisch ondertekenen, fase 1. SigningCase.SourceFingerprint — SHA-256 (hex) van
--   de broninhoud op het moment van aanmaken (ISigningDocumentSource.ComputeFingerprintAsync). Bij het
--   ondertekenen wordt ze herberekend; verschilt ze, dan wordt de handtekening geweigerd. Vervangt het
--   oorspronkelijke "gewijzigd sinds tijdstip"-vangnet: ChangeOrder heeft geen wijzigingstijdstip.
--   NULL = de bron leverde geen vingerafdruk (dan geldt enkel de vergrendeling).
--   Idempotent + additief. EF-config: DALCore/Models/cpmRunningContext.Signing.cs.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SigningCase') AND name = 'SourceFingerprint')
BEGIN
    ALTER TABLE [dbo].[SigningCase] ADD [SourceFingerprint] CHAR(64) NULL;
    PRINT 'Kolom SigningCase.SourceFingerprint toegevoegd.';
END
ELSE
    PRINT 'Kolom SigningCase.SourceFingerprint bestaat al, overgeslagen.';
GO
