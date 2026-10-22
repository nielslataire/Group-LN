-- =============================================
-- Migratie: 113_CrmContactMakelaarsPostcode
-- Datum: 2026-10-22
-- Omschrijving: Contacten (punt 44), vervolg op 105.
--   ProjectBroker                : de makelaars (bedrijven uit Leveranciers) die aan een project gekoppeld zijn (tabblad Partners van Project bewerken).
--   CrmContact.BrokerCompanyId   : de makelaar als echt bedrijf (bron Makelaar), zodat er later commissies aan gekoppeld kunnen worden. BrokerName blijft als weergavenaam.
--   CrmContact.AssignedCompanyId : toegewezen aan een makelaar (bedrijf) i.p.v. een interne medewerker (AssignedUserId).
--   CrmContact.PostcodeId        : gekozen gemeente/postcode (zelfde tabel als bij Klanten), zodat een contact later naar een klant overgezet kan worden.
--   Strikt additief + idempotent.
-- =============================================
IF OBJECT_ID('dbo.ProjectBroker', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectBroker] (
        [ProjectId] INT NOT NULL,
        [CompanyId] INT NOT NULL,
        CONSTRAINT [PK_ProjectBroker] PRIMARY KEY ([ProjectId], [CompanyId]),
        CONSTRAINT [FK_ProjectBroker_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectBroker_Company] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[CompanyInfo]([CompanyId])
    );
    PRINT 'Tabel ProjectBroker aangemaakt.';
END
ELSE PRINT 'Tabel ProjectBroker bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.CrmContact', 'BrokerCompanyId') IS NULL
BEGIN
    ALTER TABLE [dbo].[CrmContact] ADD [BrokerCompanyId] INT NULL;
    ALTER TABLE [dbo].[CrmContact] ADD CONSTRAINT [FK_CrmContact_BrokerCompany] FOREIGN KEY ([BrokerCompanyId]) REFERENCES [dbo].[CompanyInfo]([CompanyId]);
    PRINT 'Kolom CrmContact.BrokerCompanyId toegevoegd.';
END
ELSE PRINT 'Kolom CrmContact.BrokerCompanyId bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.CrmContact', 'AssignedCompanyId') IS NULL
BEGIN
    ALTER TABLE [dbo].[CrmContact] ADD [AssignedCompanyId] INT NULL;
    ALTER TABLE [dbo].[CrmContact] ADD CONSTRAINT [FK_CrmContact_AssignedCompany] FOREIGN KEY ([AssignedCompanyId]) REFERENCES [dbo].[CompanyInfo]([CompanyId]);
    PRINT 'Kolom CrmContact.AssignedCompanyId toegevoegd.';
END
ELSE PRINT 'Kolom CrmContact.AssignedCompanyId bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.CrmContact', 'PostcodeId') IS NULL
BEGIN
    ALTER TABLE [dbo].[CrmContact] ADD [PostcodeId] INT NULL;
    PRINT 'Kolom CrmContact.PostcodeId toegevoegd.';
END
ELSE PRINT 'Kolom CrmContact.PostcodeId bestaat al, overgeslagen.';
GO
-- Website-formulieren die als notitie waren overgenomen (de website bewaart de pagina-link in Origin): voortaan een automatisch formulier-bericht met die link.
UPDATE [dbo].[CrmContactEvent] SET [Kind] = N'form', [IsAuto] = 1
WHERE [Kind] = N'note' AND [LegacyRequestId] IS NOT NULL AND [UserName] LIKE N'http%';
PRINT 'Website-formulieren met een pagina-link omgezet naar formulier-berichten.';
GO
