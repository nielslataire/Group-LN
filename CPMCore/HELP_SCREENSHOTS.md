# Gewenste schermafbeeldingen voor de nieuwe help-artikels (samengevoegd)

## SAMENVATTING — stand 06/11/2026 (gecontroleerd tegen `HelpContent/` en `wwwroot/img/help/`)

Bron: dit bestand, `HELPCENTRUM_VOORTGANG.md` (o.a. 27/10: 87 afbeeldingen gemaakt) en een controle van elke `![...]`-regel tegen het bestandssysteem. 172 artikels, 185 verwijzingen. De tabellen hieronder blijven het volledige archief; **wat nog moet staat in A tot D**. Alles wat in de tabellen niet in deze samenvatting staat, bestaat al en wordt gebruikt (de rijen "6-werfkaart", "6-units", "6-voorstel", "w4 verzekeringen" zijn achterhaald: bestanden zijn gemaakt, soms onder een ander stapnummer: `dashboard-lezen/4-werfkaart`, `marktproject-bekijken/5-units`, `project-factureren/5-voorstel`).

### A. Artikel verwijst al naar een afbeelding die niet bestaat: 4 van de 10 nog open (06/11/2026: 6 gemaakt)
**Gemaakt op 06/11/2026** (Playwright, eigen profiel, enkel gelezen, niets opgeslagen; namen, e-mails, projecten en onderwerpen vervangen door fictieve; elk beeld bekeken): `mijn-taken-inbox/2-mails`, `mijn-taken-vandaag/nieuwe-afspraak`, `mijn-taken-week/nieuwe-afspraak`, `mijn-taken-vandaag/koppelen`, `mijn-taken-week/koppelen` (zelfde popover als bij Vandaag: er staat in de testdata geen afspraak met projectlabel; in de week toont het blok dan dus geen label), `instellingen/outlook-sync-bekijken/1-lijst`.

**Nog open (4):**
| artikel | bestand | waarom nog niet |
|---|---|---|
| dashboard-boekhouding-lezen | `aan-de-slag/dashboard-boekhouding-lezen/1-kerncijfers.png`, `3-inkomende-facturen.png`, `5-klaar-om-te-factureren.png` | het dashboardtype staat per gebruiker in de database; het account van Niels is Projectleider. Omzetten naar Boekhouding verandert zijn gegevens: eerst akkoord vragen, daarna terugzetten |
| mijn-profiel-bijwerken | `aan-de-slag/mijn-profiel-bijwerken/4-outlook.png` | het artikel toont de knop "Koppeling aanzetten" (uit-toestand); de koppeling staat voor Niels aan. Uitzetten wist mogelijk zijn sync-staat |

### B. Niet gemaakt omdat de testdata de toestand niet had (4 + 1 aanvulling), `![...]`-regel staat nog niet in het artikel
| artikel | wat er nodig is |
|---|---|
| traject-synchroniseren-met-sjabloon | stap 2: een traject dat afwijkt van zijn sjabloon, dan de modal Sync met sjabloon |
| document-ondertekening | een contractdocument in de map Contracten, venster Ter ondertekening sturen |
| document-revisie-uploaden | een revisie ter goedkeuring, kaart Revisies |
| offerte-gunnen | een offerte met prijsvergelijking, sectie Prijsvergelijking |
| coordinatie-factureren | de filterrij "Factureren t/m" (enkel bij een project met schijven + regie); de bestaande `1-selectiebalk.png` toont ze niet |

