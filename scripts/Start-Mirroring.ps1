#requires -Version 5.1
<#
.SYNOPSIS
Start a pinned-version UxPlay receiver on a trusted Private Windows network.
.DESCRIPTION
No automatic downloads, registry changes, firewall changes, credentials, or logging.
Uses UxPlay -pw (without a password) for a fresh random PIN per connection.
#>
[CmdletBinding()]
param(
    [string]$UxPlayPath = 'C:\msys64\ucrt64\bin\uxplay.exe',
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9 _.()-]{0,39}$')]
    [string]$ReceiverName = 'iPhone Mirror'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Security gate: do not launch a vulnerable or unapproved receiver.
# This should only be lifted after an audited, pinned, independently tested release.
throw 'SECURITY HOLD: UxPlay v1.73.7 has a reported heap buffer overflow (FDH2/UxPlay#614). Receiver launch is disabled. See docs/SECURITY_REVIEW.md.'
if ($env:OS -ne 'Windows_NT') { throw 'This script requires Windows.' }
if (-not (Test-Path -LiteralPath $UxPlayPath -PathType Leaf)) {
    throw "UxPlay executable not found: $UxPlayPath. Follow docs/WINDOWS_SETUP.md."
}
if ([IO.Path]::GetFileName($UxPlayPath) -ine 'uxplay.exe') {
    throw 'The executable must be named uxplay.exe.'
}
$ids = @(Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' } | ForEach-Object { $_.ifIndex })
$profiles = @(Get-NetConnectionProfile)
if ($profiles.Count -eq 0 -or @($profiles | Where-Object { $ids -contains $_.InterfaceIndex }).Count -eq 0) {
    throw 'No connected physical network profile detected. Refusing to start.'
}
if (@($profiles | Where-Object { $_.NetworkCategory -ne 'Private' }).Count -gt 0) {
    throw 'All connected network profiles, including VPN or virtual interfaces, must be Private. Refusing to start on Public or unidentified networks.'
}
$config = Join-Path (Split-Path -Parent $PSScriptRoot) 'config\uxplayrc'
if (-not (Test-Path -LiteralPath $config)) { throw "Missing safety configuration: $config" }
$originalPath = $env:PATH
$bin = Split-Path -Parent (Resolve-Path -LiteralPath $UxPlayPath).Path
$env:PATH = "$bin;$originalPath"
$plugins = [IO.Path]::GetFullPath((Join-Path $bin '..\lib\gstreamer-1.0'))
if (Test-Path -LiteralPath $plugins) { $env:GST_PLUGIN_PATH_1_0 = $plugins }
Write-Host "Starting receiver '$ReceiverName' on Private network only. Pair using iPhone Control Center -> Screen Mirroring." -ForegroundColor Cyan
Write-Host 'UxPlay will display a NEW random PIN for EACH connection. Press Ctrl+C to stop.' -ForegroundColor Cyan
try {
    & $UxPlayPath -rc $config -n $ReceiverName -nh -vs d3d11videosink -as wasapisink -pw
    if ($LASTEXITCODE -ne 0) { throw "UxPlay exited with code $LASTEXITCODE" }
} finally {
    $env:PATH = $originalPath
}
