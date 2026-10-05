// gl-v2 — Projecten/ChangeOrderDetailV2 (design-handoff punt 28, "Wijzigingsopdracht — scherm per stap").
// Alles hier dient enkel de BEWERKBARE fase (concept, 28a/28b); in elke latere stap is het scherm een
// server-gerenderde leesweergave en doen de knoppen gewone form-posts (zie de view). Drie stukken:
// 1) Regels-tabel: live herberekening (prijs klant/totaal/samenvatting), zelfde formule als de legacy
//    EditChangeOrder.cshtml (base=aantal*prijs, commissie=base*comm%, btw=(base+commissie)*btw%), plus
//    het "Commissie %"-veld van de kaart Opdracht dat op elke regel doorwerkt.
// 2) Facturatieplan: snelkeuzes bouwen de termijnrijen opnieuw op via AddTerm (server rendert de juiste
//    rijstructuur per Kind — Saldo heeft een vergrendeld %-veld, geen los kind-veld om client-side te
//    hertekenen), "+ Tussentijdse termijn" voegt één losse rij toe.
// 3) Opslaan/verzenden: verplichte velden en (bij verzenden) regels + 100 %-controle vóór de POST.
// 4) 21d — de verzendmodal (kanaal, ontvangers, regel, vervaldatum, voorbeeld), opent na het opslaan.
(function () {
    "use strict";

    var configEl = document.getElementById("gl-v2-co-config");
    if (!configEl) return;
    var cfg = JSON.parse(configEl.textContent);

    function $(sel, root) { return (root || document).querySelector(sel); }
    function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }

    function getNum(el) {
        if (!el) return 0;
        var v = (el.value || "").toString().trim();
        return parseFloat(v.replace(",", ".")) || 0;
    }
    function formatEUR(n) {
        return (n < 0 ? "− € " : "€ ") + Math.abs(n).toLocaleString("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }
    function toast(tone, title, body) {
        if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body });
    }

    // "Getekende versie opladen" (28c): het bestand kiezen ís de bevestiging — meteen posten.
    var signedFile = document.getElementById("gl-v2-co-signed-file");
    if (signedFile) {
        signedFile.addEventListener("change", function () {
            if (signedFile.files && signedFile.files.length > 0) signedFile.form.submit();
        });
    }

    if (!cfg.editable) return;

    // ── 1. Regels: herberekening ────────────────────────────────────────────────────────────────────
    var rowsBody = document.getElementById("gl-v2-co-rows-body");
    var defaultCommission = document.getElementById("gl-v2-co-default-commission");

    function recalcRow(tr) {
        var number = getNum($(".js-co-number", tr));
        var price = getNum($(".js-co-price", tr));
        var commission = getNum($(".js-co-commission", tr));
        var priceKlant = price * (1 + commission / 100);
        var total = number * priceKlant;
        var $priceKlant = $(".js-co-priceklant", tr);
        var $total = $(".js-co-total", tr);
        if ($priceKlant) $priceKlant.textContent = formatEUR(priceKlant);
        if ($total) $total.textContent = formatEUR(total);
    }

    function recomputeTotals() {
        if (!rowsBody) return;
        var cost = 0, commissionTotal = 0, vatTotal = 0;
        $$(".js-co-row", rowsBody).forEach(function (tr) {
            var qty = getNum($(".js-co-number", tr));
            var price = getNum($(".js-co-price", tr));
            var commission = getNum($(".js-co-commission", tr));
            var vat = getNum($(".js-co-vat", tr));
            var base = qty * price;
            var commissionAmt = base * (commission / 100);
            var vatAmt = (base + commissionAmt) * (vat / 100);
            cost += base;
            commissionTotal += commissionAmt;
            vatTotal += vatAmt;
        });
        var excl = cost + commissionTotal;
        var grand = excl + vatTotal;
        var set = function (id, v) { var el = document.getElementById(id); if (el) el.textContent = formatEUR(v); };
        set("gl-v2-co-sum-cost", cost);
        set("gl-v2-co-sum-commission", commissionTotal);
        set("gl-v2-co-sum-excl", excl);
        set("gl-v2-co-sum-vat", vatTotal);
        set("gl-v2-co-sum-total", grand);
        set("gl-v2-co-quote-cost", cost); // kaart "Offerte leverancier" (28a): bedrag excl. btw
        var typeEl = document.getElementById("gl-v2-co-type");
        if (typeEl) typeEl.textContent = excl < 0 ? "Minwerk — aan kostprijs verrekend, zonder commissie" : "Meerwerk";
        recomputeTermAmounts(excl);
    }

    // De modelbinder leest rows[0], rows[1], … en stopt bij het eerste gat: na het verwijderen van een
    // rij moeten de overige dus opnieuw aaneensluitend genummerd worden, anders gaan alle rijen ná het
    // gat bij Opslaan verloren.
    function renumberRows() {
        $$(".js-co-row", rowsBody).forEach(function (tr, i) {
            tr.setAttribute("data-index", i);
            $$("[name]", tr).forEach(function (el) {
                el.name = el.name.replace(/^rows\[\d+\]/, "rows[" + i + "]");
            });
        });
    }

    if (rowsBody) {
        rowsBody.addEventListener("input", function (e) {
            var tr = e.target.closest(".js-co-row");
            if (!tr) return;
            recalcRow(tr);
            recomputeTotals();
        });
        rowsBody.addEventListener("click", function (e) {
            var del = e.target.closest(".js-co-delete-row");
            if (del) {
                del.closest(".js-co-row").remove();
                renumberRows();
                recomputeTotals();
                return;
            }
            // "controleer"-vlag (onzekere waarde uit 20c): aanklikken = bevestigd.
            var flag = e.target.closest(".js-co-confirm-row");
            if (flag) {
                var row = flag.closest(".js-co-row");
                row.classList.remove("is-needs-review");
                var hidden = $(".js-co-needsreview", row);
                if (hidden) hidden.value = "false";
                flag.remove();
            }
        });
        $$(".js-co-row", rowsBody).forEach(recalcRow);
    }

    // Volgorde (28a): de greep verplaatst een rij — slepen met muis/vinger (pointer events, werkt ook op
    // touch) of, met focus op de greep, pijl omhoog/omlaag. De rij verhuist meteen in de DOM; bij het
    // loslaten worden de rows[i]-namen opnieuw genummerd, want de formuliervolgorde is wat bewaard wordt.
    if (rowsBody) {
        var dragRow = null;

        rowsBody.addEventListener("pointerdown", function (e) {
            var handle = e.target.closest(".js-co-handle");
            if (!handle || (e.pointerType === "mouse" && e.button !== 0)) return;
            dragRow = handle.closest(".js-co-row");
            dragRow.classList.add("is-dragging");
            rowsBody.classList.add("gl-v2-co-rows-dragging");
            handle.setPointerCapture(e.pointerId);
            e.preventDefault();
        });

        rowsBody.addEventListener("pointermove", function (e) {
            if (!dragRow) return;
            var rows = $$(".js-co-row", rowsBody);
            for (var i = 0; i < rows.length; i++) {
                var other = rows[i];
                if (other === dragRow) continue;
                var rect = other.getBoundingClientRect();
                var middle = rect.top + rect.height / 2;
                var isAbove = rows.indexOf(dragRow) > i;
                if (isAbove && e.clientY < middle) { rowsBody.insertBefore(dragRow, other); break; }
                if (!isAbove && e.clientY > middle) { rowsBody.insertBefore(dragRow, other.nextSibling); break; }
            }
        });

        var endDrag = function () {
            if (!dragRow) return;
            dragRow.classList.remove("is-dragging");
            rowsBody.classList.remove("gl-v2-co-rows-dragging");
            dragRow = null;
            renumberRows();
        };
        rowsBody.addEventListener("pointerup", endDrag);
        rowsBody.addEventListener("pointercancel", endDrag);

        rowsBody.addEventListener("keydown", function (e) {
            var handle = e.target.closest(".js-co-handle");
            if (!handle || (e.key !== "ArrowUp" && e.key !== "ArrowDown")) return;
            e.preventDefault();
            var row = handle.closest(".js-co-row");
            var rows = $$(".js-co-row", rowsBody);
            var index = rows.indexOf(row);
            if (e.key === "ArrowUp" && index > 0) rowsBody.insertBefore(row, rows[index - 1]);
            else if (e.key === "ArrowDown" && index < rows.length - 1) rowsBody.insertBefore(row, rows[index + 1].nextSibling);
            else return;
            renumberRows();
            handle.focus();
        });
    }

    // Commissie % op de kaart Opdracht: één waarde voor alle regels (per regel nog bij te sturen).
    if (defaultCommission && rowsBody) {
        defaultCommission.addEventListener("input", function () {
            var value = defaultCommission.value;
            $$(".js-co-row", rowsBody).forEach(function (tr) {
                var input = $(".js-co-commission", tr);
                if (input) input.value = value;
                recalcRow(tr);
            });
            recomputeTotals();
        });
    }

    // "Leeg beginnen" (28b): de klant · eenheid wordt hier gekozen — de btw klant volgt de betalingsgroep
    // van die eenheid en werkt meteen door op alle regels (en op regels die er nog bij komen).
    var clientSelect = document.getElementById("gl-v2-co-client");
    if (clientSelect) {
        clientSelect.addEventListener("change", function () {
            var opt = clientSelect.selectedIndex > 0 ? clientSelect.options[clientSelect.selectedIndex] : null;
            if (!opt) return;
            cfg.vatKlant = getNum({ value: opt.getAttribute("data-vat") });
            var label = document.getElementById("gl-v2-co-vat-label");
            if (label) label.textContent = opt.getAttribute("data-vat-label") || "";
            if (rowsBody) {
                $$(".js-co-vat", rowsBody).forEach(function (input) { input.value = cfg.vatKlant; });
                recomputeTotals();
            }
        });
    }

    var addRowBtn = document.getElementById("gl-v2-co-add-row");
    if (addRowBtn) {
        addRowBtn.addEventListener("click", function () {
            var index = $$(".js-co-row", rowsBody).length; // klopt altijd: renumberRows() houdt de reeks dicht
            var commission = defaultCommission ? getNum(defaultCommission) : 0;
            fetch(cfg.addRowUrl + "?index=" + index + "&vatKlant=" + encodeURIComponent(cfg.vatKlant) + "&commission=" + encodeURIComponent(commission))
                .then(function (r) { return r.text(); })
                .then(function (html) {
                    rowsBody.insertAdjacentHTML("beforeend", html);
                    var tr = rowsBody.lastElementChild;
                    recalcRow(tr);
                    recomputeTotals();
                    var desc = $(".js-co-description", tr);
                    if (desc) desc.focus();
                });
        });
    }

    // ── 2. Facturatieplan ───────────────────────────────────────────────────────────────────────────
    var termsBody = document.getElementById("gl-v2-co-terms-body");
    var termsTotalPct = document.getElementById("gl-v2-co-terms-total-pct");
    var termsTotalBadge = document.getElementById("gl-v2-co-terms-total-badge");

    function termAmount(pct, excl) { return Math.round(excl * (pct || 0) / 100 * 100) / 100; }

    function termsTotal() {
        return $$(".js-co-term-row", termsBody).reduce(function (sum, tr) { return sum + getNum($(".js-co-term-pct", tr)); }, 0);
    }

    // Het saldo is altijd "wat overblijft": zodra een voorschot/tussentijdse termijn wijzigt, vult het
    // saldo zichzelf aan tot 100 % (het %-veld van het saldo is vergrendeld).
    function rebalanceSaldo() {
        var saldoRow = $('.js-co-term-row[data-kind="3"]', termsBody);
        if (!saldoRow) return;
        var others = $$(".js-co-term-row", termsBody).reduce(function (sum, tr) {
            return tr === saldoRow ? sum : sum + getNum($(".js-co-term-pct", tr));
        }, 0);
        var input = $(".js-co-term-pct", saldoRow);
        if (input) input.value = Math.max(0, Math.round((100 - others) * 100) / 100);
    }

    function recomputeTermAmounts(excl) {
        if (!termsBody) return;
        $$(".js-co-term-row", termsBody).forEach(function (tr) {
            var amountCell = $(".js-co-term-amount", tr);
            if (amountCell) amountCell.textContent = formatEUR(termAmount(getNum($(".js-co-term-pct", tr)), excl));
        });
        var totalPct = termsTotal();
        if (termsTotalPct) termsTotalPct.textContent = Math.round(totalPct * 100) / 100 + " %";
        if (termsTotalBadge) {
            var ok = Math.abs(totalPct - 100) < 0.01;
            termsTotalBadge.textContent = ok ? "KLOPT" : "MOET 100 % ZIJN";
            termsTotalBadge.className = "gl-v2-badge " + (ok ? "is-positive" : "is-blocked");
        }
    }

    function renumberTerms() {
        $$(".js-co-term-row", termsBody).forEach(function (tr, i) {
            $$("[name]", tr).forEach(function (el) {
                el.name = el.name.replace(/^terms\[\d+\]/, "terms[" + i + "]");
            });
        });
    }

    if (termsBody) {
        termsBody.addEventListener("input", function (e) {
            if (e.target.classList.contains("js-co-term-pct")) {
                rebalanceSaldo();
                recomputeTotals();
                markPreset("eigen");
            }
        });
        termsBody.addEventListener("change", function (e) {
            if (e.target.classList.contains("js-co-term-trigger")) {
                var tr = e.target.closest(".js-co-term-row");
                var stageBox = $(".js-co-term-stagebox", tr);
                if (stageBox) stageBox.classList.toggle("is-hidden", e.target.value !== "2");
                markPreset("eigen");
            }
        });
        termsBody.addEventListener("click", function (e) {
            var del = e.target.closest(".js-co-delete-term");
            if (!del) return;
            del.closest(".js-co-term-row").remove();
            renumberTerms();
            rebalanceSaldo();
            recomputeTotals();
            markPreset("eigen");
        });
    }

    function addTermRow(kind, percentage, beforeSaldo) {
        var index = termsBody ? $$(".js-co-term-row", termsBody).length : 0;
        var url = cfg.addTermUrl + "?index=" + index + "&projectId=" + cfg.projectId + "&kind=" + kind +
            (percentage != null ? "&percentage=" + percentage : "");
        return fetch(url).then(function (r) { return r.text(); }).then(function (html) {
            var saldoRow = beforeSaldo ? $('.js-co-term-row[data-kind="3"]', termsBody) : null;
            if (saldoRow) saldoRow.insertAdjacentHTML("beforebegin", html);
            else termsBody.insertAdjacentHTML("beforeend", html);
            renumberTerms();
        });
    }

    var presetBar = document.getElementById("gl-v2-co-presets");
    function markPreset(preset) {
        if (!presetBar) return;
        $$(".gl-v2-co-preset", presetBar).forEach(function (b) { b.classList.toggle("is-active", b.getAttribute("data-preset") === preset); });
    }

    var addTermBtn = document.getElementById("gl-v2-co-add-term");
    if (addTermBtn) {
        addTermBtn.addEventListener("click", function () {
            // Een tussentijdse termijn hoort vóór het saldo; zonder saldo-rij komt er eerst een bij.
            var ensureSaldo = $('.js-co-term-row[data-kind="3"]', termsBody) ? Promise.resolve() : addTermRow(3, 100, false);
            ensureSaldo
                .then(function () { return addTermRow(2, null, true); })
                .then(function () { rebalanceSaldo(); recomputeTotals(); markPreset("eigen"); });
        });
    }

    if (presetBar) {
        presetBar.addEventListener("click", function (e) {
            var btn = e.target.closest(".gl-v2-co-preset");
            if (!btn) return;
            var preset = btn.getAttribute("data-preset");
            markPreset(preset);
            if (preset === "eigen") return; // laat de bestaande rijen ongemoeid

            termsBody.innerHTML = "";
            var chain;
            if (preset === "laatste-schijf") {
                chain = addTermRow(3, 100, false);
            } else if (preset === "voorschot-saldo") {
                chain = addTermRow(1, 30, false).then(function () { return addTermRow(3, 70, false); });
            } else if (preset === "na-ondertekening") {
                chain = addTermRow(1, 100, false).then(function () { return addTermRow(3, 0, false); });
            }
            if (chain) chain.then(recomputeTotals);
        });
    }

    // ── 3. Opslaan / verzenden ──────────────────────────────────────────────────────────────────────
    // Klant (enkel bij een nieuwe rij een keuzelijst) en leverancier·contract zijn verplichte FK's —
    // blokkeer de POST vóór de server ze afwijst en de ingevulde regels verloren gaan. Verzenden vraagt
    // daarbovenop minstens één regel, geen "controleer"-vlaggen meer en een plan van samen 100 %.
    var mainForm = document.getElementById("gl-v2-co-form");
    if (mainForm) {
        mainForm.addEventListener("submit", function (e) {
            var client = document.getElementById("gl-v2-co-client");
            var contract = document.getElementById("gl-v2-co-contractactivity");
            if ((client && !client.value) || (contract && !contract.value)) {
                e.preventDefault();
                (client && !client.value ? client : contract).focus();
                toast("warning", "Nog niet compleet", "Kies eerst een klant · eenheid en een leverancier · contract.");
                return;
            }

            var sending = e.submitter && e.submitter.name === "afterSave" && (e.submitter.value === "send" || e.submitter.value === "convert");
            if (!sending) return;

            var rows = rowsBody ? $$(".js-co-row", rowsBody) : [];
            var problem = null;
            if (rows.length === 0) problem = "Voeg minstens één regel toe.";
            else if (rows.some(function (tr) { return tr.classList.contains("is-needs-review"); })) problem = "Bevestig eerst de regels die op \"controleer\" staan.";
            else if (termsBody && $$(".js-co-term-row", termsBody).length > 0 && Math.abs(termsTotal() - 100) >= 0.01) problem = "Het facturatieplan moet samen 100 % zijn.";
            if (problem) {
                e.preventDefault();
                toast("warning", "Nog niet te verzenden", problem);
            }
        });
        // Zichtbaar dat er opgeslagen wordt: de knop toont een draaiertje en is niet nog eens aan te klikken. Geregistreerd
        // ná de validatie hierboven, dus enkel bij een submit die echt vertrekt.
        mainForm.addEventListener("submit", function (e) {
            if (e.defaultPrevented) return;
            var btn = e.submitter || document.getElementById("gl-v2-co-save");
            if (btn) { btn.classList.add("is-saving"); btn.setAttribute("aria-busy", "true"); }
            toast("info", "Bezig met opslaan …", "");
        });
    }

    recomputeTotals();
})();

