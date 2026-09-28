-- =============================================
-- Migratie: 057_KlantenaccountVoornaamEnFacturatie
-- Datum: 2026-09-28
-- Omschrijving: Klantenaccounts (design-handoff 23 "Klant toevoegen — aanvulling op 12c/12d") — herwerkte
--   Projecten/AddClientAccount + Klanten/EditProject (project-blad). Twee dingen:
--   1) Voornaam op het ACCOUNT zelf (ClientAccount) — ClientContacts (mede-eigenaars) heeft al Name+
--      Forename gesplitst, het account/de hoofdeigenaar had enkel het vrije-tekstveld Name. Puur additief:
--      bestaande rijen behouden hun ene Name-waarde ongewijzigd, ClientAccountBO.DisplayName valt terug op
--      Name alleen zolang Forename leeg is.
--   2) Facturatie-/ondertekenvoorkeuren + een verzendlijst — enkel BEWAARD, nog niet uitgevoerd: de
--      eigenlijke facturatie-generatie (ServiceCore/InvoiceCommandService.cs) en de ondertekenregel
--      (ServiceCore/Signing/SigningRuleEvaluator.cs) blijven ongewijzigd. DefaultSigningRule stuurt enkel
--      het ondertekenaarsvoorstel in ChangeOrderSigningSource; NULL = huidig gedrag (altijd "Alle eigenaars").
--   Strikt additief + idempotent (opnieuw uitvoeren is veilig).
-- =============================================

------------------------------------------------------------
-- 1. ClientAccount uitbreiden
------------------------------------------------------------
IF COL_LENGTH('dbo.ClientAccount', 'Forename') IS NULL
    ALTER TABLE [dbo].[ClientAccount] ADD [Forename] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.ClientAccount', 'InvoicingMode') IS NULL
    ALTER TABLE [dbo].[ClientAccount] ADD [InvoicingMode] TINYINT NOT NULL CONSTRAINT [DF_ClientAccount_InvoicingMode] DEFAULT 0;
IF COL_LENGTH('dbo.ClientAccount', 'BilledToType') IS NULL
    ALTER TABLE [dbo].[ClientAccount] ADD [BilledToType] TINYINT NULL;
IF COL_LENGTH('dbo.ClientAccount', 'BilledToClientContactId') IS NULL
    ALTER TABLE [dbo].[ClientAccount] ADD [BilledToClientContactId] INT NULL;
IF COL_LENGTH('dbo.ClientAccount', 'DefaultSigningRule') IS NULL
    ALTER TABLE [dbo].[ClientAccount] ADD [DefaultSigningRule] TINYINT NULL;
IF COL_LENGTH('dbo.ClientAccount', 'PortalInviteRequested') IS NULL
    ALTER TABLE [dbo].[ClientAccount] ADD [PortalInviteRequested] BIT NOT NULL CONSTRAINT [DF_ClientAccount_PortalInviteRequested] DEFAULT 0;
PRINT 'ClientAccount uitgebreid (Forename, InvoicingMode, BilledToType, BilledToClientContactId, DefaultSigningRule, PortalInviteRequested).';
GO

-- ON DELETE NO ACTION (impliciet, geen clausule) i.p.v. SET NULL: ClientContacts.ClientAccountId →
-- ClientAccount cascade't al (FK_ClientContacts_ClientAccount), dus een terugwijzende SET NULL-pad
-- hiervandaan naar ClientContacts geeft SQL Server se Msg 1785 ("multiple cascade paths"). De app
-- ruimt BilledToClientContactId zelf op vóór ze een mede-eigenaar-rij verwijdert (zie
-- ClientAccountTranslator) — dezelfde "app normaliseert, DB blijft simpel"-regel als elders.
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClientAccount_BilledToContact')
    ALTER TABLE [dbo].[ClientAccount] ADD CONSTRAINT [FK_ClientAccount_BilledToContact]
        FOREIGN KEY ([BilledToClientContactId]) REFERENCES [dbo].[ClientContacts]([Id]) ON DELETE NO ACTION;
PRINT 'FK_ClientAccount_BilledToContact klaar.';
GO

------------------------------------------------------------
-- 2. ClientAccountInvoiceRecipient ("Verzenden naar")
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ClientAccountInvoiceRecipient]'))
BEGIN
    CREATE TABLE [dbo].[ClientAccountInvoiceRecipient] (
        [Id]              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ClientAccountId] INT            NOT NULL,
        [ClientContactId] INT            NULL,
        [Email]           NVARCHAR(254)  NULL,
        [DisplayName]     NVARCHAR(150)  NULL,
        [SortOrder]       INT            NOT NULL DEFAULT 0,
        [CreatedDate]     DATETIME2(7)   NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_ClientAccountInvoiceRecipient_ClientAccount]
            FOREIGN KEY ([ClientAccountId]) REFERENCES [dbo].[ClientAccount]([Id]) ON DELETE CASCADE,
        -- ON DELETE NO ACTION (impliciet) i.p.v. SET NULL — zelfde reden als FK_ClientAccount_
        -- BilledToContact hierboven: ClientContacts cascade't al vanaf ClientAccount, dus een tweede
        -- cascade-pad hiervandaan zou opnieuw Msg 1785 geven.
        CONSTRAINT [FK_ClientAccountInvoiceRecipient_ClientContact]
            FOREIGN KEY ([ClientContactId]) REFERENCES [dbo].[ClientContacts]([Id]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_ClientAccountInvoiceRecipient_ClientAccountId] ON [dbo].[ClientAccountInvoiceRecipient] ([ClientAccountId]);
    PRINT 'Tabel ClientAccountInvoiceRecipient aangemaakt.';
END
ELSE
    PRINT 'Tabel ClientAccountInvoiceRecipient bestaat al, overgeslagen.';
GO

PRINT 'Migratie 057_KlantenaccountVoornaamEnFacturatie voltooid.';
