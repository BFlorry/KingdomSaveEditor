<#
    build-testbuild.ps1

    Produces a self-contained Windows build of Kingdom Save Editor into its own timestamped folder under .\builds\

    Usage:
        Double-click build-testbuild.cmd
        or:  .\build-testbuild.ps1
             .\build-testbuild.ps1 -Version 1.16.2 -Label drivegauge
             .\build-testbuild.ps1 -NoZip
#>

[CmdletBinding()]
param(
    # Numeric version (x.y.z). Defaults to <Version> in KHSave.SaveEditor.csproj
    [string]$Version,

    # Optional tag appended to the informational version and folder name, e.g. "drivegauge". Must not be used in AssemblyVersion/FileVersion
    [string]$Label,

    # Skip creating the .zip
    [switch]$NoZip
)

$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

$project = 'KHSave.SaveEditor\KHSave.SaveEditor.csproj'

function Fail($message) {
    Write-Host ''
    Write-Host "BUILD FAILED: $message" -ForegroundColor Red
    Write-Host ''
    if ($Host.Name -eq 'ConsoleHost') { Read-Host 'Press Enter to close' }
    exit 1
}

# Preflight

if (-not (Test-Path $project)) {
    Fail "Cannot find $project. Run this script from the repository root."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Fail 'The dotnet CLI is not on PATH. Install the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0'
}

$sdks = & dotnet --list-sdks
if (-not ($sdks | Where-Object { $_ -match '^10\.' })) {
    Write-Host 'Installed SDKs:' -ForegroundColor Yellow
    $sdks | ForEach-Object { Write-Host "  $_" }
    Fail 'No .NET 10 SDK found. This repository targets net10.0. Install it with: winget install Microsoft.DotNet.SDK.10'
}

# Make sure Xe.Tools submodule exists
if ((Test-Path '.gitmodules') -and -not (Test-Path 'XeEngine.Tools.Public\Xe.Tools\Xe.Tools.csproj')) {
    Write-Host 'Initialising git submodules...' -ForegroundColor Cyan
    & git submodule update --init --recursive
    if ($LASTEXITCODE -ne 0) { Fail 'git submodule update failed.' }
}

# Version

if (-not $Version) {
    $csproj = Get-Content -Raw -LiteralPath $project
    if ($csproj -match '<Version>([^<]+)</Version>') {
        $Version = $Matches[1].Trim()
    }
    else {
        Fail "Could not read <Version> from $project. Pass -Version instead."
    }
}

# AssemblyVersion and FileVersion reject pre-release suffixes, so the label only ever goes into the informational version
if ($Version -notmatch '^\d+(\.\d+){1,3}$') {
    Fail "-Version must be numeric (e.g. 1.16.1). Use -Label for a suffix like 'drivegauge'."
}

$informational = if ($Label) { "$Version-$Label" } else { $Version }

# Output location

$stamp = Get-Date -Format 'yyyy-MM-dd_HHmmss'
$name = if ($Label) { "$stamp`_$Version-$Label" } else { "$stamp`_$Version" }
$buildRoot = Join-Path $PSScriptRoot 'builds'
$outDir = Join-Path $buildRoot $name

New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Write-Host ''
Write-Host "Kingdom Save Editor test build" -ForegroundColor Green
Write-Host "  version : $informational"
Write-Host "  output  : $outDir"
Write-Host ''

# Publish

$publishArgs = @(
    'publish', $project,
    '-c', 'Release',
    '-r', 'win-x64',
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:DebugType=none',
    '-p:DebugSymbols=false',
    "-p:Version=$informational",
    "-p:FileVersion=$Version",
    "-p:AssemblyVersion=$Version",
    '-o', $outDir
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { Fail "dotnet publish exited with code $LASTEXITCODE." }

# Package

$zipPath = $null
if (-not $NoZip) {
    $zipPath = Join-Path $buildRoot "KingdomSaveEditor-$informational-win-x64-$stamp.zip"
    Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zipPath
}

# Summary

$exe = Join-Path $outDir 'KHSave.SaveEditor.exe'

Write-Host ''
Write-Host 'Build succeeded.' -ForegroundColor Green
if (Test-Path $exe) {
    $sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
    Write-Host "  exe : $exe  ($sizeMb MB)"
}
if ($zipPath) {
    $zipMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host "  zip : $zipPath  ($zipMb MB)"
}
Write-Host ''

if (Test-Path $outDir) { Start-Process explorer.exe $outDir }

if ($Host.Name -eq 'ConsoleHost') { Read-Host 'Press Enter to close' }
