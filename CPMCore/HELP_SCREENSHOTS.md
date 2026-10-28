# Gewenste schermafbeeldingen voor de nieuwe help-artikels (samengevoegd)

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
