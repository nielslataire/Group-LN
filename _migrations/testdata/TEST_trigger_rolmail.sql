-- =============================================
-- TEST (27/11/2026): trigger "Verwittig rol" naar Boekhouder of bestuurders (A2). Alleen voor een testproject (bv. 85).
-- Draai de stappen één voor één.
--
-- LET OP: de mail gaat echt uit. Met rol 4 (Boekhouder) naar ALLE actieve gebruikers met de rol Boekhouding, met rol 3 (CEO / CFO)
-- naar alle actieve gebruikers met de rol Bestuurder of Admin. Wil je eerst veilig testen, gebruik dan stap 2a met je eigen e-mailadres.
-- Het triggerpad is hetzelfde als bij Aktefacturen: de trigger vuurt als vandaag >= doeldatum van de mijlpaal - 1 dag en de mijlpaal niet bereikt is.
-- Starten kan met de achtergrondtaak (12 min na app-start) of met de url /api/trigger/traject-recalc?key=<sleutel> (status 202 = gestart).
-- Die run evalueert ALLE openstaande triggers van alle projecten, niet enkel de testtrigger.
-- =============================================
SET NOCOUNT ON;
DECLARE @ProjectId INT = 85;
DECLARE @MijlpaalId INT = NULL;   -- <-- invullen: een mijlpaal met Status 0 uit stap 1, bv. 765 (Lot 09)

-- STAP 0: wie zou de mail krijgen? (controleer dit eerst; leeg = er gaat niets uit)
SELECT r.Name AS Rol, u.Id AS UserId, u.Email, u.IsActive
FROM dbo.SecurityUserRole ur
JOIN dbo.SecurityRole r ON r.RoleId = ur.RoleId AND r.IsActive = 1
JOIN dbo.Users u ON u.Id = ur.UserId
WHERE r.Name IN (N'Boekhouding', N'Bestuurder', N'Admin')
ORDER BY r.Name, u.Email;

-- STAP 1: open mijlpalen van het testproject
SELECT mp.Id AS MijlpaalId, mp.Code, mp.Naam, u.Name AS Eenheid, mp.Status, mp.Doeldatum, mp.DoeldatumBerekend, mp.VerantwoordelijkeRol
FROM dbo.Projecttraject tr
JOIN dbo.Mijlpaal mp ON mp.ProjecttrajectId = tr.Id
LEFT JOIN dbo.Units u ON u.Id = mp.UnitId
WHERE tr.ProjectId = @ProjectId AND mp.Status = 0
ORDER BY mp.Code, u.Name;

-- STAP 2a (VEILIG): trigger met je EIGEN e-mailadres. Vul het adres in en haal de commentaar weg.
/*
INSERT INTO dbo.MijlpaalTrigger (MijlpaalId, TriggerEvent, TriggerActie, OffsetDagen, ActieParametersJson, MagProjectWijzigen, IsActief)
VALUES (@MijlpaalId, 2, 0, 1, N'{"email":"JOUW@ADRES.BE","testrolmail":true}', 0, 1);
*/

-- STAP 2b (ECHT): trigger op een rol: 4 = Boekhouder, 3 = CEO / CFO (bestuurders). Haal de commentaar weg.
/*
INSERT INTO dbo.MijlpaalTrigger (MijlpaalId, TriggerEvent, TriggerActie, OffsetDagen, ActieParametersJson, MagProjectWijzigen, IsActief)
VALUES (@MijlpaalId, 2, 0, 1, N'{"rol":4,"testrolmail":true}', 0, 1);
*/

-- STAP 3: zet de doeldatum van die mijlpaal op morgen en start de run (url of wachten)
/*
UPDATE dbo.Mijlpaal SET Doeldatum = CAST(DATEADD(DAY, 1, GETDATE()) AS DATE) WHERE Id = @MijlpaalId AND Status = 0;
SELECT @@ROWCOUNT AS Aangepast;   -- verwacht: 1
*/

-- STAP 4 (na de run): is de trigger gevuurd en naar wie? Resultaat toont de adressen ("E-mail verzonden naar a@x.be, b@x.be (rol: Boekhouder)").
SELECT TOP 10 r.Uitgevoerd, r.MijlpaalId, r.Status, r.Resultaat, r.Fout
FROM dbo.MijlpaalTriggerRun r
JOIN dbo.MijlpaalTrigger t ON t.Id = r.MijlpaalTriggerId AND t.ActieParametersJson LIKE N'%testrolmail%'
ORDER BY r.Uitgevoerd DESC;

-- STAP 5: opruimen (haal de commentaar weg)
/*
DELETE r FROM dbo.MijlpaalTriggerRun r JOIN dbo.MijlpaalTrigger t ON t.Id = r.MijlpaalTriggerId WHERE t.ActieParametersJson LIKE N'%testrolmail%';
DELETE FROM dbo.MijlpaalTrigger WHERE ActieParametersJson LIKE N'%testrolmail%';
UPDATE dbo.Mijlpaal SET Doeldatum = NULL WHERE Id = @MijlpaalId;
*/
