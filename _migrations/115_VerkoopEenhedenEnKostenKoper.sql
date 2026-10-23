-- =============================================
-- Migratie: 115_VerkoopEenhedenEnKostenKoper
-- Datum: 2026-10-23
-- Omschrijving: Verkoop (design-handoff 45a/45b), pagina Te koop en Verkoopinstellingen.
--   Units.OptionUntil  : tot wanneer een eenheid in optie staat (NULL = geen einddatum).
--   Units.OptionHolder : naam van de optant (nog geen koper/klantenaccount).
--   Units.SaleVia      : makelaar of herkomst van de optie of verkoop, vrije tekst (bv. "Immo Lataire").
--   Units.OnlineSince  : sinds wanneer de eenheid op de website staat (NULL = niet online gezet).
--   ProjectSalesExtraCost : eigen kostenposten bij de kosten koper van een project ("Kost toevoegen"), naast de vaste posten van ProjectSalesSettings.
--   Strikt additief + idempotent.
-- =============================================
IF COL_LENGTH('dbo.Units', 'OptionUntil') IS NULL
BEGIN
    ALTER TABLE [dbo].[Units] ADD [OptionUntil] DATE NULL;
    PRINT 'Kolom Units.OptionUntil toegevoegd.';
END
ELSE PRINT 'Kolom Units.OptionUntil bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.Units', 'OptionHolder') IS NULL
BEGIN
    ALTER TABLE [dbo].[Units] ADD [OptionHolder] NVARCHAR(150) NULL;
    PRINT 'Kolom Units.OptionHolder toegevoegd.';
END
ELSE PRINT 'Kolom Units.OptionHolder bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.Units', 'SaleVia') IS NULL
BEGIN
    ALTER TABLE [dbo].[Units] ADD [SaleVia] NVARCHAR(150) NULL;
    PRINT 'Kolom Units.SaleVia toegevoegd.';
END
ELSE PRINT 'Kolom Units.SaleVia bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.Units', 'OnlineSince') IS NULL
BEGIN
    ALTER TABLE [dbo].[Units] ADD [OnlineSince] DATE NULL;
    PRINT 'Kolom Units.OnlineSince toegevoegd.';
END
ELSE PRINT 'Kolom Units.OnlineSince bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ProjectSalesExtraCost', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProjectSalesExtraCost] (
        [Id]        INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ProjectSalesExtraCost] PRIMARY KEY,
        [ProjectId] INT NOT NULL,
        [Name]      NVARCHAR(120) NOT NULL,
        [Amount]    DECIMAL(18,2) NOT NULL CONSTRAINT [DF_ProjectSalesExtraCost_Amount] DEFAULT (0),
        [Note]      NVARCHAR(200) NULL,
        [SortOrder] INT NOT NULL CONSTRAINT [DF_ProjectSalesExtraCost_Sort] DEFAULT (0),
        CONSTRAINT [FK_ProjectSalesExtraCost_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectId]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ProjectSalesExtraCost_Project] ON [dbo].[ProjectSalesExtraCost] ([ProjectId], [SortOrder]);
    PRINT 'Tabel ProjectSalesExtraCost aangemaakt.';
END
ELSE PRINT 'Tabel ProjectSalesExtraCost bestaat al, overgeslagen.';
GO
