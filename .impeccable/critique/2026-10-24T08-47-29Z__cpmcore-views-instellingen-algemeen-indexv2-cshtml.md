---
target: pagina Instellingen
total_score: 28
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 1
target_identity: "file:C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\Instellingen\\Algemeen\\IndexV2.cshtml"
target_fingerprint: "sha256:96742a48144c1db14fc9989c114e367279892a6e8568b1e52ed0778da41a822c"
target_path: "C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\Instellingen\\Algemeen\\IndexV2.cshtml"
timestamp: 2026-10-24T08-47-29Z
slug: cpmcore-views-instellingen-algemeen-indexv2-cshtml
---
Method: DEGRADED single-context (geen sub-agents ingezet; geen browser, bronanalyse)

Score 28/40 (Good). Heuristics: 1:3 2:3 3:3 4:3 5:3 6:3 7:2 8:3 9:3 10:2. Detector: 0 bevindingen.

Priority issues
- [P1] Kaarten (div tabindex=0 + click-listener, gl-v2-shell.js initClickableRows) niet met Enter te openen, geen link-rol, geen middelklik. Fix: echte <a> in de titel met stretched link; chips erboven. (harden)
- [P2] 24 kaarten even zwaar, geen hierarchie; dagelijkse onderdelen eerst/zwaarder. (layout)
- [P2] Zoeken verbergt groepen maar niet de menu-items/tellers; klik scrolt naar verborgen sectie. (polish)
- [P3] Lege zoekstaat zonder "Zoekterm wissen"-knop. (clarify)
- [P3] 9/10/11px tekst voor info-dragende elementen. (typeset)

Personas: Alex (Enter doet niets, geen sneltoets), Sam (kaart zonder rol), Casey (chips 30px < 44px, menu-rij scrolt niet mee onder 1024px).