### C. Artikels zonder één afbeelding die op de lijst ontbreken (nieuw sinds 27/10, rijen toegevoegd onderaan)
- Verkoop (45): `te-koop-overzicht`, `verkoopinstellingen-aanpassen`, `eenheid-berekenen`. Pagina's nooit in de browser gezien (zie VERKOOP_VOORTGANG.md): eerst testen, dan pas fotograferen.
- Nog te beslissen of ze een afbeelding nodig hebben (alle zonder `![...]`): contacten-CRM (`contacten-overzicht`, `contact-toevoegen`, `contact-opvolgen`, `contactenlijsten-en-export`), facturen (`factuur-bewerken`, `factuur-verzenden`, `factuur-nummeren-en-verzenden`, `terugkerende-facturen-beheren`/`-instellen`, `terugkerende-factuur-aanmaken`, `klant-aanmaken-vanuit-factuur`), `mijn-taken-overzicht`, `deadlines-overzicht`, `trajectsjablonen-overzicht`/`trajectsjabloon-bewerken`, `gebruikers-overzicht` en de overige beheerpagina's onder `instellingen/` (~35 artikels zonder afbeelding: bouwindexen, activiteiten, vacatures, blog, rollen, e-mailtemplates, enz.), `marktanalyse/` (3), `punten-verslagen/` (8, `punt-met-spraakbericht` en `punten-lijst-overzicht` hadden ooit afbeeldingen via een oude toestand).

### D. Verouderd of te vervangen
- `budget/budget-nieuw/1-nieuw-budget.png`, `2-nieuwe-versie.png` en `budget/budget-overzicht/1-overzicht.png`: tonen het oude scherm, worden door geen artikel meer gebruikt. Nieuwe opnames van de modal Nieuw budget, het overzicht en `budgetten-vergelijken` zijn nodig (zie HELPCENTRUM_VOORTGANG 14/10); daarna de oude bestanden verwijderen.
- `budget/budget-stap-verkoop/1-prijscodes.png`: toont `€ 2 500` met spatie, de view toont nu punten (`€ 2.500`).
- `budget-stap-resultaat` (PDF/Excel-sectie) en `budget-pdf-opmaken`: PDF-opmaakpagina is nieuw sinds de opnames, controleer of `1-pdf-menu` en `3-klaar` nog kloppen.
- Maak de lijst "Bestaande artikels waarvan de afbeelding ontbreekt" (verzekeringen) niet opnieuw af: die 9 bestanden bestaan.

### E. Open punten rond de opnames zelf
- De eigen facturerende bedrijven (BCO, Group LN, Home-Estate, Coördinatie) staan herkenbaar op de afbeeldingen van Instellingen › Bedrijven: Niels moet beslissen of dat mag.
- Voor elke nieuwe batch: Niels logt in (nieuw automatiseringsprofiel), cookie `gl_v2_preview=1`, migraties van de recente pagina's moeten gedraaid zijn (o.a. 115 Verkoop, 127 Dashboard Boekhouding, 133 terugkerende facturen), app herstart.
- Totaal nog te maken: **4 (A) + 5 (B) + 3 (C, Verkoop, minstens 3) + 3 (D, nieuwe budgetopnames)** ≈ 15 afbeeldingen, plus de nog te beslissen artikels uit C.

---

Volg het Playwright-recept in CPMCore/DESIGN.md ("Schermafbeeldingen maken"): eigen profiel, Niels logt in, cookie `gl_v2_preview=1`, uitsnijden tot de kaart/sectie, echte namen vervangen. Daarna de `![...]`-regel als eerste regel van de stap in het artikel zetten.

# Screenshots te maken (code w1)

