-- =============================================
-- Migratie: 041_TrajectSjablonenResidentieel
-- Datum: 2026-09-16
-- Omschrijving: Seedt 4 nieuwe trajectsjablonen (ProjectType = 1, Woonproject) op basis van een
--   extern aangeleverde procesbeschrijving (JSON) voor residentiële ontwikkelingsprojecten:
--   Woningen (IsStandaard = 1, vervangt géén bestaand standaardsjabloon — er was nog geen
--   standaard voor dit ProjectType/deze insteek), Verkaveling zonder wegenis, Verkaveling met
--   wegenis en Appartementen (mede-eigendom/VME). Strikt additief + idempotent (één IF NOT EXISTS
--   op [Naam] per sjabloon). Geen schemawijziging.
--
--   Vertaalkeuzes t.o.v. de brondata (na overleg, zie ook 030/032/036 voor het bestaande patroon):
--   - Elke losse "phase" uit de brondata is één TrajectSjabloonMijlpaal; verwante mijlpalen zijn
--     gegroepeerd in bredere TrajectSjabloonFase-secties (zelfde stijl als het bestaande
--     Woonproject-standaardsjabloon: Aankoop / Vergunning / Verkoop / Uitvoering / Oplevering).
--   - "depends_on" met één item -> DoeldatumAnkerCode = dat item. Bij meerdere items (het systeem
--     kent geen multi-dependency-anker; TrajectSjabloonMijlpaalAfhankelijkheid bestaat in het
--     datamodel maar wordt nergens in de applicatie gebruikt) is de meest bindende/laatste
--     afhankelijkheid als anker gekozen; de overige afhankelijkheid staat toegelicht in
--     [Omschrijving].
--   - "start_timer" + latere "date_elapsed" op dezelfde timer -> DoeldatumOffsetDagen op de
--     mijlpaal die de timer consumeert (bv. beroepstermijn 35 dagen, waarborgperiode 3650 dagen).
--     Timers die door niets consumeert worden (louter informatief, bv. termijn_van_orde,
--     opmerkingentermijn) staan enkel toegelicht in [Omschrijving].
--   - "document_upload"/"external_event"/"percentage"/"manual"/"dependency" op de mijlpaal zelf
--     hebben geen generieke tegenhanger in ComputedBinding en blijven dus Handmatig (BronBinding
--     NULL) — behalve de gevallen die exact overeenkomen met een reeds bestaande binding uit
--     migratie 032 (compromis/akte -> ClientAccountDatum; opleveringen -> ProjectDatum/
--     ClientAccountDatum; start uitvoering -> ProjectDatum.StartDateConstruction).
--   - "unlock_phase" -> TriggerActie 7 (DeblokkeerVolgendeFase) enkel wanneer het doel effectief de
--     eerstvolgende fase is (die actie heeft geen parameters, kan dus niet naar specifieke/meerdere
--     fases tegelijk wijzen). Andere gevallen: geen trigger-rij, toegelicht in [Omschrijving].
--   - "create_task" -> TriggerActie 2 (MaakTaak); "send_notification" -> TriggerActie 0
--     (VerwittigRol); "create_invoice"/"set_status" (unit-status)/"archive" hebben geen
--     TriggerActie-equivalent en blijven uitsluitend toegelicht in [Omschrijving].
--   - "verantwoordelijke_rol" -> InterneRol, zelfde mapping-logica als het bestaande
--     standaardsjabloon: acquisitie/pm_bouw/pm -> Projectleider of Projectontwikkelaar naargelang
--     aard, architect -> Architect, sales -> Verkoper, financieel -> Boekhouder,
--     management -> CeoCfo, notaris/landmeter/studiebureau/syndicus -> Extern (geen intern
--     rol-equivalent).
--
--   InterneRol: 1=Projectleider 2=Projectontwikkelaar 3=CeoCfo 4=Boekhouder 5=Verkoper
--       6=Architect 7=Extern
--   MijlpaalType: 0=Algemeen 1=Administratief 2=Vergunning 3=Werf 4=Verkoop 5=Financieel
--       6=Keuring 7=Oplevering
--   MijlpaalScope: 0=Project 1=PerEenheid
--   ComputedBinding: 2=ProjectDatum 7=ClientAccountDatum
--   TriggerEvent: 0=BijBereiken
--   TriggerActie: 0=VerwittigRol 2=MaakTaak 7=DeblokkeerVolgendeFase
-- =============================================