// ── 4. Verzend- en omzetmodals ──────────────────────────────────────────────────────────────────────
// Eigen scope, los van de bewerkbare-fase-code hierboven: ook een VERZONDEN offerte (vergrendeld) opent
// hier "Opnieuw mailen" en "Omzetten naar wijzigingsopdracht".
(function () {
    "use strict";

    var configEl = document.getElementById("gl-v2-co-config");
    if (!configEl) return;
    var cfg = JSON.parse(configEl.textContent);

    function $(sel, root) { return (root || document).querySelector(sel); }
    function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }

    // ── 4. 21d — Verzenden ter ondertekening ────────────────────────────────────────────────────────
    // "Verzenden naar klant" slaat eerst op (afterSave=send); de server stuurt daarna terug naar dit
    // scherm met ?send=true en dan opent deze modal op de zopas bewaarde toestand (cfg.openSend). De
    // inhoud komt van de server (ontvangers, regel, vervaldatum = voorstel van de ondertekenmodule).
    var sendModalEl = document.getElementById("gl-v2-co-send-modal");

    function wireSend(body) {
        if (window.GlV2DatePicker) window.GlV2DatePicker.init();
        var data = $("#gl-v2-co-send-data", body);
        var subtitle = document.getElementById("gl-v2-co-send-subtitle");
        if (subtitle && data) subtitle.textContent = data.getAttribute("data-subtitle") || "";
        var submit = document.getElementById("gl-v2-co-send-submit");
        var submitLabel = document.getElementById("gl-v2-co-send-submit-label");
        var canSend = !!data && data.getAttribute("data-can-send") === "true";

        function refresh() {
            var checked = $('input[name="channel"]:checked', body);
            var channel = checked ? checked.value : "sign"; // offertemail: geen kanaalkeuze, enkel ontvangers
            $$(".js-send-signonly", body).forEach(function (el) { el.hidden = channel !== "sign"; });
            $$(".js-send-pdfonly", body).forEach(function (el) { el.hidden = channel !== "pdf"; });
            if (submitLabel) submitLabel.textContent = channel === "pdf" ? "Markeren als verzonden" : "Verzenden";
            var hasRecipient = channel === "pdf" || $$(".js-send-party:checked", body).length > 0;
            if (submit) submit.disabled = !(canSend && hasRecipient);
        }
        body.addEventListener("change", refresh);
        refresh();
    }

    function openSend() {
        var body = document.getElementById("gl-v2-co-send-body");
        if (!sendModalEl || !body || !window.bootstrap) return;
        body.innerHTML = '<div class="gl-v2-co-send-loading">Laden…</div>';
        window.bootstrap.Modal.getOrCreateInstance(sendModalEl).show();
        fetch(cfg.sendModalUrl)
            .then(function (r) { if (!r.ok) throw new Error("status " + r.status); return r.text(); })
            .then(function (html) { body.innerHTML = html; wireSend(body); })
            .catch(function (err) { body.innerHTML = '<div class="gl-v2-co-send-loading">Kon het verzendformulier niet laden (' + (err && err.message ? err.message : "geen verbinding") + ').</div>'; });
    }

    // ?send=true / ?convert=true uit de adresbalk: een herlaad of "terug" opent de modal niet opnieuw.
    function clearParam(name) {
        try {
            var url = new URL(window.location.href);
            url.searchParams.delete(name);
            window.history.replaceState(null, "", url.toString());
        } catch (err) { /* oude browser: de modal opent dan bij herladen opnieuw, geen kwaad */ }
    }

    if (cfg.openSend) {
        clearParam("send");
        openSend();
    }

    // 21c — Omzetten (offerte aan de klant → nieuwe wijzigingsopdracht): de modal laadt zijn inhoud zelf
    // (gl-v2-projecten-convertquote.js); hier enkel de ingangen. Een bewerkbare offerte slaat eerst op
    // (knop met afterSave=convert) en opent de modal via ?convert=true.
    function openConvert() {
        if (window.GlV2ConvertQuote && cfg.convertModalUrl) window.GlV2ConvertQuote.open({ url: cfg.convertModalUrl });
    }
    if (cfg.openConvert) {
        clearParam("convert");
        openConvert();
    }
    document.addEventListener("click", function (e) {
        if (e.target.closest(".js-co-open-convert")) { e.preventDefault(); openConvert(); }
        else if (e.target.closest(".js-co-open-send")) { e.preventDefault(); openSend(); }
    });
})();
