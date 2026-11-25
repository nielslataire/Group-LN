-- =============================================
-- 162 (25/11/2026): twee planningssjablonen voor een verkaveling met woningen (startpunt, uit de doorloop met Niels):
--   1) "Verkaveling wegenis"  = de hoofdplanning van het project: landmeter, grondwerken, riolering, fundering, nutsvoorzieningen, boordstenen, verharding, groenaanleg, oplevering aan de gemeente.
--      Verkavelingen ZONDER wegenis gebruiken dit sjabloon niet.
--   2) "Woning"               = één bouwdeel-planning die door een groep woningen gedeeld wordt: gelijkvloers + 1 verdieping (bouwlagen bij het toepassen), plat of hellend dak, geen kelder.
--      Meerdere groepen woningen = meerdere bouwdelen met elk hun eigen planning uit dit sjabloon (per groep ook individueel te schuiven).
--   Vereist 158, 158e en 161 (bouwstaten). Activiteiten worden op naam opgezocht (wat niet gevonden wordt komt zonder activiteit in de uitvoer).
--   Bouwstaten: de wegenis gebruikt enkel WEGENIS_* (of geen), de woning enkel de gewone bouwstaten. Zo wacht een woningschijf nooit op een wegenistaak of omgekeerd.
--   Duren zijn SCHATTINGEN in werkdagen, niet gevalideerd; pas ze aan in de editor. Startvoorwaarde van de woningen t.o.v. de wegenis ("onmiddellijk / na funderingen / na nutsvoorzieningen / na voorlopige oplevering") komt als projectkeuze bij het toepassen, niet in dit sjabloon.
--   Idempotent: bestaat een sjabloon met die naam al, dan wordt het overgeslagen.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

------------------------------------------------------------------ 1) Verkaveling wegenis
DECLARE @naam1 NVARCHAR(200) = N'Verkaveling wegenis';
IF EXISTS (SELECT 1 FROM dbo.PlanningSjabloon WHERE Naam = @naam1)
    PRINT N'Het sjabloon "' + @naam1 + N'" bestaat al: overgeslagen.';
