-- =============================================
-- 169 (26/11/2026): trajectsjablonen "Verkaveling met wegenis - woningen per lot" en "Verkaveling zonder wegenis - woningen per lot".
--   Naast de bestaande sjablonen uit 041 (die blijven onaangeroerd). Gebaseerd op docs/voortgang/TRAJECT_VERKAVELING_VOORSTEL.md (lijst van Niels, 26/11/2026).
--   Twee delen: het PROJECTDEEL (scope 0: de verkaveling zelf) en het WONINGDEEL (scope 1: één rij per woning/lot, identiek in beide varianten).
--   * Bouwmijlpalen per woning hebben als code een BOUWSTAAT (WERF_UITGEZET, FUNDERING_KLAAR, ...): ze volgen de planning van die woning (PlanningTrajectKoppeling).
--     Wegenismijlpalen hebben de code van de wegenisbouwstaat (WEGENIS_NUTSVOORZIENINGEN, ...) en volgen de wegenisplanning in de hoofdplanning.
--   * Compromis, akte en sleuteloverdracht komen uit het klantaccount (ComputedBinding 7), net als bij het appartementstraject (158i).
--   * "Dak dicht" bestaat tweemaal (plat dak en hellend dak): enkel de mijlpaal die bij het gekozen dak past, volgt de planning; de andere kan je per woning verwijderen.
--   * De bouwvergunning van de koper wacht bij "met wegenis" vanzelf op de wegenis: verkoop start pas na de voorlopige oplevering van de wegenis (anker van kadastrering).
--   * Trigger "Aktefacturen klaarzetten": 1 dag voor de akte krijgt de boekhouder een taak (zie 168).
--   * Termijnen zijn indicatief (definitieve oplevering minimum 1 jaar na de voorlopige).
--   * De projectsjablonen "Verkaveling met/zonder wegenis" (165) worden naar deze nieuwe trajecten omgezet, enkel als ze nog naar de oude 041-trajecten wijzen.
--   InterneRol: 1=Projectleider 2=Projectontwikkelaar 3=CeoCfo 4=Boekhouder 5=Verkoper 6=Architect 7=Extern
--   MijlpaalType: 0=Algemeen 1=Administratief 2=Vergunning 3=Werf 4=Verkoop 5=Financieel 6=Keuring 7=Oplevering
--   Idempotent: bestaat een sjabloon al, dan gebeurt er niets met dat sjabloon.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

CREATE TABLE #fase (Code NVARCHAR(40), Naam NVARCHAR(100), VolgZ INT NULL, VolgM INT NULL, Status INT NULL);
INSERT INTO #fase VALUES
 (N'AANKOOP',            N'Aankoop & studie',                     10, 10,  NULL),
 (N'ONTWERP',            N'Ontwerp infrastructuur',               NULL, 20, 3),
 (N'VERGUNNING',         N'Vergunning',                           20, 30,  4),
 (N'UITVOERING_WEGENIS', N'Aanleg wegenis & nutsvoorzieningen',   NULL, 40, 2),
 (N'KAVELVORMING',       N'Kavelvorming & kadaster',              30, 50,  NULL),
 (N'VERKOOP',            N'Verkoop',                              40, 60,  5),
 (N'WERFVOORBEREIDING',  N'Werfvoorbereiding',                    50, 70,  NULL),
 (N'BOUW',               N'Bouw woningen',                        60, 80,  2),
 (N'OPLEVERING',         N'Oplevering woningen',                  70, 90,  NULL),
 (N'NAZORG',             N'Nazorg',                               80, 100, 1);

CREATE TABLE #mp (Fase NVARCHAR(40), Naam NVARCHAR(200), Code NVARCHAR(60), Vol INT, Typ INT, Rol INT, Scope INT, AnkZ NVARCHAR(60) NULL, AnkM NVARCHAR(60) NULL,
                  Offs INT NULL, Verpl BIT, Bind INT NULL, Param NVARCHAR(60) NULL, Omschr NVARCHAR(1000) NULL, Z BIT, M BIT);
