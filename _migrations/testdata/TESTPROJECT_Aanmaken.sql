-- ============================================================================================
-- TESTPROJECT_Aanmaken.sql  (GEEN genummerde migratie: enkel testgegevens, nooit op productie draaien)
-- Maakt een volledig opgebouwd, realistisch testproject met fictieve maar geloofwaardige gegevens.
--
-- TWEE VARIANTEN (zet @Variant hieronder):
--   1 = "Residentie Hofstede"    appartementsgebouw in Kortrijk: 18 appartementen, 14 autostaanplaatsen, 18 bergingen, 5 bouwlagen
--   2 = "Verkaveling Wilgenhoek" verkaveling in Zulte: 14 loten met eengezinswoningen (halfopen en open bebouwing)
--   3 = "Verkaveling Berkenhof"   zelfde opbouw als 2 (14 loten), bedoeld voor het projectsjabloon "Verkaveling zonder wegenis" (code TP-VERK-02)
--
-- WAT HET AANMAAKT (per project):
--   * partijen als aparte bedrijven: ontwikkelaar, architect, stabiliteitsingenieur, veiligheidscoördinator, EPB-verslaggever, notaris + aannemers
--   * het project (in uitvoering, geen publicatie, GEEN toegangsrijen)  -> ENKEL BEHEERDERS ZIEN HET (zie hieronder)
--   * bouwlagen, eenheden (met oppervlaktes, grondaandeel, bouw- en grondwaarde), klanten (kopers), mede-eigenaars
--   * betalingsgroepen met schijven (de eerste schijven zijn al factureerbaar) en bouwwaarden per eenheid
--   * budget, aannemerscontracten met activiteiten en prijzen
--   * hoofdplanning met fasen en taken (sommige afgerond, één lopend, rest gepland; alles relatief ten opzichte van vandaag)
--   * verkoopinstellingen
--
-- ZICHTBAARHEID: een beheerder ziet alle projecten (SecurityService.GetAllowedProjectIds); iedere andere gebruiker enkel projecten met
-- een rij in ProjectUserAccess. Dit script maakt NOOIT zo'n rij aan, dus het project is enkel voor beheerders zichtbaar.
-- Geef je het project ooit aan een gebruiker, dan is dat jouw bewuste keuze via Instellingen.
--
-- HERKENNING/OPRUIMEN: ProjectCode begint met "TP-" (TP-APP-01, TP-VERK-01); de partijen dragen in Opmerkingen "TESTPROJECT-DATA <code>".
-- Verwijderen: TESTPROJECT_Verwijderen.sql.
--
-- UITBREIDEN: elk blok hieronder heeft een eigen kop "-- [N] ..." en gebruikt @ProjectId. Voeg nieuwe blokken toe vóór "-- EINDE"
-- (bv. verzekeringen, borgen, facturen, wijzigingsopdrachten, documenten) en neem ze op in TESTPROJECT_Verwijderen.sql als ze
-- niet automatisch meeverdwijnen (alles dat via een verplichte verwijzing aan het project of een eenheid hangt verdwijnt vanzelf).
--
-- HOE: eerst @Uitvoeren = 0 (proefrit, alles wordt teruggedraaid, je ziet de aantallen), daarna @Uitvoeren = 1.
-- Herhaalbaar: bestaat de projectcode al, dan doet het script niets.
-- Referentiegegevens (postcodes, eenheidstypes, activiteiten, projectstatus, uitgevende firma) worden op NAAM opgezocht in jouw
-- database; wat niet gevonden wordt komt in de uitvoer als "niet gevonden" en wordt overgeslagen of vervangen door een bestaande rij.
-- E-mailadressen eindigen op @example.com (gereserveerd domein): er kan nooit echt mail naar buiten gaan.
-- ============================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Variant  INT = 1;   -- <<< 1 = appartementsgebouw, 2 = verkaveling (met wegenis), 3 = verkaveling (zonder wegenis)
DECLARE @Uitvoeren BIT = 0;  -- <<< 0 = proefrit, 1 = echt bewaren

DECLARE @code NVARCHAR(30)  = CASE @Variant WHEN 1 THEN N'TP-APP-01' WHEN 3 THEN N'TP-VERK-02' ELSE N'TP-VERK-01' END;
DECLARE @marker NVARCHAR(100) = N'TESTPROJECT-DATA ' + @code;
DECLARE @vandaag DATE = CAST(GETDATE() AS DATE);

IF @Variant NOT IN (1, 2, 3) THROW 50020, N'@Variant moet 1, 2 of 3 zijn.', 1;
IF EXISTS (SELECT 1 FROM dbo.Project WHERE ProjectCode = @code)
BEGIN PRINT N'Project ' + @code + N' bestaat al, niets gedaan. Verwijder het eerst met TESTPROJECT_Verwijderen.sql.'; RETURN; END

BEGIN TRAN;

-- ---------------------------------------------------------------------------------------------
-- [0] Referentiegegevens opzoeken
-- ---------------------------------------------------------------------------------------------
DECLARE @gemeente NVARCHAR(50) = CASE @Variant WHEN 1 THEN N'Kortrijk' WHEN 3 THEN N'Deinze' ELSE N'Zulte' END;
DECLARE @postcode NVARCHAR(10) = CASE @Variant WHEN 1 THEN N'8500'     WHEN 3 THEN N'9800' ELSE N'9870'  END;
DECLARE @pcId INT = (SELECT TOP 1 PostcodeId FROM dbo.PostalCode WHERE Postcode = @postcode AND Gemeente LIKE @gemeente + N'%' ORDER BY PostcodeId);
IF @pcId IS NULL
BEGIN
    SET @pcId = (SELECT TOP 1 PostcodeId FROM dbo.PostalCode WHERE Postcode = @postcode ORDER BY PostcodeId);
    IF @pcId IS NULL BEGIN SET @pcId = (SELECT TOP 1 PostcodeId FROM dbo.PostalCode ORDER BY PostcodeId); PRINT N'   niet gevonden: postcode ' + @postcode + N' (er wordt een andere gebruikt)'; END
END

