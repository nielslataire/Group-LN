-- =============================================
-- TEST_project79_sjablonen_gelijkvloers.sql  (GEEN genummerde migratie: enkel testgegevens)
-- Vereist: TEST_project79_sjablonen.sql en migratie 157_PlanningSjabloonTaakVoorwaarde.sql (voorwaarde per taak) zijn gedraaid.
-- Geen nieuwe migratie nodig voor de blokstand "Eén fase, taken per bouwlaag": Herhaling is een TINYINT en krijgt de waarde 2.
--
-- Past het testsjabloon "TEST Appartementsgebouw (naar Res. Moleken)" aan zoals Niels een gebouw ziet:
--   * Fase "Ruwbouw - Onderbouw" = de kelder (enkel met kelder). De kelder heeft geen afdek: de eerste plaat van de bovenbouw is de plaat boven de kelder.
--   * Fase "Ruwbouw - Bovenbouw" = ÉÉN fase met per bouwlaag (gelijkvloers, 1ste verdieping, 2de verdieping ...):
--         Predallen, Opstort, Metselwerk
--     Zonder kelder is het gelijkvloers gewoon één taak "Vloerplaat gelijkvloers" (geen predallen en opstort).
--     Predallen wachten op het metselwerk van de vorige bouwlaag (en op de kelderwanden), opstort op de predallen, metselwerk op de opstort.
--     Terrassen en luifels: een taak in dezelfde fase, enkel voor de bouwlagen die je bij het toepassen kiest.
--     Plat dak: na de bovenste bouwlaag komen er nog "Predallen dak" en "Opstort dak" bij (eenmalig, enkel bij plat dak).
--     Gevelmetselwerk: één taak (40 werkdagen) in deze fase, zonder voorgangers (start dus op dag 1: geef ze zelf een voorganger als dat niet klopt).
--   * De taken die vroeger op de afdek wachtten (ramen, leidingen, dak ...) wachten nu op het metselwerk van de bovenste bouwlaag.
-- Past ook het schijvensjabloon "TEST Appartement (met verdiepingen)" aan: een schijf op de opstort van elke bouwlaag en
--   zonder kelder "Gieten vloerplaat gelijkvloers".
-- Herhaalbaar: het blok Bovenbouw wordt telkens opnieuw opgebouwd. Overblijfsels van een eerdere versie van dit script (de aparte fases
--   "Vloerplaat gelijkvloers" en "Terrassen en luifels bij afdek") worden verwijderd.
-- Let op: de duren zijn schattingen (predallen 3, opstort 2, metselwerk 15 werkdagen); pas ze aan in de editor.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @s INT, @bv INT, @bk INT, @t INT, @sc INT;

IF COL_LENGTH(N'dbo.PlanningSjabloonTaak', N'Voorwaarde') IS NULL
BEGIN
    PRINT N'Eerst migratie 157_PlanningSjabloonTaakVoorwaarde.sql draaien.';
    RETURN;
END

SELECT @s = Id FROM PlanningSjabloon WHERE Naam = N'TEST Appartementsgebouw (naar Res. Moleken)';
IF @s IS NULL
BEGIN
    PRINT N'Sjabloon "TEST Appartementsgebouw (naar Res. Moleken)" niet gevonden: eerst TEST_project79_sjablonen.sql draaien.';
    RETURN;
END

SELECT @bv = Id FROM PlanningSjabloonBlok WHERE SjabloonId = @s AND Code = N'B_VERDIEPING';
IF @bv IS NULL
BEGIN
    PRINT N'Blok B_VERDIEPING niet gevonden: het sjabloon is aangepast. Niets gewijzigd.';
    RETURN;
END
SELECT @bk = Id FROM PlanningSjabloonBlok WHERE SjabloonId = @s AND Code = N'B_KELDER';

BEGIN TRAN;

