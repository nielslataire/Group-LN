-- =============================================
-- Migratie: 135_Gezondheidsindex
-- Datum: 2026-11-06
-- Omschrijving: Gezondheidsindex (Statbel) voor het indexeren van terugkerende facturen (design-handoff 49).
--   HealthIndexValue   één rij per maand en basisjaar (bv. oktober 2026, basis 2013 = 100). De app haalt de laatste 13 maanden op de
--                      laatste dag van de maand op bij Statbel en bewaart ze: de historiek groeit dus aan, ze wordt niet overschreven.
--   RecurringInvoiceSchedule.IndexationBaseIndex   optionele basisindex (basis 2013 = 100) voor een startdatum die ouder is dan wat bewaard is.
--   Strikt additief + idempotent. Draai dit script VÓÓR de nieuwe versie van de applicatie opstart.
-- =============================================
IF OBJECT_ID('dbo.HealthIndexValue', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[HealthIndexValue]
    (
        [Month]      DATE          NOT NULL,   -- eerste dag van de maand waarop de index slaat
        [BaseYear]   SMALLINT      NOT NULL,   -- 1996, 2004, 2013 of 2025 (= 100)
        [Value]      DECIMAL(10,2) NOT NULL,
        [FetchedAt]  DATETIME2     NOT NULL CONSTRAINT [DF_HealthIndexValue_FetchedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_HealthIndexValue] PRIMARY KEY ([Month], [BaseYear])
    );
    PRINT 'HealthIndexValue aangemaakt.';
END
GO

IF COL_LENGTH('dbo.RecurringInvoiceSchedule', 'IndexationBaseIndex') IS NULL
BEGIN
    ALTER TABLE [dbo].[RecurringInvoiceSchedule] ADD [IndexationBaseIndex] DECIMAL(10,2) NULL;
    PRINT 'RecurringInvoiceSchedule.IndexationBaseIndex toegevoegd.';
END
GO
