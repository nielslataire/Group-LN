---
title: Een wijzigingsopdracht opmaken en laten ondertekenen
topic: facturatie-wijzigingen
role: Projectleider
device: Desktop · tablet
minutes: 5
updated: 2026-11-11
screens: wijzigingen-opmaken
synonyms: WO, meerwerk, minwerk, wijzigingsopdracht, itsme, meerdere leveranciers, leverancier toevoegen
newsTag: Bijgewerkt
description: offerte inlezen, meerdere leveranciers, regels, ondertekenen via het klantenportaal
related: punten-lijst-overzicht
intro: |
  Een wijzigingsopdracht legt meerwerk of minwerk vast voor één eenheid. Je maakt er een als een
  klant iets wil toevoegen of schrappen. Na verzending tekenen de eigenaars via het klantenportaal.
  Daarna komt de wijzigingsopdracht in het facturatieplan.
before: |
  De klant en de eenheid moeten al bestaan. De eigenaars hebben een e-mailadres nodig om te kunnen
  tekenen.
faq:
  - q: Wat als een eigenaar niet wil ondertekenen?
    a: De eigenaar kan weigeren en geeft daarbij een reden. De wijzigingsopdracht krijgt dan de status Geweigerd. Je maakt een nieuwe versie of annuleert ze.
  - q: Kan ik een wijzigingsopdracht maken zonder offerte van de leverancier?
    a: Ja. Kies Leeg beginnen en vul de regels zelf in.
  - q: Moet ik een contract kiezen?
    a: Nee. Een leverancier in een offerte of wijzigingsopdracht kan zonder contract bestaan. Het project toont een waarschuwing tot je het contract van die leverancier koppelt.
  - q: Kan ik meerdere leveranciers in één offerte zetten?
    a: Ja. Elke leverancier krijgt een eigen groep met regels. De klant krijgt één offerte en tekent één keer.
  - q: Ziet de klant de naam van de leverancier?
    a: Nee. De klant ziet de titel van elke groep, bijvoorbeeld Schilderwerken, en de prijs voor de klant. De leverancier, de kostprijs en de commissie blijven intern.
  - q: Een regel staat bij de verkeerde leverancier. Wat nu?
    a: Sleep de regel met de greep naar de groep van de juiste leverancier. Met de pijltjestoetsen kan dat ook.
  - q: Kan ik een leverancier verwijderen die al regels heeft?
    a: Ja. Je krijgt eerst een bevestiging. De regels van die leverancier verdwijnen dan uit de offerte.
  - q: Waarom ziet de klant de commissie niet?
    a: De klantversie toont enkel de prijs inclusief commissie, nooit beide bedragen apart.
---
## Een nieuwe wijzigingsopdracht starten

1. Open **Offertes & wijzigingen**.
2. Klik op **+ Nieuw**.
3. Kies hoe je start: **Offerte inlezen**, **Leeg beginnen** of **Kopie van een bestaande wijzigingsopdracht**.

Lees je een offerte in, dan herkent CPM de regels automatisch.

## Meerdere leveranciers in één offerte

De leverancier hoort niet meer bij de hele offerte, maar bij een groep regels. Een offerte met één leverancier heeft één groep. Bij meerdere leveranciers heeft elke leverancier een eigen groep.

In de kaart **Opdracht** staan de leveranciers enkel als samenvatting, met het aantal regels per leverancier. Kiezen doe je in de kaart **Regels per leverancier**.

Een leverancier toevoegen:

1. Klik onder de laatste groep op **Leverancier toevoegen**.
2. Zoek de leverancier. Eerst staan de leveranciers met een contract in dit project, daarna de andere.
3. Kies de leverancier. Er verschijnt meteen een nieuwe groep met één lege regel.

Staat de leverancier er niet bij, maak hem dan eerst aan bij Leveranciers en zoek hem daarna opnieuw. Een leverancier die al in de offerte staat, kan je niet nog eens toevoegen.

