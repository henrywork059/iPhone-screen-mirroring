#requires -Version 7.0
<#
.SYNOPSIS
Offline loader-only smoke check: launches pinned UxPlay with -h/--help, which exits
during argument parsing before network receiver creation in the reviewed upstream commit.
NOT a pairing, network, streaming, security or clean-machine test.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [Parameter(Mandatory = $true)][string]$PluginDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$exe = (Resolve-Path -LiteralPath $Executable).Path
$plugins = (Resolve-Path -LiteralPath $PluginDirectory).Path
if ([IO.Path]::GetFileName($exe) -ine 'uxplay.exe') { throw 'Only expected UxPlay executable is permitted.' }
$nativeBin = Split-Path -Parent $exe
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$rc = (Resolve-Path (Join-Path $root 'config\uxplayrc')).Path
$psi = [System.Diagnostics.ProcessStartInfo]::new($exe)
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.WorkingDirectory = $nativeBin
$psi.ArgumentList.Add('-rc')
$psi.ArgumentList.Add($rc)
$psi.ArgumentList.Add('-h')
$psi.Environment['PATH'] = $nativeBin + ';' + (Join-Path $env:SystemRoot 'System32') + ';' + $env:SystemRoot
$psi.Environment['GST_PLUGIN_PATH_1_0'] = $plugins
$psi.Environment['GST_PLUGIN_SYSTEM_PATH_1_0'] = ''
$scanner = Join-Path (Split-Path -Parent $plugins) '..\libexec\gstreamer-1.0\gst-plugin-scanner.exe'
if (Test-Path $scanner -PathType Leaf) { $psi.Environment['GST_PLUGIN_SCANNER_1_0'] = [IO.Path]::GetFullPath($scanner) }
$process = [System.Diagnostics.Process]::Start($psi)
if ($null -eq $process) { throw 'Failed to load receiver executable.' }
try {
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    if (-not $process.WaitForExit(10000)) {
        $process.Kill($true)
        throw 'Help-only receiver process did not exit promptly.'
    }
    if ($process.ExitCode -ne 0 -or $stdout -notmatch 'UxPlay' -or $stdout -notmatch 'Usage:') {
        throw ('Help-only receiver load failed. Exit=' + $process.ExitCode + '; stderr=' + $stderr)
    }
    Write-Host 'PASS: relocated native UxPlay executable loaded and exited from -h without network initialization.'
} finally { $process.Dispose() }
