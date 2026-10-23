// gl-v2 — Factuur verzenden (Invoices/SendV2, design-handoff 11g): kanaal kiezen, ontvangers als tags (komma of Enter), bericht als opgemaakte tekst met
// "Veld invoegen", bijlagen als tegels en een samenvatting onderaan. Het formulier post naar InvoicesController.Send.
(function () {
    "use strict";
    var form = document.getElementById("invoiceSendForm");
    var root = document.getElementById("gl-v2-is-root");
    if (!form || !root) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    var EMAIL = /^[^\s@,;]+@[^\s@,;]+\.[^\s@,;]{2,}$/;

    if (window.GlV2ErrorSummary) window.GlV2ErrorSummary.init({ form: form, container: document.getElementById("gl-v2-error-summary") });

    // ── ontvangers als tags ──
    var tagBoxes = {};
    $$("[data-is-tags]").forEach(function (box) {
        var key = box.dataset.isTags, hidden = $(box.dataset.target), input = $(".gl-v2-is-tags-input", box), help = $('[data-is-help="' + key + '"]');
        var defaultHelp = help ? help.textContent : "";
        var list = [];
        function sync() { hidden.value = list.join(", "); summary(); }
        function render() {
            $$(".gl-v2-is-tag", box).forEach(function (t) { t.remove(); });
            list.forEach(function (addr, i) {
                var t = document.createElement("span"); t.className = "gl-v2-is-tag" + (EMAIL.test(addr) ? "" : " is-bad");
                t.appendChild(document.createTextNode(addr));
                var b = document.createElement("button"); b.type = "button"; b.setAttribute("aria-label", addr + " verwijderen"); b.innerHTML = '<i class="ph ph-x" aria-hidden="true"></i>';
                b.addEventListener("click", function (e) { e.stopPropagation(); list.splice(i, 1); render(); sync(); input.focus(); });
                t.appendChild(b); box.insertBefore(t, input);
            });
        }
        function add(raw) {
            String(raw || "").split(/[,;\s]+/).forEach(function (a) { a = a.trim(); if (a && list.indexOf(a) < 0) list.push(a); });
            input.value = ""; render(); sync(); box.classList.remove("is-error");
            if (help) { help.textContent = defaultHelp; help.classList.remove("is-error"); }
        }
        (hidden.value || "").split(/[,;]+/).forEach(function (a) { a = a.trim(); if (a && list.indexOf(a) < 0) list.push(a); });
        render();
        box.addEventListener("click", function () { input.focus(); });
        input.addEventListener("keydown", function (e) {
            if (e.key === "Enter" || e.key === ",") { e.preventDefault(); if (input.value.trim()) add(input.value); }
            else if (e.key === "Backspace" && !input.value && list.length) { list.pop(); render(); sync(); }
        });
        input.addEventListener("blur", function () { if (input.value.trim()) add(input.value); });
        input.addEventListener("paste", function (e) {
            var t = (e.clipboardData || window.clipboardData).getData("text");
            if (/[,;\s]/.test(t.trim())) { e.preventDefault(); add(t); }
        });
        tagBoxes[key] = {
            list: function () { return list; },
            fail: function (msg) { box.classList.add("is-error"); if (help) { help.textContent = msg; help.classList.add("is-error"); } box.scrollIntoView({ block: "center", behavior: "smooth" }); input.focus(); },
            set: function (arr) { list = arr.slice(); render(); sync(); }
        };
    });

    // ── kanaal ──
    var radios = $$('input[name="Channel"]');
    function channel() { var c = radios.filter(function (r) { return r.checked; })[0]; return c ? c.value : "email"; }
    radios.forEach(function (r) { r.addEventListener("change", function () { $$(".gl-v2-is-channel").forEach(function (l) { l.classList.toggle("is-on", !!$("input:checked", l)); }); applyChannel(); }); });
    function applyChannel() {
        var c = channel(), mail = c !== "peppol";
        ["to", "cc"].forEach(function (k) { var box = $('[data-is-tags="' + k + '"]'); if (box) box.closest(".gl-v2-field").style.opacity = mail ? "" : ".5"; });
        var ed = $("#is-body-edit"); if (ed) ed.setAttribute("contenteditable", mail ? "true" : "false");
        summary();
    }

    // ── bijlagen als tegels ──
    $$("[data-is-tile]").forEach(function (tile) {
        var cb = $("input[type=checkbox]", tile);
        if (!cb) return;
        cb.addEventListener("change", function () { tile.classList.toggle("is-on", cb.checked); summary(); });
    });

    // ── bericht als opgemaakte tekst ──
    var edit = $("#is-body-edit"), bodyField = $("#is-body");
    var saved = null;
    function remember() { var s = window.getSelection(); if (s && s.rangeCount && edit.contains(s.anchorNode)) saved = s.getRangeAt(0).cloneRange(); }
    function restore() { if (!saved) return; var s = window.getSelection(); s.removeAllRanges(); s.addRange(saved); }
    ["keyup", "mouseup", "blur"].forEach(function (ev) { edit.addEventListener(ev, remember); });
    $$("[data-is-cmd]").forEach(function (b) {
        b.addEventListener("mousedown", function (e) { e.preventDefault(); });   // behoud de selectie in het bericht
        b.addEventListener("click", function () {
            edit.focus(); restore();
            var cmd = b.dataset.isCmd;
            if (cmd === "createLink") {
                // geen browserdialoog: de link komt via een kleine invoerbalk
                openLink();
                return;
            }
            document.execCommand(cmd, false, null); remember();
        });
    });
    // kleine invoerbalk voor een link (geen window.prompt)
    var linkBar = null;
    function openLink() {
        remember();
        if (!linkBar) {
            linkBar = document.createElement("div"); linkBar.className = "gl-v2-is-fieldlist"; linkBar.style.cssText = "right:auto;left:0;top:calc(100% + 4px);min-width:320px;flex-direction:row;gap:6px;align-items:center";
            linkBar.innerHTML = '<input type="url" class="gl-v2-field-input" placeholder="https://" style="flex:1;min-width:0;height:32px;border:1px solid rgba(44,59,42,.18);border-radius:8px;padding:0 10px" aria-label="Adres van de link" /><button type="button" class="gl-v2-btn gl-v2-btn-primary" style="height:32px">Toepassen</button>';
            $(".gl-v2-is-toolbar").appendChild(linkBar); $(".gl-v2-is-toolbar").style.position = "relative";
            var inp = $("input", linkBar), ok = $("button", linkBar);
            function apply() { var u = inp.value.trim(); if (u) { edit.focus(); restore(); document.execCommand("createLink", false, /^https?:\/\//i.test(u) ? u : "https://" + u); } linkBar.hidden = true; inp.value = ""; }
            ok.addEventListener("click", apply); inp.addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); apply(); } if (e.key === "Escape") linkBar.hidden = true; });
        }
        linkBar.hidden = false; $("input", linkBar).focus();
    }
    var fbtn = $("#is-field-btn"), flist = $("#is-field-list");
    if (fbtn && flist) {
        fbtn.addEventListener("mousedown", function (e) { e.preventDefault(); });
        fbtn.addEventListener("click", function () { var open = flist.hidden; flist.hidden = !open; fbtn.setAttribute("aria-expanded", open ? "true" : "false"); });
        flist.addEventListener("mousedown", function (e) { e.preventDefault(); });
        flist.addEventListener("click", function (e) {
            var b = e.target.closest("[data-is-field]"); if (!b) return;
            edit.focus(); restore(); document.execCommand("insertHTML", false, "<b>" + b.dataset.isField.replace(/[&<>]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;" }[c]; }) + "</b>&nbsp;");
            remember(); flist.hidden = true; fbtn.setAttribute("aria-expanded", "false");
        });
        document.addEventListener("click", function (e) { if (!e.target.closest(".gl-v2-is-fieldmenu")) { flist.hidden = true; fbtn.setAttribute("aria-expanded", "false"); } });
    }

    // ── samenvatting onderaan ──
    function summary() {
        var el = $("#is-summary"); if (!el) return;
        var c = channel(), to = (tagBoxes.to && tagBoxes.to.list().length) || 0, cc = (tagBoxes.cc && tagBoxes.cc.list().length) || 0;
        var parts = [];
        if (c !== "peppol") parts.push("verzendt naar " + to + (to === 1 ? " ontvanger" : " ontvangers") + (cc ? " + " + cc + " cc" : "") + " via e-mail");
        if (c !== "email") parts.push(c === "peppol" ? "doorgestuurd naar Octopus (Peppol)" : "en doorgestuurd naar Octopus (Peppol)");
        el.textContent = "· " + parts.join(" ");
    }

    // ── verzenden ──
    var copyBtn = $("#is-copy-btn"), selfField = $("#is-copy-self");
    if (copyBtn) copyBtn.addEventListener("click", function () { selfField.value = "1"; });
    var sendBtn = $("#is-send-btn"); if (sendBtn) sendBtn.addEventListener("click", function () { selfField.value = "0"; });
    form.addEventListener("submit", function (e) {
        var submitter = e.submitter, isCopySelf = submitter && submitter.id === "is-copy-btn";
        var isOctopus = submitter && submitter.hasAttribute("formnovalidate");
        bodyField.value = edit.innerHTML;
        if (isOctopus) return;
        var mail = channel() !== "peppol";
        if (mail && !isCopySelf) {
            var to = tagBoxes.to.list();
            if (!to.length) { e.preventDefault(); tagBoxes.to.fail("Voeg minstens één e-mailadres toe."); return; }
            var bad = to.concat(tagBoxes.cc.list()).filter(function (a) { return !EMAIL.test(a); });
            if (bad.length) { e.preventDefault(); tagBoxes.to.fail("Controleer dit adres: " + bad[0]); return; }
            if (!$$(".gl-v2-is-attach-input").some(function (c) { return c.checked; }) && root.dataset.copy !== "1") { e.preventDefault(); window.GlV2Toast && window.GlV2Toast.show({ tone: "danger", title: "Bijlage ontbreekt", message: "Kies minstens één bijlage." }); return; }
        }
        if (isCopySelf && !root.dataset.me) { e.preventDefault(); window.GlV2Toast && window.GlV2Toast.show({ tone: "danger", title: "Geen e-mailadres", message: "Van jouw account is geen e-mailadres bekend." }); return; }
        [sendBtn, copyBtn].forEach(function (b) { if (b) b.classList.add("is-loading"); });
        window.setTimeout(function () { [sendBtn, copyBtn].forEach(function (b) { if (b) b.disabled = true; }); }, 0);
    });

    applyChannel();
})();
