-- =============================================
-- 165 (25/11/2026): twee projectsjablonen voor een verkaveling met woningen (startpunt).
--   "Verkaveling met wegenis"   = planning "Verkaveling wegenis" in de hoofdplanning + planning "Woning" per woning + schijvensjabloon "Woning" + traject.
--                                 De woningen starten standaard na de wegverharding (WEGENIS_VERHARDING); per project aan te passen in de planning.
--   "Verkaveling zonder wegenis" = planning "Woning" per woning + schijvensjabloon "Woning" + traject.
--   TRAJECT: tijdelijk de bestaande trajectsjablonen "Verkaveling met wegenis" en "Verkaveling zonder wegenis" (migratie 041). Die worden nog herwerkt
--   zoals het appartementstraject (158i); koppel dan het nieuwe trajectsjabloon in Instellingen > Sjablonen > Projectsjablonen.
--   Standaard: 2 bouwlagen (gelijkvloers + 1 verdieping), zonder kelder, plat dak.
--   Vereist 156, 160, 161, 162, 163 en 164. Ontbreekt een planning- of schijvensjabloon, dan gebeurt er niets (en staat dat in de melding).
--   Idempotent: bestaat een projectsjabloon met die naam al, dan wordt het overgeslagen.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @wegenis INT = (SELECT TOP 1 Id FROM dbo.PlanningSjabloon WHERE Naam = N'Verkaveling wegenis' ORDER BY Id);
DECLARE @woning  INT = (SELECT TOP 1 Id FROM dbo.PlanningSjabloon WHERE Naam = N'Woning' ORDER BY Id);
DECLARE @schijf  INT = (SELECT TOP 1 Id FROM dbo.SchijfSjabloon  WHERE Naam = N'Woning' ORDER BY Id);
DECLARE @tMet    INT = (SELECT TOP 1 Id FROM dbo.TrajectSjabloon WHERE Naam LIKE N'Verkaveling met wegenis%' ORDER BY Id);
DECLARE @tZonder INT = (SELECT TOP 1 Id FROM dbo.TrajectSjabloon WHERE Naam LIKE N'Verkaveling zonder wegenis%' ORDER BY Id);

IF @wegenis IS NULL OR @woning IS NULL OR @schijf IS NULL
BEGIN
    PRINT N'NIETS GEDAAN: er ontbreekt een sjabloon. Planning wegenis: ' + ISNULL(CAST(@wegenis AS NVARCHAR(20)), N'ONTBREEKT (162)')
        + N' · Planning woning: ' + ISNULL(CAST(@woning AS NVARCHAR(20)), N'ONTBREEKT (162)')
        + N' · Schijven woning: ' + ISNULL(CAST(@schijf AS NVARCHAR(20)), N'ONTBREEKT (163)');
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.ProjectSjabloon WHERE Naam = N'Verkaveling met wegenis')
    INSERT INTO dbo.ProjectSjabloon (Naam, Beschrijving, PlanningSjabloonId, PlanningSjabloonProjectId, WoningenStartNa, TrajectSjabloonId, SchijfSjabloonId, StandaardVerdiepingen, StandaardKelder, StandaardDak, IsActief, CreatedDate)
    VALUES (N'Verkaveling met wegenis', N'Verkaveling met woningen en aanleg van de wegenis: wegenisplanning in de hoofdplanning, planning en schijven per woning.',
            @woning, @wegenis, N'WEGENIS_VERHARDING', @tMet, @schijf, 2, 0, N'plat', 1, SYSUTCDATETIME());
ELSE PRINT N'"Verkaveling met wegenis" bestaat al: overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM dbo.ProjectSjabloon WHERE Naam = N'Verkaveling zonder wegenis')
    INSERT INTO dbo.ProjectSjabloon (Naam, Beschrijving, PlanningSjabloonId, PlanningSjabloonProjectId, WoningenStartNa, TrajectSjabloonId, SchijfSjabloonId, StandaardVerdiepingen, StandaardKelder, StandaardDak, IsActief, CreatedDate)
    VALUES (N'Verkaveling zonder wegenis', N'Verkaveling met woningen zonder wegenis: planning en schijven per woning.',
            @woning, NULL, NULL, @tZonder, @schijf, 2, 0, N'plat', 1, SYSUTCDATETIME());
ELSE PRINT N'"Verkaveling zonder wegenis" bestaat al: overgeslagen.';

SELECT j.Id, j.Naam, pr.Naam AS PlanningProject, pw.Naam AS PlanningWoning, t.Naam AS Traject, s.Naam AS Schijven, j.WoningenStartNa
FROM dbo.ProjectSjabloon j
LEFT JOIN dbo.PlanningSjabloon pr ON pr.Id = j.PlanningSjabloonProjectId
LEFT JOIN dbo.PlanningSjabloon pw ON pw.Id = j.PlanningSjabloonId
LEFT JOIN dbo.TrajectSjabloon t ON t.Id = j.TrajectSjabloonId
LEFT JOIN dbo.SchijfSjabloon s ON s.Id = j.SchijfSjabloonId
WHERE j.Naam IN (N'Verkaveling met wegenis', N'Verkaveling zonder wegenis');
IF @tMet IS NULL OR @tZonder IS NULL PRINT N'LET OP: een bestaand trajectsjabloon "Verkaveling met/zonder wegenis" ontbreekt: koppel er zelf een in de editor.';
PRINT N'KLAAR. Open Instellingen > Sjablonen > Projectsjablonen.';
