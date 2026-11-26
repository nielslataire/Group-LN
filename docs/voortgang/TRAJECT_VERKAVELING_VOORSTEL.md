# Voorstel trajectsjablonen verkaveling (met en zonder wegenis) – 26/11/2026

Status: GEBOUWD als script 169 (26/11/2026, niet gedraaid) op basis van de bijgewerkte lijst van Niels. Basis: de huidige sjablonen uit migratie 041, de bouwstaten (158/161), het planningssjabloon "Woning" (162) en de schijven (163).
Rollen: PL = projectleider, PO = projectontwikkelaar, BH = boekhouder/facturatie, VK = verkoper, EX = extern (notaris, landmeter, architect, studiebureau), ARCH = architect.
Type: P = projectniveau (één keer), W = per woning (één per lot).

## Opbouw
Eén sjabloon per variant, met twee delen:
1. Projectdeel (verschilt): alleen het stuk van de verkaveling zelf.
2. Woningdeel (identiek in beide varianten): fases per woning, scope "per eenheid".
Enige verschil in het woningdeel: de bouwvergunning van de koper wacht bij "met wegenis" op de voorlopige oplevering van de wegenis.

## Deel 1 – Projectdeel

### Beide varianten
| Fase | Mijlpaal | Rol | Type | Opmerking |
|---|---|---|---|---|
| Aankoop & studie | Grondverwerving en verkavelingsstudie | PO | P | bestaat |
| Aankoop & studie | Financiering rond (bankkrediet, bankgaranties) | PO | P | NIEUW (ontwikkelaar) |
| Aankoop & studie | Verkoopprijzen en verkoopdossier vastgelegd | PO | P | NIEUW (verkoper): prijslijst, brochure, verkoopsvoorwaarden |
| Vergunning | Aanvraag verkavelingsvergunning | PO | P | bestaat |
| Vergunning | Openbaar onderzoek | PO | P | bestaat, trigger bezwaartermijn |
| Vergunning | Verkavelingsvergunning definitief | PO & PL | P | bestaat (35 dagen) |
| Kavelvorming | Landmeterplan en afpaling | PO | P | bestaat |
| Kavelvorming | Kadastrering kavels | PO | P | bestaat |
| Verkoop | Vrijgave verkoop kavels | PO & VK | P | bestaat |
| Werfvoorbereiding | Aanstelling veiligheidscoördinator en EPB-verslaggever | PL | P | NIEUW |
| Werfvoorbereiding | Werfmelding ingediend | PL | P | NIEUW |
| Werfvoorbereiding | Verzekering Alle Bouwplaatsrisico's afgesloten | PL | P | NIEUW (koppeling met Verzekeringen) |
| Overdracht & nazorg | Verkavelaarsverplichtingen afgerond | PL | P | bestaat, eindpunt |

### Enkel "met wegenis"
| Fase | Mijlpaal | Rol | Type | Opmerking |
|---|---|---|---|---|
| Ontwerp infrastructuur | Voorontwerp infrastructuur | PO | P | bestaat |
| Vergunning | Adviezen instanties compleet | PO | P | bestaat |
| Vergunning | Gemeenteraadsbeslissing overname wegenis | PO | P | bestaat |
| Vergunning | Financiële waarborg gestort | PO | P | bestaat, trigger "bankgarantie regelen" |
| Aanleg wegenis | Aanbesteding wegenis-aannemer | PL | P | bestaat |
| Aanleg wegenis | Nutsvoorzieningen aangelegd | PL | P | bestaat (de bouwstaten WEGENIS_* uit 161 volgen de planning) |
| Aanleg wegenis | Voorlopige oplevering wegenis | PL | P | bestaat, ontgrendelt verkoop en bouwvergunning koper |
| Aanleg wegenis | Definitieve oplevering en overdracht wegenis | PL | P | bestaat, trigger "vrijgave waarborg" |
| Overdracht & nazorg | Waarborgperiode wegenis | PL | P | bestaat |

