---
title: Een planning importeren uit MS Project
topic: projecten
parent: planning-handleiding
treeOrder: 12
role: Projectleider
device: Desktop
minutes: 6
updated: 2026-11-19
newsTag: Nieuw
screens: planning-index
related: planning-per-aannemer, planning-per-bouwdeel, planning-sjablonen, planning-opbouwen, voorgangers-en-wachttijd-invullen
synonyms: importeren, import, ms project, project xml, xml, mpp, planning overnemen, bestaande planning, aannemer planning, gantt importeren
description: een planning uit MS Project overnemen met fases, taken, duur en voorgangers, zonder alles opnieuw in te typen
intro: |
  Heb je de planning al in MS Project gemaakt, dan hoef je ze niet opnieuw in te typen. Sla ze in MS Project op als XML en lees ze in CPM in.
  CPM zet de fases, taken, mijlpalen, duur en voorgangers over en rekent alles opnieuw door met de werkkalender van dit project.
before: |
  Je hebt schrijfrecht op de planning. Je hebt de planning in MS Project als XML opgeslagen: **Bestand › Opslaan als** en dan **XML-indeling (*.xml)**. Een .mpp-bestand kan CPM niet rechtstreeks lezen.
faq:
  - q: Kan ik een .mpp-bestand importeren?
    a: Niet rechtstreeks. Open het in MS Project, kies **Bestand › Opslaan als** en dan **XML-indeling (*.xml)**, en kies dat bestand in CPM. Taken, duur, voorgangers en fases gaan mee.
  - q: Kan ik ook uit Excel importeren?
    a: Nog niet. Voorlopig kan alleen MS Project-XML.
  - q: Wat wordt niet overgenomen?
    a: Kosten en werk (uren) worden genegeerd. Resources worden niet als resource overgenomen, maar je kunt ze koppelen aan een uitvoerder. De kalender uit het bestand (verlof, feestdagen) neemt CPM niet over: CPM rekent met de kalender van het project (Projecten › Weather). Geef daar je verlofdagen in, anders ligt het einde te vroeg. Een duur in uren wordt afgerond naar hele werkdagen. De beperking "zo laat mogelijk" wordt "zo vroeg mogelijk". Een voorganger die niet bestaat of die een kring zou maken, wordt weggelaten. Dat staat per regel bij **Melding** in het voorbeeld.
  - q: Waarom lopen de datums anders dan in MS Project?
    a: CPM rekent alles opnieuw door met de werkkalender van dit project: feestdagen, bouwverlof en weerverlet. De duur en de voorgangers komen mee, de datums volgen daaruit.
  - q: Wat gebeurt er met een voorganger van een fase?
    a: Een fase die op een andere fase wacht, laat haar eerste taak wachten. Een taak die op een hele fase wacht, wacht op alle taken in die fase.
  - q: Kan ik de import ongedaan maken?
    a: Niet met de knop Ongedaan maken. Is het resultaat niet goed, verwijder dan de planning (Instellen › Verwijderen) en importeer opnieuw. Bij **Vervangen** vraagt CPM eerst om bevestiging.
  - q: Waarom lukt het inlezen niet?
    a: Het bestand is geen MS Project-XML, het is groter dan 10 MB of het heeft meer dan 5000 regels. Splits een heel groot bestand eerst op in MS Project.
---
## Een planning importeren

1. Klik naast de tabs op **Importeren**.
2. Sleep het XML-bestand in het venster, of klik op **Bestand kiezen**.
3. CPM leest het bestand en toont het voorbeeld.
4. Controleer het voorbeeld. Bovenaan staat hoeveel taken, fases, mijlpalen en afhankelijkheden CPM vond. Bij **Aandachtspunten** zie je wat CPM moest aanpassen. Met de knop **Aandachtspunten** toon je alleen die regels.
5. Heeft het bestand resources (meestal aannemers), klik dan op **Volgende** en koppel ze aan een bedrijf uit je project (zie hieronder). Zonder resources sla je deze stap over.
6. Klik op **Volgende** en kies waar de planning terechtkomt.
7. Kies de startdatum.
8. Klik op **Importeren**.

![Het voorbeeld van de import met aandachtspunten](/img/help/projecten/planning-importeren-ms-project/2-controle.png)

## Uitvoerders koppelen

Heeft je MS Project-bestand resources, dan toont CPM ze in een lijst met het aantal taken per resource.

- Kies bij elke resource de **aannemer** die erbij hoort. Je kiest enkel uit aannemers met een contract in dit project; tik in het veld om te zoeken op naam.
- CPM stelt zelf een koppeling voor op basis van de naam, bijvoorbeeld "Dakw. Demeyer" aan Demeyer Dakwerken. Controleer die voorstellen.
- Een resource die je niet koppelt, komt als tekst in de notitie van de taak ("Uitvoerder (uit MS Project): …"). Je koppelt ze later zelf, zodra de aannemer is aangesteld.
- Het bedrijf staat daarna als **Uitvoerder** bij de taak. Taken zonder eigen uitvoerder nemen die van hun fase over.
- Ook het veld **Uitvoerder (aannemer)** bij een fase of taak is een keuzelijst met zoekveld en bevat enkel aannemers met een contract in dit project. Staat een aannemer er niet bij, maak dan eerst een contract voor hem aan in het project.

## Waar komt de planning terecht?

- **Nieuwe planning.** Geef de planning een naam en kies eventueel de woningen of gebouwen waar ze voor geldt. Dit is de standaard. Heb je maar één woning gekozen, dan neemt CPM haar naam over.
- **Deze planning.** De import komt achteraan in de planning die je nu bekijkt, of vervangt haar fases en taken. Bij **Vervangen** vraagt CPM eerst om bevestiging, want je kunt dit niet ongedaan maken.

![De opties van de import](/img/help/projecten/planning-importeren-ms-project/3-opties.png)

## Welke startdatum?

- **Zoals in het bestand.** De planning start op de startdatum van het project in MS Project.
- **Nieuwe startdatum.** Alle taken schuiven mee. Vastgepinde taken blijven staan. CPM past de werkkalender van het project toe.
- **De planning niet aanraken.** Alleen bij een bestaande planning: haar startdatum blijft zoals ze is.

## Wat CPM overneemt

Fases (samenvattingstaken), taken, mijlpalen, duur, voorgangers met hun soort (FS, SS, FF, SF) en wachttijd, beperkingen zoals "niet eerder beginnen dan", deadlines, de basisplanning, werkelijke datums, voortgang en notities.

Na het importeren open je de planning en kijk je de duur en de voorgangers na. De lijst met aandachtspunten van de planning toont wat nog niet klopt.