-- Overblijfsels van een eerdere versie van dit script: aparte fases voor de vloerplaat gelijkvloers en de terrassen
DELETE FROM PlanningSjabloonBlok WHERE SjabloonId = @s AND Id <> @bv AND (Naam LIKE N'Vloerplaat gelijkvloers%' OR Naam LIKE N'Terrassen en luifels%');

-- Kelder: geen afdek meer; de fase heet Onderbouw
IF @bk IS NOT NULL
BEGIN
    UPDATE PlanningSjabloonBlok SET Naam = N'Ruwbouw - Onderbouw' WHERE Id = @bk;
    DELETE FROM PlanningSjabloonTaak WHERE BlokId = @bk AND Code = N'AFDEK_KELDER';   -- de voorgangers verdwijnen mee (cascade)
END
-- verwijzingen naar de verdwenen afdek van de kelder
DELETE v FROM PlanningSjabloonVoorganger v JOIN PlanningSjabloonTaak t ON t.Id = v.TaakId JOIN PlanningSjabloonBlok b ON b.Id = t.BlokId
WHERE b.SjabloonId = @s AND v.VoorgangerCode = N'AFDEK_KELDER';

-- Het blok wordt één fase met de taken per bouwlaag (Herhaling = 2)
UPDATE PlanningSjabloonBlok SET Naam = N'Ruwbouw - Bovenbouw', Herhaling = 2, VolgtVorigeVerdieping = 0 WHERE Id = @bv;
DELETE FROM PlanningSjabloonTaak WHERE BlokId = @bv;   -- herbouwen (ook de taken van een eerdere versie van dit script)

-- Gevelmetselwerk: één taak in deze fase, zonder voorgangers (komt na de bouwlagen: laag:eenmalig)
SELECT @t = t.Id FROM PlanningSjabloonTaak t JOIN PlanningSjabloonBlok b ON b.Id = t.BlokId WHERE b.SjabloonId = @s AND t.Code = N'GEVELMETSELWERK';
IF @t IS NOT NULL
BEGIN
    DELETE FROM PlanningSjabloonVoorganger WHERE TaakId = @t;
    UPDATE PlanningSjabloonTaak SET BlokId = @bv, Volgorde = 80, Voorwaarde = N'laag:eenmalig' WHERE Id = @t;
END

-- Zonder kelder: het gelijkvloers rust op de grond (grond:ja) en is één vloerplaat
INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Voorwaarde) VALUES (@bv, N'VLOERPLAAT_GLVL', N'Vloerplaat', 2, 10, N'grond:ja');
SET @t = SCOPE_IDENTITY();
INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'WERF_UITZETTEN', 0, 0);

-- Predallen: op het gelijkvloers na de kelderwanden, daarboven na het metselwerk van de vorige bouwlaag
INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Voorwaarde) VALUES (@bv, N'PREDALLEN', N'Predallen', 3, 20, N'grond:nee');
SET @t = SCOPE_IDENTITY();
INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'VORIGE:METSELWERK_RUWBOUW', 0, 0);
INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'WANDEN_KELDER', 0, 0);

INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Voorwaarde) VALUES (@bv, N'OPSTORT', N'Opstort', 2, 30, N'grond:nee');
SET @t = SCOPE_IDENTITY();
INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'PREDALLEN', 0, 0);

INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@bv, N'METSELWERK_RUWBOUW', N'Metselwerk', 15, 40);
SET @t = SCOPE_IDENTITY();
INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'VLOERPLAAT_GLVL', 0, 0);
INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'OPSTORT', 0, 0);

-- Terrassen en luifels: in dezelfde fase, enkel bij de bouwlagen die je kiest
INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Voorwaarde) VALUES (@bv, N'TERRAS_LUIFEL', N'Terrassen en luifels', 5, 50, N'terras:ja');
SET @t = SCOPE_IDENTITY();
INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'METSELWERK_RUWBOUW', 0, 0);

