// gl-v2 layout-pilot — Klanten/EditV2.cshtml ÉN CreateV2.cshtml. Eigen pagina-JS (zelfde "eigen
// <pagina>.js"-conventie als gl-v2-leveranciers-form.js, waarvan dit bestand de directe hertoepassing
// is — Leveranciers/EditV2 was de eerste gl-v2-formulierpagina, dit is 'm toegepast op Klanten). Was
// gl-v2-klanten-edit.js, hernoemd toen CreateV2 bleek dit bestand ongewijzigd te kunnen hergebruiken
// (geen Id-afhankelijke logica erin). Vanille JS, geen jQuery. window.glV2ClientFormConfig
// (Edit/CreateV2.cshtml) draagt de endpoint-urls + het BE-land-id + de server-bepaalde force-tab.
(function () {
    "use strict";

    var config = window.glV2ClientFormConfig || {};
    var backdrop = null;
    var activateTab = null;

    initTabs();
    initMultiSelects();
    initSearchSelects();
    initClientTypeToggle();
    initInvoiceAddressToggle();
    initAddressToggles();
    initBtwCheck();
    initVatModalApply();
    initContactRows();
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
    // .gl-v2-select-backdrop is per-ontwerp enkel op mobiel (<768px) echt zichtbaar/klikbaar — op
    // desktop/tablet doet 'm niets, vandaar deze document-brede klik-erbuiten-sluit-luisteraar.
    document.addEventListener("click", function (e) {
        if (e.target.closest(".gl-v2-select, .gl-v2-select-panel")) return;
        closeAllPanels();
    });
    window.addEventListener("resize", closeAllPanels);

    // ── Tabs ────────────────────────────────────────────────────────────────────────────────────
    function initTabs() {
        var tabbar = document.getElementById("gl-v2-client-tabbar");
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

    // ── Meervoudige kiezer (Facturatiebedrijven) — volledig client-side, opties staan al server-
    //    gerenderd. ─────────────────────────────────────────────────────────────────────────────
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
                var field = root.closest(".gl-v2-field");
                if (field) refreshTabError(fieldTabKey(field));
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
                // Zelfde fix als gl-v2-leveranciers-form.js se eigen initMultiSelects(): het
                // zoekveld deed enkel e.stopPropagation() op klik, dus een klik precies óp het veld
                // (i.p.v. ernaast) bereikte de trigger se eigen open/dicht-toggle nooit — "gaat niet
                // consistent open". focus/input openen nu zelf expliciet (idempotent).
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

    // ── Zoekende dropdown (GlV2SearchSelect — postcode/gemeente, hoofdadres én facturatieadres). ──
    function initSearchSelects() {
        document.querySelectorAll("[data-gl-v2-search-select]").forEach(wireSearchSelect);
    }

    // Paneelskelet wordt ÉÉN keer opgebouwd, niet opnieuw bij elke klik — zie de uitgebreide
    // toelichting in gl-v2-leveranciers-edit.js se eigen wireSearchSelect (zelfde bug/fix hier
    // hergebruikt): panel.innerHTML volledig herschrijven bij elke zoekopdracht zou het zoekveld
    // zélf mee wegvegen.
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
            var wrapper = root.closest('[data-role="be-address-wrapper"], [data-role="invoice-be-address-wrapper"]');
            if (wrapper && wrapper.getAttribute("data-role") === "invoice-be-address-wrapper") {
                var invoiceCountrySelect = document.querySelector('[data-role="invoice-country-select"]');
                if (invoiceCountrySelect && invoiceCountrySelect.value) return invoiceCountrySelect.value;
            }
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

    // ── Bedrijf/particulier-schakelaar — toont/verbergt de Bedrijfsnaam+Ondernemingsnummer- resp.
    //    Aanspreking+Naam-velden. Verplichte-veldencontrole hoeft hier niets speciaals voor te doen:
    //    validateField() slaat elk veld binnen een [hidden]-voorouder vanzelf over. ─────────────────
    function initClientTypeToggle() {
        var toggle = document.querySelector('[data-role="client-type-toggle"]');
        var companyFields = document.querySelector('[data-role="company-fields"]');
        var personFields = document.querySelector('[data-role="person-fields"]');
        if (!toggle || !companyFields || !personFields) return;

        function apply() {
            companyFields.hidden = !toggle.checked;
            personFields.hidden = toggle.checked;
            ["algemeen"].forEach(refreshTabError);
        }

        toggle.addEventListener("change", function () {
            apply();
            markDirty();
        });
        apply();
    }

    // ── "Ander facturatieadres"-schakelaar — toont/verbergt het volledige facturatieadresblok. ────
    function initInvoiceAddressToggle() {
        var toggle = document.querySelector('[data-role="invoice-address-toggle"]');
        var wrapper = document.querySelector('[data-role="invoice-address-wrapper"]');
        if (!toggle || !wrapper) return;

        function apply() {
            wrapper.hidden = !toggle.checked;
        }

        toggle.addEventListener("change", function () {
            apply();
            markDirty();
        });
        apply();
    }

    // ── BE/niet-BE-adrestoggle — twee onafhankelijke instanties (hoofdadres + facturatieadres),
    //    zelfde gedrag als Leveranciers/EditV2 se eigen (enkelvoudige) initAddressToggle, hier
    //    veralgemeend naar een herbruikbare wireAddressToggle(refs) per adresblok. De "andere land"-
    //    tak had in de Leveranciers-versie ongebruikte otherPostalInput/otherCityInput-variabelen
    //    (geen listener) — hier wél gewired: getypte tekst in het handmatige postcode-/gemeenteveld
    //    synct naar het gedeelde, benoemde PostalCode/City-hidden-veld (zie EditV2.cshtml se eigen
    //    toelichting: het niet-BE-veld draagt zelf ook al name="PostalCode"/"City", deze sync is dus
    //    vooral voor het BE-hidden-tegenpaar dat anders stil zou blijven als je nadien terugschakelt). ──
    function initAddressToggles() {
        wireAddressToggle({
            countrySelect: document.querySelector('[data-role="country-select"]'),
            beWrapper: document.querySelector('[data-role="be-address-wrapper"]'),
            otherWrapper: document.querySelector('[data-role="other-address-wrapper"]'),
            bePostalHidden: document.querySelector('[data-role="be-postal-hidden"]'),
            beCityHidden: document.querySelector('[data-role="be-city-hidden"]'),
            otherPostalInput: document.querySelector('[data-role="other-postal-input"]'),
            otherCityInput: document.querySelector('[data-role="other-city-input"]')
        });
        wireAddressToggle({
            countrySelect: document.querySelector('[data-role="invoice-country-select"]'),
            beWrapper: document.querySelector('[data-role="invoice-be-address-wrapper"]'),
            otherWrapper: document.querySelector('[data-role="invoice-other-address-wrapper"]'),
            bePostalHidden: document.querySelector('[data-role="invoice-be-postal-hidden"]'),
            beCityHidden: document.querySelector('[data-role="invoice-be-city-hidden"]'),
            otherPostalInput: document.querySelector('[data-role="invoice-other-postal-input"]'),
            otherCityInput: document.querySelector('[data-role="invoice-other-city-input"]')
        });
    }

    function wireAddressToggle(refs) {
        if (!refs.countrySelect || !refs.beWrapper || !refs.otherWrapper) return;

        function isBelgian() {
            var opt = refs.countrySelect.options[refs.countrySelect.selectedIndex];
            return opt && (opt.getAttribute("data-isocode") || "").toUpperCase() === "BE";
        }

        function apply() {
            var belgian = isBelgian();
            refs.beWrapper.hidden = !belgian;
            refs.otherWrapper.hidden = belgian;
        }

        refs.countrySelect.addEventListener("change", function () {
            apply();
            markDirty();
        });

        var searchSelectRoot = refs.beWrapper.querySelector("[data-gl-v2-search-select]");
        if (searchSelectRoot && refs.bePostalHidden && refs.beCityHidden) {
            searchSelectRoot.addEventListener("gl-v2:postal-selected", function (e) {
                var parts = (e.detail && e.detail.text || "").split(" - ");
                refs.bePostalHidden.value = parts[0] || "";
                refs.beCityHidden.value = parts.slice(1).join(" - ").trim();
            });
        }

        if (refs.otherPostalInput && refs.bePostalHidden) {
            refs.otherPostalInput.addEventListener("input", function () {
                refs.bePostalHidden.value = refs.otherPostalInput.value;
            });
        }
        if (refs.otherCityInput && refs.beCityHidden) {
            refs.otherCityInput.addEventListener("input", function () {
                refs.beCityHidden.value = refs.otherCityInput.value;
            });
        }

        apply();
    }

    // ── Ondernemingsnummer automatisch formatteren naar 0000.000.000 terwijl je tikt (design-
    //    handoff 8f·1) — zelfde .is-tabular-veldklasse als Leveranciers se eigen velden. ─────────────
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

    // ── "Gegevens overnemen" — kopieert de btw-opzoekgegevens naar Bedrijfsnaam + het hoofdadres
    //    (niet het facturatieadres — dat blijft de eigen keuze van de gebruiker), lost daarna de
    //    postcode op via FindPostalMatch. ──────────────────────────────────────────────────────────
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

            if (name) document.getElementById("CompanyName").value = name;
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
            markDirty();
        });
    }

    // ── Contacten-rijen — toevoegen (fetch blanco rij) / verwijderen. Geen departement-koppeling
    //    (Klanten heeft dat concept niet), wel de per-rij UBL-afhankelijkheid opnieuw wiren zodra
    //    een nieuwe rij toegevoegd wordt (zie initUblDependency/wireUblPair hieronder). ─────────────
    function initContactRows() {
        var contactRows = document.getElementById("gl-v2-contact-rows");
        var addContactBtn = document.getElementById("gl-v2-add-contact");
        var contactCountEl = document.querySelector('[data-role="contacten-count"]');

        function updateCount() {
            if (contactCountEl && contactRows) {
                contactCountEl.textContent = contactRows.querySelectorAll(".gl-v2-client-row").length;
            }
        }

        if (addContactBtn && contactRows) {
            addContactBtn.addEventListener("click", function (e) {
                e.preventDefault();
                fetch(config.addContactUrl)
                    .then(function (r) { return r.text(); })
                    .then(function (html) {
                        var fragment = document.createElement("div");
                        fragment.innerHTML = html;
                        var rowEl = fragment.firstElementChild;
                        if (!rowEl) return;
                        contactRows.appendChild(rowEl);
                        rowEl.querySelectorAll("[data-gl-v2-search-select]").forEach(wireSearchSelect);
                        rowEl.querySelectorAll('[data-role="ubl-pair"]').forEach(wireUblPair);
                        updateCount();
                        markDirty();
                    })
                    .catch(function () {
                        if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Fout", body: "Kon geen nieuw contact toevoegen." });
                    });
            });

            contactRows.addEventListener("click", function (e) {
                var deleteBtn = e.target.closest(".js-gl-v2-delete-contact-row");
                if (deleteBtn) {
                    deleteBtn.closest(".gl-v2-client-row").remove();
                    updateCount();
                    markDirty();
                    return;
                }

                var primaryBtn = e.target.closest(".js-gl-v2-primary-contact-toggle");
                if (primaryBtn) {
                    e.preventDefault();
                    setPrimaryContact(primaryBtn.closest(".gl-v2-client-row"));
                    markDirty();
                }
            });
        }

        // Slechts één contact mag primair zijn — bij een klik wordt de eigen rij aan/uit gezet
        // en worden alle andere rijen defensief uitgezet (zelfde exclusiviteit als de server-side
        // normalisatie in KlantenController/ClientAccountTranslator.NormalizePrimaryContact).
        function setPrimaryContact(row) {
            if (!row || !contactRows) return;
            var input = row.querySelector(".js-gl-v2-primary-contact-input");
            var btn = row.querySelector(".js-gl-v2-primary-contact-toggle");
            if (!input || !btn) return;
            var makePrimary = !input.checked;

            contactRows.querySelectorAll(".gl-v2-client-row").forEach(function (otherRow) {
                var otherInput = otherRow.querySelector(".js-gl-v2-primary-contact-input");
                var otherBtn = otherRow.querySelector(".js-gl-v2-primary-contact-toggle");
                if (!otherInput || !otherBtn) return;
                var isThisRow = otherRow === row;
                var checked = isThisRow && makePrimary;
                otherInput.checked = checked;
                otherBtn.classList.toggle("is-primary", checked);
                otherBtn.setAttribute("aria-pressed", checked ? "true" : "false");
                var icon = otherBtn.querySelector("i");
                if (icon) icon.className = "ph ph-star";
            });
        }
    }

    // ── Verplichte-veldencontrole (design-handoff 8d) — bij Opslaan, client-side vóór de server-
    //    ronde: elk [data-gl-v2-required]-veld (Facturatiebedrijven, Bedrijfsnaam-of-Aanspreking+
    //    Naam afhankelijk van de bedrijf/particulier-schakelaar, elke contactrij se eigen Naam) dat
    //    leeg is krijgt .is-error + een foutregel, de bijhorende tab krijgt het rode stipje. ─────────
    function isFieldElementEmpty(field) {
        // Meervoudige kiezers (Facturatiebedrijven) hebben bij nul selecties GEEN enkele hidden
        // input (niet één met een lege waarde) — controleer daarom eerst of er een hidden-inputs-
        // host is en tel die, in plaats van door te vallen naar de generieke input/select-check
        // hieronder (die anders het filter-tikveld zou beoordelen, niet de echte selectie).
        var hiddenHost = field.querySelector('[data-role="hidden-inputs"]');
        if (hiddenHost) return hiddenHost.querySelectorAll('[data-role="hidden-input"]').length === 0;
        var hiddenInput = field.querySelector('input[type="hidden"]');
        if (hiddenInput) return !hiddenInput.value;
        var input = field.querySelector("input, select");
        return !input || !input.value || !input.value.trim();
    }

    function resolveFieldTarget(field) {
        if (field.classList.contains("gl-v2-field")) return field;
        return field.querySelector(".gl-v2-field") || field;
    }

    function setFieldError(field, message, isInvalid) {
        var target = resolveFieldTarget(field);
        target.classList.toggle("is-error", isInvalid);
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

    function fieldTabKey(field) {
        var panel = field.closest("[data-tab-panel]");
        return panel ? panel.getAttribute("data-tab-panel") : "algemeen";
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

        ["algemeen", "contacten"].forEach(refreshTabError);

        return firstInvalid;
    }

    function initRequiredValidation() {
        var form = document.getElementById("gl-v2-client-form");
        if (!form) return;

        form.addEventListener("focusout", function (e) {
            var field = e.target.closest("[data-gl-v2-required]");
            if (!field) return;
            validateField(field);
            refreshTabError(fieldTabKey(field));
        });

        form.addEventListener("submit", function (e) {
            var firstInvalid = validateRequiredFields();
            if (!firstInvalid) return;
            e.preventDefault();
            e.stopImmediatePropagation();
            var tabKey = fieldTabKey(firstInvalid);
            if (activateTab) activateTab(tabKey);
            firstInvalid.scrollIntoView({ behavior: "smooth", block: "center" });
            var input = firstInvalid.querySelector("input, select");
            if (input) input.focus();
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

    // ── "Niet-opgeslagen wijzigingen"-badge — zelfde puur client-side aanpak als Leveranciers/
    //    EditV2. ──────────────────────────────────────────────────────────────────────────────────
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

    // ── Annuleren met wijzigingen → eerst bevestigen (Type 1-modal). ──────────────────────────────
    // Zowel Annuleren als de topbar-terugknop (_LayoutV2.cshtml se eigen .gl-v2-topbar-back) wijzen
    // naar dezelfde @backUrl (EditV2.cshtml berekent 'm één keer), dus één gedeeld intercept-gedrag
    // volstaat voor beide.
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

    // ── UBL hangt af van digitale facturatie — hier op TWEE plekken (het hoofdformulier se eigen
    //    "Facturatievoorkeuren"-kaart, én elke contactrij afzonderlijk), vandaar een herbruikbare
    //    wireUblPair() i.p.v. Leveranciers se eigen enkelvoudige, hardgecodeerde id-paar. Native
    //    disabled-checkbox stuurt zowel de grijze .gl-v2-switch-track als het opslaggedrag: Html.
    //    CheckBoxFor's verborgen "false"-schaduwveld blijft ongeacht disabled meesturen. ────────────
    function wireUblPair(pair) {
        var digital = pair.querySelector('[data-role="digital-invoice-toggle"]');
        var ubl = pair.querySelector('[data-role="ubl-toggle"]');
        if (!digital || !ubl) return;

        function apply() {
            ubl.disabled = !digital.checked;
        }

        digital.addEventListener("change", apply);
        apply();
    }

    function initUblDependency() {
        document.querySelectorAll('[data-role="ubl-pair"]').forEach(wireUblPair);
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
