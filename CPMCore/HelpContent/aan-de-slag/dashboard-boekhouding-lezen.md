---
title: Het dashboard van de boekhouding lezen
topic: aan-de-slag
role: Boekhouding
device: Desktop · tablet · gsm
minutes: 5
updated: 2026-11-05
newsTag: Nieuw
screens: dashboard
related: dashboard-lezen, bedrijf-bewerken, meldingen-instellen, account-en-rol
synonyms: boekhouding, facturen, inkomende facturen, openstaand, vervallen, btw-aangifte, betalen, goedkeuren, dubbele factuur, klaar om te factureren, proforma, schijven, akte, facturatiebedrijf
description: de startpagina voor de boekhouding met inkomende facturen, openstaande klantfacturen, proforma's en te factureren schijven per klant
intro: |
  Het **Dashboard** van de boekhouding toont geld in en uit. Bovenaan zie je wat nu aandacht vraagt. Daaronder werk je de inkomende facturen af, volg je wat klanten nog moeten betalen en zie je wat er te factureren valt.
  Alle cijfers gelden voor de facturatiebedrijven waartoe jij toegang hebt. Bij elke rij staat het bedrijf waartoe ze behoort.
before: |
  Je ziet dit dashboard als je dashboardtype **Boekhouding** is. Een beheerder stelt dat in bij **Gebruikers & rollen**. Welke facturatiebedrijven je ziet, hangt af van je rechten bij Facturatie.
faq:
  - q: Waarom ontbreken sommige kaarten, zoals Inkomende facturen en Openstaand bij klanten?
    a: Die onderdelen zijn tijdelijk verborgen tot ze op punt staan. Ze komen later terug op deze pagina.
  - q: Waarom zie ik maar een deel van onze bedrijven?
    a: Het dashboard toont enkel de facturatiebedrijven waartoe je toegang hebt bij Facturatie. Onder de kerncijfers staat de lijst van de bedrijven die meetellen. Mis je er een, vraag dan een beheerder om je rechten aan te passen.
  - q: Waarom staat de btw-aangifte op Niet ingesteld?
    a: Elk facturatiebedrijf dient op zijn eigen ritme in. Open het bedrijf bij **Instellingen** en kies bij **Btw-aangifte** Maandelijks of Per kwartaal. Daarna verschijnt de datum.
  - q: Wat is een proforma?
    a: Een conceptfactuur aan een klant die al is opgemaakt maar nog geen definitief nummer heeft. Je maakt ze definitief via **Definitief maken**.
  - q: Wanneer staat iets onder Te factureren per klant?
    a: Een schijf staat er als ze bereikt is, de akte van de klant verleden is en ze nog op geen enkele factuur of proforma staat. Een wijzigingsopdracht staat er als ze ondertekend is en er nog een bedrag van te factureren is. Wat op een proforma staat, staat bij de proforma's.
  - q: Waarom staat er bij Geblokkeerd altijd 0?
    a: Facturen die geblokkeerd zijn door een inhoudingsplicht of een ontbrekend attest tienjarige aansprakelijkheid kan CPM nog niet herkennen. Het tabblad is er al, maar blijft voorlopig leeg.
  - q: Waarom zijn sommige snelacties grijs?
    a: Betaling registreren, Herinnering versturen, Export naar boekhoudpakket, Bankafschrift importeren en het btw-overzicht bestaan nog niet. Ze staan alvast op hun plaats.
  - q: Wat telt mee bij Vervallen?
    a: Facturen aan klanten met een openstaand saldo waarvan de vervaldatum voorbij is. Concepten en geannuleerde facturen tellen niet mee.
---
## De vier kerncijfers bovenaan lezen

![De vier kerncijfers van het dashboard van de boekhouding](/img/help/aan-de-slag/dashboard-boekhouding-lezen/1-kerncijfers.png)

- **Inkomend te goedkeuren**: het aantal inkomende facturen dat nog verwerkt moet worden, met het totaalbedrag en hoeveel er deze week vervallen. Staat er minstens één vervallen deze week, dan kleurt de kaart goud.
- **Openstaand bij klanten**: het totaal dat klanten nog moeten betalen, het aantal facturen en het aantal bedrijven.
- **Vervallen**: de klantfacturen waarvan de vervaldatum voorbij is, met het bedrag en het aantal dagen van de oudste.
- **Btw-aangifte**: de eerstvolgende uiterste datum van al je bedrijven, met de naam van het bedrijf. Staan er bedrijven zonder ingestelde aangifte, dan zie je dat in de tekst.

