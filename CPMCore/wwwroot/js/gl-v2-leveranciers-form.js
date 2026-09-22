// gl-v2 layout-pilot — Leveranciers/EditV2.cshtml ÉN CreateV2.cshtml. Eigen pagina-JS (zelfde "eigen
// <pagina>.js"-conventie als gl-v2-invoices.js) — gl-v2-leveranciers.js blijft de lijstpagina, andere
// zorgen. Was aanvankelijk gl-v2-leveranciers-edit.js, enkel voor Edit — hernoemd toen bleek dat
// CreateV2 exact hetzelfde formulier is (geen enkele Id-afhankelijke logica hierin — alles loopt via
// window.glV2SupplierFormConfig se eigen endpoint-urls). Vanille JS, geen jQuery, zelfde stijl
// als gl-v2-shell.js. window.glV2SupplierFormConfig (Edit/CreateV2.cshtml) draagt de endpoint-urls +
// het BE-land-id + de server-bepaalde force-tab.
(function () {
    "use strict";

    var config = window.glV2SupplierFormConfig || {};
    var backdrop = null;
    var activateTab = null;

    initTabs();
    initMultiSelects();
    initSearchSelects();
    initAddressToggle();
    initBtwCheck();
    initVatModalApply();
    initDepartmentContactRows();
    initRequiredValidation();
    initSubmitLoading();
    initGotoFirstError();
    initDirtyBadge();
    initUblDependency();
    initDiscardChangesModal();
    initBeNumberFormatting();

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
            panel.style.left = "";
            panel.style.width = "";
            return;
        }
        var rect = trigger.getBoundingClientRect();
        var width = Math.max(rect.width, 260);
        panel.style.width = width + "px";
        var left = Math.min(rect.left, window.innerWidth - width - 12);
        panel.style.left = Math.max(12, left) + "px";
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

    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") closeAllPanels();
    });
    // .gl-v2-select-backdrop (hierboven) is per-ontwerp enkel op mobiel (<768px) echt zichtbaar/
    // klikbaar (zie de basisregel "display:none" op .gl-v2-select-backdrop in gl-v2-shell.css, enkel
    // binnen de mobiele media query weer aangezet) — op desktop/tablet doet 'm dus niets, en zonder
    // deze document-brede klik-erbuiten-sluit-luisteraar bleef een open paneel open staan zodra de
    // gebruiker gewoon naar een ander veld klikte. Sluit alles zodra de klik buiten élk .gl-v2-select
    // (trigger + paneel horen daar allebei bij) en buiten élke multiselect-chipsrij valt.
    document.addEventListener("click", function (e) {
        if (e.target.closest(".gl-v2-select, .gl-v2-select-panel")) return;
        closeAllPanels();
    });
    window.addEventListener("resize", closeAllPanels);

    // ── Tabs ────────────────────────────────────────────────────────────────────────────────────
    function initTabs() {
        var tabbar = document.getElementById("gl-v2-supplier-tabbar");
        if (!tabbar) return;

        function activate(key) {
            tabbar.querySelectorAll(".gl-v2-tabbar-tab").forEach(function (tab) {
                var isActive = tab.getAttribute("data-tab") === key;
                tab.classList.toggle("is-active", isActive);
                tab.setAttribute("aria-selected", isActive ? "true" : "false");
            });
            document.querySelectorAll(".gl-v2-tab-panel").forEach(function (panel) {
                panel.hidden = panel.getAttribute("data-tab-panel") !== key;
            });
        }
        activateTab = activate;

        tabbar.addEventListener("click", function (e) {
            var tab = e.target.closest(".gl-v2-tabbar-tab");
            if (!tab) return;
            activate(tab.getAttribute("data-tab"));
        });

        if (config.forceTab && config.forceTab !== "algemeen") {
            activate(config.forceTab);
        }
    }

    // ── Meervoudige kiezers (Facturatiebedrijven, Activiteiten) — volledig client-side, opties
    //    staan al server-gerenderd. ─────────────────────────────────────────────────────────────
    function initMultiSelects() {
        document.querySelectorAll("[data-gl-v2-multiselect]").forEach(function (root) {
            var trigger = root.querySelector(".gl-v2-select-trigger");
            var panel = root.querySelector(".gl-v2-select-panel");
            var chipsHost = root.querySelector('[data-role="chips"]');
            var filterInput = root.querySelector('[data-role="filter"]');
            // Enkel aanwezig op de dropdown-variant (geen zoekveld) — "Kies …"-tekst die verdwijnt
            // zodra er minstens één chip staat, zelfde rol als het zoekveld se eigen placeholder-
            // tekst had op de zoekende variant.
            var placeholder = root.querySelector('[data-role="placeholder"]');
            var hiddenHost = root.querySelector('[data-role="hidden-inputs"]');
            var fieldName = root.getAttribute("data-field-name");
            if (!trigger || !panel || !chipsHost || !hiddenHost || !fieldName) return;

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
                if (placeholder) placeholder.hidden = selectedIds().length > 0;
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
                // Het zoekveld zat áltijd al in de trigger (dus binnen .gl-v2-select-trigger-multi),
                // maar deed enkel e.stopPropagation() op klik — waardoor een klik precies óp het
                // veld zelf (i.p.v. ernaast, op de chips-ruimte) het paneel nooit opende: de klik
                // bereikte de trigger se eigen open/dicht-toggle daardoor gewoonweg nooit. Vandaar
                // "gaat niet consistent open" — welk stukje van de trigger je raakte, bepaalde het
                // resultaat. focus/input openen nu zelf expliciet (idempotent, enkel als nog dicht),
                // dus zowel muis- als toetsenbordgebruik (Tab erin, dan typen) werkt hetzelfde.
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
        });
    }

    // ── Zoekende dropdown (GlV2SearchSelect — postcode/gemeente) — eerste echte toepassing van dit
    //    component. Departementsrijen zoeken altijd binnen België (config.beCountryId); het
    //    hoofdformulier se eigen veld zoekt binnen de op dat moment gekozen SelectedCountryId. ─────
    function initSearchSelects() {
        document.querySelectorAll("[data-gl-v2-search-select]").forEach(wireSearchSelect);
    }

    // Paneelskelet (design-handoff 8f·3) wordt ÉÉN keer opgebouwd, niet opnieuw bij elke klik — de
    // eerdere versie herschreef panel.innerHTML bij elke zoekopdracht, wat het zoekveld zélf (een
    // kind van diezelfde panel) mee wegveegde zodra de gebruiker 1 letter tikte (de "minstens 2
    // tekens"-melding verving dan letterlijk het invoerveld). Nu heeft enkel .gl-v2-select-options
    // (de resultatenlijst) een wisselende innerHTML; het zoekveld zelf blijft altijd bestaan.
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
            return config.beCountryId;
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
                    root.dispatchEvent(new CustomEvent("gl-v2:postal-selected", { bubbles: true, detail: item }));
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

        trigger.addEventListener("click", function (e) {
            if (e.target.closest('[data-role="clear-trigger"]')) return;
            if (panel.classList.contains("is-open")) {
                closeAllPanels();
                return;
            }
            openThisPanel();
        });
        // De trigger is een <div role="button"> (geen echte <button>, zie GlV2SearchSelect.cshtml se
        // eigen toelichting) — toetsenbordactivatie (Enter/spatie) moet daardoor hier zelf.
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

        if (clearTrigger) {
            clearTrigger.addEventListener("click", function (e) {
                e.stopPropagation();
                hidden.value = "";
                label.textContent = searchPlaceholder;
                trigger.classList.remove("is-filled");
                closeAllPanels();
                markDirty();
                root.dispatchEvent(new CustomEvent("gl-v2:postal-selected", { bubbles: true, detail: { id: "", text: "" } }));
            });
        }
    }

    // ── BE/niet-BE-adrestoggle — client-side op wijziging van het landveld, zelfde gedrag als
    //    vandaag (postcode-zoekveld vs. platte tekstvelden). ────────────────────────────────────
    function initAddressToggle() {
        var countrySelect = document.querySelector('[data-role="country-select"]');
        var beWrapper = document.querySelector('[data-role="be-address-wrapper"]');
        var otherWrapper = document.querySelector('[data-role="other-address-wrapper"]');
        var bePostalHidden = document.querySelector('[data-role="be-postal-hidden"]');
        var beCityHidden = document.querySelector('[data-role="be-city-hidden"]');
        var otherPostalInput = document.querySelector('[data-role="other-postal-input"]');
        var otherCityInput = document.querySelector('[data-role="other-city-input"]');
        if (!countrySelect || !beWrapper || !otherWrapper) return;

        function isBelgian() {
            var opt = countrySelect.options[countrySelect.selectedIndex];
            return opt && (opt.getAttribute("data-isocode") || "").toUpperCase() === "BE";
        }

        function apply() {
            var belgian = isBelgian();
            beWrapper.hidden = !belgian;
            otherWrapper.hidden = belgian;
        }

        countrySelect.addEventListener("change", apply);

        var searchSelectRoot = beWrapper.querySelector("[data-gl-v2-search-select]");
        if (searchSelectRoot && bePostalHidden && beCityHidden) {
            searchSelectRoot.addEventListener("gl-v2:postal-selected", function (e) {
                var parts = (e.detail && e.detail.text || "").split(" - ");
                bePostalHidden.value = parts[0] || "";
                beCityHidden.value = parts.slice(1).join(" - ").trim();
            });
        }

        apply();
    }

    // ── Belgische ondernemings-/btw-nummers automatisch formatteren naar 0000.000.000 terwijl je
    //    tikt — zelfde .is-tabular-veldklasse dragen zowel GlV2OndernemingsNummer als GlV2BtwNummer
    //    al (design-handoff 8f·1 toont beide zo, "0453.139.854"). De server maakt zelf niets van de
    //    punten (SanitizeDigits strip ze toch voor opslag/btw-controle), dit is dus puur voor
    //    leesbaarheid tijdens het invullen — en garandeert meteen dat de waarde die naar Controleren
    //    gaat al in dat formaat staat. Enkel voor cijfers, geen volledige inputmask-library nodig:
    //    max 10 cijfers (BE-nummer), punten na het 4de en 7de cijfer. */
    function formatBeNumber(digitsOnly) {
        var d = digitsOnly.slice(0, 10);
        var parts = [];
        if (d.length > 0) parts.push(d.slice(0, 4));
        if (d.length > 4) parts.push(d.slice(4, 7));
        if (d.length > 7) parts.push(d.slice(7, 10));
        return parts.join(".");
    }

    function initBeNumberFormatting() {
        document.querySelectorAll(".gl-v2-field-input.is-tabular").forEach(function (input) {
            if (input.value) {
                input.value = formatBeNumber(input.value.replace(/\D/g, ""));
            }
            input.addEventListener("input", function () {
                var caretFromEnd = input.value.length - (input.selectionEnd || input.value.length);
                input.value = formatBeNumber(input.value.replace(/\D/g, ""));
                var pos = Math.max(0, input.value.length - caretFromEnd);
                input.setSelectionRange(pos, pos);
            });
        });
    }

    // ── Btw-controle — fetch naar ValidateVat, toggelt .is-busy/.is-checked op .gl-v2-field-
    //    attached, opent bij succes de gl-v2-modal-form-btw-modal. ────────────────────────────────
    function initBtwCheck() {
        var wrapper = document.querySelector("[data-gl-v2-btw-check-url]");
        if (!wrapper) return;
        var btn = wrapper.querySelector("[data-gl-v2-btw-check-btn]");
        var input = wrapper.querySelector(".gl-v2-field-input");
        if (!btn || !input) return;

        btn.addEventListener("click", function () {
            if (wrapper.classList.contains("is-checked")) {
                // "Opnieuw" — gewoon terug naar rust, gebruiker tikt/controleert opnieuw.
                wrapper.classList.remove("is-checked");
                btn.innerHTML = '<i class="ph ph-check" aria-hidden="true"></i><span>Controleren</span>';
                return;
            }

            var vatNumber = (input.value || "").trim();
            if (!vatNumber) return;

            wrapper.classList.add("is-busy");
            var originalHtml = btn.innerHTML;
            btn.innerHTML = '<span class="gl-v2-spinner"></span><span>Controleren …</span>';

            var url = config.validateVatUrl + "?vatCountryCode=BE&vatNumber=" + encodeURIComponent(vatNumber);
            fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })
                .then(function (r) { return r.ok ? r.json() : Promise.reject(); })
                .then(function (data) {
                    wrapper.classList.remove("is-busy");
                    wrapper.classList.add("is-checked");
                    btn.innerHTML = '<span>Opnieuw</span>';
                    fillVatModal(data);
                    var modalEl = document.getElementById("gl-v2-vat-lookup-modal");
                    if (modalEl && window.bootstrap) {
                        window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
                    }
                })
                .catch(function () {
                    wrapper.classList.remove("is-busy");
                    btn.innerHTML = originalHtml;
                    if (window.GlV2Toast) {
                        window.GlV2Toast.show({ tone: "danger", title: "Btw-controle mislukt", body: "Dit btw-nummer werd niet gevonden of de dienst antwoordde niet." });
                    }
                });
        });
    }

    function fillVatModal(data) {
        if (!data) return;
        var address = data.address || {};
        setVal("gl-v2-vat-lookup-name", data.name);
        setVal("gl-v2-vat-lookup-street", address.street);
        setVal("gl-v2-vat-lookup-nr", address.number);
        setVal("gl-v2-vat-lookup-postal", address.zip || address.zip_code || address.postcode);
        setVal("gl-v2-vat-lookup-city", address.city);
        var iso = (address.countryCode || data.countryCode || "BE").toString().toUpperCase();
        var countrySelect = document.getElementById("gl-v2-vat-lookup-country");
        if (countrySelect && countrySelect.querySelector('option[value="' + iso + '"]')) {
            countrySelect.value = iso;
        }
    }

    function setVal(id, value) {
        var el = document.getElementById(id);
        if (el && value) el.value = value;
    }

    // ── "Gegevens overnemen" — kopieert de btw-opzoekgegevens naar het hoofdformulier, lost daarna
    //    de postcode op via FindPostalMatch (zelfde tweestapsflow als de legacy modal). ────────────
    function initVatModalApply() {
        var applyBtn = document.getElementById("gl-v2-vat-lookup-apply");
        if (!applyBtn) return;

        applyBtn.addEventListener("click", function () {
            var name = document.getElementById("gl-v2-vat-lookup-name").value;
            var street = document.getElementById("gl-v2-vat-lookup-street").value;
            var nr = document.getElementById("gl-v2-vat-lookup-nr").value;
            var postal = document.getElementById("gl-v2-vat-lookup-postal").value;
            var city = document.getElementById("gl-v2-vat-lookup-city").value;
            var iso = document.getElementById("gl-v2-vat-lookup-country").value;

            if (name) document.getElementById("Name").value = name;
            if (street) document.getElementById("Street").value = street;
            if (nr) document.getElementById("HouseNumber").value = nr;

            var countrySelect = document.querySelector('[data-role="country-select"]');
            if (iso && countrySelect) {
                Array.prototype.forEach.call(countrySelect.options, function (opt) {
                    if ((opt.getAttribute("data-isocode") || "").toUpperCase() === iso.toUpperCase()) {
                        countrySelect.value = opt.value;
                        countrySelect.dispatchEvent(new Event("change"));
                    }
                });
            }

            if (postal) {
                var body = new URLSearchParams();
                body.set("postalCode", postal);
                if (city) body.set("city", city);
                if (iso) body.set("countryIso", iso);
                fetch(config.postalMatchUrl + "?" + body.toString())
                    .then(function (r) { return r.ok ? r.json() : null; })
                    .then(function (match) {
                        if (!match || !match.id) return;
                        var root = document.querySelector('[data-role="be-address-wrapper"] [data-gl-v2-search-select]');
                        if (!root) return;
                        var hidden = root.querySelector('input[type="hidden"]');
                        var label = root.querySelector(".gl-v2-select-trigger-label");
                        var trigger = root.querySelector(".gl-v2-select-trigger");
                        if (hidden) hidden.value = match.id;
                        if (label) label.textContent = match.text;
                        if (trigger) trigger.classList.add("is-filled");
                        var bePostalHidden = document.querySelector('[data-role="be-postal-hidden"]');
                        var beCityHidden = document.querySelector('[data-role="be-city-hidden"]');
                        if (bePostalHidden && beCityHidden && match.text) {
                            var parts = match.text.split(" - ");
                            bePostalHidden.value = parts[0] || "";
                            beCityHidden.value = parts.slice(1).join(" - ").trim();
                        }
                    })
                    .catch(function () { /* geen match — velden blijven zoals de gebruiker ze zag/aanpaste */ });
            }

            var modalEl = document.getElementById("gl-v2-vat-lookup-modal");
            if (modalEl && window.bootstrap) {
                window.bootstrap.Modal.getOrCreateInstance(modalEl).hide();
            }
        });
    }

    // ── Afdelingen/Contacten-rijen — toevoegen (fetch blanco rij) / verwijderen, departement-
    //    kiezer in elke contact-rij client-side gesynct met de actuele Afdelingen-rijen. ──────────
    function initDepartmentContactRows() {
        var departmentRows = document.getElementById("gl-v2-department-rows");
        var contactRows = document.getElementById("gl-v2-contact-rows");
        var addDepartmentBtn = document.getElementById("gl-v2-add-department");
        var addContactBtn = document.getElementById("gl-v2-add-contact");
        var departmentCountEl = document.querySelector('[data-role="afdelingen-count"]');
        var contactCountEl = document.querySelector('[data-role="contacten-count"]');

        function updateCounts() {
            if (departmentCountEl && departmentRows) {
                departmentCountEl.textContent = departmentRows.querySelectorAll(".gl-v2-supplier-row").length;
            }
            if (contactCountEl && contactRows) {
                contactCountEl.textContent = contactRows.querySelectorAll(".gl-v2-supplier-row").length;
            }
        }

        function refreshContactDepartmentOptions() {
            if (!departmentRows) return;
            var departments = [];
            departmentRows.querySelectorAll(".gl-v2-supplier-row").forEach(function (row) {
                var keyInput = row.querySelector('input[name$=".Key"]');
                var key = keyInput ? keyInput.value : row.getAttribute("data-key");
                var nameInput = row.querySelector(".js-gl-v2-department-name");
                var name = nameInput ? nameInput.value : "Afdeling";
                if (key) departments.push({ key: key, name: name || "Afdeling" });
            });

            document.querySelectorAll(".js-gl-v2-contact-department").forEach(function (select) {
                var current = select.value || select.getAttribute("data-selected-key") || "";
                select.innerHTML = "";
                var noneOpt = document.createElement("option");
                noneOpt.value = "";
                noneOpt.textContent = "Geen";
                select.appendChild(noneOpt);
                var matched = false;
                departments.forEach(function (dept) {
                    var opt = document.createElement("option");
                    opt.value = dept.key;
                    opt.textContent = dept.name;
                    if (dept.key === current) { opt.selected = true; matched = true; }
                    select.appendChild(opt);
                });
                select.setAttribute("data-selected-key", matched ? current : "");
            });
        }

        function appendRow(container, html, isDepartment) {
            var fragment = document.createElement("div");
            fragment.innerHTML = html;
            var rowEl = fragment.firstElementChild;
            if (!rowEl) return;
            container.appendChild(rowEl);
            rowEl.querySelectorAll("[data-gl-v2-search-select]").forEach(wireSearchSelect);
            updateCounts();
            if (isDepartment) refreshContactDepartmentOptions();
        }

        if (addDepartmentBtn && departmentRows) {
            addDepartmentBtn.addEventListener("click", function (e) {
                e.preventDefault();
                fetch(config.addDepartmentUrl)
                    .then(function (r) { return r.text(); })
                    .then(function (html) { appendRow(departmentRows, html, true); })
                    .catch(function () {
                        if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Fout", body: "Kon geen nieuwe afdeling toevoegen." });
                    });
            });

            departmentRows.addEventListener("click", function (e) {
                var deleteBtn = e.target.closest(".js-gl-v2-delete-department-row");
                if (!deleteBtn) return;
                deleteBtn.closest(".gl-v2-supplier-row").remove();
                updateCounts();
                refreshContactDepartmentOptions();
            });

            departmentRows.addEventListener("input", function (e) {
                if (e.target.classList.contains("js-gl-v2-department-name")) {
                    refreshContactDepartmentOptions();
                }
            });
        }

        if (addContactBtn && contactRows) {
            addContactBtn.addEventListener("click", function (e) {
                e.preventDefault();
                fetch(config.addContactUrl)
                    .then(function (r) { return r.text(); })
                    .then(function (html) { appendRow(contactRows, html, false); })
                    .catch(function () {
                        if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Fout", body: "Kon geen nieuw contact toevoegen." });
                    });
            });

            contactRows.addEventListener("click", function (e) {
                var deleteBtn = e.target.closest(".js-gl-v2-delete-contact-row");
                if (!deleteBtn) return;
                deleteBtn.closest(".gl-v2-supplier-row").remove();
                updateCounts();
            });
        }

        refreshContactDepartmentOptions();
    }

    // ── Verplichte-veldencontrole (design-handoff 8d) — bij Opslaan, client-side vóór de server-
    //    ronde: elk [data-gl-v2-required]-veld (Bedrijfsnaam/Straat/Nr/Gemeente-postcode — de enige
    //    top-niveau-velden met [Required] op SupplierFormViewModel; Afdelingen/Contacten-rijen hebben
    //    vandaag geen verplichte velden, dus niets om hier te controleren) dat leeg is krijgt
    //    .is-error + een foutregel, de bijhorende tab krijgt het rode stipje, de eerste foute tab
    //    wordt actief en de actiebalk toont de tellers — exact 8d se eigen "tabbar-stip + rode rand +
    //    actiebalk-teller"-combinatie, nu ook zonder eerst naar de server te moeten posten. De server
    //    blijft de uiteindelijke bron van waarheid (o.a. e-mailformaat, dubbele ondernemingsnummers,
    //    btw-validatie) — dit vangt enkel de meest voorkomende "iets vergeten"-fout meteen af. ───────
    function isFieldElementEmpty(field) {
        var hiddenInput = field.querySelector('input[type="hidden"]');
        if (hiddenInput) return !hiddenInput.value;
        var input = field.querySelector("input, select");
        return !input || !input.value || !input.value.trim();
    }

    // data-gl-v2-required zit voor de meeste velden al rechtstreeks op de .gl-v2-field-div zelf,
    // maar voor GlV2SearchSelect (de postcode/gemeente-kiezer) staat het op de buitenste .gl-v2-col-*/
    // data-role="...-address-wrapper"-omhullende div — de template rendert haar EIGEN .gl-v2-field
    // pas daarbinnen. Zonder deze resolutie landde .is-error op de verkeerde div: geen enkele
    // .gl-v2-field.is-error-CSS (rode rand/achtergrond/tekst) matchte dan, terwijl een gewoon
    // tekstveld die styling wél correct kreeg — vandaar het gemelde verschil.
    function resolveFieldTarget(field) {
        if (field.classList.contains("gl-v2-field")) return field;
        return field.querySelector(".gl-v2-field") || field;
    }

    function setFieldError(field, message, isInvalid) {
        var target = resolveFieldTarget(field);
        target.classList.toggle("is-error", isInvalid);
        // .gl-v2-select-trigger.is-error is een losse regel (geen .gl-v2-field.is-error-afgeleide) —
        // GlV2SearchSelect-velden hebben zo'n trigger, gewone tekstvelden niet.
        var trigger = target.querySelector(".gl-v2-select-trigger");
        if (trigger) trigger.classList.toggle("is-error", isInvalid);
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

    function setTabError(tabKey, isInvalid) {
        var tab = document.querySelector('.gl-v2-tabbar-tab[data-tab="' + tabKey + '"]');
        if (!tab) return;
        var dot = tab.querySelector('[data-role="error-dot"]');
        if (dot) dot.hidden = !isInvalid;
    }

    // Herbereken het foutstipje van precies één tab, uitgaande van de velden die er nu ECHT nog in
    // staan (i.o.m. enkel het veld dat net gecontroleerd werd) — nodig omdat een blur-check maar één
    // veld tegelijk beoordeelt, terwijl een tab pas veilig "geen fout" mag tonen als ECHT elk
    // [data-gl-v2-required]-veld erin intussen weer geldig is.
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

        ["algemeen", "afdelingen", "contacten"].forEach(refreshTabError);

        return firstInvalid;
    }

    function initRequiredValidation() {
        var form = document.getElementById("gl-v2-supplier-form");
        if (!form) return;

        // Live, per veld: zodra de gebruiker een verplicht veld verlaat (blur) terwijl het leeg is —
        // niet pas bij Opslaan. "focusout" i.p.v. "blur" omdat het moet bubbelen (gedelegeerd op het
        // formulier, werkt dus ook voor rijen die pas later worden toegevoegd).
        form.addEventListener("focusout", function (e) {
            var field = e.target.closest("[data-gl-v2-required]");
            if (!field) return;
            validateField(field);
            var panel = field.closest("[data-tab-panel]");
            refreshTabError(panel ? panel.getAttribute("data-tab-panel") : "algemeen");
        });

        form.addEventListener("submit", function (e) {
            var firstInvalid = validateRequiredFields();
            if (!firstInvalid) return;
            e.preventDefault();
            e.stopImmediatePropagation();
            var panel = firstInvalid.closest("[data-tab-panel]");
            var tabKey = panel ? panel.getAttribute("data-tab-panel") : "algemeen";
            if (activateTab) activateTab(tabKey);
            firstInvalid.scrollIntoView({ behavior: "smooth", block: "center" });
            var input = firstInvalid.querySelector("input, select");
            if (input) input.focus();
        });
    }

    // ── Actiebalk-submit — dubbele-submit-blokkade. ────────────────────────────────────────────
    function initSubmitLoading() {
        var form = document.getElementById("gl-v2-supplier-form");
        var submitBtn = document.getElementById("gl-v2-supplier-submit");
        if (!form || !submitBtn) return;
        form.addEventListener("submit", function () {
            submitBtn.classList.add("is-loading");
            submitBtn.disabled = true;
        });
    }

    // ── "Niet-opgeslagen wijzigingen"-badge (design-handoff 8b/8d) — puur client-side, geen
    //    dirty-tracking server-side. Zodra ze getoond is, blijft ze staan (geen "ongedaan maken tot
    //    exact het origineel"-logica); de generieke input/change-delegatie vangt tekst-/select-/
    //    switch-velden vanzelf, maar niets wat via JS geprogrammeerd wordt (chip-toggle, rij
    //    toevoegen/verwijderen, "Gegevens overnemen") vuurt een echt input/change-event, dus die
    //    plekken roepen markDirty() zelf expliciet aan. ──────────────────────────────────────────
    function markDirty() {
        var badge = document.getElementById("gl-v2-dirty-badge");
        if (badge) badge.hidden = false;
    }

    function initDirtyBadge() {
        var form = document.getElementById("gl-v2-supplier-form");
        if (!form) return;
        form.addEventListener("input", markDirty);
        form.addEventListener("change", markDirty);
    }

    function isFormDirty() {
        var badge = document.getElementById("gl-v2-dirty-badge");
        return !!badge && !badge.hidden;
    }

    // ── Annuleren mét de topbar-terugknop (_LayoutV2.cshtml se eigen .gl-v2-topbar-back) met
    //    wijzigingen → eerst bevestigen (Type 1-modal) — hergebruikt dezelfde dirty-detectie als de
    //    badge hierboven: enkel onderbreken wanneer er ook echt iets te verliezen is, een
    //    ongewijzigd formulier verlaat gewoon meteen. Beide knoppen wijzen naar dezelfde @backUrl
    //    (EditV2.cshtml berekent 'm één keer), dus één gedeeld intercept-gedrag volstaat. ──────────
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

    // ── UBL hangt af van digitale facturatie (design-handoff 8i, "Samenwerking & facturatie") —
    //    de UBL-schakelaar is enkel bedienbaar zolang "Digitale facturatie vereist" aanstaat. De
    //    native disabled-checkbox stuurt zowel de grijze .gl-v2-switch-track (al bestaande CSS-
    //    staat) als het opslaggedrag: Html.CheckBoxFor's eigen verborgen "false"-schaduwveld blijft
    //    ongeacht disabled meesturen, dus een disabled/uitgevinkte UBL-schakelaar bewaart correct
    //    als false, geen aparte serverlogica nodig. ────────────────────────────────────────────────
    function initUblDependency() {
        var digitalInvoice = document.getElementById("gl-v2-requires-digital-invoice");
        var ubl = document.getElementById("gl-v2-attach-ubl");
        if (!digitalInvoice || !ubl) return;

        function apply() {
            ubl.disabled = !digitalInvoice.checked;
        }

        digitalInvoice.addEventListener("change", apply);
        apply();
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