| artikel-slug | stap | pad | route/pagina | uitsnede | anonimiseren |
|---|---|---|---|---|---|
| dashboard-lezen | 1 | CPMCore/wwwroot/img/help/aan-de-slag/dashboard-lezen/1-kerncijfers.png | / (Dashboard projectleider) | KPI-strip met 4 tellers | projectnamen niet zichtbaar, ok |
| dashboard-lezen | 3 | CPMCore/wwwroot/img/help/aan-de-slag/dashboard-lezen/3-snooze-menu.png | / | open Snooze-menu bij een melding | projectnamen in de meldingtekst |
| dashboard-lezen | 5 | CPMCore/wwwroot/img/help/aan-de-slag/dashboard-lezen/5-werfkaart.png | / | één werfkaart met Fysiek/Financieel | projectnaam, gemeente |
| dashboard-lezen | 7 | CPMCore/wwwroot/img/help/aan-de-slag/dashboard-lezen/7-snelacties.png | / (breed scherm) | kaart Snelacties | projectnaam bij Actief project |
| instellingen-vinden | 3 | CPMCore/wwwroot/img/help/instellingen/instellingen-vinden/3-zoeken.png | /Instellingen | zoekbalk met resultaat | geen |
| budget-pdf-opmaken | 1 | CPMCore/wwwroot/img/help/budget/budget-pdf-opmaken/1-pdf-menu.png | Budget stap 9 | menu BUDGET ALS PDF | projectnaam |
| budget-pdf-opmaken | 3 | CPMCore/wwwroot/img/help/budget/budget-pdf-opmaken/3-klaar.png | Projecten/BudgetPdfOpmaken | klaarkaart met Download PDF | projectnaam |
| factuur-bekijken | 1 | CPMCore/wwwroot/img/help/facturatie-wijzigingen/factuur-bekijken/1-balk.png | Invoices/Detail/{id} | titel met labels + balk Wij factureren/Aan/Vervalt/Nog te ontvangen | klantnaam, bedrijfsnaam |
| factuur-bekijken | 3 | CPMCore/wwwroot/img/help/facturatie-wijzigingen/factuur-bekijken/3-lijnen.png | Invoices/Detail/{id} | kaart Factuurlijnen met totalen | klant/omschrijvingen |
| project-factureren | 3 | CPMCore/wwwroot/img/help/facturatie-wijzigingen/project-factureren/3-account-kaart.png | Projecten/InvoicingV2 | één klantkaart met posten en balk Klaar om te factureren | klant- en eenheidsnamen |
| project-factureren | 6 | CPMCore/wwwroot/img/help/facturatie-wijzigingen/project-factureren/6-voorstel.png | Projecten/InvoicingV2 | venster Facturen opmaken | klantnamen |
| betalingsschijven-volgen | 2 | CPMCore/wwwroot/img/help/facturatie-wijzigingen/betalingsschijven-volgen/2-matrix.png | Projecten/PaymentStagesV2 | groepskaart met matrix en legende | eenheidsnamen |
| betalingsschijven-volgen | 3 | CPMCore/wwwroot/img/help/facturatie-wijzigingen/betalingsschijven-volgen/3-bereikt-venster.png | idem | venster Schijf bereikt aanduiden | eenheid · klantnamen |
| betalingsgroep-instellen | 3 | CPMCore/wwwroot/img/help/facturatie-wijzigingen/betalingsgroep-instellen/3-schijven.png | Projecten/PaymentGroupEditV2 | kaart Betalingsschijven met totaalbolletje | eenheidsnamen |
| betalingsgroep-instellen | 1 | CPMCore/wwwroot/img/help/facturatie-wijzigingen/betalingsgroep-instellen/1-nieuwe-groep.png | Projecten/PaymentStagesV2 | venster Nieuwe betalingsgroep | projectnamen in de lijst |
| offertes-en-wijzigingen-volgen | 1 | CPMCore/wwwroot/img/help/facturatie-wijzigingen/offertes-en-wijzigingen-volgen/1-kpis-lijst.png | Projecten/ChangeOrdersV2 | KPI-strip + tabel Wijzigingsopdrachten | klant-, leveranciers- en eenheidsnamen |
| offertes-en-wijzigingen-volgen | 5 | CPMCore/wwwroot/img/help/facturatie-wijzigingen/offertes-en-wijzigingen-volgen/5-nieuw-menu.png | idem | open menu Nieuw | geen |
| bedrijven-beheren | 1 | CPMCore/wwwroot/img/help/instellingen/bedrijven-beheren/1-lijst.png | Instellingen/IssuerCompanies | tabel met rijen | btw-nummers, IBAN |
| bedrijf-bewerken | 2 | CPMCore/wwwroot/img/help/instellingen/bedrijf-bewerken/2-identiteit.png | Instellingen/IssuerCompanies/Edit/{id} | tabbalk + kaart Identiteit | btw-/ondernemingsnummer |
| bankrekening-toevoegen | 2 | CPMCore/wwwroot/img/help/instellingen/bankrekening-toevoegen/2-nieuwe-rekening.png | tab Bankrekeningen | venster Nieuwe bankrekening | IBAN |
| nummerreeks-beheren | 1 | CPMCore/wwwroot/img/help/instellingen/nummerreeks-beheren/1-reeksen.png | tab Facturatie | kaart Nummerreeksen | geen |
| bedrijf-factuurstandaarden | 2 | CPMCore/wwwroot/img/help/instellingen/bedrijf-factuurstandaarden/2-betaaltermijn.png | tab Facturatie | venster Betaaltermijn | geen |
| octopus-koppelen | 1 | CPMCore/wwwroot/img/help/instellingen/octopus-koppelen/1-stappen.png | tab Boekhouding | kaart Octopus-koppeling met 3 stappen | gebruikersnaam, dossiernummer |
| factuurlayout-en-mail-instellen | 2 | CPMCore/wwwroot/img/help/instellingen/factuurlayout-en-mail-instellen/2-mail.png | tab Lay-out & e-mail | kaart E-mail bij factuur | geen |
| uurtarieven-instellen | 2 | CPMCore/wwwroot/img/help/instellingen/uurtarieven-instellen/2-tarief.png | tab Uurtarieven | venster Uurtarief | medewerkersnaam |
| marktproject-bekijken | 3 | CPMCore/wwwroot/img/help/marktanalyse/marktproject-bekijken/3-kerncijfers.png | Marktanalyse/Projectdetail/{id} | strip Kerncijfers | projectnaam is publiek, ok |
| marktproject-bekijken | 6 | CPMCore/wwwroot/img/help/marktanalyse/marktproject-bekijken/6-units.png | idem | kaart Units met uitgeklapte bron | geen |


