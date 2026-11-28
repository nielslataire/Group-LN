-- =============================================
-- 176 (28/11/2026): het gekozen projectsjabloon en de antwoorden (bouwlagen, kelder, dak, terrassen, gevelafwerkingen, ...) bewaren op het project (E1).
--   Project.ProjectSjabloonId  : het gekozen projectsjabloon (leeg = nog geen keuze)
--   Project.ProjectSjabloonAntwoorden : de antwoorden als JSON (ProjectSjabloonAntwoorden); aanpasbaar, toepassen blijft een bewuste actie
-- Additief en idempotent.
-- =============================================
SET NOCOUNT ON;
IF COL_LENGTH(N'dbo.Project', N'ProjectSjabloonId') IS NULL
BEGIN
    ALTER TABLE dbo.Project ADD ProjectSjabloonId INT NULL;
    PRINT N'Kolom Project.ProjectSjabloonId toegevoegd.';
END
IF COL_LENGTH(N'dbo.Project', N'ProjectSjabloonAntwoorden') IS NULL
BEGIN
    ALTER TABLE dbo.Project ADD ProjectSjabloonAntwoorden NVARCHAR(MAX) NULL;
    PRINT N'Kolom Project.ProjectSjabloonAntwoorden toegevoegd.';
END
PRINT N'KLAAR.';
