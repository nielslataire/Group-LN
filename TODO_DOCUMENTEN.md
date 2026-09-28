# TODO — Documenten (design-handoff 17) en ondertekenen

Stand 2026-09-25: pagina `Projecten/DetailDocs` (gl-v2), model, service en migratie 049 zijn klaar en getest op
InMemory + gerenderde HTML. Zie DESIGN.md "Projecten/DetailDocsV2" en DEVNOTES.md §8.

## Eerst doen (voor de uitrol)
- [ ] `_migrations/049_DocumentenModel.sql` en daarna `_migrations/054_OndertekeningPerLink.sql` uitvoeren op de live DB **vóór** de nieuwe
      CPMCore-build (054 verwacht 049; de app-code leest de nieuwe kolommen van beide).
- [ ] Na de migratie de pagina echt doorlopen in de browser (upload, revisie, koppelen, aanvragen, ondertekenen,
      gunnen) — tot nu toe enkel getest tegen InMemory-data en statische screenshots, niet in de volledige shell.
- [ ] Controleren dat WWWCOPRO `/projecten` (index + detail) nog exact dezelfde brochures toont als vóór de migratie.
- [ ] Startsjablonen in `DocumentTemplates` nalopen (nu enkel projecttype 1 = woonproject) en aanpassen.

## Ondertekenen van wijzigingsopdrachten (gebouwd 2026-09-25, zonder itsme)
Klaar: PDF van de wijzigingsopdracht → persoonlijke link per ondertekenaar (`/ondertekenen/{token}`, geen login) → code per e-mail →
ingetypte naam + akkoordtekst → bewijsdossier → ondertekend PDF (met handtekeningblok) als nieuwe versie in de map Contracten, document
bevroren, `ChangeOrder.DateAgreement` gezet, bevestigingsmails. Intern: `Projecten/ChangeOrderSignV2` (vanuit het icoon "Digitaal ter
ondertekening sturen" in de lijst wijzigingsopdrachten). Juridische afweging: JURIDISCH_ELEKTRONISCH_ONDERTEKENEN.md. Migratie 054 (na 049).
- [ ] **Migratie 054 uitvoeren** (na 049) en de flow één keer echt doorlopen met je eigen e-mailadres (mail, PDF-generatie via wkhtmltopdf
      in een anonieme request, opslag in storage, ondertekend PDF).
- [ ] Advocaat laten nakijken: Breyne-clausule over wijzigingen, drempel eenvoudig vs itsme, bewijsclausule, akkoordtekst, bewaartermijn/privacy.
- [ ] Bewijsclausule in de overeenkomst/algemene voorwaarden opnemen (e-mailadres van de koper, elektronisch ondertekenen, logbestanden als bewijs).
- [ ] itsme: aanbieder kiezen (Connective, Signicat, Namirial, Docusign, …), contract + sandbox; tekenpagina laten doorverwijzen naar de aanbieder
      i.p.v. de code-stap; webhook verwerkt `DocumentSignatures.Method = 'itsme'`.
- [ ] Drempel-regel: vanaf een bedrag (bv. € 3.500 incl. btw) itsme/papier verplichten i.p.v. eenvoudige handtekening.
- [ ] Automatische herinnering aan ondertekenaars die nog niet tekenden (nu enkel "Link opnieuw sturen").
- [ ] Wijzigingsopdrachten hebben nog geen gl-v2-lijstpagina: het icoon zit op de legacy lijst (`Klanten/Partials/ChangeOrders`); bij de
      gl-v2-versie de actie "Ter ondertekening sturen" opnemen (ook in `Klanten/DetailV2`).
- [ ] Ondertekend PDF opnieuw maken als het genereren of uploaden na het tekenen mislukte (nu zie je enkel dat de laatste versie het
      ondertekende PDF nog niet is).
- [ ] Papieren handtekening: ingescand exemplaar als nieuw document vastleggen + "Getekend" handmatig registreren werkt al; een aparte
      "getekend op papier"-knop is nog niet gemaakt.
- [ ] Ondertekenen ook voor andere documenten (compromis, offerte-aanvaarding, bestelbon): de service is generiek behalve de PDF-bouw
      (`ChangeOrderPdfService`) en de link `ProjectDocs.ChangeOrderId`.

## Portalen (klant en leverancier)
- Opmerking: er bestaat al een aannemersportaal (`ContractorPortalController`, `/Werfportaal`, aanmelden als "contractor"). Het leveranciersgedeelte
  van de documenten kan daarop aansluiten i.p.v. een nieuw portaal; een klantportaal (`/klantenportaal`) bestaat nog niet (enkel de redirect).
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
