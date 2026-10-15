---
target: projectpagina-v4
total_score: 23
max_score: 32
na_heuristics: 7,10
p0_count: 0
p1_count: 2
target_identity: "file:E:\\TFS\\WWWCOPRO\\Views\\Projects\\DetailV4.vbhtml"
target_fingerprint: "sha256:e1f6f73b3dc5188231f2b20480152ac8f344c67ee9daeb359e2e739ee04adb11"
target_path: "E:\\TFS\\WWWCOPRO\\Views\\Projects\\DetailV4.vbhtml"
timestamp: 2026-10-15T16-29-38Z
slug: wwwcopro-views-projects-detailv4-vbhtml
---
# Critique: projectpagina-v4 (inline, degraded: geen sub-agents)

Score 23/32 (Good, 72%); n/a: heuristiek 7 en 10. Detector: 147 advisory, 0 harde antipatronen (allemaal valse positieven t.o.v. admin-DESIGN.md).

Priority issues:
- [P1] Komma staat alleen aan het begin van een regel in de h1; titel loopt in halve kolom over drie regels (typeset).
- [P1] Gsm-veld weigert niet-Belgische nummers (harden).
- [P2] Woningenblok vraagt te veel in een halve kolom: 6-koloms prijslijst, schakel op kolombreedte (adapt).
- [P2] aria-live op het fotolabel, glyph in downloadlinks, focusbeheer lotkaartje (audit).
- [P2] reCAPTCHA en Leaflet laden bij elk bezoek (optimize).
