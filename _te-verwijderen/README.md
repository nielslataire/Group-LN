# _te-verwijderen

Opruimdossier voor CPMCore-views die niet meer nodig zijn (analyse van 2026-10-23, branch `layout-experiment`).
Deze map staat bewust **buiten** het `CPMCore`-project, anders compileert Razor de views nog mee.
Onder `CPMCore/` is de oorspronkelijke padstructuur gespiegeld. Verplaatst met `git mv`: de geschiedenis blijft
bewaard (`git log --follow <pad>`).

Dit document bevat: (1) wat effectief verplaatst/verwijderd is en hoe je het terugzet, (2) de volledige analyse van
alle pagina's zonder V2-versie, zodat je later bij het afwerken van de V2-layout kunt beslissen wat er nog moet.

---

## 1. Effectief verplaatst of verwijderd

| Oorspronkelijke locatie | Nu | Reden |
|---|---|---|
| `CPMCore/Views/Klanten/Core/DetailCO.cshtml` | `_te-verwijderen/CPMCore/Views/Klanten/Core/DetailCO.cshtml` | De actie `KlantenController.DetailCO` doet enkel een redirect naar `Projecten/DetailsChangeOrder`; de view wordt nooit gerenderd. (De actie zelf staat nog in `KlantenController.Core.cs`, ~r.1511.) |
| `CPMCore/Views/Projecten/AddClient.cshtml` | `_te-verwijderen/CPMCore/Views/Projecten/AddClient.cshtml` | Geen controller-actie `AddClient`, geen enkele link ernaar; het formulier post naar een niet-bestaande actie. |
| `CPMCore/Views/Projecten/ChangeOrders/MinimalTestPDF.cshtml` | `_te-verwijderen/CPMCore/Views/Projecten/ChangeOrders/MinimalTestPDF.cshtml` | Test-PDF ("Hallo Rotativa PDF!"). Ook de bijhorende **actie is verwijderd** (zie hieronder). |
| `CPMCore/Views/Projecten/Partials/Photos.cshtml` | `_te-verwijderen/CPMCore/Views/Projecten/Partials/Photos.cshtml` | Wordt nergens als partial geladen. |

### Verwijderde code: actie `MinimalTestPDF`

Stond in `CPMCore/Controllers/ProjectenController.ChangeOrders.cs`, onderaan de klasse, direct na `ChangeOrderFooter`:

```csharp
        public IActionResult MinimalTestPDF()
        {
            var pdf = new ViewAsPdf("MinimalTestPDF", "Dit is een test.")
            {
                PageOrientation = Orientation.Portrait,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Margins(10, 5, 40, 5),
                FileName = "MinimalTest.pdf"
            };
            return pdf;
        }
```

Nergens anders verwezen (gecontroleerd in .cs/.cshtml/.js). Terugzetten = deze methode terugplakken + de view terugzetten.

### Terugzetten

Vanuit de repo-root (`Group-LN`), per bestand, bv.:

    git mv _te-verwijderen/CPMCore/Views/Projecten/AddClient.cshtml CPMCore/Views/Projecten/AddClient.cshtml

Alles in één keer (bash):

    cp -r _te-verwijderen/CPMCore/Views/. CPMCore/Views/

(PowerShell: `Copy-Item _te-verwijderen\CPMCore\Views\* CPMCore\Views\ -Recurse`.) Daarna de map leegmaken/verwijderen.

---

## 2. Bewust NIET verplaatst (voorlopig laten staan)

Ofwel nog in gebruik, ofwel niet voldoende gecontroleerd. **Niets hiervan mag verwijderd worden** tot je bij de
bijhorende V2-pagina bent en er expliciet over beslist.

