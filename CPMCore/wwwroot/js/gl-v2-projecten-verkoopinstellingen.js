// gl-v2 — Verkoopinstellingen (design-handoff 45b): live voorbeeld voor de koper, eigen kostenposten, tekst met opmaakknoppen, wijzigingsteller en opslaan via fetch.
(function () {
    "use strict";
    var form = document.getElementById("vs-form");
    if (!form) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    function esc(t) { var d = document.createElement("div"); d.textContent = t == null ? "" : t; return d.innerHTML; }
    var canWrite = form.dataset.canWrite === "1";
    var nf0 = new Intl.NumberFormat("nl-BE", { maximumFractionDigits: 0 });
    var nf2 = new Intl.NumberFormat("nl-BE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    function eur(v) { return "€ " + nf0.format(Math.round(v)); }
    function num(s) {
        s = String(s || "").replace(/[€\s ]/g, "");
        if (s.indexOf(",") >= 0) s = s.replace(/\./g, "").replace(",", ".");
        else if ((s.match(/\./g) || []).length > 1) s = s.replace(/\./g, "");
        var n = parseFloat(s); return isFinite(n) ? n : 0;
    }
    function val(id) { var e = document.getElementById(id); return e ? num(e.value) : 0; }
    function toast(msg) {
        var t = document.createElement("div"); t.className = "gl-v2-vs-toast"; t.setAttribute("role", "status"); t.textContent = msg; document.body.appendChild(t);
        requestAnimationFrame(function () { t.classList.add("is-in"); });
        setTimeout(function () { t.classList.remove("is-in"); setTimeout(function () { t.remove(); }, 300); }, 5000);
    }

    // ── bedragvelden netjes formatteren bij het verlaten ──
    form.addEventListener("focusout", function (e) { if (e.target.classList && e.target.classList.contains("vk-num") && e.target.value.trim() !== "") e.target.value = nf2.format(num(e.target.value)); });

    // ── Verkoop onder / Type registratie ──
    var regtype = $("#vs-regtype"), onder = $("#vs-onder"), typ = $("#vs-type"), typeWrap = $("#vs-type-wrap");
    function segSet(seg, value) { $$("button", seg).forEach(function (b) { var on = b.dataset.value === value; b.classList.toggle("is-active", on); b.setAttribute("aria-pressed", on ? "true" : "false"); }); }
    function segGet(seg) { var a = $("button.is-active", seg); return a ? a.dataset.value : ""; }
    onder.addEventListener("click", function (e) {
        var b = e.target.closest("button"); if (!b) return; segSet(onder, b.dataset.value);
        regtype.value = b.dataset.value === "reg" ? "1" : segGet(typ);
        typeWrap.hidden = b.dataset.value === "reg"; refresh();
    });
    typ.addEventListener("click", function (e) { var b = e.target.closest("button"); if (!b) return; segSet(typ, b.dataset.value); regtype.value = b.dataset.value; refresh(); });

    // ── eigen kostenposten ──
    var extra = $("#vs-extra");
    function rowHtml() {
        return '<div class="gl-v2-vs-extra-row">' +
            '<div class="gl-v2-field"><label class="gl-v2-field-label">Naam</label><div class="gl-v2-field-box"><input class="gl-v2-field-input" name="extraname" maxlength="120" autocomplete="off" /></div></div>' +
            '<div class="gl-v2-field"><label class="gl-v2-field-label">Bedrag</label><div class="gl-v2-field-box"><input type="text" inputmode="decimal" class="gl-v2-field-input is-num is-tabular vk-num" name="extraamount" value="0,00" autocomplete="off" /><span class="gl-v2-field-unit">€</span></div></div>' +
            '<div class="gl-v2-field"><label class="gl-v2-field-label">Toelichting</label><div class="gl-v2-field-box"><input class="gl-v2-field-input" name="extranote" maxlength="200" autocomplete="off" /></div></div>' +
            '<button type="button" class="gl-v2-btn gl-v2-btn-text js-vs-extra-del" aria-label="Kost verwijderen"><i class="ph ph-trash" aria-hidden="true"></i></button></div>';
    }
    var addBtn = $("#vs-extra-add");
    if (addBtn) addBtn.addEventListener("click", function () { extra.insertAdjacentHTML("beforeend", rowHtml()); var last = extra.lastElementChild; $("input", last).focus(); refresh(); });
    extra.addEventListener("click", function (e) { var d = e.target.closest(".js-vs-extra-del"); if (d) { d.closest(".gl-v2-vs-extra-row").remove(); refresh(); } });

    // ── bank ──
    var bank = $("#vs-bank"), newWrap = $("#vs-newiban-wrap");
    function bankRefresh() { newWrap.hidden = !bank || bank.value !== "new"; }

    // ── tekst ──
    var edit = $("#vs-text-edit"), hid = $("#vs-text"), title = $("#vs-title");
    $$(".gl-v2-vs-rte-bar button").forEach(function (b) { b.addEventListener("mousedown", function (e) { e.preventDefault(); document.execCommand(b.dataset.cmd, false, null); refresh(); }); });
    var insEdit = $("#vs-ins-edit"), insHid = $("#vs-ins-text"), status = $("#vs-status");
    [edit, insEdit].forEach(function (el) { if (el) el.addEventListener("paste", function (e) { e.preventDefault(); var t = (e.clipboardData || window.clipboardData).getData("text/plain"); document.execCommand("insertText", false, t); }); });
    // titel en tekst van de inschrijvingspagina tonen we enkel als de status Inschrijving (4) gekozen is
    function insRefresh() { var on = status && status.value === "4"; $("#vs-ins").hidden = !on; $("#vs-ins-text-wrap").hidden = !on; }
    // enkel p, br, b, i, ul, li bewaren; geen attributen
    function clean(html) {
        var d = document.createElement("div"); d.innerHTML = html;
        (function walk(n) {
            Array.prototype.slice.call(n.childNodes).forEach(function (c) {
                if (c.nodeType === 1) {
                    var tag = c.tagName.toLowerCase();
                    if (tag === "strong") tag = "b"; if (tag === "em") tag = "i";
                    // Enter in het tekstvak maakt een <div> per regel: bewaar die als alinea, anders vallen de regels samen
                    if (tag === "div") { var par = document.createElement("p"); while (c.firstChild) par.appendChild(c.firstChild); n.replaceChild(par, c); c = par; tag = "p"; }
                    walk(c);
                    if (["p", "br", "b", "i", "ul", "li"].indexOf(tag) < 0) { while (c.firstChild) n.insertBefore(c.firstChild, c); n.removeChild(c); }
                    else { while (c.attributes.length) c.removeAttribute(c.attributes[0].name); }
                } else if (c.nodeType !== 3) n.removeChild(c);
            });
        })(d);
        return d.innerHTML.replace(/(<br>)+$/g, "").trim();
    }
    function textLen() { return (edit.innerText || "").replace(/\s+/g, " ").trim().length; }

    // ── notarishonorarium (Tarief 2026, art. 17 nr. 81 2° en art. 2 §2): zelfde regels als NotarisTarief in VerkoopService.cs ──
    var J = [[7500, 2.5], [10000, 2.5], [12500, 1.75], [15495, 1.71], [18595, 1.14], [186005, 0.57], [249905, 0.20], [null, 0.20]];
    var JBIS = [[7500, 2.5], [10000, 2.5], [12500, 2.0], [15495, 1.5], [18595, 0.5], [186005, 0.485], [249905, 0.3], [null, 0.2]];
    var NOT_BTW_BASIS = 855 + 309 + 100, NOT_VAST_ZONDER_BTW = 100;
    function notaris(grond, bouw, rt, jbis, overschrijving) {
        var basis = Math.round(grond + bouw); if (basis <= 0) return { basis: 0, fee: 0, totaal: overschrijving, jbis: jbis };
        var rest = basis, fee = jbis ? 257 : 285;
        (jbis ? JBIS : J).forEach(function (s) { if (rest <= 0) return; var d = s[0] == null ? rest : Math.min(rest, s[0]); fee += d * s[1] / 100; rest -= d; });
        fee = Math.round(fee * 100) / 100;
        var btw = Math.round((fee + NOT_BTW_BASIS) * 21) / 100;
        return { basis: basis, fee: fee, jbis: jbis, totaal: fee + NOT_BTW_BASIS + NOT_VAST_ZONDER_BTW + overschrijving + btw };
    }

    // ── live voorbeeld ──
    var pv = $("#vs-preview");
    function refresh() {
        $("#vs-title-count").textContent = title.value.length + " / 70 tekens · lang genoeg om informatief te zijn, kort genoeg om niet afgeknipt te worden";
        $("#vs-text-count").textContent = "opmaak zonder HTML-codes · " + nf0.format(textLen()) + " tekens";
        var rt = parseInt(regtype.value, 10) || 0, regPct = val("vs-reg"), vatPct = val("vs-vat");
        var note = rt === 1 ? "registratierechten op grond en bouw" : rt === 2 ? "btw op de bouw, registratierechten op de grond" : "btw op grond en bouw";
        $("#vs-regime-note").textContent = "Gevolg voor de koper: " + note + ".";
        changes();
        if (!pv || !pv.dataset.grond) return;
        var grond = parseFloat(pv.dataset.grond) || 0, bouw = parseFloat(pv.dataset.bouw) || 0, apt = pv.dataset.apt === "1";
        var reg = 0, vat = 0;
        if (rt === 2) { reg = grond * regPct / 100; vat = bouw * vatPct / 100; } else if (rt === 1) { reg = (grond + bouw) * regPct / 100; } else { vat = (grond + bouw) * vatPct / 100; }
        // 21 % btw op akte, hypotheek, basisakte, verkaveling, landmeter en eigen posten; aansluitkosten aan het btw-percentage van het project
        var kosten21 = val("vs-parcel") + val("vs-surveyor") + (apt ? val("vs-basecert") : 0);
        $$('input[name="extraamount"]', extra).forEach(function (i) { kosten21 += num(i.value); });
        var aansl = val("vs-connection");
        var kosten = kosten21 + aansl + Math.round(kosten21 * 21) / 100 + Math.round(aansl * vatPct) / 100;
        reg = Math.round(reg * 100) / 100; vat = Math.round(vat * 100) / 100;
        $("#vs-pv-vraag").textContent = eur(grond + bouw);
        $("#vs-pv-grond").textContent = eur(grond); $("#vs-pv-bouw").textContent = eur(bouw);
        $("#vs-pv-reg").textContent = eur(reg); $("#vs-pv-vat").textContent = eur(vat); $("#vs-pv-kosten").textContent = eur(kosten);
        $("#vs-pv-reg-l").textContent = "Registratierechten " + nf0.format(regPct) + " %"; $("#vs-pv-vat-l").textContent = "Btw " + nf0.format(vatPct) + " %";
        $("#vs-pv-reg").parentElement.hidden = rt === 0; $("#vs-pv-vat").parentElement.hidden = rt === 1;
        var not = notaris(grond, bouw, rt, $("#vs-enige").checked, val("vs-mortgage"));
        $("#vs-pv-not-l").textContent = "Notaris (schaal " + (not.jbis ? "Jbis" : "J") + ")"; $("#vs-pv-not").textContent = eur(not.totaal);
        $("#vs-pv-not-l").title = "Ereloon " + eur(not.fee) + " + administratieve kosten, uitgaven, overschrijving, rechten en 21 % btw · grondslag " + eur(not.basis);
        $("#vs-pv-totaal").textContent = eur(grond + bouw + reg + vat + kosten + not.totaal);
    }

    // ── wijzigingsteller ──
    function snapshot() {
        var o = {};
        $$("input[name], select[name]", form).forEach(function (el) {
            if (el.name === "__RequestVerificationToken" || el.name === "text" || el.name === "instext") return;
            var k = el.name; if (el.type === "checkbox") o[k] = el.checked ? "1" : "0"; else o[k] = (o[k] == null ? "" : o[k] + "|") + el.value;
        });
        o.text = clean(edit.innerHTML);
        o.instext = clean(insEdit.innerHTML);
        return o;
    }
    var start = snapshot();
    function changes() {
        var now = snapshot(), n = 0;
        Object.keys(now).forEach(function (k) { if (now[k] !== start[k]) n++; });
        Object.keys(start).forEach(function (k) { if (!(k in now)) n++; });
        $("#vs-changes").textContent = n > 0 ? n + (n === 1 ? " wijziging" : " wijzigingen") + " niet opgeslagen" : "";
        $("#vs-dirty-badge").hidden = n === 0;
    }
    form.addEventListener("input", refresh); form.addEventListener("change", function () { bankRefresh(); insRefresh(); refresh(); });
    if (bank) bank.addEventListener("change", function () { bankRefresh(); });

    // ── opslaan ──
    form.addEventListener("submit", function (e) {
        e.preventDefault(); if (!canWrite) return;
        hid.value = clean(edit.innerHTML);
        insHid.value = clean(insEdit.innerHTML);
        var btn = $("#vs-save"); btn.disabled = true;
        fetch(form.dataset.saveUrl, { method: "POST", body: new FormData(form), credentials: "same-origin" })
            .then(function (r) { return r.json(); })
            .then(function (j) { if (j.ok) { window.onbeforeunload = null; location.href = j.redirect; } else { toast(j.error || "Opslaan is mislukt."); btn.disabled = false; } })
            .catch(function () { toast("Opslaan is mislukt."); btn.disabled = false; });
    });
    bankRefresh(); insRefresh(); refresh();
})();
