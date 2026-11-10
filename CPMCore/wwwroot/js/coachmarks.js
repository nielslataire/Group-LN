/**
 * coachmarks.js — Feature discovery / coachmark systeem (design-handoff punt 58, "CRM Coachmarks")
 *
 * Leest window.GlCoachmarkFlow (gezet door _Coachmarks.cshtml) en toont een groene bubbel naast het doel.
 *
 *  - Eén tip: kicker NIEUW, titel, tekst, primaire knop "Begrepen". Geen overlay; klik buiten of Esc = sluiten.
 *  - Rondleiding (3–5 stappen): kicker "2 VAN 3", spotlight met gouden rand rond het doel, Overslaan · Vorige · Volgende,
 *    laatste stap "Klaar". Esc = overslaan; Tab blijft in de bubbel.
 *  - Plaatsing: onder → boven → rechts → links, kantelt vanzelf; de pijl wijst naar het midden van het doel.
 *  - Telefoon (<576px): geen bubbel met pijl maar een bottom sheet, het doel omkaderd erboven.
 *  - Toegankelijk: role="dialog", focus op de primaire knop, aria-describedby op het doel.
 *  - Geen doelelement op deze pagina/schermbreedte: stil overslaan.
 *
 * Tekst: **vet** in een bericht wordt <b>. Meer opmaak is er niet (titel en tekst blijven kort, zie DESIGN.md).
 */