DECLARE @statusId INT = (SELECT TOP 1 StatusId FROM dbo.ProjectStatus WHERE StatusName LIKE N'%itvoering%' ORDER BY StatusId);
IF @statusId IS NULL BEGIN SET @statusId = (SELECT TOP 1 StatusId FROM dbo.ProjectStatus ORDER BY StatusId); PRINT N'   niet gevonden: projectstatus "Uitvoering"'; END

-- Projecttype en uitgevende firma: gekopieerd van een bestaand project dat erop lijkt (zoals TESTDATA_Financieel.sql)
DECLARE @donor INT = (SELECT TOP 1 ProjectId FROM dbo.Project
                      WHERE ProjectCode IS NULL OR ProjectCode NOT LIKE N'TP-%'
                      ORDER BY CASE WHEN @Variant = 1 AND ProjectName LIKE N'%Res%' THEN 0
                                    WHEN @Variant IN (2, 3) AND ProjectName LIKE N'%erkaveling%' THEN 0 ELSE 1 END, ProjectId DESC);
DECLARE @projType INT = ISNULL((SELECT ProjectType FROM dbo.Project WHERE ProjectId = @donor), 1);
DECLARE @issuer   INT = (SELECT TOP 1 IssuerCompanyIdBuilder FROM dbo.Project WHERE IssuerCompanyIdBuilder IS NOT NULL ORDER BY ProjectId DESC);
IF @issuer IS NULL SET @issuer = (SELECT TOP 1 Id FROM dbo.IssuerCompany ORDER BY Id);

-- Eenheidstypes op naam
DECLARE @tApp INT  = (SELECT TOP 1 Id FROM dbo.UnitTypes WHERE Name LIKE N'%partement%' ORDER BY Id);
DECLARE @tPark INT = (SELECT TOP 1 Id FROM dbo.UnitTypes WHERE Name LIKE N'%staanplaats%' OR Name LIKE N'%parking%' OR Name LIKE N'%garage%' ORDER BY Id);
DECLARE @tBerg INT = (SELECT TOP 1 Id FROM dbo.UnitTypes WHERE Name LIKE N'%erging%' ORDER BY Id);
DECLARE @tLot INT  = (SELECT TOP 1 Id FROM dbo.UnitTypes WHERE Name LIKE N'Lot%' OR Name LIKE N'%woning%' ORDER BY CASE WHEN Name LIKE N'Lot%' THEN 0 ELSE 1 END, Id);
DECLARE @tAny INT  = (SELECT TOP 1 Id FROM dbo.UnitTypes ORDER BY Id);
IF @tApp  IS NULL BEGIN SET @tApp  = @tAny; PRINT N'   niet gevonden: eenheidstype appartement'; END
IF @tPark IS NULL BEGIN SET @tPark = @tAny; PRINT N'   niet gevonden: eenheidstype parking/garage'; END
IF @tBerg IS NULL BEGIN SET @tBerg = @tAny; PRINT N'   niet gevonden: eenheidstype berging'; END
IF @tLot  IS NULL BEGIN SET @tLot  = @tAny; PRINT N'   niet gevonden: eenheidstype lot/woning'; END

-- Activiteiten op naam (de naam moet een deel van de omschrijving zijn)
DECLARE @act TABLE (Code NVARCHAR(10) PRIMARY KEY, Zoek NVARCHAR(60), Id INT NULL);
INSERT INTO @act (Code, Zoek) VALUES
 (N'RUW', N'%uwbouw%'), (N'DAK', N'%dakwerk%'), (N'ELEK', N'%lektri%'), (N'SANI', N'%anitair%'), (N'ARCH', N'%rchitect%'),
 (N'STAB', N'%tabiliteit%'), (N'PLEI', N'%leister%'), (N'SCHR', N'%chrijnwerk%'), (N'TEGEL', N'%egel%'), (N'LIFT', N'%ift%'), (N'VEIL', N'%eiligheid%');
UPDATE a SET a.Id = (SELECT TOP 1 x.ActivityId FROM dbo.Activity x WHERE x.Omschrijving LIKE a.Zoek ORDER BY x.ActivityId) FROM @act a;
DECLARE @nietGevonden NVARCHAR(500) = (SELECT STRING_AGG(Code, N', ') FROM @act WHERE Id IS NULL);
IF @nietGevonden IS NOT NULL PRINT N'   activiteiten niet gevonden (contractlijnen en budget daarvoor worden overgeslagen): ' + @nietGevonden;

-- ---------------------------------------------------------------------------------------------
-- [1] Partijen (bedrijven)
-- ---------------------------------------------------------------------------------------------
DECLARE @bed TABLE (Rol NVARCHAR(10) PRIMARY KEY, Naam NVARCHAR(200), Vorm NVARCHAR(10), Straat NVARCHAR(100), Nr NVARCHAR(10), Ond NVARCHAR(20), Id INT NULL);
IF @Variant = 1
    INSERT INTO @bed (Rol, Naam, Vorm, Straat, Nr, Ond) VALUES
     (N'DEV',  N'Hofstede Vastgoed',                 N'BV', N'Doorniksewijk', N'112', N'0782.451.903'),
     (N'ARCH', N'Architectenbureau Delrue & Verstraete', N'BV', N'Minister Tacklaan', N'34', N'0745.118.226'),
     (N'STAB', N'Studiebureau Deprez Stabiliteit',   N'BV', N'Kortrijksesteenweg', N'201', N'0731.664.087'),
     (N'VEIL', N'Veilig Bouwen Coördinatie',         N'BV', N'Pottelberg', N'18', N'0803.290.511'),
     (N'EPB',  N'EPB Verslaggeving Maes',            N'BV', N'Gentsesteenweg', N'77', N'0689.742.360'),
     (N'NOT',  N'Notariaat Dhondt & Claeys',         N'BV', N'Grote Markt', N'9', N'0541.870.226'),
     (N'RUW',  N'Bouwwerken Lootens',                N'NV', N'Industrielaan', N'45', N'0412.903.668'),
     (N'DAK',  N'Dakwerken Vanhove',                 N'BV', N'Heirweg', N'61', N'0768.205.119'),
     (N'ELEK', N'Elektro Cools',                     N'BV', N'Bruggestraat', N'140', N'0725.381.904'),
     (N'SANI', N'Technieken Verhaeghe',              N'BV', N'Zwevegemsestraat', N'92', N'0754.099.317'),
     (N'PLEI', N'Pleisterwerken Bossuyt',            N'BV', N'Kerkstraat', N'23', N'0792.516.440'),
     (N'SCHR', N'Schrijnwerkerij Demeyer',           N'BV', N'Ambachtenlaan', N'8', N'0714.862.075'),
     (N'TEGEL',N'Tegel en Vloer Pauwels',            N'BV', N'Meensesteenweg', N'305', N'0738.427.691'),
     (N'LIFT', N'Liftservice Claerhout',             N'NV', N'Rijselsestraat', N'54', N'0455.610.382');
