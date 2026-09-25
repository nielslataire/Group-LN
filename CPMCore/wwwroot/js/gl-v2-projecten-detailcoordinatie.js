// gl-v2 layout-pilot — Projecten/DetailCoordinatieV2 (design-handoff punt 18, opties 18a en 18b).
//
// Wat hier zit, in volgorde van de pagina:
//   1. Tabs (Overzicht/Schijven/Regie/Facturen) — enkel bij schijven + regie; tonen/verbergen kaarten.
//   2. Selectie (18a §5/§6): schijven aanvinken + "Factureren t/m" bij de regie → één donkere selectiebalk die
//      beide optelt en er één factuur van maakt (POST MakeCoordInvoice via een formulier, geen fetch: de
//      controller redirect terug en toont zelf de melding).
//   3. Regie invoeren (18a §4): één rij, verplaatsing als schakelaar + potlood, tarief eronder. Na het opslaan
//      herlaadt de pagina (met de tab in de hash): KPI's, tab-tellers, "oudere prestaties" en totalen kloppen
//      dan vanzelf i.p.v. dat de JS ze allemaal apart moet bijhouden.
//   4. Modals: schijf toevoegen/bewerken, verwijderen, factuur koppelen, prestatie verwijderen.
//   5. Instellingen-zijpaneel (18b): contracttype, tarieven per medewerker (goud bij afwijking van het
//      standaardtarief), opslaan als gewoon formulier.
//
// Niet hier herbouwd: het ⋯-rijmenu (.js-gl-v2-menu-trigger), de keuzelijst (.gl-v2-select) en het datumveld —
// generieke shell-componenten (gl-v2-shell.js).
(function () {
    "use strict";

    var configEl = document.getElementById("gl-v2-co-config");
    if (!configEl) return;
    var cfg = JSON.parse(configEl.textContent);

    // ── Hulpfuncties ────────────────────────────────────────────────────────────────────────────────
    function $(sel, root) { return (root || document).querySelector(sel); }
    function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }
    function pad(n) { return n < 10 ? "0" + n : "" + n; }
    function isoDate(d) { return d.getFullYear() + "-" + pad(d.getMonth() + 1) + "-" + pad(d.getDate()); }
    function displayDate(iso) { var p = iso.split("-"); return p[2] + "/" + p[1] + "/" + p[0]; }
    function shortDate(iso) { var p = iso.split("-"); return p[2] + "/" + p[1]; }
    function fmtNum(v, dec) { return v.toLocaleString("nl-BE", { minimumFractionDigits: dec, maximumFractionDigits: dec }); }
    function fmtEur(v) { return "€ " + fmtNum(v, 2); }
    function num(v) { var n = parseFloat(String(v == null ? "" : v).replace(",", ".")); return isNaN(n) ? 0 : n; }
    function token() { var el = $('input[name="__RequestVerificationToken"]'); return el ? el.value : ""; }
    function closeMenus() { if (window.GlV2Menu && window.GlV2Menu.closeAll) window.GlV2Menu.closeAll(); }
    function toast(tone, title, body) {
        if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body });
    }
    function bsModal(id) {
        var el = document.getElementById(id);
        return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null;
    }

    // Een melding die vóór een herlaad gezet werd (prestatie toegevoegd/verwijderd).
    try {
        var flash = window.sessionStorage.getItem("glV2CoFlash");
        if (flash) {
            window.sessionStorage.removeItem("glV2CoFlash");
            var f = JSON.parse(flash);
            toast(f.tone || "success", f.title || "", f.body || "");
        }
    } catch (err) { /* sessionStorage kan geblokkeerd zijn — dan gewoon geen melding */ }

    function reloadWithFlash(flash, tab) {
        try { window.sessionStorage.setItem("glV2CoFlash", JSON.stringify(flash)); } catch (err) { /* zie boven */ }
        window.location.hash = tab ? "#" + tab : "";
        window.location.reload();
    }

    // ── 1. Tabs ─────────────────────────────────────────────────────────────────────────────────────
    var tabs = $$("[data-co-tab]");
    var panels = $$("[data-co-panel]");
    function showTab(name) {
        if (!tabs.length) return;
        var valid = tabs.some(function (t) { return t.getAttribute("data-co-tab") === name; });
        if (!valid) name = "overzicht";
        tabs.forEach(function (t) {
            var on = t.getAttribute("data-co-tab") === name;
            t.classList.toggle("is-active", on);
            t.setAttribute("aria-selected", on ? "true" : "false");
        });
        panels.forEach(function (p) {
            var key = p.getAttribute("data-co-panel");
            var show = name === "overzicht" ? key !== "facturen" : key === name;
            p.hidden = !show;
        });
    }
    tabs.forEach(function (t) {
        t.addEventListener("click", function () {
            var name = t.getAttribute("data-co-tab");
            showTab(name);
            if (window.history && window.history.replaceState) window.history.replaceState(null, "", name === "overzicht" ? window.location.pathname + window.location.search : "#" + name);
        });
    });
    showTab((window.location.hash || "").replace("#", ""));

    // ── 2. Selectie en selectiebalk ─────────────────────────────────────────────────────────────────
    var selbar = document.getElementById("gl-v2-co-selbar");
    var invoiceForm = document.getElementById("gl-v2-co-invoice-form");
    var regieIdsHost = document.getElementById("gl-v2-co-invoice-regie-ids");
    var selAll = document.getElementById("gl-v2-co-slices-all");
    var cutoff = ""; // yyyy-MM-dd of "" = geen periodefilter

    function sliceBoxes() { return $$(".js-co-slice"); }
    function selectedRegieRows() {
        if (!cutoff) return [];
        return $$('#gl-v2-co-regie-open [data-regie-row][data-invoiced="false"]').filter(function (tr) {
            var d = tr.getAttribute("data-date");
            return d && d <= cutoff;
        });
    }

    function updateSelection() {
        var boxes = sliceBoxes();
        var checked = boxes.filter(function (b) { return b.checked; });
        var slicePct = 0, sliceAmount = 0;
        checked.forEach(function (b) { slicePct += num(b.getAttribute("data-pct")); sliceAmount += num(b.getAttribute("data-amount")); });

        var regieRows = selectedRegieRows();
        var regieHours = 0, regieAmount = 0;
        regieRows.forEach(function (tr) { regieHours += num(tr.getAttribute("data-hours")); regieAmount += num(tr.getAttribute("data-amount")); });

        // Rijen markeren
        boxes.forEach(function (b) {
            var tr = b.closest("tr");
            if (tr) tr.classList.toggle("is-selected", b.checked);
        });
        var selectedIds = {};
        regieRows.forEach(function (tr) { selectedIds[tr.getAttribute("data-id")] = true; });
        $$('#gl-v2-co-regie-open [data-regie-row]').forEach(function (tr) {
            tr.classList.toggle("is-selected", !!selectedIds[tr.getAttribute("data-id")]);
        });

        // Alles-aanvinken
        if (selAll) {
            selAll.checked = boxes.length > 0 && checked.length === boxes.length;
            selAll.indeterminate = checked.length > 0 && checked.length < boxes.length;
        }

        // Verloopbalk (18a §3): het geselecteerde deel als eigen segment
        var seg = document.getElementById("gl-v2-co-seg");
        if (seg) {
            var barTotal = num(seg.getAttribute("data-bar-total")) || 100;
            var openPct = num(seg.getAttribute("data-open-pct"));
            var selFill = document.getElementById("gl-v2-co-seg-selected");
            var selLegend = document.getElementById("gl-v2-co-seg-selected-legend");
            var selPct = document.getElementById("gl-v2-co-seg-selected-pct");
            var openLbl = document.getElementById("gl-v2-co-seg-open-pct");
            if (selFill) selFill.style.width = (slicePct / barTotal * 100) + "%";
            if (selLegend) selLegend.hidden = checked.length === 0;
            if (selPct) selPct.textContent = fmtNum(slicePct, 2).replace(/,00$/, "") + " %";
            if (openLbl) openLbl.textContent = fmtNum(Math.max(openPct - slicePct, 0), 2).replace(/,00$/, "") + " %";
        }

        // Regie: notitie in de groepskop
        var note = document.getElementById("gl-v2-co-open-selected");
        if (note) {
            var openAll = $$('#gl-v2-co-regie-open [data-regie-row][data-invoiced="false"]').length;
            note.hidden = regieRows.length === 0;
            if (regieRows.length) {
                note.textContent = regieRows.length === openAll ? "alle " + openAll + " geselecteerd" : regieRows.length + " van " + openAll + " geselecteerd";
            }
        }

        // Selectiebalk
        if (!selbar) return;
        var any = checked.length > 0 || regieRows.length > 0;
        selbar.hidden = !any;
        var partSlices = $("[data-co-sel-slices]", selbar);
        var partRegie = $("[data-co-sel-regie]", selbar);
        var plus = $("[data-co-sel-plus]", selbar);
        if (partSlices) {
            partSlices.hidden = checked.length === 0;
            partSlices.innerHTML = "<b>" + checked.length + " " + (checked.length === 1 ? "schijf" : "schijven") + "</b> " + fmtEur(sliceAmount);
        }
        if (partRegie) {
            partRegie.hidden = regieRows.length === 0;
            partRegie.innerHTML = "<b>" + fmtNum(regieHours, 2) + " u</b> regie" + (cutoff ? " t/m " + shortDate(cutoff) : "") + " " + fmtEur(regieAmount);
        }
        if (plus) plus.hidden = !(checked.length > 0 && regieRows.length > 0);
        var total = $("[data-co-sel-total]", selbar);
        if (total) total.textContent = fmtEur(sliceAmount + regieAmount);

        // Verborgen regie-id's voor het formulier
        if (regieIdsHost) {
            regieIdsHost.innerHTML = "";
            regieRows.forEach(function (tr) {
                var inp = document.createElement("input");
                inp.type = "hidden"; inp.name = "regieUurIds"; inp.value = tr.getAttribute("data-id");
                regieIdsHost.appendChild(inp);
            });
        }
    }

    document.addEventListener("change", function (e) {
        if (e.target.classList && e.target.classList.contains("js-co-slice")) updateSelection();
    });
    if (selAll) {
        selAll.addEventListener("change", function () {
            sliceBoxes().forEach(function (b) { b.checked = selAll.checked; });
            updateSelection();
        });
    }

    // Factureren t/m (18a §5)
    var cutoffHidden = document.getElementById("regie-cutoff");
    var cutoffText = document.getElementById("regie-cutoff_text");
    var cutoffClear = document.getElementById("regie-cutoff-clear");
    var chips = $$("[data-co-cutoff]");

    function writeCutoffField(iso) {
        if (cutoffHidden) cutoffHidden.value = iso;
        if (cutoffText) cutoffText.value = iso ? displayDate(iso) : "";
    }
    function setCutoff(iso, fromChip) {
        cutoff = iso || "";
        chips.forEach(function (c) {
            var kind = c.getAttribute("data-co-cutoff");
            var want = kind === "today" ? isoDate(new Date()) : isoDate(new Date(new Date().getFullYear(), new Date().getMonth(), 0));
            c.classList.toggle("is-active", !!cutoff && fromChip === kind && want === cutoff);
        });
        if (fromChip) writeCutoffField(cutoff);
        if (cutoffClear) cutoffClear.hidden = !cutoff;
        updateSelection();
    }
    chips.forEach(function (c) {
        c.addEventListener("click", function () {
            var kind = c.getAttribute("data-co-cutoff");
            var now = new Date();
            var iso = kind === "today" ? isoDate(now) : isoDate(new Date(now.getFullYear(), now.getMonth(), 0));
            setCutoff(iso, kind);
        });
    });
    if (cutoffHidden) {
        var onCutoffField = function () { setCutoff(cutoffHidden.value || "", null); };
        cutoffHidden.addEventListener("change", onCutoffField);
        cutoffHidden.addEventListener("input", onCutoffField);
    }
    if (cutoffClear) cutoffClear.addEventListener("click", function () { writeCutoffField(""); setCutoff("", null); });

    var clearBtn = document.getElementById("gl-v2-co-sel-clear");
    if (clearBtn) {
        clearBtn.addEventListener("click", function () {
            sliceBoxes().forEach(function (b) { b.checked = false; });
            writeCutoffField("");
            setCutoff("", null);
        });
    }

    // Snelle actie in het ⋯-menu / de rij van een schijf: geen — de schijf aanvinken is één klik.

    if (invoiceForm) {
        invoiceForm.addEventListener("submit", function (e) {
            updateSelection();
            var hasSlices = sliceBoxes().some(function (b) { return b.checked; });
            var hasRegie = regieIdsHost && regieIdsHost.querySelector("input");
            if (!hasSlices && !hasRegie) { e.preventDefault(); return; }
            if (invoiceForm.getAttribute("data-submitting") === "true") { e.preventDefault(); return; }
            invoiceForm.setAttribute("data-submitting", "true");
            var go = document.getElementById("gl-v2-co-invoice-go");
            if (go) go.disabled = true;
            var m = bsModal("gl-v2-co-busy-modal");
            if (m) m.show();
        });
    }

    // Oudere prestaties / groepen in- en uitklappen
    var olderBtn = document.getElementById("gl-v2-co-older-btn");
    if (olderBtn) {
        olderBtn.addEventListener("click", function () {
            var open = olderBtn.getAttribute("aria-expanded") !== "true";
            $$('#gl-v2-co-regie-open [data-older="true"]').forEach(function (tr) { tr.hidden = !open; });
            olderBtn.setAttribute("aria-expanded", open ? "true" : "false");
            olderBtn.textContent = open ? olderBtn.getAttribute("data-less") : olderBtn.getAttribute("data-more");
        });
    }
    $$("[data-co-group-toggle]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var open = btn.getAttribute("aria-expanded") !== "true";
            btn.setAttribute("aria-expanded", open ? "true" : "false");
            var group = btn.closest("tbody");
            $$("[data-regie-row]", group).forEach(function (tr) { tr.hidden = !open; });
        });
    });

    // ── 3. Regie invoeren (18a §4) ──────────────────────────────────────────────────────────────────
    var workerHidden = document.getElementById("regie-worker");
    var dateHidden = document.getElementById("regie-date");
    var hoursInput = document.getElementById("regie-hours");
    var descInput = document.getElementById("regie-desc");
    var travelOn = document.getElementById("regie-travel-on");
    var travelLabel = document.getElementById("regie-travel-label");
    var travelEdit = document.getElementById("regie-travel-edit");
    var travelBox = document.getElementById("regie-travel-input-box");
    var travelKm = document.getElementById("regie-travel-km");
    var addBtn = document.getElementById("regie-add");
    var hint = document.getElementById("regie-hint");
    var travelCustom = false; // potlood actief: afwijkende km voor deze ene prestatie

    function currentWorker() { return workerHidden && workerHidden.value && cfg.rates[workerHidden.value] ? cfg.rates[workerHidden.value] : null; }
    function effectiveKm() {
        if (!travelOn || !travelOn.checked) return 0;
        return travelCustom ? num(travelKm ? travelKm.value : 0) : cfg.defaultKm;
    }

    function renderTravel() {
        if (!travelLabel) return;
        var checked = travelOn && travelOn.checked;
        if (travelEdit) travelEdit.disabled = !checked || !cfg.defaultKm;
        if (travelBox) travelBox.hidden = !(checked && travelCustom);
        if (!checked) {
            travelLabel.innerHTML = '<span class="gl-v2-co-muted">geen verplaatsing</span>';
        } else if (travelCustom) {
            travelLabel.innerHTML = '<span class="gl-v2-co-travel-tag is-custom">aangepast</span>';
        } else {
            travelLabel.innerHTML = "<b>" + fmtNum(cfg.defaultKm, 1).replace(/,0$/, "") + " km</b><span class=\"gl-v2-co-travel-tag\"> standaard</span>";
        }
    }

    function renderHint() {
        if (!hint) return;
        hint.classList.remove("is-error");
        var w = currentWorker();
        var parts = [];
        if (w) parts.push("tarief <b>" + escapeHtml(w.name.split(" ")[0]) + "</b> " + fmtEur(w.rate) + "/u");
        var km = effectiveKm();
        if (km > 0) {
            parts.push(travelCustom
                ? "afwijkende verplaatsing: " + fmtNum(km, 1).replace(/,0$/, "") + " km à " + fmtEur(cfg.kmAllowance) + "/km"
                : "verplaatsing staat standaard aan: 2 × " + fmtNum(cfg.defaultKm / 2, 2) + " km (bureel → werf) à " + fmtEur(cfg.kmAllowance) + "/km");
        } else if (cfg.defaultKm > 0) {
            parts.push("geen verplaatsing voor deze prestatie");
        }
        var hours = num(hoursInput ? hoursInput.value : 0);
        if (w && hours > 0) parts.push("deze prestatie: <b>" + fmtEur(hours * w.rate + km * cfg.kmAllowance) + "</b>");
        parts.push("potlood = afwijkende km enkel voor deze prestatie");
        parts.push("Enter voegt toe");
        hint.innerHTML = parts.join(" · ");
    }

    function escapeHtml(s) { return String(s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }

    if (travelOn) travelOn.addEventListener("change", function () { if (!travelOn.checked) travelCustom = false; renderTravel(); renderHint(); });
    if (travelEdit) {
        travelEdit.addEventListener("click", function () {
            travelCustom = !travelCustom;
            if (travelCustom && travelKm) { travelKm.value = fmtNum(cfg.defaultKm, 1).replace(/[^0-9,]/g, "").replace(",", "."); travelKm.focus(); travelKm.select(); }
            renderTravel(); renderHint();
        });
    }
    if (travelKm) travelKm.addEventListener("input", renderHint);
    if (workerHidden) workerHidden.addEventListener("change", renderHint);
    if (hoursInput) hoursInput.addEventListener("input", renderHint);
    if (hint) { renderTravel(); renderHint(); }

    function addRegie() {
        if (!addBtn || addBtn.disabled) return;
        var w = currentWorker();
        var hours = num(hoursInput ? hoursInput.value : 0);
        var date = dateHidden ? dateHidden.value : "";
        function fail(msg, focusEl) {
            if (hint) { hint.classList.add("is-error"); hint.textContent = msg; }
            if (focusEl) focusEl.focus();
        }
        if (!w) return fail("Kies eerst een medewerker.", null);
        if (!date) return fail("Kies een datum.", document.getElementById("regie-date_text"));
        if (hours <= 0) return fail("Geef het aantal uren in.", hoursInput);

        var km = effectiveKm();
        addBtn.disabled = true;
        fetch(cfg.addUrl, {
            method: "POST",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": token() },
            body: JSON.stringify({
                projectId: cfg.projectId,
                userId: workerHidden.value,
                date: date,
                hours: hours,
                travelKm: km > 0 ? km : null,
                description: descInput ? descInput.value.trim() : null
            })
        })
            .then(function (r) { return r.ok ? r.json() : Promise.reject(r); })
            .then(function () {
                reloadWithFlash({ tone: "success", title: "Prestatie toegevoegd", body: w.name + " · " + fmtNum(hours, 2) + " u op " + displayDate(date) + "." }, "regie");
            })
            .catch(function () {
                addBtn.disabled = false;
                toast("danger", "Opslaan mislukt", "De prestatie kon niet opgeslagen worden. Probeer opnieuw.");
            });
    }
    if (addBtn) addBtn.addEventListener("click", addRegie);
    var entry = document.getElementById("gl-v2-co-entry");
    if (entry) {
        entry.addEventListener("keydown", function (e) {
            if (e.key === "Enter" && e.target.tagName === "INPUT" && e.target.type !== "checkbox" && !e.target.closest(".gl-v2-datepicker")) {
                e.preventDefault();
                addRegie();
            }
        });
    }

    // Prestatie verwijderen
    var pendingRegieDelete = null;
    document.addEventListener("click", function (e) {
        var link = e.target.closest(".js-co-regie-delete");
        if (!link) return;
        e.preventDefault();
        closeMenus();
        pendingRegieDelete = link.getAttribute("data-id");
        var name = document.getElementById("gl-v2-co-regie-delete-name");
        if (name) name.textContent = link.getAttribute("data-label") || "";
        var m = bsModal("gl-v2-co-regie-delete-modal");
        if (m) m.show();
    });
    var regieDeleteConfirm = document.getElementById("gl-v2-co-regie-delete-confirm");
    if (regieDeleteConfirm) {
        regieDeleteConfirm.addEventListener("click", function () {
            if (!pendingRegieDelete) return;
            var id = pendingRegieDelete;
            regieDeleteConfirm.disabled = true;
            fetch(cfg.deleteUrl + "?id=" + encodeURIComponent(id), { method: "POST", headers: { "RequestVerificationToken": token() } })
                .then(function (r) {
                    if (r.ok) {
                        reloadWithFlash({ tone: "success", title: "Prestatie verwijderd", body: "" }, "regie");
                    } else {
                        regieDeleteConfirm.disabled = false;
                        var m = bsModal("gl-v2-co-regie-delete-modal");
                        if (m) m.hide();
                        toast("danger", "Niet verwijderd", r.status === 409 ? "Deze prestatie is al gefactureerd en kan niet verwijderd worden." : "Verwijderen mislukt. Probeer opnieuw.");
                    }
                })
                .catch(function () { regieDeleteConfirm.disabled = false; toast("danger", "Niet verwijderd", "Verwijderen mislukt. Probeer opnieuw."); });
        });
    }

    // ── 4. Schijven: modals ─────────────────────────────────────────────────────────────────────────
    var sliceModalEl = document.getElementById("gl-v2-co-slice-modal");
    var sliceId = document.getElementById("gl-v2-co-slice-id");
    var sliceDesc = document.getElementById("gl-v2-co-slice-desc");
    var slicePct = document.getElementById("gl-v2-co-slice-pct");
    var sliceAmount = document.getElementById("gl-v2-co-slice-amount");
    var sliceRoom = document.getElementById("gl-v2-co-slice-room");
    var sliceTitle = document.getElementById("gl-v2-co-slice-modal-title");

    function otherSlicesPct(excludeId) {
        var total = 0;
        $$("[data-slice-row]").forEach(function (tr) {
            if (excludeId && tr.getAttribute("data-slice-id") === String(excludeId)) return;
            total += num(tr.getAttribute("data-pct"));
        });
        return total;
    }
    function renderSliceHelp() {
        if (!sliceAmount || !slicePct) return;
        var price = num(sliceAmount.getAttribute("data-price"));
        var pct = num(slicePct.value);
        sliceAmount.textContent = fmtEur(price * pct / 100);
        if (sliceRoom) {
            var others = otherSlicesPct(num(sliceId ? sliceId.value : 0));
            var total = others + pct;
            sliceRoom.textContent = price <= 0
                ? "Stel het contractbedrag in (Instellingen) om hier een bedrag te zien."
                : "Samen met de andere schijven kom je op " + fmtNum(total, 2).replace(/,00$/, "") + " %" + (total > 100.005 ? " — dat is meer dan 100 %." : total < 99.995 ? " — nog " + fmtNum(100 - total, 2).replace(/,00$/, "") + " % te verdelen." : ".");
            sliceRoom.style.color = total > 100.005 ? "var(--gl-v2-danger)" : "";
        }
    }
    function openSliceModal(id, description, pct) {
        closeMenus();
        if (sliceId) sliceId.value = id || 0;
        if (sliceDesc) sliceDesc.value = description || "";
        if (slicePct) slicePct.value = pct || "";
        if (sliceTitle) sliceTitle.textContent = id ? "Schijf bewerken" : "Schijf toevoegen";
        // Bij een nieuwe schijf: stel het nog niet verdeelde percentage voor.
        if (!id && slicePct) {
            var left = 100 - otherSlicesPct(0);
            if (left > 0.004) slicePct.value = left.toFixed(2).replace(/\.?0+$/, "");
        }
        renderSliceHelp();
        var m = bsModal("gl-v2-co-slice-modal");
        if (m) m.show();
        if (sliceModalEl) sliceModalEl.addEventListener("shown.bs.modal", function once() { sliceModalEl.removeEventListener("shown.bs.modal", once); if (sliceDesc) sliceDesc.focus(); });
    }
    document.addEventListener("click", function (e) {
        var add = e.target.closest(".js-co-slice-add");
        if (add) { e.preventDefault(); openSliceModal(0, "", ""); return; }
        var edit = e.target.closest(".js-co-slice-edit");
        if (edit) { e.preventDefault(); openSliceModal(edit.getAttribute("data-slice-id"), edit.getAttribute("data-description"), edit.getAttribute("data-percentage")); return; }

        var del = e.target.closest(".js-co-slice-delete");
        if (del) {
            e.preventDefault();
            closeMenus();
            var idEl = document.getElementById("gl-v2-co-slice-delete-id");
            var nameEl = document.getElementById("gl-v2-co-slice-delete-name");
            if (idEl) idEl.value = del.getAttribute("data-slice-id");
            if (nameEl) nameEl.textContent = del.getAttribute("data-description") || "";
            var dm = bsModal("gl-v2-co-slice-delete-modal");
            if (dm) dm.show();
            return;
        }

        var link = e.target.closest(".js-co-slice-link");
        if (link) {
            e.preventDefault();
            closeMenus();
            var lid = document.getElementById("gl-v2-co-link-slice-id");
            var lfor = document.getElementById("gl-v2-co-link-for");
            if (lid) lid.value = link.getAttribute("data-slice-id");
            if (lfor) lfor.textContent = "Koppel een bestaande factuur aan “" + (link.getAttribute("data-description") || "") + "”.";
            var lm = bsModal("gl-v2-co-link-modal");
            if (lm) lm.show();
            return;
        }

        var unlink = e.target.closest(".js-co-slice-unlink");
        if (unlink) {
            e.preventDefault();
            closeMenus();
            var form = document.getElementById("gl-v2-co-unlink-form");
            if (!form) return;
            form.querySelector("[name='sliceId']").value = unlink.getAttribute("data-slice-id");
            form.submit();
        }
    });
    if (slicePct) slicePct.addEventListener("input", renderSliceHelp);

    var sliceForm = document.getElementById("gl-v2-co-slice-form");
    if (sliceForm) {
        sliceForm.addEventListener("submit", function (e) {
            var pct = num(slicePct ? slicePct.value : 0);
            if (!sliceDesc || !sliceDesc.value.trim() || pct <= 0 || pct > 100) {
                e.preventDefault();
                (sliceDesc && !sliceDesc.value.trim() ? sliceDesc : slicePct).focus();
            }
        });
    }
    var linkForm = document.getElementById("gl-v2-co-link-form");
    if (linkForm) {
        linkForm.addEventListener("submit", function (e) {
            var v = document.getElementById("co-link-invoice");
            if (!v || !v.value) { e.preventDefault(); var t = document.getElementById("co-link-invoice_trigger"); if (t) t.focus(); }
        });
    }

    // ── 5. Instellingen-zijpaneel ───────────────────────────────────────────────────────────────────
    var drawer = document.getElementById("gl-v2-co-drawer");
    var backdrop = document.getElementById("gl-v2-co-drawer-backdrop");
    if (!drawer) { updateSelection(); return; }
    var settingsForm = document.getElementById("gl-v2-co-settings-form");
    var ratesHost = document.getElementById("gl-v2-co-rates");
    var ratesEmpty = document.getElementById("gl-v2-co-rates-empty");
    var lastFocus = null;
    var issuerDefaults = { ratePerKm: null, userRates: {} };

    function currentType() { var r = $("input[name='ContractType']:checked", drawer); return r ? r.value : ""; }
    function applyType() {
        var t = currentType();
        var schijven = t === "1" || t === "3";
        var regie = t === "2" || t === "3";
        $$("[data-co-if]", drawer).forEach(function (el) {
            var k = el.getAttribute("data-co-if");
            el.hidden = k === "schijven" ? !schijven : !regie;
        });
    }
    $$("input[name='ContractType']", drawer).forEach(function (r) { r.addEventListener("change", applyType); });
    applyType();

    function focusables() {
        return $$("a[href], button:not([disabled]), input:not([disabled]):not([type='hidden']), [tabindex]:not([tabindex='-1'])", drawer)
            .filter(function (el) { return el.offsetParent !== null; });
    }
    function openDrawer(focus) {
        closeMenus();
        lastFocus = document.activeElement;
        drawer.hidden = false;
        if (backdrop) backdrop.hidden = false;
        document.documentElement.style.overflow = "hidden";
        if (focus === "rates") {
            var t = currentType();
            if (t !== "2" && t !== "3") { var reg = $("input[name='ContractType'][value='" + (t === "1" ? "3" : "2") + "']", drawer); if (reg) { reg.checked = true; applyType(); } }
            var block = document.getElementById("gl-v2-co-rates-block");
            if (block) block.scrollIntoView({ block: "start" });
            var first = $("[data-rate-input]", drawer);
            if (first) first.focus();
        } else {
            var f = focusables()[0];
            if (f) f.focus();
        }
        loadDefaults();
    }
    function closeDrawer() {
        drawer.hidden = true;
        if (backdrop) backdrop.hidden = true;
        document.documentElement.style.overflow = "";
        if (lastFocus && lastFocus.focus) lastFocus.focus();
    }
    document.addEventListener("click", function (e) {
        var open = e.target.closest(".js-co-open-settings");
        if (open) { e.preventDefault(); openDrawer(open.getAttribute("data-focus")); return; }
        if (e.target.closest(".js-co-close-settings")) { e.preventDefault(); closeDrawer(); }
    });
    if (backdrop) backdrop.addEventListener("click", closeDrawer);
    document.addEventListener("keydown", function (e) {
        if (drawer.hidden) return;
        if (e.key === "Escape") {
            // Een open keuzelijst sluit eerst zelf op Escape; het paneel pas bij de volgende.
            if ($(".gl-v2-select-panel.is-open")) return;
            e.preventDefault(); closeDrawer(); return;
        }
        if (e.key === "Tab") {
            var f = focusables();
            if (!f.length) return;
            var first = f[0], last = f[f.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        }
    });

    // Standaardtarieven van het gekozen coördinatiebedrijf (voor "aangepast · terug naar …" en nieuwe rijen).
    function issuerId() { var h = document.getElementById("co-issuer"); return h ? h.value : ""; }
    var defaultsFor = null;
    function loadDefaults() {
        var id = issuerId();
        if (!id || defaultsFor === id) { renderRateNotes(); return; }
        fetch(cfg.defaultsUrl + "?issuerCompanyId=" + encodeURIComponent(id))
            .then(function (r) { return r.ok ? r.json() : Promise.reject(r); })
            .then(function (d) {
                defaultsFor = id;
                issuerDefaults = { ratePerKm: d.ratePerKm, userRates: {} };
                (d.userRates || []).forEach(function (u) { issuerDefaults.userRates[u.userId] = u.hourlyRate; });
                var km = document.getElementById("co-km-allowance");
                if (km && !km.value && d.ratePerKm != null) km.value = Number(d.ratePerKm).toFixed(2);
                renderRateNotes();
            })
            .catch(function () { renderRateNotes(); });
    }
    var issuerHidden = document.getElementById("co-issuer");
    if (issuerHidden) issuerHidden.addEventListener("change", function () { defaultsFor = null; loadDefaults(); });

    function rateRows() { return $$("[data-rate-row]", drawer); }
    function renderRateNotes() {
        rateRows().forEach(function (row) {
            var uid = row.getAttribute("data-user-id");
            var input = $("[data-rate-input]", row);
            var note = $("[data-rate-note]", row);
            var def = issuerDefaults.userRates[uid];
            var custom = def != null && Math.abs(num(input.value) - Number(def)) > 0.004;
            row.classList.toggle("is-custom", !!custom);
            if (!note) return;
            if (custom) {
                note.hidden = false;
                note.innerHTML = "aangepast · <button type=\"button\" data-rate-reset>terug naar " + fmtEur(Number(def)) + "</button>";
            } else if (def != null) {
                note.hidden = false;
                note.textContent = "standaard " + fmtEur(Number(def)) + "/u";
                note.style.color = "var(--gl-v2-muted-light)";
            } else {
                note.hidden = true;
            }
            if (custom) note.style.color = "";
        });
        if (ratesEmpty) ratesEmpty.hidden = rateRows().length > 0;
    }
    drawer.addEventListener("input", function (e) { if (e.target.matches("[data-rate-input]")) renderRateNotes(); });
    drawer.addEventListener("click", function (e) {
        var reset = e.target.closest("[data-rate-reset]");
        if (reset) {
            var row = reset.closest("[data-rate-row]");
            var def = issuerDefaults.userRates[row.getAttribute("data-user-id")];
            if (def != null) { $("[data-rate-input]", row).value = Number(def).toFixed(2); renderRateNotes(); }
            return;
        }
        var rm = e.target.closest(".js-co-rate-remove");
        if (rm) { rm.closest("[data-rate-row]").remove(); renderRateNotes(); }
    });

    // Medewerker toevoegen
    var addUserHidden = document.getElementById("co-add-user");
    function initials(name) { return name.split(/[\s-]+/).filter(Boolean).slice(0, 2).map(function (p) { return p.charAt(0).toUpperCase(); }).join("") || "?"; }
    if (addUserHidden) {
        addUserHidden.addEventListener("change", function () {
            var uid = addUserHidden.value;
            if (!uid) return;
            var wrap = document.getElementById("co-add-user_select");
            var opt = wrap ? $(".gl-v2-select-option[data-value='" + uid.replace(/'/g, "\\'") + "']", wrap) : null;
            var name = opt ? (opt.textContent || "").trim() : uid;
            // Keuzelijst terug naar de placeholder, klaar voor de volgende.
            addUserHidden.value = "";
            var label = wrap ? $(".gl-v2-select-trigger-label", wrap) : null;
            var trig = wrap ? $(".gl-v2-select-trigger", wrap) : null;
            if (label && wrap) label.textContent = wrap.getAttribute("data-placeholder") || "";
            if (trig) trig.classList.remove("is-filled");
            if (wrap) $$(".gl-v2-select-option.is-selected", wrap).forEach(function (o) { o.classList.remove("is-selected"); });

            if (rateRows().some(function (r) { return r.getAttribute("data-user-id") === uid; })) {
                toast("warning", "Al in de lijst", name + " staat er al.");
                return;
            }
            var def = issuerDefaults.userRates[uid];
            var row = document.createElement("div");
            row.className = "gl-v2-co-raterow";
            row.setAttribute("data-rate-row", "");
            row.setAttribute("data-user-id", uid);
            row.innerHTML =
                '<input type="hidden" data-rate-user value="' + escapeHtml(uid) + '" />' +
                '<span class="gl-v2-avatar gl-v2-co-avatar">' + escapeHtml(initials(name)) + "</span>" +
                '<span class="gl-v2-co-rate-name">' + escapeHtml(name) + "</span>" +
                '<div class="gl-v2-field-box gl-v2-co-rate-box"><input type="number" class="gl-v2-field-input is-tabular" data-rate-input min="0" step="0.01" inputmode="decimal" value="' + (def != null ? Number(def).toFixed(2) : "") + '" aria-label="Uurtarief ' + escapeHtml(name) + '" /><span class="gl-v2-field-unit">€/u</span></div>' +
                '<button type="button" class="gl-v2-icon-btn gl-v2-co-icon-sm js-co-rate-remove" aria-label="' + escapeHtml(name) + ' verwijderen uit de lijst" title="Verwijderen uit de lijst"><i class="ph ph-x" aria-hidden="true"></i></button>' +
                '<span class="gl-v2-co-rate-note" data-rate-note hidden></span>';
            ratesHost.insertBefore(row, ratesEmpty);
            renderRateNotes();
            var inp = $("[data-rate-input]", row);
            if (inp) { inp.focus(); inp.select(); }
        });
    }

    // Opslaan: gewoon formulier. De rijen krijgen hier hun index (HourlyRates[i].…) zodat de modelbinder ze als
    // lijst leest — ook de rijen in een verborgen blok gaan mee, anders wist wisselen naar "Schijven" de tarieven.
    if (settingsForm) {
        settingsForm.addEventListener("submit", function (e) {
            if (!currentType()) {
                e.preventDefault();
                toast("warning", "Kies een contracttype", "Schijven, regie of een combinatie.");
                var r = $("input[name='ContractType']", drawer);
                if (r) r.focus();
                return;
            }
            var i = 0;
            rateRows().forEach(function (row) {
                var uid = $("[data-rate-user]", row);
                var rate = $("[data-rate-input]", row);
                if (!uid.value) return;
                uid.name = "HourlyRates[" + i + "].UserId";
                rate.name = "HourlyRates[" + i + "].HourlyRate";
                if (!rate.value) rate.value = "0";
                i++;
            });
            var save = document.getElementById("gl-v2-co-settings-save");
            if (save) { save.disabled = true; save.classList.add("is-loading"); }
        });
    }

    updateSelection();
})();
