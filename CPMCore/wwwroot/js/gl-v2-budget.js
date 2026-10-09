/* gl-v2 — Budgetflow (design-handoff punt 39): gedeeld gedrag van de negen wizardstappen.
   · Opslagstaat in de actiebalk (opgeslagen · wijzigingen · bezig · fout)
   · "opslaan, dan navigeren": elke link met [data-bw-nav] en elke stap-chip wacht op de save-handler van de pagina
   · pagina-overgang (richting uit de stapvolgorde), laadstaat na 400 ms, voortgangslijn die van de vorige naar de nieuwe stap loopt
   · "Toon berekeningen" (body.bw-show-calc), tablet-stappenpaneel, alleen-lezen voor een definitieve versie
   Contract voor een pagina:
       GlV2Budget.register({ save: async () => true|false, dirty: () => bool });   // optioneel
       GlV2Budget.markDirty(); GlV2Budget.setStatus('saving'|'saved'|'error'|'dirty'); GlV2Budget.saved();
       GlV2Budget.autosave(form, saveFn, delayMs)  // debounced opslaan van een formulier
   Inhoud is altijd standaard zichtbaar; zonder dit script blijft de pagina volledig bruikbaar (gewone links, geen overgang). */
(function () {
    "use strict";

    var KEY_FROM = "bw-progress-from", KEY_DIR = "bw-dir";
    var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var stappen = document.getElementById("gl-v2-bw-stappen");
    var wrap = document.querySelector(".gl-v2-bw");
    var scroll = document.querySelector(".gl-v2-bw-scroll");
    var statusEl = document.querySelector("[data-bw-status]");
    var step = stappen ? parseInt(stappen.getAttribute("data-step"), 10) || 1 : 1;
    var total = stappen ? parseInt(stappen.getAttribute("data-steps"), 10) || 9 : 9;
    var hrefs = []; try { hrefs = JSON.parse(stappen.getAttribute("data-hrefs") || "[]"); } catch (e) { }
    var handler = { save: null, dirty: null };
    var busy = false, loaderTimer = null, dirtyFlag = false;

    function p2(n) { return (n < 10 ? "0" : "") + n; }
    function nu() { var d = new Date(); return p2(d.getHours()) + ":" + p2(d.getMinutes()); }

    // ── Opslagstaat ─────────────────────────────────────────────────────────────────────────────
    var laatsteFout = "";
    function setStatus(state, text) {
        if (state === "error") laatsteFout = text || "";
        if (!statusEl) return;
        var t = text;
        if (!t) t = state === "saving" ? "Opslaan…" : state === "dirty" ? "Niet-opgeslagen wijzigingen" : state === "error" ? "Opslaan mislukt — probeer opnieuw"
            : state === "locked" ? "Definitief — alleen-lezen" : "Alle wijzigingen opgeslagen";
        statusEl.setAttribute("data-state", state);
        statusEl.textContent = t;
    }
    function saved() { dirtyFlag = false; setStatus("saved", "Opgeslagen " + nu()); }
    function markDirty() { dirtyFlag = true; setStatus("dirty"); }
    var inDirtyCheck = false;
    function isDirty() {
        if (dirtyFlag) return true;
        if (!handler.dirty || inDirtyCheck) return false;   // een callback die isDirty() zelf aanroept mag nooit een oneindige lus geven
        inDirtyCheck = true; try { return !!handler.dirty(); } finally { inDirtyCheck = false; }
    }

    // ── Debounced autosave van een formulier ─────────────────────────────────────────────────────
    function autosave(form, saveFn, delay) {
        var timer = null, running = null;
        async function run() {
            if (running) { await running; }
            if (!dirtyFlag) return true;
            setStatus("saving");
            running = (async function () {
                try { var ok = await saveFn(); if (ok === false) { setStatus("error"); return false; } saved(); return true; }
                catch (e) { setStatus("error"); return false; }
            })();
            var r = await running; running = null; return r;
        }
        function plan() { markDirty(); clearTimeout(timer); timer = setTimeout(run, delay || 900); }
        form.addEventListener("input", plan);
        form.addEventListener("change", plan);
        handler.save = async function () { clearTimeout(timer); return dirtyFlag ? await run() : true; };
        handler.dirty = function () { return dirtyFlag; };
        return { flush: handler.save, plan: plan };
    }
    function register(h) { handler.save = h.save || null; handler.dirty = h.dirty || null; }

    // ── Overgang ─────────────────────────────────────────────────────────────────────────────────
    function toonLoader() { stappen && stappen.classList.add("is-loading"); scroll && scroll.classList.add("is-loading"); }
    function stopLoader() { clearTimeout(loaderTimer); stappen && stappen.classList.remove("is-loading", "is-leaving"); scroll && scroll.classList.remove("is-loading"); wrap && wrap.classList.remove("is-leaving"); busy = false; }

    function stapVan(a) {
        var d = a.getAttribute("data-bw-step");
        if (d) return parseInt(d, 10);
        var i = hrefs.indexOf(a.getAttribute("href"));
        return i >= 0 ? i + 1 : null;
    }

    async function navigeer(a) {
        if (busy) return;
        busy = true;
        var doel = stapVan(a);
        var dir = a.getAttribute("data-bw-nav") === "back" ? "prev" : a.getAttribute("data-bw-nav") === "next" ? "next" : (doel && doel < step ? "prev" : "next");
        if (handler.save && isDirty()) {
            setStatus("saving");
            var ok = true;
            try {
                ok = await Promise.race([handler.save(), new Promise(function (res) { setTimeout(function () { laatsteFout = "Het opslaan duurt te lang (geen antwoord van de server na 20 seconden)."; res(false); }, 20000); })]);
            } catch (e) { ok = false; laatsteFout = (e && e.message) || String(e); }
            if (ok === false) {
                var reden = laatsteFout; setStatus("error", reden || undefined); busy = false;
                var fouten = openFouten();
                fout({ title: "Opslaan lukt niet", desc: reden ? "De wijzigingen konden niet bewaard worden: " + reden : "De wijzigingen konden niet bewaard worden. Je blijft op deze stap zodat er niets verloren gaat.",
                       items: fouten.length ? fouten.map(function (f) { return { tekst: f.label + ": " + f.tekst, href: f.href, label: "Naar stap " + f.stap }; }) : [] });
                return;
            }
        }
        try { sessionStorage.setItem(KEY_DIR, dir); sessionStorage.setItem(KEY_FROM, String(step / total)); } catch (e) { }
        if (wrap) { wrap.setAttribute("data-bw-leave", dir); wrap.classList.add("is-leaving"); }
        if (stappen && doel) { stappen.style.setProperty("--bw-leave", String(doel / total)); stappen.classList.add("is-leaving"); }
        loaderTimer = setTimeout(toonLoader, 400);
        var url = a.href;
        setTimeout(function () { window.location.href = url; }, reduce ? 60 : 150);
    }

    document.addEventListener("click", function (e) {
        var a = e.target.closest("a[data-bw-nav], #gl-v2-bw-stappen a.gl-v2-steps-item, #gl-v2-bw-stappen .gl-v2-steps a");
        if (!a || !a.getAttribute("href")) return;
        if (e.metaKey || e.ctrlKey || e.shiftKey || e.button === 1) return;
        if (a.hasAttribute("data-bw-plain")) return;
        e.preventDefault();
        navigeer(a);
    });
    // Stappenplan: een klik op een chip gaat altijd naar die stap (via de bestemming uit data-hrefs, ook als de chip geen echte link is), in de
    // capture-fase zodat geen ander script de klik kan inslikken. Eerst opslaan (navigeer), terugkeren naar een eerdere stap werkt hetzelfde.
    if (stappen) stappen.addEventListener("click", function (e) {
        var item = e.target.closest(".gl-v2-steps-item"); if (!item) return;
        if (e.metaKey || e.ctrlKey || e.shiftKey || e.button === 1) return;
        var li = item.closest("li"), lijst = li && li.parentElement; if (!lijst) return;
        var idx = Array.prototype.indexOf.call(lijst.children, li), href = item.getAttribute("href") || hrefs[idx];
        e.preventDefault();
        if (!href || idx < 0 || idx === step - 1) return;
        e.stopPropagation();
        var tmp = document.createElement("a"); tmp.href = href; tmp.setAttribute("data-bw-step", String(idx + 1)); tmp.setAttribute("data-bw-nav", idx + 1 < step ? "back" : "next");
        navigeer(tmp);
    }, true);
    window.addEventListener("pageshow", function (e) { if (e.persisted) stopLoader(); });

    // Binnenkomst: de vorige voortgang reist mee zodat de lijn van daar naar de nieuwe stap loopt
    (function binnenkomst() {
        if (!stappen) return;
        var from = 0; try { from = parseFloat(sessionStorage.getItem(KEY_FROM)) || 0; sessionStorage.removeItem(KEY_FROM); } catch (e) { }
        stappen.style.setProperty("--bw-to", String(step / total));
        stappen.style.setProperty("--bw-from", String(from || step / total));
    })();

    // ── Toon berekeningen ───────────────────────────────────────────────────────────────────────
    var calc = document.getElementById("bw-calc-toggle");
    if (calc) {
        try { calc.checked = localStorage.getItem("bw-show-calc") === "1"; } catch (e) { }
        var zet = function () { document.body.classList.toggle("bw-show-calc", calc.checked); try { localStorage.setItem("bw-show-calc", calc.checked ? "1" : "0"); } catch (e) { } };
        calc.addEventListener("change", zet); zet();
    }

    // ── Tablet-paneel met alle stappen ──────────────────────────────────────────────────────────
    var balkBtn = document.querySelector(".gl-v2-bw-balk-btn"), panel = document.getElementById("gl-v2-bw-panel");
    if (balkBtn && panel) balkBtn.addEventListener("click", function () {
        var open = balkBtn.getAttribute("aria-expanded") !== "true";
        balkBtn.setAttribute("aria-expanded", open ? "true" : "false");
        panel.setAttribute("data-open", open ? "true" : "false");
    });

    // ── Alleen-lezen ────────────────────────────────────────────────────────────────────────────
    if (wrap && wrap.classList.contains("is-locked")) {
        setStatus("locked");
        document.querySelectorAll(".gl-v2-bw-scroll input, .gl-v2-bw-scroll textarea, .gl-v2-bw-scroll select, .gl-v2-bw-scroll button[data-bw-edit]").forEach(function (el) {
            if (!el.hasAttribute("data-bw-keep")) el.disabled = true;
        });
        document.querySelectorAll(".gl-v2-bw-scroll .gl-v2-select-trigger").forEach(function (el) { el.setAttribute("aria-disabled", "true"); el.tabIndex = -1; el.style.pointerEvents = "none"; });
    }

    // ── Toast (bestaande gl-v2-toasts) ──────────────────────────────────────────────────────────
    function toast(tone, title, body) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: body || "" }); }

    // ── CSRF voor fetch-posts ───────────────────────────────────────────────────────────────────
    function token() { var t = document.querySelector('input[name="__RequestVerificationToken"]'); return t ? t.value : ""; }
    async function post(url, data, asJson) {
        var opts = { method: "POST", credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest", "RequestVerificationToken": token(), "Accept": "application/json" } };
        if (asJson) { opts.headers["Content-Type"] = "application/json"; opts.body = JSON.stringify(data); }
        else { var fd = data instanceof FormData ? data : new URLSearchParams(data || {}); if (!(fd instanceof FormData)) fd.append("__RequestVerificationToken", token()); else if (!fd.has("__RequestVerificationToken")) fd.append("__RequestVerificationToken", token()); opts.body = fd; }
        var r = await fetch(url, opts);
        var j = null; try { j = await r.json(); } catch (e) { }
        if (!r.ok) {
            // Een mislukte aanvraag geeft altijd een leesbare reden terug (validatiefouten, serverfout, sessie verlopen, ...)
            var reden = j && (j.message || j.title) ? (j.message || j.title) : "HTTP " + r.status;
            if (j && j.errors) { try { reden += ": " + Object.keys(j.errors).map(function (k) { return k + " — " + [].concat(j.errors[k]).join(" "); }).join("; "); } catch (e) { } }
            if (r.status === 401 || r.status === 403) reden = "Je sessie is verlopen of je hebt geen rechten (HTTP " + r.status + "). Herlaad de pagina.";
            return { success: false, message: reden, status: r.status };
        }
        return j || { success: true };
    }

    var BEV_TONES = { danger: ["is-danger", "gl-v2-btn-danger", "ph-trash"], warning: ["is-warning", "gl-v2-btn-warning", "ph-warning"], success: ["is-success", "gl-v2-btn-primary", "ph-check-circle"] };
    /** Custom bevestigingsmodal (DESIGN.md TYPE 1) i.p.v. window.confirm. o = { title, desc, ok, tone: danger|warning|success, icon }. Geeft Promise<boolean>. */
    function bevestig(o) {
        return new Promise(function (resolve) {
            var el = document.getElementById("bw-bevestig");
            if (!el || !window.bootstrap || !window.bootstrap.Modal) { resolve(window.confirm(o.title)); return; }
            var t = BEV_TONES[o.tone] || BEV_TONES.danger, content = el.querySelector(".modal-content"), icon = el.querySelector(".gl-v2-modal-icon"), ok = el.querySelector("[data-bw-ok]");
            ["is-danger", "is-warning", "is-success"].forEach(function (c) { content.classList.remove(c); icon.classList.remove(c); });
            content.classList.add(t[0]); icon.classList.add(t[0]);
            icon.querySelector("i").className = "ph " + (o.icon || t[2]);
            el.querySelector(".gl-v2-modal-title").textContent = o.title || "";
            el.querySelector(".gl-v2-modal-desc").textContent = o.desc || "";
            ok.className = "gl-v2-btn " + t[1]; ok.textContent = o.ok || "Bevestigen";
            var modal = window.bootstrap.Modal.getOrCreateInstance(el), klaar = false;
            function sluit(r) { if (klaar) return; klaar = true; ok.removeEventListener("click", jaKlik); el.removeEventListener("hidden.bs.modal", neeKlik); resolve(r); }
            function jaKlik() { modal.hide(); sluit(true); }
            function neeKlik() { sluit(false); }
            ok.addEventListener("click", jaKlik); el.addEventListener("hidden.bs.modal", neeKlik);
            modal.show();
        });
    }
    /** Foutvenster (zelfde TYPE 1-modal): wat er mislukt is en wat je moet aanpassen. o = { title, desc, items: [{ tekst, href, label }] }. */
    function fout(o) {
        return new Promise(function (resolve) {
            var el = document.getElementById("bw-bevestig");
            if (!el || !window.bootstrap || !window.bootstrap.Modal) { window.alert((o.title || "") + " — " + (o.desc || "")); resolve(); return; }
            var content = el.querySelector(".modal-content"), icon = el.querySelector(".gl-v2-modal-icon"), ok = el.querySelector("[data-bw-ok]"), annuleer = el.querySelector("[data-bs-dismiss]"), lijst = el.querySelector(".gl-v2-modal-lijst");
            ["is-danger", "is-warning", "is-success"].forEach(function (c) { content.classList.remove(c); icon.classList.remove(c); });
            content.classList.add("is-danger"); icon.classList.add("is-danger"); icon.querySelector("i").className = "ph ph-warning-circle";
            el.querySelector(".gl-v2-modal-title").textContent = o.title || "";
            el.querySelector(".gl-v2-modal-desc").textContent = o.desc || "";
            lijst.innerHTML = ""; (o.items || []).forEach(function (it) { var li = document.createElement("li"); li.appendChild(document.createTextNode(it.tekst + " ")); if (it.href) { var a = document.createElement("a"); a.href = it.href; a.setAttribute("data-bw-plain", ""); a.textContent = it.label || "Openen"; li.appendChild(a); } lijst.appendChild(li); });
            lijst.hidden = !(o.items && o.items.length);
            annuleer.hidden = true; ok.className = "gl-v2-btn gl-v2-btn-primary"; ok.textContent = "Sluiten";
            var modal = window.bootstrap.Modal.getOrCreateInstance(el);
            function klaar() { el.removeEventListener("hidden.bs.modal", klaar); ok.removeEventListener("click", sluit); annuleer.hidden = false; lijst.hidden = true; resolve(); }
            function sluit() { modal.hide(); }
            ok.addEventListener("click", sluit); el.addEventListener("hidden.bs.modal", klaar);
            modal.show();
        });
    }
    /** Open fouten van de versie (uit het Stappenplan: data-fouten) als lijst met link naar de stap. */
    function openFouten() { try { return JSON.parse((document.getElementById("gl-v2-bw-stappen") || {}).getAttribute("data-fouten") || "[]"); } catch (e) { return []; } }

    // Formulieren met data-bw-bevestig vragen eerst bevestiging
    document.addEventListener("submit", async function (e) {
        var f = e.target; if (!f.hasAttribute || !f.hasAttribute("data-bw-bevestig") || f.dataset.bwOk) return;
        e.preventDefault();
        if (await bevestig({ title: f.getAttribute("data-bw-bevestig"), desc: f.getAttribute("data-bw-desc"), ok: f.getAttribute("data-bw-ok"), tone: f.getAttribute("data-bw-tone"), icon: f.getAttribute("data-bw-icon") })) { f.dataset.bwOk = "1"; f.submit(); }
    }, true);
    // 39k "Negeren" / "Terugzetten" van een waarschuwing (per code); de pagina herlaadt zodat tellers en kaders kloppen.
    document.addEventListener("click", async function (e) {
        var b = e.target.closest("[data-bw-negeer-url]"); if (!b) return;
        e.preventDefault(); b.disabled = true;
        var herstel = b.hasAttribute("data-bw-herstel");
        var r = await post(b.getAttribute("data-bw-negeer-url"), { versieId: b.getAttribute("data-bw-versie"), sleutel: b.getAttribute("data-bw-code"), bevestigd: herstel ? "false" : "true" }, false);
        if (r && r.success) window.location.reload(); else { b.disabled = false; toast("danger", "Niet gelukt", "De waarschuwing kon niet aangepast worden."); }
    });
    window.GlV2Budget = { fout: fout, bevestig: bevestig, register: register, autosave: autosave, markDirty: markDirty, saved: saved, setStatus: setStatus, isDirty: isDirty, toast: toast, post: post, token: token, step: step };
})();
