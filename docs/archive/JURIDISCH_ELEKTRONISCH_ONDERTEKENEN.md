# Elektronisch ondertekenen van wijzigingsopdrachten — juridische afweging

Stand 2026-09-25. Opgesteld op basis van openbaar beschikbare bronnen (onderaan) om de keuze in CPM te onderbouwen.
**Dit is geen juridisch advies.** Laat de punten onder "Te laten nakijken" door een advocaat of notaris bevestigen vóór
je dit systematisch op klanten (consumenten) toepast.

## Korte conclusie

| Vorm | Geldig? | Bewijskracht als de klant het later betwist |
|---|---|---|
| **Handtekening op papier** | Ja | Sterk, maar ook een papieren handtekening kan ontkend worden (art. 8.19 BW); dan moet ze bewezen worden. |
| **Bevestiging per gewone e-mail** ("ik ga akkoord") | Ja voor wat geen vorm vereist | **Zwakst.** Geen handtekening in de zin van het bewijsrecht; boven € 3.500 tegenover een consument onvoldoende als enig bewijs (art. 8.9 BW). |
| **Onze oplossing**: persoonlijke link + code per e-mail + ingetypte naam + akkoordtekst + bewijsdossier | Ja — een elektronische handtekening mag niet geweigerd worden omdat ze elektronisch is (eIDAS art. 25) | **Middelmatig tot goed.** Duidelijk sterker dan een e-mail, maar het blijft een *eenvoudige* elektronische handtekening: de rechter beoordeelt of ze betrouwbaar genoeg is. |
| **itsme (gekwalificeerde e-handtekening, QES)** | Ja | **Sterkst.** Automatisch gelijkgesteld met een handgeschreven handtekening (art. 8.18 BW); de bewijslast ligt bij wie ze betwist. |

Dus: onze oplossing is **niet "automatisch even geldig" als een handtekening op papier of itsme**, wel **duidelijk beter
dan een bevestiging per mail** en in de meeste gewone gevallen bruikbaar. Voor grote bedragen of risicovolle klanten is
itsme (of een handtekening op papier) de zekerste keuze.

## 1. Wat de wet zegt (voor zover ik het kon bevestigen)

- **Niet weigeren omdat het elektronisch is.** De eIDAS-verordening (art. 25, lid 1) bepaalt dat een elektronische
  handtekening haar rechtsgevolg niet verliest louter omdat ze elektronisch is, ook niet als ze niet gekwalificeerd is.
- **Drie niveaus** (art. 8.1, 3° NBW verwijst naar de definities in eIDAS): gewone, geavanceerde en gekwalificeerde
  elektronische handtekening. Elk niveau heeft een andere bewijswaarde.
- **Gekwalificeerd = gelijk aan handgeschreven.** Een gekwalificeerde e-handtekening (itsme Sign, eID) wordt
  gelijkgesteld met een handgeschreven handtekening (art. 8.18 BW) en verschuift de bewijslast naar wie ze betwist.
- **Gewoon en geavanceerd**: geldig, maar de rechter oordeelt of ze in het concrete geval **voldoende betrouwbaar** zijn
  (identificatie, band met het document, integriteit). Hoe meer spoor je kan voorleggen, hoe sterker.
- **Ontkennen** (art. 8.19 BW): wie een handtekening tegengeworpen krijgt, mag ze ontkennen; dan moet de authenticiteit
  aangetoond worden. Dat geldt voor papier en voor niet-gekwalificeerde e-handtekeningen — daarom telt het bewijsdossier.
- **Bewijs boven € 3.500** (art. 8.9 BW): een rechtshandeling van € 3.500 of meer moet in principe bewezen worden met een
  **ondertekend geschrift**. Tegenover een **handelaar** is het bewijs vrij (dan volstaat zelfs een e-mail); tegenover een
  **consument** niet. Een wijzigingsopdracht van een woning zit vaak boven die grens, en de klant is meestal consument.
  Een gewone e-mail volstaat daar dus niet als enig bewijs; een (eenvoudige) elektronische handtekening met bewijsdossier is
  wel een geschrift met handtekening dat de rechter kan aanvaarden.
