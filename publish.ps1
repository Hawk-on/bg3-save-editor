<#
.SYNOPSIS
    Build & publish BG3 Save Editor as a self-contained Windows executable.
.DESCRIPTION
    1. Builds the Angular frontend (production)
    2. Copies the output into the API's wwwroot
    3. Publishes the .NET API as a single-file self-contained exe
    The result lands in ./publish/ ready to zip or distribute.
#>
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "$PSScriptRoot\publish"
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "=== BG3 Save Editor — Publish ===" -ForegroundColor Cyan

# ── 1. Angular build ───────────────────────────────────────────
Write-Host "`n[1/3] Building Angular frontend..." -ForegroundColor Yellow
Push-Location frontend-ng
npm ci --silent
npx ng build --configuration production
Pop-Location

# Find Angular output (Angular 17+ uses dist/<project>/browser)
$angularOut = "frontend-ng\dist\bg3-editor-ng\browser"
if (-not (Test-Path $angularOut)) {
    $angularOut = "frontend-ng\dist\bg3-editor-ng"
}
if (-not (Test-Path $angularOut)) {
    throw "Angular build output not found! Check frontend-ng/dist/"
}
Write-Host "  Angular output: $angularOut"

# ── 2. Copy into wwwroot ──────────────────────────────────────
Write-Host "`n[2/3] Copying Angular output to API wwwroot..." -ForegroundColor Yellow
$wwwroot = "backend\BG3.SaveEditor.Api\wwwroot"
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $wwwroot | Out-Null
Copy-Item -Path "$angularOut\*" -Destination $wwwroot -Recurse -Force
Write-Host "  Copied $(( Get-ChildItem $wwwroot -Recurse -File ).Count) files"

# ── 3. .NET publish ───────────────────────────────────────────
Write-Host "`n[3/3] Publishing .NET backend (self-contained, single-file)..." -ForegroundColor Yellow
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }

dotnet publish backend\BG3.SaveEditor.Api\BG3.SaveEditor.Api.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputDir

# ── Done ──────────────────────────────────────────────────────
$exe = Get-Item "$OutputDir\BG3SaveEditor.exe" -ErrorAction SilentlyContinue
if ($exe) {
    Write-Host "`n✅ Published successfully!" -ForegroundColor Green
    Write-Host "   $($exe.FullName)  ($([math]::Round($exe.Length / 1MB, 1)) MB)"
    Write-Host "   Run it, then open http://localhost:5000"
} else {
    Write-Error "BG3SaveEditor.exe not found in $OutputDir"
}
