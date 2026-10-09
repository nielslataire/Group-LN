// Projecten/EditV2 — tab "SEO & website": lijst eigen kerncijfers (titel + tekst) van de publieke projectpagina.
// Toevoegen, verwijderen en herschikken (pijltjes + handvat); de positie in de lijst wordt de volgorde op de site,
// dus de veldnamen (Website.Kpis[i].…) worden na elke wijziging hernummerd voor de modelbinding.
(function () {
    "use strict";
    var container = document.getElementById("websiteKpiContainer");
    if (!container) return;
    var addBtn = document.getElementById("addWebKpi");

    function rows() { return Array.prototype.slice.call(container.querySelectorAll(".web-kpi-row")); }
    function esc(s) { return String(s == null ? "" : s).replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;"); }
    function changed() { container.dispatchEvent(new Event("input", { bubbles: true })); }

    function renumber() {
        var list = rows();
        list.forEach(function (row, i) {
            row.querySelectorAll("input[name^='Website.Kpis[']").forEach(function (inp) {
                inp.name = inp.name.replace(/^Website\.Kpis\[\d+\]/, "Website.Kpis[" + i + "]");
            });
            var up = row.querySelector(".move-kpi-up");
            var down = row.querySelector(".move-kpi-down");
            if (up) up.disabled = i === 0;
            if (down) down.disabled = i === list.length - 1;
        });
    }

    function rowHtml(i) {
        return '<input type="hidden" name="Website.Kpis[' + i + '].Id" value="0" />' +
            '<span class="gl-v2-pf-handle" title="Sleep om te herschikken" aria-hidden="true"><i class="ph ph-dots-six-vertical"></i></span>' +
            '<div class="gl-v2-field"><div class="gl-v2-field-box"><input type="text" name="Website.Kpis[' + i + '].Title" maxlength="80" class="gl-v2-field-input" placeholder=" " autocomplete="off" aria-label="Titel kerncijfer" /></div></div>' +
            '<div class="gl-v2-field"><div class="gl-v2-field-box"><input type="text" name="Website.Kpis[' + i + '].Text" maxlength="200" class="gl-v2-field-input" placeholder=" " autocomplete="off" aria-label="Tekst kerncijfer" /></div></div>' +
            '<div class="gl-v2-pf-row-actions">' +
            '<button type="button" class="gl-v2-icon-btn move-kpi-up" aria-label="Kerncijfer omhoog" title="Omhoog"><i class="ph ph-arrow-up" aria-hidden="true"></i></button>' +
            '<button type="button" class="gl-v2-icon-btn move-kpi-down" aria-label="Kerncijfer omlaag" title="Omlaag"><i class="ph ph-arrow-down" aria-hidden="true"></i></button>' +
            '<button type="button" class="gl-v2-icon-btn is-danger remove-kpi" aria-label="Kerncijfer verwijderen"><i class="ph ph-trash" aria-hidden="true"></i></button>' +
            '</div>';
    }

    renumber();

    if (addBtn) addBtn.addEventListener("click", function () {
        var row = document.createElement("div");
        row.className = "gl-v2-pf-row gl-v2-pf-kpirow web-kpi-row";
        row.innerHTML = rowHtml(rows().length);
        container.appendChild(row);
        renumber();
        var first = row.querySelector("input[type=text]");
        if (first) first.focus();
        changed();
    });

    container.addEventListener("click", function (e) {
        var del = e.target.closest(".remove-kpi");
        if (del) {
            del.closest(".web-kpi-row").remove();
            renumber();
            changed();
            return;
        }
        var mv = e.target.closest(".move-kpi-up, .move-kpi-down");
        if (!mv || mv.disabled) return;
        var row = mv.closest(".web-kpi-row");
        var isUp = mv.classList.contains("move-kpi-up");
        if (isUp) {
            if (row.previousElementSibling) container.insertBefore(row, row.previousElementSibling);
        } else if (row.nextElementSibling) {
            container.insertBefore(row.nextElementSibling, row);
        }
        renumber();
        changed();
        var again = row.querySelector(isUp ? ".move-kpi-up" : ".move-kpi-down");
        if (again && !again.disabled) again.focus(); else mv.blur();
    });

    // Slepen met de muis: enkel via het handvat, zodat tekst selecteren in de velden blijft werken.
    var dragRow = null;
    container.addEventListener("mousedown", function (e) {
        var row = e.target.closest(".web-kpi-row");
        if (row) row.draggable = !!e.target.closest(".gl-v2-pf-handle");
    });
    container.addEventListener("dragstart", function (e) {
        dragRow = e.target.closest ? e.target.closest(".web-kpi-row") : null;
        if (!dragRow) return;
        e.dataTransfer.effectAllowed = "move";
        try { e.dataTransfer.setData("text/plain", "kpi"); } catch (err) { }
    });
    container.addEventListener("dragover", function (e) {
        if (!dragRow) return;
        e.preventDefault();
        var over = e.target.closest(".web-kpi-row");
        if (!over || over === dragRow) return;
        var rect = over.getBoundingClientRect();
        var after = (e.clientY - rect.top) > rect.height / 2;
        container.insertBefore(dragRow, after ? over.nextElementSibling : over);
    });
    container.addEventListener("dragend", function () {
        if (!dragRow) return;
        dragRow.draggable = false;
        dragRow = null;
        renumber();
        changed();
    });
})();
