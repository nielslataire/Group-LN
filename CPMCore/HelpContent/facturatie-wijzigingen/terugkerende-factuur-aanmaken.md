---
title: Een terugkerende factuur aanmaken
topic: facturatie-wijzigingen
role: Boekhouding
device: Desktop · tablet
minutes: 6
updated: 2026-11-06
screens: facturen-nieuw
synonyms: terugkerend, schema, abonnement, huur, beheer, onderhoud, maandelijks, kwartaal, jaarlijks, herhalen, periodiek factureren, indexeren, variabelen, maand jaar
newsTag: Nieuw
description: één schema maken zodat het CRM elke periode zelf een concept klaarzet
related: terugkerende-facturen-beheren, terugkerende-facturen-instellen, factuur-aanmaken, factuur-nummeren-en-verzenden
intro: |
  Factureer je dezelfde dingen regelmatig, bijvoorbeeld huur, beheer of een vergoeding? Maak dan één schema. Het CRM zet daarna bij elke factuurdatum zelf een concept klaar.
  Je maakt een schema aan op dezelfde pagina als een gewone factuur. Het enige verschil is dat je bij **Soort** voor **Terugkerend** kiest.
before: |
  Je hebt het recht om facturen aan te maken. Een schema hoort bij één klant of leverancier en bij één facturatiebedrijf.
faq:
  - q: Worden de facturen automatisch genummerd en verstuurd?
    a: Nee. Het CRM zet een concept klaar als Proforma met de status Ter controle. Jij kijkt het na en nummert het in de lijst met facturen. Automatisch nummeren en versturen volgt later.
  - q: Wat als de factuurdag in het weekend of op een feestdag valt?
    a: Dan schuift de factuurdatum naar de volgende of de vorige werkdag, naargelang je instelling bij Weekend of feestdag. Je kan ook kiezen om de datum niet te wijzigen.
  - q: Wat betekent Enkel de eerste?
    a: Zo'n lijn komt alleen op de eerste factuur. Handig voor een waarborg of een eenmalige kost naast de vaste huur.
  - q: Hoe rekent het CRM met de gezondheidsindex?
    a: Het CRM neemt de gezondheidsindex van de maand vóór de verjaardag en deelt die door de index van de maand vóór de startdatum. Het nieuwe bedrag is het basisbedrag maal die verhouding. De indexen komen van Statbel en worden op de laatste dag van elke maand opgehaald.
  - q: Wat als de startdatum van het huurcontract al lang geleden is?
    a: Het CRM bewaart de indexen vanaf de eerste keer dat ze werden opgehaald. Is de maand vóór je startdatum niet bewaard, vul dan bij Basisindex zelf de index van die maand in. Gebruik basis 2013 = 100.
  - q: Wat als de index nog niet gepubliceerd is op de dag van de indexatie?
    a: Dan wacht het concept en komt het vanzelf klaar zodra de index bekend is. Op de detailpagina van het schema staat waarop er gewacht wordt.
  - q: Wanneer wordt een lijn geïndexeerd?
    a: Enkel lijnen met Geïndexeerd in stap 3. De indexatie gebeurt op de datum die je koos, bijvoorbeeld elk jaar op 1 januari of op de verjaardag van de start.
  - q: Kan ik een creditnota of een factuur uit schijven terugkerend maken?
    a: Nee. Een schema bestaat alleen uit vrije lijnen. Bij Soort kies je dan eerst Eenmalig.
  - q: Kan ik de eerste factuur in het verleden zetten?
    a: Nee. De eerste factuur moet vandaag of later vallen. Zo maakt het CRM niet in één keer een reeks oude concepten aan.
  - q: Mijn schema is nog niet klaar. Kan ik het bewaren?
    a: Ja. Kies Opslaan als draft. Het schema blijft dan een concept en er wordt niets aangemaakt tot je het activeert.
---
## Terugkerend kiezen

1. Begin zoals bij een gewone factuur en kies in stap 1 het facturatiebedrijf en de klant of leverancier.
2. Kies in stap 2 bij **Soort** voor **Terugkerend**. De factuurdatum maakt plaats voor een schema. De betaaltermijn blijft staan.
3. Of ga rechtstreeks via **Facturen**, tabblad **Terugkerend** en kies **Nieuw schema**.

## Het schema instellen

1. Kies bij **Herhaal** hoe vaak er gefactureerd wordt: week, maand, kwartaal of jaar.
2. Kies de **Factuurdag**. Bij een maand of kwartaal is dat een vaste dag of de laatste dag van de maand. Bij een week kies je een weekdag.
3. Bij **Eerste factuur** staat de eerstvolgende datum die past. Pas ze aan als de eerste factuur op een andere dag valt.
4. Kies bij **Periode op factuur** of de factuur de lopende of de vorige periode vermeldt. Huur wordt meestal vooraf gefactureerd, dus de lopende periode.
5. Bij **Stopt** kies je Nooit, Na aantal of Op datum.
6. Rechts zie je de planning met de eerstvolgende facturen, hun periode en hun bedrag. Ze past zich aan terwijl je kiest.

## Bepalen wat het CRM doet

1. Kies bij **Concept klaar** hoeveel dagen voor de factuurdatum het concept klaarstaat.
2. Kies bij **Weekend of feestdag** wat er met de datum gebeurt als die geen werkdag is.
3. Zet **Jaarlijks indexeren** aan als de bedragen stijgen. Kies wanneer. Bij de gezondheidsindex vul je geen percentage in, het CRM rekent met de indexen van Statbel. Bij een ander type vul je het percentage zelf in.
4. Laat **Nieuw bedrag eerst laten bevestigen** aanstaan als je het nieuwe bedrag wil zien voor het concept klaarstaat.
5. Zet **Melding als een vorige factuur nog openstaat** aan als je gewaarschuwd wil worden bij onbetaalde facturen.

## Lijnen invullen met variabelen

1. Typ de omschrijving van elke lijn één keer. Gebruik variabelen voor wat verandert: **{maand}**, **{jaar}**, **{kwartaal}**, **{periode}** of **{volgnr}**.
2. Klik in een omschrijving en daarna op een variabele om ze toe te voegen. Het CRM vult ze in op de dag dat de factuur gemaakt wordt.
3. Kies per lijn **Elke factuur** of **Enkel de eerste**. Zet bij een geïndexeerd schema per lijn **Geïndexeerd** aan of uit. Het bedrag dat je invult is het basisbedrag op de startdatum.
4. Rechts zie je hoe de eerste drie facturen eruitzien, met de variabelen al ingevuld.

## Controleren en activeren

1. Controleer in stap 4 de samenvatting van het schema. De teksten mogen ook variabelen bevatten.
2. Kies **Schema activeren**. Het schema opent op zijn detailpagina en de eerste factuur staat gepland.
3. Wil je nog niets laten aanmaken, kies dan **Opslaan als draft**.

## Let op

Elke factuur uit het schema is een gewone factuur. Ze krijgt pas haar nummer als je ze nummert. Een gewijzigd schema geldt voor de facturen die nog moeten komen. Concepten die al klaarstaan blijven ongewijzigd.
