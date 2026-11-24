-- =============================================
-- 158k (24/11/2026): trajectsjabloon "Appartementsgebouw (projectontwikkeling en bouw)": oplevering van het gebouw.
--   * "Voorlopige oplevering gebouw": streefdatum = UITVOERING_DEADLINE (aanvangsdatum bouwwerken + uitvoeringstermijn in werkdagen);
--     bereikt zodra Project.DeliveryDate (werkelijke datum voorlopige oplevering) ingevuld is (ProjectDatum, binding 2).
--   * "Definitieve oplevering gebouw": bereikt zodra Project.DeliveryDateDef (werkelijke datum) ingevuld is.
--   Past het sjabloon aan (elke naam die begint met "Appartementsgebouw (projectontwikkeling en bouw)") en de reeds aangemaakte
--   mijlpalen van projecttrajecten op dat sjabloon die nog niet bereikt zijn. Hun streefdatum rekent CPM zelf opnieuw uit.
--   Vereist 158i. Idempotent.
-- =============================================
SET NOCOUNT ON;

;WITH sm AS (
    SELECT m.Id, m.Code
    FROM dbo.TrajectSjabloonMijlpaal m
    JOIN dbo.TrajectSjabloonFase f ON f.Id = m.TrajectSjabloonFaseId
    JOIN dbo.TrajectSjabloon s ON s.Id = f.TrajectSjabloonId
    WHERE s.Naam LIKE N'Appartementsgebouw (projectontwikkeling en bouw)%' AND m.Code IN (N'VOORLOPIGE_OPLEVERING', N'DEFINITIEVE_OPLEVERING_GEBOUW'))
UPDATE m SET
    m.DoeldatumAnkerCode = CASE WHEN m.Code = N'VOORLOPIGE_OPLEVERING' THEN N'UITVOERING_DEADLINE' ELSE m.DoeldatumAnkerCode END,
    m.DoeldatumOffsetDagen = CASE WHEN m.Code = N'VOORLOPIGE_OPLEVERING' THEN 0 ELSE m.DoeldatumOffsetDagen END,
    m.BronBinding = 2,
    m.BronParam = CASE WHEN m.Code = N'VOORLOPIGE_OPLEVERING' THEN N'DeliveryDate' ELSE N'DeliveryDateDef' END
FROM dbo.TrajectSjabloonMijlpaal m JOIN sm ON sm.Id = m.Id;
PRINT CONCAT(N'Sjabloonmijlpalen aangepast: ', @@ROWCOUNT);

-- Reeds aangemaakte mijlpalen (enkel op projectniveau, enkel nog niet bereikt; Status 2 = bereikt)
UPDATE mp SET
    mp.BronBinding = 2,
    mp.BronParam = CASE WHEN mp.Code = N'VOORLOPIGE_OPLEVERING' THEN N'DeliveryDate' ELSE N'DeliveryDateDef' END,
    mp.BronRefId = NULL,
    mp.DoeldatumBerekend = NULL
FROM dbo.Mijlpaal mp
JOIN dbo.TrajectSjabloonMijlpaal sm ON sm.Id = mp.SjabloonMijlpaalId
JOIN dbo.TrajectSjabloonFase f ON f.Id = sm.TrajectSjabloonFaseId
JOIN dbo.TrajectSjabloon s ON s.Id = f.TrajectSjabloonId
WHERE s.Naam LIKE N'Appartementsgebouw (projectontwikkeling en bouw)%'
  AND sm.Code IN (N'VOORLOPIGE_OPLEVERING', N'DEFINITIEVE_OPLEVERING_GEBOUW')
  AND mp.UnitId IS NULL AND mp.Status <> 2;
PRINT CONCAT(N'Mijlpalen in projecten aangepast: ', @@ROWCOUNT);

SELECT s.Naam AS Sjabloon, m.Code, m.DoeldatumAnkerCode AS Anker, m.DoeldatumOffsetDagen AS Dagen, m.BronBinding, m.BronParam
FROM dbo.TrajectSjabloonMijlpaal m
JOIN dbo.TrajectSjabloonFase f ON f.Id = m.TrajectSjabloonFaseId
JOIN dbo.TrajectSjabloon s ON s.Id = f.TrajectSjabloonId
WHERE s.Naam LIKE N'Appartementsgebouw (projectontwikkeling en bouw)%' AND m.Code IN (N'VOORLOPIGE_OPLEVERING', N'DEFINITIEVE_OPLEVERING_GEBOUW');
