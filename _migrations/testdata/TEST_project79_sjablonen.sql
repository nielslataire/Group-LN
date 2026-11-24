-- =============================================
-- TEST_project79_sjablonen.sql  (GEEN genummerde migratie: enkel testgegevens)
-- Maakt testsjablonen aan, afgeleid van de planningen en betalingsschijven van Koksijde, Erpe-Mere, RDR en Res. Moleken:
--   * 2 planningssjablonen   (Appartementsgebouw, Woning)
--   * 1 trajectsjabloon      (mijlpalen met dezelfde code als taken in de planning)
--   * 6 schijvensjablonen    (Appartement, Woning, Garage/parking/berging, Bovengrondse parkeerplaats, Carport, Parking)
--   * 2 projectsjablonen     (Appartementsgebouw, Woning) die ze bundelen
-- Alle namen beginnen met "TEST ". Enkel INSERTs, geen bestaande gegevens worden gewijzigd. Herhaalbaar: een sjabloon met dezelfde naam wordt overgeslagen.
-- Vereist: migraties 146, 147, 155 en 156 (en 030 voor trajectsjablonen) zijn gedraaid.
-- Opruimen: DELETE FROM PlanningSjabloon/SchijfSjabloon/TrajectSjabloon/ProjectSjabloon WHERE Naam LIKE N'TEST %' (kinderen verdwijnen mee).
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM PlanningSjabloon WHERE Naam = N'TEST Appartementsgebouw (naar Res. Moleken)')
BEGIN
    INSERT INTO PlanningSjabloon (Naam, Beschrijving, IsActief) VALUES (N'TEST Appartementsgebouw (naar Res. Moleken)', N'Appartementsgebouw met optionele kelder, een blok per verdieping en een plat of hellend dak.', 1);
    SET @s = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_VOORBEREIDING', N'Voorbereiding', 1, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'AFBRAAK', N'Afbraak bestaande gebouwen', 10, 1);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'WERF_UITZETTEN', N'Werf inrichten en uitzetten', 3, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'AFBRAAK', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_KELDER', N'Ruwbouw kelder', 2, 0, 0, N'kelder', N'ja');
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'UITGRAVEN_KELDER', N'Uitgraven kelder', 15, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'WERF_UITZETTEN', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'VLOERPLAAT_KELDER', N'Gieten vloerplaat kelder', 5, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'UITGRAVEN_KELDER', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'WANDEN_KELDER', N'Buiten- en binnenwanden kelder', 25, 3);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'VLOERPLAAT_KELDER', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'AFDEK_KELDER', N'Afdek kelder', 15, 4);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'WANDEN_KELDER', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_VERDIEPING', N'Ruwbouw', 3, 1, 1, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'VLOERPLAAT', N'Gieten vloerplaat', 2, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'WERF_UITZETTEN', 0, 0);
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'AFDEK_KELDER', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'METSELWERK_RUWBOUW', N'Metselwerk', 15, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'VLOERPLAAT', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'AFDEK', N'Afdek', 10, 3);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'METSELWERK_RUWBOUW', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_DAK_PLAT', N'Plat dak', 4, 0, 0, N'dak', N'plat');
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'DAKOPSTANDEN', N'Dak- en liftopstanden', 5, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'AFDEK', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'DAKBEDEKKING_PLAT', N'Platte dakbedekking', 15, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'DAKOPSTANDEN', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_DAK_HELLEND', N'Hellend dak', 5, 0, 0, N'dak', N'hellend');
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'DAKSTRUCTUUR', N'Dakstructuur', 15, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'AFDEK', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'DAKBEDEKKING_HELLEND', N'Dakbedekking (pannen)', 15, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'DAKSTRUCTUUR', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_GEVEL', N'Gevel en buitenschrijnwerk', 6, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'GEVELMETSELWERK', N'Gevelmetselwerk', 40, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'AFDEK', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'RAMEN', N'Plaatsen ramen en glas', 15, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'AFDEK', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_TECHNIEKEN', N'Technieken', 7, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'LEIDINGEN', N'Plaatsen leidingen technieken', 20, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'AFDEK', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_PLEISTER', N'Pleisterwerken', 8, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'PLEISTER', N'Pleisterwerken', 25, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'LEIDINGEN', 0, 0);
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'RAMEN', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_VLOEREN', N'Dekvloeren', 9, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'ISOLATIE', N'Vloerisolatie', 3, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'PLEISTER', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'CHAPE', N'Plaatsen chape', 5, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'ISOLATIE', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_AFWERKING', N'Afwerking', 10, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'TEGELWERK', N'Vloeren en wandtegels', 15, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'CHAPE', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'KEUKEN', N'Plaatsen keuken', 10, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'TEGELWERK', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'BINNENSCHRIJNWERK', N'Binnenschrijnwerk', 15, 3);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'TEGELWERK', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'B_OPLEVERING', N'Oplevering', 11, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'OPKUIS', N'Opkuis', 3, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'KEUKEN', 0, 0);
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'BINNENSCHRIJNWERK', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'VOORLOPIGE_OPLEVERING', N'Voorlopige oplevering (sleutels)', 1, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'OPKUIS', 0, 0);
    PRINT N'Planningssjabloon aangemaakt: TEST Appartementsgebouw (naar Res. Moleken)';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM PlanningSjabloon WHERE Naam = N'TEST Woning (naar Koksijde en RDR)')
