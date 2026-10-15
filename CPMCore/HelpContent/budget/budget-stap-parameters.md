---
title: De percentages, de grondkost en de marges instellen
topic: budget
parent: eerste-budget
treeOrder: 7
treeLabel: Stap 7 · Parameters
role: Projectleider
device: Desktop · tablet
minutes: 8
updated: 2026-10-13
screens: budget-stap-7
synonyms: stap 7, parameters, erelonen, architect, ingenieur, decennale, grondkost, aankoop grond, registratierechten, notaris, financiering, breyne, straight loan, onvoorzien, marge, makelaarscommissie
description: erelonen, verzekeringen, grondkost, financiering, marges en verkoopkosten
related: eerste-budget, budget-stap-activiteiten, budget-stap-verkoop, budget-meldingen
intro: |
  In stap 7 stel je de kosten in die geen bouwwerk zijn. Dat zijn erelonen, verzekeringen, de aankoop van de grond, de financiering en de
  marges. De meeste zijn percentages van de bouwkost uit stap 6.
before: |
  Stap 6 is ingevuld. De percentages rekenen op de bouwkost uit die stap.
faq:
  - q: Waar komen de standaardpercentages vandaan?
    a: Uit Instellingen, bij Bouwkost %. Je kunt ze hier voor dit budget aanpassen. Een leeg veld of een veld met 0 volgt de standaard.
  - q: Waarom staat EPB niet bij de erelonen?
    a: EPB, veiligheidscoördinatie en ventilatieverslaggeving staan als activiteit onder Lot 0 Algemeen in stap 6.
  - q: Wat als ik meerdere stukken grond koop?
    a: Voeg voor elke akte een regel toe bij Aankopen. Elke akte krijgt eigen notariskosten.
  - q: Waarom krijg ik een waarschuwing bij een percentage?
    a: Het wijkt meer dan 50 % af van de standaard in Instellingen. Controleer of het klopt.
---
## De bouwkost en het totaal bekijken

![Erelonen, verzekeringen en andere percentages](/img/help/budget/budget-stap-parameters/1-erelonen-verzekeringen.png)

Bovenaan staan vier blokken: **Bouwkost activiteiten** (de basis voor de percentages), **Aantal eenheden**, **Liften** en **Totaal parameters**.
Onder elk percentage staat het bedrag in euro, bijvoorbeeld **= € 99.715**.

## Erelonen en verzekeringen invullen

1. Controleer de percentages bij **Erelonen & studies**: projectcoördinatie, architect, ingenieur, opmeting en sondering.
2. Controleer bij **Verzekeringen** de **Decennale gesloten ruwbouw** en de **ABR + plaatsbeschrijving**.
3. Pas een percentage aan als dit budget afwijkt van de standaard.

De decennale is de verzekering uit de Wet Peeters. Die schrijft een verzekering van 10 jaar verplicht voor. Een decennale van 0 % geeft een waarschuwing.

## Infrastructuur, liften en publiciteit invullen

Bij **Infrastructuur & liften** vul je een vast bedrag in voor de **Infrastructuur** en een bedrag **Lift per stuk**. Het aantal liften komt uit stap 1.
**Publiciteit** is een percentage van de totale verkoopprijs.

## De grondkost invullen

![De kaart Grondkost](/img/help/budget/budget-stap-parameters/2-grondkost.png)

1. Vul bij **Aankopen** een regel in per akte: een **Omschrijving**, de **Datum akte** en de **Aankoopprijs**.
2. Klik op **Toevoegen** voor een volgende akte. Met het icoon naast een regel kopieer of verwijder je ze.
3. Vul de **Registratierechten** in, als percentage van de totale aankoopprijs.
4. Vul de **Aktekosten per akte** in, exclusief btw. CPM vermenigvuldigt ze met het aantal akten.

De **Notaris — ereloon** berekent CPM zelf, volgens de wettelijke schaal plus 21 % btw. Je past het niet aan.
Rechts staat de **Berekening** met de totale grondkost en de grondkost per eenheid. Die kost telt mee in de totale kostprijs en in de grondwaarde in stap 8.
Is de aankoopprijs leeg, dan krijg je een waarschuwing.

## De financiering invullen

![Financiering, marges en verkoopkosten](/img/help/budget/budget-stap-parameters/3-financiering-marges.png)

Bij **Financiering** vul je per lening het percentage en de looptijd in maanden in:

- **Wet Breyne**
- **Straight loan gebouw**
- **Straight loan grond**

Vul ook de **Onvoorzien** in, als percentage. Een lening zonder aantal maanden is een fout.
**Wet Breyne** is de kost die de bank aanrekent voor het stellen van een bankwaarborg van 100 %. Een **straight loan** is een lening van de bank om de grond of het bouwwerk te financieren.
Wet Breyne en de straight loan voor het gebouw rekenen op de bouwkost. De straight loan voor de grond rekent op de aankoopprijs van de grond.

## De marges invullen

Bij **Verkoop — marges** vul je drie percentages in:

- **Doelmarge op bouwkost**: de winst op de bouw, netto na verkoopkosten.
- **Marge op grondkost**: de winst op de grond, netto na verkoopkosten.
- **Waarde 2e grondzone**: de waarde van de tweede grondzone tegenover de eerste. CPM gebruikt ze om de grondkost te verdelen over de eenheden. Laat je ze leeg, dan rekent CPM met 20 %.

Een leeg veld volgt de standaard. Onder elk percentage staat het bedrag.

## De verkoopkosten invullen

Bij **Verkoopkosten** vul je de **Makelaarscommissie** in, als percentage van de verkoopprijs per eenheid. De **Btw op commissie** staat vast op 21 %.
Vul bij **Overige verkoopkosten** een bedrag in voor zaken zoals een EPC of een keuring, per project.

Het kader rechts legt de btw uit. Op het grondaandeel is de btw niet aftrekbaar. Ze telt dus mee als kost. Op het bouwaandeel is ze aftrekbaar. Dan telt alleen de commissie zonder btw mee.
De verdeling tussen grond en bouw komt uit stap 8.

## Resultaat

CPM rekent de bedragen meteen uit. Klik onderaan op **Opslaan & volgende: Verkoop**.

## Let op

- Een percentage dat meer dan 50 % afwijkt van Instellingen geeft een waarschuwing.
- Een lening zonder aantal maanden is een fout. Je kunt dan niet afronden.
