# Versie 2 afwerken — inventaris (stand 26/10/2026, 2e versie na feedback Niels)

Statisch uit de code (views, controllers, HelpContent, VOORTGANG-bestanden); niets in de browser nagelopen. "Verwijzing" = een `Url.Action`/`asp-action`/link in een V2-view of -script.

## 1. Buiten scope / lopend
- **Documentencentrum** (Index + Detail, geen V2): later.
- **Werfportaal** (aannemers, 5 pagina's): blijft voorlopig legacy.
- **Mijn taken**: Niels is bezig (migratie 120).
- **Snel punt aanmaken op een project** (`CreateQuick`): bestaat op het dashboard, bij een project voorlopig niet nodig; komt misschien later terug. `CreateQuick`/`CreateQuickSuccess` blijven staan.

## 2. Vervangen door V2 — legacy-view wordt door geen enkele V2-pagina meer bereikt (kandidaten om te verwijderen, nog NIET gedaan)
| Legacy-view | Reden |
|---|---|
| `Clients/AddContact`, `EditContact`, `ContactDetails` | enkel verwezen vanuit legacy `DetailContacts`; V2 = `ProjectenController.ContactsV2` |
| `Coordinatie/CoordinatieInstellingen` | enkel verwezen vanuit legacy `DetailCoordinatie` |
| `Invoicing/PaymentGroupLink`, `PaymentStagesAddUpdate` | enkel eigen view / legacy `PaymentStages`; V2 = `PaymentStagesV2`, `PaymentGroupEditV2` |
| `Media/DetailNews` | enkel legacy `DetailMenu`; **niet meer nodig** (Niels) |
| `Instellingen/AppRoles/Index|Create` | enkel legacy zijbalk; V2 = `UserAdmin/RollenV2` |
| `UserAdmin/Create|Edit` | geen enkele verwijzing; V2 = gebruiker uitnodigen in `UserAdmin/IndexV2` |
| `Facturatie/BankAccountCreate|Edit`, `SeriesCreate|Edit`, `Sequences` | alles zit in `IssuerCompaniesEditV2` (+ `_IceFacturatieV2`); de legacy-views zijn enkel nog GET-pagina's voor `?classic` |
| `Budget/KostprijsUpdatePreview`, `Facturatie/OctopusRelationSuggestions` | helemaal geen verwijzing |
| `Invoices/Editor/EditDraft` | geen verwijzing; V2 = `EditV2` |
| `Account/MijnHandtekening` | enkel de legacy `_Layout`; V2 = Mijn profiel (`MijnProfielV2`) |
| `ChangeOrders/AddChangeOrder`, `EditChangeOrder`, `DetailsChangeOrder` | de gebruikte pagina's zijn `ChangeOrdersV2` (Offerten en wijzigingen), `QuoteIntakeV2`, `ChangeOrderDetailV2`, `ChangeOrderSignV2`; de legacy-views zijn de classic-terugval (Niels, 26/10). Let op, één restverwijzing: de **dashboard-snelactiekaart** (`Shared/GlV2/_SnelactiesCard`, regel "Wijzigingsopdracht opmaken") linkt nog naar `AddChangeOrder` — omleggen naar `QuoteIntakeV2` of de regel schrappen vóór je de view verwijdert |
| E-mailtemplates | **was een vergissing in mijn eerste lijst**: `EmailTemplateBeheerController` rendert `RenderV2` (lijst + editor in `IndexV2`); `Bewerken.cshtml` is enkel de classic-terugval |
Klaar: ook de PDF/print-views (`ChangeOrderPDF`, `PrintRecalculation`) en de publieke ondertekenpagina's blijven bewust apart.

## 3. Nog te bouwen
| Scherm | Stand |
|---|---|
| **Nieuw facturatiebedrijf** (`IssuerCompaniesCreate`) | bevestigd (Niels): er moet nog een V2-pagina voor komen. Nu opent de knop in `IssuerCompaniesV2` een legacy-formulier in de V2-shell (actie `InstellingenController.IssuerCompaniesCreate` heeft geen V2-tak). Mogelijk hergebruik van `IssuerCompaniesEditV2`-onderdelen (`_IceBankV2`, `_IceFacturatieV2`) |

## 4. Help
114 artikels / 8 topics, 76 schermen gedekt, 61 `HelpScreenId`-waarden in code.
- Artikel-schermen zonder vaste koppeling in de code (17): `facturen-nieuw`, `facturen-concept-bewerken`, `verzekeringen-overzicht`, `verzekeringen-attesten` verdacht; `account-*` en `budget-stap-1…9` zijn dynamisch (OK).
- 80 V2-paginaviews zetten zelf geen `HelpScreenId` (kan in de controller): per pagina controleren — vooral Marktanalyse (3), Mijn taken (4 artikels bestaan, views zetten geen id), Invoices Index/Detail, Klanten/Leveranciers Create/Edit/Detail, Instellingen Index/IssuerCompanies.
- Uit HELPCENTRUM_VOORTGANG: `HelpFeedback`-SQL nog op live DB, `[VRAAG: …]`-markeringen, ontbrekende screenshots (Verkoop), schermuitleg-artikels nog niet per taak gesplitst, dashboardmelding "punten goed te keuren" bestaat niet.

## 5. Open uit de eigen VOORTGANG-bestanden
- Veel gebouwd maar niets in de browser getest en niets gecommit (ook deze sessie).
- Migraties vóór gebruik: o.a. 049/054, 074, 079, 100, 105/111, 110/112, 115, 119, 120, `HelpFeedback.sql`.
- Te beslissen: nacalculatie 42d, vrijgavebrief borg, klantportaal, itsme/juridisch, herinneringen ondertekenaars.
- Structuur: legacy/V2-dubbels opruimen (zie §2), grote controllers.
- Config: Zimmo 403 (Hetzner), Graph Mail.Send-beleid, `TriggerKeys:Signing`, user-secrets.

## 6. Niet vastgesteld
1. Of er via `?classic` of dynamische viewnamen nog legacy-views bereikt worden (alleen verwijzingen doorzocht).
2. Welke controllers een `HelpScreenId` zetten (alleen views doorzocht).
3. Of bestaande V2-pagina's volledig volgens het ontwerp zijn (staten, mobiel) — browser-ronde nodig.

## Voorstel volgorde
1. Browser-ronde op wat gebouwd maar ongetest is (+ migraties eerst).
2. §3: V2-pagina voor "Nieuw facturatiebedrijf" bouwen; de snelactie "Wijzigingsopdracht opmaken" op het dashboard omleggen.
3. §4 Help: koppeling per pagina controleren, ontbrekende artikels.
4. §2 opruimen (views en bijhorende actions verwijderen) nadat 1–2 klaar zijn.
