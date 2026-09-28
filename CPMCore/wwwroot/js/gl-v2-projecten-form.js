// gl-v2 — Projecten/EditV2 + ToevoegenV2 (design-handoff punt 19 "Project bewerken"). Eigen pagina-JS
// (zelfde "elke formulierpagina houdt haar eigen kopie"-afspraak als gl-v2-klanten-editproject.js —
// de patronen zijn daar opgelost en hier overgenomen, niet opnieuw uitgevonden): tabs, zoekende
// keuzelijsten (gemeente/weerstation/bedrijven), verplichte-veldencontrole, dirty-badge +
// "wijzigingen niet opslaan"-modal, foto-voorbeeld, SEO-tellers + Google-voorbeeld, Quill, en de
// coördinatie-logica (contracttype, schijven = 100 %, uurtarieven). Keuzelijsten uit
// Views/Shared/GlV2/_SelectList worden door gl-v2-shell.js se initGlV2Select() bediend. Elk onderdeel
// bewaakt zichzelf met een element-check, zodat ToevoegenV2 (geen tabs/SEO/coördinatie) hetzelfde
// bestand gebruikt.
(function () {
    "use strict";

    var config = window.glV2ProjectFormConfig || {};
    var form = document.getElementById("gl-v2-project-form");
    if (!form) return;

    var backdrop = null;
    var activateTab = null;

    // ── Gedeelde paneel-helpers voor de zoekende keuzelijsten ───────────────────────────────────
    function getBackdrop() {
        if (backdrop) return backdrop;
        backdrop = document.createElement("div");
        backdrop.className = "gl-v2-select-backdrop";
        document.body.appendChild(backdrop);
        backdrop.addEventListener("click", closeAllPanels);
        return backdrop;
    }
    function closeAllPanels() {
        document.querySelectorAll("[data-gl-v2-search-select] .gl-v2-select-panel.is-open").forEach(function (p) { p.classList.remove("is-open"); });
        document.querySelectorAll("[data-gl-v2-search-select] .gl-v2-select-trigger.is-open").forEach(function (t) { t.classList.remove("is-open"); });
        if (backdrop) backdrop.classList.remove("is-open");
    }
    function positionPanel(trigger, panel) {
        if (window.innerWidth < 768) { panel.style.top = ""; panel.style.left = ""; panel.style.width = ""; return; }
        var rect = trigger.getBoundingClientRect();
        var width = Math.max(rect.width, 260);
        panel.style.width = width + "px";
        panel.style.left = Math.max(12, Math.min(rect.left, window.innerWidth - width - 12)) + "px";
        var top = rect.bottom + 8;
        panel.style.top = top + "px";
        panel.style.maxHeight = Math.max(160, window.innerHeight - top - 16) + "px";
    }
    function openPanel(trigger, panel) {
        closeAllPanels();
        panel.classList.add("is-open");
        trigger.classList.add("is-open");
        getBackdrop().classList.add("is-open");
        positionPanel(trigger, panel);
    }
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") closeAllPanels(); });
    window.addEventListener("resize", closeAllPanels);

    // Pas na de initialisatie tellen wijzigingen: Quill (innerHTML-vulling) en de kiezers vuren bij het
    // laden zelf events af, en die zijn geen bewerking van de gebruiker.
    var formReady = false;
    function markDirty() {
        if (!formReady) return;
        var badge = document.getElementById("gl-v2-dirty-badge");
        if (badge) badge.hidden = false;
    }
    function isFormDirty() {
        var badge = document.getElementById("gl-v2-dirty-badge");
        return !!badge && !badge.hidden;
    }

    // ── Tabs (alleen op EditV2) ─────────────────────────────────────────────────────────────────
    function initTabs() {
        var tabbar = document.getElementById("gl-v2-project-tabbar");
        if (!tabbar) return;

        function activate(key, updateHash) {
            var tab = tabbar.querySelector('.gl-v2-tabbar-tab[data-tab="' + key + '"]');
            if (!tab) key = "algemeen";
            tabbar.querySelectorAll(".gl-v2-tabbar-tab").forEach(function (t) {
                var on = t.getAttribute("data-tab") === key;
                t.classList.toggle("is-active", on);
                t.setAttribute("aria-selected", on ? "true" : "false");
            });
            document.querySelectorAll(".gl-v2-tab-panel").forEach(function (p) { p.hidden = p.getAttribute("data-tab-panel") !== key; });
            closeAllPanels();
            if (updateHash) { try { history.replaceState(null, "", "#" + key); } catch (e) { /* geen history-API */ } }
        }
        activateTab = function (key) { activate(key, false); };

        tabbar.addEventListener("click", function (e) {
            var tab = e.target.closest(".gl-v2-tabbar-tab");
            if (tab) activate(tab.getAttribute("data-tab"), true);
        });

        var known = Array.prototype.map.call(tabbar.querySelectorAll(".gl-v2-tabbar-tab"), function (t) { return t.getAttribute("data-tab"); });
        var fromHash = (location.hash || "").replace("#", "");
        var initial = config.forceTab && known.indexOf(config.forceTab) !== -1 ? config.forceTab
            : (known.indexOf(fromHash) !== -1 ? fromHash : known[0]);
        activate(initial, false);

        if (config.forceTab) {
            var panel = document.querySelector('.gl-v2-tab-panel[data-tab-panel="' + config.forceTab + '"]');
            var bad = panel && panel.querySelector(".gl-v2-field.is-error");
            if (bad && bad.scrollIntoView) bad.scrollIntoView({ block: "center" });
        }
    }

    function tabKeyOf(el) {
        var panel = el.closest(".gl-v2-tab-panel");
        return panel ? panel.getAttribute("data-tab-panel") : null;
    }
    function refreshTabDot(tabKey) {
        if (!tabKey) return;
        var tab = document.querySelector('.gl-v2-tabbar-tab[data-tab="' + tabKey + '"]');
        var panel = document.querySelector('.gl-v2-tab-panel[data-tab-panel="' + tabKey + '"]');
        if (!tab || !panel) return;
        var dot = tab.querySelector('[data-role="error-dot"]');
        var slicesErr = panel.querySelector("#slicesTotalError");
        var has = !!panel.querySelector(".gl-v2-field.is-error") || !!(slicesErr && !slicesErr.hidden);
        if (dot) dot.hidden = !has;
    }

    // ── Zoekende keuzelijsten ───────────────────────────────────────────────────────────────────
    function wireSearchSelect(root) {
        if (root.hasAttribute("data-gl-v2-wired")) return;
        var idHidden = root.querySelector('[data-role="id-hidden"]');
        var textHidden = root.querySelector('[data-role="text-hidden"]');
        var trigger = root.querySelector(".gl-v2-select-trigger");
        var label = root.querySelector(".gl-v2-select-trigger-label");
        var clearTrigger = root.querySelector('[data-role="clear-trigger"]');
        var panel = root.querySelector('[data-role="panel"]');
        if (!idHidden || !trigger || !label || !panel) return;
        root.setAttribute("data-gl-v2-wired", "1");

        var url = root.getAttribute("data-lookup-url");
        var countrySource = root.getAttribute("data-country-source");
        var minChars = parseInt(root.getAttribute("data-min-chars") || "2", 10);
        var placeholder = root.getAttribute("data-search-placeholder") || "Zoek …";
        var timer = null;

        panel.innerHTML =
            '<div class="gl-v2-select-search"><div class="gl-v2-select-search-field">' +
            '<i class="ph ph-magnifying-glass" aria-hidden="true"></i>' +
            '<input type="text" data-role="input" autocomplete="off" />' +
            '<i class="ph ph-x gl-v2-select-search-clear" data-role="clear" hidden aria-hidden="true"></i>' +
            '</div></div><div class="gl-v2-select-options" data-role="results"></div>';
        var searchInput = panel.querySelector('[data-role="input"]');
        var clearBtn = panel.querySelector('[data-role="clear"]');
        var results = panel.querySelector('[data-role="results"]');
        searchInput.placeholder = placeholder;

        function hint(text) { results.innerHTML = '<div class="gl-v2-select-hint-footer">' + text + "</div>"; }
        function currentId() { return idHidden.value && idHidden.value !== "0" ? idHidden.value : ""; }

        function choose(id, text) {
            idHidden.value = id || "0";
            if (textHidden) textHidden.value = text || "";
            label.textContent = text || placeholder;
            trigger.classList.toggle("is-filled", !!id);
            var field = root.closest(".gl-v2-field");
            if (field) { field.classList.remove("is-error"); trigger.classList.remove("is-error"); var h = field.querySelector('[data-role="help"]'); if (h && field.hasAttribute("data-gl-v2-required")) h.hidden = true; }
            idHidden.dispatchEvent(new Event("change", { bubbles: true }));
            closeAllPanels();
            refreshTabDot(tabKeyOf(root));
        }

        function renderResults(items) {
            results.innerHTML = "";
            if (!items || !items.length) {
                results.innerHTML = '<div class="gl-v2-select-empty-suggestion"><span class="gl-v2-select-empty-suggestion-msg">Niets gevonden.</span></div>';
                return;
            }
            items.forEach(function (item) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "gl-v2-select-option" + (String(currentId()) === String(item.id) ? " is-selected" : "");
                btn.innerHTML = '<i class="ph ph-check" aria-hidden="true"></i><span></span>';
                btn.querySelector("span").textContent = item.text;
                btn.addEventListener("click", function () { choose(item.id, item.text); });
                results.appendChild(btn);
            });
        }

        function search(term) {
            clearBtn.hidden = term.length === 0;
            if (!term) { hint("Typ om te zoeken …"); return; }
            if (term.length < minChars) { hint("Typ nog minstens " + (minChars - term.length) + " teken(s) …"); return; }
            hint("Zoeken …");
            var body = new URLSearchParams();
            body.set("term", term);
            if (countrySource) {
                var c = document.querySelector(countrySource);
                body.set("countryId", c ? c.value : "");
            }
            fetch(url, { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body: body.toString() })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(renderResults)
                .catch(function () { renderResults([]); });
        }

        searchInput.addEventListener("click", function (e) { e.stopPropagation(); });
        searchInput.addEventListener("input", function () {
            window.clearTimeout(timer);
            var term = searchInput.value.trim();
            timer = window.setTimeout(function () { search(term); }, 300);
        });
        clearBtn.addEventListener("click", function (e) { e.stopPropagation(); searchInput.value = ""; search(""); searchInput.focus(); });

        function openThis() {
            openPanel(trigger, panel);
            hint("Typ om te zoeken …");
            searchInput.value = "";
            clearBtn.hidden = true;
            searchInput.focus();
        }
        trigger.addEventListener("click", function (e) {
            if (e.target.closest('[data-role="clear-trigger"]')) return;
            if (panel.classList.contains("is-open")) closeAllPanels(); else openThis();
        });
        trigger.addEventListener("keydown", function (e) {
            if (e.target.closest('[data-role="clear-trigger"]')) return;
            if (e.key !== "Enter" && e.key !== " ") return;
            e.preventDefault();
            if (panel.classList.contains("is-open")) closeAllPanels(); else openThis();
        });
        if (clearTrigger) clearTrigger.addEventListener("click", function (e) { e.stopPropagation(); choose("", ""); });
    }

    // ── Verplichte velden (client-side vangnet; de server blijft de bron van waarheid) ─────────
    function resolveField(marker) { return marker.classList.contains("gl-v2-field") ? marker : (marker.querySelector(".gl-v2-field") || marker); }
    function isEmpty(marker) {
        var idHidden = marker.querySelector('[data-role="id-hidden"]');
        if (idHidden) return !(parseInt(idHidden.value, 10) > 0);
        var input = marker.querySelector("input:not([type=hidden]), select, textarea");
        return !input || !String(input.value || "").trim();
    }
    function setFieldError(marker, message, invalid) {
        var field = resolveField(marker);
        field.classList.toggle("is-error", invalid);
        var trig = field.querySelector(".gl-v2-select-trigger");
        if (trig) trig.classList.toggle("is-error", invalid);
        var help = field.querySelector('[data-role="help"]');
        if (help) { help.textContent = invalid ? message : ""; help.hidden = !invalid; }
    }
    function validateRequired() {
        var first = null;
        document.querySelectorAll("[data-gl-v2-required]").forEach(function (marker) {
            var invalid = isEmpty(marker);
            setFieldError(marker, marker.getAttribute("data-gl-v2-required"), invalid);
            if (invalid && !first) first = marker;
        });
        document.querySelectorAll(".gl-v2-tabbar-tab").forEach(function (t) { refreshTabDot(t.getAttribute("data-tab")); });
        return first;
    }
    form.addEventListener("focusout", function (e) {
        var marker = e.target.closest("[data-gl-v2-required]");
        if (!marker) return;
        setFieldError(marker, marker.getAttribute("data-gl-v2-required"), isEmpty(marker));
        refreshTabDot(tabKeyOf(marker));
    });

    // ── Coördinatie: contracttype, schijven (= 100 %), uurtarieven ─────────────────────────────
    function initCoordination() {
        var card = document.getElementById("coordinationCard");
        if (!card) return;
        var toggle = document.getElementById("Project_IsCoordinationProject");
        var typeInput = document.getElementById("Project_ContractType");
        var slicesSection = document.getElementById("slicesSection");
        var regieSection = document.getElementById("regieSection");

        function updateTotal() {
            var total = 0;
            document.querySelectorAll(".slice-pct").forEach(function (i) { total += parseFloat(i.value) || 0; });
            var rounded = Math.round(total * 100) / 100;
            var totalEl = document.getElementById("slicesTotal");
            if (totalEl) totalEl.textContent = String(rounded).replace(".", ",");
            var fill = document.getElementById("slicesProgressFill");
            if (fill) {
                fill.style.transform = "scaleX(" + (Math.min(Math.max(rounded, 0), 100) / 100) + ")";
                fill.classList.toggle("is-complete", rounded === 100);
            }
            var chip = document.getElementById("gl-v2-pf-slices-chip");
            if (chip) {
                chip.textContent = rounded === 100 ? "KLOPT" : "TOTAAL ≠ 100 %";
                chip.classList.toggle("is-positive", rounded === 100);
                chip.classList.toggle("is-attention", rounded !== 100);
            }
            return rounded;
        }
        function updateVisibility() {
            var isCoord = !toggle || toggle.checked;
            card.hidden = !isCoord;
            var type = typeInput ? typeInput.value : "";
            if (slicesSection) slicesSection.hidden = !(type === "1" || type === "3");
            if (regieSection) regieSection.hidden = !(type === "2" || type === "3");
            updateTotal();
        }

        if (toggle) toggle.addEventListener("change", updateVisibility);
        var seg = document.getElementById("gl-v2-pf-contracttype");
        if (seg && typeInput) {
            seg.addEventListener("click", function (e) {
                var btn = e.target.closest(".gl-v2-pf-seg-btn");
                if (!btn) return;
                var same = btn.classList.contains("is-active");
                seg.querySelectorAll(".gl-v2-pf-seg-btn").forEach(function (b) { b.classList.remove("is-active"); b.setAttribute("aria-checked", "false"); });
                typeInput.value = same ? "" : btn.getAttribute("data-value");
                if (!same) { btn.classList.add("is-active"); btn.setAttribute("aria-checked", "true"); }
                updateVisibility();
                markDirty();
            });
        }
        document.addEventListener("input", function (e) { if (e.target.classList && e.target.classList.contains("slice-pct")) updateTotal(); });

        function esc(s) { return String(s == null ? "" : s).replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;"); }

        // De volgorde van de schijven is betekenisvol (schijf 1 = eerste factuur…): ze wordt als SortOrder
        // bewaard uit de positie in de lijst, dus namen altijd hernummeren na verplaatsen/verwijderen.
        var slicesContainer = document.getElementById("slicesContainer");
        function sliceRows() { return slicesContainer ? Array.prototype.slice.call(slicesContainer.querySelectorAll(".slice-row")) : []; }
        function renumberSlices() {
            var rows = sliceRows();
            rows.forEach(function (row, i) {
                row.querySelectorAll("input[name^='ContractSlices[']").forEach(function (inp) {
                    inp.name = inp.name.replace(/^ContractSlices\[\d+\]/, "ContractSlices[" + i + "]");
                });
                var up = row.querySelector(".move-slice-up");
                var down = row.querySelector(".move-slice-down");
                if (up) up.disabled = i === 0;
                if (down) down.disabled = i === rows.length - 1;
            });
        }
        renumberSlices();

        var addSlice = document.getElementById("addSlice");
        if (addSlice) addSlice.addEventListener("click", function () {
            var i = sliceRows().length;
            var row = document.createElement("div");
            row.className = "gl-v2-pf-row slice-row";
            row.innerHTML =
                '<input type="hidden" name="ContractSlices[' + i + '].Id" value="0" />' +
                '<span class="gl-v2-pf-handle" title="Sleep om te herschikken" aria-hidden="true"><i class="ph ph-dots-six-vertical"></i></span>' +
                '<div class="gl-v2-field gl-v2-pf-row-main"><div class="gl-v2-field-box"><input type="text" name="ContractSlices[' + i + '].Description" class="gl-v2-field-input" placeholder=" " autocomplete="off" aria-label="Omschrijving schijf" /></div></div>' +
                '<div class="gl-v2-field gl-v2-pf-row-num"><div class="gl-v2-field-box"><input type="number" step="0.01" min="0.01" max="100" name="ContractSlices[' + i + '].Percentage" class="gl-v2-field-input is-tabular slice-pct" placeholder=" " aria-label="Percentage schijf" /><span class="gl-v2-field-suffix">%</span></div></div>' +
                '<div class="gl-v2-pf-row-actions">' +
                '<button type="button" class="gl-v2-icon-btn move-slice-up" aria-label="Schijf omhoog" title="Omhoog"><i class="ph ph-arrow-up" aria-hidden="true"></i></button>' +
                '<button type="button" class="gl-v2-icon-btn move-slice-down" aria-label="Schijf omlaag" title="Omlaag"><i class="ph ph-arrow-down" aria-hidden="true"></i></button>' +
                '<button type="button" class="gl-v2-icon-btn is-danger remove-slice" aria-label="Schijf verwijderen"><i class="ph ph-trash" aria-hidden="true"></i></button>' +
                '</div>';
            slicesContainer.appendChild(row);
            renumberSlices();
            updateTotal();
            markDirty();
        });
        document.addEventListener("click", function (e) {
            var del = e.target.closest(".remove-slice");
            if (del) {
                del.closest(".slice-row").remove();
                renumberSlices();
                updateTotal();
                markDirty();
                return;
            }
            var mv = e.target.closest(".move-slice-up, .move-slice-down");
            if (!mv || mv.disabled) return;
            var row = mv.closest(".slice-row");
            var isUp = mv.classList.contains("move-slice-up");
            if (isUp) {
                if (row.previousElementSibling) slicesContainer.insertBefore(row, row.previousElementSibling);
            } else if (row.nextElementSibling) {
                slicesContainer.insertBefore(row.nextElementSibling, row);
            }
            renumberSlices();
            markDirty();
            var again = row.querySelector(isUp ? ".move-slice-up" : ".move-slice-down");
            if (again && !again.disabled) again.focus(); else mv.blur();
        });

        // Slepen (muis): enkel via het handvat, zodat tekst selecteren in de invoervelden blijft werken.
        // Touch gebruikt de pijltjes — HTML5-drag werkt niet op tablet/gsm.
        var dragRow = null;
        if (slicesContainer) {
            slicesContainer.addEventListener("mousedown", function (e) {
                var row = e.target.closest(".slice-row");
                if (!row) return;
                row.draggable = !!e.target.closest(".gl-v2-pf-handle");
            });
            slicesContainer.addEventListener("dragstart", function (e) {
                var row = e.target.closest && e.target.closest(".slice-row");
                if (!row || !row.draggable) return;
                dragRow = row;
                row.classList.add("is-dragging");
                e.dataTransfer.effectAllowed = "move";
                try { e.dataTransfer.setData("text/plain", "slice"); } catch (err) { }
            });
            slicesContainer.addEventListener("dragover", function (e) {
                if (!dragRow) return;
                e.preventDefault();
                var over = e.target.closest(".slice-row");
                if (!over || over === dragRow) return;
                var rect = over.getBoundingClientRect();
                var after = e.clientY > rect.top + rect.height / 2;
                slicesContainer.insertBefore(dragRow, after ? over.nextElementSibling : over);
            });
            slicesContainer.addEventListener("drop", function (e) { if (dragRow) e.preventDefault(); });
            slicesContainer.addEventListener("dragend", function () {
                if (!dragRow) return;
                dragRow.classList.remove("is-dragging");
                dragRow.draggable = false;
                dragRow = null;
                renumberSlices();
                markDirty();
            });
        }

        var rateIndex = document.querySelectorAll("#ratesContainer .rate-row").length;
        var users = config.availableUsers || [];
        var addRate = document.getElementById("addRate");
        if (addRate) addRate.addEventListener("click", function () {
            var i = rateIndex++;
            var options = '<option value="">— Selecteer persoon —</option>' + users.map(function (u) { return '<option value="' + esc(u.id) + '">' + esc(u.text) + "</option>"; }).join("");
            var row = document.createElement("div");
            row.className = "gl-v2-pf-row rate-row";
            row.innerHTML =
                '<div class="gl-v2-field gl-v2-pf-row-main"><div class="gl-v2-field-box"><select name="HourlyRates[' + i + '].UserId" class="gl-v2-field-input rate-user" aria-label="Persoon">' + options + "</select></div>" +
                '<input type="hidden" name="HourlyRates[' + i + '].UserFullName" value="" /></div>' +
                '<div class="gl-v2-field gl-v2-pf-row-num"><div class="gl-v2-field-box"><span class="gl-v2-field-prefix">€</span><input type="number" step="0.01" min="0" name="HourlyRates[' + i + '].HourlyRate" class="gl-v2-field-input is-tabular" placeholder=" " aria-label="Uurtarief" /></div></div>' +
                '<button type="button" class="gl-v2-icon-btn is-danger remove-rate" aria-label="Tarief verwijderen"><i class="ph ph-trash" aria-hidden="true"></i></button>';
            document.getElementById("ratesContainer").appendChild(row);
            markDirty();
        });
        document.addEventListener("change", function (e) {
            if (!e.target.classList || !e.target.classList.contains("rate-user")) return;
            var name = e.target.options[e.target.selectedIndex];
            e.target.closest(".rate-row").querySelector('input[name*="UserFullName"]').value = name && name.value ? name.text : "";
        });
        document.addEventListener("click", function (e) {
            var del = e.target.closest(".remove-rate");
            if (!del) return;
            del.closest(".rate-row").remove();
            document.querySelectorAll("#ratesContainer .rate-row").forEach(function (row, i) {
                row.querySelector("select").name = "HourlyRates[" + i + "].UserId";
                row.querySelector('input[name*="UserFullName"]').name = "HourlyRates[" + i + "].UserFullName";
                row.querySelector('input[name*="HourlyRate"]').name = "HourlyRates[" + i + "].HourlyRate";
            });
            rateIndex = document.querySelectorAll("#ratesContainer .rate-row").length;
            markDirty();
        });

        updateVisibility();
        return updateTotal;
    }

    // ── Foto-voorbeeld + sleepzone ──────────────────────────────────────────────────────────────
    function initPhoto() {
        var input = document.getElementById("StandardFotoUpload");
        var zone = document.getElementById("gl-v2-pf-dropzone");
        if (!input || !zone) return;
        function showFile(file) {
            if (!file) return;
            var reader = new FileReader();
            reader.onload = function (ev) {
                var img = document.getElementById("projectStdFotoPreview");
                if (img) { img.src = ev.target.result; img.hidden = false; }
                var empty = document.getElementById("projectStdFotoEmpty");
                if (empty) empty.hidden = true;
            };
            reader.readAsDataURL(file);
            var title = document.getElementById("gl-v2-pf-dropzone-title");
            if (title) title.textContent = file.name;
        }
        input.addEventListener("change", function () { showFile(input.files && input.files[0]); });
        zone.addEventListener("dragover", function (e) { e.preventDefault(); zone.classList.add("is-drag-over"); });
        zone.addEventListener("dragleave", function () { zone.classList.remove("is-drag-over"); });
        zone.addEventListener("drop", function (e) {
            e.preventDefault();
            zone.classList.remove("is-drag-over");
            if (!e.dataTransfer.files.length) return;
            var dt = new DataTransfer();
            dt.items.add(e.dataTransfer.files[0]);
            input.files = dt.files;
            input.dispatchEvent(new Event("change", { bubbles: true }));
        });
    }

    // ── SEO: tekenteller + Google-voorbeeld ─────────────────────────────────────────────────────
    function initSeo() {
        var titleInput = document.getElementById("Project_SeoTitle");
        var descInput = document.getElementById("Project_SeoDescription");
        var serp = document.getElementById("gl-v2-pf-serp");
        if (!titleInput || !descInput) return;
        var titleCount = document.getElementById("gl-v2-pf-seotitle-count");
        var descCount = document.getElementById("gl-v2-pf-seodesc-count");

        function counter(el, len) {
            if (!el) return;
            var limit = parseInt(el.getAttribute("data-limit"), 10);
            el.textContent = len + " / " + limit;
            el.classList.toggle("is-over", len > limit);
        }
        function update() {
            counter(titleCount, titleInput.value.length);
            counter(descCount, descInput.value.length);
            if (!serp) return;
            var slug = serp.getAttribute("data-slug") || "";
            document.getElementById("gl-v2-pf-serp-url").textContent = "groupln.be › projecten" + (slug ? " › " + slug : "");
            var t = document.getElementById("gl-v2-pf-serp-title");
            var d = document.getElementById("gl-v2-pf-serp-desc");
            var title = titleInput.value.trim();
            var desc = descInput.value.trim();
            t.textContent = title || serp.getAttribute("data-fallback-title") || "";
            t.classList.toggle("is-auto", !title);
            d.textContent = desc ? (desc.length > 155 ? desc.slice(0, 155).trim() + " …" : desc) : "Wordt automatisch gegenereerd.";
            d.classList.toggle("is-auto", !desc);
        }
        titleInput.addEventListener("input", update);
        descInput.addEventListener("input", update);
        update();
    }

    function initQuill() {
        var container = document.getElementById("commercialTextEditorContainer");
        var area = document.getElementById("commercialTextEditor");
        if (!container || !area || typeof Quill === "undefined") return;
        var quill = new Quill(container, {
            theme: "snow",
            modules: { toolbar: [["bold", "italic", "underline"], [{ list: "ordered" }, { list: "bullet" }], ["link"], ["clean"]] }
        });
        // "silent": de startinhoud is geen gebruikerswijziging (innerHTML-toewijzing liet de badge meteen verschijnen).
        if (area.value) quill.clipboard.dangerouslyPasteHTML(area.value, "silent");
        var baseline = quill.root.innerHTML;
        quill.on("text-change", function (delta, old, source) {
            area.value = quill.root.innerHTML;
            if (source === "user" && quill.root.innerHTML !== baseline) markDirty();
        });
    }

    // ── Werfmelding: "nog N maanden geldig" ─────────────────────────────────────────────────────
    function initWerfmelding() {
        var input = document.getElementById("Project_WerfmeldingEndDate");
        var out = document.getElementById("gl-v2-pf-werfmelding-months");
        if (!input || !out) return;
        function update() {
            if (!input.value) { out.hidden = true; return; }
            var end = new Date(input.value + "T00:00:00");
            if (isNaN(end.getTime())) { out.hidden = true; return; }
            var now = new Date();
            var months = (end.getFullYear() - now.getFullYear()) * 12 + (end.getMonth() - now.getMonth()) - (end.getDate() < now.getDate() ? 1 : 0);
            out.textContent = months < 0 ? "verlopen" : (months === 0 ? "verloopt binnen de maand" : "nog " + months + " maand" + (months === 1 ? "" : "en") + " geldig");
            out.hidden = false;
        }
        input.addEventListener("input", update);
        input.addEventListener("change", update);
        update();
    }

    // ── Verplichte documenten: "N / M aanwezig" ─────────────────────────────────────────────────
    function initDocCount() {
        var counter = document.getElementById("gl-v2-pf-doc-count");
        if (!counter) return;
        function update() {
            var required = 0, present = 0;
            document.querySelectorAll(".gl-v2-pf-docrow").forEach(function (row) {
                var cb = row.querySelector(".js-gl-v2-pf-doc-required");
                if (cb && cb.checked) { required++; if (row.getAttribute("data-delivered") === "1") present++; }
            });
            counter.innerHTML = "<b>" + present + "</b> / " + required + " AANWEZIG";
        }
        document.addEventListener("change", function (e) { if (e.target.classList && e.target.classList.contains("js-gl-v2-pf-doc-required")) update(); });
    }

    // ── Dirty-badge, annuleren-modal, opslaan ───────────────────────────────────────────────────
    // Zoektermen in een keuzelijst-paneel zijn geen wijziging aan het project.
    function onFormEdit(e) { if (e.target.matches && e.target.matches('[data-role="input"]')) return; markDirty(); }
    form.addEventListener("input", onFormEdit);
    form.addEventListener("change", onFormEdit);
    window.addEventListener("beforeunload", function (e) {
        if (!isFormDirty()) return;
        e.preventDefault();
        e.returnValue = "";
    });

    function initDiscardModal() {
        var modalEl = document.getElementById("gl-v2-discard-changes-modal");
        var confirmLink = document.getElementById("gl-v2-discard-changes-confirm");
        var triggers = [document.getElementById("gl-v2-cancel-link"), document.getElementById("gl-v2-topbar-back-link")].filter(Boolean);
        if (!triggers.length || !modalEl || !confirmLink || !window.bootstrap) return;
        confirmLink.href = triggers[0].href;
        triggers.forEach(function (t) {
            t.addEventListener("click", function (e) {
                if (!isFormDirty()) return;
                e.preventDefault();
                window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
            });
        });
        // Wie kiest "Niet opslaan" mag zonder browserwaarschuwing weg.
        confirmLink.addEventListener("click", function () { var b = document.getElementById("gl-v2-dirty-badge"); if (b) b.hidden = true; });
    }

    var recomputeSlices = null;
    form.addEventListener("submit", function (e) {
        var firstInvalid = validateRequired();
        var slicesOk = true;
        var slicesSection = document.getElementById("slicesSection");
        var slicesErr = document.getElementById("slicesTotalError");
        if (slicesSection && !slicesSection.hidden && !slicesSection.closest("[hidden]")) {
            var total = 0;
            document.querySelectorAll(".slice-pct").forEach(function (i) { total += parseFloat(i.value) || 0; });
            slicesOk = Math.round(total * 100) / 100 === 100;
            if (slicesErr) slicesErr.hidden = slicesOk;
            refreshTabDot("coordinatie");
        }
        if (firstInvalid || !slicesOk) {
            e.preventDefault();
            e.stopImmediatePropagation();
            var target = firstInvalid || slicesSection;
            var key = tabKeyOf(target);
            if (activateTab && key) activateTab(key);
            (resolveField(target) || target).scrollIntoView({ behavior: "smooth", block: "center" });
            return;
        }
        var submit = document.getElementById("gl-v2-project-submit");
        if (submit) { submit.classList.add("is-loading"); submit.disabled = true; }
        var badge = document.getElementById("gl-v2-dirty-badge");
        if (badge) badge.hidden = true;
    });

    var gotoFirst = document.getElementById("gl-v2-goto-first-error");
    if (gotoFirst) gotoFirst.addEventListener("click", function () {
        var err = document.querySelector(".gl-v2-field.is-error");
        if (!err) return;
        var key = tabKeyOf(err);
        if (activateTab && key) activateTab(key);
        err.scrollIntoView({ behavior: "smooth", block: "center" });
    });

    // ── Init ────────────────────────────────────────────────────────────────────────────────────
    initTabs();
    document.querySelectorAll("[data-gl-v2-search-select]").forEach(wireSearchSelect);
    recomputeSlices = initCoordination();
    initPhoto();
    initSeo();
    initQuill();
    initWerfmelding();
    initDocCount();
    initDiscardModal();
    // Na de eerste tick: alles wat de initialisatie zelf afvuurde (Quill, kiezers) is dan voorbij.
    window.setTimeout(function () { formReady = true; }, 0);
})();
