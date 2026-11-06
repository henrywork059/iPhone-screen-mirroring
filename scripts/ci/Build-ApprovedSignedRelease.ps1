#requires -Version 7.0
<#
Creates a signed Windows mirror-only release from an already staged UxPlay
native runtime. This script FAILS CLOSED if publisher signing credentials or
human security/device approval are absent. It does not bypass Smart App Control.
Do not run against a non-reviewed native candidate.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$NativeRoot,
    [Parameter(Mandatory=$true)][string]$Version,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9.-]+)?$') {
    throw 'Invalid release version.'
}
if ([string]::IsNullOrWhiteSpace($env:WINDOWS_SIGNING_PFX_BASE64) -or
    [string]::IsNullOrWhiteSpace($env:WINDOWS_SIGNING_PFX_PASSWORD) -or
    [string]::IsNullOrWhiteSpace($env:TRUSTED_PUBLISHER_SUBJECT)) {
    throw 'RELEASE HOLD: trusted code-signing secrets and approved publisher are required.'
}
if ([string]::IsNullOrWhiteSpace($env:GITHUB_SHA) -or
    $env:GITHUB_SHA -ne $env:SECURITY_REVIEWED_SHA -or
    $env:GITHUB_SHA -ne $env:DEVICE_TESTED_SHA) {
    throw 'RELEASE HOLD: independent security review and real-device validation must approve this exact commit.'
}
$native = (Resolve-Path -LiteralPath $NativeRoot).Path.TrimEnd('\','/')
if (-not (Test-Path (Join-Path $native 'bin/uxplay.exe'))) {
    throw 'Pinned UxPlay executable is missing.'
}
if (-not (Test-Path (Join-Path $native 'source-notices/uxplay-patched-corresponding-source.tar.gz'))) {
    throw 'Corresponding GPL receiver source is missing.'
}
$kitRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)) 'Windows Kits/10/bin'
$signTool = Get-ChildItem -Path $kitRoot -Recurse -File -Filter signtool.exe -ErrorAction Stop |
    Where-Object { $_.FullName -match '[\\/]x64[\\/]signtool\.exe$' } |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $signTool) { throw 'Windows SDK SignTool not found; cannot create a signed release.' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$releaseRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path
$desktop = Join-Path $releaseRoot 'desktop'
if (Test-Path $desktop) { throw 'Release output directory must be empty.' }
$pfxPath = Join-Path $env:RUNNER_TEMP 'temporary-windows-release-signing.pfx'
try {
    [IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($env:WINDOWS_SIGNING_PFX_BASE64))
    $binaries = @(Get-ChildItem -LiteralPath $native -Recurse -File |
        Where-Object { $_.Extension -in @('.exe','.dll') } | Sort-Object FullName)
    if ($binaries.Count -lt 3) { throw 'Native binary inventory unexpectedly small.' }
    foreach ($file in $binaries) {
        & $signTool sign /f $pfxPath /p $env:WINDOWS_SIGNING_PFX_PASSWORD /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $file.FullName
        if ($LASTEXITCODE -ne 0) { throw "Authenticode signing failed: $($file.Name)" }
    }
    & pwsh -NoProfile -File scripts/ci/Assert-TrustedWindowsSignatures.ps1 -PackageRoot $native -ExpectedPublisherSubject $env:TRUSTED_PUBLISHER_SUBJECT
    if ($LASTEXITCODE -ne 0) { throw 'Native signatures were not trusted and valid.' }

    # Authenticode changes EXE/DLL bytes. Rebuild the entire native manifest
    # after signing, and bind the signed hashes into the Windows controller.
    $entries = @(Get-ChildItem -LiteralPath $native -Recurse -File |
        Where-Object { $_.Name -notin @('SHA256SUMS.txt','native-components.cdx.json') } |
        Sort-Object FullName | ForEach-Object {
            $relative = $_.FullName.Substring($native.Length+1).Replace('\','/')
            (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash + '  ./' + $relative
        })
    if ($entries.Count -lt $binaries.Count) { throw 'Signed package manifest is incomplete.' }
    $entries | Set-Content -LiteralPath (Join-Path $native 'SHA256SUMS.txt') -Encoding utf8

    $receiverHash = (Get-FileHash (Join-Path $native 'bin/uxplay.exe') -Algorithm SHA256).Hash
    $manifestHash = (Get-FileHash (Join-Path $native 'SHA256SUMS.txt') -Algorithm SHA256).Hash
    $identity = @"
#if !EXPERIMENTAL_RECEIVER
#error Explicit receiver build required
#endif
namespace MirrorControl;
internal static class ExperimentalBuildIdentity
{
    internal const string UxPlaySha256 = "$receiverHash";
    internal const string NativeManifestSha256 = "$manifestHash";
}
"@
    $identity | Set-Content -LiteralPath 'src/MirrorControl/ExperimentalBuildIdentity.g.cs' -Encoding utf8
    dotnet publish src/MirrorControl/MirrorControl.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:EnableExperimentalReceiver=true --output $desktop
    if ($LASTEXITCODE -ne 0) { throw 'Windows controller compilation failed.' }
    Copy-Item -LiteralPath $native -Destination (Join-Path $desktop 'native') -Recurse

    # .NET single-file publish may still emit root-level native DLLs.
    # Sign those too, but never re-sign native/ after its manifest is bound.
    $guiBinaries = @(Get-ChildItem -LiteralPath $desktop -File |
        Where-Object { $_.Extension -in @('.exe','.dll') })
    if (-not (Test-Path (Join-Path $desktop 'MirrorControl.exe'))) {
        throw 'Windows GUI binary was not produced.'
    }
    foreach ($file in $guiBinaries) {
        & $signTool sign /f $pfxPath /p $env:WINDOWS_SIGNING_PFX_PASSWORD /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $file.FullName
        if ($LASTEXITCODE -ne 0) { throw "Controller binary signing failed: $($file.Name)" }
    }
    & pwsh -NoProfile -File scripts/ci/Assert-TrustedWindowsSignatures.ps1 -PackageRoot $desktop -ExpectedPublisherSubject $env:TRUSTED_PUBLISHER_SUBJECT
    if ($LASTEXITCODE -ne 0) { throw 'Signed package contains untrusted executable code.' }

    & pwsh -NoProfile -File scripts/ci/New-NativeSbom.ps1 -NativeRoot (Join-Path $desktop 'native')
    if ($LASTEXITCODE -ne 0) { throw 'Signed native SBOM generation failed.' }
    @(
        'MIRRORING ONLY. This ZIP is provided only after separate security review,',
        'real-device validation and Authenticode verification for this exact revision.',
        'Keep Smart App Control, Defender and Windows Firewall enabled.',
        'Extract the entire directory; do not launch uxplay.exe independently.',
        'Successful signing does not guarantee Smart App Control acceptance.'
    ) | Set-Content -LiteralPath (Join-Path $desktop 'READ-ME-FIRST.txt') -Encoding utf8

    $zip = Join-Path $releaseRoot "iPhoneMirror-win-x64-signed-$Version.zip"
    Compress-Archive -Path (Join-Path $desktop '*') -DestinationPath $zip -CompressionLevel Optimal
    Write-Host ('Signed ZIP SHA-256: ' + (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash)
    Write-Host ('SIGNED_PACKAGE_PATH=' + $zip)
}
finally {
    Remove-Item -LiteralPath $pfxPath -Force -ErrorAction SilentlyContinue
    Remove-Item Env:WINDOWS_SIGNING_PFX_BASE64 -ErrorAction SilentlyContinue
    Remove-Item Env:WINDOWS_SIGNING_PFX_PASSWORD -ErrorAction SilentlyContinue
}
