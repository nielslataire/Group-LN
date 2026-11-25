---
title: Betalingsschijven volgen en als bereikt aanduiden
topic: facturatie-wijzigingen
role: Projectleider · Boekhouding
device: Desktop · tablet
minutes: 4
updated: 2026-11-25
newsTag: Nieuw
screens: projecten-betalingsschijven
related: planning-schijven-koppelen, project-factureren, betalingsgroep-instellen, factuur-bekijken
synonyms: te markeren, volgens planning bereikt, alle markeren, stand, niet verkocht, kolommen per bouwlaag, schijf bereikt, betalingsschijf, schijven, eenheden, bewijs, werffoto, eindafrekening, betalingsgroep, terug uitzetten, factureerbaar
description: per eenheid zien welke schijf gefactureerd, bereikt of nog niet bereikt is
intro: |
  Op **Betalingsschijven** zie je per betalingsgroep een overzicht van schijven tegenover eenheden. Zodra een stap van de bouw klaar is, duid je de schijf aan als bereikt.
  Daarna verschijnt ze op de pagina Facturatie en kan je ze factureren.
before: |
  Je hebt schrijfrecht op facturatie van projecten. Zonder dat recht kun je het overzicht lezen, maar niets aanduiden. Het project heeft minstens één betalingsgroep met schijven.
faq:
  - q: Wat betekenen de bolletjes?
    a: Een vinkje is gefactureerd. Een gevuld bolletje is bereikt en klaar om te factureren. Een leeg bolletje is nog niet bereikt. Een gouden bolletje is volgens de planning bereikt, maar nog niet gemarkeerd. Een gearceerd vakje betekent dat de eenheid nog niet verkocht is of dat de akte nog niet verleden is.
  - q: Waarom staat er een gearceerd vakje in plaats van een bolletje?
    a: Bij de titel van het vakje staat de reden. Ofwel is de eenheid nog niet verkocht, ofwel is de akte nog niet verleden en is er nog geen verkochte waarde. Een niet verkochte eenheid heeft ook een grijze kolomkop.
  - q: Wat betekent "6 te markeren" bij een groep?
    a: Zes schijven van die groep zijn volgens de planning bereikt (de taak is afgevinkt of haar einddatum is voorbij), maar nog niet voor alle eenheden gemarkeerd.
  - q: Wat doet "Alle 6 markeren voor 12 eenheden…"?
    a: Na jouw bevestiging markeert CPM die schijven voor alle eenheden die nog open staan, met vandaag als datum. Wil je kiezen, markeer dan per schijf.
  - q: Kan ik een schijf terug uitzetten?
    a: Ja, zolang ze nog niet gefactureerd is. Klik op het gevulde bolletje van die eenheid. Een gefactureerde schijf kan niet meer terug.
  - q: Waarom kan ik een bereikt bolletje niet aanklikken?
    a: De schijf is voor de hele groep op factureerbaar gezet. Dan is ze voor alle eenheden bereikt en kun je ze niet per eenheid terugzetten.
  - q: Wat is de eindafrekening?
    a: De laatste schijf van de groep. Bij het factureren trekt ze de andere open schijven en de open meer- en minwerken van het klantaccount mee.
  - q: Moet ik een bewijs toevoegen?
    a: Nee, het is optioneel. Een gekozen werffoto komt mee als bijlage bij de factuur.
---
## De betalingsgroepen en schijven lezen

Bovenaan staat elke betalingsgroep als tab, met de naam, het btw-percentage en het aantal eenheden. Staat er een gouden label zoals **6 te markeren**, dan zijn er schijven die volgens de planning bereikt zijn. Met **+ Betalingsgroep** maak je een nieuwe groep.

De kop van de groep staat op één regel: btw, het aantal eenheden waarvan er zoveel verkocht zijn, het aantal schijven en het totaal. Dat totaal hoort 100 % te zijn; dan staat er een vinkje. Rechts staan **Sjabloon toepassen** en **Groep bewerken**.