# Screenshotlijst w2 (klanten en leveranciers, topic projecten)

Pad = `CPMCore/wwwroot/img/help/projecten/<slug>/<n>-<naam>.png`. Nog geen `![...]`-regels in de artikels. Stapnummer = volgorde van de `## `-stappen in het artikel.

| artikel-slug | stap | pad | route/pagina | uitsnede | anonimiseren |
|---|---|---|---|---|---|
| klanten-overzicht | 1 | CPMCore/wwwroot/img/help/projecten/klanten-overzicht/1-lijst.png | /Klanten | tabelkaart met kolomkoppen en 4 rijen | klantnamen, ondernemingsnummers, e-mails |
| klanten-overzicht | 3 | CPMCore/wwwroot/img/help/projecten/klanten-overzicht/3-filters.png | /Klanten, Filters open | zoekveld + filterpaneel Facturatiebedrijf | niets |
| klanten-overzicht | 6 | CPMCore/wwwroot/img/help/projecten/klanten-overzicht/6-verwijderen.png | /Klanten, ⋯ > Verwijderen | bevestigingsvenster Klant verwijderen? | klantnaam |
| klant-bekijken | 2 | CPMCore/wwwroot/img/help/projecten/klant-bekijken/2-kaarten.png | /Klanten/Details/{id} | tabbar + kaarten Identificatie en Adres | naam, adres, e-mail |
| klant-beheren | 2 | CPMCore/wwwroot/img/help/projecten/klant-beheren/2-identificatie.png | /Klanten/Create | kaart Identificatie met Controleren-knop | niets (leeg formulier) |
| klant-beheren | 7 | CPMCore/wwwroot/img/help/projecten/klant-beheren/7-contacten.png | /Klanten/Edit/{id}, tab Contacten | een contactkaart met ster en prullenbak | namen, e-mail, telefoon |
| project-klant-bewerken | 1 | CPMCore/wwwroot/img/help/projecten/project-klant-bewerken/1-tabbladen.png | /Klanten/EditProject?projectid=..&clientid=..&activetab=0 | tabbar met zeven tabbladen | niets |
| project-klant-bewerken | 5 | CPMCore/wwwroot/img/help/projecten/project-klant-bewerken/5-verdeelsleutel.png | idem, tab Mede-eigenaars | balk Aandeel eigenaar 1 + Verdeelsleutel + Gelijk verdelen | namen |
| project-klant-bewerken | 6 | CPMCore/wwwroot/img/help/projecten/project-klant-bewerken/6-facturatie.png | idem, tab Facturatie | kaart Facturatie met Verzenden naar | namen, e-mails |
| leveranciers-overzicht | 1 | CPMCore/wwwroot/img/help/projecten/leveranciers-overzicht/1-lijst.png | /Leveranciers | tabelkaart met INACTIEF-label | namen, nummers, e-mails |
| leveranciers-overzicht | 4 | CPMCore/wwwroot/img/help/projecten/leveranciers-overzicht/4-filters.png | /Leveranciers, Filters open | drie filtervelden + chips | niets |
| leverancier-bekijken | 1 | CPMCore/wwwroot/img/help/projecten/leverancier-bekijken/1-tabbladen.png | /Leveranciers/Details/{id} | titel met labels + tabbar met aantallen | naam |
| leverancier-beheren | 3 | CPMCore/wwwroot/img/help/projecten/leverancier-beheren/3-identificatie.png | /Leveranciers/Create | kaart Identificatie met Controleren | niets |
| leverancier-beheren | 7 | CPMCore/wwwroot/img/help/projecten/leverancier-beheren/7-afdeling.png | /Leveranciers/Edit/{id}, tab Afdelingen | een afdelingskaart | adres, telefoon |
| leverancier-portaal-uitnodigen | 1 | CPMCore/wwwroot/img/help/projecten/leverancier-portaal-uitnodigen/1-uitnodigen.png | /Leveranciers/Details/{id}, tab Contacten | contactkaart met de twee uitnodigknoppen | naam, e-mail |