## Deel 2 – Woningdeel (per lot)

### Verkoop (VK)
| Mijlpaal | Rol | Opmerking |
|---|---|---|
| Compromis getekend | VK | bestaat, datum uit het klantaccount |
| Opschortende voorwaarden vervuld (lening, ...) | VK | NIEUW |
| Akte verleden | VK | bestaat, datum uit het klantaccount; gekoppeld aan aktefacturen |
| Bouwvergunning koper ingediend | PO | NIEUW (met wegenis: pas na voorlopige oplevering wegenis) |
| Bouwvergunning koper verleend | PO | bestaat als "Bouwvergunning koper mogelijk" |
| Afwerkingskeuzes en meerwerk bevestigd | PL | NIEUW |
| Sleuteloverdracht aan koper | PL | NIEUW, valt samen met voorlopige oplevering |

### Facturatie (BH)
| Mijlpaal | Rol | Opmerking |
|---|---|---|
| Aktefacturen klaargezet (grond en kosten) | BH | NIEUW, via de trigger uit 168 (dag vóór de akte), vervangt de losse taak |
| Aktefacturen verstuurd en betaald | BH | NIEUW |
| Schijven 1 tot 13 gefactureerd | BH | niet als aparte mijlpalen: de schijven volgen de bouwstaten (163) en geven al een taak "schijf bereikt" |
| Eindafrekening (meer- en minwerk, aansluitkosten) | BH | NIEUW, bij voorlopige oplevering |
| Saldo ontvangen voor sleuteloverdracht | BH | NIEUW |

### Bouw (PL), de bouwstaten uit 162
| Mijlpaal | Bouwstaat | Rol |
|---|---|---|
| Werf uitgezet | WERF_UITGEZET | PL |
| Funderingen klaar | FUNDERING_KLAAR | PL |
| Vloerplaat gelijkvloers | VLOERPLAAT_GLVL | PL |
| Opstort verdieping | OPSTORT | PL |
| Gesloten ruwbouw (metselwerk, dakplaat of dakstructuur) | METSELWERK_RUWBOUW | PL |
| Dak dicht | DAK_PLAT_DICHT of DAK_HELLEND_DICHT | PL |
| Buitenschrijnwerk geplaatst (winddicht) | BUITENSCHRIJNWERK | PL |
| Technieken ruw | TECHNIEKEN_RUW | PL |
| Keuring riolering | – | PL |
| Keuring elektriciteit | – | PL |
| Pleisterwerk | PLEISTER | PL |
| Chape | CHAPE | PL |
| Keuken geplaatst | KEUKEN | PL |
| Voorlopige oplevering | VOORLOPIGE_OPLEVERING | PL |
| EPB-aangifte ingediend | – | PL |
| EPB-aangifte | – | PL |
| PID afgeleverd | – | PL |
| Definitieve oplevering | – | PL |

### Ontwikkelaar (PO)
| Mijlpaal | Rol | Opmerking |
|---|---|---|
| Nacalculatie woning afgesloten | PO | NIEUW, koppeling met Nacalculatie |
| Garantie (10 jaar) en verzekering woning in orde | PO | NIEUW, koppeling met Verzekeringen |

## Vragen aan Niels
1. Klopt de lijst per rol, en wat ontbreekt of is overbodig?
2. Moeten de bouwmijlpalen echt allemaal per woning, of volstaan er minder (bv. alleen funderingen, gesloten ruwbouw, dak, voorlopige oplevering)? Meer mijlpalen betekent meer rijen: 14 lots x 40 = 560.
3. Termijn definitieve oplevering: 1 jaar na voorlopige? [VRAAG]
4. Welke vergunningen horen in het projectdeel behalve de verkavelingsvergunning (bv. omgevingsvergunning, rooivergunning)?
5. Mag de bestaande 041-sjabloon voor verkaveling vervangen worden, of liever een nieuwe versie ernaast zodat bestaande projecten niet veranderen? Aanbevolen: ernaast, met een nieuwe naam.
