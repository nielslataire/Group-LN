<#
.SYNOPSIS
    Maakt voor bestaande project-foto's de WebP-varianten op breedte aan (pictures/w320 ... w1280) en zet ProjectPictures.HasVariants = 1.

.DESCRIPTION
    Foto's die vóór de nieuwe upload (15/10/2026) zijn opgeladen hebben enkel de oude varianten (pictures, 800, 447). Dit script
    laat de Storage API per foto (.webp, .jpg of .png) de ontbrekende breedtes afleiden van het bestand in "pictures" (zelfde bestandsnaam en formaat als de bron) (POST /api/assets/photo/{naam}/variants)
    en bewaart daarna in de database dat de foto varianten heeft, zodat de publieke site srcset kan gebruiken.

    Let op:
      - De varianten worden van het bestaande bestand in "pictures" gemaakt (meestal max 1280 px breed): er komt dus niets
        groter dan 1280 bij, en er is geen master-jpeg (het oorspronkelijke bestand is nooit bewaard).
      - WidthPx/HeightPx van de foto worden overschreven met de afmetingen van dat bronbestand, want de site gebruikt ze om
        enkel breedtes aan te bieden die echt bestaan.
      - Veilig om opnieuw te draaien: bestaande varianten blijven staan, foto's met HasVariants = 1 worden overgeslagen.

    Vereist de migratie 087_ProjectPicturesVarianten.sql en de nieuwe versie van de Storage API.

.PARAMETER ConnectionString
    SQL Server-verbindingsstring (dezelfde als "testdbSql" in WWWCOPRO / de CPMCore-verbinding).
.PARAMETER StorageBaseUrl
    Basis-URL van de Storage API, bv. https://cpm.groupln.be/assets
.PARAMETER WriteApiKey
    StorageApi:WriteApiKey.
.PARAMETER ProjectId
    Optioneel: enkel de foto's van dit project.
.PARAMETER Limit
    Optioneel: maximaal aantal foto's in deze run (handig om eerst een proef te doen).
.PARAMETER DryRun
    Toont wat er zou gebeuren, zonder iets aan te roepen of te wijzigen.

.EXAMPLE
    .\Backfill-FotoVarianten.ps1 -ConnectionString "Server=...;Database=...;User Id=...;Password=..." `
        -StorageBaseUrl https://cpm.groupln.be/assets -WriteApiKey "lnwrt_..." -Limit 5
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ConnectionString,
    [Parameter(Mandatory = $true)][string]$StorageBaseUrl,
    [Parameter(Mandatory = $true)][string]$WriteApiKey,
    [int]$ProjectId = 0,
    [int]$Limit = 0,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$base = $StorageBaseUrl.TrimEnd('/')

$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.Open()
try {
    $hasColumn = New-Object System.Data.SqlClient.SqlCommand "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProjectPictures') AND name = 'HasVariants'", $conn
    if ([int]$hasColumn.ExecuteScalar() -eq 0) { throw "Kolom HasVariants ontbreekt: voer eerst _migrations/087_ProjectPicturesVarianten.sql uit." }

    $sql = "SELECT " + $(if ($Limit -gt 0) { "TOP ($Limit) " } else { "" }) + "Id, ProjectId, Name FROM dbo.ProjectPictures WHERE MediaType = 0 AND HasVariants = 0 AND Name IS NOT NULL AND (Name LIKE '%.webp' OR Name LIKE '%.jpg' OR Name LIKE '%.jpeg' OR Name LIKE '%.png')"
    if ($ProjectId -gt 0) { $sql += " AND ProjectId = $ProjectId" }
    $sql += " ORDER BY Id"
    $cmd = New-Object System.Data.SqlClient.SqlCommand $sql, $conn
    $table = New-Object System.Data.DataTable
    (New-Object System.Data.SqlClient.SqlDataAdapter $cmd).Fill($table) | Out-Null

    Write-Host "$($table.Rows.Count) foto('s) zonder varianten gevonden." -ForegroundColor Cyan
    $ok = 0; $skipped = 0; $failed = 0

    foreach ($row in $table.Rows) {
        $id = [int]$row.Id; $name = [string]$row.Name
        if ($DryRun) { Write-Host "[dry-run] foto $id ($name)"; continue }
        try {
            $url = "$base/api/assets/photo/$([Uri]::EscapeDataString($name))/variants"
            $res = Invoke-RestMethod -Method Post -Uri $url -Headers @{ 'X-Api-Key' = $WriteApiKey } -TimeoutSec 120
            $widths = @($res.widths)
            if ($widths.Count -eq 0) {
                Write-Host "foto $id ($name): bron te klein voor varianten (breedte $($res.width)), overgeslagen." -ForegroundColor Yellow
                $skipped++; continue
            }
            $upd = New-Object System.Data.SqlClient.SqlCommand "UPDATE dbo.ProjectPictures SET HasVariants = 1, WidthPx = @w, HeightPx = @h WHERE Id = @id", $conn
            $upd.Parameters.AddWithValue('@w', [int]$res.width) | Out-Null
            $upd.Parameters.AddWithValue('@h', [int]$res.height) | Out-Null
            $upd.Parameters.AddWithValue('@id', $id) | Out-Null
            $upd.ExecuteNonQuery() | Out-Null
            Write-Host "foto $id ($name): $($res.width)x$($res.height), breedtes $($widths -join ', ')" -ForegroundColor Green
            $ok++
        }
        catch {
            $status = $null
            if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
            if ($status -eq 404) { Write-Host "foto $id ($name): bronbestand ontbreekt in de opslag, overgeslagen." -ForegroundColor Yellow; $skipped++ }
            else { Write-Host "foto $id ($name): FOUT - $($_.Exception.Message)" -ForegroundColor Red; $failed++ }
        }
    }
    Write-Host "Klaar: $ok bijgewerkt, $skipped overgeslagen, $failed mislukt." -ForegroundColor Cyan
}
finally { $conn.Close() }
