-- Migratie 006: ContactId toevoegen aan UserCompanyAccess
-- Koppelt een portaalgebruiker aan de specifieke contactpersoon van het bedrijf.
-- Beperkte toegang = enkel contracten waarvoor dit contact als SiteManager staat.

ALTER TABLE [dbo].[UserCompanyAccess]
    ADD [ContactId] INT NULL
        CONSTRAINT [FK_UserCompanyAccess_CompanyContacts]
        FOREIGN KEY REFERENCES [dbo].[CompanyContacts]([ContactId])
        ON DELETE SET NULL;
