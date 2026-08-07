/* Scroll-reveal via IntersectionObserver — geen externe libraries.
   Vereist dat <html> de klasse "js-reveal" draagt (zie inline script
   in _Layout.vbhtml) — anders houdt reveal.css de elementen sowieso
   gewoon zichtbaar en doet dit script feitelijk niets zichtbaars. */
(function () {
    'use strict';

    if (!('IntersectionObserver' in window)) return;
    if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    var elements = document.querySelectorAll('.reveal');
    if (!elements.length) return;

    // Stagger: elementen die dezelfde <section> delen, verschijnen na
    // elkaar met een oplopende transition-delay (100ms per element).
    // Gebonden aan een maximum — anders loopt de delay bij lange lijsten
    // (bv. een grid met tientallen kaarten die één ouder delen) op tot
    // meerdere seconden, waardoor kaarten die al in beeld staan alsnog
    // lang onzichtbaar blijven.
    var MAX_STAGGER_STEPS = 4;
    var groups = [];
    elements.forEach(function (el) {
        var section = el.closest('section') || el.parentElement;
        var group = groups.filter(function (g) { return g.section === section; })[0];
        if (!group) {
            group = { section: section, items: [] };
            groups.push(group);
        }
        group.items.push(el);
    });
    groups.forEach(function (group) {
        group.items.forEach(function (el, index) {
            el.style.transitionDelay = (Math.min(index, MAX_STAGGER_STEPS) * 100) + 'ms';
        });
    });

    var observer = new IntersectionObserver(function (entries, obs) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) return;
            entry.target.classList.add('visible');
            obs.unobserve(entry.target);
        });
    }, { threshold: 0.15 });

    elements.forEach(function (el) {
        observer.observe(el);
    });
})();