- **Bewijsovereenkomsten** (art. 8.2 BW): partijen mogen contractueel afspreken hoe iets bewezen wordt. Dat kan de zwakte van
  een eenvoudige e-handtekening opvangen (zie aanbeveling 1), maar bij consumenten kan een zulke clausule als
  onrechtmatig beding aangevochten worden.

## 2. Wet Breyne (woningbouwwet)

Als Group LN een woning (te bouwen) aan een particulier verkoopt of laat bouwen, valt de overeenkomst waarschijnlijk onder
de Wet Breyne. Wat ik daarover kon bevestigen: de overeenkomst moet **schriftelijk** zijn, verplichte vermeldingen
bevatten (o.a. **een bepaling over wijzigingen aan het oorspronkelijke project**, zowel op initiatief van de bouwheer als
noodzakelijke aanpassingen) en bij ontbreken ervan kan de koper de nietigheid van het beding of de overeenkomst vragen.
**Wat ik niet kon bevestigen**: of de wet zelf een specifieke vorm of handtekening eist voor een *afzonderlijke
wijzigingsopdracht*, en of een elektronische handtekening daarvoor volstaat. Dat hangt in de praktijk af van de clausule
"wijzigingen/meerwerk" in **jullie eigen overeenkomst**: staat daar "schriftelijk en ondertekend", dan moet aan die vorm
voldaan zijn — en een elektronische handtekening is dan een geldige invulling *als de overeenkomst dat toelaat*.

## 3. Wat het systeem vastlegt (bewijsdossier)

Per ondertekenaar (`DocumentSignatures`): de ingetypte naam, tijdstip, IP-adres en user-agent, de **exacte akkoordtekst**
die getoond werd, een **SHA-256-hash** van het PDF dat ter ondertekening ging, een unieke referentie (WO-…), het
e-mailadres waarnaar link en code gingen, wanneer de link geopend werd en wanneer de code werd aangevraagd. Van link en
code wordt enkel een hash bewaard. Het ondertekende PDF (met handtekeningblok en referentie) wordt als nieuwe versie
bewaard en het document is daarna **bevroren** (geen nieuwe revisies). De klant en de projectleider krijgen een
bevestigingsmail (de klant met het ondertekende PDF).

Wat dit **niet** bewijst: dat de persoon achter het toetsenbord echt de klant is — enkel dat iemand met toegang tot dat
mailadres de link opende en de code invoerde. Daarom is het een eenvoudige e-handtekening en geen geavanceerde of
gekwalificeerde (er is geen sterke identiteitscontrole zoals itsme/eID).

## 4. Aanbevelingen

1. **Bewijsclausule in de overeenkomst/algemene voorwaarden** (laten opstellen): de koper aanvaardt dat wijzigingsopdrachten
   elektronisch ondertekend mogen worden via een persoonlijke link en code op het opgegeven e-mailadres, dat dit geldt als
   handtekening, en dat de logbestanden als bewijs gelden. Geef het e-mailadres van de klant bij de overeenkomst op.
2. **Kies het niveau naar het risico.** Eenvoudige e-handtekening (ons systeem) voor gewone, kleine meerwerken; itsme of
   papier voor grote bedragen of als de klant het al betwistte. Een drempel (bv. vanaf € 3.500 incl. btw, de grens van
   art. 8.9) is een logische keuze — laat de grens bevestigen.
3. **Elke medekoper tekent.** Bij een koppel/meerdere kopers: laat elke persoon tekenen (het systeem ondersteunt meerdere
   ondertekenaars, elk met eigen link en e-mailadres). Eén handtekening namens twee mensen is zwak.
