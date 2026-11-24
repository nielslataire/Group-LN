-- =============================================
-- 158f (24/11/2026): startpunt voor het planningssjabloon "Appartementsgebouw", uit de doorloop met Niels.
--   Vereist 158 en 158e (bouwstaten) en de activiteiten uit je database (op naam opgezocht). Wat niet gevonden wordt, komt in de uitvoer.
--   Opbouw: Voorbereiding > Onderbouw > Bovenbouw (per bouwlaag een deelfase) > Dak > Gevel > Technieken > Afwerking > Omgevingswerken > Oplevering.
--   Duren zijn SCHATTINGEN voor het hele gebouw (werkdagen), niet gevalideerd; pas ze aan in de editor. Later moet AI ze aanpassen aan het aantal eenheden,
--   bouwlagen, de grootte van gebouw, kelder en omgeving (niet voor dit sjabloon).
--   NIET opgenomen: de optionele gevelafwerkingen (gevelbepleistering, houten en aluminium gevelbekleding). Zonder keuze per project zouden ze altijd
--   in elke planning staan. Ze komen er bij zodra de projectkeuze "gevelafwerking" bestaat; de bouwstaten bestaan al.
--   Idempotent: bestaat een sjabloon met deze naam al, dan gebeurt er niets. Verwijder of hernoem het eerst als je opnieuw wilt beginnen.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @naam NVARCHAR(200) = N'Appartementsgebouw';
IF EXISTS (SELECT 1 FROM dbo.PlanningSjabloon WHERE Naam = @naam)
BEGIN PRINT N'Het sjabloon "' + @naam + N'" bestaat al: niets gedaan.'; RETURN; END

BEGIN TRAN;

INSERT INTO dbo.PlanningSjabloon (Naam, Beschrijving, IsActief, CreatedDate)
VALUES (@naam, N'Appartementsgebouw met kelder, bouwlagen en plat of hellend dak. Duren zijn schattingen voor het hele gebouw.', 1, SYSUTCDATETIME());
DECLARE @s INT = SCOPE_IDENTITY();

-- Blokken (Herhaling 2 = één fase met per bouwlaag een deelfase)
DECLARE @blok TABLE (Nr INT IDENTITY(1,1), Code NVARCHAR(60), Naam NVARCHAR(200), Herhaling TINYINT);
INSERT INTO @blok (Code, Naam, Herhaling) VALUES
 (N'B_VOORBEREIDING', N'Voorbereiding', 0), (N'B_ONDERBOUW', N'Onderbouw', 0), (N'B_BOVENBOUW', N'Bovenbouw', 2), (N'B_DAK', N'Dak', 0),
 (N'B_GEVEL', N'Gevel en buitenschrijnwerk', 0), (N'B_TECHNIEKEN', N'Technieken', 0), (N'B_AFWERKING', N'Afwerking', 0),
 (N'B_OMGEVING', N'Omgevingswerken', 0), (N'B_OPLEVERING', N'Oplevering', 0);
INSERT INTO dbo.PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping)
SELECT @s, Code, Naam, Nr * 10, Herhaling, 0 FROM @blok;

