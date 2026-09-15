# Prononce for Windows - Automated Build Script
# Compiles a high-performance, self-contained single-file executable.

param (
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $true
)

$ErrorActionPreference = "Stop"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " 🇫🇷 Building Prononce for Windows ($Runtime)" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "..\src\Prononce"
$OutputDir = Join-Path $ScriptDir "..\dist"

if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}

Write-Host "`n[1/3] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $ProjectDir

Write-Host "`n[2/3] Publishing single-file executable ($Configuration)..." -ForegroundColor Yellow
$publishArgs = @(
    "publish", "$ProjectDir\Prononce.csproj",
    "-c", $Configuration,
    "-r", $Runtime,
    "-o", $OutputDir,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true"
)

if ($SelfContained) {
    $publishArgs += "--self-contained", "true"
} else {
    $publishArgs += "--self-contained", "false"
}

& dotnet @publishArgs

Write-Host "`n[3/3] Verifying output..." -ForegroundColor Yellow
$exePath = Join-Path $OutputDir "Prononce.exe"

if (Test-Path $exePath) {
    $fileSize = (Get-Item $exePath).Length / 1MB
    Write-Host "`n✅ Build Successful!" -ForegroundColor Green
    Write-Host "Binary Location: $exePath" -ForegroundColor White
    Write-Host ("Size: {0:N2} MB" -f $fileSize) -ForegroundColor White
} else {
    Write-Error "Build failed: Prononce.exe not found in $OutputDir"
}