In de kop van elke groep stel je in:

- **Op de offerte**: de titel die de klant ziet als tussenkop, bijvoorbeeld Schilderwerken. CPM vult de activiteit van het contract in zodra je een contract kiest.
- **Contract**: het contract van deze leverancier in dit project. Heeft de leverancier precies één contract in dit project, dan koppelt CPM het automatisch. Anders staat er **Geen contract · koppelen** en toont de groep een waarschuwing.
- **Offerte leverancier**: de offerte van deze leverancier. Via **Offerte koppelen of inlezen** lees je ze in voor alleen deze groep. De andere groepen blijven ongemoeid.
- **Via ons gefactureerd**: aan als wij de leverancier factureren, uit als hij zelf factureert aan de klant. Dit hoort bij de leverancier en niet meer bij de hele offerte.

Factureren alle leveranciers zelf, dan is er niets door ons te factureren. De kaart **Facturatieplan** verdwijnt dan en je hoeft geen plan in te vullen. Zet je bij minstens één leverancier **Via ons gefactureerd** weer aan, dan komt het facturatieplan terug.

Onder elke groep staat het subtotaal. Onderaan de kaart zie je één totaal voor de klant. Intern staat daar de kostprijs per leverancier en de commissie samen.

Een regel naar een andere leverancier verplaatsen kan door de greep naar de andere groep te slepen.

Een groep zonder contract is geen reden om te blokkeren. Bij **Verzenden** krijg je een waarschuwing, maar je kan gewoon verder. Een leverancier zonder regels moet je eerst invullen of verwijderen.

Op de projectpagina staat een waarschuwing **Wijzigingsopdracht zonder contract** zolang minstens één leverancier nog geen contract heeft.

## Meerwerk en minwerk op een regel

Elke regel is meerwerk of minwerk. Voor de omschrijving staat een knop met **+** of **−**. Klik erop om te wisselen.

- **Meerwerk (+)**: werk dat erbij komt. De prijs voor de klant is de prijs van de leverancier plus de commissie.
- **Minwerk (−)**: werk dat wegvalt. Dat wordt aan kostprijs verrekend, dus de commissie staat vast op 0 %. De prijs voor de klant en het totaal staan met een min.

Je vult aantal en prijs altijd als positief getal in. Staat het totaal van de offerte onder nul, dan staat er **Terug te betalen aan de klant**.

Een bedrag dat in een oudere offerte met een min stond, zie je nu als minwerk.

## Schadevergoeding op een minwerk

Soms krijgt de klant bij een minwerk maar een deel van de waarde terug. Dan zet je een schadevergoeding op dat minwerk.

1. Zet de regel op **minwerk (−)**.
2. Klik onder de regel op **Schadevergoeding op dit minwerk**.
3. Vul het percentage in of kies een snelkeuze: **10 %**, **20 %** of **25 %**. CPM stelt het standaardpercentage voor.
4. Kies bij **Gaat naar** wie de vergoeding krijgt.

- **Bouwheer**: de leverancier crediteert de volle waarde. De vergoeding blijft bij de bouwheer.
- **Leverancier**: de leverancier crediteert enkel wat de klant terugkrijgt.

Het bedrag van de schadevergoeding en het resultaat van de bouwheer zie je onder de regels bij **Afrekening van het minwerk**. Dat is enkel intern.

De klant ziet de schadevergoeding niet. Op de offerte, de wijzigingsopdracht en in het klantenportaal staat één minwerkregel met het eindbedrag, bijvoorbeeld − € 900,00 in plaats van − € 1.200,00 en + € 300,00. De btw rekent op dat eindbedrag. Laat voor de zekerheid je boekhouder bevestigen dat dit klopt voor jullie contracten.

Het standaardpercentage stel je in bij **Instellingen**, onder **Facturatie** bij **Minwerk**.

## Wat de klant ziet

