-- =============================================
-- Migratie: 058_KlantenaccountTelefoon
-- Datum: 2026-09-28
-- Omschrijving: ClientAccount (eigenaar 1/het account zelf) had enkel Email — elke mede-eigenaar en
--   contactpersoon (ClientContacts) heeft al Phone/Cellphone. Gat gevonden bij een analyse van hoe
--   personen/contactgegevens rond een klantenaccount samenhangen (Klanten/AddClientAccountV2 +
--   EditProjectV2). Zelfde kolomlengte (NVARCHAR(50)) als ClientContacts.Phone/Cellphone.
--   Strikt additief + idempotent (opnieuw uitvoeren is veilig).
-- =============================================

IF COL_LENGTH('dbo.ClientAccount', 'Phone') IS NULL
    ALTER TABLE [dbo].[ClientAccount] ADD [Phone] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.ClientAccount', 'Cellphone') IS NULL
    ALTER TABLE [dbo].[ClientAccount] ADD [Cellphone] NVARCHAR(50) NULL;
PRINT 'ClientAccount uitgebreid (Phone, Cellphone).';
GO

PRINT 'Migratie 058_KlantenaccountTelefoon voltooid.';