4. **Papier blijft mogelijk.** De e-mail bevat het PDF; wie liever op papier tekent, kan dat. Leg dan het ingescande,
   getekende exemplaar als nieuw document vast en registreer de handtekening handmatig.
5. **itsme later toevoegen** via een erkende aanbieder (Connective, Signicat, Namirial, Docusign, …): de tabel heeft al het
   veld `Method` ("itsme"), en de tekenpagina kan naar de aanbieder doorverwijzen in plaats van de code-stap.
6. **Privacy (AVG).** IP-adres en user-agent zijn persoonsgegevens: vermeld het doel (bewijs) in de privacyverklaring en het
   verwerkingsregister, en spreek een bewaartermijn af (bv. minstens zolang aansprakelijkheid kan spelen — laat bevestigen).
7. **Bewaar het originele PDF en de hash** samen met de logbestanden; de hash toont dat het document na verzending niet
   gewijzigd werd.

## 5. Te laten nakijken door een advocaat/notaris
- Welke clausule over wijzigingen/meerwerk staat in jullie Breyne-overeenkomst, en welke vorm eist ze?
- Volstaat een eenvoudige elektronische handtekening voor jullie soort klanten en bedragen (consument, ≥ € 3.500)?
- Tekst van de bewijsclausule en de akkoordtekst (`SigningService.ConsentText`).
- Bewaartermijn en privacyvermelding.

## Bronnen
- Orde van Vlaamse Balies, [Elektronische handtekening](https://www.ordevanvlaamsebalies.be/nl/nieuws-en-events/elektronische-handtekening) en [Wetgevingsdossier Bewijsrecht](https://www.advocaat.be/nl/fetch-asset?path=ovb%2FDocumenten%2Fburgerlijk-recht%2FWetgevingsdossier-Bewijsrecht.pdf)
- Wet van 13 april 2019 (Burgerlijk Wetboek, Boek 8 "Bewijs"): [Belgisch Staatsblad / Justel](https://www.ejustice.just.fgov.be/cgi_loi/change_lg.pl?language=nl&la=N&cn=2019041329&table_name=wet), [etaamb](https://etaamb.openjustice.be/nl/wet-van-13-april-2019_n2019012168.html)
- Kocks & Partners, [De elektronische handtekening en het (nieuw) Belgisch bewijsrecht (deel 2)](https://www.kockspartners-law.be/nl/aktuelles-beitrag-lesen-kopie/de-elektronische-handtekening-en-het-nieuw-belgisch-bewijsrecht-deel-2.html)
- NAV, [E-handtekening: rechtsgeldig voor architectuurovereenkomsten?](https://www.nav.be/kennisnet/e-handtekening-rechtsgeldig-voor-architectuurovereenkomsten)
- Bewijs boven € 3.500: [Jubel — Nieuw bewijsrecht](https://www.jubel.be/nieuw-bewijsrecht/), [Elfri](https://www.elfri.be/bewijs-boven-de-3500-euro-vereist-geschrift), [Embuild Antwerpen](https://embuildantwerpen.be/bewijsrecht/)
- Wet Breyne: [notaris.be — verplichte vermeldingen](https://www.notaris.be/wonen/bouwen-en-renoveren/mijn-bescherming-als-bouwheer-wet-breyne/verplichte-vermeldingen-in-de-overeenkomst-1), [Bouwunie](https://www.bouwunie.be/nl/advies/juridisch/wet-breyne/de-wet-breyne-of-woningbouwwet), [Elfri](https://www.elfri.be/artikel/wet-breyne)
- itsme Sign / QES: [itsme](https://www.itsme-id.com/en-BE/business/services/sign), [OneSpan](https://www.onespan.com/blog/everything-you-need-know-about-itsme-and-qualified-e-signatures-belgium), [Docusign + itsme](https://www.docusign.com/en-gb/integrations/itsme), [Signicat](https://developer.signicat.com/identity-methods/itsme/about-itsme/)