# Screenshots nodig (w3)

Alle paden onder `CPMCore/wwwroot/img/help/projecten/<slug>/`. Nog geen `![...]`-regel in de artikels; voeg ze toe als eerste regel van de stap zodra het bestand bestaat.

| artikel-slug | stap | pad | route/pagina | uitsnede | anonimiseren |
|---|---|---|---|---|---|
| coordinatie-opvolgen | De kerncijfers lezen | .../coordinatie-opvolgen/2-kerncijfers.png | Projecten/DetailCoordinatie?projectid=X (type Schijven + Regie) | kaartenstrook + tabbladen | projectnaam |
| coordinatie-opvolgen | Het contract instellen | .../coordinatie-opvolgen/3-instellingen.png | idem, tandwiel geopend | zijpaneel Coördinatie-instellingen | namen medewerkers, bedrijfsnaam, adres |
| coordinatie-schijven-regie | Een schijf toevoegen | .../coordinatie-schijven-regie/1-schijven.png | idem | kaart Schijven met voortgangsbalk en tabel | namen |
| coordinatie-schijven-regie | Een prestatie invoeren | .../coordinatie-schijven-regie/2-prestatie-invoeren.png | idem | invoerrij van de kaart Regie | medewerkersnamen |
| coordinatie-factureren | De selectiebalk lezen | .../coordinatie-factureren/1-selectiebalk.png | idem, schijven aangevinkt en Factureren t/m | donkere balk onderaan + filterrij | namen |
| documenten-beheren | Een map of opvolglijst kiezen | .../documenten-beheren/1-mappen.png | Projecten/DetailDocs?projectid=X | mappenpaneel links | geen |
| documenten-beheren | De tabel lezen | .../documenten-beheren/2-tabel.png | idem | toolbar + paar rijen | klant-/leveranciersnamen in chips |
| documenten-beheren | Het detailpaneel openen | .../documenten-beheren/3-paneel.png | idem, document geopend | detailpaneel rechts | namen |
| document-uploaden | De gegevens invullen | .../document-uploaden/1-uploadvenster.png | idem, Uploaden | het uploadvenster | geen |
| document-revisie-uploaden | Een revisie goedkeuren | .../document-revisie-uploaden/1-revisies.png | idem, paneel met revisie ter goedkeuring | kaart Revisies | namen uploader |
| document-delen | Delen met het klantportaal | .../document-delen/1-gedeeld-met.png | idem, paneel | sectie Gedeeld met + Gekoppeld aan | klant- en leveranciersnamen |
| document-aanvragen | Een document aanvragen | .../document-aanvragen/1-aanvraagvenster.png | idem, Document aanvragen | het venster | geen |
| document-ondertekening | Ondertekenaars vastleggen | .../document-ondertekening/1-ondertekenaars.png | idem, map Contracten, venster Ter ondertekening sturen | het venster | namen |
| offerte-gunnen | Prijzen vergelijken | .../offerte-gunnen/1-prijsvergelijking.png | idem, map Offertes, paneel | sectie Prijsvergelijking | leveranciersnamen |
| fotos-beheren | Een sectie kiezen / Het hoofdbeeld | .../fotos-beheren/1-tabs-hoofdbeeld.png | Projecten/DetailPhotos?projectid=X | tabbladen + hoofdbeeldstrook | geen |
| fotos-beheren | Een beeld bekijken en beschrijven | .../fotos-beheren/2-detailpaneel.png | idem, beeld geopend | detailpaneel | geen |
| fotos-beheren | Meerdere beelden tegelijk | .../fotos-beheren/3-selectiebalk.png | idem, beelden aangevinkt | donkere balk | geen |
| foto-uploaden | Via het uploadvenster | .../foto-uploaden/1-uploadvenster.png | idem, Media uploaden | het venster | geen |
| eenheden-beheren | De lijst lezen | .../eenheden-beheren/1-lijst.png | Projecten/DetailUnits?projectid=X | kaartjes + groep Woningen met gekoppelde berging | kopernamen |
| eenheid-koppelen | Koppelen vanuit de lijst | .../eenheid-koppelen/1-koppeldialoog.png | idem, Eenheid koppelen | het dialoogvenster | geen |
| eenheid-bewerken | Het formulier openen | .../eenheid-bewerken/1-sectielijst.png | Projecten/EditUnit?projectid=X&unitid=Y | sectielijst links + eerste sectie | adres |
| eenheid-bewerken | 3. Bedragen | .../eenheid-bewerken/3-bedragen.png | idem | sectie Bedragen met kaart VERKOOPPRIJS | geen |
| slechtweerdagen-registreren | Een dag registreren | .../slechtweerdagen-registreren/1-kalender.png | Projecten/Weather | toolbar + twee maanden met regen/wind | geen |


