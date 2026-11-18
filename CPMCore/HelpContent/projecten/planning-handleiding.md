---
title: Een volledige planning opstellen
topic: projecten
treeOrder: 1
role: Projectleider
device: Desktop · tablet
minutes: 10
updated: 2026-11-15
newsTag: Nieuw
screens: planning-index
related: planning-voorbereiden, planning-opbouwen, planning-per-bouwdeel, planning-importeren-ms-project, voorgangers-en-wachttijd-invullen, taak-datums-en-verantwoordelijke, planning-lezen-en-opvolgen, basisplanning-vastleggen, planning-bijhouden-tijdens-de-bouw, planning-voortgang-budget, planning-navigeren, planning-problemen-oplossen
synonyms: planning, bouwplanning, planning opstellen, planning maken, gantt, werkwijze, handleiding, fases, taken, mijlpaal, voorgangers, kritiek pad, deadline, oplevering
description: de hele planning van een bouwproject opbouwen, controleren en opvolgen, stap voor stap
intro: |
  Dit artikel loopt door een volledige planning zoals een projectleider ze opstelt: van het klaarzetten van het project tot het opvolgen tijdens de bouw.
  Bij elke stap staat waar je op klikt en wat CPM daarna doet. Voor de details verwijst elke stap naar een eigen artikel.
  De planning rekent in werkdagen. Weekends, feestdagen, bouwverlof en weerverletdagen tellen niet mee. Geef elke taak een duur en een voorganger, en CPM rekent alle data zelf uit.
before: |
  Je hebt schrijfrecht op de planning en het project heeft een aanvangsdatum. Zie "Je project klaarzetten voor de planning" als je dat nog niet zeker weet.
faq:
  - q: Waar begin ik als ik een planning van papier of Excel overneem?
    a: Maak eerst de fases aan in de volgorde van de bouw. Typ daarna per fase de taken met hun duur. Vul als laatste de voorgangers in. Zo zie je de planning groeien en kun je elke stap controleren.
  - q: Hoe gedetailleerd moet de planning zijn?
    a: Hou taken groot genoeg om op te volgen, meestal van 2 tot 15 werkdagen. Splits enkel een taak op als twee ploegen of twee leveranciers erin werken, of als er iets op moet wachten.
  - q: Kan ik de planning importeren of exporteren?
    a: Nog niet. Importeren uit MS Project of Excel, exporteren naar Excel of PDF, planningssjablonen en het slepen van balken volgen later. Je bouwt de planning nu in de lijst op.
  - q: Kan ik de planning laten vertrekken van het budget?
    a: Nog niet. Je maakt fases en taken zelf aan. Wel kun je een fase koppelen aan een lot uit het budget, zodat de voortgang van de taken meetelt voor de fysieke voortgang van het project.
  - q: Hoe krijgt het Traject de data uit de planning?
    a: Koppel een mijlpaal uit het Traject aan een taak of fase. Daarna volgt haar streefdatum de planning en staat ze op bereikt zodra de taak klaar is.
  - q: Ziet de klant de planning?
    a: Nee. De planning is een intern werkinstrument. De klant ziet enkel wat je zelf met hem deelt.
---
## Het project klaarzetten

Controleer eerst de **Aanvangsdatum**, de **Uitvoeringstermijn** en het **Weerstation** van het project. Zonder aanvangsdatum rekent de planning vanaf vandaag en toont CPM een melding. Zonder uitvoeringstermijn is er geen deadline om tegen te meten.
Controleer ook of de verlofdagen van het jaar zijn ingesteld. Lees "Je project klaarzetten voor de planning".

## De pagina Planning openen

1. Open het project.
2. Kies in het projectmenu onder **Opvolging** de pagina **Planning**.

Is het menu-item er niet, dan heb je het recht **Planning** niet. Vraag het aan wie de gebruikers beheert. Bij een nieuw project zie je een lege pagina met de knoppen **Eerste taak toevoegen** en **Eerst een fase maken**.

## De fases maken

Een fase is een groep taken, zoals Ruwbouw of Binnenafwerking. Maak ze in de volgorde van de bouw.

1. Klik rechtsboven op **Fase**.
2. Typ de naam en kies **Als hoofdfase, achteraan**.
3. Typ eventueel de eerste taak en klik op **Fase toevoegen**.

Een voorbeeld voor een woningbouwproject:

| Fase | Taken en duur in werkdagen |
|---|---|
| 1 Voorbereiding en grondwerken | Startvergadering werf (1), Werfinrichting en afsluiting (3), Grondwerken en uitgraving (6) |
| 2 Ruwbouw | Funderingen en vloerplaat (10), Rioleringswerken (4), Metselwerk gelijkvloers (12), Verdiepingsvloer (4), Metselwerk verdieping (12), mijlpaal Ruwbouw afgewerkt (0) |
| 3 Dak en gevel | Dakstructuur (6), Dakbedekking (5), Buitenschrijnwerk (6), Gevelwerken (15), mijlpaal Gebouw winddicht (0) |
| 4 Technieken ruwbouw | Ruwbouw elektriciteit (8), Sanitair en verwarming ruwbouw (10), Ventilatie (5) |
| 5 Binnenafwerking | Pleisterwerk (10), Dekvloeren (5), Droogtijd dekvloer (15), Tegelwerken (8), Binnenschrijnwerk (6), Schilderwerken (10) |
| 6 Afwerking technieken en oplevering | Afwerking elektriciteit (5), Sanitair plaatsen (4), Poetswerken (3), Voorlopige oplevering (1) |