Onder de kerncijfers staan de **Facturatiebedrijven** als filter. Klik op een bedrijf om enkel dat bedrijf te tonen. Je kunt meerdere bedrijven aanklikken. Met **Alle**, of door alle pillen uit te zetten, tellen alle bedrijven weer mee. De kerncijfers, meldingen en kaarten volgen de keuze.

## De meldingen afwerken

Links staat het paneel **Meldingen**, in drie groepen. Bij elke melding staat het bedrijf.

- **ACTIE VEREIST**: een inkomende factuur die mogelijk dubbel is. Vergelijk ze met de eerdere factuur voor je ze goedkeurt.
- **OP TE LOSSEN**: facturen zonder project, facturen met een onopgeloste waarschuwing, klantfacturen die al meer dan 30 dagen vervallen zijn en proforma's die op een definitieve factuur wachten.
- **TER INFO**: de btw-aangifte van elk bedrijf in de laatste drie weken voor de uiterste datum. Is de aangifte van een bedrijf nog niet ingesteld, dan staat daar ook een melding met een link naar dat bedrijf.

Klik op **Bekijk** om rechtstreeks naar de factuur te gaan. Meldingen van de boekhouding kun je niet uitstellen. Ze verdwijnen vanzelf als de oorzaak weg is.

## Inkomende facturen afwerken

![De kaart Inkomende facturen met drie tabbladen](/img/help/aan-de-slag/dashboard-boekhouding-lezen/3-inkomende-facturen.png)

De kaart **Inkomende facturen** heeft drie tabbladen.

1. **Te goedkeuren**: nieuwe, verrijkte en te keuren facturen, de vroegst vervallende eerst.
2. **Klaar om te betalen**: facturen die goedgekeurd of geboekt zijn en nog niet betaald.
3. **Geblokkeerd**: voorlopig leeg, zie de vragen onderaan.

Per factuur zie je de leverancier met het facturatiebedrijf, het project, het bedrag, de vervaldatum en een status. Een vervaldatum in het rood is al voorbij. De knop rechts opent de factuur in het documentencentrum en past zich aan de status aan, bijvoorbeeld **Goedkeuren**, **Koppelen** of **Vergelijk**.

Elk tabblad toont de eerste zes facturen. Met **Naar documentencentrum** zie je ze allemaal.

## Openstaand bij klanten volgen

De kaart **Openstaand bij klanten** toont het totaal en de verdeling naar vervaldatum: **Niet vervallen**, **1 – 30 dagen**, **31 – 60 dagen** en **Meer dan 60 dagen**. Onder **PER FACTURATIEBEDRIJF** zie je per bedrijf wat openstaat en wat vervallen is. Onder **LANGST VERVALLEN** staan de drie oudste facturen. Klik op een rij om de factuur te openen.

## Klaar om te factureren

![De kaart Klaar om te factureren met proforma's en schijven per klant](/img/help/aan-de-slag/dashboard-boekhouding-lezen/5-klaar-om-te-factureren.png)

De kaart **Klaar om te factureren** heeft twee delen. Bovenaan staat het totaalbedrag.

- **PROFORMA'S**: conceptfacturen aan klanten die nog definitief gemaakt moeten worden. Klik op **Definitief maken** om de factuur te openen en af te werken.
- **TE FACTUREREN PER KLANT**: per klantenaccount de schijven die bereikt zijn, waarvan de akte verleden is en die nog niet gefactureerd zijn, plus de ondertekende wijzigingsopdrachten waarvan nog een bedrag te factureren is. Je ziet het project, de eenheden, de schijven, de wijzigingsopdrachten, de aktedatum en het totaalbedrag. Klik op **Factuur maken** om naar de facturatie van dat project te gaan.

Beide lijsten tonen eerst vijf rijen. Met **Toon alle** zie je de rest. Een wijzigingsopdracht die nog niet ondertekend is, staat hier niet bij. Die vind je bij de facturatie van het project als geblokkeerd.

## Een snelactie starten

Rechts, op een breed scherm, staat de kaart **Snelacties**. Onder **Facturatie** maak je een factuur of creditnota aan en ga je naar het documentencentrum. Onder **Algemeen** zoek je een leverancier of klant. Op een tablet of gsm staan de belangrijkste acties in de balk onderaan.

De kaart **Deze maand** toont per bedrijf de btw-aangifte van deze maand en de afsluiting voor de boekhouder. Een bedrijf met maandaangifte heeft elke maand een **Maandafsluiting**. Een bedrijf met kwartaalaangifte heeft enkel op het einde van een kwartaal een **Kwartaalafsluiting**. Een bedrijf zonder ingestelde aangifte toont geen afsluiting.
