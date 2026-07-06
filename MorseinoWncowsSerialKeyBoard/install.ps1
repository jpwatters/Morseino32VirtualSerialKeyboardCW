<#
.SYNOPSIS
    Sets up and builds Morserino32 Serial Keyboard for Windows.

.DESCRIPTION
    1. Checks whether the .NET 8 SDK is installed; installs it via winget
       if it's missing and winget is available.
    2. Runs `dotnet restore`, which is what actually downloads the
       System.IO.Ports NuGet package (and anything else the project needs)
       from nuget.org.
    3. Builds the project (Release configuration).
    4. Optionally publishes a standalone win-x64 .exe with -Publish.

    Run this from inside the "Windows" folder you were given -- it expects
    MorserinoWinKeyboard\MorserinoWinKeyboard.csproj to sit next to it.

.PARAMETER Publish
    Also produce a self-contained, single-file win-x64 .exe under
    MorserinoWinKeyboard\bin\Release\net8.0-windows\win-x64\publish\.

.PARAMETER SkipSdkCheck
    Skip the .NET SDK presence/installation check (use this if you already
    know the SDK is installed and just want to restore/build).

.EXAMPLE
    .\install.ps1

.EXAMPLE
    .\install.ps1 -Publish
#>

[CmdletBinding()]
param(
    [switch]$Publish,
    [switch]$SkipSdkCheck
)

$ErrorActionPreference = "Stop"

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir "MorserinoWinKeyboard"
$csproj     = Join-Path $projectDir "MorserinoWinKeyboard.csproj"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Test-DotNet8Sdk {
    $dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetCmd) {
        return $false
    }
    $sdkList = & dotnet --list-sdks 2>$null
    if (-not $sdkList) {
        return $false
    }
    return ($sdkList | Where-Object { $_ -match '^8\.' }) -ne $null
}

if (-not (Test-Path $csproj)) {
    Write-Error "Can't find $csproj. Run install.ps1 from inside the 'Windows' folder without moving it out of place (it expects a sibling MorserinoWinKeyboard\ folder)."
    exit 1
}

if (-not $SkipSdkCheck) {
    Write-Step ".NET 8 SDK"
    if (Test-DotNet8Sdk) {
        Write-Host "Found." -ForegroundColor Green
    }
    else {
        Write-Host "Not found." -ForegroundColor Yellow
        $winget = Get-Command winget -ErrorAction SilentlyContinue
        if ($winget) {
            Write-Step "Installing .NET 8 SDK via winget (this can take a few minutes)"
            winget install --id Microsoft.DotNet.SDK.8 --source winget --accept-package-agreements --accept-source-agreements
            if ($LASTEXITCODE -ne 0) {
                Write-Error "winget install failed. Install the .NET 8 SDK manually from https://dotnet.microsoft.com/download/dotnet/8.0 and re-run this script."
                exit 1
            }
        }
        else {
            Write-Host ""
            Write-Host "winget isn't available on this machine." -ForegroundColor Yellow
            Write-Host "Install the .NET 8 SDK manually from:" -ForegroundColor Yellow
            Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
            Write-Host "then re-run this script." -ForegroundColor Yellow
            exit 1
        }

        # winget installs can need a fresh shell before PATH picks up dotnet.
        if (-not (Test-DotNet8Sdk)) {
            Write-Error "The .NET 8 SDK still isn't visible to this shell after installing. Close this window, open a new PowerShell window, and re-run install.ps1."
            exit 1
        }
    }
}

Write-Step "Restoring NuGet packages (this downloads System.IO.Ports from nuget.org)"
& dotnet restore "$csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet restore failed. Check your internet connection, and that nuget.org isn't blocked by a firewall or proxy."
    exit 1
}

Write-Step "Building (Release)"
& dotnet build "$csproj" -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed -- see the errors above."
    exit 1
}

if ($Publish) {
    Write-Step "Publishing a standalone win-x64 executable"
    & dotnet publish "$csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Publish failed -- see the errors above."
        exit 1
    }
    $publishDir = Join-Path $projectDir "bin\Release\net8.0-windows\win-x64\publish"
    Write-Host ""
    Write-Host "Standalone .exe:" -ForegroundColor Green
    Write-Host "  $publishDir\MorserinoWinKeyboard.exe" -ForegroundColor Green
}

Write-Step "Done"
Write-Host "Run it with:" -ForegroundColor Green
Write-Host "  dotnet run --project `"$csproj`" -c Release" -ForegroundColor Green
Write-Host "or launch the build directly:" -ForegroundColor Green
Write-Host "  $projectDir\bin\Release\net8.0-windows\MorserinoWinKeyboard.exe" -ForegroundColor Green
