param(
    [Parameter(Mandatory=$false)]
    [string]$Profile = "Accounting"
)

# Get the script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

Write-Host "Using launch profile: $Profile" -ForegroundColor Cyan

# Load launchSettings.json
$launchSettingsPath = Join-Path $scriptPath "Properties\launchSettings.json"
if (-not (Test-Path $launchSettingsPath)) {
    Write-Host "Error: launchSettings.json not found at $launchSettingsPath" -ForegroundColor Red
    exit 1
}

$launchSettings = Get-Content $launchSettingsPath | ConvertFrom-Json

# Check if profile exists
if (-not $launchSettings.profiles.$Profile) {
    Write-Host "Error: Profile '$Profile' not found in launchSettings.json" -ForegroundColor Red
    Write-Host "Available profiles:" -ForegroundColor Yellow
    $launchSettings.profiles.PSObject.Properties.Name | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    exit 1
}

# Set environment variables from the selected profile
$envVars = $launchSettings.profiles.$Profile.environmentVariables
$envVars.PSObject.Properties | ForEach-Object {
    Set-Item -Path "env:$($_.Name)" -Value $_.Value
}

Write-Host "Environment variables loaded from profile '$Profile'" -ForegroundColor Green

# Find the last migration number
$migrationsPath = Join-Path $scriptPath "Migrations"
$migrationFiles = Get-ChildItem -Path $migrationsPath -Filter "*_M-*.cs" | Where-Object { $_.Name -notlike "*.Designer.cs" }

if ($migrationFiles.Count -eq 0) {
    $nextNumber = 1
} else {
    $lastMigration = $migrationFiles | 
        ForEach-Object { 
            if ($_.Name -match '_M-(\d+)\.cs$') { 
                [PSCustomObject]@{
                    File = $_
                    Number = [int]$Matches[1]
                }
            }
        } | 
        Sort-Object -Property Number -Descending | 
        Select-Object -First 1
    
    $nextNumber = $lastMigration.Number + 1
    Write-Host "Last migration: M-$($lastMigration.Number.ToString('000'))" -ForegroundColor Yellow
}

$migrationName = "M-$($nextNumber.ToString('000'))"
Write-Host "Creating migration: $migrationName" -ForegroundColor Cyan

# Run the migration command - environment variables are already set in current session
Write-Host "Executing: dotnet ef migrations add `"$migrationName`" --context AccountingDbContext --project SoftMax.Accounting.csproj" -ForegroundColor Gray
Write-Host ""

# Use direct command execution which inherits environment from current session
& dotnet ef migrations add $migrationName --context AccountingDbContext --project "SoftMax.Accounting.csproj"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Migration '$migrationName' created successfully!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Failed to create migration" -ForegroundColor Red
    exit $LASTEXITCODE
}
