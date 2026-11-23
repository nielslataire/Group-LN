# Planning, schijven en traject: herzien ontwerp (23/11/2026)

Dit ontwerp is opnieuw van nul bekeken door een projectengineer en een projectleider van bouwprojecten, en afgestemd met Niels (vragenrondes van 23/11). Het vervangt de "volgende stappen" in `PLANNING_STAND_23112026.md`. Wat al gebouwd is, blijft: dit ontwerp zegt wat daarvan de basis wordt en wat ontbreekt.

## 1. Besluiten van Niels (vastgelegd)

| Onderwerp | Besluit |
|---|---|
| Ruggengraat | **Bouwstaat per eenheid**: planning, schijven en traject verwijzen naar dezelfde lijst bouwstaten. De planning *levert* een bouwstaat (taak klaar), maar is niet de bron. |
| Lijst bouwstaten | **Eén bedrijfsbrede standaardlijst met vaste codes**, uitbreidbaar per project. |
| Planningseenheid | Zoals nu: een gebouw heeft meestal één planning; woningen kunnen dezelfde planning hebben, met of zonder estafette. |
| Bereik van een bouwstaat | **Appartementsgebouw:** een schijf (bv. "1ste verdieping", ruwbouw, buitenschrijnwerk, dak) geldt voor **alle appartementen** tegelijk. **Enkel de afwerking is per eenheid.** **Woningen:** alle schijven per woning. Er is dus **geen** bouwlaag-per-eenheid nodig. |
| Afwerking in een gebouw | **Eén fase Afwerking in de gebouwplanning**; de schijven van de afwerking worden **per eenheid handmatig bevestigd** door de projectleider. |
| Schijf bereikt | De **planning stelt voor, de projectleider bevestigt** (per eenheid of voor alle eenheden van de groep, met foto of nota). Daarvoor een lijst **"Te bevestigen schijven"** op het project en op het dashboard. |
| Na bevestiging | De schijf wordt **factureerbaar** en komt in een **lijst voor de boekhouding**. Geen automatische facturen. |
| Levensfases | **Poorten:** vergunning verkregen, start verkoop, start werken, oplevering en waarborg. Elke poort is een **checklist met waarschuwingen**; de projectleider beslist en kan met een reden doorgaan. |
| Termijn | Planning rekent vooruit; de **marge** tegenover de contractuele termijn (werkdagen, met verlof en slecht weer) wordt getoond en bewaakt. Waarschuwing onder een drempel, **instelbaar per project, standaard 10 %** van de termijn; rood bij negatieve marge; de kritieke weg wordt getoond. |
| Projectsjabloon | Een **lijst onderdelen** (bv. Gebouw A, Woningen, Parking), elk met eigen planning- en schijvensjabloon. Het traject geldt voor het hele project. |
| Aannemers | **Discipline per taak** in het sjabloon; een aannemer met een contract voor die discipline wordt uitvoerder van alle taken van die discipline in alle plannings (aanpasbaar per taak). Aannemers worden nooit bij de start gekoppeld. |
| AI | Alle vier: projectvoorstel uit een beschrijving, concept-sjabloon als niets past, controle van een planning als projectengineer, samenvatting en risico's voor de projectleider. **Nooit** namen, persoonlijke gegevens, bedragen of adressen naar de AI. |

## 2. Het model in één beeld

```
Project
 ├─ Levensfases / poorten: vergunning · start verkoop · start werken · oplevering en waarborg   (checklist per poort)
 ├─ Traject: administratieve mijlpalen en deadlines van het hele project, gekoppeld aan poorten of bouwstaten
 └─ Onderdelen  (Gebouw A · Woningen · Parking …)       <- projectsjabloon = lijst onderdelen
      ├─ Eenheden + betalingsgroep(en)
      ├─ Planning (gebouw: één planning; woningen: per woning of gedeeld, met/zonder estafette)
      │     taak/fase  ──levert──►  BOUWSTAAT  (code uit de standaardlijst)      + discipline per taak
      └─ Schijvensjabloon:  schijf  ──verwijst naar──►  BOUWSTAAT
                 bereik: "gebouw" (alle eenheden) of "eenheid" (afwerking, of alles bij woningen)
                 planning stelt voor ─► projectleider bevestigt ─► factureerbaar ─► boekhoudlijst
```

