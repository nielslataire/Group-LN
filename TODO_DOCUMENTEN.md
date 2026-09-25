# TODO — Documenten (design-handoff 17) en ondertekenen

Stand 2026-09-25: pagina `Projecten/DetailDocs` (gl-v2), model, service en migratie 049 zijn klaar en getest op
InMemory + gerenderde HTML. Zie DESIGN.md "Projecten/DetailDocsV2" en DEVNOTES.md §8.

## Eerst doen (voor de uitrol)
- [ ] `_migrations/049_DocumentenModel.sql` uitvoeren op de live DB **vóór** de nieuwe CPMCore-build.
- [ ] Na de migratie de pagina echt doorlopen in de browser (upload, revisie, koppelen, aanvragen, ondertekenen,
      gunnen) — tot nu toe enkel getest tegen InMemory-data en statische screenshots, niet in de volledige shell.
- [ ] Controleren dat WWWCOPRO `/projecten` (index + detail) nog exact dezelfde brochures toont als vóór de migratie.
- [ ] Startsjablonen in `DocumentTemplates` nalopen (nu enkel projecttype 1 = woonproject) en aanpassen.

## Ondertekenen met itsme (klant tekent een wijzigingsopdracht via een link)
Nu: handtekeningen worden handmatig geregistreerd (`DocumentSignatures.Method = 'manueel'`).
- [ ] Beslissen welk handtekeningniveau nodig is (eenvoudig / geavanceerd / gekwalificeerd) — juridisch laten bevestigen
      voor wijzigingsopdrachten aan consumenten.
- [ ] itsme rechtstreeks kan niet: itsme Sign loopt via een erkende dienstverlener (o.a. Connective, Signicat, Namirial)
      met een eigen contract. Aanbieder kiezen, sandbox-account aanvragen.
- [ ] Publieke ondertekenpagina met **unieke, niet-raadbare link** (token in de DB, vervalt, éénmalig geldig per
      ondertekenaar), zonder CPM-login: PDF tonen → identiteit bevestigen → tekenen.
- [ ] Eerst versie 1 zonder itsme: link per e-mail + code per sms/e-mail + vinkje "ik ga akkoord", vastleggen van
      tijdstip, IP en hash van het PDF-bestand.
- [ ] Van een `ChangeOrder` (bestaat al als data + factuurlijnen) een PDF genereren (QuestPDF, zoals de andere
      documenten) en als revisie in de map Contracten zetten → "Ter ondertekening sturen" vult de link.
- [ ] Na ondertekening: het getekende PDF (met handtekeningblok) bevriezen als revisie, mail met kopie naar de klant,
      wijzigingsopdracht op "aanvaard" zetten (en eventueel meewerken in de facturatie).
- [ ] Webhook/terugmelding van de aanbieder verwerken (`DocumentSignatures.Status/SignedDate/Method = 'itsme'`).

## Portalen (klant en leverancier)
- [ ] Klantportaal: "mijn woning" / "het project", enkel huidige goedgekeurde revisie van gedeelde documenten
      (`DocumentService.GetPortalDocuments` levert dit al).
- [ ] Leveranciersportaal: gedeelde documenten met revisieletter, open aanvragen met uploadknop; upload komt binnen als
      "ter goedkeuring" (`UploaderKind`), offertes als nieuwe versie met bedrag + reden.
- [ ] Portaaltoegang/login-mechanisme kiezen (magic link, account, itsme-login).
- [ ] "Gezien in portaal" bijhouden (`DocumentLinks.SeenDate`, `DocumentRequests.SeenInPortalDate`).

## Documentenpagina — nog niet gebouwd
- [ ] Automatische herinneringen (achtergrondtaak): "30 dagen voor deadline, dan wekelijks" —
      `ReminderDaysBefore` en `LastReminderDate` liggen klaar. Nu enkel op klik.
- [ ] Bestelbon genereren als PDF bij "Gunnen" (nu een verwacht document "Bestelbon …").
- [ ] Documentenkaart (`Partials/_DocumentsCardV2`, endpoint `DocumentCardV2`) inbouwen op eenheid-, klant- en
      leveranciersdetail (nu enkel het ⋯-menu "Documenten" → gefilterde lijst).
- [ ] Kolomsets per map instelbaar voor een beheerder (nu vast per `ViewKind`).
- [ ] Beheerscherm voor `DocumentFolders` en `DocumentTemplates` (nu enkel via SQL).
- [ ] Verwijderen haalt bestanden niet uit de storage (zoals de legacy pagina) — opruimtaak overwegen.
- [ ] Revisies van een document naast elkaar vergelijken (nu enkel bedragen bij offertes).
- [ ] Vervaldatum-meldingen ("vervalt binnen 90 d") in het meldingenscherm/dashboard.
- [ ] Legacy pagina `DetailDocs` (v1) en `_ModalAddDoc` verwijderen zodra de oude lay-out uitgezet wordt.
