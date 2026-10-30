# Open vragen uit de help-artikels (bijgewerkt 28/10/2026)

Elke vraag staat als `[VRAAG: …]` in het artikel en mag niet als feit gelezen worden. Beantwoord ze hier of rechtstreeks in het artikel; daarna verhuist het antwoord naar de woordenlijst in `HELP_SCHRIJFGIDS.md` en verdwijnt de markering.

Legende: **[Niels]** = hangt af van jullie werkwijze, **[beslissing]** = de code wijkt af van wat de help zegt of belooft.

## projecten

**Een project aanmaken** (`project-aanmaken`)
- [beslissing] wat gebeurt er met facturen en documenten van een verwijderd project? Code: harde delete van enkel de projectrij, geen opruiming, geen vangnet. Is er een factuur, document of eenheid, dan blokkeert de database waarschijnlijk en krijgt de gebruiker een foutpagina.

**Bedrijven en werfmelding** (`project-bedrijven-werfmelding`)
- [Niels] wat is de bindingsdatum, en doet CPM nog iets met deze gegevens?
- [Niels] wie is de Bouwheer en de Projectontwikkelaar in jullie context, en is de projectontwikkelaar altijd hetzelfde bedrijf?

**Projectdetail** (`project-detail-overzicht`)
- [Niels] uitleg over nacalculatie in één zin, voor wie het begrip niet kent.

**Traject en eenheden** (`project-traject-eenheden`)
- [Niels] zijn eenheid en lot hetzelfde, en welk woord gebruiken we voor een woning of appartement?

**Projecten algemeen** (`projecten-algemeen`)
- [beslissing] wie geeft toegang tot een project? Code: Projecten/Index toont alle projecten aan iedereen met het recht. De tabel ProjectUserAccess bestaat maar wordt nergens afgedwongen en er is geen beheerscherm. De zin "je ziet enkel projecten waar je toegang toe hebt" klopt dus niet.

**Publiceren** (`project-publiceren`)
- [beslissing] kun je Gepubliceerd weer uitzetten, en verdwijnt het project dan van de website? Code: de schakelaar kan uit, maar de publieke site filtert de lijst en projectpagina niet op IsPublished (enkel Opgeleverd en Stopgezet vallen weg, plus blogartikels). Eerst testen en beslissen of de site moet filteren.
- wat ziet een bezoeker precies? Code: titel, ondertitel, intro, kerncijfers, foto's, verhaal/architectuur/comfort/tijdlijn uit de projectwebsite, eenheden met status Beschikbaar/Optie/Verkocht. Prijzen, afwerkingsprijzen en brochure enkel als "Verkoop zichtbaar" aanstaat. Nog niet in het artikel verwerkt: wacht op de beslissing hierboven.

## budget

**Stap Gevels** (`budget-stap-gevels`)
- [Niels] wat betekent de code in de kolom Eenheid, bijvoorbeeld W1-4 of W5? Is dat een groep eenheden? (Vrij tekstveld, niet gedocumenteerd.)

**Stap Sanitair** (`budget-stap-sanitair`)
- [Niels] wat is het verschil tussen een toilet in de badkamer en een afzonderlijk toilet, en wat telt als een douchekamer?
- welke activiteiten in stap 6 gebruiken deze aantallen? Code: ze zijn enkel beschikbaar als formuleparameters (Instellingen > Budgetformules). Welke formules ze gebruiken staat in de database, niet in de code.

**Stap Verkoop** (`budget-stap-verkoop`)
- hoe zet je de vraagprijzen zelf op de eenheden? Code: "Doorzetten" schrijft grond- en bouwwaarde naar de eenheid. Een aparte vraagprijs vond ik niet.

## punten-verslagen

**Puntenlijst** (`punten-lijst-overzicht`)
- krijgt de aannemer een melding als je in de lijst Goedkeuren kiest? Code: Goedkeuren en doorsturen op de detailpagina mailt de aannemer, de bulkactie in de lijst zet enkel de status. Te bevestigen met een test.
