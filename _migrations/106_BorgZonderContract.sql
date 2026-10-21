-- =============================================
-- Migratie: 106_BorgZonderContract
-- Datum: 2026-10-21
-- Omschrijving: Nacalculatie, Borgen (design-handoff 42i/42h): ook borg op facturen zonder contract kan vrijgegeven worden.
--   ContractBorgVrijgave.ContractId wordt nullable; nieuwe kolommen CompanyId + ProjectId voor een vrijgave van de borg
--   die op facturen zonder contract van één leverancier is ingehouden. Een rij hoort bij een contract OF bij een leverancier in een project.
--   Strikt additief + idempotent. Draai dit NA 105.
-- =============================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContractBorgVrijgave') AND name = 'ContractId' AND is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[ContractBorgVrijgave] ALTER COLUMN [ContractId] INT NULL;
    PRINT 'ContractBorgVrijgave.ContractId is nu nullable.';
END
ELSE PRINT 'ContractBorgVrijgave.ContractId was al nullable, overgeslagen.';
GO

IF COL_LENGTH('dbo.ContractBorgVrijgave', 'CompanyId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContractBorgVrijgave] ADD [CompanyId] INT NULL;
    PRINT 'Kolom ContractBorgVrijgave.CompanyId toegevoegd.';
END
ELSE PRINT 'Kolom ContractBorgVrijgave.CompanyId bestaat al, overgeslagen.';
GO

IF COL_LENGTH('dbo.ContractBorgVrijgave', 'ProjectId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContractBorgVrijgave] ADD [ProjectId] INT NULL;
    PRINT 'Kolom ContractBorgVrijgave.ProjectId toegevoegd.';
END
ELSE PRINT 'Kolom ContractBorgVrijgave.ProjectId bestaat al, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ContractBorgVrijgave_Eigenaar')
BEGIN
    ALTER TABLE [dbo].[ContractBorgVrijgave] ADD CONSTRAINT [CK_ContractBorgVrijgave_Eigenaar]
        CHECK ([ContractId] IS NOT NULL OR ([CompanyId] IS NOT NULL AND [ProjectId] IS NOT NULL));
    PRINT 'Controle CK_ContractBorgVrijgave_Eigenaar toegevoegd.';
END
ELSE PRINT 'Controle CK_ContractBorgVrijgave_Eigenaar bestaat al, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ContractBorgVrijgave_Project_Company' AND object_id = OBJECT_ID('dbo.ContractBorgVrijgave'))
BEGIN
    CREATE INDEX [IX_ContractBorgVrijgave_Project_Company] ON [dbo].[ContractBorgVrijgave] ([ProjectId], [CompanyId]);
    PRINT 'Index IX_ContractBorgVrijgave_Project_Company aangemaakt.';
END
GO
