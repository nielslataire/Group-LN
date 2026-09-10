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

    var container = document.getElementById("fasesContainer");
    var tplFase = document.getElementById("tplFase");
    var tplMijlpaal = document.getElementById("tplMijlpaal");
    if (!container || !tplFase || !tplMijlpaal) return;

    function typeSelectHtml(selected) {
        return typeOpties.map(function (o) {
            return '<option value="' + o.v + '"' + (o.v === selected ? " selected" : "") + ">" + o.n + "</option>";
        }).join("");
    }

    function addMijlpaal(mijlpalenHost, m) {
        m = m || {};
        var node = tplMijlpaal.content.firstElementChild.cloneNode(true);
        node.querySelector('[data-m="naam"]').value = m.naam || "";
        node.querySelector('[data-m="code"]').value = m.code || "";
        node.querySelector('[data-m="anker"]').value = m.doeldatumAnkerCode || "";
        node.querySelector('[data-m="offset"]').value = (m.doeldatumOffsetDagen != null ? m.doeldatumOffsetDagen : "");
        var sel = node.querySelector('[data-m="type"]');
        sel.innerHTML = typeSelectHtml(m.mijlpaalType || 0);
        node.querySelector('[data-m="scope"]').value = String(m.scope || 0);
        var bindSel = node.querySelector('[data-m="binding"]');
        bindSel.innerHTML = bindingOpties.map(function (o) {
            return '<option value="' + o.v + '">' + o.n + "</option>";
        }).join("");
        bindSel.value = String(m.bronBinding != null ? m.bronBinding : 0);
        node.querySelector('[data-m="param"]').value = m.bronParam || "";
        node.querySelector("[data-del-mijlpaal]").addEventListener("click", function () { node.remove(); });
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
                    isVerplicht: true
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
