// gl-v2 — Projecten/DetailInsurancesV2 en AddInsuranceV2 (design-handoff 43a, 43b, 43c).
// Overzicht: zoeken en filteren van polissen en attesten, contextmenu-acties (beëindigen, verwijderen), attest vastleggen, attesten opvragen
// (selectiebalk en venster met één mail per aannemer). Formulier: velden per type, start bij voorlopige oplevering, bewaren als aangevraagd, dropzone.
(function () {
    "use strict";

    var cfg = window.glV2VerzekeringenConfig || {};
    function q(sel, scope) { return (scope || document).querySelector(sel); }
    function qa(sel, scope) { return Array.prototype.slice.call((scope || document).querySelectorAll(sel)); }
    function byId(id) { return document.getElementById(id); }
    function modalOf(id) { var el = byId(id); return el && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(el) : null; }
    function token() { var i = q('input[name="__RequestVerificationToken"]'); return i ? i.value : ""; }

    function post(url, params) {
        return fetch(url, {
            method: "POST", credentials: "same-origin",
            headers: { "RequestVerificationToken": token(), "X-Requested-With": "XMLHttpRequest", "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
            body: params.toString()
        }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); });
    }

    // ── Overzicht: zoeken en filteren ───────────────────────────────────────────────────────────
    var root = byId("gln-vz-root");
    if (root) {
        var rows = qa("[data-row]", root);
        var filter = "alle";
        var zoek = byId("gln-vz-zoek");
        var leeg = byId("gln-vz-leeg");

        function pas() {
            var term = zoek ? zoek.value.trim().toLowerCase() : "";
            var zichtbaar = 0;
            rows.forEach(function (r) {
                var okFilter = filter === "alle" || r.getAttribute("data-status") === filter;
                var okZoek = !term || (r.getAttribute("data-zoek") || "").indexOf(term) >= 0;
                r.hidden = !(okFilter && okZoek);
                if (!r.hidden) zichtbaar++;
            });
            if (leeg) leeg.hidden = zichtbaar > 0 || rows.length === 0;
        }
        if (zoek) zoek.addEventListener("input", pas);
        qa("#gln-vz-chips .gln-vz-chip").forEach(function (chip) {
            chip.addEventListener("click", function () {
                filter = chip.getAttribute("data-filter");
                qa("#gln-vz-chips .gln-vz-chip").forEach(function (c) { c.classList.toggle("is-active", c === chip); });
                pas();
            });
        });

        // ── Contextmenu: beëindigen en verwijderen ──────────────────────────────────────────────
        document.addEventListener("click", function (e) {
            var einde = e.target.closest("[data-vz-einde]");
            if (einde) {
                e.preventDefault();
                if (window.GlV2Menu && window.GlV2Menu.closeAll) window.GlV2Menu.closeAll();
                byId("gln-vz-einde-id").value = einde.getAttribute("data-vz-einde");
                byId("gln-vz-einde-naam").textContent = einde.getAttribute("data-naam") + ": vanaf welke datum is deze polis beëindigd?";
                var m = modalOf("gln-vz-einde-modal"); if (m) m.show();
                return;
            }
            var del = e.target.closest("[data-vz-verwijder]");
            if (del) {
                e.preventDefault();
                if (window.GlV2Menu && window.GlV2Menu.closeAll) window.GlV2Menu.closeAll();
                var f = byId("gln-vz-del-form");
                if (!f) return;
                byId("gln-vz-del-id").value = del.getAttribute("data-vz-verwijder");
                f.setAttribute("data-bv-tekst", del.getAttribute("data-naam") + " wordt definitief verwijderd, samen met de verzekeringsactiviteit op het contract van de makelaar. Het polisdocument in Documenten blijft staan.");
                delete f.dataset.bvOk;
                if (f.requestSubmit) f.requestSubmit(); else f.submit();
            }
        });

        // ── Attest vastleggen ───────────────────────────────────────────────────────────────────
        var atForm = byId("gln-vz-attest-form");
        var atData = null;
        function atKind() { var r = q('input[name="kind"]:checked', atForm); return r ? r.value : "0"; }
        function atStatus() { var r = q('input[name="status"]:checked', atForm); return r ? r.value : "1"; }
        function setRadio(name, value) { qa('input[name="' + name + '"]', atForm).forEach(function (r) { r.checked = r.value === String(value); }); }
        function setDate(id, iso) {
            var hidden = byId(id), text = byId(id + "_text");
            if (!hidden || !text) return;
            hidden.value = iso || "";
            if (iso) { var p = iso.split("-"); text.value = p[2] + "/" + p[1] + "/" + p[0]; } else text.value = "";
            hidden.dispatchEvent(new Event("change", { bubbles: true }));
        }
        function atToon() {
            if (!atForm) return;
            var kind = atKind(), status = atStatus();
            qa('[data-at-veld="ontvangen"]', atForm).forEach(function (el) { el.hidden = status !== "1"; });
            qa('[data-at-veld="woningen"]', atForm).forEach(function (el) { el.hidden = !(status === "1" && kind === "0"); });
        }
        function atVul() {
            var d = atKind() === "1" ? atData.ba : atData.tj;
            setRadio("status", d.status);
            var gekozen = d.unitIds || [];
            qa('input[name="unitIds"]', atForm).forEach(function (c) { c.checked = gekozen.indexOf(parseInt(c.value, 10)) >= 0; });
            setDate("gln-vz-at-geldig", d.validUntil);
            byId("gln-vz-at-maatschappij").value = d.insurer || "";
            byId("gln-vz-at-polis").value = d.policyNumber || "";
            byId("gln-vz-at-opm").value = d.note || "";
            byId("gln-vz-at-bestand").value = "";
            atToon();
        }
        if (atForm) {
            var alles = q("[data-vz-alles]", atForm);
            if (alles) alles.addEventListener("click", function (e) {
                e.preventDefault();
                var boxen = qa('input[name="unitIds"]', atForm);
                var allemaal = boxen.every(function (c) { return c.checked; });
                boxen.forEach(function (c) { c.checked = !allemaal; });
            });
            qa('input[name="kind"]', atForm).forEach(function (r) { r.addEventListener("change", function () { if (atData) atVul(); }); });
            qa('input[name="status"]', atForm).forEach(function (r) { r.addEventListener("change", atToon); });
        }
        document.addEventListener("click", function (e) {
            var knop = e.target.closest("[data-vz-attest]");
            if (!knop || !atForm) return;
            e.preventDefault();
            try { atData = JSON.parse(knop.getAttribute("data-vz-attest")); } catch (err) { return; }
            byId("gln-vz-at-contract").value = atData.contractId;
            byId("gln-vz-at-naam").textContent = atData.bedrijf + " · " + atData.label;
            setRadio("kind", 0);
            atVul();
            var m = modalOf("gln-vz-attest-modal"); if (m) m.show();
        });

        // ── Selectie en attesten opvragen ───────────────────────────────────────────────────────
        var selbar = byId("gln-vz-selbar");
        function gekozen() {
            return qa(".gln-vz-check:checked", root).map(function (c) { return c.closest("[data-row]").getAttribute("data-contract"); });
        }
        function selUpdate() {
            if (!selbar) return;
            var ids = gekozen();
            selbar.hidden = ids.length === 0;
            byId("gln-vz-sel-aantal").textContent = ids.length + " geselecteerd";
            byId("gln-vz-sel-n").textContent = ids.length;
        }
        root.addEventListener("change", function (e) { if (e.target.classList && e.target.classList.contains("gln-vz-check")) selUpdate(); });
        var wis = byId("gln-vz-sel-wis");
        if (wis) wis.addEventListener("click", function () { qa(".gln-vz-check", root).forEach(function (c) { c.checked = false; }); selUpdate(); });

        var mailsGedaan = false;
        function toonMails(ids) {
            var params = new URLSearchParams();
            params.append("projectid", cfg.projectId);
            ids.forEach(function (id) { params.append("contractIds", id); });
            post(cfg.urls.mails, params).then(function (mails) {
                var host = byId("gln-vz-mails");
                host.innerHTML = "";
                mailsGedaan = false;
                if (!mails.length) { host.innerHTML = '<span class="gln-vz-mail-geen">Er ontbreekt niets bij de gekozen aannemers.</span>'; }
                mails.forEach(function (m) {
                    var rij = document.createElement("div");
                    rij.className = "gln-vz-mail";
                    var tekst = document.createElement("div");
                    tekst.className = "gln-vz-mail-tekst";
                    var naam = document.createElement("strong"); naam.textContent = m.bedrijf;
                    var sub = document.createElement("span"); sub.textContent = m.email ? m.email + " · " + m.onderwerp : "Geen e-mailadres bekend bij dit bedrijf. Vul het in bij de leverancier.";
                    tekst.appendChild(naam); tekst.appendChild(sub);
                    rij.appendChild(tekst);
                    if (m.email) {
                        var knop = document.createElement("button");
                        knop.type = "button"; knop.className = "gl-v2-btn gl-v2-btn-secondary";
                        knop.innerHTML = '<i class="ph ph-envelope-simple" aria-hidden="true"></i>E-mail openen';
                        knop.addEventListener("click", function () {
                            var p = new URLSearchParams(); p.append("projectid", cfg.projectId); p.append("contractId", m.contractId);
                            post(cfg.urls.markRequested, p).then(function () { rij.classList.add("is-klaar"); mailsGedaan = true; }).catch(function () { /* de mail opent toch */ });
                            window.location.href = m.mailto;
                        });
                        rij.appendChild(knop);
                    }
                    host.appendChild(rij);
                });
                var modal = modalOf("gln-vz-mails-modal"); if (modal) modal.show();
            }).catch(function () {
                if (window.GlV2Toast) window.GlV2Toast.show({ tone: "error", title: "Opvragen mislukt", body: "De mails konden niet klaargezet worden. Probeer opnieuw." });
            });
        }
        var opv = byId("gln-vz-sel-opvragen");
        if (opv) opv.addEventListener("click", function () { var ids = gekozen(); if (ids.length) toonMails(ids); });
        document.addEventListener("click", function (e) {
            var knop = e.target.closest("[data-vz-opvragen]");
            if (knop) { e.preventDefault(); toonMails([knop.getAttribute("data-vz-opvragen")]); }
        });
        var klaar = byId("gln-vz-mails-klaar");
        if (klaar) klaar.addEventListener("click", function () { var m = modalOf("gln-vz-mails-modal"); if (m) m.hide(); if (mailsGedaan) window.location.reload(); });
        var mm = byId("gln-vz-mails-modal");
        if (mm) mm.addEventListener("hidden.bs.modal", function () { if (mailsGedaan) window.location.reload(); });
    }

    // ── Formulier (43c) ─────────────────────────────────────────────────────────────────────────
    var form = byId("gln-vz-form");
    if (form) {
        function typeWaarde() { var r = q('input[name="Input.Type"]:checked', form); return r ? r.value : "1"; }
        function gebouwWaarde() { var r = q('input[name="Input.Building"]:checked', form); return r ? r.value : "1"; }
        var voTekst = form.getAttribute("data-vo-tekst") || "";
        var startVo = byId("gln-vz-startvo");
        var startHidden = byId("gln-vz-start"), startText = byId("gln-vz-start_text");
        var bewaardeStart = startText ? startText.value : "";

        var LOSTOP = {
            "1": ["Verzekerde waarde ABR lager dan het definitieve budget", "ABR-termijn loopt af of is vervallen"],
            "2n": ["Brandverzekering nieuwbouw nog niet afgesloten"],
            "3": ["Projectpolis tienjarige ontbreekt, of eenheden zonder tienjarige polis (contracten met Via projectpolis)"]
        };

        function toonVelden() {
            var t = typeWaarde();
            qa("[data-vz-types]", form).forEach(function (el) {
                var types = el.getAttribute("data-vz-types").split(" ");
                el.hidden = types.indexOf(t) < 0;
            });
            var lijst = byId("gln-vz-losop-lijst"), kaart = byId("gln-vz-losop");
            var sleutel = t === "2" ? (gebouwWaarde() === "0" ? "" : "2n") : t;
            var items = LOSTOP[sleutel] || [];
            if (lijst) { lijst.innerHTML = ""; items.forEach(function (tekst) { var li = document.createElement("li"); li.textContent = tekst; lijst.appendChild(li); }); }
            if (kaart) kaart.hidden = items.length === 0;
            var hint = q("[data-vz-nieuwbouw-hint]", form);
            if (hint) hint.hidden = gebouwWaarde() === "0";
            zetStartBijVo();
        }

        function zetStartBijVo() {
            if (!startVo || !startText || !startHidden) return;
            var aan = startVo.checked && !startVo.closest("[data-vz-types]").hidden;
            if (aan) {
                if (!startText.disabled) bewaardeStart = startText.value;
                startText.value = voTekst;
                startText.disabled = true;
                startHidden.disabled = true;
            } else if (startText.disabled) {
                startText.disabled = false;
                startHidden.disabled = false;
                startText.value = bewaardeStart;
                startHidden.dispatchEvent(new Event("change", { bubbles: true }));
            }
        }

        qa('input[name="Input.Type"]', form).forEach(function (r) {
            r.addEventListener("change", function () {
                // Brandverzekering voor nieuwbouw start standaard bij de voorlopige oplevering
                if (typeWaarde() === "2" && gebouwWaarde() === "1" && startVo && !startVo.checked && !(startHidden && startHidden.value) && form.getAttribute("data-vo")) startVo.checked = true;
                toonVelden();
            });
        });
        qa('input[name="Input.Building"]', form).forEach(function (r) {
            r.addEventListener("change", function () {
                if (gebouwWaarde() === "1" && startVo && !startVo.checked && !(startHidden && startHidden.value) && form.getAttribute("data-vo")) startVo.checked = true;
                toonVelden();
            });
        });
        if (startVo) startVo.addEventListener("change", zetStartBijVo);
        var eenhedenAlles = q("[data-vz-eenheden-alles]", form);
        if (eenhedenAlles) eenhedenAlles.addEventListener("click", function (e) {
            e.preventDefault();
            var boxen = qa('input[name="Input.UnitIds"]', form);
            var allemaal = boxen.every(function (c) { return c.checked; });
            boxen.forEach(function (c) { c.checked = !allemaal; });
        });
        toonVelden();

        // Bewaren als aangevraagd of gewoon opslaan
        var aangevraagd = byId("gln-vz-aangevraagd"), requested = byId("gln-vz-requested");
        if (aangevraagd) aangevraagd.addEventListener("click", function () { requested.value = "true"; });
        qa('.gl-v2-form-actionbar button[type="submit"]', form).forEach(function (b) {
            if (b !== aangevraagd) b.addEventListener("click", function () { requested.value = "false"; });
        });

        // Dropzone voor het polisdocument
        var zone = byId("gln-vz-dropzone"), fileInput = byId("gln-vz-polisfile"), zoneTekst = byId("gln-vz-dropzone-tekst");
        function toonBestand(f) {
            if (!zone || !f) return;
            zone.classList.add("heeft-bestand");
            zoneTekst.textContent = f.name + " (" + (f.size >= 1048576 ? (f.size / 1048576).toFixed(1).replace(".", ",") + " MB" : Math.max(1, Math.round(f.size / 1024)) + " kB") + ") wordt bewaard in Documenten. Of";
        }
        if (fileInput) fileInput.addEventListener("change", function () { if (fileInput.files && fileInput.files[0]) toonBestand(fileInput.files[0]); });
        if (zone && fileInput) {
            ["dragenter", "dragover"].forEach(function (ev) { zone.addEventListener(ev, function (e) { e.preventDefault(); zone.classList.add("is-dragover"); }); });
            ["dragleave", "drop"].forEach(function (ev) { zone.addEventListener(ev, function (e) { e.preventDefault(); zone.classList.remove("is-dragover"); }); });
            zone.addEventListener("drop", function (e) {
                var f = e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0];
                if (!f) return;
                try { var dt = new DataTransfer(); dt.items.add(f); fileInput.files = dt.files; } catch (err) { /* oudere browsers: enkel via klik */ }
                toonBestand(f);
            });
        }
    }
})();
