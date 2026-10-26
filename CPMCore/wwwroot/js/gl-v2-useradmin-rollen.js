// gl-v2 — Instellingen › Gebruikers en rollen › Rollen (design-handoff punt 49c). Rollenlijst links; rechts de naam, bedrijfstoegang, leden en de gedeelde
// rechtenboom (js/gl-v2-rechtenboom.js). Wijzigingen blijven lokaal ("pending") tot Opslaan: de balk onderaan toont wat er verandert en hoeveel leden geraakt
// worden. Van rol wisselen kan zonder herladen (met een vraag als er niet-opgeslagen wijzigingen zijn).
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-ua-rol");
    if (!root) return;
    var data = JSON.parse(document.getElementById("rol-data").textContent);
    var canWrite = root.dataset.canWrite === "1", canDelete = root.dataset.canDelete === "1";
    var token = (root.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";
    var U = { state: root.dataset.urlState, opslaan: root.dataset.urlOpslaan, nieuw: root.dataset.urlNieuw, verwijder: root.dataset.urlVerwijder, leden: root.dataset.urlLeden, kandidaten: root.dataset.urlKandidaten };

    var stand = data.stand || {};
    var pending = {};            // code -> niveau (alleen wat afwijkt van de opgeslagen stand)
    var pendingChips = null;     // null = ongewijzigd, anders de gewenste lijst bedrijf-id's
    var pendingNaam = null;
    var itemByCode = {};
    data.boom.forEach(function (i) { itemByCode[i.code] = i; });

    function $(id) { return document.getElementById(id); }
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function norm(s) { return String(s || "").toLowerCase(); }
    function toast(tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" }); }
    function post(url, d) {
        var fd = new FormData();
        Object.keys(d).forEach(function (k) { [].concat(d[k]).forEach(function (v) { if (v !== null && v !== undefined) fd.append(k, v); }); });
        return fetch(url, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); });
    }
    function fout(r) { toast("danger", "Niet gelukt", (r && r.bericht) || "De server gaf geen antwoord."); }
    try { var f = sessionStorage.getItem("ua-flash"); if (f) { sessionStorage.removeItem("ua-flash"); var fo = JSON.parse(f); toast(fo.tone, fo.title, fo.body); } } catch (e) { }

    var readonly = function () { return !canWrite || !!stand.admin; };
    function dirty() { return Object.keys(pending).length > 0 || pendingChips !== null || (pendingNaam !== null && pendingNaam !== stand.naam); }

    // ── Rollenlijst ───────────────────────────────────────────────────────────────────────────────
    function renderList() {
        var intern = data.rollen.filter(function (r) { return !r.portaal; }), portaal = data.rollen.filter(function (r) { return r.portaal; });
        function group(label, list) {
            if (!list.length) return "";
            return '<div class="gl-v2-ua-rolegroup">' + label + "</div>" + list.map(function (r) {
                return '<a class="gl-v2-ua-roleitem' + (r.id === stand.id ? " is-active" : "") + '" href="' + root.dataset.rollenUrl + "/" + r.id + '" data-rol="' + r.id + '"' + (r.id === stand.id ? ' aria-current="page"' : "") + '><span class="n">' + esc(r.naam) + '</span><span class="c" title="' + r.leden + ' leden">' + r.leden + "</span></a>";
            }).join("");
        }
        $("rol-list").innerHTML = group("Interne rollen", intern) + group("Portaalrollen", portaal) || '<div class="gl-v2-ua-pick-empty">Nog geen rollen.</div>';
        $("rol-count-roles").textContent = data.rollen.length;
        if ($("rol-count-users")) $("rol-count-users").textContent = data.aantalGebruikers;
    }
    $("rol-list").addEventListener("click", function (e) {
        var a = e.target.closest("[data-rol]"); if (!a) return;
        e.preventDefault();
        var id = parseInt(a.dataset.rol, 10);
        if (id === stand.id) return;
        var go = function () {
            fetch(U.state + "?id=" + id, { headers: { "X-Requested-With": "XMLHttpRequest" } }).then(function (r) { return r.json(); }).then(function (s) {
                stand = s; pending = {}; pendingChips = null; pendingNaam = null;
                try { history.replaceState(null, "", a.getAttribute("href")); } catch (x) { }
                renderAll();
            }).catch(function () { toast("danger", "Rol laden mislukt", "De server gaf geen antwoord."); });
        };
        if (dirty()) window.GlV2Bevestig({ title: "Wijzigingen niet opslaan?", desc: "Je hebt wijzigingen aan " + (stand.naam || "deze rol") + " die nog niet zijn opgeslagen. Ze gaan verloren als je van rol wisselt.", ok: "Niet opslaan", tone: "warning" }).then(function (ok) { if (ok) go(); });
        else go();
    });

    // ── Detail ────────────────────────────────────────────────────────────────────────────────────
    function levels() {
        var l = Object.assign({}, stand.niveaus || {});
        Object.keys(pending).forEach(function (c) { l[c] = pending[c]; });
        return l;
    }
    function chips() { return pendingChips !== null ? pendingChips : (stand.bedrijven || []); }

    function renderHead() {
        $("rol-detail").hidden = !stand.id;
        if (!stand.id) return;
        var n = $("rol-naam");
        if (document.activeElement !== n) n.value = pendingNaam !== null ? pendingNaam : stand.naam;
        n.disabled = readonly();
        $("rol-admin-badge").hidden = !stand.admin;
        if ($("rol-del")) $("rol-del").disabled = !canDelete || stand.admin || (stand.leden || []).length > 0;
        if ($("rol-del")) $("rol-del").title = stand.admin ? "De rol Admin kan niet verwijderd worden" : (stand.leden || []).length ? "Een rol met leden kan pas verwijderd worden nadat de leden een andere rol hebben" : "";
        if ($("rol-dup")) $("rol-dup").disabled = !canWrite;
    }
    function renderChips() {
        var on = chips();
        $("rol-chips").innerHTML = data.bedrijven.map(function (b) {
            var isOn = on.indexOf(b.id) >= 0;
            return '<button type="button" class="gl-v2-ua-chip' + (isOn ? " is-on" : "") + '" data-bedrijf="' + b.id + '" aria-pressed="' + (isOn ? "true" : "false") + '"' + (readonly() ? " disabled" : "") + ">" + (isOn ? '<i class="ph ph-check" aria-hidden="true"></i>' : "") + esc(b.naam) + "</button>";
        }).join("") || '<span class="gl-v2-ua-help">Er zijn nog geen facturatiebedrijven.</span>';
    }
    $("rol-chips").addEventListener("click", function (e) {
        var b = e.target.closest("[data-bedrijf]"); if (!b || readonly()) return;
        var id = parseInt(b.dataset.bedrijf, 10), cur = chips().slice(), i = cur.indexOf(id);
        if (i >= 0) cur.splice(i, 1); else cur.push(id);
        var orig = (stand.bedrijven || []).slice().sort().join(",");
        pendingChips = cur.slice().sort().join(",") === orig ? null : cur;
        renderChips(); renderBar();
    });

    function renderMembers() {
        var m = stand.leden || [];
        $("rol-stack").innerHTML = m.slice(0, 5).map(function (x) { return '<span class="gl-v2-ua-avatar is-sm" title="' + esc(x.naam) + '">' + esc(x.initialen) + "</span>"; }).join("");
        var names = m.slice(0, 2).map(function (x) { return x.naam; });
        $("rol-members-text").textContent = !m.length ? "Nog geen leden" : names.join(", ") + (m.length > 2 ? " en " + (m.length - 2) + (m.length - 2 === 1 ? " andere" : " anderen") : "");
        var dev = m.filter(function (x) { return x.afwijkend; }).length;
        $("rol-dev-text").hidden = !dev;
        $("rol-dev-text").textContent = dev + (dev === 1 ? " lid heeft afwijkingen" : " leden hebben afwijkingen") + " op deze rol.";
    }

    var tree = window.GlV2RightsTree.create($("rol-tree"), {
        items: data.boom, levels: {}, base: null, readonly: true,
        onChange: function (codes, level) {
            codes.forEach(function (c) {
                if ((stand.niveaus || {})[c] === level) delete pending[c]; else pending[c] = level;
            });
            renderTree(); renderBar();
        }
    });
    function renderTree() { tree.update({ levels: levels(), changed: Object.keys(pending), readonly: readonly() }); }

    function summarize() {
        // namen van de functies die de gebruiker zelf aanklikte (groepen vervangen hun kinderen)
        var codes = Object.keys(pending), set = {};
        codes.forEach(function (c) { set[c] = true; });
        var tops = codes.filter(function (c) {
            var p = itemByCode[c] && itemByCode[c].ouder;
            while (p) { if (set[p] && pending[p] === pending[c]) return false; p = itemByCode[p] && itemByCode[p].ouder; }
            return true;
        });
        var byLevel = {};
        tops.forEach(function (c) { (byLevel[pending[c]] = byLevel[pending[c]] || []).push(itemByCode[c].naam); });
        return Object.keys(byLevel).sort().map(function (l) { return byLevel[l].join(" en ") + " naar " + window.GlV2RightsTree.levelNames[l]; });
    }
    function renderBar() {
        var bar = $("rol-savebar"), d = dirty();
        bar.hidden = !d || readonly();
        if (bar.hidden) return;
        var leden = (stand.leden || []).length, parts = summarize();
        var count = Object.keys(pending).length;
        var tekst = [];
        if (parts.length) tekst.push(parts.join(" · "));
        if (pendingChips !== null) tekst.push("bedrijfstoegang aangepast");
        if (pendingNaam !== null && pendingNaam !== stand.naam) tekst.push("naam gewijzigd");
        var tel = (parts.length ? parts.length : 0) + (pendingChips !== null ? 1 : 0) + (pendingNaam !== null && pendingNaam !== stand.naam ? 1 : 0);
        $("rol-savetext").innerHTML = "<b>" + Math.max(tel, 1) + (tel === 1 ? " wijziging" : " wijzigingen") + "</b> — " + esc(tekst.join(" · ")) + " · geldt voor " + leden + (leden === 1 ? " lid" : " leden") + (count > 0 ? ". Afwijkingen van gebruikers blijven behouden." : ".");
    }

    $("rol-naam").addEventListener("input", function () { pendingNaam = this.value; renderBar(); });
    $("rol-search").addEventListener("input", function () { tree.setFilter({ query: this.value }); });
    var expanded = false;
    $("rol-expand").addEventListener("click", function () { expanded = !expanded; tree.expandAll(expanded); this.textContent = expanded ? "Alles dichtklappen" : "Alles openklappen"; });

    $("rol-cancel").addEventListener("click", function () { pending = {}; pendingChips = null; pendingNaam = null; renderAll(); });
    $("rol-save").addEventListener("click", function () {
        var btn = this; btn.disabled = true;
        var wijzigingen = Object.keys(pending).map(function (c) { return { code: c, niveau: pending[c] }; });
        post(U.opslaan, {
            roleId: stand.id, naam: pendingNaam !== null ? pendingNaam : stand.naam, wijzigingen: JSON.stringify(wijzigingen),
            bedrijven: pendingChips !== null ? pendingChips : [], bedrijvenGewijzigd: pendingChips !== null
        }).then(function (r) {
            btn.disabled = false;
            if (!r.ok) { fout(r); return; }
            var i = data.rollen.filter(function (x) { return x.id === stand.id; })[0];
            stand = r.stand; pending = {}; pendingChips = null; pendingNaam = null;
            if (i) i.naam = stand.naam;
            renderAll();
            toast("success", "Rol opgeslagen", r.leden + (r.leden === 1 ? " lid krijgt" : " leden krijgen") + " de nieuwe rechten" + (r.afwijkingenBehouden ? "; " + r.afwijkingenBehouden + (r.afwijkingenBehouden === 1 ? " lid behoudt zijn afwijking." : " leden behouden hun afwijking.") : "."));
        }).catch(function (x) { btn.disabled = false; fout(x); });
    });

    window.addEventListener("beforeunload", function (e) { if (dirty() && !readonly()) { e.preventDefault(); e.returnValue = ""; } });

    function renderAll() { renderList(); renderHead(); renderChips(); renderMembers(); renderTree(); renderBar(); }

    // ── Nieuwe rol, dupliceren, verwijderen ──────────────────────────────────────────────────────
    function openRole(id) { window.location.href = root.dataset.rollenUrl + "/" + id; }
    if (canWrite) {
        $("rol-new").addEventListener("submit", function (e) {
            e.preventDefault();
            var naam = $("rol-new-naam").value.trim();
            if (!naam) { toast("warning", "Naam ontbreekt", "Geef de naam van de nieuwe rol in."); $("rol-new-naam").focus(); return; }
            post(U.nieuw, { naam: naam }).then(function (r) { if (r.ok) { try { sessionStorage.setItem("ua-flash", JSON.stringify({ tone: "success", title: "Rol gemaakt", body: naam })); } catch (x) { } openRole(r.id); } else fout(r); }).catch(fout);
        });
        var dupEl = $("rol-dup-modal");
        $("rol-dup").addEventListener("click", function () {
            $("rol-dup-naam").value = "Kopie van " + stand.naam; $("rol-dup-err").hidden = true;
            window.bootstrap.Modal.getOrCreateInstance(dupEl).show();
            dupEl.addEventListener("shown.bs.modal", function once() { dupEl.removeEventListener("shown.bs.modal", once); $("rol-dup-naam").focus(); $("rol-dup-naam").select(); });
        });
        dupEl.querySelector("form").addEventListener("submit", function (e) {
            e.preventDefault();
            post(U.nieuw, { naam: $("rol-dup-naam").value, kopieVan: stand.id }).then(function (r) {
                if (r.ok) { try { sessionStorage.setItem("ua-flash", JSON.stringify({ tone: "success", title: "Rol gedupliceerd", body: "Dezelfde rechten en bedrijfstoegang, nog geen leden." })); } catch (x) { } openRole(r.id); }
                else { $("rol-dup-err").textContent = r.bericht || "Dupliceren is niet gelukt."; $("rol-dup-err").hidden = false; }
            }).catch(function () { $("rol-dup-err").textContent = "De server gaf geen antwoord."; $("rol-dup-err").hidden = false; });
        });
    }
    if (canDelete) $("rol-del").addEventListener("click", function () {
        window.GlV2Bevestig({ title: stand.naam + " verwijderen?", desc: "De rol en haar rechten verdwijnen. Deze rol heeft geen leden, dus niemand verliest rechten.", ok: "Verwijderen", tone: "danger" }).then(function (ok) {
            if (!ok) return;
            post(U.verwijder, { roleId: stand.id }).then(function (r) {
                if (r.ok) { try { sessionStorage.setItem("ua-flash", JSON.stringify({ tone: "success", title: "Rol verwijderd", body: stand.naam })); } catch (x) { } window.location.href = root.dataset.rollenUrl; }
                else fout(r);
            }).catch(fout);
        });
    });

    // ── Leden beheren ────────────────────────────────────────────────────────────────────────────
    if (canWrite) {
        var kand = [], mEl = $("rol-members-modal");
        function renderKand() {
            var q = norm($("rol-members-search").value);
            var rows = kand.filter(function (k) { return !q || norm(k.naam).indexOf(q) >= 0 || norm(k.email).indexOf(q) >= 0; });
            $("rol-members-list").innerHTML = rows.length ? rows.map(function (k) {
                return '<label class="gl-v2-ua-pick-row' + (k.inRol ? " is-current" : "") + '"><input type="checkbox" value="' + k.id + '"' + (k.inRol ? " checked disabled" : "") + ' /><span class="gl-v2-ua-avatar is-sm">' + esc(k.initialen) + '</span><span class="gl-v2-ua-person-text"><span class="gl-v2-ua-person-name">' + esc(k.naam) + '</span><span class="gl-v2-ua-person-sub">' + esc(k.email) + "</span></span>" +
                    (k.inRol ? '<span class="gl-v2-badge is-positive" style="margin-left:auto">Lid</span>' : k.rol ? '<span class="gl-v2-ua-person-sub" style="margin-left:auto">nu: ' + esc(k.rol) + "</span>" : "") + "</label>";
            }).join("") : '<div class="gl-v2-ua-pick-empty">Niemand gevonden.</div>';
            sync();
        }
        function sync() { $("rol-members-submit").disabled = !$("rol-members-list").querySelector("input:checked:not(:disabled)"); }
        $("rol-members-open").addEventListener("click", function () {
            $("rol-members-search").value = ""; $("rol-members-submit").disabled = true; $("rol-members-list").innerHTML = '<div class="gl-v2-ua-pick-empty">Laden…</div>';
            window.bootstrap.Modal.getOrCreateInstance(mEl).show();
            fetch(U.kandidaten + "?roleId=" + stand.id, { headers: { "X-Requested-With": "XMLHttpRequest" } }).then(function (r) { return r.json(); }).then(function (l) { kand = l; renderKand(); })
                .catch(function () { $("rol-members-list").innerHTML = '<div class="gl-v2-ua-pick-empty">Kon de gebruikers niet laden.</div>'; });
        });
        $("rol-members-search").addEventListener("input", renderKand);
        $("rol-members-list").addEventListener("change", sync);
        mEl.querySelector("form").addEventListener("submit", function (e) {
            e.preventDefault();
            var ids = Array.prototype.map.call($("rol-members-list").querySelectorAll("input:checked:not(:disabled)"), function (i) { return parseInt(i.value, 10); });
            if (!ids.length) return;
            $("rol-members-submit").disabled = true;
            post(U.leden, { roleId: stand.id, gebruikers: ids }).then(function (r) {
                if (!r.ok) { fout(r); $("rol-members-submit").disabled = false; return; }
                // De aantallen van de andere rollen veranderen ook (wie van rol wisselt, verlaat zijn oude rol): pagina vernieuwen.
                try { sessionStorage.setItem("ua-flash", JSON.stringify({ tone: "success", title: "Leden toegevoegd", body: ids.length + (ids.length === 1 ? " gebruiker heeft" : " gebruikers hebben") + " nu de rol " + stand.naam + "." })); } catch (x) { }
                pending = {}; pendingChips = null; pendingNaam = null;
                window.location.href = root.dataset.rollenUrl + "/" + stand.id;
            }).catch(function (x) { fout(x); $("rol-members-submit").disabled = false; });
        });
    }

    renderAll();
})();