INSERT INTO #mp VALUES
 -- Aankoop & studie
 (N'AANKOOP', N'Grondverwerving & verkavelingsstudie', N'GRONDVERWERVING', 10, 1, 2, 0, N'PROJECT_CREATED', N'PROJECT_CREATED', NULL, 1, NULL, NULL, NULL, 1, 1),
 (N'AANKOOP', N'Financiering rond (bankkrediet, bankgaranties)', N'FINANCIERING', 20, 5, 2, 0, N'GRONDVERWERVING', N'GRONDVERWERVING', NULL, 1, NULL, NULL, NULL, 1, 1),
 (N'AANKOOP', N'Verkoopprijzen en verkoopdossier vastgelegd', N'VERKOOPDOSSIER', 30, 4, 2, 0, N'GRONDVERWERVING', N'GRONDVERWERVING', NULL, 1, NULL, NULL, N'Prijslijst, brochure en verkoopsvoorwaarden klaar.', 1, 1),
 -- Ontwerp infrastructuur (met wegenis)
 (N'ONTWERP', N'Voorontwerp infrastructuur', N'VOORONTWERP_INFRASTRUCTUUR', 10, 0, 2, 0, NULL, N'GRONDVERWERVING', NULL, 1, NULL, NULL, NULL, 0, 1),
 -- Vergunning
 (N'VERGUNNING', N'Aanvraag verkavelingsvergunning', N'VERGUNNING_AANVRAAG', 10, 2, 2, 0, N'GRONDVERWERVING', N'VOORONTWERP_INFRASTRUCTUUR', NULL, 1, NULL, NULL, N'Start termijn van orde (indicatief 75 dagen zonder wegenis, 105 dagen met wegenis).', 1, 1),
 (N'VERGUNNING', N'Openbaar onderzoek', N'OPENBAAR_ONDERZOEK', 20, 2, 2, 0, N'VERGUNNING_AANVRAAG', N'VERGUNNING_AANVRAAG', NULL, 1, NULL, NULL, NULL, 1, 1),
 (N'VERGUNNING', N'Adviezen instanties compleet', N'ADVIEZEN_INSTANTIES', 30, 2, 2, 0, NULL, N'VERGUNNING_AANVRAAG', NULL, 1, NULL, NULL, N'Wacht op adviezen van nutsmaatschappijen, gemeente en brandweer.', 0, 1),
 (N'VERGUNNING', N'Gemeenteraadsbeslissing overname wegenis', N'GEMEENTERAADSBESLISSING', 40, 2, 2, 0, NULL, N'ADVIEZEN_INSTANTIES', NULL, 1, NULL, NULL, NULL, 0, 1),
 (N'VERGUNNING', N'Financiële waarborg gestort', N'FINANCIELE_WAARBORG', 50, 5, 2, 0, NULL, N'GEMEENTERAADSBESLISSING', NULL, 1, NULL, NULL, NULL, 0, 1),
 (N'VERGUNNING', N'Verkavelingsvergunning definitief', N'VERGUNNING_DEFINITIEF', 60, 2, 2, 0, N'OPENBAAR_ONDERZOEK', N'FINANCIELE_WAARBORG', 35, 1, NULL, NULL, N'Definitief 35 dagen na de vergunningsbeslissing, voor zover geen beroep werd ontvangen.', 1, 1),
 -- Aanleg wegenis (met wegenis); codes = bouwstaten van de wegenisplanning
 (N'UITVOERING_WEGENIS', N'Aanbesteding wegenis-aannemer', N'AANBESTEDING_WEGENIS', 10, 3, 1, 0, NULL, N'VERGUNNING_DEFINITIEF', NULL, 1, NULL, NULL, NULL, 0, 1),
 (N'UITVOERING_WEGENIS', N'Nutsvoorzieningen aangelegd', N'WEGENIS_NUTSVOORZIENINGEN', 20, 3, 1, 0, NULL, N'AANBESTEDING_WEGENIS', NULL, 1, NULL, NULL, N'Volgt de wegenisplanning. Wacht op attesten van water, elektriciteit, riolering en telecom.', 0, 1),
 (N'UITVOERING_WEGENIS', N'Voorlopige oplevering wegenis', N'WEGENIS_VOORLOPIGE_OPLEVERING', 30, 7, 1, 0, NULL, N'WEGENIS_NUTSVOORZIENINGEN', NULL, 1, NULL, NULL, N'Volgt de wegenisplanning. Start de proefperiode van de wegenis (indicatief 730 dagen). Verkoop en bouwvergunning van de koper volgen hierna.', 0, 1),
 (N'UITVOERING_WEGENIS', N'Definitieve oplevering en overdracht wegenis', N'WEGENIS_DEFINITIEVE_OPLEVERING', 40, 7, 1, 0, NULL, N'WEGENIS_VOORLOPIGE_OPLEVERING', 730, 1, NULL, NULL, N'Bij overdracht wordt de wegenis eigendom van de gemeente. Er is geen overeenkomstige projectstatus: manueel registreren.', 0, 1),
 -- Kavelvorming
 (N'KAVELVORMING', N'Landmeterplan & afpaling', N'LANDMETERPLAN', 10, 1, 2, 0, N'VERGUNNING_DEFINITIEF', N'VERGUNNING_DEFINITIEF', NULL, 1, NULL, NULL, NULL, 1, 1),
 (N'KAVELVORMING', N'Kadastrering kavels', N'KADASTRERING', 20, 1, 2, 0, N'LANDMETERPLAN', N'WEGENIS_VOORLOPIGE_OPLEVERING', NULL, 1, NULL, NULL, N'Met wegenis volgt de kadastrering de voorlopige oplevering van de wegenis.', 1, 1),
 -- Verkoop: project (1x) en woning (per lot)
 (N'VERKOOP', N'Vrijgave verkoop kavels', N'COMMERCIALISATIE', 10, 4, 2, 0, N'KADASTRERING', N'KADASTRERING', NULL, 1, NULL, NULL, NULL, 1, 1),
 (N'VERKOOP', N'Compromis woning', N'VERKOOP_COMPROMIS', 20, 4, 5, 1, N'COMMERCIALISATIE', N'COMMERCIALISATIE', NULL, 1, 7, N'DateSalesAgreement', N'Datum uit de klantenrekening van de woning.', 1, 1),
 (N'VERKOOP', N'Opschortende voorwaarden vervuld (lening, ...)', N'OPSCHORTENDE_VOORWAARDEN', 30, 4, 5, 1, N'VERKOOP_COMPROMIS', N'VERKOOP_COMPROMIS', 60, 0, NULL, NULL, N'Niet elke verkoop heeft opschortende voorwaarden: verwijder de mijlpaal als die niet van toepassing is.', 1, 1),
 (N'VERKOOP', N'Notariële akte woning', N'NOTARIELE_AKTE', 40, 4, 5, 1, N'VERKOOP_COMPROMIS', N'VERKOOP_COMPROMIS', NULL, 1, 7, N'DateDeedOfSale', N'Datum uit de klantenrekening van de woning.', 1, 1),
 (N'VERKOOP', N'Aktefacturen klaargezet (grond en kosten)', N'AKTEFACTUREN_KLAAR', 50, 5, 4, 1, N'NOTARIELE_AKTE', N'NOTARIELE_AKTE', -1, 1, NULL, NULL, N'Met de knop Aktefacturen klaarzetten op de pagina Facturatie. De boekhouder krijgt een dag vóór de akte een taak.', 1, 1),
 (N'VERKOOP', N'Aktefacturen betaald', N'AKTEFACTUREN_BETAALD', 60, 5, 4, 1, N'NOTARIELE_AKTE', N'NOTARIELE_AKTE', 14, 1, NULL, NULL, NULL, 1, 1),
 (N'VERKOOP', N'Bouwvergunning koper ingediend', N'BOUWVERGUNNING_KOPER_INGEDIEND', 70, 2, 2, 1, N'NOTARIELE_AKTE', N'NOTARIELE_AKTE', 14, 1, NULL, NULL, N'Bij verkaveling met wegenis kan dit pas na de voorlopige oplevering van de wegenis; de akte volgt die oplevering al.', 1, 1),
 (N'VERKOOP', N'Bouwvergunning koper verleend', N'BOUWVERGUNNING_KOPER', 80, 2, 2, 1, N'BOUWVERGUNNING_KOPER_INGEDIEND', N'BOUWVERGUNNING_KOPER_INGEDIEND', 105, 1, NULL, NULL, N'Uiterlijk 105 dagen na de aanvraag (indicatief).', 1, 1),
 (N'VERKOOP', N'Afwerkingskeuzes en meerwerk bevestigd', N'KEUZES_MEERWERK', 90, 0, 1, 1, N'NOTARIELE_AKTE', N'NOTARIELE_AKTE', 60, 1, NULL, NULL, NULL, 1, 1),
 -- Werfvoorbereiding (project)
 (N'WERFVOORBEREIDING', N'Aanstelling veiligheidscoördinator en EPB-verslaggever', N'AANSTELLING_COORDINATOR_EPB', 10, 3, 1, 0, N'VERGUNNING_DEFINITIEF', N'VERGUNNING_DEFINITIEF', NULL, 1, NULL, NULL, NULL, 1, 1),
 (N'WERFVOORBEREIDING', N'Werfmelding ingediend', N'WERFMELDING', 20, 3, 1, 0, N'AANSTELLING_COORDINATOR_EPB', N'AANSTELLING_COORDINATOR_EPB', NULL, 1, NULL, NULL, NULL, 1, 1),
 (N'WERFVOORBEREIDING', N'Verzekering Alle Bouwplaatsrisico''s afgesloten', N'VERZEKERING_ABR', 30, 5, 1, 0, N'AANSTELLING_COORDINATOR_EPB', N'AANSTELLING_COORDINATOR_EPB', NULL, 1, NULL, NULL, N'Zie de pagina Verzekeringen van het project.', 1, 1),
 -- Bouw per woning: codes = bouwstaten, ze volgen de planning van de woning
 (N'BOUW', N'Werf uitgezet', N'WERF_UITGEZET', 10, 3, 1, 1, N'BOUWVERGUNNING_KOPER', N'BOUWVERGUNNING_KOPER', 14, 1, NULL, NULL, N'Volgt de planning van de woning.', 1, 1),
 (N'BOUW', N'Funderingen klaar', N'FUNDERING_KLAAR', 20, 3, 1, 1, N'WERF_UITGEZET', N'WERF_UITGEZET', 21, 1, NULL, NULL, N'Volgt de planning van de woning.', 1, 1),
 (N'BOUW', N'Keuring riolering', N'KEURING_RIOLERING', 30, 6, 1, 1, N'FUNDERING_KLAAR', N'FUNDERING_KLAAR', 14, 1, NULL, NULL, NULL, 1, 1),
 (N'BOUW', N'Vloerplaat gelijkvloers', N'VLOERPLAAT_GLVL', 40, 3, 1, 1, N'FUNDERING_KLAAR', N'FUNDERING_KLAAR', 10, 1, NULL, NULL, N'Volgt de planning van de woning.', 1, 1),
 (N'BOUW', N'Opstort verdieping', N'OPSTORT', 50, 3, 1, 1, N'VLOERPLAAT_GLVL', N'VLOERPLAAT_GLVL', 14, 1, NULL, NULL, N'Volgt de planning van de woning.', 1, 1),
 (N'BOUW', N'Gesloten ruwbouw (metselwerk afgewerkt)', N'METSELWERK_RUWBOUW', 60, 3, 1, 1, N'OPSTORT', N'OPSTORT', 35, 1, NULL, NULL, N'Volgt de planning: de laatste bouwlaag is gemetseld.', 1, 1),
 (N'BOUW', N'Dak dicht (plat dak)', N'DAK_PLAT_DICHT', 70, 3, 1, 1, N'METSELWERK_RUWBOUW', N'METSELWERK_RUWBOUW', 30, 0, NULL, NULL, N'Enkel bij een plat dak; verwijder de mijlpaal bij een hellend dak.', 1, 1),
 (N'BOUW', N'Dak dicht (hellend dak)', N'DAK_HELLEND_DICHT', 75, 3, 1, 1, N'METSELWERK_RUWBOUW', N'METSELWERK_RUWBOUW', 30, 0, NULL, NULL, N'Enkel bij een hellend dak; verwijder de mijlpaal bij een plat dak.', 1, 1),
 (N'BOUW', N'Buitenschrijnwerk geplaatst (winddicht)', N'BUITENSCHRIJNWERK', 80, 3, 1, 1, N'METSELWERK_RUWBOUW', N'METSELWERK_RUWBOUW', 45, 1, NULL, NULL, N'Volgt de planning van de woning.', 1, 1),
 (N'BOUW', N'Leidingen en technieken geplaatst', N'TECHNIEKEN_RUW', 90, 3, 1, 1, N'BUITENSCHRIJNWERK', N'BUITENSCHRIJNWERK', 20, 1, NULL, NULL, N'Volgt de planning van de woning.', 1, 1),
 (N'BOUW', N'Keuring elektriciteit', N'KEURING_ELEKTRICITEIT', 100, 6, 1, 1, N'TECHNIEKEN_RUW', N'TECHNIEKEN_RUW', 30, 1, NULL, NULL, NULL, 1, 1),
 (N'BOUW', N'Pleisterwerk klaar', N'PLEISTER', 110, 3, 1, 1, N'TECHNIEKEN_RUW', N'TECHNIEKEN_RUW', 21, 1, NULL, NULL, N'Volgt de planning van de woning.', 1, 1),
 (N'BOUW', N'Chape geplaatst', N'CHAPE', 120, 3, 1, 1, N'PLEISTER', N'PLEISTER', 14, 1, NULL, NULL, N'Volgt de planning van de woning.', 1, 1),
 (N'BOUW', N'Keuken geplaatst', N'KEUKEN', 130, 3, 1, 1, N'CHAPE', N'CHAPE', 30, 1, NULL, NULL, N'Volgt de planning van de woning.', 1, 1),
 -- Oplevering per woning
 (N'OPLEVERING', N'EPB-aangifte ingediend', N'EPB_AANGIFTE', 10, 1, 1, 1, N'KEUKEN', N'KEUKEN', 0, 1, NULL, NULL, NULL, 1, 1),
 (N'OPLEVERING', N'PID afgeleverd', N'PID_AFGELEVERD', 20, 1, 1, 1, N'KEUKEN', N'KEUKEN', 0, 1, NULL, NULL, N'Postinterventiedossier van de veiligheidscoördinator.', 1, 1),
 (N'OPLEVERING', N'Voorlopige oplevering woning', N'VOORLOPIGE_OPLEVERING', 30, 7, 1, 1, N'KEUKEN', N'KEUKEN', 14, 1, NULL, NULL, N'Volgt de planning van de woning; bereikt ook de laatste schijf.', 1, 1),
 (N'OPLEVERING', N'Eindafrekening (meer- en minwerk, aansluitkosten)', N'EINDAFREKENING', 40, 5, 4, 1, N'VOORLOPIGE_OPLEVERING', N'VOORLOPIGE_OPLEVERING', -7, 1, NULL, NULL, N'De aansluitkosten rekent de bouwvennootschap hier af.', 1, 1),
 (N'OPLEVERING', N'Saldo ontvangen voor sleuteloverdracht', N'SALDO_ONTVANGEN', 50, 5, 4, 1, N'VOORLOPIGE_OPLEVERING', N'VOORLOPIGE_OPLEVERING', 0, 1, NULL, NULL, NULL, 1, 1),
 (N'OPLEVERING', N'Sleuteloverdracht aan koper', N'SLEUTELOVERDRACHT', 60, 7, 1, 1, N'VOORLOPIGE_OPLEVERING', N'VOORLOPIGE_OPLEVERING', NULL, 1, 7, N'DeliveryDate', N'Datum uit de klantenrekening van de woning.', 1, 1),
 (N'OPLEVERING', N'Definitieve oplevering woning', N'DEFINITIEVE_OPLEVERING_EENHEID', 70, 7, 1, 1, N'VOORLOPIGE_OPLEVERING', N'VOORLOPIGE_OPLEVERING', 365, 1, 7, N'DeliveryDateDef', N'Minimum 1 jaar na de voorlopige oplevering. Datum uit de klantenrekening van de woning.', 1, 1),
 -- Nazorg
 (N'NAZORG', N'Nacalculatie woning afgesloten', N'NACALCULATIE_WONING', 10, 5, 2, 1, N'DEFINITIEVE_OPLEVERING_EENHEID', N'DEFINITIEVE_OPLEVERING_EENHEID', 30, 1, NULL, NULL, N'Zie de pagina Nacalculatie.', 1, 1),
 (N'NAZORG', N'Garantie (10 jaar) en verzekering woning in orde', N'GARANTIE_VERZEKERING_WONING', 20, 1, 2, 1, N'VOORLOPIGE_OPLEVERING', N'VOORLOPIGE_OPLEVERING', NULL, 1, NULL, NULL, N'Tienjarige aansprakelijkheid: zie de pagina Verzekeringen.', 1, 1),
 (N'NAZORG', N'Verkavelaarsverplichtingen afgerond', N'NAZORG_VERKAVELING', 30, 0, 1, 0, N'KADASTRERING', NULL, NULL, 1, NULL, NULL, N'Eindpunt van het sjabloon; het dossier kan gearchiveerd worden.', 1, 0),
 (N'NAZORG', N'Waarborgperiode wegenis afgerond', N'WAARBORGPERIODE_WEGENIS', 40, 0, 1, 0, NULL, N'WEGENIS_DEFINITIEVE_OPLEVERING', NULL, 1, NULL, NULL, N'Eindpunt van het sjabloon; het dossier kan gearchiveerd worden.', 0, 1);

