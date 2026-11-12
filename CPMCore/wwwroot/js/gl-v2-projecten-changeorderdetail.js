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
    // De container van ALLE leveranciersgroepen (28t): elke groep heeft een eigen <tbody>, de regel-logica werkt over alle groepen heen.
    var rowsBody = document.getElementById("gl-v2-co-groups");
    function initCurrency(scope) { if (window.CurrencyMask && scope) CurrencyMask.init(scope.querySelectorAll(".Currencymask")); }
    initCurrency(rowsBody);
    var defaultCommission = document.getElementById("gl-v2-co-default-commission");

    // Omschrijving: begint op één regel en groeit mee met wat je typt (field-sizing in de CSS; dit is de terugval voor browsers zonder).
    function autoGrow(ta) { ta.style.height = "auto"; ta.style.height = ta.scrollHeight + "px"; }

    // Minwerk (28v): aantal en prijs staan als positief getal in het scherm; de knop vóór de omschrijving geeft het teken. Een minwerk wordt
    // aan kostprijs verrekend: commissie vast op 0 %.
    function rowSign(tr) { return tr.classList.contains("is-minwerk") ? -1 : 1; }

    // De schadevergoeding (28v) staat als eigen rij direct onder het minwerk (zoals in het ontwerp). Deze helpers vinden haar terug.
    function dmgRow(tr) { var n = tr.nextElementSibling; return n && n.classList.contains("js-co-damage-row") ? n : null; }
    function dq(tr, sel) { var d = dmgRow(tr); return d ? $(sel, d) : null; }
    function dqa(tr, sel) { var d = dmgRow(tr); return d ? $$(sel, d) : []; }

    function setMinwerk(tr, minus) {
        tr.classList.toggle("is-minwerk", minus);
        var hidden = $(".js-co-isminwerk", tr);
        if (hidden) hidden.value = minus ? "true" : "false";
        var btn = $(".js-co-sign", tr);
        if (btn) {
            btn.classList.toggle("is-minus", minus);
            btn.setAttribute("aria-pressed", minus ? "true" : "false");
            btn.setAttribute("aria-label", "Meerwerk of minwerk: nu " + (minus ? "minwerk" : "meerwerk"));
            btn.title = minus ? "Minwerk: waarde die wegvalt, aan kostprijs verrekend. Klik voor meerwerk." : "Meerwerk: waarde die erbij komt. Klik voor minwerk.";
            var glyph = $("span", btn); if (glyph) glyph.textContent = minus ? "−" : "+";
        }
        var dmgPanel = dmgRow(tr);
        if (dmgPanel) {
            dmgPanel.hidden = !minus;
            if (!minus) { var dmgInput = dq(tr, ".js-co-dmg-input"); if (dmgInput) dmgInput.value = "0"; var dmgBox = dq(tr, ".js-co-dmg-box"); if (dmgBox) dmgBox.hidden = true; var dmgAdd = dq(tr, ".js-co-dmg-add-row"); if (dmgAdd) dmgAdd.hidden = false; }
        }
        var comm = $(".js-co-commission", tr);
        if (comm && !tr.classList.contains("is-price-locked")) {
            if (minus) { comm.dataset.prevCommission = comm.value; comm.value = "0.00"; comm.readOnly = true; }
            else { comm.readOnly = false; if (comm.dataset.prevCommission !== undefined) comm.value = comm.dataset.prevCommission; else if (defaultCommission) comm.value = defaultCommission.value; }
        }
    }

    // Eén regel in euro's (28v): base = brutowaarde met teken; dmg = schadevergoeding (positief, enkel bij minwerk met een geopende subregel);
    // clientExcl = wat de klant betaalt/terugkrijgt na vergoeding; supplierCost = wat de leverancier rekent/crediteert; bouwheer = vergoeding die bij de bouwheer blijft.
    function rowMoney(tr) {
        var sign = rowSign(tr);
        var qty = getNum($(".js-co-number", tr));
        var price = getNum($(".js-co-price", tr));
        var commission = getNum($(".js-co-commission", tr));
        var vat = getNum($(".js-co-vat", tr));
        var base = sign * qty * price;
        var commissionAmt = base * (commission / 100);
        var box = dq(tr, ".js-co-dmg-box");
        var pct = sign < 0 && box && !box.hidden ? Math.min(100, Math.max(0, getNum(dq(tr, ".js-co-dmg-input")))) : 0;
        var dmg = Math.round(qty * price * pct) / 100;
        var toEl = dq(tr, ".js-co-dmg-to");
        var toSupplier = toEl && toEl.value === "2";
        var clientExcl = base + commissionAmt + dmg;
        return {
            qty: qty, price: price, base: base, commissionAmt: commissionAmt, pct: pct, dmg: dmg, toSupplier: toSupplier,
            clientExcl: clientExcl, vatAmt: clientExcl * (vat / 100),
            supplierCost: base + (pct > 0 && toSupplier ? dmg : 0), bouwheer: pct > 0 && !toSupplier ? dmg : 0
        };
    }

    function recalcRow(tr) {
        var sign = rowSign(tr);
        var number = getNum($(".js-co-number", tr));
        var price = getNum($(".js-co-price", tr));
        var commission = getNum($(".js-co-commission", tr));
        var priceKlant = sign * price * (1 + commission / 100);
        var total = number * priceKlant;
        var $priceKlant = $(".js-co-priceklant", tr);
        var $total = $(".js-co-total", tr);
        if ($priceKlant) $priceKlant.textContent = formatEUR(priceKlant);
        if ($total) $total.textContent = formatEUR(total);

        // Subregel schadevergoeding: bedrag, wat de klant terugkrijgt, wat de leverancier crediteert.
        var box = dq(tr, ".js-co-dmg-box");
        if (box && !box.hidden) {
            var m = rowMoney(tr);
            var baseAbs = number * price;
            var back = baseAbs - m.dmg;
            var set = function (cls, text) { var el = dq(tr, "." + cls); if (el) el.textContent = text; };
            set("js-co-dmg-base", formatEUR(baseAbs));
            set("js-co-dmg-amt", "+ " + formatEUR(m.dmg));
            set("js-co-dmg-back", formatEUR(-back));
            var dmgDesc = $(".js-co-description", tr);
            var dmgBoxEl = dq(tr, ".js-co-dmg-box");
            if (dmgBoxEl) dmgBoxEl.setAttribute("aria-label", "Schadevergoeding op " + ((dmgDesc && dmgDesc.value.trim()) || "dit minwerk"));
            var group = tr.closest(".js-co-group");
            var supplier = group ? (($(".gl-v2-co-group-name", group) || {}).textContent || "De leverancier") : "De leverancier";
            set("js-co-dmg-sub", m.toSupplier
                ? supplier + " crediteert enkel " + formatEUR(back) + "; de vergoeding gaat naar de leverancier."
                : supplier + " crediteert de volle " + formatEUR(baseAbs) + "; de vergoeding blijft bij de bouwheer.");
            dqa(tr, ".js-co-dmg-quick").forEach(function (b) { b.classList.toggle("is-active", Math.abs(parseFloat(b.getAttribute("data-pct")) - m.pct) < 0.001); });
            dqa(tr, ".js-co-dmg-toggle").forEach(function (b) {
                var on = (b.getAttribute("data-to") === "2") === m.toSupplier;
                b.classList.toggle("is-active", on); b.setAttribute("aria-pressed", on ? "true" : "false");
            });
        }
    }

    function recomputeTotals() {
        if (!rowsBody) return;
        var cost = 0, commissionTotal = 0, vatTotal = 0, clientTotal = 0;
        var settle = { value: 0, dmg: 0, credit: 0, bouwheer: 0, any: false };
        var costLines = [];
        $$(".js-co-group", rowsBody).forEach(function (group) {
            var gCost = 0, gComm = 0, gClient = 0, gBouwheer = 0, n = 0;
            $$(".js-co-row", group).forEach(function (tr) {
                var m = rowMoney(tr);
                gCost += m.supplierCost;
                gComm += m.commissionAmt;
                gClient += m.clientExcl;
                gBouwheer += m.bouwheer;
                vatTotal += m.vatAmt;
                if (m.pct > 0) { settle.any = true; settle.value += m.qty * m.price; settle.dmg += m.dmg; settle.credit += m.supplierCost; settle.bouwheer += m.bouwheer; }
                n++;
            });
            cost += gCost;
            commissionTotal += gComm;
            clientTotal += gClient;
            var setIn = function (cls, v) { var el = $("." + cls, group); if (el) el.textContent = formatEUR(v); };
            setIn("js-co-group-cost", gCost);
            setIn("js-co-group-commission", gComm);
            setIn("js-co-group-subtotal", gClient);
            var dmgLine = $(".js-co-group-damage-line", group);
            if (dmgLine) { dmgLine.hidden = !(gBouwheer > 0); setIn("js-co-group-damage", gBouwheer); var dv = $(".js-co-group-damage", group); if (dv) dv.textContent = "+ " + formatEUR(gBouwheer); }
            var count = $(".js-co-group-count", group);
            if (count) count.textContent = n + (n === 1 ? " regel" : " regels");
            var title = $(".js-co-g-title", group);
            var sub = $(".js-co-group-subtitle", group);
            var name = $(".gl-v2-co-group-name", group);
            if (sub) sub.textContent = (title && title.value.trim()) || (name ? name.textContent : "");
            costLines.push({ key: group.getAttribute("data-key"), name: (name ? name.textContent : "") + (title && title.value.trim() ? " · " + title.value.trim() : ""), cost: gCost });
        });
        // Afrekening van het minwerk (28v): enkel intern, enkel zichtbaar zolang er een schadevergoeding staat.
        var settleBox = document.getElementById("gl-v2-co-settle");
        if (settleBox) {
            settleBox.hidden = !settle.any;
            var setS = function (id, text) { var el = document.getElementById(id); if (el) el.textContent = text; };
            setS("gl-v2-co-settle-value", formatEUR(-settle.value));
            setS("gl-v2-co-settle-damage", "+ " + formatEUR(settle.dmg));
            setS("gl-v2-co-settle-back", formatEUR(-(settle.value - settle.dmg)));
            setS("gl-v2-co-settle-credit", formatEUR(settle.credit));
            setS("gl-v2-co-settle-result", (settle.bouwheer > 0 ? "+ " : "") + formatEUR(settle.bouwheer));
        }
        var excl = clientTotal;

        var grand = excl + vatTotal;
        var set = function (id, v) { var el = document.getElementById(id); if (el) el.textContent = formatEUR(v); };
        set("gl-v2-co-sum-cost", cost);
        set("gl-v2-co-sum-commission", commissionTotal);
        set("gl-v2-co-sum-excl", excl);
        set("gl-v2-co-sum-vat", vatTotal);
        // Netto negatief (28v): "Terug te betalen aan de klant" (een creditpost in Facturatie), het bedrag zonder min.
        set("gl-v2-co-sum-total", Math.abs(grand));
        var totalLabel = document.getElementById("gl-v2-co-sum-total-label");
        if (totalLabel) totalLabel.textContent = grand < 0 ? "Terug te betalen aan de klant" : "Te betalen door klant";
        set("gl-v2-co-quote-cost", cost); // kaart "Offerte leverancier" (28a): bedrag excl. btw
        renderCostLines(costLines);
        renderSupplierChips();
        var typeEl = document.getElementById("gl-v2-co-type");
        if (typeEl) typeEl.textContent = excl < 0 ? "Minwerk — aan kostprijs verrekend, zonder commissie" : "Meerwerk";
        recomputeTermAmounts(excl);
    }

    // Totalen (28t): intern de kostprijs per leverancier; de commissie staat samen op één regel.
    function renderCostLines(lines) {
        var commissionLine = document.getElementById("gl-v2-co-commission-line");
        if (!commissionLine || lines.length === 0) return;
        $$(".js-co-cost-line", commissionLine.parentNode).forEach(function (tr) { tr.parentNode.removeChild(tr); });
        lines.forEach(function (l) {
            var tr = document.createElement("tr");
            tr.className = "js-co-cost-line";
            tr.setAttribute("data-key", l.key);
            var label = document.createElement("td");
            label.colSpan = 7; label.className = "is-right";
            label.textContent = "Kostprijs " + l.name + " · intern";
            var val = document.createElement("td");
            val.className = "is-right gl-v2-co-tabular gl-v2-co-internal";
            val.textContent = formatEUR(l.cost);
            tr.appendChild(label); tr.appendChild(val);
            commissionLine.parentNode.insertBefore(tr, commissionLine);
        });
    }

    // "Leveranciers" bij Opdracht (28t): enkel een samenvatting, chips met het aantal regels per groep.
    function renderSupplierChips() {
        var box = document.getElementById("gl-v2-co-supplier-chips");
        if (!box) return;
        $$(".gl-v2-co-supplier-chip", box).forEach(function (c) { c.parentNode.removeChild(c); });
        var none = $(".gl-v2-co-supplier-none", box);
        var groups = $$(".js-co-group", rowsBody);
        if (none) none.hidden = groups.length > 0;
        groups.forEach(function (g) {
            var name = ($(".gl-v2-co-group-name", g) || {}).textContent || "";
            var ini = ($(".gl-v2-co-group-avatar", g) || {}).textContent || "";
            var n = $$(".js-co-row", g).length;
            var chip = document.createElement("span");
            chip.className = "gl-v2-co-supplier-chip";
            chip.setAttribute("data-key", g.getAttribute("data-key"));
            var i = document.createElement("span"); i.className = "gl-v2-co-chip-ini"; i.setAttribute("aria-hidden", "true"); i.textContent = ini;
            var nm = document.createElement("span"); nm.className = "gl-v2-co-chip-name"; nm.textContent = name;
            var ct = document.createElement("span"); ct.className = "gl-v2-co-chip-n"; ct.textContent = n + (n === 1 ? " regel" : " regels");
            chip.appendChild(i); chip.appendChild(nm); chip.appendChild(ct);
            box.insertBefore(chip, none);
        });
    }

    // De modelbinder leest rows[0], rows[1], … en stopt bij het eerste gat: na het verwijderen van een
    // rij moeten de overige dus opnieuw aaneensluitend genummerd worden, anders gaan alle rijen ná het
    // gat bij Opslaan verloren.
    function renumberRows() {
        // Elke regel draagt de sleutel van de groep waarin ze nu staat (slepen kan ze naar een andere leverancier verplaatsen).
        $$(".js-co-group", rowsBody).forEach(function (group) {
            var key = group.getAttribute("data-key");
            $$(".js-co-row", group).forEach(function (tr) { var k = $(".js-co-groupkey", tr); if (k) k.value = key; });
        });
        $$(".js-co-row", rowsBody).forEach(function (tr, i) {
            tr.setAttribute("data-index", i);
            var scopes = [tr]; var companion = dmgRow(tr); if (companion) { scopes.push(companion); companion.setAttribute("data-index", i); }
            scopes.forEach(function (scope) {
                $$("[name]", scope).forEach(function (el) {
                    el.name = el.name.replace(/^rows\[\d+\]/, "rows[" + i + "]");
                });
            });
        });
    }

    // Zelfde verhaal voor de groepen (groups[0], groups[1], …): na het verwijderen/toevoegen opnieuw aaneensluitend nummeren.
    function renumberGroups() {
        $$(".js-co-group", rowsBody).forEach(function (group, i) {
            $$("[name]", group).forEach(function (el) {
                el.name = el.name.replace(/^groups\[\d+\]/, "groups[" + i + "]");
            });
        });
    }

    if (rowsBody) {
        rowsBody.addEventListener("input", function (e) {
            if (e.target.classList.contains("js-co-g-title")) { recomputeTotals(); return; }
            var tr = e.target.closest(".js-co-row");
            if (!tr) { var companionRow = e.target.closest(".js-co-damage-row"); tr = companionRow && companionRow.previousElementSibling; }   // het percentage staat in de rij eronder
            if (!tr || !tr.classList.contains("js-co-row")) return;
            if (e.target.classList.contains("js-co-description")) autoGrow(e.target);
            recalcRow(tr);
            recomputeTotals();
        });
        rowsBody.addEventListener("click", function (e) {
            var del = e.target.closest(".js-co-delete-row");
            if (del) {
                var delRow = del.closest(".js-co-row");
                var delCompanion = dmgRow(delRow);
                if (delCompanion) delCompanion.remove();
                delRow.remove();
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
            // De groep onder de aanwijzer bepaalt het doel; binnen die groep gaat de regel vóór de eerste regel waarvan het midden lager ligt.
            var target = null;
            $$(".js-co-group", rowsBody).forEach(function (group) {
                var rect = group.getBoundingClientRect();
                if (e.clientY >= rect.top && e.clientY <= rect.bottom) target = $(".js-co-group-body", group);
            });
            if (!target) return;
            var before = null;
            var rows = $$(".js-co-row", target);
            for (var i = 0; i < rows.length; i++) {
                if (rows[i] === dragRow) continue;
                var rect = rows[i].getBoundingClientRect();
                if (e.clientY < rect.top + rect.height / 2) { before = rows[i]; break; }
            }
            // De regel en haar schadevergoeding (rij eronder) verhuizen samen.
            var dragCompanion = dmgRow(dragRow);
            var dragEnd = dragCompanion || dragRow;
            if (before) { if (dragEnd.nextElementSibling !== before) target.insertBefore(dragRow, before); }
            else if (target.lastElementChild !== dragEnd) target.appendChild(dragRow);
            if (dragCompanion) dragRow.parentNode.insertBefore(dragCompanion, dragRow.nextSibling);
        });

        var endDrag = function () {
            if (!dragRow) return;
            dragRow.classList.remove("is-dragging");
            rowsBody.classList.remove("gl-v2-co-rows-dragging");
            dragRow = null;
            renumberRows();
            recomputeTotals();
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
            // Pijl omhoog/omlaag loopt ook over de groepsgrens: de regel schuift naar de vorige/volgende leverancier.
            var rowCompanion = dmgRow(row);
            if (e.key === "ArrowUp" && index > 0) rows[index - 1].parentNode.insertBefore(row, rows[index - 1]);
            else if (e.key === "ArrowDown" && index < rows.length - 1) {
                var nextRow = rows[index + 1], nextCompanion = dmgRow(nextRow);
                nextRow.parentNode.insertBefore(row, (nextCompanion || nextRow).nextSibling);
            }
            else return;
            if (rowCompanion) row.parentNode.insertBefore(rowCompanion, row.nextSibling);
            renumberRows();
            recomputeTotals();
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
                if (input && !tr.classList.contains("is-minwerk")) input.value = value;   // minwerk blijft op 0 % (aan kostprijs)
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

    // Factureren alle leveranciers zelf, dan is er geen facturatieplan: de kaart verdwijnt (en telt niet mee bij het verzenden).
    var planCard = document.getElementById("gl-v2-co-plan-card");
    function planIsHidden() { return !!planCard && planCard.hidden; }
    function syncPlanVisibility() {
        if (!planCard || !rowsBody) return;
        var groups = $$(".js-co-group", rowsBody);
        var anyByUs = groups.some(function (g) { var t = $(".js-co-g-invoiced", g); return t && t.checked; });
        planCard.hidden = groups.length > 0 && !anyByUs;
    }

    // "+ Regel": komt in de groep (leverancier) waarvan je op de knop klikt.
    function addRowTo(group) {
        var body = $(".js-co-group-body", group);
        var index = $$(".js-co-row", rowsBody).length; // klopt altijd: renumberRows() houdt de reeks dicht
        var commission = defaultCommission ? getNum(defaultCommission) : 0;
        return fetch(cfg.addRowUrl + "?index=" + index + "&projectId=" + encodeURIComponent(cfg.projectId) + "&vatKlantTypeId=" + encodeURIComponent(cfg.vatKlantTypeId || "") + "&vatKlant=" + encodeURIComponent(cfg.vatKlant) + "&commission=" + encodeURIComponent(commission))
            .then(function (r) { return r.text(); })
            .then(function (html) {
                body.insertAdjacentHTML("beforeend", html);
                var tr = $$(".js-co-row", body).pop();   // de nieuwe regel (daarna volgt nog de rij voor haar schadevergoeding)
                var k = $(".js-co-groupkey", tr); if (k) k.value = group.getAttribute("data-key");
                renumberRows();   // de nieuwe regel staat midden in het formulier: volgnummers opnieuw in DOM-volgorde
                initSelects(tr);
                initCurrency(tr);
                var d0 = $(".js-co-description", tr); if (d0) autoGrow(d0);
                recalcRow(tr);
                recomputeTotals();
                var desc = $(".js-co-description", tr);
                if (desc) desc.focus();
            });
    }

    if (rowsBody) {
        rowsBody.addEventListener("click", function (e) {
            var add = e.target.closest(".js-co-add-row");
            if (add) { addRowTo(add.closest(".js-co-group")); return; }

            // Schadevergoeding op een minwerk (28v): openen met het standaardpercentage, weghalen, snelkeuze, gaat naar bouwheer/leverancier.
            var dmgAdd = e.target.closest(".js-co-dmg-add");
            var dmgRemove = e.target.closest(".js-co-dmg-remove");
            var dmgQuick = e.target.closest(".js-co-dmg-quick");
            var dmgTo = e.target.closest(".js-co-dmg-toggle");
            if (dmgAdd || dmgRemove || dmgQuick || dmgTo) {
                var dmgHost = e.target.closest(".js-co-damage-row");
                var dmgMain = dmgHost && dmgHost.previousElementSibling;
                var box = $(".js-co-dmg-box", dmgHost), input = $(".js-co-dmg-input", dmgHost), add = $(".js-co-dmg-add-row", dmgHost);
                if (dmgAdd && !dmgAdd.disabled) { input.value = String(cfg.defaultDamage != null ? cfg.defaultDamage : 25); box.hidden = false; add.hidden = true; input.focus(); }
                else if (dmgRemove) { input.value = "0"; box.hidden = true; add.hidden = false; }
                else if (dmgQuick && !dmgQuick.disabled) input.value = dmgQuick.getAttribute("data-pct");
                else if (dmgTo && !dmgTo.disabled) $(".js-co-dmg-to", dmgHost).value = dmgTo.getAttribute("data-to");
                recalcRow(dmgMain);
                recomputeTotals();
                return;
            }

            var sign = e.target.closest(".js-co-sign");
            if (sign && !sign.disabled) {
                var signRow = sign.closest(".js-co-row");
                setMinwerk(signRow, !signRow.classList.contains("is-minwerk"));
                recalcRow(signRow);
                recomputeTotals();
                return;
            }

            var remove = e.target.closest(".js-co-remove-group");
            if (remove) {
                var group = remove.closest(".js-co-group");
                var filled = $$(".js-co-description", group).filter(function (t) { return t.value.trim() !== ""; }).length;
                var name = ($(".gl-v2-co-group-name", group) || {}).textContent || "deze leverancier";
                var doRemove = function () {
                    group.parentNode.removeChild(group);
                    renumberGroups(); renumberRows(); recomputeTotals();
                    if ($$(".js-co-group", rowsBody).length === 0) openPicker();
                };
                if (filled === 0) { doRemove(); return; }
                var ask = window.GlV2Dialog
                    ? window.GlV2Dialog.confirm({ tone: "danger", icon: "ph-trash", title: name + " verwijderen?", description: "De " + filled + (filled === 1 ? " regel" : " regels") + " van deze leverancier verdwijnen mee uit de offerte.", confirmLabel: "Verwijderen", cancelLabel: "Behouden" })
                    : Promise.resolve(window.confirm(name + " verwijderen?"));
                ask.then(function (ok) { if (ok) doRemove(); });
            }
        });

        // Contract per groep: zonder contract toont de groep een goud waarschuwinkje; de titel volgt de activiteit zolang je er niets eigens zette.
        rowsBody.addEventListener("change", function (e) {
            var wrap = selectWrap(e.target);
            if (!wrap || wrap.getAttribute("data-role") !== "group-contract") {
                if (e.target.classList && e.target.classList.contains("js-co-g-invoiced")) {
                    var label = $(".gl-v2-toggle-label", e.target.closest(".gl-v2-co-group-invoiced"));
                    if (label) label.textContent = e.target.checked ? "Via ons gefactureerd" : "Factureert zelf";
                    syncPlanVisibility();
                }
                return;
            }
            var group = wrap.closest(".js-co-group");
            var opt = chosenOption(wrap);
            var has = !!(opt && opt.getAttribute("data-value"));
            group.classList.toggle("is-no-contract", !has);
            var warn = $(".js-co-group-nocontract", group);
            if (warn) warn.hidden = has;
            var title = $(".js-co-g-title", group);
            if (has && title && !title.value.trim()) { title.value = opt.textContent.replace(/^Contract · /, "").trim(); recomputeTotals(); }
        });
    }

    // "Leverancier toevoegen" (28u): het zoekvak opent onder de laatste groep, geen modal.
    var addGroupBtn = document.getElementById("gl-v2-co-add-group");
    var picker = document.getElementById("gl-v2-co-picker");
    function openPicker() {
        if (!picker) return;
        picker.hidden = false;
        if (addGroupBtn) addGroupBtn.hidden = true;
        var input = $(".gl-v2-combo-input", picker);
        if (input) input.focus();
    }
    function closePicker() {
        if (!picker || $$(".js-co-group", rowsBody).length === 0) return; // zonder groep blijft het zoekvak open
        picker.hidden = true;
        if (addGroupBtn) addGroupBtn.hidden = false;
    }
    if (addGroupBtn) addGroupBtn.addEventListener("click", openPicker);
    if (picker) {
        picker.addEventListener("click", function (e) { if (e.target.closest(".js-co-cancel-pick")) closePicker(); });
        picker.addEventListener("keydown", function (e) { if (e.key === "Escape") closePicker(); });
        // companyId 0 = een groep zonder leverancier (later te kiezen in de kop van de groep).
        function addGroupFor(companyId, name) {
            var exists = companyId && $$(".js-co-group", rowsBody).some(function (g) { return g.getAttribute("data-supplier") === String(companyId); });
            if (exists) { toast("warning", "Staat al in deze offerte", (name || "Deze leverancier") + " heeft hier al een groep."); return; }
            var groupIndex = $$(".js-co-group", rowsBody).length;
            var rowIndex = $$(".js-co-row", rowsBody).length;
            var commission = defaultCommission ? getNum(defaultCommission) : 0;
            fetch(cfg.addGroupUrl + "?projectId=" + encodeURIComponent(cfg.projectId) + "&companyId=" + encodeURIComponent(companyId) +
                "&groupIndex=" + groupIndex + "&rowIndex=" + rowIndex + "&vatKlant=" + encodeURIComponent(cfg.vatKlant) +
                "&vatKlantTypeId=" + encodeURIComponent(cfg.vatKlantTypeId || "") + "&commission=" + encodeURIComponent(commission))
                .then(function (r) { if (!r.ok) throw new Error("status " + r.status); return r.text(); })
                .then(function (html) {
                    rowsBody.insertAdjacentHTML("beforeend", html);
                    var group = rowsBody.lastElementChild;
                    initSelects(group);
                    initCurrency(group);
                    $$(".js-co-description", group).forEach(autoGrow);
                    $$(".js-co-row", group).forEach(recalcRow);
                    renumberGroups(); renumberRows(); recomputeTotals();
                    picker.hidden = true;
                    if (addGroupBtn) addGroupBtn.hidden = false;
                    var desc = $(".js-co-description", group);
                    if (desc) desc.focus();
                })
                .catch(function () { toast("danger", "Leverancier niet toegevoegd", "Probeer het opnieuw."); });
        }
        document.addEventListener("gl-v2:combo-select", function (e) {
            if (!picker.contains(e.target)) return;
            var companyId = e.detail && e.detail.id;
            if (companyId) addGroupFor(companyId, e.detail.text);
        });
        picker.addEventListener("click", function (e) { if (e.target.closest(".js-co-pick-none")) addGroupFor(0, ""); });
    }

    // Leverancier later kiezen: een groep zonder leverancier toont een zoekveld in haar kop. Na de keuze wordt het de naam van de groep,
    // en het contract kiest uit de contracten van díe leverancier (precies één = meteen gekoppeld).
    function initialsOf(text) {
        return String(text || "").replace(/[^A-Za-z ]/g, "").split(" ").filter(Boolean).slice(0, 2).map(function (w) { return w[0]; }).join("").toUpperCase() || "·";
    }
    document.addEventListener("gl-v2:combo-select", function (e) {
        var pickBox = e.target.closest ? e.target.closest(".js-co-g-pick") : null;
        if (!pickBox) return;
        var group = pickBox.closest(".js-co-group");
        var id = e.detail && e.detail.id;
        if (!group || !id) return;
        var taken = $$(".js-co-group", rowsBody).some(function (g) { return g !== group && g.getAttribute("data-supplier") === String(id); });
        if (taken) { toast("warning", "Staat al in deze offerte", (e.detail.text || "Deze leverancier") + " heeft hier al een groep."); return; }

        group.setAttribute("data-supplier", id);
        var hiddenSupplier = $(".js-co-g-supplier", group); if (hiddenSupplier) hiddenSupplier.value = id;
        var nameEl = $(".js-co-g-name", group);
        if (nameEl) { nameEl.textContent = e.detail.text; nameEl.hidden = false; }
        var avatar = $(".gl-v2-co-group-avatar", group); if (avatar) avatar.textContent = initialsOf(e.detail.text);
        var head = $(".gl-v2-co-group-head", group); if (head) head.classList.remove("has-pick");
        var who = $(".gl-v2-co-group-who", group); if (who) who.classList.remove("is-pick");   // de regelteller verschijnt weer, het label verdwijnt
        var pickLabel = $(".js-co-g-pick-label", group); if (pickLabel) pickLabel.parentNode.removeChild(pickLabel);
        pickBox.parentNode.removeChild(pickBox);
        var warn = $(".js-co-group-nocontract", group);
        if (warn) warn.innerHTML = '<i class="ph ph-warning" aria-hidden="true"></i>';
        if (warn) warn.appendChild(document.createTextNode("Geen contract met " + e.detail.text + " — het project toont een waarschuwing tot er een contract aan hangt."));

        var wrap = $('.gl-v2-select[data-role="group-contract"]', group);
        if (wrap && cfg.supplierContractsUrl) {
            fetch(cfg.supplierContractsUrl + "?projectId=" + encodeURIComponent(cfg.projectId) + "&companyId=" + encodeURIComponent(id))
                .then(function (r) { return r.json(); })
                .then(function (items) {
                    var list = [{ value: "", text: "Geen contract · koppelen" }].concat(items);
                    var chosen = items.length === 1 ? items[0].value : "";
                    window.GlV2Select.setItems(wrap, list, chosen);
                    var hidden = wrap.querySelector("input[type=hidden]");
                    if (hidden) hidden.dispatchEvent(new Event("change", { bubbles: true }));   // zelfde afhandeling als een gewone keuze (waarschuwing, titel)
                });
        }
        recomputeTotals();
    });

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
        // Foutoverzicht (DESIGN.md "Foutoverzicht (punt 24)"): de gedeelde engine blokkeert de submit en toont de rode kaart bovenaan.
        // De klant · eenheid draagt data-gl-v2-required (de view). Het contract en de omschrijving voor de klant zijn optioneel.
        // De regels-controles gelden enkel bij verzenden/omzetten; de submitter onthouden we in een listener die vóór die van de engine staat.
        var sendingNow = false;
        mainForm.addEventListener("submit", function (e) {
            sendingNow = !!(e.submitter && e.submitter.name === "afterSave" && (e.submitter.value === "send" || e.submitter.value === "convert"));
        });
        var errorSummary = window.GlV2ErrorSummary ? window.GlV2ErrorSummary.init({
            form: mainForm,
            container: document.getElementById("gl-v2-error-summary"),
            stickyLabel: "fouten",
            validators: [function (errors) {
                if (!rowsBody) return;
                var rows = $$(".js-co-row", rowsBody);
                var loc = "Regels per leverancier";
                // Altijd (ook bij gewoon opslaan): minstens één regel met een omschrijving en een bedrag.
                var hasAmountRow = rows.some(function (tr) {
                    var d = $(".js-co-description", tr), p = $(".js-co-price", tr);
                    return d && d.value.trim() !== "" && getNum(p) > 0;
                });
                if (!hasAmountRow) {
                    errors.push({ message: "Voeg minstens één regel met een bedrag toe.", field: "Regels", location: loc, tab: "", target: rowsBody });
                    return;
                }
                if (!sendingNow) return;
                $$(".js-co-group", rowsBody).forEach(function (g) {
                    var empty = $$(".js-co-description", g).every(function (t) { return t.value.trim() === ""; });
                    if (!empty) return;
                    var name = ($(".gl-v2-co-group-name", g) || {}).textContent || "Een leverancier";
                    errors.push({ message: "heeft nog geen regels. Voeg regels toe of verwijder de leverancier.", field: name.trim(), location: loc, tab: "", target: g });
                });
                var review = rows.filter(function (tr) { return tr.classList.contains("is-needs-review"); });
                if (review.length > 0) errors.push({ message: "Bevestig eerst de regels die op \"controleer\" staan.", field: review.length === 1 ? "1 regel" : review.length + " regels", location: loc, tab: "", target: review[0] });
                if (!planIsHidden() && termsBody && $$(".js-co-term-row", termsBody).length > 0 && Math.abs(termsTotal() - 100) >= 0.01)
                    errors.push({ message: "moet samen 100 % zijn.", field: "Facturatieplan", location: "Facturatieplan", tab: "", target: termsBody });
            }]
        }) : null;

        mainForm.addEventListener("submit", function (e) {
            if (e.defaultPrevented || !sendingNow) return;
            // Per leverancier: geen contract is geen reden om te blokkeren, wel om te waarschuwen (zoals het project dat ook doet).
            var noContract = rowsBody ? $$(".js-co-group.is-no-contract", rowsBody).map(function (g) { return ($(".gl-v2-co-group-name", g) || {}).textContent; }) : [];
            var noSupplier = rowsBody ? $$(".js-co-group", rowsBody).filter(function (g) { return g.getAttribute("data-supplier") === "0"; }).length : 0;
            if (noSupplier > 0) toast("warning", "Nog geen leverancier", noSupplier === 1 ? "Een groep heeft nog geen leverancier. Je kan de offerte verzenden, maar vul hem later in." : noSupplier + " groepen hebben nog geen leverancier. Je kan de offerte verzenden, maar vul ze later in.");
            if (noContract.length > 0) toast("warning", "Geen contract", noContract.join(", ") + (noContract.length === 1 ? " heeft" : " hebben") + " nog geen contract in dit project.");
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