(function () {
    'use strict';

    // ── Config ────────────────────────────────────────────────────────────────

    const flow = window.GlCoachmarkFlow;
    if (!flow || !Array.isArray(flow.steps) || flow.steps.length === 0) return;

    const API = {
        markShown:   '/Coachmark/MarkShown',
        dismiss:     '/Coachmark/Dismiss',
        complete:    '/Coachmark/Complete',
        advanceStep: '/Coachmark/AdvanceStep'
    };

    const GAP = 10;            // px tussen bubbel en doel
    const MOBILE_BP = 576;     // px: onder deze breedte een bottom sheet
    const SPOT_PAD = 6;        // px marge van de spotlight rond het doel
    const ARROW_MIN = 16;      // px: de pijl blijft minstens zover van de hoek
    const ORDER = ['bottom', 'top', 'right', 'left'];
    const isTour = flow.steps.length > 1;
    const reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // ── State ─────────────────────────────────────────────────────────────────

    let currentStep  = flow.currentStep ?? 0;
    let popoverEl    = null;
    let spotEl       = null;
    let targetRef    = null;
    let resizeTimer  = null;
    let shownMarked  = false;
    let retries      = 0;

    // ── Initialise ────────────────────────────────────────────────────────────

    function init() {
        // Na het laden, als de pagina rustig is: niet boven een open modal.
        if (document.querySelector('.modal.show') && retries++ < 5) { setTimeout(init, 1500); return; }
        showStep(currentStep);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    function isSheet() { return window.innerWidth < MOBILE_BP; }

    // ── Show step ─────────────────────────────────────────────────────────────

    function showStep(stepIndex) {
        const step = flow.steps[stepIndex];
        if (!step) { teardown(); return; }

        const targetEl = document.querySelector(step.targetSelector);
        if (!targetEl) {
            console.debug('[coachmarks] target not found:', step.targetSelector);
            return;
        }

        if (!popoverEl) {
            popoverEl = buildPopover();
            document.body.appendChild(popoverEl);
            bindPopoverEvents();
        }

        if (targetRef && targetRef !== targetEl) targetRef.removeAttribute('aria-describedby');
        targetRef = targetEl;
        targetEl.setAttribute('aria-describedby', 'gl-cm-body');

        currentStep = stepIndex;
        renderContent(step, stepIndex);

        if (isSheet()) targetEl.scrollIntoView({ block: 'center', behavior: 'auto' });

        // Laat browser eerst renderen zodat getBoundingClientRect() klopt
        requestAnimationFrame(function () {
            layout();
            popoverEl.classList.add('gl-cm--visible');
            focusPrimary();

            if (!shownMarked) {
                shownMarked = true;
                apiPost(API.markShown, { stateKey: flow.stateKey });
            }
        });
    }

    // ── Build popover DOM ─────────────────────────────────────────────────────

    function buildPopover() {
        const el = document.createElement('div');
        el.id        = 'gl-coachmark';
        el.className = 'gl-cm';
        el.setAttribute('role', 'dialog');
        el.setAttribute('aria-labelledby', 'gl-cm-title');
        el.innerHTML =
            '<div class="gl-cm__arrow" aria-hidden="true"></div>' +
            '<div class="gl-cm__head">' +
                '<span class="gl-cm__kicker"></span>' +
                '<button class="gl-cm__close" type="button" aria-label="Sluiten">' +
                    '<svg width="11" height="11" viewBox="0 0 11 11" fill="none" aria-hidden="true">' +
                        '<path d="M1 1L10 10M10 1L1 10" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>' +
                    '</svg>' +
                '</button>' +
            '</div>' +
            '<div class="gl-cm__title" id="gl-cm-title"></div>' +
            '<p class="gl-cm__body" id="gl-cm-body"></p>' +
            '<div class="gl-cm__foot">' +
                '<button class="gl-cm__btn gl-cm__btn--text gl-cm__btn--skip" type="button">Overslaan</button>' +
                '<span class="gl-cm__spacer"></span>' +
                '<button class="gl-cm__btn gl-cm__btn--text gl-cm__btn--prev" type="button">Vorige</button>' +
                '<button class="gl-cm__btn gl-cm__btn--primary gl-cm__btn--next" type="button">Volgende</button>' +
                '<button class="gl-cm__btn gl-cm__btn--primary gl-cm__btn--done" type="button">Begrepen</button>' +
            '</div>';
        return el;
    }

    // ── Render content ────────────────────────────────────────────────────────

    function setBody(el, text) {
        el.textContent = '';
        String(text || '').split(/(\*\*[^*]+\*\*)/g).forEach(function (part) {
            if (/^\*\*[^*]+\*\*$/.test(part)) {
                const b = document.createElement('b');
                b.textContent = part.slice(2, -2);
                el.appendChild(b);
            } else if (part) {
                el.appendChild(document.createTextNode(part));
            }
        });
    }

    function renderContent(step, stepIndex) {
        const total   = flow.steps.length;
        const isFirst = stepIndex === 0;
        const isLast  = stepIndex === total - 1;

        popoverEl.querySelector('.gl-cm__title').textContent = step.title;
        setBody(popoverEl.querySelector('.gl-cm__body'), step.message);

        // Kicker: NIEUW bij één tip, "2 VAN 3" in een rondleiding.
        const kicker = popoverEl.querySelector('.gl-cm__kicker');
        kicker.textContent = isTour ? (stepIndex + 1) + ' VAN ' + total : 'NIEUW';
        kicker.classList.toggle('is-step', isTour);

        setHidden('.gl-cm__btn--skip', !isTour || isLast);
        setHidden('.gl-cm__btn--prev', !isTour || isFirst);
        setHidden('.gl-cm__btn--next', !isTour || isLast);
        setHidden('.gl-cm__btn--done', isTour && !isLast);
        popoverEl.querySelector('.gl-cm__btn--done').textContent = isTour ? 'Klaar' : 'Begrepen';

        popoverEl.dataset.placement = step.placement || 'bottom';
        popoverEl.classList.toggle('gl-cm--tour', isTour);
    }

    function setHidden(selector, hidden) {
        const el = popoverEl.querySelector(selector);
        if (el) el.hidden = hidden;
    }

    function focusPrimary() {
        const btn = popoverEl.querySelector('.gl-cm__btn--primary:not([hidden])');
        if (btn) btn.focus({ preventScroll: true });
    }

    // ── Layout: bubbel (of sheet) + spotlight ─────────────────────────────────

    function layout() {
        if (!popoverEl || !targetRef) return;
        const step = flow.steps[currentStep];
        if (isSheet()) {
            popoverEl.classList.add('gl-cm--sheet');
            popoverEl.style.top = popoverEl.style.left = '';
        } else {
            popoverEl.classList.remove('gl-cm--sheet');
            positionPopover(targetRef, step.placement);
        }
        updateSpot();
    }

    function updateSpot() {
        // Spotlight enkel in een rondleiding; op telefoon (sheet) omkadert die het doel erboven.
        if (!isTour && !isSheet()) { removeSpot(); return; }
        if (!spotEl) {
            spotEl = document.createElement('div');
            spotEl.className = 'gl-cm-spot' + (isTour ? '' : ' gl-cm-spot--ring');
            spotEl.setAttribute('aria-hidden', 'true');
            document.body.appendChild(spotEl);
        }
        const r = targetRef.getBoundingClientRect();
        const pad = SPOT_PAD;
        spotEl.style.transform = 'translate(' + (r.left - pad) + 'px, ' + (r.top - pad) + 'px)';
        spotEl.style.width  = (r.width + pad * 2) + 'px';
        spotEl.style.height = (r.height + pad * 2) + 'px';
        if (reduceMotion) spotEl.style.transition = 'none';
    }

    function removeSpot() {
        if (spotEl) { spotEl.remove(); spotEl = null; }
    }

    // ── Position (bubbel) ─────────────────────────────────────────────────────

    function fits(placement, tr, pr, vw, vh) {
        switch (placement) {
            case 'bottom': return tr.bottom + GAP + pr.height <= vh;
            case 'top':    return tr.top - GAP - pr.height >= 0;
            case 'right':  return tr.right + GAP + pr.width <= vw;
            case 'left':   return tr.left - GAP - pr.width >= 0;
        }
        return false;
    }

    function positionPopover(targetEl, desiredPlacement) {
        const tr  = targetEl.getBoundingClientRect();
        const vw  = window.innerWidth;
        const vh  = window.innerHeight;
        const sx  = window.scrollX;
        const sy  = window.scrollY;

        // Reset zodat getBoundingClientRect() de echte afmetingen geeft
        popoverEl.style.top  = '0px';
        popoverEl.style.left = '0px';
        const pr = popoverEl.getBoundingClientRect();

        // Voorkeur: gevraagde kant, dan onder → boven → rechts → links; kantelt vanzelf
        const candidates = [desiredPlacement || 'bottom'].concat(ORDER);
        let placement = candidates.find(function (p) { return fits(p, tr, pr, vw, vh); }) || candidates[0];

        let top, left;
        switch (placement) {
            case 'top':
                top  = tr.top + sy - pr.height - GAP;
                left = tr.left + sx + (tr.width - pr.width) / 2;
                break;
            case 'right':
                top  = tr.top + sy + (tr.height - pr.height) / 2;
                left = tr.right + sx + GAP;
                break;
            case 'left':
                top  = tr.top + sy + (tr.height - pr.height) / 2;
                left = tr.left + sx - pr.width - GAP;
                break;
            default: // bottom
                top  = tr.bottom + sy + GAP;
                left = tr.left + sx + (tr.width - pr.width) / 2;
        }

        // Binnen het venster houden
        left = Math.max(sx + 8, Math.min(left, sx + vw - pr.width - 8));
        top  = Math.max(sy + 8, top);

        popoverEl.style.top  = top + 'px';
        popoverEl.style.left = left + 'px';
        popoverEl.dataset.placement = placement;

        // Pijl: wijst naar het midden van het doel, minstens 16px van de hoek.
        const horizontal = placement === 'top' || placement === 'bottom';
        const center = horizontal ? (tr.left + sx + tr.width / 2) - left : (tr.top + sy + tr.height / 2) - top;
        const size = horizontal ? pr.width : pr.height;
        const pos = Math.max(ARROW_MIN, Math.min(center, size - ARROW_MIN));
        popoverEl.style.setProperty('--gl-cm-arrow', pos + 'px');
    }

    // ── Reposition on resize / scroll ─────────────────────────────────────────

    function scheduleReposition() {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function () {
            if (!popoverEl || !targetRef) return;
            layout();
        }, 60);
    }

    window.addEventListener('resize', scheduleReposition, { passive: true });
    window.addEventListener('scroll', scheduleReposition, { passive: true });

    // ── Events ────────────────────────────────────────────────────────────────

    function bindPopoverEvents() {
        popoverEl.querySelector('.gl-cm__close').addEventListener('click', onDismiss);
        popoverEl.querySelector('.gl-cm__btn--skip').addEventListener('click', onDismiss);
        popoverEl.querySelector('.gl-cm__btn--prev').addEventListener('click', onPrev);
        popoverEl.querySelector('.gl-cm__btn--next').addEventListener('click', onNext);
        popoverEl.querySelector('.gl-cm__btn--done').addEventListener('click', onDone);
        popoverEl.addEventListener('keydown', onKeyDown);
        document.addEventListener('keydown', onDocKeyDown, true);
        document.addEventListener('mousedown', onOutside, true);
    }

    // Esc sluit (rondleiding: overslaan). Gebeurt ook buiten de bubbel, zolang er geen modal open is.
    function onDocKeyDown(e) {
        if (e.key !== 'Escape' || !popoverEl) return;
        if (document.querySelector('.modal.show, .gl-v2-menu.is-open')) return;
        onDismiss();
    }

    // Tab blijft in de bubbel tijdens een rondleiding.
    function onKeyDown(e) {
        if (e.key !== 'Tab' || !isTour) return;
        const items = Array.prototype.filter.call(
            popoverEl.querySelectorAll('button'), function (b) { return !b.hidden; });
        if (!items.length) return;
        const first = items[0], last = items[items.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    }

    // Eén tip: klik buiten de bubbel = sluiten. In een rondleiding niet (de pagina blijft bruikbaar).
    function onOutside(e) {
        if (isTour || !popoverEl) return;
        if (popoverEl.contains(e.target)) return;
        onDismiss();
    }

    function onDismiss() {
        apiPost(API.dismiss, { stateKey: flow.stateKey });
        teardown();
    }

    function onPrev() {
        if (currentStep > 0) showStep(currentStep - 1);
    }

    function onNext() {
        const nextStep = currentStep + 1;
        if (nextStep >= flow.steps.length) return;

        // Persist gevorderde stap zodat na refresh op de juiste stap verder gegaan wordt
        if (flow.isSequence) {
            apiPost(API.advanceStep, { stateKey: flow.stateKey, step: nextStep });
        }

        showStep(nextStep);
    }

    function onDone() {
        apiPost(API.complete, { stateKey: flow.stateKey });
        teardown();
    }

    // ── Teardown ──────────────────────────────────────────────────────────────

    function teardown() {
        window.removeEventListener('resize', scheduleReposition);
        window.removeEventListener('scroll', scheduleReposition);
        document.removeEventListener('keydown', onDocKeyDown, true);
        document.removeEventListener('mousedown', onOutside, true);

        if (targetRef) {
            targetRef.removeAttribute('aria-describedby');
            targetRef = null;
        }
        removeSpot();

        if (popoverEl) {
            popoverEl.remove();
            popoverEl = null;
        }
    }

    // ── API helper ────────────────────────────────────────────────────────────

    function getAntiForgeryToken() {
        return document.querySelector('meta[name="request-verification-token"]')?.content
            ?? document.querySelector('[name="__RequestVerificationToken"]')?.value
            ?? '';
    }

    function apiPost(url, body) {
        return fetch(url, {
            method:  'POST',
            headers: {
                'Content-Type':              'application/json',
                'RequestVerificationToken':  getAntiForgeryToken()
            },
            body: JSON.stringify(body)
        }).catch(function (err) {
            // Stil falen — coachmark-acties zijn non-blocking
            console.warn('[coachmarks] api error:', err);
        });
    }
})();
