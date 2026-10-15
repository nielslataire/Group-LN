-- =============================================
-- Migratie: 094_BudgetParamsGrondAankopen
-- Datum: 2026-10-13
-- Omschrijving: Budgetwizard stap 7, kaart "Grondkost": de aankoop van de grond kan uit meerdere delen (akten) bestaan, elk met eigen notariskosten.
--   BudgetParams.GrondAankopenJson : JSON-lijst [{"o":"omschrijving","p":prijs}]; leeg = één aankoop ter waarde van AankoopprijsGrond.
--                                    AankoopprijsGrond blijft het totaal van de delen (alle bestaande berekeningen blijven werken).
--   BudgetParams.AktekostenPerAkte : bijkomende notariskosten per akte (vacaties, kadaster, uitgaven); NULL = standaard 1.500 EUR.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetParams') AND name = 'GrondAankopenJson')
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [GrondAankopenJson] NVARCHAR(MAX) NULL;
    PRINT 'Kolom GrondAankopenJson toegevoegd aan BudgetParams.';
END
ELSE PRINT 'Kolom GrondAankopenJson bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetParams') AND name = 'AktekostenPerAkte')
BEGIN
    ALTER TABLE [dbo].[BudgetParams] ADD [AktekostenPerAkte] DECIMAL(18,2) NULL;
    PRINT 'Kolom AktekostenPerAkte toegevoegd aan BudgetParams.';
END
ELSE PRINT 'Kolom AktekostenPerAkte bestaat al, overgeslagen.';
GO
