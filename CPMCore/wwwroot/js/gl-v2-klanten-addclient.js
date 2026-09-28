// gl-v2 layout-pilot — Klanten/AddClientAccountV2.cshtml ("klant toevoegen aan een project", design-
// handoff 23a). Eigen pagina-JS, geen tabbar (in tegenstelling tot gl-v2-klanten-editproject.js) — één
// scrollende pagina met sectiekaarten, dus alle "welke tab is actief"-machinerie van die andere pagina
// vervalt hier. De generieke mechanismen (zoekende select, schakelaars, verdeelsleutel, facturatie-
// voorbeeldkaart, dirty-badge, wijzigingen-niet-opslaan-modal) zijn er wél 1-op-1 van overgenomen —
// zelfde opgeloste patronen, niet opnieuw uitgevonden. window.glV2AddClientConfig (in de view) draagt
// de endpoint-urls.
(function () {
    "use strict";

    var config = window.glV2AddClientConfig || {};
    var backdrop = null;

    initCurrencyFields();
    initSearchSelects(document);
    initClientTypeToggle();
    initSalutationFirstnameToggle(document);
    initInvoiceAddressToggle();
    initUblDependency(document);
    initContactRows();
    initCoOwnerRows();
    initShareTotal();
    initInvoicingSection();
    initUnitPicker();
    initRequiredValidation();
    initSubmitLoading();
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

    // ── Eenheden-kaart: grond-/constructiewaarden (dezelfde .Currencymask-klasse als EditProjectV2se
    //    Eenheden-tab) — hier WEL opnieuw aangeroepen na elke fetch-toevoeging, want deze pagina voegt
    //    rijen dynamisch toe (CurrencyMask.init() zelf scant enkel bij aanroep, niet automatisch). ────
    function initCurrencyFields() {
        if (window.CurrencyMask) window.CurrencyMask.init(".Currencymask");
    }

    // ── Zoekende dropdown (GlV2SearchSelect — postcode/gemeente), zelfde recept als
    //    gl-v2-klanten-editproject.js se wireSearchSelect. ─────────────────────────────────────────
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

        // mousedown (i.p.v. de click zelf) legt vast of het paneel al open stond VÓÓR deze interactie —
        // nodig omdat de focus-listener hieronder het paneel soms al opent nog vóórdat de click zelf
        // afgaat (browser-volgorde: mousedown → focus → click), anders zou de click meteen weer sluiten
        // wat de focus-listener net opende.
        var wasOpenBeforeInteraction = false;
        trigger.addEventListener("mousedown", function () {
            wasOpenBeforeInteraction = panel.classList.contains("is-open");
        });
        trigger.addEventListener("click", function (e) {
            if (e.target.closest('[data-role="clear-trigger"]')) return;
            if (wasOpenBeforeInteraction) {
                closeAllPanels();
            } else {
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
            if (!panel.classList.contains("is-open")) openThisPanel();
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

    // ── Bedrijf/particulier-schakelaar (eigenaar 1). ──────────────────────────────────────────────
    function initClientTypeToggle() {
        var toggle = document.getElementById("gl-v2-is-company");
        if (!toggle) return;

        function apply() {
            document.querySelectorAll('[data-role="company-fields"]').forEach(function (el) {
                el.hidden = !toggle.checked;
            });
            document.querySelectorAll('#gl-v2-kd-billedto option[data-role="billedto-company-option"]').forEach(function (opt) {
                opt.hidden = !toggle.checked;
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
    //    er is dan geen eigen voornaam. Werkt op eigenaar 1 én, via de scope-parameter, op elke (ook
    //    dynamisch toegevoegde) mede-eigenaar-rij — zie initCoOwnerRows. .gl-v2-field-input:disabled
    //    heeft al de grijze opmaak (gl-v2-shell.css). ────────────────────────────────────────────────
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

    // ── "Afwijkend facturatieadres"-schakelaar (eigenaar 1) — meerdere velden i.p.v. één wrapper,
    //    zelfde queryAll-aanpak als de bedrijf-schakelaar hierboven. ─────────────────────────────────
    function initInvoiceAddressToggle() {
        var toggle = document.getElementById("gl-v2-use-invoice-address");
        if (!toggle) return;

        function apply() {
            document.querySelectorAll('[data-role="invoice-address-fields"]').forEach(function (el) {
                el.hidden = !toggle.checked;
            });
        }

        toggle.addEventListener("change", function () {
            apply();
            markDirty();
        });
        apply();
    }

    // ── UBL hangt af van digitale facturatie (contactrijen). ──────────────────────────────────────
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

    // ── Contactpersonen-rijen (migratie-onafhankelijk, bestond al op de legacy AddClientAccount.cshtml
    //    als "Contactgegevens" — ontbrak aanvankelijk op deze gl-v2-versie). Zelfde add-/delete-/
    //    primair-contact-patroon als gl-v2-klanten-editproject.js se Contacten-tab, hier zonder
    //    tabbar-teller. ──────────────────────────────────────────────────────────────────────────────
    function initContactRows() {
        var rows = document.getElementById("gl-v2-contact-rows");
        var addBtn = document.getElementById("gl-v2-add-contact");
        if (!rows) return;

        rows.querySelectorAll('[data-role="ubl-pair"]').forEach(wireUblPair);

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
                        initSalutationFirstnameToggle(rowEl);
                        initUblDependency(rowEl);
                        var emptyHint = document.getElementById("gl-v2-kd-contacts-empty");
                        if (emptyHint) emptyHint.hidden = true;
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
                if (!rows.querySelector(".gl-v2-client-row")) {
                    var emptyHint = document.getElementById("gl-v2-kd-contacts-empty");
                    if (emptyHint) emptyHint.hidden = false;
                }
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

    // ── Mede-eigenaars-rijen — zelfde toggle-/add-/delete-patroon als gl-v2-klanten-editproject.js,
    //    hier met "ClientAccount.CoOwners" als collectienaam (BlankCoOwnerRow-URL draagt die al mee). ─
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
                        var card = rowEl.querySelector(".gl-v2-card");
                        if (card) {
                            applyCoOwnerCompanyToggle(card);
                            applyCoOwnerInvoiceToggle(card);
                        }
                        markDirty();
                        recalcShares();
                        updateInvoicePreview();
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
            markDirty();
            recalcShares();
            updateInvoicePreview();
        });

        rows.addEventListener("input", function (e) {
            if (e.target.matches('[data-role="coowner-percentage"]') || /CoOwnerPercentage$/.test(e.target.name || "")) {
                recalcShares();
                updateInvoicePreview();
            }
        });
    }

    // ── Verdeelsleutel (migratie 057) — som van eigenaar 1 se aandeel + alle zichtbare mede-
    //    eigenaar-percentages, live herberekend. "Gelijk verdelen" verdeelt 100 % gelijk (rest naar
    //    eigenaar 1). Zelfde recept als gl-v2-klanten-editproject.js. ────────────────────────────────
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
        updateAccountSummary();
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
                updateInvoicePreview();
            });
        }
        recalcShares();
    }

    // ── Facturatie-sectie (migratie 057): "op naam van"-afhankelijk, ontvangers-chips, live
    //    voorbeeldkaart. Enkel weergave/bewaren — geen echte facturatie-aanroep. ────────────────────
    function invoicingPreviewData() {
        var mode = document.querySelector('#gl-v2-kep-invmode input:checked');
        var perOwner = !!mode && mode.getAttribute("data-role") === "invmode-perowner";
        var nameEl = document.querySelector('[name$="ClientAccount.Name"]');
        var name = (nameEl && nameEl.value) || "Eigenaar 1";
        var owners = [{ name: name, pct: 100 }];
        if (perOwner) {
            owners = [];
            var owner1Pct = parseFloat(((document.querySelector('[data-role="share-input"]') || {}).value || "100").replace(",", ".")) || 0;
            owners.push({ name: name, pct: owner1Pct });
            document.querySelectorAll("#gl-v2-coowner-rows .gl-v2-card").forEach(function (card) {
                var coNameEl = card.querySelector('input[name$=".Name"]');
                var pctEl = card.querySelector('input[name$="CoOwnerPercentage"]');
                var pct = parseFloat(((pctEl || {}).value || "0").replace(",", ".")) || 0;
                owners.push({ name: (coNameEl && coNameEl.value) || "Mede-eigenaar", pct: pct });
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

    function initInvoicingSection() {
        var section = document.getElementById("gl-v2-kd-facturatie");
        if (!section) return;

        var recipients = section.querySelector('[data-role="recipients"]');
        var addInput = document.getElementById("gl-v2-kep-recipient-add");
        var suggestionsHost = section.querySelector('[data-role="recipient-suggestions"]');
        var billedToSelect = section.querySelector('[data-role="billedto-select"]');
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

        // ── "Snel toevoegen": de e-mails die op deze pagina al ingevuld staan (eigenaar 1, mede-
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

        section.addEventListener("change", updateInvoicePreview);
        document.addEventListener("input", function (e) {
            if (shareInputs().indexOf(e.target) >= 0) updateInvoicePreview();
            if (e.target.matches('[name$="ClientAccount.Name"]')) updateInvoicePreview();
            if (e.target.matches('input[name$=".Email"], input[name$=".Name"], input[name$=".Firstname"]')) renderSuggestions();
        });
        document.addEventListener("click", function (e) {
            if (e.target.closest(".js-gl-v2-delete-contact-row, .js-gl-v2-delete-coowner-row")) window.setTimeout(renderSuggestions, 0);
        });
        updateInvoicePreview();
        renderSuggestions();
    }

    // ── Eenheden-kiezer — leent het interactiepatroon van _ModalAttachUnitV2 (gegroepeerde lijst),
    //    hier als eenvoudige <select> + knop i.p.v. een modal (23a toont de kiezer rechtstreeks op de
    //    pagina, geen apart venster). Elke toevoeging haalt de echte kaart op bij AddSelectedUnits
    //    (dezelfde actie/partial als EditProjectV2 se Eenheden-tab), verwijderen zet de eenheid terug
    //    in de keuzelijst zodat ze opnieuw gekozen kan worden. ──────────────────────────────────────
    // ── Actiebalk-samenvatting (23a: "2 eigenaars · 1 eenheid · verdeelsleutel 100 %"). ──────────
    function updateAccountSummary() {
        var el = document.getElementById("gl-v2-kd-summary");
        if (!el) return;
        var owners = 1 + document.querySelectorAll("#gl-v2-coowner-rows .gl-v2-client-row").length;
        var units = document.querySelectorAll("#gl-v2-kd-unit-rows .gl-v2-addclient-unit-row").length;
        var shareEl = document.querySelector('[data-role="share-value"]');
        var share = shareEl ? shareEl.textContent.trim() : "100";
        el.textContent = owners + (owners === 1 ? " eigenaar" : " eigenaars")
            + " · " + units + (units === 1 ? " eenheid" : " eenheden")
            + " · verdeelsleutel " + share + " %";
    }

    // ── Bedragen: de GlV2Currency-velden dragen "121.000,00" (nl-BE, via CurrencyMask) of "121000.00"
    //    (rauw, vóór de mask) — beide lezen. ───────────────────────────────────────────────────────
    function parseMoney(value) {
        var v = String(value || "").trim();
        if (!v) return 0;
        if (v.indexOf(",") >= 0) v = v.replace(/\./g, "").replace(",", ".");
        return parseFloat(v.replace(/[^\d.\-]/g, "")) || 0;
    }

    function formatMoney(amount) {
        return "€ " + amount.toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function setMoneyField(input, raw) {
        // Via AutoNumeric als die aan het veld hangt (CurrencyMask), anders rechtstreeks.
        try {
            var an = window.AutoNumeric && window.AutoNumeric.getAutoNumericElement ? window.AutoNumeric.getAutoNumericElement(input) : null;
            if (an) { an.set(parseFloat(raw) || 0); return; }
        } catch (err) { /* geen AutoNumeric-instantie: val terug op de rauwe waarde */ }
        input.value = raw;
    }

    // ── Eenhedenkiezer (design-handoff 23a/23b) — zoekende dropdown (zelfde trigger/paneel-shell als
    //    GlV2SearchSelect, maar de volledige projectlijst zit al in de pagina: beschikbaar = kiesbaar,
    //    verkocht/in optie = zichtbaar maar niet kiesbaar mét de koper als reden). "Eenheid toevoegen"
    //    haalt de echte kaart op bij AddSelectedUnits (zelfde actie/partial als EditProjectV2 se
    //    Eenheden-tab); verwijderen zet de eenheid terug in de lijst. Elke kaart telt live haar totaal,
    //    het account-totaal staat eronder, en "Herstellen" zet de uit Eenheden voorgestelde prijzen terug. ─
    function initUnitPicker() {
        var root = document.getElementById("gl-v2-kd-unit-picker");
        var addBtn = document.getElementById("gl-v2-kd-unit-add");
        var rows = document.getElementById("gl-v2-kd-unit-rows");
        var emptyHint = document.getElementById("gl-v2-kd-units-empty");
        var totalBox = document.getElementById("gl-v2-kd-units-total");
        if (!root || !addBtn || !rows || !config.addUnitUrl) return;

        var hidden = root.querySelector('input[type="hidden"]');
        var trigger = root.querySelector(".gl-v2-select-trigger");
        var label = root.querySelector(".gl-v2-select-trigger-label");
        var clearTrigger = root.querySelector('[data-role="clear-trigger"]');
        var panel = root.querySelector('[data-role="panel"]');
        var searchInput = panel && panel.querySelector('[data-role="input"]');
        if (!hidden || !trigger || !label || !panel || !searchInput) return;
        var placeholder = root.getAttribute("data-search-placeholder") || "Zoek een beschikbare eenheid …";
        var selectedOption = null;

        function options() { return Array.prototype.slice.call(panel.querySelectorAll(".gl-v2-kd-upk-option")); }
        function isChoosable(opt) { return !opt.classList.contains("is-disabled") && !opt.hasAttribute("data-in-account"); }

        function applyFilter() {
            var term = (searchInput.value || "").trim().toLowerCase();
            var anyChoosable = false;
            options().forEach(function (opt) {
                var matches = !term || (opt.getAttribute("data-search") || "").indexOf(term) >= 0;
                var show = matches && !opt.hasAttribute("data-in-account");
                opt.hidden = !show;
                if (show && isChoosable(opt)) anyChoosable = true;
            });
            var emptyEl = panel.querySelector('[data-role="picker-empty"]');
            if (emptyEl) emptyEl.hidden = anyChoosable;
        }

        function setSelected(opt) {
            selectedOption = opt;
            options().forEach(function (o) {
                var on = o === opt;
                o.classList.toggle("is-selected", on);
                o.setAttribute("aria-selected", on ? "true" : "false");
            });
            hidden.value = opt ? opt.getAttribute("data-unit-id") : "";
            label.textContent = opt ? opt.getAttribute("data-unit-name") : placeholder;
            trigger.classList.toggle("is-filled", !!opt);
            addBtn.disabled = !opt;
        }

        function openThis() {
            openPanel(trigger, panel);
            searchInput.value = "";
            applyFilter();
            searchInput.focus();
        }

        var wasOpenBeforeInteraction = false;
        trigger.addEventListener("mousedown", function () { wasOpenBeforeInteraction = panel.classList.contains("is-open"); });
        trigger.addEventListener("click", function (e) {
            if (e.target.closest('[data-role="clear-trigger"]')) return;
            if (wasOpenBeforeInteraction) closeAllPanels(); else openThis();
        });
        trigger.addEventListener("keydown", function (e) {
            if (e.target.closest('[data-role="clear-trigger"]')) return;
            if (e.key !== "Enter" && e.key !== " ") return;
            e.preventDefault();
            if (panel.classList.contains("is-open")) closeAllPanels(); else openThis();
        });
        trigger.addEventListener("focus", function () { if (!panel.classList.contains("is-open")) openThis(); });
        searchInput.addEventListener("click", function (e) { e.stopPropagation(); });
        searchInput.addEventListener("input", applyFilter);
        if (clearTrigger) {
            clearTrigger.addEventListener("click", function (e) {
                e.stopPropagation();
                setSelected(null);
                closeAllPanels();
            });
        }
        panel.addEventListener("click", function (e) {
            var opt = e.target.closest(".gl-v2-kd-upk-option");
            if (!opt || !isChoosable(opt)) return;
            setSelected(opt);
            closeAllPanels();
        });

        function findOption(unitId) {
            return panel.querySelector('.gl-v2-kd-upk-option[data-unit-id="' + unitId + '"]');
        }

        function updateUnitTotals() {
            var account = 0;
            rows.querySelectorAll(".gl-v2-addclient-unit-row").forEach(function (card) {
                var sum = 0;
                card.querySelectorAll("input.Currencymask").forEach(function (input) { sum += parseMoney(input.value); });
                var totalEl = card.querySelector('[data-role="unit-total"]');
                if (totalEl) totalEl.textContent = formatMoney(sum);
                account += sum;
            });
            var accountEl = totalBox && totalBox.querySelector('[data-role="units-total"]');
            if (accountEl) accountEl.textContent = formatMoney(account);
        }

        function refreshState() {
            var hasRows = rows.querySelectorAll(".gl-v2-addclient-unit-row").length > 0;
            if (emptyHint) emptyHint.hidden = hasRows;
            if (totalBox) totalBox.hidden = !hasRows;
            updateUnitTotals();
            updateAccountSummary();
        }

        function resetCardDefaults(card) {
            var land = card.getAttribute("data-default-land");
            var landInput = card.querySelector('input.Currencymask[name$=".LandValueSold"]');
            if (landInput && land) setMoneyField(landInput, land);
            var cvs = [];
            try { cvs = JSON.parse(card.getAttribute("data-default-cvs") || "[]"); } catch (err) { cvs = []; }
            cvs.forEach(function (cv) {
                var input = card.querySelector('input.Currencymask[name$="ConstructionValues[' + cv.i + '].ValueSold"]');
                if (input) setMoneyField(input, cv.v);
            });
            updateUnitTotals();
            markDirty();
        }

        addBtn.addEventListener("click", function (e) {
            e.preventDefault();
            var opt = selectedOption;
            if (!opt) return;
            var unitId = opt.getAttribute("data-unit-id");
            var url = config.addUnitUrl + "?unitId=" + encodeURIComponent(unitId)
                + "&unitName=" + encodeURIComponent(opt.getAttribute("data-unit-name") || "")
                + "&unitGroup=" + encodeURIComponent(opt.getAttribute("data-unit-group") || "");
            addBtn.disabled = true;
            // AddSelectedUnits is [HttpPost] — de parameters blijven in de query string (ASP.NET Core
            // bindt eenvoudige parameters standaard ook vanuit de query op een POST).
            fetch(url, { method: "POST" })
                .then(function (r) { return r.text(); })
                .then(function (html) {
                    var fragment = document.createElement("div");
                    fragment.innerHTML = html;
                    var rowEl = fragment.firstElementChild;
                    if (!rowEl) return;
                    rows.appendChild(rowEl);
                    opt.setAttribute("data-in-account", "1");
                    setSelected(null);
                    initCurrencyFields();
                    refreshState();
                    markDirty();
                })
                .catch(function () {
                    if (window.GlV2Toast) window.GlV2Toast.show({ tone: "danger", title: "Fout", body: "Kon geen eenheid toevoegen." });
                    addBtn.disabled = !selectedOption;
                });
        });

        rows.addEventListener("click", function (e) {
            var resetBtn = e.target.closest('[data-role="unit-reset"]');
            if (resetBtn) {
                var resetCard = resetBtn.closest(".gl-v2-addclient-unit-row");
                if (resetCard) resetCardDefaults(resetCard);
                return;
            }
            var deleteBtn = e.target.closest(".js-gl-v2-delete-unit-row");
            if (!deleteBtn) return;
            var card = deleteBtn.closest(".gl-v2-addclient-unit-row");
            if (!card) return;
            var returned = findOption(card.getAttribute("data-unit-id"));
            if (returned) returned.removeAttribute("data-in-account");
            card.remove();
            refreshState();
            markDirty();
        });

        rows.addEventListener("input", function (e) {
            if (e.target.matches("input.Currencymask")) updateUnitTotals();
        });
        rows.addEventListener("change", function (e) {
            if (e.target.matches("input.Currencymask")) updateUnitTotals();
        });

        setSelected(null);
        refreshState();
    }

    // ── Verplichte-veldencontrole (Naam is voorlopig het enige harde vereiste, zelfde controle als
    //    de legacy pagina). Geen tabs hier, dus enkel scrollen-naar + focus, geen tab-activatie. ──────
    function resolveFieldTarget(field) {
        return field.classList.contains("gl-v2-field") ? field : (field.querySelector(".gl-v2-field") || field);
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
        var firstInvalid = null;
        document.querySelectorAll("[data-gl-v2-required]").forEach(function (field) {
            if (validateField(field) && !firstInvalid) firstInvalid = field;
        });
        return firstInvalid;
    }

    function initRequiredValidation() {
        var form = document.getElementById("gl-v2-client-form");
        if (!form) return;

        form.addEventListener("focusout", function (e) {
            var field = e.target.closest("[data-gl-v2-required]");
            if (field) validateField(field);
        });

        form.addEventListener("submit", function (e) {
            var firstInvalid = validateRequiredFields();
            if (!firstInvalid) return;
            e.preventDefault();
            e.stopImmediatePropagation();
            firstInvalid.scrollIntoView({ behavior: "smooth", block: "center" });
            var input = firstInvalid.querySelector("input, select");
            if (input) input.focus();
        });
    }

    // ── Actiebalk-submit — dubbele-submit-blokkade. ────────────────────────────────────────────
    function initSubmitLoading() {
        var form = document.getElementById("gl-v2-client-form");
        var submitBtn = document.getElementById("gl-v2-kd-submit");
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

    // ── Annuleren met wijzigingen → eerst bevestigen. ──────────────────────────────────────────────
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
})();
