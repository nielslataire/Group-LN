-- =============================================
-- TEST (27/11/2026): trigger "Aktefacturen klaarzetten" (migratie 168) controleren zonder de rol boekhouding.
-- Alleen voor een testproject (bv. 85). Draai de stappen één voor één.
--
-- Hoe het werkt: de trigger vuurt als vandaag >= doeldatum van de mijlpaal NOTARIELE_AKTE - 1 dag en de mijlpaal nog niet bereikt is.
-- De doeldatum volgt normaal de aktedatum van de klant (ClientAccount.DateDeedOfSale); voor de test zetten we de Doeldatum van de mijlpaal zelf. De trigger wordt NIET bij het openen van een pagina
-- geëvalueerd maar door de achtergrondtaak (TrajectHostedService): 12 minuten na het starten van de app en daarna elke 24 uur.
-- De achtergrondtaak herrekent eerst het traject en evalueert dan de triggers. De taak komt bij de ROL boekhouder (ToegewezenAanRol = 4),
-- tenzij de mijlpaal een vaste gebruiker heeft; je ziet ze dus hier in de tabel en niet in jouw Mijn taken.
-- =============================================
SET NOCOUNT ON;
DECLARE @ProjectId INT = 85;

-- STAP 1: bestaat de trigger op de akte-mijlpalen van het project? (verwacht: een rij per lot, IsActief = 1, LaatstGevuurdOp = NULL)
SELECT mp.Id AS MijlpaalId, u.Name AS Eenheid, mp.Status, mp.Doeldatum, mp.DoeldatumBerekend, mp.VerantwoordelijkeRol,
       t.Id AS TriggerId, t.IsActief, t.OffsetDagen, t.LaatstGevuurdOp
FROM dbo.Projecttraject tr
JOIN dbo.Mijlpaal mp ON mp.ProjecttrajectId = tr.Id AND mp.Code = N'NOTARIELE_AKTE'
LEFT JOIN dbo.Units u ON u.Id = mp.UnitId
LEFT JOIN dbo.MijlpaalTrigger t ON t.MijlpaalId = mp.Id AND t.TriggerActie = 2 AND t.ActieParametersJson LIKE N'%Aktefacturen klaarzetten%'
WHERE tr.ProjectId = @ProjectId
ORDER BY u.Name;

-- STAP 2: zet de doeldatum van de akte-mijlpaal van een OPEN lot op morgen. Gebruik een lot met Status 0 (nog te doen) uit stap 1: bij Status 2 (bereikt)
-- vuurt de trigger nooit, en een lot zonder klant heeft geen ClientAccount om een aktedatum op te zetten. Een handmatige Doeldatum gaat voor op de berekende
-- datum en wordt door het herberekenen niet overschreven. Daarna: de app (her)starten en minstens 12 minuten wachten, of wachten tot de dagelijkse run.
DECLARE @MijlpaalId INT = NULL;   -- <-- invullen, bv. 765 (Lot 09)
/*
UPDATE dbo.Mijlpaal SET Doeldatum = CAST(DATEADD(DAY, 1, GETDATE()) AS DATE)
WHERE Id = @MijlpaalId AND Code = N'NOTARIELE_AKTE' AND Status = 0;
SELECT @@ROWCOUNT AS Aangepast;   -- verwacht: 1
*/

-- STAP 3 (na de run): is de trigger gevuurd en is er een taak? (verwacht: Status 0 = geslaagd in de run, en een ProjectTaak met Titel "Aktefacturen klaarzetten")
SELECT TOP 20 r.Uitgevoerd, r.MijlpaalId, r.Status, r.Resultaat, r.Fout
FROM dbo.MijlpaalTriggerRun r
JOIN dbo.Mijlpaal mp ON mp.Id = r.MijlpaalId
JOIN dbo.Projecttraject tr ON tr.Id = mp.ProjecttrajectId AND tr.ProjectId = @ProjectId
JOIN dbo.MijlpaalTrigger t ON t.Id = r.MijlpaalTriggerId AND t.ActieParametersJson LIKE N'%Aktefacturen klaarzetten%'
ORDER BY r.Uitgevoerd DESC;

SELECT pt.Id, pt.Titel, pt.Status, pt.Vervaldatum, pt.ToegewezenAanRol, pt.ToegewezenAanUserId, pt.MijlpaalId, pt.CreatedDate
FROM dbo.ProjectTaak pt
WHERE pt.ProjectId = @ProjectId AND pt.Titel = N'Aktefacturen klaarzetten'
ORDER BY pt.CreatedDate DESC;

-- STAP 4: opruimen en eventueel opnieuw testen (haal de commentaar weg en vul de oude aktedatum in)
/*
UPDATE dbo.Mijlpaal SET Doeldatum = NULL WHERE Id = @MijlpaalId;

DELETE FROM dbo.ProjectTaak WHERE ProjectId = @ProjectId AND Titel = N'Aktefacturen klaarzetten';

UPDATE t SET t.LaatstGevuurdOp = NULL
FROM dbo.MijlpaalTrigger t
JOIN dbo.Mijlpaal mp ON mp.Id = t.MijlpaalId
JOIN dbo.Projecttraject tr ON tr.Id = mp.ProjecttrajectId AND tr.ProjectId = @ProjectId
WHERE t.ActieParametersJson LIKE N'%Aktefacturen klaarzetten%';
*/