-- Triggers: event 0 = bij bereiken, 2 = x dagen voor de doeldatum; actie 2 = maak taak, 7 = ontgrendel volgende fase
CREATE TABLE #tr (Code NVARCHAR(60), Ev INT, Act INT, Params NVARCHAR(500) NULL, Offs INT NULL, Omschr NVARCHAR(300) NULL, Z BIT, M BIT);
INSERT INTO #tr VALUES
 (N'VERKOOPDOSSIER',   0, 7, NULL, NULL, N'Ontgrendelt de volgende fase.', 1, 1),
 (N'VOORONTWERP_INFRASTRUCTUUR', 0, 7, NULL, NULL, N'Ontgrendelt de vergunningsfase.', 0, 1),
 (N'OPENBAAR_ONDERZOEK', 0, 2, N'{"titel":"Bezwaartermijn opvolgen","offsetDagen":30}', NULL, NULL, 1, 1),
 (N'GEMEENTERAADSBESLISSING', 0, 2, N'{"titel":"Financiële waarborg (bankgarantie) regelen","rol":4}', NULL, NULL, 0, 1),
 (N'VERGUNNING_DEFINITIEF', 0, 7, NULL, NULL, N'Ontgrendelt de volgende fase.', 1, 1),
 (N'WEGENIS_VOORLOPIGE_OPLEVERING', 0, 7, NULL, NULL, N'Ontgrendelt de kavelvormingsfase.', 0, 1),
 (N'WEGENIS_DEFINITIEVE_OPLEVERING', 0, 2, N'{"titel":"Vrijgave financiële waarborg","rol":4}', NULL, NULL, 0, 1),
 (N'LANDMETERPLAN',    0, 2, N'{"titel":"Kadastrale mutatie aanvragen","rol":7}', NULL, NULL, 1, 1),
 (N'KADASTRERING',     0, 7, NULL, NULL, N'Ontgrendelt de verkoopfase.', 1, 1),
 (N'COMMERCIALISATIE', 0, 7, NULL, NULL, N'Ontgrendelt de werfvoorbereiding.', 1, 1),
 (N'NOTARIELE_AKTE',   2, 2, N'{"titel":"Aktefacturen klaarzetten","omschrijving":"De akte is morgen. Zet op de pagina Facturatie van het project de aktefacturen (grond, kosten en werken) klaar met de knop Aktefacturen klaarzetten.","rol":4,"offsetDagen":0}', 1, N'Een dag vóór de akte: taak voor de boekhouder om de aktefacturen klaar te zetten.', 1, 1),
 (N'VERZEKERING_ABR',  0, 7, NULL, NULL, N'Ontgrendelt de bouwfase.', 1, 1),
 (N'KEUKEN',           0, 7, NULL, NULL, N'Ontgrendelt de opleveringsfase.', 1, 1);

