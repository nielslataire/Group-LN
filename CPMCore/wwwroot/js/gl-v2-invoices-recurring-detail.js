// gl-v2 — Schemadetail van een terugkerende factuur (Invoices/RecurringDetailV2, design-handoff punt 50d): pauzeren, hervatten, nu een factuur maken,
// indexatie bevestigen en stopzetten. Elke actie post naar InvoicesController.RecurringAction; de server beslist en antwoordt { ok, error }.
(function () {
    "use strict";
    var cfg = window.GlV2RecurringDetail, root = document.getElementById("ri-detail");
    if (!cfg || !root) return;
    var id = root.getAttribute("data-schedule-id"), listUrl = root.getAttribute("data-list-url");
    var tokenInput = document.querySelector("#ri-token-form input[name=__RequestVerificationToken]");

    function toast(type, text) {
        if (window.GlV2Toast && window.GlV2Toast.show) window.GlV2Toast.show({ tone: type === "error" ? "danger" : type, title: type === "error" ? "Niet gelukt" : "Gelukt", body: text });
        else if (type === "error") window.alert(text);
    }
    function post(op, extra) {
        var fd = new FormData();
        fd.append("__RequestVerificationToken", tokenInput ? tokenInput.value : "");
        fd.append("id", id); fd.append("op", op);
        Object.keys(extra || {}).forEach(function (k) { fd.append(k, extra[k]); });
        return fetch(cfg.actionUrl, { method: "POST", body: fd, credentials: "same-origin", headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" } })
            .then(function (r) { return r.json(); })
            .then(function (r) { if (!r.ok) throw new Error(r.error || "De actie kon niet uitgevoerd worden."); return r; });
    }
    function run(op, extra, after) {
        var buttons = Array.prototype.slice.call(document.querySelectorAll("[data-ri-act]"));
        buttons.forEach(function (b) { b.disabled = true; });
        return post(op, extra).then(function (r) { if (after) after(r); else window.location.reload(); }, function (e) {
            buttons.forEach(function (b) { b.disabled = false; });
            toast("error", e.message);
        });
    }
    function ask(o) { return window.GlV2Bevestig ? window.GlV2Bevestig(o) : Promise.resolve(window.confirm(o.title)); }

    document.addEventListener("click", function (e) {
        var b = e.target.closest("[data-ri-act]"); if (!b) return;
        var act = b.getAttribute("data-ri-act");
        if (act === "pause") {
            ask({ title: "Schema pauzeren?", desc: "Er worden geen concepten meer klaargezet tot je het schema hervat. Bestaande concepten en facturen blijven staan.", ok: "Pauzeren", tone: "warning", icon: "ph-pause" })
                .then(function (ok) { if (ok) run("pause"); });
        } else if (act === "resume") {
            var missed = parseInt(b.getAttribute("data-ri-missed"), 10) || 0, mode = b.getAttribute("data-ri-mode");
            if (missed === 0) { run("resume", { backfill: "false" }); return; }
            if (mode === "2") { run("resume", { backfill: "false" }); return; }
            document.getElementById("ri-resume-desc").textContent = "Het schema stond stil terwijl " + missed + " factuurdatum" + (missed === 1 ? "" : "s") + " voorbij " + (missed === 1 ? "ging" : "gingen") + ". Wil je die periodes alsnog factureren of overslaan?";
            window.bootstrap.Modal.getOrCreateInstance(document.getElementById("ri-resume-modal")).show();
        } else if (act === "generate") {
            if (b.getAttribute("data-ri-held") === "1") { toast("error", "Bevestig eerst de indexatie voor je een factuur maakt."); return; }
            ask({ title: "Nu een factuur maken?", desc: "Het concept voor de volgende factuurdatum wordt nu klaargezet als Proforma. Het schema schuift een periode op.", ok: "Concept maken", tone: "success", icon: "ph-plus" })
                .then(function (ok) { if (ok) run("generate", {}, function (r) { window.location.assign(cfg.invoicesUrl); }); });
        } else if (act === "stop") {
            ask({ title: "Schema stopzetten?", desc: "Er komen geen nieuwe facturen meer uit dit schema. Bestaande concepten en facturen blijven staan. Dit kan je niet ongedaan maken.", ok: "Stopzetten", tone: "danger", icon: "ph-stop-circle" })
                .then(function (ok) { if (ok) run("stop"); });
        } else if (act === "reactivate") {
            ask({ title: "Schema opnieuw activeren?", desc: "Het schema loopt weer. Factuurdatums die intussen voorbij zijn worden overgeslagen; de volgende factuur is de eerstvolgende datum vanaf vandaag.", ok: "Activeren", tone: "success", icon: "ph-arrow-counter-clockwise" })
                .then(function (ok) { if (ok) run("reactivate"); });
        } else if (act === "delete") {
            var made = parseInt(b.getAttribute("data-ri-created"), 10) || 0;
            ask({ title: "Schema verwijderen?", desc: made > 0 ? "Het schema verdwijnt. De " + made + " aangemaakte factu" + (made === 1 ? "ur" : "ren") + " en concepten blijven staan als gewone facturen, zonder koppeling met dit schema. Dit kan je niet ongedaan maken." : "Het schema verdwijnt volledig. Dit kan je niet ongedaan maken.", ok: "Verwijderen", tone: "danger" })
                .then(function (ok) { if (ok) run("delete", {}, function () { window.location.assign(listUrl); }); });
        } else if (act === "confirm-index") {
            var p = document.getElementById("ri-index-percent");
            run("confirm-index", { percent: p ? p.value : "" });
        }
    });

    document.addEventListener("click", function (e) {
        var b = e.target.closest("[data-ri-resume]"); if (!b) return;
        window.bootstrap.Modal.getOrCreateInstance(document.getElementById("ri-resume-modal")).hide();
        run("resume", { backfill: b.getAttribute("data-ri-resume") === "backfill" ? "true" : "false" });
    });

    // een rij in "Aangemaakte facturen" opent de factuur
    document.addEventListener("click", function (e) {
        var tr = e.target.closest("tr[data-href]"); if (!tr || e.target.closest("a,button")) return;
        window.location.assign(tr.getAttribute("data-href"));
    });
    document.addEventListener("keydown", function (e) {
        if (e.key !== "Enter") return;
        var tr = e.target.closest && e.target.closest("tr[data-href]"); if (tr) window.location.assign(tr.getAttribute("data-href"));
    });
})();