ELSE
    INSERT INTO @bed (Rol, Naam, Vorm, Straat, Nr, Ond) VALUES
     (N'DEV',  N'Wilgenhoek Projecten',              N'BV', N'Dorp', N'14', N'0794.337.152'),
     (N'ARCH', N'Architectuur Van Hecke & Partners', N'BV', N'Stationsstraat', N'52', N'0728.904.463'),
     (N'STAB', N'Ingenieursbureau Callewaert',       N'BV', N'Gentsesteenweg', N'330', N'0715.668.201'),
     (N'VEIL', N'Coördinatie Werven Dumon',          N'BV', N'Heidestraat', N'7', N'0779.145.836'),
     (N'EPB',  N'Energieverslag Ponnet',             N'BV', N'Kasteeldreef', N'3', N'0701.256.944'),
     (N'NOT',  N'Notaris Vermeersch & Goethals',     N'BV', N'Markt', N'21', N'0563.118.407'),
     (N'RUW',  N'Bouwbedrijf Seynaeve',              N'NV', N'Nijverheidsstraat', N'28', N'0405.771.239'),
     (N'DAK',  N'Daktechniek De Backer',             N'BV', N'Mandelstraat', N'66', N'0749.320.585'),
     (N'ELEK', N'Elektriciteit Rogiers',             N'BV', N'Oudenaardsesteenweg', N'118', N'0762.884.017'),
     (N'SANI', N'Sanitair en Verwarming Dewulf',     N'BV', N'Molenstraat', N'40', N'0736.509.662'),
     (N'PLEI', N'Pleister- en Stukadoorswerk Willems',N'BV', N'Leiestraat', N'12', N'0781.047.298'),
     (N'SCHR', N'Houtbouw en Schrijnwerk Lammens',   N'BV', N'Ambachtsstraat', N'5', N'0707.693.144'),
     (N'TEGEL',N'Vloeren Tegelzetterij Hoste',       N'BV', N'Kortrijkstraat', N'88', N'0743.215.870');

-- Variant 3 deelt de partijnamen met variant 2, maar het ondernemingsnummer is uniek: eerste cijfer 0 wordt 1.
IF @Variant = 3 UPDATE @bed SET Ond = N'1' + SUBSTRING(Ond, 2, 20);

INSERT INTO dbo.CompanyInfo (BedrijfsNaam, Straat, Huisnummer, Telefoon1, Email, Ondernemingsnummer, VatNumber, Type, LandCode, PostCodeId, Postcode, Gemeente, IsActive, Opmerkingen)
SELECT b.Naam + N' ' + b.Vorm, b.Straat, b.Nr, N'056 ' + RIGHT(N'000000' + CAST(ABS(CHECKSUM(b.Naam)) % 1000000 AS NVARCHAR(10)), 6),
       N'info@' + LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(b.Naam, N' ', N''), N'&', N''), N'ë', N'e'), N'ö', N'o'), N'-', N'')) + N'.example.com',
       b.Ond, N'BE' + REPLACE(b.Ond, N'.', N''), b.Vorm, N'BE', @pcId, @postcode, @gemeente, 1, @marker
FROM @bed b;
UPDATE b SET b.Id = c.CompanyId FROM @bed b JOIN dbo.CompanyInfo c ON c.BedrijfsNaam = b.Naam + N' ' + b.Vorm AND c.Opmerkingen = @marker;
DECLARE @t1 INT, @t2 INT;   -- tellers voor de PRINT-regels (een subquery mag niet rechtstreeks in PRINT)
SET @t1 = (SELECT COUNT(*) FROM @bed);
PRINT N'[1] partijen aangemaakt: ' + CAST(@t1 AS NVARCHAR(10));

-- ---------------------------------------------------------------------------------------------
-- [2] Project
-- ---------------------------------------------------------------------------------------------
DECLARE @naam NVARCHAR(100)  = CASE @Variant WHEN 1 THEN N'Residentie Hofstede' WHEN 3 THEN N'Verkaveling Berkenhof' ELSE N'Verkaveling Wilgenhoek' END;
DECLARE @slug NVARCHAR(100)  = CASE @Variant WHEN 1 THEN N'residentie-hofstede' WHEN 3 THEN N'verkaveling-berkenhof' ELSE N'verkaveling-wilgenhoek' END;
DECLARE @straat NVARCHAR(100)= CASE @Variant WHEN 1 THEN N'Hofstedestraat' WHEN 3 THEN N'Berkenlaan' ELSE N'Wilgenhoekstraat' END;
DECLARE @huisnr NVARCHAR(10) = CASE @Variant WHEN 1 THEN N'24' ELSE N'2' END;
DECLARE @start DATE = CASE @Variant WHEN 1 THEN DATEADD(MONTH, -9, @vandaag) ELSE DATEADD(MONTH, -5, @vandaag) END;
-- begin op een maandag
SET @start = DATEADD(DAY, -(DATEDIFF(DAY, '19000101', @start) % 7), @start);
DECLARE @uitvoerdagen INT = CASE @Variant WHEN 1 THEN 420 ELSE 360 END;
DECLARE @voorlopig DATE = DATEADD(DAY, CAST(@uitvoerdagen * 7.0 / 5 AS INT), @start);

