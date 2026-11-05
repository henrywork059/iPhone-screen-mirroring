#requires -Version 7.0
<#
.SYNOPSIS
Fail-closed signature audit for a future Windows release directory.
.DESCRIPTION
Not a signing tool, exception or Smart App Control bypass. Runs only after
publisher signing has been completed using a trusted certificate. Refuses
unsigned or invalid native DLLs as well as unsigned GUI/helper EXEs.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$PackageRoot,
    [string]$ExpectedPublisherSubject = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PackageRoot).Path
$code = @(Get-ChildItem -LiteralPath $root -Recurse -File |
    Where-Object { $_.Extension -in @('.exe', '.dll') } |
    Sort-Object FullName)
if ($code.Count -lt 3) { throw 'Package contains fewer native executables than expected.' }
$bad = [System.Collections.Generic.List[string]]::new()
foreach ($file in $code) {
    $signature = Get-AuthenticodeSignature -LiteralPath $file.FullName
    if ($signature.Status -ne 'Valid' -or $null -eq $signature.SignerCertificate) {
        $bad.Add($file.FullName + ' => ' + $signature.Status)
        continue
    }
    if ($ExpectedPublisherSubject -and
        $signature.SignerCertificate.Subject -ne $ExpectedPublisherSubject) {
        $bad.Add($file.FullName + ' => unapproved publisher')
    }
}
if ($bad.Count) {
    foreach ($entry in $bad) { Write-Error $entry }
    throw "Release blocked: $($bad.Count) executable files are unsigned, untrusted or not approved."
}
Write-Host "All $($code.Count) executable files have valid Windows signatures."
Write-Host 'Note: signatures do not replace malware review, an audit or Smart App Control testing.'
