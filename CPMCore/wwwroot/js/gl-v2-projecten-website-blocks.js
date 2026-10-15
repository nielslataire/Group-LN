// Projecten/EditV2 — tab "SEO & website": volgorde + zichtbaarheid van de blokken, en de herhaalbare lijsten
// (verhaalbeelden, quotes, details). De positie in een lijst wordt de volgorde op de site; namen
// (Website.<Lijst>[i].…) worden na elke wijziging hernummerd voor de modelbinding.
(function () {
    "use strict";
    var changed = function (el) { el.dispatchEvent(new Event("input", { bubbles: true })); };

    // ── Blokken: volgorde + aan/uit ───────────────────────────────────────────────────────────
    var list = document.getElementById("webBlocksList");
    var json = document.getElementById("Website_BlocksJson");
    if (list && json) {
        var rows = function () { return Array.prototype.slice.call(list.querySelectorAll(".web-block-row")); };
        var sync = function (notify) {
            var rs = rows();
            json.value = JSON.stringify(rs.map(function (r) {
                return { key: r.getAttribute("data-key"), visible: r.querySelector(".web-block-visible").checked };
            }));
            rs.forEach(function (r, i) {
                r.querySelector(".web-block-up").disabled = i === 0;
                r.querySelector(".web-block-down").disabled = i === rs.length - 1;
            });
            if (notify) changed(json);
        };
        list.addEventListener("click", function (e) {
            var mv = e.target.closest(".web-block-up, .web-block-down");
            if (!mv || mv.disabled) return;
            var row = mv.closest(".web-block-row"), up = mv.classList.contains("web-block-up");
            if (up && row.previousElementSibling) list.insertBefore(row, row.previousElementSibling);
            else if (!up && row.nextElementSibling) list.insertBefore(row.nextElementSibling, row);
            sync(true);
            var again = row.querySelector(up ? ".web-block-up" : ".web-block-down");
            if (again && !again.disabled) again.focus();
        });
        list.addEventListener("change", function (e) { if (e.target.classList.contains("web-block-visible")) sync(true); });
        // slepen via het handvat
        var drag = null;
        list.addEventListener("mousedown", function (e) {
            var r = e.target.closest(".web-block-row");
            if (r) r.draggable = !!e.target.closest(".gl-v2-pf-handle");
        });
        list.addEventListener("dragstart", function (e) {
            drag = e.target.closest ? e.target.closest(".web-block-row") : null;
            if (drag) { e.dataTransfer.effectAllowed = "move"; try { e.dataTransfer.setData("text/plain", "b"); } catch (x) { } }
        });
        list.addEventListener("dragover", function (e) {
            if (!drag) return;
            e.preventDefault();
            var over = e.target.closest(".web-block-row");
            if (!over || over === drag) return;
            var rect = over.getBoundingClientRect();
            list.insertBefore(drag, (e.clientY - rect.top) > rect.height / 2 ? over.nextElementSibling : over);
        });
        list.addEventListener("dragend", function () { if (drag) { drag.draggable = false; drag = null; sync(true); } });
        sync(false);
    }

    // ── Vragen: opgemaakt antwoord (Quill, zelfde werkbalk als de commerciële tekst) ──────────────────
    // De verborgen textarea .web-faq-source bevat de HTML die mee gepost wordt; de service schoont ze op (enkel eenvoudige opmaak + links).
    function initFaqEditors(root) {
        if (typeof Quill === "undefined") return;
        (root || document).querySelectorAll(".web-faq-editor").forEach(function (box) {
            if (box.__quill) return;
            var source = box.parentNode.querySelector(".web-faq-source");
            var quill = new Quill(box, {
                theme: "snow",
                placeholder: box.getAttribute("data-placeholder") || "",
                modules: { toolbar: [["bold", "italic", "underline"], [{ list: "ordered" }, { list: "bullet" }], ["link"], ["clean"]] }
            });
            box.__quill = quill;
            if (source.value) quill.clipboard.dangerouslyPasteHTML(source.value, "silent");
            quill.on("text-change", function (delta, old, src) {
                var empty = quill.getText().trim().length === 0 && !quill.root.querySelector("img");
                source.value = empty ? "" : quill.root.innerHTML;
                if (src === "user") source.dispatchEvent(new Event("input", { bubbles: true }));
            });
        });
    }

    // ── Herhaalbare lijsten ──────────────────────────────────────────────────────────────────
    var pop = document.getElementById("webPhotoPop");
    var popTarget = null;
    var imageBase = (document.getElementById("websiteBlocksCard") || {}).getAttribute ? document.getElementById("websiteBlocksCard").getAttribute("data-image-base") : "";

    document.querySelectorAll("[data-repeat]").forEach(function (box) {
        var prefix = box.getAttribute("data-prefix");
        var listEl = box.querySelector(".web-rep-list");
        var tpl = box.querySelector("template.web-rep-tpl");
        var re = new RegExp("^" + prefix.replace(/[.\[\]]/g, "\\$&") + "\\[(\\d+|__i__)\\]");
        function repRows() { return Array.prototype.slice.call(listEl.querySelectorAll(".web-rep-row")); }
        function renumber() {
            var rs = repRows();
            rs.forEach(function (row, i) {
                row.querySelectorAll("[name^='" + prefix + "[']").forEach(function (inp) { inp.name = inp.name.replace(re, prefix + "[" + i + "]"); });
                row.querySelector(".web-rep-up").disabled = i === 0;
                row.querySelector(".web-rep-down").disabled = i === rs.length - 1;
            });
        }
        renumber();

        box.querySelector("[data-rep-add]").addEventListener("click", function () {
            var holder = document.createElement("div");
            holder.innerHTML = tpl.innerHTML.replace(/__i__/g, String(repRows().length));
            var row = holder.firstElementChild;
            listEl.appendChild(row);
            renumber();
            initFaqEditors(row);
            var f = row.querySelector("textarea:not([hidden]), input[type=text]");
            if (f) f.focus();
            changed(listEl);
        });
        listEl.addEventListener("click", function (e) {
            var row = e.target.closest(".web-rep-row");
            if (!row) return;
            if (e.target.closest(".web-rep-remove")) { row.remove(); renumber(); changed(listEl); return; }
            var mv = e.target.closest(".web-rep-up, .web-rep-down");
            if (mv && !mv.disabled) {
                var up = mv.classList.contains("web-rep-up");
                if (up && row.previousElementSibling) listEl.insertBefore(row, row.previousElementSibling);
                else if (!up && row.nextElementSibling) listEl.insertBefore(row.nextElementSibling, row);
                renumber(); changed(listEl);
                var again = row.querySelector(up ? ".web-rep-up" : ".web-rep-down");
                if (again && !again.disabled) again.focus();
                return;
            }
            var pick = e.target.closest(".web-pick");
            if (pick) { openPop(pick); }
        });
        var drag = null;
        listEl.addEventListener("mousedown", function (e) {
            var r = e.target.closest(".web-rep-row");
            if (r) r.draggable = !!e.target.closest(".gl-v2-pf-handle");
        });
        listEl.addEventListener("dragstart", function (e) {
            drag = e.target.closest ? e.target.closest(".web-rep-row") : null;
            if (drag) { e.dataTransfer.effectAllowed = "move"; try { e.dataTransfer.setData("text/plain", "r"); } catch (x) { } }
        });
        listEl.addEventListener("dragover", function (e) {
            if (!drag) return;
            e.preventDefault();
            var over = e.target.closest(".web-rep-row");
            if (!over || over === drag) return;
            var rect = over.getBoundingClientRect();
            listEl.insertBefore(drag, (e.clientY - rect.top) > rect.height / 2 ? over.nextElementSibling : over);
        });
        listEl.addEventListener("dragend", function () { if (drag) { drag.draggable = false; drag = null; renumber(); changed(listEl); } });
    });

    initFaqEditors(document);

    // ── Ligging: kaartpunt via adres + voorzieningen voorstellen ────────────────────────────────────
    (function () {
        var card = document.getElementById("websiteLocationCard");
        if (!card) return;
        var token = function () { var t = document.querySelector("input[name='__RequestVerificationToken']"); return t ? t.value : ""; };
        var latInput = document.getElementById("Website_LocationLat"), lngInput = document.getElementById("Website_LocationLng");
        var say = function (title, description) { if (window.GlV2Dialog) GlV2Dialog.alert({ tone: "danger", icon: "ph-warning-circle", title: title, description: description }); else alert(description); };
        function post(url, data) {
            var body = new URLSearchParams(data);
            return fetch(url, { method: "POST", headers: { "RequestVerificationToken": token(), "Content-Type": "application/x-www-form-urlencoded" }, body: body }).then(function (r) { return r.json(); });
        }
        function busy(btn, on) { btn.disabled = on; btn.classList.toggle("is-loading", on); }

        var geoBtn = document.getElementById("webGeoBtn");
        if (geoBtn) geoBtn.addEventListener("click", function () {
            busy(geoBtn, true);
            post(card.getAttribute("data-geocode-url"), { projectId: card.getAttribute("data-project-id") }).then(function (res) {
                if (!res.ok) { say("Adres niet gevonden", res.error); return; }
                latInput.value = res.lat; lngInput.value = res.lng;
                latInput.dispatchEvent(new Event("input", { bubbles: true }));
                document.getElementById("webGeoHelp").textContent = "Gevonden: " + (res.label || (res.lat + ", " + res.lng)) + " — controleer of dit klopt.";
            }).catch(function () { say("Zoeken mislukt", "De opzoeking kon niet uitgevoerd worden. Probeer het opnieuw."); }).then(function () { busy(geoBtn, false); });
        });

        var poiBtn = document.getElementById("webPoiSuggestBtn");
        if (poiBtn) poiBtn.addEventListener("click", function () {
            if (!latInput.value.trim() || !lngInput.value.trim()) { say("Kaartpunt ontbreekt", "Vul eerst het kaartpunt van het project in of klik op “Zoek via adres”."); return; }
            busy(poiBtn, true);
            post(card.getAttribute("data-poi-url"), { lat: latInput.value, lng: lngInput.value }).then(function (res) {
                if (!res.ok) { say("Voorzieningen ophalen mislukt", res.error); return; }
                var box = document.getElementById("webPoiBox");
                var rows = function () { return Array.prototype.slice.call(box.querySelectorAll(".web-rep-row")); };
                var have = {};
                rows().forEach(function (r) {
                    var c = r.querySelector("select"), n = r.querySelector("input[name$='.Name']");
                    if (c && n) have[c.value + "|" + n.value.trim().toLowerCase()] = true;
                });
                var added = 0;
                res.items.forEach(function (it) {
                    if (have[it.category + "|" + it.name.toLowerCase()]) return;
                    box.querySelector("[data-rep-add]").click();
                    var row = rows()[rows().length - 1];
                    row.querySelector("select").value = it.category;
                    row.querySelector("input[name$='.Name']").value = it.name;
                    row.querySelector("input[name$='.Distance']").value = it.distance;
                    row.querySelector("input[name$='.Time']").value = it.time;
                    row.querySelector("input[name$='.Lat']").value = it.lat;
                    row.querySelector("input[name$='.Lng']").value = it.lng;
                    added++;
                });
                document.getElementById("webPoiHelp").textContent = added + " voorziening(en) toegevoegd" + (res.items.length - added ? " (" + (res.items.length - added) + " stonden er al)" : "") + ". Controleer ze, pas aan of verwijder wat niet past.";
            }).catch(function () { say("Voorzieningen ophalen mislukt", "De opzoeking kon niet uitgevoerd worden. Probeer het opnieuw."); }).then(function () { busy(poiBtn, false); });
        });
    })();

    // ── Foto kiezen (één gedeeld venstertje) ──────────────────────────────────────────────────
    function openPop(btn) {
        if (!pop) return;
        if (popTarget === btn && !pop.hidden) { closePop(); return; }
        popTarget = btn;
        var r = btn.getBoundingClientRect();
        pop.hidden = false;
        var w = pop.offsetWidth, h = pop.offsetHeight;
        var left = Math.min(Math.max(8, r.left), window.innerWidth - w - 8);
        var top = r.bottom + 6;
        if (top + h > window.innerHeight - 8) top = Math.max(8, r.top - h - 6);
        pop.style.left = left + "px"; pop.style.top = top + "px";
    }
    function closePop() { if (pop) pop.hidden = true; popTarget = null; }
    function setPhoto(name) {
        if (!popTarget) return;
        var cell = popTarget.closest(".web-pick-cell");
        var input = cell.querySelector("input[type=hidden]");
        var img = popTarget.querySelector("img");
        input.value = name || "";
        if (name) { img.src = imageBase + "pictures/447/" + name; img.hidden = false; popTarget.classList.remove("is-empty"); }
        else { img.removeAttribute("src"); img.hidden = true; popTarget.classList.add("is-empty"); }
        changed(input);
        closePop();
    }
    if (pop) {
        pop.addEventListener("click", function (e) {
            var opt = e.target.closest(".web-photo-opt");
            if (opt) { setPhoto(opt.getAttribute("data-name")); return; }
            if (e.target.closest(".web-photo-none")) setPhoto("");
        });
        document.addEventListener("click", function (e) {
            if (pop.hidden) return;
            if (e.target.closest("#webPhotoPop") || e.target.closest(".web-pick")) return;
            closePop();
        });
        document.addEventListener("keydown", function (e) { if (e.key === "Escape") closePop(); });
        window.addEventListener("scroll", closePop, true);
    }
})();