DECLARE @ProjectId INT;
INSERT INTO dbo.Project (ProjectName, Slug, PostalCodeID, Street, Number, StatusID, ProjectType, ProjectCode,
                         CommercialTitleNl, CommercialTextNl,
                         StartDateConstruction, DeliveryDate, DeliveryDateDef, ExecutionDays, TotalLandShare,
                         DeveloperId, ArchitectId, EngineerId, SecurityCoordinatorId, EpbReporterId, NotaryCompanyId, BuilderId,
                         IssuerCompanyIdBuilder, IssuerCompanyIdLandOwner,
                         WerfmeldingDate, WerfmeldingDossier,
                         IsCoordinationProject, IsOnlyCoordinationProject, IsPublished)
SELECT @naam, @slug, @pcId, @straat, @huisnr, @statusId, @projType, @code,
       CASE @Variant WHEN 1 THEN N'Residentie Hofstede: 18 energiezuinige appartementen in het hart van Kortrijk'
                     ELSE N'Wilgenhoek: 14 kavels voor open en halfopen woningen aan de rand van Zulte' END,
       CASE @Variant WHEN 1 THEN N'Afwerking naar keuze, ondergrondse parking, berging per appartement en een gemeenschappelijke tuin.'
                     ELSE N'Ruime kavels tussen 320 en 640 m² in een rustige, volledig uitgeruste verkaveling met bouwplicht binnen drie jaar.' END,
       @start, @voorlopig, DATEADD(YEAR, 1, @voorlopig), @uitvoerdagen, 1000,
       (SELECT Id FROM @bed WHERE Rol = N'DEV'), (SELECT Id FROM @bed WHERE Rol = N'ARCH'), (SELECT Id FROM @bed WHERE Rol = N'STAB'),
       (SELECT Id FROM @bed WHERE Rol = N'VEIL'), (SELECT Id FROM @bed WHERE Rol = N'EPB'), (SELECT Id FROM @bed WHERE Rol = N'NOT'),
       (SELECT Id FROM @bed WHERE Rol = N'RUW'),
       @issuer, @issuer,
       DATEADD(DAY, -14, @start), CASE @Variant WHEN 1 THEN N'WM-2026-004217' ELSE N'WM-2026-011058' END,
       0, 0, 0;
SET @ProjectId = SCOPE_IDENTITY();
PRINT N'[2] project aangemaakt: ' + @naam + N' (ProjectId ' + CAST(@ProjectId AS NVARCHAR(10)) + N', ' + @code + N')';
-- Bewust GEEN rij in ProjectUserAccess: enkel beheerders zien dit project.

-- ---------------------------------------------------------------------------------------------
-- [3] Bouwlagen en eenheden
-- ---------------------------------------------------------------------------------------------
DECLARE @lvl TABLE (Nr INT, Naam NVARCHAR(50), Id INT NULL);
IF @Variant = 1 INSERT INTO @lvl (Nr, Naam) VALUES (-1, N'Ondergrond'), (0, N'Gelijkvloers'), (1, N'Eerste verdieping'), (2, N'Tweede verdieping'), (3, N'Derde verdieping');
ELSE            INSERT INTO @lvl (Nr, Naam) VALUES (0, N'Gelijkvloers'), (1, N'Verdieping');
INSERT INTO dbo.ProjectLevels (Level, ProjectId, Number) SELECT Naam, @ProjectId, Nr FROM @lvl;
UPDATE l SET l.Id = pl.Id FROM @lvl l JOIN dbo.ProjectLevels pl ON pl.ProjectId = @ProjectId AND pl.Number = l.Nr;

DECLARE @u TABLE (Seq INT IDENTITY(1,1), Naam NVARCHAR(100), Soort CHAR(1), Nr INT, Verd INT, Bus NVARCHAR(10), Huisnr NVARCHAR(10),
                  M2 DECIMAL(9,2), Grond DECIMAL(9,2), Aandeel DECIMAL(9,4), Bouw DECIMAL(12,2), Land DECIMAL(12,2), Koper INT NULL);
-- Soort: A = appartement, P = parking, B = berging, W = woning op lot
DECLARE @i INT, @n INT, @v INT, @m2 DECIMAL(9,2);
IF @Variant = 1
BEGIN
    DECLARE @perVerd TABLE (Verd INT, Aantal INT);
    INSERT INTO @perVerd VALUES (0, 4), (1, 5), (2, 5), (3, 4);
    SET @i = 0;
    DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Verd, Aantal FROM @perVerd ORDER BY Verd;
    OPEN c; FETCH NEXT FROM c INTO @v, @n;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @k INT = 1;
        WHILE @k <= @n
        BEGIN
            SET @i += 1;
            SET @m2 = 62 + ((@k * 7 + @v * 5) % 40);
            INSERT INTO @u (Naam, Soort, Nr, Verd, Bus, Huisnr, M2, Aandeel, Bouw, Land, Koper)
            VALUES (N'Appartement ' + CAST(@v AS NVARCHAR(2)) + N'.' + RIGHT(N'0' + CAST(@k AS NVARCHAR(2)), 2), 'A', @i, @v,
                    CAST(@v AS NVARCHAR(2)) + RIGHT(N'0' + CAST(@k AS NVARCHAR(2)), 2), @huisnr,
                    @m2, 48.0, ROUND(@m2 * (2800 + @v * 60), -2), ROUND(@m2 * 850, -2), CASE WHEN @i <= 12 THEN @i END);
            SET @k += 1;
        END
        FETCH NEXT FROM c INTO @v, @n;
    END
    CLOSE c; DEALLOCATE c;
    SET @i = 1;
    WHILE @i <= 14
    BEGIN
        INSERT INTO @u (Naam, Soort, Nr, Verd, Bus, Huisnr, M2, Aandeel, Bouw, Land, Koper)
        VALUES (N'Autostaanplaats P' + RIGHT(N'0' + CAST(@i AS NVARCHAR(2)), 2), 'P', @i, -1, N'P' + RIGHT(N'0' + CAST(@i AS NVARCHAR(2)), 2), @huisnr, 12.5, 6.0, 22000, 3000, CASE WHEN @i <= 12 THEN @i END);
        SET @i += 1;
    END
    SET @i = 1;
    WHILE @i <= 18
    BEGIN
        INSERT INTO @u (Naam, Soort, Nr, Verd, Bus, Huisnr, M2, Aandeel, Bouw, Land, Koper)
        VALUES (N'Berging B' + RIGHT(N'0' + CAST(@i AS NVARCHAR(2)), 2), 'B', @i, -1, N'B' + RIGHT(N'0' + CAST(@i AS NVARCHAR(2)), 2), @huisnr, 5.0 + (@i % 4), 2.89, 7000, 1000, CASE WHEN @i <= 12 THEN @i END);
        SET @i += 1;
    END
