# ─────────────────────────────────────────────────────────────────────────────
# Deploy van de MarketData-worker naar de Hetzner-server (Docker).
#
# Gebruik (PowerShell, vanuit E:\TFS):
#   .\deploy-marketdata.ps1            # wacht tot de worker idle is, dan upload + rebuild + herstart
#   .\deploy-marketdata.ps1 -LogsOnly  # enkel de laatste logregels van de container tonen
#   .\deploy-marketdata.ps1 -NoWait    # niet wachten tot de worker idle is (lopende crawl wordt afgebroken)
#   .\deploy-marketdata.ps1 -MaxWaitMinutes 180   # langer wachten dan de standaard 120 min
#
# Inplannen (Windows Taakplanner), bv. eenmalig vannacht om 03:00:
#   schtasks /Create /TN "MarketData deploy" /SC ONCE /ST 03:00 /TR "powershell -ExecutionPolicy Bypass -File E:\TFS\deploy-marketdata.ps1"
# Het script wacht dan zelf tot er geen crawl bezig is en deployt daarna.
#
# Wat het doet:
#   1. Pakt de 4 MarketData-projecten + docker-compose.yml + .dockerignore in een tar
#      (zonder bin/ en obj/, die zijn niet nodig en maken de upload traag).
#   2. Kopieert de tar via scp naar /opt/marketdata op de server.
#   3. Pakt uit en draait "docker compose up -d --build" (rebuild + herstart).
#   4. Toont de laatste logregels + een ping op het trigger-endpoint.
#
# Het .env-bestand met wachtwoorden staat al op de server en wordt NIET aangeraakt.
# appsettings.Production.json wordt wél meegestuurd (die wordt als volume in de
# container gemount), dus lokale wijzigingen daaraan gaan mee.
# ─────────────────────────────────────────────────────────────────────────────
param(
    [string] $ServerHost = "root@2.28.26.231",
    [string] $RemoteDir  = "/opt/marketdata",
    [switch] $LogsOnly,
    [switch] $NoWait,
    [int]    $MaxWaitMinutes = 120
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

if ($LogsOnly) {
    ssh $ServerHost "cd $RemoteDir && docker compose ps && docker compose logs --tail=80"
    exit $LASTEXITCODE
}

# ── Wachten tot de worker idle is ──────────────────────────────────────────
# De worker logt "Bronnen controleren" bij de start van een ronde en
# "Bronnencheck klaar" op het einde. Is de laatste van die twee "klaar" (of is er
# nog geen ronde gelogd), dan is er geen crawl bezig en is herstarten veilig.
function Get-WorkerIdle {
    $last = ssh $ServerHost "cd $RemoteDir && docker compose logs --tail=5000 2>/dev/null | grep -E 'Bronnen controleren|Bronnencheck klaar' | tail -1"
    if ($LASTEXITCODE -ne 0) { throw "Kon de logs van de server niet lezen." }
    return ([string]::IsNullOrWhiteSpace($last) -or $last -match "Bronnencheck klaar")
}

if (-not $NoWait) {
    Write-Host "0/4  Wachten tot de worker geen crawl meer uitvoert (max $MaxWaitMinutes min)..." -ForegroundColor Cyan
    $deadline = (Get-Date).AddMinutes($MaxWaitMinutes)
    while (-not (Get-WorkerIdle)) {
        if ((Get-Date) -gt $deadline) {
            throw "Worker is na $MaxWaitMinutes min nog steeds bezig. Probeer later, of gebruik -NoWait om de crawl af te breken."
        }
        Write-Host ("     crawl bezig, opnieuw proberen om {0:HH:mm}" -f (Get-Date).AddMinutes(2))
        Start-Sleep -Seconds 120
    }
    Write-Host "     worker is idle." -ForegroundColor Green
}

$archive = Join-Path $env:TEMP "marketdata-deploy.tgz"
if (Test-Path $archive) { Remove-Item $archive -Force }

Write-Host "1/4  Inpakken (zonder bin/obj)..." -ForegroundColor Cyan
& tar -czf $archive `
    --exclude="*/bin" --exclude="*/obj" --exclude="*/debug" `
    GroupLN.MarketData.Worker `
    GroupLN.MarketData.Core `
    GroupLN.MarketData.Infrastructure `
    GroupLN.MarketData.Persistence `
    docker-compose.yml `
    .dockerignore
if ($LASTEXITCODE -ne 0) { throw "tar mislukt." }
Write-Host ("     {0:N1} MB" -f ((Get-Item $archive).Length / 1MB))

Write-Host "2/4  Uploaden naar $ServerHost`:$RemoteDir ..." -ForegroundColor Cyan
& scp $archive "$ServerHost`:$RemoteDir/marketdata-deploy.tgz"
if ($LASTEXITCODE -ne 0) { throw "scp mislukt." }

Write-Host "3/4  Uitpakken + container herbouwen..." -ForegroundColor Cyan
& ssh $ServerHost "cd $RemoteDir && tar -xzf marketdata-deploy.tgz && rm marketdata-deploy.tgz && docker compose up -d --build"
if ($LASTEXITCODE -ne 0) { throw "Remote build/start mislukt. Bekijk de output hierboven." }

Write-Host "4/4  Status..." -ForegroundColor Cyan
& ssh $ServerHost "cd $RemoteDir && docker compose ps && sleep 5 && curl -s localhost:8080/api/trigger/ping; echo; docker compose logs --tail=40"

Remove-Item $archive -Force -ErrorAction SilentlyContinue
Write-Host "Klaar. Controleer daarna in CPMCore: Instellingen > MarketData status." -ForegroundColor Green
