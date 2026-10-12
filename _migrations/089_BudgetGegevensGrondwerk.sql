-- =============================================
-- Migratie: 089_BudgetGegevensGrondwerk
-- Datum: 2026-10-12
-- Omschrijving: Budgetwizard stap 1 - grondwerk automatisch berekend en herleidbare funderingsoppervlakte.
--   BudgetGegevens.FunderingLijnen      : JSON-lijst [{omschrijving, aantal, oppervlakte}] waaruit "Oppervlakte funderingen" de som is
--                                         (zodat later nog te zien is hoe het getal tot stand kwam). NULL/[] = het getal werd rechtstreeks ingevuld.
--   BudgetGegevens.DiepteFunderingCm    : diepte van de gewone fundering in cm (NULL = standaard 40).
--   BudgetGegevens.DiepteOndergrondsCm  : diepte per ondergrondse verdieping in cm (NULL = standaard 300).
--   BudgetGegevens.OppOndergronds       : totale oppervlakte van alle ondergrondse verdiepingen in m2 (NULL = aantal ondergrondse verdiepingen x oppervlakte funderingen).
--   BudgetGegevens.OndergrondsLijnen    : JSON-lijst [{omschrijving, aantal, oppervlakte}] waaruit OppOndergronds de som is (herleidbaar).
--   M3Grondwerk = oppervlakte funderingen x diepte fundering / 100 + oppervlakte ondergrondse verdiepingen x diepte per verdieping / 100.
--   Strikt additief + idempotent.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetGegevens') AND name = 'FunderingLijnen')
BEGIN
    ALTER TABLE [dbo].[BudgetGegevens] ADD [FunderingLijnen] NVARCHAR(MAX) NULL;
    PRINT 'Kolom FunderingLijnen toegevoegd aan BudgetGegevens.';
END
ELSE PRINT 'Kolom FunderingLijnen bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetGegevens') AND name = 'DiepteFunderingCm')
BEGIN
    ALTER TABLE [dbo].[BudgetGegevens] ADD [DiepteFunderingCm] DECIMAL(8,2) NULL;
    PRINT 'Kolom DiepteFunderingCm toegevoegd aan BudgetGegevens.';
END
ELSE PRINT 'Kolom DiepteFunderingCm bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetGegevens') AND name = 'DiepteOndergrondsCm')
BEGIN
    ALTER TABLE [dbo].[BudgetGegevens] ADD [DiepteOndergrondsCm] DECIMAL(8,2) NULL;
    PRINT 'Kolom DiepteOndergrondsCm toegevoegd aan BudgetGegevens.';
END
ELSE PRINT 'Kolom DiepteOndergrondsCm bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetGegevens') AND name = 'OppOndergronds')
BEGIN
    ALTER TABLE [dbo].[BudgetGegevens] ADD [OppOndergronds] DECIMAL(10,2) NULL;
    PRINT 'Kolom OppOndergronds toegevoegd aan BudgetGegevens.';
END
ELSE PRINT 'Kolom OppOndergronds bestaat al, overgeslagen.';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BudgetGegevens') AND name = 'OndergrondsLijnen')
BEGIN
    ALTER TABLE [dbo].[BudgetGegevens] ADD [OndergrondsLijnen] NVARCHAR(MAX) NULL;
    PRINT 'Kolom OndergrondsLijnen toegevoegd aan BudgetGegevens.';
END
ELSE PRINT 'Kolom OndergrondsLijnen bestaat al, overgeslagen.';
GO