| Pagina | Status / bevinding |
|---|---|
| `Klanten/Partials/ChangeOrders` | Nog in gebruik: geladen door `Projecten/ChangeOrders/DetailsChangeOrder.cshtml`. |
| `Projecten/ChangeOrders/EditChangeOrder` | Nog in gebruik: gelinkt vanuit bovenstaande partial; actie is live. |
| `Projecten/Coordinatie/CoordinatieInstellingen` | Actie live, bereikbaar via URL. Enkel de oude `DetailCoordinatie` linkt ernaar; V2-instellingen zitten al in `_CoordinatieV2Settings` (`SaveCoordinatieInstellingenV2`). Waarschijnlijk later weg. |
| `Projecten/ChangeOrders/DetailsChangeOrder`, `AddChangeOrder` | Nog gelinkt vanuit V2-schermen (`Klanten/DetailV2`, `_SnelactiesCard`, dashboard, ondertekenflow-terug-urls, `SigningAdmin`). V2-flow = `ChangeOrdersV2`, `ChangeOrderDetailV2`, `QuoteIntakeV2`. Beslissing: die links naar V2 laten wijzen, dan kunnen de oude weg. |
| `Projecten/ChangeOrders/ChangeOrderPDF`, `ChangeOrderFooter` | Actie live; kiest zelf tussen oude opmaak en `ChangeOrderDocumentV2`. Houden zolang de oude PDF bestaat. |
| `Projecten/Contracts/PrintRecalculation` | PDF-sjabloon via `PdfController`; gelinkt vanuit `RecalculationV2`. Houden. |
| `Projecten/Clients/AddContact`, `ContactDetails`, `EditContact` | Vervangen door `ContactFormV2`/`ContactV2` in gl-v2-modus (redirect in `ProjectenController.ContactsV2.cs`). Enkel nog fallback voor klassieke layout, gelinkt vanuit oude `DetailContacts`. |
| `Projecten/Units/AddUnit`, `EditUnit` | Vervangen door `UnitFormV2`. Fallback voor klassieke layout; actienamen/breadcrumbs blijven hetzelfde. |
| `Projecten/Invoicing/PaymentStagesAddUpdate` | In gl-v2 redirect naar `PaymentGroupEditV2`/`PaymentStagesV2`. Fallback. |
| `Projecten/Invoicing/PaymentGroupLink` | Niets linkt ernaar behalve eigen formulier/breadcrumb; geen V2-equivalent. **Uitzoeken**: dode functionaliteit of gemist V2-scherm. |
| `Projecten/Budget/BudgetMasterAanmaken` | `BudgetIndexV2` gebruikt een modal die naar dezelfde POST-actie post; GET-view enkel nog gelinkt vanuit oude `BudgetIndex`. POST-actie blijft nodig. |
| `Projecten/Budget/BudgetVergelijken` | `BudgetVergelijkV2` bestaat; `BudgetResultaatV2` linkt nog naar de actie `BudgetVergelijken`. **Uitzoeken** of die naar V2 doorschakelt. |
| `Projecten/Issues/CreateQuick`, `CreateQuickSuccess` | Live (dashboard-snelactie "Punt", `/Projects/{id}/Issues/CreateQuick`). **V2 nodig.** |
| `Projecten/Media/DetailNews` (+ `Partials/News`) | Alleen oude `DetailMenu` linkt ernaar; V2-projectmenu niet. **Beslissen**: nieuws nog nodig? Zo ja V2 bouwen. |
| `Projecten/Partials/*` (`Clients`, `Contracts`, `Docs`, `Insurances`, `News`, `Sales`, `Units`, `ProjectActionButtons`) | Oude tab-partials, enkel geladen door de oude `Detail*`-pagina's. Weg zodra die oude pagina's weg zijn. |
| `Invoices/Editor/EditDraft` | Concept-facturen renderen altijd `EditDraft`, ook in gl-v2-modus (`EditView` kiest enkel voor niet-concept `EditV2`). **V2 nodig.** |
| `Klanten/Core/Form` | Oude read-only klantdetail; in gl-v2-modus gebruikt de controller `DetailsV2`. Fallback. |
| `Werfportaal/ContractorPortal/Dashboard`, `Werven`, `WerfDetail` | Externe aannemerspagina's met eigen `_Layout`, los van gl-v2-shell. Houden; V2 = apart besluit. |
| `Werfportaal/ContractorInvite/Aanmaken`, `Overzicht` | Interne pagina's op de hoofdlayout, gelinkt vanuit `DetailContract`. **V2 nodig.** |
| `Werfportaal/ContractorInvite/ResendInviteResult` | `Layout = null`, kleine statuspagina. Houden, evt. stylen. |
| `Ondertekenen/Signing/Index`, `Expired`, `Invalid` | Publieke ondertekenpagina's voor klanten (`SigningController`, `OndertekenenController`). Houden. |

## 3. Overzicht per categorie (voor later)

- **Reeds weggehaald:** zie sectie 1.
- **V2 nog te bouwen:** `EditDraft`, `CreateQuick`/`CreateQuickSuccess`, `ContractorInvite/Aanmaken` + `Overzicht`.
- **Beslissen:** wijzigingsopdracht-links (`DetailsChangeOrder`, `AddChangeOrder`), `DetailNews`, `PaymentGroupLink`, `BudgetVergelijken`.
- **Weg zodra de klassieke layout stopt:** alle fallbacks met een V2-doorverwijzing (contacten, eenheden, betaalstadia, budget) + de oude `Projecten/Partials/*` en `Klanten/Core/Form`.
- **Buiten deze analyse gelaten (op verzoek):** Algemeen/gebruiker (`Account`, `Deadlines`, `MijnTaken`, `Search`, `DocumentenCentrum`) en alle `Instellingen`-pagina's.

> Let op: "ongebruikt" is bepaald op basis van verwijzingen in `Controllers` en `Views` (en een controle in `.js`).
> Hardcoded URL's of verwijzingen van buiten de repo kunnen gemist zijn.

