# Prononce for Windows - Release Packaging Script
# Packages Prononce.exe and documentation into a portable ZIP distribution.

param (
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DistDir = Join-Path $ScriptDir "..\dist"
$ReleaseDir = Join-Path $ScriptDir "..\release"
$ZipPath = Join-Path $ReleaseDir "Prononce-Windows-v$Version.zip"

if (-not (Test-Path "$DistDir\Prononce.exe")) {
    Write-Host "Binaries not found. Running build.ps1 first..." -ForegroundColor Yellow
    & "$ScriptDir\build.ps1"
}

if (-not (Test-Path $ReleaseDir)) {
    New-Item -ItemType Directory -Path $ReleaseDir | Out-Null
}

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Write-Host "`nCompressing release package to $ZipPath..." -ForegroundColor Cyan
Compress-Archive -Path "$DistDir\*" -DestinationPath $ZipPath

$zipSize = (Get-Item $ZipPath).Length / 1MB
Write-Host "`n🎉 Distributable Package Created!" -ForegroundColor Green
Write-Host "Archive: $ZipPath" -ForegroundColor White
Write-Host ("Size: {0:N2} MB" -f $zipSize) -ForegroundColor White