-- Taken: blok, code, naam, duur, voorwaarde, bouwstaatcode, activiteit (naam in Activity.Omschrijving)
DECLARE @t TABLE (Nr INT IDENTITY(1,1), Blok NVARCHAR(60), Code NVARCHAR(60), Naam NVARCHAR(200), Duur INT, Voorwaarde NVARCHAR(100), Bouwstaat NVARCHAR(60), Activiteit NVARCHAR(250));
INSERT INTO @t (Blok, Code, Naam, Duur, Voorwaarde, Bouwstaat, Activiteit) VALUES
 (N'B_VOORBEREIDING', N'AFBRAAK',              N'Afbraak bestaande gebouwen',            10, NULL, N'VOORBEREIDING_KLAAR', N'Afbraakwerken'),
 (N'B_VOORBEREIDING', N'LANDMETER_UITZETTEN',  N'Uitzetten door landmeter',               2, NULL, N'WERF_UITGEZET',       N'Landmeter'),
 (N'B_VOORBEREIDING', N'WERF_INRICHTEN',       N'Werf inrichten',                         5, NULL, N'WERF_UITGEZET',       N'Werfinrichting'),
 (N'B_ONDERBOUW',     N'UITGRAVEN_KELDER',     N'Uitgraven kelder',                      15, N'kelder:ja', N'KELDER_UITGEGRAVEN', N'Grondwerken'),
 (N'B_ONDERBOUW',     N'FUNDERINGEN',          N'Funderingen',                           15, NULL, N'FUNDERING_KLAAR',    N'Ruwbouw'),
 (N'B_ONDERBOUW',     N'RIOLERING',            N'Rioleringen',                           10, NULL, N'RIOLERING',          N'Grondwerken'),
 (N'B_ONDERBOUW',     N'VLOERPLAAT_KELDER',    N'Gieten vloerplaat kelder',               5, N'kelder:ja', N'VLOERPLAAT_KELDER', N'Ondergrondse kelder'),
 (N'B_ONDERBOUW',     N'WANDEN_KELDER',        N'Kelderwanden',                          20, N'kelder:ja', N'WANDEN_KELDER',   N'Ondergrondse kelder'),
 (N'B_BOVENBOUW',     N'VLOERPLAAT_GLVL',      N'Vloerplaat',                             5, N'grond:ja',  N'VLOERPLAAT_GLVL', N'Ruwbouw'),
 (N'B_BOVENBOUW',     N'PREDALLEN',            N'Predallen',                              4, N'grond:nee', N'PREDALLEN',       N'Ruwbouw'),
 (N'B_BOVENBOUW',     N'OPSTORT',              N'Opstort',                                3, N'grond:nee', N'OPSTORT',         N'Ruwbouw'),
 (N'B_BOVENBOUW',     N'METSELWERK_RUWBOUW',   N'Metselwerk',                            20, NULL, N'METSELWERK_RUWBOUW', N'Ruwbouw'),
 (N'B_BOVENBOUW',     N'GIPSBLOKKEN',          N'Gipsblokken',                           10, NULL, N'GIPSBLOKKEN',        N'Gipsblokken - plaatsen'),
 (N'B_BOVENBOUW',     N'TERRAS_LUIFEL',        N'Terrassen en luifels',                   5, N'terras:ja', N'TERRAS_LUIFEL',   N'Ruwbouw'),
 (N'B_BOVENBOUW',     N'DORPELS',              N'Dorpels',                                8, N'laag:eenmalig', N'DORPELS',    N'Ruwbouw'),
 (N'B_BOVENBOUW',     N'GEVELMETSELWERK',      N'Gevelmetselwerk',                       40, N'laag:eenmalig', N'GEVELMETSELWERK', N'Ruwbouw'),
 (N'B_BOVENBOUW',     N'PREDALLEN_DAK',        N'Predallen dak',                          4, N'laag:eenmalig,dak:plat', N'DAKPLAAT', N'Ruwbouw'),
 (N'B_BOVENBOUW',     N'OPSTORT_DAK',          N'Opstort dak',                            3, N'laag:eenmalig,dak:plat', N'DAKPLAAT', N'Ruwbouw'),
 (N'B_BOVENBOUW',     N'HELLINGSSCHAPE',       N'Hellingsschape plat dak',                5, N'laag:eenmalig,dak:plat', N'HELLINGSSCHAPE', N'Hellingsbeton'),
 (N'B_DAK',           N'DAKOPSTANDEN',         N'Dak- en liftopstanden',                  5, N'dak:plat',    N'DAKOPSTANDEN',  N'Dakbedekking - Plat dak - EPDM'),
 (N'B_DAK',           N'DAKBEDEKKING_PLAT',    N'Platte dakbedekking',                   15, N'dak:plat',    N'DAK_PLAT_DICHT', N'Dakbedekking - Plat dak - EPDM'),
 (N'B_DAK',           N'DAKSTRUCTUUR',         N'Dakstructuur',                          15, N'dak:hellend', NULL,             N'Daktimmerwerken'),
 (N'B_DAK',           N'DAKBEDEKKING_HELLEND', N'Dakbedekking hellend dak',              10, N'dak:hellend', N'DAK_HELLEND_DICHT', N'Dakbedekking - Hellend Dak'),
 (N'B_GEVEL',         N'BUITENSCHRIJNWERK',    N'Plaatsen buitenschrijnwerk en glas',     20, NULL, N'BUITENSCHRIJNWERK',  N'Buitenschrijnwerk - Aluminium'),
 (N'B_TECHNIEKEN',    N'LEIDINGEN_ELEKTRA',    N'Plaatsen leidingen elektriciteit',       25, NULL, N'TECHNIEKEN_RUW',     N'Elektrische installatie'),
 (N'B_TECHNIEKEN',    N'LEIDINGEN_SANITAIR',   N'Plaatsen leidingen sanitair en verwarming', 25, NULL, N'TECHNIEKEN_RUW',  N'Sanitaire installatie'),
 (N'B_TECHNIEKEN',    N'LIFT',                 N'Plaatsen lift',                          15, NULL, NULL,                  N'Liften'),
 (N'B_AFWERKING',     N'PLEISTER',             N'Pleisterwerken',                         25, NULL, N'PLEISTER',           N'Binnenbepleistering'),
 (N'B_AFWERKING',     N'ISOLATIE',             N'Vloerisolatie',                           5, NULL, N'ISOLATIE_VLOER',     N'Vloer - Isolatie - PUR'),
 (N'B_AFWERKING',     N'CHAPE',                N'Plaatsen chape',                          8, NULL, N'CHAPE',              N'Vloer - Dekvloer'),
 (N'B_AFWERKING',     N'TEGELWERK',            N'Vloeren en wandtegels',                  20, NULL, N'TEGELWERK',          N'Vloer - Keramisch'),
 (N'B_AFWERKING',     N'PARKET',               N'Parket',                                 10, NULL, N'VLOEREN_PARKET',     N'Vloer - Hout'),
 (N'B_AFWERKING',     N'BINNENSCHRIJNWERK',    N'Binnenschrijnwerk',                      15, NULL, N'BINNENSCHRIJNWERK',  N'Binnendeuren'),
 (N'B_AFWERKING',     N'KEUKEN',               N'Plaatsen keuken',                        10, NULL, N'KEUKEN',             N'Vast Meubilair - Keukens'),
 (N'B_AFWERKING',     N'SCHILDERWERK',         N'Schilderwerken',                         20, NULL, NULL,                  N'Schilderwerken'),
 (N'B_OMGEVING',      N'OMGEVINGSWERKEN',      N'Omgevingswerken',                        30, NULL, N'OMGEVINGSWERKEN',    N'Buitenverhardingen'),
 (N'B_OPLEVERING',    N'OPKUIS',               N'Opkuis',                                  5, NULL, N'OPKUIS',             N'Schoonmaak'),
 (N'B_OPLEVERING',    N'VOORLOPIGE_OPLEVERING', N'Voorlopige oplevering (sleutels)',      1, NULL, N'VOORLOPIGE_OPLEVERING', NULL);

