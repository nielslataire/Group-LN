// gl-v2 layout-pilot — Projecten/UnitFormV2.cshtml (design-handoff 16b "Eenheid bewerken" + 16c "Nieuwe
// eenheid"). Eigen pagina-JS (zelfde "eigen <pagina>.js"-conventie als gl-v2-projecten-editcontract.js).
// Vanille JS behalve waar AutoNumeric/currency.js het vereist. window.glV2UnitFormConfig (in de view)
// draagt de endpoint-urls en het aandelentotaal — Razor kan niet in een los .js-bestand.
(function () {
    "use strict";

    var config = window.glV2UnitFormConfig || {};
    var form = document.getElementById("gl-v2-unit-form");
    if (!form) return;

    // De interne scroller (zie .gl-v2-unit-scroll in de CSS) — niet .gl-v2-content zelf.
    var scroller = document.querySelector(".gl-v2-unit-scroll") || null;

    // ── Kleine helpers ──────────────────────────────────────────────────────────────────────────
    function $(sel, root) { return (root || document).querySelector(sel); }
    function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }
    function uid() { return "n" + Date.now().toString(36) + Math.random().toString(36).slice(2, 7); }
    function cloneTemplate(id, replacements) {
        var tpl = document.getElementById(id);
        if (!tpl) return null;
        var html = tpl.innerHTML;
        Object.keys(replacements).forEach(function (token) { html = html.split(token).join(replacements[token]); });
        var wrap = document.createElement("div");
        wrap.innerHTML = html.trim();
        return wrap.firstElementChild;
    }
    function initFields(root) {
        if (window.CurrencyMask) window.CurrencyMask.init($$(".Currencymask", root));
        if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(root);
    }
    function numberOf(el) {
        if (!el) return 0;
        if (window.AutoNumeric) {
            var an = window.AutoNumeric.getAutoNumericElement(el);
            if (an) return an.getNumber() || 0;
        }
        var v = parseFloat(String(el.value || "").replace(/\./g, "").replace(",", "."));
        return isNaN(v) ? 0 : v;
    }
    function euro(v, decimals) {
        try {
            return v.toLocaleString("nl-BE", { style: "currency", currency: "EUR", minimumFractionDigits: decimals, maximumFractionDigits: decimals });
        } catch (e) { return "€ " + v.toFixed(decimals); }
    }
    function euroCompact(v) {
        if (!v) return "";
        if (v >= 1000000) return "€ " + (v / 1000000).toFixed(2).replace(".", ",") + "M";
        return "€ " + Math.round(v / 1000) + "k";
    }
    function toast(title, body, tone) {
        if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone || "info", title: title, body: body });
    }

    // ── Niet-opgeslagen wijzigingen (zelfde recept als gl-v2-klanten-editproject.js) ───────────
    function markDirty() {
        var badge = document.getElementById("gl-v2-unit-dirty-badge");
        if (badge) badge.hidden = false;
    }
    function isDirty() {
        var badge = document.getElementById("gl-v2-unit-dirty-badge");
        return !!badge && !badge.hidden;
    }
    var discardModalEl = document.getElementById("gl-v2-unit-discard-changes-modal");
    var discardConfirm = document.getElementById("gl-v2-unit-discard-changes-confirm");
    /** Navigeert naar url; met openstaande wijzigingen eerst de bevestigingsmodal. */
    function navigateGuarded(url) {
        if (!isDirty() || !discardModalEl || !discardConfirm || !window.bootstrap) { window.location.href = url; return; }
        discardConfirm.href = url;
        window.bootstrap.Modal.getOrCreateInstance(discardModalEl).show();
    }
    function initDiscardChanges() {
        var triggers = [document.getElementById("gl-v2-unit-cancel-link"), document.getElementById("gl-v2-topbar-back-link")].filter(Boolean);
        if (!triggers.length || !discardModalEl || !discardConfirm || !window.bootstrap) return;
        triggers.forEach(function (trigger) {
            trigger.addEventListener("click", function (e) {
                if (!isDirty()) return;
                e.preventDefault();
                navigateGuarded(trigger.href);
            });
        });
    }

    // ── Sectielijst: springen + meescrollen (8b) ────────────────────────────────────────────────
    // Twee weergaven van dezelfde sectielijst: de klevende kolom links (≥1024px) en de standaard tabbar
    // (<1024px) — beide dragen data-sec en worden samen bijgehouden.
    var navItems = $$(".gl-v2-unit-secnav-item, .gl-v2-unit-tab");
    function setActiveNav(key) {
        navItems.forEach(function (a) {
            var active = a.getAttribute("data-sec") === key;
            var was = a.classList.contains("is-active");
            a.classList.toggle("is-active", active);
            if (active && !was && a.classList.contains("gl-v2-unit-tab") && a.scrollIntoView) {
                a.scrollIntoView({ block: "nearest", inline: "center" });
            }
        });
    }
    function setBadge(key, text) {
        $$('[data-nav-badge="' + key + '"]').forEach(function (b) { b.textContent = text; });
    }
    // Onder 768px verhuist de vaste balk naar position:fixed en scrolt alsnog de wrapper; valt die om
    // een of andere reden niet te scrollen, dan is de viewport de root (root: null).
    function scrollRoot() {
        if (!scroller) return null;
        var oy = window.getComputedStyle(scroller).overflowY;
        return (oy === "auto" || oy === "scroll") ? scroller : null;
    }
    function initSectionNav() {
        navItems.forEach(function (a) {
            a.addEventListener("click", function (e) {
                var targetId = a.getAttribute("data-target") || (a.getAttribute("href") || "").slice(1);
                var target = document.getElementById(targetId);
                if (!target) return;
                e.preventDefault();
                setActiveNav(a.getAttribute("data-sec"));
                target.scrollIntoView({ behavior: "smooth", block: "start" });
            });
        });
        var anchors = $$("[data-sec-anchor]");
        if (!("IntersectionObserver" in window) || !anchors.length) return;
        var visible = {};
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) { visible[en.target.getAttribute("data-sec-anchor")] = en.isIntersecting; });
            // De eerste (bovenste) zichtbare sectie in DOM-volgorde is de actieve.
            for (var i = 0; i < anchors.length; i++) {
                var key = anchors[i].getAttribute("data-sec-anchor");
                if (visible[key]) { setActiveNav(key); return; }
            }
        }, { root: scrollRoot(), rootMargin: "-10% 0px -60% 0px", threshold: 0 });
        anchors.forEach(function (a) { observer.observe(a); });
    }

    // ── Verplichte velden + statusregel in de actiebalk ────────────────────────────────────────
    var required = [
        { key: "name", input: function () { return $("#Unit_Name"); }, message: "Geef de eenheid een naam." },
        { key: "group", input: function () { return $('input[name="SelectedGroupType"]'); }, message: "Kies een type." },
        { key: "subtype", input: function () { return $('input[name="SelectedType"]'); }, message: "Kies een subtype." }
    ];
    function missingRequired() {
        return required.filter(function (r) { var i = r.input(); return !i || !String(i.value || "").trim() || i.value === "0"; });
    }
    function wrapperOf(r) { return $('[data-required-field="' + r.key + '"]'); }
    function clearRequiredError(r) {
        var w = wrapperOf(r);
        if (!w) return;
        var f = $(".gl-v2-field", w);
        if (f) f.classList.remove("is-error");
        var t = $(".gl-v2-select-trigger", w);
        if (t) t.classList.remove("is-error");
        $$("[data-js-error]", w).forEach(function (n) { n.remove(); });
    }
    function showRequiredError(r) {
        var w = wrapperOf(r);
        if (!w) return;
        clearRequiredError(r);
        var f = $(".gl-v2-field", w);
        if (f) f.classList.add("is-error");
        var t = $(".gl-v2-select-trigger", w);
        if (t) t.classList.add("is-error");
        var help = document.createElement("span");
        help.className = "gl-v2-field-help";
        help.setAttribute("data-js-error", "");
        help.textContent = r.message;
        if (f) f.appendChild(help);
    }
    function updateRequiredStatus(showErrors) {
        var status = document.getElementById("gl-v2-unit-required-status");
        var text = status ? $("[data-status-text]", status) : null;
        var missing = missingRequired();
        required.forEach(function (r) {
            if (missing.indexOf(r) === -1) clearRequiredError(r);
            else if (showErrors) showRequiredError(r);
        });
        if (!status || !text) return missing;
        var icon = $("i", status);
        if (missing.length === 0) {
            text.textContent = "Alle verplichte velden ingevuld";
            status.classList.remove("is-error");
            status.classList.add("is-ok");
            if (icon) icon.className = "ph ph-check-circle";
        } else {
            text.textContent = missing.length === 1 ? "1 verplicht veld" : missing.length + " verplichte velden";
            status.classList.toggle("is-error", !!showErrors);
            status.classList.remove("is-ok");
            if (icon) icon.className = "ph ph-warning-circle";
        }
        return missing;
    }

    // ── Type → subtype (afhankelijke keuzelijst) ───────────────────────────────────────────────
    function initTypeCascade() {
        var group = $('[data-role="group"] input[type="hidden"]');
        var subWrap = $('[data-role="subtype"]');
        if (!group || !subWrap || !window.GlV2Select) return;
        group.addEventListener("change", function () {
            var val = group.value;
            if (!val) {
                subWrap.setAttribute("data-placeholder", "Kies eerst een type");
                window.GlV2Select.setItems(subWrap, [], "");
                updateRequiredStatus(false);
                return;
            }
            fetch(config.subTypeUrl + "?id=" + encodeURIComponent(val))
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    var items = (data || []).map(function (d) { return { value: d.id != null ? d.id : d.ID, text: d.display != null ? d.display : d.Display }; });
                    subWrap.setAttribute("data-placeholder", "Kies een subtype");
                    window.GlV2Select.setItems(subWrap, items, "");
                    updateRequiredStatus(false);
                })
                .catch(function () { toast("Subtypes niet geladen", "Probeer opnieuw of herlaad de pagina.", "danger"); });
        });
    }

    // ── Live prijsberekening (16b §4) ───────────────────────────────────────────────────────────
    function optionBlocks() { return $$("[data-option]", document.getElementById("gl-v2-unit-options")); }
    function recalc() {
        var land = numberOf($("#Unit_LandValue"));
        var linkedEl = document.getElementById("gl-v2-unit-linked");
        var linkedTotal = linkedEl ? parseFloat(linkedEl.getAttribute("data-linked-total") || "0") || 0 : 0;
        var linkedRows = $$(".gl-v2-unit-linked-row");
        var linkedNames = linkedRows.map(function (r) { return r.getAttribute("data-linked-name"); }).filter(Boolean);

        var baseSum = 0;
        var bList = baseList();
        if (bList) $$("[data-cv-amount]", bList).forEach(function (inp) { baseSum += numberOf(inp); });
        var baseSub = baseBlock ? $("[data-base-subtotal]", baseBlock) : null;
        if (baseSub) baseSub.textContent = euro(baseSum, 2);

        var options = optionBlocks().map(function (block) {
            var sum = 0;
            $$("[data-cv-amount]", block).forEach(function (inp) { sum += numberOf(inp); });
            var sub = $("[data-option-subtotal]", block);
            if (sub) sub.textContent = euro(sum, 2);
            var nameInput = $("[data-option-name]", block);
            var rawName = nameInput ? nameInput.value.trim() : "";
            return {
                name: rawName || "Afwerking",
                isDefault: block.classList.contains("is-default"),
                bouw: sum,
                // Een net toegevoegd, nog leeg blok (geen naam, geen bedrag) hoeft nog geen prijsregel.
                blank: rawName === "" && sum === 0
            };
        });

        var rows = $("#gl-v2-unit-price [data-price-rows]");
        var unit = config.unitName || "Eenheid";
        var defaultOpt = options.filter(function (o) { return o.isDefault; })[0] || options[0];
        var html = "";
        function row(label, value, cls) {
            return '<div class="gl-v2-unit-price-row ' + (cls || "") + '"><span>' + label + '</span><span>' + value + "</span></div>";
        }
        function esc(s) { return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;"); }
        if (!defaultOpt) {
            // Geen afwerkingen: grond + alle constructieprijzen = verkoopprijs.
            html += row("Grond", euro(land, 2));
            html += row("Bouw", euro(baseSum, 2));
            html += row(esc(unit), euro(land + baseSum, 2), "is-strong");
            if (linkedTotal > 0) {
                html += row("+ " + esc(linkedNames.join(", ")), euro(linkedTotal, 2));
                html += row("Totaal met gekoppeld", euro(land + baseSum + linkedTotal, 2), "is-total");
            }
        } else {
            html += row("Grond", euro(land, 2));
            html += row("Bouw — " + esc(defaultOpt.name.toLowerCase()), euro(defaultOpt.bouw, 2));
            html += row(esc(unit) + ", " + esc(defaultOpt.name.toLowerCase()), euro(land + defaultOpt.bouw, 2), "is-strong");
            options.filter(function (o) { return o !== defaultOpt && !o.blank; }).forEach(function (o) {
                html += row(esc(unit) + ", " + esc(o.name.toLowerCase()), euro(land + o.bouw, 2), "is-strong");
            });
            if (linkedTotal > 0) {
                html += row("+ " + esc(linkedNames.join(", ")), euro(linkedTotal, 2));
                html += row("Totaal met gekoppeld", euro(land + defaultOpt.bouw + linkedTotal, 2), "is-total");
            }
        }
        if (rows) rows.innerHTML = html;

        setBadge("bedragen", euroCompact(land + (defaultOpt ? defaultOpt.bouw : baseSum)));
    }

    // ── Constructieprijzen zonder afwerking ↔ afwerkingen ──────────────────────────────────────
    // Zelfde regel als de publieke site (ServiceCore.Helpers.UnitPricing): zonder afwerkingen staan de
    // constructieprijzen los onder de grondwaarde (BaseConstructionValues[…]); zodra er een afwerking is,
    // horen ze bij die afwerking (FinishingOptions[…].ConstructionValues[…]) en is elke afwerking een
    // volledig alternatief. Rijen verhuizen tussen die twee door enkel hun name-attributen te herschrijven.
    var baseBlock = document.getElementById("gl-v2-unit-base");
    var optionsWrap = document.getElementById("gl-v2-unit-options-wrap");
    function baseList() { return baseBlock ? $("[data-base-list]", baseBlock) : null; }
    function syncMode() {
        var has = optionBlocks().length > 0;
        if (baseBlock) baseBlock.hidden = has;
        if (optionsWrap) optionsWrap.hidden = !has;
    }
    function rekeyRow(row, newPrefix) {
        $$("[name]", row).forEach(function (el) {
            el.name = el.name.replace(/^(?:BaseConstructionValues|FinishingOptions\[[^\]]+\]\.ConstructionValues)/, newPrefix);
        });
    }
    function moveRows(fromList, toList, newPrefix) {
        if (!fromList || !toList) return;
        $$("[data-cv-row]", fromList).forEach(function (row) {
            rekeyRow(row, newPrefix);
            toList.appendChild(row);
        });
    }

    // ── Afwerkingsblokken (16b §3) ─────────────────────────────────────────────────────────────
    function makeDefault(block) {
        optionBlocks().forEach(function (b) {
            var isThis = b === block;
            b.classList.toggle("is-default", isThis);
            var hidden = $("[data-option-default]", b);
            if (hidden) hidden.value = isThis ? "true" : "false";
        });
    }
    function addOption() {
        var key = uid();
        var block = cloneTemplate("gl-v2-unit-tpl-option", { "__OPT__": key, "__CV0__": uid() });
        if (!block) return;
        var container = document.getElementById("gl-v2-unit-options");
        var firstOption = optionBlocks().length === 0;
        if (firstOption) {
            // De constructieprijzen van de eenheid gaan mee in de eerste afwerking — niets gaat verloren.
            var bl = baseList();
            var moving = bl ? $$("[data-cv-row]", bl) : [];
            if (moving.length) $$("[data-cv-row]", $("[data-cv-list]", block)).forEach(function (r) { r.remove(); });
            container.appendChild(block);
            moveRows(bl, $("[data-cv-list]", block), "FinishingOptions[" + key + "].ConstructionValues");
        } else {
            container.appendChild(block);
        }
        initFields(block);
        if (firstOption) makeDefault(block);
        syncMode();
        var name = $("[data-option-name]", block);
        if (name) name.focus();
        recalc();
        markDirty();
    }
    function addCv(block) {
        var key = block.getAttribute("data-option-key");
        var row = cloneTemplate("gl-v2-unit-tpl-cv", { "__OPT__": key, "__CV__": uid() });
        if (!row) return;
        $("[data-cv-list]", block).appendChild(row);
        initFields(row);
        var amount = $("[data-cv-amount]", row);
        if (amount) amount.focus();
        markDirty();
    }
    function addBaseCv() {
        var bl = baseList();
        if (!bl) return;
        var row = cloneTemplate("gl-v2-unit-tpl-cv-base", { "__CV__": uid() });
        if (!row) return;
        bl.appendChild(row);
        initFields(row);
        var amount = $("[data-cv-amount]", row);
        if (amount) amount.focus();
        markDirty();
    }
    var pendingRemoveBlock = null;
    var removeOptionModalEl = document.getElementById("gl-v2-unit-remove-option-modal");
    function removeOption(block) {
        var wasDefault = block.classList.contains("is-default");
        if (optionBlocks().length === 1) {
            // Laatste afwerking weg: de eenheid heeft geen afwerkingen meer, dus haar constructieprijzen
            // gaan terug naar de basis (grond + alles) i.p.v. verloren te gaan.
            var bl = baseList();
            if (bl) {
                $$("[data-cv-row]", bl).forEach(function (r) { r.remove(); });
                moveRows($("[data-cv-list]", block), bl, "BaseConstructionValues");
                if (!$$("[data-cv-row]", bl).length) addBaseCv();
            }
        }
        block.remove();
        syncMode();
        var left = optionBlocks();
        if (wasDefault && left.length) makeDefault(left[0]);
        recalc();
        markDirty();
    }
    function blockHasContent(block) {
        return $$("[data-cv-amount]", block).some(function (i) { return numberOf(i) !== 0 || i.value.trim() !== ""; })
            || $$('input[name$=".Description"]', block).some(function (i) { return i.value.trim() !== ""; });
    }
    function initOptions() {
        var container = document.getElementById("gl-v2-unit-options");
        var addBtn = document.getElementById("gl-v2-unit-add-option");
        if (addBtn) addBtn.addEventListener("click", addOption);
        if (!container) return;
        container.addEventListener("click", function (e) {
            var block = e.target.closest("[data-option]");
            if (!block) return;
            if (e.target.closest("[data-make-default]")) { makeDefault(block); recalc(); markDirty(); return; }
            if (e.target.closest("[data-add-cv]")) { addCv(block); return; }
            var rmCv = e.target.closest("[data-remove-cv]");
            if (rmCv) { rmCv.closest("[data-cv-row]").remove(); recalc(); markDirty(); return; }
            if (e.target.closest("[data-remove-option]")) {
                if (optionBlocks().length > 1 && blockHasContent(block) && removeOptionModalEl && window.bootstrap) {
                    pendingRemoveBlock = block;
                    window.bootstrap.Modal.getOrCreateInstance(removeOptionModalEl).show();
                } else {
                    removeOption(block);
                }
            }
        });
        var bl = baseList();
        var addBase = document.getElementById("gl-v2-unit-add-base-cv");
        if (addBase) addBase.addEventListener("click", addBaseCv);
        if (bl) bl.addEventListener("click", function (e) {
            var rm = e.target.closest("[data-remove-cv]");
            if (!rm) return;
            rm.closest("[data-cv-row]").remove();
            recalc();
            markDirty();
        });
        var confirm = document.getElementById("gl-v2-unit-remove-option-confirm");
        if (confirm) confirm.addEventListener("click", function () {
            if (pendingRemoveBlock) removeOption(pendingRemoveBlock);
            pendingRemoveBlock = null;
            if (removeOptionModalEl && window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(removeOptionModalEl).hide();
        });
    }

    // ── Indeling (16b §6) ──────────────────────────────────────────────────────────────────────
    function recalcRooms() {
        var rows = $$("[data-room-row]");
        var count = 0, area = 0, filled = 0;
        rows.forEach(function (r) {
            var n = parseInt(($("[data-room-count]", r) || {}).value, 10) || 0;
            var s = numberOf($("[data-room-surface]", r));
            count += n;
            area += n * s;
            if (s > 0) filled++;
        });
        var total = document.getElementById("gl-v2-unit-rooms-total");
        if (total) total.textContent = rows.length
            ? count + (count === 1 ? " ruimte" : " ruimtes") + " · " + area.toLocaleString("nl-BE", { maximumFractionDigits: 2 }) + " m² ingevuld"
            : "";
        setBadge("indeling", rows.length ? String(count) : "");
    }
    function initRooms() {
        var container = document.getElementById("gl-v2-unit-room-rows");
        var addBtn = document.getElementById("gl-v2-unit-add-room");
        if (addBtn) addBtn.addEventListener("click", function () {
            var row = cloneTemplate("gl-v2-unit-tpl-room", { "__ROOM__": uid() });
            if (!row) return;
            container.appendChild(row);
            initFields(row);
            recalcRooms();
            markDirty();
        });
        if (container) container.addEventListener("click", function (e) {
            var rm = e.target.closest("[data-remove-room]");
            if (!rm) return;
            rm.closest("[data-room-row]").remove();
            recalcRooms();
            markDirty();
        });
    }

    // ── Documenten (16b §7) ────────────────────────────────────────────────────────────────────
    function isPdf(file) { return file && ((file.type && file.type === "application/pdf") || /\.pdf$/i.test(file.name)); }
    function updateDocBadge() {
        var n = 0;
        var plan = $("[data-plan-current]");
        if (plan && !plan.hidden) n++;
        n += $$("[data-exec-existing]").filter(function (r) { return !r.hidden; }).length;
        n += $$("[data-exec-new]").length;
        setBadge("docs", n ? String(n) : "");
        var empty = document.getElementById("gl-v2-unit-exec-empty");
        var execCount = $$("[data-exec-existing]").filter(function (r) { return !r.hidden; }).length + $$("[data-exec-new]").length;
        if (empty) empty.hidden = execCount > 0;
    }
    function wireDrop(dropEl, inputEl, onFiles, multiple) {
        if (!dropEl || !inputEl) return;
        dropEl.addEventListener("click", function () { inputEl.click(); });
        ["dragenter", "dragover"].forEach(function (ev) {
            dropEl.addEventListener(ev, function (e) { e.preventDefault(); dropEl.classList.add("is-dragover"); });
        });
        ["dragleave", "drop"].forEach(function (ev) {
            dropEl.addEventListener(ev, function (e) { e.preventDefault(); dropEl.classList.remove("is-dragover"); });
        });
        dropEl.addEventListener("drop", function (e) {
            var files = Array.prototype.slice.call((e.dataTransfer && e.dataTransfer.files) || []);
            if (!multiple) files = files.slice(0, 1);
            onFiles(files);
        });
    }
    function initPlan() {
        var wrap = document.getElementById("gl-v2-unit-plan");
        var input = document.getElementById("gl-v2-unit-plan-file");
        if (!wrap || !input) return;
        var current = $("[data-plan-current]", wrap);
        var drop = $("[data-plan-drop]", wrap);
        var removeFlag = document.getElementById("gl-v2-unit-removeplan");
        var hadPlan = wrap.getAttribute("data-has-plan") === "true";
        var originalName = $("[data-plan-name]", wrap) ? $("[data-plan-name]", wrap).textContent : "";
        var originalSub = $("[data-plan-sub]", wrap) ? $("[data-plan-sub]", wrap).textContent : "";

        function showChosen(file) {
            if (!isPdf(file)) { toast("Kies een pdf", "Het verkoopplan moet een pdf-bestand zijn.", "danger"); return; }
            var dt = new DataTransfer();
            dt.items.add(file);
            input.files = dt.files;
            $("[data-plan-name]", wrap).textContent = file.name;
            $("[data-plan-sub]", wrap).textContent = "nieuw — wordt bewaard bij opslaan";
            current.hidden = false;
            drop.hidden = true;
            if (removeFlag) removeFlag.value = "false";
            updateDocBadge();
            markDirty();
        }
        wireDrop(drop, input, function (files) { if (files[0]) showChosen(files[0]); }, false);
        input.addEventListener("change", function () { if (input.files && input.files[0]) showChosen(input.files[0]); });
        $("[data-plan-replace]", wrap).addEventListener("click", function () { input.click(); });
        $("[data-plan-remove]", wrap).addEventListener("click", function () {
            input.value = "";
            current.hidden = true;
            drop.hidden = false;
            if (removeFlag) removeFlag.value = hadPlan ? "true" : "false";
            $("[data-plan-name]", wrap).textContent = originalName;
            $("[data-plan-sub]", wrap).textContent = originalSub;
            updateDocBadge();
            markDirty();
        });
    }
    var execQueue = [];
    function rebuildExecFiles() {
        var input = document.getElementById("gl-v2-unit-exec-files");
        if (!input) return;
        var dt = new DataTransfer();
        execQueue.forEach(function (q) { dt.items.add(q.file); });
        input.files = dt.files;
    }
    function addExecFiles(files) {
        var list = document.getElementById("gl-v2-unit-exec-list");
        files.forEach(function (file) {
            if (!isPdf(file)) { toast("Kies een pdf", file.name + " is geen pdf-bestand.", "danger"); return; }
            var key = uid();
            execQueue.push({ key: key, file: file });
            var row = document.createElement("div");
            row.className = "gl-v2-unit-doc-row";
            row.setAttribute("data-exec-new", key);
            row.innerHTML = '<span class="gl-v2-unit-doc-type">PDF</span>' +
                '<div class="gl-v2-field gl-v2-unit-doc-name-input"><div class="gl-v2-field-box"><input type="text" name="executionPlanNames" class="gl-v2-field-input" autocomplete="off" placeholder="Naam van het plan" aria-label="Naam van het uitvoeringsplan"></div></div>' +
                '<button type="button" class="gl-v2-icon-btn is-warning" data-exec-remove-new="' + key + '" title="Verwijderen" aria-label="Verwijderen"><i class="ph ph-trash" aria-hidden="true"></i></button>';
            $("input", row).value = file.name.replace(/\.pdf$/i, "");
            list.appendChild(row);
        });
        rebuildExecFiles();
        updateDocBadge();
        markDirty();
    }
    function initExecPlans() {
        var list = document.getElementById("gl-v2-unit-exec-list");
        var drop = $("[data-exec-drop]");
        var input = document.getElementById("gl-v2-unit-exec-files");
        if (!list || !input) return;
        wireDrop(drop, input, addExecFiles, true);
        // De <input type=file multiple> bevat na elke keuze precies de gekozen bestanden; die gaan in de
        // wachtrij (die de echte inhoud van het formulierveld beheert via DataTransfer).
        input.addEventListener("change", function () {
            var chosen = Array.prototype.slice.call(input.files || []);
            var previous = execQueue.map(function (q) { return q.file; });
            // Alleen wat nieuw is t.o.v. de wachtrij toevoegen (rebuildExecFiles zet de wachtrij terug).
            var fresh = chosen.filter(function (f) { return previous.indexOf(f) === -1; });
            addExecFiles(fresh);
        });
        list.addEventListener("click", function (e) {
            var existing = e.target.closest("[data-exec-remove]");
            if (existing) {
                var id = existing.getAttribute("data-exec-remove");
                var del = $('[data-exec-delete="' + id + '"]');
                if (del) del.disabled = false;
                existing.closest("[data-exec-existing]").hidden = true;
                updateDocBadge();
                markDirty();
                return;
            }
            var fresh = e.target.closest("[data-exec-remove-new]");
            if (fresh) {
                var key = fresh.getAttribute("data-exec-remove-new");
                execQueue = execQueue.filter(function (q) { return q.key !== key; });
                fresh.closest("[data-exec-new]").remove();
                rebuildExecFiles();
                updateDocBadge();
                markDirty();
            }
        });
    }

    // ── Koppelen/ontkoppelen (16b §5, dialoog uit 16c) ─────────────────────────────────────────
    function initLinking() {
        if (config.isNew) return;
        var detachForm = document.getElementById("gl-v2-unit-detach-form");
        document.addEventListener("click", function (e) {
            var detach = e.target.closest("[data-detach]");
            if (!detach || !detachForm) return;
            e.preventDefault();
            if (isDirty()) { toast("Eerst opslaan", "Sla je wijzigingen eerst op — ontkoppelen laadt de pagina opnieuw.", "info"); return; }
            detachForm.querySelector("[name='unitId']").value = detach.getAttribute("data-detach");
            detachForm.submit();
        });

        var modalEl = document.getElementById("gl-v2-unit-attach-modal");
        var modal = modalEl && window.bootstrap ? new window.bootstrap.Modal(modalEl, { backdrop: "static", keyboard: false }) : null;
        var openBtn = document.getElementById("gl-v2-unit-attach-open");
        if (!openBtn || !modal) return;
        openBtn.addEventListener("click", function () {
            if (isDirty()) { toast("Eerst opslaan", "Sla je wijzigingen eerst op — koppelen laadt de pagina opnieuw.", "info"); return; }
            var container = document.getElementById("gl-v2-unit-attach-container");
            container.innerHTML = '<div class="modal-body"><div class="gl-v2-du-picker-empty">Laden…</div></div>';
            modal.show();
            fetch(config.attachModalUrl + "?unitid=" + encodeURIComponent(config.unitId) + "&returnUrl=" + encodeURIComponent(config.attachReturnUrl || ""))
                .then(function (r) { return r.text(); })
                .then(function (html) { container.innerHTML = html; initAttachForm(container); })
                .catch(function () {
                    container.innerHTML = '<div class="modal-body"><div class="gl-v2-du-picker-empty">De koppelgegevens konden niet geladen worden.</div></div>';
                });
        });
    }
    // Zelfde logica als gl-v2-projecten-detailunits.js (page-local kopie — dezelfde conventie als de
    // andere per-pagina-recepten): de keuze vult grond-/bouwwaarde en de nieuwe prijs van het lot.
    function initAttachForm(container) {
        var f = $("form[data-attach-form]", container);
        if (!f) return;
        var unitInput = $("[data-attach-unit-id]", f);
        var landInput = $("[data-attach-land-value]", f);
        var buildValue = $("[data-attach-build-value]", f);
        var resultNew = $("[data-attach-result-new]", f);
        var resultOld = $("[data-attach-result-old]", f);
        var submitBtn = $("[data-attach-submit]", f);
        var lotPrice = parseFloat(f.getAttribute("data-lot-price") || "0") || 0;
        function select(option) {
            $$(".gl-v2-du-picker-option", f).forEach(function (o) { o.classList.remove("is-selected"); });
            option.classList.add("is-selected");
            var price = parseFloat(option.getAttribute("data-price") || "0") || 0;
            if (unitInput) unitInput.value = option.getAttribute("data-unit-id");
            if (landInput) landInput.value = option.getAttribute("data-land-value") || "0,00";
            if (buildValue) buildValue.textContent = euro(parseFloat(option.getAttribute("data-build-value") || "0") || 0, 0);
            if (resultOld) resultOld.hidden = false;
            if (resultNew) resultNew.textContent = euro(lotPrice + price, 0);
            if (submitBtn) submitBtn.disabled = false;
        }
        $$(".gl-v2-du-picker-option:not(.is-disabled)", f).forEach(function (o) { o.addEventListener("click", function () { select(o); }); });
        var only = $$(".gl-v2-du-picker-option:not(.is-disabled)", f);
        if (only.length === 1) select(only[0]);
    }

    // ── Kopiëren van een bestaande eenheid (16c) ───────────────────────────────────────────────
    function initCopy() {
        var hidden = $('input[name="CopyFromUnitId"]');
        if (!hidden || !config.addUnitUrl) return;
        hidden.addEventListener("change", function () {
            var returnUrl = ($('input[name="ReturnUrl"]') || {}).value || "";
            var url = config.addUnitUrl + (hidden.value ? "&copyFrom=" + encodeURIComponent(hidden.value) : "")
                + (returnUrl ? "&returnUrl=" + encodeURIComponent(returnUrl) : "");
            navigateGuarded(url);
        });
    }

    // ── Aandeel in de basisakte: live "nog X te verdelen" ──────────────────────────────────────
    function initShare() {
        var input = $("#Unit_Landshare");
        var wrap = document.getElementById("gl-v2-unit-share");
        if (!input || !wrap || !config.landShareTotal) return;
        var help = $(".gl-v2-field-help", wrap);
        if (!help) return;
        function update() {
            var mine = parseFloat(String(input.value).replace(",", ".")) || 0;
            var left = config.landShareTotal - (config.landShareOthers || 0) - mine;
            help.textContent = left >= 0
                ? "nog " + Math.round(left).toLocaleString("nl-BE") + " te verdelen"
                : "meer dan het totaal, " + Math.round(-left).toLocaleString("nl-BE") + " te veel";
            help.style.color = left < 0 ? "#8A3B2A" : "";
        }
        input.addEventListener("input", update);
    }

    // ── Bewaren ────────────────────────────────────────────────────────────────────────────────
    function initSubmit() {
        // Enter in een tekstveld mag het formulier niet bewaren (zou de éérste submitknop — "Opslaan en
        // naar …" — kiezen); bewaren gebeurt enkel via de knoppen onderaan.
        form.addEventListener("keydown", function (e) {
            if (e.key === "Enter" && e.target.tagName === "INPUT" && e.target.type !== "submit" && e.target.type !== "button") e.preventDefault();
        });

        $$("[data-save-mode]", form).forEach(function (btn) {
            // Validatie op de klik, vóór het submit-event: AutoNumeric's unformatOnSubmit draait op het
            // submit-event en zou na een geblokkeerde submit de velden ongeformatteerd achterlaten.
            btn.addEventListener("click", function (e) {
                var missing = updateRequiredStatus(true);
                if (missing.length) {
                    e.preventDefault();
                    var first = wrapperOf(missing[0]);
                    if (first && first.scrollIntoView) first.scrollIntoView({ behavior: "smooth", block: "center" });
                    return;
                }
                // Onaangeraakte lege ruimte-rijen (geen type/opmerking/oppervlakte) vallen weg.
                $$("[data-room-row]").forEach(function (r) {
                    var type = $('input[name$=".Type"]', r);
                    var remark = $('input[name$=".Remark"]', r);
                    var surf = $("[data-room-surface]", r);
                    if (type && !type.value && remark && !remark.value.trim() && surf && !surf.value.trim()) r.remove();
                });
                // RoomBO.Number/Surface zijn niet-nullable: leeg → 0.
                $$("[data-zero-if-empty]").forEach(function (inp) {
                    if (String(inp.value).trim() !== "") return;
                    if (window.AutoNumeric && window.AutoNumeric.getAutoNumericElement(inp)) window.AutoNumeric.getAutoNumericElement(inp).set(0);
                    else inp.value = "0";
                });
                var mode = document.getElementById("gl-v2-unit-savemode");
                if (mode) mode.value = btn.getAttribute("data-save-mode");
            });
        });
        form.addEventListener("submit", function () {
            $$("[data-save-mode]", form).forEach(function (b) { b.classList.add("is-loading"); b.setAttribute("aria-disabled", "true"); });
        });
    }

    // ── Opstart ────────────────────────────────────────────────────────────────────────────────
    initFields(document);
    initSectionNav();
    initTypeCascade();
    initOptions();
    initRooms();
    initPlan();
    initExecPlans();
    initLinking();
    initCopy();
    initShare();
    initSubmit();

    // Sectielijst-badge "Type & koppeling" = aantal gekoppelde eenheden.
    (function () {
        var n = $$(".gl-v2-unit-linked-row").length;
        if (n) setBadge("type", String(n));
    })();

    recalc();
    recalcRooms();
    updateDocBadge();
    updateRequiredStatus(false);

    // Live herrekenen: elke bedragwijziging (AutoNumeric meldt via een eigen event, de rest via
    // input/change) en elke naamwijziging van een afwerking.
    document.addEventListener("autoNumeric:rawValueModified", function () { recalc(); recalcRooms(); });
    form.addEventListener("input", function (e) {
        if (e.target.matches("[data-cv-amount], [data-option-name], #Unit_LandValue")) recalc();
        if (e.target.matches("[data-room-count], [data-room-surface]")) recalcRooms();
        if (e.target.matches("#Unit_Name")) { updateRequiredStatus(false); }
    });
    form.addEventListener("change", function (e) {
        recalc();
        recalcRooms();
        if (e.target.matches('input[type="hidden"]')) updateRequiredStatus(false);
    });

    // Dirty-tracking als LAATSTE (zelfde reden als gl-v2-projecten-editcontract.js: de setItems()/init-
    // aanroepen hierboven mogen zelf niet als een echte wijziging tellen).
    form.addEventListener("input", markDirty);
    form.addEventListener("change", markDirty);
    initDiscardChanges();
})();
