using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace MirrorControl;

/// <summary>
/// Owns one opt-in, pinned BleHid v0.6.0 CLI process. Bluetooth is never started
/// automatically. No external paths, extra hardware or installation is required.
/// </summary>
internal sealed class BleHidCliProcess : IDisposable
{
    private Process? _process;
    private readonly object _writeLock = new();
    private bool _ready;

    internal event Action<string>? Output;
    internal event Action? Exited;
    internal bool IsReady => IsRunning && _ready;

    internal bool IsRunning
    {
        get
        {
            try { return _process is { HasExited: false }; }
            catch (InvalidOperationException) { return false; }
        }
    }

    internal void Start()
    {
        if (!BackendSecurityGate.IsExperimental)
            throw new InvalidOperationException("Bluetooth control is available in the explicit experimental build only.");
        if (IsRunning)
            throw new InvalidOperationException("Bluetooth service already running.");

        string exe = Path.Combine(AppContext.BaseDirectory, "bluetooth", "BleHid.Cli.exe");
        if (!File.Exists(exe))
            throw new FileNotFoundException("The pinned Bluetooth HID v0.6.0 companion was not bundled.", exe);

#if EXPERIMENTAL_RECEIVER
        using (var stream = File.OpenRead(exe))
        {
            string sha256 = Convert.ToHexString(SHA256.HashData(stream));
            if (!string.Equals(sha256, ExperimentalBuildIdentity.BleHidSha256,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Bluetooth companion checksum mismatch; refusing to run it.");
        }
#else
        throw new InvalidOperationException("Bluetooth companion is not enabled in the normal build.");
#endif

        var info = new ProcessStartInfo(exe)
        {
            WorkingDirectory = Path.GetDirectoryName(exe)!,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // The optional third-party process is started only on an explicit click.
        // It uses the encrypted HID GATT protection level (never --plain).
        var p = new Process { StartInfo = info, EnableRaisingEvents = true };
        p.OutputDataReceived += (_, e) => OnLine(e.Data);
        p.ErrorDataReceived += (_, e) => OnLine(e.Data);
        p.Exited += (_, _) => Exited?.Invoke();
        if (!p.Start())
        {
            p.Dispose();
            throw new InvalidOperationException("Could not start the Bluetooth companion.");
        }
        _process = p;
        _ready = false;
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
    }

    private void OnLine(string? line)
    {
        if (string.IsNullOrEmpty(line)) return;
        Output?.Invoke(line);
        if (line.Contains("Advertisement status: Started", StringComparison.OrdinalIgnoreCase))
        {
            // BleHid defaults to broadcasting to every paired host. Switch to
            // local-only immediately, before the user may send any input.
            _ready = true;
            TrySend("host local");
            Output?.Invoke("[controller] Bluetooth advertising started. Input stays LOCAL until you explicitly select the iPhone.");
        }
    }

    internal bool TrySend(string command)
    {
        if (!BleHidCommands.IsSafe(command) || !IsRunning) return false;
        lock (_writeLock)
        {
            try
            {
                _process!.StandardInput.WriteLine(command);
                _process.StandardInput.Flush();
                return true;
            }
            catch (IOException) { return false; }
            catch (InvalidOperationException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
    }

    internal void Stop()
    {
        var p = _process;
        if (p is null) return;
        try
        {
            if (!p.HasExited)
            {
                // A running 'capture' command blocks the CLI command reader.
                // Ask for graceful shutdown first, then terminate only this child.
                TrySend("host local");
                TrySend("quit");
                if (!p.WaitForExit(1200))
                {
                    p.Kill(entireProcessTree: true);
                    p.WaitForExit(1500);
                }
            }
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        finally
        {
            _ready = false;
            _process = null;
            p.Dispose();
        }
    }

    public void Dispose() => Stop();
}
