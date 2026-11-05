#requires -Version 5.1
<#
.SYNOPSIS
Read-only preflight for eventual isolated device testing. DOES NOT start UxPlay.
.DESCRIPTION
Examines all connected Windows network profiles, UxPlay file hash, and program-specific
inbound Windows Firewall rules. It never enables rules or opens ports. A PASS is not a
complete security audit; do not treat it as authorization to lift SECURITY HOLD.
#>
[CmdletBinding()]
param(
    [string]$UxPlayPath = 'C:\msys64\ucrt64\bin\uxplay.exe',
    [ValidatePattern('^[A-Fa-f0-9]{64}$')]
    [string]$ExpectedSha256 = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$blockers = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
if ($env:OS -ne 'Windows_NT') { throw 'Windows 11 is required.' }
Write-Host 'Offline iPhone Mirror test-readiness inspection (no network listener)' -ForegroundColor Cyan

$profiles = @(Get-NetConnectionProfile)
if ($profiles.Count -eq 0) { $blockers.Add('No connected Windows network profiles found.') }
$activePhysical = @(Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' })
if ($activePhysical.Count -eq 0) { $blockers.Add('No active physical network adapter found.') }
$ids = @($activePhysical | ForEach-Object { $_.InterfaceIndex })
if (@($profiles | Where-Object { $ids -contains $_.InterfaceIndex }).Count -eq 0) {
    $blockers.Add('No connected physical network profile found.')
}
if (@($profiles | Where-Object { $_.NetworkCategory -ne 'Private' }).Count -gt 0) {
    $blockers.Add('All connected network profiles must be Private for isolated testing.')
}
$virtual = @(Get-NetAdapter | Where-Object { $_.Status -eq 'Up' -and -not ($ids -contains $_.InterfaceIndex) })
if ($virtual.Count -gt 0) {
    $warnings.Add('Connected nonphysical adapters detected (e.g., VPN or VM switch). Inspect and disconnect them for isolation.')
}
Write-Host ('Connected network profiles: ' + $profiles.Count)
$profiles | Select-Object InterfaceAlias,NetworkCategory,IPv4Connectivity,IPv6Connectivity | Format-Table -AutoSize

if (-not (Test-Path -LiteralPath $UxPlayPath -PathType Leaf)) {
    $blockers.Add('UxPlay executable is not present. No runtime has been approved or distributed.')
} else {
    $resolved = (Resolve-Path -LiteralPath $UxPlayPath).Path
    if ([IO.Path]::GetFileName($resolved) -ine 'uxplay.exe') { $blockers.Add('Selected file must be named uxplay.exe.') }
    $hash = (Get-FileHash -LiteralPath $resolved -Algorithm SHA256).Hash.ToUpperInvariant()
    Write-Host ('Candidate receiver SHA-256: ' + $hash)
    if ([string]::IsNullOrWhiteSpace($ExpectedSha256)) {
        $blockers.Add('Expected approved receiver SHA-256 not supplied. Cannot establish binary identity.')
    } elseif ($hash -ne $ExpectedSha256.ToUpperInvariant()) {
        $blockers.Add('Receiver SHA-256 does not match the supplied expected digest.')
    }
    $signature = Get-AuthenticodeSignature -LiteralPath $resolved
    Write-Host ('Windows code-signature status: ' + $signature.Status)
    if ($signature.Status -ne 'Valid') {
        $warnings.Add('Receiver lacks a Windows-validated code signature; a matching SHA-256 alone is not proof of trust.')
    }
    try {
        $allowed = @(Get-NetFirewallRule -Direction Inbound -Action Allow -Enabled True)
        $matching = @()
        foreach ($rule in $allowed) {
            $filters = @($rule | Get-NetFirewallApplicationFilter)
            foreach ($filter in $filters) {
                if (-not $filter.Program -or $filter.Program -eq 'Any') { continue }
                if ([string]::Equals($filter.Program, $resolved, [StringComparison]::OrdinalIgnoreCase)) {
                    $matching += $rule
                }
            }
        }
        foreach ($rule in $matching) {
            $remote = @(($rule | Get-NetFirewallAddressFilter).RemoteAddress)
            Write-Host ('Program rule: ' + $rule.DisplayName + ' / Profile: ' + $rule.Profile + ' / Remote: ' + ($remote -join ','))
            if ($rule.Profile.ToString() -match 'Public|Any') {
                $blockers.Add('A program-specific firewall rule permits Public/Any profile: ' + $rule.DisplayName)
            }
            if ($remote -contains 'Any') {
                $warnings.Add('An inbound rule for UxPlay permits any remote address: ' + $rule.DisplayName)
            }
        }
        $warnings.Add('Program-specific rule inspection does not detect every broad port/service policy. Verify the whole firewall and router separately.')
    } catch {
        $blockers.Add('Could not inspect firewall policy: ' + $_.Exception.Message)
    }
}

foreach ($item in $warnings) { Write-Warning $item }
if ($blockers.Count) {
    foreach ($item in $blockers) { Write-Host ('BLOCKED: ' + $item) -ForegroundColor Red }
    Write-Host 'Not ready. Receiver SECURITY HOLD remains active.'
    exit 1
}
Write-Host 'Preflight items passed, but no receiver is approved. Security hold stays active.' -ForegroundColor Yellow
exit 0
