-- =============================================
-- Migratie: 091_BudgetAlgemeenFormules
-- Datum: 2026-10-13
-- Omschrijving: EPB, veiligheidscoordinatie en ventilatieverslaggeving worden activiteiten met een formule
--   (aantal woon-/commerciele eenheden x prijs uit Instellingen > Kostprijsmaterialen) in plaats van een percentage/forfait op de
--   Parameters-pagina van het budget. De activiteiten bestaan al (ActivityID's door Niels opgegeven):
--     166 EPB                     : @aantal_wooncomm * @mat_algemeen_epb
--     165 Veiligheidscoordinatie  : @aantal_wooncomm * @mat_algemeen_vc
--     278 Ventilatieverslaggeving : @aantal_wooncomm * @mat_algemeen_ventilatieverslaggeving
--   Een formule wordt enkel toegevoegd als de activiteit er nog geen heeft; ontbreekt een activiteit, dan wordt dat gemeld en overgeslagen.
--   Vereist: de koppelingen algemeen_epb, algemeen_vc en algemeen_ventilatieverslaggeving (Instellingen > Kostprijsmaterialen > Formule koppelingen).
--   Idempotent: een tweede run past niets meer aan.
-- =============================================
SET NOCOUNT ON;

DECLARE @def TABLE (ActivityId INT, Naam NVARCHAR(250), Sleutel NVARCHAR(100));
INSERT INTO @def (ActivityId, Naam, Sleutel) VALUES
    (166, N'EPB',                     N'algemeen_epb'),
    (165, N'Veiligheidscoördinatie',  N'algemeen_vc'),
    (278, N'Ventilatieverslaggeving', N'algemeen_ventilatieverslaggeving');

DECLARE @act INT, @naam NVARCHAR(250), @sleutel NVARCHAR(100);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT ActivityId, Naam, Sleutel FROM @def;
OPEN c;
FETCH NEXT FROM c INTO @act, @naam, @sleutel;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Activity WHERE ActivityID = @act)
        PRINT 'Activiteit ' + CAST(@act AS NVARCHAR(20)) + ' (' + @naam + ') bestaat niet: overgeslagen.';
    ELSE IF EXISTS (SELECT 1 FROM dbo.BudgetActivityFormule WHERE ActivityId = @act)
        PRINT 'Formule bestond al voor ' + @naam + ' (ActivityID ' + CAST(@act AS NVARCHAR(20)) + '), overgeslagen.';
    ELSE
    BEGIN
        INSERT INTO dbo.BudgetActivityFormule (ActivityId, Formule, Omschrijving, Actief, LaatstGewijzigd)
        VALUES (@act, N'@aantal_wooncomm * @mat_' + @sleutel, @naam, 1, SYSUTCDATETIME());
        PRINT 'Formule toegevoegd voor ' + @naam + ' (ActivityID ' + CAST(@act AS NVARCHAR(20)) + ').';
    END
    FETCH NEXT FROM c INTO @act, @naam, @sleutel;
END
CLOSE c; DEALLOCATE c;
GO