Lees "Een planning opbouwen" voor deelfases, verplaatsen en verwijderen.

## De taken invullen

1. Klik in een fase op **+ Taak**, of rechtsboven op **Taak toevoegen**.
2. Typ de **Naam** en de **Duur** in werkdagen.
3. Klik op **Toevoegen en nog één** om meteen de volgende taak in te vullen.

Een duur van 0 maakt van de taak een mijlpaal, een ruitje op één dag. Gebruik mijlpalen voor momenten zoals "Gebouw winddicht" of "Oplevering".
Een droogtijd of wachttijd zonder werk maak je als gewone taak met een duur, zoals **Droogtijd dekvloer** van 15 werkdagen. Dan zie je ze in de grafiek. Voor een korte wachttijd kun je ook een wachttijd bij de voorganger invullen.

## De taken aan elkaar koppelen

Bij **Voorgangers** typ je het nummer van de taak die eerst klaar moet zijn, bijvoorbeeld **5.1**. CPM vult de taak erboven alvast in.

- Tegelwerken start pas na de droogtijd: **5.3**.
- Rioleringswerken starten drie werkdagen na de start van de funderingen: **2.1SS+3d**.
- Pleisterwerk wacht op twee taken: **4.1; 4.2**.

Lees "Voorgangers en wachttijd invullen" voor alle soorten. Vanaf nu schuift alles vanzelf mee als een taak uitloopt. Een taak zonder voorganger start op de eerste projectdag en krijgt een aandachtspunt.

## Datums vastzetten waar nodig

Soms ligt een datum vast, bijvoorbeeld een levering van ramen of de dag van de plaatsing van een kraan.

1. Klik op de taak om ze te openen.
2. Kies bij **Beperking** bijvoorbeeld **Niet eerder beginnen dan** en een datum.
3. Vul eventueel een **Deadline** in. CPM waarschuwt dan als de taak daarna eindigt.

Lees "Een datum, deadline of verantwoordelijke bij een taak instellen".

## De planning controleren

![De planning met de werkbalk, de lijst en de grafiek, met het kritieke pad aan](/img/help/projecten/planning-handleiding/1-planning-overzicht.png)

Kijk bovenaan naar de kop.

- **Start** en **Einde** zijn de eerste en laatste dag van de planning.
- **Deadline oplevering** toont hoeveel werkdagen marge je hebt, of hoeveel te laat je bent.
- Een rode melding "Einde valt na de voorziene oplevering" betekent dat de planning te lang is.

Zet **Kritiek pad** aan. De rode, gearceerde taken hebben geen speling: elke dag uitloop daar schuift het einde op. Wil je sneller klaar zijn, dan kort je die taken in of laat je ze overlappen met een negatieve wachttijd.
Open de aandachtspunten en los ze op. Lees "Aandachtspunten en fouten in de planning oplossen".

## De basisplanning vastleggen

Ben je tevreden met de planning, klik dan op **Basis vastleggen**. CPM bewaart de huidige data als referentie. Later zie je bij elke taak hoeveel ze afwijkt. Lees "Een basisplanning vastleggen en afwijkingen volgen".

## De planning aan het Traject koppelen

Houdt het project een Traject bij met mijlpalen zoals "Ruwbouw klaar" of "Voorlopige oplevering", koppel die dan aan de taak of fase die ze bepaalt. Open de taak en kies bij **Koppeling met trajectmijlpaal** de mijlpaal. De streefdatum in het Traject volgt dan de planning, en de mijlpaal staat op bereikt zodra de taak klaar is. Lees "Een datum, deadline of verantwoordelijke bij een taak instellen".

## De planning opvolgen tijdens de bouw

Vink een taak af met het rondje voor de naam zodra ze klaar is. Is ze maar voor een deel klaar, open dan de taak en vul de **Voortgang** in. CPM herrekent alle taken die erop wachten.
Was een taak eerder klaar dan vandaag, vul dan haar **Werkelijk einde** in. Lees "De planning bijhouden tijdens de bouw".

## De voortgang laten meetellen

Open een fase en kies een **Budgetlot**. Zet daarna **Telt mee voor de fysieke voortgang** aan. De voortgang van de taken in de fase telt dan mee voor de fysieke voortgang van het project. Lees "De planning laten meetellen voor de fysieke voortgang".

## Let op

Je wijzigingen worden meteen bewaard. Met **Ongedaan maken** in de balk bovenaan keer je de laatste wijziging terug, tot je de pagina herlaadt. Werk je met twee personen tegelijk in dezelfde planning, dan herlaadt CPM de pagina als iemand anders net iets wijzigde. Je verliest dan niets, maar je laatste wijziging is niet bewaard.
