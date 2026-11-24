---
title: Een schijvensjabloon maken en toepassen
topic: instellingen
role: Projectleider · Beheerder
device: Desktop
minutes: 6
updated: 2026-11-24
newsTag: Nieuw
screens: instellingen-schijvensjablonen, instellingen-schijvensjablonen-bewerken, projecten-betalingsgroep-bewerken
related: betalingsgroep-instellen, planning-schijven-koppelen, planning-sjablonen, projectsjabloon-toepassen, projectsjablonen-beheren
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
  - q: Wat is het verschil met de oude kolommen Vast % en Gewicht?
    a: Je kiest nu per schijf één soort, **Gewicht** of **Vast %**, met één waarde. Zo staat er nooit een grijs veld dat niet meetelt.
  - q: Wat doet de code?
    a: De code verwijst naar een taak of fase in je planningssjabloon, bijvoorbeeld VLOERPLAAT. Zodra de planning er is, hangt de schijf er vanzelf aan. Je hoeft ze niet meer met de hand te koppelen.
  - q: Wat als de code in de planning niet voorkomt?
    a: Dan blijft de schijf los. Koppel ze met de hand in het taak- of fasepaneel van de planning.
  - q: Wat gebeurt er met groepen die het sjabloon al gebruikten als ik het wijzig of verwijder?
    a: Niets. Een betalingsgroep krijgt haar eigen schijven en blijft ze houden.
  - q: Waarom kan ik een sjabloon niet verwijderen?
    a: Een projectsjabloon gebruikt het (kolom **Gebruikt in**). Kies in dat projectsjabloon eerst een ander schijvensjabloon, of zet dit sjabloon op inactief.
---
## De lijst

Onder **Instellingen › Sjablonen › Schijven** staan alle schijvensjablonen. Bovenaan wissel je met de tabs naar de projectsjablonen, de planningssjablonen en de trajectsjablonen.

![De lijst met schijvensjablonen](/img/help/instellingen/schijvensjablonen-beheren/2-lijst.png)

Per sjabloon zie je het aantal schijven, welke schijven vast zijn, welke enkel bij een kelder of dak gelden, in welk projectsjabloon het zit, wie het laatst wijzigde en de status. Via **⋯** kan je het bewerken, dupliceren, inactief zetten of verwijderen. Verwijderen kan enkel als geen projectsjabloon het gebruikt.

## Een sjabloon maken

1. Kies **Nieuw sjabloon**.
2. Geef het sjabloon een naam, bijvoorbeeld "Woning met twee verdiepingen".
3. Voeg de schijven toe in de volgorde waarin je ze factureert. De laatste schijf is de eindafrekening.
4. Geef elke schijf een **omschrijving**. Dat is de tekst op de factuur.
5. Kies per schijf de **soort**: **Gewicht** of **Vast %**, en vul de waarde in.
6. Klik op **Sjabloon opslaan**.

![De editor van een schijvensjabloon met schijven, voorbeeld en controle](/img/help/instellingen/schijvensjablonen-beheren/1-editor.png)

Boven de schijven staat één regel die het uitrekent, bijvoorbeeld "vast 5 % · 95 % over gewicht 7 = 13,57 % per gewicht voor het gebouw rechts (2 bouwlagen, zonder kelder, geen dak gekozen)". Hij rekent met het voorbeeldgebouw rechts, dus een schijf die daar niet bij past telt niet mee. Staan er geen gewichten in, dan moeten de vaste percentages samen 100 % zijn.

Elke schijf heeft twee regels. Op de eerste regel staan de omschrijving, de soort, de waarde, **Per verd.**, **Enkel bij** en het voorbeeld. Op de tweede regel, naast **WACHT OP**, kies je de bouwstaat (zie "Een schijf aan de planning hangen"). Een grijze rij komt niet voor bij het voorbeeldgebouw rechts.

### Het voorbeeld rechts

Kies rechts het aantal bouwlagen, of er een kelder is, welk dak en een constructiewaarde. Je ziet meteen alle schijven met hun percentage en bedrag. Een schijf per verdieping staat er per verdieping uitgeschreven ("1ste verdieping", "2de verdieping"). Een schijf die niet bij dat gebouw past, staat er niet. In de tabel toont de kolom **Voorbeeld** hetzelfde per schijf. Die kolom zie je enkel op brede schermen.

### De volgorde en verwijderen

Sleep een schijf aan het handvat links om ze te verplaatsen, of kies **⋯ › Omhoog** of **Omlaag**. Verwijderen en dupliceren van een schijf zit ook onder **⋯**. Dupliceren is handig voor een variant: pas bij de kopie **Enkel bij** aan, bijvoorbeeld één schijf met kelder en één zonder.

## Schijven per verdieping

Zet **Per verd.** aan bij een schijf die voor elke verdieping terugkomt, bijvoorbeeld het gieten van de vloerplaat. Het percentage of het gewicht van die schijf wordt gelijk verdeeld over de verdiepingen.

Schrijf **{verdieping}** in de omschrijving om te bepalen waar "1ste verdieping" komt te staan. Zonder dat woord komt het achteraan.

## Schijven voor een kelder of een soort dak

Kies bij **Enkel bij** wanneer een schijf meetelt: altijd, met kelder, zonder kelder, hellend dak, plat dak of voor elke bouwlaag boven het gelijkvloers. Een keuze anders dan **Altijd** kleurt groen. Een schijf die niet bij het gebouw past, valt weg. De overige percentages worden dan opnieuw verdeeld, dus het totaal blijft 100 %.

CPM controleert bij het opslaan elke combinatie van kelder en dak. Komt er in één combinatie geen 100 % uit, dan krijg je een melding en wordt het sjabloon niet bewaard.

## Een schijf aan de planning hangen

Kies op de tweede regel van de schijf, naast **WACHT OP**, de bouwstaat waarop de schijf wacht, bijvoorbeeld "Ruwbouw bovenbouw · Opstort gegoten". Naast de bouwstaat kies je **gestart** of **voltooid**. Zolang er geen bouwstaat is, staat het veld goud en kan je dat niet kiezen. De schijf wordt betaalbaar zodra alle taken die die bouwstaat opleveren voltooid zijn, of gestart als je dat kiest.

In de planning hangt zo'n schijf vanzelf aan **alle** taken die die bouwstaat opleveren, zonder dat je ze met de hand koppelt. Bij een bouwstaat per bouwlaag (zoals de opstort) hoort schijf 1 bij de opstort van de eerste bouwlaag, schijf 2 bij de tweede, enzovoort. Koppel je een schijf zelf aan een taak, dan telt alleen die taak.

Een schijf zonder bouwstaat hangt niet vanzelf aan de planning. Het paneel **Controle** telt ze en noemt ze bij naam.

### Oude planningscode

Schijven die van vóór de bouwstaten zijn, kunnen nog een **oude code** hebben. Dat veld staat dan op de tweede regel, naast de bouwstaat, en wordt gecontroleerd tegen de planningssjablonen: gevonden, niet gevonden (met een voorstel) of niet leesbaar. Nieuwe schijven hebben dit veld niet. Kies bij een oude schijf liever een bouwstaat.

Rechts in het paneel **Controle** staan de aandachtspunten samen. Een rode of gouden code houdt het opslaan niet tegen, maar de schijf hangt dan niet aan de planning.

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