------------------------------------------------------------
-- 1. Woningen (nieuwbouw/verbouw eengezinswoning) — IsStandaard = 1
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TrajectSjabloon] WHERE [Naam] = N'Woningen (nieuwbouw/verbouw eengezinswoning)')
BEGIN
    DECLARE @won INT;

    INSERT INTO [dbo].[TrajectSjabloon] ([Naam], [ProjectType], [IsStandaard], [IsActief], [Omschrijving])
    VALUES (N'Woningen (nieuwbouw/verbouw eengezinswoning)', 1, 1, 1,
            N'Traject voor de ontwikkeling en verkoop van één of meerdere eengezinswoningen, van grondaankoop tot einde garantieperiode.');
    SET @won = SCOPE_IDENTITY();

    INSERT INTO [dbo].[TrajectSjabloonFase] ([TrajectSjabloonId], [Naam], [Code], [Volgorde], [StandaardProjectStatusId])
    VALUES
        (@won, N'Aankoop & studie',          N'AANKOOP',       10, NULL),
        (@won, N'Vergunning',                N'VERGUNNING',    20, 4),
        (@won, N'Verkoop',                   N'VERKOOP',       30, 5),
        (@won, N'Voorbereiding uitvoering',  N'VOORBEREIDING', 40, NULL),
        (@won, N'Uitvoering',                N'UITVOERING',    50, 2),
        (@won, N'Oplevering & nazorg',       N'OPLEVERING',    60, 1);

    ;WITH f AS (SELECT [Id], [Code] FROM [dbo].[TrajectSjabloonFase] WHERE [TrajectSjabloonId] = @won)
    INSERT INTO [dbo].[TrajectSjabloonMijlpaal]
        ([TrajectSjabloonFaseId], [Naam], [Code], [Volgorde], [MijlpaalType], [VerantwoordelijkeRol], [Scope], [DoeldatumAnkerCode], [DoeldatumOffsetDagen], [IsVerplicht], [BronBinding], [BronParam], [Omschrijving])
    SELECT f.[Id], v.[Naam], v.[Code], v.[Volgorde], v.[MijlpaalType], v.[Rol], v.[Scope], v.[Anker], v.[Offset], v.[Verplicht], v.[Binding], v.[Param], v.[Omschr]
    FROM f
    JOIN (VALUES
        (N'AANKOOP',       N'Grondverwerving',                    N'GRONDVERWERVING',          10, 1, 2, 0, N'PROJECT_CREATED',       NULL, 1, NULL, NULL, N'Optie op de grond genomen. Bij afronding: taak ''Stedenbouwkundig onderzoek aanvragen'' (deadline in de bron gekoppeld aan het einde van de grondoptie, niet aan deze mijlpaal — manueel te bepalen).'),
        (N'AANKOOP',       N'Haalbaarheidsstudie',                N'HAALBAARHEIDSSTUDIE',      20, 0, 1, 0, N'GRONDVERWERVING',       NULL, 1, NULL, NULL, N'Intern financieel plan goedgekeurd.'),
        (N'VERGUNNING',    N'Vergunningsaanvraag ingediend',      N'VERGUNNING_AANVRAAG',      10, 2, 6, 0, N'HAALBAARHEIDSSTUDIE',   NULL, 1, NULL, NULL, N'Start van de wettelijke termijn van orde (indicatief 105 dagen).'),
        (N'VERGUNNING',    N'Openbaar onderzoek',                 N'OPENBAAR_ONDERZOEK',       20, 2, 1, 0, N'VERGUNNING_AANVRAAG',   NULL, 0, NULL, NULL, N'Optioneel — niet elke aanvraag doorloopt een openbaar onderzoek.'),
        (N'VERGUNNING',    N'Vergunning verleend',                N'VERGUNNING_VERLEEND',      30, 2, 1, 0, N'VERGUNNING_AANVRAAG',   NULL, 1, NULL, NULL, N'Start beroepstermijn van 35 dagen.'),
        (N'VERGUNNING',    N'Vergunning definitief',              N'VERGUNNING_DEFINITIEF',    40, 2, 1, 0, N'VERGUNNING_VERLEEND',   35,   1, NULL, NULL, N'Definitief zodra de beroepstermijn (35 dagen) verstreken is zonder ontvangen beroep.'),
        (N'VERKOOP',       N'Vrijgave commercialisatie',          N'COMMERCIALISATIE',         10, 4, 5, 0, N'VERGUNNING_DEFINITIEF', NULL, 1, NULL, NULL, N'Verkoopdossier compleet.'),
        (N'VERKOOP',       N'Compromis ondertekend',              N'VERKOOP_COMPROMIS',        20, 4, 5, 1, N'COMMERCIALISATIE',      NULL, 1, 7, N'DateSalesAgreement', N'Bij tekenen: unit-status naar ''verkocht onder opschortende voorwaarde''.'),
        (N'VERKOOP',       N'Notariële akte verleden',            N'NOTARIELE_AKTE',           30, 4, 7, 1, N'VERKOOP_COMPROMIS',     NULL, 1, 7, N'DateDeedOfSale', N'Bij verlijden: unit-status naar ''definitief verkocht''.'),
        (N'VOORBEREIDING', N'Start uitvoering',                   N'UITVOERING_START',         10, 3, 1, 0, N'VERGUNNING_DEFINITIEF', NULL, 1, 2, N'StartDateConstruction', NULL),
        (N'UITVOERING',    N'Milestone: ruwbouw',                 N'BOUWVOORTGANG_RUWBOUW',    10, 3, 1, 1, N'UITVOERING_START',      NULL, 1, NULL, NULL, N'Hangt ook af van de notariële akte van de eenheid (verkoop moet rond zijn); de bouwvoortgang zelf is het bindende anker. Facturatieschijf ''ruwbouw'' (Wet Breyne) — manueel te factureren.'),
        (N'UITVOERING',    N'Milestone: dak dicht',               N'BOUWVOORTGANG_DAK_DICHT',  20, 3, 1, 1, N'BOUWVOORTGANG_RUWBOUW', NULL, 1, NULL, NULL, N'Facturatieschijf ''dak dicht'' (Wet Breyne) — manueel te factureren.'),
        (N'UITVOERING',    N'Milestone: afwerking',               N'BOUWVOORTGANG_AFWERKING',  30, 3, 1, 1, N'BOUWVOORTGANG_DAK_DICHT', NULL, 1, NULL, NULL, N'Facturatieschijf ''afwerking'' (Wet Breyne) — manueel te factureren.'),
        (N'OPLEVERING',    N'Voorlopige oplevering',              N'VOORLOPIGE_OPLEVERING',    10, 7, 1, 1, N'BOUWVOORTGANG_AFWERKING', NULL, 1, 7, N'DeliveryDate', N'Start opmerkingentermijn van 1 jaar (herinnering op dag 335).'),
        (N'OPLEVERING',    N'Definitieve oplevering',             N'DEFINITIEVE_OPLEVERING',   20, 7, 1, 1, N'VOORLOPIGE_OPLEVERING', NULL, 1, 7, N'DeliveryDateDef', N'Start de 10-jarige Wet Breyne-waarborgperiode.'),
        (N'OPLEVERING',    N'Garantieperiode verstreken',         N'GARANTIEPERIODE',          30, 0, 1, 1, N'DEFINITIEVE_OPLEVERING', 3650, 1, NULL, NULL, N'Einde van de 10-jarige (Wet Breyne) waarborgperiode; dossier kan gearchiveerd worden.')
    ) AS v([FaseCode], [Naam], [Code], [Volgorde], [MijlpaalType], [Rol], [Scope], [Anker], [Offset], [Verplicht], [Binding], [Param], [Omschr])
      ON v.[FaseCode] = f.[Code];

    INSERT INTO [dbo].[TrajectSjabloonMijlpaalTrigger]
        ([TrajectSjabloonMijlpaalId], [TriggerEvent], [TriggerActie], [ActieParametersJson], [MagProjectWijzigen], [IsActief], [Omschrijving])
    SELECT m.[Id], t.[Event], t.[Actie], t.[Params], 0, 1, t.[Omschr]
    FROM [dbo].[TrajectSjabloonMijlpaal] m
    JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
    JOIN (VALUES
        (N'GRONDVERWERVING',    0, 2, N'{"titel":"Stedenbouwkundig onderzoek aanvragen","rol":1}', N'Vervaldag niet ingevuld — zie toelichting op de mijlpaal.'),
        (N'HAALBAARHEIDSSTUDIE',0, 0, N'{"rol":3}', N'Go/no-go beslissing vereist.'),
        (N'OPENBAAR_ONDERZOEK', 0, 2, N'{"titel":"Bezwaartermijn opvolgen","rol":1,"offsetDagen":30}', NULL),
        (N'VERGUNNING_DEFINITIEF', 0, 7, NULL, N'Ontgrendelt de verkoopfase. Ontgrendelt daarnaast manueel ook de uitvoeringsfase — die hangt in de bron enkel af van de definitieve vergunning, niet van de verkoop.')
    ) AS t([MijlpaalCode], [Event], [Actie], [Params], [Omschr]) ON t.[MijlpaalCode] = m.[Code]
    WHERE f.[TrajectSjabloonId] = @won;

    PRINT 'Sjabloon "Woningen (nieuwbouw/verbouw eengezinswoning)" aangemaakt.';
