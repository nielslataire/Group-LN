# Schrijfgids voor helpartikels

Hoort bij het Helpcentrum (`CPMCore/HelpContent/`). De technische kant (bestand, frontmatter, boom,
screenshots) staat in `DESIGN.md`, sectie "Een nieuw help-artikel schrijven — stappenplan". Dit
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

### Punten en verslagen
- Een concept-punt moet door de projectleider goedgekeurd worden. Wie het punt maakte, krijgt geen melding bij goedkeuring.
- Het dashboard van de projectleider toont nog niet dat er punten goed te keuren zijn (die melding is nog niet gebouwd). Schrijf hier geen belofte over in artikels. Zodra ze er is, het artikel "Een punt inspreken" aanpassen.
- [VRAAG: wanneer wordt een punt "afgesloten"?]

### Traject en dossiers
- [VRAAG: aanvullen]

### Documenten
- [VRAAG: welke documenten zijn zichtbaar voor de klant en welke enkel intern?]

### Instellingen
- [VRAAG: aanvullen]

## Werkwijze
- Begin elk nieuw artikel met een lijst van de 3 à 5 vragen die een gebruiker
  over dit scherm zou stellen. Schrijf daarna per vraag een taakartikel.
- Weet je een gevolg of zakelijke regel niet zeker? Verzin niets, maar zet
  [VRAAG: ...] in de tekst zodat Niels het kan aanvullen.
- Herlees het artikel als een nieuwe gebruiker zonder voorkennis
  en verbeter alles wat onduidelijk is voor je het afrondt.
- Een [VRAAG: ...] die Niels beantwoordt, verhuist naar de woordenlijst of de bedrijfslogica hierboven,
  zodat dezelfde vraag niet bij het volgende artikel terugkomt.
