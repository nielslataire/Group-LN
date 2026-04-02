## CSS & JS Structuur (VERPLICHT TE VOLGEN)

Binnen dit project moet alle styling en scripting gecentraliseerd worden. Inline CSS of JS is NIET toegestaan tenzij expliciet gevraagd.

---

### 📁 Locaties

- Algemene CSS: `/wwwroot/css/custom.css`
- Pagina-specifieke CSS: `/wwwroot/css/{pagina-naam}.css`
- Algemene JS: `/wwwroot/js/site.js`
- Pagina-specifieke JS: `/wwwroot/js/{pagina-naam}.js`

---

### 🎨 CSS Richtlijnen

1. **Algemene styling**
   - Alle globale styles (layout, kleuren, componenten, herbruikbare classes) moeten in:
     ```
     /wwwroot/css/custom.css
     ```
2. 
2. **Pagina-specifieke styling**
   - Indien styling enkel voor één pagina is:
     ```
     /wwwroot/css/{pagina}.css
     ```
   - Indien voor een beperkte set pagina’s:
     → gebruik een logische verzamelnaam (bv. `projects.css`, `issues.css`)

3. **Integratie in Razor (VERPLICHT)**
   - CSS moet via de `PageStyle` section geladen worden:

```cshtml
@section PageStyle {
    <link rel="stylesheet" href="~/css/{pagina}.css" />
}
```

4. **Integratie in Razor (VERPLICHT)**
   - JS moet via de `PageScript` section geladen worden:

```cshtml
@section PageScript {
    <script src="~/js/{pagina}.js"></script>
}
```
5. **Naming**
    - alle css classes moeten starten met gl-:
         ```
         Gebruik kebab-case:
            gl-project-card
            gl-kpi-strip
            gl-floating-action
        CSS en JS bestanden:
            project-detail.css
            projects-issues.js
        ```