BEGIN
    INSERT INTO PlanningSjabloon (Naam, Beschrijving, IsActief) VALUES (N'TEST Woning (naar Koksijde en RDR)', N'Eén woning, van het uitzetten tot de voorlopige oplevering.', 1);
    SET @s = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'W_RUWBOUW', N'Ruwbouw', 1, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'UITZETTEN', N'Uitzetten van de woning', 1, 1);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'FUNDERINGEN', N'Funderingen', 8, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'UITZETTEN', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'VLOERPLAAT_GLVL', N'Gieten vloerplaat gelijkvloers', 1, 3);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'FUNDERINGEN', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'RUWBOUW_GLVL', N'Ruwbouw gelijkvloers', 25, 4);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'VLOERPLAAT_GLVL', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'VLOERPLAAT_V1', N'Afdek gelijkvloers (vloerplaat eerste verdieping)', 10, 5);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'RUWBOUW_GLVL', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'RUWBOUW_V1', N'Ruwbouw eerste verdieping', 10, 6);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'VLOERPLAAT_V1', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'DAKPLAAT', N'Afdek eerste verdieping (dakplaat)', 5, 7);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'RUWBOUW_V1', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'GEVELMETSELWERK', N'Gevelmetselwerk', 12, 8);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'DAKPLAAT', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'W_DAK', N'Dak', 2, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'DAKOPSTANDEN', N'Dakopstanden', 1, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'DAKPLAAT', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'DAKBEDEKKING', N'Plaatsen dakbedekking', 10, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'DAKOPSTANDEN', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'W_SCHRIJNWERK', N'Buitenschrijnwerk', 3, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'RAMEN', N'Plaatsen ramen en glas', 8, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'GEVELMETSELWERK', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'W_TECHNIEKEN', N'Technieken', 4, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'LEIDINGEN', N'Plaatsen leidingen technieken', 8, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'RAMEN', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'W_PLEISTER', N'Pleisterwerken', 5, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'PLEISTER', N'Pleisterwerken', 7, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'LEIDINGEN', 0, 0);
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'DAKBEDEKKING', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'W_VLOEREN', N'Dekvloeren', 6, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'ISOLATIE', N'PUR en EPS', 4, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'PLEISTER', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'CHAPE', N'Plaatsen chape', 2, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'ISOLATIE', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'W_AFWERKING', N'Afwerking', 7, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'TEGELWERK', N'Vloeren en wandtegels', 8, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'CHAPE', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'PARKET', N'Parket', 4, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'TEGELWERK', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'KEUKEN', N'Plaatsen keuken', 2, 3);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'TEGELWERK', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'PLINTEN', N'Afwerking plinten', 2, 4);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'PARKET', 0, 0);
    INSERT INTO PlanningSjabloonBlok (SjabloonId, Code, Naam, Volgorde, Herhaling, VolgtVorigeVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, N'W_OPLEVERING', N'Oplevering', 8, 0, 0, NULL, NULL);
    SET @b = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'OPKUIS', N'Opkuis', 2, 1);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'KEUKEN', 0, 0);
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'PLINTEN', 0, 0);
    INSERT INTO PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde) VALUES (@b, N'VOORLOPIGE_OPLEVERING', N'Voorlopige oplevering (sleutels)', 1, 2);
    SET @t = SCOPE_IDENTITY();
    INSERT INTO PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen) VALUES (@t, N'OPKUIS', 0, 0);
    PRINT N'Planningssjabloon aangemaakt: TEST Woning (naar Koksijde en RDR)';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM TrajectSjabloon WHERE Naam = N'TEST Bouwtraject (mijlpalen volgen de planning)')
