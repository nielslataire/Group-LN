---
title: De factuurstandaarden van een bedrijf instellen
topic: instellingen
role: Boekhouding
device: Desktop · tablet
minutes: 5
updated: 2026-10-26
newsTag: Nieuw
screens: instellingen-bedrijven-bewerken
related: bedrijf-bewerken, nummerreeks-beheren, bankrekening-toevoegen, factuurlayout-en-mail-instellen
synonyms: betaaltermijn, btw-code, taal, valuta, nummerpatroon, epc, qr-code, peppol, e-invoicing, ubl, facturatie
description: standaard betaaltermijn, nummerpatroon, betaal-QR en Peppol
intro: |
  Op het tabblad **Facturatie** bepaal je wat standaard op nieuwe facturen van dit bedrijf staat. Je beheert hier ook de betaaltermijnen, de betaal-QR en Peppol.
before: |
  Je bewaart de wijzigingen met **Bedrijf opslaan** onderaan. Enkel de nummerreeksen en de bankrekeningen bewaren apart.
faq:
  - q: Wat doet de QR-code op de factuur?
    a: Klanten betalen met één scan in hun bankapp. Laat je het IBAN leeg, dan gebruikt CPM de standaardrekening.
  - q: Wat is het nummerpatroon?
    a: Het uiterlijk van het factuurnummer, bijvoorbeeld {num:0000}-{date:MM-yyyy}.
  - q: Wat betekent het cijfer 1 op het tabblad Facturatie?
    a: E-invoicing staat uit en er is geen Octopus-dossier gekoppeld. Het cijfer verdwijnt zodra een van beide ingesteld is.
  - q: Wat is Peppol Participant ID?
    a: Het adres waarmee het bedrijf e-facturen ontvangt. CPM stelt er een voor uit het ondernemingsnummer.
---
## De standaarden voor nieuwe facturen kiezen

Onder **Standaarden voor nieuwe facturen** kies je de **Standaard betaaltermijn**, de **Standaard btw-code**, de **Taal** en de **Valuta**. Vul ook het **Nummerpatroon** in.

## Betaaltermijnen beheren

![Het venster Betaaltermijn](/img/help/instellingen/bedrijf-factuurstandaarden/2-betaaltermijn.png)

Onder **Betaaltermijnen** staan de termijnen die kiesbaar zijn op facturen van dit bedrijf.

1. Klik op **Toevoegen**.
2. Geef een **Naam**, kies het **Type** en vul het aantal **Dagen** in.
3. Kies de **Weergave op factuur** en vul eventueel de **Tekst op factuur** in.
4. Klik op **Toepassen** en bewaar daarna het bedrijf.

Klik op een bestaande termijn om ze aan te passen of met **Verwijderen** te schrappen.

## De betaal-QR instellen

Onder **EPC-betaal-QR** zet je **QR-code op de factuur** aan. Vul de **Naam begunstigde**, het **IBAN** en de **BIC** in. Kies het type **Mededeling** en het **Sjabloon mededeling**.

## Peppol activeren

Onder **Peppol / e-invoicing** zet je **E-invoicing actief** aan en vul je het **Peppol Participant ID** in. Met **PDF bijvoegen in UBL** gaat de pdf mee in het UBL-bestand.

## Nummerreeksen

De kaart **Nummerreeksen** staat op hetzelfde tabblad. Zie "Een nummerreeks voor facturen beheren".

## Let op

Zodra er in het lopende jaar al facturen een nummer hebben, kun je het nummerpatroon niet meer wijzigen. CPM toont dan de melding dat het patroon niet meer gewijzigd kan worden. Pas het patroon aan bij de start van een nieuw boekjaar, voor je de eerste factuur van dat jaar nummert. Facturen die al een nummer hebben, behouden altijd hun nummer.
