(function () {
    "use strict";

    var PROJECT_CREATED = "PROJECT_CREATED";

    function readJson(id, fallback) {
        var el = document.getElementById(id);
        if (!el) return fallback;
        try { return JSON.parse(el.textContent); } catch (e) { return fallback; }
    }

    function debounce(fn, wait) {
        var t;
        return function () {
            clearTimeout(t);
            var args = arguments;
            t = setTimeout(function () { fn.apply(null, args); }, wait);
        };
    }

    /** Vrij ingetypte tekst (mijlpaal-/fasenaam, bewaarde waarden) kan naar innerHTML — altijd escapen. */
    function escapeHtml(s) {
        return String(s == null ? "" : s)
            .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
    }

    var data = readJson("sjabloonData", { fases: [] });
    var typeOpties = readJson("typeOpties", []);
    var bindingOpties = readJson("bindingOpties", [{ v: 0, n: "Handmatig" }]);
    var triggerEventOpties = readJson("triggerEventOpties", []);
    var triggerActieOpties = readJson("triggerActieOpties", []);
    var bronParamOptiesByBinding = readJson("bronParamOptiesByBinding", {});
    var bronParamModeByBinding = readJson("bronParamModeByBinding", {});
    var bronParamHintByBinding = readJson("bronParamHintByBinding", {});

    var container = document.getElementById("fasesContainer");
    var tplFase = document.getElementById("tplFase");
    var tplMijlpaal = document.getElementById("tplMijlpaal");
    var tplTrigger = document.getElementById("tplTrigger");
    if (!container || !tplFase || !tplMijlpaal || !tplTrigger) return;

    var hasSelect2 = typeof window.jQuery === "function" && !!window.jQuery.fn.select2;

    function optionsHtml(opties, selected) {
        return opties.map(function (o) {
            return '<option value="' + o.v + '"' + (o.v === selected ? " selected" : "") + ">" + o.n + "</option>";
        }).join("");
    }

    function select2ify($el) {
        if (!hasSelect2) return;
        var $ = window.jQuery;
        if ($el.data("select2")) $el.select2("destroy");
        $el.select2({ theme: "bootstrap", width: "100%", language: "nl", dropdownAutoWidth: false, minimumResultsForSearch: 6 });
    }

    // ── Trigger-rijen (ongewijzigd t.o.v. de vorige versie) ────────────────────────────
    function addTrigger(triggersHost, t) {
        t = t || {};
        var node = tplTrigger.content.firstElementChild.cloneNode(true);
        node.querySelector('[data-t="event"]').innerHTML = optionsHtml(triggerEventOpties, t.triggerEvent || 0);
        node.querySelector('[data-t="actie"]').innerHTML = optionsHtml(triggerActieOpties, t.triggerActie || 0);
        node.querySelector('[data-t="offset"]').value = (t.offsetDagen != null ? t.offsetDagen : "");
        node.querySelector('[data-t="params"]').value = t.actieParametersJson || "";
        node.querySelector('[data-t="magwijzigen"]').checked = !!t.magProjectWijzigen;
        node.querySelector('[data-t="actief"]').checked = t.isActief !== false;
        node.querySelector("[data-del-trigger]").addEventListener("click", function () { node.remove(); });
        triggersHost.appendChild(node);
    }

    // ── Streefdatum-anker: alle mijlpalen van het sjabloon als keuzelijst i.p.v. vrije tekst ──

    /** Leest alle huidige mijlpaal-rijen in de DOM: {node, naam, code, faseNaam}. */
    function buildRegistry() {
        var out = [];
        container.querySelectorAll("[data-mijlpaal]").forEach(function (mn) {
            var faseNode = mn.closest("[data-fase]");
            var faseNaamEl = faseNode ? faseNode.querySelector('[data-f="naam"]') : null;
            out.push({
                node: mn,
                naam: (mn.querySelector('[data-m="naam"]').value || "").trim(),
                code: (mn.querySelector('[data-m="code"]').value || "").trim(),
                faseNaam: faseNaamEl ? (faseNaamEl.value || "").trim() : ""
            });
        });
        return out;
    }

    function anchorOptionLabel(entry) {
        var naam = entry.naam || "(naamloze mijlpaal)";
        var prefix = entry.faseNaam ? entry.faseNaam + " · " : "";
        return prefix + naam + " (" + entry.code + ")";
    }

    function refreshAnchorSelect(selectEl, registry, selfNode) {
        var saved = selectEl.value || selectEl.dataset.pendingAnker || PROJECT_CREATED;

        var html = '<option value="' + PROJECT_CREATED + '">Bij aanmaken van het project</option>';
        var known = { };
        known[PROJECT_CREATED] = true;

        registry.forEach(function (entry) {
            if (entry.node === selfNode || !entry.code) return;
            html += '<option value="' + escapeHtml(entry.code) + '">' + escapeHtml(anchorOptionLabel(entry)) + "</option>";
            known[entry.code] = true;
        });

        if (saved && !known[saved]) {
            html = '<option value="' + escapeHtml(saved) + '">⚠ Onbekende mijlpaal-code: ' + escapeHtml(saved) + "</option>" + html;
        }

        selectEl.innerHTML = html;
        selectEl.value = saved;
        delete selectEl.dataset.pendingAnker;

        if (hasSelect2) select2ify(window.jQuery(selectEl));
    }

    var refreshAllAnchors = debounce(function () {
        var registry = buildRegistry();
        registry.forEach(function (entry) {
            var sel = entry.node.querySelector('[data-m="anker"]');
            if (sel) refreshAnchorSelect(sel, registry, entry.node);
        });
    }, 200);

    // ── Bron-parameter: leesbare keuzelijst afhankelijk van de gekozen binding ──────────

    function renderParamControl(mijlpaalNode, bindingVal, storedValue) {
        var cell = mijlpaalNode.querySelector("[data-m-param-cell]");
        var host = mijlpaalNode.querySelector("[data-m-param-control]");
        var hintEl = mijlpaalNode.querySelector("[data-m-param-hint]");
        if (!cell || !host || !hintEl) return;

        var mode = bronParamModeByBinding[bindingVal] || (bronParamOptiesByBinding[bindingVal] ? "select" : "hidden");
        var opties = bronParamOptiesByBinding[bindingVal] || [];
        var hint = bronParamHintByBinding[bindingVal] || "";

        if (hasSelect2 && window.jQuery(host).find("select").data("select2")) {
            window.jQuery(host).find("select").select2("destroy");
        }

        if (mode === "select") {
            var known = {};
            var optHtml = opties.map(function (o) {
                known[o.v] = true;
                return '<option value="' + escapeHtml(o.v) + '">' + escapeHtml(o.n) + "</option>";
            }).join("");
            if (storedValue && !known[storedValue]) {
                optHtml = '<option value="' + escapeHtml(storedValue) + '">⚠ Huidige waarde: ' + escapeHtml(storedValue) + " (niet in lijst)</option>" + optHtml;
            }
            host.innerHTML = '<select class="form-select form-select-sm" data-m="param"></select>';
            var sel = host.querySelector("select");
            sel.innerHTML = optHtml;
            sel.value = storedValue || (opties[0] ? opties[0].v : "");
            if (hasSelect2) select2ify(window.jQuery(sel));
            hintEl.textContent = "";
            cell.hidden = false;
        } else if (mode === "numeriek") {
            host.innerHTML = '<input type="text" inputmode="numeric" pattern="[0-9]*" class="form-control form-control-sm" placeholder="bv. 123" data-m="param" />';
            host.querySelector("input").value = storedValue || "";
            hintEl.textContent = hint;
            cell.hidden = false;
        } else {
            host.innerHTML = "";
            hintEl.textContent = hint;
            cell.hidden = !hint; // enkel volledig verbergen als er ook geen toelichting is
        }
    }

    function addMijlpaal(mijlpalenHost, m) {
        m = m || {};
        var node = tplMijlpaal.content.firstElementChild.cloneNode(true);
        node.querySelector('[data-m="naam"]').value = m.naam || "";
        node.querySelector('[data-m="code"]').value = m.code || "";
        node.querySelector('[data-m="offset"]').value = (m.doeldatumOffsetDagen != null ? m.doeldatumOffsetDagen : "");
        node.querySelector('[data-m="type"]').innerHTML = optionsHtml(typeOpties, m.mijlpaalType || 0);
        node.querySelector('[data-m="scope"]').value = String(m.scope || 0);

        var ankerSel = node.querySelector('[data-m="anker"]');
        ankerSel.dataset.pendingAnker = m.doeldatumAnkerCode || PROJECT_CREATED;

        var bindSel = node.querySelector('[data-m="binding"]');
        var bindingVal = m.bronBinding != null ? m.bronBinding : 0;
        bindSel.innerHTML = optionsHtml(bindingOpties, bindingVal);
        renderParamControl(node, bindingVal, m.bronParam || "");
        bindSel.addEventListener("change", function () {
            renderParamControl(node, parseInt(bindSel.value, 10) || 0, null);
        });

        node.querySelector("[data-del-mijlpaal]").addEventListener("click", function () {
            node.remove();
            refreshAllAnchors();
        });

        var naamCodeDebounced = debounce(function () { refreshAllAnchors(); }, 250);
        node.querySelector('[data-m="naam"]').addEventListener("input", naamCodeDebounced);
        node.querySelector('[data-m="code"]').addEventListener("input", naamCodeDebounced);

        var tHost = node.querySelector("[data-triggers]");
        (m.triggers || []).forEach(function (t) { addTrigger(tHost, t); });
        node.querySelector("[data-add-trigger]").addEventListener("click", function () { addTrigger(tHost, {}); });

        mijlpalenHost.appendChild(node);
        return node;
    }

    function addFase(f) {
        f = f || {};
        var node = tplFase.content.firstElementChild.cloneNode(true);
        node.querySelector('[data-f="naam"]').value = f.naam || "";
        node.querySelector('[data-f="code"]').value = f.code || "";
        node.querySelector('[data-f="volgorde"]').value = (f.volgorde != null ? f.volgorde : 0);
        node.querySelector('[data-f="status"]').value = (f.standaardProjectStatusId != null ? f.standaardProjectStatusId : "");
        var mHost = node.querySelector("[data-mijlpalen]");
        (f.mijlpalen || []).forEach(function (m) { addMijlpaal(mHost, m); });
        node.querySelector("[data-add-mijlpaal]").addEventListener("click", function () { addMijlpaal(mHost, {}); refreshAllAnchors(); });
        node.querySelector("[data-del-fase]").addEventListener("click", function () { node.remove(); refreshAllAnchors(); });
        node.querySelector('[data-f="naam"]').addEventListener("input", debounce(function () { refreshAllAnchors(); }, 250));
        container.appendChild(node);
    }

    (data.fases || []).forEach(addFase);
    refreshAllAnchors();
    document.getElementById("btnAddFase").addEventListener("click", function () { addFase({}); });

    document.getElementById("sjabloonForm").addEventListener("submit", function () {
        var fases = [];
        container.querySelectorAll("[data-fase]").forEach(function (fn, fi) {
            var mijlpalen = [];
            fn.querySelectorAll("[data-mijlpaal]").forEach(function (mn, mi) {
                var naam = mn.querySelector('[data-m="naam"]').value.trim();
                if (!naam) return;
                var offset = mn.querySelector('[data-m="offset"]').value;
                var binding = parseInt(mn.querySelector('[data-m="binding"]').value, 10) || 0;
                var paramEl = mn.querySelector('[data-m="param"]');
                var param = paramEl ? paramEl.value.trim() : "";
                var ankerVal = mn.querySelector('[data-m="anker"]').value;

                var triggers = [];
                mn.querySelectorAll("[data-trigger]").forEach(function (tn) {
                    var paramsJson = tn.querySelector('[data-t="params"]').value.trim();
                    var toff = tn.querySelector('[data-t="offset"]').value;
                    triggers.push({
                        triggerEvent: parseInt(tn.querySelector('[data-t="event"]').value, 10) || 0,
                        triggerActie: parseInt(tn.querySelector('[data-t="actie"]').value, 10) || 0,
                        offsetDagen: toff === "" ? null : parseInt(toff, 10),
                        actieParametersJson: paramsJson || null,
                        magProjectWijzigen: tn.querySelector('[data-t="magwijzigen"]').checked,
                        isActief: tn.querySelector('[data-t="actief"]').checked
                    });
                });

                mijlpalen.push({
                    naam: naam,
                    code: mn.querySelector('[data-m="code"]').value.trim(),
                    volgorde: (mi + 1) * 10,
                    mijlpaalType: parseInt(mn.querySelector('[data-m="type"]').value, 10) || 0,
                    scope: parseInt(mn.querySelector('[data-m="scope"]').value, 10) || 0,
                    doeldatumAnkerCode: (ankerVal && ankerVal !== PROJECT_CREATED) ? ankerVal.trim() : PROJECT_CREATED,
                    doeldatumOffsetDagen: offset === "" ? null : parseInt(offset, 10),
                    bronBinding: binding === 0 ? null : binding,
                    bronParam: param || null,
                    isVerplicht: true,
                    triggers: triggers
                });
            });
            var fnaam = fn.querySelector('[data-f="naam"]').value.trim();
            if (!fnaam) return;
            var st = fn.querySelector('[data-f="status"]').value;
            fases.push({
                naam: fnaam,
                code: fn.querySelector('[data-f="code"]').value.trim() || fnaam.toUpperCase().replace(/[^A-Z0-9]+/g, "_"),
                volgorde: parseInt(fn.querySelector('[data-f="volgorde"]').value, 10) || (fi + 1) * 10,
                standaardProjectStatusId: st === "" ? null : parseInt(st, 10),
                mijlpalen: mijlpalen
            });
        });

        var payload = {
            id: data.id || null,
            naam: document.getElementById("sj-naam").value.trim(),
            projectType: document.getElementById("sj-type").value === "" ? null : parseInt(document.getElementById("sj-type").value, 10),
            isStandaard: document.getElementById("sj-standaard").checked,
            isActief: document.getElementById("sj-actief").checked,
            omschrijving: document.getElementById("sj-omschrijving").value,
            fases: fases
        };
        document.getElementById("payloadJson").value = JSON.stringify(payload);
    });
})();
