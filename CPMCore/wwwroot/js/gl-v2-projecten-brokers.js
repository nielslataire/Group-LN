// gl-v2 — Project bewerken, tabblad Partners: makelaars toevoegen (zoekveld) en verwijderen (chip). Elke chip draagt een verborgen BrokerIds-veld dat met het formulier mee gepost wordt.
(function () {
    "use strict";
    var list = document.getElementById("pf-brokers");
    var add = document.getElementById("pf-broker-add");
    if (!list || !add) return;
    var combo = add.closest("[data-gl-v2-combo]");
    function dirty() { var f = document.getElementById("gl-v2-project-form"); if (f) { f.dispatchEvent(new Event("input", { bubbles: true })); f.dispatchEvent(new Event("change", { bubbles: true })); } }
    list.addEventListener("click", function (e) {
        var x = e.target.closest(".gl-v2-pf-broker-x"); if (!x) return;
        x.closest(".gl-v2-pf-broker").remove(); dirty();
    });
    document.addEventListener("gl-v2:combo-select", function (e) {
        if (!combo || !combo.contains(e.target)) return;
        var d = e.detail || {}; if (!d.id) return;
        if (!list.querySelector('.gl-v2-pf-broker[data-id="' + d.id + '"]')) {
            var chip = document.createElement("span"); chip.className = "gl-v2-pf-broker"; chip.setAttribute("data-id", d.id);
            var h = document.createElement("input"); h.type = "hidden"; h.name = "BrokerIds"; h.value = d.id;
            var ic = document.createElement("i"); ic.className = "ph ph-handshake"; ic.setAttribute("aria-hidden", "true");
            var t = document.createElement("span"); t.textContent = d.text || "";
            var b = document.createElement("button"); b.type = "button"; b.className = "gl-v2-pf-broker-x"; b.setAttribute("aria-label", (d.text || "") + " verwijderen"); b.innerHTML = '<i class="ph ph-x" aria-hidden="true"></i>';
            chip.appendChild(h); chip.appendChild(ic); chip.appendChild(t); chip.appendChild(b); list.appendChild(chip); dirty();
        }
        window.setTimeout(function () { if (window.GlV2Combo) window.GlV2Combo.setValue(combo, "", ""); }, 0);
    });
})();