BEGIN
    INSERT INTO TrajectSjabloon (Naam, IsStandaard, IsActief, Omschrijving) VALUES (N'TEST Bouwtraject (mijlpalen volgen de planning)', 0, 1, N'Testtraject: elke mijlpaal heeft dezelfde code als een taak in de testplanningen.');
    SET @s = SCOPE_IDENTITY();
    INSERT INTO TrajectSjabloonFase (TrajectSjabloonId, Naam, Code, Volgorde) VALUES (@s, N'Bouw', N'BOUW', 1);
    SET @f = SCOPE_IDENTITY();
    INSERT INTO TrajectSjabloonMijlpaal (TrajectSjabloonFaseId, Naam, Code, Volgorde, MijlpaalType, IsVerplicht) VALUES (@f, N'Leidingen technieken geplaatst', N'LEIDINGEN', 1, 0, 1);
    INSERT INTO TrajectSjabloonMijlpaal (TrajectSjabloonFaseId, Naam, Code, Volgorde, MijlpaalType, IsVerplicht) VALUES (@f, N'Ramen geplaatst', N'RAMEN', 2, 0, 1);
    INSERT INTO TrajectSjabloonMijlpaal (TrajectSjabloonFaseId, Naam, Code, Volgorde, MijlpaalType, IsVerplicht) VALUES (@f, N'Chape gelegd', N'CHAPE', 3, 0, 1);
    INSERT INTO TrajectSjabloonMijlpaal (TrajectSjabloonFaseId, Naam, Code, Volgorde, MijlpaalType, IsVerplicht) VALUES (@f, N'Keuken geplaatst', N'KEUKEN', 4, 0, 1);
    INSERT INTO TrajectSjabloonMijlpaal (TrajectSjabloonFaseId, Naam, Code, Volgorde, MijlpaalType, IsVerplicht) VALUES (@f, N'Voorlopige oplevering', N'VOORLOPIGE_OPLEVERING', 5, 0, 1);
    PRINT N'Trajectsjabloon aangemaakt: TEST Bouwtraject (mijlpalen volgen de planning)';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM SchijfSjabloon WHERE Naam = N'TEST Appartement (met verdiepingen)')
BEGIN
    INSERT INTO SchijfSjabloon (Naam, Beschrijving, MinimumPercentage, IsActief) VALUES (N'TEST Appartement (met verdiepingen)', N'5 % vast voor technieken, chape, keuken en oplevering; de rest gelijk verdeeld (vloerplaten tellen voor 4).', 5, 1);
    SET @s = SCOPE_IDENTITY();
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 1, N'Afbraak bestaande gebouwen', N'AFBRAAK', NULL, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 2, N'Gieten vloerplaat kelder', N'VLOERPLAAT_KELDER', NULL, 1, 0, N'kelder', N'ja');
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 3, N'Gieten vloerplaat {verdieping}', N'VLOERPLAAT', NULL, 4, 1, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 4, N'Plaatsen dakbedekking (plat dak)', N'DAKBEDEKKING_PLAT', NULL, 1, 0, N'dak', N'plat');
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 5, N'Plaatsen dakbedekking (hellend dak)', N'DAKBEDEKKING_HELLEND', NULL, 1, 0, N'dak', N'hellend');
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 6, N'Plaatsen ramen en glas', N'RAMEN', NULL, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 7, N'Plaatsen leidingen technieken', N'LEIDINGEN', 5, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 8, N'Plaatsen chape', N'CHAPE', 5, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 9, N'Plaatsen keuken', N'KEUKEN', 5, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 10, N'Bij de voorlopige oplevering (ontvangst van de sleutels)', N'VOORLOPIGE_OPLEVERING', 5, 1, 0, NULL, NULL);
    PRINT N'Schijvensjabloon aangemaakt: TEST Appartement (met verdiepingen)';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM SchijfSjabloon WHERE Naam = N'TEST Woning')