END
ELSE
    PRINT 'Sjabloon "Woningen (nieuwbouw/verbouw eengezinswoning)" bestaat al, overgeslagen.';

------------------------------------------------------------
-- 2. Verkaveling zonder wegenis
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TrajectSjabloon] WHERE [Naam] = N'Verkaveling zonder wegenis')
BEGIN
    DECLARE @vzw INT;

    INSERT INTO [dbo].[TrajectSjabloon] ([Naam], [ProjectType], [IsStandaard], [IsActief], [Omschrijving])
    VALUES (N'Verkaveling zonder wegenis', 1, 0, 1,
            N'Traject voor het verkavelen en verkopen van bouwkavels zonder aanleg van nieuwe openbare wegenis/nutsvoorzieningen.');
    SET @vzw = SCOPE_IDENTITY();

    INSERT INTO [dbo].[TrajectSjabloonFase] ([TrajectSjabloonId], [Naam], [Code], [Volgorde], [StandaardProjectStatusId])
    VALUES
        (@vzw, N'Aankoop & studie',          N'AANKOOP',      10, NULL),
        (@vzw, N'Vergunning',                N'VERGUNNING',   20, 4),
        (@vzw, N'Kavelvorming & kadaster',   N'KAVELVORMING', 30, NULL),
        (@vzw, N'Verkoop',                   N'VERKOOP',      40, 5),
        (@vzw, N'Overdracht & nazorg',       N'OVERDRACHT',   50, 1);

    ;WITH f AS (SELECT [Id], [Code] FROM [dbo].[TrajectSjabloonFase] WHERE [TrajectSjabloonId] = @vzw)
    INSERT INTO [dbo].[TrajectSjabloonMijlpaal]
        ([TrajectSjabloonFaseId], [Naam], [Code], [Volgorde], [MijlpaalType], [VerantwoordelijkeRol], [Scope], [DoeldatumAnkerCode], [DoeldatumOffsetDagen], [IsVerplicht], [BronBinding], [BronParam], [Omschrijving])
    SELECT f.[Id], v.[Naam], v.[Code], v.[Volgorde], v.[MijlpaalType], v.[Rol], v.[Scope], v.[Anker], v.[Offset], v.[Verplicht], v.[Binding], v.[Param], v.[Omschr]
    FROM f
    JOIN (VALUES
        (N'AANKOOP',      N'Grondverwerving & verkavelingsstudie', N'GRONDVERWERVING',    10, 1, 2, 0, N'PROJECT_CREATED',   NULL, 1, NULL, NULL, NULL),
        (N'VERGUNNING',   N'Aanvraag verkavelingsvergunning',      N'VERGUNNING_AANVRAAG', 10, 2, 7, 0, N'GRONDVERWERVING',   NULL, 1, NULL, NULL, N'Start termijn van orde (indicatief 75 dagen).'),
        (N'VERGUNNING',   N'Openbaar onderzoek',                   N'OPENBAAR_ONDERZOEK',  20, 2, 1, 0, N'VERGUNNING_AANVRAAG', NULL, 1, NULL, NULL, NULL),
        (N'VERGUNNING',   N'Verkavelingsvergunning definitief',    N'VERGUNNING_DEFINITIEF', 30, 2, 1, 0, N'VERGUNNING_AANVRAAG', 35, 1, NULL, NULL, N'Definitief 35 dagen na de vergunningsbeslissing, voor zover geen beroep werd ontvangen.'),
        (N'KAVELVORMING', N'Landmeterplan & afpaling',             N'LANDMETERPLAN',       10, 1, 7, 0, N'VERGUNNING_DEFINITIEF', NULL, 1, NULL, NULL, NULL),
        (N'KAVELVORMING', N'Kadastrering kavels',                  N'KADASTRERING',        20, 1, 1, 0, N'LANDMETERPLAN',       NULL, 1, NULL, NULL, NULL),
        (N'VERKOOP',      N'Vrijgave verkoop kavels',              N'COMMERCIALISATIE',    10, 4, 5, 0, N'KADASTRERING',        NULL, 1, NULL, NULL, NULL),
        (N'VERKOOP',      N'Compromis kavel',                      N'VERKOOP_COMPROMIS',   20, 4, 5, 1, N'COMMERCIALISATIE',    NULL, 1, 7, N'DateSalesAgreement', NULL),
        (N'VERKOOP',      N'Akte & overdracht kavel',              N'NOTARIELE_AKTE',      30, 4, 7, 1, N'VERKOOP_COMPROMIS',   NULL, 1, 7, N'DateDeedOfSale', N'De bron vermeldt hierna een ontgrendeling van ''bouwvergunning koper'' — niet van toepassing bij een verkaveling zonder wegenis (enkel relevant met wegenis).'),
        (N'OVERDRACHT',   N'Overdracht groene/publieke zones',     N'OVERDRACHT_GROENZONES', 10, 7, 1, 0, N'KADASTRERING',      NULL, 0, NULL, NULL, NULL),
        (N'OVERDRACHT',   N'Verkavelaarsverplichtingen afgerond',  N'NAZORG',              20, 0, 1, 0, N'OVERDRACHT_GROENZONES', NULL, 1, NULL, NULL, N'Eindpunt van het sjabloon; dossier kan gearchiveerd worden.')
    ) AS v([FaseCode], [Naam], [Code], [Volgorde], [MijlpaalType], [Rol], [Scope], [Anker], [Offset], [Verplicht], [Binding], [Param], [Omschr])
      ON v.[FaseCode] = f.[Code];

    INSERT INTO [dbo].[TrajectSjabloonMijlpaalTrigger]
        ([TrajectSjabloonMijlpaalId], [TriggerEvent], [TriggerActie], [ActieParametersJson], [MagProjectWijzigen], [IsActief], [Omschrijving])
    SELECT m.[Id], t.[Event], t.[Actie], t.[Params], 0, 1, t.[Omschr]
    FROM [dbo].[TrajectSjabloonMijlpaal] m
    JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
    JOIN (VALUES
        (N'OPENBAAR_ONDERZOEK',    0, 2, N'{"titel":"Bezwaartermijn opvolgen","offsetDagen":30}', NULL),
        (N'VERGUNNING_DEFINITIEF', 0, 7, NULL, N'Ontgrendelt de kavelvormingsfase.'),
        (N'LANDMETERPLAN',         0, 2, N'{"titel":"Kadastrale mutatie aanvragen","rol":7}', NULL),
        (N'KADASTRERING',          0, 7, NULL, N'Ontgrendelt de verkoopfase.')
    ) AS t([MijlpaalCode], [Event], [Actie], [Params], [Omschr]) ON t.[MijlpaalCode] = m.[Code]
    WHERE f.[TrajectSjabloonId] = @vzw;

    PRINT 'Sjabloon "Verkaveling zonder wegenis" aangemaakt.';
