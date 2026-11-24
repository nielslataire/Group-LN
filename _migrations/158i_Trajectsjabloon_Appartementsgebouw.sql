-- =============================================
-- 158i (24/11/2026): trajectsjabloon "Appartementsgebouw (projectontwikkeling en bouw)".
--   Voor een appartementsgebouw zonder verkavelingsvergunning, gezien door de projectontwikkelaar (grond, vergunning, juridisch, financiering, verkoop)
--   EN door de projectleider (voorbereiding, uitvoering, oplevering). ProjectType 1 (Woonproject), NIET standaard: de bestaande sjablonen blijven onaangeroerd.
--
--   * Mijlpalen in de fase Uitvoering hebben als code een BOUWSTAAT (WERF_UITGEZET, FUNDERING_KLAAR, METSELWERK_RUWBOUW, BUITENSCHRIJNWERK, TECHNIEKEN_RUW,
--     PLEISTER, CHAPE, KEUKEN, OMGEVINGSWERKEN, VOORLOPIGE_OPLEVERING): ze volgen de taak die de bouwstaat als laatste oplevert in de planning (PlanningTrajectKoppeling).
--     Dat zijn enkel bouwstaten die het planningssjabloon "Appartementsgebouw" (158f) altijd oplevert, ook zonder kelder en met plat of hellend dak.
--   * Scope 1 (per eenheid) enkel voor wat per appartement verschilt: compromis, akte, oplevering van het appartement en garantie.
--   * Datums van compromis, akte en oplevering komen uit de klantenrekening (ComputedBinding 7) en de startdatum bouw uit het project (2), zoals in 041.
--   * Elke fase wordt ontgrendeld door de laatste mijlpaal ervan (DeblokkeerVolgendeFase), behalve de eerste.
--   Doeldatums: elke mijlpaal heeft één anker (de strakste afhankelijkheid); de andere staan in de omschrijving.
--   Termijnen en wettelijke punten (termijn van orde, beroepstermijn 35 dagen, Breyne, 10 jaar) zijn indicatief: laat ze nakijken.
--   InterneRol: 1=Projectleider 2=Projectontwikkelaar 3=CeoCfo 4=Boekhouder 5=Verkoper 6=Architect 7=Extern
--   MijlpaalType: 0=Algemeen 1=Administratief 2=Vergunning 3=Werf 4=Verkoop 5=Financieel 6=Keuring 7=Oplevering
--   Idempotent: bestaat het sjabloon al, dan gebeurt er niets.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @naam NVARCHAR(200) = N'Appartementsgebouw (projectontwikkeling en bouw)';
IF EXISTS (SELECT 1 FROM [dbo].[TrajectSjabloon] WHERE [Naam] = @naam)
BEGIN PRINT N'Het trajectsjabloon "' + @naam + N'" bestaat al: niets gedaan.'; RETURN; END

BEGIN TRAN;

DECLARE @t INT;
INSERT INTO [dbo].[TrajectSjabloon] ([Naam], [ProjectType], [IsStandaard], [IsActief], [Omschrijving])
VALUES (@naam, 1, 0, 1,
        N'Appartementsgebouw zonder verkavelingsvergunning: van grondoptie, vergunning, basisakte en verkoop tot financiering, bouw (volgt de planning via bouwstaten), oplevering en garantie.');
SET @t = SCOPE_IDENTITY();

INSERT INTO [dbo].[TrajectSjabloonFase] ([TrajectSjabloonId], [Naam], [Code], [Volgorde], [StandaardProjectStatusId])
VALUES
    (@t, N'Grond & haalbaarheid',      N'AANKOOP',         10, NULL),
    (@t, N'Ontwerp & vergunning',      N'VERGUNNING',      20, 4),
    (@t, N'Basisakte & VME-opstart',   N'JURIDISCH',       30, NULL),
    (@t, N'Verkoop',                   N'VERKOOP',         40, 5),
    (@t, N'Voorbereiding bouw',        N'VOORBEREIDING',   50, NULL),
    (@t, N'Uitvoering',                N'UITVOERING',      60, 2),
    (@t, N'Oplevering & nazorg',       N'OPLEVERING',      70, 1);

