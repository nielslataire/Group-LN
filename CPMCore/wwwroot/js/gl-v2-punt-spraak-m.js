// gl-v2 — Punten: Inspreken (design-handoff 41c, 40f) en Rondgang (41d, 40g/40h) op gsm, plus het microfoontje bij de titel (41a stap 2).
// Opnemen gebeurt in de browser (MediaRecorder). Spraak naar tekst (OpenAI) en het herkennen van punten (Anthropic) gebeuren op de server
// (ProjectIssuesController.Spraak.cs). Een voorstel bewaren = een concept aanmaken; foto's (camera/galerij) en een plek op het plan kunnen per voorstel
// nog toegevoegd worden vóór het bewaren.
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
    function canWrite() { return root.dataset.canWrite === "1"; }
    var BACK = '<i class="ph ph-arrow-left" aria-hidden="true"></i>';
    var CHECK = '<svg viewBox="0 0 16 16"><path d="M3.4 8.4l3 3 6.2-6.8"/></svg>';

    // ── is spraak ingesteld? (sleutels op de server) ──
    var configured = null;
    function ensureConfigured() {
        if (configured === true) return Promise.resolve(true);
        return fetch(base + "/SpraakStatusV2", { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) {
            configured = !!j.configured;
            if (!configured) toast("Inspreken is nog niet ingesteld: de server mist " + (j.missing || "de sleutels voor spraak en AI") + ". Herstart de server na het toevoegen.");
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
    // iPhone (Safari) neemt op als AAC in een mp4-bestand: m4a is de naam die de spraakdienst daarvoor kent
    function extOf(mime) { return /mp4|aac|m4a/.test(mime) ? "m4a" : /ogg/.test(mime) ? "ogg" : "webm"; }
    // start een opname op een (audio)stream; geeft een handvat met stop() -> Blob, pause(), resume()
    function recordStream(stream) {
        var mime = pickMime(), chunks = [], rec = new MediaRecorder(stream, mime ? { mimeType: mime, audioBitsPerSecond: 32000 } : { audioBitsPerSecond: 32000 });
        var done = new Promise(function (res) { rec.onstop = function () { res(new Blob(chunks, { type: rec.mimeType || mime || "audio/webm" })); }; });
        rec.ondataavailable = function (e) { if (e.data && e.data.size) chunks.push(e.data); };
        // Safari levert met een tijdsinterval stukjes (gefragmenteerde mp4) die samen geen geldig bestand vormen: daar één opname als geheel
        if (/mp4/.test(rec.mimeType || mime || "")) rec.start(); else rec.start(1000);
        return { rec: rec, stop: function () { if (rec.state !== "inactive") rec.stop(); return done; }, pause: function () { if (rec.state === "recording") rec.pause(); }, resume: function () { if (rec.state === "paused") rec.resume(); } };
    }
    function micStream() { return navigator.mediaDevices.getUserMedia({ audio: { echoCancellation: true, noiseSuppression: true }, video: false }); }
    function stopTracks(s) { if (s) s.getTracks().forEach(function (t) { t.stop(); }); }
    function micError() { toast("De microfoon is niet beschikbaar. Controleer of je de microfoon hebt toegestaan voor deze website."); }

    // ── gedeeld scherm: groene kop met terugpijl, lichte achtergrond (zoals de ontwerpen 41c/41d) ──
    var screenEl = null;
    function openScreen(html, cls) {
        if (!screenEl) { screenEl = document.createElement("div"); screenEl.className = "gl-v2-ptv"; screenEl.setAttribute("role", "dialog"); screenEl.setAttribute("aria-modal", "true"); document.body.appendChild(screenEl); }
        screenEl.className = "gl-v2-ptv " + (cls || ""); screenEl.innerHTML = html; screenEl.hidden = false; document.body.classList.add("gl-v2-ptcam-on");
        return screenEl;
    }
    function closeScreen() { if (screenEl) { screenEl.hidden = true; screenEl.innerHTML = ""; screenEl.onclick = null; } document.body.classList.remove("gl-v2-ptcam-on"); }
    function head(title, sub) {
        return '<header class="gl-v2-ptv-head"><button type="button" class="gl-v2-ptv-back" data-act="close" aria-label="Terug">' + BACK + '</button><div><b>' + esc(title) + '</b>' + (sub ? '<small>' + sub + '</small>' : '') + '</div></header>';
    }
    function unitLabel(id) { var u = units.filter(function (x) { return String(x.id) === String(id); })[0]; return u ? u.name : "Algemeen"; }

    // ───────────────────────────── Inspreken (41c) ─────────────────────────────
    var voice = null, BARS = [8, 48, 18, 45, 28, 40, 36, 33, 42, 24, 46, 14, 48, 12, 47, 22, 43, 31, 37, 39, 29, 44, 20, 47, 10, 48, 17, 46, 26, 41];
    function openVoice() {
        ensureConfigured().then(function (ok) {
            if (!ok) return;
            if (!navigator.mediaDevices || !window.MediaRecorder) { toast("Opnemen wordt niet ondersteund op dit toestel."); return; }
            var l = last(), unitId = l.unitId != null ? String(l.unitId) : "";
            var opts = '<option value="">Algemeen</option>' + units.map(function (u) { return '<option value="' + u.id + '"' + (String(u.id) === unitId ? " selected" : "") + '>' + esc(u.name) + '</option>'; }).join("");
            // de eenheid kies je via de ondertitel van de kop ("Lot 2 · laatst gebruikt ▾"), zoals in het ontwerp
            var sub = '<label class="gl-v2-ptv-unitsub"><span id="ptv-unit-txt">' + esc(unitLabel(unitId)) + (l.unitId != null ? " · laatst gebruikt" : "") + ' ▾</span><select id="ptv-unit" aria-label="Eenheid">' + opts + '</select></label>';
            var bars = BARS.map(function (h) { return '<span style="height:' + h + 'px"></span>'; }).join("");
            var el = openScreen(head("Inspreken", sub) +
                '<div class="gl-v2-ptv-body gl-v2-ptv-center"><p class="gl-v2-ptv-lead">Zeg wat er moet gebeuren, waar, en wie het moet doen. Meerdere punten mag.</p>' +
                '<div class="gl-v2-ptv-wave" id="ptv-wave" aria-hidden="true">' + bars + '</div>' +
                '<div class="gl-v2-ptv-timer" id="ptv-timer">0:00</div>' +
                '<button type="button" class="gl-v2-ptv-mic" id="ptv-mic" aria-label="Opnemen"><i class="ph ph-microphone" aria-hidden="true"></i></button>' +
                '<p class="gl-v2-ptv-hint" id="ptv-hint">Houd ingedrukt · loslaten = klaar</p></div>', "is-voice");
            voice = { stream: null, handle: null, started: 0, timer: null, toggle: false, audio: null };
            var mic = $("#ptv-mic", el), timer = $("#ptv-timer", el), hint = $("#ptv-hint", el), wave = $("#ptv-wave", el), spans = $$("span", wave);
            var sel = $("#ptv-unit", el);
            sel.addEventListener("change", function () { $("#ptv-unit-txt", el).textContent = (sel.value ? unitLabel(sel.value) : "Algemeen") + " ▾"; });
            function meter(stream) {
                // het geluidsniveau laat de balken bewegen; lukt dat niet, dan bewegen ze via CSS
                try {
                    var AC = window.AudioContext || window.webkitAudioContext, ac = new AC(), an = ac.createAnalyser(), src = ac.createMediaStreamSource(stream), buf = new Uint8Array(256);
                    an.fftSize = 256; src.connect(an); var lv = new Array(spans.length).fill(0.08);
                    voice.audio = { ac: ac, tick: setInterval(function () {
                        an.getByteTimeDomainData(buf); var sum = 0; for (var i = 0; i < buf.length; i++) { var v = (buf[i] - 128) / 128; sum += v * v; }
                        lv.shift(); lv.push(Math.min(1, Math.sqrt(sum / buf.length) * 5 + 0.08));
                        spans.forEach(function (s, i) { s.style.transform = "scaleY(" + lv[i].toFixed(2) + ")"; });
                    }, 80) };
                } catch (e) { wave.classList.add("is-css"); }
            }
            function stopMeter() { if (voice && voice.audio) { clearInterval(voice.audio.tick); try { voice.audio.ac.close(); } catch (e) { /* niets */ } } }
            function begin() {
                if (voice.handle) return;
                micStream().then(function (s) {
                    voice.stream = s; voice.handle = recordStream(s); voice.started = Date.now(); mic.classList.add("is-rec"); wave.classList.add("is-rec"); hint.textContent = "Aan het opnemen…"; meter(s);
                    voice.timer = setInterval(function () { timer.textContent = mss((Date.now() - voice.started) / 1000); }, 250);
                }).catch(micError);
            }
            function finish() {
                if (!voice || !voice.handle) return;
                var secs = (Date.now() - voice.started) / 1000, uid = sel.value; clearInterval(voice.timer); stopMeter();
                voice.handle.stop().then(function (blob) {
                    stopTracks(voice.stream); voice = null;
                    if (secs < 1.2 || blob.size < 1000) { toast("Dat was te kort. Houd de knop ingedrukt terwijl je spreekt."); closeScreen(); return; }
                    processVoice(blob, secs, uid);
                });
            }
            var downAt = 0;
            mic.addEventListener("pointerdown", function (e) { e.preventDefault(); downAt = Date.now(); if (voice.toggle && voice.handle) { finish(); return; } begin(); });
            mic.addEventListener("pointerup", function () { if (!voice || !voice.handle) return; if (Date.now() - downAt < 450) { voice.toggle = true; hint.textContent = "Tik nog eens om te stoppen"; } else finish(); });
            mic.addEventListener("pointercancel", function () { if (voice && voice.handle && !voice.toggle) finish(); });
            mic.addEventListener("contextmenu", function (e) { e.preventDefault(); });
            el.onclick = function (e) { if (e.target.closest("[data-act=close]")) { if (voice) { clearInterval(voice.timer); stopMeter(); if (voice.handle) voice.handle.stop(); stopTracks(voice.stream); voice = null; } closeScreen(); } };
        });
    }
    // verwerken: één kaart per stap (afgevinkt / bezig / nog te doen), daaronder het uitgeschreven bericht (41c stap 3)
    function stepCards(steps) {
        return steps.map(function (s) {
            var ico = s[1] === "done" ? '<span class="gl-v2-ptv-sc is-done">' + CHECK + '</span>' : '<span class="gl-v2-ptv-sc is-' + s[1] + '"><i></i></span>';
            return '<div class="gl-v2-ptv-stepcard is-' + s[1] + '">' + ico + '<b>' + esc(s[0]) + '</b><em>' + esc(s[1] === "done" ? "klaar" : s[1] === "act" ? "bezig…" : "") + '</em></div>';
        }).join("");
    }
    function processing(sub, steps, quote) {
        openScreen(head("Verwerken", esc(sub)) + '<div class="gl-v2-ptv-body">' + stepCards(steps) +
            (quote ? '<div class="gl-v2-ptv-quote">“' + esc(quote.length > 220 ? quote.slice(0, 220) + "…" : quote) + '”</div>' : '') +
            '<p class="gl-v2-ptv-hint2">Meestal binnen 10 seconden.</p></div>', "is-proc");
    }
    function processVoice(blob, secs, unitId) {
        var STEPS = ["Uitschrijven", "Punten herkennen", "Aannemer voorstellen"], sub = mss(secs) + (unitId ? " · " + unitLabel(unitId) : "");
        processing(sub, [[STEPS[0], "act"], [STEPS[1], "todo"], [STEPS[2], "todo"]], "");
        var fd = new FormData(); fd.append("audio", blob, "opname." + extOf(blob.type)); if (unitId) fd.append("unitId", unitId);
        post("SpraakTranscribeV2", fd).then(function (j) {
            if (!j.ok) throw new Error(j.error || "Uitschrijven mislukt.");
            if (!j.text) throw new Error("Er is geen spraak herkend. Probeer opnieuw en spreek dichter bij de microfoon.");
            processing(sub, [[STEPS[0], "done"], [STEPS[1], "act"], [STEPS[2], "todo"]], j.text);
            var f2 = new FormData(); f2.append("text", j.text); if (unitId) f2.append("unitId", unitId);
            return post("SpraakVoorstelV2", f2).then(function (r) {
                if (!r.ok) throw new Error(r.error || "Punten herkennen mislukt.");
                processing(sub, [[STEPS[0], "done"], [STEPS[1], "done"], [STEPS[2], "done"]], j.text);
                return { text: j.text, proposals: r.proposals };
            });
        }).then(function (r) {
            if (!r.proposals.length) throw new Error("Er zijn geen punten herkend in dit bericht.");
            openReview({ kind: "voice", transcript: r.text, seconds: secs, proposals: r.proposals, unitId: unitId });
        }).catch(function (err) {
            openScreen(head("Inspreken", "") + '<div class="gl-v2-ptv-body gl-v2-ptv-center"><p class="gl-v2-ptv-lead">' + esc(err.message) + '</p><button type="button" class="gl-v2-btn gl-v2-btn-primary" data-act="retry">Opnieuw proberen</button></div>', "is-voice");
            screenEl.onclick = function (e) { if (e.target.closest("[data-act=close]")) closeScreen(); else if (e.target.closest("[data-act=retry]")) openVoice(); };
        });
    }

    // ───────────────────────────── Controleren en bewaren (41c stap 4, 41d stap 4) ─────────────────────────────
    var audioEl = null;
    function openReview(job) {
        var props = job.proposals.map(function (p, i) { p._i = i; p._on = !p.existingNr; p._files = []; p._plan = null; return p; });
        var filter = "all";
        function visible() { return props.filter(function (p) { return filter === "all" || (filter === "new" ? !p.existingNr : !!p.existingNr); }); }
        function whereOf(p) { return [p.unitName || "Algemeen", p.zone ? p.zone.charAt(0).toUpperCase() + p.zone.slice(1) : ""].filter(Boolean).join(" · "); }
        function attached(p) {
            var bits = [];
            if (p._files.length) bits.push('<i class="ph ph-camera" aria-hidden="true"></i>' + p._files.length + (p._files.length === 1 ? " foto" : " foto’s"));
            if (p._plan) bits.push('<i class="ph ph-map-pin" aria-hidden="true"></i>' + esc(p._plan.name || "op plan"));
            return bits.length ? '<span class="gl-v2-ptv-att">' + bits.join('<span class="gl-v2-ptv-dot">·</span>') + '</span>' : "";
        }
        function links() {
            return '<span class="gl-v2-ptv-links"><button type="button" data-act="edit">Wijzigen</button><span>·</span><button type="button" data-act="photo">Foto</button><span>·</span><button type="button" data-act="plan">Op plan</button></span>';
        }
        function rowVoice(p) {
            return '<div class="gl-v2-ptv-card' + (p._on ? "" : " is-off") + '" data-i="' + p._i + '">' +
                '<div class="gl-v2-ptv-card-top"><button type="button" class="gl-v2-ptv-cb' + (p._on ? " is-on" : "") + '" data-act="toggle" aria-pressed="' + p._on + '" aria-label="Dit punt bewaren">' + CHECK + '</button><b>' + esc(p.title) + '</b></div>' +
                '<span class="gl-v2-ptv-sub">' + esc(whereOf(p) + " · " + (p.contractorName || "aannemer?")) + '</span>' +
                (p.existingNr ? '<span class="gl-v2-ptv-sub is-note">bestaat al: P-' + ("00" + p.existingNr).slice(-3) + '</span>' : '') + attached(p) + links() + '</div>';
        }
        function rowRondgang(p) {
            var idx = (p.photoIndexes || [])[0], src = idx != null ? base + "/RondgangFotoV2?id=" + encodeURIComponent(job.id) + "&i=" + idx : "";
            var thumb = '<button type="button" class="gl-v2-ptv-thumb' + (src ? " has-img" : "") + '" data-act="play" aria-label="Beluister" ' + (src ? 'style="background-image:url(' + src + ')"' : "") + '><i class="ph ph-play" aria-hidden="true"></i></button>';
            var n = (p.photoIndexes || []).length;
            return '<div class="gl-v2-ptv-row' + (p._on ? "" : " is-off") + '" data-i="' + p._i + '">' + thumb +
                '<div class="gl-v2-stack gl-v2-ptv-row-main"><b>' + esc(p.title) + '</b><small>' + esc(whereOf(p) + (p.atSeconds != null ? " · " + mmss(p.atSeconds) : "")) + (n ? " · " + n + (n === 1 ? " foto" : " foto’s") : "") + '</small>' +
                (p.existingNr ? '<span class="gl-v2-ptv-who">bestaat al: P-' + ("00" + p.existingNr).slice(-3) + '</span>' : (p.contractorName ? '<span class="gl-v2-ptv-who">' + esc(p.contractorName) + '</span>' : '<span class="gl-v2-ptv-who is-warn">aannemer kiezen</span>')) + attached(p) + links() + '</div>' +
                (p.existingNr ? '<span class="gl-v2-ptv-badge">bestaat al</span>' : '') +
                '<button type="button" class="gl-v2-ptv-cb sm' + (p._on ? " is-on" : "") + '" data-act="toggle" aria-pressed="' + p._on + '" aria-label="Dit punt bewaren">' + CHECK + '</button></div>';
        }
        function render() {
            var n = props.filter(function (p) { return p._on; }).length, nieuw = props.filter(function (p) { return !p.existingNr; }).length, best = props.length - nieuw;
            var title = job.kind === "rondgang" ? "Rondgang verwerkt" : props.length + (props.length === 1 ? " punt herkend" : " punten herkend");
            var sub = job.kind === "rondgang" ? mmss(job.seconds || 0) + " · " + props.length + " voorstellen" : "controleer en bewaar";
            var html = head(title, esc(sub)) + '<div class="gl-v2-ptv-body gl-v2-ptv-review">';
            if (job.kind === "rondgang") {
                if (best) html += '<p class="gl-v2-ptv-note">Transcript en foto’s verwerkt. Ontdubbeld met ' + best + ' bestaande punten.</p>';
                html += '<div class="gl-v2-ptv-seg"><button type="button" data-f="all" class="' + (filter === "all" ? "is-on" : "") + '">Alle ' + props.length + '</button><button type="button" data-f="new" class="' + (filter === "new" ? "is-on" : "") + '">Nieuw ' + nieuw + '</button><button type="button" data-f="old" class="' + (filter === "old" ? "is-on" : "") + '">Bestaand ' + best + '</button></div>';
            }
            html += '<div class="gl-v2-ptv-props">' + visible().map(job.kind === "rondgang" ? rowRondgang : rowVoice).join("") + '</div></div>' +
                '<footer class="gl-v2-ptv-foot">' + (job.kind === "voice" ? '<button type="button" class="gl-v2-btn gl-v2-btn-secondary" data-act="again">Opnieuw</button>' : '<button type="button" class="gl-v2-btn gl-v2-btn-secondary" data-act="later">Later</button>') +
                '<button type="button" class="gl-v2-btn gl-v2-btn-primary" data-act="save"' + (n ? "" : " disabled") + '>' + n + (n === 1 ? " punt" : " punten") + (job.kind === "rondgang" ? " als concept bewaren" : " bewaren") + '</button></footer>';
            var keep = screenEl && $(".gl-v2-ptv-body", screenEl) ? $(".gl-v2-ptv-body", screenEl).scrollTop : 0;
            openScreen(html, "is-review");
            var b = $(".gl-v2-ptv-body", screenEl); if (b) b.scrollTop = keep;
            screenEl.onclick = onClick;
        }
        function onClick(e) {
            var f = e.target.closest("[data-f]"); if (f) { filter = f.dataset.f; render(); return; }
            var row = e.target.closest("[data-i]"), p = row ? props[parseInt(row.dataset.i, 10)] : null, a = (e.target.closest("[data-act]") || {}).dataset;
            if (!a) return;
            var act = a.act;
            if (act === "close" || act === "later") { if (audioEl) audioEl.pause(); closeScreen(); return; }
            if (act === "again") { openVoice(); return; }
            if (act === "toggle" && p) { p._on = !p._on; render(); return; }
            if (act === "play" && p) { playAt(job, p.atSeconds || 0); return; }
            if (act === "photo" && p) {   // de camera opent meteen: een nieuwe foto nemen of er een uit de galerij kiezen
                if (!window.PuntCamera) return;
                window.PuntCamera.open(function (files) { p._files = p._files.concat(files); render(); }, p.title);
                return;
            }
            if (act === "plan" && p) {    // de plannen van de eenheid komen meteen in beeld; daarna het punt aanduiden
                if (!window.PuntPanel || !window.PuntPanel.pickPlan) return;
                window.PuntPanel.pickPlan({ unitId: p.unitId, title: p.title }, function (res, msg) {
                    if (msg) toast(msg);
                    if (res) { p._plan = res; render(); }
                });
                return;
            }
            if (act === "edit" && p) { editInPanel(job, p, function () { props = props.filter(function (x) { return x !== p; }); if (!props.length) { closeScreen(); location.reload(); } else render(); }); return; }
            if (act === "save") saveAll(job, props);
        }
        render();
    }
    function playAt(job, sec) {
        if (!audioEl) audioEl = new Audio();
        audioEl.src = base + "/RondgangAudioV2?id=" + encodeURIComponent(job.id);
        audioEl.addEventListener("loadedmetadata", function once() { audioEl.removeEventListener("loadedmetadata", once); try { audioEl.currentTime = Math.max(0, sec - 1); } catch (e) { /* niet doorspoelbaar */ } audioEl.play(); });
        audioEl.load();
    }
    // Wijzigen: het gewone nieuw-puntformulier, vooraf ingevuld (met de foto's en het plan die al gekozen zijn); het voorstel verdwijnt na bewaren
    function editInPanel(job, p, onSaved) {
        if (!window.PuntPanel) return;
        var photos = job.kind === "rondgang" ? (p.photoIndexes || []) : [];
        Promise.all(photos.map(function (i) { return fetch(base + "/RondgangFotoV2?id=" + encodeURIComponent(job.id) + "&i=" + i, { credentials: "same-origin" }).then(function (r) { return r.blob(); }).then(function (b) { return new File([b], "foto-" + (i + 1) + ".jpg", { type: "image/jpeg" }); }); }))
            .catch(function () { return []; }).then(function (files) {
                var pre = { title: p.title, unitId: p.unitId, zone: p.zone, contractorId: p.contractorId, description: p.description, priority: p.priority, files: files.concat(p._files || []), onSaved: onSaved };
                if (p._plan && p._plan.key) { pre.planKey = p._plan.key; pre.page = p._plan.page; pre.x = p._plan.x; pre.y = p._plan.y; }
                window.PuntPanel.open("new", 0, pre);
            });
    }
    function saveAll(job, props) {
        var chosen = props.filter(function (p) { return p._on; });
        if (!chosen.length) return;
        var btn = $("[data-act=save]", screenEl); if (btn) { btn.disabled = true; btn.textContent = "Bewaren…"; }
        var fd = new FormData();
        fd.append("items", JSON.stringify(chosen.map(function (p) {
            return { idx: p._i, title: p.title, unitId: p.unitId, zone: p.zone, contractorId: p.contractorId, description: p.description, priority: p.priority, photoIndexes: p.photoIndexes || [],
                planId: p._plan ? p._plan.planId : null, planPage: p._plan ? p._plan.page : null, planX: p._plan ? p._plan.x : null, planY: p._plan ? p._plan.y : null };
        })));
        chosen.forEach(function (p) { p._files.forEach(function (f) { fd.append("foto_" + p._i, f, f.name || "foto.jpg"); }); });
        if (job.id) fd.append("jobId", job.id); if (job.verslagId) fd.append("verslagId", job.verslagId);
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
            openScreen(head("Rondgang starten", esc(root.dataset.projectName || "")) +
                '<div class="gl-v2-ptv-body"><div class="gl-v2-ptv-grp"><span class="gl-v2-ptv-lbl">WELKE EENHEDEN</span><div class="gl-v2-ptv-chips" id="ptv-units">' + chips + '</div><span class="gl-v2-ptv-hint3">helpt om vermeldingen te koppelen</span></div>' +
                (verslagen.length ? '<div class="gl-v2-ptv-grp"><span class="gl-v2-ptv-lbl">KOPPELEN AAN VERSLAG</span><label class="gl-v2-ptv-sel"><select id="ptv-verslag">' + vopts + '</select><i class="ph ph-caret-down" aria-hidden="true"></i></label></div>' : '') +
                '<div class="gl-v2-ptv-tips"><b>Tips</b><span>• Zeg eerst de eenheid en kamer: “lot twee, badkamer”.<br>• Beschrijf daarna wat er moet gebeuren en voor wie.<br>• Tik op het scherm of op de cameraknop voor een foto van dat moment.<br>• Pauzeren mag; de opname wordt achteraf verwerkt.</span></div></div>' +
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
        // beeld + geluid: het beeld is de zoeker waarmee je foto's neemt (er wordt geen video bewaard, enkel het geluid en je foto's)
        navigator.mediaDevices.getUserMedia({ audio: { echoCancellation: true, noiseSuppression: true }, video: { facingMode: { ideal: "environment" }, width: { ideal: 1280 } } }).catch(function () { return micStream(); }).then(function (stream) {
            var hasVideo = stream.getVideoTracks().length > 0;
            var el = document.createElement("div"); el.className = "gl-v2-ptrg"; el.setAttribute("role", "dialog"); el.setAttribute("aria-modal", "true"); el.setAttribute("aria-label", "Rondgang opnemen");
            el.innerHTML = '<span class="gl-v2-ptrg-rec"><i></i>OPNAME · ' + esc((root.dataset.projectName || "").toUpperCase()) + '</span><span class="gl-v2-ptrg-time" id="ptrg-time">00:00</span>' +
                '<div class="gl-v2-ptrg-cam" id="ptrg-cam">' + (hasVideo ? '' : '<div class="gl-v2-ptrg-novideo"><i class="ph ph-microphone" aria-hidden="true"></i><p>Alleen geluid. De camera is niet beschikbaar, dus foto’s nemen kan niet.</p></div>') + '<div class="gl-v2-ptrg-flash" aria-hidden="true"></div>' + (hasVideo ? '<span class="gl-v2-ptrg-taphint">Tik voor een foto</span>' : '') + '</div>' +
                '<div class="gl-v2-ptrg-marks" id="ptrg-marks"></div>' +
                '<div class="gl-v2-ptrg-ctl"><button type="button" class="gl-v2-ptrg-circle" data-act="pause" aria-label="Pauze"><i class="ph ph-pause" aria-hidden="true"></i></button>' +
                '<button type="button" class="gl-v2-ptrg-stop" data-act="stop" aria-label="Opname stoppen"><span></span></button>' +
                '<button type="button" class="gl-v2-ptrg-circle" data-act="snap" aria-label="Foto nemen"' + (hasVideo ? '' : ' disabled') + '><i class="ph ph-camera" aria-hidden="true"></i><b id="ptrg-n" hidden>0</b></button></div>';
            document.body.appendChild(el); document.body.classList.add("gl-v2-ptcam-on");
            var v = null;
            if (hasVideo) {
                v = document.createElement("video"); v.muted = true; v.defaultMuted = true; v.setAttribute("muted", ""); v.setAttribute("playsinline", ""); v.setAttribute("autoplay", ""); v.playsInline = true;
                v.srcObject = stream; $("#ptrg-cam", el).insertBefore(v, $("#ptrg-cam", el).firstChild); var pl = v.play(); if (pl && pl.catch) pl.catch(function () { /* start na de eerste tik */ });
            }
            var audioOnly = new MediaStream(stream.getAudioTracks());
            rg = { el: el, v: v, stream: stream, handle: recordStream(audioOnly), t0: Date.now(), pausedAt: 0, pausedTotal: 0, photos: [], times: [], opt: opt, timer: null, wake: null };
            rg.elapsed = function () { return ((rg.pausedAt || Date.now()) - rg.t0 - rg.pausedTotal) / 1000; };
            rg.timer = setInterval(function () { $("#ptrg-time", el).textContent = mmss(rg.elapsed()); }, 500);
            try { if (navigator.wakeLock) navigator.wakeLock.request("screen").then(function (w) { rg.wake = w; }).catch(function () { }); } catch (e) { /* geen wake lock */ }
            // een foto: tik op het beeld (pointerup, werkt ook op iOS waar een tik op een video geen click geeft) of op de cameraknop
            $("#ptrg-cam", el).addEventListener("pointerup", function (e) { e.preventDefault(); snap(); });
            el.addEventListener("click", function (e) {
                var b = e.target.closest("[data-act]"); if (!b) return;
                var act = b.dataset.act;
                if (act === "snap") { snap(); return; }
                if (act === "stop") { stopRondgang(); return; }
                if (act === "pause") {
                    var ic = $("i", b);
                    if (!rg.pausedAt) { rg.handle.pause(); rg.pausedAt = Date.now(); ic.className = "ph ph-play"; el.classList.add("is-paused"); }
                    else { rg.handle.resume(); rg.pausedTotal += Date.now() - rg.pausedAt; rg.pausedAt = 0; ic.className = "ph ph-pause"; el.classList.remove("is-paused"); }
                }
            });
        }).catch(micError);
    }
    function snap() {
        if (!rg || !rg.v) return;
        if (rg.pausedAt) { toast("De opname staat op pauze. Hervat om een foto te nemen."); return; }
        var v = rg.v;
        if (!v.videoWidth || v.readyState < 2) { var pl = v.play(); if (pl && pl.catch) pl.catch(function () { }); toast("De camera is nog niet klaar. Probeer het zo meteen opnieuw."); return; }
        var sc = Math.min(1, 1600 / Math.max(v.videoWidth, v.videoHeight)), c = document.createElement("canvas");
        c.width = Math.round(v.videoWidth * sc); c.height = Math.round(v.videoHeight * sc);
        try { c.getContext("2d").drawImage(v, 0, 0, c.width, c.height); } catch (e) { toast("Kon geen foto nemen."); return; }
        var t = rg.elapsed(), fl = $(".gl-v2-ptrg-flash", rg.el); fl.classList.remove("is-on"); void fl.offsetWidth; fl.classList.add("is-on");
        try { if (navigator.vibrate) navigator.vibrate(30); } catch (e) { /* niets */ }
        c.toBlob(function (b) {
            if (!b) { toast("Kon geen foto nemen."); return; }
            rg.photos.push(b); rg.times.push(t);
            var n = $("#ptrg-n", rg.el); n.hidden = false; n.textContent = rg.photos.length;
            var url = URL.createObjectURL(b), row = document.createElement("div"); row.className = "gl-v2-ptrg-mark";
            row.innerHTML = '<span class="gl-v2-ptrg-mt">' + mmss(t) + '</span><span class="gl-v2-ptrg-ml">foto ' + rg.photos.length + '</span><img src="' + url + '" alt="">';
            var list = $("#ptrg-marks", rg.el); list.appendChild(row); list.scrollTop = list.scrollHeight;
        }, "image/jpeg", 0.82);
    }
    function stopRondgang() {
        var r = rg; rg = null; clearInterval(r.timer); var secs = ((r.pausedAt ? r.pausedAt : Date.now()) - r.t0 - r.pausedTotal) / 1000;
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

    // ── kaart op de lijst terwijl een rondgang verwerkt wordt (41d stap 3) ──
    var bannerEl = null, lastJob = null, curId = null, curState = "";
    function banner(s) {
        if (!bannerEl) { bannerEl = document.createElement("div"); bannerEl.className = "gl-v2-ptrg-banner"; bannerEl.setAttribute("role", "status"); root.insertBefore(bannerEl, root.firstChild); }
        var html;
        if (s.state === "upload" || s.state === "bezig") {
            var up = s.state === "upload";
            html = '<div class="gl-v2-ptrg-b-top"><b>Rondgang ' + mmss(s.duration) + (up ? " wordt geüpload" : " wordt verwerkt") + '</b><span>' + s.percent + ' %</span></div>' +
                '<div class="gl-v2-ptrg-b-bar"><i style="transform:scaleX(' + (s.percent / 100) + ')"></i></div>' +
                '<small>' + (up ? "Blijf op deze pagina tot het geüpload is." : "Je mag verder werken of de app sluiten. Open Punten later opnieuw om te controleren.") + '</small>';
        } else if (s.state === "klaar") html = '<button type="button" class="gl-v2-ptrg-b-open" data-act="open-rondgang"><i class="ph ph-check-circle" aria-hidden="true"></i><span><b>Rondgang verwerkt</b><small>' + s.count + ' punten voorgesteld · tik om te controleren</small></span><i class="ph ph-caret-right" aria-hidden="true"></i></button>';
        else html = '<div class="gl-v2-ptrg-b-top"><b class="is-err">Rondgang niet verwerkt</b><button type="button" class="gl-v2-ptrg-b-x" data-act="close-banner" aria-label="Sluiten"><i class="ph ph-x" aria-hidden="true"></i></button></div><small>' + esc(s.error || "") + '</small>';
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
            // een mislukte rondgang weggooien, anders blijft de kaart bij elk bezoek terugkomen
            if (curId && curState === "fout") { var fd = new FormData(); fd.append("id", curId); post("RondgangVerwijderV2", fd); }
            if (bannerEl) { bannerEl.remove(); bannerEl = null; }
            return;
        }
        if (e.target.closest("#pt-qa-voice")) { openVoice(); return; }
        if (e.target.closest("#pt-qa-rondgang")) { openRondgangStart(); return; }
    });
    // een rondgang die nog niet bewaard is (staat in de database): ook na het sluiten van de app of op een ander toestel
    if (canWrite()) fetch(base + "/RondgangLopendV2", { credentials: "same-origin" }).then(function (r) { return r.json(); }).then(function (j) {
        if (j && j.ok && !j.none && j.id) pollRondgang(j.id, j.duration || 0);
    }).catch(function () { /* geen kaart */ });

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
