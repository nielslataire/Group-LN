// gl-v2 — Marktanalyse › Vergelijkbare panden (design-handoff punt 46b/46c). Enkel geladen door Views/Marktanalyse/VergelijkbarePandenV2.cshtml,
// dat de gegevens klaarzet in window.glV2VpConfig ({ gemeenten, geselecteerd, criteria, initLat, initLng, urls }).
// Zelfde gedrag als het oude scherm: zoekgebied met gemeenten of een punt met straal op een kaart, meteen zoeken bij type/status/gemeente,
// profiel opslaan via fetch, en de resultatentabel met een eigen status- en zoekfilter.
(function () {
    "use strict";

    var cfg = window.glV2VpConfig;
    var form = document.getElementById("vpForm");
    if (!cfg || !form) return;

    var $ = window.GlV2.byId; // gedeeld (gl-v2-shell.js)
    var toast = (window.GlV2Toast && window.GlV2Toast.notify) || function () {}; // gedeeld, zie gl-v2-shell.js

    // ── Laadsluier bij elke zoekopdracht ─────────────────────────────────────────────────────────────────────────────────────
    var overlay = $("vpLoadingOverlay");
    form.addEventListener("submit", function () { if (overlay) overlay.classList.add("show"); });
    // Terug-knop van de browser toont de pagina uit de cache: sluier dan weer weg.
    window.addEventListener("pageshow", function () { if (overlay) overlay.classList.remove("show"); });

    // Type en status passen meteen toe (net als de gemeente).
    form.querySelectorAll('input[name="type"], input[name="status"]').forEach(function (radio) {
        radio.addEventListener("change", function () { form.requestSubmit(); });
    });

    // ── Profiel opslaan ──────────────────────────────────────────────────────────────────────────────────────────────────────
    var saveBtn = $("vpProfileOpslaanConfirm");
    var naamInput = $("vpProfileNaam");
    var naamErr = $("vpProfileNaamErr");
    function naamFout(show) {
        if (naamErr) naamErr.hidden = !show;
        if (naamInput) naamInput.closest(".gl-v2-field-box").classList.toggle("is-error", show);
    }
    if (saveBtn) {
        saveBtn.addEventListener("click", function () {
            var naam = (naamInput.value || "").trim();
            if (!naam) { naamFout(true); naamInput.focus(); return; }
            naamFout(false);

            var c = cfg.criteria;
            var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
            var body = new URLSearchParams();
            body.append("__RequestVerificationToken", tokenEl ? tokenEl.value : "");
            body.append("Naam", naam);
            body.append("ZoekgebiedTab", c.ZoekgebiedTab || "");
            (c.GemeenteIds || []).forEach(function (id) { body.append("GemeenteIds", id); });
            body.append("RondAdresPostcode", c.RondAdresPostcode || "");
            if (c.RondAdresLat != null) body.append("RondAdresLat", c.RondAdresLat);
            if (c.RondAdresLng != null) body.append("RondAdresLng", c.RondAdresLng);
            body.append("RondAdresStraal", c.RondAdresStraal);
            body.append("Type", c.Type || "");
            if (c.Oppervlakte != null) body.append("Oppervlakte", c.Oppervlakte);
            body.append("Tolerantie", c.Tolerantie);
            if (c.PrijsMin != null) body.append("PrijsMin", c.PrijsMin);
            if (c.PrijsMax != null) body.append("PrijsMax", c.PrijsMax);
            if (c.Slaapkamers != null) body.append("Slaapkamers", c.Slaapkamers);
            body.append("Status", c.Status || "");

            saveBtn.disabled = true;
            fetch(cfg.urls.saveProfile, { method: "POST", body: body, headers: { "X-Requested-With": "XMLHttpRequest" } })
                .then(function (resp) { return resp.json().then(function (data) { return { ok: resp.ok, data: data }; }); })
                .then(function (result) {
                    saveBtn.disabled = false;
                    if (result.ok && result.data && result.data.success) {
                        var modalEl = $("vpSaveProfileModal");
                        if (window.bootstrap && modalEl) window.bootstrap.Modal.getOrCreateInstance(modalEl).hide();
                        naamInput.value = "";
                        toast("success", "Profiel opgeslagen", "Zoekprofiel “" + naam + "” is opgeslagen.");
                    } else {
                        naamFout(true);
                        toast("danger", "Opslaan mislukt", (result.data && result.data.message) || "Het profiel kon niet opgeslagen worden.");
                    }
                })
                .catch(function () {
                    saveBtn.disabled = false;
                    toast("danger", "Opslaan mislukt", "Het profiel kon niet opgeslagen worden.");
                });
        });
        naamInput.addEventListener("input", function () { naamFout(false); });
        naamInput.addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); saveBtn.click(); } });
        var modalEl = $("vpSaveProfileModal");
        if (modalEl) modalEl.addEventListener("shown.bs.modal", function () { naamInput.focus(); });
    }

    // ── Zoekgebied: gemeenten of rond een adres ───────────────────────────────────────────────────────────────────────────────
    var tabGem = $("tabGemeenten"), tabRond = $("tabRondAdres");
    var panGem = $("panelGemeenten"), panRond = $("panelRondAdres"), hidTab = $("hidZoekgebiedTab");
    function toonTab(rond) {
        tabGem.classList.toggle("is-active", !rond); tabGem.setAttribute("aria-pressed", rond ? "false" : "true");
        tabRond.classList.toggle("is-active", rond); tabRond.setAttribute("aria-pressed", rond ? "true" : "false");
        panGem.hidden = rond; panRond.hidden = !rond;
        hidTab.value = rond ? "RondAdres" : "Gemeenten";
    }
    tabGem.addEventListener("click", function () { toonTab(false); });
    tabRond.addEventListener("click", function () { toonTab(true); });

    // ── Oppervlakte en tolerantie ─────────────────────────────────────────────────────────────────────────────────────────────
    var oppInput = $("oppervlakte"), tolSlider = $("tolerantieSlider"), tolLabel = $("tolLabel"), oppRangeLbl = $("oppRangeLabel");
    function updateOppRange() {
        var opp = parseFloat(oppInput.value) || 0;
        var tol = parseInt(tolSlider.value, 10) || 0;
        tolLabel.textContent = "± " + tol + " m²";
        if (opp > 0) {
            oppRangeLbl.hidden = false;
            oppRangeLbl.textContent = "zoekt tussen " + Math.max(0, opp - tol) + " en " + (opp + tol) + " m²";
        } else {
            oppRangeLbl.hidden = true;
        }
    }
    oppInput.addEventListener("input", updateOppRange);
    tolSlider.addEventListener("input", updateOppRange);

    // ── Gemeenten: meerdere kiezen, met het aantal panden ────────────────────────────────────────────────────────────────────
    var gemeenten = cfg.gemeenten || [];
    var geselecteerd = new Set(cfg.geselecteerd || []);
    var gemBtn = $("gemBtn"), gemDropdown = $("gemDropdown"), gemBtnLabel = $("gemBtnLabel"), gemSearch = $("gemSearch"), gemList = $("gemList"), gemWissen = $("gemWissen");
    var hiddenInputs = [];

    function esc(s) { return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;"); }

    function syncHiddenInputs() {
        hiddenInputs.forEach(function (el) { if (el.parentNode) el.parentNode.removeChild(el); });
        hiddenInputs = [];
        geselecteerd.forEach(function (id) {
            var inp = document.createElement("input");
            inp.type = "hidden"; inp.name = "gemeenteIds"; inp.value = id;
            form.appendChild(inp);
            hiddenInputs.push(inp);
        });
    }
    function updateGemLabel() {
        var tekst, filled = geselecteerd.size > 0;
        if (!filled) tekst = "Kies een gemeente";
        else if (geselecteerd.size === 1) {
            var g = gemeenten.find(function (x) { return x.id === Array.from(geselecteerd)[0]; });
            tekst = g ? g.naam : "1 gemeente";
        } else tekst = geselecteerd.size + " gemeenten geselecteerd";
        gemBtnLabel.textContent = tekst;
        gemBtnLabel.classList.toggle("is-placeholder", !filled);
        gemBtn.classList.toggle("is-filled", filled);
    }
    function renderGemeenteList(filter) {
        var term = (filter || "").toLowerCase();
        var html = "";
        gemeenten.forEach(function (g) {
            if (term && g.naam.toLowerCase().indexOf(term) === -1) return;
            html += '<div class="gl-v2-vp-gem-row"><input type="checkbox" id="gem_' + g.id + '" value="' + g.id + '"' + (geselecteerd.has(g.id) ? " checked" : "") + ' />' +
                '<label for="gem_' + g.id + '">' + esc(g.naam) + '</label><span class="gl-v2-vp-gem-count">' + (g.aantal || 0) + "</span></div>";
        });
        gemList.innerHTML = html || '<div class="gl-v2-vp-panel-head"><span>Geen gemeenten gevonden</span></div>';
        syncHiddenInputs();
    }
    function setOpen(btn, panel, open) {
        panel.classList.toggle("open", open);
        btn.classList.toggle("open", open);
        btn.setAttribute("aria-expanded", open ? "true" : "false");
    }
    gemList.addEventListener("change", function (e) {
        var cb = e.target;
        if (!cb || cb.type !== "checkbox") return;
        var id = parseInt(cb.value, 10);
        if (cb.checked) geselecteerd.add(id); else geselecteerd.delete(id);
        updateGemLabel();
        syncHiddenInputs();
        form.requestSubmit();
    });
    gemBtn.addEventListener("click", function (e) {
        e.stopPropagation();
        var open = !gemDropdown.classList.contains("open");
        setOpen(gemBtn, gemDropdown, open);
        if (open) { renderGemeenteList(""); gemSearch.value = ""; gemSearch.focus(); }
    });
    gemSearch.addEventListener("input", function () { renderGemeenteList(this.value); });
    gemSearch.addEventListener("keydown", function (e) { if (e.key === "Enter") e.preventDefault(); });
    function wissen() {
        var had = geselecteerd.size > 0;
        geselecteerd.clear();
        renderGemeenteList(gemSearch.value);
        updateGemLabel();
        if (had) form.requestSubmit();
    }
    gemWissen.addEventListener("click", wissen);
    gemWissen.addEventListener("keydown", function (e) { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); wissen(); } });

    // ── Rond adres: middelpunt en straal op een kaart ────────────────────────────────────────────────────────────────────────
    var rond = (function () {
        var btn = $("rondAdresBtn"), btnLabel = $("rondAdresBtnLabel"), dropdown = $("rondAdresDropdown"), searchInp = $("rondAdresSearch");
        var straalInp = $("rondAdresStraal"), straalLbl = $("straalLabel"), countBadge = $("rondAdresCount"), mapDiv = $("rondAdresMap");
        var latHidden = $("rondAdresLat"), lngHidden = $("rondAdresLng");
        if (!btn || !mapDiv) return null;

        var BELGIE_LAT = 50.85, BELGIE_LNG = 4.35, BELGIE_ZOOM = 7, GROEN = "#00532D";
        var map, circle, centerMarker, handleMarker;
        var huidigeLat = cfg.initLat !== null ? cfg.initLat : BELGIE_LAT;
        var huidigeLng = cfg.initLng !== null ? cfg.initLng : BELGIE_LNG;
        var puntGekozen = cfg.initLat !== null;
        var gewijzigd = false;

        function haversine(lat1, lng1, lat2, lng2) {
            var R = 6371000, dLat = (lat2 - lat1) * Math.PI / 180, dLng = (lng2 - lng1) * Math.PI / 180;
            var a = Math.sin(dLat / 2) * Math.sin(dLat / 2) + Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.sin(dLng / 2) * Math.sin(dLng / 2);
            return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        }
        function destination(lat, lng, distM, bearingDeg) {
            var R = 6371000, brng = bearingDeg * Math.PI / 180, lat1 = lat * Math.PI / 180, lng1 = lng * Math.PI / 180;
            var lat2 = Math.asin(Math.sin(lat1) * Math.cos(distM / R) + Math.cos(lat1) * Math.sin(distM / R) * Math.cos(brng));
            var lng2 = lng1 + Math.atan2(Math.sin(brng) * Math.sin(distM / R) * Math.cos(lat1), Math.cos(distM / R) - Math.sin(lat1) * Math.sin(lat2));
            return { lat: lat2 * 180 / Math.PI, lng: lng2 * 180 / Math.PI };
        }
        function dot(size, fill, border) {
            return L.divIcon({
                className: "",
                html: '<div style="width:' + size + "px;height:" + size + "px;border-radius:50%;background:" + fill + ";border:2px solid " + border + ';box-shadow:0 1px 4px rgba(0,0,0,.35)"></div>',
                iconSize: [size, size], iconAnchor: [size / 2, size / 2]
            });
        }
        function fmtKm(m) { var km = m / 1000; return (km < 1 ? km.toFixed(2) : km.toFixed(1)).replace(/\.?0+$/, "").replace(".", ",") + " km"; }
        function updateLabel() {
            straalLbl.textContent = fmtKm(parseInt(straalInp.value, 10));
            if (puntGekozen) {
                btnLabel.textContent = "Eigen punt (" + huidigeLat.toFixed(3) + ", " + huidigeLng.toFixed(3) + ") · " + fmtKm(parseInt(straalInp.value, 10));
                btnLabel.classList.remove("is-placeholder");
                btn.classList.add("is-filled");
            }
        }
        function syncHidden() { latHidden.value = puntGekozen ? huidigeLat : ""; lngHidden.value = puntGekozen ? huidigeLng : ""; }

        var countTimer = null;
        function telling() {
            if (countTimer) clearTimeout(countTimer);
            if (!puntGekozen) { countBadge.textContent = "0"; return; }
            countTimer = setTimeout(function () {
                var params = new URLSearchParams({
                    lat: huidigeLat, lng: huidigeLng, straal: straalInp.value,
                    type: (document.querySelector('input[name="type"]:checked') || {}).value || "Appartement",
                    status: (document.querySelector('input[name="status"]:checked') || {}).value || "Alles"
                });
                if (oppInput.value) { params.set("oppervlakte", oppInput.value); params.set("tolerantie", tolSlider.value); }
                var slaap = document.querySelector('input[name="slaapkamers"]:checked');
                if (slaap && slaap.value) params.set("slaapkamers", slaap.value);
                var pMin = document.querySelector('input[name="prijsMin"]'), pMax = document.querySelector('input[name="prijsMax"]');
                if (pMin && pMin.value) params.set("prijsMin", pMin.value);
                if (pMax && pMax.value) params.set("prijsMax", pMax.value);
                fetch(cfg.urls.count + "?" + params.toString())
                    .then(function (r) { return r.json(); })
                    .then(function (d) { countBadge.textContent = d.aantal; })
                    .catch(function () { /* de telling is een hulpmiddel, geen vereiste */ });
            }, 300);
        }

        function initMap() {
            if (map) { map.invalidateSize(); return; }
            map = L.map(mapDiv, { zoomControl: true }).setView([huidigeLat, huidigeLng], BELGIE_ZOOM);
            map.attributionControl.setPrefix(false);
            L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", { attribution: "&copy; OpenStreetMap-bijdragers", maxZoom: 19 }).addTo(map);
            circle = L.circle([huidigeLat, huidigeLng], { radius: parseInt(straalInp.value, 10), color: GROEN, weight: 2, dashArray: "4 6", fillColor: GROEN, fillOpacity: 0.08 }).addTo(map);
            centerMarker = L.marker([huidigeLat, huidigeLng], { draggable: true, icon: dot(16, GROEN, "#fff") }).addTo(map);
            var hp = destination(huidigeLat, huidigeLng, parseInt(straalInp.value, 10), 90);
            handleMarker = L.marker([hp.lat, hp.lng], { draggable: true, icon: dot(12, "#fff", GROEN) }).addTo(map);

            centerMarker.on("drag", function (e) {
                var p = e.target.getLatLng();
                circle.setLatLng(p);
                var h = destination(p.lat, p.lng, circle.getRadius(), 90);
                handleMarker.setLatLng([h.lat, h.lng]);
            });
            centerMarker.on("dragend", function (e) { zetPunt(e.target.getLatLng().lat, e.target.getLatLng().lng, true); });
            handleMarker.on("drag", function (e) {
                var r = haversine(huidigeLat, huidigeLng, e.target.getLatLng().lat, e.target.getLatLng().lng);
                r = Math.min(25000, Math.max(250, Math.round(r / 250) * 250));
                circle.setRadius(r); straalInp.value = r; updateLabel();
            });
            handleMarker.on("dragend", function () { syncHidden(); telling(); });
            map.on("click", function (e) { zetPunt(e.latlng.lat, e.latlng.lng, true); });
            if (puntGekozen) map.fitBounds(circle.getBounds(), { padding: [24, 24] });
        }
        function zetPunt(lat, lng, kaartActie) {
            huidigeLat = lat; huidigeLng = lng; puntGekozen = true; gewijzigd = true;
            if (map) {
                circle.setLatLng([lat, lng]); centerMarker.setLatLng([lat, lng]);
                var h = destination(lat, lng, circle.getRadius(), 90);
                handleMarker.setLatLng([h.lat, h.lng]);
                if (kaartActie === "fit") map.fitBounds(circle.getBounds(), { padding: [24, 24] });
                else if (kaartActie) map.panTo([lat, lng]);
            }
            updateLabel(); syncHidden(); telling();
        }

        straalInp.addEventListener("input", function () {
            var r = parseInt(this.value, 10);
            if (circle) {
                circle.setRadius(r);
                var h = destination(huidigeLat, huidigeLng, r, 90);
                if (handleMarker) handleMarker.setLatLng([h.lat, h.lng]);
            }
            updateLabel(); syncHidden(); telling();
        });

        var geoTimer = null;
        searchInp.addEventListener("input", function () {
            var q = this.value.trim();
            if (geoTimer) clearTimeout(geoTimer);
            if (q.length < 3) return;
            geoTimer = setTimeout(function () {
                fetch(cfg.urls.geocode + "?query=" + encodeURIComponent(q))
                    .then(function (r) { return r.json(); })
                    .then(function (d) { if (d && d.success) zetPunt(d.lat, d.lng, "fit"); })
                    .catch(function () { /* de gebruiker kan het punt ook verslepen */ });
            }, 450);
        });
        searchInp.addEventListener("keydown", function (e) { if (e.key === "Enter") e.preventDefault(); });

        btn.addEventListener("click", function (e) {
            e.stopPropagation();
            var open = !dropdown.classList.contains("open");
            setOpen(btn, dropdown, open);
            if (open) { initMap(); telling(); }
        });

        updateLabel(); syncHidden();
        return {
            contains: function (t) { return btn.contains(t) || dropdown.contains(t); },
            // Sluiten toont meteen de resultaten voor het nieuwe punt, net als een gemeente kiezen.
            close: function () {
                if (!dropdown.classList.contains("open")) return;
                setOpen(btn, dropdown, false);
                if (gewijzigd && puntGekozen) { gewijzigd = false; form.requestSubmit(); }
            }
        };
    })();

    document.addEventListener("click", function (e) {
        if (!gemBtn.contains(e.target) && !gemDropdown.contains(e.target)) setOpen(gemBtn, gemDropdown, false);
        if (rond && !rond.contains(e.target)) rond.close();
    });
    document.addEventListener("keydown", function (e) {
        if (e.key !== "Escape") return;
        setOpen(gemBtn, gemDropdown, false);
        if (rond) rond.close();
    });

    renderGemeenteList("");
    updateGemLabel();

    // ── Resultatentabel: status- en zoekfilter ────────────────────────────────────────────────────────────────────────────────
    var tableEl = $("tblVergelijkbaar");
    if (tableEl && typeof DataTable !== "undefined") {
        var statusCol = parseInt(tableEl.getAttribute("data-col-status"), 10);
        var priceCol = parseInt(tableEl.getAttribute("data-col-price"), 10);
        var lastCol = parseInt(tableEl.getAttribute("data-col-last"), 10);
        var table = new DataTable(tableEl, {
            order: [[priceCol, "asc"]],
            pageLength: 15,
            layout: { topStart: null, topEnd: null, bottomStart: "info", bottomEnd: "paging" },
            columnDefs: [{ className: "is-num", targets: [3, 4, 5, 6] }],
            language: {
                info: "_START_ tot _END_ van _TOTAL_",
                infoEmpty: "Geen resultaten",
                infoFiltered: "(gefilterd uit _MAX_)",
                zeroRecords: "Niets gevonden voor deze zoekterm.",
                paginate: { previous: "‹", next: "›" }
            }
        });
        void lastCol;

        var search = $("vpSearch");
        if (search) search.addEventListener("input", function () { table.search(this.value).draw(); });

        var filter = $("vpStatusFilter");
        if (filter) filter.addEventListener("click", function (e) {
            var btn = e.target.closest("button[data-filter]");
            if (!btn) return;
            filter.querySelectorAll("button").forEach(function (b) {
                var on = b === btn;
                b.classList.toggle("is-active", on);
                b.setAttribute("aria-pressed", on ? "true" : "false");
            });
            var f = btn.getAttribute("data-filter");
            // Verkocht omvat ook "Vermoedelijk verkocht"; Beschikbaar is exact.
            var regex = f === "Beschikbaar" ? "^Beschikbaar$" : f === "Verkocht" ? "^(Verkocht|Vermoedelijk verkocht)$" : "";
            table.column(statusCol).search(regex, true, false).draw();
        });

        tableEl.addEventListener("click", function (e) {
            var tr = e.target.closest("tr.is-clickable");
            if (!tr) return;
            var url = tr.getAttribute("data-url");
            if (!url) return;
            if (tr.getAttribute("data-external") === "true") window.open(url, "_blank", "noopener");
            else window.location.href = url;
        });
    }
})();