BEGIN
    INSERT INTO SchijfSjabloon (Naam, Beschrijving, MinimumPercentage, IsActief) VALUES (N'TEST Woning', N'Vaste percentages voor een woning (uitzetten 5 %, vloerplaat 20 %, ...).', 5, 1);
    SET @s = SCOPE_IDENTITY();
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 1, N'Bij het uitzetten van de woning', N'UITZETTEN', 5, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 2, N'Bij het gieten van de vloerplaat van de gelijkvloerse verdieping', N'VLOERPLAAT_GLVL', 20, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 3, N'Bij het gieten van de vloerplaat van de eerste verdieping', N'VLOERPLAAT_V1', 15, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 4, N'Bij het gieten van de dakplaat boven de eerste verdieping', N'DAKPLAAT', 10, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 5, N'Bij het plaatsen van de dakbedekking', N'DAKBEDEKKING', 10, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 6, N'Bij het plaatsen van de ramen', N'RAMEN', 10, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 7, N'Bij het plaatsen van de leidingen en technieken', N'LEIDINGEN', 5, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 8, N'Bij het plaatsen van het pleisterwerk op de muren', N'PLEISTER', 5, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 9, N'Bij het plaatsen van de chape', N'CHAPE', 10, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 10, N'Bij de plaatsen van de keuken', N'KEUKEN', 5, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 11, N'Bij de voorlopige oplevering (ontvangst van de sleutels)', N'VOORLOPIGE_OPLEVERING', 5, 1, 0, NULL, NULL);
    PRINT N'Schijvensjabloon aangemaakt: TEST Woning';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM SchijfSjabloon WHERE Naam = N'TEST Ondergrondse garage, parking of bovengrondse berging')
BEGIN
    INSERT INTO SchijfSjabloon (Naam, Beschrijving, MinimumPercentage, IsActief) VALUES (N'TEST Ondergrondse garage, parking of bovengrondse berging', N'10 / 40 / 25 / 20 / 5 %.', 5, 1);
    SET @s = SCOPE_IDENTITY();
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 1, N'Bij het afbreken van de bestaande gebouwen', NULL, 10, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 2, N'Bij het gieten van de vloerplaat kelder', NULL, 40, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 3, N'Bij het gieten van de vloerplaat gelijkvloers', NULL, 25, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 4, N'Bij het plaatsen van de dakbedekking', NULL, 20, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 5, N'Bij de voorlopige oplevering (ontvangst van de sleutels)', NULL, 5, 1, 0, NULL, NULL);
    PRINT N'Schijvensjabloon aangemaakt: TEST Ondergrondse garage, parking of bovengrondse berging';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM SchijfSjabloon WHERE Naam = N'TEST Bovengrondse parkeerplaats')
BEGIN
    INSERT INTO SchijfSjabloon (Naam, Beschrijving, MinimumPercentage, IsActief) VALUES (N'TEST Bovengrondse parkeerplaats', N'10 / 50 / 35 / 5 %.', 5, 1);
    SET @s = SCOPE_IDENTITY();
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 1, N'Bij het afbreken van de bestaande gebouwen', NULL, 10, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 2, N'Bij het aanvullen van de bouwput', NULL, 50, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 3, N'Bij de aanleg van de parkeerplaatsen', NULL, 35, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 4, N'Bij de voorlopige oplevering (ontvangst van de sleutels)', NULL, 5, 1, 0, NULL, NULL);
    PRINT N'Schijvensjabloon aangemaakt: TEST Bovengrondse parkeerplaats';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM SchijfSjabloon WHERE Naam = N'TEST Carport')
