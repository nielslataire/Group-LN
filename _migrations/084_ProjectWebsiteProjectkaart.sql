-- =============================================
-- Migratie: 084_ProjectWebsiteProjectkaart
-- Datum: 2026-10-09
-- Omschrijving: Interactieve projectkaart op de publieke projectpagina (blok "De woningen"): een gekozen foto
--   (luchtfoto/render) met per eenheid een omtrek (polygoon in % van de foto).
--   ProjectWebsite.AerialImageName : bestandsnaam van de gekozen projectfoto (map "pictures").
--   ProjectWebsiteLot              : per eenheid de omtrek ("x,y x,y x,y" in %, 0-100) + labelpositie.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectWebsite') AND name = 'AerialImageName')
BEGIN
    ALTER TABLE [dbo].[ProjectWebsite] ADD [AerialImageName] NVARCHAR(260) NULL;
    PRINT 'Kolom AerialImageName toegevoegd aan ProjectWebsite.';
END
ELSE PRINT 'Kolom AerialImageName bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectWebsiteLot', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectWebsiteLot] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [ProjectId]  INT            NOT NULL,
        [UnitId]     INT            NOT NULL,
        [Polygon]    NVARCHAR(1000) NOT NULL,
        [LabelX]     DECIMAL(6,2)   NOT NULL,
        [LabelY]     DECIMAL(6,2)   NOT NULL,
        CONSTRAINT PK_ProjectWebsiteLot PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_ProjectWebsiteLot_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE,
        CONSTRAINT FK_ProjectWebsiteLot_Unit FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id]),
        CONSTRAINT UQ_ProjectWebsiteLot_Unit UNIQUE ([UnitId])
    );
    PRINT 'Tabel ProjectWebsiteLot aangemaakt.';
END
ELSE PRINT 'Tabel ProjectWebsiteLot bestaat al, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProjectWebsiteLot_Project' AND object_id = OBJECT_ID('dbo.ProjectWebsiteLot'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProjectWebsiteLot_Project] ON [dbo].[ProjectWebsiteLot]([ProjectId]) INCLUDE ([UnitId], [Polygon], [LabelX], [LabelY]);
    PRINT 'Index IX_ProjectWebsiteLot_Project aangemaakt.';
END
ELSE PRINT 'Index IX_ProjectWebsiteLot_Project bestaat al, overgeslagen.';
GO
