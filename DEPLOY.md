# Deploy & hosting — waar draait wat en hoe zet je een nieuwe versie online

Laatst bijgewerkt: 2026-09-24. Alles hieronder is geverifieerd op die datum.

## Overzicht

| Onderdeel | Waar | Hoe bereiken | Hoe deployen |
|---|---|---|---|
| **CPMCore** (backoffice, cpm.groupln.be) | SmarterASP.NET | Controlepaneel op smarterasp.net | Visual Studio publish-profiel `FolderProfile` → `C:\BUILDCPM` → uploaden naar SmarterASP |
| **Databases** (CPM + `db_ab5fbb_cpmmarketdata`) | SmarterASP.NET, server `sql6031.site4now.net` | SSMS met de connection string uit appsettings | Schema-wijzigingen CPM-database: handmatig SQL-script in `_migrations/NNN_*.sql`. MarketData-database: EF-migratie in `GroupLN.MarketData.Persistence`, wordt automatisch toegepast bij opstart van de worker (`ApplyMigrationsOnStartup=true`) |
| **MarketData-worker** (de crawler) | **Hetzner Cloud VPS**, IP `2.28.26.231`, Docker-container `groupln-marketdata-worker` | `ssh root@2.28.26.231` (sleutel `~/.ssh/id_ed25519` op deze pc) · beheer via console.hetzner.cloud | `.\deploy-marketdata.ps1` vanuit `E:\TFS` (zie hieronder) |
| WWWCOPRO (publieke site groupln.be) | SmarterASP.NET | idem CPMCore | Visual Studio publish (`Lokaal.pubxml`) |

Belangrijk: bij SmarterASP staat ook nog een oude upload van de worker (versie 28/07/2026, IIS-variant).
**Die doet niets** — Playwright/Chromium werkt niet op gedeelde Windows-hosting. De echte crawler is de
Docker-container op Hetzner. Die upload mag weg.

## MarketData-worker deployen

Je hoeft lokaal **niet** te builden of te publishen. Het script stuurt de broncode naar de server en de
Dockerfile daar doet zelf `dotnet restore` + `dotnet publish` tijdens de container-build.

```powershell
cd E:\TFS
.\deploy-marketdata.ps1
```

Wat het script doet:

1. Wacht tot de worker geen crawl uitvoert (kijkt in de containerlogs naar "Bronnencheck klaar"). Max 120 min, daarna stopt het met een melding.
2. Pakt de vier MarketData-projecten + `docker-compose.yml` + `.dockerignore` in een tar, zonder `bin/` en `obj/`.
3. Kopieert de tar via scp naar `/opt/marketdata` op de server.
4. Pakt uit en draait `docker compose up -d --build` (rebuild + herstart).
5. Toont `docker compose ps`, een ping op `/api/trigger/ping` en de laatste logregels.

Opties:

| Optie | Effect |
|---|---|
| `-LogsOnly` | Niets deployen, enkel containerstatus + laatste 80 logregels tonen |
| `-NoWait` | Niet wachten tot de worker idle is. Een lopende crawl wordt afgebroken (veilig: de run wordt als mislukt gemarkeerd en er wordt niets als inactief gezet) |
| `-MaxWaitMinutes 180` | Langer wachten dan de standaard 120 min |

Inplannen, bv. eenmalig vannacht om 03:00 (het script wacht zelf tot de worker idle is):

```powershell
schtasks /Create /TN "MarketData deploy" /SC ONCE /ST 03:00 /TR "powershell -ExecutionPolicy Bypass -File E:\TFS\deploy-marketdata.ps1"
```

Voorwaarden:

- Alle wijzigingen zijn **opgeslagen op schijf** (het script neemt de bestanden, niet de git-commit).
- De MarketData-projecten **compileren** lokaal (`dotnet build GroupLN.MarketData.Worker`). Faalt de build op de server, dan blijft de oude container gewoon draaien.
- Het `.env`-bestand met connection string, trigger-key en Anthropic-sleutel staat al op de server in `/opt/marketdata` en wordt **niet** overschreven. Sjabloon: `.env.example`.
- `appsettings.Production.json` wordt **wél** meegestuurd (volume-mount in de container). Wijzigingen aan bronnen, locaties of intervallen doe je dus lokaal in dat bestand en deploy je mee.

Eerste keer op een nieuwe pc: PowerShell staat scripts standaard niet toe. Eenmalig:

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

Dubbelklikken op het .ps1-bestand werkt niet; altijd vanuit een PowerShell-venster starten.

## Controleren of de worker draait

Vanaf deze pc:

```powershell
.\deploy-marketdata.ps1 -LogsOnly
```

