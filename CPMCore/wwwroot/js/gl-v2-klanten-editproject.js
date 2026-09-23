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
                        var card = rowEl.querySelector(".gl-v2-card");
                        if (card) {
                            applyCoOwnerCompanyToggle(card);
                            applyCoOwnerInvoiceToggle(card);
                        }
                        updateTabCount("medeeigenaars", rows, ".gl-v2-client-row");
                        markDirty();
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
        });
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
