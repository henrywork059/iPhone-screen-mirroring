#requires -Version 7.0
# Build-only, deterministic CycloneDX file-level SBOM for staged native code.
# The component hashes are copied from staged SHA256SUMS; the accompanying
# MSYS2 package inventory lists the upstream dependency package versions.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$NativeRoot)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $NativeRoot).Path
$shaPath = Join-Path $root 'SHA256SUMS.txt'
if (-not (Test-Path -LiteralPath $shaPath -PathType Leaf)) {
    throw 'Native SHA256SUMS inventory is missing.'
}
# Read the staged files directly; sha256sum escapes unusual filenames and
# its text format is not suitable as a universal manifest parser on Windows.
$nativePrefix = $root.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$binaryFiles = @(Get-ChildItem -LiteralPath $root -File -Recurse |
    Where-Object { $_.Extension -in @('.dll', '.exe') } | Sort-Object FullName)
$components = @(
    foreach ($file in $binaryFiles) {
        $relative = $file.FullName.Substring($nativePrefix.Length).Replace('\', '/')
        $digest = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        [ordered]@{
            type = 'file'
            name = $relative
            'bom-ref' = "native/$relative"
            hashes = @([ordered]@{ alg = 'SHA-256'; content = $digest })
        }
    }
)
if ($components.Count -lt 3) { throw 'Native executable component list is unexpectedly empty.' }
$sbom = [ordered]@{
    bomFormat = 'CycloneDX'
    specVersion = '1.5'
    version = 1
    metadata = [ordered]@{
        component = [ordered]@{ type = 'application'; name = 'iPhoneMirror-UxPlay-experimental' }
    }
    components = $components
}
$destination = Join-Path $root 'native-components.cdx.json'
$sbom | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $destination -Encoding utf8
$parsed = Get-Content -LiteralPath $destination -Raw | ConvertFrom-Json
if ($parsed.bomFormat -ne 'CycloneDX' -or $parsed.components.Count -ne $components.Count) {
    throw 'Native CycloneDX SBOM validation failed.'
}
Write-Host "Generated CycloneDX native file SBOM: $($components.Count) code components."