# Schermafbeeldingen te maken (w4)

Pad-prefix: `CPMCore/wwwroot/img/help/<topic>/<slug>/<n>-<naam>.png`. Nooit een volle pagina, enkel het genoemde deel.

| artikel-slug | stap | pad | route / pagina | uitsnede | te anonimiseren |
|---|---|---|---|---|---|
| verslagen-overzicht | 1 | CPMCore/wwwroot/img/help/punten-verslagen/verslagen-overzicht/1-lijst.png | /Projecten/{id}/Verslagen | linkerkaart met segmenten en tabel | aanwezigen, projectnaam |
| verslagen-overzicht | 2 | CPMCore/wwwroot/img/help/punten-verslagen/verslagen-overzicht/2-samenvatting.png | idem | rechterkaart met samenvatting en knoppen | namen aanwezigen |
| traject-volgen | 1 | CPMCore/wwwroot/img/help/traject-dossiers/traject-volgen/1-kerncijfers.png | Projecten/Traject (ProjectTraject/Index) | tabbar met kerncijfers rechts | nee |
| traject-volgen | 2 | CPMCore/wwwroot/img/help/traject-dossiers/traject-volgen/2-tijdlijn.png | idem, tab Tijdlijn | stappenplan, melding en actieve fase | namen verantwoordelijken |
| mijlpaal-toevoegen-of-bewerken | 3 | CPMCore/wwwroot/img/help/traject-dossiers/mijlpaal-toevoegen-of-bewerken/3-formulier.png | Traject, knop Mijlpaal | volledige modal Mijlpaal toevoegen | nee |
| mijlpaal-als-bereikt-aanduiden | 3 | CPMCore/wwwroot/img/help/traject-dossiers/mijlpaal-als-bereikt-aanduiden/3-cel-popover.png | Traject, tab Per eenheid, klik op een cel | popover met Bereikt vandaag | eenheidsnamen |
| traject-synchroniseren-met-sjabloon | 2 | CPMCore/wwwroot/img/help/traject-dossiers/traject-synchroniseren-met-sjabloon/2-voorbeeld.png | Traject, knop Sync met sjabloon | de modal met de groepen | nee |
| mijlpalen-in-bulk-aanpassen | 1 | CPMCore/wwwroot/img/help/traject-dossiers/mijlpalen-in-bulk-aanpassen/1-selectiebalk.png | Traject, tab Mijlpalen met selectie | tabelrijen plus donkere selectiebalk | nee |
| traject-aanmaken-of-verwijderen | 1 | CPMCore/wwwroot/img/help/traject-dossiers/traject-aanmaken-of-verwijderen/1-nog-geen-traject.png | Traject van een project zonder traject | kaart Nog geen traject | nee |
| dossiers-overzicht | 2 | CPMCore/wwwroot/img/help/traject-dossiers/dossiers-overzicht/2-matrix.png | Projecten/Dossiers, tab Nutsaanvragen | kaart Per eenheid | eenheidsnamen, netbeheerder |
| dossiers-overzicht | 4 | CPMCore/wwwroot/img/help/traject-dossiers/dossiers-overzicht/4-nieuw-menu.png | idem, menu Nieuw | het uitgeklapte menu | nee |
| nutsaansluitingen-bulk-aanvragen | 2 | CPMCore/wwwroot/img/help/traject-dossiers/nutsaansluitingen-bulk-aanvragen/2-eenheden.png | Dossiers, Nieuw, Bulk per eenheid | sectie Type en eenheden | eenheidsnamen |
| ander-dossier-toevoegen | 2 | CPMCore/wwwroot/img/help/traject-dossiers/ander-dossier-toevoegen/2-tegels.png | Dossiers, Nieuw, Ander dossier | de typetegels | nee |
| dossier-opvolgen | 1 | CPMCore/wwwroot/img/help/traject-dossiers/dossier-opvolgen/1-stappenbalk.png | Projecten/Dossiers/Details/{id} van een nutsaansluiting | stappenbalk met knop volgende stap | klantnaam in Kosten |
| dossier-opvolgen | 4 | CPMCore/wwwroot/img/help/traject-dossiers/dossier-opvolgen/4-checklist.png | idem | kaart Keuring & overdracht | nee |
| dossier-opvolgen | 7 | CPMCore/wwwroot/img/help/traject-dossiers/dossier-opvolgen/7-gekoppeld.png | idem | kaart Gekoppeld | nee |
| nutsaansluiting-aanvragen | 2 | CPMCore/wwwroot/img/help/traject-dossiers/nutsaansluiting-aanvragen/2-werkstroom.png | Dossiers/Nuts/Nieuw of Bewerken | kaart Werkstroom plus de statuskaart rechts | nee |


