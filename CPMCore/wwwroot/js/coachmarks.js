/**
 * coachmarks.js — Feature discovery / coachmark systeem
 *
 * Leest window.GlCoachmarkFlow (gezet door _Coachmarks.cshtml) en toont
 * een positioneerbare popover naast het doelelement.
 *
 * Ondersteunt:
 *  - single coachmarks
 *  - multi-step sequences (Volgende / Vorige / Begrepen)
 *  - auto-flip bij ruimtegebrek
 *  - herpositionering bij resize / scroll
 *  - graceful degradation als het doelelement niet bestaat
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

    const GAP = 10;         // px between popover and target
    const MOBILE_BP = 576;  // px breakpoint for mobile adjustments

    // ── State ─────────────────────────────────────────────────────────────────

    let currentStep  = flow.currentStep ?? 0;
    let popoverEl    = null;
    let highlightEl  = null;
    let resizeTimer  = null;
    let shownMarked  = false;

    // ── Initialise ────────────────────────────────────────────────────────────

    function init() {
        showStep(currentStep);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // ── Show step ─────────────────────────────────────────────────────────────

    function showStep(stepIndex) {
        const step = flow.steps[stepIndex];
        if (!step) { teardown(); return; }

        const targetEl = document.querySelector(step.targetSelector);
        if (!targetEl) {
            // Target niet gevonden — skip zonder fout
            console.debug('[coachmarks] target not found:', step.targetSelector);
            return;
        }

        if (!popoverEl) {
            popoverEl = buildPopover();
            document.body.appendChild(popoverEl);
            bindPopoverEvents();
        }

        currentStep = stepIndex;
        renderContent(step, stepIndex);
        setHighlight(targetEl);

        // Laat browser eerst renderen zodat getBoundingClientRect() klopt
        requestAnimationFrame(function () {
            positionPopover(targetEl, step.placement);
            popoverEl.classList.add('gl-cm--visible');

            // Rapporteer "getoond" eenmalig per paginalading
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
        el.setAttribute('aria-live', 'polite');
        el.innerHTML =
            '<div class="gl-cm__arrow" aria-hidden="true"></div>' +
            '<div class="gl-cm__header">' +
                '<span class="gl-cm__title"></span>' +
                '<button class="gl-cm__close" type="button" aria-label="Sluiten">' +
                    '<svg width="11" height="11" viewBox="0 0 11 11" fill="none" aria-hidden="true">' +
                        '<path d="M1 1L10 10M10 1L1 10" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>' +
                    '</svg>' +
                '</button>' +
            '</div>' +
            '<div class="gl-cm__body"></div>' +
            '<div class="gl-cm__footer">' +
                '<span class="gl-cm__counter"></span>' +
                '<div class="gl-cm__actions">' +
                    '<button class="gl-cm__btn gl-cm__btn--prev" type="button">Vorige</button>' +
                    '<button class="gl-cm__btn gl-cm__btn--next" type="button">Volgende</button>' +
                    '<button class="gl-cm__btn gl-cm__btn--done" type="button">Begrepen</button>' +
                '</div>' +
            '</div>';
        return el;
    }

    // ── Render content ────────────────────────────────────────────────────────

    function renderContent(step, stepIndex) {
        const total   = flow.steps.length;
        const isFirst = stepIndex === 0;
        const isLast  = stepIndex === total - 1;
        const isMulti = total > 1;

        popoverEl.querySelector('.gl-cm__title').textContent = step.title;
        popoverEl.querySelector('.gl-cm__body').textContent  = step.message;

        const counter = popoverEl.querySelector('.gl-cm__counter');
        counter.textContent = isMulti ? (stepIndex + 1) + ' / ' + total : '';
        counter.hidden      = !isMulti;

        setHidden('.gl-cm__btn--prev', !isMulti || isFirst);
        setHidden('.gl-cm__btn--next', isLast);
        setHidden('.gl-cm__btn--done', isMulti && !isLast);

        popoverEl.dataset.placement = step.placement || 'bottom';
    }

    function setHidden(selector, hidden) {
        const el = popoverEl.querySelector(selector);
        if (el) el.hidden = hidden;
    }

    // ── Highlight ─────────────────────────────────────────────────────────────

    function setHighlight(targetEl) {
        if (highlightEl) highlightEl.classList.remove('gl-cm-target');
        highlightEl = targetEl;
        targetEl.classList.add('gl-cm-target');
    }

    // ── Position ──────────────────────────────────────────────────────────────

    function positionPopover(targetEl, desiredPlacement) {
        const tr  = targetEl.getBoundingClientRect();
        const vw  = window.innerWidth;
        const vh  = window.innerHeight;
        const sx  = window.scrollX;
        const sy  = window.scrollY;

        // Reset so getBoundingClientRect() gives real popover dimensions
        popoverEl.style.top  = '0px';
        popoverEl.style.left = '0px';
        const pr = popoverEl.getBoundingClientRect();

        let placement = desiredPlacement || 'bottom';

        // Auto-flip if insufficient viewport space
        if (placement === 'bottom' && tr.bottom + pr.height + GAP > vh) placement = 'top';
        if (placement === 'top'    && tr.top    - pr.height - GAP < 0)  placement = 'bottom';
        if (placement === 'right'  && tr.right  + pr.width  + GAP > vw) placement = 'left';
        if (placement === 'left'   && tr.left   - pr.width  - GAP < 0)  placement = 'right';

        let top, left;

        switch (placement) {
            case 'bottom':
                top  = tr.bottom + sy + GAP;
                left = tr.left   + sx + (tr.width  - pr.width)  / 2;
                break;
            case 'top':
                top  = tr.top  + sy - pr.height - GAP;
                left = tr.left + sx + (tr.width  - pr.width)  / 2;
                break;
            case 'right':
                top  = tr.top   + sy + (tr.height - pr.height) / 2;
                left = tr.right + sx + GAP;
                break;
            case 'left':
                top  = tr.top  + sy + (tr.height - pr.height) / 2;
                left = tr.left + sx - pr.width - GAP;
                break;
            default:
                top  = tr.bottom + sy + GAP;
                left = tr.left   + sx + (tr.width  - pr.width)  / 2;
        }

        // Clamp horizontally within viewport (accounting for scroll)
        const minLeft = sx + 8;
        const maxLeft = sx + vw - pr.width - 8;
        left = Math.max(minLeft, Math.min(left, maxLeft));

        // Clamp vertically (don't go above scroll position)
        top = Math.max(sy + 8, top);

        popoverEl.style.top       = top + 'px';
        popoverEl.style.left      = left + 'px';
        popoverEl.dataset.placement = placement;
    }

    // ── Reposition on resize / scroll ─────────────────────────────────────────

    function scheduleReposition() {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function () {
            if (!popoverEl) return;
            const step = flow.steps[currentStep];
            if (!step) return;
            const targetEl = document.querySelector(step.targetSelector);
            if (targetEl) positionPopover(targetEl, step.placement);
        }, 80);
    }

    window.addEventListener('resize', scheduleReposition, { passive: true });
    window.addEventListener('scroll', scheduleReposition, { passive: true });

    // ── Events ────────────────────────────────────────────────────────────────

    function bindPopoverEvents() {
        popoverEl.querySelector('.gl-cm__close')
            .addEventListener('click', onDismiss);
        popoverEl.querySelector('.gl-cm__btn--prev')
            .addEventListener('click', onPrev);
        popoverEl.querySelector('.gl-cm__btn--next')
            .addEventListener('click', onNext);
        popoverEl.querySelector('.gl-cm__btn--done')
            .addEventListener('click', onDone);
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

        if (highlightEl) {
            highlightEl.classList.remove('gl-cm-target');
            highlightEl = null;
        }

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
