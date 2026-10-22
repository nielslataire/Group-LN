# Contacten gl-v2 (punt 44) - voortgang
Gebouwd 22/10/2026: lijst (44a), detail met opvolging (44b), toevoegen (44c), lijsten en export (44d). Help: 4 artikelen in HelpContent/projecten (contacten-overzicht, contact-opvolgen, contact-toevoegen, contactenlijsten-en-export), met openstaande [VRAAG]-punten.
## Voor gebruik
- Migratie `_migrations/105_CrmContacten.sql` (samen met 100 en 104 als die nog niet gelopen zijn).
- Niets browser-getest; build zonder CS/RZ-fouten.
## Details
Zie DESIGN.md, sectie "Contacten gl-v2".

## Vervolg 22/10/2026 (feedback Niels)
Gebouwd: makelaars als gekoppelde bedrijven (Project bewerken > Partners), toegewezen = medewerker of makelaar van het project (zoekveld), gemeente/postcode zoals bij Klanten, omzetten naar klant met voorinvulling, exportrecht (`Projects.ContactsExport`), nieuwsbriefexport, exportvenster herwerkt, detail: icoonknoppen, link onder contactaanvraag, icoon/witruimte/datumbreedte.
Voor gebruik: migratie `_migrations/113_CrmContactMakelaarsPostcode, daarna 114_CrmContactVerwijderd.sql` (na 105). Rol(len) het recht Contacten exporteren geven bij Instellingen > Rollen. Koppel eerst makelaars bij Project bewerken > Partners.
Niet browser-getest. Open: commissies aan makelaars koppelen (later), partner als mede-eigenaar bij omzetten.