De klant krijgt één offerte met één handtekening. Bij meerdere leveranciers staat de titel van elke groep als tussenkop met een subtotaal eronder. De naam van de leverancier, de kostprijs en de commissie staan nooit op het document.

Na akkoord komt er één wijzigingsopdracht. Daarin blijven de groepen bewaard, zodat elke leverancier aan zijn eigen regels gekoppeld blijft.

## De regels en het facturatieplan invullen

Bij elke regel zie je de prijs van de leverancier, de commissie en de prijs voor de klant. Het
facturatieplan bepaalt wanneer de wijzigingsopdracht gefactureerd wordt. Standaard gebeurt dat bij
de volgende betalingsschijf.

De commissie vul je in met twee cijfers na de komma, bijvoorbeeld 7,50 %. Dat geldt voor de
standaardcommissie bovenaan en voor de commissie per regel. Is een bedragveld te smal voor het
bedrag, dan verdwijnt het euroteken zodat het bedrag volledig leesbaar blijft.

## Verzenden ter ondertekening

1. Controleer de klantversie.
2. Klik op **Verzenden**.

Elke eigenaar krijgt een e-mail met een link naar het klantenportaal en tekent met itsme.

Onderaan staat de actiebalk. Die blijft in beeld terwijl je scrolt. Heeft de klant geen e-mailadres, dan zie je daar de waarschuwing **Klant heeft geen e-mailadres**, want daar klik je op **Verzenden**.

## Werken op een kleiner scherm

De pagina past zich aan de breedte van je venster aan, zonder dat je zijwaarts moet scrollen. Dat werkt ook als je het venster kleiner maakt of inzoomt.

![De regels op tablet, met twee lijnen per regel](/img/help/facturatie-wijzigingen/wijzigingsopdracht-opmaken/regels-tablet.png)

- **Regels**: op een breed scherm staan de regels in een tabel. Wordt het venster smaller, dan krijgt elke regel twee lijnen: bovenaan de omschrijving en het totaal, eronder de getallen met hun naam. Op een heel smal scherm wordt elke regel een kaartje.
- **Zijkolom**: Ondertekening (bij een offerte Verzending), Bron en Historiek staan rechts naast het formulier. Op een kleiner scherm schuiven ze onder het formulier.
- **Stappenplan**: bovenaan staan alle zeven stappen. Op een smal scherm zie je enkel de huidige stap, de volgende stap en een balkje met zeven streepjes.
- **Velden**: op een kleiner scherm staan er twee velden per rij in plaats van vier. Op een heel smal scherm staan ze onder elkaar.

![De actiebalk met het menu Meer acties](/img/help/facturatie-wijzigingen/wijzigingsopdracht-opmaken/actiebalk-meer-acties.png)

De actiebalk blijft altijd op één lijn. Wat niet meer past, verdwijnt of verhuist:

- Op een laptop verdwijnt de uitlegzin onder de knoppen.
- Op een tablet staat **Omzetten naar wijzigingsopdracht** in het menu **Meer acties** (de drie puntjes).
- Op een tablet in staande stand verdwijnt ook **Annuleren**. Je ziet dan **Opslaan**, **Verzenden** en de drie puntjes.

**Verwijderen** staat altijd in het menu **Meer acties**. Je krijgt eerst een bevestigingsvenster.

Op een tablet is **Voorbeeld-PDF** bovenaan een knop met enkel een icoon, en verdwijnt het label **Meerwerk** naast de status.

## Wachten op de handtekeningen

De status toont hoeveel eigenaars al getekend hebben. Na de laatste handtekening voegt CPM het
ondertekeningsblad toe als pagina 2. De wijzigingsopdracht komt dan in het facturatieplan.

## Let op

Na de eerste handtekening kun je de wijzigingsopdracht niet meer aanpassen. Maak dan een nieuwe
versie. De handtekeningen die al gezet waren, vervallen.