INSERT INTO dbo.PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Estafette, EstafetteWacht, Voorwaarde, BouwstaatId, ActivityId)
SELECT b.Id, t.Code, t.Naam, t.Duur, t.Nr * 10, 0, 0, t.Voorwaarde,
       (SELECT TOP 1 x.Id FROM dbo.Bouwstaat x WHERE x.Code = t.Bouwstaat AND x.ProjectId IS NULL),
       (SELECT TOP 1 a.ActivityId FROM dbo.Activity a WHERE a.Omschrijving = t.Activiteit ORDER BY a.ActivityId)
FROM @t t JOIN dbo.PlanningSjabloonBlok b ON b.SjabloonId = @s AND b.Code = t.Blok
ORDER BY t.Nr;

-- Voorgangers: taakcode, voorgangercode (taak, blok, VORIGE:taak of EERSTE:taak), type (0 FS, 1 SS, 2 FF, 3 SF), wachtdagen
DECLARE @v TABLE (Taak NVARCHAR(60), Voorganger NVARCHAR(60), Type TINYINT, Wacht INT);
INSERT INTO @v (Taak, Voorganger, Type, Wacht) VALUES
 (N'LANDMETER_UITZETTEN', N'AFBRAAK', 0, 0), (N'WERF_INRICHTEN', N'AFBRAAK', 0, 0),
 (N'UITGRAVEN_KELDER', N'B_VOORBEREIDING', 0, 0),
 (N'FUNDERINGEN', N'B_VOORBEREIDING', 0, 0), (N'FUNDERINGEN', N'UITGRAVEN_KELDER', 0, 0),
 (N'RIOLERING', N'FUNDERINGEN', 0, 0), (N'VLOERPLAAT_KELDER', N'FUNDERINGEN', 0, 0), (N'WANDEN_KELDER', N'VLOERPLAAT_KELDER', 0, 0),
 (N'VLOERPLAAT_GLVL', N'B_ONDERBOUW', 0, 0),
 (N'PREDALLEN', N'B_ONDERBOUW', 0, 0), (N'PREDALLEN', N'VORIGE:METSELWERK_RUWBOUW', 0, 0),
 (N'OPSTORT', N'PREDALLEN', 0, 0),
 (N'METSELWERK_RUWBOUW', N'VLOERPLAAT_GLVL', 0, 0), (N'METSELWERK_RUWBOUW', N'OPSTORT', 0, 0),
 (N'GIPSBLOKKEN', N'METSELWERK_RUWBOUW', 0, 0), (N'TERRAS_LUIFEL', N'METSELWERK_RUWBOUW', 0, 0),
 (N'DORPELS', N'METSELWERK_RUWBOUW', 0, 0),
 (N'GEVELMETSELWERK', N'EERSTE:METSELWERK_RUWBOUW', 0, 0),
 (N'PREDALLEN_DAK', N'METSELWERK_RUWBOUW', 0, 0), (N'OPSTORT_DAK', N'PREDALLEN_DAK', 0, 0), (N'HELLINGSSCHAPE', N'OPSTORT_DAK', 0, 0),
 (N'DAKOPSTANDEN', N'HELLINGSSCHAPE', 0, 0), (N'DAKBEDEKKING_PLAT', N'DAKOPSTANDEN', 0, 0),
 (N'DAKSTRUCTUUR', N'METSELWERK_RUWBOUW', 0, 0), (N'DAKBEDEKKING_HELLEND', N'DAKSTRUCTUUR', 0, 0),
 (N'BUITENSCHRIJNWERK', N'B_DAK', 0, 0),
 (N'LEIDINGEN_ELEKTRA', N'B_DAK', 0, 0), (N'LEIDINGEN_ELEKTRA', N'BUITENSCHRIJNWERK', 0, 0),
 (N'LEIDINGEN_SANITAIR', N'B_DAK', 0, 0), (N'LEIDINGEN_SANITAIR', N'BUITENSCHRIJNWERK', 0, 0),
 (N'LIFT', N'B_DAK', 0, 0),
 (N'PLEISTER', N'LEIDINGEN_ELEKTRA', 0, 0), (N'PLEISTER', N'LEIDINGEN_SANITAIR', 0, 0), (N'PLEISTER', N'BUITENSCHRIJNWERK', 0, 0),
 (N'ISOLATIE', N'PLEISTER', 0, 0), (N'CHAPE', N'ISOLATIE', 0, 0), (N'TEGELWERK', N'CHAPE', 0, 0), (N'PARKET', N'TEGELWERK', 0, 0),
 (N'BINNENSCHRIJNWERK', N'PARKET', 0, 0), (N'KEUKEN', N'PARKET', 0, 0),
 (N'SCHILDERWERK', N'BINNENSCHRIJNWERK', 0, 0), (N'SCHILDERWERK', N'KEUKEN', 0, 0), (N'SCHILDERWERK', N'LIFT', 0, 0),
 (N'OMGEVINGSWERKEN', N'B_AFWERKING', 0, 0),
 (N'OPKUIS', N'B_OMGEVING', 0, 0), (N'VOORLOPIGE_OPLEVERING', N'OPKUIS', 0, 0);

