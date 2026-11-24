-- =============================================
-- 158j (24/11/2026): projectsjabloon "Appartementsgebouw": bundelt het planningssjabloon "Appartementsgebouw" (158f),
--   het trajectsjabloon "Appartementsgebouw (projectontwikkeling en bouw)" (158i; ook als je het hernoemd hebt, bv. met " - DEF") en het schijvensjabloon "Appartementsgebouw" (158g).
--   Standaardgebouw (enkel een voorstel, bij het toepassen kan je het per project aanpassen): 4 bouwlagen, met kelder, plat dak.
--   Vereist 156 (tabel ProjectSjabloon), 158f, 158g en 158i. Ontbreekt er één van de drie sjablonen, dan gebeurt er niets (en staat dat in de melding).
--   Idempotent: bestaat het projectsjabloon al, dan gebeurt er niets.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @naam NVARCHAR(200) = N'Appartementsgebouw';
IF EXISTS (SELECT 1 FROM dbo.ProjectSjabloon WHERE Naam = @naam)
BEGIN PRINT N'Het projectsjabloon "' + @naam + N'" bestaat al: niets gedaan.'; RETURN; END

DECLARE @p INT = (SELECT TOP 1 Id FROM dbo.PlanningSjabloon WHERE Naam = N'Appartementsgebouw' ORDER BY Id);
DECLARE @t INT = (SELECT TOP 1 Id FROM dbo.TrajectSjabloon WHERE Naam LIKE N'Appartementsgebouw (projectontwikkeling en bouw)%' ORDER BY Id);
DECLARE @s INT = (SELECT TOP 1 Id FROM dbo.SchijfSjabloon WHERE Naam = N'Appartementsgebouw' ORDER BY Id);

IF @p IS NULL OR @t IS NULL OR @s IS NULL
BEGIN
    PRINT N'NIETS GEDAAN: er ontbreekt een sjabloon. Planning: ' + ISNULL(CAST(@p AS NVARCHAR(20)), N'ONTBREEKT (158f)')
        + N' · Traject: ' + ISNULL(CAST(@t AS NVARCHAR(20)), N'ONTBREEKT (158i)')
        + N' · Schijven: ' + ISNULL(CAST(@s AS NVARCHAR(20)), N'ONTBREEKT (158g)');
    RETURN;
END

INSERT INTO dbo.ProjectSjabloon (Naam, Beschrijving, PlanningSjabloonId, TrajectSjabloonId, SchijfSjabloonId, StandaardVerdiepingen, StandaardKelder, StandaardDak, IsActief, CreatedDate)
VALUES (@naam, N'Appartementsgebouw zonder verkavelingsvergunning: planning, traject (projectontwikkelaar en projectleider) en betalingsschijven in één keer.',
        @p, @t, @s, 4, 1, N'plat', 1, SYSUTCDATETIME());

SELECT j.Id, j.Naam, p.Naam AS Planning, t.Naam AS Traject, s.Naam AS Schijven, j.StandaardVerdiepingen, j.StandaardKelder, j.StandaardDak
FROM dbo.ProjectSjabloon j
LEFT JOIN dbo.PlanningSjabloon p ON p.Id = j.PlanningSjabloonId
LEFT JOIN dbo.TrajectSjabloon t ON t.Id = j.TrajectSjabloonId
LEFT JOIN dbo.SchijfSjabloon s ON s.Id = j.SchijfSjabloonId
WHERE j.Naam = @naam;
PRINT N'KLAAR: projectsjabloon aangemaakt. Open Instellingen > Sjablonen > Projectsjablonen om het te bekijken.';
