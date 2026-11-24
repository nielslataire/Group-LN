# CPMCore

## Ontwerp: altijd eerst `DESIGN.md`

Voor **elke** CPMCore-pagina, partial of modal (nieuw of aangepast) is [`DESIGN.md`](DESIGN.md) de bron voor het ontwerp: componenten (`.gl-v2-*`, `Views/Shared/GlV2/`), kleuren en tokens (`--gl-v2-*`), veldcontract, CSS-laadvolgorde, animaties en mappenstructuur. Lees de relevante sectie vóór je HTML/CSS schrijft en bouw met de bestaande componenten in plaats van eigen varianten. Weet je niet welk component past, zoek het eerst in `DESIGN.md`.

## Help: elke pagina of aanpassing krijgt ook een help-update

Bij **elke** nieuwe pagina of functie, en bij **elke** wijziging die zichtbaar gedrag verandert (velden, knoppen, stappen, regels, teksten), pas je in dezelfde taak ook het Helpcentrum aan. Volg daarvoor de staande regel bovenaan `DESIGN.md` en de sectie "Helpcentrum — design-handoff punt 41" (frontmatter, `parent:`/`treeOrder`/`treeLabel`, `screens:`):

- Nieuwe pagina of functie: nieuw artikel op de juiste plek in de boom.
- Gewijzigde pagina of functie: bestaand artikel bijwerken (stappen, uitleg, schermafbeelding-verwijzingen). Verwijderd gedrag haal je ook uit de help.
- Lees vóór het schrijven altijd `HELP_SCHRIJFGIDS.md` (toon, vaste opbouw, woordenlijst, bedrijfslogica). Weet je iets niet zeker over bedrijfslogica, zet dan `[VRAAG: …]` in het artikel en noteer de vraag in `docs/archive/help/HELP_VRAGEN.md`; presenteer het niet als feit.

### Schermafbeeldingen: noteren, niet meteen nemen

Neem tijdens het bouwen **geen** screenshots voor de help. Schrijf op welke er nog gemaakt moeten worden, zodat ze later in één batch met Playwright genomen worden volgens het recept "Schermafbeeldingen maken" in `DESIGN.md` (eigen profiel, cookie `gl_v2_preview=1`, uitsnijden, echte namen vervangen).

- Voeg per gewenste afbeelding een rij toe aan `docs/archive/help/HELP_SCREENSHOTS.md`, met dezelfde kolommen als de bestaande tabel: artikel-slug, stap, pad (`CPMCore/wwwroot/img/help/<topic>/<slug>/<stap>-<naam>.png`), route/pagina, uitsnede, anonimiseren.
- Zet de `![...]`-verwijzing wel al in het artikel (eerste regel van de stap), zodat de afbeelding na de batch meteen op zijn plek staat.
- Wijzigt een pagina zo dat een bestaande afbeelding niet meer klopt, zet die dan opnieuw in de lijst met in de uitsnede-kolom "opnieuw nemen".

## Een nieuwe controller toevoegen: registreer hem op vier plekken

Een nieuwe MVC-controller met views in een submap (bv. `Views/Instellingen/BouwstaatAdmin/`) werkt pas als hij op **alle** onderstaande plekken staat. Vergeet je er één, dan krijg je "The view 'IndexV2' was not found" of een geblokkeerde pagina. Dit is al eens fout gelopen bij `BouwstaatAdmin` (24/11/2026); loop de lijst daarom altijd af voor je zegt dat een pagina klaar is.

1. **Views vinden:** `Helpers/CustomViewLocationExpander.cs`, woordenboek `ControllerFolders`: `["NaamZonderController"] = "Map/Onder/Views"`. Zonder deze regel zoekt Razor enkel in `Views/<Controller>/` en de vaste functiemappen, niet in `Views/Instellingen/...`.
2. **Recht:** `Services/Security/PermissionResolver.cs`: koppel de controllernaam aan een `PermissionCodes.*`. Zonder regel valt de controller op een standaardrecht of wordt de pagina voor niet-admins geblokkeerd. Heeft de pagina een eigen recht nodig, voeg het dan toe in `PermissionCodes`, `PermissionCatalog` en `PermissionResolver`.
3. **Services:** een nieuwe `IXxxService` in `FacadeCore`, de implementatie in `ServiceCore` en de registratie in `Program.cs` (`builder.Services.AddScoped<...>`). Anders faalt de controller bij het aanmaken.
4. **Toegang in de interface:** een kaart op de Instellingen-hub (`InstellingenController.Algemeen.cs`, `SettingsItemVm`) of een menu-item, met dezelfde permissiecontrole als de pagina zelf.

Zet ook altijd mee: `ViewData["HelpScreenId"]`, een help-artikel (zie hierboven) en, bij een lijst of editor in een sjabloonfamilie, de tab in `Views/Shared/GlV2/_SjabloonTabs.cshtml` als de pagina daarbij hoort.

Controle: bouw het project en **open de pagina daadwerkelijk** (of laat de gebruiker dat doen en vraag het expliciet). Compileren bewijst niet dat de view gevonden wordt.