ELSE
BEGIN
    INSERT INTO dbo.PlanningSjabloon (Naam, Beschrijving, IsActief, CreatedDate)
    VALUES (@naam1, N'Aanleg van de wegenis en nutsvoorzieningen van een verkaveling, tot en met oplevering aan de gemeente. Duren zijn schattingen.', 1, SYSUTCDATETIME());
    DECLARE @s1 INT = SCOPE_IDENTITY();

    DECLARE @b1 TABLE (Nr INT IDENTITY(1,1), Code NVARCHAR(60), Naam NVARCHAR(200), Herhaling TINYINT);
    INSERT INTO @b1 (Code, Naam, Herhaling) VALUES
     (N'W_VOORBEREIDING', N'Voorbereiding wegenis', 0), (N'W_WEGENIS', N'Wegenis en nutsvoorzieningen', 0), (N'W_OPLEVERING', N'Oplevering wegenis', 0);
    INSERT INTO dbo.PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping)
    SELECT @s1, Code, Naam, Nr * 10, Herhaling, 0 FROM @b1;

    DECLARE @t1 TABLE (Nr INT IDENTITY(1,1), Blok NVARCHAR(60), Code NVARCHAR(60), Naam NVARCHAR(200), Duur INT, Voorwaarde NVARCHAR(100), Bouwstaat NVARCHAR(60), Activiteit NVARCHAR(250));
    INSERT INTO @t1 (Blok, Code, Naam, Duur, Voorwaarde, Bouwstaat, Activiteit) VALUES
     (N'W_VOORBEREIDING', N'W_LANDMETER',      N'Uitzetten wegenis en loten door landmeter', 3,  NULL, NULL,                          N'Landmeter'),
     (N'W_VOORBEREIDING', N'W_WERFINRICHTING', N'Werfinrichting wegenis',                    5,  NULL, NULL,                          N'Werfinrichting'),
     (N'W_WEGENIS',       N'W_GRONDWERKEN',    N'Grondwerken (ontbossing, ophoging, wegenbed)', 20, NULL, N'WEGENIS_GRONDWERKEN',     N'Grondwerken'),
     (N'W_WEGENIS',       N'W_RIOLERING',      N'Riolering en regenwaterafvoer',             25, NULL, N'WEGENIS_RIOLERING',          N'Grondwerken'),
     (N'W_WEGENIS',       N'W_FUNDERING',      N'Wegenfundering',                            15, NULL, N'WEGENIS_FUNDERING',          N'Grondwerken'),
     (N'W_WEGENIS',       N'W_NUTSVOORZIENINGEN', N'Nutsvoorzieningen (water, elektriciteit, gas, telecom)', 30, NULL, N'WEGENIS_NUTSVOORZIENINGEN', NULL),
     (N'W_WEGENIS',       N'W_BOORDEN',        N'Boordstenen en kantstroken',                10, NULL, N'WEGENIS_BOORDEN',            N'Buitenverhardingen'),
     (N'W_WEGENIS',       N'W_VERHARDING',     N'Wegverharding (asfalt of klinkers)',        15, NULL, N'WEGENIS_VERHARDING',         N'Buitenverhardingen'),
     (N'W_WEGENIS',       N'W_GROENAANLEG',    N'Groenaanleg en openbare ruimte',            10, NULL, N'WEGENIS_GROENAANLEG',        N'Tuinaanleg'),
     (N'W_OPLEVERING',    N'W_VOORLOPIGE_OPLEVERING', N'Voorlopige oplevering aan de gemeente', 1, NULL, N'WEGENIS_VOORLOPIGE_OPLEVERING', NULL);

    INSERT INTO dbo.PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Estafette, EstafetteWacht, Voorwaarde, BouwstaatId, ActivityId)
    SELECT b.Id, t.Code, t.Naam, t.Duur, t.Nr * 10, 0, 0, t.Voorwaarde,
           (SELECT TOP 1 x.Id FROM dbo.Bouwstaat x WHERE x.Code = t.Bouwstaat AND x.ProjectId IS NULL),
           (SELECT TOP 1 a.ActivityId FROM dbo.Activity a WHERE a.Omschrijving = t.Activiteit ORDER BY a.ActivityId)
    FROM @t1 t JOIN dbo.PlanningSjabloonBlok b ON b.SjabloonId = @s1 AND b.Code = t.Blok
    ORDER BY t.Nr;

    DECLARE @v1 TABLE (Taak NVARCHAR(60), Voorganger NVARCHAR(60), Type TINYINT, Wacht INT);
    INSERT INTO @v1 (Taak, Voorganger, Type, Wacht) VALUES
     (N'W_WERFINRICHTING', N'W_LANDMETER', 0, 0),
     (N'W_GRONDWERKEN', N'W_VOORBEREIDING', 0, 0),
     (N'W_RIOLERING', N'W_GRONDWERKEN', 0, 0),
     (N'W_FUNDERING', N'W_RIOLERING', 0, 0),
     (N'W_NUTSVOORZIENINGEN', N'W_RIOLERING', 0, 0),     -- nutsleidingen gelijktijdig met de fundering; boordstenen wachten op beide
     (N'W_BOORDEN', N'W_FUNDERING', 0, 0), (N'W_BOORDEN', N'W_NUTSVOORZIENINGEN', 0, 0),
     (N'W_VERHARDING', N'W_BOORDEN', 0, 0),
     (N'W_GROENAANLEG', N'W_VERHARDING', 0, 0),
     (N'W_VOORLOPIGE_OPLEVERING', N'W_GROENAANLEG', 0, 0);
    INSERT INTO dbo.PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen)
    SELECT t.Id, UPPER(v.Voorganger), v.Type, v.Wacht
    FROM @v1 v JOIN dbo.PlanningSjabloonTaak t ON t.Code = v.Taak AND t.BlokId IN (SELECT Id FROM dbo.PlanningSjabloonBlok WHERE SjabloonId = @s1);
    PRINT N'Sjabloon "' + @naam1 + N'" aangemaakt.';
END

------------------------------------------------------------------ 2) Woning
DECLARE @naam2 NVARCHAR(200) = N'Woning';
IF EXISTS (SELECT 1 FROM dbo.PlanningSjabloon WHERE Naam = @naam2)
    PRINT N'Het sjabloon "' + @naam2 + N'" bestaat al: overgeslagen.';