Verwacht: `groupln-marketdata-worker ... Up ...` en logregels van de crawl. Onderaan de logs betekent
"Bronnencheck klaar" dat de worker idle is, "Bronnen controleren" zonder "klaar" erna dat er een crawl loopt.

In CPMCore: **Instellingen → MarketData status** toont per bron de laatste geslaagde crawl, de duur, de
laatste fout en de recente runs. Immoweb crawlt elke dag, Zimmo om de drie dagen.

Rechtstreeks op de server:

```bash
ssh root@2.28.26.231
cd /opt/marketdata
docker compose ps
docker compose logs --tail=200 -f      # live meekijken, Ctrl+C om te stoppen
curl -s localhost:8080/api/trigger/ping
docker compose restart                 # enkel herstarten, geen rebuild
```

Poort 8080 is enkel lokaal op de server bereikbaar (bewust), vandaar `localhost` via ssh.

## Zimmo vindt niets? Diagnose zonder database

Zimmo wijzigt af en toe de opbouw van zijn zoekpagina (in de zomer van 2026 van Next.js naar Angular).
Symptoom: Zimmo-runs van ~1 minuut met 0 gevonden. Draai dan de diagnose, die één zoekpagina opent en
rapporteert of het Cloudflare is (server-IP geblokkeerd) of een gewijzigde pagina-opbouw (selectors aanpassen
in `ZimmoCrawler.ExtractDomCardsAsync`):

```powershell
# lokaal (vanaf je eigen IP)
cd E:\TFS\GroupLN.MarketData.Worker\bin\Debug\net8.0
$env:DOTNET_ENVIRONMENT="Development"; dotnet GroupLN.MarketData.Worker.dll --zimmo-search-test --postcode 8000
```

```bash
# op de server (vanaf het Hetzner-IP), zelfde image als de worker
ssh root@2.28.26.231
cd /opt/marketdata && docker compose run --rm marketdata-worker dotnet GroupLN.MarketData.Worker.dll --zimmo-search-test --postcode 8000
```

Uitkomst per locatie: `OK` (kaarten gevonden), `CLOUDFLARE-BLOCKED`, `LEEG` (pagina laadt maar geen kaarten:
opbouw gewijzigd) of `NAVIGATIE-FOUT`. HTML, tekst en screenshot staan in `debug/zimmo-search-test/`.

Afspraak: Zimmo-detailpagina's worden **niet** gecrawld (`OpenProjectDetailPages=false`,
`OpenDetailPagesForLooseListings=false`). Zimmo levert dus enkel wat op de zoekkaart staat: code, adres,
prijs, oppervlakte, slaapkamers en voor projecten het label "Project - 80% beschikbaar".

## Crawl-time-out

`CrawlerSettings.CrawlTimeoutHours` (productie: 8) is de maximale duur van één run per bron. Een run die
afgebroken wordt staat op de statuspagina als **Gedeeltelijk** met de melding "Afgebroken door time-out";
verdwenen listings worden dan niet inactief gezet. Staat Immoweb structureel op Gedeeltelijk: time-out verhogen,
gemeenten verminderen of `Sources.Immoweb.DelayBetweenRequestsSeconds` verlagen.

## Als het misloopt

- **ssh weigert de verbinding** → server bestaat niet meer of staat uit: kijk in console.hetzner.cloud. Sleutel-probleem: de publieke sleutel `~/.ssh/id_ed25519.pub` moet in `/root/.ssh/authorized_keys` op de server staan; via de Hetzner-webconsole kun je altijd binnen.
- **Docker-build faalt op de server** → de foutmelding staat in de output van het script. Meestal een compilefout; lokaal `dotnet build GroupLN.MarketData.Worker` geeft dezelfde fout. De oude container blijft draaien.
- **Container start maar crawlt niet** → `docker compose logs --tail=200`. Controleer `.env` op de server (connection string, trigger-key) en `CrawlerSettings.EnableCrawler` in `appsettings.Production.json`.
- **Playwright/Chromium-fout** → de runtime-image in de Dockerfile (`mcr.microsoft.com/playwright/dotnet:v1.49.0-noble`) moet dezelfde versie hebben als het `Microsoft.Playwright` NuGet-pakket in `GroupLN.MarketData.Infrastructure`. Bij een upgrade van dat pakket ook de tag in de Dockerfile aanpassen.
- **Schijf vol op de server** → oude images opruimen: `docker image prune -f`.

## CPMCore deployen

1. In Visual Studio: rechtsklik CPMCore → Publish → profiel `FolderProfile` (doel `C:\BUILDCPM`).
2. De inhoud van `C:\BUILDCPM` uploaden naar de site bij SmarterASP (controlepaneel of FTP).
3. Schema-wijzigingen: het bijhorende script uit `_migrations/` handmatig uitvoeren op de live database (zie de kop van elk script; ze zijn idempotent en additief).
