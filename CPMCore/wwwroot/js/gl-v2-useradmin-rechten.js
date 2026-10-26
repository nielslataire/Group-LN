// gl-v2 — Instellingen › Gebruikers en rollen › Gebruiker · rechten (design-handoff punt 49b). Account links (rol, bedrijfstoegang, toegang, Entra),
// effectieve rechten rechts met de gedeelde rechtenboom (js/gl-v2-rechtenboom.js). Elke wijziging gaat meteen naar de server (UserAdminController.V2Acties.cs),
// die de nieuwe "stand" teruggeeft: niveaus (effectief), basis (uit de rol), afwijkende functies, bedrijfstoegang.
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-ua-r");
    if (!root) return;
    var data = JSON.parse(document.getElementById("ua-r-data").textContent);
    var token = (root.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";
    var U = {
        rol: root.dataset.urlRol, niveau: root.dataset.urlNiveau, reset: root.dataset.urlReset, bedrijf: root.dataset.urlBedrijf,
        toegang: root.dataset.urlToegang, uitnodiging: root.dataset.urlUitnodiging, kopieren: root.dataset.urlKopieren, kandidaten: root.dataset.urlKandidaten
    };
    var g = data.gebruiker, canWrite = !!data.kanSchrijven;
    var stand = data.stand;

    function $(id) { return document.getElementById(id); }
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function norm(s) { return String(s || "").toLowerCase(); }
    function toast(tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" }); }
    function post(url, d) {
        var fd = new FormData();
        Object.keys(d).forEach(function (k) { [].concat(d[k]).forEach(function (v) { if (v !== null && v !== undefined) fd.append(k, v); }); });
        return fetch(url, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); });
    }
    function reloadWith(tone, title, body) {
        try { sessionStorage.setItem("ua-flash", JSON.stringify({ tone: tone, title: title, body: body || "" })); } catch (e) { }
        window.location.reload();
    }
    try { var f = sessionStorage.getItem("ua-flash"); if (f) { sessionStorage.removeItem("ua-flash"); var fo = JSON.parse(f); toast(fo.tone, fo.title, fo.body); } } catch (e) { }
    function fout(r) { toast("danger", "Niet gelukt", (r && r.bericht) || "De server gaf geen antwoord."); }

    var TYPE_LABEL = { intern: "Intern", aannemer: "Aannemer", gast: "Gast" };
    var TYPE_BADGE = { intern: "is-neutral", aannemer: "is-attention", gast: "is-info" };
    var STATUS_LABEL = { actief: "Actief", uitgenodigd: "Uitgenodigd", uitgeschakeld: "Uitgeschakeld" };
    var STATUS_BADGE = { actief: "is-positive", uitgenodigd: "is-attention", uitgeschakeld: "is-inactive" };
    var voornaam = (g.naam || "").split(" ")[0] || "De gebruiker";
    var itemByCode = {};
    data.boom.forEach(function (i) { itemByCode[i.code] = i; });
    var readonly = !canWrite || stand.admin;

    // ── Account ───────────────────────────────────────────────────────────────────────────────────
    function fmtLogin(iso) {
        if (!iso) return "—";
        var d = new Date(iso); if (isNaN(d)) return "—";
        var t = new Date(), days = Math.round((new Date(t.getFullYear(), t.getMonth(), t.getDate()) - new Date(d.getFullYear(), d.getMonth(), d.getDate())) / 86400000);
        var hm = String(d.getHours()).padStart(2, "0") + ":" + String(d.getMinutes()).padStart(2, "0");
        return days === 0 ? "vandaag " + hm : days === 1 ? "gisteren " + hm : String(d.getDate()).padStart(2, "0") + "/" + String(d.getMonth() + 1).padStart(2, "0") + "/" + d.getFullYear();
    }
    // De rolkeuze is de projectbrede keuzelijst (GlV2/_SelectList, window.GlV2Select): de opties komen uit de pagina-data.
    function setRol(v) {
        window.GlV2Select.setItems($("ur-rol_select"), data.rollen.map(function (r) { return { value: r.id, text: r.naam }; }), v == null ? "" : v, "Geen rol");
    }
    function renderAccount() {
        $("ur-avatar").textContent = g.initialen; $("ur-avatar").className = "gl-v2-ua-avatar is-lg is-" + g.type;
        $("ur-naam").textContent = g.naam; $("ur-mail").textContent = g.email;
        $("ur-badges").innerHTML = '<span class="gl-v2-badge ' + TYPE_BADGE[g.type] + '">' + TYPE_LABEL[g.type] + '</span><span class="gl-v2-badge ' + STATUS_BADGE[g.status] + '" id="ur-status-badge">' + STATUS_LABEL[g.status] + "</span>";
        setRol(stand.roleId);
        var rolTrigger = $("ur-rol_trigger"); var rolLocked = !canWrite || g.ikzelf; rolTrigger.setAttribute("aria-disabled", rolLocked ? "true" : "false"); rolTrigger.tabIndex = rolLocked ? -1 : 0;
        if (g.ikzelf) $("ur-rol-help").textContent = "Je kunt je eigen rol niet wijzigen.";
        $("ur-actief").checked = stand.actief; $("ur-actief").disabled = !canWrite || g.ikzelf;
        $("ur-actief-hint").textContent = g.ikzelf ? "je kunt je eigen toegang niet uitschakelen" : stand.actief ? "kan inloggen" : "toegang ingetrokken, de historiek blijft bewaard";
        $("ur-entra").innerHTML = g.entraId ? '<i class="ph ph-link" aria-hidden="true"></i> Gekoppeld <span class="mono">' + esc(String(g.entraId).slice(0, 8)) + "…</span>" : "Niet gekoppeld";
        $("ur-login").textContent = fmtLogin(g.laatsteLogin);
        $("ur-invite").hidden = !canWrite || !(g.type === "gast" || g.uitnodiging);
        $("ur-copy").hidden = !canWrite || g.ikzelf;
        renderChips();
    }
    function renderChips() {
        var on = stand.bedrijven, rol = stand.bedrijvenRol;
        $("ur-chips").innerHTML = data.bedrijven.map(function (b) {
            var isOn = on.indexOf(b.id) >= 0, extra = isOn && rol.indexOf(b.id) < 0;
            return '<button type="button" class="gl-v2-ua-chip' + (isOn ? " is-on" : "") + (extra ? " is-extra" : "") + '" data-bedrijf="' + b.id + '" aria-pressed="' + (isOn ? "true" : "false") + '"' + (readonly ? " disabled" : "") + ">" + (isOn ? '<i class="ph ph-check" aria-hidden="true"></i>' : "") + esc(b.naam) + "</button>";
        }).join("") || '<span class="gl-v2-ua-help">Er zijn nog geen facturatiebedrijven.</span>';
        var names = function (ids) { return data.bedrijven.filter(function (b) { return ids.indexOf(b.id) >= 0; }).map(function (b) { return b.naam; }); };
        var added = names(on.filter(function (i) { return rol.indexOf(i) < 0; })), removed = names(rol.filter(function (i) { return on.indexOf(i) < 0; }));
        var parts = [];
        if (added.length) parts.push(added.join(", ") + " toegevoegd bovenop de rol");
        if (removed.length) parts.push(removed.join(", ") + " weggehaald ten opzichte van de rol");
        var help = $("ur-chips-help");
        help.className = "gl-v2-ua-help" + (parts.length ? " is-gold" : "");
        help.textContent = parts.length ? parts.join("; ") + " (rol: " + (names(rol).join(", ") || "geen bedrijven") + ")." : "Geldt voor de functies die als “per bedrijf” gemarkeerd zijn, zoals Klanten, Leveranciers en Facturatie. Een bedrijf telt pas mee als de gebruiker daar minstens Lezen heeft.";
    }
    $("ur-chips").addEventListener("click", function (e) {
        var b = e.target.closest("[data-bedrijf]"); if (!b || readonly) return;
        var id = parseInt(b.dataset.bedrijf, 10), aan = stand.bedrijven.indexOf(id) < 0;
        post(U.bedrijf, { userId: g.id, issuerId: id, toegang: aan }).then(function (r) { if (r.ok) { stand = r.stand; renderAll(); } else fout(r); }).catch(fout);
    });
    $("ur-rol").addEventListener("change", function () {
        var roleId = this.value, sel = this;
        if (!roleId) { setRol(stand.roleId); return; }
        var doen = function () { post(U.rol, { userId: g.id, roleId: roleId }).then(function (r) { if (r.ok) reloadWith("success", "Rol gewijzigd", "De afwijkingen zijn teruggezet; de gebruiker volgt de nieuwe rol."); else { fout(r); setRol(stand.roleId); } }).catch(function (x) { fout(x); setRol(stand.roleId); }); };
        if (tree.deviations().length || stand.bedrijvenAfwijkend) {
            window.GlV2Bevestig({ title: "Rol wijzigen?", desc: voornaam + " heeft afwijkingen op de huidige rol. Een andere rol zet die allemaal terug.", ok: "Rol wijzigen", tone: "warning" }).then(function (ok) { if (ok) doen(); else setRol(stand.roleId); });
        } else doen();
    });
    $("ur-actief").addEventListener("change", function () {
        var aan = this.checked, el = this;
        post(U.toegang, { userId: g.id, actief: aan }).then(function (r) {
            if (r.ok) { stand = r.stand; g.status = aan ? (g.status === "uitgeschakeld" ? "actief" : g.status) : "uitgeschakeld"; renderAccount(); toast("success", aan ? "Toegang ingeschakeld" : "Toegang uitgeschakeld", ""); }
            else { fout(r); el.checked = !aan; }
        }).catch(function (x) { fout(x); el.checked = !aan; });
    });
    $("ur-invite").addEventListener("click", function () {
        post(U.uitnodiging, { userId: g.id, actie: g.uitnodiging ? "opnieuw" : "uitnodigen" }).then(function (r) { if (r.ok) toast("success", "Uitnodiging verstuurd", "De gebruiker krijgt een e-mail."); else fout(r); }).catch(fout);
    });

    // ── Rechten ───────────────────────────────────────────────────────────────────────────────────
    var tree = window.GlV2RightsTree.create($("ur-tree"), {
        items: data.boom, levels: stand.niveaus, base: stand.basis, baseLabel: stand.rolNaam || "", readonly: readonly,
        onChange: function (codes, level) {
            var before = JSON.parse(JSON.stringify(stand.niveaus));
            codes.forEach(function (c) { stand.niveaus[c] = level; });          // meteen tonen
            renderTree();
            post(U.niveau, { userId: g.id, codes: codes, niveau: level }).then(function (r) {
                if (r.ok) { stand = r.stand; renderAll(); } else { stand.niveaus = before; renderTree(); fout(r); }
            }).catch(function (x) { stand.niveaus = before; renderTree(); fout(x); });
        },
        onReset: function (codes) {
            post(U.reset, { userId: g.id, codes: codes }).then(function (r) { if (r.ok) { stand = r.stand; renderAll(); } else fout(r); }).catch(fout);
        }
    });

    function levelPhrase(code, lv) {
        var n = itemByCode[code] ? itemByCode[code].naam : code;
        return lv === 0 ? "geen " + n : n + " " + window.GlV2RightsTree.levelText[lv];
    }
    function renderSummary() {
        var devs = tree.deviations();
        $("ur-dev-count").textContent = devs.length;
        $("ur-reset-all").hidden = readonly || (!devs.length && !stand.bedrijvenAfwijkend);
        var rol = stand.rolNaam ? "<b>" + esc(stand.rolNaam) + "</b>" : "<b>geen rol</b>";
        if (stand.admin) { $("ur-summary").innerHTML = esc(voornaam) + " heeft de rol " + rol + ": die heeft altijd alle rechten. Afwijkingen zijn hier niet nodig."; return; }
        if (!devs.length) { $("ur-summary").innerHTML = esc(voornaam) + " volgt de rol " + rol + (stand.bedrijvenAfwijkend ? " en heeft een afwijkende bedrijfstoegang" : " zonder afwijkingen") + ". Wijzigingen aan de rol werken voor alle rechten door."; return; }
        var shown = devs.slice(0, 3).map(function (d) { return esc(levelPhrase(d.code, stand.niveaus[d.code])); });
        $("ur-summary").innerHTML = esc(voornaam) + " volgt de rol " + rol + " met <b>" + devs.length + (devs.length === 1 ? " afwijking" : " afwijkingen") + "</b>: " + shown.join(", ") + (devs.length > 3 ? " en " + (devs.length - 3) + " andere" : "") + ". Wijzigingen aan de rol blijven voor alle andere rechten doorwerken.";
    }
    function renderTree() { tree.update({ levels: stand.niveaus, base: stand.basis, baseLabel: stand.rolNaam || "", readonly: readonly }); }
    function renderAll() { renderAccount(); renderTree(); renderSummary(); }

    $("ur-reset-all").addEventListener("click", function () {
        window.GlV2Bevestig({ title: "Alles terug naar de rol?", desc: voornaam + " volgt daarna op elke functie en bij de bedrijfstoegang de rol " + (stand.rolNaam || "") + ".", ok: "Terugzetten", tone: "warning" }).then(function (ok) {
            if (!ok) return;
            post(U.reset, { userId: g.id, codes: [] }).then(function (r) { if (r.ok) { stand = r.stand; renderAll(); toast("success", "Terug naar de rol", ""); } else fout(r); }).catch(fout);
        });
    });
    $("ur-filter").addEventListener("click", function (e) {
        var b = e.target.closest("[data-f]"); if (!b) return;
        $("ur-filter").querySelectorAll("button").forEach(function (x) { x.classList.toggle("is-active", x === b); });
        tree.setFilter({ onlyDeviations: b.dataset.f === "dev" });
    });
    $("ur-search").addEventListener("input", function () { tree.setFilter({ query: this.value }); });
    var expanded = false;
    $("ur-expand").addEventListener("click", function () { expanded = !expanded; tree.expandAll(expanded); this.textContent = expanded ? "Alles dichtklappen" : "Alles openklappen"; });

    // ── Rechten kopiëren van… ─────────────────────────────────────────────────────────────────────
    if (canWrite && !g.ikzelf) {
        var kandidaten = [], picked = null, modalEl = $("ur-copy-modal");
        function renderCopy() {
            var q = norm($("ur-copy-search").value);
            var rows = kandidaten.filter(function (k) { return !q || norm(k.naam).indexOf(q) >= 0 || norm(k.email).indexOf(q) >= 0; });
            $("ur-copy-list").innerHTML = rows.length ? rows.map(function (k) {
                return '<label class="gl-v2-ua-pick-row"><input type="radio" name="van" value="' + k.id + '"' + (picked === k.id ? " checked" : "") + ' /><span class="gl-v2-ua-avatar is-sm">' + esc(k.initialen) + '</span><span class="gl-v2-ua-person-text"><span class="gl-v2-ua-person-name">' + esc(k.naam) + '</span><span class="gl-v2-ua-person-sub">' + esc(k.email) + "</span></span></label>";
            }).join("") : '<div class="gl-v2-ua-pick-empty">Niemand gevonden.</div>';
        }
        $("ur-copy").addEventListener("click", function () {
            picked = null; $("ur-copy-submit").disabled = true; $("ur-copy-search").value = "";
            window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
            fetch(U.kandidaten + "?exclude=" + g.id, { headers: { "X-Requested-With": "XMLHttpRequest" } }).then(function (r) { return r.json(); }).then(function (list) { kandidaten = list; renderCopy(); })
                .catch(function () { $("ur-copy-list").innerHTML = '<div class="gl-v2-ua-pick-empty">Kon de gebruikers niet laden.</div>'; });
        });
        $("ur-copy-search").addEventListener("input", renderCopy);
        $("ur-copy-list").addEventListener("change", function (e) { if (e.target.name === "van") { picked = parseInt(e.target.value, 10); $("ur-copy-submit").disabled = false; } });
        modalEl.querySelector("form").addEventListener("submit", function (e) {
            e.preventDefault(); if (!picked) return;
            $("ur-copy-submit").disabled = true;
            post(U.kopieren, { userId: g.id, vanGebruikerId: picked }).then(function (r) { if (r.ok) reloadWith("success", "Rechten gekopieerd", "Rol en afwijkingen zijn overgenomen."); else { fout(r); $("ur-copy-submit").disabled = false; } }).catch(function (x) { fout(x); $("ur-copy-submit").disabled = false; });
        });
    }

    renderAll();
    tree.expandDeviations();
})();