ELSE
BEGIN
    INSERT INTO dbo.PlanningSjabloon (Naam, Beschrijving, IsActief, CreatedDate)
    VALUES (@naam2, N'Eengezinswoning (gelijkvloers + verdieping, geen kelder) met plat of hellend dak. Wordt per groep woningen als bouwdeel-planning toegepast. Duren zijn schattingen.', 1, SYSUTCDATETIME());
    DECLARE @s2 INT = SCOPE_IDENTITY();

    DECLARE @b2 TABLE (Nr INT IDENTITY(1,1), Code NVARCHAR(60), Naam NVARCHAR(200), Herhaling TINYINT);
    INSERT INTO @b2 (Code, Naam, Herhaling) VALUES
     (N'B_VOORBEREIDING', N'Voorbereiding', 0), (N'B_ONDERBOUW', N'Onderbouw', 0), (N'B_BOVENBOUW', N'Bovenbouw', 2), (N'B_DAK', N'Dak', 0),
     (N'B_GEVEL', N'Gevel en buitenschrijnwerk', 0), (N'B_TECHNIEKEN', N'Technieken', 0), (N'B_AFWERKING', N'Afwerking', 0),
     (N'B_OMGEVING', N'Omgevingswerken', 0), (N'B_OPLEVERING', N'Oplevering', 0);
    INSERT INTO dbo.PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping)
    SELECT @s2, Code, Naam, Nr * 10, Herhaling, 0 FROM @b2;

    DECLARE @t2 TABLE (Nr INT IDENTITY(1,1), Blok NVARCHAR(60), Code NVARCHAR(60), Naam NVARCHAR(200), Duur INT, Voorwaarde NVARCHAR(100), Bouwstaat NVARCHAR(60), Activiteit NVARCHAR(250));
    INSERT INTO @t2 (Blok, Code, Naam, Duur, Voorwaarde, Bouwstaat, Activiteit) VALUES
     (N'B_VOORBEREIDING', N'WERF_INRICHTEN',       N'Werf inrichten',                         3, NULL, N'WERF_UITGEZET',       N'Werfinrichting'),
     (N'B_VOORBEREIDING', N'LANDMETER_UITZETTEN',  N'Uitzetten door landmeter',               1, NULL, N'WERF_UITGEZET',       N'Landmeter'),
     (N'B_ONDERBOUW',     N'FUNDERINGEN',          N'Funderingen',                            8, NULL, N'FUNDERING_KLAAR',     N'Ruwbouw'),
     (N'B_ONDERBOUW',     N'RIOLERING',            N'Rioleringen',                            5, NULL, N'RIOLERING',           N'Grondwerken'),
     (N'B_BOVENBOUW',     N'VLOERPLAAT_GLVL',      N'Vloerplaat',                             3, N'grond:ja',  N'VLOERPLAAT_GLVL', N'Ruwbouw'),
     (N'B_BOVENBOUW',     N'PREDALLEN',            N'Predallen',                              2, N'grond:nee', N'PREDALLEN',       N'Ruwbouw'),
     (N'B_BOVENBOUW',     N'OPSTORT',              N'Opstort',                                2, N'grond:nee', N'OPSTORT',         N'Ruwbouw'),
     (N'B_BOVENBOUW',     N'METSELWERK_RUWBOUW',   N'Metselwerk',                            15, NULL, N'METSELWERK_RUWBOUW', N'Ruwbouw'),
     (N'B_BOVENBOUW',     N'GIPSBLOKKEN',          N'Gipsblokken',                            5, NULL, N'GIPSBLOKKEN',        N'Gipsblokken - plaatsen'),
     (N'B_BOVENBOUW',     N'TERRAS_LUIFEL',        N'Terrassen en luifels',                   3, N'terras:ja', N'TERRAS_LUIFEL',   N'Ruwbouw'),
     (N'B_BOVENBOUW',     N'DORPELS',              N'Dorpels',                                2, N'laag:eenmalig', N'DORPELS',    N'Ruwbouw'),
     (N'B_BOVENBOUW',     N'GEVELMETSELWERK',      N'Gevelmetselwerk',                       15, N'laag:eenmalig', N'GEVELMETSELWERK', N'Ruwbouw'),
     (N'B_BOVENBOUW',     N'PREDALLEN_DAK',        N'Predallen dak',                          2, N'laag:eenmalig,dak:plat', N'DAKPLAAT', N'Ruwbouw'),
     (N'B_BOVENBOUW',     N'OPSTORT_DAK',          N'Opstort dak',                            2, N'laag:eenmalig,dak:plat', N'DAKPLAAT', N'Ruwbouw'),
     (N'B_BOVENBOUW',     N'HELLINGSSCHAPE',       N'Hellingsschape plat dak',                3, N'laag:eenmalig,dak:plat', N'HELLINGSSCHAPE', N'Hellingsbeton'),
     (N'B_DAK',           N'DAKOPSTANDEN',         N'Dakopstanden',                           2, N'dak:plat',    N'DAKOPSTANDEN',  N'Dakbedekking - Plat dak - EPDM'),
     (N'B_DAK',           N'DAKBEDEKKING_PLAT',    N'Platte dakbedekking',                    5, N'dak:plat',    N'DAK_PLAT_DICHT', N'Dakbedekking - Plat dak - EPDM'),
     (N'B_DAK',           N'DAKSTRUCTUUR',         N'Dakstructuur',                           8, N'dak:hellend', N'DAKSTRUCTUUR',  N'Daktimmerwerken'),
     (N'B_DAK',           N'DAKBEDEKKING_HELLEND', N'Dakbedekking hellend dak',               8, N'dak:hellend', N'DAK_HELLEND_DICHT', N'Dakbedekking - Hellend Dak'),
     (N'B_GEVEL',         N'BUITENSCHRIJNWERK',    N'Plaatsen buitenschrijnwerk en glas',     8, NULL, N'BUITENSCHRIJNWERK',  N'Buitenschrijnwerk - Aluminium'),
     (N'B_TECHNIEKEN',    N'LEIDINGEN_ELEKTRA',    N'Plaatsen leidingen elektriciteit',       8, NULL, N'TECHNIEKEN_RUW',     N'Elektrische installatie'),
     (N'B_TECHNIEKEN',    N'LEIDINGEN_SANITAIR',   N'Plaatsen leidingen sanitair en verwarming', 8, NULL, N'TECHNIEKEN_RUW',  N'Sanitaire installatie'),
     (N'B_AFWERKING',     N'PLEISTER',             N'Pleisterwerken',                        10, NULL, N'PLEISTER',           N'Binnenbepleistering'),
     (N'B_AFWERKING',     N'ISOLATIE',             N'Vloerisolatie',                          2, NULL, N'ISOLATIE_VLOER',     N'Vloer - Isolatie - PUR'),
     (N'B_AFWERKING',     N'CHAPE',                N'Plaatsen chape',                         3, NULL, N'CHAPE',              N'Vloer - Dekvloer'),
     (N'B_AFWERKING',     N'TEGELWERK',            N'Vloeren en wandtegels',                  8, NULL, N'TEGELWERK',          N'Vloer - Keramisch'),
     (N'B_AFWERKING',     N'PARKET',               N'Parket',                                 5, NULL, N'VLOEREN_PARKET',     N'Vloer - Hout'),
     (N'B_AFWERKING',     N'BINNENSCHRIJNWERK',    N'Binnenschrijnwerk',                      6, NULL, N'BINNENSCHRIJNWERK',  N'Binnendeuren'),
     (N'B_AFWERKING',     N'KEUKEN',               N'Plaatsen keuken',                        3, NULL, N'KEUKEN',             N'Vast Meubilair - Keukens'),
     (N'B_AFWERKING',     N'SCHILDERWERK',         N'Schilderwerken',                         8, NULL, NULL,                  N'Schilderwerken'),
     (N'B_OMGEVING',      N'OMGEVINGSWERKEN',      N'Omgevingswerken (oprit, terras, tuin)',  8, NULL, N'OMGEVINGSWERKEN',    N'Buitenverhardingen'),
     (N'B_OPLEVERING',    N'OPKUIS',               N'Opkuis',                                 2, NULL, N'OPKUIS',             N'Schoonmaak'),
     (N'B_OPLEVERING',    N'VOORLOPIGE_OPLEVERING', N'Voorlopige oplevering (sleutels)',      1, NULL, N'VOORLOPIGE_OPLEVERING', NULL);

    INSERT INTO dbo.PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Estafette, EstafetteWacht, Voorwaarde, BouwstaatId, ActivityId)
    SELECT b.Id, t.Code, t.Naam, t.Duur, t.Nr * 10, 0, 0, t.Voorwaarde,
           (SELECT TOP 1 x.Id FROM dbo.Bouwstaat x WHERE x.Code = t.Bouwstaat AND x.ProjectId IS NULL),
           (SELECT TOP 1 a.ActivityId FROM dbo.Activity a WHERE a.Omschrijving = t.Activiteit ORDER BY a.ActivityId)
    FROM @t2 t JOIN dbo.PlanningSjabloonBlok b ON b.SjabloonId = @s2 AND b.Code = t.Blok
    ORDER BY t.Nr;

    DECLARE @v2 TABLE (Taak NVARCHAR(60), Voorganger NVARCHAR(60), Type TINYINT, Wacht INT);
    INSERT INTO @v2 (Taak, Voorganger, Type, Wacht) VALUES
     (N'LANDMETER_UITZETTEN', N'WERF_INRICHTEN', 0, 0),
     (N'FUNDERINGEN', N'B_VOORBEREIDING', 0, 0),
     (N'RIOLERING', N'FUNDERINGEN', 0, 0),
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
     (N'PLEISTER', N'LEIDINGEN_ELEKTRA', 0, 0), (N'PLEISTER', N'LEIDINGEN_SANITAIR', 0, 0), (N'PLEISTER', N'BUITENSCHRIJNWERK', 0, 0),
     (N'ISOLATIE', N'PLEISTER', 0, 0), (N'CHAPE', N'ISOLATIE', 0, 0), (N'TEGELWERK', N'CHAPE', 0, 0), (N'PARKET', N'TEGELWERK', 0, 0),
     (N'BINNENSCHRIJNWERK', N'PARKET', 0, 0), (N'KEUKEN', N'PARKET', 0, 0),
     (N'SCHILDERWERK', N'BINNENSCHRIJNWERK', 0, 0), (N'SCHILDERWERK', N'KEUKEN', 0, 0),
     (N'OMGEVINGSWERKEN', N'B_AFWERKING', 0, 0),
     (N'OPKUIS', N'B_OMGEVING', 0, 0), (N'VOORLOPIGE_OPLEVERING', N'OPKUIS', 0, 0);
    INSERT INTO dbo.PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen)
    SELECT t.Id, UPPER(v.Voorganger), v.Type, v.Wacht
    FROM @v2 v JOIN dbo.PlanningSjabloonTaak t ON t.Code = v.Taak AND t.BlokId IN (SELECT Id FROM dbo.PlanningSjabloonBlok WHERE SjabloonId = @s2);
    PRINT N'Sjabloon "' + @naam2 + N'" aangemaakt.';
