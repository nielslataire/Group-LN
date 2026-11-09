---
title: De inhoudingsplicht van aannemers controleren
topic: facturatie-wijzigingen
role: Boekhouding · Projectleider
device: Desktop · tablet
minutes: 4
updated: 2026-11-09
newsTag: Nieuw
screens: inhoudingsplicht
related: factuur-aanmaken, betalingsschijven-volgen
synonyms: inhoudingsplicht, 30bis, 30ter, rsz, fiscus, sociale schulden, fiscale schulden, aannemer controleren, btw-nummer, bewijs
description: aannemers vóór een betaling bij de RSZ en de FOD Financiën controleren en het bewijs bewaren
intro: |
  Heeft een aannemer schulden bij de RSZ of de fiscus, dan moet je bij de betaling een deel inhouden en zelf doorstorten. Op **Inhoudingsplicht** controleer je meerdere aannemers in één keer, vlak voor je betaalt. CPM bewaart elke lijst als bewijs.
before: |
  Je hebt leesrecht op facturatie om de lijsten te bekijken. Om een controle te starten heb je schrijfrecht nodig. De koppeling met de RSZ moet ingesteld zijn. Staat er een gele melding over de testomgeving, dan zijn de antwoorden vaste testgegevens en gelden ze niet als bewijs.
faq:
  - q: Wanneer controleert CPM zelf?
    a: Bij elke nieuwe factuur van een aannemer en bij elk nieuw contract. Is er al een recente controle die nog geldig is, dan gebruikt CPM die. Zonder inhoudingsplicht blijft een controle geldig tot de vervaldatum die de RSZ opgeeft. Met inhoudingsplicht controleert CPM na 24 uur opnieuw.
  - q: Kan ik een contract afsluiten met een aannemer met inhoudingsplicht?
    a: Nee. CPM weigert het contract zolang de aannemer een inhoudingsplicht heeft. Een factuur van die aannemer kun je wel registreren, want je hebt ze ontvangen. Je krijgt dan een waarschuwing.
  - q: Waar zie ik de inhoudingsplicht van één aannemer?
    a: Op de fiche van de leverancier, bovenaan het tabblad Algemeen. Daar zie je de laatste controle en de eerdere controles. Met **Nu controleren** start je een nieuwe.
  - q: Wie krijgt een melding bij een inhoudingsplicht?
    a: De boekhouding en de CEO krijgen een melding op hun dashboard zolang de laatste controle van een aannemer een inhoudingsplicht toont.
  - q: Wat staat er in het bewijs?
    a: Per onderneming de referentie van de controle, het trace-id van de RSZ, het tijdstip, wie de controle deed en de omgeving. Het JSON-bestand bevat bovendien het volledige antwoord van de RSZ.
  - q: Waarom staat er "Mislukt" bij een aannemer?
    a: De RSZ gaf geen antwoord, of het nummer is niet gekend. Controleer het btw-nummer en probeer het later opnieuw. Er is dan nog geen uitspraak over de inhoudingsplicht.
---
## Aannemers kiezen

1. Open **Facturatie** en kies **Inhoudingsplicht**.
2. Kies bij **Bron** welke aannemers je wilt zien: **Actieve werven**, **Laatste 12 maanden** of **Alle leveranciers**.
3. Bij **Actieve werven** kun je werven aan of uit zetten met de knoppen onder **Werven**.
4. Vink de aannemers aan die je wilt controleren. Bij elke aannemer zie je de werven, de laatste factuur en het resultaat van de laatste controle.

Zoek je een aannemer, typ dan een deel van de naam, de activiteit of het btw-nummer.

## Extra btw-nummers toevoegen

Staat een onderneming niet bij je leveranciers, plak dan het nummer in **Extra btw-nummers**. Zet één nummer per lijn of scheid ze met komma's. Met of zonder BE, punten of spaties lukt allemaal. Een naam ervoor mag, bijvoorbeeld `Renovatie Bauwens; 0767.412.093`.

Hoort een nummer bij een bestaande leverancier, dan vinkt CPM die aannemer gewoon aan in de lijst. Ongeldige nummers slaat CPM over en meldt dat.

## Controleren en bewaren

1. Kijk rechts bij **Te controleren** of de lijst klopt. Verwijder een aannemer met het kruisje.
2. Geef de lijst een **naam**, bijvoorbeeld de betaalronde. Laat je het leeg, dan bewaart CPM ze zonder naam.
3. Klik op **Controleer en bewaar**.

CPM vraagt de RSZ en bewaart de lijst. Het zijpaneel met het resultaat opent meteen.

## Het resultaat lezen

Het zijpaneel toont per onderneming twee kolommen: **RSZ** voor sociale schulden en **Fiscus** voor fiscale schulden. Staat er **Inhouden** met een percentage, dan houd je dat percentage van het factuurbedrag excl. btw in en stort je het aan de RSZ of de FOD Financiën. De rest betaal je aan de aannemer.

Onder elke onderneming staat het bewijs: de referentie en het trace-id. Met **Bewijs (PDF)** open je de afdrukbare lijst. Met **Bewijs (JSON)** download je het volledige antwoord van de RSZ.

## Een eerdere lijst opnieuw gebruiken

Onderaan staan **Eerdere controlelijsten**. Klik op de naam van een lijst om het bewijs te openen. Klik op **Opnieuw selecteren** om dezelfde aannemers opnieuw aan te vinken. De naam krijgt dan "(herhaling)" erachter.

## Let op

Een controle geldt op het moment dat ze gebeurd is. Controleer daarom vlak vóór je betaalt, en bewaar de lijst als bewijs dat je dat gedaan hebt. Een resultaat uit de testomgeving heeft geen bewijswaarde.
