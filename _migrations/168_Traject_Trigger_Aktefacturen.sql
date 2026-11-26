-- =============================================
-- 168 (26/11/2026): trajecttrigger "Aktefacturen klaarzetten" op de mijlpaal "Notariële akte verleden" (code NOTARIELE_AKTE).
--   TriggerEvent 2 = XDagenVoorDoeldatum (1 dag), TriggerActie 2 = MaakTaak.
--   De taak komt bij de verantwoordelijke van de mijlpaal in Mijn taken; vervaldatum = dezelfde dag (offsetDagen 0).
-- Toegevoegd aan: alle trajectsjablonen met die mijlpaal, en de bestaande trajecten van projecten (kopie met SjabloonTriggerId).
-- Zonder datum van de akte (doeldatum) vuurt de trigger niet. Idempotent: bestaat de trigger al, dan gebeurt er niets.
-- Aandachtspunt: bij een verkaveling heeft elke kavel een eigen aktedatum; of de doeldatum van de mijlpaal die per kavel volgt, hangt af van het traject (zie TRAJECT_VOORTGANG).
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @titel NVARCHAR(200) = N'Aktefacturen klaarzetten';
DECLARE @json NVARCHAR(MAX) = N'{"titel":"Aktefacturen klaarzetten","omschrijving":"De akte is morgen. Zet op de pagina Facturatie van het project de aktefacturen (grond, kosten en werken) klaar met de knop Aktefacturen klaarzetten.","offsetDagen":0}';

INSERT INTO dbo.TrajectSjabloonMijlpaalTrigger (TrajectSjabloonMijlpaalId, TriggerEvent, TriggerActie, OffsetDagen, ActieParametersJson, MagProjectWijzigen, IsActief, Omschrijving)
SELECT m.Id, 2, 2, 1, @json, 0, 1, N'Een dag vóór de akte: taak om de aktefacturen klaar te zetten.'
FROM dbo.TrajectSjabloonMijlpaal m
WHERE m.Code = N'NOTARIELE_AKTE'
  AND NOT EXISTS (SELECT 1 FROM dbo.TrajectSjabloonMijlpaalTrigger t WHERE t.TrajectSjabloonMijlpaalId = m.Id AND t.TriggerEvent = 2 AND t.TriggerActie = 2 AND t.ActieParametersJson LIKE N'%Aktefacturen klaarzetten%');
PRINT N'Sjabloontriggers toegevoegd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

INSERT INTO dbo.MijlpaalTrigger (MijlpaalId, SjabloonTriggerId, TriggerEvent, TriggerActie, OffsetDagen, ActieParametersJson, MagProjectWijzigen, IsActief)
SELECT mp.Id, st.Id, 2, 2, 1, @json, 0, 1
FROM dbo.Mijlpaal mp
JOIN dbo.TrajectSjabloonMijlpaalTrigger st ON st.TrajectSjabloonMijlpaalId = mp.SjabloonMijlpaalId AND st.TriggerEvent = 2 AND st.TriggerActie = 2 AND st.ActieParametersJson LIKE N'%Aktefacturen klaarzetten%'
WHERE mp.Code = N'NOTARIELE_AKTE'
  AND NOT EXISTS (SELECT 1 FROM dbo.MijlpaalTrigger x WHERE x.MijlpaalId = mp.Id AND x.TriggerEvent = 2 AND x.TriggerActie = 2 AND x.ActieParametersJson LIKE N'%Aktefacturen klaarzetten%');
PRINT N'Triggers op bestaande trajecten toegevoegd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

COMMIT;
PRINT N'KLAAR.';
