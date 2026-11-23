---
title: Een projectsjabloon toepassen op je project
topic: projecten
parent: planning-handleiding
treeOrder: 12
role: Projectleider
device: Desktop · tablet
minutes: 5
updated: 2026-11-20
newsTag: Nieuw
screens: planning-index, instellingen-projectsjablonen, instellingen-projectsjablonen-bewerken
related: planning-sjablonen, schijvensjablonen-beheren, planning-schijven-koppelen
synonyms: projectsjabloon, sjabloon, appartementsgebouw, verdiepingen, kelder, hellend dak, plat dak, ai, beschrijving, planning traject schijven, in één keer
description: planning, traject en betalingsschijven in één keer opzetten, aangepast aan het gebouw
intro: |
  Een projectsjabloon bundelt de planning, het traject en de betalingsschijven voor een soort project. Je beantwoordt een paar vragen over het gebouw, en het sjabloon past zich aan.
  Een appartementsgebouw met een plat dak en een kelder krijgt dus andere taken en schijven dan een rijwoning met een hellend dak.
before: |
  Je hebt schrijfrecht op de planning en op de facturatie van projecten. Het project heeft eenheden, en minstens een betalingsgroep. Een beheerder heeft het projectsjabloon al gemaakt onder Instellingen › Sjablonen › Projectsjablonen.
faq:
  - q: Wat als er al een planning is?
    a: Dan blijft die staan. Een eenheid die al een planning heeft, krijgt er geen nieuwe. Enkel eenheden zonder planning krijgen er een.
  - q: En als het project al een traject heeft?
    a: Dan blijft dat traject ongewijzigd. Het projectsjabloon maakt enkel een traject als er nog geen is.
  - q: Wat gebeurt er met betalingsgroepen die al schijven hebben?
    a: Die blijven ongemoeid. Enkel groepen zonder schijven, of met enkel de standaardschijf van 100 %, krijgen de schijven uit het sjabloon. Is er al gefactureerd of bereikt, dan verandert er niets.
  - q: Wat doet de AI precies?
    a: Je beschrijft het gebouw in gewone woorden. De AI kiest uit je projectsjablonen het sjabloon dat het best past en vult de vragen in: aantal bouwlagen, terrassen of luifels, kelder, dak en of het één gebouw of losse woningen zijn. Daaronder zie je wat de AI heeft aangenomen omdat het er niet stond, eventuele vragen, en hoeveel werkdagen het plan volgens de planning duurt tegenover de uitvoeringstermijn van het project. Die duur rekent CPM zelf uit, niet de AI. Het is een voorstel: jij kijkt het na en past aan voor er iets gebeurt.
  - q: Wat ziet de AI van mijn project?
    a: Enkel de beschrijving die je zelf intikt, de structuur van je projectsjablonen, het aantal eenheden en de uitvoeringstermijn. Geen namen van klanten, aannemers of eenheden, geen adressen en geen bedragen. Vermeld die ook niet in je beschrijving: staat er een e-mailadres, telefoonnummer, adres, bedrag of ondernemingsnummer in, dan stuurt CPM de beschrijving niet door en vraagt het je dat weg te halen. Namen van mensen kan CPM niet herkennen, dus laat die zelf weg.
  - q: De AI is niet beschikbaar. Wat nu?
    a: Vul de vragen zelf in. Alles werkt ook zonder AI.
  - q: Kan ik het later opnieuw toepassen?
    a: Ja. Het projectsjabloon vult dan opnieuw enkel aan wat nog leeg is, bijvoorbeeld voor eenheden die je later toevoegt.
---
## Een projectsjabloon toepassen

1. Open **Projecten › Planning** en kies **Projectsjabloon** naast de tabs.
2. Kies het projectsjabloon dat bij je project past.
3. Beantwoord de vragen, of beschrijf het gebouw en klik op **Laat de AI de vragen invullen**:
   - het aantal **bouwlagen** (gelijkvloers + verdiepingen: gelijkvloers + 2 verdiepingen = 3)
   - bij welke afdek **terrassen of luifels** komen (knoppen GV, 1, 2 …; leeg = geen)
   - of er een **kelder** is
   - of het **dak** hellend of plat is
   - of het **één planning voor alle eenheden** is (een gebouw met appartementen) of een planning per woning
   - bij één planning: of ze **in de hoofdplanning** komt. Dat kan enkel als de hoofdplanning nog leeg is. Is ze niet leeg, of zet je dit uit, dan komt er een aparte planning naast de hoofdplanning
4. Lees onder **Wat er gebeurt** wat CPM gaat doen. Daar staan ook de schijven met hun percentages.
5. Klik op **Toepassen**.

![Het venster Projectsjabloon toepassen met de vragen en het voorbeeld](/img/help/projecten/projectsjabloon-toepassen/1-toepassen.png)

## Wat het sjabloon aanpast

- **Planning:** blokken voor kelder of voor een hellend of plat dak komen enkel in de planning als ze bij het gebouw passen. De ruwbouw wordt herhaald per bouwlaag (gelijkvloers, 1ste verdieping, 2de verdieping …); blokken voor terrassen of luifels komen enkel bij de gekozen afdekken.
- **Schijven:** een schijf als de ruwbouw wordt onderverdeeld per bouwlaag ("... gelijkvloers", "... 1ste verdieping"). Schijven die enkel bij een kelder of een bepaald dak horen, komen enkel voor als het gebouw dat heeft.
- **Traject:** het traject wordt aangemaakt. Mijlpalen met dezelfde code als een taak in de planning volgen die taak.

## Let op

- Controleer wat de AI voorstelt. Zij kan een kelder of dak niet afleiden als je het niet vermeldt, en laat het dan leeg.
- Er wordt enkel aangevuld. Wil je een bestaande planning of bestaande schijven vervangen, doe dat dan met **Sjabloon** op de planningpagina of met **Sjabloon toepassen** in de betalingsgroep.
- Zijn de percentages van het schijvensjabloon samen geen 100 %, dan worden er geen schijven toegepast en staat dat in de melding.
