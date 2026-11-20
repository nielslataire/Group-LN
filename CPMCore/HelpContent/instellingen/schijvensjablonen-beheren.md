---
title: Een schijvensjabloon maken en toepassen
topic: instellingen
role: Projectleider · Beheerder
device: Desktop
minutes: 6
updated: 2026-11-20
newsTag: Nieuw
screens: instellingen-schijvensjablonen, instellingen-schijvensjablonen-bewerken, projecten-betalingsgroep-bewerken
related: betalingsgroep-instellen, planning-schijven-koppelen, planning-sjablonen
synonyms: schijvensjabloon, sjabloon, betalingsschijven, percentage, gewicht, vast percentage, minimum, per verdieping, eindafrekening, voorlopige oplevering, code, standaardschijven
description: een vaste lijst betalingsschijven bewaren en met één handeling op een betalingsgroep toepassen
intro: |
  Bouw je telkens dezelfde soort woning, dan zijn de betalingsschijven ook telkens dezelfde. Bewaar ze als sjabloon en pas ze toe op een betalingsgroep.
  CPM rekent de percentages uit. Jij kijkt ze na, past aan wat nodig is en slaat de groep op.
before: |
  Voor het maken van een sjabloon heb je het recht Planningssjablonen nodig. Voor het toepassen heb je schrijfrecht op facturatie van projecten. Een groep waarvan al een schijf gefactureerd of bereikt is, kan geen sjabloon meer krijgen.
faq:
  - q: Wat is het verschil tussen een vast percentage en een gewicht?
    a: Een vast percentage gaat eerst van de 100 % af. Wat overblijft, verdeelt CPM over de schijven met een gewicht, naar verhouding. Een schijf met gewicht 4 krijgt dus viermaal zoveel als een schijf met gewicht 1.
  - q: Waarom is de laatste schijf vast 5 %?
    a: Dat is de gewoonte voor de eindafrekening bij de voorlopige oplevering. Je kan dit in het sjabloon wijzigen.
  - q: Wat doet het minimum per schijf?
    a: Komt een schijf na het uitrekenen onder dit percentage, dan krijg je een waarschuwing. Je kan toch doorgaan.
  - q: Wat doet de code?
    a: De code verwijst naar een taak of fase in je planningssjabloon, bijvoorbeeld VLOERPLAAT. Zodra de planning er is, hangt de schijf er vanzelf aan. Je hoeft ze niet meer met de hand te koppelen.
  - q: Wat als de code in de planning niet voorkomt?
    a: Dan blijft de schijf los. Koppel ze met de hand in het taak- of fasepaneel van de planning.
  - q: Wat gebeurt er met groepen die het sjabloon al gebruikten als ik het wijzig of verwijder?
    a: Niets. Een betalingsgroep krijgt haar eigen schijven en blijft ze houden.
---
## Een sjabloon maken

1. Open **Instellingen › Schijvensjablonen** en kies **Nieuw sjabloon**.
2. Geef het sjabloon een naam, bijvoorbeeld "Woning met twee verdiepingen".
3. Voeg de schijven toe in de volgorde waarin je ze factureert. De laatste schijf is de eindafrekening.
4. Geef elke schijf een **omschrijving**. Dat is de tekst op de factuur.
5. Kies per schijf: een **vast percentage**, of laat het leeg en geef een **gewicht**.
6. Klik op **Sjabloon opslaan**.

![De editor van een schijvensjabloon met schijven, vaste percentages en gewichten](/img/help/instellingen/schijvensjablonen-beheren/1-editor.png)

Boven de schijven zie je hoeveel er vast is en hoeveel naar gewicht verdeeld wordt. Staan er geen gewichten in, dan moeten de vaste percentages samen 100 % zijn.

## Schijven per verdieping

Vink **Per verdieping** aan bij een schijf die voor elke verdieping terugkomt, bijvoorbeeld het gieten van de vloerplaat. Het percentage of het gewicht van die schijf wordt gelijk verdeeld over de verdiepingen.

Schrijf **{verdieping}** in de omschrijving om te bepalen waar "1ste verdieping" komt te staan. Zonder dat woord komt het achteraan.

## Een schijf aan de planning hangen

Vul bij **Code in planning** de code in van de taak of fase uit je planningssjabloon. Wordt de planning uit dat sjabloon opgebouwd, of bewaar je de betalingsgroep, dan hangt CPM de schijf aan de juiste taak. Bij een schijf per verdieping hangt de eerste verdieping aan de eerste taak met die code, de tweede aan de tweede, enzovoort.

## Een sjabloon toepassen

1. Open **Projecten › Betalingsschijven** en kies **Groep bewerken** bij de groep.
2. Kies **Sjabloon toepassen** onder de schijven.
3. Kies het sjabloon. Heeft het schijven per verdieping, vul dan het aantal verdiepingen in.
4. Bekijk het voorstel met de percentages. Waarschuwingen staan eronder.
5. Klik op **Schijven overnemen**. De huidige schijven worden vervangen.
6. Pas in het formulier nog elk percentage of elke omschrijving aan en klik op **Betalingsgroep opslaan**.

Er is nog niets bewaard tot je opslaat. Sluit je de pagina, dan blijft de groep zoals ze was.

## Let op

- Een sjabloon vervangt alle schijven van de groep. Wil je er enkel schijven bij, gebruik dan **Schijven overnemen uit andere groep**.
- Is er al een schijf gefactureerd of bereikt, dan is de knop niet beschikbaar. De groep ligt dan vast.
