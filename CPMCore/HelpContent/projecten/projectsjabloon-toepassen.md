---
title: Een projectsjabloon toepassen op je project
topic: projecten
parent: planning-handleiding
treeOrder: 12
role: Projectleider
device: Desktop · tablet
minutes: 5
updated: 2026-11-27
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

1. Open **Projecten › Planning** en kies **Sjablonen › Projectsjabloon**. Het venster heeft twee kolommen: links de vragen in vier stappen, rechts **Wat er gebeurt**. Dat rechts verandert meteen mee met elk antwoord.
2. Wil je hulp, beschrijf dan het gebouw bij stap 1 en klik op **Laat de AI een voorstel doen**. Wat de AI invulde, staat blauw met het label **AI**, zodat je ziet wat je moet nakijken. Met **Ongedaan maken** zet je alles terug. Wijzig je zelf een antwoord, dan verdwijnt het label bij dat antwoord.
3. Kies bij stap 2 het projectsjabloon dat bij je project past. Elke kaart toont wat erin zit: het aantal fases van de planning en het traject, en het aantal schijven.
4. Beantwoord bij stap 3 en 4 de vragen over het gebouw:
   - het aantal **bouwlagen** met de knoppen − en + (gelijkvloers + verdiepingen: gelijkvloers + 2 verdiepingen = 3)
   - bij welke afdek **terrassen of luifels** komen (knoppen GV, 1, 2 …; leeg = geen)
   - welke **gevelafwerkingen** er naast de stenen gevel komen (blok **Gevelafwerking**, enkel zichtbaar als het planningssjabloon ze kent): **Gevelpleister**, **Houten bekleding** en/of **Aluminium bekleding**. Je kan er meerdere kiezen of geen. De stenen gevel is er altijd en komt eerst, daarna de gevelpleister, en pas daarna de houten of aluminium bekleding
   - of er een **kelder** is (**Met kelder** of **Zonder**)
   - of het **dak** plat of hellend is, of **Geen keuze**
   - of het **één planning voor het gebouw** is (een gebouw met appartementen) of **per eenheid**, met erbij hoeveel eenheden nog geen planning hebben. Bij een appartementsgebouw is één planning de standaard: de werf loopt per bouwlaag, niet per appartement
   - bij **per eenheid**: het veld **Woningen per planning**. Laat je het leeg, dan krijgt elke woning een eigen planning. Vul je bv. 7 in bij 14 woningen, dan komt er één planning voor lot 1 tot 7 en één voor lot 8 tot 14. Die planning (bv. "Lot 01 t/m Lot 07") bevat de taken van één woning en geldt voor alle woningen van de groep. Pas de duur van de taken daarna aan als een groep meer tijd nodig heeft
   - wil je zelf bepalen welke woningen samen in een planning komen? Kies **Woningen zelf indelen**. Je ziet dan elke woning met een nummer: woningen met hetzelfde nummer delen één planning. Het nummer bepaalt ook de volgorde van de estafette. De knop "Terug naar vast aantal per planning" zet de keuze weer uit
   - heeft het project al een planning en wil je opnieuw beginnen? Dan staat er, enkel voor wie het recht Verwijderen op Planning heeft, een schakelaar **Bestaande planning eerst verwijderen**. Het voorbeeld rechts toont wat er verdwijnt. Na je bevestiging gaan alle plannings van de woningen en de fases en taken van de hoofdplanning weg, met hun voortgang, en komt de nieuwe planning ervoor in de plaats. Dit kan je niet ongedaan maken. Het traject en de schijven blijven staan
   - bij één planning: of ze **in de hoofdplanning** komt. Dat kan enkel als de hoofdplanning nog leeg is. Is ze niet leeg, of zet je dit uit, dan komt er een aparte planning naast de hoofdplanning
5. Lees rechts bij **Wat er gebeurt** wat CPM gaat doen: een kaart voor de planning, het traject en de schijven met hun percentages. Een schijf die onder het minimum van het sjabloon zakt, staat oker met een melding. Onderaan de kaarten staat wat **ongemoeid blijft**, en onderaan het venster een samenvatting van wat **Toepassen** maakt.
6. Klik op **Toepassen**.

![Het venster Projectsjabloon toepassen met de vragen en het voorbeeld](/img/help/projecten/projectsjabloon-toepassen/1-toepassen.png)

Heeft het sjabloon ook een planning voor het project zelf, bijvoorbeeld de wegenis bij een verkaveling, dan verschijnt in het venster de vraag **Start van de woningen**. Daar kies je of de woningen meteen starten of pas na een stap van de wegenis (fundering, nutsvoorzieningen, verharding of voorlopige oplevering). Het sjabloon stelt zelf een keuze voor. Het voorstel rechts toont de startdatum die daaruit volgt, en je kan die later per planning nog aanpassen.

## Wat het sjabloon aanpast

- **Planning:** blokken voor kelder of voor een hellend of plat dak komen enkel in de planning als ze bij het gebouw passen. De ruwbouw wordt herhaald per bouwlaag (gelijkvloers, 1ste verdieping, 2de verdieping …); blokken voor terrassen of luifels komen enkel bij de gekozen afdekken.
- **Schijven:** een schijf als de ruwbouw wordt onderverdeeld per bouwlaag ("... gelijkvloers", "... 1ste verdieping"). Schijven die enkel bij een kelder of een bepaald dak horen, komen enkel voor als het gebouw dat heeft.
- **Traject:** het traject wordt aangemaakt. Mijlpalen met dezelfde code als een taak in de planning volgen die taak.

## Let op

- Controleer wat de AI voorstelt. Zij kan een kelder of dak niet afleiden als je het niet vermeldt, en laat het dan leeg.
- Er wordt enkel aangevuld. Wil je een bestaande planning of bestaande schijven vervangen, doe dat dan met **Sjabloon** op de planningpagina of met **Sjabloon toepassen** in de betalingsgroep.
- Zijn de percentages van het schijvensjabloon samen geen 100 %, dan worden er geen schijven toegepast en staat dat in de melding.
