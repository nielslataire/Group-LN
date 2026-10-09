// Gedeelde projectdata + kaart voor de Keerstraat-varianten
(function () {
  const P = [50.9155, 3.9390];
  const eur = n => '€ ' + Math.round(n).toLocaleString('nl-BE');
  const D = {
    P, eur,
    units: {
      1: { id: 1, lot: 'Lot 1', type: 'Halfopen', bew: 200, grond: 467, sk: 3, bk: 1, tuin: 'Zuidwest', epc: 'E20', status: 'Verkocht', prijs: null },
      2: { id: 2, lot: 'Lot 2', type: 'Gesloten', bew: 230, grond: 411, sk: 4, bk: 2, tuin: 'Zuid', epc: 'E20', status: 'Verkocht', prijs: null },
      3: { id: 3, lot: 'Lot 3', type: 'Halfopen', bew: 200, grond: 404, sk: 3, bk: 1, tuin: 'Zuidoost', epc: 'E20', status: 'Beschikbaar', prijs: 567000 },
      4: { id: 4, lot: 'Lot 4', type: 'Open', bew: 241, grond: 2081, sk: 4, bk: 2, tuin: 'West', epc: 'E15', status: 'Verkocht', prijs: null }
    },
    cats: [
      { id: 'school', label: 'Scholen', ic: 'S' }, { id: 'shop', label: 'Winkels', ic: 'W' },
      { id: 'ov', label: 'Openbaar vervoer', ic: 'OV' }, { id: 'groen', label: 'Groen', ic: 'G' },
      { id: 'sport', label: 'Sport', ic: 'Sp' }, { id: 'fiets', label: 'Fietsbereik', ic: '◎' }
    ],
    pois: [
      { id: 1, cat: 'school', name: 'Basisschool Vlekkem', ll: [50.9178, 3.9352], dist: '350 m', time: '5 min te voet' },
      { id: 2, cat: 'school', name: 'Secundaire school Erpe', ll: [50.9290, 3.9660], dist: '2,4 km', time: '8 min fietsen' },
      { id: 3, cat: 'shop', name: 'Dorpsbakker', ll: [50.9168, 3.9418], dist: '250 m', time: '3 min te voet' },
      { id: 4, cat: 'shop', name: 'Supermarkt Erpe-Mere', ll: [50.9262, 3.9612], dist: '2,0 km', time: '7 min fietsen' },
      { id: 5, cat: 'ov', name: 'Bushalte Keerstraat', ll: [50.9149, 3.9362], dist: '200 m', time: '3 min te voet' },
      { id: 6, cat: 'ov', name: 'Station Erpe-Mere', ll: [50.9272, 3.9690], dist: '2,5 km', time: '9 min fietsen' },
      { id: 7, cat: 'groen', name: 'Wandelroute Molenbeekvallei', ll: [50.9102, 3.9305], dist: '800 m', time: '10 min te voet' },
      { id: 8, cat: 'sport', name: 'Sporthal & voetbalvelden', ll: [50.9230, 3.9560], dist: '1,6 km', time: '6 min fietsen' }
    ],
    travel: [{ to: 'Aalst', min: '12 min' }, { to: 'E40', min: '8 min' }, { to: 'Gent', min: '30 min' }, { to: 'Brussel', min: '35 min' }],
    timeline: [
      { datum: 'Mrt 2025', titel: 'Vergunning', st: 'done' }, { datum: 'Sep 2025', titel: 'Start werken', st: 'done' },
      { datum: 'Feb 2026', titel: 'Ruwbouw', st: 'done' }, { datum: 'Jun 2026', titel: 'Wind- en waterdicht', st: 'done' },
      { datum: 'Nu', titel: 'Afwerking', st: 'now' }, { datum: 'Voorjaar 2027', titel: 'Oplevering', st: 'next' }
    ],
    steps: [
      { t: 'Kennismaking', p: 'Een gesprek over je wensen, budget en timing.' },
      { t: 'Plannen & keuze', p: 'Samen overlopen we plannen, lastenboek en prijs.' },
      { t: 'Reservatie', p: 'We houden je woning vrij terwijl je financiering rond raakt.' },
      { t: 'Compromis & akte', p: 'Wij begeleiden je tot bij de notaris.' },
      { t: 'Afwerking kiezen', p: 'Keuken, sanitair en vloeren bij onze partners.' },
      { t: 'Sleutels', p: 'Samen lopen we alles na. Welkom thuis.' }
    ],
    faq: [
      { q: 'Wat kost een nieuwbouwwoning aan de Keerstraat in Vlekkem?', a: 'Lot 3, een halfopen woning van 200 m² op een perceel van 404 m², is beschikbaar vanaf € 567.000. De andere drie woningen zijn verkocht.' },
      { q: 'Wanneer worden de woningen opgeleverd?', a: 'De werken zitten in de afwerkingsfase. De oplevering is gepland in het voorjaar van 2027.' },
      { q: 'Betaal ik btw of registratierechten?', a: 'Bij nieuwbouw betaal je doorgaans btw op het constructiegedeelte en registratierechten op het grondaandeel. Je notaris maakt vooraf een exacte raming.' },
      { q: 'Kan ik de afwerking nog zelf kiezen?', a: 'Ja. Keuken, sanitair, vloeren en binnendeuren kies je bij onze partners, binnen een afwerkingsbudget dat in de prijs zit.' },
      { q: 'Hoe energiezuinig zijn de woningen?', a: 'E-peil E20 of lager, met warmtepomp, vloerverwarming, ventilatie D en voorbereiding voor zonnepanelen.' },
      { q: 'Hoe bereikbaar is Vlekkem?', a: 'Station Erpe-Mere ligt op 2,5 km (lijn Gent–Brussel). Aalst en de E40 bereik je met de auto in een kwartier.' }
    ]
  };

  D.injectLd = function (id) {
    if (document.getElementById(id)) return;
    const u = D.units[3];
    const ld = [
      { '@context': 'https://schema.org', '@type': 'Residence', name: 'Verkaveling Keerstraat', address: { '@type': 'PostalAddress', streetAddress: 'Keerstraat 237', postalCode: '9420', addressLocality: 'Vlekkem', addressRegion: 'Oost-Vlaanderen', addressCountry: 'BE' }, geo: { '@type': 'GeoCoordinates', latitude: P[0], longitude: P[1] } },
      { '@context': 'https://schema.org', '@type': 'Offer', name: 'Lot 3 — halfopen woning, Keerstraat Vlekkem', price: u.prijs, priceCurrency: 'EUR', availability: 'https://schema.org/InStock', seller: { '@type': 'Organization', name: 'Group LN', telephone: '+32 9 216 49 50' } },
      { '@context': 'https://schema.org', '@type': 'FAQPage', mainEntity: D.faq.map(f => ({ '@type': 'Question', name: f.q, acceptedAnswer: { '@type': 'Answer', text: f.a } })) }
    ];
    const s = document.createElement('script'); s.type = 'application/ld+json'; s.id = id; s.textContent = JSON.stringify(ld); document.head.appendChild(s);
  };

  // opts: { layers: [...], dark: bool, zoom: n }
  D.initMap = function (el, opts) {
    opts = opts || {};
    const L = window.L; if (!L || !el) return null;
    const m = L.map(el, { scrollWheelZoom: false }).setView(P, opts.zoom || 15);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap-bijdragers', maxZoom: 19 }).addTo(m);
    const ring = opts.dark ? '#C9A96E' : '#00532D';
    const pin = (t, big) => L.divIcon({ className: '', iconSize: big ? [52, 52] : [30, 30], iconAnchor: big ? [26, 26] : [15, 15],
      html: big
        ? '<div style="width:52px;height:52px;border-radius:50%;background:#00532D;border:3px solid #C9A96E;display:flex;align-items:center;justify-content:center;box-shadow:0 6px 18px rgba(0,0,0,.35)"><img src="WWWCOPRO/Content/img/logo.png" style="width:30px;height:30px;object-fit:contain"></div>'
        : '<div style="width:30px;height:30px;border-radius:50%;background:' + (opts.dark ? '#00532D' : '#fff') + ';border:2px solid ' + ring + ';color:' + (opts.dark ? '#fff' : '#00532D') + ';font:900 10px Avenir,sans-serif;display:flex;align-items:center;justify-content:center;box-shadow:0 2px 8px rgba(0,0,0,.25)">' + t + '</div>' });
    L.marker(P, { icon: pin('', true), zIndexOffset: 1000 }).addTo(m).bindPopup('<b>Verkaveling Keerstraat</b><br>Keerstraat 237, 9420 Vlekkem');
    const groups = {}, markers = {};
    D.cats.forEach(c => { groups[c.id] = L.layerGroup(); });
    D.pois.forEach(p => {
      const mk = L.marker(p.ll, { icon: pin(D.cats.find(c => c.id === p.cat).ic) }).bindPopup('<b>' + p.name + '</b><br>' + p.dist + ' · ' + p.time);
      groups[p.cat].addLayer(mk); markers[p.id] = mk;
    });
    [[1300, '5 min'], [2600, '10 min'], [3900, '15 min']].forEach(([r, l]) =>
      groups.fiets.addLayer(L.circle(P, { radius: r, color: ring, weight: 1.2, dashArray: '4 6', fillColor: ring, fillOpacity: 0.04 }).bindTooltip(l + ' fietsen')));
    const api = {
      map: m,
      set(layers) {
        D.cats.forEach(c => { const on = layers.includes(c.id); if (on && !m.hasLayer(groups[c.id])) groups[c.id].addTo(m); if (!on && m.hasLayer(groups[c.id])) m.removeLayer(groups[c.id]); });
        if (layers.includes('fiets')) m.flyTo(P, 13, { duration: 0.6 });
      },
      fly(id) { const p = D.pois.find(x => x.id === id); if (!p) return; if (!m.hasLayer(groups[p.cat])) groups[p.cat].addTo(m); m.flyTo(p.ll, 16, { duration: 0.6 }); setTimeout(() => markers[id].openPopup(), 650); },
      destroy() { m.remove(); }
    };
    api.set(opts.layers || ['school', 'shop', 'ov', 'groen']);
    return api;
  };
  window.KEERSTRAAT = D;
})();
