-- =============================================
-- Migratie: 138_InhoudingsplichtLijstnaam
-- Datum: 2026-11-09
-- Omschrijving: Een controlelijst inhoudingsplicht (BatchId) krijgt een naam ("Betaalronde november - Keerstraat"), zodat eerdere lijsten
--   terug te vinden zijn en opnieuw geselecteerd kunnen worden. De naam staat op elke rij van de lijst. De bewijsgegevens (referentie, trace-id,
--   ruw RSZ-antwoord, tijdstip, wie, omgeving) blijven ongewijzigd. Strikt additief + idempotent. Draai dit VOOR de nieuwe versie van de applicatie opstart.
-- =============================================
IF COL_LENGTH('dbo.SupplierRetainmentCheck', 'BatchName') IS NULL
    ALTER TABLE [dbo].[SupplierRetainmentCheck] ADD [BatchName] NVARCHAR(200) NULL;
GO
