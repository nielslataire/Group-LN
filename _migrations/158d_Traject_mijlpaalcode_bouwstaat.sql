-- =============================================
-- 158d (24/11/2026): de trajectmijlpaal "Leidingen technieken geplaatst" (code LEIDINGEN) krijgt de code van de bouwstaat TECHNIEKEN_RUW.
--   Sinds de taak LEIDINGEN gesplitst is (158c) bestaat er geen taak met die code meer. Een mijlpaal met de code van een bouwstaat
--   volgt de taak die de bouwstaat als laatste oplevert (PlanningTrajectKoppeling). Beide taken leveren TECHNIEKEN_RUW.
--   Raakt: trajectsjablonen (TrajectSjabloonMijlpaal) en reeds aangemaakte mijlpalen (Mijlpaal), enkel waar de code LEIDINGEN is
--   en er in het project geen taak of fase met die code meer bestaat. Idempotent.
-- =============================================

UPDATE m SET m.Code = N'TECHNIEKEN_RUW'
FROM [dbo].[TrajectSjabloonMijlpaal] m
WHERE m.Code = N'LEIDINGEN';

UPDATE m SET m.Code = N'TECHNIEKEN_RUW', m.BronBinding = NULL, m.BronRefId = NULL
FROM [dbo].[Mijlpaal] m
JOIN [dbo].[Projecttraject] t ON t.Id = m.ProjecttrajectId
WHERE m.Code = N'LEIDINGEN'
  AND NOT EXISTS (SELECT 1 FROM [dbo].[PlanningTaak] pt JOIN [dbo].[PlanningSectie] ps ON ps.Id = pt.PlanningSectieId WHERE ps.ProjectId = t.ProjectId AND pt.Code = N'LEIDINGEN')
  AND (m.BronBinding IS NULL OR m.BronBinding IN (0, 4, 5));
GO

SELECT 'Trajectmijlpalen met code TECHNIEKEN_RUW' AS Soort, COUNT(*) AS Aantal FROM [dbo].[TrajectSjabloonMijlpaal] WHERE Code = N'TECHNIEKEN_RUW'
UNION ALL
SELECT 'Mijlpalen in projecten met code TECHNIEKEN_RUW', COUNT(*) FROM [dbo].[Mijlpaal] WHERE Code = N'TECHNIEKEN_RUW';
GO
