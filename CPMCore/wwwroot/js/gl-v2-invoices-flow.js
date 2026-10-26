// gl-v2 — "Achter de schermen bij facturen" (design-handoff punt 48): voortgangsscherm op volle pagina voor nummeren en verzenden (48c–f)
// en doorboeken naar Octopus (48a–b), vanuit Invoices/Index (gl-v2-invoices.js). Opmaak en inhoud komen uit het gedeelde GlV2ProgressScreen
// (gl-v2-progress-screen.js + css/gl-v2/progress-screen.css), hetzelfde als bij "Verslag voorbereiden" en de budget-pdf.
//
//   GlV2InvoiceFlow.issue({ invoiceId, issuerCompanyId, urls: { issue, resendPeppol, detail, send, create, index } })
//   GlV2InvoiceFlow.book({ issuerCompanyId, invoiceIds, urls: { book, index } })
//
// De server doet het werk in één aanroep (IssueV2 / BookInvoicesV2); tijdens het wachten toont het scherm wat er gebeurt, daarna wat er écht
// gebeurd is (nummer, Peppol-status uit Octopus, e-mail). Er wordt geen voortgang gesimuleerd.
(function () {
    "use strict";

    var redirectTimer = null;

    function pad(n) { return (n < 10 ? "0" : "") + n; }
    function nowText() { var d = new Date(); return pad(d.getDate()) + "/" + pad(d.getMonth() + 1) + "/" + d.getFullYear() + " · " + pad(d.getHours()) + ":" + pad(d.getMinutes()); }

    function host() {
        var h = document.getElementById("iv-flow");
        if (h) return h;
        h = document.createElement("section");
        h.id = "iv-flow"; h.className = "gl-v2-vsprep"; h.setAttribute("role", "status"); h.setAttribute("aria-live", "polite"); h.hidden = true;
        var main = document.getElementById("gl-v2-main") || document.body;
        main.insertBefore(h, main.firstChild);
        return h;
    }

    // o: zie GlV2ProgressScreen.render; actions: [{ label, id, href, primary }]
    function show(o) {
        var h = host();
        clearTimeout(redirectTimer);
        window.GlV2ProgressScreen.render(h, o);
        h.hidden = false; document.body.classList.add("gl-v2-vsprep-on", "gl-v2-vsprep-all");
        window.scrollTo(0, 0);
        var first = h.querySelector("[data-pscr], a.gl-v2-btn"); if (first && !o.busy) first.focus({ preventScroll: true });
    }
    function hide() {
        clearTimeout(redirectTimer);
        var h = document.getElementById("iv-flow"); if (h) { window.GlV2ProgressScreen.stop(h); h.hidden = true; }
        document.body.classList.remove("gl-v2-vsprep-on", "gl-v2-vsprep-all");
    }

    function token() { var i = document.querySelector('input[name="__RequestVerificationToken"]'); return i ? i.value : ""; }
    function post(url, data) {
        var fd = new FormData();
        Object.keys(data || {}).forEach(function (k) {
            var v = data[k];
            if (Array.isArray(v)) v.forEach(function (x) { fd.append(k, x); }); else if (v != null) fd.append(k, v);
        });
        fd.append("__RequestVerificationToken", token());
        return fetch(url, { method: "POST", body: fd, credentials: "same-origin", headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" } }).then(function (r) {
            var type = r.headers.get("content-type") || "";
            if (type.indexOf("json") < 0) throw new Error(r.redirected || r.status === 401 || r.status === 403 ? "Je sessie is verlopen of je hebt hier geen rechten voor. Meld je opnieuw aan en probeer opnieuw." : "De server gaf geen geldig antwoord (" + r.status + "). Probeer opnieuw.");
            return r.json();
        });
    }
    function navigate(url) { if (url) window.location.assign(url); }

    // ─────────────── Nummeren en verzenden (48c–f) ───────────────
    function issue(opts) {
        var u = opts.urls || {};
        var detailUrl = function (id, issuer) { return u.detail + "?id=" + encodeURIComponent(id) + "&issuerCompanyId=" + encodeURIComponent(issuer || opts.issuerCompanyId || ""); };
        var run = function () {
            show({
                art: "invoice", busy: true, kicker: "Nummeren en verzenden", title: "De factuur wordt genummerd",
                sub: "Na het nummeren kan je de factuur niet meer verwijderen, enkel crediteren. Sluit deze pagina niet tot het klaar is.",
                steps: [{ t: "Nummer toekennen en factuur opmaken", s: "act" }, { t: "Doorsturen naar Octopus en Peppol", s: "todo" }, { t: "E-mail met pdf", s: "todo" }],
                foot: ["Gemiddeld 5 à 10 seconden", "Het volgnummer wordt toegekend…", "De pdf wordt opgemaakt met de bijlage…", "De factuur gaat naar Octopus…"]
            });
            return post(u.issue, { id: opts.invoiceId }).then(result, failed);
        };
        var failed = function (err) {
            var message = err && err.message ? err.message : "Er ging iets mis. Probeer opnieuw.";
            show({ art: "warn", warn: true, kicker: "Nummeren", title: "Nummeren is niet gelukt", sub: message, steps: [], actions: [{ label: "Terug", id: "back", primary: true }] });
        };
        var result = function (r) {
            if (!r || !r.ok) return failed(new Error(r && r.error ? r.error : ""));
            var rows = [];
            var number = r.publicId || "";
            rows.push({ t: "Nummer toegekend", m: number, s: "done" });
            if (r.structuredMessage) rows.push({ t: "Gestructureerde mededeling en QR-code", m: r.structuredMessage, s: "done" });
            rows.push({ t: "Doorgestuurd naar Octopus", s: "done" });
            var peppolFailed = r.peppol === "failed", emailFailed = r.email === "failed";
            if (r.peppol === "delivered") rows.push({ t: "Via Peppol afgeleverd", m: "afgeleverd", d: r.peppolId || "", s: "done" });
            else if (r.peppol === "pending") rows.push({ t: "Via Peppol verzonden", m: "onderweg", d: r.peppolId || "", s: "act" });
            else if (peppolFailed) rows.push({ t: "Via Peppol", m: "mislukt", d: r.peppolComment || "Het Peppol-netwerk weigerde de levering.", s: "err" });
            else rows.push({ t: "Via Peppol", m: "niet gemeld", d: "Octopus meldt geen Peppol-levering. De klant is mogelijk niet bereikbaar via Peppol, of de status volgt later.", s: "skip" });
            if (r.email === "sent") rows.push({ t: "E-mail met pdf", m: "verzonden", d: r.clientEmail || "", s: "done" });
            else if (emailFailed) rows.push({ t: "E-mail met pdf", m: "mislukt", d: "Verstuur ze opnieuw via het verzendscherm.", s: "err" });
            else rows.push({ t: "E-mail met pdf", m: "niet verzonden", d: r.clientEmail ? "Er is geen klassieke e-mail nodig of ze werd overgeslagen." : "Er is geen e-mailadres bij de klant.", s: "skip" });

            var open = { label: "Factuur openen", href: detailUrl(r.invoiceId || opts.invoiceId, r.issuerCompanyId), primary: !(peppolFailed || emailFailed) };
            var sent = [];
            if (r.peppol === "delivered") sent.push("afgeleverd via Peppol"); else if (r.peppol === "pending") sent.push("onderweg via Peppol");
            if (r.email === "sent") sent.push("per e-mail verzonden");
            var due = r.dueDate ? " Te betalen vóór " + r.dueDate + "." : "";
            if (peppolFailed) {
                show({
                    art: "warn", warn: true, kicker: "Genummerd · 1 kanaal mislukt", title: "Peppol-levering mislukt",
                    sub: "De factuur is genummerd en naar Octopus gestuurd" + (r.email === "sent" ? " en per e-mail verzonden" : "") + ". Het Peppol-netwerk weigerde de levering. Probeer opnieuw" + (r.email === "sent" ? " of laat de e-mail volstaan." : "."),
                    steps: rows, actions: [{ label: "Peppol opnieuw proberen", id: "retry-peppol", primary: true }, open]
                });
                return;
            }
            if (emailFailed) {
                var actions = [open];
                if (u.send) actions.unshift({ label: "Verzenden", href: u.send + "?id=" + encodeURIComponent(r.invoiceId || opts.invoiceId) + "&issuerCompanyId=" + encodeURIComponent(r.issuerCompanyId || opts.issuerCompanyId || ""), primary: true });
                actions[actions.length - 1].primary = false;
                show({
                    art: "warn", warn: true, kicker: "Genummerd · e-mail mislukt", title: "De e-mail kon niet verzonden worden",
                    sub: "De factuur is genummerd en naar Octopus gestuurd. Verstuur ze zelf opnieuw per e-mail via het verzendscherm.",
                    steps: rows, actions: actions
                });
                return;
            }
            show({
                art: "ok", kicker: "Verzonden · " + nowText(), title: number ? "Factuur " + number + " verzonden" : "Factuur genummerd",
                sub: "Doorgestuurd naar Octopus" + (sent.length ? " en " + sent.join(" en ") : "") + "." + due,
                steps: rows, note: "Je gaat vanzelf naar de factuur.",
                actions: [open, { label: "Nieuwe factuur", href: u.create, primary: false }].filter(function (a) { return a.href; })
            });
            redirectTimer = setTimeout(function () { navigate(open.href); }, 6000);
        };
        host().onclick = function (ev) {
            var b = ev.target.closest("[data-pscr]"); if (!b) return;
            var id = b.getAttribute("data-pscr");
            if (id === "back") hide();
            else if (id === "retry-peppol") {
                show({ art: "send", busy: true, kicker: "Peppol opnieuw proberen", title: "De factuur wordt opnieuw verzonden", sub: "Octopus probeert de factuur opnieuw via Peppol af te leveren.", steps: [{ t: "Opnieuw via Peppol", s: "act" }], foot: ["Gemiddeld 5 à 10 seconden"] });
                post(u.resendPeppol, { id: opts.invoiceId }).then(result, failed);
            }
        };
        run();
    }

    // ─────────────── Doorboeken naar Octopus (48a–b) ───────────────
    function book(opts) {
        var u = opts.urls || {};
        var n = (opts.invoiceIds || []).length;
        var run = function () {
            show({
                art: "sync", busy: true, kicker: "Doorboeken naar Octopus · " + n + (n === 1 ? " factuur" : " facturen"), title: n === 1 ? "De factuur wordt doorgeboekt" : "De facturen worden doorgeboekt",
                sub: "De facturen worden gecontroleerd en in Octopus geboekt. Sluit deze pagina niet tot het klaar is.",
                steps: [{ t: "Facturen controleren", m: n + " geselecteerd", s: "act" }, { t: "Facturen doorboeken in Octopus", s: "todo" }, { t: "Status bijwerken in het CRM", s: "todo" }],
                foot: ["Gemiddeld 2 à 4 seconden per factuur", "Octopus boekt de facturen in het dagboek…"]
            });
            post(u.book, { issuerCompanyId: opts.issuerCompanyId, invoiceIds: opts.invoiceIds }).then(done, failed);
        };
        var failed = function (err) {
            var message = err && err.message ? err.message : "Er ging iets mis. Probeer opnieuw.";
            show({
                art: "warn", warn: true, kicker: "Doorboeken · " + nowText(), title: "Boeken is niet gelukt", sub: message + " De facturen zijn niet als geboekt gemarkeerd. Los de fout op en probeer opnieuw.",
                steps: [{ t: "Facturen controleren", m: n + " geselecteerd", s: "done" }, { t: "Facturen doorboeken in Octopus", m: "mislukt", d: message, s: "err" }, { t: "Status bijwerken in het CRM", s: "skip" }],
                actions: [{ label: "Opnieuw boeken", id: "retry", primary: true }, { label: "Naar facturen", id: "list" }]
            });
        };
        var done = function (r) {
            if (!r || !r.ok) return failed(new Error(r && r.error ? r.error : ""));
            var count = r.count || n;
            var range = r.from === r.until ? "nummer " + r.from : "nummer " + r.from + " tot en met " + r.until;
            show({
                art: "ok", kicker: "Geboekt · " + nowText(), title: count === 1 ? "1 factuur doorgeboekt" : count + " facturen doorgeboekt",
                sub: "De facturen van " + range + " staan in Octopus en zijn in de lijst gemarkeerd als geboekt.",
                steps: [{ t: "Facturen controleren", m: n + " geselecteerd", s: "done" }, { t: "Facturen doorgeboekt in Octopus", m: count + (count === 1 ? " factuur" : " facturen"), d: "nummer " + r.from + (r.from === r.until ? "" : " t.e.m. " + r.until), s: "done" }, { t: "Status bijgewerkt in het CRM", s: "done" }],
                note: "De lijst wordt vanzelf ververst.", actions: [{ label: "Naar facturen", id: "list", primary: true }]
            });
            redirectTimer = setTimeout(function () { reload(); }, 6000);
        };
        var reload = function () { if (u.index) navigate(u.index); else window.location.reload(); };
        host().onclick = function (ev) {
            var b = ev.target.closest("[data-pscr]"); if (!b) return;
            var id = b.getAttribute("data-pscr");
            if (id === "list") reload(); else if (id === "retry") run();
        };
        run();
    }

    window.addEventListener("pageshow", function (e) { if (e.persisted) hide(); });
    window.GlV2InvoiceFlow = { issue: issue, book: book, hide: hide };
})();