END
ELSE
BEGIN
    SET @i = 1;
    WHILE @i <= 14
    BEGIN
        DECLARE @grond DECIMAL(9,2) = 320 + ((@i * 37) % 9) * 40;      -- 320 tot 640 m²
        INSERT INTO @u (Naam, Soort, Nr, Verd, Bus, Huisnr, M2, Grond, Aandeel, Bouw, Land, Koper)
        VALUES (N'Lot ' + RIGHT(N'0' + CAST(@i AS NVARCHAR(2)), 2), 'W', @i, 0, NULL, CAST(@i * 2 AS NVARCHAR(10)),
                CASE WHEN @grond >= 480 THEN 185 ELSE 165 END, @grond, ROUND(1000.0 / 14, 4),
                CASE WHEN @grond >= 480 THEN 335000 ELSE 295000 END, ROUND(@grond * 410, -3), CASE WHEN @i <= 8 THEN @i END);
        SET @i += 1;
    END
END

INSERT INTO dbo.Units (Name, TypeId, ProjectId, LevelId, Level, Street, Housenumber, Busnumber, Surface, GroundSurface, Landshare,
                       ConstructionValue, LandValue, IsLink, IsOption)
SELECT u.Naam, CASE u.Soort WHEN 'A' THEN @tApp WHEN 'P' THEN @tPark WHEN 'B' THEN @tBerg ELSE @tLot END, @ProjectId,
       (SELECT l.Id FROM @lvl l WHERE l.Nr = u.Verd), u.Verd, @straat, u.Huisnr, u.Bus, u.M2, u.Grond, u.Aandeel, u.Bouw, u.Land, 0, 0
FROM @u u ORDER BY u.Seq;
SET @t1 = (SELECT COUNT(*) FROM dbo.Units WHERE ProjectId = @ProjectId);
PRINT N'[3] eenheden aangemaakt: ' + CAST(@t1 AS NVARCHAR(10));

-- ---------------------------------------------------------------------------------------------
-- [4] Klanten (kopers) en mede-eigenaars
-- ---------------------------------------------------------------------------------------------
DECLARE @kl TABLE (Nr INT PRIMARY KEY, Voor NVARCHAR(50), Naam NVARCHAR(80), Partner NVARCHAR(50), Straat NVARCHAR(80), Hnr NVARCHAR(10), Aanc INT NULL);
INSERT INTO @kl (Nr, Voor, Naam, Partner, Straat, Hnr) VALUES
 (1,  N'Wouter',  N'Vandenbussche', N'Lien',    N'Sint-Amandsstraat', N'12'),
 (2,  N'Hanne',   N'Decock',        NULL,       N'Pater Damiaanstraat', N'4'),
 (3,  N'Bram',    N'Vermeulen',     N'Elke',    N'Koning Albertstraat', N'31'),
 (4,  N'Sofie',   N'Declercq',      NULL,       N'Zandstraat', N'88'),
 (5,  N'Thomas',  N'Maertens',      N'Katrien', N'Lindestraat', N'7'),
 (6,  N'Joke',    N'Hoste',         NULL,       N'Beukenlaan', N'15'),
 (7,  N'Pieter',  N'Van Damme',     N'Charlotte', N'Hoogstraat', N'102'),
 (8,  N'Evelien', N'Wyckaert',      NULL,       N'Meersstraat', N'9'),
 (9,  N'Dries',   N'Lambrecht',     N'Nele',    N'Kerkhofstraat', N'21'),
 (10, N'Marie',   N'Goethals',      NULL,       N'Wijngaardstraat', N'3'),
 (11, N'Jonas',   N'Verbeke',       N'Silke',   N'Molenweg', N'56'),
 (12, N'Annelies',N'Standaert',     NULL,       N'Vlasmarkt', N'18');

DECLARE @kNr INT, @kVoor NVARCHAR(50), @kNaam NVARCHAR(80), @kPartner NVARCHAR(50), @kStraat NVARCHAR(80), @kHnr NVARCHAR(10), @aid INT;
DECLARE ck CURSOR LOCAL FAST_FORWARD FOR SELECT Nr, Voor, Naam, Partner, Straat, Hnr FROM @kl WHERE Nr IN (SELECT DISTINCT Koper FROM @u WHERE Koper IS NOT NULL) ORDER BY Nr;
OPEN ck; FETCH NEXT FROM ck INTO @kNr, @kVoor, @kNaam, @kPartner, @kStraat, @kHnr;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.ClientAccount (Name, Forename, Salutation, Street, Housenumber, PostalCodeID, DateSalesAgreement, DateDeedOfSale, OwnerTypeId, Email)
    VALUES (@kNaam, @kVoor, CASE WHEN @kVoor IN (N'Hanne', N'Sofie', N'Joke', N'Evelien', N'Marie', N'Annelies') THEN N'Mevr' ELSE N'Dhr' END,
            @kStraat, @kHnr, @pcId,
            DATEADD(DAY, -(40 + @kNr * 17), @vandaag),
            CASE WHEN @kNr <= 8 THEN DATEADD(DAY, -(40 + @kNr * 17) + 45, @vandaag) END,
            4, LOWER(@kVoor + N'.' + REPLACE(@kNaam, N' ', N'')) + N'@example.com');
    SET @aid = SCOPE_IDENTITY();
    UPDATE @kl SET Aanc = @aid WHERE Nr = @kNr;
    IF @kPartner IS NOT NULL
        INSERT INTO dbo.ClientContacts (Name, Forename, Salutation, IsCoOwner, CoOwnerPercentage, ClientAccountId, Email)
        VALUES (@kNaam, @kPartner, N'Mevr', 1, 50, @aid, LOWER(@kPartner + N'.' + REPLACE(@kNaam, N' ', N'')) + N'@example.com');
    FETCH NEXT FROM ck INTO @kNr, @kVoor, @kNaam, @kPartner, @kStraat, @kHnr;
