---
title: Een planning per woning of gebouw maken
topic: projecten
parent: planning-handleiding
treeOrder: 9
role: Projectleider
device: Desktop · tablet
minutes: 5
updated: 2026-11-18
newsTag: Nieuw
screens: planning-index
related: planning-handleiding, planning-opbouwen, voorgangers-en-wachttijd-invullen, planning-navigeren
synonyms: meerdere plannings, hoofdplanning, bouwdeel, woning, gebouw, verkaveling, planning per woning, tabs, startdatum planning, H:
description: een hoofdplanning voor het hele project en een eigen planning voor elke woning of elk gebouw
intro: |
  Bij een verkaveling of een project met meerdere gebouwen werk je met meer dan één planning. De hoofdplanning geldt voor het hele project, bijvoorbeeld de wegenis en de nutsvoorzieningen. Daarnaast krijgt elke woning, elk gebouw of elke groep woningen een eigen planning. De planning staat los van de betalingsgroep: woningen uit dezelfde betalingsgroep kunnen elk een eigen planning hebben, zodat een betalingsschijf per woning op een ander moment bereikt wordt.
  CPM rekent alle plannings samen door. Een taak in een woning kan dus wachten op een taak in de hoofdplanning, en een wijziging schuift alles mee.
before: |
  Je hebt schrijfrecht op de planning. Het project heeft woningen of gebouwen, zodat je ze kunt kiezen.
faq:
  - q: Waar zie ik mijn plannings?
    a: Bovenaan de pagina staat voor elke planning een tab. De eerste is de hoofdplanning. Klik op een tab om die planning te tonen. Is de hoofdplanning leeg en heeft het project wel een planning voor een gebouw of woning, dan staat er geen tab voor de hoofdplanning. Voeg een planning toe zodra je werken hebt die voor het hele project gelden.
  - q: Kan ik de hoofdplanning verwijderen?
    a: Nee. De hoofdplanning hoort bij het hele project. Je kunt wel haar fases en taken verwijderen.
  - q: Kan een woning in twee plannings zitten?
    a: Nee. Een woning of gebouw zit in hoogstens één planning. In het planningspaneel staan eenheden die al in een andere planning zitten grijs, met de naam van die planning.
  - q: Kan één planning meerdere woningen bundelen?
    a: Ja. Vink bij **Woningen en gebouwen** alle eenheden aan die samen één planning vormen, bijvoorbeeld een hele rij woningen of een volledig appartementsgebouw.
  - q: Wat gebeurt er met de taken als ik een planning verwijder?
    a: Alle fases en taken van die planning worden verwijderd. Taken in andere plannings die erop wachtten, verliezen die voorganger. Dit kun je niet ongedaan maken.
  - q: Mag de naam van een planning een dubbelpunt bevatten?
    a: Nee. Je gebruikt de naam om in het veld Voorgangers naar de planning te verwijzen, dus een dubbelpunt, puntkomma of komma kan niet. De naam moet ook uniek zijn en mag niet alleen H zijn.
  - q: Waarom verandert het nummer van een taak per planning?
    a: Elke planning telt zelf vanaf 1. Taak 1.1 in een woning is dus niet dezelfde als taak 1.1 in de hoofdplanning.
---
## Een planning toevoegen

1. Klik bovenaan op **Planning**.
2. Geef de planning een naam, bijvoorbeeld de naam van de woning.
3. Vink bij **Woningen en gebouwen** de eenheden aan waar deze planning voor geldt. Dit is optioneel. Eenheden die al in een andere planning zitten, kun je niet kiezen.
4. Vul eventueel een **Startdatum van de planning** in.
5. Klik op **Planning toevoegen**.

CPM toont de nieuwe, lege planning. Maak er fases en taken in zoals in elke planning. Zie "Een planning opbouwen".

## De startdatum van een planning

De startdatum is de eerste dag waarop de planning mag beginnen. Laat je ze leeg, dan geldt de aanvangsdatum van het project. Een voorganger kan een taak nog altijd later laten starten. Een startdatum vóór de aanvangsdatum van het project doet niets.

## Wachten op een taak uit de hoofdplanning

Typ in het veld **Voorgangers** van een taak in een woning de letter H en een dubbelpunt voor het nummer. **H:3.1** betekent taak 3.1 uit de hoofdplanning. Je kunt het soort en de wachttijd erbij zetten, zoals bij andere voorgangers: **H:3.1FS+2d**.
In de hoofdplanning zelf werkt H: niet. Daar verwijs je gewoon naar het nummer.

## Wachten op dezelfde taak in de vorige woning (estafette)

Bij een verkaveling wil je soms dat een taak in woning 2 pas begint als dezelfde taak in woning 1 klaar is. Bijvoorbeeld de elektriciteit: het team kan pas naar woning 2 als het in woning 1 klaar is.

1. Open de taak in woning 2 (klik op de rij).
2. Kies bij **Estafette** in de lijst **Planning toevoegen…** de woning of het gebouw waarop de taak moet wachten. De lijst toont de plannings met een taak met dezelfde naam of hetzelfde nummer. De keuze verschijnt als een label, dat je met het kruisje weer wegneemt. Je bepaalt zelf waarop de taak wacht, ook op meer dan één woning.
3. Klik op **Opslaan**.

In het veld **Voorgangers** staat nu bijvoorbeeld **Woning 1:3.2**. Je kunt dat ook zelf typen: de naam van de planning, een dubbelpunt en het taaknummer. Soort en wachttijd werken zoals bij andere voorgangers: **Woning 1:3.2FS+2d**.
Je kiest dit per taak. Een taak zonder deze keuze wacht niet op de vorige woning.

## Een voorganger zoeken in de lijst

Weet je het nummer niet, dan hoef je het niet op te zoeken in een andere planning.

1. Open de taak en klik onder **Voorgangers** op **Kies uit de lijst**.
2. Typ een deel van de naam of het nummer in het zoekveld. De lijst toont taken en fases van alle plannings, per planning gegroepeerd.
3. Klik op een regel om ze als voorganger toe te voegen. Een vinkje betekent dat ze al gekozen is. Klik nog eens om ze weg te halen.
4. Klik op **Opslaan**.

CPM zet de juiste tekst voor je in het veld, bijvoorbeeld **H:3.1** of **Woning 1:3.2**. Een fase kiezen betekent wachten op al haar taken.

## Alle plannings samen bekijken

Heb je meer dan één planning, dan staat achter de tabs een tab **Globaal**. Die toont alle plannings in één overzicht met één tijdas.

1. Klik op de tab **Globaal**.
2. Elke planning is een groep met haar eigen begin en einde. De hoofdplanning staat bovenaan.
![Het globale overzicht met een groep per planning](/img/help/projecten/planning-per-bouwdeel/globaal-overzicht.png)
3. Klap een groep open om de fases en taken te zien. Pijlen tonen waar een taak op een taak in een andere planning wacht.
4. Bovenaan zie je het begin en einde van het hele project.

Het globale overzicht is alleen om te lezen. Wil je iets wijzigen, ga dan naar de tab van de planning zelf. Wil je zien wanneer men aan welke woning bezig is, geef dan elke woning een eigen planning: een planning met meerdere woningen is één schema voor al die woningen samen.

## Een planning instellen of verwijderen

Klik naast de tabs op **Instellen** om de getoonde planning te wijzigen: naam, woningen en gebouwen en startdatum.
**Verwijderen** staat onderaan in hetzelfde paneel. CPM vraagt eerst om bevestiging.

## Let op

Het overzicht, de kop en de aandachtspunten gelden enkel voor de getoonde planning. De deadline van het project blijft dezelfde voor elke planning.
