-- =============================================
-- Migratie: Dashboard type per gebruiker
-- Datum: 2026-03-30
-- Omschrijving: Voegt DashboardType kolom toe
--               aan de Users tabel.
--               0 = Geen, 1 = Boekhouding,
--               2 = CeoCfo, 3 = Projectleider
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]')
      AND name = N'DashboardType'
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [DashboardType] INT NULL;

    PRINT 'Kolom DashboardType toegevoegd aan tabel Users.';
END
ELSE
    PRINT 'Kolom DashboardType bestaat al, overgeslagen.';