END

------------------------------------------------------------------ Controle
SELECT s.Naam AS Sjabloon,
       (SELECT COUNT(*) FROM dbo.PlanningSjabloonBlok b WHERE b.SjabloonId = s.Id) AS Blokken,
       (SELECT COUNT(*) FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId WHERE b.SjabloonId = s.Id) AS Taken,
       (SELECT COUNT(*) FROM dbo.PlanningSjabloonVoorganger g JOIN dbo.PlanningSjabloonTaak t ON t.Id = g.TaakId JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId WHERE b.SjabloonId = s.Id) AS Voorgangers
FROM dbo.PlanningSjabloon s WHERE s.Naam IN (N'Verkaveling wegenis', N'Woning');

SELECT s.Naam AS Sjabloon, 'Taak zonder bouwstaat' AS Controle, t.Code, t.Naam
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN dbo.PlanningSjabloon s ON s.Id = b.SjabloonId
WHERE s.Naam IN (N'Verkaveling wegenis', N'Woning') AND t.BouwstaatId IS NULL
UNION ALL
SELECT s.Naam, 'Taak zonder activiteit (naam niet gevonden in Activity)', t.Code, t.Naam
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN dbo.PlanningSjabloon s ON s.Id = b.SjabloonId
WHERE s.Naam IN (N'Verkaveling wegenis', N'Woning') AND t.ActivityId IS NULL
UNION ALL
SELECT s.Naam, 'Voorganger niet gevonden in het sjabloon', g.VoorgangerCode, t.Code
FROM dbo.PlanningSjabloonVoorganger g JOIN dbo.PlanningSjabloonTaak t ON t.Id = g.TaakId JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN dbo.PlanningSjabloon s ON s.Id = b.SjabloonId
WHERE s.Naam IN (N'Verkaveling wegenis', N'Woning')
  AND REPLACE(REPLACE(g.VoorgangerCode, N'VORIGE:', N''), N'EERSTE:', N'') NOT IN (SELECT Code FROM dbo.PlanningSjabloonBlok WHERE SjabloonId = s.Id)
  AND REPLACE(REPLACE(g.VoorgangerCode, N'VORIGE:', N''), N'EERSTE:', N'') NOT IN (SELECT t2.Code FROM dbo.PlanningSjabloonTaak t2 JOIN dbo.PlanningSjabloonBlok b2 ON b2.Id = t2.BlokId WHERE b2.SjabloonId = s.Id);

COMMIT;
PRINT N'KLAAR. Open Instellingen > Sjablonen > Planning om de twee sjablonen te bekijken en te verfijnen.';
