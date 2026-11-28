# Planning in de echte app uitproberen (C1, 28/11/2026)

Doel: één keer systematisch doorlopen wat tot nu toe enkel met unit-tests gecontroleerd is. Gebruik een testproject (bv. project 79 of 81). Vink af wat klopt en noteer wat niet klopt (scherm, wat je deed, wat je verwachtte). Vóór je begint: migraties tot en met 170 gedraaid, app herstart, Ctrl+F5.

## 1. Rechten
- [ ] Gebruiker met alleen leesrecht: ziet de planning, geen knoppen om te wijzigen, geen menu "Sjabloon", "Importeren", "Fase", "Taak toevoegen".
- [ ] Gebruiker met schrijfrecht: kan toevoegen, wijzigen, slepen en verwijderen.
- [ ] Gebruiker zonder recht op Planningssjablonen: ziet Instellingen › Planningssjablonen niet.

## 2. Taken en fases
- [ ] Fase en taak toevoegen, naam en duur inline wijzigen; WBS-nummers kloppen.
- [ ] Taak verplaatsen (slepen of menu) en fase inklappen/uitklappen.
- [ ] Taak verwijderen: vraagt bevestiging; voorgangers van andere taken blijven kloppen.
- [ ] Een taak met een onbestaande voorganger (bv. 9.9): er verschijnt een opvallende melding in het midden, geen toast rechtsonder; ze sluit met het kruisje of Esc.

## 3. Voorgangers en rekenregels
- [ ] Voorganger FS, SS, FF, SF met wachttijd: start- en einddatum volgen.
- [ ] Een kring (A wacht op B, B op A) wordt geweigerd met een duidelijke melding.
- [ ] Voorgangers uit een andere planning (`Woning 1:3.2`, `H:1.1`) in het veld Voorgangers.
- [ ] Estafette: de schakelaar "Wacht op dezelfde taak in …" in het taakpaneel; een taak waar je ze niet aanzet wacht niet op de vorige woning.

## 4. Kalender, weer en deadline
- [ ] Weekend en feestdagen tellen niet mee; verlof van het project (Projecten › Weather) verschuift taken.
- [ ] Slecht-weerdagen ("verlet") schuiven buitentaken op; binnentaken niet.
- [ ] Deadline (oplevering/uitvoeringstermijn): de marge of overschrijding staat bovenaan de planning; verleng een taak tot de deadline overschreden wordt en controleer de waarschuwing.
- [ ] Klantdetail: waarschuwing als het project geen weerstation heeft (C3, nog niet gebouwd: noteer of je het mist).

## 5. Sjablonen en projectsjabloon (nieuw sinds 22/11)
- [ ] Projectsjabloon "Appartementsgebouw": "Eén planning voor het gebouw" staat standaard aan; "Woningen per planning" staat er niet.
- [ ] Projectsjabloon "Woning": "Woningen per planning" en "Woningen zelf indelen" zijn er wel.
- [ ] Per betalingsgroep met schijven staat bij "Wat er gebeurt" wat ongemoeid blijft en waarom, met het aantal schijven van de groep en van het sjabloon.
- [ ] Gevelafwerking (pleister, hout, aluminium): per keuze een taak in Gevel en buitenschrijnwerk, in de volgorde stenen gevel, pleister, hout of aluminium.
- [ ] Toepassen met "Vervang": de oude planning verdwijnt en de schijven hangen weer aan de nieuwe taken.

## 5b. Schijven en traject
- [ ] Taak klaar zetten: de vraag of de schijf bereikt is verschijnt; bevestigen markeert de eenheden.
- [ ] Een mijlpaal van één woning volgt enkel de planning van die woning.
- [ ] Een trajectmijlpaal koppelen aan een taak (vanuit de planning) en omgekeerd (vanuit het Traject: C4, nog niet gebouwd: noteer of je het mist).

## 6. Import en uitvoerders
- [ ] MS Project-XML inlezen: koppeling van resources enkel met aannemers met een contract in het project.
- [ ] Uitvoerder kiezen bij een fase en taak (zoekveld); een taak erft van haar fase.
- [ ] Tab "Per aannemer": toont enkel de taken van die aannemer.

## 7. Afdrukken en weergave
- [ ] Afdrukken, tab Globaal (alle plannings), inklappen van fases, Gantt schalen (dag/week/maand).
- [ ] Op een tablet of smal scherm: bruikbaar, niets valt weg.

Resultaat: noteer afwijkingen hieronder of in `PLANNING_VOORTGANG.md`.
