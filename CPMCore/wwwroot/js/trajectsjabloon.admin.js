(function () {
    "use strict";

    function readJson(id, fallback) {
        var el = document.getElementById(id);
        if (!el) return fallback;
        try { return JSON.parse(el.textContent); } catch (e) { return fallback; }
    }

    var data = readJson("sjabloonData", { fases: [] });
    var typeOpties = readJson("typeOpties", []);
    var bindingOpties = readJson("bindingOpties", [{ v: 0, n: "Handmatig" }]);
    var triggerEventOpties = readJson("triggerEventOpties", []);
    var triggerActieOpties = readJson("triggerActieOpties", []);

    var container = document.getElementById("fasesContainer");
    var tplFase = document.getElementById("tplFase");
    var tplMijlpaal = document.getElementById("tplMijlpaal");
    var tplTrigger = document.getElementById("tplTrigger");
    if (!container || !tplFase || !tplMijlpaal || !tplTrigger) return;

    function optionsHtml(opties, selected) {
        return opties.map(function (o) {
            return '<option value="' + o.v + '"' + (o.v === selected ? " selected" : "") + ">" + o.n + "</option>";
        }).join("");
    }

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

    function addMijlpaal(mijlpalenHost, m) {
        m = m || {};
        var node = tplMijlpaal.content.firstElementChild.cloneNode(true);
        node.querySelector('[data-m="naam"]').value = m.naam || "";
        node.querySelector('[data-m="code"]').value = m.code || "";
        node.querySelector('[data-m="anker"]').value = m.doeldatumAnkerCode || "";
        node.querySelector('[data-m="offset"]').value = (m.doeldatumOffsetDagen != null ? m.doeldatumOffsetDagen : "");
        node.querySelector('[data-m="type"]').innerHTML = optionsHtml(typeOpties, m.mijlpaalType || 0);
        node.querySelector('[data-m="scope"]').value = String(m.scope || 0);
        var bindSel = node.querySelector('[data-m="binding"]');
        bindSel.innerHTML = optionsHtml(bindingOpties, m.bronBinding != null ? m.bronBinding : 0);
        node.querySelector('[data-m="param"]').value = m.bronParam || "";
        node.querySelector("[data-del-mijlpaal]").addEventListener("click", function () { node.remove(); });

        var tHost = node.querySelector("[data-triggers]");
        (m.triggers || []).forEach(function (t) { addTrigger(tHost, t); });
        node.querySelector("[data-add-trigger]").addEventListener("click", function () { addTrigger(tHost, {}); });

        mijlpalenHost.appendChild(node);
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
        node.querySelector("[data-add-mijlpaal]").addEventListener("click", function () { addMijlpaal(mHost, {}); });
        node.querySelector("[data-del-fase]").addEventListener("click", function () { node.remove(); });
        container.appendChild(node);
    }

    (data.fases || []).forEach(addFase);
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
                var param = mn.querySelector('[data-m="param"]').value.trim();

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
                    doeldatumAnkerCode: mn.querySelector('[data-m="anker"]').value.trim() || null,
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
