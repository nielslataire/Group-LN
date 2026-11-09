// gl-v2 — Inhoudingsplicht (design-handoff 57): bron kiezen, filteren, aanvinken, extra btw-nummers, het overzicht "Te controleren"
// en het zijpaneel met het bewijs van een eerdere lijst. Het bewaren (en het bewijs) gebeurt op de server (InhoudingsplichtController).
(function () {
    var root = document.getElementById('gl-v2-ip');
    if (!root) return;

    var rows = Array.prototype.slice.call(document.querySelectorAll('#gl-v2-ip-rows tr'));
    var tbody = document.getElementById('gl-v2-ip-rows');
    var q = document.getElementById('gl-v2-ip-q');
    var all = document.getElementById('gl-v2-ip-all');
    var extra = document.getElementById('gl-v2-ip-extra');
    var sitesBox = document.getElementById('gl-v2-ip-sites');
    var yearNote = document.getElementById('gl-v2-ip-yearnote');
    var source = root.getAttribute('data-source') || 'actief';
    var offSites = {};
    var wasMatched = {}; // rij-id -> true als het vakje door een geplakt nummer is aangevinkt

    function cb(row) { return row.querySelector('input[type=checkbox]'); }
    function esc(s) { var d = document.createElement('div'); d.textContent = s == null ? '' : s; return d.innerHTML; }

    // ── Nummers normaliseren (zelfde regels als de server: InhoudingsplichtController.ParseExtra) ──
    function norm(raw) {
        var s = String(raw).toUpperCase().replace(/[^A-Z0-9]/g, '');
        var m;
        if ((m = s.match(/^(?:BE)?(\d{9,10})$/))) return { id: m[1].length === 9 ? '0' + m[1] : m[1] };
        if (/^(BE)?\d+$/.test(s)) return { bad: true };
        if (/^[A-Z]{2}[A-Z0-9]{4,14}$/.test(s)) return { id: s, foreign: true };
        return { bad: true };
    }
    function fmt(id) { return /^\d{10}$/.test(id) ? 'BE' + id.slice(0, 4) + '.' + id.slice(4, 7) + '.' + id.slice(7) : id; }

    function parseExtra() {
        var out = [], seen = {};
        String(extra ? extra.value : '').split('\n').forEach(function (line) {
            line = line.trim(); if (!line) return;
            var nm = '', nums;
            var sep = line.search(/[;\t]/);
            if (sep >= 0) { nm = line.slice(0, sep).trim(); nums = [line.slice(sep + 1)]; } else nums = line.split(',');
            nums.forEach(function (n) {
                if (!n.trim()) return;
                var e = norm(n); e.nm = nm; e.raw = n.trim();
                if (e.id) { if (seen[e.id]) return; seen[e.id] = true; }
                out.push(e);
            });
        });
        return out;
    }

    // ── Bron + filters ───────────────────────────────────────────────────────────────────────
    function inSource(row) {
        if (source === 'alle') return true;
        if (source === 'jaar') return row.getAttribute('data-year') === '1';
        if (row.getAttribute('data-actief') !== '1') return false;
        return row.getAttribute('data-sites').split(',').some(function (id) { return id && !offSites[id]; });
    }

    function applyFilters() {
        var term = (q ? q.value : '').trim().toLowerCase();
        var digits = term.replace(/\D/g, '');
        var visible = 0, allChecked = true;
        if (source === 'jaar') {
            rows.sort(function (a, b) { return Number(b.getAttribute('data-lastinvoice')) - Number(a.getAttribute('data-lastinvoice')); });
        } else {
            rows.sort(function (a, b) { return a.getAttribute('data-name').localeCompare(b.getAttribute('data-name'), 'nl'); });
        }
        rows.forEach(function (row) {
            tbody.appendChild(row);
            var hay = row.getAttribute('data-search');
            var match = !term || hay.indexOf(term) >= 0 || (digits.length >= 3 && (row.getAttribute('data-ident') || '').indexOf(digits) >= 0);
            var show = inSource(row) && match;
            row.hidden = !show;
            if (show) { visible++; if (!cb(row).checked) allChecked = false; }
        });
        document.getElementById('gl-v2-ip-norows').hidden = visible > 0;
        all.checked = visible > 0 && allChecked;
        if (sitesBox) sitesBox.hidden = source !== 'actief';
        if (yearNote) yearNote.hidden = source !== 'jaar';
    }

    document.querySelectorAll('#gl-v2-ip-sources button').forEach(function (btn) {
        btn.addEventListener('click', function () {
            source = btn.getAttribute('data-source');
            document.querySelectorAll('#gl-v2-ip-sources button').forEach(function (b) {
                var on = b === btn; b.classList.toggle('is-active', on); b.setAttribute('aria-selected', on ? 'true' : 'false');
            });
            applyFilters();
            summary();
        });
    });
    if (sitesBox) sitesBox.addEventListener('click', function (e) {
        var chip = e.target.closest('[data-site]'); if (!chip) return;
        var id = chip.getAttribute('data-site');
        offSites[id] = !offSites[id];
        chip.classList.toggle('is-on', !offSites[id]);
        chip.setAttribute('aria-pressed', offSites[id] ? 'false' : 'true');
        applyFilters();
    });
    if (q) q.addEventListener('input', applyFilters);
    all.addEventListener('change', function () {
        rows.forEach(function (r) { if (!r.hidden && !cb(r).disabled) cb(r).checked = all.checked; });
        summary();
    });
    tbody.addEventListener('change', function () { summary(); applyFilters(); });

    // ── Overzicht "Te controleren" ──────────────────────────────────────────────────────────
    var nameInput = document.getElementById('gl-v2-ip-name');
    var runBtn = document.getElementById('gl-v2-ip-run');
    var unconfigured = !!(runBtn && runBtn.disabled);

    function chipHtml(tone, icon, label) { return '<span class="gl-v2-badge ' + tone + '"><i class="ph ' + icon + '" aria-hidden="true"></i>' + esc(label) + '</span>'; }

    function summary() {
        var entries = parseExtra();
        var byIdent = {};
        rows.forEach(function (r) { var i = r.getAttribute('data-ident'); if (i) byIdent[i] = r; });

        // Een geplakt nummer van een bestaande leverancier vinkt zijn rij aan (en vergrendelt ze zolang het nummer in de lijst staat).
        var matched = {};
        entries.forEach(function (e) { if (e.id && byIdent[e.id]) matched[byIdent[e.id].getAttribute('data-id')] = true; });
        rows.forEach(function (r) {
            var id = r.getAttribute('data-id'), box = cb(r);
            if (!r.getAttribute('data-ident')) return;
            if (matched[id]) { box.checked = true; box.disabled = true; wasMatched[id] = true; }
            else if (wasMatched[id]) { box.disabled = false; box.checked = false; wasMatched[id] = false; }
            r.classList.toggle('is-selected', box.checked);
        });

        var picked = rows.filter(function (r) { return cb(r).checked && !matched[r.getAttribute('data-id')]; });
        var matchedRows = rows.filter(function (r) { return matched[r.getAttribute('data-id')]; });
        var extraOnly = entries.filter(function (e) { return !(e.id && byIdent[e.id]); });
        var valid = extraOnly.filter(function (e) { return e.id; });
        var bad = extraOnly.filter(function (e) { return e.bad; }).length;
        var total = picked.length + matchedRows.length + valid.length;

        document.getElementById('gl-v2-ip-total').textContent = total;
        document.getElementById('gl-v2-ip-selcount').textContent = 'UIT DE SELECTIELIJST · ' + (picked.length + matchedRows.length);
        document.getElementById('gl-v2-ip-clear').hidden = picked.length === 0;

        var plicht = 0;
        var list = document.getElementById('gl-v2-ip-sellist');
        list.innerHTML = '';
        picked.concat(matchedRows).forEach(function (r) {
            var warn = r.getAttribute('data-warn');
            if (warn === 'plicht') plicht++;
            var li = document.createElement('li');
            li.innerHTML = '<span class="n">' + esc(r.getAttribute('data-name')) + '</span>'
                + (warn === 'plicht' ? chipHtml('is-blocked', 'ph-warning', 'vorige: inhouden') : warn === 'fout' ? chipHtml('is-neutral', 'ph-x', 'vorige: mislukt') : '')
                + (matched[r.getAttribute('data-id')] ? '' : '<button type="button" class="x" aria-label="' + esc(r.getAttribute('data-name')) + ' weghalen" data-remove="' + r.getAttribute('data-id') + '"><i class="ph ph-x" aria-hidden="true"></i></button>');
            list.appendChild(li);
        });

        var xBlock = document.getElementById('gl-v2-ip-extra-block');
        var xList = document.getElementById('gl-v2-ip-extralist');
        xBlock.hidden = extraOnly.length === 0;
        document.getElementById('gl-v2-ip-extracount').textContent = 'EXTRA BTW-NUMMERS · ' + valid.length;
        xList.innerHTML = '';
        extraOnly.forEach(function (e) {
            var li = document.createElement('li');
            li.innerHTML = e.bad
                ? '<span class="n">' + esc(e.raw) + '<small>wordt overgeslagen</small></span>' + chipHtml('is-attention', 'ph-warning', 'Ongeldig')
                : '<span class="n">' + esc(fmt(e.id)) + '<small>' + esc(e.nm || (e.foreign ? 'buitenlands btw-nummer' : 'naam volgt uit de RSZ')) + '</small></span>' + chipHtml('is-info', 'ph-plus', 'Nieuw');
            xList.appendChild(li);
        });

        document.getElementById('gl-v2-ip-selempty').hidden = total > 0 || extraOnly.length > 0;
        var note = document.getElementById('gl-v2-ip-plichtnote');
        note.hidden = plicht === 0;
        if (plicht) note.querySelector('span').textContent = plicht + (plicht === 1 ? ' aannemer had' : ' aannemers hadden') + ' bij de vorige controle inhoudingsplicht.';
        document.getElementById('gl-v2-ip-skipnote').textContent = [matchedRows.length ? matchedRows.length + ' geplakt nummer' + (matchedRows.length > 1 ? 's' : '') + ' aangevinkt' : '', bad ? bad + ' ongeldig' : ''].filter(Boolean).join(' · ');

        if (runBtn) {
            runBtn.disabled = unconfigured || total === 0;
            runBtn.querySelector('span').textContent = total ? 'Controleer en bewaar (' + total + ')' : 'Controleer en bewaar';
        }
        if (nameInput) nameInput.placeholder = (source === 'actief' ? 'Actieve werven' : source === 'jaar' ? 'Laatste 12 maanden' : 'Controle') + ' — ' + new Date().toLocaleDateString('nl-BE');
    }

    document.getElementById('gl-v2-ip-sellist').addEventListener('click', function (e) {
        var x = e.target.closest('[data-remove]'); if (!x) return;
        var row = rows.filter(function (r) { return r.getAttribute('data-id') === x.getAttribute('data-remove'); })[0];
        if (row) { cb(row).checked = false; summary(); applyFilters(); }
    });
    document.getElementById('gl-v2-ip-clear').addEventListener('click', function () {
        rows.forEach(function (r) { if (!cb(r).disabled) cb(r).checked = false; });
        summary(); applyFilters();
    });
    if (extra) extra.addEventListener('input', function () { summary(); applyFilters(); });

    // ── Eerdere lijsten: zoeken + zijpaneel ─────────────────────────────────────────────────
    var hq = document.getElementById('gl-v2-ip-hq');
    if (hq) hq.addEventListener('input', function () {
        var term = hq.value.trim().toLowerCase(), shown = 0;
        document.querySelectorAll('#gl-v2-ip-hist tr').forEach(function (tr) {
            var ok = !term || tr.getAttribute('data-search').indexOf(term) >= 0; tr.hidden = !ok; if (ok) shown++;
        });
        var none = document.getElementById('gl-v2-ip-nolists'); if (none) none.hidden = shown > 0;
    });

    var drawer = document.getElementById('gl-v2-ip-drawer');
    var backdrop = document.getElementById('gl-v2-ip-backdrop');
    var drawerBody = document.getElementById('gl-v2-ip-drawer-body');
    var lastFocus = null;

    function closeDrawer() {
        drawer.hidden = true; backdrop.hidden = true;
        if (lastFocus && lastFocus.focus) lastFocus.focus();
    }
    function openDrawer(id, trigger) {
        lastFocus = trigger || document.activeElement;
        drawerBody.innerHTML = '<p class="gl-v2-ip-empty">Laden…</p>';
        drawer.hidden = false; backdrop.hidden = false;
        fetch(root.getAttribute('data-panel-url') + '?id=' + encodeURIComponent(id), { credentials: 'same-origin' })
            .then(function (r) { if (!r.ok) throw new Error(); return r.text(); })
            .then(function (html) {
                drawerBody.innerHTML = html;
                var close = drawerBody.querySelector('[data-ip-close]'); if (close) close.focus();
            })
            .catch(function () { drawerBody.innerHTML = '<p class="gl-v2-ip-empty">De controlelijst kon niet geladen worden.</p>'; });
    }
    function toast(tone, title) { if (window.GlV2Toast) window.GlV2Toast.show({ tone: tone, title: title, body: '' }); }

    function closeMenus() {
        drawer.querySelectorAll('.gl-v2-ip-menu').forEach(function (m) { m.hidden = true; });
        drawer.querySelectorAll('[data-ip-menu]').forEach(function (b) { b.setAttribute('aria-expanded', 'false'); });
    }

    // "Naam geven": de titel wordt een invoerveld; Enter of de focus verlaten bewaart de naam (enkel de naam, het bewijs blijft ongewijzigd).
    function startRename(btn) {
        var h2 = document.getElementById('gl-v2-ip-drawer-title');
        var input = document.createElement('input');
        input.type = 'text'; input.maxLength = 200; input.setAttribute('aria-label', 'Naam van de lijst'); input.placeholder = 'Naam van de lijst';
        h2.parentNode.insertBefore(input, h2); h2.hidden = true; btn.hidden = true;
        input.focus();
        var done = false;
        function finish(save) {
            if (done) return; done = true;
            var name = input.value.trim();
            if (!save || !name) { input.remove(); h2.hidden = false; btn.hidden = false; return; }
            var tokenEl = document.querySelector('#gl-v2-ip-form input[name="__RequestVerificationToken"]');
            var body = new URLSearchParams({ id: h2.getAttribute('data-batch'), name: name });
            if (tokenEl) body.set('__RequestVerificationToken', tokenEl.value);
            fetch(root.getAttribute('data-rename-url'), { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: body.toString() })
                .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
                .then(function (d) {
                    h2.textContent = d.name || name; input.remove(); h2.hidden = false; btn.remove();
                    var link = document.querySelector('#gl-v2-ip-hist [data-open="' + h2.getAttribute('data-batch') + '"]');
                    if (link) link.textContent = d.name || name;
                    toast('success', 'Naam bewaard');
                })
                .catch(function () { input.remove(); h2.hidden = false; btn.hidden = false; toast('danger', 'De naam kon niet bewaard worden'); });
        }
        input.addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); finish(true); } else if (e.key === 'Escape') { e.stopPropagation(); finish(false); } });
        input.addEventListener('blur', function () { finish(true); });
    }

    document.addEventListener('click', function (e) {
        var open = e.target.closest('button[data-open]');
        if (open) { openDrawer(open.getAttribute('data-open'), open); return; }
        var copy = e.target.closest('[data-copy]');
        if (copy) {
            var text = copy.getAttribute('data-copy'), label = copy.getAttribute('data-copy-label') || 'Tekst';
            (navigator.clipboard ? navigator.clipboard.writeText(text) : Promise.reject()).then(function () { toast('success', label + ' gekopieerd'); }, function () { toast('danger', 'Kopiëren is niet gelukt'); });
            closeMenus();
            return;
        }
        var menuBtn = e.target.closest('[data-ip-menu]');
        if (menuBtn) {
            var menu = menuBtn.parentNode.querySelector('.gl-v2-ip-menu'), willOpen = menu.hidden;
            closeMenus(); menu.hidden = !willOpen; menuBtn.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
            return;
        }
        var ren = e.target.closest('[data-ip-rename]');
        if (ren) { startRename(ren); return; }
        if (!e.target.closest('.gl-v2-ip-menu')) closeMenus();
        if (e.target.closest('[data-ip-close]') || e.target === backdrop) closeDrawer();
    });
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape' || drawer.hidden) return;
        var openMenu = drawer.querySelector('.gl-v2-ip-menu:not([hidden])');
        if (openMenu) closeMenus(); else closeDrawer();
    });

    applyFilters();
    summary();
    applyFilters();
    var auto = root.getAttribute('data-open');
    if (auto) openDrawer(auto);
})();
