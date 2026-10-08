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
    function setStatus(state, text) {
        if (!statusEl) return;
        var t = text;
        if (!t) t = state === "saving" ? "Opslaan…" : state === "dirty" ? "Niet-opgeslagen wijzigingen" : state === "error" ? "Opslaan mislukt — probeer opnieuw"
            : state === "locked" ? "Definitief — alleen-lezen" : "Alle wijzigingen opgeslagen";
        statusEl.setAttribute("data-state", state);
        statusEl.textContent = t;
    }
    function saved() { dirtyFlag = false; setStatus("saved", "Opgeslagen " + nu()); }
    function markDirty() { dirtyFlag = true; setStatus("dirty"); }
    function isDirty() { return dirtyFlag || (handler.dirty ? !!handler.dirty() : false); }

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
            var ok = true; try { ok = await handler.save(); } catch (e) { ok = false; }
            if (ok === false) { setStatus("error"); busy = false; return; }
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
        if (!r.ok && !j) throw new Error("HTTP " + r.status);
        return j || { success: r.ok };
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
    // Formulieren met data-bw-bevestig vragen eerst bevestiging
    document.addEventListener("submit", async function (e) {
        var f = e.target; if (!f.hasAttribute || !f.hasAttribute("data-bw-bevestig") || f.dataset.bwOk) return;
        e.preventDefault();
        if (await bevestig({ title: f.getAttribute("data-bw-bevestig"), desc: f.getAttribute("data-bw-desc"), ok: f.getAttribute("data-bw-ok"), tone: f.getAttribute("data-bw-tone"), icon: f.getAttribute("data-bw-icon") })) { f.dataset.bwOk = "1"; f.submit(); }
    }, true);
    window.GlV2Budget = { bevestig: bevestig, register: register, autosave: autosave, markDirty: markDirty, saved: saved, setStatus: setStatus, isDirty: isDirty, toast: toast, post: post, token: token, step: step };
})();
