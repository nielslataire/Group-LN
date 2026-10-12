// Gl-v2 — Helpcentrum (design-handoff punt 41): zoeken-terwijl-je-typt (41b) op de startpagina,
// FAQ-accordion + "Was dit nuttig"-feedback (41c) op de artikelpagina. Eén bestand voor beide
// pagina's (zelfde patroon als gl-v2-instellingen.js) — elk stuk checkt zelf of zijn element
// bestaat, dus niets breekt op de pagina die het andere stuk niet heeft.
(function () {
    "use strict";

    function tokenValue() {
        var t = document.querySelector('input[name="__RequestVerificationToken"]');
        return t ? t.value : "";
    }

    function escapeHtml(s) {
        return (s || "").replace(/[&<>"']/g, function (c) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
        });
    }

    function highlight(text, index, length) {
        var safe = escapeHtml(text);
        if (index == null || index < 0 || length <= 0) return safe;
        var before = escapeHtml(text.slice(0, index));
        var mid = escapeHtml(text.slice(index, index + length));
        var after = escapeHtml(text.slice(index + length));
        return before + "<b>" + mid + "</b>" + after;
    }

    // ── 41b/43i — Zoeken-terwijl-je-typt, herbruikbaar voor élk zoekvak-paar (hero op de
    //    startpagina + het compacte topbar-zoekvak op zowel start- als artikelpagina). ────────────
    function wireHelpSearch(input, results, opts) {
        if (!input || !results) return;
        opts = opts || {};

        var activeIndex = -1;
        var timer = null;

        function rowsOnScreen() { return results.querySelectorAll("[data-help-result-row]"); }

        function setActive(i) {
            var rows = rowsOnScreen();
            rows.forEach(function (r) { r.classList.remove("is-active"); });
            if (rows.length === 0) { activeIndex = -1; return; }
            activeIndex = (i + rows.length) % rows.length;
            rows[activeIndex].classList.add("is-active");
            rows[activeIndex].scrollIntoView({ block: "nearest" });
        }

        function render(groups) {
            if (!groups || groups.length === 0) {
                results.innerHTML = '<div class="gl-v2-help-search-empty">Niets gevonden. Probeer een ander woord, of <a href="#" data-help-support-shortcut>stel je vraag aan support</a>.</div>';
                results.hidden = false;
                return;
            }
            var html = "";
            groups.forEach(function (g) {
                html += '<div class="gl-v2-help-search-heading"><span>' + escapeHtml(g.heading) + '</span><span>' + g.count + '</span></div>';
                g.items.forEach(function (item) {
                    html += '<a class="gl-v2-help-search-row" data-help-result-row href="' +
                        '/Help/Artikel/' + encodeURIComponent(item.articleSlug) + '">' +
                        '<span class="gl-v2-help-search-row-text">' +
                        '<span class="gl-v2-help-search-row-title">' + highlight(item.title, item.matchIndex, item.matchLength) + '</span>' +
                        '<span class="gl-v2-help-search-row-path">' + escapeHtml(item.path) + '</span>' +
                        '</span>' +
                        (item.meta ? '<span class="gl-v2-help-search-row-meta">' + escapeHtml(item.meta) + '</span>' : "") +
                        '</a>';
                });
            });
            results.innerHTML = html;
            results.hidden = false;
            activeIndex = -1;
        }

        function search(term) {
            if (!term) { results.hidden = true; results.innerHTML = ""; return; }
            fetch("/Help/Search?term=" + encodeURIComponent(term), { credentials: "same-origin" })
                .then(function (r) { return r.json(); })
                .then(function (data) { render(data.groups); })
                .catch(function () { results.hidden = true; });
        }

        input.addEventListener("input", function () {
            clearTimeout(timer);
            var term = input.value.trim();
            timer = setTimeout(function () { search(term); }, 180);
        });

        input.addEventListener("keydown", function (e) {
            var rows = rowsOnScreen();
            if (rows.length === 0) return;
            if (e.key === "ArrowDown") { e.preventDefault(); setActive(activeIndex + 1); }
            else if (e.key === "ArrowUp") { e.preventDefault(); setActive(activeIndex - 1); }
            else if (e.key === "Enter") {
                e.preventDefault();
                var target = activeIndex >= 0 ? rows[activeIndex] : rows[0];
                if (target) window.location.href = target.getAttribute("href");
            }
        });

        document.addEventListener("click", function (e) {
            if (!results.contains(e.target) && e.target !== input) results.hidden = true;
        });

        if (opts.clearBtn) {
            opts.clearBtn.addEventListener("click", function () {
                input.value = "";
                results.hidden = true;
                results.innerHTML = "";
                input.focus();
            });
        }

        if (opts.popularChips) {
            opts.popularChips.forEach(function (chip) {
                chip.addEventListener("click", function () {
                    input.value = chip.textContent.trim();
                    input.focus();
                    search(input.value.trim());
                });
            });
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        wireHelpSearch(
            document.getElementById("gl-v2-help-search-input"),
            document.getElementById("gl-v2-help-search-results"),
            {
                clearBtn: document.getElementById("gl-v2-help-search-clear"),
                popularChips: document.querySelectorAll("[data-help-popular-term]"),
            }
        );
        wireHelpSearch(
            document.getElementById("gl-v2-help-topbar-search-input"),
            document.getElementById("gl-v2-help-topbar-search-results")
        );

        // "Stel je vraag aan support" in een lege-zoekresultaten-staat (41b) — zelfde paneel/
        // formulier als het "?"-zijpaneel, geen los mechanisme.
        document.addEventListener("click", function (e) {
            var shortcut = e.target.closest("[data-help-support-shortcut]");
            if (!shortcut) return;
            e.preventDefault();
            var panel = document.getElementById("glV2HelpPanel");
            var helpBtn = document.getElementById("gl-v2-help-button");
            if (helpBtn && panel && !panel.classList.contains("is-open")) helpBtn.click();
            var form = document.querySelector("[data-help-support-form]");
            if (form) form.hidden = false;
        });
    });

    // ── 41c — FAQ-accordion ─────────────────────────────────────────────────────────────────────
    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll("[data-help-faq-toggle]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var item = btn.closest("[data-help-faq-item]");
                if (item) item.classList.toggle("is-open");
            });
        });
    });

    // ── 41c — "Was dit artikel nuttig?" ─────────────────────────────────────────────────────────
    document.addEventListener("DOMContentLoaded", function () {
        var wrap = document.querySelector("[data-help-feedback]");
        if (!wrap) return;
        var slug = wrap.getAttribute("data-article-slug");
        var form = document.querySelector("[data-help-feedback-form]");
        var yesBtn = wrap.querySelector("[data-help-feedback-yes]");
        var noBtn = wrap.querySelector("[data-help-feedback-no]");

        function send(helpful, message) {
            var body = new URLSearchParams();
            body.set("articleSlug", slug);
            body.set("helpful", helpful ? "true" : "false");
            if (message) body.set("message", message);
            return fetch("/Help/Feedback", {
                method: "POST",
                headers: {
                    "RequestVerificationToken": tokenValue(),
                    "Content-Type": "application/x-www-form-urlencoded",
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: body.toString(),
                credentials: "same-origin"
            });
        }

        if (yesBtn) {
            yesBtn.addEventListener("click", function () {
                send(true).then(function () {
                    wrap.innerHTML = '<span class="gl-v2-help-feedback-thanks"><i class="ph ph-check-circle" aria-hidden="true"></i> Bedankt voor je feedback.</span>';
                });
            });
        }
        if (noBtn && form) {
            noBtn.addEventListener("click", function () {
                form.hidden = false;
                wrap.hidden = true;
                var textarea = form.querySelector("[data-help-feedback-message]");
                if (textarea) textarea.focus();
            });
            var sendBtn = form.querySelector("[data-help-feedback-send]");
            if (sendBtn) {
                sendBtn.addEventListener("click", function () {
                    var textarea = form.querySelector("[data-help-feedback-message]");
                    var message = textarea ? textarea.value.trim() : "";
                    send(false, message).then(function () {
                        form.innerHTML = '<span class="gl-v2-help-feedback-thanks"><i class="ph ph-check-circle" aria-hidden="true"></i> Bedankt — we kijken ernaar.</span>';
                    });
                });
            }
        }
    });
})();
