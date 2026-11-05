using System;
using System.Drawing;
using System.Windows.Forms;

namespace MirrorControl;

/// <summary>
/// Opt-in Windows Bluetooth HID control for paired iPhones. Uses the tested
/// Windows LE peripheral capability; no ESP32, external serial adapter or
/// iPhone app. The AirPlay video still has a separate GStreamer window.
/// </summary>
internal sealed class BluetoothControlForm : Form
{
    private sealed record HostChoice(int Number, string Display)
    {
        public override string ToString() => $"{Number}. {Display}";
    }

    private readonly BleHidCliProcess _cli = new();
    private readonly ComboBox _hosts = new();
    private readonly Button _start;
    private readonly Button _stop;
    private readonly Button _refresh;
    private readonly Button _select;
    private readonly Button _capture;
    private readonly TextBox _typing = new();
    private readonly RichTextBox _log = new();
    private readonly Label _status = new();
    private string? _pendingHost;
    private bool _targetReady;
    private bool _capturing;
    private int _keyboardSubscribers;
    private int _mouseSubscribers;
    private int? _lePeerCount;
    private int? _classicPeerCount;

    internal BluetoothControlForm()
    {
        Text = "iPhone Mirror — Windows Bluetooth Mouse and Keyboard";
        Size = new Size(830, 745);
        MinimumSize = new Size(690, 655);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(48, 52, 70);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 9
        };
        foreach (var height in new[] { 100, 49, 46, 52, 46, 48, 82, 0, 34 })
            root.RowStyles.Add(height == 0
                ? new RowStyle(SizeType.Percent, 100)
                : new RowStyle(SizeType.Absolute, height));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Windows Bluetooth HID — experimental\n" +
                   "No external hardware. Pair this PC in iPhone Settings → Bluetooth and enable " +
                   "Settings → Accessibility → Touch → AssistiveTouch.\n" +
                   "Close any BleHid.Cli.exe already running in PowerShell before starting here.",
            ForeColor = Color.FromArgb(211, 226, 238),
            Font = new Font("Segoe UI", 10)
        }, 0, 0);

        var launch = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _start = MakeButton("Start Bluetooth", (_, _) => StartBluetooth(), 164);
        _stop = MakeButton("Stop Bluetooth", (_, _) => StopBluetooth(), 154);
        _refresh = MakeButton("Refresh paired hosts", (_, _) => RefreshHosts(), 176);
        launch.Controls.AddRange(new Control[] { _start, _stop, _refresh });
        root.Controls.Add(launch, 0, 1);

        var hostsRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _hosts.DropDownStyle = ComboBoxStyle.DropDownList;
        _hosts.Width = 490;
        _hosts.BackColor = Color.FromArgb(79, 86, 106);
        _hosts.ForeColor = Color.White;
        hostsRow.Controls.Add(_hosts);
        _select = MakeButton("Use selected iPhone", (_, _) => SelectHost(), 182);
        hostsRow.Controls.Add(_select);
        root.Controls.Add(hostsRow, 0, 2);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _capture = MakeButton("Capture Windows mouse + keyboard", (_, _) => StartCapture(), 290);
        actions.Controls.Add(_capture);
        actions.Controls.Add(new Label
        {
            Text = "Press Ctrl+Alt+Q to RETURN control to Windows.",
            AutoSize = true, Padding = new Padding(8, 8, 0, 0),
            ForeColor = Color.FromArgb(255, 221, 157)
        });
        root.Controls.Add(actions, 0, 3);

        var keyboard = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _typing.Width = 440;
        _typing.MaxLength = 64;
        _typing.PlaceholderText = "Non-sensitive ASCII text to type on iPhone";
        _typing.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            SendText();
            e.SuppressKeyPress = true;
        };
        keyboard.Controls.Add(_typing);
        keyboard.Controls.Add(MakeButton("Send text", (_, _) => SendText(), 120));
        keyboard.Controls.Add(MakeButton("Enter", (_, _) => SendKey("enter"), 85));
        root.Controls.Add(keyboard, 0, 4);

        var pointer = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        pointer.Controls.Add(MakeButton("←", (_, _) => Send(BleHidCommands.Move(-50, 0)), 57));
        pointer.Controls.Add(MakeButton("↑", (_, _) => Send(BleHidCommands.Move(0, -50)), 57));
        pointer.Controls.Add(MakeButton("↓", (_, _) => Send(BleHidCommands.Move(0, 50)), 57));
        pointer.Controls.Add(MakeButton("→", (_, _) => Send(BleHidCommands.Move(50, 0)), 57));
        pointer.Controls.Add(MakeButton("Tap / click", (_, _) => Send(BleHidCommands.Click), 105));
        pointer.Controls.Add(MakeButton("Scroll up", (_, _) => Send(BleHidCommands.Scroll(2)), 106));
        pointer.Controls.Add(MakeButton("Scroll down", (_, _) => Send(BleHidCommands.Scroll(-2)), 120));
        root.Controls.Add(pointer, 0, 5);

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "IMPORTANT: Only select the iPhone you own. Bluetooth input is separate from AirPlay.\n" +
                   "Capture redirects mouse and keyboard system-wide until Ctrl+Alt+Q. " +
                   "The pointer is relative: it is not automatically aligned to pixels in the separate AirPlay video window. " +
                   "Do not type passwords or codes through this experimental controller.",
            ForeColor = Color.FromArgb(255, 221, 157)
        }, 0, 6);

        _log.Dock = DockStyle.Fill;
        _log.ReadOnly = true;
        _log.BackColor = Color.FromArgb(35, 39, 54);
        _log.ForeColor = Color.White;
        _log.BorderStyle = BorderStyle.None;
        _log.Font = new Font("Consolas", 9);
        _log.DetectUrls = false;
        root.Controls.Add(_log, 0, 7);

        _status.Dock = DockStyle.Fill;
        _status.ForeColor = Color.FromArgb(196, 215, 230);
        _status.Text = "Not running. Start Bluetooth, pair on iPhone, refresh hosts, select that iPhone.";
        root.Controls.Add(_status, 0, 8);

        _cli.Output += line => SafeUi(() => HandleOutput(line));
        _cli.Exited += () => SafeUi(() =>
        {
            _targetReady = false;
            _capturing = false;
            _pendingHost = null;
            _status.Text = "Bluetooth companion stopped.";
            UpdateControls();
        });
        FormClosing += (_, _) => _cli.Dispose();
        UpdateControls();
    }

    private static Button MakeButton(string title, EventHandler click, int width)
    {
        var button = new Button { Text = title, Width = width, Height = 33 };
        button.Click += click;
        return button;
    }

    private void StartBluetooth()
    {
        if (_cli.IsRunning) return;
        var consent = MessageBox.Show(this,
            "Start a Bluetooth LE keyboard and mouse peripheral on this PC?\n\n" +
            "Only pair with your own iPhone. This experimental process is separately " +
            "downloaded from a pinned open-source MIT-licensed release and checked by SHA-256.\n\n" +
            "Close any other BleHid application first. Turn on AssistiveTouch on the iPhone.",
            "Start experimental Bluetooth input", MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (consent != DialogResult.Yes) return;

        try
        {
            _targetReady = false;
            _capturing = false;
            _pendingHost = null;
            _keyboardSubscribers = 0;
            _mouseSubscribers = 0;
            _hosts.Items.Clear();
            _cli.Start();
            _status.Text = "Starting Bluetooth advertising... pair your iPhone in Bluetooth settings.";
        }
        catch (Exception ex)
        {
            Append("ERROR: " + ex.Message);
            _status.Text = "Could not start Bluetooth. Check that the other CLI is closed.";
        }
        UpdateControls();
    }

    private void RefreshHosts()
    {
        if (!_cli.IsReady) return;
        _targetReady = false;
        _pendingHost = null;
        _keyboardSubscribers = 0;
        _mouseSubscribers = 0;
        _hosts.Items.Clear();
        _lePeerCount = null;
        _classicPeerCount = null;
        // Bluetooth pairing alone does not guarantee HID report subscriptions.
        // Query actual LE and Classic link state before listing HID subscribers.
        _cli.TrySend("peers");
        _cli.TrySend("status");
        _cli.TrySend("host");
        _status.Text = "Inspecting Bluetooth links and HID subscribers...";
        UpdateControls();
    }

    private void SelectHost()
    {
        if (!_cli.IsReady || _hosts.SelectedItem is not HostChoice host) return;
        _targetReady = false;
        _pendingHost = host.Display;
        if (!_cli.TrySend(BleHidCommands.SelectHost(host.Number)))
            _status.Text = "Could not select iPhone; restart Bluetooth.";
        else
            _status.Text = "Verifying selected Bluetooth host...";
        UpdateControls();
    }

    private void StartCapture()
    {
        if (!CanSend || _capturing) return;
        var confirmed = MessageBox.Show(this,
            "This temporarily redirects your Windows mouse AND keyboard to the selected " +
            "iPhone. Your local Windows clicks and keystrokes will stop working during capture.\n\n" +
            "IMPORTANT: Press Ctrl+Alt+Q to stop capture and return control to Windows.\n\n" +
            "Do not type sensitive information or open secure apps during the test.\n\n" +
            "Start capture now?",
            "Bluetooth input capture — confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirmed != DialogResult.Yes) return;
        if (_cli.TrySend("capture"))
        {
            _status.Text = "Capture requested. Press Ctrl+Alt+Q to return control to Windows.";
            _capture.Enabled = false;
        }
    }

    private void StopBluetooth()
    {
        _cli.Stop();
        _targetReady = false;
        _capturing = false;
        _pendingHost = null;
        _keyboardSubscribers = 0;
        _mouseSubscribers = 0;
        _hosts.Items.Clear();
        _status.Text = "Bluetooth stopped.";
        UpdateControls();
    }

    private bool CanSend => _cli.IsReady && _targetReady &&
        _keyboardSubscribers > 0 && _mouseSubscribers > 0;

    private void Send(string command)
    {
        if (!CanSend || _capturing)
        {
            _status.Text = "Pair and select an active iPhone first, then send input.";
            return;
        }
        if (!_cli.TrySend(command))
            _status.Text = "Bluetooth input command failed.";
    }

    private void SendKey(string key) => Send(BleHidCommands.Key(key));

    private void SendText()
    {
        if (!CanSend || _capturing) return;
        try
        {
            string command = BleHidCommands.Type(_typing.Text);
            if (_cli.TrySend(command)) _typing.Clear();
        }
        catch (ArgumentException ex) { _status.Text = ex.Message; }
    }

    private void HandleOutput(string line)
    {
        // No persistent controller logs, no user-input content in the status.
        Append(line);
        if (line.Contains("Advertisement status: Started", StringComparison.OrdinalIgnoreCase))
        {
            _status.Text = "Advertising. Pair with iPhone, enable AssistiveTouch, then Refresh paired hosts.";
        }
        if (BleHidCliOutput.TryReadPeerCount(line, "LE", out int leCount))
            _lePeerCount = leCount;
        if (BleHidCliOutput.TryReadPeerCount(line, "classic", out int classicCount))
            _classicPeerCount = classicCount;

        if (BleHidCliOutput.TryReadSubscribers(line, "keyboard", out int keyboardCount))
            _keyboardSubscribers = keyboardCount;
        if (BleHidCliOutput.TryReadSubscribers(line, "mouse", out int mouseCount))
            _mouseSubscribers = mouseCount;

        // A redirected BleHid CLI can prefix command responses with "> ".
        // Parse only the normalized host records, not arbitrary console text.
        if (BleHidCliOutput.TryReadHost(line, out int index, out string display))
        {
            if (!_hosts.Items.Cast<HostChoice>().Any(x => x.Number == index && x.Display == display))
                _hosts.Items.Add(new HostChoice(index, display));
            if (_hosts.SelectedIndex == -1 && _hosts.Items.Count > 0)
                _hosts.SelectedIndex = 0;
            _status.Text = "Subscribed HID device found. Select only your iPhone.";
        }

        if (_pendingHost is not null &&
            BleHidCliOutput.SelectedHostConfirmed(line, _pendingHost))
        {
            _targetReady = true;
            _status.Text = _keyboardSubscribers > 0 && _mouseSubscribers > 0
                ? "iPhone target selected. Test pointer buttons or start capture."
                : "Host selected, but keyboard/mouse subscriptions are not both active. Refresh hosts.";
            _pendingHost = null;
        }

        if (BleHidCliOutput.NoSubscribedHosts(line))
        {
            _status.Text = _lePeerCount.GetValueOrDefault() > 0
                ? "BLE link exists, but no HID subscriptions. Forget and re-pair the PC on iPhone if this persists."
                : _classicPeerCount.GetValueOrDefault() > 0
                    ? "A Classic Bluetooth link is present, but no LE HID subscriber. Re-pair after starting Bluetooth HID."
                    : "No connected BLE HID host. iPhone's Bluetooth pairing alone does not mean HID is connected.";
        }
        if (line.Contains("capturing - Ctrl+Alt+Q stops.", StringComparison.OrdinalIgnoreCase))
        {
            _capturing = true;
            _status.Text = "Mouse/keyboard captured by iPhone. Press Ctrl+Alt+Q to return to Windows.";
        }
        if (line.Contains("capture stopped.", StringComparison.OrdinalIgnoreCase))
        {
            _capturing = false;
            _targetReady = false;
            _pendingHost = null;
            _cli.TrySend("host local");
            _status.Text = "Capture ended. Windows input restored. Re-select iPhone to send more commands.";
        }
        if (line.Contains("Startup failed:", StringComparison.OrdinalIgnoreCase))
            _status.Text = "BLE startup failed. Close other BleHid processes and retry.";
        UpdateControls();
    }

    private void Append(string message)
    {
        _log.AppendText(message + Environment.NewLine);
        if (_log.TextLength > 16000)
            _log.Text = _log.Text[^11000..];
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    private void UpdateControls()
    {
        bool running = _cli.IsRunning;
        _start.Enabled = !running && BackendSecurityGate.IsExperimental;
        _stop.Enabled = running;
        _refresh.Enabled = _cli.IsReady && !_capturing;
        _select.Enabled = _cli.IsReady && _hosts.SelectedItem is HostChoice && !_capturing;
        _capture.Enabled = CanSend && !_capturing;
        _typing.Enabled = CanSend && !_capturing;
    }

    private void SafeUi(Action action)
    {
        if (Disposing || IsDisposed) return;
        try
        {
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }
        catch (InvalidOperationException) { /* closing */ }
    }
}