END
CLOSE ck; DEALLOCATE ck;

-- Eenheden aan hun koper hangen (parking en berging volgen de koper van het appartement met hetzelfde nummer)
UPDATE un SET un.ClientAccountId = k.Aanc, un.ConstructionValueSold = un.ConstructionValue, un.LandValueSold = un.LandValue
FROM dbo.Units un
JOIN @u u ON u.Naam = un.Name
JOIN @kl k ON k.Nr = u.Koper
WHERE un.ProjectId = @ProjectId;
SET @t1 = (SELECT COUNT(*) FROM @kl WHERE Aanc IS NOT NULL);
SET @t2 = (SELECT COUNT(*) FROM dbo.Units WHERE ProjectId = @ProjectId AND ClientAccountId IS NOT NULL);
PRINT N'[4] klanten aangemaakt: ' + CAST(@t1 AS NVARCHAR(10)) + N', verkochte eenheden: ' + CAST(@t2 AS NVARCHAR(10));

-- ---------------------------------------------------------------------------------------------
-- [5] Betalingsgroepen, schijven en bouwwaarden
-- ---------------------------------------------------------------------------------------------
DECLARE @bereikt INT = CASE @Variant WHEN 1 THEN 6 ELSE 3 END;   -- aantal schijven dat al factureerbaar is
DECLARE @gA INT, @gB INT;
INSERT INTO dbo.InvoicingPaymentGroup (Name, ProjectId, VatPercentage) VALUES (CASE @Variant WHEN 1 THEN N'Appartementen' ELSE N'Woningen' END, @ProjectId, 21);
SET @gA = SCOPE_IDENTITY();
DECLARE @st TABLE (Volgnr INT, Naam NVARCHAR(100), Pct DECIMAL(9,3));
IF @Variant = 1
    INSERT INTO @st VALUES (1, N'Bij ondertekening van de verkoopovereenkomst', 5), (2, N'Na het uitzetten en de funderingen', 15), (3, N'Na de vloerplaat van het gelijkvloers', 15),
        (4, N'Na het ruwbouwwerk', 15), (5, N'Na het dichten van het dak', 10), (6, N'Na het plaatsen van het buitenschrijnwerk', 10),
        (7, N'Na de technieken (elektriciteit en sanitair)', 10), (8, N'Na het pleisterwerk', 10), (9, N'Na de tegelwerken', 5), (10, N'Bij de voorlopige oplevering', 5);
ELSE
    INSERT INTO @st VALUES (1, N'Bij ondertekening van de bouwovereenkomst', 5), (2, N'Na het uitzetten en de funderingen', 15), (3, N'Na de vloerplaat', 15),
        (4, N'Na het ruwbouwwerk en het dak', 20), (5, N'Na het buitenschrijnwerk', 10), (6, N'Na de technieken', 15),
        (7, N'Na het pleisterwerk', 10), (8, N'Na de tegelwerken en binnenafwerking', 5), (9, N'Bij de voorlopige oplevering', 5);
INSERT INTO dbo.InvoicingPaymentStages (Name, Percentage, Invoicable, GroupId, VatPercentage)
SELECT Naam, Pct, CASE WHEN Volgnr <= @bereikt THEN 1 ELSE 0 END, @gA, 21 FROM @st ORDER BY Volgnr;

IF @Variant = 1
BEGIN
    INSERT INTO dbo.InvoicingPaymentGroup (Name, ProjectId, VatPercentage) VALUES (N'Parkings en bergingen', @ProjectId, 21);
    SET @gB = SCOPE_IDENTITY();
    INSERT INTO dbo.InvoicingPaymentStages (Name, Percentage, Invoicable, GroupId, VatPercentage) VALUES
        (N'Bij ondertekening', 30, 1, @gB, 21), (N'Na de ruwbouw van de ondergrond', 40, 1, @gB, 21), (N'Bij de voorlopige oplevering', 30, 0, @gB, 21);
END

INSERT INTO dbo.UnitConstructionValue (UnitId, PaymentGroupId, Value, ValueSold)
SELECT un.Id, CASE WHEN u.Soort = 'A' OR u.Soort = 'W' THEN @gA ELSE @gB END, un.ConstructionValue, un.ConstructionValueSold
FROM dbo.Units un JOIN @u u ON u.Naam = un.Name WHERE un.ProjectId = @ProjectId;
SET @t1 = (SELECT COUNT(*) FROM dbo.InvoicingPaymentGroup WHERE ProjectId = @ProjectId);
SET @t2 = (SELECT COUNT(*) FROM dbo.InvoicingPaymentStages s JOIN dbo.InvoicingPaymentGroup g ON g.Id = s.GroupId WHERE g.ProjectId = @ProjectId);
PRINT N'[5] betalingsgroepen: ' + CAST(@t1 AS NVARCHAR(10)) + N', schijven: ' + CAST(@t2 AS NVARCHAR(10));

-- ---------------------------------------------------------------------------------------------
-- [6] Contracten met aannemers en budget
-- ---------------------------------------------------------------------------------------------
DECLARE @ct TABLE (Rol NVARCHAR(10), Naam NVARCHAR(100), Prijs DECIMAL(12,2), Borg DECIMAL(5,2));
IF @Variant = 1
    INSERT INTO @ct VALUES (N'RUW', N'Ruwbouw', 1650000, 5), (N'DAK', N'Dakwerken', 185000, 5), (N'ELEK', N'Elektriciteit', 142000, 0), (N'SANI', N'Sanitair, verwarming en ventilatie', 268000, 0),
        (N'PLEI', N'Pleisterwerken', 112000, 0), (N'SCHR', N'Buitenschrijnwerk', 205000, 0), (N'TEGEL', N'Tegel- en vloerwerken', 96000, 0), (N'LIFT', N'Lift', 54000, 0),
        (N'ARCH', N'Architectuur en bouwdirectie', 198000, 0), (N'STAB', N'Stabiliteit', 42000, 0), (N'VEIL', N'Veiligheidscoördinatie', 18500, 0);