BEGIN
    INSERT INTO SchijfSjabloon (Naam, Beschrijving, MinimumPercentage, IsActief) VALUES (N'TEST Carport', N'10 / 30 / 20 / 35 / 5 %.', 5, 1);
    SET @s = SCOPE_IDENTITY();
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 1, N'Bij het afbreken van de bestaande gebouwen', NULL, 10, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 2, N'Bij het uitvoeren van de fundering van de verharding', NULL, 30, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 3, N'Bij de aanleg van de verharding', NULL, 20, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 4, N'Bij het plaatsen van de carports', NULL, 35, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 5, N'Bij de voorlopige oplevering (ontvangst van de sleutels)', NULL, 5, 1, 0, NULL, NULL);
    PRINT N'Schijvensjabloon aangemaakt: TEST Carport';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM SchijfSjabloon WHERE Naam = N'TEST Parking (verharding)')
BEGIN
    INSERT INTO SchijfSjabloon (Naam, Beschrijving, MinimumPercentage, IsActief) VALUES (N'TEST Parking (verharding)', N'10 / 30 / 55 / 5 %.', 5, 1);
    SET @s = SCOPE_IDENTITY();
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 1, N'Bij het afbreken van de bestaande gebouwen', NULL, 10, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 2, N'Bij het uitvoeren van de fundering van de verharding', NULL, 30, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 3, N'Bij de aanleg van de verharding', NULL, 55, 1, 0, NULL, NULL);
    INSERT INTO SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde) VALUES (@s, 4, N'Bij de voorlopige oplevering (ontvangst van de sleutels)', NULL, 5, 1, 0, NULL, NULL);
    PRINT N'Schijvensjabloon aangemaakt: TEST Parking (verharding)';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM ProjectSjabloon WHERE Naam = N'TEST Appartementsgebouw')
BEGIN
    INSERT INTO ProjectSjabloon (Naam, Beschrijving, PlanningSjabloonId, TrajectSjabloonId, SchijfSjabloonId, StandaardVerdiepingen, StandaardKelder, StandaardDak, IsActief)
    VALUES (N'TEST Appartementsgebouw', N'Appartementsgebouw met kelder en plat dak.',
        (SELECT TOP 1 Id FROM PlanningSjabloon WHERE Naam = N'TEST Appartementsgebouw (naar Res. Moleken)'),
        (SELECT TOP 1 Id FROM TrajectSjabloon WHERE Naam = N'TEST Bouwtraject (mijlpalen volgen de planning)'),
        (SELECT TOP 1 Id FROM SchijfSjabloon WHERE Naam = N'TEST Appartement (met verdiepingen)'),
        3, 1, N'plat', 1);
    PRINT N'Projectsjabloon aangemaakt: TEST Appartementsgebouw';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

IF NOT EXISTS (SELECT 1 FROM ProjectSjabloon WHERE Naam = N'TEST Woning')
BEGIN
    INSERT INTO ProjectSjabloon (Naam, Beschrijving, PlanningSjabloonId, TrajectSjabloonId, SchijfSjabloonId, StandaardVerdiepingen, StandaardKelder, StandaardDak, IsActief)
    VALUES (N'TEST Woning', N'Rijwoning of losse woning.',
        (SELECT TOP 1 Id FROM PlanningSjabloon WHERE Naam = N'TEST Woning (naar Koksijde en RDR)'),
        (SELECT TOP 1 Id FROM TrajectSjabloon WHERE Naam = N'TEST Bouwtraject (mijlpalen volgen de planning)'),
        (SELECT TOP 1 Id FROM SchijfSjabloon WHERE Naam = N'TEST Woning'),
        1, 0, NULL, 1);
    PRINT N'Projectsjabloon aangemaakt: TEST Woning';
END
GO
DECLARE @s INT, @b INT, @t INT, @f INT, @vgl INT;

PRINT N'Klaar. Controle:';
SELECT 'Planning' AS Soort, Naam FROM PlanningSjabloon WHERE Naam LIKE N'TEST %'
UNION ALL SELECT 'Traject', Naam FROM TrajectSjabloon WHERE Naam LIKE N'TEST %'
UNION ALL SELECT 'Schijven', Naam FROM SchijfSjabloon WHERE Naam LIKE N'TEST %'
UNION ALL SELECT 'Project', Naam FROM ProjectSjabloon WHERE Naam LIKE N'TEST %';
