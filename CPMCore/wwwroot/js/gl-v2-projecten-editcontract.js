// gl-v2 layout-pilot — Projecten/EditContractV2.cshtml. Eigen pagina-JS (zelfde "eigen <pagina>.js"-
// conventie als gl-v2-leveranciers-form.js/gl-v2-klanten-form.js) — voorheen helemaal inline in de
// view, verhuisd hierheen toen "Lot toevoegen" van select2 naar het gedeelde gl-v2-multiselect-
// component ging (data-gl-v2-multiselect, design-handoff optie 4h — dezelfde "volledig juist"
// component als Leveranciers/EditV2's Activiteiten-veld, zie DESIGN.md). window.glV2EditContractConfig
// (in de view) draagt de endpoint-urls — Razor kan niet in een los .js-bestand.
(function () {
    "use strict";

    var config = window.glV2EditContractConfig || {};
    var backdrop = null;

    function initCurrencyMasks(target) {
        if (target) { CurrencyMask.init(target); } else { CurrencyMask.init(".Currencymask"); }
    }
    // _ActivityRowV2's eigen AJAX-succes hieronder roept dit ook aan voor de net toegevoegde rij —
    // was al globaal (window-scope) toen dit nog inline stond, ongewijzigd gedrag.
    window.initCurrencyMasks = initCurrencyMasks;

    // ── Generieke paneel-infra voor .gl-v2-select (open/sluiten/positioneren) — zelfde recept als
    //    gl-v2-leveranciers-form.js/gl-v2-klanten-form.js, hier page-local voor het ene "Lot
    //    toevoegen"-veld (deze pagina heeft er maar één, dus geen generieke multi-instance-registry
    //    nodig zoals die andere twee bestanden wel hebben). ─────────────────────────────────────────
    function getBackdrop() {
        if (backdrop) return backdrop;
        backdrop = document.createElement("div");
        backdrop.className = "gl-v2-select-backdrop";
        document.body.appendChild(backdrop);
        backdrop.addEventListener("click", closeAllPanels);
        return backdrop;
    }
    function closeAllPanels() {
        document.querySelectorAll(".gl-v2-select-panel.is-open").forEach(function (panel) { panel.classList.remove("is-open"); });
        document.querySelectorAll(".gl-v2-select-trigger.is-open").forEach(function (trigger) { trigger.classList.remove("is-open"); });
        if (backdrop) backdrop.classList.remove("is-open");
    }
    function positionPanel(trigger, panel) {
        if (window.innerWidth < 768) { panel.style.top = ""; panel.style.left = ""; panel.style.width = ""; return; }
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
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") closeAllPanels(); });
    document.addEventListener("click", function (e) {
        if (e.target.closest(".gl-v2-select, .gl-v2-select-panel")) return;
        closeAllPanels();
    });
    window.addEventListener("resize", closeAllPanels);

    // ── "Lot toevoegen" — gl-v2-multiselect als klaarzetvak, niet form-bound. Kiezen vult chips;
    //    "Toevoegen" stuurt élk gekozen lot meteen via AJAX naar AddSelectedActivities (zelfde flow
    //    als voorheen met select2('data')) en leegt zichzelf. Al-op-het-contract-staande loten
    //    renderen server-side mee maar `hidden`, zodat button.deleterow ze via .restore() terug kan
    //    tonen i.p.v. een DOM-node te moeten aanmaken. ───────────────────────────────────────────────
    var lotPicker = (function () {
        var root = document.getElementById("gl-v2-ec-lot-multiselect");
        if (!root) return null;
        var trigger = root.querySelector(".gl-v2-select-trigger");
        var panel = root.querySelector(".gl-v2-select-panel");
        var chipsHost = root.querySelector('[data-role="chips"]');
        var filterInput = root.querySelector('[data-role="filter"]');
        var hiddenHost = root.querySelector('[data-role="hidden-inputs"]');
        if (!trigger || !panel || !chipsHost || !hiddenHost) return null;

        function selectedIds() {
            return Array.prototype.map.call(hiddenHost.querySelectorAll('[data-role="hidden-input"]'), function (el) { return el.value; });
        }
        function isSelected(id) { return selectedIds().indexOf(id) !== -1; }
        function optionFor(id) { return panel.querySelector('.gl-v2-select-option[data-id="' + id + '"]'); }

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
                remove.addEventListener("click", function (e) { e.stopPropagation(); toggle(id); });
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
                input.value = id;
                input.setAttribute("data-role", "hidden-input");
                hiddenHost.appendChild(input);
            }
            renderChips();
        }

        panel.addEventListener("click", function (e) {
            var opt = e.target.closest(".gl-v2-select-option.is-multi");
            if (!opt || opt.hidden) return;
            toggle(opt.getAttribute("data-id"));
        });
        trigger.addEventListener("click", function (e) {
            if (e.target.closest(".gl-v2-select-chip")) return;
            if (panel.classList.contains("is-open")) { closeAllPanels(); }
            else { openPanel(trigger, panel); if (filterInput) filterInput.focus(); }
        });
        if (filterInput) {
            filterInput.addEventListener("focus", function () { if (!panel.classList.contains("is-open")) openPanel(trigger, panel); });
            filterInput.addEventListener("input", function () {
                if (!panel.classList.contains("is-open")) openPanel(trigger, panel);
                var term = filterInput.value.trim().toLowerCase();
                panel.querySelectorAll(".gl-v2-select-option.is-multi").forEach(function (opt) {
                    if (opt.classList.contains("is-added")) return;
                    var text = (opt.getAttribute("data-text") || "").toLowerCase();
                    opt.hidden = term.length > 0 && text.indexOf(term) === -1;
                });
            });
            filterInput.addEventListener("click", function (e) { e.stopPropagation(); });
        }

        renderChips();

        return {
            selected: function () {
                return selectedIds().map(function (id) {
                    var opt = optionFor(id);
                    return { id: id, text: opt ? opt.getAttribute("data-text") : "" };
                });
            },
            markAdded: function (id) {
                hiddenHost.querySelectorAll('[data-role="hidden-input"][value="' + id + '"]').forEach(function (el) { el.remove(); });
                var opt = optionFor(id);
                if (opt) { opt.hidden = true; opt.classList.add("is-added"); }
                renderChips();
                if (filterInput) filterInput.value = "";
            },
            // Contract toevoegen: de loten van de gekozen leverancier. items = [{id, text}], addedIds = al op het contract.
            setOptions: function (items, addedIds) {
                hiddenHost.innerHTML = "";
                var host = panel.querySelector('[data-role="options"]');
                host.innerHTML = "";
                items.forEach(function (item) {
                    var isAdded = addedIds.indexOf(String(item.id)) !== -1;
                    var btn = document.createElement("button");
                    btn.type = "button";
                    btn.className = "gl-v2-select-option is-multi" + (isAdded ? " is-added" : "");
                    btn.setAttribute("data-id", item.id);
                    btn.setAttribute("data-text", item.text);
                    btn.hidden = isAdded;
                    btn.innerHTML = '<span class="gl-v2-select-option-check"><i class="ph ph-check" aria-hidden="true"></i></span><span></span>';
                    btn.lastChild.textContent = item.text;
                    host.appendChild(btn);
                });
                renderChips();
            },
            setEnabled: function (enabled) {
                if (filterInput) {
                    filterInput.disabled = !enabled;
                    filterInput.placeholder = enabled ? "Zoek een lot…" : "Kies eerst een leverancier";
                }
                var addBtn = document.getElementById("btnAddActivities");
                if (addBtn) addBtn.disabled = !enabled;
            },
            restore: function (id) {
                var opt = optionFor(id);
                if (opt) { opt.hidden = false; opt.classList.remove("is-added"); }
            }
        };
    })();

    $(function () {
        // Leverancier: de keuzelijst met zoekveld (punt 34, gl-v2-combo.js). Het verborgen #txtCompanyID wordt door
        // het component zelf bijgehouden; hier enkel wat een andere leverancier met de pagina doet.
        var companyCombo = document.querySelector("#gl-v2-ec-company-field .gl-v2-combo, .gl-v2-combo:has(#txtCompanyID)");
        if (companyCombo) {
            companyCombo.addEventListener("gl-v2:combo-select", function (e) {
                var d = e.detail || {};
                if (d.id && d.id !== "0") {
                    LoadSiteManagers(d.id, "#ddlSiteManager");
                    if (config.isAdd) loadCompanyLots(d.id, d.text);
                } else {
                    $("#ddlSiteManager").empty().append(new Option("Geen", "", false, false));
                    if (config.isAdd) clearLots();
                }
                markDirty();
            });
        }

        $("#Contract_GuaranteeType").on("change", function () {
            var val = $(this).val();
            if (val == 2) { $("#Contract_GuaranteePercentage").removeAttr("disabled"); } else { $("#Contract_GuaranteePercentage").attr("disabled", "disabled"); }
            var isBank = val == 3;
            $("#guaranteeDocRow").toggle(isBank);
            var hasDoc = $("#guaranteeDocRow").find('a[href*="GuaranteeDoc"]').length > 0;
            $("#guaranteeDocWarningRow").toggle(isBank && !hasDoc);
        }).trigger("change");

        $("#chkCashDiscount").on("change", function () {
            var isChecked = $(this).prop("checked");
            $("#Contract_CashDiscountPercentage, #Contract_CashDiscountPaymentTerm").prop("disabled", !isChecked);
        });
        $("#chkCashDiscount").trigger("change");
    });

    // ── Contract toevoegen: loten volgen de leverancier. Bij een andere leverancier vervallen de al
    //    toegevoegde loten (ze horen bij de vorige leverancier) — na bevestiging als er al iets staat. ──
    var lastCompanyId = null;
    var lastCompanyText = "";
    function addedLotIds() {
        return $("#ActivityRows button.deleterow").map(function () { return String($(this).data("id")); }).get();
    }
    function clearLots() {
        $("#ActivityRows").empty();
        if (lotPicker) { lotPicker.setOptions([], []); lotPicker.setEnabled(false); }
        lastCompanyId = null;
        lastCompanyText = "";
    }
    function loadCompanyLots(companyId, companyText) {
        if (!lotPicker) return;
        if (lastCompanyId && String(lastCompanyId) !== String(companyId) && $("#ActivityRows").children().length &&
            !window.confirm("De loten die je al toevoegde horen bij de vorige leverancier en worden verwijderd. Doorgaan?")) {
            var combo = document.querySelector(".gl-v2-combo:has(#txtCompanyID)");
            if (combo && window.GlV2Combo) window.GlV2Combo.setValue(combo, String(lastCompanyId), lastCompanyText);
            return;
        }
        if (lastCompanyId && String(lastCompanyId) !== String(companyId)) $("#ActivityRows").empty();
        lastCompanyId = companyId;
        lastCompanyText = companyText || "";
        $.ajax({
            type: "POST", url: config.getCompanyActivitiesUrl, data: { companyid: companyId }, dataType: "json",
            success: function (data) {
                lotPicker.setOptions((data || []).map(function (i) { return { id: i.id, text: i.text }; }), addedLotIds());
                lotPicker.setEnabled(true);
            },
            error: function (xhr, status, error) { console.error("Fout bij ophalen activiteiten:", error); }
        });
    }

    function LoadSiteManagers(companyId, selectSelector) {
        $.ajax({
            type: "POST", url: config.getCompanyContactsUrl, data: { companyid: companyId }, dataType: "json",
            success: function (data) {
                var $select = $(selectSelector);
                var selectedValue = $select.val();
                $select.empty();
                $select.append(new Option("Geen", "", false, false));
                $.each(data, function (i, item) { $select.append(new Option(item.text, item.id, false, false)); });
                if (selectedValue) { $select.val(selectedValue); }
            },
            error: function (xhr, status, error) { console.error("Fout bij ophalen contactpersonen:", error); }
        });
    }
    // _SiteManagerNewModalV2's succesvolle aanmaak-flow roept 'm ook aan — was al globaal toen dit
    // nog inline stond, ongewijzigd contract.
    window.LoadSiteManagers = LoadSiteManagers;

    $("#btnAddActivities").click(function () {
        if (!lotPicker) return false;
        var items = lotPicker.selected();
        if (!items.length) return false;
        items.forEach(function (item) {
            $.ajax({
                url: config.addSelectedActivitiesUrl,
                data: { ActivityId: item.id, ActivityName: item.text },
                cache: false, traditional: true, type: "POST",
                success: function (result) {
                    var $row = $(result);
                    $("#ActivityRows").append($row);
                    initCurrencyMasks($row.find(".Currencymask"));
                }
            });
            lotPicker.markAdded(item.id);
        });
        markDirty();
        return false;
    });

    $(document).on("click", "button.deleterow", function () {
        $(this).closest("div").parent("div").parent("div").remove();
        var activiteitId = $(this).data("id");
        if (lotPicker) lotPicker.restore(String(activiteitId));
        markDirty();
        return false;
    });

    // ── Niet-opgeslagen wijzigingen — zelfde recept als gl-v2-klanten-editproject.js
    //    (markDirty/initDirtyBadge/isFormDirty/initDiscardChangesModal, woordelijk overgenomen):
    //    élke input/change op #gl-v2-ec-form toont de "NIET-OPGESLAGEN"-badge (al aanwezig in de
    //    markup, tot nu toe nooit gevuld) en Annuleren/de topbar-terugknop onderscheppen dan naar
    //    een bevestigingsmodal i.p.v. gewoon weg te navigeren. initDirtyBadge() wordt bewust pas in
    //    het LAATSTE $(document).ready-blok hieronder aangeroepen (registratievolgorde bepaalt
    //    jQuery se eigen ready-volgorde) — de .trigger("change")-aanroepen hierboven (Waarborg-type/
    //    Korting-contant, puur om de UI bij het laden te synchroniseren) zouden anders zelf al als
    //    een "echte" wijziging meetellen en de badge meteen bij het openen tonen. ─────────────────
    function markDirty() {
        var badge = document.getElementById("gl-v2-ec-dirty-badge");
        if (badge) badge.hidden = false;
    }
    // _SiteManagerNewModalV2's eigen script draait als een aparte IIFE (eigen scope) en wijzigt
    // #ddlSiteManager programmatisch (.val(data.id), geen echte change-event) na een geslaagde
    // aanmaak — dat moet ook als een niet-opgeslagen wijziging tellen, vandaar global.
    window.glV2EditContractMarkDirty = markDirty;
    function isFormDirty() {
        var badge = document.getElementById("gl-v2-ec-dirty-badge");
        return !!badge && !badge.hidden;
    }
    function initDirtyBadge() {
        var form = document.getElementById("gl-v2-ec-form");
        if (!form) return;
        form.addEventListener("input", markDirty);
        form.addEventListener("change", markDirty);
    }
    function initDiscardChangesModal() {
        var modalEl = document.getElementById("gl-v2-ec-discard-changes-modal");
        var confirmLink = document.getElementById("gl-v2-ec-discard-changes-confirm");
        var triggers = [
            document.getElementById("gl-v2-ec-cancel-link"),
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

    // Foutoverzicht (DESIGN.md punt 24) voor toevoegen én bewerken: verplichte velden via data-gl-v2-required
    // (leverancier bij toevoegen, betaaltermijn …), loten als eigen validator.
    function initErrorSummary() {
        var form = document.getElementById("gl-v2-ec-form");
        if (!form || !window.GlV2ErrorSummary) return;
        window.GlV2ErrorSummary.init({
            form: form,
            container: document.getElementById("gl-v2-error-summary"),
            validators: [function (errors) {
                if ($("#ActivityRows").children().length === 0) {
                    errors.push({
                        message: "Voeg minstens één lot toe.",
                        field: "Loten",
                        location: "Loten & bijbestellingen",
                        tab: null,
                        target: document.getElementById("gl-v2-ec-lot-multiselect")
                    });
                }
            }]
        });
    }

    $(document).ready(function () {
        initCurrencyMasks();
        LoadSiteManagers($("#txtCompanyID").val(), "#ddlSiteManager");
        initDirtyBadge();
        initDiscardChangesModal();
        initErrorSummary();
    });
})();