Bouwstaat-bereik per onderdeel:
- **Gebouw:** bouwstaat uit de gebouwplanning = voor alle eenheden van dat gebouw; bouwstaten van de afwerking = per eenheid (handmatig bevestigen).
- **Woning:** de planning hoort bij één woning, dus alles per woning.

## 3. Wat er al is en wat ervan de basis wordt

| Bestaand | Rol in het nieuwe model |
|---|---|
| Plannings per gebouw of woning (`BouwPlanning`, `BouwPlanningEenheid`), estafette, voorgangers over plannings | Blijft. Dit klopt volgens Niels. |
| Sjablonen met blokken, taken, voorwaarden, bouwlagen, deelfases (herhaling 2) | Blijft; de **vrije code** op taak en fase wordt een **bouwstaat uit de standaardlijst** (plus discipline). |
| Schijvensjablonen met `PlanningCode`, stappen van 5 %, `laag:boven` | Blijft; `PlanningCode` wordt een verwijzing naar een bouwstaat. De regel `laag:boven` en het volgnummer `kelder ? i : i-1` verdwijnen dan, want een schijf per bouwlaag verwijst gewoon naar de bouwstaat "ruwbouw 1ste verdieping". |
| Koppeling schijf ↔ taak (`PlanningSchijfKoppeling`, automatisch op code, handmatig in het paneel) | Wordt koppeling via bouwstaat; het handmatige koppelen blijft als uitzondering. |
| `UnitPaymentStageReached` (schijf bereikt per eenheid) | Wordt het bevestigingsspoor (wie, wanneer, nota of foto). |
| Trajectmijlpalen gekoppeld op code aan taken/fases | Verwijzen ook naar bouwstaten of poorten. |
| Projectsjabloon (één planning-, traject-, schijvensjabloon + antwoorden) | Wordt een lijst onderdelen. |
| Uitvoerder per fase/taak, contract per bedrijf in het project | Aangevuld met discipline op taak én contract. |
| AI-voorstel voor een sjabloonkeuze (privacyveilig) | Uitgebreid volgens hoofdstuk 5. |

**Nog niet onderzocht (eerst nakijken vóór het bouwen):** hoe `Contract` en betalingsgroep nu gekoppeld zijn (kan een contract een discipline krijgen?), hoe "factureerbaar" (`Invoicable`) nu gezet wordt, en welke dashboardplek voor "Te bevestigen schijven" past in de v2-layout.

## 4. Nieuwe onderdelen (wat er ontbreekt)

1. **Catalogus van bouwstaten** (bedrijfsbreed, vaste codes, volgorde, standaardomschrijving, bereik gebouw/eenheid, uitbreidbaar per project) en **disciplines** (ruwbouw, dak, gevel, schrijnwerk, elektriciteit, sanitair/verwarming, pleister, chape, vloeren, schilder, keuken, ...).
2. **"Te bevestigen schijven"**: lijst per project en op het dashboard, per eenheid, met bevestigen (met nota of foto), ongedaan maken zolang er niet gefactureerd is, en de boekhoudlijst "te factureren".
3. **Onderdelen in het projectsjabloon**.
4. **Poorten met checklist** (vergunning, start verkoop, start werken, oplevering en waarborg): wat ontbreekt, wat optioneel is, doorgaan met reden, wie en wanneer.
5. **Marge en kritieke weg**: marge tegenover de contractuele termijn, drempel per project (standaard 10 %), melding naar de projectleider, de taken van de kritieke weg gemarkeerd.
6. **Aannemer via discipline**: discipline op sjabloontaak en op contract; "koppel aannemer aan discipline" past alle taken aan.
7. **AI-uitbreiding**: zie hoofdstuk 5.

