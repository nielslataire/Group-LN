-- =============================================
-- Migratie: 077_BudgetReferentiesBeheer
-- Datum: 2026-10-08
-- Omschrijving: Beheerschermen Instellingen › Prijsreferenties verkoop en Referentieprojecten (design-handoff punt 38).
--   BudgetPrijsReferentie.Gearchiveerd       : codes die in een budget gebruikt zijn, kan je niet verwijderen — wel archiveren.
--   BudgetReferentieProjectLijn.ActivityId   : wordt NULL-baar; niet-gematchte Excel-regels tellen mee in het totaal, niet per
--                                              activiteit (38d). ExcelNaam/Match bewaren hoe de regel binnenkwam.
--                                              De unieke sleutel (ReferentieProjectId, ActivityId) wordt een gefilterde index.
--   BudgetReferentieProject.CreatedBy        : "Excel · Niels, 02/10/2026" in de lijst.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetPrijsReferentie') AND name = 'Gearchiveerd')
BEGIN
    ALTER TABLE [dbo].[BudgetPrijsReferentie] ADD [Gearchiveerd] BIT NOT NULL CONSTRAINT DF_BudgetPrijsRef_Gearchiveerd DEFAULT 0;
    PRINT 'Kolom Gearchiveerd toegevoegd aan BudgetPrijsReferentie.';
END
ELSE PRINT 'Kolom Gearchiveerd bestaat al, overgeslagen.';
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetReferentieProjectLijn') AND name = 'ActivityId' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_BudgetRefProjectLijn_Activiteit')
        ALTER TABLE [dbo].[BudgetReferentieProjectLijn] DROP CONSTRAINT [UQ_BudgetRefProjectLijn_Activiteit];
    ALTER TABLE [dbo].[BudgetReferentieProjectLijn] ALTER COLUMN [ActivityId] INT NULL;
    PRINT 'BudgetReferentieProjectLijn.ActivityId is nu NULL-baar.';
END
ELSE PRINT 'BudgetReferentieProjectLijn.ActivityId was al NULL-baar, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BudgetRefProjectLijn_Activiteit' AND object_id = OBJECT_ID('dbo.BudgetReferentieProjectLijn'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_BudgetRefProjectLijn_Activiteit]
        ON [dbo].[BudgetReferentieProjectLijn]([ReferentieProjectId], [ActivityId]) WHERE [ActivityId] IS NOT NULL;
    PRINT 'Gefilterde unieke index UX_BudgetRefProjectLijn_Activiteit aangemaakt.';
END
ELSE PRINT 'Index UX_BudgetRefProjectLijn_Activiteit bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetReferentieProjectLijn') AND name = 'ExcelNaam')
BEGIN
    ALTER TABLE [dbo].[BudgetReferentieProjectLijn] ADD [ExcelNaam] NVARCHAR(200) NULL, [Match] NVARCHAR(10) NULL;
    PRINT 'Kolommen ExcelNaam en Match toegevoegd aan BudgetReferentieProjectLijn.';
END
ELSE PRINT 'Kolommen ExcelNaam/Match bestaan al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetReferentieProject') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE [dbo].[BudgetReferentieProject] ADD [CreatedBy] NVARCHAR(100) NULL;
    PRINT 'Kolom CreatedBy toegevoegd aan BudgetReferentieProject.';
END
ELSE PRINT 'Kolom CreatedBy bestaat al, overgeslagen.';
GO
