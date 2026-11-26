-- =============================================
-- 167 (26/11/2026): planningssjabloon "Woning": estafette aanzetten.
-- In 162 stond bij geen enkele taak de estafettevlag. Gevolg: alle woningen (lots) startten tegelijk en liepen even lang.
-- Nu wacht elke vakmanstaak in een woning op dezelfde taak in de vorige woning (ploegen schuiven door van lot naar lot, wachttijd 0 dagen).
-- Zonder estafette blijven: werf inrichten, uitzetten door landmeter, opkuis en voorlopige oplevering (per woning onafhankelijk).
-- Past enkel het SJABLOON aan. Bestaande projecten (zoals 83) moeten hun woningplanning opnieuw laten toepassen (planning leegmaken, sjabloon opnieuw toepassen).
-- Idempotent.
-- =============================================
SET NOCOUNT ON;
DECLARE @s INT = (SELECT TOP 1 Id FROM dbo.PlanningSjabloon WHERE Naam = N'Woning' ORDER BY Id DESC);
IF @s IS NULL BEGIN PRINT N'Planningssjabloon "Woning" niet gevonden: niets gedaan.'; RETURN; END

UPDATE t SET Estafette = 1, EstafetteWacht = 0
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId
WHERE b.SjabloonId = @s AND t.Estafette = 0
  AND t.Code NOT IN (N'WERF_INRICHTEN', N'LANDMETER_UITZETTEN', N'OPKUIS', N'VOORLOPIGE_OPLEVERING');
PRINT N'Taken met estafette gezet: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

SELECT b.Code AS Blok, t.Code AS Taak, t.Duur, t.Estafette
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId
WHERE b.SjabloonId = @s ORDER BY b.Volgorde, t.Volgorde;