## Bestaande artikels waarvan de afbeelding ontbreekt (verwezen in de markdown, bestand bestaat niet)
| artikel | bestand |
|---|---|
| attesten-aannemers-opvragen | `wwwroot/img/help/projecten/attesten-aannemers-opvragen/1-tabel.png`, `2-selectie.png`, `3-vastleggen.png` |
| verzekering-toevoegen | `.../verzekering-toevoegen/1-type.png`, `2-looptijd.png` |
| verzekeringen-overzicht | `.../verzekeringen-overzicht/1-totalen.png`, `2-aandachtspunten.png`, `3-polissen.png`, `4-tijdlijn.png` |

# Screenshots te maken (Outlook-sync, ticket #6)

| artikel-slug | stap | pad | route/pagina | uitsnede | anonimiseren |
|---|---|---|---|---|---|
| mijn-profiel-bijwerken | 4 | CPMCore/wwwroot/img/help/aan-de-slag/mijn-profiel-bijwerken/4-outlook.png | /Account/MijnProfiel (tabblad Profiel) | kaart Outlook, aan en uit | geen |
| mijn-taken-inbox | 2 | CPMCore/wwwroot/img/help/aan-de-slag/mijn-taken-inbox/2-mails.png | /MijnTaken/Inbox | blok Mails met enkele mails en een verzonden sectie | afzenders, onderwerpen, klant- en projectnamen |
| outlook-sync-bekijken | 1 | CPMCore/wwwroot/img/help/instellingen/outlook-sync-bekijken/1-lijst.png | /UserAdmin/OutlookSync | lijst met enkele gebruikers | namen en e-mailadressen |
| mijn-taken-vandaag | 4 | CPMCore/wwwroot/img/help/aan-de-slag/mijn-taken-vandaag/koppelen.png | /MijnTaken (Vandaag) | popover van een afspraak met de knop Koppel aan een project | projectnamen, titel van de afspraak |
| mijn-taken-week | 4 | CPMCore/wwwroot/img/help/aan-de-slag/mijn-taken-week/koppelen.png | /MijnTaken/Week | afspraak met projectlabel in het blok | projectnamen, titel van de afspraak |

