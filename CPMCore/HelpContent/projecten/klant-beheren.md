---
title: Een klant toevoegen of bewerken
topic: projecten
role: Administratief medewerker
device: Desktop · tablet
minutes: 6
updated: 2026-10-26
newsTag: Nieuw
screens: klanten-nieuw, klanten-bewerken
related: klanten-overzicht, klant-bekijken, project-klant-toevoegen, project-klant-bewerken
synonyms: nieuwe klant, klant aanmaken, klantgegevens wijzigen, ondernemingsnummer controleren, facturatieadres, contactpersoon, primair contact, digitale factuur
description: een klant met adres, contactpersonen en facturatievoorkeuren aanmaken of wijzigen
intro: |
  Met dit formulier maak je een klant aan of pas je de vaste gegevens van een klant aan, los van een project.
  Ben je een koper in een project aan het inschrijven? Gebruik dan **Klant toevoegen** in de klantenlijst van dat project.
before: |
  Je hebt schrijfrecht op klanten, voor minstens één facturatiebedrijf. Je start vanuit de klantenlijst met **+ Klant toevoegen**, of met **Bewerken** bij een bestaande klant.
faq:
  - q: Wat is het verschil met een klant toevoegen in een project?
    a: In een project koppel je de klant meteen aan eenheden en vul je ook mede-eigenaars en facturatie in. Dit formulier bewaart enkel de klant en zijn contactpersonen.
  - q: Kan ik hier een mede-eigenaar toevoegen?
    a: Nee. Mede-eigenaars beheer je enkel in de projectschermen. Bestaande mede-eigenaars blijven hier onaangeroerd.
  - q: Wat doet de ster bij een contactpersoon?
    a: Je stelt die persoon in als primair contact. In de klantenlijst gebruikt CPM zijn e-mailadres als de klant zelf geen e-mailadres heeft.
  - q: Wat gebeurt er met een lege contactpersoon?
    a: Een contactrij zonder ingevulde gegevens wordt bij het opslaan genegeerd.
  - q: Waarom vraagt CPM of ik de wijzigingen wil weggooien?
    a: Je hebt iets aangepast en klikt op Annuleren zonder op te slaan. Kies Terug naar het formulier om verder te werken, of Wijzigingen niet opslaan om alles te laten vallen.
  - q: Wat gebeurt er in Octopus als ik een klant bewerk?
    a: Was de klant al doorgegeven aan Octopus (het boekhoudpakket), dan geeft CPM de gewijzigde gegevens daar ook door. Lukt dat niet, dan is de klant wel bijgewerkt in CPM en zie je een gele melding dat Octopus niet bijgewerkt is. Sla de klant later opnieuw op om het nog eens te proberen.
---
## Het formulier openen

1. Open de klantenlijst via **Klanten** in het hoofdmenu.
2. Klik op **+ Klant toevoegen**, of kies bij een klant de drie puntjes en dan **Bewerken**. Je kan ook op de klantpagina op **Bewerken** klikken.

Het formulier heeft twee tabbladen: **Algemene gegevens** en **Contacten**. Rechtsboven verschijnt **NIET-OPGESLAGEN WIJZIGINGEN** zodra je iets aanpast.

## Identificatie invullen

![De kaart Identificatie in het lege formulier, met de knop Controleren bij het ondernemingsnummer](/img/help/projecten/klant-beheren/2-identificatie.png)

1. Kies bij **Facturatiebedrijven** minstens één bedrijf. Dit veld is verplicht.
2. Staat **Dit is een onderneming** aan? Vul dan **Bedrijfsnaam** in (verplicht) en eventueel het **Ondernemingsnummer**.
3. Staat het uit, dan is de klant een particulier. Kies een **Aanspreking** en vul **Naam** in (beide verplicht). **Voornaam** is optioneel.

Klik naast het ondernemingsnummer op **Controleren**. CPM zoekt het nummer op en toont een venster met naam en adres.
Klik op **Gegevens overnemen** om bedrijfsnaam, straat, nummer, postcode en gemeente in te vullen. Het facturatieadres vult CPM niet in.

## Adres invullen

Vul **Straat**, **Nr** en **Bus** in, en kies bij **Land** het land.
Bij België zoek je de **Gemeente / postcode** in de lijst. Bij een ander land typ je **Postcode** en **Gemeente** zelf.

Moet de factuur naar een ander adres? Zet **Ander facturatieadres** aan en vul de velden met **(facturatie)** in.

## Bereikbaarheid en facturatievoorkeuren

Vul **Telefoon**, **Gsm**, **E-mail** en **Facturatie e-mail** in.
Onder **Facturatievoorkeuren** kies je twee zaken.

- **Digitale facturatie vereist**: de klant wil enkel digitale facturen.
- **UBL standaard meesturen**: dit hangt af van de vorige schakelaar.

## Contactpersonen toevoegen

![Een contactkaart met de ster voor het primaire contact en de prullenbak om het contact te verwijderen](/img/help/projecten/klant-beheren/7-contacten.png)

1. Open het tabblad **Contacten** en klik op **Contact toevoegen**.
2. Vul **Naam** in (verplicht) en optioneel voornaam, e-mail, telefoon en gsm.
3. Zet per contact **Digitale factuur vereist** en **UBL standaard meesturen** zoals bij de klant zelf.
4. Klik op de ster om dit contact als primair contact in te stellen.
5. Klik op de prullenbak om een contact te verwijderen. Dat gebeurt pas bij het opslaan.

## Opslaan

Klik onderaan op **Opslaan**. Vult CPM iets niet goed in, dan krijg je een overzicht van de fouten en gaat het formulier naar het tabblad met de fout.
Na het opslaan kom je terug op de pagina waar je vandaan kwam en zie je een bevestiging.
Klik je op **Annuleren** met niet-opgeslagen wijzigingen, dan vraagt CPM of je ze wil weggooien.

## Resultaat

De klant staat in de klantenlijst, bij elk gekozen facturatiebedrijf.
Een klant die je hier aanmaakt, heeft nog geen eenheid. Een klant aan een eenheid koppelen kan voorlopig enkel via **Klant toevoegen** in het project. Maak de klant dus altijd via het project aan.

## Let op

Het e-mailadres en het gsm-nummer van de klant en zijn contactpersonen kunnen gebruikt worden om een code te sturen wanneer een wijzigingsopdracht ondertekend wordt.
CPM houdt wijzigingen van die gegevens bij. Controleer ze dus goed.
Wil je eenheden, mede-eigenaars of facturatie per project aanpassen? Ga dan naar "Een klant bewerken binnen een project".