END
ELSE
    PRINT 'Sjabloon "Verkaveling zonder wegenis" bestaat al, overgeslagen.';

------------------------------------------------------------
-- 3. Verkaveling met wegenis
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TrajectSjabloon] WHERE [Naam] = N'Verkaveling met wegenis')
BEGIN
    DECLARE @vmw INT;

    INSERT INTO [dbo].[TrajectSjabloon] ([Naam], [ProjectType], [IsStandaard], [IsActief], [Omschrijving])
    VALUES (N'Verkaveling met wegenis', 1, 0, 1,
            N'Traject voor het verkavelen van bouwkavels inclusief aanleg van nieuwe openbare wegenis en nutsvoorzieningen, met overdracht aan de gemeente.');
    SET @vmw = SCOPE_IDENTITY();

    INSERT INTO [dbo].[TrajectSjabloonFase] ([TrajectSjabloonId], [Naam], [Code], [Volgorde], [StandaardProjectStatusId])
    VALUES
        (@vmw, N'Ontwerp infrastructuur',            N'ONTWERP',           10, 3),
        (@vmw, N'Vergunning',                        N'VERGUNNING',        20, 4),
        (@vmw, N'Aanleg wegenis & nutsvoorzieningen', N'UITVOERING_WEGENIS', 30, 2),
        (@vmw, N'Kavelvorming & kadaster',           N'KAVELVORMING',      40, NULL),
        (@vmw, N'Verkoop',                           N'VERKOOP',           50, 5),
        (@vmw, N'Overdracht & nazorg',                N'OVERDRACHT',        60, 1);

    ;WITH f AS (SELECT [Id], [Code] FROM [dbo].[TrajectSjabloonFase] WHERE [TrajectSjabloonId] = @vmw)
    INSERT INTO [dbo].[TrajectSjabloonMijlpaal]
        ([TrajectSjabloonFaseId], [Naam], [Code], [Volgorde], [MijlpaalType], [VerantwoordelijkeRol], [Scope], [DoeldatumAnkerCode], [DoeldatumOffsetDagen], [IsVerplicht], [BronBinding], [BronParam], [Omschrijving])
    SELECT f.[Id], v.[Naam], v.[Code], v.[Volgorde], v.[MijlpaalType], v.[Rol], v.[Scope], v.[Anker], v.[Offset], v.[Verplicht], v.[Binding], v.[Param], v.[Omschr]
    FROM f
    JOIN (VALUES
        (N'ONTWERP',           N'Voorontwerp infrastructuur',                  N'VOORONTWERP_INFRASTRUCTUUR', 10, 0, 7, 0, N'PROJECT_CREATED',            NULL, 1, NULL, NULL, NULL),
        (N'VERGUNNING',        N'Aanvraag verkavelings- + wegenisvergunning',  N'VERGUNNING_AANVRAAG',        10, 2, 1, 0, N'VOORONTWERP_INFRASTRUCTUUR', NULL, 1, NULL, NULL, N'Start termijn van orde (indicatief 105 dagen).'),
        (N'VERGUNNING',        N'Adviezen instanties compleet',                N'ADVIEZEN_INSTANTIES',        20, 2, 1, 0, N'VERGUNNING_AANVRAAG',        NULL, 1, NULL, NULL, N'Wacht op adviezen van nutsmaatschappijen, gemeente en brandweer.'),
        (N'VERGUNNING',        N'Gemeenteraadsbeslissing overname wegenis',    N'GEMEENTERAADSBESLISSING',    30, 2, 1, 0, N'ADVIEZEN_INSTANTIES',        NULL, 1, NULL, NULL, NULL),
        (N'VERGUNNING',        N'Financiële waarborg gestort',                 N'FINANCIELE_WAARBORG',        40, 5, 4, 0, N'GEMEENTERAADSBESLISSING',    NULL, 1, NULL, NULL, NULL),
        (N'VERGUNNING',        N'Vergunning definitief',                       N'VERGUNNING_DEFINITIEF',      50, 2, 1, 0, N'FINANCIELE_WAARBORG',        35,   1, NULL, NULL, N'Definitief 35 dagen na de vergunningsbeslissing, voor zover geen beroep werd ontvangen.'),
        (N'UITVOERING_WEGENIS', N'Aanbesteding wegenis-aannemer',              N'AANBESTEDING_WEGENIS',       10, 3, 1, 0, N'VERGUNNING_DEFINITIEF',      NULL, 1, NULL, NULL, NULL),
        (N'UITVOERING_WEGENIS', N'Nutsvoorzieningen aangelegd',                N'NUTSVOORZIENINGEN',          20, 3, 1, 0, N'AANBESTEDING_WEGENIS',       NULL, 1, NULL, NULL, N'Wacht op attesten van water, elektriciteit, riolering en telecom.'),
        (N'UITVOERING_WEGENIS', N'Voorlopige oplevering wegenis',              N'VOORLOPIGE_OPLEVERING_WEGENIS', 30, 7, 1, 0, N'NUTSVOORZIENINGEN',      NULL, 1, 2, N'DeliveryDate', N'Start proefperiode wegenis van 730 dagen (indicatief/informatief — de definitieve oplevering blijft document-gedreven, niet automatisch na de proefperiode).'),
        (N'UITVOERING_WEGENIS', N'Definitieve oplevering & overdracht wegenis', N'DEFINITIEVE_OPLEVERING_WEGENIS', 40, 7, 1, 0, N'VOORLOPIGE_OPLEVERING_WEGENIS', NULL, 1, 2, N'DeliveryDateDef', N'Bij overdracht wordt de wegenis eigendom van de gemeente — geen overeenkomstige projectstatus in het systeem, manueel te registreren/communiceren.'),
        (N'KAVELVORMING',      N'Kadastrering kavels',                         N'KADASTRERING',               10, 1, 1, 0, N'VOORLOPIGE_OPLEVERING_WEGENIS', NULL, 1, NULL, NULL, NULL),
        (N'VERKOOP',           N'Vrijgave verkoop kavels',                     N'COMMERCIALISATIE',           10, 4, 5, 0, N'KADASTRERING',               NULL, 1, NULL, NULL, N'Hangt ook af van de voorlopige oplevering wegenis; kadastrering is de strakste/laatste van de twee en dus als anker gekozen.'),
        (N'VERKOOP',           N'Compromis kavel',                            N'VERKOOP_COMPROMIS',          20, 4, 5, 1, N'COMMERCIALISATIE',           NULL, 1, 7, N'DateSalesAgreement', NULL),
        (N'VERKOOP',           N'Akte & overdracht kavel',                     N'NOTARIELE_AKTE',             30, 4, 7, 1, N'VERKOOP_COMPROMIS',          NULL, 1, 7, N'DateDeedOfSale', NULL),
        (N'VERKOOP',           N'Bouwvergunning koper mogelijk',               N'BOUWVERGUNNING_KOPER',       40, 2, 1, 1, N'NOTARIELE_AKTE',             NULL, 1, NULL, NULL, N'Harde blokkade: mag pas ná de voorlopige oplevering van de wegenis (bijkomende voorwaarde naast de eigen akte van deze kavel).'),
        (N'OVERDRACHT',        N'Waarborgperiode wegenis & garantie kavels',   N'NAZORG',                     10, 0, 1, 0, N'DEFINITIEVE_OPLEVERING_WEGENIS', NULL, 1, NULL, NULL, N'Eindpunt van het sjabloon; dossier kan gearchiveerd worden.')
    ) AS v([FaseCode], [Naam], [Code], [Volgorde], [MijlpaalType], [Rol], [Scope], [Anker], [Offset], [Verplicht], [Binding], [Param], [Omschr])
      ON v.[FaseCode] = f.[Code];

    INSERT INTO [dbo].[TrajectSjabloonMijlpaalTrigger]
        ([TrajectSjabloonMijlpaalId], [TriggerEvent], [TriggerActie], [ActieParametersJson], [MagProjectWijzigen], [IsActief], [Omschrijving])
    SELECT m.[Id], t.[Event], t.[Actie], t.[Params], 0, 1, t.[Omschr]
    FROM [dbo].[TrajectSjabloonMijlpaal] m
    JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
    JOIN (VALUES
        (N'GEMEENTERAADSBESLISSING',        0, 2, N'{"titel":"Financiële waarborg (bankgarantie) regelen","rol":4}', NULL),
        (N'VERGUNNING_DEFINITIEF',          0, 7, NULL, N'Ontgrendelt de uitvoeringsfase wegenis.'),
        (N'VOORLOPIGE_OPLEVERING_WEGENIS',  0, 7, NULL, N'Ontgrendelt de kavelvormingsfase. Verkoop en de bouwvergunning van de koper worden pas daarna (via hun eigen afhankelijkheden) mogelijk.'),
        (N'DEFINITIEVE_OPLEVERING_WEGENIS', 0, 2, N'{"titel":"Vrijgave financiële waarborg","rol":4}', NULL)
    ) AS t([MijlpaalCode], [Event], [Actie], [Params], [Omschr]) ON t.[MijlpaalCode] = m.[Code]
    WHERE f.[TrajectSjabloonId] = @vmw;

    PRINT 'Sjabloon "Verkaveling met wegenis" aangemaakt.';