;WITH f AS (SELECT [Id], [Code] FROM [dbo].[TrajectSjabloonFase] WHERE [TrajectSjabloonId] = @t)
INSERT INTO [dbo].[TrajectSjabloonMijlpaal]
    ([TrajectSjabloonFaseId], [Naam], [Code], [Volgorde], [MijlpaalType], [VerantwoordelijkeRol], [Scope], [DoeldatumAnkerCode], [DoeldatumOffsetDagen], [IsVerplicht], [BronBinding], [BronParam], [Omschrijving])
SELECT f.[Id], v.[Naam], v.[Code], v.[Volgorde], v.[MijlpaalType], v.[Rol], v.[Scope], v.[Anker], v.[Offset], v.[Verplicht], v.[Binding], v.[Param], v.[Omschr]
FROM f
JOIN (VALUES
    -- Grond & haalbaarheid (projectontwikkelaar)
    (N'AANKOOP',       N'Optie op de grond',                               N'GRONDOPTIE',                 10, 1, 2, 0, N'PROJECT_CREATED',            NULL, 1, NULL, NULL, N'Optie genomen op de grond, met als voorwaarde een definitieve vergunning.'),
    (N'AANKOOP',       N'Haalbaarheidsstudie goedgekeurd',                 N'HAALBAARHEIDSSTUDIE',        20, 0, 2, 0, N'GRONDOPTIE',                 NULL, 1, NULL, NULL, N'Financieel plan en bouwprogramma intern goedgekeurd (go/no-go).'),
    -- Ontwerp & vergunning
    (N'VERGUNNING',    N'Schetsontwerp en voorafgaand overleg met de gemeente', N'SCHETSONTWERP',        10, 2, 6, 0, N'HAALBAARHEIDSSTUDIE',        NULL, 1, NULL, NULL, N'Architect maakt het schetsontwerp; overleg met de dienst stedenbouw.'),
    (N'VERGUNNING',    N'Omgevingsvergunning aangevraagd',                 N'VERGUNNING_AANVRAAG',        20, 2, 6, 0, N'SCHETSONTWERP',              NULL, 1, NULL, NULL, N'Dossier ingediend. Binnen 30 dagen moet het volledig of onvolledig verklaard worden.'),
    (N'VERGUNNING',    N'Dossier volledig verklaard',                      N'DOSSIER_VOLLEDIG',           30, 2, 1, 0, N'VERGUNNING_AANVRAAG',        30,   1, NULL, NULL, N'Uiterlijk 30 dagen na de aanvraag. Hier start de behandelingstermijn van 105 dagen.'),
    (N'VERGUNNING',    N'Openbaar onderzoek',                              N'OPENBAAR_ONDERZOEK',         40, 2, 1, 0, N'DOSSIER_VOLLEDIG',           NULL, 0, NULL, NULL, N'Optioneel: niet elke aanvraag doorloopt een openbaar onderzoek.'),
    (N'VERGUNNING',    N'Vergunning verleend',                             N'VERGUNNING_VERLEEND',        50, 2, 1, 0, N'DOSSIER_VOLLEDIG',           105,  1, NULL, NULL, N'Uiterlijk 105 dagen na de volledigverklaring (behandelingstermijn). Start de beroepstermijn van 35 dagen.'),
    (N'VERGUNNING',    N'Vergunning definitief',                           N'VERGUNNING_DEFINITIEF',      60, 2, 1, 0, N'VERGUNNING_VERLEEND',        35,   1, NULL, NULL, N'Definitief 35 dagen na de beslissing, voor zover er geen beroep werd ontvangen.'),
    -- Basisakte & VME-opstart (juridisch)
    (N'JURIDISCH',     N'Aankoopakte grond verleden',                      N'AANKOOPAKTE_GROND',          10, 1, 7, 0, N'VERGUNNING_DEFINITIEF',      NULL, 0, NULL, NULL, N'Meestal pas na de definitieve vergunning (opschortende voorwaarde), maar dat verschilt per project. Daarom niet verplicht: pas de datum aan of laat hem weg.'),
    (N'JURIDISCH',     N'Basisakte en reglement van mede-eigendom',        N'BASISAKTE',                  20, 1, 7, 0, N'VERGUNNING_DEFINITIEF',      NULL, 1, NULL, NULL, N'Opgemaakt door de notaris, op basis van het bouwprogramma en de quotiteiten.'),
    (N'JURIDISCH',     N'Voorlopige aanstelling syndicus',                 N'SYNDICUS_VOORLOPIG',         30, 1, 7, 0, N'BASISAKTE',                  NULL, 1, NULL, NULL, NULL),
    -- Verkoop
    (N'VERKOOP',       N'Vrijgave commercialisatie',                       N'COMMERCIALISATIE',           10, 4, 5, 0, N'BASISAKTE',                  NULL, 1, NULL, NULL, N'Verkoopdossier compleet: prijslijst, plannen, lastenboek, verkoopsovereenkomst.'),
    (N'VERKOOP',       N'Compromis appartement',                           N'VERKOOP_COMPROMIS',          20, 4, 5, 1, N'COMMERCIALISATIE',           NULL, 1, 7,    N'DateSalesAgreement', N'Datum uit de klantenrekening van het appartement.'),
    (N'VERKOOP',       N'Waarborg Wet Breyne aangevraagd (bij de eerste verkoop)', N'BREYNE_AANVRAAG',   30, 5, 3, 0, N'COMMERCIALISATIE',           NULL, 1, NULL, NULL, N'Aan te vragen zodra het eerste appartement verkocht is.'),
    (N'VERKOOP',       N'Waarborg Wet Breyne in orde (vóór de eerste akte)', N'BREYNE_WAARBORG',         40, 5, 3, 0, N'BREYNE_AANVRAAG',            NULL, 1, NULL, NULL, N'Moet er zijn vóór de eerste notariële akte.'),
    (N'VERKOOP',       N'Verkoopdrempel bereikt',                          N'VERKOOPDREMPEL',             50, 5, 4, 0, N'COMMERCIALISATIE',           NULL, 1, NULL, NULL, N'Drempel voor de bankfinanciering: 15 % van de appartementen verkocht. Niet automatisch gemeten: manueel afvinken.'),
    (N'VERKOOP',       N'Notariële akte appartement',                      N'NOTARIELE_AKTE',             60, 4, 7, 1, N'VERKOOP_COMPROMIS',          NULL, 1, 7,    N'DateDeedOfSale', N'Datum uit de klantenrekening van het appartement. Pas mogelijk als de Breyne-waarborg in orde is.'),
    -- Voorbereiding bouw (projectleider)
    (N'VOORBEREIDING', N'Bankfinanciering project rond',                   N'FINANCIERING',               10, 5, 4, 0, N'VERKOOPDREMPEL',             NULL, 1, NULL, NULL, N'Kredietovereenkomst getekend en voorwaarden voldaan.'),
    (N'VOORBEREIDING', N'Uitvoeringsplannen en stabiliteitsstudie klaar',  N'UITVOERINGSDOSSIER',         20, 3, 6, 0, N'VERGUNNING_DEFINITIEF',      NULL, 1, NULL, NULL, N'Architect en studiebureau: uitvoeringsplannen, stabiliteit, EPB en technieken.'),
    (N'VOORBEREIDING', N'Aannemers aangesteld (aanbesteding afgerond)',    N'AANBESTEDING',               30, 3, 1, 0, N'UITVOERINGSDOSSIER',         NULL, 1, NULL, NULL, N'Offertes vergeleken en aannemers gekozen per discipline.'),
    (N'VOORBEREIDING', N'Veiligheidscoördinator en veiligheidsdossier',    N'VEILIGHEIDSCOORDINATIE',     40, 3, 1, 0, N'UITVOERINGSDOSSIER',         NULL, 1, NULL, NULL, N'Coördinator ontwerp en uitvoering aangesteld; startdossier en PID opgemaakt.'),
    (N'VOORBEREIDING', N'Verzekeringen bouwheer afgesloten',               N'VERZEKERINGEN',              50, 1, 1, 0, N'FINANCIERING',               NULL, 1, NULL, NULL, N'Bouwplaatsverzekering (TAR) en burgerlijke aansprakelijkheid van de bouwheer, vóór de start van de werken.'),
    (N'VOORBEREIDING', N'Start uitvoering',                                N'UITVOERING_START',           60, 3, 1, 0, N'AANBESTEDING',               NULL, 1, 2,    N'StartDateConstruction', N'Kan pas starten als ook de financiering, de verzekeringen en de veiligheidscoördinatie klaar zijn (anker is de aanbesteding).'),
    -- Uitvoering: de mijlpalen volgen de planning via hun bouwstaat-code
    (N'UITVOERING',    N'Werf ingericht en uitgezet',                      N'WERF_UITGEZET',              10, 3, 1, 0, N'UITVOERING_START',           NULL, 1, NULL, NULL, N'Volgt de planning (bouwstaat "Werf ingericht en uitgezet").'),
    (N'UITVOERING',    N'Funderingen klaar',                               N'FUNDERING_KLAAR',            20, 3, 1, 0, N'WERF_UITGEZET',              NULL, 1, NULL, NULL, N'Volgt de planning (bouwstaat "Fundering klaar").'),
    (N'UITVOERING',    N'Ruwbouw: metselwerk afgewerkt',                   N'METSELWERK_RUWBOUW',         30, 3, 1, 0, N'FUNDERING_KLAAR',            NULL, 1, NULL, NULL, N'Volgt de planning: de laatste bouwlaag is gemetseld.'),
    (N'UITVOERING',    N'Gesloten ruwbouw (buitenschrijnwerk geplaatst)',  N'BUITENSCHRIJNWERK',          40, 3, 1, 0, N'METSELWERK_RUWBOUW',         NULL, 1, NULL, NULL, N'Volgt de planning. Vanaf hier is minstens 75 % van de schijven bereikt.'),
    (N'UITVOERING',    N'Leidingen en technieken geplaatst',               N'TECHNIEKEN_RUW',             50, 3, 1, 0, N'BUITENSCHRIJNWERK',          NULL, 1, NULL, NULL, N'Volgt de planning (elektriciteit en sanitair/verwarming).'),
    (N'UITVOERING',    N'Pleisterwerken klaar',                            N'PLEISTER',                   60, 3, 1, 0, N'TECHNIEKEN_RUW',             NULL, 1, NULL, NULL, N'Volgt de planning.'),
    (N'UITVOERING',    N'Chape geplaatst',                                 N'CHAPE',                      70, 3, 1, 0, N'PLEISTER',                   NULL, 1, NULL, NULL, N'Volgt de planning.'),
    (N'UITVOERING',    N'Afwerking klaar (keuken geplaatst)',              N'KEUKEN',                     80, 3, 1, 0, N'CHAPE',                      NULL, 1, NULL, NULL, N'Volgt de planning: tegelwerk, parket, binnenschrijnwerk en keuken.'),
    (N'UITVOERING',    N'Omgevingswerken klaar',                           N'OMGEVINGSWERKEN',            90, 3, 1, 0, N'KEUKEN',                     NULL, 1, NULL, NULL, N'Volgt de planning.'),
    (N'UITVOERING',    N'Voorlopige oplevering gebouw (ontvangst sleutels)', N'VOORLOPIGE_OPLEVERING',   100, 7, 1, 0, N'UITVOERING_DEADLINE',        0,    1, 2, N'DeliveryDate', N'Streefdatum = uiterlijke einde van de bouwwerken (aanvangsdatum bouw + uitvoeringstermijn). Bereikt zodra de werkelijke datum van de voorlopige oplevering op het project staat. Daarna volgen de opleveringen per appartement.'),
    -- Oplevering & nazorg
    (N'OPLEVERING',    N'Voorlopige oplevering appartement',               N'VOORLOPIGE_OPLEVERING_EENHEID', 10, 7, 1, 1, N'VOORLOPIGE_OPLEVERING',   NULL, 1, 7,    N'DeliveryDate', N'Datum uit de klantenrekening van het appartement. Start de tienjarige garantie.'),
    (N'OPLEVERING',    N'Oprichting VME en definitieve syndicus',          N'OPRICHTING_VME',             20, 1, 7, 0, N'VOORLOPIGE_OPLEVERING',      NULL, 1, NULL, NULL, N'Eerste algemene vergadering; hangt ook af van de voorlopige syndicus.'),
    (N'OPLEVERING',    N'Definitieve oplevering gebouw',                   N'DEFINITIEVE_OPLEVERING_GEBOUW', 30, 7, 1, 0, N'VOORLOPIGE_OPLEVERING',   365, 1, 2, N'DeliveryDateDef', N'Definitieve oplevering van het ganse gebouw (gemeenschappelijke delen), minstens 1 jaar na de voorlopige oplevering. Herinnering aan de projectleider en de bestuurders 30 dagen vooraf.'),
    (N'OPLEVERING',    N'Definitieve oplevering appartement',              N'DEFINITIEVE_OPLEVERING_EENHEID', 40, 7, 1, 1, N'VOORLOPIGE_OPLEVERING_EENHEID', NULL, 1, 7, N'DeliveryDateDef', N'Datum uit de klantenrekening van het appartement.'),
    (N'OPLEVERING',    N'Einde garantieperiode (10 jaar na voorlopige oplevering)', N'NAZORG',              50, 0, 1, 1, N'VOORLOPIGE_OPLEVERING_EENHEID', 3650, 1, NULL, NULL, N'Tien jaar na de voorlopige oplevering van het appartement; het dossier kan gearchiveerd worden.')
) AS v([FaseCode], [Naam], [Code], [Volgorde], [MijlpaalType], [Rol], [Scope], [Anker], [Offset], [Verplicht], [Binding], [Param], [Omschr])
  ON v.[FaseCode] = f.[Code];

