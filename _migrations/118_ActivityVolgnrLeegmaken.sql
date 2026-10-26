-- =============================================
-- Migratie: 118_ActivityVolgnrLeegmaken
-- Datum: 2026-10-24
-- Omschrijving: Migratie 117 vulde Activity.Volgnr voor alle bestaande activiteiten (1, 2, 3 … alfabetisch per groep). Dat was niet de bedoeling: de
--   nummers vul je zelf aan op de pagina Activiteiten. Dit script zet ze terug op NULL voor wie 117 al draaide (de huidige versie van 117 vult niet meer).
--   LET OP: dit wist ALLE volgnummers, ook nummers die je intussen zelf hebt ingevuld. Heb je er al ingevuld, draai het dan niet of beperk het tot de rijen die
--   nog het automatische nummer hebben.
--   Idempotent: tweemaal draaien doet niets extra.
-- =============================================
IF COL_LENGTH('dbo.Activity', 'Volgnr') IS NOT NULL
BEGIN
    UPDATE [dbo].[Activity] SET [Volgnr] = NULL WHERE [Volgnr] IS NOT NULL;
    PRINT 'Volgnummers van activiteiten leeggemaakt.';
END
ELSE PRINT 'Kolom Activity.Volgnr bestaat niet, niets te doen.';
GO
