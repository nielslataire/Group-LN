# GroupLN.MarketData — Setup & Testgids

Vastgoedmarktdata-verzamelaar voor Belgische vastgoedwebsites.
Aparte database `CPM_MarketData` — schrijft NOOIT naar de CPMCore-database.

---

## Inhoud

- [Vereisten](#vereisten)
- [Stap 1: Database aanmaken](#stap-1-database-aanmaken)
- [Stap 2: Playwright installeren](#stap-2-playwright-installeren)
- [Stap 3: Veilige testrun uitvoeren](#stap-3-veilige-testrun-uitvoeren)
- [Stap 4: Opgeslagen data controleren](#stap-4-opgeslagen-data-controleren)
- [Stap 5: Bronnen activeren of deactiveren](#stap-5-bronnen-activeren-of-deactiveren)
- [Configuratie-overzicht](#configuratie-overzicht)
- [Veiligheidschecklist](#veiligheidschecklist)

---

## Vereisten

- .NET 8 SDK
- SQL Server (local of remote) — standaard `Server=.`
- PowerShell (voor Playwright-setup)
- Toegang tot internet vanuit de machine die de worker draait

---

## Stap 1: Database aanmaken

### Optie A — Automatisch via EF migraties (aanbevolen voor development)

Zorg dat `ApplyMigrationsOnStartup = true` staat in `appsettings.Development.json` (staat al zo).

```powershell
# In de solution-root
dotnet restore
dotnet run --project GroupLN.MarketData.Worker --environment Development
```

De worker detecteert automatisch openstaande migraties en past ze toe bij opstart.
De database `CPM_MarketData_Dev` wordt aangemaakt als ze nog niet bestaat.

### Optie B — Manueel via EF CLI (aanbevolen voor productie)

```powershell
# Installeer EF CLI tools (eenmalig)
dotnet tool install --global dotnet-ef

# Bekijk openstaande migraties
dotnet ef migrations list `
  --project GroupLN.MarketData.Persistence `
  --startup-project GroupLN.MarketData.Worker

# Pas migraties toe
dotnet ef database update `
  --project GroupLN.MarketData.Persistence `
  --startup-project GroupLN.MarketData.Worker

# Of met expliciete connection string
dotnet ef database update `
  --project GroupLN.MarketData.Persistence `
  --startup-project GroupLN.MarketData.Worker `
  -- --ConnectionStrings:MarketData "Server=.;Database=CPM_MarketData;Trusted_Connection=True;TrustServerCertificate=True"
```

### Controleer of de tabellen aangemaakt zijn (SQL Server)

```sql
USE CPM_MarketData_Dev;

SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
-- Verwacht: CrawlerSource, CrawlerRun, MarketProperty, MarketPropertySnapshot, MarketPropertyPriceHistory

SELECT Id, Name, IsActive, CrawlFrequencyHours FROM CrawlerSource;
-- Verwacht: Immoweb (IsActive=1), alle anderen (IsActive=0)
```

---

## Stap 2: Playwright installeren

Playwright vereist aparte browser-binaries die na het bouwen geïnstalleerd worden.

```powershell
# Bouw het project eerst
dotnet build GroupLN.MarketData.Worker

# Installeer Chromium (eenmalig per machine)
$env:PLAYWRIGHT_BROWSERS_PATH = "$env:USERPROFILE\.playwright"
pwsh GroupLN.MarketData.Worker\bin\Debug\net8.0\playwright.ps1 install chromium

# Controleer installatie
pwsh GroupLN.MarketData.Worker\bin\Debug\net8.0\playwright.ps1 install-deps chromium
```

> **Opmerking**: Als `playwright.ps1` niet gevonden wordt, zoek dan in `bin\Debug\net8.0\` of bouw eerst met `dotnet build`.

---

## Stap 3: Veilige testrun uitvoeren

### Configuratie voor veilige test (development)

`appsettings.Development.json` heeft al veilige standaarden:

```json
{
  "CrawlerSettings": {
    "EnableCrawler": false,    ← worker draait maar crawlt NOOIT
    "DryRun": true,            ← schrijft niets naar database
    "MaxListingsPerRun": 5,    ← stopt na 5 listings
    "AllowedPostalCodes": ["8730", "8000", "8020", "8310"],
    "AllowedCities": ["Beernem", "Brugge", "Oostkamp", "Knokke-Heist"]
  }
}
```

### Testprocedure (gelaagd)

#### Fase A: Database-only test (geen crawling)

```powershell
dotnet run --project GroupLN.MarketData.Worker --environment Development
```

Verwachte output:
```
ApplyMigrationsOnStartup = true. Database migraties worden toegepast...
Database is up-to-date. Geen migraties nodig.
EnableCrawler = false. De worker is actief maar zal NOOIT crawlen.
```

De worker start en stopt meteen met crawlen. Goed teken.

#### Fase B: DryRun test (crawl zonder opslaan)

Pas `appsettings.Development.json` aan:
```json
"EnableCrawler": true,   ← aanzetten voor test
"DryRun": true,          ← blijft aan! niets wordt opgeslagen
"MaxListingsPerRun": 3   ← slechts 3 listings proberen
```

```powershell
dotnet run --project GroupLN.MarketData.Worker --environment Development
```

Verwachte output:
```
[Immoweb] ══ Crawl gestart ══ DryRun=True | MaxListings=3 | PostcodeFilter=8730,8000,...
[Immoweb] 4 zoekpagina(s) te verwerken.
[Immoweb] ▶ Zoekpagina ophalen: https://www.immoweb.be/en/search/house/for-sale?...
[Immoweb]   → 60 listing-URL's gevonden op deze pagina.
[Immoweb] [DRYRUN] Zou aanmaken/bijwerken → ID=12345678 | Beernem 8730 | €395.000 | House DetachedHouse
[Immoweb] MarkInactive OVERGESLAGEN — DryRun actief.
[Immoweb] ══ Crawl klaar (00:45) ══ Gevonden: 3 | Nieuw: 3 | Bijgewerkt: 0 | Fouten: 0 [DRYRUN - niets opgeslagen]
```

#### Fase C: Echte testrun (kleine scope)

Pas aan:
```json
"EnableCrawler": true,
"DryRun": false,          ← nu worden records wél opgeslagen
"MaxListingsPerRun": 5,
"AllowedPostalCodes": ["8730"]  ← enkel Beernem
```

Controleer na de run:

```sql
SELECT COUNT(*) FROM MarketProperty WHERE SourceId = 1;         -- aantal panden
SELECT COUNT(*) FROM MarketPropertySnapshot;                     -- aantal snapshots
SELECT City, PostalCode, AskingPrice FROM MarketProperty LIMIT 5;
```

---

## Stap 4: Opgeslagen data controleren

### Laatste crawl-runs bekijken

```sql
SELECT
    cs.Name,
    cr.StartedAt,
    cr.FinishedAt,
    DATEDIFF(SECOND, cr.StartedAt, cr.FinishedAt) AS DuurSeconden,
    cr.Status,         -- 0=Running, 1=Completed, 2=Failed, 3=PartialSuccess
    cr.ListingsFound,
    cr.ListingsCreated,
    cr.ListingsUpdated,
    cr.Errors,
    cr.LogMessage
FROM CrawlerRun cr
JOIN CrawlerSource cs ON cr.SourceId = cs.Id
ORDER BY cr.StartedAt DESC;
```

### Panden per gemeente bekijken

```sql
SELECT
    City,
    PostalCode,
    COUNT(*) AS AantalPanden,
    MIN(AskingPrice) AS MinPrijs,
    MAX(AskingPrice) AS MaxPrijs,
    AVG(AskingPrice) AS GemPrijs,
    AVG(LivingArea) AS GemOppervlakte
FROM MarketProperty mp
JOIN (
    SELECT MarketPropertyId, MAX(SnapshotDate) AS LatestSnapshot
    FROM MarketPropertySnapshot
    GROUP BY MarketPropertyId
) ls ON mp.Id = ls.MarketPropertyId
JOIN MarketPropertySnapshot s ON s.MarketPropertyId = mp.Id AND s.SnapshotDate = ls.LatestSnapshot
WHERE mp.IsActive = 1
GROUP BY City, PostalCode
ORDER BY AantalPanden DESC;
```

### Recente prijswijzigingen bekijken

```sql
SELECT
    mp.City,
    mp.PostalCode,
    mp.Url,
    ph.DetectedAt,
    ph.PreviousPrice,
    ph.AskingPrice,
    ph.PriceChangeAmount,
    ph.PriceChangePercentage
FROM MarketPropertyPriceHistory ph
JOIN MarketProperty mp ON ph.MarketPropertyId = mp.Id
WHERE ph.PreviousPrice IS NOT NULL
ORDER BY ph.DetectedAt DESC;
```

### Status van bronnen

```sql
SELECT
    Name,
    IsActive,
    CrawlFrequencyHours,
    LastCrawledAt,
    CASE
        WHEN LastCrawledAt IS NULL THEN 'Nog nooit gecrawld'
        WHEN DATEADD(HOUR, CrawlFrequencyHours, LastCrawledAt) < GETUTCDATE() THEN 'Aan de beurt'
        ELSE 'Recent gecrawld'
    END AS Status
FROM CrawlerSource
ORDER BY IsActive DESC, Name;
```

---

## Stap 5: Bronnen activeren of deactiveren

### Via SQL (directe aanpassing)

```sql
-- Zimmo activeren
UPDATE CrawlerSource SET IsActive = 1, UpdatedAt = GETUTCDATE() WHERE Name = 'Zimmo';

-- Immoweb tijdelijk deactiveren
UPDATE CrawlerSource SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Name = 'Immoweb';

-- Overzicht
SELECT Name, IsActive FROM CrawlerSource;
```

### Via appsettings (globale uitschakelaar)

```json
// In appsettings.json of appsettings.Development.json
{
  "CrawlerSettings": {
    "EnableCrawler": false   ← stopt ALLE bronnen, DB-records blijven intact
  }
}
```

### Nieuwe bron toevoegen (wanneer Zimmo geïmplementeerd is)

1. Implementeer `ZimmoCrawler.cs` (de stub staat klaar)
2. Activeer in de database: `UPDATE CrawlerSource SET IsActive = 1 WHERE Name = 'Zimmo'`
3. De worker pikt de bron automatisch op bij de volgende check

---

## Configuratie-overzicht

| Setting | Dev standaard | Prod standaard | Beschrijving |
|---------|--------------|----------------|--------------|
| `EnableCrawler` | `false` | `true` | Globale schakelaar |
| `DryRun` | `true` | `false` | Loggen zonder opslaan |
| `ApplyMigrationsOnStartup` | `true` | `false` | Auto-migratie |
| `MaxListingsPerRun` | `5` | `0` (onbeperkt) | Limiet per run |
| `MinListingsBeforeMarkInactive` | `20` | `20` | Veiligheidsdrempel |
| `AllowedPostalCodes` | `["8730","8000",...]` | `[]` (alles) | Postcode-filter |
| `AllowedCities` | `["Beernem","Brugge",...]` | `[]` (alles) | Gemeente-filter |
| `DelayBetweenRequestsSeconds` | `6` | `4` | Pauze tussen requests |
| `MarkInactiveAfterDays` | `7` | `30` | Na X dagen = inactief |

---

## Veiligheidschecklist

Gebruik deze checklist vóór elke productie-activatie.

### Database

- [ ] `SELECT * FROM CrawlerSource` — enkel Immoweb heeft `IsActive = 1`
- [ ] `SELECT COUNT(*) FROM MarketProperty` — 0 (lege database)
- [ ] `SELECT COUNT(*) FROM CrawlerRun` — 0 (nog geen runs)
- [ ] Database heet `CPM_MarketData`, NIET `CPMCore` of een andere bestaande database
- [ ] Connection string in appsettings.json verwijst naar de juiste server

### Configuratie (development)

- [ ] `EnableCrawler = false` in `appsettings.Development.json`
- [ ] `DryRun = true` in `appsettings.Development.json`
- [ ] `MaxListingsPerRun = 5` in `appsettings.Development.json`
- [ ] `ApplyMigrationsOnStartup = true` in `appsettings.Development.json`

### Playwright

- [ ] `playwright.ps1 install chromium` succesvol uitgevoerd
- [ ] Test met `dotnet run --environment Development` — geen crash bij browser-init

### Eerste testrun (DryRun)

- [ ] `EnableCrawler = true` aangezet voor de test
- [ ] `DryRun = true` staat aan
- [ ] Logging toont `[DRYRUN] Zou aanmaken/bijwerken` berichten
- [ ] Logging toont `MarkInactive OVERGESLAGEN — DryRun actief`
- [ ] `SELECT COUNT(*) FROM MarketProperty` = 0 na de run (niets opgeslagen)
- [ ] Geen fouten in de log

### Eerste echte run (kleine scope)

- [ ] `DryRun = false` aangezet
- [ ] `MaxListingsPerRun = 5` staat nog aan
- [ ] `AllowedPostalCodes` beperkt tot 1-3 postcodes
- [ ] Na de run: `SELECT COUNT(*) FROM MarketProperty` toont 1-5 records
- [ ] `SELECT * FROM CrawlerRun` toont Status = 1 (Completed)
- [ ] `SELECT * FROM MarketPropertySnapshot` toont bijhorende snapshots

### Productie-activatie

- [ ] Manuele migratie uitgevoerd (`dotnet ef database update`)
- [ ] `ApplyMigrationsOnStartup = false` in `appsettings.json`
- [ ] `DryRun = false` in `appsettings.json`
- [ ] `MaxListingsPerRun = 0` (onbeperkt) in `appsettings.json`
- [ ] `AllowedPostalCodes = []` (geen filter) in `appsettings.json`
- [ ] `EnableCrawler = true` in `appsettings.json`
- [ ] Eerste run gemonitord via `SELECT * FROM CrawlerRun ORDER BY StartedAt DESC`