END
ELSE
    PRINT 'Sjabloon "Verkaveling met wegenis" bestaat al, overgeslagen.';

------------------------------------------------------------
-- 4. Appartementen (mede-eigendom / VME)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TrajectSjabloon] WHERE [Naam] = N'Appartementen (mede-eigendom / VME)')
BEGIN
    DECLARE @app INT;

    INSERT INTO [dbo].[TrajectSjabloon] ([Naam], [ProjectType], [IsStandaard], [IsActief], [Omschrijving])
    VALUES (N'Appartementen (mede-eigendom / VME)', 1, 0, 1,
            N'Traject voor de ontwikkeling en verkoop van appartementen/loten in mede-eigendom, inclusief oprichting van de vereniging van mede-eigenaars (VME).');
    SET @app = SCOPE_IDENTITY();

    INSERT INTO [dbo].[TrajectSjabloonFase] ([TrajectSjabloonId], [Naam], [Code], [Volgorde], [StandaardProjectStatusId])
    VALUES
        (@app, N'Aankoop & studie',          N'AANKOOP',             10, NULL),
        (@app, N'Vergunning',                N'VERGUNNING',          20, 4),
        (@app, N'Basisakte & VME-opstart',   N'JURIDISCHE_OPSTART',  30, NULL),
        (@app, N'Verkoop',                   N'VERKOOP',             40, 5),
        (@app, N'Uitvoering',                N'UITVOERING',          50, 2),
        (@app, N'Oplevering & mede-eigendom', N'OPLEVERING',          60, 1);

    ;WITH f AS (SELECT [Id], [Code] FROM [dbo].[TrajectSjabloonFase] WHERE [TrajectSjabloonId] = @app)
    INSERT INTO [dbo].[TrajectSjabloonMijlpaal]
        ([TrajectSjabloonFaseId], [Naam], [Code], [Volgorde], [MijlpaalType], [VerantwoordelijkeRol], [Scope], [DoeldatumAnkerCode], [DoeldatumOffsetDagen], [IsVerplicht], [BronBinding], [BronParam], [Omschrijving])
    SELECT f.[Id], v.[Naam], v.[Code], v.[Volgorde], v.[MijlpaalType], v.[Rol], v.[Scope], v.[Anker], v.[Offset], v.[Verplicht], v.[Binding], v.[Param], v.[Omschr]
    FROM f
    JOIN (VALUES
        (N'AANKOOP',            N'Grondverwerving & haalbaarheid',      N'GRONDVERWERVING',    10, 1, 2, 0, N'PROJECT_CREATED',    NULL, 1, NULL, NULL, NULL),
        (N'VERGUNNING',         N'Vergunningsaanvraag ingediend',       N'VERGUNNING_AANVRAAG', 10, 2, 6, 0, N'GRONDVERWERVING',    NULL, 1, NULL, NULL, N'Start van de wettelijke termijn van orde (indicatief 105 dagen).'),
        (N'VERGUNNING',         N'Vergunning definitief',               N'VERGUNNING_DEFINITIEF', 20, 2, 1, 0, N'VERGUNNING_AANVRAAG', 35, 1, NULL, NULL, N'Definitief 35 dagen na de vergunningsbeslissing, voor zover geen beroep werd ontvangen.'),
        (N'JURIDISCHE_OPSTART', N'Basisakte & reglement mede-eigendom', N'BASISAKTE',          10, 1, 7, 0, N'VERGUNNING_DEFINITIEF', NULL, 1, NULL, NULL, NULL),
        (N'JURIDISCHE_OPSTART', N'Voorlopige aanstelling syndicus',     N'SYNDICUS_VOORLOPIG', 20, 1, 7, 0, N'BASISAKTE',          NULL, 1, NULL, NULL, NULL),
        (N'VERKOOP',            N'Vrijgave verkoop loten',              N'COMMERCIALISATIE',   10, 4, 5, 0, N'BASISAKTE',          NULL, 1, NULL, NULL, NULL),
        (N'VERKOOP',            N'Compromis lot',                       N'VERKOOP_COMPROMIS',  20, 4, 5, 1, N'COMMERCIALISATIE',   NULL, 1, 7, N'DateSalesAgreement', NULL),
        (N'VERKOOP',            N'Verkoopdrempel bereikt',              N'VERKOOPDREMPEL',     30, 5, 4, 0, N'COMMERCIALISATIE',   NULL, 1, NULL, NULL, N'Drempel: 50% van de loten verkocht — niet automatisch detecteerbaar in het systeem, manueel op te volgen.'),
        (N'UITVOERING',         N'Start uitvoering',                    N'UITVOERING_START',   10, 3, 1, 0, N'VERKOOPDREMPEL',     NULL, 1, 2, N'StartDateConstruction', NULL),
        (N'UITVOERING',         N'Milestones bouwvoortgang',            N'BOUWVOORTGANG',      20, 3, 1, 1, N'UITVOERING_START',   NULL, 1, NULL, NULL, N'Omvat ruwbouw, gevel, technieken en afwerking gemeenschappelijke delen; facturatieschijven (Wet Breyne) manueel op te volgen.'),
        (N'OPLEVERING',         N'Voorlopige oplevering',               N'VOORLOPIGE_OPLEVERING', 10, 7, 1, 1, N'BOUWVOORTGANG',    NULL, 1, 7, N'DeliveryDate', N'Start opmerkingentermijn van 1 jaar.'),
        (N'OPLEVERING',         N'Oprichting VME',                      N'OPRICHTING_VME',     20, 1, 7, 0, N'VOORLOPIGE_OPLEVERING', NULL, 1, NULL, NULL, N'Hangt ook af van de voorlopige aanstelling syndicus; de oplevering is de strakste/laatste van de twee en dus als anker gekozen. Bij afronding wordt de VME operationeel — geen overeenkomstige projectstatus in het systeem, manueel te registreren.'),
        (N'OPLEVERING',         N'Definitieve oplevering',              N'DEFINITIEVE_OPLEVERING', 30, 7, 1, 1, N'VOORLOPIGE_OPLEVERING', NULL, 1, 7, N'DeliveryDateDef', N'Start de 10-jarige Wet Breyne-waarborgperiode.'),
        (N'OPLEVERING',         N'Garantieperiode',                     N'NAZORG',             40, 0, 1, 1, N'DEFINITIEVE_OPLEVERING', 3650, 1, NULL, NULL, N'Einde van de 10-jarige (Wet Breyne) waarborgperiode; dossier kan gearchiveerd worden.')
    ) AS v([FaseCode], [Naam], [Code], [Volgorde], [MijlpaalType], [Rol], [Scope], [Anker], [Offset], [Verplicht], [Binding], [Param], [Omschr])
      ON v.[FaseCode] = f.[Code];

    INSERT INTO [dbo].[TrajectSjabloonMijlpaalTrigger]
        ([TrajectSjabloonMijlpaalId], [TriggerEvent], [TriggerActie], [ActieParametersJson], [MagProjectWijzigen], [IsActief], [Omschrijving])
    SELECT m.[Id], t.[Event], t.[Actie], t.[Params], 0, 1, t.[Omschr]
    FROM [dbo].[TrajectSjabloonMijlpaal] m
    JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
    JOIN (VALUES
        (N'VERGUNNING_DEFINITIEF', 0, 7, NULL, N'Ontgrendelt de fase basisakte/VME-opstart.'),
        (N'BASISAKTE',             0, 7, NULL, N'Ontgrendelt de verkoopfase.'),
        (N'VERKOOPDREMPEL',        0, 0, N'{"rol":4}', N'Verkoopdrempel bereikt — financiering/bouwstart evalueren.')
    ) AS t([MijlpaalCode], [Event], [Actie], [Params], [Omschr]) ON t.[MijlpaalCode] = m.[Code]
    WHERE f.[TrajectSjabloonId] = @app;

    PRINT 'Sjabloon "Appartementen (mede-eigendom / VME)" aangemaakt.';
END
ELSE
    PRINT 'Sjabloon "Appartementen (mede-eigendom / VME)" bestaat al, overgeslagen.';

PRINT 'Migratie 041_TrajectSjablonenResidentieel voltooid.';