## 5. AI in het nieuwe model (zonder persoonlijke gegevens, bedragen of adressen)

1. **Projectvoorstel uit een beschrijving:** onderdelen, sjabloonkeuze per onderdeel, bouwlagen, kelder, dak, terrassen, en de gevolgen (termijn, marge, schijven), per levensfase.
2. **Concept-sjabloon als niets past:** inactief concept met standaard bouwstaten en disciplines, dat jij nakijkt en bewaart.
3. **Controle als projectengineer:** ontbrekende stappen, onrealistische duren, ontbrekende voorgangers of disciplinevolgorde, ontbrekende bouwstaten voor schijven.
4. **Samenvatting en risico's:** wat loopt achter, welke schijven staan te bevestigen, welke poorten ontbreken, in gewone taal.
Telkens: de AI krijgt enkel anonieme structuur (sjablooncodes, aantallen, bouwstaten), het resultaat gaat door een zuivere controle (`ProjectSjabloonAiVerwerker`-stijl) en niets wordt automatisch bewaard.

## 6. Voorgestelde volgorde

| Stap | Inhoud | Waarom nu |
|---|---|---|
| **A** | Catalogus van bouwstaten en disciplines; sjablooneditor kiest bouwstaat en discipline in plaats van een vrije code; bestaande codes worden omgezet (migratie met mapping). | Alles hangt eraan. Het maakt schijven, traject en AI robuust en vervangt de volgnummer-hack bij `laag:boven`. |
| **B** | Schijf bevestigen: lijst "Te bevestigen schijven" (project + dashboard), bevestigen met nota of foto, boekhoudlijst "te factureren", bereik gebouw/eenheid. | Dit is de dagelijkse handeling van de projectleider en de overgang naar facturatie. |
| **C** | Projectsjabloon met onderdelen (gebouw, woningen, parking), elk met eigen planning- en schijvensjabloon. | Gemengde projecten kloppen dan in één keer. |
| **D** | Poorten met checklist (4 poorten). | Geeft de levensfases en bepaalt wat de AI bij elke fase voorstelt. |
| **E** | Marge en kritieke weg met drempel per project. | Termijnbewaking tijdens de uitvoering. |
| **F** | Aannemer via discipline en contract. | Maakt het koppelen van aannemers snel en volledig. |
| **G** | AI-uitbreiding (voorstel per fase met onderdelen, concept-sjabloon, controle, samenvatting). | Werkt het best bovenop A tot F en kan dan met vaste codes en poorten werken. |
| Tussendoor | Echte AI-aanroep testen met de huidige versie; gevelmetselwerk-voorganger beslissen; dak als v2-keuzelijst; help en schermafbeeldingen bijwerken. | Kleine open punten uit de vorige stand. |

## 7. Nog open vragen aan Niels (volgende ronde)
- **Standaardlijst bouwstaten:** de eerste lijst en volgorde (voorstel volgt, jij corrigeert), en welke bouwstaten per eenheid gelden bij een gebouw (afwerking: pleister, chape, vloeren, keuken, binnenschrijnwerk, ...).
- **Disciplines:** de eerste lijst, en of één taak meerdere disciplines kan hebben.
- **Schijven in een gebouw voor de afwerking:** per eenheid andere percentages of dezelfde voor alle appartementen?
- **Foto of nota bij bevestigen:** verplicht, optioneel of bij bepaalde bouwstaten?
- **Ongedaan maken** van een bevestigde schijf: tot wanneer en door wie?
- **Dashboard:** welke rollen zien "Te bevestigen schijven" en de boekhoudlijst?
- **Traject:** blijft het project-traject een aparte lijst mijlpalen, of worden de poorten het traject?
