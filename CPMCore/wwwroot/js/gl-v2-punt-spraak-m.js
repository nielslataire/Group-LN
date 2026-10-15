// gl-v2 — Punten: Inspreken (design-handoff 41c, 40f) en Rondgang (41d, 40g/40h) op gsm, plus het microfoontje bij de titel (41a stap 2).
// Opnemen gebeurt in de browser (MediaRecorder). Spraak naar tekst (OpenAI) en het herkennen van punten (Anthropic) gebeuren op de server
// (ProjectsIssuesController.Spraak.cs). Een voorstel bewaren = een concept aanmaken, via dezelfde route als het gewone paneel.
(function () {
    "use strict";
    var root = document.getElementById("gl-v2-pt-root");
    if (!root) return;
    function $(s, c) { return (c || document).querySelector(s); }
    function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }
    function esc(t) { var d = document.createElement("div"); d.textContent = t == null ? "" : t; return d.innerHTML; }
    function mmss(sec) { sec = Math.max(0, Math.round(sec)); return ("0" + Math.floor(sec / 60)).slice(-2) + ":" + ("0" + (sec % 60)).slice(-2); }
    function mss(sec) { sec = Math.max(0, Math.round(sec)); return Math.floor(sec / 60) + ":" + ("0" + (sec % 60)).slice(-2); }
    var base = root.dataset.issuesBase || "", projectId = root.dataset.projectId || "0";
    var units = [], verslagen = [];
    try { units = JSON.parse(root.dataset.units || "[]"); verslagen = JSON.parse(root.dataset.verslagen || "[]"); } catch (e) { /* leeg */ }
    function token() { var i = document.querySelector("#pt-new-form input[name=__RequestVerificationToken]"); return i ? i.value : ""; }
    function post(action, fd) {
        fd.append("__RequestVerificationToken", token());
        return fetch(base + "/" + action, { method: "POST", body: fd, credentials: "same-origin" }).then(function (r) { return r.json(); });
    }
    function toast(msg) {
        var t = document.createElement("div"); t.className = "gl-v2-pt-toast"; t.setAttribute("role", "status"); t.textContent = msg; document.body.appendChild(t);
        requestAnimationFrame(function () { t.classList.add("is-in"); });
        setTimeout(function () { t.classList.remove("is-in"); setTimeout(function () { t.remove(); }, 300); }, 5200);
    }
    function last() { return (window.PuntLast && window.PuntLast.get()) || {}; }

    // ── is spraak ingesteld? (sleutels op de server) ──
    var configured = null;
    function ensureConfigured() {
        if (configured === true) return Promise.resolve(true);
        return fetch(base + "/SpraakStatusV2", { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) {
            configured = !!j.configured;
            if (!configured) toast("Inspreken is nog niet ingesteld. Vraag een beheerder om de sleutels voor spraak en AI toe te voegen.");
            return configured;
        }).catch(function () { toast("Kan de spraakdienst nu niet bereiken."); return false; });
    }

    // ── opnemen ──
    function pickMime() {
        var list = ["audio/webm;codecs=opus", "audio/webm", "audio/mp4", "audio/ogg;codecs=opus"];
        if (!window.MediaRecorder) return "";
        for (var i = 0; i < list.length; i++) if (MediaRecorder.isTypeSupported(list[i])) return list[i];
        return "";
    }
    function extOf(mime) { return /mp4/.test(mime) ? "mp4" : /ogg/.test(mime) ? "ogg" : "webm"; }
    // start een opname op een (audio)stream; geeft een handvat met stop() -> Blob, pause(), resume()
    function recordStream(stream) {
        var mime = pickMime(), chunks = [], rec = new MediaRecorder(stream, mime ? { mimeType: mime, audioBitsPerSecond: 32000 } : { audioBitsPerSecond: 32000 });
        var done = new Promise(function (res) { rec.onstop = function () { res(new Blob(chunks, { type: rec.mimeType || mime || "audio/webm" })); }; });
        rec.ondataavailable = function (e) { if (e.data && e.data.size) chunks.push(e.data); };
        rec.start(1000);
        return { rec: rec, stop: function () { if (rec.state !== "inactive") rec.stop(); return done; }, pause: function () { if (rec.state === "recording") rec.pause(); }, resume: function () { if (rec.state === "paused") rec.resume(); } };
    }
    function micStream() { return navigator.mediaDevices.getUserMedia({ audio: { echoCancellation: true, noiseSuppression: true }, video: false }); }
    function stopTracks(s) { if (s) s.getTracks().forEach(function (t) { t.stop(); }); }
    function micError() { toast("De microfoon is niet beschikbaar. Controleer of je de microfoon hebt toegestaan voor deze website."); }

    // ── gedeeld scherm (licht) ──
    var screenEl = null;
    function openScreen(html, cls) {
        if (!screenEl) { screenEl = document.createElement("div"); screenEl.className = "gl-v2-ptv"; screenEl.setAttribute("role", "dialog"); screenEl.setAttribute("aria-modal", "true"); document.body.appendChild(screenEl); }
        screenEl.className = "gl-v2-ptv " + (cls || ""); screenEl.innerHTML = html; screenEl.hidden = false; document.body.classList.add("gl-v2-ptcam-on");
        return screenEl;
    }
    function closeScreen() { if (screenEl) { screenEl.hidden = true; screenEl.innerHTML = ""; } document.body.classList.remove("gl-v2-ptcam-on"); }
    var CHECK = '<svg viewBox="0 0 16 16"><path d="M3.4 8.4l3 3 6.2-6.8"/></svg>';
    function head(title, sub, withClose) {
        return '<header class="gl-v2-ptv-head"><div><b>' + esc(title) + '</b>' + (sub ? '<small>' + esc(sub) + '</small>' : '') + '</div>' +
            (withClose === false ? '' : '<button type="button" class="gl-v2-ptv-x" data-act="close" aria-label="Sluiten"><i class="ph ph-x" aria-hidden="true"></i></button>') + '</header>';
    }
    function unitLabel(id) { var u = units.filter(function (x) { return String(x.id) === String(id); })[0]; return u ? u.name : "Algemeen"; }

    // ───────────────────────────── Inspreken (41c) ─────────────────────────────
    var voice = null;
    function openVoice() {
        ensureConfigured().then(function (ok) {
            if (!ok) return;
            if (!navigator.mediaDevices || !window.MediaRecorder) { toast("Opnemen wordt niet ondersteund op dit toestel."); return; }
            var l = last(), unitId = l.unitId != null ? String(l.unitId) : "";
            var opts = '<option value="">Algemeen</option>' + units.map(function (u) { return '<option value="' + u.id + '"' + (String(u.id) === unitId ? " selected" : "") + '>' + esc(u.name) + '</option>'; }).join("");
            var el = openScreen(head("Inspreken", "", true) +
                '<div class="gl-v2-ptv-body gl-v2-ptv-center"><label class="gl-v2-ptv-unit"><span>Eenheid</span><select id="ptv-unit">' + opts + '</select></label>' +
                '<p class="gl-v2-ptv-lead">Zeg wat er moet gebeuren, waar, en wie het moet doen. Meerdere punten mag.</p>' +
                '<div class="gl-v2-ptv-timer" id="ptv-timer">0:00</div>' +
                '<button type="button" class="gl-v2-ptv-mic" id="ptv-mic" aria-label="Opnemen"><i class="ph ph-microphone" aria-hidden="true"></i></button>' +
                '<p class="gl-v2-ptv-hint" id="ptv-hint">Houd ingedrukt · loslaten = klaar</p></div>', "is-voice");
            voice = { stream: null, handle: null, started: 0, timer: null, toggle: false };
            var mic = $("#ptv-mic", el), timer = $("#ptv-timer", el), hint = $("#ptv-hint", el);
            function begin() {
                if (voice.handle) return;
                micStream().then(function (s) {
                    voice.stream = s; voice.handle = recordStream(s); voice.started = Date.now(); mic.classList.add("is-rec"); hint.textContent = "Aan het opnemen…";
                    voice.timer = setInterval(function () { timer.textContent = mss((Date.now() - voice.started) / 1000); }, 250);
                }).catch(micError);
            }
            function finish() {
                if (!voice || !voice.handle) return;
                var secs = (Date.now() - voice.started) / 1000; clearInterval(voice.timer);
                voice.handle.stop().then(function (blob) { stopTracks(voice.stream); var h = voice; voice = null; if (secs < 1.2 || blob.size < 1000) { toast("Dat was te kort. Houd de knop ingedrukt terwijl je spreekt."); closeScreen(); return; } processVoice(blob, secs, $("#ptv-unit", screenEl) ? $("#ptv-unit", screenEl).value : unitId); });
            }
            var downAt = 0;
            mic.addEventListener("pointerdown", function (e) { e.preventDefault(); downAt = Date.now(); if (voice.toggle && voice.handle) { finish(); return; } begin(); });
            mic.addEventListener("pointerup", function () { if (!voice || !voice.handle) return; if (Date.now() - downAt < 450) { voice.toggle = true; hint.textContent = "Tik nog eens om te stoppen"; } else finish(); });
            mic.addEventListener("pointercancel", function () { if (voice && voice.handle && !voice.toggle) finish(); });
            mic.addEventListener("contextmenu", function (e) { e.preventDefault(); });
            el.addEventListener("click", function (e) { if (e.target.closest("[data-act=close]")) { if (voice) { clearInterval(voice.timer); if (voice.handle) voice.handle.stop(); stopTracks(voice.stream); voice = null; } closeScreen(); } });
        });
    }
    function stepsHtml(steps) {
        return '<div class="gl-v2-ptv-steps">' + steps.map(function (s) {
            var ico = s[1] === "done" ? '<span class="gl-v2-ptv-ico done">' + CHECK + '</span>' : '<span class="gl-v2-ptv-ico ' + s[1] + '"><i></i></span>';
            return '<div class="gl-v2-ptv-step is-' + s[1] + '">' + ico + '<b>' + esc(s[0]) + '</b><em>' + esc(s[2] || "") + '</em></div>';
        }).join("") + '</div>';
    }
    function processing(title, sub, steps, quote, extra) {
        openScreen(head(title, sub, false) + '<div class="gl-v2-ptv-body"><div class="gl-v2-ptv-bar"><i></i></div>' + stepsHtml(steps) +
            (quote ? '<blockquote class="gl-v2-ptv-quote">' + esc(quote) + '</blockquote>' : '') + (extra ? '<p class="gl-v2-ptv-hint">' + esc(extra) + '</p>' : '') + '</div>', "is-proc");
    }
    function processVoice(blob, secs, unitId) {
        var STEPS = ["Uitschrijven", "Punten herkennen", "Aannemer voorstellen"];
        processing("Verwerken", mss(secs) + " · " + unitLabel(unitId), [[STEPS[0], "act"], [STEPS[1], "todo"], [STEPS[2], "todo"]], "", "Meestal binnen 10 seconden.");
        var fd = new FormData(); fd.append("audio", blob, "opname." + extOf(blob.type)); if (unitId) fd.append("unitId", unitId);
        post("SpraakTranscribeV2", fd).then(function (j) {
            if (!j.ok) throw new Error(j.error || "Uitschrijven mislukt.");
            if (!j.text) throw new Error("Er is geen spraak herkend. Probeer opnieuw en spreek dichter bij de microfoon.");
            processing("Verwerken", mss(secs) + " · " + unitLabel(unitId), [[STEPS[0], "done"], [STEPS[1], "act"], [STEPS[2], "todo"]], j.text, "Meestal binnen 10 seconden.");
            var f2 = new FormData(); f2.append("text", j.text); if (unitId) f2.append("unitId", unitId);
            return post("SpraakVoorstelV2", f2).then(function (r) { if (!r.ok) throw new Error(r.error || "Punten herkennen mislukt."); return { text: j.text, proposals: r.proposals }; });
        }).then(function (r) {
            if (!r.proposals.length) throw new Error("Er zijn geen punten herkend in dit bericht.");
            openReview({ kind: "voice", transcript: r.text, seconds: secs, proposals: r.proposals, unitId: unitId });
        }).catch(function (err) {
            openScreen(head("Inspreken", "", true) + '<div class="gl-v2-ptv-body gl-v2-ptv-center"><p class="gl-v2-ptv-lead">' + esc(err.message) + '</p><button type="button" class="gl-v2-btn gl-v2-btn-primary" data-act="retry">Opnieuw proberen</button></div>', "is-voice");
            screenEl.onclick = function (e) { if (e.target.closest("[data-act=close]")) closeScreen(); else if (e.target.closest("[data-act=retry]")) openVoice(); };
        });
    }

    // ───────────────────────────── Controleren en bewaren (40f / 40h) ─────────────────────────────
    var audioEl = null;
    function openReview(job) {
        var props = job.proposals.map(function (p, i) { p._i = i; p._on = !p.existingNr; return p; });
        var filter = "all";
        function visible() { return props.filter(function (p) { return filter === "all" || (filter === "new" ? !p.existingNr : !!p.existingNr); }); }
        function whereOf(p) { return [p.unitName || "Algemeen", p.zone ? p.zone.charAt(0).toUpperCase() + p.zone.slice(1) : ""].filter(Boolean).join(" · "); }
        function rowHtml(p) {
            var photos = (p.photoIndexes || []).map(function (i) { return '<img src="' + base + '/RondgangFotoV2?id=' + encodeURIComponent(job.id) + '&i=' + i + '" alt="" loading="lazy">'; }).join("");
            return '<div class="gl-v2-ptv-prop' + (p._on ? "" : " is-off") + '" data-i="' + p._i + '">' +
                '<button type="button" class="gl-v2-ptv-tg' + (p._on ? " is-on" : "") + '" data-act="toggle" aria-pressed="' + p._on + '" aria-label="Dit punt bewaren"><i class="ph ph-check" aria-hidden="true"></i></button>' +
                '<div class="gl-v2-ptv-prop-main"><b>' + esc(p.title) + '</b><small>' + esc(whereOf(p) + (job.kind === "rondgang" && p.atSeconds != null ? " · " + mmss(p.atSeconds) : "") + " · " + (p.contractorName || "aannemer?")) + '</small>' +
                (p.existingNr ? '<span class="gl-v2-ptv-tag">bestaat al: P-' + ("00" + p.existingNr).slice(-3) + '</span>' : '') +
                (photos ? '<div class="gl-v2-ptv-prop-photos">' + photos + '</div>' : '') +
                '<div class="gl-v2-ptv-prop-act">' + (job.kind === "rondgang" && p.atSeconds != null ? '<button type="button" data-act="play"><i class="ph ph-play" aria-hidden="true"></i>Beluister</button>' : '') +
                '<button type="button" data-act="edit"><i class="ph ph-camera" aria-hidden="true"></i>Foto toevoegen</button><button type="button" data-act="edit"><i class="ph ph-map-trifold" aria-hidden="true"></i>Op plan</button><button type="button" data-act="edit"><i class="ph ph-pencil-simple" aria-hidden="true"></i>Wijzigen</button></div></div></div>';
        }
        function render() {
            var n = props.filter(function (p) { return p._on; }).length, nieuw = props.filter(function (p) { return !p.existingNr; }).length, best = props.length - nieuw;
            var title = job.kind === "rondgang" ? "Rondgang verwerkt" : props.length + (props.length === 1 ? " punt herkend" : " punten herkend");
            var sub = job.kind === "rondgang" ? mmss(job.seconds || 0) + " · " + props.length + " voorstellen" : "controleer en bewaar";
            var html = head(title, sub, true) + '<div class="gl-v2-ptv-body gl-v2-ptv-review">';
            if (job.kind === "voice") html += '<div class="gl-v2-ptv-quotecard"><span>JOUW BERICHT · ' + mss(job.seconds) + '</span><p>“' + esc(job.transcript) + '”</p></div>';
            else if (best) html += '<p class="gl-v2-ptv-note">Transcript en foto’s verwerkt. Ontdubbeld met ' + best + ' bestaande punten.</p>';
            if (job.kind === "rondgang") html += '<div class="gl-v2-ptv-seg"><button type="button" data-f="all" class="' + (filter === "all" ? "is-on" : "") + '">Alle ' + props.length + '</button><button type="button" data-f="new" class="' + (filter === "new" ? "is-on" : "") + '">Nieuw ' + nieuw + '</button><button type="button" data-f="old" class="' + (filter === "old" ? "is-on" : "") + '">Bestaand ' + best + '</button></div>';
            html += '<div class="gl-v2-ptv-props">' + visible().map(rowHtml).join("") + '</div></div>' +
                '<footer class="gl-v2-ptv-foot">' + (job.kind === "voice" ? '<button type="button" class="gl-v2-btn gl-v2-btn-secondary" data-act="again">Opnieuw</button>' : '<button type="button" class="gl-v2-btn gl-v2-btn-secondary" data-act="later">Later</button>') +
                '<button type="button" class="gl-v2-btn gl-v2-btn-primary" data-act="save"' + (n ? "" : " disabled") + '>' + n + (n === 1 ? " punt" : " punten") + (job.kind === "rondgang" ? " als concept bewaren" : " bewaren") + '</button></footer>';
            var scroll = screenEl && $(".gl-v2-ptv-body", screenEl) ? $(".gl-v2-ptv-body", screenEl).scrollTop : 0;
            openScreen(html, "is-review");
            var b = $(".gl-v2-ptv-body", screenEl); if (b) b.scrollTop = scroll;
        }
        render();
        screenEl.onclick = function (e) {
            var f = e.target.closest("[data-f]"); if (f) { filter = f.dataset.f; render(); return; }
            var row = e.target.closest(".gl-v2-ptv-prop"), p = row ? props[parseInt(row.dataset.i, 10)] : null, act = (e.target.closest("[data-act]") || {}).dataset;
            if (!act) return;
            if (act.act === "close" || act.act === "later") { if (audioEl) audioEl.pause(); closeScreen(); return; }
            if (act.act === "again") { openVoice(); return; }
            if (act.act === "toggle" && p) { p._on = !p._on; render(); return; }
            if (act.act === "play" && p) { playAt(job, p.atSeconds || 0); return; }
            if (act.act === "edit" && p) { editInPanel(job, p, function () { props = props.filter(function (x) { return x !== p; }); if (!props.length) { closeScreen(); location.reload(); } else render(); }); return; }
            if (act.act === "save") saveAll(job, props);
        };
    }
    function playAt(job, sec) {
        if (!audioEl) audioEl = new Audio();
        audioEl.src = base + "/RondgangAudioV2?id=" + encodeURIComponent(job.id);
        audioEl.addEventListener("loadedmetadata", function once() { audioEl.removeEventListener("loadedmetadata", once); try { audioEl.currentTime = Math.max(0, sec - 1); } catch (e) { /* niet doorspoelbaar */ } audioEl.play(); });
        audioEl.load();
    }
    // Wijzigen / Foto toevoegen / Op plan: het gewone nieuw-puntformulier, vooraf ingevuld; het voorstel verdwijnt na bewaren
    function editInPanel(job, p, onSaved) {
        if (!window.PuntPanel) return;
        var photos = job.kind === "rondgang" ? (p.photoIndexes || []) : [];
        Promise.all(photos.map(function (i) { return fetch(base + "/RondgangFotoV2?id=" + encodeURIComponent(job.id) + "&i=" + i, { credentials: "same-origin" }).then(function (r) { return r.blob(); }).then(function (b) { return new File([b], "foto-" + (i + 1) + ".jpg", { type: "image/jpeg" }); }); }))
            .catch(function () { return []; }).then(function (files) {
                window.PuntPanel.open("new", 0, { title: p.title, unitId: p.unitId, zone: p.zone, contractorId: p.contractorId, description: p.description, priority: p.priority, files: files, onSaved: onSaved });
            });
    }
    function saveAll(job, props) {
        var items = props.filter(function (p) { return p._on; }).map(function (p) { return { title: p.title, unitId: p.unitId, zone: p.zone, contractorId: p.contractorId, description: p.description, priority: p.priority, photoIndexes: p.photoIndexes || [] }; });
        if (!items.length) return;
        var btn = $("[data-act=save]", screenEl); if (btn) { btn.disabled = true; btn.textContent = "Bewaren…"; }
        var fd = new FormData(); fd.append("items", JSON.stringify(items)); if (job.id) fd.append("jobId", job.id); if (job.verslagId) fd.append("verslagId", job.verslagId);
        post("SpraakBewaarV2", fd).then(function (j) {
            if (!j.ok) throw new Error((j.errors && j.errors[0]) || j.error || "Bewaren mislukt.");
            try { sessionStorage.setItem("pt-toast", j.count + (j.count === 1 ? " punt" : " punten") + " bewaard als concept"); } catch (e) { /* niet erg */ }
            window.location.reload();
        }).catch(function (err) { toast(err.message); if (btn) { btn.disabled = false; btn.textContent = "Opnieuw proberen"; } });
    }

    // ───────────────────────────── Rondgang (41d) ─────────────────────────────
    function openRondgangStart() {
        ensureConfigured().then(function (ok) {
            if (!ok) return;
            if (!navigator.mediaDevices || !window.MediaRecorder) { toast("Opnemen wordt niet ondersteund op dit toestel."); return; }
            var chips = '<button type="button" class="gl-v2-ptv-chip is-on" data-u="all">Alle</button>' + units.map(function (u) { return '<button type="button" class="gl-v2-ptv-chip" data-u="' + u.id + '">' + esc(u.name) + '</button>'; }).join("");
            var vopts = '<option value="">Niet koppelen</option>' + verslagen.map(function (v, i) { return '<option value="' + v.id + '"' + (i === 0 ? " selected" : "") + '>' + esc(v.name) + '</option>'; }).join("");
            openScreen(head("Rondgang starten", root.dataset.projectName || "", true) +
                '<div class="gl-v2-ptv-body"><span class="gl-v2-ptv-lbl">WELKE EENHEDEN</span><div class="gl-v2-ptv-chips" id="ptv-units">' + chips + '</div><p class="gl-v2-ptv-hint2">helpt om vermeldingen te koppelen</p>' +
                (verslagen.length ? '<span class="gl-v2-ptv-lbl">KOPPELEN AAN VERSLAG</span><label class="gl-v2-ptv-sel"><select id="ptv-verslag">' + vopts + '</select></label>' : '') +
                '<div class="gl-v2-ptv-tips"><b>Tips</b><ul><li>Zeg eerst de eenheid en kamer: “lot twee, badkamer”.</li><li>Beschrijf daarna wat er moet gebeuren en voor wie.</li><li>Tik op het scherm voor een foto van dat moment.</li><li>Pauzeren mag.</li></ul></div></div>' +
                '<footer class="gl-v2-ptv-foot"><button type="button" class="gl-v2-btn gl-v2-btn-secondary" data-act="close">Annuleren</button><button type="button" class="gl-v2-btn gl-v2-btn-primary" data-act="start"><i class="ph ph-record" aria-hidden="true"></i>Start opname</button></footer>', "is-start");
            screenEl.onclick = function (e) {
                var c = e.target.closest("[data-u]");
                if (c) {
                    if (c.dataset.u === "all") $$("[data-u]", screenEl).forEach(function (x) { x.classList.toggle("is-on", x === c); });
                    else { $("[data-u=all]", screenEl).classList.remove("is-on"); c.classList.toggle("is-on"); if (!$$("[data-u].is-on", screenEl).length) $("[data-u=all]", screenEl).classList.add("is-on"); }
                    return;
                }
                if (e.target.closest("[data-act=close]")) { closeScreen(); return; }
                if (e.target.closest("[data-act=start]")) {
                    var sel = $$("[data-u].is-on", screenEl).map(function (x) { return x.dataset.u; }).filter(function (x) { return x !== "all"; });
                    var vs = $("#ptv-verslag", screenEl);
                    closeScreen(); startRondgang({ unitId: sel.length === 1 ? sel[0] : "", verslagId: vs ? vs.value : "" });
                }
            };
        });
    }

    var rg = null;
    function startRondgang(opt) {
        var wantVideo = { facingMode: { ideal: "environment" }, width: { ideal: 1280 } };
        navigator.mediaDevices.getUserMedia({ audio: { echoCancellation: true, noiseSuppression: true }, video: wantVideo }).catch(function () { return micStream(); }).then(function (stream) {
            var hasVideo = stream.getVideoTracks().length > 0;
            var el = document.createElement("div"); el.className = "gl-v2-ptcam gl-v2-ptrg"; el.setAttribute("role", "dialog"); el.setAttribute("aria-modal", "true"); el.setAttribute("aria-label", "Rondgang opnemen");
            el.innerHTML = '<div class="gl-v2-ptrg-top"><span class="gl-v2-ptrg-rec"><i></i>OPNAME · ' + esc((root.dataset.projectName || "").toUpperCase()) + '</span><span class="gl-v2-ptrg-time" id="ptrg-time">00:00</span></div>' +
                (hasVideo ? '<video class="gl-v2-ptcam-video" playsinline muted autoplay></video>' : '<div class="gl-v2-ptrg-novideo"><i class="ph ph-microphone" aria-hidden="true"></i><p>Alleen geluid. De camera is niet beschikbaar, dus foto’s nemen kan niet.</p></div>') +
                '<div class="gl-v2-ptcam-flash" aria-hidden="true"></div>' +
                '<div class="gl-v2-ptrg-bottom"><ol class="gl-v2-ptrg-marks" id="ptrg-marks"></ol><p class="gl-v2-ptrg-tip">Zeg de eenheid en de kamer hardop. Tik op het scherm voor een foto.</p>' +
                '<div class="gl-v2-ptrg-row"><button type="button" class="gl-v2-ptcam-side" data-act="pause"><i class="ph ph-pause" aria-hidden="true"></i><span>Pauze</span></button>' +
                '<button type="button" class="gl-v2-ptrg-stop" data-act="stop" aria-label="Opname stoppen"><span></span></button>' +
                '<span class="gl-v2-ptcam-side"><i class="ph ph-camera" aria-hidden="true"></i><span id="ptrg-n">0</span></span></div></div>';
            document.body.appendChild(el); document.body.classList.add("gl-v2-ptcam-on");
            if (hasVideo) { var v = $("video", el); v.srcObject = stream; v.play().catch(function () { }); }
            var audioOnly = new MediaStream(stream.getAudioTracks());
            rg = { el: el, stream: stream, handle: recordStream(audioOnly), t0: Date.now(), pausedAt: 0, pausedTotal: 0, photos: [], times: [], opt: opt, timer: null, wake: null };
            function elapsed() { return ((rg.pausedAt || Date.now()) - rg.t0 - rg.pausedTotal) / 1000; }
            rg.elapsed = elapsed;
            rg.timer = setInterval(function () { $("#ptrg-time", el).textContent = mmss(elapsed()); }, 500);
            try { if (navigator.wakeLock) navigator.wakeLock.request("screen").then(function (w) { rg.wake = w; }).catch(function () { }); } catch (e) { /* geen wake lock */ }
            el.addEventListener("click", function (e) {
                var b = e.target.closest("[data-act]");
                if (b && b.dataset.act === "pause") {
                    if (!rg.pausedAt) { rg.handle.pause(); rg.pausedAt = Date.now(); $("span", b).textContent = "Hervatten"; $("i", b).className = "ph ph-play"; el.classList.add("is-paused"); }
                    else { rg.handle.resume(); rg.pausedTotal += Date.now() - rg.pausedAt; rg.pausedAt = 0; $("span", b).textContent = "Pauze"; $("i", b).className = "ph ph-pause"; el.classList.remove("is-paused"); }
                    return;
                }
                if (b && b.dataset.act === "stop") { stopRondgang(); return; }
                if (hasVideo && !e.target.closest(".gl-v2-ptrg-bottom") && !e.target.closest(".gl-v2-ptrg-top")) snap();
            });
        }).catch(micError);
    }
    function snap() {
        var v = $("video", rg.el); if (!v || !v.videoWidth || rg.pausedAt) return;
        var sc = Math.min(1, 1600 / Math.max(v.videoWidth, v.videoHeight)), c = document.createElement("canvas");
        c.width = Math.round(v.videoWidth * sc); c.height = Math.round(v.videoHeight * sc); c.getContext("2d").drawImage(v, 0, 0, c.width, c.height);
        var t = rg.elapsed(), fl = $(".gl-v2-ptcam-flash", rg.el); fl.classList.remove("is-on"); void fl.offsetWidth; fl.classList.add("is-on");
        c.toBlob(function (b) {
            if (!b) return; rg.photos.push(b); rg.times.push(t);
            $("#ptrg-n", rg.el).textContent = rg.photos.length;
            var li = document.createElement("li"); li.innerHTML = '<b>' + mmss(t) + '</b><span>foto ' + rg.photos.length + '</span>'; $("#ptrg-marks", rg.el).appendChild(li);
        }, "image/jpeg", 0.82);
    }
    function stopRondgang() {
        var r = rg; rg = null; clearInterval(r.timer); var secs = (r.pausedAt ? r.pausedAt : Date.now()) - r.t0 - r.pausedTotal; secs = secs / 1000;
        try { if (r.wake) r.wake.release(); } catch (e) { /* niets */ }
        r.handle.stop().then(function (blob) {
            stopTracks(r.stream); r.el.remove(); document.body.classList.remove("gl-v2-ptcam-on");
            if (secs < 5 || blob.size < 2000) { toast("De opname is te kort om te verwerken."); return; }
            uploadRondgang(blob, secs, r);
        });
    }
    function uploadRondgang(blob, secs, r) {
        var fd = new FormData(); fd.append("audio", blob, "rondgang." + extOf(blob.type)); fd.append("duration", String(secs));
        r.photos.forEach(function (b, i) { fd.append("photos", b, "foto-" + (i + 1) + ".jpg"); });
        fd.append("photoTimes", r.times.map(function (t) { return t.toFixed(1); }).join(",")); if (r.opt.unitId) fd.append("unitId", r.opt.unitId); if (r.opt.verslagId) fd.append("verslagId", r.opt.verslagId);
        fd.append("__RequestVerificationToken", token());
        banner({ state: "upload", percent: 0, duration: secs });
        var xhr = new XMLHttpRequest(); xhr.open("POST", base + "/RondgangStartV2"); xhr.withCredentials = true;
        xhr.upload.onprogress = function (e) { if (e.lengthComputable) banner({ state: "upload", percent: Math.round(e.loaded / e.total * 100), duration: secs }); };
        xhr.onload = function () {
            var j = {}; try { j = JSON.parse(xhr.responseText); } catch (e) { /* leeg */ }
            if (!j.ok) { banner({ state: "fout", error: j.error || "Uploaden mislukt." }); return; }
            pollRondgang(j.id, secs);
        };
        xhr.onerror = function () { banner({ state: "fout", error: "Uploaden mislukt. Controleer je verbinding." }); };
        xhr.send(fd);
    }

    // ── balk op de lijst: verwerkt in de achtergrond (40h stap 3) ──
    var bannerEl = null, lastJob = null, curId = null, curState = "";
    function banner(s) {
        if (!bannerEl) { bannerEl = document.createElement("div"); bannerEl.className = "gl-v2-ptrg-banner"; bannerEl.setAttribute("role", "status"); root.insertBefore(bannerEl, root.firstChild); }
        var html;
        if (s.state === "upload") html = '<div class="gl-v2-ptrg-b-text"><b>Rondgang ' + mmss(s.duration) + ' wordt geüpload</b><small>Blijf op deze pagina tot het klaar is.</small></div><div class="gl-v2-ptrg-b-bar"><i style="transform:scaleX(' + (s.percent / 100) + ')"></i></div><span class="gl-v2-ptrg-b-pct">' + s.percent + ' %</span>';
        else if (s.state === "bezig") html = '<div class="gl-v2-ptrg-b-text"><b>Rondgang ' + mmss(s.duration) + ' wordt verwerkt</b><small>' + esc(s.step || "") + ' · je mag verder werken</small></div><div class="gl-v2-ptrg-b-bar"><i style="transform:scaleX(' + (s.percent / 100) + ')"></i></div><span class="gl-v2-ptrg-b-pct">' + s.percent + ' %</span>';
        else if (s.state === "klaar") html = '<button type="button" class="gl-v2-ptrg-b-open" data-act="open-rondgang"><span class="gl-v2-ptrg-b-text"><b>Rondgang verwerkt</b><small>' + s.count + ' punten voorgesteld · tik om te controleren</small></span><i class="ph ph-caret-right" aria-hidden="true"></i></button>';
        else html = '<div class="gl-v2-ptrg-b-text is-err"><b>Rondgang niet verwerkt</b><small>' + esc(s.error || "") + '</small></div><button type="button" class="gl-v2-ptrg-b-x" data-act="close-banner" aria-label="Sluiten"><i class="ph ph-x" aria-hidden="true"></i></button>';
        bannerEl.className = "gl-v2-ptrg-banner is-" + s.state; bannerEl.innerHTML = html;
    }
    function pollRondgang(id, secs) {
        curId = id;
        var tick = function () {
            fetch(base + "/RondgangStatusV2?id=" + encodeURIComponent(id), { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) {
                if (!j.ok) { if (bannerEl) { bannerEl.remove(); bannerEl = null; } if (!j.gone) banner({ state: "fout", error: j.error }); return; }
                curState = j.state;
                if (j.state === "bezig") { banner({ state: "bezig", percent: j.percent, step: j.step, duration: j.duration || secs }); setTimeout(tick, 3000); return; }
                if (j.state === "fout") { banner({ state: "fout", error: j.error }); return; }
                lastJob = { kind: "rondgang", id: id, transcript: j.transcript, seconds: j.duration || secs, proposals: j.proposals, verslagId: j.verslagId };
                banner({ state: "klaar", count: j.proposals.length });
            }).catch(function () { setTimeout(tick, 5000); });
        };
        tick();
    }
    document.addEventListener("click", function (e) {
        if (e.target.closest("[data-act=open-rondgang]") && lastJob) { openReview(lastJob); return; }
        if (e.target.closest("[data-act=close-banner]")) {
            // een mislukte rondgang weggooien, anders blijft de balk bij elk bezoek terugkomen
            if (curId && curState === "fout") { var fd = new FormData(); fd.append("id", curId); post("RondgangVerwijderV2", fd); }
            if (bannerEl) { bannerEl.remove(); bannerEl = null; }
            return;
        }
        if (e.target.closest("#pt-qa-voice")) { openVoice(); return; }
        if (e.target.closest("#pt-qa-rondgang")) { openRondgangStart(); return; }
    });
    // een rondgang die nog niet bewaard is (staat in de database): ook na het sluiten van de app of op een ander toestel
    if (canWrite) fetch(base + "/RondgangLopendV2", { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) {
        if (j && j.ok && !j.none && j.id) pollRondgang(j.id, j.duration || 0);
    }).catch(function () { /* geen balk */ });

    // ───────────────────────────── microfoon bij de titel (41a stap 2) ─────────────────────────────
    var title = document.getElementById("pt-new-title");
    if (title) {
        var box = title.closest(".gl-v2-field-box");
        if (box) {
            var mb = document.createElement("button"); mb.type = "button"; mb.className = "gl-v2-pt-mic"; mb.setAttribute("aria-label", "Titel inspreken"); mb.innerHTML = '<i class="ph ph-microphone" aria-hidden="true"></i>';
            box.appendChild(mb);
            var t = null;
            mb.addEventListener("click", function () {
                if (t) { var h = t; t = null; mb.classList.remove("is-rec"); h.handle.stop().then(function (blob) {
                    stopTracks(h.stream); if (blob.size < 1000) return;
                    mb.classList.add("is-busy"); var fd = new FormData(); fd.append("audio", blob, "titel." + extOf(blob.type));
                    var u = document.getElementById("pt-new-unit"); if (u && u.value && u.value !== "0") fd.append("unitId", u.value);
                    post("SpraakTranscribeV2", fd).then(function (j) {
                        if (!j.ok) throw new Error(j.error || "Uitschrijven mislukt.");
                        var txt = (j.text || "").trim().replace(/[.!?]+$/, ""); if (txt) { title.value = txt.charAt(0).toUpperCase() + txt.slice(1); title.dispatchEvent(new Event("input", { bubbles: true })); }
                    }).catch(function (err) { toast(err.message); }).then(function () { mb.classList.remove("is-busy"); title.focus(); });
                }); return; }
                ensureConfigured().then(function (ok) {
                    if (!ok) return;
                    micStream().then(function (s) { t = { stream: s, handle: recordStream(s) }; mb.classList.add("is-rec"); }).catch(micError);
                });
            });
        }
    }
})();
