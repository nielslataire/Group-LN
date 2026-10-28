// gl-v2 — Instellingen › Gebruikers en rollen › Gebruikers (design-handoff punt 49a). Tekent de gebruikerslijst uit één JSON-blok, met filters
// (zoeken, type, rol, status, enkel afwijkingen), een ⋯-menu per rij en de modals Gebruiker uitnodigen, Importeren uit Entra en Koppelingen nakijken.
// Schrijfacties gaan via fetch naar UserAdminController (…V2-acties); na een wijziging herlaadt de pagina met een toast (sessionStorage).
(function () {
    "use strict";

    var root = document.getElementById("gl-v2-ua");
    if (!root) return;
    var data = JSON.parse(document.getElementById("ua-data").textContent);
    var canWrite = root.getAttribute("data-can-write") === "1";
    var canDelete = root.getAttribute("data-can-delete") === "1";
    var token = (root.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";
    var U = {
        toegang: root.dataset.urlToegang, uitnodiging: root.dataset.urlUitnodiging, uitnodigen: root.dataset.urlUitnodigen, del: root.dataset.urlDelete,
        kandidaten: root.dataset.urlEntraKandidaten, import: root.dataset.urlEntraImport, koppelingen: root.dataset.urlKoppelingen, koppelen: root.dataset.urlKoppelen
    };
    var PAGE = 25;
    var ui = { q: "", type: "", rol: "", status: "", dev: false, shown: PAGE };

    var $ = window.GlV2.byId; // gedeeld (gl-v2-shell.js)
    function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
    function norm(s) { return String(s || "").toLowerCase(); }
    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {}; // gedeeld, zie gl-v2-shell.js
    function post(url, d) {
        var fd = new FormData();
        Object.keys(d).forEach(function (k) { [].concat(d[k]).forEach(function (v) { if (v !== null && v !== undefined) fd.append(k, v); }); });
        return fetch(url, { method: "POST", headers: { "RequestVerificationToken": token }, body: fd }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); });
    }
    function get(url) { return fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); }); }
    function reloadWith(tone, title, body) {
        try { sessionStorage.setItem("ua-flash", JSON.stringify({ tone: tone, title: title, body: body || "" })); } catch (e) { }
        window.location.reload();
    }
    try {
        var f = sessionStorage.getItem("ua-flash");
        if (f) { sessionStorage.removeItem("ua-flash"); var o = JSON.parse(f); toast(o.tone, o.title, o.body); }
    } catch (e) { }

    var TYPE_LABEL = { intern: "Intern", aannemer: "Aannemer", gast: "Gast" };
    var TYPE_BADGE = { intern: "is-neutral", aannemer: "is-attention", gast: "is-info" };
    var STATUS_LABEL = { actief: "Actief", uitgenodigd: "Uitgenodigd", uitgeschakeld: "Uitgeschakeld" };
    var STATUS_BADGE = { actief: "is-positive", uitgenodigd: "is-attention", uitgeschakeld: "is-inactive" };
    var TYPE_ORDER = { intern: 0, gast: 1, aannemer: 2 };

    function fmtLogin(iso) {
        if (!iso) return "—";
        var d = new Date(iso); if (isNaN(d)) return "—";
        var now = new Date(), t0 = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        var days = Math.round((t0 - new Date(d.getFullYear(), d.getMonth(), d.getDate())) / 86400000);
        if (days === 0) return "vandaag " + String(d.getHours()).padStart(2, "0") + ":" + String(d.getMinutes()).padStart(2, "0");
        if (days === 1) return "gisteren";
        return String(d.getDate()).padStart(2, "0") + "/" + String(d.getMonth() + 1).padStart(2, "0") + "/" + String(d.getFullYear()).slice(2);
    }

    // ── Filters ───────────────────────────────────────────────────────────────────────────────────
    function counts() {
        var c = { "": data.gebruikers.length, intern: 0, aannemer: 0, gast: 0 };
        data.gebruikers.forEach(function (u) { c[u.type]++; });
        return c;
    }
    function renderTypes() {
        var c = counts();
        var items = [["", "Alle"], ["intern", "Intern"], ["aannemer", "Aannemers"], ["gast", "Gasten"]];
        $("ua-types").innerHTML = items.map(function (it) {
            return '<button type="button" class="' + (ui.type === it[0] ? "is-active" : "") + '" data-type="' + it[0] + '">' + it[1] + ' <span class="gl-v2-ua-typecount">' + c[it[0]] + "</span></button>";
        }).join("");
    }
    function renderRoleSelect() {
        window.GlV2Select.setItems($("ua-role_select"), data.rollen.map(function (r) { return { value: r.id, text: r.naam + " · " + r.leden }; }), ui.rol, "Alle rollen");
    }
    function filtered() {
        var q = norm(ui.q);
        return data.gebruikers.filter(function (u) {
            if (ui.type && u.type !== ui.type) return false;
            if (ui.rol && String(u.roleId) !== String(ui.rol)) return false;
            if (ui.status && u.status !== ui.status) return false;
            if (ui.dev && !u.afwijkingen) return false;
            if (!q) return true;
            return norm(u.naam).indexOf(q) >= 0 || norm(u.email).indexOf(q) >= 0 || norm(u.bedrijven).indexOf(q) >= 0 || norm(u.rol).indexOf(q) >= 0;
        }).sort(function (a, b) { return (TYPE_ORDER[a.type] - TYPE_ORDER[b.type]) || a.naam.localeCompare(b.naam, "nl"); });
    }

    // ── Rijen ─────────────────────────────────────────────────────────────────────────────────────
    function menuHtml(u) {
        var id = "ua-menu-" + u.id, items = [];
        items.push('<a class="gl-v2-menu-item" href="' + root.dataset.rechtenUrl + "/" + u.id + '"><i class="ph ph-shield-check" aria-hidden="true"></i><span class="gl-v2-menu-item-text"><span class="gl-v2-menu-item-label">Rechten bekijken</span></span></a>');
        if (canWrite) {
            if (u.type === "gast" || u.uitnodiging) {
                items.push('<button type="button" class="gl-v2-menu-item" data-ua-uitnodiging="' + (u.uitnodiging ? "opnieuw" : "uitnodigen") + '" data-id="' + u.id + '"><i class="ph ph-paper-plane-tilt" aria-hidden="true"></i><span class="gl-v2-menu-item-text"><span class="gl-v2-menu-item-label">' + (u.uitnodiging ? "Uitnodiging opnieuw sturen" : "Uitnodiging sturen") + "</span></span></button>");
            }
            if (!u.ikzelf) {
                items.push('<button type="button" class="gl-v2-menu-item" data-ua-actief="' + (u.status === "uitgeschakeld" ? "1" : "0") + '" data-id="' + u.id + '"><i class="ph ' + (u.status === "uitgeschakeld" ? "ph-check-circle" : "ph-prohibit") + '" aria-hidden="true"></i><span class="gl-v2-menu-item-text"><span class="gl-v2-menu-item-label">' + (u.status === "uitgeschakeld" ? "Opnieuw inschakelen" : "Uitschakelen") + "</span></span></button>");
            }
        }
        if (canDelete && !u.ikzelf) items.push('<button type="button" class="gl-v2-menu-item is-danger" data-ua-delete data-id="' + u.id + '" data-naam="' + esc(u.naam) + '"><i class="ph ph-trash" aria-hidden="true"></i><span class="gl-v2-menu-item-text"><span class="gl-v2-menu-item-label">Verwijderen</span></span></button>');
        return '<button type="button" class="gl-v2-icon-btn js-gl-v2-menu-trigger" aria-haspopup="true" aria-expanded="false" aria-controls="' + id + '" aria-label="Meer acties voor ' + esc(u.naam) + '"><i class="ph ph-dots-three" aria-hidden="true"></i></button><div class="gl-v2-menu" id="' + id + '">' + items.join("") + "</div>";
    }
    function rowHtml(u) {
        return '<div class="gl-v2-ua-row gl-v2-ua-grid--u' + (u.status === "uitgeschakeld" ? " is-off" : "") + '">' +
            '<div class="gl-v2-ua-person"><span class="gl-v2-ua-avatar is-' + u.type + '">' + esc(u.initialen) + '</span><span class="gl-v2-ua-person-text"><span class="gl-v2-ua-person-name"><a href="' + root.dataset.rechtenUrl + "/" + u.id + '">' + esc(u.naam) + "</a>" + (u.ikzelf ? '<span class="gl-v2-ua-me">JIJ</span>' : "") + '</span><span class="gl-v2-ua-person-sub" title="' + esc(u.email) + '">' + esc(u.email) + "</span></span></div>" +
            '<span><span class="gl-v2-badge ' + TYPE_BADGE[u.type] + '">' + TYPE_LABEL[u.type] + "</span></span>" +
            '<span class="gl-v2-ua-cell">' + (u.rol ? esc(u.rol) : '<span class="gl-v2-ua-cell is-muted">Geen rol</span>') + "</span>" +
            '<span class="gl-v2-ua-cell ' + (u.afwijkingen ? "is-gold" : "is-muted") + '">' + (u.afwijkingen ? u.afwijkingen + (u.afwijkingen === 1 ? " afwijking" : " afwijkingen") : "volgt rol") + "</span>" +
            '<span class="gl-v2-ua-cell" title="' + esc(u.bedrijven) + '">' + esc(u.bedrijven) + "</span>" +
            '<span class="gl-v2-ua-cell ' + (u.entra ? "" : "is-muted") + '">' + (u.entra ? '<i class="ph ph-link" aria-hidden="true"></i> Gekoppeld' : "—") + "</span>" +
            '<span><span class="gl-v2-badge ' + STATUS_BADGE[u.status] + '">' + STATUS_LABEL[u.status] + "</span></span>" +
            '<span class="gl-v2-ua-cell is-muted">' + fmtLogin(u.laatsteLogin) + "</span>" +
            '<div class="gl-v2-ua-actions">' + menuHtml(u) + "</div></div>";
    }

    function render() {
        var list = filtered(), total = data.gebruikers.length;
        var slice = list.slice(0, ui.shown);
        $("ua-rows").innerHTML = slice.map(rowHtml).join("");
        $("ua-empty").hidden = list.length > 0;
        $("ua-count-users").textContent = total;
        if ($("ua-count-roles")) $("ua-count-roles").textContent = data.rollen.length;
        $("ua-foot").textContent = list.length ? slice.length + " van " + total + " getoond" : "";
        var more = $("ua-more");
        if (list.length > slice.length) {
            var hidden = list.length - slice.length;
            var contractors = list.slice(slice.length).filter(function (u) { return u.type === "aannemer"; }).length;
            more.hidden = false;
            more.textContent = contractors === hidden && contractors > 0 ? "Toon alle " + contractors + " aannemers" : "Toon alle " + hidden + " resterende gebruikers";
        } else more.hidden = true;
        renderTypes();
    }

    function setFilter(k, v) { ui[k] = v; ui.shown = PAGE; render(); }
    $("ua-types").addEventListener("click", function (e) { var b = e.target.closest("[data-type]"); if (b) setFilter("type", b.getAttribute("data-type")); });
    $("ua-role").addEventListener("change", function () { setFilter("rol", this.value); });
    $("ua-status").addEventListener("change", function () { setFilter("status", this.value); });
    $("ua-dev").addEventListener("change", function () { setFilter("dev", this.checked); });
    $("ua-search").addEventListener("input", function () { setFilter("q", this.value); });
    $("ua-search-clear").addEventListener("click", function () { $("ua-search").value = ""; setFilter("q", ""); $("ua-search").focus(); });
    $("ua-more").addEventListener("click", function () { ui.shown = 100000; render(); });
    $("ua-reset").addEventListener("click", function () {
        ui = { q: "", type: "", rol: "", status: "", dev: false, shown: PAGE };
        $("ua-search").value = ""; window.GlV2Select.setItems($("ua-status_select"), [{ value: "actief", text: "Actief" }, { value: "uitgenodigd", text: "Uitgenodigd" }, { value: "uitgeschakeld", text: "Uitgeschakeld" }], "", "Alle statussen"); $("ua-dev").checked = false; renderRoleSelect(); render();
    });

    // ── Rijacties ─────────────────────────────────────────────────────────────────────────────────
    function closeMenus() { if (window.GlV2Menu) window.GlV2Menu.closeAll(); }
    root.addEventListener("click", function (e) {
        var inv = e.target.closest("[data-ua-uitnodiging]");
        if (inv) {
            closeMenus();
            post(U.uitnodiging, { userId: inv.dataset.id, actie: inv.dataset.uaUitnodiging }).then(function (r) {
                if (r.ok) reloadWith("success", "Uitnodiging verstuurd", "De gebruiker krijgt een e-mail met de uitnodiging.");
                else toast("danger", "Uitnodigen mislukt", r.bericht);
            }).catch(function () { toast("danger", "Uitnodigen mislukt", "De server gaf geen antwoord."); });
            return;
        }
        var act = e.target.closest("[data-ua-actief]");
        if (act) {
            closeMenus();
            var aan = act.dataset.uaActief === "1";
            post(U.toegang, { userId: act.dataset.id, actief: aan }).then(function (r) {
                if (r.ok) reloadWith("success", aan ? "Gebruiker ingeschakeld" : "Gebruiker uitgeschakeld", aan ? "" : "De toegang is ingetrokken. De historiek blijft bewaard.");
                else toast("danger", "Niet gelukt", r.bericht);
            }).catch(function () { toast("danger", "Niet gelukt", "De server gaf geen antwoord."); });
            return;
        }
        var del = e.target.closest("[data-ua-delete]");
        if (del) {
            closeMenus();
            var id = del.dataset.id, naam = del.dataset.naam;
            window.GlV2Bevestig({ title: naam + " verwijderen?", desc: "De gebruiker en zijn rechten worden verwijderd. Wil je de toegang enkel intrekken, kies dan Uitschakelen.", ok: "Verwijderen", tone: "danger" }).then(function (ok) {
                if (!ok) return;
                var f = document.createElement("form"); f.method = "post"; f.action = U.del;
                [["id", id], ["__RequestVerificationToken", token]].forEach(function (p) { var i = document.createElement("input"); i.type = "hidden"; i.name = p[0]; i.value = p[1]; f.appendChild(i); });
                document.body.appendChild(f); f.submit();
            });
        }
    });

    if (!canWrite) { renderRoleSelect(); render(); return; }

    // ── Modals ────────────────────────────────────────────────────────────────────────────────────
    function modal(id) { return window.bootstrap.Modal.getOrCreateInstance($(id)); }
    // Rolkeuzes zijn de projectbrede keuzelijst (GlV2/_SelectList): de opties komen uit de pagina-data.
    function setRoleSelect(wrapId) { window.GlV2Select.setItems($(wrapId), data.rollen.map(function (r) { return { value: r.id, text: r.naam }; }), "", "Geen rol"); }

    // Gebruiker uitnodigen
    var invForm = $("ua-invite-modal").querySelector("form"), invType = "intern";
    $("ua-invite-open").addEventListener("click", function () {
        invForm.reset(); $("ua-inv-err").hidden = true; invType = "intern"; setInvType("intern");
        setRoleSelect("ua-inv-rol_select");
        modal("ua-invite-modal").show();
        $("ua-invite-modal").addEventListener("shown.bs.modal", function once() { $("ua-invite-modal").removeEventListener("shown.bs.modal", once); $("ua-inv-voornaam").focus(); });
    });
    function setInvType(t) {
        invType = t;
        $("ua-inv-type").querySelectorAll("button").forEach(function (b) { b.classList.toggle("is-active", b.dataset.type === t); });
        $("ua-inv-type-help").textContent = t === "gast" ? "Een externe persoon. Hij of zij krijgt per e-mail een uitnodiging via Entra." : "Een medewerker van Group LN. De gebruiker wordt aangemaakt; er gaat geen uitnodiging uit.";
        $("ua-inv-submit").textContent = t === "gast" ? "Uitnodigen" : "Toevoegen";
    }
    $("ua-inv-type").addEventListener("click", function (e) { var b = e.target.closest("button"); if (b) setInvType(b.dataset.type); });
    invForm.addEventListener("submit", function (e) {
        e.preventDefault();
        var email = $("ua-inv-email").value.trim();
        if (!email || email.indexOf("@") < 1) { $("ua-inv-err").textContent = "Geef een geldig e-mailadres in."; $("ua-inv-err").hidden = false; $("ua-inv-email").focus(); return; }
        $("ua-inv-submit").disabled = true;
        post(U.uitnodigen, { voornaam: $("ua-inv-voornaam").value, naam: $("ua-inv-naam").value, email: email, type: invType, roleId: $("ua-inv-rol").value }).then(function (r) {
            if (r.ok) { reloadWith(r.bericht ? "warning" : "success", r.bericht ? "Gebruiker toegevoegd" : (invType === "gast" ? "Uitnodiging verstuurd" : "Gebruiker toegevoegd"), r.bericht || ""); }
            else { $("ua-inv-err").textContent = r.bericht || "Dat is niet gelukt."; $("ua-inv-err").hidden = false; $("ua-inv-submit").disabled = false; }
        }).catch(function () { $("ua-inv-err").textContent = "De server gaf geen antwoord."; $("ua-inv-err").hidden = false; $("ua-inv-submit").disabled = false; });
    });

    // Importeren uit Entra
    var impList = [], impForm = $("ua-import-modal").querySelector("form");
    function renderImport() {
        var q = norm($("ua-import-search").value);
        var rows = impList.filter(function (k) { return !q || norm(k.naam).indexOf(q) >= 0 || norm(k.email).indexOf(q) >= 0; });
        $("ua-import-list").innerHTML = rows.length ? rows.map(function (k) {
            return '<label class="gl-v2-ua-pick-row"><input type="checkbox" value="' + esc(k.id) + '" /><span class="gl-v2-ua-person-text"><span class="gl-v2-ua-person-name">' + esc(k.naam) + '</span><span class="gl-v2-ua-person-sub">' + esc(k.email) + "</span></span>" + (k.gast ? '<span class="gl-v2-badge is-info" style="margin-left:auto">Gast</span>' : "") + "</label>";
        }).join("") : '<div class="gl-v2-ua-pick-empty">' + (impList.length ? "Niemand gevonden voor deze zoekterm." : "Alle Entra-accounts hebben al een gebruiker.") + "</div>";
        syncImport();
    }
    function syncImport() { $("ua-import-submit").disabled = !$("ua-import-list").querySelector("input:checked"); }
    $("ua-import-open").addEventListener("click", function () {
        setRoleSelect("ua-import-rol_select"); $("ua-import-search").value = "";
        $("ua-import-list").innerHTML = '<div class="gl-v2-ua-pick-empty">Entra laden…</div>'; $("ua-import-submit").disabled = true;
        modal("ua-import-modal").show();
        get(U.kandidaten).then(function (r) {
            if (!r.ok) { $("ua-import-list").innerHTML = '<div class="gl-v2-ua-pick-empty">' + esc(r.bericht) + "</div>"; return; }
            impList = r.kandidaten || []; renderImport();
            if (!r.beschikbaar) $("ua-import-list").innerHTML = '<div class="gl-v2-ua-pick-empty">Entra is niet gekoppeld in deze omgeving.</div>';
        }).catch(function () { $("ua-import-list").innerHTML = '<div class="gl-v2-ua-pick-empty">Entra is niet bereikbaar. Probeer het later opnieuw.</div>'; });
    });
    $("ua-import-search").addEventListener("input", renderImport);
    $("ua-import-list").addEventListener("change", syncImport);
    impForm.addEventListener("submit", function (e) {
        e.preventDefault();
        var ids = Array.prototype.map.call($("ua-import-list").querySelectorAll("input:checked"), function (i) { return i.value; });
        if (!ids.length) return;
        $("ua-import-submit").disabled = true;
        post(U.import, { ids: ids, roleId: $("ua-import-rol").value }).then(function (r) {
            if (r.ok) reloadWith("success", r.aantal + (r.aantal === 1 ? " gebruiker geïmporteerd" : " gebruikers geïmporteerd"), "Ze zijn gekoppeld aan hun Entra-account.");
            else { toast("danger", "Importeren mislukt", r.bericht); $("ua-import-submit").disabled = false; }
        }).catch(function () { toast("danger", "Importeren mislukt", "De server gaf geen antwoord."); $("ua-import-submit").disabled = false; });
    });

    // Koppelingen nakijken
    var linkData = null;
    function renderLinks() {
        var rows = linkData.voorstellen;
        $("ua-link-list").innerHTML = rows.length ? rows.map(function (v) {
            return '<div class="gl-v2-ua-match"><span class="gl-v2-ua-person-text"><span class="gl-v2-ua-person-name">' + esc(v.entraNaam) + '</span><span class="gl-v2-ua-person-sub">' + esc(v.entraEmail) + ' · Entra-gast</span></span><i class="ph ph-arrow-right" aria-hidden="true"></i>' +
                (v.gebruikerId ? '<span class="gl-v2-ua-person-text"><span class="gl-v2-ua-person-name">' + esc(v.gebruikerNaam) + '</span><span class="gl-v2-ua-person-sub">zelfde e-mailadres' + (v.aannemer ? " · aannemer" : "") + "</span></span>" +
                    '<button type="button" class="gl-v2-btn gl-v2-btn-secondary" data-koppel="' + v.gebruikerId + '" data-entra="' + esc(v.entraId) + '">Koppelen</button>' :
                    '<span class="gl-v2-ua-person-sub">geen gebruiker met dit e-mailadres</span><span></span>') + "</div>";
        }).join("") : '<div class="gl-v2-ua-pick-empty">Alle Entra-gasten zijn gekoppeld.</div>';
    }
    function loadLinks(silent) {
        return get(U.koppelingen).then(function (r) {
            if (!r.ok || !r.beschikbaar) { if (!silent) $("ua-link-list").innerHTML = '<div class="gl-v2-ua-pick-empty">' + esc(r.bericht || "Entra is niet gekoppeld in deze omgeving.") + "</div>"; return; }
            linkData = r;
            if (r.aantal > 0) {
                $("ua-link-title").textContent = r.aantal + (r.aantal === 1 ? " Entra-gast is nog niet aan een gebruiker gekoppeld" : " Entra-gasten zijn nog niet aan een gebruiker gekoppeld");
                $("ua-link-text").textContent = r.metAannemer > 0 ? r.metAannemer + (r.metAannemer === 1 ? " ervan heeft" : " ervan hebben") + " hetzelfde e-mailadres als een bestaande aannemer." : "";
                $("ua-link-notice").hidden = false;
            } else $("ua-link-notice").hidden = true;
            if (!silent) renderLinks();
        }).catch(function () { if (!silent) $("ua-link-list").innerHTML = '<div class="gl-v2-ua-pick-empty">Entra is niet bereikbaar. Probeer het later opnieuw.</div>'; });
    }
    $("ua-link-open").addEventListener("click", function () {
        $("ua-link-list").innerHTML = '<div class="gl-v2-ua-pick-empty">Entra laden…</div>';
        modal("ua-link-modal").show();
        if (linkData) renderLinks(); else loadLinks(false);
    });
    $("ua-link-list").addEventListener("click", function (e) {
        var b = e.target.closest("[data-koppel]"); if (!b) return;
        b.disabled = true;
        post(U.koppelen, { userId: b.dataset.koppel, entraId: b.dataset.entra }).then(function (r) {
            if (r.ok) reloadWith("success", "Gekoppeld", "De gebruiker is aan zijn Entra-account gekoppeld.");
            else { toast("danger", "Koppelen mislukt", r.bericht); b.disabled = false; }
        }).catch(function () { toast("danger", "Koppelen mislukt", "De server gaf geen antwoord."); b.disabled = false; });
    });

    renderRoleSelect();
    render();
    loadLinks(true);   // op de achtergrond: toont de melding enkel als er iets na te kijken valt
})();
