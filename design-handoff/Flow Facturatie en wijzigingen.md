# Flow — Facturatie, offertes, wijzigingsopdrachten en nutsaansluitingen

Functionele beschrijving bij de schermen in *CRM Facturatie en wijzigingen*: **20a–20e** (desktop), **21a–21m** (modals en bijkomende schermen), **22a–22l** (tablet en gsm). Scope: één project (bv. Verkaveling Keerstraat).

---

## 0. Harde regel: de commissie is intern

De klant ziet **nooit** een kostprijs of een commissie. Dat geldt voor het portaal, de PDF van een WO en elke factuur. Hij ziet alleen de **prijs klant**, en daar zit de commissie al in. Kostprijs en commissie zijn alleen intern zichtbaar, gemarkeerd met "intern" (20d, 21c).

---

## 1. Plek in het menu

Groep **FINANCIEEL** in het dossiermenu:

| Item | Wat | Scherm |
|---|---|---|
| Facturatie | Werklijst van alles wat nu gefactureerd kan worden, per klantenaccount | 20a, 21a, 21j |
| Offertes & wijzigingen | Offertes van leveranciers en wijzigingsopdrachten (WO) voor klanten | 20b, 20c, 20d |
| Nutsaansluitingen | Kosten uit de nacalculatie verdelen over de eenheden, met voorschot en saldo | 20e, 21k, 21l |
| Betalingsschijven | Betalingsgroepen instellen: schijven, percentages, btw; schijf bereikt aanduiden | 21g, 21h |
| Betalingen | Ontvangen betalingen koppelen aan facturen | bestaand |

De badges tellen wat actie vraagt: het aantal factureerbare posten, en het aantal WO's die verzonden zijn maar nog niet ondertekend, samen met offertes die bijna verlopen.

---

## 2. Begrippen

- **Eenheid (lot)**: een verkocht deel van het project.
- **Klantenaccount**: de koper van één of meer eenheden. Facturatie gebeurt per account.
- **Facturatiewijze** (per account, 23a): *Gemeenschappelijk* = één factuur op naam van alle eigenaars (of één eigenaar / een bedrijf), één facturatieadres, meerdere e-mailontvangers; *Per eigenaar volgens aandeel* = één factuur per eigenaar. Vervangt het "hoofdaccount".
- **Eigenaars en verdeelsleutel**: een account heeft één of meer eigenaars, elk met een percentage. Samen is dat exact 100 % (21i). De sleutel geldt **globaal voor alles wat het account koopt**. Bij facturatiewijze *per eigenaar* krijgt elke eigenaar een eigen factuur; bij *gemeenschappelijk* één factuur voor het account.
- **Betalingsgroep**: hangt aan de eenheid en bepaalt twee dingen:
  - de schijven, samen 100 %;
  - de **btw voor de klant**. Die btw geldt ook voor meer- en minwerken en nutsaansluitingen van die eenheid.
- **Schijf**: een deel van de aannemingsprijs. Ze is factureerbaar zodra ze "bereikt" is. De laatste schijf is altijd de **eindafrekening**.
- **Offerte**: een prijs van een leverancier. Ze is intern en bindt niemand.
- **Wijzigingsopdracht (WO)**: het document voor de klant. Een meerwerk is positief, een minwerk negatief. Een WO wordt pas factureerbaar na ondertekening.
- **Facturatieplan**: per WO de verdeling van het bedrag in termijnen: voorschot, tussentijds en saldo.
- **Nutsaansluitingen**: kosten van nutsbedrijven. Ze komen binnen als leveranciersfactuur in de nacalculatie en worden verdeeld over de eenheden. Er kan een voorschot op staan; het saldo gaat mee met de laatste schijf.

---

## 3. Statussen

**Offerte**: Ingelezen → Omgezet (wordt een WO). Een offerte kan ook Verlopen of Geweigerd worden.

**Wijzigingsopdracht**: Opgemaakt → Verzonden → Ondertekend → Factureerbaar → Gefactureerd → Betaald. Een WO kan ook Geweigerd (door de klant) of Geannuleerd (door ons) worden.

| Status | Wat mag nog |
|---|---|
| Opgemaakt | alles bewerken |
| Verzonden | alleen bewerken via een nieuwe versie; de handtekening wordt opnieuw gevraagd |
| Ondertekend | bedragen liggen vast; een extra tussentijdse termijn toevoegen kan wel (21m) |
| Gefactureerd / Betaald | alleen lezen |