---

## 4. Factuurtemplates (24/10/2026): de pagina Instellingen › Factuurtemplates is weg

**Waarom:** er is geen bedrijf dat de JSON-layout `layoutB` gebruikt. `layoutA` is sinds 7/10 de gl-v2-factuur (`GlV2InvoiceTemplate`) en leest de sjabloon-JSON niet; `layoutHE` is niet geregistreerd en valt terug op `layoutA`. Een JSON-layout beheren heeft dus geen effect meer.

### Verplaatst
| Oorspronkelijke locatie | Nu |
|---|---|
| `CPMCore/Views/Instellingen/Facturatie/InvoiceTemplates.cshtml` | `_te-verwijderen/CPMCore/Views/Instellingen/Facturatie/InvoiceTemplates.cshtml` |
| `CPMCore/Views/Instellingen/Facturatie/InvoiceTemplatesCreate.cshtml` | idem onder `_te-verwijderen/` |
| `CPMCore/Views/Instellingen/Facturatie/InvoiceTemplatesEdit.cshtml` | idem onder `_te-verwijderen/` |
| `CPMCore/Views/Instellingen/Partials/_InvoiceTemplateForm.cshtml` | idem onder `_te-verwijderen/` |

### Verwijderde code
- **Controller-acties** `InvoiceTemplates` (GET), `InvoiceTemplatesCreate` (GET/POST), `InvoiceTemplatesEdit` (GET/POST), `InvoiceTemplatesDelete` (POST) uit `CPMCore/Controllers/InstellingenController.Facturatie.cs`, tussen `IssuerCompanyEdit` (POST, eindigt met `RedirectToAction(nameof(IssuerCompanies))`) en `IssuerCompanyOctopusAuthenticate`. Volledige tekst: `_te-verwijderen/CPMCore/Controllers/InstellingenController.Facturatie.InvoiceTemplates-acties.cs.txt`. Terugzetten = die tekst op die plek terugplakken.
- **Kaart "Factuurtemplates"** uit `InstellingenController.Algemeen.cs` (`if (permissionService.HasRead(PermissionCodes.SettingsInvoiceTemplates)) { facturatie.Items.Add(new SettingsItemVm { Icon = "ph-file-text", Title = "Factuurtemplates", Description = "Beheer factuurlay-outs die je per bedrijf kunt selecteren en hergebruiken.", Href = Url.Action("InvoiceTemplates", "Instellingen") ?? "#" }); }`, tussen de kaarten Mijn bedrijven en Bouwindexen.
- **Legacy**: het kaartblok `@if (canInvoiceTemplates) {…}` in `Views/Instellingen/Algemeen/Index.cshtml` (en `+ (canInvoiceTemplates ? 1 : 0)` in `countFacturatie`), de menu-regel `<li>@Html.ActionLink("Factuurtemplates", "InvoiceTemplates", "Instellingen")</li>` in `_LeftSidebarPartial.cshtml` en `_LeftSidebarPartialA.cshtml`, en de zin "Beheer templates in de templatemanager" onder het keuzeveld in `_IssuerCompanyForm.cshtml`.
- **Tab Lay-out & e-mail** (`Views/Instellingen/Partials/_IceLayoutV2.cshtml`): het keuzeveld Sjabloon, lettertype, secundaire kleur, juridische footer, het blok "Geavanceerd — sjabloon-JSON" (met Voettekst HTML) en de knop "Reset naar standaard". Blijven: de huiskleur (`BrandPrimaryColor`) en de e-mail. De verwijderde velden staan als verborgen velden in het formulier, zodat hun bewaarde waarden niet gewist worden.

### Aangepast (niet verwijderd)
- `ServiceCore/Invoicing/Pdf/InvoicePdfService.LoadLayout`: de JSON van een bedrijf wordt enkel nog gelezen en gevalideerd voor de sleutel `layoutB`; anders `DefaultLayouts.Get(sleutel)`. Een foute JSON laat de gl-v2-factuur niet meer mislukken. Terugzetten: de voorwaarde `|| !string.Equals(templateKey, "layoutB", …)` weghalen.
- `InstellingenController.Facturatie.cs`: `ValidateTemplateJson(vm.TemplateJson)` (twee plaatsen, bij bedrijf aanmaken en bewerken) draait enkel nog als `TemplateKey` `layoutB` is.

### Bewust blijven staan
Permissiecode `SettingsInvoiceTemplates` (beveiligt nog `InvoiceLayoutController.Preview`, het live PDF-voorbeeld), de service `InvoiceLayoutTemplateService` + tabel `InvoiceLayoutTemplate` (nog gebruikt door `GetInvoiceTemplateOptionsAsync` en het oude bedrijfsformulier), de kolommen `TemplateKey`, `TemplateJson`, `BrandSecondaryColor`, `FontFamily`, `FooterLegalText`, `InvoiceFooterHtml` op `IssuerCompany`, en de JSON-pijplijn zelf (`JsonInvoiceTemplate`, de renderers, het schema).

### Later opruimen
Als `layoutB` definitief weg mag: `JsonInvoiceTemplate`, de `ISectionRenderer`-klassen, `BandsRenderer`, het schema en `DefaultLayouts` verwijderen, de registratie in `Program.cs`, de tabel `InvoiceLayoutTemplate` en de bovenstaande kolommen laten vallen via een SQL-script in `_migrations/`. Bedrijven met `TemplateKey = 'layoutHE'` kunnen op `layoutA` gezet worden: `UPDATE IssuerCompany SET TemplateKey = 'layoutA' WHERE TemplateKey <> 'layoutA'` (optioneel, het resultaat is al hetzelfde).


---

## 5. Gebruikers en rollen (25/10/2026): vervangen door gl-v2, NOG NIET verplaatst

Sinds 24/10 staan Gebruikers, Gebruiker · rechten en Rollen op gl-v2 (`UserAdmin/IndexV2`, `RechtenV2`, `RollenV2`). De oude schermen hieronder zijn daardoor in gl-v2-modus onbereikbaar of dubbel. Er is **nog niets verplaatst of verwijderd**: dit is de lijst voor later, zodra de klassieke layout (`?classic=true`) mag stoppen.

### Kandidaten
| Locatie | Waarom | Let op bij verwijderen |
|---|---|---|
| `CPMCore/Views/Instellingen/AppRoles/Index.cshtml` en `Create.cshtml` | Vervangen door het tabblad Rollen (`UserAdmin/Rollen`). | Eerst `AppRolesController` (73 regels, acties `Index`, `Create` GET/POST) mee verwijderen. De kaart-chip "Rollen" op Instellingen wijst al naar `UserAdmin/Rollen`. |
| Links naar `AppRoles` in `Views/Instellingen/Algemeen/Index.cshtml` (r. ~276), `Views/Shared/_LeftSidebarPartial.cshtml` (r. ~356) en `_LeftSidebarPartialA.cshtml` (r. ~253) | Wijzen naar de oude rollenpagina. | Samen met de views weghalen, anders 404/lege link. |
| `CPMCore/Views/Instellingen/UserAdmin/Create.cshtml` en `Edit.cshtml` | Vervangen door "Gebruiker uitnodigen" (modal op `IndexV2`) en de pagina `Rechten`. | De acties `Create` (GET/POST) en `Edit` in `UserAdminController.cs`. `Rechten` en `Rollen` verwijzen in klassieke modus nog naar `Edit` (redirect). |
| `CPMCore/Views/Instellingen/UserAdmin/Index.cshtml` (1362 regels, de oude alles-in-één pagina) | Vervangen door `IndexV2`. | Enkel nog in klassieke modus. Hoort met `Modals/_UserAdminModals.cshtml`, `Modals/_GuestInviteModals.cshtml` en `Modals/_ModalDeleteUser.cshtml`. |
| Oude acties in `UserAdminController.cs`: `CreateModal`, `QuickUpdate`, `ResetToInternal`, `LinkEntra`, `ModalDelete`, `PermissionMatrix`, `SaveRolePermission`, `RolePermissionMatrix`, `DeleteRole`, `RenameRole`, `CreateRole`, `SetUserActive`, `SaveUserPermissionOverride`, `InviteGuest`, `ResendInvite`, `ResetInvite`, `UnlinkGuest` | Vervangen door de `…V2`-acties in `UserAdminController.V2*.cs`. | **Behouden**: `Delete` (de nieuwe lijst post er nog naar), `GetPermissionDefinitionsAsync`, `EnsurePermissionCodeExistsAsync`, `LoadEntraUsersAsync`, `HiddenChildCodes` is dubbel met `PermissionTreeV2.HiddenChildren` en mag mee weg. |
| Oude CSS/JS enkel voor die schermen (`instellingen-custom.css`-delen, inline scripts in `Index.cshtml`) | Alleen de oude pagina gebruikt ze. | Controleer eerst of andere klassieke pagina's `instellingen-custom.css` nog laden. |

### Eerst controleren
- Klassieke modus (`?classic=true`) moet echt weg mogen, anders breek je die weg.
- Of iets buiten de repo naar `/AppRoles` of `/UserAdmin/Edit` linkt (bookmarks, e-mails).
- Dat `UserAdmin/Delete` en de Entra-hulpmethodes blijven staan.

### Zo zet je dit later in de map
Zelfde recept als sectie 1: `git mv` per bestand naar `_te-verwijderen/CPMCore/…`, de verwijderde controller-acties als `.cs.txt` bewaren naast het README en hier de originele locaties noteren.
