---
title: Een leverancier toevoegen of bewerken
topic: projecten
role: Administratief medewerker
device: Desktop · tablet
minutes: 6
updated: 2026-10-26
newsTag: Nieuw
screens: leveranciers-nieuw, leveranciers-bewerken
related: leveranciers-overzicht, leverancier-bekijken, project-leverancier-toevoegen
synonyms: nieuwe leverancier, leverancier aanmaken, ondernemingsnummer controleren, btw-status, bedrijfsvorm, afdeling toevoegen, contactpersoon, inactief zetten, ook klant
description: een leverancier met adres, activiteiten, afdelingen en contactpersonen aanmaken of wijzigen
intro: |
  Met dit formulier maak je een leverancier aan of pas je haar vaste gegevens aan. Het formulier heeft drie tabbladen: algemene gegevens, afdelingen en contacten.
before: |
  Je hebt schrijfrecht op leveranciers, voor minstens één facturatiebedrijf. Je start vanuit de leverancierslijst met **+ Leverancier toevoegen**, of met **Bewerken** bij een bestaande leverancier.
faq:
  - q: Wat doet de knop Controleren?
    a: CPM zoekt het ondernemingsnummer op en toont naam en adres in een venster. Met Gegevens overnemen vult CPM die in het formulier in.
  - q: Waarom zegt CPM dat er al een leverancier met dit ondernemingsnummer bestaat?
    a: Een ondernemingsnummer mag maar één keer voorkomen. Zoek de bestaande leverancier in de lijst en pas die aan.
  - q: Waarom moet het btw-nummer gelijk zijn aan het ondernemingsnummer?
    a: Voor Belgische ondernemingen is het hetzelfde nummer. Laat je het btw-nummer leeg, dan vult CPM het zelf in.
  - q: Wat doet Onderneming is actief?
    a: Staat het uit, dan blijft de leverancier in de historiek staan maar kan je ze niet meer kiezen. Je ziet het label INACTIEF.
  - q: Wat doet Deze leverancier is ook klant?
    a: Dan toont CPM het label OOK KLANT. Het heeft ook een gevolg voor Octopus, het boekhoudpakket. Alleen bij een leverancier die ook klant is, geeft CPM gewijzigde gegevens door aan Octopus, en enkel als de leverancier daar al staat. Staat de schakelaar uit, dan blijft Octopus ongemoeid. Staat de leverancier in Octopus als klant, dan zet CPM de schakelaar zelf aan.
  - q: Wat gebeurt er met de contacten als ik een afdeling verwijder?
    a: De afdeling wordt verwijderd zodra je het formulier opslaat. Haar contacten blijven bestaan, maar hebben dan geen afdeling meer.
---
## Het formulier openen

1. Klik in de leverancierslijst op **+ Leverancier toevoegen**, of kies bij een leverancier de drie puntjes en dan **Bewerken**. Je kan ook op de leverancierspagina op **Bewerken** klikken.
2. Rechtsboven verschijnt **NIET-OPGESLAGEN WIJZIGINGEN** zodra je iets aanpast.

## Identificatie invullen

![De kaart Identificatie in het lege formulier, met de knop Controleren bij het ondernemingsnummer](/img/help/projecten/leverancier-beheren/3-identificatie.png)

1. Vul **Bedrijfsnaam** in. Dit veld is verplicht.
2. Kies een **Bedrijfsvorm** uit de lijst.
3. Kies bij **Facturatiebedrijven** minstens één bedrijf.
4. Vul het **Ondernemingsnummer** in en klik op **Controleren**. Bij een Belgische onderneming moet het 10 cijfers hebben.
5. Controleer het venster en klik op **Gegevens overnemen**.
6. Het **Btw-nummer** is bij een Belgische onderneming gelijk aan het ondernemingsnummer. Kies bij **Btw-status** de juiste optie.

## Adres invullen

Vul **Straat** en **Nr** in. Kies bij **Land** het land.
Bij België zoek je de **Gemeente / postcode** in de lijst. Bij een ander land typ je **Postcode** en **Gemeente** zelf. Straat, nummer, gemeente en land zijn verplicht.
Dit is de maatschappelijke zetel. Leveradressen zet je bij een afdeling.

## Bereikbaarheid en samenwerking

Vul **Telefoon**, **Gsm**, **E-mail**, **Facturatie e-mail** en **Website** in.
Onder **Samenwerking & facturatie** staan de schakelaars.

- **Onderneming is actief**: uit betekent dat de leverancier in de historiek blijft maar niet meer kiesbaar is.
- **Digitale facturatie vereist** en **UBL standaard meesturen**: de tweede hangt af van de eerste.
- **Deze leverancier is ook klant**.

Kies bij **Activiteiten** de werkzaamheden van de leverancier. Typ om te zoeken. De lijst is gegroepeerd.

## Afdelingen toevoegen

![Een afdelingskaart met de adres- en contactvelden](/img/help/projecten/leverancier-beheren/7-afdeling.png)

1. Open het tabblad **Afdelingen** en klik op **Afdeling toevoegen**.
2. Vul **Naam**, e-mail, adres, telefoon en gsm in.
3. Klik op de prullenbak om een afdeling te verwijderen.

## Contacten toevoegen

1. Open het tabblad **Contacten** en klik op **Contact toevoegen**.
2. Vul **Naam**, **Voornaam** en **Functie** in, en kies een **Afdeling** uit de afdelingen die je net toevoegde. **Geen** is ook mogelijk.
3. Vul e-mail, telefoon en gsm in.

## Opslaan

Klik onderaan op **Opslaan**. Is iets fout, dan toont CPM een overzicht en opent het tabblad met de fout. Een rode stip bij een tabblad wijst erop.
Na het opslaan kom je terug op de pagina waar je vandaan kwam.
Klik je op **Annuleren** met niet-opgeslagen wijzigingen, dan vraagt CPM of je ze wil weggooien.

## Resultaat

De leverancier staat in de leverancierslijst en kan gekozen worden bij een contract.
Is de leverancier ook klant en stond ze al in Octopus, dan geeft CPM de gewijzigde gegevens daar ook door. Lukt dat niet, dan is de leverancier wel bijgewerkt in CPM en zie je een gele melding dat Octopus niet bijgewerkt is. Sla de leverancier later opnieuw op om het nog eens te proberen.

## Let op

Staat het e-mailadres van een contact goed? Dat adres is nodig om hem later uit te nodigen voor het werfportaal. Zie "Een leveranciersmedewerker uitnodigen voor het werfportaal".