ELSE
    INSERT INTO @ct VALUES (N'RUW', N'Ruwbouw woningen', 2150000, 5), (N'DAK', N'Dakwerken', 310000, 5), (N'ELEK', N'Elektriciteit', 168000, 0), (N'SANI', N'Sanitair en verwarming', 296000, 0),
        (N'PLEI', N'Pleisterwerken', 134000, 0), (N'SCHR', N'Buitenschrijnwerk', 242000, 0), (N'TEGEL', N'Tegel- en vloerwerken', 118000, 0),
        (N'ARCH', N'Architectuur en bouwdirectie', 176000, 0), (N'STAB', N'Stabiliteit', 36000, 0), (N'VEIL', N'Veiligheidscoördinatie', 15500, 0);

DECLARE @cRol NVARCHAR(10), @cNaam NVARCHAR(100), @cPrijs DECIMAL(12,2), @cBorg DECIMAL(5,2), @cid INT, @cmp INT, @aact INT;
DECLARE cc CURSOR LOCAL FAST_FORWARD FOR SELECT Rol, Naam, Prijs, Borg FROM @ct;
OPEN cc; FETCH NEXT FROM cc INTO @cRol, @cNaam, @cPrijs, @cBorg;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @cmp = (SELECT Id FROM @bed WHERE Rol = @cRol);
    INSERT INTO dbo.Contract (ProjectID, CompanyID, CashDiscount, VatPercentage, PaymentTerm, GuaranteeType, GuaranteePercentage, ContractSigned, ContractName)
    VALUES (@ProjectId, @cmp, 0, 21, 30, CASE WHEN @cBorg > 0 THEN 2 ELSE NULL END, NULLIF(@cBorg, 0), 1, @cNaam);
    SET @cid = SCOPE_IDENTITY();
    SET @aact = (SELECT Id FROM @act WHERE Code = @cRol);
    IF @aact IS NOT NULL
    BEGIN
        INSERT INTO dbo.ContractActivity (ContractID, ActivityID, Price) VALUES (@cid, @aact, @cPrijs);
        INSERT INTO dbo.ProjectBudget (ProjectID, ActivityID, Price) VALUES (@ProjectId, @aact, ROUND(@cPrijs * 1.04, -2));
    END
    FETCH NEXT FROM cc INTO @cRol, @cNaam, @cPrijs, @cBorg;
END
CLOSE cc; DEALLOCATE cc;
SET @t1 = (SELECT COUNT(*) FROM dbo.Contract WHERE ProjectID = @ProjectId);
SET @t2 = (SELECT COUNT(*) FROM dbo.ProjectBudget WHERE ProjectID = @ProjectId);
PRINT N'[6] contracten: ' + CAST(@t1 AS NVARCHAR(10)) + N', budgetlijnen: ' + CAST(@t2 AS NVARCHAR(10));

-- ---------------------------------------------------------------------------------------------
-- [7] Hoofdplanning met fasen en taken (relatief ten opzichte van vandaag, enkel werkdagen)
-- ---------------------------------------------------------------------------------------------
DECLARE @bp INT;
INSERT INTO dbo.BouwPlanning (ProjectId, Naam, IsHoofd, StartDatum, Volgorde) VALUES (@ProjectId, N'Hoofdplanning', 1, @start, 0);
SET @bp = SCOPE_IDENTITY();

DECLARE @pl TABLE (Nr INT IDENTITY(1,1), Fase INT, FaseNaam NVARCHAR(100), Rol NVARCHAR(10), Taak NVARCHAR(150), Duur INT);
IF @Variant = 1
    INSERT INTO @pl (Fase, FaseNaam, Rol, Taak, Duur) VALUES
     (1, N'Voorbereiding', N'RUW', N'Inrichten en uitzetten van de werf', 5),
     (1, N'Voorbereiding', N'RUW', N'Grondwerken en uitgraven van de ondergrond', 15),
     (2, N'Ruwbouw ondergrond', N'RUW', N'Funderingen en vloerplaat ondergrond', 15),
     (2, N'Ruwbouw ondergrond', N'RUW', N'Wanden en afdek ondergrond', 20),
     (3, N'Ruwbouw verdiepingen', N'RUW', N'Gelijkvloers: vloerplaat en metselwerk', 20),
     (3, N'Ruwbouw verdiepingen', N'RUW', N'Eerste verdieping: vloerplaat en metselwerk', 20),
     (3, N'Ruwbouw verdiepingen', N'RUW', N'Tweede verdieping: vloerplaat en metselwerk', 20),
     (3, N'Ruwbouw verdiepingen', N'RUW', N'Derde verdieping: vloerplaat en metselwerk', 20),
     (4, N'Dak en gevel', N'DAK', N'Dakopstanden en platte dakbedekking', 15),
     (4, N'Dak en gevel', N'SCHR', N'Plaatsen buitenschrijnwerk', 20),
     (5, N'Technieken', N'ELEK', N'Plaatsen leidingen elektriciteit', 20),
     (5, N'Technieken', N'SANI', N'Plaatsen leidingen sanitair en verwarming', 20),
     (5, N'Technieken', N'LIFT', N'Plaatsen lift', 10),
     (6, N'Afwerking', N'PLEI', N'Pleisterwerken', 30),
     (6, N'Afwerking', N'TEGEL', N'Chape en tegelwerken', 25),
     (6, N'Afwerking', N'ELEK', N'Afwerking elektriciteit en sanitair', 15),
     (7, N'Oplevering', N'RUW', N'Opkuis en voorlopige oplevering', 10);
