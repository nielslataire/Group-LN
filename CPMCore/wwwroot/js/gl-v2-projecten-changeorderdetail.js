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
        // Valutaveld (AutoNumeric): de zichtbare tekst is "1.234,50"; het kale getal komt uit het component.
        var an = window.AutoNumeric && AutoNumeric.getAutoNumericElement(el);
        if (an) { var n = an.getNumber(); return n == null || isNaN(n) ? 0 : n; }
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
    function initCurrency(scope) { if (window.CurrencyMask && scope) CurrencyMask.init(scope.querySelectorAll(".Currencymask")); }
    initCurrency(rowsBody);
    var defaultCommission = document.getElementById("gl-v2-co-default-commission");

    // Omschrijving: begint op één regel en groeit mee met wat je typt (field-sizing in de CSS; dit is de terugval voor browsers zonder).
    function autoGrow(ta) { ta.style.height = "auto"; ta.style.height = ta.scrollHeight + "px"; }

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
            if (e.target.classList.contains("js-co-description")) autoGrow(e.target);
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
        $$(".js-co-description", rowsBody).forEach(autoGrow);
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

    // ── Bijlagen bij de offerte: direct opladen/verwijderen (AJAX), het formulier blijft ongemoeid ───────────
    var attachList = document.getElementById("gl-v2-co-attach-list");
    var attachFile = document.getElementById("gl-v2-co-attach-file");
    if (attachList && cfg.changeOrderId > 0) {
        var attachEmpty = document.getElementById("gl-v2-co-attach-empty");
        var attachStatus = document.getElementById("gl-v2-co-attach-status");
        var antiforgery = function () { var t = document.querySelector("#gl-v2-co-form input[name='__RequestVerificationToken']"); return t ? t.value : ""; };
        var setStatus = function (text) { if (attachStatus) attachStatus.textContent = text || ""; };
        var syncEmpty = function () { if (attachEmpty) attachEmpty.hidden = attachList.children.length > 0; };

        function addAttachmentItem(a) {
            var li = document.createElement("li");
            li.className = "gl-v2-co-attach-item";
            li.dataset.id = a.id;
            var icon = document.createElement("i"); icon.className = "ph ph-file"; icon.setAttribute("aria-hidden", "true");
            var name = document.createElement(a.url ? "a" : "span");
            name.className = "gl-v2-co-attach-name" + (a.url ? " gl-v2-co-link" : "");
            name.textContent = a.fileName;
            if (a.url) { name.href = a.url; name.target = "_blank"; name.rel = "noopener"; }
            var size = document.createElement("span"); size.className = "gl-v2-co-attach-size"; size.textContent = a.sizeText;
            var del = document.createElement("button");
            del.type = "button"; del.className = "gl-v2-co-row-delete js-co-attach-delete"; del.title = "Bijlage verwijderen";
            del.setAttribute("aria-label", "Bijlage " + a.fileName + " verwijderen");
            del.innerHTML = '<i class="ph ph-trash" aria-hidden="true"></i>';
            li.appendChild(icon); li.appendChild(name); li.appendChild(size); li.appendChild(del);
            attachList.appendChild(li);
            syncEmpty();
        }

        function postAttachment(url, body) {
            body.append("projectId", cfg.projectId);
            body.append("changeOrderId", cfg.changeOrderId);
            body.append("__RequestVerificationToken", antiforgery());
            return fetch(url, { method: "POST", body: body }).then(function (r) { return r.json(); });
        }

        if (attachFile) {
            attachFile.addEventListener("change", function () {
                var files = Array.prototype.slice.call(attachFile.files || []);
                attachFile.value = "";
                var chain = Promise.resolve();
                files.forEach(function (file) {
                    chain = chain.then(function () {
                        setStatus("Bezig met opladen: " + file.name + " …");
                        var fd = new FormData();
                        fd.append("file", file, file.name);
                        return postAttachment(cfg.attachAddUrl, fd).then(function (res) {
                            if (res.success) { addAttachmentItem(res); setStatus(""); }
                            else { setStatus(""); toast("danger", "Bijlage niet toegevoegd", res.message || file.name); }
                        }).catch(function () { setStatus(""); toast("danger", "Bijlage niet toegevoegd", file.name); });
                    });
                });
            });
        }

        attachList.addEventListener("click", function (e) {
            var btn = e.target.closest ? e.target.closest(".js-co-attach-delete") : null;
            if (!btn) return;
            var li = btn.closest(".gl-v2-co-attach-item");
            if (!li) return;
            btn.disabled = true;
            var fd = new FormData();
            fd.append("attachmentId", li.dataset.id);
            postAttachment(cfg.attachDeleteUrl, fd).then(function (res) {
                if (res.success) { li.remove(); syncEmpty(); }
                else { btn.disabled = false; toast("danger", "Bijlage niet verwijderd", res.message || ""); }
            }).catch(function () { btn.disabled = false; toast("danger", "Bijlage niet verwijderd", ""); });
        });
    }

    // Commissie met twee cijfers na de komma (5 → 5.00) zodra je het veld verlaat; opgeslagen als decimal(9,2).
    document.addEventListener("change", function (e) {
        var t = e.target;
        if (!t || !t.classList || !(t.classList.contains("js-co-commission") || t === defaultCommission)) return;
        var n = parseFloat((t.value || "").toString().replace(",", "."));
        if (!isNaN(n)) t.value = n.toFixed(2);
    });

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

    // Keuzelijsten = de gedeelde Basis-keuzelijst (GlV2/_SelectList): de waarde staat in de verborgen input, de gekozen
    // optieknop (.is-selected) draagt de data-attributen. Btw-code: zichtbaar "6 % · CODE", tooltip = volledige omschrijving.
    function selectWrap(el) { return el && el.closest ? el.closest(".gl-v2-select") : null; }
    function chosenOption(wrap) { return wrap ? wrap.querySelector(".gl-v2-select-option.is-selected") : null; }
    function setSelectValue(wrap, value) { // zonder change-event; de aanroeper rekent zelf door
        if (!wrap) return;
        var hidden = wrap.querySelector("input[type=hidden]"), label = wrap.querySelector(".gl-v2-select-trigger-label");
        wrap.querySelectorAll(".gl-v2-select-option").forEach(function (o) {
            var on = o.getAttribute("data-value") === String(value);
            o.classList.toggle("is-selected", on);
            if (on) { hidden.value = String(value); if (label) label.textContent = o.textContent.trim(); }
        });
    }
    function initSelects(scope) { if (window.GlV2Select && window.GlV2Select.init) window.GlV2Select.init(scope); }

    // Btw-code per regel (migratie 073): het verborgen percentage volgt de gekozen code.
    if (rowsBody) {
        rowsBody.addEventListener("change", function (e) {
            var wrap = selectWrap(e.target);
            if (!wrap || wrap.getAttribute("data-role") !== "vattype") return;
            var tr = wrap.closest(".js-co-row");
            var opt = chosenOption(wrap);
            var input = tr ? $(".js-co-vat", tr) : null;
            if (input && opt) input.value = opt.getAttribute("data-vat");
            if (tr) recalcRow(tr);
            recomputeTotals();
        });
    }

    // Klant · eenheid: vast (cfg) of gekozen in het scherm zelf (Leeg beginnen) — de schijven van de termijnen horen bij die klant.
    function currentClientId() {
        var picker = document.getElementById("gl-v2-co-client");
        return (picker && picker.value) || cfg.clientAccountId || 0;
    }

    // "Leeg beginnen" (28b): de klant · eenheid wordt hier gekozen — de btw klant volgt de betalingsgroep
    // van die eenheid en werkt meteen door op alle regels (en op regels die er nog bij komen).
    var clientSelect = document.getElementById("gl-v2-co-client");
    if (clientSelect) {
        clientSelect.addEventListener("change", function () {
            var opt = clientSelect.value ? chosenOption(selectWrap(clientSelect)) : null;
            if (!opt) return;
            // Andere klant = andere schijven: de keuzelijsten van de termijnen opnieuw vullen.
            fetch(cfg.stagesUrl + "?projectId=" + encodeURIComponent(cfg.projectId) + "&clientAccountId=" + encodeURIComponent(clientSelect.value))
                .then(function (r) { return r.json(); })
                .then(function (items) {
                    $$('.gl-v2-select[data-role="term-stage"]').forEach(function (w) { window.GlV2Select.setItems(w, items, items.length ? items[0].value : ""); });
                });
            cfg.vatKlant = getNum({ value: opt.getAttribute("data-vat") });
            cfg.vatKlantTypeId = opt.getAttribute("data-vat-type") || null;
            var label = document.getElementById("gl-v2-co-vat-label");
            if (label) label.textContent = opt.getAttribute("data-vat-label") || "";
            if (rowsBody) {
                $$(".js-co-row", rowsBody).forEach(function (tr) {
                    var input = $(".js-co-vat", tr);
                    if (input) input.value = cfg.vatKlant;
                    // De btw-code volgt de betalingsgroep van de gekozen klant (migratie 073).
                    var sel = $('.gl-v2-select[data-role="vattype"]', tr);
                    if (sel && cfg.vatKlantTypeId) setSelectValue(sel, cfg.vatKlantTypeId);
                    recalcRow(tr);
                });
                recomputeTotals();
            }
        });
    }

    var addRowBtn = document.getElementById("gl-v2-co-add-row");
    if (addRowBtn) {
        addRowBtn.addEventListener("click", function () {
            var index = $$(".js-co-row", rowsBody).length; // klopt altijd: renumberRows() houdt de reeks dicht
            var commission = defaultCommission ? getNum(defaultCommission) : 0;
            fetch(cfg.addRowUrl + "?index=" + index + "&projectId=" + encodeURIComponent(cfg.projectId) + "&vatKlantTypeId=" + encodeURIComponent(cfg.vatKlantTypeId || "") + "&vatKlant=" + encodeURIComponent(cfg.vatKlant) + "&commission=" + encodeURIComponent(commission))
                .then(function (r) { return r.text(); })
                .then(function (html) {
                    rowsBody.insertAdjacentHTML("beforeend", html);
                    var tr = rowsBody.lastElementChild;
                    initSelects(tr);
                    initCurrency(tr);
                    var d0 = $(".js-co-description", tr); if (d0) autoGrow(d0);
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
            var wrap = selectWrap(e.target);
            if (wrap && wrap.getAttribute("data-role") === "term-trigger") {
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
        var url = cfg.addTermUrl + "?index=" + index + "&projectId=" + cfg.projectId + "&kind=" + kind + "&clientAccountId=" + currentClientId() +
            (percentage != null ? "&percentage=" + percentage : "");
        return fetch(url).then(function (r) { return r.text(); }).then(function (html) {
            var saldoRow = beforeSaldo ? $('.js-co-term-row[data-kind="3"]', termsBody) : null;
            if (saldoRow) saldoRow.insertAdjacentHTML("beforebegin", html);
            else termsBody.insertAdjacentHTML("beforeend", html);
            renumberTerms();
            initSelects(termsBody);
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
    // De klant (enkel bij een nieuwe rij een keuzelijst) is een verplichte FK — blokkeer de POST vóór de server
    // ze afwijst en de ingevulde regels verloren gaan. Het contract is optioneel. Verzenden vraagt
    // daarbovenop minstens één regel, geen "controleer"-vlaggen meer en een plan van samen 100 %.
    var mainForm = document.getElementById("gl-v2-co-form");
    if (mainForm) {
        mainForm.addEventListener("submit", function (e) {
            // Enkel de klant · eenheid is verplicht. Het contract is optioneel (offerte én wijzigingsopdracht): het veld staat zelf op
            // "Geen contract (nog te koppelen)" en de server (ChangeOrderDetailV2Save) vraagt er ook geen. Dit blokkeerde het opslaan
            // van een offerte zonder contract, zonder dat de server ooit werd aangeroepen.
            var client = document.getElementById("gl-v2-co-client");
            if (client && !client.value) {
                e.preventDefault();
                var trigger = document.getElementById(client.id + "_trigger");
                if (trigger) trigger.focus();
                toast("warning", "Nog niet compleet", "Kies eerst een klant · eenheid.");
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

    // Meer acties (⋯ in de voetbalk, 28r): een keuze sluit het menu, ook als ze een modal opent (Verwijderen) of het formulier verzendt (Omzetten).
    document.addEventListener("click", function (e) {
        if (e.target.closest("#gl-v2-co-more-menu .gl-v2-menu-item") && window.GlV2Menu) window.GlV2Menu.closeAll();
    });
})();
