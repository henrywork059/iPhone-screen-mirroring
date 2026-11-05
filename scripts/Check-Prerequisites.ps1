#requires -Version 5.1
[CmdletBinding()]
param([string]$UxPlayPath = 'C:\msys64\ucrt64\bin\uxplay.exe')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host 'iPhone Mirror Windows prerequisites' -ForegroundColor Cyan
if ($env:OS -ne 'Windows_NT') { throw 'Windows is required.' }
Write-Host '[OK] Windows detected'
if (Test-Path -LiteralPath $UxPlayPath -PathType Leaf) {
    Write-Host "[OK] UxPlay located at $UxPlayPath"
} else {
    Write-Warning "UxPlay missing: $UxPlayPath (see docs/WINDOWS_SETUP.md)"
}
$ids = @(Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' } | ForEach-Object { $_.ifIndex })
$p = @(Get-NetConnectionProfile | Where-Object { $ids -contains $_.InterfaceIndex })
if ($p.Count -eq 0) {
    Write-Warning 'No connected physical network profiles found.'
} else {
    $p | Format-Table -Property Name,NetworkCategory,InterfaceAlias -AutoSize
    if (@($p | Where-Object { $_.NetworkCategory -ne 'Private' }).Count -gt 0) {
        Write-Warning 'Receiver will refuse to start until all connected physical networks are Private.'
    } else {
        Write-Host '[OK] Physical network profiles are Private'
    }
}
Write-Host 'The receiver does not automatically configure Windows Firewall.'
