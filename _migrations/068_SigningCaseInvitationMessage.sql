-- =============================================
-- Migratie: 068_SigningCaseInvitationMessage
-- Datum: 2026-10-02
-- Omschrijving: Elektronisch ondertekenen — een vrij bericht van de afzender in de uitnodigingsmail
--   (design-handoff 21d "Verzenden ter ondertekening", veld BERICHT). Het bericht hoort bij het
--   dossier: het wordt bij het aanmaken bewaard en gaat mee in de uitnodiging én in elke herinnering
--   van dat dossier. NULL = geen bericht (alle bestaande dossiers; de mail blijft dan zoals hij was).
--   Strikt additief + idempotent.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SigningCase') AND name = 'InvitationMessage')
BEGIN
    ALTER TABLE [dbo].[SigningCase] ADD [InvitationMessage] NVARCHAR(2000) NULL;
    PRINT 'Kolom InvitationMessage toegevoegd aan SigningCase.';
END
ELSE PRINT 'Kolom InvitationMessage bestaat al, overgeslagen.';