ELSE
    INSERT INTO @pl (Fase, FaseNaam, Rol, Taak, Duur) VALUES
     (1, N'Voorbereiding', N'RUW', N'Inrichten en uitzetten van de werf', 5),
     (1, N'Voorbereiding', N'RUW', N'Aanleg werfweg en nutsvoorzieningen', 10),
     (2, N'Funderingen', N'RUW', N'Funderingen en rioleringen woningen lot 1 tot 7', 20),
     (2, N'Funderingen', N'RUW', N'Funderingen en rioleringen woningen lot 8 tot 14', 20),
     (3, N'Ruwbouw', N'RUW', N'Metselwerk en vloerplaat woningen lot 1 tot 7', 35),
     (3, N'Ruwbouw', N'RUW', N'Metselwerk en vloerplaat woningen lot 8 tot 14', 35),
     (4, N'Dak en gevel', N'DAK', N'Dakstructuur en dakbedekking', 25),
     (4, N'Dak en gevel', N'SCHR', N'Plaatsen buitenschrijnwerk', 20),
     (5, N'Technieken', N'ELEK', N'Plaatsen leidingen elektriciteit', 20),
     (5, N'Technieken', N'SANI', N'Plaatsen leidingen sanitair en verwarming', 20),
     (6, N'Afwerking', N'PLEI', N'Pleisterwerken', 30),
     (6, N'Afwerking', N'TEGEL', N'Chape en tegelwerken', 25),
     (7, N'Oplevering', N'RUW', N'Opkuis en voorlopige oplevering', 10);

DECLARE @fase INT = 0, @sid INT, @d DATE = @start, @e DATE, @rest INT, @wd INT;
DECLARE @pFase INT, @pFaseNaam NVARCHAR(100), @pRol NVARCHAR(10), @pTaak NVARCHAR(150), @pDuur INT, @vol INT = 0;
DECLARE cp CURSOR LOCAL FAST_FORWARD FOR SELECT Fase, FaseNaam, Rol, Taak, Duur FROM @pl ORDER BY Nr;
OPEN cp; FETCH NEXT FROM cp INTO @pFase, @pFaseNaam, @pRol, @pTaak, @pDuur;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF @pFase <> @fase
    BEGIN
        SET @fase = @pFase;
        INSERT INTO dbo.PlanningSectie (ProjectId, Naam, Volgorde, IsActief, TeltMeeVoorVoortgang, BouwPlanningId, UitvoerderCompanyId)
        VALUES (@ProjectId, @pFaseNaam, @fase, 1, 1, @bp, (SELECT Id FROM @bed WHERE Rol = @pRol));
        SET @sid = SCOPE_IDENTITY();
        SET @vol = 0;
    END
    SET @vol += 1;
    -- einddatum = start + (duur - 1) werkdagen
    SET @rest = @pDuur - 1;
    SET @e = DATEADD(DAY, (@rest / 5) * 7 + (@rest % 5) + CASE WHEN (DATEDIFF(DAY, '19000101', @d) % 7) + (@rest % 5) >= 5 THEN 2 ELSE 0 END, @d);
    INSERT INTO dbo.PlanningTaak (PlanningSectieId, Naam, Volgorde, Duur, Startdatum, Einddatum, IsAfgerond, AfgerondOp, VoortgangPct, UitvoerderCompanyId)
    VALUES (@sid, @pTaak, @vol, @pDuur, @d, @e,
            CASE WHEN @e < @vandaag THEN 1 ELSE 0 END,
            CASE WHEN @e < @vandaag THEN @e END,
            CASE WHEN @e < @vandaag THEN 100
                 WHEN @d <= @vandaag THEN CAST(100.0 * (DATEDIFF(DAY, @d, @vandaag) + 1) / (DATEDIFF(DAY, @d, @e) + 1) AS INT)
                 ELSE 0 END,
            (SELECT Id FROM @bed WHERE Rol = @pRol));
    -- volgende taak start de eerstvolgende werkdag
    SET @d = DATEADD(DAY, 1, @e);
    SET @wd = DATEDIFF(DAY, '19000101', @d) % 7;
    IF @wd = 5 SET @d = DATEADD(DAY, 2, @d); ELSE IF @wd = 6 SET @d = DATEADD(DAY, 1, @d);
    FETCH NEXT FROM cp INTO @pFase, @pFaseNaam, @pRol, @pTaak, @pDuur;
END
CLOSE cp; DEALLOCATE cp;
SET @t1 = (SELECT COUNT(*) FROM dbo.PlanningSectie WHERE ProjectId = @ProjectId);
SET @t2 = (SELECT COUNT(*) FROM dbo.PlanningTaak t JOIN dbo.PlanningSectie s ON s.Id = t.PlanningSectieId WHERE s.ProjectId = @ProjectId);
PRINT N'[7] planning: ' + CAST(@t1 AS NVARCHAR(10)) + N' fasen, ' + CAST(@t2 AS NVARCHAR(10)) + N' taken, geplande einde ' + CONVERT(NVARCHAR(10), @e, 23);

-- ---------------------------------------------------------------------------------------------
-- [8] Verkoopinstellingen
-- ---------------------------------------------------------------------------------------------
INSERT INTO dbo.ProjectSalesSettings (Projectid, Vatpercentage, RegistrationPercentage, SaleVisible)
VALUES (@ProjectId, 21, 12, 0);

-- EINDE (voeg hierboven nieuwe blokken toe, zie de kop van dit bestand)

PRINT N'';
PRINT N'Overzicht ' + @naam + N':';
SELECT (SELECT COUNT(*) FROM dbo.Units WHERE ProjectId = @ProjectId) AS Eenheden,
       (SELECT COUNT(*) FROM dbo.Units WHERE ProjectId = @ProjectId AND ClientAccountId IS NOT NULL) AS Verkocht,
       (SELECT COUNT(*) FROM dbo.InvoicingPaymentGroup WHERE ProjectId = @ProjectId) AS Betalingsgroepen,
       (SELECT COUNT(*) FROM dbo.Contract WHERE ProjectID = @ProjectId) AS Contracten,
       (SELECT COUNT(*) FROM dbo.PlanningSectie WHERE ProjectId = @ProjectId) AS PlanningFasen,
       (SELECT COUNT(*) FROM dbo.ProjectUserAccess WHERE ProjectId = @ProjectId) AS ToegangsrijenMoet0Zijn;

IF @Uitvoeren = 1
BEGIN
    COMMIT;
    PRINT N'KLAAR: bewaard. Project ' + @naam + N' heeft ProjectId ' + CAST(@ProjectId AS NVARCHAR(10)) + N'.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT N'PROEFRIT afgelopen: alles is teruggedraaid. Zet @Uitvoeren op 1 om het echt te doen.';
END
