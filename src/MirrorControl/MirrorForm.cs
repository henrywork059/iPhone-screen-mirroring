using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MirrorControl;

public sealed class MirrorForm : Form
{
    private static readonly Color BackgroundColor = Color.FromArgb(77, 78, 97);
    private static readonly Color PanelColor = Color.FromArgb(48, 52, 70);
    private static readonly Color AccentColor = Color.FromArgb(87, 255, 237);
    private static readonly Color SecondaryColor = Color.FromArgb(195, 205, 221);

    private readonly TextBox _executable = new();
    private readonly TextBox _name = new();
    private readonly ComboBox _quality = new();
    private readonly RichTextBox _output = new();
    private readonly Label _status = new();
    private readonly Button _start = new();
    private readonly Button _stop = new();
    private ReceiverProcess? _receiver;
    private bool _starting;

    public MirrorForm()
    {
        Text = "iPhone Mirror | Windows Controller";
        MinimumSize = new Size(620, 650);
        Size = new Size(790, 725);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BackgroundColor;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        FormClosing += (_, _) => _receiver?.Dispose();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1,
            RowCount = 12, BackColor = BackgroundColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "iPhone Mirror", ForeColor = AccentColor,
            Font = new Font("Segoe UI Semibold", 21), Dock = DockStyle.Fill
        }, 0, 0);
        root.Controls.Add(new Label
        {
            Text = BackendSecurityGate.IsExperimental
                ? "EXPERIMENTAL AIRPLAY TEST — not security audited"
                : "SECURITY HOLD — receiver launch disabled; offline checks only",
            ForeColor = SecondaryColor, AutoSize = false, Dock = DockStyle.Fill
        }, 0, 1);
        root.Controls.Add(MakeLabel("Path to uxplay.exe"), 0, 2);
        string bundledReceiver = Path.Combine(AppContext.BaseDirectory, "native", "bin", "uxplay.exe");
        _executable.Text = File.Exists(bundledReceiver) ? bundledReceiver :
            @"C:\msys64\ucrt64\bin\uxplay.exe";
        StyleTextBox(_executable);
        _executable.ReadOnly = BackendSecurityGate.IsExperimental;
        root.Controls.Add(_executable, 0, 3);

        root.Controls.Add(MakeLabel("Receiver name shown on iPhone"), 0, 4);
        _name.Text = "iPhone Mirror";
        StyleTextBox(_name);
        root.Controls.Add(_name, 0, 5);

        root.Controls.Add(MakeLabel("Mirror quality (requested, not guaranteed)"), 0, 6);
        _quality.DropDownStyle = ComboBoxStyle.DropDownList;
        _quality.Dock = DockStyle.Fill;
        _quality.BackColor = PanelColor;
        _quality.ForeColor = Color.White;
        foreach (MirrorQualityPreset preset in MirrorQualityPreset.Options)
            _quality.Items.Add(preset);
        _quality.SelectedIndex = MirrorQualityPreset.DefaultIndex; // Begin with compatible H.264; HEVC remains optional.
        root.Controls.Add(_quality, 0, 7);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _start.Text = "Start receiver";
        _start.BackColor = AccentColor; _start.ForeColor = Color.Black; _start.FlatStyle = FlatStyle.Flat;
        _start.Size = new Size(165, 42);
        _start.Click += async (_, _) => await StartAsync();
        _start.Enabled = !BackendSecurityGate.IsBlocked;
        if (BackendSecurityGate.IsBlocked) _start.Text = "Launch blocked";
        else _start.Text = "Start test receiver";
        _stop.Text = "Stop receiver";
        _stop.BackColor = PanelColor; _stop.ForeColor = Color.White; _stop.FlatStyle = FlatStyle.Flat;
        _stop.Size = new Size(165, 42); _stop.Enabled = false;
        _stop.Click += (_, _) => StopReceiver();
        actions.Controls.Add(_start);
        actions.Controls.Add(_stop);
        root.Controls.Add(actions, 0, 8);

        _status.Text = BackendSecurityGate.IsBlocked
            ? "Status: SECURITY HOLD — see docs/SECURITY_REVIEW.md"
            : "Status: Experimental receiver not running";
        _status.ForeColor = SecondaryColor;
        _status.Dock = DockStyle.Fill;
        root.Controls.Add(_status, 0, 9);

        _output.ReadOnly = true;
        _output.BackColor = PanelColor;
        _output.ForeColor = Color.White;
        _output.Font = new Font("Consolas", 9);
        _output.BorderStyle = BorderStyle.None;
        _output.Dock = DockStyle.Fill;
        _output.DetectUrls = false;
        root.Controls.Add(_output, 0, 10);

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = SecondaryColor,
            Text = "Read each connection PIN in the UxPlay console window. The controller writes no logs to disk. " +
                   "Allow Windows Firewall access on Private networks only."
        }, 0, 11);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text, Dock = DockStyle.Fill, ForeColor = SecondaryColor, TextAlign = ContentAlignment.BottomLeft
    };

    private static void StyleTextBox(TextBox box)
    {
        box.Dock = DockStyle.Fill;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = PanelColor;
        box.ForeColor = Color.White;
    }

    private async Task StartAsync()
    {
        if (BackendSecurityGate.IsBlocked)
        {
            SetStatus(BackendSecurityGate.Reason);
            Append(BackendSecurityGate.Reason);
            return;
        }
        if (_starting || _receiver?.IsRunning == true) return;

        if (BackendSecurityGate.IsExperimental)
        {
            DialogResult consent = MessageBox.Show(this,
                "This is an experimental AirPlay receiver. Its native network and media parsers " +
                "have NOT received a complete security audit.\n\n" +
                "Use only a trusted, isolated Private network with no unrelated devices. " +
                "Mirror non-sensitive content and keep your firewall's Public profile blocked.\n\n" +
                "Start an AirPlay network listener on this Windows PC?",
                "Experimental mirroring: confirm network test",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (consent != DialogResult.Yes)
            {
                SetStatus("Experimental receiver start cancelled.");
                return;
            }
        }

        string executable = _executable.Text.Trim().Trim('"');
        string name = _name.Text.Trim();
        if (!Regex.IsMatch(name, @"^[A-Za-z0-9][A-Za-z0-9 _.()-]{0,39}$"))
        {
            SetStatus("Receiver name must be 1–40 basic Latin characters.");
            return;
        }
        _starting = true;
        _start.Enabled = false;
        try
        {
            // The native bundle includes many third-party files; hashing all of
            // them synchronously on the UI thread can appear to freeze Start.
            SetStatus("Verifying bundled receiver files...");
            Append("Checking receiver and native dependency hashes. No network listener is active yet.");
            await Task.Run(() => ReceiverIdentity.Validate(executable));

            SetStatus("Checking Windows network and firewall settings...");
            var (allowed, message) = await NetworkGuard.CheckAsync();
            Append("Network: " + message);
            if (!allowed)
            {
                Append("BLOCKED BY NETWORK SAFETY PREFLIGHT: " + message);
                SetStatus("Network preflight blocked; see specific reason below.");
                return;
            }

            _receiver?.Dispose();
            _receiver = new ReceiverProcess();
            _receiver.Output += line => Append(line);
            _receiver.Exited += code => SafeUi(() =>
            {
                _start.Enabled = !BackendSecurityGate.IsBlocked;
                _stop.Enabled = false;
                string reason = code.HasValue ? "Receiver process exited (code " + code.Value + ")." : "Receiver process exited.";
                Append(reason + " Check the native UxPlay console for details.");
                SetStatus(reason);
            });
            MirrorQualityPreset preset = _quality.SelectedItem as MirrorQualityPreset
                ?? MirrorQualityPreset.Options[1];
            Append("Requested mirror quality: " + preset.Label + ". The iPhone may transmit at a lower resolution/frame rate.");
            _receiver.Start(executable, name, preset);
            if (!_receiver.IsRunning)
            {
                Append("UxPlay stopped immediately. Check the UxPlay console and Windows Security history.");
                SetStatus("Receiver exited immediately; see output.");
                return;
            }
            _stop.Enabled = true;
            Append("If the iPhone cannot find this PC, check that both are on the same trusted network. Do not bypass Windows Firewall or Smart App Control.");
            Append("Video appears in a separate GStreamer window AFTER iPhone connection; not inside this controller.");
            SetStatus("Receiver started; look for '" + name + "' in iPhone Screen Mirroring.");
        }
        catch (Exception ex)
        {
            Append("START FAILED: " + ex.Message);
            SetStatus("Start failed; see details in Output.");
        }
        finally
        {
            _starting = false;
            _start.Enabled = !BackendSecurityGate.IsBlocked && _receiver?.IsRunning != true;
        }
    }

    private void StopReceiver()
    {
        _receiver?.Stop();
        _stop.Enabled = false;
        _start.Enabled = !BackendSecurityGate.IsBlocked;
        SetStatus("Receiver stopped.");
    }

    private void SetStatus(string message) => _status.Text = "Status: " + message;

    private void Append(string message) => SafeUi(() =>
    {
        // Display PINs only in volatile UI memory; cap retained output.
        _output.AppendText(message + Environment.NewLine);
        if (_output.TextLength > 14000)
            _output.Text = _output.Text[^10000..];
        _output.SelectionStart = _output.TextLength;
        _output.ScrollToCaret();
    });

    private void SafeUi(Action action)
    {
        if (IsDisposed || Disposing) return;
        try
        {
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }
        catch (InvalidOperationException) { /* Form is closing. */ }
    }
}