# Dashboard Boekhouding (punt 1a)

| artikel-slug | stap | pad | route/pagina | uitsnede | anonimiseren |
|---|---|---|---|---|---|
| dashboard-boekhouding-lezen | 1 | CPMCore/wwwroot/img/help/aan-de-slag/dashboard-boekhouding-lezen/1-kerncijfers.png | / (dashboardtype Boekhouding) | KPI-strip met 4 kaarten | bedragen mogen blijven |
| dashboard-boekhouding-lezen | 3 | CPMCore/wwwroot/img/help/aan-de-slag/dashboard-boekhouding-lezen/3-inkomende-facturen.png | / | kaart Inkomende facturen, tabblad Te goedkeuren | leveranciers- en projectnamen |
| dashboard-boekhouding-lezen | 5 | CPMCore/wwwroot/img/help/aan-de-slag/dashboard-boekhouding-lezen/5-klaar-om-te-factureren.png | / | kaart Klaar om te factureren met proforma's en schijven | klantnamen, projectnamen |
| mijn-taken-vandaag | 5 | CPMCore/wwwroot/img/help/aan-de-slag/mijn-taken-vandaag/nieuwe-afspraak.png | /MijnTaken (Vandaag) | modal Nieuwe afspraak, met een soort gekozen, een project, uren, een plaats en een paar gekozen deelnemers (chips) | projectnamen, namen en e-mailadressen |
| mijn-taken-week | 5 | CPMCore/wwwroot/img/help/aan-de-slag/mijn-taken-week/nieuwe-afspraak.png | /MijnTaken/Week | modal Nieuwe afspraak, met een soort, project, uren en deelnemers | projectnamen |

# Verkoop (punt 45) — nog nooit gefotografeerd

Eerst in de browser testen (zie VERKOOP_VOORTGANG.md), migratie 115 vóór gebruik. Pad-prefix `CPMCore/wwwroot/img/help/projecten/<slug>/`. Voeg de `![...]`-regel daarna toe als eerste regel van de stap.

| artikel-slug | stap | pad | route/pagina | uitsnede | anonimiseren |
|---|---|---|---|---|---|
| te-koop-overzicht | De lijst lezen | CPMCore/wwwroot/img/help/projecten/te-koop-overzicht/1-lijst.png | Projecten/DetailSales?projectid=X | tabs, zoekveld en tabel met statussen | koper-, optant- en makelaarsnamen |
| te-koop-overzicht | Meerdere eenheden tegelijk aanpassen | CPMCore/wwwroot/img/help/projecten/te-koop-overzicht/2-selectiebalk.png | idem, eenheden aangevinkt | donkere selectiebalk | niets |
| verkoopinstellingen-aanpassen | Financieel / Kosten koper | CPMCore/wwwroot/img/help/projecten/verkoopinstellingen-aanpassen/1-financieel.png | Projecten/SalesSettings?projectid=X | kaarten Financieel en Kosten koper met voorbeeld | niets |
| verkoopinstellingen-aanpassen | Website | CPMCore/wwwroot/img/help/projecten/verkoopinstellingen-aanpassen/2-website.png | idem | kaart Website (titel en tekst) | niets |
| eenheid-berekenen | Een eenheid kiezen / Het resultaat lezen | CPMCore/wwwroot/img/help/projecten/eenheid-berekenen/1-resultaat.png | Projecten/SalesCalculate?projectid=X&unitid=Y | grond en bouw, kosten koper en vergelijking met budget | adres, kopernaam |
