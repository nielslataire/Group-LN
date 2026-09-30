// gl-v2 layout-pilot — Klanten/EditProjectV2.cshtml ("klant bewerken vanuit een project"). Eigen
// pagina-JS (niet samengevoegd met gl-v2-klanten-form.js — ander model, andere tabs), maar de
// opgeloste patronen (zoekende/meervoudige select, schakelaar-gestuurd tonen/verbergen, rij
// toevoegen/verwijderen via fetch, dirty-badge, wijzigingen-niet-opslaan-modal, verplichte-
// veldencontrole, vaste actiebalk op mobiel) zijn hier overgenomen, niet opnieuw uitgevonden.
// window.glV2EditProjectConfig (EditProjectV2.cshtml) draagt de endpoint-urls + de server-bepaalde
// force-tab.
(function () {
    "use strict";

    var config = window.glV2EditProjectConfig || {};
    var backdrop = null;
    var activateTab = null;

    initTabs();
    initOwnerTypeTabToggle();
    initCurrencyFields();
    initMultiSelects(document);
    initSearchSelects(document);
    initClientTypeToggle();
    initSalutationFirstnameToggle(document);
    initInvoiceAddressToggle();
    initContactRows();
    initCoOwnerRows();
    initGiftRows();
    initPoaRows();
    initUblDependency(document);
    initRequiredValidation();
    initSubmitLoading();
    initGotoFirstError();
    initDirtyBadge();
    initDiscardChangesModal();
    initShareTotal();
    initInvoicingTab();

    function getBackdrop() {
        if (backdrop) return backdrop;
        backdrop = document.createElement("div");
        backdrop.className = "gl-v2-select-backdrop";
        document.body.appendChild(backdrop);
        backdrop.addEventListener("click", closeAllPanels);
        return backdrop;
    }

    function closeAllPanels() {
        document.querySelectorAll(".gl-v2-select-panel.is-open").forEach(function (panel) {
            panel.classList.remove("is-open");
        });
        document.querySelectorAll(".gl-v2-select-trigger.is-open").forEach(function (trigger) {
            trigger.classList.remove("is-open");
        });
        if (backdrop) backdrop.classList.remove("is-open");
    }

    function positionPanel(trigger, panel) {
        if (window.innerWidth < 768) {
            panel.style.top = "";
            panel.style.bottom = "";
            panel.style.left = "";
            panel.style.width = "";
            return;
        }
        var rect = trigger.getBoundingClientRect();
        var width = Math.max(rect.width, 260);
        panel.style.width = width + "px";
        var left = Math.min(rect.left, window.innerWidth - width - 12);
        panel.style.left = Math.max(12, left) + "px";
        // Onderaan te weinig plaats (trigger onderaan het scherm)? Dan naar bóven openklappen i.p.v.
        // buiten beeld te vallen — position:fixed, dus via bottom i.p.v. top. Projectwijd hetzelfde
        // in elke positionPanel-kopie.
        var spaceBelow = window.innerHeight - rect.bottom - 16;
        var spaceAbove = rect.top - 16;
        if (spaceBelow < 160 && spaceAbove > spaceBelow) {
            panel.style.top = "";
            panel.style.bottom = (window.innerHeight - rect.top + 8) + "px";
            panel.style.maxHeight = Math.max(160, spaceAbove - 8) + "px";
        } else {
            panel.style.bottom = "";
            panel.style.top = (rect.bottom + 8) + "px";
            panel.style.maxHeight = Math.max(160, spaceBelow) + "px";
        }
    }

    function openPanel(trigger, panel) {
        closeAllPanels();
        panel.classList.add("is-open");
        trigger.classList.add("is-open");
        getBackdrop().classList.add("is-open");
        positionPanel(trigger, panel);
    }

    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") closeAllPanels();
    });
    document.addEventListener("click", function (e) {
        if (e.target.closest(".gl-v2-select, .gl-v2-select-panel")) return;
        closeAllPanels();
    });
    window.addEventListener("resize", closeAllPanels);

    // ── Tabs ────────────────────────────────────────────────────────────────────────────────────
    function initTabs() {
        var tabbar = document.getElementById("gl-v2-editproject-tabbar");
        if (!tabbar) return;

        function activate(key) {
            var tab = tabbar.querySelector('.gl-v2-tabbar-tab[data-tab="' + key + '"]');
            if (!tab || tab.hidden) key = "algemeen";
            tabbar.querySelectorAll(".gl-v2-tabbar-tab").forEach(function (t) {
                var isActive = t.getAttribute("data-tab") === key;
                t.classList.toggle("is-active", isActive);
                t.setAttribute("aria-selected", isActive ? "true" : "false");
            });
            document.querySelectorAll(".gl-v2-tab-panel").forEach(function (panel) {
                panel.hidden = panel.getAttribute("data-tab-panel") !== key;
            });
        }
        activateTab = activate;

        tabbar.addEventListener("click", function (e) {
            var tab = e.target.closest(".gl-v2-tabbar-tab");
            if (!tab || tab.hidden) return;
            activate(tab.getAttribute("data-tab"));
        });

        if (config.forceTab && config.forceTab !== "algemeen") {
            activate(config.forceTab);
        }
    }

    // ── Mede-eigenaars-tab verbergen bij "particulier" (OwnerType 1) — zelfde
    //    toggleMedeEigenaarsTab()-gedrag als de legacy inline script. ─────────────────────────────
    function initOwnerTypeTabToggle() {
        var select = document.getElementById("Client_OwnerType_Id");
        var tab = document.getElementById("gl-v2-tab-medeeigenaars");
        if (!select || !tab) return;

        function apply() {
            var isParticulier = select.value === "1";
            var wasActive = tab.classList.contains("is-active");
            tab.hidden = isParticulier;
            if (isParticulier && wasActive && activateTab) activateTab("algemeen");
        }

        select.addEventListener("change", function () {
            apply();
            markDirty();
        });
        apply();
    }

    // ── Eenheden-tab: grond-/constructiewaarden — CurrencyMask.init() moet hier expliciet aangeroepen
    //    (currency.js initialiseert zichzelf niet), zelfde als elke andere .Currencymask-pagina in de
    //    app. Geen rijen toevoegen/verwijderen op deze tab, dus één init bij het laden volstaat. ─────
    function initCurrencyFields() {
        if (window.CurrencyMask) window.CurrencyMask.init(".Currencymask");
    }

    // ── Meervoudige kiezer (Activiteiten, zoekende variant) — herbruikbaar per root i.p.v. één keer
    //    over het hele document, zodat dynamisch toegevoegde Toegift-/Aandachtspunt-rijen 'm ook
    //    kunnen aanroepen. ───────────────────────────────────────────────────────────────────────
    function initMultiSelects(scope) {
        scope.querySelectorAll("[data-gl-v2-multiselect]").forEach(wireMultiSelect);
    }

    function wireMultiSelect(root) {
        if (root.hasAttribute("data-gl-v2-wired")) return;
        var trigger = root.querySelector(".gl-v2-select-trigger");
        var panel = root.querySelector(".gl-v2-select-panel");
        var chipsHost = root.querySelector('[data-role="chips"]');
        var filterInput = root.querySelector('[data-role="filter"]');
        var hiddenHost = root.querySelector('[data-role="hidden-inputs"]');
        var fieldName = root.getAttribute("data-field-name");
        if (!trigger || !panel || !chipsHost || !hiddenHost || !fieldName) return;
        root.setAttribute("data-gl-v2-wired", "1");

        function selectedIds() {
            return Array.prototype.map.call(hiddenHost.querySelectorAll('[data-role="hidden-input"]'), function (el) {
                return el.value;
            });
        }

        function isSelected(id) { return selectedIds().indexOf(id) !== -1; }

        function renderChips() {
            chipsHost.innerHTML = "";
            panel.querySelectorAll(".gl-v2-select-option.is-multi").forEach(function (opt) {
                var id = opt.getAttribute("data-id");
                opt.classList.toggle("is-selected", isSelected(id));
                if (!isSelected(id)) return;
                var chip = document.createElement("span");
                chip.className = "gl-v2-select-chip";
                var label = document.createElement("span");
                label.textContent = opt.getAttribute("data-text");
                chip.appendChild(label);
                var remove = document.createElement("button");
                remove.type = "button";
                remove.className = "gl-v2-select-chip-remove";
                remove.setAttribute("aria-label", "Verwijderen");
                remove.innerHTML = '<i class="ph ph-x" aria-hidden="true"></i>';
                remove.addEventListener("click", function (e) {
                    e.stopPropagation();
                    toggle(id);
                });
                chip.appendChild(remove);
                chipsHost.appendChild(chip);
            });
        }

        function toggle(id) {
            var existing = hiddenHost.querySelector('[data-role="hidden-input"][value="' + id + '"]');
            if (existing) {
                existing.remove();
            } else {
                var input = document.createElement("input");
                input.type = "hidden";
                input.name = fieldName;
                input.value = id;
                input.setAttribute("data-role", "hidden-input");
                hiddenHost.appendChild(input);
            }
            renderChips();
            markDirty();
        }

        panel.addEventListener("click", function (e) {
            var opt = e.target.closest(".gl-v2-select-option.is-multi");
            if (!opt) return;
            toggle(opt.getAttribute("data-id"));
        });

        trigger.addEventListener("click", function (e) {
            if (e.target.closest(".gl-v2-select-chip")) return;
            if (panel.classList.contains("is-open")) {
                closeAllPanels();
            } else {
                openPanel(trigger, panel);
                if (filterInput) filterInput.focus();
            }
        });

        if (filterInput) {
            filterInput.addEventListener("focus", function () {
                if (!panel.classList.contains("is-open")) openPanel(trigger, panel);
            });
            filterInput.addEventListener("input", function () {
                if (!panel.classList.contains("is-open")) openPanel(trigger, panel);
                var term = filterInput.value.trim().toLowerCase();
                panel.querySelectorAll(".gl-v2-select-option.is-multi").forEach(function (opt) {
                    var text = (opt.getAttribute("data-text") || "").toLowerCase();
                    opt.hidden = term.length > 0 && text.indexOf(term) === -1;
                });
            });
            filterInput.addEventListener("click", function (e) { e.stopPropagation(); });
        }

        renderChips();
    }

    // ── Zoekende dropdown (GlV2SearchSelect — postcode/gemeente). Deze pagina heeft geen BE/niet-BE-
    //    handmatige-invoertak (de legacy pagina had die hier ook nooit) — altijd zoeken, binnen het
    //    land dat op dat moment gekozen staat in de dichtstbijzijnde [data-gl-v2-address-block]. ────
    function initSearchSelects(scope) {
        scope.querySelectorAll("[data-gl-v2-search-select]").forEach(wireSearchSelect);
    }

    function wireSearchSelect(root) {
        if (root.hasAttribute("data-gl-v2-wired")) return;
        var hidden = root.querySelector('input[type="hidden"]');
        var trigger = root.querySelector(".gl-v2-select-trigger");
        var label = root.querySelector(".gl-v2-select-trigger-label");
        var clearTrigger = root.querySelector('[data-role="clear-trigger"]');
        var panel = root.querySelector('[data-role="panel"]');
        if (!hidden || !trigger || !label || !panel) return;
        root.setAttribute("data-gl-v2-wired", "1");

        var debounceTimer = null;
        var searchPlaceholder = root.getAttribute("data-search-placeholder") || "Zoek …";

        panel.innerHTML =
            '<div class="gl-v2-select-search">' +
            '<div class="gl-v2-select-search-field">' +
            '<i class="ph ph-magnifying-glass" aria-hidden="true"></i>' +
            '<input type="text" data-role="input" autocomplete="off" />' +
            '<i class="ph ph-x gl-v2-select-search-clear" data-role="clear" hidden aria-hidden="true"></i>' +
            "</div></div>" +
            '<div class="gl-v2-select-options" data-role="results"></div>';
        var searchInput = panel.querySelector('[data-role="input"]');
        var clearBtn = panel.querySelector('[data-role="clear"]');
        var results = panel.querySelector('[data-role="results"]');
        searchInput.placeholder = searchPlaceholder;

        function countryId() {
            var block = root.closest("[data-gl-v2-address-block]");
            var countrySelect = block && block.querySelector('[data-role="country-select"]');
            return countrySelect ? countrySelect.value : "";
        }

        function renderHint(text) {
            results.innerHTML = '<div class="gl-v2-select-hint-footer">' + text + "</div>";
        }

        function renderResults(items) {
            results.innerHTML = "";
            if (!items || !items.length) {
                var empty = document.createElement("div");
                empty.className = "gl-v2-select-empty-suggestion";
                empty.innerHTML = '<span class="gl-v2-select-empty-suggestion-msg">Niets gevonden.</span>';
                results.appendChild(empty);
                return;
            }
            items.forEach(function (item) {
                var isSelected = hidden.value && String(hidden.value) === String(item.id);
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "gl-v2-select-option" + (isSelected ? " is-selected" : "");
                var check = document.createElement("i");
                check.className = "ph ph-check";
                check.setAttribute("aria-hidden", "true");
                btn.appendChild(check);
                var textSpan = document.createElement("span");
                textSpan.textContent = item.text;
                btn.appendChild(textSpan);
                btn.addEventListener("click", function () {
                    hidden.value = item.id;
                    label.textContent = item.text;
                    trigger.classList.add("is-filled");
                    closeAllPanels();
                    markDirty();
                });
                results.appendChild(btn);
            });
        }

        function search(term) {
            clearBtn.hidden = term.length === 0;
            if (!term) {
                renderHint("Typ om te zoeken …");
                return;
            }
            if (term.length < 2) {
                renderHint("Typ nog minstens " + (2 - term.length) + " teken(s) …");
                return;
            }
            renderHint("Zoeken …");
            var body = new URLSearchParams();
            body.set("term", term);
            body.set("CountryId", countryId());
            fetch(config.postalLookupUrl, {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: body.toString()
            })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (items) { renderResults(items); })
                .catch(function () { renderResults([]); });
        }

        searchInput.addEventListener("click", function (e) { e.stopPropagation(); });
        searchInput.addEventListener("input", function () {
            window.clearTimeout(debounceTimer);
            var term = searchInput.value.trim();
            debounceTimer = window.setTimeout(function () { search(term); }, 300);
        });
        // Toetsenbord in de resultaten (projectwijd, zelfde blok in elke wireSearchSelect-kopie): ↓/↑
        // lopen door de zichtbare opties (.is-active), Enter kiest de actieve — of de eerste als er nog
        // geen actief is — en Escape sluit. Enter mag hier nooit het formulier indienen.
        searchInput.addEventListener("keydown", function (e) {
            if (e.key === "Escape") { closeAllPanels(); return; }
            if (e.key !== "ArrowDown" && e.key !== "ArrowUp" && e.key !== "Enter") return;
            var items = Array.prototype.filter.call(panel.querySelectorAll(".gl-v2-select-option"), function (b) { return !b.hidden; });
            if (e.key === "Enter") e.preventDefault();
            if (!items.length) return;
            var current = items.findIndex(function (b) { return b.classList.contains("is-active"); });
            if (e.key === "Enter") { items[current >= 0 ? current : 0].click(); return; }
            e.preventDefault();
            var next = e.key === "ArrowDown" ? (current + 1) % items.length : (current <= 0 ? items.length - 1 : current - 1);
            items.forEach(function (b, i) { b.classList.toggle("is-active", i === next); });
            items[next].scrollIntoView({ block: "nearest" });
        });
        clearBtn.addEventListener("click", function (e) {
            e.stopPropagation();
            searchInput.value = "";
            search("");
            searchInput.focus();
        });

        function openThisPanel() {
            openPanel(trigger, panel);
            renderHint("Typ om te zoeken …");
            searchInput.value = "";
            clearBtn.hidden = true;
            searchInput.focus();
        }

        // De focus-listener hieronder opent het paneel vaak al in dezelfde tik/klik; de click die daarop
        // volgt mag dat dan niet meteen weer sluiten. Een mousedown-vlag was daarvoor niet betrouwbaar:
        // op touch (gsm) komt focus per browser vóór óf ná de geëmuleerde mousedown, waardoor het veld
        // "niet openging". Nu telt de tijd: een click binnen 500 ms na het openen laat het paneel open,
        // een latere click op de open trigger sluit wel. Projectwijd hetzelfde in elke kopie.
        var openedAt = 0;
        trigger.addEventListener("click", function (e) {
            if (e.target.closest('[data-role="clear-trigger"]')) return;
            if (panel.classList.contains("is-open")) {
                if (Date.now() - openedAt > 500) closeAllPanels();
            } else {
                openedAt = Date.now();
                openThisPanel();
            }
        });
        trigger.addEventListener("keydown", function (e) {
            if (e.target.closest('[data-role="clear-trigger"]')) return;
            if (e.key !== "Enter" && e.key !== " ") return;
            e.preventDefault();
            if (panel.classList.contains("is-open")) {
                closeAllPanels();
            } else {
                openThisPanel();
            }
        });
        // Focus (bv. Tab erin) opent het paneel meteen mee, zodat je meteen kan typen zonder eerst nog
        // Enter/een klik nodig te hebben — zelfde discipline als de zoekende multiselect elders al kreeg.
        trigger.addEventListener("focus", function () {
            if (!panel.classList.contains("is-open")) { openedAt = Date.now(); openThisPanel(); }
        });

        if (clearTrigger) {
            clearTrigger.addEventListener("click", function (e) {
                e.stopPropagation();
                hidden.value = "";
                label.textContent = searchPlaceholder;
                trigger.classList.remove("is-filled");
                closeAllPanels();
                markDirty();
            });
        }
    }

    // ── Bedrijf/particulier-schakelaar (Identificatie-kaart, hoofdklant). ─────────────────────────
    function initClientTypeToggle() {
        var toggle = document.getElementById("gl-v2-is-company");
        if (!toggle) return;

        function apply() {
            document.querySelectorAll('[data-role="company-fields"]').forEach(function (el) {
                el.hidden = !toggle.checked;
            });
        }

        toggle.addEventListener("change", function () {
            apply();
            markDirty();
        });
        apply();
    }

    // ── "Voornaam" uitschakelen bij een gedeelde aanspreking ("Dhr. & Mevr."/"Dhr. & Dhr."/"Mevr. &
    //    Mevr.", Salutation-waarden 2/3/4) — dat is een account/mede-eigenaar voor meerdere personen,
    //    er is dan geen eigen voornaam. Werkt op de hoofdklant (één vast koppel op de pagina) én, via
    //    de scope-parameter, op elke (ook dynamisch toegevoegde) mede-eigenaar-rij — zie initCoOwnerRows.
    //    .gl-v2-field-input:disabled heeft al de grijze opmaak (gl-v2-shell.css). ─────────────────────
    function initSalutationFirstnameToggle(scope) {
        scope.querySelectorAll('[data-role="salutation-select"]').forEach(wireSalutationFirstnameToggle);
    }

    function wireSalutationFirstnameToggle(select) {
        if (select.hasAttribute("data-gl-v2-wired-firstname")) return;
        select.setAttribute("data-gl-v2-wired-firstname", "1");
        var fieldScope = select.closest(".gl-v2-field-group") || select.closest(".gl-v2-section-grid");
        var input = fieldScope && fieldScope.querySelector('[data-role="firstname-input"]');
        if (!input) return;

        function apply() {
            input.disabled = parseInt(select.value, 10) >= 2;
        }

        select.addEventListener("change", function () {
            apply();
            markDirty();
        });
        apply();
    }

    // ── "Afwijkend facturatieadres"-schakelaar (hoofdklant, singulier — de mede-eigenaars hebben
    //    elk hun eigen, zie initCoOwnerRows). ──────────────────────────────────────────────────────
    function initInvoiceAddressToggle() {
        var toggle = document.getElementById("gl-v2-use-invoice-address");
        var wrapper = document.querySelector('[data-role="invoice-address-wrapper"]');
        if (!toggle || !wrapper) return;

        function apply() { wrapper.hidden = !toggle.checked; }

        toggle.addEventListener("change", function () {
            apply();
            markDirty();
        });
        apply();
    }

    // ── UBL hangt af van digitale facturatie (contact-/mede-eigenaarsrijen). ──────────────────────
    function wireUblPair(pair) {
        var digital = pair.querySelector('[data-role="digital-invoice-toggle"]');
        var ubl = pair.querySelector('[data-role="ubl-toggle"]');
        if (!digital || !ubl) return;

        function apply() { ubl.disabled = !digital.checked; }

        digital.addEventListener("change", apply);
        apply();
    }

    function initUblDependency(scope) {
        scope.querySelectorAll('[data-role="ubl-pair"]').forEach(wireUblPair);
    }

    function updateTabCount(tabKey, container, rowSelector) {
        var tab = document.querySelector('.gl-v2-tabbar-tab[data-tab="' + tabKey + '"]');
        var countEl = tab && tab.querySelector(".gl-v2-tabbar-count");
        if (countEl) countEl.textContent = container.querySelectorAll(rowSelector).length;
    }

    // ── Contacten-rijen. ────────────────────────────────────────────────────────────────────────
    function initContactRows() {
        var rows = document.getElementById("gl-v2-contact-rows");
        var addBtn = document.getElementById("gl-v2-add-contact");
        if (!rows) return;

        if (addBtn) {
            addBtn.addEventListener("click", function (e) {
                e.preventDefault();
                fetch(config.addContactUrl)
                    .then(function (r) { return r.text(); })
                    .then(function (html) {
                        var fragment = document.createElement("div");
                        fragment.innerHTML = html;
                        var rowEl = fragment.firstElementChild;
                        if (!rowEl) return;
                        rows.appendChild(rowEl);
                        initSearchSelects(rowEl);
                        initUblDependency(rowEl);
                        if (window.GlV2Phone) window.GlV2Phone.init(rowEl);
                        updateTabCount("contacten", rows, ".gl-v2-client-row");
                        markDirty();
                    })
                    .catch(function () {
                        if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Fout", body: "Kon geen nieuw contact toevoegen." });
                    });
            });
        }

        rows.addEventListener("click", function (e) {
            var deleteBtn = e.target.closest(".js-gl-v2-delete-contact-row");
            if (deleteBtn) {
                deleteBtn.closest(".gl-v2-client-row").remove();
                updateTabCount("contacten", rows, ".gl-v2-client-row");
                markDirty();
                return;
            }

            var primaryBtn = e.target.closest(".js-gl-v2-primary-contact-toggle");
            if (primaryBtn) {
                e.preventDefault();
                setPrimaryContact(rows, primaryBtn.closest(".gl-v2-client-row"));
                markDirty();
            }
        });
    }

    // Slechts één contact mag primair zijn — bij een klik wordt de eigen rij aan/uit gezet en
    // worden alle andere rijen defensief uitgezet (zelfde exclusiviteit als de server-side
    // normalisatie in KlantenController/ClientAccountTranslator.NormalizePrimaryContact).
    function setPrimaryContact(rows, row) {
        if (!row || !rows) return;
        var input = row.querySelector(".js-gl-v2-primary-contact-input");
        var btn = row.querySelector(".js-gl-v2-primary-contact-toggle");
        if (!input || !btn) return;
        var makePrimary = !input.checked;

        rows.querySelectorAll(".gl-v2-client-row").forEach(function (otherRow) {
            var otherInput = otherRow.querySelector(".js-gl-v2-primary-contact-input");
            var otherBtn = otherRow.querySelector(".js-gl-v2-primary-contact-toggle");
            if (!otherInput || !otherBtn) return;
            var checked = otherRow === row && makePrimary;
            otherInput.checked = checked;
            otherBtn.classList.toggle("is-primary", checked);
            otherBtn.setAttribute("aria-pressed", checked ? "true" : "false");
            var icon = otherBtn.querySelector("i");
            if (icon) icon.className = "ph ph-star";
        });
    }

    // ── Mede-eigenaars-rijen — twee per-rij schakelaars (bedrijf-toggle, puur UI; afwijkend-
    //    facturatieadres-toggle, gebonden) via gedelegeerde listeners + .closest(".gl-v2-card"),
    //    zodat elke rij onafhankelijk werkt ongeacht of ze bij het laden al bestond of net via fetch
    //    is toegevoegd. ──────────────────────────────────────────────────────────────────────────
    function applyCoOwnerCompanyToggle(card) {
        var toggle = card.querySelector('[data-role="coowner-company-toggle"]');
        var fields = card.querySelector('[data-role="coowner-company-fields"]');
        if (toggle && fields) fields.hidden = !toggle.checked;
    }

    function applyCoOwnerInvoiceToggle(card) {
        var toggle = card.querySelector('[data-role="coowner-invoice-address-toggle"]');
        if (!toggle) return;
        card.querySelectorAll('[data-role="coowner-invoice-address-fields"]').forEach(function (el) {
            el.hidden = !toggle.checked;
        });
    }

    function initCoOwnerRows() {
        var rows = document.getElementById("gl-v2-coowner-rows");
        var addBtn = document.getElementById("gl-v2-add-coowner");
        if (!rows) return;

        rows.querySelectorAll(".gl-v2-card").forEach(function (card) {
            applyCoOwnerCompanyToggle(card);
            applyCoOwnerInvoiceToggle(card);
        });
        initSalutationFirstnameToggle(rows);

        rows.addEventListener("change", function (e) {
            var card = e.target.closest(".gl-v2-card");
            if (!card) return;
            if (e.target.matches('[data-role="coowner-company-toggle"]')) {
                applyCoOwnerCompanyToggle(card);
                markDirty();
            }
            if (e.target.matches('[data-role="coowner-invoice-address-toggle"]')) {
                applyCoOwnerInvoiceToggle(card);
                markDirty();
            }
        });

        if (addBtn) {
            addBtn.addEventListener("click", function (e) {
                e.preventDefault();
                fetch(config.addCoOwnerUrl)
                    .then(function (r) { return r.text(); })
                    .then(function (html) {
                        var fragment = document.createElement("div");
                        fragment.innerHTML = html;
                        var rowEl = fragment.firstElementChild;
                        if (!rowEl) return;
                        rows.appendChild(rowEl);
                        initSearchSelects(rowEl);
                        initSalutationFirstnameToggle(rowEl);
                        if (window.GlV2Phone) window.GlV2Phone.init(rowEl);
                        var card = rowEl.querySelector(".gl-v2-card");
                        if (card) {
                            applyCoOwnerCompanyToggle(card);
                            applyCoOwnerInvoiceToggle(card);
                        }
                        updateTabCount("medeeigenaars", rows, ".gl-v2-client-row");
                        markDirty();
                        recalcShares();
                    })
                    .catch(function () {
                        if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Fout", body: "Kon geen mede-eigenaar toevoegen." });
                    });
            });
        }

        rows.addEventListener("click", function (e) {
            var deleteBtn = e.target.closest(".js-gl-v2-delete-coowner-row");
            if (!deleteBtn) return;
            deleteBtn.closest(".gl-v2-client-row").remove();
            updateTabCount("medeeigenaars", rows, ".gl-v2-client-row");
            markDirty();
            recalcShares();
        });

        rows.addEventListener("input", function (e) {
            if (e.target.matches('[data-role="coowner-percentage"]') || /CoOwnerPercentage$/.test(e.target.name || "")) recalcShares();
        });
    }

    // ── Verdeelsleutel (migratie 057, design-handoff 23a "100 % KLOPT") — som van eigenaar 1 se
    //    aandeel + alle zichtbare mede-eigenaar-percentages, live herberekend. "Gelijk verdelen"
    //    verdeelt 100 % gelijk over het aantal zichtbare eigenaars (afgerond, rest naar eigenaar 1).
    function shareInputs() {
        var list = [];
        var owner1 = document.querySelector('[data-role="share-input"]');
        if (owner1) list.push(owner1);
        document.querySelectorAll('#gl-v2-coowner-rows input[name$="CoOwnerPercentage"]').forEach(function (el) { list.push(el); });
        return list;
    }

    function recalcShares() {
        var bar = document.getElementById("gl-v2-kep-shares");
        if (!bar) return;
        var inputs = shareInputs();
        var total = inputs.reduce(function (sum, el) { return sum + (parseFloat((el.value || "0").replace(",", ".")) || 0); }, 0);
        var rounded = Math.round(total * 100) / 100;
        var valueEl = bar.querySelector('[data-role="share-value"]');
        var badge = bar.querySelector('[data-role="share-badge"]');
        if (valueEl) valueEl.textContent = (rounded % 1 === 0 ? rounded.toFixed(0) : rounded.toFixed(2));
        var ok = Math.abs(rounded - 100) < 0.01;
        bar.classList.toggle("is-off", !ok);
        if (badge) badge.innerHTML = ok
            ? '<i class="ph ph-check" aria-hidden="true"></i>Klopt'
            : '<i class="ph ph-warning" aria-hidden="true"></i>' + (rounded > 100 ? "Te veel" : "Nog niet 100 %");
    }

    function initShareTotal() {
        var bar = document.getElementById("gl-v2-kep-shares");
        if (!bar) return;
        document.addEventListener("input", function (e) {
            if (shareInputs().indexOf(e.target) >= 0) recalcShares();
        });
        var equalizeBtn = bar.querySelector('[data-role="share-equalize"]');
        if (equalizeBtn) {
            equalizeBtn.addEventListener("click", function () {
                var inputs = shareInputs();
                if (!inputs.length) return;
                var each = Math.floor(100 / inputs.length);
                var remainder = 100 - each * inputs.length;
                inputs.forEach(function (el, i) {
                    el.value = String(i === 0 ? each + remainder : each);
                    el.dispatchEvent(new Event("input", { bubbles: true }));
                });
                markDirty();
                recalcShares();
            });
        }
        recalcShares();
    }

    // ── Facturatie-tab (migratie 057): "op naam van"-afhankelijk veld, ontvangers-chips, live
    //    voorbeeldkaart. Enkel weergave/bewaren — geen echte facturatie-aanroep. ────────────────────
    function invoicingPreviewData() {
        var mode = document.querySelector('#gl-v2-kep-invmode input:checked');
        var perOwner = !!mode && mode.getAttribute("data-role") === "invmode-perowner";
        var name = (document.getElementById("gl-v2-kd-account-name") || {}).value
            || (document.querySelector('[name$="Client.Name"], [name$="ClientAccount.Name"]') || {}).value
            || "Klant";
        var owners = [{ name: name, pct: 100 }];
        if (perOwner) {
            owners = [];
            var owner1Pct = parseFloat(((document.querySelector('[data-role="share-input"]') || {}).value || "100").replace(",", ".")) || 0;
            owners.push({ name: name, pct: owner1Pct });
            document.querySelectorAll("#gl-v2-coowner-rows .gl-v2-card, #gl-v2-add-owner-rows .gl-v2-card").forEach(function (card) {
                var nameEl = card.querySelector('input[name$=".Name"]');
                var pctEl = card.querySelector('input[name$="CoOwnerPercentage"]');
                var pct = parseFloat(((pctEl || {}).value || "0").replace(",", ".")) || 0;
                owners.push({ name: (nameEl && nameEl.value) || "Mede-eigenaar", pct: pct });
            });
        }
        return owners;
    }

    function updateInvoicePreview() {
        var host = document.querySelector('[data-role="invoice-preview-rows"]');
        if (!host) return;
        var owners = invoicingPreviewData();
        var amount = 10000;
        host.innerHTML = "";
        owners.forEach(function (o) {
            var row = document.createElement("div");
            row.className = "gl-v2-kep-invoice-row";
            var share = owners.length > 1 ? " · " + (o.pct % 1 === 0 ? o.pct.toFixed(0) : o.pct.toFixed(1)) + " %" : "";
            var part = owners.length > 1 ? amount * o.pct / 100 : amount;
            row.innerHTML = "<span>" + o.name + share + "</span><b>€ " + part.toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + "</b>";
            host.appendChild(row);
        });
    }

    function initInvoicingTab() {
        var panel = document.querySelector('[data-tab-panel="facturatie"]');
        if (!panel) return;

        var billedToSelect = panel.querySelector('[data-role="billedto-select"]');
        var ownerField = panel.querySelector('[data-role="billedto-owner-field"]');
        if (billedToSelect && ownerField) {
            billedToSelect.addEventListener("change", function () {
                ownerField.hidden = billedToSelect.value !== "1";
            });
        }

        var recipients = panel.querySelector('[data-role="recipients"]');
        var addInput = document.getElementById("gl-v2-kep-recipient-add");
        var suggestionsHost = panel.querySelector('[data-role="recipient-suggestions"]');
        var recipientIndex = recipients ? recipients.querySelectorAll(".gl-v2-kep-recipient-chip").length : 0;
        var namePrefix = (billedToSelect && billedToSelect.name || "").replace(/BilledToType$/, "InvoiceRecipients");

        function escapeAttr(value) {
            return String(value || "").replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;");
        }

        function existingRecipientEmails() {
            if (!recipients) return [];
            return Array.prototype.map.call(recipients.querySelectorAll('input[name$=".Email"]'), function (el) {
                return (el.value || "").trim().toLowerCase();
            });
        }

        // displayName/contactId zijn optioneel: een vrij ingetikt adres heeft ze niet, een "snel
        // toevoegen"-suggestie (uit de personen op deze pagina) wél — dan gaat ook ClientContactId mee
        // (enkel voor een al opgeslagen mede-eigenaar/contact met echte Id, anders leeg).
        function addRecipientChip(email, displayName, contactId) {
            if (!recipients || !namePrefix) return;
            if (existingRecipientEmails().indexOf(email.trim().toLowerCase()) >= 0) return;
            var chip = document.createElement("span");
            chip.className = "gl-v2-kep-recipient-chip";
            var idx = recipientIndex++;
            chip.innerHTML =
                '<input type="hidden" name="' + namePrefix + '[' + idx + '].Id" value="0" />' +
                '<input type="hidden" name="' + namePrefix + '[' + idx + '].ClientContactId" value="' + escapeAttr(contactId) + '" />' +
                '<input type="hidden" name="' + namePrefix + '[' + idx + '].Email" value="' + escapeAttr(email) + '" />' +
                '<input type="hidden" name="' + namePrefix + '[' + idx + '].DisplayName" value="' + escapeAttr(displayName) + '" />' +
                '<span data-role="recipient-label"></span>' +
                '<button type="button" data-role="remove-recipient" aria-label="Ontvanger verwijderen"><i class="ph ph-x" aria-hidden="true"></i></button>';
            chip.querySelector('[data-role="recipient-label"]').textContent = displayName ? displayName + " · " + email : email;
            recipients.appendChild(chip);
            markDirty();
            renderSuggestions();
        }

        // ── "Snel toevoegen": de e-mails die op deze pagina al ingevuld staan (hoofdklant, mede-
        //    eigenaars, contactpersonen) als één-klik-suggesties, zodat je een adres niet een tweede
        //    keer moet overtikken. Al toegevoegde adressen vallen weg uit de suggesties. ─────────────
        function personLabel(nameEl, firstnameEl) {
            var name = (nameEl && nameEl.value || "").trim();
            var first = (firstnameEl && firstnameEl.value || "").trim();
            return (name + (first ? " " + first : "")).trim();
        }

        function knownPeople() {
            var list = [];
            var owner1Email = document.querySelector('[name$="ClientAccount.Email"], [name$="Client.Email"]');
            if (owner1Email && owner1Email.value.indexOf("@") > 0) {
                list.push({
                    email: owner1Email.value.trim(),
                    name: personLabel(document.querySelector('[name$="ClientAccount.Name"], [name$="Client.Name"]'), document.querySelector('[name$="ClientAccount.Firstname"], [name$="Client.Firstname"]')) || "Eigenaar 1",
                    contactId: ""
                });
            }
            document.querySelectorAll("#gl-v2-coowner-rows .gl-v2-client-row, #gl-v2-contact-rows .gl-v2-client-row").forEach(function (row) {
                var emailEl = row.querySelector('input[name$=".Email"]');
                if (!emailEl || emailEl.value.indexOf("@") <= 0) return;
                var idEl = row.querySelector('input[type="hidden"][name$=".Id"]');
                var id = idEl && parseInt(idEl.value, 10) > 0 ? idEl.value : "";
                list.push({
                    email: emailEl.value.trim(),
                    name: personLabel(row.querySelector('input[name$=".Name"]'), row.querySelector('input[name$=".Firstname"]')),
                    contactId: id
                });
            });
            return list;
        }

        function renderSuggestions() {
            if (!suggestionsHost) return;
            var taken = existingRecipientEmails();
            var seen = {};
            var items = knownPeople().filter(function (p) {
                var key = p.email.toLowerCase();
                if (seen[key] || taken.indexOf(key) >= 0) return false;
                seen[key] = true;
                return true;
            });
            suggestionsHost.innerHTML = "";
            suggestionsHost.hidden = items.length === 0;
            if (!items.length) return;
            var label = document.createElement("span");
            label.className = "gl-v2-kep-recipient-suggestions-label";
            label.textContent = "Snel toevoegen:";
            suggestionsHost.appendChild(label);
            items.forEach(function (p) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "gl-v2-kep-recipient-suggestion";
                btn.textContent = (p.name ? p.name + " · " : "") + p.email;
                btn.addEventListener("click", function () { addRecipientChip(p.email, p.name, p.contactId); });
                suggestionsHost.appendChild(btn);
            });
        }

        if (addInput) {
            addInput.addEventListener("keydown", function (e) {
                if (e.key !== "Enter") return;
                e.preventDefault();
                var value = addInput.value.trim();
                if (!value || value.indexOf("@") < 0) return;
                addRecipientChip(value, "", "");
                addInput.value = "";
            });
            addInput.addEventListener("blur", function () {
                var value = addInput.value.trim();
                if (value && value.indexOf("@") > 0) { addRecipientChip(value, "", ""); addInput.value = ""; }
            });
        }
        if (recipients) {
            recipients.addEventListener("click", function (e) {
                var btn = e.target.closest('[data-role="remove-recipient"]');
                if (!btn) return;
                btn.closest(".gl-v2-kep-recipient-chip").remove();
                markDirty();
                renderSuggestions();
            });
        }

        panel.addEventListener("change", updateInvoicePreview);
        document.addEventListener("input", function (e) {
            if (shareInputs().indexOf(e.target) >= 0) updateInvoicePreview();
            if (e.target.matches('input[name$=".Email"], input[name$=".Name"], input[name$=".Firstname"]')) renderSuggestions();
        });
        document.addEventListener("click", function (e) {
            if (e.target.closest(".js-gl-v2-delete-contact-row, .js-gl-v2-delete-coowner-row")) window.setTimeout(renderSuggestions, 0);
        });
        updateInvoicePreview();
        renderSuggestions();
    }

    // ── Toegiften-rijen. ────────────────────────────────────────────────────────────────────────
    function initGiftRows() {
        var rows = document.getElementById("gl-v2-gift-rows");
        var addBtn = document.getElementById("gl-v2-add-gift");
        if (!rows) return;

        if (addBtn) {
            addBtn.addEventListener("click", function (e) {
                e.preventDefault();
                fetch(config.addGiftUrl)
                    .then(function (r) { return r.text(); })
                    .then(function (html) {
                        var fragment = document.createElement("div");
                        fragment.innerHTML = html;
                        var rowEl = fragment.firstElementChild;
                        if (!rowEl) return;
                        rows.appendChild(rowEl);
                        initMultiSelects(rowEl);
                        updateTabCount("toegiften", rows, ".gl-v2-client-row");
                        markDirty();
                    })
                    .catch(function () {
                        if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Fout", body: "Kon geen toegift toevoegen." });
                    });
            });
        }

        rows.addEventListener("click", function (e) {
            var deleteBtn = e.target.closest(".js-gl-v2-delete-gift-row");
            if (!deleteBtn) return;
            deleteBtn.closest(".gl-v2-client-row").remove();
            updateTabCount("toegiften", rows, ".gl-v2-client-row");
            markDirty();
        });
    }

    // ── Aandachtspunten-rijen. ──────────────────────────────────────────────────────────────────
    function initPoaRows() {
        var rows = document.getElementById("gl-v2-poa-rows");
        var addBtn = document.getElementById("gl-v2-add-poa");
        if (!rows) return;

        if (addBtn) {
            addBtn.addEventListener("click", function (e) {
                e.preventDefault();
                fetch(config.addPoaUrl)
                    .then(function (r) { return r.text(); })
                    .then(function (html) {
                        var fragment = document.createElement("div");
                        fragment.innerHTML = html;
                        var rowEl = fragment.firstElementChild;
                        if (!rowEl) return;
                        rows.appendChild(rowEl);
                        initMultiSelects(rowEl);
                        updateTabCount("aandachtspunten", rows, ".gl-v2-client-row");
                        markDirty();
                    })
                    .catch(function () {
                        if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Fout", body: "Kon geen aandachtspunt toevoegen." });
                    });
            });
        }

        rows.addEventListener("click", function (e) {
            var deleteBtn = e.target.closest(".js-gl-v2-delete-poa-row");
            if (!deleteBtn) return;
            deleteBtn.closest(".gl-v2-client-row").remove();
            updateTabCount("aandachtspunten", rows, ".gl-v2-client-row");
            markDirty();
        });
    }

    // ── Verplichte-veldencontrole (Naam is voorlopig het enige harde vereiste op deze pagina, zelfde
    //    als de legacy versie — die deed enkel een server-side check op Client.Name). ───────────────
    function resolveFieldTarget(field) {
        if (field.classList.contains("gl-v2-field")) return field;
        return field.querySelector(".gl-v2-field") || field;
    }

    function isFieldElementEmpty(field) {
        var input = field.querySelector("input, select, textarea");
        return !input || !input.value || !input.value.trim();
    }

    function setFieldError(field, message, isInvalid) {
        var target = resolveFieldTarget(field);
        target.classList.toggle("is-error", isInvalid);
        var help = target.querySelector(".gl-v2-field-help[data-role='required-help']");
        if (isInvalid) {
            if (!help) {
                help = document.createElement("span");
                help.className = "gl-v2-field-help";
                help.setAttribute("data-role", "required-help");
                target.appendChild(help);
            }
            help.textContent = message;
        } else if (help) {
            help.remove();
        }
    }

    function fieldTabKey(field) {
        var panel = field.closest("[data-tab-panel]");
        return panel ? panel.getAttribute("data-tab-panel") : "algemeen";
    }

    function setTabError(tabKey, isInvalid) {
        var tab = document.querySelector('.gl-v2-tabbar-tab[data-tab="' + tabKey + '"]');
        if (!tab) return;
        var dot = tab.querySelector('[data-role="error-dot"]');
        if (dot) dot.hidden = !isInvalid;
    }

    function refreshTabError(tabKey) {
        var panel = document.querySelector('.gl-v2-tab-panel[data-tab-panel="' + tabKey + '"]');
        var hasError = panel
            ? Array.prototype.some.call(panel.querySelectorAll("[data-gl-v2-required]"), function (field) {
                return resolveFieldTarget(field).classList.contains("is-error");
            })
            : false;
        setTabError(tabKey, hasError);
    }

    function validateField(field) {
        if (field.closest("[hidden]")) {
            setFieldError(field, "", false);
            return false;
        }
        var invalid = isFieldElementEmpty(field);
        setFieldError(field, field.getAttribute("data-gl-v2-required"), invalid);
        return invalid;
    }

    function validateRequiredFields() {
        var fields = document.querySelectorAll("[data-gl-v2-required]");
        var firstInvalid = null;
        fields.forEach(function (field) {
            if (validateField(field) && !firstInvalid) firstInvalid = field;
        });
        ["algemeen", "eenheden", "contacten", "medeeigenaars", "toegiften", "aandachtspunten"].forEach(refreshTabError);
        return firstInvalid;
    }

    // Verdeelsleutel (als de pagina er een heeft): geen leeg/niet-leeg-check maar een som die exact
    // 100 % moet zijn — daarom een eigen validator voor het gedeelde Foutoverzicht (zie DESIGN.md,
    // veldcontract punt 3). Pagina's zonder verdeelsleutelbalk slaan dit gewoon over.
    function validateShares(errors) {
        var bar = document.getElementById("gl-v2-kep-shares");
        if (!bar || !bar.classList.contains("is-off")) return;
        var valueEl = bar.querySelector('[data-role="share-value"]');
        var total = valueEl ? valueEl.textContent.trim() : "?";
        var tabPanel = bar.closest("[data-tab-panel]");
        errors.push({
            message: "is samen " + total + " % — moet exact 100 % zijn",
            field: "Verdeelsleutel",
            location: "Mede-eigenaars",
            tab: tabPanel ? tabPanel.getAttribute("data-tab-panel") : "",
            target: bar
        });
    }

    var errorSummary = null;

    // Gedeeld Foutoverzicht (design-handoff punt 24, gl-v2-error-summary.js): verzamelt elk
    // [data-gl-v2-required]-veld, rendert de samenvatting bovenaan, zet de rode tab-stippen en
    // activeert bij een klik op een fout de juiste tab (via de bestaande .gl-v2-tabbar-tab-klik).
    // De losse validateField/refreshTabError-helpers hierboven blijven bestaan voor de schakelaars
    // die tussentijds een tab-stip moeten bijwerken. Een server-redisplay rendert de samenvatting
    // zelf (Views/Shared/GlV2/_ErrorSummaryV2.cshtml) — die blijft staan tot de volgende submit.
    function initRequiredValidation() {
        var form = document.getElementById("gl-v2-client-form");
        if (!form || !window.GlV2ErrorSummary) return;
        errorSummary = window.GlV2ErrorSummary.init({
            form: form,
            container: document.getElementById("gl-v2-error-summary"),
            validators: [validateShares]
        });
    }

    // ── Actiebalk-submit — dubbele-submit-blokkade. ────────────────────────────────────────────
    function initSubmitLoading() {
        var form = document.getElementById("gl-v2-client-form");
        var submitBtn = document.getElementById("gl-v2-client-submit");
        if (!form || !submitBtn) return;
        form.addEventListener("submit", function () {
            submitBtn.classList.add("is-loading");
            submitBtn.disabled = true;
        });
    }

    // ── "Niet-opgeslagen wijzigingen"-badge. ───────────────────────────────────────────────────
    function markDirty() {
        var badge = document.getElementById("gl-v2-dirty-badge");
        if (badge) badge.hidden = false;
    }

    function initDirtyBadge() {
        var form = document.getElementById("gl-v2-client-form");
        if (!form) return;
        form.addEventListener("input", markDirty);
        form.addEventListener("change", markDirty);
    }

    function isFormDirty() {
        var badge = document.getElementById("gl-v2-dirty-badge");
        return !!badge && !badge.hidden;
    }

    // ── Annuleren met wijzigingen → eerst bevestigen. Zowel Annuleren als de topbar-terugknop
    //    wijzen naar dezelfde @backUrl, dus één gedeeld intercept-gedrag volstaat voor beide. ─────
    function initDiscardChangesModal() {
        var modalEl = document.getElementById("gl-v2-discard-changes-modal");
        var confirmLink = document.getElementById("gl-v2-discard-changes-confirm");
        var triggers = [
            document.getElementById("gl-v2-cancel-link"),
            document.getElementById("gl-v2-topbar-back-link")
        ].filter(Boolean);
        if (!triggers.length || !modalEl || !confirmLink || !window.bootstrap) return;

        confirmLink.href = triggers[0].href;

        triggers.forEach(function (trigger) {
            trigger.addEventListener("click", function (e) {
                if (!isFormDirty()) return;
                e.preventDefault();
                window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
            });
        });
    }

    function initGotoFirstError() {
        var link = document.getElementById("gl-v2-goto-first-error");
        if (!link) return;
        link.addEventListener("click", function () {
            var firstError = document.querySelector(".gl-v2-field.is-error, .text-danger:not(:empty)");
            if (firstError && firstError.scrollIntoView) {
                firstError.scrollIntoView({ behavior: "smooth", block: "center" });
            }
        });
    }
})();