-- Triggers: BijBereiken (event 0); actie 0 = verwittig rol, 2 = maak taak, 7 = ontgrendel de volgende fase
INSERT INTO [dbo].[TrajectSjabloonMijlpaalTrigger]
    ([TrajectSjabloonMijlpaalId], [TriggerEvent], [TriggerActie], [ActieParametersJson], [MagProjectWijzigen], [IsActief], [Omschrijving])
SELECT m.[Id], x.[Event], x.[Actie], x.[Params], 0, 1, x.[Omschr]
FROM [dbo].[TrajectSjabloonMijlpaal] m
JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
JOIN (VALUES
    -- Taken voor de bestuurders (rol 3) zijn taken op rol: een mail naar een rol kan enkel bij projectleider en verkoper aankomen.
    (N'HAALBAARHEIDSSTUDIE',   0, 2, N'{"titel":"Go/no-go beslissing haalbaarheid","rol":3}', N'Go/no-go beslissing vereist.'),
    (N'HAALBAARHEIDSSTUDIE',   0, 7, NULL, N'Ontgrendelt de fase Ontwerp & vergunning.'),
    (N'VERGUNNING_VERLEEND',   0, 2, N'{"titel":"Beroepstermijn (35 dagen) opvolgen","rol":1}', NULL),
    (N'VERGUNNING_DEFINITIEF', 0, 7, NULL, N'Ontgrendelt de fase Basisakte & VME-opstart.'),
    (N'BREYNE_AANVRAAG',       0, 2, N'{"titel":"Waarborg Wet Breyne aanvragen (eerste verkoop)","rol":3}', N'Taak voor de bestuurders.'),
    (N'BASISAKTE',             0, 7, NULL, N'Ontgrendelt de verkoopfase.'),
    (N'VERKOOPDREMPEL',        0, 2, N'{"titel":"Bankfinanciering aanvragen en kredietovereenkomst voorbereiden","rol":4}', N'Verkoopdrempel (15 %) bereikt.'),
    (N'VERKOOPDREMPEL',        0, 7, NULL, N'Ontgrendelt de fase Voorbereiding bouw.'),
    (N'UITVOERING_START',      0, 7, NULL, N'Ontgrendelt de fase Uitvoering.'),
    (N'VOORLOPIGE_OPLEVERING', 0, 7, NULL, N'Ontgrendelt de fase Oplevering & nazorg.'),
    -- Herinnering 30 dagen vóór de definitieve oplevering van het gebouw: mail aan de projectleider en een taak voor de bestuurders (event 2 = X dagen voor de doeldatum, actie 6 = herinnering)
    (N'DEFINITIEVE_OPLEVERING_GEBOUW', 2, 6, NULL, N'Herinnering aan de projectleider.'),
    (N'DEFINITIEVE_OPLEVERING_GEBOUW', 2, 2, N'{"titel":"Definitieve oplevering gebouw nadert","rol":3,"offsetDagen":30}', N'Taak voor de bestuurders.')
) AS x([MijlpaalCode], [Event], [Actie], [Params], [Omschr]) ON x.[MijlpaalCode] = m.[Code]
WHERE f.[TrajectSjabloonId] = @t;