In de matrix staat elke schijf op een rij, met nummer, naam en percentage. Elke eenheid heeft een smalle kolom met een bolletje. De kolommen staan gegroepeerd per bouwlaag (of, zonder bouwlagen, per soort zoals parking en berging), zodat alles op één scherm past. Rechts staat de **stand** per schijf, bijvoorbeeld "0 / 12 gefactureerd".

Staat een schijf gekoppeld aan de planning, dan zie je er de taak en de datum onder. Is de taak afgevinkt of de datum voorbij, dan staat er in goud "volgens planning klaar …" en kleurt de rij licht goud.

## De stand van een schijf per eenheid aflezen

![De groepskaart met de matrix van schijven per eenheid en de legende](/img/help/facturatie-wijzigingen/betalingsschijven-volgen/2-matrix.png)

- Vinkje: gefactureerd.
- Gevuld bolletje: bereikt, klaar om te factureren.
- Leeg bolletje: nog niet bereikt.
- Gouden bolletje: volgens de planning bereikt, maar niet gemarkeerd.
- Gearceerd vakje: nog niet verkocht, of akte nog niet verleden.

De legende staat bovenaan de matrix, met erbij welke eenheden niet verkocht zijn.

De stand geldt per eenheid. Dezelfde schijf kan bij de ene eenheid bereikt zijn en bij de andere niet.

## Een schijf als bereikt aanduiden

![Het venster Schijf bereikt aanduiden met de eenheden, de datum en het bewijs](/img/help/facturatie-wijzigingen/betalingsschijven-volgen/3-bereikt-venster.png)

1. Klik op **Markeren (n)** achter de schijf, in de kolom Stand. Het getal is het aantal eenheden dat nog open staat. De knop staat er enkel als minstens één eenheid nog niet bereikt is.
2. Vink onder **Voor welke eenheden** de eenheden aan. Eenheden die al bereikt of gefactureerd zijn, of niet verkocht, staan grijs met de reden. Met **Geen selecteren** haal je alles weg.
3. Controleer de **Datum bereikt**. Standaard is dat vandaag.
4. Kies eventueel een **Bewijs** met **Werffoto's kiezen uit Media**.
5. Klik op de knop onderaan, bijvoorbeeld **Bereikt voor 2 eenheden**.

CPM toont "Bereikt voor N eenheden" en de bolletjes zijn gevuld. Op de pagina Facturatie van het project staat de schijf nu klaar. Er wordt nog niets gefactureerd. Zie "Facturen opmaken voor schijven en wijzigingen van een project".

Wil je dat CPM je er attent op maakt zodra de bouwstap klaar is? Koppel de schijf dan aan een taak of fase in de planning (zie "Schijven koppelen aan de planning").

## Alles in één keer markeren

Zijn er schijven die volgens de planning bereikt zijn, dan staat boven de matrix een gouden kader: "6 schijven zijn volgens de planning bereikt, maar nog niet voor alle eenheden gemarkeerd". Klik op **Alle 6 markeren voor 12 eenheden…** en bevestig. Elke schijf wordt dan voor alle open eenheden gemarkeerd, met vandaag als datum. Er wordt nog niets gefactureerd.

## Eén eenheid snel aanduiden of terugzetten

Klik op een leeg bolletje om die schijf voor die ene eenheid meteen als bereikt aan te duiden. Klik op een gevuld bolletje om ze terug uit te zetten.

Is de schijf al gefactureerd voor die eenheid, dan lukt terugzetten niet en krijg je een melding.

## Een betalingsgroep toevoegen of aanpassen

Klik op **+ Betalingsgroep** naast de tabs voor een nieuwe groep. Klik op **Groep bewerken** in de kop van een kaart om de schijven en percentages aan te passen, of op **Sjabloon toepassen** om ze uit een schijvensjabloon te halen. Zie "Een betalingsgroep en haar schijven instellen".

## Let op

Duid een schijf pas aan als de stap echt klaar is. Daarna kan de boekhouding ze factureren. Een foute aanduiding zet je enkel terug zolang er nog geen factuur voor die eenheid bestaat.
