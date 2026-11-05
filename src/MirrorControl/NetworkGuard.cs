using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MirrorControl;

internal static class NetworkGuard
{
    // This is a user-safety preflight, not network isolation or a substitute for firewall rules.
    internal static async Task<(bool Allowed, string Message)> CheckAsync()
    {
        if (!OperatingSystem.IsWindows())
            return (false, "Windows is required.");

        // Inspect active physical Windows adapters, not arbitrary user-configured network names.
        // Fail closed if a profile is not Private or if inspection itself fails.
        const string script = """
            $ErrorActionPreference = 'Stop'
            $ids = @(Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' } | ForEach-Object { $_.ifIndex })
            $profiles = @(Get-NetConnectionProfile | Where-Object { $ids -contains $_.InterfaceIndex })
            if ($profiles.Count -eq 0) {
                'BLOCK:No connected physical network profile was detected.'
            } elseif (@($profiles | Where-Object { $_.NetworkCategory -ne 'Private' }).Count -gt 0) {
                'BLOCK:All connected physical network profiles must be Private. Do not use this on public Wi-Fi.'
            } else {
                'ALLOW:Private network profile detected.'
            }
            """;

        string executable = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");

        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string arg in new[] { "-NoProfile", "-NonInteractive", "-Command", script })
            info.ArgumentList.Add(arg);

        try
        {
            using var process = new Process { StartInfo = info };
            if (!process.Start())
                return (false, "Could not start the network preflight.");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                return (false, "Network preflight timed out.");
            }
            string answer = (await stdout).Trim();
            string detail = (await stderr).Trim();
            if (process.ExitCode != 0)
                return (false, "Network inspection failed: " + detail);
            if (answer.StartsWith("ALLOW:", StringComparison.Ordinal))
                return (true, answer["ALLOW:".Length..]);
            if (answer.StartsWith("BLOCK:", StringComparison.Ordinal))
                return (false, answer["BLOCK:".Length..]);
            return (false, "Network inspection did not return a recognized result.");
        }
        catch (Exception ex)
        {
            return (false, "Network inspection failed: " + ex.Message);
        }
    }
}
