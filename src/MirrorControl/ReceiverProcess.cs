using System;
using System.Diagnostics;
using System.IO;

namespace MirrorControl;

internal sealed class ReceiverProcess : IDisposable
{
    private Process? _process;

    internal event Action<string>? Output;
    internal event Action? Exited;

    internal bool IsRunning
    {
        get
        {
            try { return _process is { HasExited: false }; }
            catch (InvalidOperationException) { return false; }
        }
    }

    internal void Start(string executable, string receiverName)
    {
        if (BackendSecurityGate.IsBlocked)
            throw new InvalidOperationException(BackendSecurityGate.Reason);
        if (IsRunning)
            throw new InvalidOperationException("The receiver is already running.");
        if (!File.Exists(executable) ||
            !string.Equals(Path.GetFileName(executable), "uxplay.exe", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("Select an existing uxplay.exe executable.", executable);

        string config = Path.Combine(AppContext.BaseDirectory, "config", "uxplayrc");
        if (!File.Exists(config))
            throw new FileNotFoundException("The bundled empty UxPlay startup configuration is missing.", config);

        string directory = Path.GetDirectoryName(Path.GetFullPath(executable))!;
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false
        };

        // An explicit, empty -rc file prevents unknown user-level uxplayrc options.
        // -pw with no following password requires a new random PIN on every connection.
        foreach (string arg in new[]
        {
            "-rc", config,
            "-n", receiverName,
            "-nh",
            "-vs", "d3d11videosink",
            "-as", "wasapisink",
            "-pw"
        })
            info.ArgumentList.Add(arg);

        // Do not inherit arbitrary caller PATH entries for native DLL resolution.
        string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        info.Environment["PATH"] = string.Join(Path.PathSeparator, new[]
        {
            directory, Path.Combine(windowsDirectory, "System32"), windowsDirectory
        });
        // MSYS2 UCRT64 GStreamer plugins are stored alongside the UxPlay runtime.
        string plugins = Path.GetFullPath(Path.Combine(directory, "..", "lib", "gstreamer-1.0"));
        if (Directory.Exists(plugins))
        {
            info.Environment["GST_PLUGIN_PATH_1_0"] = plugins;
            info.Environment["GST_PLUGIN_SYSTEM_PATH_1_0"] = string.Empty;
        }
        string scanner = Path.GetFullPath(Path.Combine(directory, "..", "libexec", "gstreamer-1.0",
            "gst-plugin-scanner.exe"));
        if (File.Exists(scanner))
            info.Environment["GST_PLUGIN_SCANNER_1_0"] = scanner;

        var process = new Process { StartInfo = info, EnableRaisingEvents = true };
        process.Exited += (_, _) => Exited?.Invoke();
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("UxPlay did not start.");
        }
        _process = process;
        // A console is required: upstream UxPlay may buffer PIN output when redirected.
        // Keeping a visible console ensures the random PIN is displayed immediately.
        Output?.Invoke("UxPlay started in its own console; read the PIN there.");
    }

    internal void Stop()
    {
        if (_process is null)
            return;
        try
        {
            if (!_process.HasExited)
            {
                // Prototype limitation: a hidden console process has no usable CloseMainWindow.
                // This stops the receiver and child processes, but is not graceful shutdown.
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(3000);
            }
        }
        catch (InvalidOperationException) { }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    public void Dispose() => Stop();
}
