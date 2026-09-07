-- =============================================
-- Migratie: ProjectAlgemeneGegevens
-- Datum: 2026-09-07
-- Omschrijving:
--   Extra velden voor de "Algemene gegevens"-kaart op Projecten/Detail:
--   - Project.ProjectCode                   : vrij projectkenmerk (bv. "P-2024-118")
--   - Project.SalesResponsibleAspNetUserID  : verkoopverantwoordelijke (ASP.NET-gebruiker,
--                                             zelfde principe als AspNetUserID = projectleider)
--   - Project.NotaryCompanyID               : notaris, gekoppeld aan een CompanyInfo-record
-- =============================================

IF COL_LENGTH('dbo.Project', 'ProjectCode') IS NULL
BEGIN
    ALTER TABLE [dbo].[Project] ADD [ProjectCode] NVARCHAR(50) NULL;
    PRINT 'Kolom Project.ProjectCode toegevoegd.';
END
ELSE
    PRINT 'Kolom Project.ProjectCode bestaat al, overgeslagen.';
GO

IF COL_LENGTH('dbo.Project', 'SalesResponsibleAspNetUserID') IS NULL
BEGIN
    ALTER TABLE [dbo].[Project] ADD [SalesResponsibleAspNetUserID] NVARCHAR(128) NULL;
    PRINT 'Kolom Project.SalesResponsibleAspNetUserID toegevoegd.';
END
ELSE
    PRINT 'Kolom Project.SalesResponsibleAspNetUserID bestaat al, overgeslagen.';
GO

IF COL_LENGTH('dbo.Project', 'NotaryCompanyID') IS NULL
BEGIN
    ALTER TABLE [dbo].[Project] ADD [NotaryCompanyID] INT NULL;
    PRINT 'Kolom Project.NotaryCompanyID toegevoegd.';
END
ELSE
    PRINT 'Kolom Project.NotaryCompanyID bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.FK_Project_SalesResponsibleUser', 'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[Project] WITH CHECK ADD CONSTRAINT [FK_Project_SalesResponsibleUser]
        FOREIGN KEY ([SalesResponsibleAspNetUserID]) REFERENCES [dbo].[Users]([UserID]);
    PRINT 'FK_Project_SalesResponsibleUser toegevoegd.';
END
ELSE
    PRINT 'FK_Project_SalesResponsibleUser bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.FK_Project_NotaryCompany', 'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[Project] WITH CHECK ADD CONSTRAINT [FK_Project_NotaryCompany]
        FOREIGN KEY ([NotaryCompanyID]) REFERENCES [dbo].[CompanyInfo]([CompanyID]);
    PRINT 'FK_Project_NotaryCompany toegevoegd.';
END
ELSE
    PRINT 'FK_Project_NotaryCompany bestaat al, overgeslagen.';
GO