DECLARE @v INT = 1, @naam NVARCHAR(200), @t INT;
WHILE @v <= 2
BEGIN
    SET @naam = CASE @v WHEN 1 THEN N'Verkaveling zonder wegenis - woningen per lot' ELSE N'Verkaveling met wegenis - woningen per lot' END;
    IF EXISTS (SELECT 1 FROM dbo.TrajectSjabloon WHERE Naam = @naam)
        PRINT N'Het trajectsjabloon "' + @naam + N'" bestaat al: niets gedaan.';
    ELSE
    BEGIN
        INSERT INTO dbo.TrajectSjabloon (Naam, ProjectType, IsStandaard, IsActief, Omschrijving)
        VALUES (@naam, 1, 0, 1,
                CASE @v WHEN 1 THEN N'Verkaveling zonder wegenis, met per lot het volledige traject van de woning: verkoop, akte en aktefacturen, bouw (volgt de planning), oplevering en nazorg.'
                        ELSE N'Verkaveling met wegenis, met per lot het volledige traject van de woning: verkoop, akte en aktefacturen, bouw (volgt de planning), oplevering en nazorg. De verkoop start pas na de voorlopige oplevering van de wegenis.' END);
        SET @t = SCOPE_IDENTITY();

        INSERT INTO dbo.TrajectSjabloonFase (TrajectSjabloonId, Naam, Code, Volgorde, StandaardProjectStatusId)
        SELECT @t, f.Naam, f.Code, CASE @v WHEN 1 THEN f.VolgZ ELSE f.VolgM END, f.Status
        FROM #fase f WHERE (CASE @v WHEN 1 THEN f.VolgZ ELSE f.VolgM END) IS NOT NULL;

        INSERT INTO dbo.TrajectSjabloonMijlpaal (TrajectSjabloonFaseId, Naam, Code, Volgorde, MijlpaalType, VerantwoordelijkeRol, Scope, DoeldatumAnkerCode, DoeldatumOffsetDagen, IsVerplicht, BronBinding, BronParam, Omschrijving)
        SELECT fa.Id, m.Naam, m.Code, m.Vol, m.Typ, m.Rol, m.Scope, CASE @v WHEN 1 THEN m.AnkZ ELSE m.AnkM END, m.Offs, m.Verpl, m.Bind, m.Param, m.Omschr
        FROM #mp m JOIN dbo.TrajectSjabloonFase fa ON fa.TrajectSjabloonId = @t AND fa.Code = m.Fase
        WHERE (CASE @v WHEN 1 THEN m.Z ELSE m.M END) = 1;

        INSERT INTO dbo.TrajectSjabloonMijlpaalTrigger (TrajectSjabloonMijlpaalId, TriggerEvent, TriggerActie, OffsetDagen, ActieParametersJson, MagProjectWijzigen, IsActief, Omschrijving)
        SELECT mp.Id, x.Ev, x.Act, x.Offs, x.Params, 0, 1, x.Omschr
        FROM #tr x
        JOIN dbo.TrajectSjabloonMijlpaal mp ON mp.Code = x.Code
        JOIN dbo.TrajectSjabloonFase fa ON fa.Id = mp.TrajectSjabloonFaseId AND fa.TrajectSjabloonId = @t
        WHERE (CASE @v WHEN 1 THEN x.Z ELSE x.M END) = 1;

        PRINT N'Trajectsjabloon aangemaakt: ' + @naam;

        -- Controle: ankers die niet bestaan (moet leeg zijn; PROJECT_CREATED is een vaste gebeurtenis)
        SELECT 'Anker bestaat niet' AS Controle, @naam AS Sjabloon, m.Code, m.DoeldatumAnkerCode
        FROM dbo.TrajectSjabloonMijlpaal m JOIN dbo.TrajectSjabloonFase f ON f.Id = m.TrajectSjabloonFaseId
        WHERE f.TrajectSjabloonId = @t AND m.DoeldatumAnkerCode IS NOT NULL AND m.DoeldatumAnkerCode <> N'PROJECT_CREATED'
          AND NOT EXISTS (SELECT 1 FROM dbo.TrajectSjabloonMijlpaal a JOIN dbo.TrajectSjabloonFase fa ON fa.Id = a.TrajectSjabloonFaseId WHERE fa.TrajectSjabloonId = @t AND a.Code = m.DoeldatumAnkerCode);
        -- Controle: bouwstaat-mijlpalen zonder bouwstaat in de tabel (moet leeg zijn; draai anders 158, 158e, 158h en 161)
        SELECT 'Bouwstaat ontbreekt' AS Controle, @naam AS Sjabloon, m.Code
        FROM dbo.TrajectSjabloonMijlpaal m JOIN dbo.TrajectSjabloonFase f ON f.Id = m.TrajectSjabloonFaseId
        WHERE f.TrajectSjabloonId = @t AND m.Code IN (N'WERF_UITGEZET', N'FUNDERING_KLAAR', N'VLOERPLAAT_GLVL', N'OPSTORT', N'METSELWERK_RUWBOUW', N'DAK_PLAT_DICHT', N'DAK_HELLEND_DICHT',
                  N'BUITENSCHRIJNWERK', N'TECHNIEKEN_RUW', N'PLEISTER', N'CHAPE', N'KEUKEN', N'VOORLOPIGE_OPLEVERING', N'WEGENIS_NUTSVOORZIENINGEN', N'WEGENIS_VOORLOPIGE_OPLEVERING', N'WEGENIS_DEFINITIEVE_OPLEVERING')
          AND NOT EXISTS (SELECT 1 FROM dbo.Bouwstaat b WHERE b.Code = m.Code AND b.ProjectId IS NULL);
    END
    SET @v += 1;