Een post is **Betaald** als alle facturen van alle eigenaars voor die post betaald zijn. Anders staat er "deels betaald 1/2" (21j).

---

## 4. Stap voor stap

### A. Offerte inlezen (20c, 22c, 22g)
1. Kies **Offerte inlezen**. Je kunt een PDF of bestand opladen, een foto nemen of een schermafdruk plakken.
2. Trek een kader rond een tabel: de regels worden ingelezen (omschrijving, eenheid, aantal, prijs leverancier).
3. Trek een kader rond een foto of schets: dat beeld hangt aan een regel en komt mee op de WO.
4. Onzekere waarden krijgen een gouden rand met de vraag "controleer". Een WO kan niet verzonden worden zolang die niet nagekeken zijn.
5. Het origineel gaat naar het Documentencentrum als *Offerte leverancier*, intern.

### B. Omzetten naar een WO (21c)
- Gaat mee: leverancier, account en eenheid, regels en beelden. De offerte blijft gekoppeld als bron.
- Wordt aangevuld:
  - **commissie**: standaard 20 %, aanpasbaar per WO en per regel, altijd intern;
  - **btw klant**: vergrendeld, volgt de betalingsgroep;
  - **facturatieplan** (zie 5) en de **omschrijving voor de klant**.
- **Minwerk wordt aan kostprijs verrekend**: negatieve regels, commissie vergrendeld op 0 %; de klant krijgt de kostprijs terug, geen commissie.

### C. Verzenden en ondertekenen (21d, 21e, 21f)
- Je kiest het kanaal: klantenportaal, e-mail met link, of alleen de PDF.
- Alle eigenaars van het account zijn ontvanger. Je stelt in of **alle eigenaars** moeten tekenen of dat **één volstaat**. De standaard komt van het account (21i).
- De PDF en het portaal tonen alleen prijzen incl. commissie. In het portaal ziet elke eigenaar ook **zijn eigen aandeel** (21f).
- De opvolging toont wanneer de WO verzonden, geopend en vervallen is. Na 7 dagen gaat er een herinnering uit.
- Een handtekening op papier registreer je door de getekende versie op te laden (21e).

### D. Wanneer is iets factureerbaar

| Post | Factureerbaar als |
|---|---|
| Schijf | de schijf is aangeduid als bereikt (21h) |
| Termijn van een WO | de trigger van die termijn is gebeurd (zie 5) |
| Saldo van een WO | de laatste schijf van het account is bereikt |
| Voorschot nutsaansluitingen | het moment uit 21l is bereikt |
| Saldo nutsaansluitingen | de laatste schijf is bereikt |

### E. Facturatie (20a, 21a)
- Je ziet één kaart per klantenaccount, met de eigenaars, de schijvenbalk en alle posten. Wat factureerbaar is, kun je aanvinken. Wat nog komt, staat in het grijs.
- **Facturen opmaken** maakt per account één factuur per eigenaar. Ze komen als concept in het factuurscherm terecht.

### F. De laatste schijf is de eindafrekening
- Alle ondertekende saldi van WO's en het saldo van de nutsaansluitingen staan **vergrendeld aangevinkt**. Je kunt ze niet uitvinken.
- Een WO die **verzonden maar niet ondertekend** is, **blokkeert** de eindfactuur. Je beslist eerst in 21b: herinneren, getekende versie opladen, weigering registreren of annuleren.
- Een WO die alleen **opgemaakt** is, geeft een waarschuwing maar blokkeert niet.

---

## 5. Facturatieplan van een WO (20d, 21c, 21m)

| Termijn | Wanneer factureerbaar |
|---|---|
| Voorschot | na ondertekening |
| Tussentijds (0 of meer) | na ondertekening, bij een schijf, of manueel vrijgeven |
| Saldo | **verplicht ten laatste bij de laatste schijf**; kan niet verwijderd worden |

- Snelkeuzes: *Alles bij laatste schijf* (standaard), *Voorschot + saldo*, *Volledig na ondertekening* (het saldo wordt 0 %) of *Eigen verdeling*.
- Een termijn is een percentage of een vast bedrag. Het saldo past zich automatisch aan.
- De voorwaardentekst op de WO volgt het plan. Na verzenden ligt het plan op het document vast.
- Een extra tussentijdse termijn (21m) mag daarna nog. Die verandert alleen hoe wij factureren, niet het totaal voor de klant.
- In Facturatie is elke termijn een aparte post, bv. "WO-003 · voorschot 30 %" en "WO-003 · saldo 70 %".