-- Plat dak: nog een laag predallen en opstort na de bovenste bouwlaag
INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Voorwaarde) VALUES (@bv, N'PREDALLEN_DAK', N'Predallen dak', 3, 60, N'laag:eenmalig,dak:plat');
SET @t = SCOPE_IDENTITY();
INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'VORIGE:METSELWERK_RUWBOUW', 0, 0);
INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Voorwaarde) VALUES (@bv, N'OPSTORT_DAK', N'Opstort dak', 2, 70, N'laag:eenmalig,dak:plat');
SET @t = SCOPE_IDENTITY();
INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'PREDALLEN_DAK', 0, 0);

-- Taken die op de afdek wachtten (de afdek bestaat niet meer): het dak na de opstort van het dak, de rest na het metselwerk van de bovenste bouwlaag
UPDATE v SET VoorgangerCode = N'OPSTORT_DAK'
FROM PlanningSjabloonVoorganger v JOIN PlanningSjabloonTaak t ON t.Id = v.TaakId JOIN PlanningSjabloonBlok b ON b.Id = t.BlokId
WHERE b.SjabloonId = @s AND v.VoorgangerCode = N'AFDEK' AND t.Code = N'DAKOPSTANDEN';
UPDATE v SET VoorgangerCode = N'METSELWERK_RUWBOUW'
FROM PlanningSjabloonVoorganger v JOIN PlanningSjabloonTaak t ON t.Id = v.TaakId JOIN PlanningSjabloonBlok b ON b.Id = t.BlokId
WHERE b.SjabloonId = @s AND v.VoorgangerCode = N'AFDEK';

UPDATE PlanningSjabloon SET Beschrijving = N'Appartementsgebouw met optionele kelder (Ruwbouw - Onderbouw), één fase Ruwbouw - Bovenbouw met per bouwlaag (gelijkvloers, 1ste verdieping, 2de verdieping ...) predallen, opstort en metselwerk, optionele terrassen of luifels, het gevelmetselwerk, en een plat of hellend dak.' WHERE Id = @s;

-- Schijvensjabloon volgt de nieuwe codes
SELECT @sc = Id FROM SchijfSjabloon WHERE Naam = N'TEST Appartement (met verdiepingen)';
IF @sc IS NOT NULL
BEGIN
    DECLARE @pos INT;
    DELETE FROM SchijfSjabloonSchijf WHERE SjabloonId = @sc AND PlanningCode IN (N'VLOERPLAAT', N'AFDEK', N'AFDEK_KELDER', N'VLOERPLAAT_GLVL', N'METSELWERK_RUWBOUW', N'OPSTORT');
    SELECT @pos = ISNULL(MAX(Volgorde), 1) FROM SchijfSjabloonSchijf WHERE SjabloonId = @sc AND PlanningCode IN (N'AFBRAAK', N'VLOERPLAAT_KELDER');
    UPDATE SchijfSjabloonSchijf SET Volgorde = Volgorde + 3 WHERE SjabloonId = @sc AND Volgorde > @pos;
    -- Een schijf op de opstort van elke bouwlaag; zonder kelder is het gelijkvloers één vloerplaat (geen opstort).
    -- "laag:boven" = één schijf per bouwlaag boven het gelijkvloers (gewicht per bouwlaag).
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde)
    VALUES (@sc, @pos + 1, N'Opstort gelijkvloers', N'OPSTORT', NULL, 1, 0, N'kelder', N'ja');
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde)
    VALUES (@sc, @pos + 2, N'Gieten vloerplaat gelijkvloers', N'VLOERPLAAT_GLVL', NULL, 1, 0, N'kelder', N'nee');
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde)
    VALUES (@sc, @pos + 3, N'Opstort {verdieping}', N'OPSTORT', NULL, 1, 1, N'laag', N'boven');
    PRINT N'Schijvensjabloon aangepast: "Opstort" per bouwlaag en zonder kelder de vloerplaat gelijkvloers.';
END

COMMIT TRAN;
PRINT N'Sjabloon "TEST Appartementsgebouw (naar Res. Moleken)" aangepast: Ruwbouw - Onderbouw en één fase Ruwbouw - Bovenbouw met de taken per bouwlaag.';
