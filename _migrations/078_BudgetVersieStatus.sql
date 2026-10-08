-- =============================================
-- Migratie: 078_BudgetVersieStatus
-- Datum: 2026-10-08
-- Omschrijving: Budgetflow (design-handoff punt 39). Versiestatus Concept → Afgerond → Definitief:
--   BudgetVersie.VastgezetOp / VastgezetDoor : wanneer en door wie de versie definitief gemaakt werd (één per project, alleen-lezen).
--   BudgetVersie.LaatsteStap                 : hoogste bereikte stap (1-9), voor "Concept · stap 6 van 9" in het overzicht.
--   BudgetVersie.VmswFactoren                : JSON met de 13 reductiefactoren van stap 2; NULL = standaard VMSW (niet aangepast).
--   BudgetVersie.WaarschuwingenBevestigd     : JSON/tekst met bevestigde aandachtspunten (bv. "decennale elders gedekt").
--   Status blijft een tekstkolom: "Concept", "Afgerond", "Definitief". Bestaande rijen blijven "Concept".
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVersie') AND name = 'VastgezetOp')
BEGIN
    ALTER TABLE [dbo].[BudgetVersie] ADD [VastgezetOp] DATETIME2(0) NULL, [VastgezetDoor] NVARCHAR(100) NULL;
    PRINT 'Kolommen VastgezetOp en VastgezetDoor toegevoegd aan BudgetVersie.';
END
ELSE PRINT 'Kolommen VastgezetOp/VastgezetDoor bestaan al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVersie') AND name = 'LaatsteStap')
BEGIN
    ALTER TABLE [dbo].[BudgetVersie] ADD [LaatsteStap] TINYINT NULL;
    PRINT 'Kolom LaatsteStap toegevoegd aan BudgetVersie.';
END
ELSE PRINT 'Kolom LaatsteStap bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVersie') AND name = 'VmswFactoren')
BEGIN
    ALTER TABLE [dbo].[BudgetVersie] ADD [VmswFactoren] NVARCHAR(600) NULL;
    PRINT 'Kolom VmswFactoren toegevoegd aan BudgetVersie.';
END
ELSE PRINT 'Kolom VmswFactoren bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetVersie') AND name = 'WaarschuwingenBevestigd')
BEGIN
    ALTER TABLE [dbo].[BudgetVersie] ADD [WaarschuwingenBevestigd] NVARCHAR(400) NULL;
    PRINT 'Kolom WaarschuwingenBevestigd toegevoegd aan BudgetVersie.';
END
ELSE PRINT 'Kolom WaarschuwingenBevestigd bestaat al, overgeslagen.';
GO
