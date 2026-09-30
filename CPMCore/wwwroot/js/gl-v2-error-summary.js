// gl-v2 — Foutoverzicht (design-handoff punt 24 "Foutoverzicht formulier"), gedeeld engine. Geladen
// project-wijd vanuit _LayoutV2.cshtml (na gl-v2-shell.js, zelfde conventie als GlV2Toast/GlV2Modal/
// GlV2Select) zodat elke pagina window.GlV2ErrorSummary.init({...}) kan aanroepen zonder dit zelf te
// herschrijven. Doet zelf niets tenzij een pagina init() aanroept — component klaar, niets fabriceert
// een gebruik (zelfde discipline als de rest van de gl-v2-*-panelen). Zie DESIGN.md "Foutoverzicht
// (punt 24) — gedeeld component" voor het volledige contract per veldtype.
(function () {
    "use strict";

    // ── Formaat-validators (naast de leeg/niet-leeg-check hierboven) — data-gl-v2-format="…" op
    //    hetzelfde .gl-v2-field-element geeft aan WELKE checker moet draaien zodra het veld niet
    //    leeg is; welke boodschap erbij hoort staat in data-gl-v2-format-message (elk GlV2Email/
    //    GlV2Telefoon/GlV2Gsm-template zet dit al zelf). Dit geldt ONGEACHT of het veld ook
    //    data-gl-v2-required draagt — een optioneel telefoonnummer dat wél is ingevuld moet nog
    //    steeds geldig zijn. Server-side dekt hetzelfde af ([EmailAddress]/[GlV2Phone], BOCore) —
    //    twee onafhankelijke, best-effort kopieën van dezelfde regels, zie DESIGN.md "Telefoon/Gsm-
    //    voorvoegsel + validatie" voor waarom dit niet één gedeelde bron kan zijn (C#/VB vs JS).
    var EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
    // Zelfde digit-bereiken als BOCore.GlV2PhonePrefixes.All — bij een wijziging daar, hier ook
    // aanpassen (geen gedeelde bron mogelijk over de C#/VB ↔ JS-grens heen).
    var PHONE_DIGIT_RULES = {
        "+32": [8, 9], "+31": [9, 9], "+33": [9, 9], "+49": [6, 11], "+352": [6, 9],
        "+44": [9, 10], "+41": [9, 9], "+34": [9, 9], "+39": [6, 11], "+1": [10, 10]
    };
    var FORMAT_CHECKERS = {
        email: function (field) {
            var input = field.querySelector('input[type="email"]');
            var val = input && input.value.trim();
            return !val || EMAIL_RE.test(val);
        },
        phone: function (field) {
            var number = field.querySelector('[data-role="phone-number"]');
            var val = number && number.value.trim();
            if (!val) return true;
            var digits = val.replace(/\D/g, "");
            if (!digits) return false;
            var prefixEl = field.querySelector('[data-role="phone-prefix"]');
            var rule = PHONE_DIGIT_RULES[prefixEl ? prefixEl.value : "+32"] || [6, 15];
            return digits.length >= rule[0] && digits.length <= rule[1];
        }
    };

    function GlV2ErrorSummary(options) {
        this.form = options.form;
        this.container = options.container || document.getElementById("gl-v2-error-summary");
        this.validators = options.validators || [];
        // Standaard: de tab van het veld activeren via de tabbar zelf (bestaande .gl-v2-tabbar-tab-
        // klikafhandeling van de pagina) — een pagina zonder tabbar merkt hier niets van. Een pagina
        // met een afwijkend tab-mechanisme geeft haar eigen onNavigate(target, tabKey) mee.
        this.onNavigate = options.onNavigate || function (target, tabKey) {
            if (!tabKey) return;
            var tab = document.querySelector('.gl-v2-tabbar-tab[data-tab="' + tabKey + '"]');
            if (tab && !tab.classList.contains("is-active")) tab.click();
        };
        this.stickyLabel = options.stickyLabel || "fouten";
        this.sticky = null;
        this.lastErrors = [];
        this.wire();
        this.syncServerRendered();
    }

    GlV2ErrorSummary.prototype.resolveFieldTarget = function (field) {
        return field.classList.contains("gl-v2-field") ? field : (field.querySelector(".gl-v2-field") || field);
    };

    // Leeg-check per veldtype, generiek genoeg voor elk bestaand én toekomstig GlV2-veld:
    // - meervoudige kiezer ([data-role="hidden-inputs"]): geen enkele hidden input = leeg;
    // - zoekende select/datum/id-veld (eerste hidden input draagt de waarde): lege waarde = leeg;
    // - anders het eerste zichtbare input/select/textarea.
    GlV2ErrorSummary.prototype.isFieldElementEmpty = function (field) {
        var hiddenHost = field.querySelector('[data-role="hidden-inputs"]');
        if (hiddenHost) return hiddenHost.querySelectorAll('[data-role="hidden-input"]').length === 0;
        // Projecten-formulieren: een id-veld ([data-role="id-hidden"]) is leeg zolang er geen echte id (> 0) in zit.
        var idHidden = field.querySelector('[data-role="id-hidden"]');
        if (idHidden) return !(parseInt(idHidden.value, 10) > 0);
        var hiddenInput = field.querySelector('input[type="hidden"]');
        if (hiddenInput) return !hiddenInput.value || !String(hiddenInput.value).trim();
        var input = field.querySelector("input, select, textarea");
        return !input || !input.value || !input.value.trim();
    };

    // Tab van een veld: expliciet (data-gl-v2-error-tab) of afgeleid van het omhullende tabpaneel
    // ([data-tab-panel], de bestaande tabbar-conventie) — zo hoeft een tabbar-pagina niets extra te
    // markeren. Locatie: expliciet (data-gl-v2-error-location) of de titel van de omhullende
    // sectiekaart, met de tabnaam ervoor ("Algemeen · Identiteit", 24a).
    GlV2ErrorSummary.prototype.fieldTab = function (field) {
        var explicit = field.getAttribute("data-gl-v2-error-tab");
        if (explicit) return explicit;
        var panel = field.closest("[data-tab-panel]");
        return panel ? panel.getAttribute("data-tab-panel") : "";
    };

    GlV2ErrorSummary.prototype.tabLabel = function (tabKey) {
        if (!tabKey) return "";
        var tab = document.querySelector('.gl-v2-tabbar-tab[data-tab="' + tabKey + '"]');
        if (!tab) return "";
        var clone = tab.cloneNode(true);
        Array.prototype.forEach.call(clone.querySelectorAll("span, i"), function (n) { n.remove(); });
        return clone.textContent.trim();
    };

    GlV2ErrorSummary.prototype.fieldLocation = function (field, tabKey) {
        var explicit = field.getAttribute("data-gl-v2-error-location");
        if (explicit) return explicit;
        var parts = [];
        var tabName = this.tabLabel(tabKey);
        if (tabName) parts.push(tabName);
        var card = field.closest(".gl-v2-section-card");
        var title = card && card.querySelector(".gl-v2-section-card-title");
        if (title && title.textContent.trim()) parts.push(title.textContent.trim());
        return parts.join(" · ");
    };

    GlV2ErrorSummary.prototype.activeTab = function () {
        var active = document.querySelector(".gl-v2-tabbar-tab.is-active[data-tab]");
        return active ? active.getAttribute("data-tab") : "";
    };

    // Zet/wist de rand-om-het-veld-plus-hulptekst-status (elk GlV2*-editortemplate doet dit al zelf
    // voor een server-side fout; dit is dezelfde visuele taal, hier client-side toegepast).
    // - Een template met een eigen, vaste (evt. verborgen) foutslot draagt die als
    //   [data-role="field-error"] (zie GlV2/_DateField.cshtml) — die wordt hier hergebruikt i.p.v.
    //   een tweede span toe te voegen (anders staat de boodschap dubbel).
    // - .gl-v2-select-trigger (postcode-/eenhedenkiezer e.d.) krijgt zijn eigen .is-error mee — die
    //   bestaande klasse (GlV2SearchSelect) kleurt de trigger zelf pas rood, .gl-v2-field.is-error
    //   alleen doet dat niet voor dat elementtype.
    GlV2ErrorSummary.prototype.setFieldError = function (field, message, isInvalid) {
        var target = this.resolveFieldTarget(field);
        target.classList.toggle("is-error", isInvalid);
        var trigger = target.querySelector(".gl-v2-select-trigger");
        if (trigger) trigger.classList.toggle("is-error", isInvalid);
        // Vaste foutslots die een template zelf al rendert: [data-role="field-error"] (GlV2/_DateField)
        // en [data-role="help"] (Projecten-formulierpartials) — hergebruiken, nooit verdubbelen.
        var help = target.querySelector('[data-role="field-error"], [data-role="help"]') || target.querySelector(".gl-v2-field-help[data-role='required-help']");
        if (isInvalid) {
            if (!help) {
                help = document.createElement("span");
                help.className = "gl-v2-field-help";
                help.setAttribute("data-role", "required-help");
                target.appendChild(help);
            }
            help.textContent = message;
            help.hidden = false;
        } else if (help) {
            if (help.getAttribute("data-role") !== "required-help") {
                // Vaste slot: enkel verbergen/leegmaken, nooit verwijderen — een template kan hier
                // later opnieuw in schrijven (bv. bij een volgende server-redisplay).
                help.hidden = true;
                help.textContent = "";
            } else {
                help.remove();
            }
        }
    };

    GlV2ErrorSummary.prototype.fieldLabel = function (field) {
        var target = this.resolveFieldTarget(field);
        var labelEl = target.querySelector(".gl-v2-field-label");
        if (!labelEl) return "";
        return labelEl.textContent.replace("*", "").trim();
    };

    // [data-gl-v2-required="<boodschap>"] blijft het generieke contract voor "verplicht, leeg is
    // ongeldig" — elk bestaand én toekomstig veldtype werkt hiermee zonder dat dit bestand iets
    // veldtype-specifieks moet kennen. [data-gl-v2-format="email|phone"] (+ data-gl-v2-format-
    // message) is het generieke contract voor "ingevuld, maar ongeldig van vorm" — onafhankelijk
    // van verplicht-zijn, en werkt voor elk bestaand én toekomstig formaat zolang er een
    // FORMAT_CHECKERS-ingang voor is. Eén veld kan beide dragen (bv. een verplicht e-mailveld):
    // leeg wint dan als verplicht-boodschap, ingevuld-maar-ongeldig als formaat-boodschap — nooit
    // allebei tegelijk. Groepen die geen simpele per-veld-check zijn (bv. een rijenlijst die
    // minstens 1 rij nodig heeft, of elk bedrag in N rijen > 0) horen thuis in een eigen
    // validator-functie (options.validators), niet hier.
    GlV2ErrorSummary.prototype.collectFieldErrors = function () {
        var self = this;
        var errors = [];
        (this.form || document).querySelectorAll("[data-gl-v2-required], [data-gl-v2-format]").forEach(function (field) {
            var hidden = field.closest("[hidden]");
            var requiredMsg = field.getAttribute("data-gl-v2-required");
            var isEmpty = self.isFieldElementEmpty(field);
            var message = null;
            if (requiredMsg && isEmpty) {
                message = requiredMsg;
            } else if (!isEmpty) {
                var formatType = field.getAttribute("data-gl-v2-format");
                var checker = formatType && FORMAT_CHECKERS[formatType];
                if (checker && !checker(field)) {
                    message = field.getAttribute("data-gl-v2-format-message") || "Deze waarde is niet geldig.";
                }
            }
            var invalid = !hidden && !!message;
            self.setFieldError(field, message || "", invalid);
            if (!invalid) return;
            var tab = self.fieldTab(field);
            errors.push({
                message: message,
                field: self.fieldLabel(field),
                location: self.fieldLocation(field, tab),
                tab: tab,
                target: self.resolveFieldTarget(field)
            });
        });
        return errors;
    };

    GlV2ErrorSummary.prototype.validate = function () {
        var errors = this.collectFieldErrors();
        this.validators.forEach(function (fn) { fn(errors); });
        this.lastErrors = errors;
        this.render(errors);
        this.updateTabErrors(errors);
        return errors;
    };

    // 24a "tabs tellen hun fouten": elke .gl-v2-tabbar-tab krijgt haar rode stip ([data-role=
    // "error-dot"], bestaande conventie) en — als de tab er een heeft — een teller
    // ([data-role="error-count"]) op basis van de fouten in háár paneel.
    GlV2ErrorSummary.prototype.updateTabErrors = function (errors) {
        var counts = {};
        errors.forEach(function (err) { if (err.tab) counts[err.tab] = (counts[err.tab] || 0) + 1; });
        document.querySelectorAll(".gl-v2-tabbar-tab[data-tab]").forEach(function (tab) {
            var n = counts[tab.getAttribute("data-tab")] || 0;
            var dot = tab.querySelector('[data-role="error-dot"]');
            if (dot) dot.hidden = n === 0;
            var count = tab.querySelector('[data-role="error-count"]');
            if (count) { count.textContent = String(n); count.hidden = n === 0; }
        });
    };

    GlV2ErrorSummary.prototype.scrollToTarget = function (target, tab) {
        if (!target) return;
        this.onNavigate(target, tab);
        window.setTimeout(function () {
            target.scrollIntoView({ behavior: "smooth", block: "center" });
            var input = target.querySelector('input:not([type=hidden]), select, textarea, [role="button"]');
            if (input && input.focus) input.focus({ preventScroll: true });
        }, tab ? 60 : 0); // korte vertraging als een tab nog moet omschakelen (layout kan verspringen)
    };

    // ── Render — dezelfde opmaak of er 0, 1 of N fouten zijn (24a "meerdere", 24b "één fout"). ─────
    GlV2ErrorSummary.prototype.render = function (errors) {
        var box = this.container;
        if (!box) return;
        var self = this;
        box.hidden = errors.length === 0;
        box.classList.toggle("is-single", errors.length === 1);
        this.updateSticky(errors);
        this.updateActionbar(errors.length);
        if (!errors.length) return;

        var title = box.querySelector('[data-role="title"]');
        var sub = box.querySelector('[data-role="subtitle"]');
        if (errors.length === 1) {
            // 24b "Eén fout": geen teller en geen lijst — de fout zelf is de titel, met de veldnaam als link.
            var only = errors[0];
            if (title) {
                title.innerHTML = "";
                if (only.field) {
                    var link = document.createElement("a");
                    link.href = "#";
                    link.className = "gl-v2-error-summary-field";
                    link.textContent = only.field;
                    link.addEventListener("click", function (e) { e.preventDefault(); self.scrollToTarget(only.target, only.tab); });
                    title.appendChild(link);
                    title.appendChild(document.createTextNode(" "));
                }
                title.appendChild(document.createTextNode(only.message + (/[.!?]$/.test(only.message) ? "" : ".") + " Er is nog niets opgeslagen."));
            }
            if (sub) sub.hidden = true;
        } else {
            if (title) title.textContent = errors.length + " velden moeten nog aangepast worden";
            if (sub) { sub.hidden = false; sub.textContent = "Er is nog niets opgeslagen. Klik een fout om naar het veld te gaan."; }
        }

        var list = box.querySelector('[data-role="list"]');
        if (!list) return;
        list.innerHTML = "";
        errors.forEach(function (err) {
            var li = document.createElement("li");
            var a = document.createElement("a");
            a.href = "#";
            a.className = "gl-v2-error-summary-item";
            var dot = document.createElement("span");
            dot.className = "gl-v2-error-summary-dot";
            dot.setAttribute("aria-hidden", "true");
            a.appendChild(dot);
            var msg = document.createElement("span");
            msg.className = "gl-v2-error-summary-msg";
            if (err.field) {
                var fieldSpan = document.createElement("span");
                fieldSpan.className = "gl-v2-error-summary-field";
                fieldSpan.textContent = err.field;
                msg.appendChild(fieldSpan);
            }
            var textSpan = document.createElement("span");
            textSpan.textContent = err.message;
            msg.appendChild(textSpan);
            a.appendChild(msg);
            if (err.location) {
                var loc = document.createElement("span");
                loc.className = "gl-v2-error-summary-loc";
                loc.textContent = err.location;
                a.appendChild(loc);
            }
            // "ANDERE TAB" enkel voor een veld op een ander tabblad dan het actieve (24a) — op een
            // pagina zonder tabbar is err.tab leeg en verschijnt de badge nooit.
            if (err.tab && err.tab !== self.activeTab()) {
                var badge = document.createElement("span");
                badge.className = "gl-v2-error-summary-badge";
                badge.textContent = "ANDERE TAB";
                a.appendChild(badge);
            }
            var chevron = document.createElement("i");
            chevron.className = "ph ph-caret-right";
            chevron.setAttribute("aria-hidden", "true");
            a.appendChild(chevron);
            a.addEventListener("click", function (e) {
                e.preventDefault();
                self.scrollToTarget(err.target, err.tab);
            });
            li.appendChild(a);
            list.appendChild(li);
        });
    };

    // ── Weggescrold — plakt bovenaan (24b): IntersectionObserver op het overzicht zelf; zolang er
    //    fouten zijn én de kaart niet in beeld is, toont een compacte balk met vorige/volgende. ─────
    GlV2ErrorSummary.prototype.ensureSticky = function () {
        if (this.sticky) return this.sticky;
        var bar = document.createElement("div");
        bar.className = "gl-v2-error-sticky";
        bar.innerHTML =
            '<i class="ph ph-warning-circle" aria-hidden="true"></i>' +
            '<span class="gl-v2-error-sticky-count"></span>' +
            '<span class="gl-v2-error-sticky-next"></span>' +
            '<div class="gl-v2-error-sticky-nav">' +
            '<button type="button" data-role="prev" aria-label="Vorige fout"><i class="ph ph-caret-up" aria-hidden="true"></i></button>' +
            '<button type="button" data-role="next" aria-label="Volgende fout"><i class="ph ph-caret-down" aria-hidden="true"></i></button>' +
            "</div>";
        document.body.appendChild(bar);
        var self = this;
        var index = 0;
        bar.querySelector('[data-role="next"]').addEventListener("click", function () {
            if (!self.lastErrors.length) return;
            index = (index + 1) % self.lastErrors.length;
            self.scrollToTarget(self.lastErrors[index].target, self.lastErrors[index].tab);
        });
        bar.querySelector('[data-role="prev"]').addEventListener("click", function () {
            if (!self.lastErrors.length) return;
            index = index <= 0 ? self.lastErrors.length - 1 : index - 1;
            self.scrollToTarget(self.lastErrors[index].target, self.lastErrors[index].tab);
        });
        this.sticky = bar;
        if (this.container && "IntersectionObserver" in window) {
            this.observer = new IntersectionObserver(function (entries) {
                self.summaryVisible = entries[0].isIntersecting;
                self.refreshStickyVisibility();
            }, { threshold: 0 });
            this.observer.observe(this.container);
        }
        return bar;
    };

    GlV2ErrorSummary.prototype.refreshStickyVisibility = function () {
        if (!this.sticky) return;
        var show = this.lastErrors.length > 0 && this.summaryVisible === false;
        this.sticky.classList.toggle("is-visible", show);
    };

    GlV2ErrorSummary.prototype.updateSticky = function (errors) {
        var bar = this.ensureSticky();
        var countEl = bar.querySelector(".gl-v2-error-sticky-count");
        if (countEl) countEl.textContent = errors.length + " " + this.stickyLabel;
        var nextEl = bar.querySelector(".gl-v2-error-sticky-next");
        if (nextEl) nextEl.textContent = errors.length ? "volgende: " + (errors[0].field || errors[0].message) : "";
        this.refreshStickyVisibility();
    };

    // ── Actiebalk (24a/24c/24d): rode teller-link "N fouten — naar het overzicht" (tablet/gsm: enkel
    //    "N fouten", zie CSS) als eerste kind van de .gl-v2-form-actionbar van het formulier; klik
    //    scrolt naar het overzicht. Aangemaakt bij de eerste fout, verborgen zodra er geen zijn. ────────
    GlV2ErrorSummary.prototype.updateActionbar = function (count) {
        var bar = (this.form && this.form.querySelector(".gl-v2-form-actionbar")) || document.querySelector(".gl-v2-form-actionbar");
        if (!bar) return;
        var link = bar.querySelector('[data-role="error-summary-link"]');
        if (!link) {
            if (!count) return;
            var self = this;
            link = document.createElement("a");
            link.href = "#" + (this.container && this.container.id ? this.container.id : "gl-v2-error-summary");
            link.className = "gl-v2-form-actionbar-errors";
            link.setAttribute("data-role", "error-summary-link");
            link.innerHTML =
                '<i class="ph ph-warning-circle" aria-hidden="true"></i>' +
                '<span data-role="count"></span>' +
                '<span class="gl-v2-form-actionbar-errors-suffix"> — naar het overzicht</span>';
            link.addEventListener("click", function (e) {
                e.preventDefault();
                if (!self.container) return;
                self.container.scrollIntoView({ behavior: "smooth", block: "start" });
                self.container.focus({ preventScroll: true });
            });
            bar.insertBefore(link, bar.firstChild);
        }
        link.hidden = !count;
        var countEl = link.querySelector('[data-role="count"]');
        if (countEl) countEl.textContent = count === 1 ? "1 fout" : count + " fouten";
    };

    // Server-redisplay: de partial (Views/Shared/GlV2/_ErrorSummaryV2.cshtml) heeft de kaart al
    // gevuld uit ModelState — de actiebalk-teller volgt die lijst zolang er nog geen client-validatie
    // gelopen heeft (daarna nemen validate()/render() het over).
    GlV2ErrorSummary.prototype.syncServerRendered = function () {
        if (!this.container || this.container.hidden) return;
        var rows = this.container.querySelectorAll('[data-role="list"] > li').length;
        var count = rows || (this.container.classList.contains("is-single") ? 1 : 0);
        this.updateActionbar(count);
    };

    // ── Formulier-koppeling: submit valideert en blokkeert bij fouten (scrollt naar het overzicht
    //    zelf, dat role="alert" draagt — een schermlezer leest de titel voor); focusout op een
    //    afzonderlijk veld herrekent enkel dat veld + de teller/lijst zonder alles te herscannen. ────
    GlV2ErrorSummary.prototype.wire = function () {
        var self = this;
        if (!this.form) return;

        this.form.addEventListener("submit", function (e) {
            var errors = self.validate();
            if (!errors.length) return;
            e.preventDefault();
            e.stopImmediatePropagation();
            if (self.container) {
                self.container.hidden = false;
                self.container.scrollIntoView({ behavior: "smooth", block: "start" });
                self.container.focus({ preventScroll: true });
            }
        });

        // Klikken op een rij in het overzicht zelf mag de lijst niet herrenderen: een klik verplaatst
        // eerst de focus weg van het net-bewerkte veld (focusout), en die her-render zou de <a> die je
        // aan het klikken bent uit de DOM halen vóórdat de klik zelf afgerond is — vandaar "moet twee
        // keer klikken". mousedown (vóór de focusout) zet een vlag die de eerstvolgende focusout overslaat.
        if (this.container) {
            this.container.addEventListener("mousedown", function () { self.suppressNextFocusoutValidate = true; });
        }

        this.form.addEventListener("focusout", function (e) {
            if (self.suppressNextFocusoutValidate) {
                self.suppressNextFocusoutValidate = false;
                return;
            }
            if (!self.lastErrors.length) return; // pas actief ná een eerste geblokkeerde poging (24b "Wanneer": niet tijdens het typen)
            var field = e.target.closest("[data-gl-v2-required], [data-gl-v2-format]");
            if (field) self.validate();
        });
    };

    window.GlV2ErrorSummary = {
        init: function (options) { return new GlV2ErrorSummary(options); }
    };
})();
