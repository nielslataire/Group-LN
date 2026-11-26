---
title: Een projectsjabloon maken
topic: instellingen
role: Beheerder
device: Desktop
minutes: 5
updated: 2026-11-26
newsTag: Nieuw
screens: instellingen-projectsjablonen, instellingen-projectsjablonen-bewerken
related: projectsjabloon-toepassen, schijvensjablonen-beheren, planning-sjablonen, trajectsjablonen-overzicht
synonyms: projectsjabloon, sjabloon, bundel, planning traject schijven, standaard gebouw, verdiepingen, kelder, dak, zo wordt het toegepast, sjablonen
description: een planning, een traject en schijven bundelen tot één sjabloon voor een soort project
intro: |
  Een projectsjabloon bundelt een planningssjabloon, een trajectsjabloon en een schijvensjabloon voor een soort project, bijvoorbeeld een halfopen woning of een verkaveling.
  Je legt ook het standaardgebouw vast: hoeveel verdiepingen, welk dak en of er een kelder is. De projectleider past dat bij het toepassen aan.
before: |
  Je hebt het recht Planningssjablonen nodig. De drie sjablonen die je wil bundelen bestaan al en zijn actief.
faq:
  - q: Moet ik alle drie de onderdelen kiezen?
    a: Nee, minstens één. Een onderdeel dat je leeg laat, wordt bij het toepassen overgeslagen. Een verkaveling heeft bijvoorbeeld meestal geen schijven.
  - q: Wat betekent de gouden melding "Schijven en planning passen niet helemaal"?
    a: Een schijf verwijst met haar code naar een taak die niet in het gekozen planningssjabloon staat. Die schijf komt er wel, maar zonder koppeling aan de planning. Pas de code aan in het schijvensjabloon, of kies een andere planning.
  - q: Wat is "Beide" bij het dak?
    a: Dan staat er vooraf geen keuze. De projectleider kiest het dak bij het toepassen.
  - q: Waarom kan ik een projectsjabloon niet dupliceren?
    a: Dat kan nog niet. Maak een nieuw sjabloon en kies dezelfde onderdelen.
  - q: Waar zie ik op welke projecten het sjabloon is toegepast?
    a: Dat houdt CPM nog niet per sjabloon bij.
---
## De lijst

Onder **Instellingen › Sjablonen › Projectsjablonen** staan je projectsjablonen. Per sjabloon zie je de planning, het traject en de schijven die erbij horen, elk met een kerncijfer, zoals "9 mijlpalen". Klik op een onderdeel om dat sjabloon te openen. Verder zie je het standaardgebouw, wie het laatst wijzigde en de status.

![De lijst met projectsjablonen](/img/help/instellingen/projectsjablonen-beheren/1-lijst.png)

## Een projectsjabloon maken

1. Kies **Nieuw sjabloon**.
2. Geef het sjabloon een naam en eventueel een korte omschrijving. **Actief** staat bovenaan. Alleen actieve sjablonen kan je toepassen.
3. Kies bij **Onderdelen** een planning, een traject en schijven. Onder elke keuze staan de kerncijfers van dat sjabloon. Via **Openen** ga je naar het sjabloon zelf.
   Heb je parkings of bergingen met eigen schijven, kies dan ook **Schijven parkings en bergingen**. Laat je dat leeg, dan krijgen alle groepen dezelfde schijven.
   Bij een verkaveling met wegenis kies je ook **Planning van het project zelf**: die planning (de wegenis) komt in de hoofdplanning, en de planning van de woningen komt per woning. Kies bij **Woningen starten** wanneer de woningen mogen beginnen: onmiddellijk, na de wegenfundering, na de nutsvoorzieningen, na de wegverharding of na de voorlopige oplevering van de wegenis. Bij het toepassen krijgt de planning van de woningen die startdatum. Je past ze per project nog aan in de planning.
4. Kies het **standaard gebouw**: de verdiepingen boven het gelijkvloers met de knoppen − en +, het dak en de kelder.
5. Klik op **Sjabloon opslaan**.

![De editor van een projectsjabloon met onderdelen, standaard gebouw en voorbeeld](/img/help/instellingen/projectsjablonen-beheren/2-editor.png)

## Zo wordt het toegepast

Rechts zie je wat het sjabloon doet voor het standaardgebouw: hoeveel fases en taken de planning krijgt en hoeveel werkdagen die duurt, hoeveel mijlpalen het traject heeft en welke schijven er met welk percentage komen. Het voorbeeld past zich aan als je het gebouw of een onderdeel wijzigt.

Op een project vult het sjabloon enkel aan wat nog leeg is. Een betalingsgroep waarin enkel parkings en bergingen zitten, krijgt het schijvensjabloon voor parkings en bergingen. Een groep met ook maar één appartement of andere eenheid krijgt de gewone schijven. Een parking of berging die aan een appartement hangt, zet je daarom in de groep van dat appartement: ze volgt dan diens schijven. Een bestaande planning of bestaande schijven blijven staan. De projectleider ziet altijd eerst het voorstel. Hoe dat gaat, lees je bij "Een projectsjabloon toepassen op je project".

Bij een verkaveling is het trajectsjabloon opgebouwd uit twee delen: de mijlpalen van het project zelf en een blok mijlpalen per woning (één rij per lot). Na het toepassen herrekent het traject meteen: de bouwmijlpalen volgen de planning van hun woning en de data van compromis, akte en oplevering komen uit het klantaccount. Kies je een dak (plat of hellend), dan verdwijnt bij elke woning de mijlpaal "Dak dicht" van het andere dak. Kies je geen dak, dan blijven beide staan en verwijder je de overbodige zelf.

## Let op

- Wijzig je een planning-, traject- of schijvensjabloon, dan geldt dat voor projectsjablonen die het gebruiken vanaf de volgende keer dat je ze toepast.
- Een schijvensjabloon dat in een projectsjabloon zit, kan je niet verwijderen. Zet het op inactief of kies eerst een ander sjabloon.
