---
title: Bouwstaten beheren
topic: instellingen
role: Beheerder
device: Desktop
minutes: 3
updated: 2026-11-24
newsTag: Nieuw
screens: instellingen-bouwstaten
related: schijvensjablonen-beheren, planning-sjablonen, planning-schijven-koppelen
synonyms: bouwstaat, toestand, opstort gegoten, standaardlijst, bereik, per bouwlaag, gebouw, eenheid
description: de standaardlijst toestanden die een planning kan opleveren en waar schijven en traject naar verwijzen
intro: |
  Een bouwstaat is een toestand die je bereikt, zoals "opstort gegoten" of "pleisterwerk klaar". Taken in een planning leveren een bouwstaat op. Een betalingsschijf of trajectmijlpaal wacht erop.
  Zo hangen planning, schijven en traject aan dezelfde lijst, in plaats van aan losse codes.
before: |
  Je hebt het recht Planningssjablonen nodig. De lijst wordt bij de start gevuld door migratie 158; je beheert ze hier verder.
faq:
  - q: Wat betekent Bereik?
    a: "**Gebouw**: de bouwstaat geldt voor alle eenheden van het gebouw tegelijk, bijvoorbeeld de ruwbouw of het dak. **Eenheid**: ze geldt per woning of appartement, bijvoorbeeld pleisterwerk of de keuken."
  - q: Wat betekent Per bouwlaag?
    a: De bouwstaat komt voor elke bouwlaag terug (gelijkvloers, 1ste verdieping, …). Opstort en metselwerk zijn zo'n bouwstaten.
  - q: Waarom kan ik de code niet meer wijzigen?
    a: Taken, schijven en trajectmijlpalen verwijzen ernaar. De naam, groep, volgorde en het bereik pas je wel aan.
  - q: Waarom kan ik een bouwstaat niet verwijderen?
    a: Er verwijst nog iets naar (taken, fases of schijven). Zet de bouwstaat op inactief. Ze wordt dan niet meer aangeboden, maar blijft werken waar ze al gebruikt wordt.
---
## De lijst

Onder **Instellingen › Automatisatie › Bouwstaten** staat de standaardlijst, per groep: voorbereiding, ruwbouw onderbouw, ruwbouw bovenbouw, dak, gesloten gebouw, technieken, afwerking en oplevering.

![De lijst met bouwstaten per groep](/img/help/instellingen/bouwstaten-beheren/1-lijst.png)

Per bouwstaat zie je de code, het bereik, of ze per bouwlaag voorkomt, hoeveel keer ze gebruikt wordt en de status.

## Een bouwstaat toevoegen of aanpassen

1. Kies **Nieuwe bouwstaat**, of klik op een rij.
2. Geef een **naam** en kies een **groep** en het **bereik**.
3. Bij een nieuwe bouwstaat typ je ook een **code**: hoofdletters, cijfers en underscores, bijvoorbeeld OPSTORT_DAK. Daarna ligt de code vast.
4. Zet **Per bouwlaag** aan als de bouwstaat voor elke bouwlaag terugkomt.
5. Klik op **Opslaan**.

![De editor van een bouwstaat](/img/help/instellingen/bouwstaten-beheren/2-editor.png)

## Inactief zetten of verwijderen

Met het oogje achter een rij zet je een bouwstaat op inactief of weer actief. De prullenbak verschijnt enkel bij een bouwstaat waar niets naar verwijst.
