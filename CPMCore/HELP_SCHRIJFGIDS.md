# Schrijfgids voor helpartikels

Hoort bij het Helpcentrum (`CPMCore/HelpContent/`). De technische kant (bestand, frontmatter, boom,
screenshots) staat in `CPMCore/DESIGN.md`, sectie "Een nieuw help-artikel schrijven — stappenplan". Dit
bestand regelt de **inhoud en toon**. Woordenlijst en bedrijfslogica worden bijgehouden door Niels:
alles met `[VRAAG: ...]` is nog niet beantwoord en mag niet als feit in een artikel komen.

## Doelgroep
Projectleiders, administratief medewerkers en verkopers bij een bouwbedrijf/projectontwikkelaar.
Niet technisch. Lezen vaak op tablet, op de werf, tussen twee dingen door.
Ze openen de help omdat ze iets willen gedaan krijgen, niet om de software te leren kennen.

## Basisregels
- Eén artikel beantwoordt één vraag of één taak. Titel = taak met werkwoord
  ("Een partner toevoegen"), niet de naam van een scherm.
- Schrijf in de je-vorm, actief, in eenvoudig Nederlands. Zinnen van maximaal 20 woorden.
- Gebruik geen gedachtestreepjes (—) en zo weinig mogelijk haakjes en puntkomma's.
- Knop- en veldnamen schrijf je in **vet**, exact zoals ze in de interface staan.
- Gebruik geen interne termen, code-namen, scherm-ID's of developerjargon.
  Gebruik alleen woorden uit de woordenlijst hieronder.
