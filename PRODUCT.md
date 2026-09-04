# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Broad internal back-office team at **Group LN**, with no single dominant persona — the
same application serves several roles doing different daily jobs:

- **Project managers** — run residential development projects from acquisition through
  design, sales, subcontractor management, construction follow-up and delivery.
- **Customer / sales administration** — buyers (klanten), client accounts, contracts.
- **Accounting / finance** — invoicing, VAT, Peppol e-invoicing, Octopus accounting sync.
- **Management** — cross-project, cross-company dashboards and reporting.

External users with narrower, permission-gated access:

- **Subcontractors** via the contractor portal (with an invite flow).
- **Customer-facing views** served from the same application.

Access is scoped by granular read/write permissions per module **per issuer company**.

## Product Purpose

CPM is Group LN's internal operations platform for a Belgian residential
property-development group. It is the system of record for running development
projects end to end — acquisition, design, sales to buyers, subcontractor
management, construction follow-up, budgeting and cost control, and
invoicing/accounting — across multiple legal entities ("issuer companies") and
sub-brands (Group LN, BCO, Home-Estate). It also manages public website content
(blog, vacancies, home-hero project). Success means one linked system replacing
spreadsheets and disconnected tools, with every project, buyer, subcontractor,
budget, invoice and document tied together.

## Positioning

Internal tool — the distinguishing mechanisms, not a market claim:

- **Budget formula engine** — wizard fields are linked to indexed cost-price
  materials through developer-defined C# formulas plus user-managed material
  couplings and editable per-activity formulas; hard-coded proposals remain as
  fallback.
- **Belgian construction-index integration** — S-index (arch-index.be, category A,
  2B rows) and I-2021 index feed budget proposals via a weighted index.
- **Integrated market analysis** — municipality analysis and comparable-property
  analysis backed by a separate property-listing crawler (GroupLN.MarketData).
- **Multi-entity invoicing** — per-tariff VAT computed on the taxable base
  (EN 16931 / Peppol BR-CO-17), EPC QR payment codes, Peppol/UBL output, and
  Octopus (Inaras) accounting bookings.

## Operating Context

- **Multi-company by default** — lists, permissions, documents and invoicing are
  scoped per issuer company (billing entity); users may have access to a subset.
- **Belgian construction domain** — VAT on the taxable base, Peppol/UBL
  e-invoicing, EPC QR payment codes, construction indices (S, I-2021),
  weerverlet (weather-delay) tracking, bank-guarantee documents on contracts.
- **Language & formats** — Dutch (Belgium) throughout; EUR; nl-BE number and
  date formatting. These are fixed requirements.
- **External integrations** — Octopus / Inaras accounting REST API; arch-index.be
  and FGOV index sources; the GroupLN.MarketData worker/crawler; a centralized
  Storage service for documents, plans, pictures and delivery reports.
- **Where it is used** — office desktops *and* on-site on tablets/phones by
  project managers and contractors; responsive, touch-usable behavior is a real
  requirement, not a nice-to-have.
- **Sub-brands** — Group LN (holding), BCO / Bouwen & Constructie
  (bouwenconstructie.be, construction), Home-Estate (home-estate.be, real estate).

## Capabilities and Constraints

**Modules:** Dashboard; Leveranciers (subcontractors); Klanten (buyers, client
accounts, projects per client); Projecten (project detail, contracts + bank-
guarantee documents, budget wizard, voortgang/progress, project issues +
notifications, weerverlet); Budget (cost-price materials, formula engine +
couplings, bouwkost percentages, budget-activity formulas); Facturatie (invoices
per billing company, per-tariff VAT, Peppol/UBL, EPC QR, Octopus bookings,
invoice layout/enrichment); Documentencentrum; Marktanalyse (gemeenteanalyse,
vergelijkbare panden, projectdetail); Contractor portal + contractor invites;
Instellingen (issuer companies, roles/permissions, email templates, blog,
vacancies, home-hero project); User admin; Search.

**Architecture (must be respected):** layered solution — DALCore (entities /
DbContext, no business logic), FacadeCore (viewmodels / DTOs, no UI dependency),
ServiceCore (business logic, validation, entity↔VM mapping, orchestration),
BOCore (shared business objects / enums / constants), CPMCore (controllers,
Razor views, layout, frontend — light flow logic only), Storage (all file
access, centralized). File access goes only through Storage.

**Tech:** ASP.NET Core MVC + Razor, EF Core, SQL Server. Front end is a
Bootstrap-based "modern" admin theme with a jQuery plugin ecosystem (select2,
bootstrap-datepicker, bootstrap-multiselect, pnotify, morris, magnific-popup).
New UI is expected to stay Bootstrap-compatible and avoid global CSS conflicts.

**Schema changes** ship as hand-run SQL scripts in `_migrations/` and
`DALCore/Migrations/`, not EF migrations.

**Open decision (resolve in new-work, not here):** whether the admin shell
(`CPMCore/Views/Shared/_Layout.cshtml` and the left sidebar) stays as the current
third-party theme or is reshaped to the Group LN identity was explicitly left
open by the user.

## Brand Commitments

- **Name:** "CPM" (application), operated by Group LN. Browser title is
  "CPM - GROUP LN"; `Branding.CompanyName` = "Group LN".
- **Sub-brands / external sites:** Group LN (groupln.be), BCO / Bouwen &
  Constructie (bouwenconstructie.be), Home-Estate (home-estate.be).
- **Visual identity:** a formal Group LN identity (logo, colour, type) exists and
  will be provided by the user; it must be honored. Repo assets are only partial —
  `CPMCore/wwwroot/Img/groupln-logo.png` and `CPMCore/wwwroot/Img/Logo-bco.png`
  are present; the other `logo-*.png` files belong to the third-party admin
  template, not the Group LN identity. Home-Estate brand assets are not confirmed
  in the repo.
- **Voice:** not formally defined; existing UI copy is Dutch (BE), functional and
  professional. (open)

## Evidence on Hand

- A real, mature domain model and workflows in code; `DEVNOTES.md` records recent
  feature work (budget formula engine, per-tariff VAT calculation, index scrapers).
- Brand assets: `CPMCore/wwwroot/Img/groupln-logo.png`,
  `CPMCore/wwwroot/Img/Logo-bco.png`.
- No testimonials, customer names, benchmarks, pricing, licensing or deployment
  claims exist for this product — it is an internal tool with no public marketing
  proof. Future work must not fabricate any.
- It is deployed at `cpm.groupln.be` (per config); infrastructure details are not
  product claims.

## Product Principles

1. **Multi-entity is the default** — every list, permission and document is scoped
   to an issuer company; never assume a single company.
2. **One linked system of record** — projects, buyers, subcontractors, budgets,
   invoices and documents stay connected, not siloed.
3. **Belgian construction domain is correctness, not chrome** — VAT on the taxable
   base, Peppol, construction indices, weerverlet and EPC QR must be exact.
4. **One screen set, several roles** — PM, sales admin and accounting share the
   app; prefer permission-gated density over splitting into separate tools.
5. **Field-usable** — the same screens must work on a tablet or phone on-site, not
   only on an office desktop.

## Accessibility & Inclusion

No formal standard has been established. Dutch (BE) language and nl-BE formatting
are required. On-site use implies touch-sized targets and contrast that holds up
outdoors; treat these as practical constraints rather than a mandated standard.
