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
            restore: function (id) {
                var opt = optionFor(id);
                if (opt) { opt.hidden = false; opt.classList.remove("is-added"); }
            }
        };
    })();

    $(function () {
        if ($("#ddlCompany").length) {
            $("#ddlCompany").select2({
                theme: "bootstrap", minimumInputLength: 3, width: "100%",
                placeholder: "Typ om een leverancier te zoeken...", allowClear: true, language: "nl",
                ajax: {
                    url: config.getCompanysUrl, type: "POST", dataType: "json", delay: 250,
                    data: function (params) { return { term: params.term, activeOnly: true }; },
                    processResults: function (data) { return { results: $.map(data, function (item) { return { id: item.id, text: item.text }; }) }; },
                    cache: true
                }
            });
            $("#ddlCompany").on("select2:select", function (e) {
                var selected = e.params && e.params.data;
                if (!selected || !selected.id) return;
                $("#txtCompanyID").val(selected.id);
                LoadSiteManagers(selected.id, "#ddlSiteManager");
            });
            $("#ddlCompany").on("select2:clear", function () {
                $("#txtCompanyID").val("0");
                $("#ddlSiteManager").empty().append(new Option("Geen", "", false, false));
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
        return false;
    });

    $(document).on("click", "button.deleterow", function () {
        $(this).closest("div").parent("div").parent("div").remove();
        var activiteitId = $(this).data("id");
        if (lotPicker) lotPicker.restore(String(activiteitId));
        return false;
    });

    $(document).ready(function () {
        initCurrencyMasks();
        LoadSiteManagers($("#txtCompanyID").val(), "#ddlSiteManager");
    });
})();