**Te factureren door bouwheer**: staat die schakelaar aan, dan factureert de bouwheer. De post blijft zichtbaar voor opvolging, maar je kunt hem niet selecteren. *(Aanname.)*

---

## 6. Nutsaansluitingen (20e, 21k, 21l, 22j–22l)

1. **Kost koppelen**: in de nacalculatie koppel je een deel van een leveranciersfactuur aan een nutspost. Dat bestaat al. Een kost zonder factuur voer je in als **raming**.
2. **Verdelen** (21k), per post. Methodes:
   - gelijk per eenheid
   - percentage per eenheid
   - vast bedrag per eenheid
   - volgens grondaandelen

   In elke methode kun je eenheden **uitsluiten** ("voor lot 4 niet"). De rest wordt dan herverdeeld. Het totaal moet 100 % zijn.
3. **Voorschot** (21l): een vast bedrag of een % van de raming per eenheid. Het wordt factureerbaar bij de verkoopakte, bij een schijf, of manueel. Een gefactureerd voorschot ligt vast.
4. **Saldo** = toegewezen kost − voorschot. Het gaat verplicht mee met de laatste schijf. Een voorschot mag hoger zijn dan de kost; het saldo is dan negatief en komt als creditpost op de eindafrekening. Het saldo is **nooit hoger dan de toegewezen kost**.
5. **Correcties**: vervangt een echte factuur de raming, of wijzig je een verdeling na de eindafrekening, dan komt het verschil als correctiepost in Facturatie.
6. Er komt geen commissie op nutsaansluitingen. De btw volgt de betalingsgroep. Bij meerdere eigenaars wordt verdeeld volgens de sleutel van het account.

---

## 7. Berekeningen

```
prijs klant       = prijs leverancier × (1 + commissie %)      (intern; minwerk: commissie = 0 %)
totaal regel      = aantal × prijs klant
totaal WO excl.   = Σ totaal regel
btw               = totaal excl. × btw-% van de betalingsgroep
termijn           = totaal excl. × termijn-%  (of vast bedrag)
saldo             = totaal excl. − Σ andere termijnen
nuts per eenheid  = Σ (kost post × aandeel eenheid)
nuts saldo        = nuts per eenheid − voorschot   (≤ nuts per eenheid, mag < 0)
```

**Verdeling over de eigenaars**: per factuurregel volgens de sleutel, afgerond op de cent. Het afrondingsverschil gaat naar de eerste eigenaar. De sleutel wordt op de factuur vastgelegd; een latere wijziging raakt bestaande facturen niet.

**Nacalculatie**: de commissie telt als marge. De kostprijs wordt gekoppeld aan de aankoopfactuur van de leverancier.

---

## 8. Validaties

| Situatie | Gedrag |
|---|---|
| Verdeelsleutel ≠ 100 % | facturen opmaken voor dat account is geblokkeerd |
| Facturatieplan ≠ 100 % | een WO kan niet verzonden worden |
| Regel met "controleer" | een WO kan niet verzonden worden |
| Nutsverdeling ≠ 100 % | opslaan van die verdeling gaat niet |
| Commissie op een minwerk | niet mogelijk, vergrendeld op 0 % |
| Verzonden, niet-ondertekende WO bij de laatste schijf | de eindfactuur is geblokkeerd tot er beslist is (21b) |
| Wijziging na verzenden | er komt een nieuwe versie en de handtekening wordt opnieuw gevraagd |

Een knop wordt nooit stil uitgeschakeld. Er staat altijd uitleg bij wat er moet gebeuren.

---

## 9. Koppelingen

- **Documentencentrum**: offertes (intern), elke WO-versie en getekende versies (zichtbaar voor de klant).
- **Klantenportaal**: WO's ondertekenen, met het eigen aandeel per eigenaar, en facturen per eigenaar bekijken.
- **Nacalculatie**: nutskosten koppelen, kostprijs van WO's tegenover de aankoopfacturen.
- **Eenheden en betalingsschijven**: schijven, btw en schijf bereikt.
- **Klanten**: eigenaars, verdeelsleutel en de standaard voor ondertekening.

---

## 10. Open vragen

1. Handtekening bij meerdere eigenaars: moeten standaard alle eigenaars tekenen, of volstaat één?
2. Te factureren door bouwheer: klopt de interpretatie in 5?
3. Eindafrekening: moet er een volledig overzicht van alle eerdere facturen op, of alleen de nieuwe posten?