END

-- Projectsjablonen (165): naar de nieuwe trajecten, maar enkel als ze nog naar de oude 041-trajecten wijzen
UPDATE p SET p.TrajectSjabloonId = n.Id
FROM dbo.ProjectSjabloon p
JOIN dbo.TrajectSjabloon o ON o.Id = p.TrajectSjabloonId
JOIN dbo.TrajectSjabloon n ON n.Naam = N'Verkaveling met wegenis - woningen per lot'
WHERE p.Naam LIKE N'Verkaveling met wegenis%' AND o.Naam = N'Verkaveling met wegenis';
PRINT N'Projectsjabloon met wegenis omgezet: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
UPDATE p SET p.TrajectSjabloonId = n.Id
FROM dbo.ProjectSjabloon p
JOIN dbo.TrajectSjabloon o ON o.Id = p.TrajectSjabloonId
JOIN dbo.TrajectSjabloon n ON n.Naam = N'Verkaveling zonder wegenis - woningen per lot'
WHERE p.Naam LIKE N'Verkaveling zonder wegenis%' AND o.Naam = N'Verkaveling zonder wegenis';
PRINT N'Projectsjabloon zonder wegenis omgezet: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

SELECT s.Naam AS Sjabloon, f.Volgorde AS Fase, f.Naam AS FaseNaam, m.Volgorde, m.Naam, m.Code, m.Scope, m.VerantwoordelijkeRol AS Rol, m.DoeldatumAnkerCode AS Anker, m.DoeldatumOffsetDagen AS Dagen
FROM dbo.TrajectSjabloonMijlpaal m
JOIN dbo.TrajectSjabloonFase f ON f.Id = m.TrajectSjabloonFaseId
JOIN dbo.TrajectSjabloon s ON s.Id = f.TrajectSjabloonId
WHERE s.Naam LIKE N'Verkaveling % wegenis - woningen per lot'
ORDER BY s.Naam, f.Volgorde, m.Volgorde;

DROP TABLE #fase; DROP TABLE #mp; DROP TABLE #tr;
COMMIT;
PRINT N'KLAAR: bewaard. Bekijk de sjablonen in Instellingen > Sjablonen > Trajecten.';