-- De herinnering (event 2) gaat 30 dagen vóór de doeldatum af
UPDATE tr SET tr.[OffsetDagen] = 30
FROM [dbo].[TrajectSjabloonMijlpaalTrigger] tr
JOIN [dbo].[TrajectSjabloonMijlpaal] m ON m.[Id] = tr.[TrajectSjabloonMijlpaalId]
JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
WHERE f.[TrajectSjabloonId] = @t AND tr.[TriggerEvent] = 2;

-- Controle: ankers die niet bestaan (moet leeg zijn; PROJECT_CREATED is een vaste gebeurtenis)
SELECT 'Anker bestaat niet' AS Controle, m.[Code], m.[DoeldatumAnkerCode]
FROM [dbo].[TrajectSjabloonMijlpaal] m JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
WHERE f.[TrajectSjabloonId] = @t AND m.[DoeldatumAnkerCode] <> N'PROJECT_CREATED'
  AND NOT EXISTS (SELECT 1 FROM [dbo].[TrajectSjabloonMijlpaal] a JOIN [dbo].[TrajectSjabloonFase] fa ON fa.[Id] = a.[TrajectSjabloonFaseId] WHERE fa.[TrajectSjabloonId] = @t AND a.[Code] = m.[DoeldatumAnkerCode]);
-- Controle: bouwstaat-mijlpalen zonder bouwstaat in de tabel (moet leeg zijn; draai anders 158, 158e en 158h)
SELECT 'Bouwstaat ontbreekt' AS Controle, m.[Code]
FROM [dbo].[TrajectSjabloonMijlpaal] m JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
WHERE f.[TrajectSjabloonId] = @t AND f.[Code] = N'UITVOERING' AND m.[Code] <> N'VOORLOPIGE_OPLEVERING_EENHEID'
  AND NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] b WHERE b.[Code] = m.[Code] AND b.[ProjectId] IS NULL);

SELECT f.[Volgorde] AS Fase, f.[Naam] AS FaseNaam, m.[Volgorde], m.[Naam], m.[Code], m.[Scope], m.[DoeldatumAnkerCode] AS Anker, m.[DoeldatumOffsetDagen] AS Dagen
FROM [dbo].[TrajectSjabloonMijlpaal] m JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
WHERE f.[TrajectSjabloonId] = @t ORDER BY f.[Volgorde], m.[Volgorde];

COMMIT;
PRINT N'KLAAR: trajectsjabloon aangemaakt. Open Instellingen > Sjablonen > Traject om het na te kijken.';