INSERT INTO dbo.PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen)
SELECT t.Id, UPPER(v.Voorganger), v.Type, v.Wacht
FROM @v v
JOIN dbo.PlanningSjabloonTaak t ON t.Code = v.Taak AND t.BlokId IN (SELECT Id FROM dbo.PlanningSjabloonBlok WHERE SjabloonId = @s);

-- Controle (voor het bewaren)
DECLARE @n1 INT = (SELECT COUNT(*) FROM dbo.PlanningSjabloonBlok WHERE SjabloonId = @s);
DECLARE @n2 INT = (SELECT COUNT(*) FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId WHERE b.SjabloonId = @s);
DECLARE @n3 INT = (SELECT COUNT(*) FROM dbo.PlanningSjabloonVoorganger g JOIN dbo.PlanningSjabloonTaak t ON t.Id = g.TaakId JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId WHERE b.SjabloonId = @s);
PRINT N'Sjabloon "' + @naam + N'": ' + CAST(@n1 AS NVARCHAR(10)) + N' blokken, ' + CAST(@n2 AS NVARCHAR(10)) + N' taken, ' + CAST(@n3 AS NVARCHAR(10)) + N' voorgangers.';

SELECT 'Zonder bouwstaat (verwacht: Dakstructuur, Lift, Schilderwerk)' AS Controle, t.Code, t.Naam
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId WHERE b.SjabloonId = @s AND t.BouwstaatId IS NULL
UNION ALL
SELECT 'Zonder activiteit (verwacht: Voorlopige oplevering)', t.Code, t.Naam
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId WHERE b.SjabloonId = @s AND t.ActivityId IS NULL
UNION ALL
SELECT 'Voorganger niet gevonden in het sjabloon', g.VoorgangerCode, t.Code
FROM dbo.PlanningSjabloonVoorganger g JOIN dbo.PlanningSjabloonTaak t ON t.Id = g.TaakId JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId
WHERE b.SjabloonId = @s
  AND REPLACE(REPLACE(g.VoorgangerCode, N'VORIGE:', N''), N'EERSTE:', N'') NOT IN (SELECT Code FROM dbo.PlanningSjabloonBlok WHERE SjabloonId = @s)
  AND REPLACE(REPLACE(g.VoorgangerCode, N'VORIGE:', N''), N'EERSTE:', N'') NOT IN (SELECT t2.Code FROM dbo.PlanningSjabloonTaak t2 JOIN dbo.PlanningSjabloonBlok b2 ON b2.Id = t2.BlokId WHERE b2.SjabloonId = @s);

COMMIT;
PRINT N'KLAAR: bewaard. Open het sjabloon in Instellingen > Sjablonen > Planning en verfijn het in de editor (de editor valideert alles opnieuw).';
