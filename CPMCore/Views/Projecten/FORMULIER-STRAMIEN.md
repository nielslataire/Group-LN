# Formulier-stramien — CPM

> Praktische kopieerbron voor een nieuw formulier binnen CPM, gedestilleerd uit
> `Projecten/Toevoegen`, `Projecten/Edit`, `Projecten/AddContract` en
> `Projecten/AddUnit`. Volgt de regels van de root-`DESIGN.md` ("Grounded &
> Cultivated"): één merkgroen, warme neutralen, 7 px radius, hairline-randen,
> Poppins, bruikbaar vanaf 360 px.
>
> **Kies eerst het stramien** (§2), plak het skelet, vul de velden met de
> bouwstenen uit §4. De legacy-stijl in §10 is een **anti-referentie** — niet
> kopiëren.
>
> De *afdwingbare* regels (die `impeccable` bij een scan leest) staan in
> `DESIGN.md` → `## Components` → **Formulieren (`gl-form-shell`)** + *The
> Form-Shell Rule*, en in de sidecar `.impeccable/design.json`. Dit bestand is
> de uitgewerkte kopieerbron; houd beide gelijk als het stramien verschuift.

---

## 1. Designtokens

Allemaal gedefinieerd op `:root` in `wwwroot/css/custom.css`. Nooit losse hex in
een view — altijd de token.

| Token | Waarde | Rol in een formulier |
|---|---|---|
| `--primary` | `#0a5a3b` | Primaire knop, focus, actieve tab, verplicht-ster, sectie-icoon |
| `--lightgreen` / `--green-light` | `#e8f0eb` | Achtergrond sectie-icoonbadge, hover terug-knop, hint-box |
| `--border` | `#e7e7e7` | Rand van elk `form-control-modern`, kaart, sectiescheiding |
| `--radius` | `7px` | Inputs, knoppen, kaarten (navigatie-chrome = 14–16 px, hier niet) |
| `--muted` | `#8590a5` | Subtitel, helptekst, tekenteller, placeholder-toon |
| `--white` | `#ffffff` | Kaart- en inputvlak |
| `--danger` | `#b3452f` | Validatiefout-rand, fouttekst, verwijderknop-tint |
| `--danger-tint` / `--danger-text` | `#f7ece8` / `#8a3420` | Foutbanner-achtergrond / -tekst |
| `--warning` | `#c17d1f` | Onderstreping actieve tab (`gl-form-shell__tab`), "over budget" |
| `--text` | `#26313a` | Veldlabel, sectietitel, ingevulde waarde |

Typografie: **Poppins** overal. Hiërarchie met **gewicht, niet grootte** —
`700` sectietitel, `600` label, `400` waarde/hint. Geen tweede font, geen
uppercase-tracking, geen italic.

---

## 2. Welk stramien kies ik?

| | **A · `gl-form-shell`** | **B · `card-big-info`** |
|---|---|---|
| Vorm | Volle-breedte kaart die op één scherm past; optioneel tabstrip erboven | Losse kaart(en) met linker icoon-rail + rechter velden |
| Wanneer | Standaardkeuze voor een nieuw formulier. Lang formulier → tabs. Kort → één paneel zonder tabs (zie `Toevoegen.cshtml`) | Wanneer je 2–3 duidelijk afgebakende blokken naast elkaar/onder elkaar wil met uitleg per blok |
| Acties | `_FormShellActions` — vaste balk onderaan de viewport | `.action-buttons`-rij onderaan het formulier |
| Labels | **Boven** het veld (`.gl-field`) | **Rechts uitgelijnd** naast het veld (`.control-label`) |
| Referentieviews | `Projecten/Toevoegen.cshtml`, `Projecten/Edit.cshtml` | `Projecten/AddContract.cshtml`, `Projecten/AddUnit.cshtml` |

Beide gebruiken dezelfde veldbouwstenen (§4), dezelfde knoppen (§5), dezelfde
validatie (§6). Verschil zit enkel in de schil en de labelplaatsing.

---

## 3. Stramien A — `gl-form-shell` (één scherm)

CSS: `wwwroot/css/custom.css` ("Formulierschil") + `wwwroot/css/projecten-custom.css`
("Projecten/Toevoegen + Bewerken — formulier"). Toevoegen aan een nieuwe module?
De `gl-form-shell*`-klassen zijn generiek; de `gl-form-section` / `gl-field-grid`
staan onder `.gl-project-form` — hergebruik die scope of hef ze op naar een
eigen wrapper.

### 3a. Kort formulier (geen tabs)

```cshtml
@model MyNamespace.MyFormModel

@section PageStyle
{
    <link rel="stylesheet" href="~/lib/select2/css/select2.css" />
    <link rel="stylesheet" href="~/lib/select2-bootstrap-theme/select2-bootstrap.css" />
    <link rel="stylesheet" href="~/css/admin/theme-admin-extension.css" />
    <link rel="stylesheet" href="~/css/admin/skins/extension.css" />
    <link rel="stylesheet" href="~/css/projecten-custom.css" asp-append-version="true" />
}

@using (Html.BeginForm("MyAction", "MyController", FormMethod.Post,
        new { @class = "ecommerce-form gl-project-form", enctype = "multipart/form-data" }))
{
    @Html.AntiForgeryToken()

    <div asp-validation-summary="All" class="alert alert-danger @(ViewData.ModelState.IsValid ? "d-none" : "")"></div>

    <div class="card card-modern gl-form-shell">
        <div class="card-body">
            <div class="gl-form-shell__panel">
                <section class="gl-form-section">
                    <div class="gl-form-section__body">
                        <div class="gl-form-section__head">
                            <i class="bx bx-info-circle" aria-hidden="true"></i>
                            <div>
                                <h2 class="gl-form-section__title">Algemene info</h2>
                                <p class="gl-form-section__hint">Korte uitleg wat hier hoort.</p>
                            </div>
                        </div>

                        <div class="gl-field-grid">
                            <div class="gl-field">
                                <label for="Name">@Html.DisplayNameFor(m => m.Name) <span class="gl-req" aria-hidden="true">*</span></label>
                                @Html.TextBoxFor(m => m.Name, new { @class = "form-control form-control-modern", placeholder = "…", required = "required" })
                                <span asp-validation-for="Name" class="text-danger"></span>
                            </div>
                            @* … meer .gl-field … *@
                        </div>
                    </div>
                </section>
            </div>

            @await Html.PartialAsync("_FormShellActions", new CPMCore.Models.FormShellActionsModel
            {
                SubmitLabel = "Aanmaken",
                CancelUrl = (TempData.Peek("Referrer") as string) ?? Url.Action("Index", "MyController")
            })
        </div>
    </div>
}

@section PageScripts
{
    @await Html.PartialAsync("_ValidationScriptsPartial")
    <script src="~/lib/jquery-validation/localization/messages_nl.js"></script>
}
```

### 3b. Lang formulier (tabstrip)

De tabstrip zit **buiten** de kaart (het is de primaire in-page navigatie). Alle
velden blijven altijd in het formulier — tabs tonen/verbergen enkel, één Opslaan
bewaart alles. Kopieer het tabmechanisme uit `Projecten/_ProjectFormTabs.cshtml`.

```cshtml
<div class="gl-form-shell__tabs" role="tablist" aria-label="Mijn gegevens"
     data-tabgroup="my-form" data-force-tab="@firstErrorTab">
    @foreach (var t in tabDefs)
    {
        <button type="button" class="gl-form-shell__tab" role="tab"
                id="tabbtn-@t.Key" data-tab="@t.Key"
                aria-controls="tab-@t.Key"
                aria-selected="@(t.Key == tabDefs[0].Key ? "true" : "false")"
                tabindex="@(t.Key == tabDefs[0].Key ? "0" : "-1")">
            <i class="bx @t.Icon" aria-hidden="true"></i>
            <span>@t.Label</span>
            @if (tabErrorCount(t.Key) is var n && n > 0)
            {
                <span class="gl-form-shell__tab-badge" title="@n fout(en)">@n</span>
            }
        </button>
    }
</div>

<div class="card card-modern gl-form-shell">
    <div class="card-body">
        <div id="tab-algemeen" class="gl-form-shell__panel" role="tabpanel"
             aria-labelledby="tabbtn-algemeen" tabindex="0">
            @* sectie(s) *@
        </div>
        <div id="tab-status" class="gl-form-shell__panel" role="tabpanel"
             aria-labelledby="tabbtn-status" tabindex="0" hidden>
            @* sectie(s) *@
        </div>

        @await Html.PartialAsync("_FormShellActions", new CPMCore.Models.FormShellActionsModel
        {
            SubmitLabel = "Wijzigingen opslaan",
            CancelUrl = …
        })
    </div>
</div>
```

Server zet `data-force-tab` op de eerste tab met een validatiefout; anders wint
de URL-hash, anders tab 1. Zie het `TabMap` + `tabErrorCount`-blok bovenaan
`Projecten/Edit.cshtml` voor de mapping veld→tab en de foutteller-badge.

### 3c. Bouwstenen van stramien A

| Klasse | Doet |
|---|---|
| `.gl-form-shell` | Kaart met `height: calc(100vh − …)`; body scrolt nooit |
| `.gl-form-shell__tabs` / `__tab` / `__tab-badge` | Tabstrip buiten de kaart; actieve tab = groene tekst + okergele onderstreping; badge = rode fout-teller |
| `.gl-form-shell__panel` | Scrollzone per tab; `[hidden]` verbergt niet-actieve |
| `.gl-form-shell__actions` | Vaste actiebalk (`position: fixed`, `left: 300px` / `73px` bij ingeklapte sidebar) — **enkel** via `_FormShellActions` |
| `.gl-form-section` + `__body` | Blok binnen een paneel; `margin-bottom: 16px` |
| `.gl-form-section__head` | Flexrij: 42 px getinte icoonbadge (`11px` radius, `--lightgreen` bg, `--primary` glyph) + titel + hint, hairline eronder |
| `.gl-form-section__title` / `__hint` | `1.0625rem`/`700` · `.8125rem`/`--muted` |
| `.gl-field-grid` | `grid`, `repeat(auto-fit, minmax(320px, 1fr))`, `gap: 2px 24px` — wordt vanzelf 1/2/3 kolommen |
| `.gl-field` | Flexkolom, label boven veld, `gap: 6px`, `margin-bottom: 18px` |
| `.gl-field--full` | `grid-column: 1 / -1` (volle breedte); veld zelf `max-width: 560px` |
| `.gl-field--full.gl-field--wide` | Volle breedte **en** veld `max-width: 100%` (textarea, rich text) |
| `.gl-req` | De rode `*` na een label van een verplicht veld |
| `.gl-field-address` | `display:flex; gap:10px` — `.gl-street` (`flex:1`) + `.gl-nr` (`96px` vast) |
| `.gl-switch-row` | iOS-switch + label naast elkaar (§4g) |
| `.gl-form-note` | Getinte hint-box in een sectie (`--lightgreen` bg, `bx`-icoon links) |

---

## 4. Stramien B — `card-big-info` (twee-zone kaart)

CSS: overschreven in `wwwroot/css/custom.css` ("card-big-info herwerking"). Eén
`<section class="card card-modern card-big-info">` per logisch blok; stapel
meerdere met `.row.mt-4` ertussen.

```cshtml
<div class="row mt-2">
    <div class="col">
        <section class="card card-modern card-big-info">
            <div class="card-body">
                <div class="row">
                    @* linker rail — kolombreedtes exact zo overnemen *@
                    <div class="col-lg-2-5 col-xl-1-5">
                        <i class="card-big-info-icon bx bx-box" aria-hidden="true"></i>
                        <h2 class="card-big-info-title">Algemene info</h2>
                        <p class="card-big-info-desc">Voeg hier de algemene info in.</p>
                    </div>
                    @* rechter velden *@
                    <div class="col-lg-3-5 col-xl-4-5">
                        <div class="form-group row align-items-center pb-3">
                            <label class="col-12 col-md-4 control-label text-md-end mb-0">
                                @Html.LabelFor(m => m.Field)
                            </label>
                            <div class="col-12 col-md-8">
                                @Html.TextBoxFor(m => m.Field, new { @class = "form-control form-control-modern", placeholder = "…" })
                            </div>
                        </div>
                        @* … meer .form-group.row … *@
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>

<div class="row action-buttons">
    <div class="col-12 col-md-auto">
        <button type="submit" class="submit-button btn btn-primary btn-px-4 py-3 d-flex align-items-center font-weight-semibold line-height-1" data-loading-text="Laden…">
            <i class="bx bx-save text-4 me-2" aria-hidden="true"></i> Opslaan
        </button>
    </div>
    <div class="col-12 col-md-auto px-md-0 mt-3 mt-md-0">
        <a href="@TempData.Peek("Referrer")" class="cancel-button btn btn-light btn-px-4 py-3 border font-weight-semibold text-color-dark text-3">Annuleren</a>
    </div>
</div>
```

Kernpunten:

- Linker rail: `col-lg-2-5 col-xl-1-5`; rechter zone: `col-lg-3-5 col-xl-4-5`
  (niet-standaard Bootstrap-breekpunten uit `theme.css`). Onder 768 px stapelen
  ze en verschijnt een hairline tussen de twee zones.
- `card-big-info-icon` = een `bx`-icoon in een 40 px `--lightgreen` box.
- Elke veldrij = `<div class="form-group row align-items-center pb-3">` met
  een rechts-uitgelijnd label (`col-md-4 control-label text-md-end mb-0`) en
  het veld in `col-md-8` (of smaller: `col-md-3` + tweede label + `col-md-2`
  voor twee velden op één rij).
- Wrap het formulier in
  `Html.BeginForm(..., new { @class = "ecommerce-form action-buttons-fixed", enctype = "multipart/form-data" })`.

Paginakop erboven (beide stramienen mogen dit gebruiken): zie §7 `_PageHeader`.

---

## 5. Veldbouwstenen — "de afwerking van de textboxen"

Alle tekstvelden krijgen **`form-control form-control-modern`**: witte vulling,
1 px `--border`-rand, 7 px radius, groengetinte focusring (consistent met de
knoppen). Select2 / datepicker / multiselect worden op dezelfde ~46 px hoogte en
`6px 17px` padding gehouden — **nooit** losser stylen, native en verrijkte
velden moeten uitlijnen.

Wrapper hangt af van het stramien:
`.gl-field` (A, label boven) of `.form-group.row…` + `.col-md-8` (B, label rechts).
Hieronder telkens **enkel het veld**.

### 5a. Tekst

```cshtml
@Html.TextBoxFor(m => m.Name, new {
    @class = "form-control form-control-modern",
    placeholder = "Naam van het project",
    autocomplete = "off",
    maxlength = "50"
})
```

### 5b. Tekstvak (meerregelig)

```cshtml
@Html.TextAreaFor(m => m.Description, 4, 40, new {
    @class = "form-control form-control-modern",
    placeholder = "Optionele omschrijving…",
    maxlength = "320"
})
```
Plaats in `.gl-field.gl-field--full.gl-field--wide` (stramien A) zodat het veld
100 % breed mag zijn. Tekenteller eronder: `<small class="text-muted"><span id="…Count">0</span>/320</small>` + kleine `input`-handler (zie `AddContract.cshtml`).

### 5c. Keuzelijst (native `<select>`)

```cshtml
@Html.DropDownListFor(m => m.SelectedStatus,
    new SelectList(Model.Statuses, "ID", "Display", Model.SelectedStatus),
    "-- Selecteer --",
    new { @class = "form-control form-control-modern", id = "lstStatus" })
```
Enum: `Html.GetEnumSelectList<ProjectType>()`. Native-selects krijgen expliciete
kleuren (`select.form-control-modern { background:#fff; color:#212529 }`) tegen
geforceerde Windows-thema's.

### 5d. Zoekende keuzelijst (Select2 / ajax)

```cshtml
<select id="PostalSelect" class="form-control form-control-modern w-100"
        aria-label="Gemeente" aria-required="true">
    @if (Model.SelectedId > 0)
    {
        <option value="@Model.SelectedId" selected>@Model.SelectedDisplay</option>
    }
</select>
<span id="postalSelectError" class="text-danger" role="alert" hidden>Kies een waarde uit de lijst.</span>
```
```js
$('#PostalSelect').select2({
    theme: 'bootstrap',
    width: '100%',
    minimumInputLength: 3,          // weglaten voor een korte statische lijst
    placeholder: 'Typ om te zoeken…',
    language: 'nl',
    ajax: { url: '@Url.Action("Search", "…")', dataType: 'json', delay: 250,
            data: p => ({ term: p.term }),
            processResults: d => ({ results: d }) }
});
```
Statische lijst zonder ajax: geef het `<select>` de klasse `populate` of
`select2` + `data-plugin-selectTwo` en laat `theme.admin.extension.js` het
initialiseren.

### 5e. Bedrag (€) — EditorTemplate

```cshtml
@Html.EditorFor(m => m.Price, "Currency")
```
Rendert een `input-group`: `<input class="form-control Currencymask">` + suffix
`<span class="input-group-text">€</span>`. Init de maskering:
```js
CurrencyMask.init('.Currencymask');                 // bij document.ready
CurrencyMask.init($nieuweRij.find('.Currencymask')); // na een ajax-toegevoegde rij
```
Variant met knoppen ernaast (verwijderen/bewerken in een herhaalrij):
`@Html.EditorFor(m => m.Price, "CurrencyWithActions", new { ShowDelete = true, DeleteId = id })`.

### 5f. Percentage (%) / Oppervlakte / Postcode / Telefoon

```cshtml
@Html.EditorFor(m => m.VatPercentage, new { @class = "form-control form-control-modern", placeholder = "0,00" })
```
EditorTemplates in `Views/Shared/EditorTemplates/`: `Percentage` (`%`-suffix),
`Surface`, `Postalcode`, `Phone`, `Cellphone`, `Date`. Allemaal `input-group`
met suffix/prefix in dezelfde `form-control-modern`-stijl.

### 5g. Datum

Kort (native, aanbevolen voor nieuwe formulieren):
```cshtml
@Html.TextBoxFor(m => m.StartDate, "{0:yyyy-MM-dd}", new {
    type = "date", @class = "form-control form-control-modern", id = "txtStartDate"
})
```
Met kalender-affordance (bestaande stijl):
```cshtml
<div class="input-group">
    <span class="input-group-text"><i class="fas fa-calendar-alt" aria-hidden="true"></i></span>
    @Html.TextBoxFor(m => m.StartDate, "{0:yyyy-MM-dd}", new { type = "date", @class = "form-control form-control-modern" })
</div>
```
Bootstrap-datepicker-plugin: `@Html.EditorFor(m => m.StartDate, "Date")`
(`dd/mm/yyyy`, `nl-BE`, autoclose). Zet `min-width: 8rem` op een datumveld in
een krappe kolom zodat het niet onbruikbaar smal wordt.

### 5h. Aan/uit — iOS-switch (voorkeur boven checkbox)

```cshtml
<div class="gl-switch-row">
    <div class="switch switch-sm switch-primary">
        @Html.CheckBoxFor(m => m.IsPublished, new { data_plugin_ios_switch = "", id = "IsPublished" })
    </div>
    <label class="form-check-label mb-0" for="IsPublished">
        @Html.DisplayNameFor(m => m.IsPublished)
        <small class="text-muted">— zichtbaar op de publieke website</small>
    </label>
</div>
```
```js
// scripts: <script src="~/lib/ios7-switch/ios7-switch.js"></script>
$('[data-plugin-ios-switch]').each(function () {
    if (!$(this).data('ios7-switch-init')) { new ios7Switch(this); $(this).data('ios7-switch-init', true); }
});
```
Groottes: `switch-sm` (25×55), default (35×65), `switch-lg` (45×75).

### 5i. Aanvinkvakje (legacy — enkel als een switch niet past)

```cshtml
<div class="checkbox-custom checkbox-default">
    @Html.CheckBoxFor(m => m.Signed, new { id = "chkSigned" })
    <label></label>
</div>
```

### 5j. Bestand

```cshtml
<input type="file" name="Upload" id="Upload"
       accept="image/jpeg,image/png,image/webp"
       class="form-control form-control-modern" />
<img id="preview" class="gl-form-foto-preview d-none mt-2" alt="Voorbeeld" />
```
Formulier moet `enctype = "multipart/form-data"` hebben. Client-side check op
extensie + grootte vóór submit (zie `#fileGuaranteeDoc` in `AddContract.cshtml`),
en toon de fout in `<div class="gl-danger-icon small mt-1 d-none" role="alert">`.

### 5k. Adres op één regel

```cshtml
<div class="gl-field gl-field--full gl-field--wide">
    <label for="Street">Adres</label>
    <div class="gl-field-address">
        <div class="gl-street">@Html.TextBoxFor(m => m.Street, new { @class = "form-control form-control-modern", placeholder = "Straat" })</div>
        <div class="gl-nr">@Html.TextBoxFor(m => m.HouseNumber, new { @class = "form-control form-control-modern", placeholder = "Nr.", aria_label = "Huisnummer" })</div>
    </div>
</div>
```

### 5l. Herhaalrij "toevoegen" + leeg-state

Voor lijsten die de gebruiker rij per rij opbouwt (activiteiten, bouwwaardes):

```cshtml
<div class="d-flex justify-content-between align-items-center gap-2 mb-3">
    <h3 class="text-color-dark font-weight-bold text-4 mb-0 text-uppercase">Titel</h3>
    <button type="button" class="btn btn-outline-primary btn-sm" id="btnAdd">
        <i class="bx bx-plus me-1"></i>Toevoegen
    </button>
</div>

<div id="Rows">
    @foreach (var item in Model.Items)
    {
        @await Html.PartialAsync("_MyRow", item, new ViewDataDictionary(ViewData) { { "mode", "add" } })
    }
</div>
<div id="emptyHint" class="empty-state-box">Nog niets toegevoegd.</div>
```
`.empty-state-box` = gestreepte rand, `--border`, gecentreerde `--muted`-tekst.
Rij-partial gebruikt `Html.BeginCollectionItem("items")` (pakket
`HtmlHelpers.BeginCollectionItemCore`) zodat de indexering na ajax klopt — zie
`Partials/_ActivityRow.cshtml`. Nieuwe rij ophalen via ajax en
`initCurrencyMasks()` / `initSwitches()` opnieuw draaien op de nieuwe knooppunten.

---

## 6. Knoppen

| Rol | Markup | Waar |
|---|---|---|
| Primaire submit (formulieractie) | `class="submit-button btn btn-primary btn-px-4 py-3 d-flex align-items-center font-weight-semibold line-height-1" data-loading-text="Laden…"` met `<i class="bx bx-save text-4 me-2">` | Actiebalk onderaan |
| Annuleren | `<a class="cancel-button btn btn-light border btn-px-4 py-3 font-weight-semibold text-color-dark text-3">` → `TempData.Peek("Referrer")` | Naast submit |
| Tweede submit ("Opslaan & nog een") | zelfde als submit maar `btn btn-light border` + `name="saveAction" value="addAnother"` | Naast submit |
| Secundaire actie (inline toevoegen, nieuw item) | `btn btn-outline-primary btn-sm` of `btn btn-outline-secondary` met `<i class="bx bx-plus me-1">` | In een sectie |
| Verwijderen (zacht) | `btn-gl-remove` (`#fbd0d0` bg, rode tekst) of `btn btn-danger deleterow` met `bx-trash` in een herhaalrij | Herhaalrij |
| Icoonknop (bv. terug) | 40×40, `bx` op `1.5rem`, `--muted`, hover `--lightgreen` bg + `--primary` — zie `.gl-page-header__back` | Paginakop |

Regels: rond (7 px), 1 px rand gelijk aan vulling, focusring
`0 0 0 0.2rem rgba(10,90,59,.25)`, hover naar `#0f4b40`. Geen pill-knoppen.
Zachte getinte statuskleuren boven volle luide vlakken. Bij submit: spinner in
de knop, dubbele submit blokkeren (`AddContract.cshtml` heeft het patroon).

---

## 7. Herbruikbare partials & templates

| Pad | Signatuur / gebruik |
|---|---|
| `Views/Shared/_PageHeader.cshtml` | `new PageHeaderModel { Title, Subtitle?, BackUrl?, BackAriaLabel? }` — rendert enkel het linkercluster; wrap zelf in `<div class="gl-page-header">…<div class="gl-page-header__actions">…</div></div>` |
| `Views/Shared/_FormShellActions.cshtml` | `new FormShellActionsModel { SubmitLabel, SubmitIcon = "bx-save", CancelUrl, CancelLabel = "Annuleren" }` — vaste Opslaan/Annuleren-balk voor `gl-form-shell` |
| `Views/Projecten/_ProjectFormCoreFields.cshtml` | Naam / Projectcode / Land / Gemeente / Verantwoordelijke als losse `.gl-field`'s — plaats binnen je eigen `.gl-field-grid` (model implementeert `IProjectFormModel`) |
| `Views/Projecten/_ProjectFormTabs.cshtml` | `<script>` met het volledige tab-toon/verberg + toetsenbord + `data-force-tab`-gedrag; pas de `data-tabgroup`-selector aan |
| `Views/Projecten/_ProjectFormStyles.cshtml` | De vaste set `<link>`'s (select2, theme-admin-extension, projecten-custom, quill) |
| `Views/Shared/EditorTemplates/` | `Currency`, `CurrencyWithActions`, `Percentage`, `Surface`, `Postalcode`, `Date`, `Phone`, `Cellphone` |
| `Views/Shared/_ValidationScriptsPartial.cshtml` + `messages_nl.js` | jQuery-validatie in het NL |

Paginakop-skelet:

```cshtml
<div class="gl-page-header">
    @await Html.PartialAsync("_PageHeader", new CPMCore.Models.PageHeaderModel
    {
        Title = "Contract toevoegen",
        Subtitle = $"Voeg een contract toe aan {Model.ProjectName}",
        BackUrl = Url.Action("DetailContracts", "Projecten", new { projectid = Model.ProjectId }),
        BackAriaLabel = "Terug naar leveranciers"
    })
    <div class="gl-page-header__actions">
        @* optionele paginaspecifieke knoppen *@
    </div>
</div>
```

---

## 8. Validatie & foutafhandeling

- **Samenvatting** bovenaan het formulier:
  `<div asp-validation-summary="All" class="alert alert-danger @(ViewData.ModelState.IsValid ? "d-none" : "")"></div>`
  (of `@Html.ValidationSummary(false, "", new { @class = "mb-0" })` binnen een
  `alert alert-danger`).
- **Per veld:** `<span asp-validation-for="Field" class="text-danger"></span>`
  direct onder het veld. In stramien A verbergt `.gl-field > .text-danger:empty`
  de lege span.
- **Foutstijl:** `input.input-validation-error` krijgt `--danger`-rand +
  `0 0 0 2px` getinte gloed (gedefinieerd onder `.gl-project-form`).
- **Tabs:** teller-badge per tab (`gl-form-shell__tab-badge`) + server zet
  `data-force-tab` op de eerste foute tab; script scrollt naar het eerste
  ongeldige veld. Zit een fout veld in een ingeklapte sectie → eerst uitklappen
  (zie `#opvolgingCollapse`-logica in `AddContract.cshtml`).
- **Onopgeslagen wijzigingen:** `beforeunload`-waarschuwing zolang het formulier
  "dirty" is en niet aan het submitten (patroon in `AddContract.cshtml`).
- Client + server dezelfde regels (bv. bestandscontrole) zodat een fout meteen
  opvalt i.p.v. na een volledige round-trip.

---

## 9. Responsive

- Elk scherm bruikbaar en leesbaar vanaf **360 px**; besturingselementen
  blijven ≥ 40 px hoog; `touch-action: manipulation` op knoppen en velden tegen
  dubbeltik-zoom.
- `.gl-field-grid` valt vanzelf terug van 3 → 2 → 1 kolom (`minmax(320px, 1fr)`).
- `card-big-info`: linker rail + rechter velden stapelen onder 768 px met een
  hairline ertussen; de rechts-uitgelijnde labels worden links (`col-12 col-md-4`).
- `gl-form-shell__actions` volgt de sidebar (`left: 300px` → `73px` ingeklapt →
  `0` onder 768 px) en respecteert `env(safe-area-inset-bottom)`.
- `prefers-reduced-motion`: alle switch-/tab-transities uit, kleur/toestand blijft.

---

## 10. Do / Don't

**Do**
- Kies **stramien A** tenzij je expliciet de twee-zone-uitlegkaart wil.
- Elk tekstveld = `form-control form-control-modern`; select2/datepicker op
  dezelfde hoogte houden.
- Labels in stramien A **boven** het veld (`.gl-field`), in stramien B **rechts**
  uitgelijnd (`.control-label text-md-end`). Niet mengen binnen één formulier.
- Verplicht veld: `<span class="gl-req">*</span>` in het label **en**
  `required`/`aria-required` op het veld.
- Tokens gebruiken (`var(--primary)`, `var(--border)`, `var(--radius)`) — geen
  losse hex in een view of `<style>`-blok.
- Één merkgroen-markering per veldcluster; status = `--danger`/`--warning`, nooit
  stock-rood/-amber/-blauw.
- Bedragen via `EditorFor(…, "Currency")` + `CurrencyMask.init` (ook na ajax).
- `_PageHeader` + `_FormShellActions` hergebruiken i.p.v. de rij opnieuw bouwen.

**Don't**
- Geen kale `card-modern` + `row g-3` + `col-md-6` + `form-label` + `form-control`
  (de `AddContact`/`EditContact`-stijl — zie §11).
- Geen `<h1 class="fw-bold">` + losse `<h5>` als ad-hoc paginakop; gebruik
  `_PageHeader`.
- Geen tweede lettertype, uppercase-tracking of italic voor nadruk — gewicht of
  groen.
- Geen pill-knoppen, geen scherpe 0 px hoeken, geen 4 px/12 px radius-eenmalters.
- Geen zichtbare slagschaduw op een rustende kaart; hairline-rand + eventueel
  `--lightgreen`-kop.
- Select2/datepicker/multiselect niet losser stylen dan `form-control-modern`.
- Geen alarmrood voor een goede/neutrale toestand ("verkocht" = `bg-primary`).

---

## 11. Anti-referentie (niet kopiëren)

`Projecten/AddContact.cshtml` en `EditContact.cshtml` gebruiken nog het oude
patroon:

```cshtml
<div class="card card-modern"><div class="card-body">
    <h1 class="h4 mb-4">Contact toevoegen</h1>
    <form method="post" action="…">
        <div class="row g-3">
            <div class="col-md-6">
                <label class="form-label">Voornaam</label>
                <input type="text" class="form-control" name="Firstname" />
            </div>
            …
```

Verschillen met het stramien: kale `form-control` i.p.v. `form-control-modern`,
`form-label` i.p.v. `.gl-field > label`, geen sectiekop met icoonbadge, geen
`gl-form-shell`, ad-hoc `<h1 class="h4">` i.p.v. `_PageHeader`, `btn-default`
i.p.v. `btn btn-light border`. Migreer deze views naar stramien A wanneer je ze
toch aanraakt; bouw er geen nieuwe zo.

---

## 12. Checklist — nieuw formulier

1. Controller-actie + `@model` viewmodel (FacadeCore-DTO, geen entiteit).
2. `@section PageStyle` → de `<link>`-set (of `_ProjectFormStyles`).
3. Paginakop: `<div class="gl-page-header">` + `_PageHeader` (+ acties indien nodig).
4. `Html.BeginForm(… new { @class = "ecommerce-form gl-project-form", enctype = "multipart/form-data" })` + `@Html.AntiForgeryToken()`.
5. `asp-validation-summary="All"` bovenaan.
6. `card card-modern gl-form-shell` → één of meer `gl-form-shell__panel` → per blok een `gl-form-section` met `__head` (icoon + titel + hint).
7. Velden in `gl-field-grid` → `gl-field` (+ `--full` / `--wide` waar nodig), telkens label boven, `form-control-modern`, `asp-validation-for`.
8. Lang formulier? Tabstrip + `_ProjectFormTabs`-mechanisme + veld→tab-map + foutteller.
9. `_FormShellActions` met `SubmitLabel` + `CancelUrl`.
10. `@section PageScripts`: `_ValidationScriptsPartial` + `messages_nl.js`; `CurrencyMask.init`, select2- en `ios7Switch`-init voor de velden die je gebruikt; dirty-tracking + dubbele-submit-blokkade.
11. Test op 360 px, met tab-toetsnavigatie, en met een bewust foute POST (springt de juiste tab open, staat de fout bij het veld?).
