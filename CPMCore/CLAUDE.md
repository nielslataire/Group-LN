# CPMCore

## Ontwerp: altijd eerst `DESIGN.md`

Voor **elke** CPMCore-pagina, partial of modal (nieuw of aangepast) is [`DESIGN.md`](DESIGN.md) de bron voor het ontwerp: componenten (`.gl-v2-*`, `Views/Shared/GlV2/`), kleuren en tokens (`--gl-v2-*`), veldcontract, CSS-laadvolgorde, animaties en mappenstructuur. Lees de relevante sectie vóór je HTML/CSS schrijft en bouw met de bestaande componenten in plaats van eigen varianten. Weet je niet welk component past, zoek het eerst in `DESIGN.md`.

## Help: elke pagina of aanpassing krijgt ook een help-update

Bij **elke** nieuwe pagina of functie, en bij **elke** wijziging die zichtbaar gedrag verandert (velden, knoppen, stappen, regels, teksten), pas je in dezelfde taak ook het Helpcentrum aan. Volg daarvoor de staande regel bovenaan `DESIGN.md` en de sectie "Helpcentrum — design-handoff punt 41" (frontmatter, `parent:`/`treeOrder`/`treeLabel`, `screens:`):

- Nieuwe pagina of functie: nieuw artikel op de juiste plek in de boom.
- Gewijzigde pagina of functie: bestaand artikel bijwerken (stappen, uitleg, schermafbeelding-verwijzingen). Verwijderd gedrag haal je ook uit de help.
- Lees vóór het schrijven altijd `HELP_SCHRIJFGIDS.md` (toon, vaste opbouw, woordenlijst, bedrijfslogica). Weet je iets niet zeker over bedrijfslogica, zet dan `[VRAAG: …]` in het artikel en noteer de vraag in `HELP_VRAGEN.md`; presenteer het niet als feit.

### Schermafbeeldingen: noteren, niet meteen nemen

Neem tijdens het bouwen **geen** screenshots voor de help. Schrijf op welke er nog gemaakt moeten worden, zodat ze later in één batch met Playwright genomen worden volgens het recept "Schermafbeeldingen maken" in `DESIGN.md` (eigen profiel, cookie `gl_v2_preview=1`, uitsnijden, echte namen vervangen).

- Voeg per gewenste afbeelding een rij toe aan `HELP_SCREENSHOTS.md`, met dezelfde kolommen als de bestaande tabel: artikel-slug, stap, pad (`CPMCore/wwwroot/img/help/<topic>/<slug>/<stap>-<naam>.png`), route/pagina, uitsnede, anonimiseren.
- Zet de `![...]`-verwijzing wel al in het artikel (eerste regel van de stap), zodat de afbeelding na de batch meteen op zijn plek staat.
- Wijzigt een pagina zo dat een bestaande afbeelding niet meer klopt, zet die dan opnieuw in de lijst met in de uitsnede-kolom "opnieuw nemen".
