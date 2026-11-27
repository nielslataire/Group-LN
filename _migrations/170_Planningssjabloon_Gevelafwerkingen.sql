-- =============================================
-- 170 (27/11/2026): gevelafwerkingen als projectkeuze in de planningssjablonen "Appartementsgebouw" en "Woning".
--   Drie optionele taken in het blok "Gevel en buitenschrijnwerk", elk met de taakvoorwaarde gevel:<keuze>:
--     Gevelbepleistering          (gevel:pleister)    na het gevelmetselwerk
--     Houten gevelbekleding       (gevel:hout)        na het gevelmetselwerk en (indien gekozen) de gevelbepleistering
--     Aluminium gevelbekleding    (gevel:aluminium)   na het gevelmetselwerk en (indien gekozen) de gevelbepleistering
--   De stenen gevel (Gevelmetselwerk) bestaat al en komt altijd eerst. Een voorganger die niet gekozen is, valt bij het toepassen vanzelf weg.
--   De opkuis wacht ook op de gekozen gevelafwerkingen, zodat ze nooit na de oplevering eindigen.
--   Bouwstaten GEVELBEPLEISTERING, GEVELBEKLEDING_HOUT en GEVELBEKLEDING_ALU bestaan al (158e). Duren zijn schattingen.
--   Activiteiten (door Niels opgegeven): pleister 216, aluminium 213, hout 212. Bestaat een id niet in dbo.Activity, dan blijft de activiteit leeg.
-- Idempotent: bestaat een taak met die code al in het sjabloon, dan wordt ze overgeslagen. Een sjabloon dat niet bestaat wordt overgeslagen.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @sj TABLE (Id INT, Naam NVARCHAR(200), Blok INT, Vol INT);
INSERT INTO @sj (Id, Naam)
SELECT MIN(Id), Naam FROM dbo.PlanningSjabloon WHERE Naam IN (N'Appartementsgebouw', N'Woning') GROUP BY Naam;
UPDATE x SET Blok = (SELECT TOP 1 b.Id FROM dbo.PlanningSjabloonBlok b WHERE b.SjabloonId = x.Id AND b.Code = N'B_GEVEL')
FROM @sj x;
UPDATE x SET Vol = ISNULL((SELECT MAX(t.Volgorde) FROM dbo.PlanningSjabloonTaak t WHERE t.BlokId = x.Blok), 0) FROM @sj x;
SELECT N'Sjabloon' AS Controle, Naam, Id, Blok AS BlokB_GEVEL FROM @sj;
DELETE FROM @sj WHERE Blok IS NULL;   -- sjabloon zonder blok B_GEVEL: niets te doen

DECLARE @t TABLE (Nr INT IDENTITY(1,1), Code NVARCHAR(60), Naam NVARCHAR(200), Duur INT, Voorwaarde NVARCHAR(100), Bouwstaat NVARCHAR(60), Activiteit INT);
INSERT INTO @t (Code, Naam, Duur, Voorwaarde, Bouwstaat, Activiteit) VALUES
 (N'GEVELBEPLEISTERING',   N'Gevelbepleistering',        20, N'gevel:pleister',   N'GEVELBEPLEISTERING', 216),
 (N'GEVELBEKLEDING_HOUT',  N'Houten gevelbekleding',     15, N'gevel:hout',       N'GEVELBEKLEDING_HOUT', 212),
 (N'GEVELBEKLEDING_ALU',   N'Aluminium gevelbekleding',  15, N'gevel:aluminium',  N'GEVELBEKLEDING_ALU', 213);

INSERT INTO dbo.PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Estafette, EstafetteWacht, Voorwaarde, BouwstaatId, ActivityId)
SELECT s.Blok, t.Code, t.Naam, t.Duur, s.Vol + t.Nr * 10, 0, 0, t.Voorwaarde,
       (SELECT TOP 1 x.Id FROM dbo.Bouwstaat x WHERE x.Code = t.Bouwstaat AND x.ProjectId IS NULL),
       (SELECT a.ActivityId FROM dbo.Activity a WHERE a.ActivityId = t.Activiteit)
FROM @sj s CROSS JOIN @t t
WHERE NOT EXISTS (SELECT 1 FROM dbo.PlanningSjabloonTaak e JOIN dbo.PlanningSjabloonBlok b ON b.Id = e.BlokId WHERE b.SjabloonId = s.Id AND e.Code = t.Code);
PRINT N'Taken toegevoegd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- Bij een tweede run: taken die er al stonden zonder activiteit krijgen ze alsnog (een bestaande activiteit blijft ongemoeid).
UPDATE e SET ActivityId = a.ActivityId
FROM @sj s
JOIN dbo.PlanningSjabloonBlok b ON b.SjabloonId = s.Id
JOIN dbo.PlanningSjabloonTaak e ON e.BlokId = b.Id
JOIN @t t ON t.Code = e.Code
JOIN dbo.Activity a ON a.ActivityId = t.Activiteit
WHERE e.ActivityId IS NULL;
PRINT N'Activiteit aangevuld bij bestaande taken: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- Voorgangers: taakcode, voorgangercode, type (0 = FS), wachtdagen
DECLARE @v TABLE (Taak NVARCHAR(60), Voorganger NVARCHAR(60), Type TINYINT, Wacht INT);
INSERT INTO @v (Taak, Voorganger, Type, Wacht) VALUES
 (N'GEVELBEPLEISTERING',  N'GEVELMETSELWERK',      0, 0),
 (N'GEVELBEKLEDING_HOUT', N'GEVELMETSELWERK',      0, 0), (N'GEVELBEKLEDING_HOUT', N'GEVELBEPLEISTERING', 0, 0),
 (N'GEVELBEKLEDING_ALU',  N'GEVELMETSELWERK',      0, 0), (N'GEVELBEKLEDING_ALU',  N'GEVELBEPLEISTERING', 0, 0),
 (N'OPKUIS',              N'GEVELBEPLEISTERING',   0, 0), (N'OPKUIS', N'GEVELBEKLEDING_HOUT', 0, 0), (N'OPKUIS', N'GEVELBEKLEDING_ALU', 0, 0);

INSERT INTO dbo.PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen)
SELECT t.Id, v.Voorganger, v.Type, v.Wacht
FROM @sj s
JOIN dbo.PlanningSjabloonBlok b ON b.SjabloonId = s.Id
JOIN dbo.PlanningSjabloonTaak t ON t.BlokId = b.Id
JOIN @v v ON v.Taak = t.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.PlanningSjabloonVoorganger g WHERE g.TaakId = t.Id AND g.VoorgangerCode = v.Voorganger);
PRINT N'Voorgangers toegevoegd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- Controle: bouwstaat gevonden? (verwacht: geen rijen)
SELECT N'Zonder bouwstaat' AS Controle, s.Naam AS Sjabloon, t.Code
FROM @sj s JOIN dbo.PlanningSjabloonBlok b ON b.SjabloonId = s.Id JOIN dbo.PlanningSjabloonTaak t ON t.BlokId = b.Id
WHERE t.Code IN (N'GEVELBEPLEISTERING', N'GEVELBEKLEDING_HOUT', N'GEVELBEKLEDING_ALU') AND t.BouwstaatId IS NULL;

COMMIT;
PRINT N'KLAAR. Pas het projectsjabloon toe en kies de gevelafwerkingen in het venster (blok "Gevelafwerking").';
