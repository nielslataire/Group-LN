---
title: Een bedrijf koppelen met Octopus
topic: instellingen
role: Boekhouding · Beheerder
device: Desktop
minutes: 5
updated: 2026-10-26
newsTag: Nieuw
screens: instellingen-bedrijven-bewerken
related: bedrijf-bewerken, bedrijven-beheren, facturen-doorboeken, factuur-nummeren-en-verzenden
synonyms: octopus, boekhouding, token, dossier, boekjaren, dagboek, aankoopdagboek, relaties, koppeling, synchroniseren
description: aanmelden bij Octopus, een dossier kiezen en de import instellen
intro: |
  Op het tabblad **Boekhouding** koppel je een bedrijf met zijn dossier in Octopus. Daarna kan CPM facturen doorsturen en boekjaren en dagboeken ophalen.
  De koppeling gaat in drie stappen: Aanmelden, Dossier en Import.
before: |
  Het bedrijf moet al bewaard zijn. Bij een nieuw bedrijf staat er "Bewaar eerst het bedrijf om tokens te kunnen ophalen" en zijn de knoppen grijs. Je hebt de aanmeldgegevens van Octopus nodig.
faq:
  - q: Wat betekent Token verlopen?
    a: De aanmelding bij Octopus is niet meer geldig. Doorloop stap 1 opnieuw met Token vernieuwen.
  - q: Waarom zijn stap 2 en 3 grijs?
    a: Ze werken pas als stap 1 geslaagd is, dus als er een geldig token is.
  - q: Gaan bestaande boekingen verloren als ik een ander dossier kies?
    a: Nee. Een ander dossier kiezen koppelt meteen om, maar bestaande boekingen blijven staan.
  - q: Wat doet het aankoopdagboek-prefix?
    a: Enkel dagboeken die met deze letter beginnen worden inkomende facturen. Bij A zijn dat A1, A2 en zo verder. Leeg betekent alles.
  - q: Kan ik de boekjaren hier aanpassen?
    a: Nee. Boekjaren en dagboeken komen uit Octopus en zijn alleen lezen. Synchroniseer het dossier om ze op te halen.
---
## Stap 1: aanmelden

![De kaart Octopus-koppeling met de drie stappen](/img/help/instellingen/octopus-koppelen/1-stappen.png)

Vul de **Gebruikersnaam** en het **Wachtwoord** van Octopus in. Klik op **Token ophalen**, of op **Token vernieuwen** als er al een token is. Bij een geslaagde aanmelding staat bij stap 1 "token tot" met een datum.

## Stap 2: een dossier koppelen

1. Klik op het icoon **Dossiers ophalen** naast het veld **Dossier**.
2. Kies het juiste dossier in de lijst.
3. Klik op **Koppelen**.

De status wordt "gekoppeld" en je ziet tot wanneer het dossiertoken geldig is.

## Stap 3: de import instellen

Vul het **Aankoopdagboek prefix** in, bijvoorbeeld A. Kies bij **Downloadlink-veld** het Octopus-veld dat de beveiligde downloadlink van de factuur ontvangt. Die lijst is leeg zolang je het dossier niet gesynchroniseerd hebt.

## Het dossier synchroniseren en relaties ophalen

Bovenaan staan twee knoppen, enkel actief bij een geldige koppeling.

- **Dossier synchroniseren**: haalt boekjaren, dagboeken, custom velden en btw-codes op.
- **Relaties ophalen**: toont voorstellen voor relaties tussen CPM en Octopus.

## De opgehaalde gegevens lezen

- **Boekjaren & dagboeken**: de boekjaren van het dossier. Oudere jaren klap je open met "+ oudere boekjaren".
- **Veldkoppeling**: welk Octopus-veld bij welk factuurveld hoort.
- **Verkoop-btw-codes**: enkel de **Vermelding op factuur** kun je hier aanpassen.

## Let op

Bewaar het bedrijf met **Bedrijf opslaan** om je aankoopdagboek-prefix en downloadlink-veld te bewaren.

Koppel je later een ander dossier, dan blijven de facturen die al naar Octopus gingen bij het oude dossier. Nieuwe facturen gaan naar het nieuwe dossier.