- Leg altijd uit wat er GEBEURT na een handeling: wat ziet de gebruiker,
  wat verandert er voor anderen (klant, website, collega's), is het omkeerbaar?
- Som geen velden op die voor zichzelf spreken. Leg alleen velden uit waarbij
  de betekenis of het gevolg niet vanzelfsprekend is.
- Gebruik genummerde stappen alleen voor handelingen in volgorde.

## Vaste opbouw van een taakartikel
1. Titel (taak)
2. Eén zin: wat bereik je hiermee en wanneer heb je het nodig
3. Voor je begint (alleen rechten/voorwaarden die echt nodig zijn)
4. Stappen (één handeling per stap)
5. Resultaat
6. Let op / tips
7. Veelgestelde vragen (echte vragen, geen opvulling)
8. Gerelateerde artikels

## Woordenlijst
Elke term hieronder: wat het is, en welk woord je **wel** en **niet** gebruikt. Een term die hier
niet staat en die je niet zeker kent: vraag het, of zet `[VRAAG: ...]` in de tekst.

### Kernbegrippen
- Project: [VRAAG: wat is een project precies, en wat is het verschil met een werf en een dossier? Welk woord gebruiken we naar de gebruiker toe?]
- Coördinatieproject: een project waarbij wij ... [VRAAG: uitleg aanvullen, en hoe verschilt het in het scherm van een gewoon project?]
- Totale grondaandelen: bij een project met basisakte is de grond verdeeld in een aantal aandelen. Dit veld (tabblad Status & foto) bevat dat totaal. Elke eenheid heeft een aantal grondaandelen en alle eenheden samen moeten het totaal halen. Bij een eenheid heet het veld **Aandeel basisakte**. Je vult het in bij een nieuwe eenheid, of verdeelt alles via het scherm **Aandelen basisakte** (knop **Aandelen verdelen**). De eenhedenlijst toont de som tegenover het totaal en een melding als ze niet klopt. De notaris heeft de verdeling nodig voor de verkoopakte. Leeg betekent nog geen aandeel, 0 betekent bewust geen aandeel. Zie het artikel "De aandelen van de basisakte verdelen over de eenheden".
- Grondwaarde en bouwwaarde: bij een eenheid vul je beide in onder **Bedragen**. Grondwaarde plus bouwwaarde is de verkoopprijs. Niet te verwarren met het aandeel basisakte, dat is een aantal.
- Grondaandeel: de waarde van de grond voor het appartement of de woning. Het facturatiebedrijf grondeigenaar factureert het. De koper krijgt twee facturen: één voor de aanneming en één voor het grondaandeel.
- Aanneming: de constructiewaarde van een eenheid. Het facturatiebedrijf aanneemer factureert ze aan de kopers. Het grondaandeel is de grondwaarde.
- Werfmelding: aangifte van de start van de werken bij de overheid. Bevat datum, bindingsdatum en dossiernummer. [VRAAG: wat is de bindingsdatum precies, en doet de app er iets mee of is het enkel registratie?]
- Projectbeheerder / projectleider: [VRAAG: zijn dit dezelfde persoon of rol? Welk woord kiezen we?]
- Projectcode: het dossiernummer, een koppelteken, de maand en het jaar van de startdatum. CPM vult hem niet zelf in. [VRAAG: moet de code uniek zijn?]

### Partijen
- Bouwheer / bouwheer: [VRAAG: wie is dit in onze context, en gebruiken we "bouwheer" of "opdrachtgever" naar de gebruiker?]
- Projectontwikkelaar: [VRAAG: altijd Group LN, of kan dit per project een ander bedrijf zijn?]
- Facturatiebedrijf: [VRAAG: uitleg, per rol (aanneming resp. grondaandeel)]
- Klant / klantenaccount: de eerste eigenaar van een woning of unit. [VRAAG: zeg je "klant", "koper" of "eigenaar" tegen de gebruiker?]
- Mede-eigenaar: iemand die samen met de klant eigenaar is. Wordt enkel via de projectschermen toegevoegd.
- Contactpersoon: iemand die namens de klant contact heeft, zonder eigenaar te zijn. [VRAAG: krijgt een contactpersoon toegang tot het klantenportaal?]
- Leverancier / onderaannemer: [VRAAG: is dit hetzelfde of niet? Wat is een "partner"?]
- Notaris, architect, ...: [VRAAG: lijst van rollen die in de app voorkomen en hoe je ze noemt]

### Financieel
- Budget / budgetversie: [VRAAG: wat betekent een versie, wanneer maak je er een nieuwe, en wat is de "definitieve" versie?]
- Offerte: [VRAAG: wanneer is een offerte "verstuurd", "aanvaard", "geweigerd"? Wat gebeurt er bij elke status?]
- Wijzigingsopdracht: [VRAAG: definitie, en wanneer wordt het een factuur of betalingsschijf?]
- Betalingsschijf: [VRAAG: definitie en hoe de schijven ontstaan]
- Factuur / voorschotfactuur / creditnota: [VRAAG: welke soorten bestaan er en wanneer gebruik je welke?]
- Financiële waarborg: een percentage (bv. 5 %) dat bij elke factuur van de leverancier wordt
  afgehouden, tot een bepaalde periode of voorwaarde vervuld is.
- Bankwaarborg: een percentage dat een bank waarborgt voor de leverancier, met een attest als bewijs.
  Er wordt niets afgehouden van de facturen, het waarborgdocument (het attest) moet wel toegevoegd
  worden. Zie "Het contract van een leverancier bekijken".
- Borg: het deel van een factuur dat je inhoudt bij een financiële waarborg (percentage x (factuurbedrag min extra inhouding), zonder btw). De borg komt later vrij. Zeg "borg" tegen de gebruiker, niet "waarborg" (dat is het contracttype). Zie "Borg inhouden op een factuur".
- Vrijgave van de borg: het deel dat bij de voorlopige oplevering (VO) vrijkomt staat op het contract (leeg is 50 %), de rest komt vrij bij de definitieve oplevering (DO). Open punten van de aannemer blokkeren de vrijgave niet. Zie "De borgen van een project volgen en vrijgeven". [VRAAG: hoe geef je een vrijgave door aan de boekhouding, en moet een foute vrijgave gecorrigeerd kunnen worden?]
- Korting contant: een korting als je een factuur snel betaalt, bijvoorbeeld 2 % binnen 10 dagen. Ze komt uit het contract. Bij een factuur zonder contract vul je ze zelf in en wordt ze bij de factuur bewaard. Ze wordt berekend op het factuurbedrag zonder btw, niet op wat er na borg overblijft. Zie "Een factuur van een leverancier toevoegen of aanpassen".
- Kaart Te betalen: de kaart rechts in het factuurformulier die live uitrekent wat je betaalt: factuurbedrag, min extra inhouding en borg, en daarna eventueel met korting contant.
- Extra inhouding: een bedrag dat je bij één factuur tijdelijk niet uitbetaalt (reden: creditnota te ontvangen, werk niet volledig uitgevoerd, andere). Er gaat geen borg op. Je handelt ze af door vrij te geven (het contractpercentage blijft dan als borg ingehouden), te verrekenen met een creditnota of af te boeken. Zie "Een bedrag tijdelijk niet uitbetalen met een extra inhouding".
- Vrijgeven (borg of inhouding): in CPM leg je enkel vast dat het bedrag mag worden uitbetaald. Er is geen betaalmodule.

### Opvolging en documenten
- Punt: [VRAAG: definitie ("issue"/actiepunt?), wie kan het aanmaken, wat doet de status?]
- Verslag: [VRAAG: definitie en het verband met punten]
- Traject / mijlpaal: [VRAAG: definitie en wat gebeurt er als een mijlpaal afgevinkt wordt?]
- Dossier: [VRAAG: wat zit erin en wie ziet het?]
- Document / aanvraag (voor een document): [VRAAG: wat is een documentaanvraag, en wat ziet de klant?]
- Publieke projectpagina / website: [VRAAG: noem je dit "de website", "het klantenportaal" of "de publieke pagina"?]

### Begrippen uit de bestaande artikels (nog te verduidelijken)
- Eenheid / lot: [VRAAG: zijn dit hetzelfde, en welk woord gebruiken we naar de gebruiker toe? (woning, appartement, garage ...)]
- Nacalculatie: [VRAAG: uitleg in één zin, en het verschil met het budget]
- Activiteit (in de nacalculatie): [VRAAG: definitie]
- Fysieke voortgang / financiële voortgang: de werf-stand tegenover de facturatie-stand. [VRAAG: hoe wordt het percentage berekend?]
- Onderfacturatie / Grote afwijking: [VRAAG: vanaf welk verschil in procent geeft CPM deze melding?]
- Peildatum, S-index, I-index, referentieproject: [VRAAG: uitleg van elk, voor wie de budgetwizard voor het eerst gebruikt]
- Weerverletdag: een dag waarop wind of regen voor meer dan 4 uur oponthoud zorgt. Het weerstation van het project dient om ze te berekenen. Elke weerverletdag verschuift de opleverdatum automatisch.
- Uitvoeringstermijn: het aantal kalenderdagen dat de uitvoering duurt.
- Contractschijf, regiewerk, uurtarief: [VRAAG: uitleg bij coördinatieprojecten]
- Verkoopverantwoordelijke: [VRAAG: wie is dit, en wat doet CPM met die keuze?]
- Wijzigingsopdracht (WO), meerwerk, minwerk: een wijzigingsopdracht legt meerwerk of minwerk vast voor één eenheid. [VRAAG: schrijven we "WO" ooit uit, en wanneer wordt het een factuur?]
- Commissie: [VRAAG: wat is dit, voor wie, en waarom ziet de klant ze niet apart?]
- itsme / klantenportaal: eigenaars tekenen een wijzigingsopdracht via het klantenportaal met itsme. [VRAAG: kan een eigenaar ook op een andere manier tekenen?]
- Concept, Ter goedkeuring, Doorgestuurd, Gemeld uitgevoerd, Afgesloten (fases van een punt): [VRAAG: wat betekent elke fase, en wie zet het punt naar de volgende?]
- Mijlpaal: [VRAAG: definitie en wie vinkt ze af]

### Budget (uit de budgetartikels)
- Budget en versie: een budget is een berekening voor een project, bijvoorbeeld een variant met 8 woningen. Elk budget heeft genummerde versies.
- Status van een versie: **Concept** (je werkt eraan), **Afgerond** (klaar, nog aanpasbaar) en **Definitief** (vergrendeld, één per project). Huidig is de versie waaraan je werkt.
- Actief budget: het budget van een project dat CPM gebruikt voor de nacalculatie. Per project is er één. Een budget dat niet actief is, kan verwijderd worden.
- Peildatum: de datum waarop de indexen gelden. Na 6 maanden geeft CPM een waarschuwing. S-index: de index van de lonen. I-index: de index van de materiaalprijzen.
- Bouwkostindexering: de prijzen uit de nacalculatie, aangepast met de S-index (lonen) en de I-index (materialen).
- Nacalculatie en referentieproject: de bouwkost van afgewerkte projecten, geïndexeerd naar vandaag. Je vergelijkt het budget ermee in stap 6. [VRAAG: wie beheert de lijst referentieprojecten en welke projecten komen erin?]
- Lot: een groep van werken in stap 6, bijvoorbeeld Lot 2 Bovenbouw. [VRAAG: klopt deze uitleg van lot in het budget? Lot heeft ook een andere betekenis bij eenheden.]
- Correctie: een percentage op het berekende bedrag van een activiteit. 100 betekent geen correctie. Een correctie vraagt een opmerking.
- Gereduceerde oppervlakte en VMSW-factoren: elke soort oppervlakte telt met een eigen factor mee, bijvoorbeeld tuin met een kleine factor. De factoren zijn per versie aanpasbaar. [VRAAG: wat betekent VMSW, en waar komen de standaardfactoren vandaan?]
- Bouwwaarde en grondwaarde: samen de verkoopprijs van een eenheid. Bouwwaarde rekent op de gereduceerde oppervlakte, grondwaarde op de grondoppervlakte.
- Prijscode: een vaste prijs per m² met een code, bijvoorbeeld B-01 (bouw) of G-03 (grond). [VRAAG: wat betekent het teken P achter een code?]
- Decennale: de verzekering uit de Wet Peeters, een verplichte verzekering van 10 jaar.
- Wet Breyne: de kost die de bank aanrekent voor het stellen van een bankwaarborg van 100 %.
- Straight loan: een lening van de bank om een grond of een bouwwerk te financieren.
- Aan te bouwen buren: buren waar je tegenaan bouwt of waar er een risico is op schade.
- Berlinerwanden, secanspalen, onderschoeiingen: specifieke funderingstechnieken.
- Overnemen (stap 1): zet de standaardprijs terug na een eigen aanpassing. Referentie overnemen (stap 6): neemt het bedrag van de geselecteerde referentieprojecten over.
- Voorstel overnemen en Markt overnemen (stap 8): nemen het verkoopprijsvoorstel van CPM of de gemiddelde marktprijs over.
- Doorzetten naar units (stap 8): schrijft grond- en bouwwaarde van de gekoppelde rijen naar de eenheden van het project. Verkochte eenheden blijven ongemoeid.
- Min. verkoopwaarde: kostprijs plus marge. Break-even verkoopwaarde: kostprijs plus verkoopkosten.
- Vergelijken: je zet 2 of 3 versies van één project naast elkaar. De eerste is de basis (standaard het definitieve budget), de verschillen worden daartegenover getoond. Oker is duurder of een lagere opbrengst, groen is goedkoper of een hogere opbrengst.
- Marge (op de pagina Budgetten en bij het vergelijken): het resultaat gedeeld door de totale kostprijs. Het resultaat is de verkoopprijs min de verkoopkosten min de kostprijs. Verkoopkosten zijn geen onderdeel van de kostprijs.
- Overnemen (bij een nieuw budget): de stappen die uit de gekozen versie meekomen. Wat je niet aanvinkt, begint leeg. Een nieuw budget is niet vanzelf actief, tenzij je Meteen actief budget maken aanzet of het het eerste budget van het project is.

### Verzekeringen
- ABR: de verzekering alle bouwplaatsrisico's. Ze dekt de werken tot de voorlopige oplevering. Termijn en verlenging staan in maanden bij de polis, daarna volgt de waarborgperiode. Zeg "ABR" tegen de gebruiker.
- Tienjarige (aansprakelijkheid): de verplichte verzekering uit de wet Peeters-Borsus voor werken aan stabiliteit, waterdichtheid of dak. Een aannemer kan per woning een attest afgeven, of de ontwikkelaar neemt een globale polis die alle aannemers dekt (bij woningen én appartementen). Bij een globale polis is er vaak per eenheid een eigen polis. Zeg "tienjarige" zoals het scherm, niet "decennale".
- BA: de burgerlijke aansprakelijkheid van de aannemer of van het project. Een BA-attest van de aannemer is niet verplicht, maar aanbevolen en op te nemen in het contract.
- Brand nieuwbouw / bestaand gebouw: de brandverzekering. Tot de voorlopige oplevering dekt de ABR het gebouw. Een verkochte eenheid verzekert de koper. Een opgeleverde eenheid zonder koper verzekert de ontwikkelaar. Een bestaand gebouw is verzekerd tot de afbraak of de verkoop.
- Attest aannemer: het bewijs dat een aannemer een verzekering heeft. Het staat per contract en per soort (tienjarige, BA) op het tabblad Attesten aannemers.
- Aandachtspunt (verzekeringen): een melding die CPM zelf afleidt uit de polissen, de contracten, het definitieve budget en de opleveringsdata. Het wordt nergens bewaard en verdwijnt als de oorzaak weg is.
- Aangevraagd (polis): een polis die je bewaarde zonder polisnummer, in afwachting van de maatschappij.
- Makelaar (verzekeringen): een leverancier met de activiteit Verzekeringen. Een polis via een makelaar hangt aan een contract met die makelaar. Een polis zonder makelaar loopt rechtstreeks bij de maatschappij.

### Taal
- Gebruik: "aanmaken" of "toevoegen", "opslaan", "wijzigen" of "bewerken": [VRAAG: kies één woord per handeling zodat alle artikels gelijk klinken]
- Vermijd: id, slug, entity, record, DTO, endpoint, API, database, cache, gl-v2, scherm-ID's.

## Bedrijfslogica die niet uit de code blijkt
Dit is het belangrijkste deel. Per module aanvullen. Pas een artikel af te ronden als de relevante
regel hier staat; anders `[VRAAG: ...]`.

### Projecten
- "Gepubliceerd" aanzetten betekent: het project is zichtbaar op de website. [VRAAG: wat ziet een bezoeker precies, en kun je het weer offline zetten?]
- Status "Opstart", "In uitvoering", "Afgewerkt", "Opgeleverd": [VRAAG: wat verandert er bij elke status, en is het omkeerbaar?]
- Status "Opgeleverd" betekent dat het volledige gebouw of project voorlopig is opgeleverd. [VRAAG: heeft de status nog andere gevolgen?]
- Het weerstation wordt gebruikt om de weerverletdagen van het project te berekenen. Die verschuiven de opleverdatum automatisch.
- De km-afstand uit het adres wordt gebruikt voor: [VRAAG: coördinatie, en wat als het adres ontbreekt?]
- Een project verwijderen kan enkel een beheerder. Archiveren kan nog niet. [VRAAG: wat gebeurt er met gekoppelde facturen en documenten bij verwijderen?]

### Klanten en leveranciers
- Status van een eenheid: **In optie** is tijdelijk, voorbehouden voor één klant. **Verkocht** komt er
  na ondertekening van de compromis (de verkoopovereenkomst). **Akte verleden** komt er na het
  verlijden van de notariële akte.
- Een klant verwijderen kan niet ongedaan gemaakt worden. Heeft die klant al facturatie of
  wijzigingsopdrachten, dan kan hij niet meer verwijderd worden. Bij een geslaagde verwijdering wordt
  de gekoppelde eenheid weer "Nog geen klant".
- Contactaanvragen (vanuit de website, `Projecten/DetailContacts`) en de klantenlijst zijn vandaag
  volledig losse lijsten. Er is geen automatische koppeling als een contact een eenheid koopt.
- Een klantenaccount wordt altijd aangemaakt met minstens één gekoppelde eenheid, dat kan niet leeg
  blijven.
- Alle eenheden van één klant samen bekijk je normaal op de klantfiche (`Klanten/Detail`). De
  gl-v2-versie van die kaart toont vandaag wel enkel de eerste eenheid met een teller "+ N andere" en
  heeft nog geen link naar de volledige lijst.
- "Herinnering sturen" (bij een niet-getekend contract) en "Opvragen" (bij ontbrekende PID-attesten)
  openen enkel het eigen e-mailprogramma van de gebruiker. CPM houdt nergens bij of en wanneer die
  mail effectief verstuurd is.

### Budget
- Fouten blokkeren het afronden van een versie. Waarschuwingen blokkeren niet en je kunt ze negeren per versie.
- Een definitieve versie is vergrendeld. Er is er maar één per project. Een nieuwe definitieve versie maakt de vorige weer Afgerond.
- Het afdrukken als PDF en het exporteren naar Excel in stap 9 zijn nog in opbouw. Schrijf er geen uitleg over tot ze klaar zijn.
- De koppeling van het definitieve budget met contracten en facturen is nog niet gebouwd. De blokken op de pagina Budgetten tonen nog streepjes.
- Een definitief budget wordt enkel herzien als het bedrijf beslissingen neemt die het budget beïnvloeden. [VRAAG: hoe start je een herziening, en is dat een nieuwe budgetversie?]
- [VRAAG: wat gebeurt er met een goedgekeurd budget als de grondgegevens achteraf wijzigen?]

### Facturatie en wijzigingen
- [VRAAG: wat ziet de klant bij een verstuurde offerte, en hoe keurt hij ze goed?]
- Een betaling van een klant kan nog niet in CPM geregistreerd worden. Ze komt later automatisch uit de boekhouding (Octopus). Schrijf er geen knop of stappen voor.
- Verwijdert de gebruiker een conceptfactuur, dan komen de schijven, wijzigingsopdrachten en regie-uren erop weer vrij om opnieuw te factureren. Bij regie-uren wordt ook het gefactureerde uurtarief gewist.

### Punten en verslagen
- Een concept-punt moet door de projectleider goedgekeurd worden. Wie het punt maakte, krijgt geen melding bij goedkeuring.
- Het dashboard van de projectleider toont nog niet dat er punten goed te keuren zijn (die melding is nog niet gebouwd). Schrijf hier geen belofte over in artikels. Zodra ze er is, het artikel "Een punt inspreken" aanpassen.
- [VRAAG: wanneer wordt een punt "afgesloten"?]

### Marktanalyse
- Verkocht (op de Gemeenteanalyse en bij Vergelijkbare panden): de bron toont de unit als verkocht, of de unit is uit het aanbod verdwenen en geldt dan als vermoedelijk verkocht. Vermoedelijk verkocht telt mee als verkocht.
- Doorlooptijd: dagen tussen de eerste waarneming door CPM en de verkoop. Was een unit al verkocht bij de eerste waarneming, dan is ze onbekend.
- Verkooptempo: verkochte units per maand over de gekozen periode. Bij de periode Alles is er geen tempo.
- Vraagprijs: de prijs uit de advertentie, nooit de verkoopprijs. Een verkocht pand toont de laatste vraagprijs.
- Verkoopgraad van een project: het deel van de units dat verkocht is. Kent CPM de units niet, dan staat het percentage uit de advertentie zelf.
- Opgeslagen profielen en laatste zoekacties zijn per gebruiker. [VRAAG: kan een gebruiker een profiel hernoemen of verwijderen, en waar?]
- Bronnen zijn Immoweb en Zimmo. Volgens de productie-instellingen van de worker haalt CPM Immoweb ongeveer eenmaal per dag op (1439 minuten) en Zimmo ongeveer om de drie dagen (4320 minuten). Wijzigt dat in de instellingen, pas dan de artikels aan.

### Verzekeringen (bedrijfslogica)
- Een brandverzekering voor nieuwbouw is pas nodig als de voorlopige oplevering van het project bereikt is en een woning of appartement nog geen koper heeft. Dan toont CPM een rood aandachtspunt, tenzij een lopende brandpolis (niet als bestaand gebouw aangeduid) voor die eenheid of voor het hele project bestaat. CPM kent geen opleveringsdatum per eenheid: de voorlopige oplevering van het project geldt.
- Een tienjarige polis kan voor het hele project gelden (Geldt voor leeg) of per eenheid. Bij een nieuwe polis kan de gebruiker meerdere eenheden aanvinken: CPM maakt dan per eenheid één polis met dezelfde gegevens. Een polis voor het hele project dekt alle eenheden. Contracten met Via projectpolis vragen geen eigen attest van de aannemer; ontbreekt de polis of dekt ze niet alle eenheden, dan komt er een aandachtspunt.
- Het aandachtspunt dat de oorspronkelijke ABR-termijn voorbij is en de verlenging loopt, is een grijze mededeling. Die telt niet mee op het dashboard.
- Wie de tienjarige geeft, kiest de gebruiker één keer voor het hele project (tab Attesten aannemers, kaart Tienjarige voor het hele project): per aannemer een attest, of via een globale projectpolis (geen eigen attest van de aannemers, bij woningen én appartementen). Een opmerking erbij (bv. "polis volgt van de notaris") staat bij het aandachtspunt zolang de polis ontbreekt; het aandachtspunt is dan een grijze mededeling. Een contract met een eigen keuze (Veiligheid en attesten, Tienjarige aansprakelijkheid: via de aannemer, via projectpolis of niet vereist) houdt die keuze; Nog niet bepaald volgt het project. Is er nergens iets gekozen, dan leidt CPM het af uit de naam van de activiteitengroep en de activiteit (ruwbouw, onderbouw, bovenbouw, dak, stabiliteit, fundering, structuur, gevel, architect, ingenieur). Het oude contractformulier kent de keuze niet en laat ze ongemoeid.
- Een tienjarige-attest geldt voor de woningen die de gebruiker bij het vastleggen aanvinkt (eenheden met een woontype). Heeft het attest een bestand, dan koppelt CPM het aan elke aangevinkte woning en aan de koper ervan in Documenten en deelt het meteen in het klantenportaal. Hetzelfde geldt voor het document van een tienjarige polis voor één eenheid, en voor dat van een polis voor het hele project (alle verkochte woningen). Krijgt een eenheid later een klant, waar ook in CPM, dan koppelt CPM de polissen en attesten van die eenheid zelf aan die klant en deelt ze in het klantenportaal. De knop Aan kopers koppelen bij het aandachtspunt blijft als vangnet.
- Rode en gouden aandachtspunten over verzekeringen komen ook op het dashboard van de projectleider, voor projecten in uitvoering. Grijze mededelingen en opgeloste punten niet.
- "Opvragen" en "Herinneren" bij attesten openen enkel het e-mailprogramma van de gebruiker. CPM onthoudt de datum waarop hij op E-mail openen klikte, niet of de mail echt verstuurd is.
- Herinneringen bij een polis zijn geen mails. Ze bepalen enkel of er een aandachtspunt verschijnt 30 dagen vóór de start, bij de verkoop van de eenheid en 30 dagen vóór de vervaldag.
- Herinneringen over verzekeringen moeten later ook per mail komen (besluit Niels, 22/10/2026). Nog niet gebouwd: schrijf in artikels niets dat een mail belooft en pas de artikels aan zodra het er is.

### Traject en dossiers
- Een trigger is een automatische actie die aan een mijlpaal hangt en start als de mijlpaal bereikt wordt (of bij een ingestelde datum, bv. enkele dagen voor de doeldatum). Mogelijke acties: gebruiker of rol verwittigen, taak maken, herinneringsmail, dossier maken, aanvraagmail voor een dossier, volgende fase ontgrendelen, projectstatus of vlag zetten. Triggers moeten per mijlpaal ingesteld zijn.
- Een dossier verwijderen laat de mijlpalen staan; enkel de koppeling dossier-mijlpaal verdwijnt. Een traject verwijderen verwijdert zijn mijlpalen en de koppelingen met dossiers; de dossiers blijven.
- Instantie bij een dossier is de instantie die het dossier behandelt, bv. de gemeente.

### Documenten
- Zichtbaarheid: intern is alles wat niet gedeeld is. Gedeelde, goedgekeurde of getekende documenten zijn voor een koper zichtbaar als ze aan zijn eenheid of aan hemzelf gekoppeld zijn, of als ze voor alle kopers van het project gedeeld zijn. Er is nog geen klantenportaal, dus de klant ziet ze nog niet.
- Op de publieke website komen enkel goedgekeurde documenten uit de map Verkoop die aan geen klant of eenheid gekoppeld zijn: verkoopplannen, inplantingsplan en brochures.
- Een revisie goedkeuren of afwijzen stuurt geen melding naar leverancier of klant (niet gebouwd). Een afgewezen revisie kan later alsnog goedgekeurd worden (knop alsnog goedkeuren) en wordt dan de huidige. Meldingen komen er pas met de portalen, die nog uitgebouwd worden.
- Het leveranciersportaal bestaat, maar toont de gedeelde documenten nog niet. Een klantenportaal bestaat nog niet. De koppelingen en schakelaars staan er al, zodat het klantenportaal later meteen volledig met documenten werkt. Schrijf niet dat een klant of leverancier een document nu al ziet.
- Automatische mails voor opvolging van documentaanvragen (bv. voor de deadline) en zelf uploaden door een leverancier zijn de bedoeling voor later. Nog niet gebouwd.
- Een handtekening op een document registreren (knop Getekend, enkel zonder digitale handtekening) vraagt een bewijsbestand: de getekende versie, een scan of een mail. Zonder bestand wordt ze niet geregistreerd. Het bewijs is nadien te openen via Bewijs achter de naam. Registreren kan niet teruggedraaid worden. Wie schrijfrecht op het project heeft, kan het doen. Is het document volledig getekend, dan is het bevroren en vervang je het door een nieuw document.

### Leveranciers (bedrijfslogica)
- Een leverancier met contracten of facturen kan niet verwijderd worden; de melding zegt dat er al overeenkomsten of facturatie met de leverancier zijn. Een leverancier met enkel afdelingen en contactpersonen kan wel verwijderd worden, die gaan mee.
- De schakelaar Deze leverancier is ook klant bepaalt of gewijzigde gegevens naar Octopus gaan (enkel als de leverancier daar al staat). Lukt dat niet, dan is de leverancier wel opgeslagen en toont CPM een gele melding.
- Een afdeling verwijderen gebeurt bij het opslaan; haar contacten blijven bestaan zonder afdeling.
- Werfportaal: de uitgenodigde ziet de werven van zijn bedrijf, voortgang, de punten die aan zijn bedrijf toegewezen zijn (reageren, foto, opgelost melden) en werfverslagen. SiteManager is de werfverantwoordelijke van de leverancier op een contract; beperkte toegang betekent enkel die contracten. CPM zet geen einddatum op de uitnodiging; de link om opnieuw te versturen is 30 dagen geldig.
- Een offerte gunnen verwittigt de leverancier niet.

### Klanten in een project (bedrijfslogica)
- Een toegift is iets dat de koper bij of na de koop kreeg. Een aandachtspunt is een onderwerp waar de projectleider of leveranciers rekening mee moeten houden en dat geen toegift is. Beide worden aan activiteiten gekoppeld. Een overzicht per klant of per leverancier bestaat nog niet.
- De verkoopbedragen per eenheid bepalen rechtstreeks de facturen en betalingsschijven van de klant. Facturatie-instellingen van de klant (wijze, op naam van, verzenden naar) worden gebruikt bij het opmaken en versturen van facturen; het factureren zelf is manueel (de gebruiker kiest welke schijven).
- Er is geen klantenportaal; eigenaars tekenen via een aparte ondertekenpagina.

### Slechtweerdagen
- Regen- en winddagen tellen even zwaar; een dag met beide telt één keer. De opleverdatum wordt telkens berekend uit startdatum, uitvoeringsdagen, weekends, verlofdagen en alle slechtweerdagen van het weerstation.

### Eenheden en klanten (bedrijfslogica)
- Een eenheid die verkocht is of een klant heeft, kan niet verwijderd worden. De verkoopbedragen van een eenheid (grond en constructie) liggen vast zodra er een factuur voor de eenheid is.
- Een klant met facturatie (geboekt of niet), wijzigingsopdrachten of bereikte betalingsschijven kan niet verwijderd worden. De melding zegt dat er al overeenkomsten of facturatie met de klant zijn.
- Een klant wordt altijd via een project toegevoegd (Klant toevoegen in het project). Een klant aanmaken in de klantenlijst koppelt hem niet aan een eenheid; een scherm om dat achteraf te doen bestaat niet.
- Het boekhoudpakket is Octopus. Bewerk je een klant die al in Octopus staat, dan worden de gegevens daar ook bijgewerkt. Mislukt dat, dan is de klant wel bijgewerkt in CPM en toont CPM een gele melding.
- De sectie-instelling Publiek zichtbaar van foto's en video's telt mee, naast de zichtbaarheid van elk beeld. Oude foto's moeten niet opnieuw geüpload worden.

### Instellingen
- Een facturerend bedrijf wordt nooit verwijderd (er is geen verwijderknop), enkel op inactief gezet.
- Een bedrijf heeft een standaardrekening nodig om een factuur definitief te maken. Zonder standaard krijgt de gebruiker een melding en wordt er niets verstuurd; concepten kunnen nog wel. De eerste rekening van een bedrijf wordt automatisch de standaard. De standaard kan niet zomaar uitgezet worden, en bij verwijderen springt een andere geldige rekening bij.
- Nummerpatroon: kan niet meer gewijzigd worden zodra er in het lopende kalenderjaar facturen met een nummer zijn. Wijzigen kan dus bij de start van een nieuw jaar. Soort nummerreeks (factuur of creditnota): kan niet meer gewijzigd worden als er al facturen aan de reeks hangen; dan maak je een nieuwe reeks. Bestaande facturen behouden hun nummer.
- Een pdf van een factuur wordt met de huidige huiskleur van het bedrijf opgebouwd, ook een pdf die al eerder gemaakt was.
- Octopus: wordt een ander dossier gekoppeld, dan blijven facturen die al verstuurd werden bij het oude dossier, en nieuwe facturen gaan naar het nieuwe.
- Uurtarieven per bedrijf gelden voor interne medewerkers (actieve gebruikers zonder klantentoegang). Een medewerker kan bij meerdere bedrijven een uurprijs hebben.
- De kilometervergoeding wordt gebruikt bij coördinatieprojecten met regie-uren, voor de verplaatsing naar een werf bij een werfbezoek.
- Coördinatiecontracttypes: Schijven (all-in contract in delen, voor de meeste projecten), Regie (volledig uurtarief en verplaatsingskosten) en Combinatie (schijven plus regie voor bv. nazorg, later afgewerkte eenheden of rechtszaken).

## Werkwijze
- Begin elk nieuw artikel met een lijst van de 3 à 5 vragen die een gebruiker
  over dit scherm zou stellen. Schrijf daarna per vraag een taakartikel.
- Weet je een gevolg of zakelijke regel niet zeker? Verzin niets, maar zet
  [VRAAG: ...] in de tekst zodat Niels het kan aanvullen.
- Herlees het artikel als een nieuwe gebruiker zonder voorkennis
  en verbeter alles wat onduidelijk is voor je het afrondt.
- Een [VRAAG: ...] die Niels beantwoordt, verhuist naar de woordenlijst of de bedrijfslogica hierboven,
  zodat dezelfde vraag niet bij het volgende artikel terugkomt.
