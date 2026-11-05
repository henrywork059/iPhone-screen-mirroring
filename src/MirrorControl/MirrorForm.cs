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
    private readonly RichTextBox _output = new();
    private readonly Label _status = new();
    private readonly Button _start = new();
    private readonly Button _stop = new();
    private ReceiverProcess? _receiver;
    private bool _starting;

    public MirrorForm()
    {
        Text = "iPhone Mirror | Windows Controller";
        MinimumSize = new Size(620, 570);
        Size = new Size(760, 630);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BackgroundColor;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        FormClosing += (_, _) => _receiver?.Dispose();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1,
            RowCount = 10, BackColor = BackgroundColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
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
            Text = "Private-network AirPlay receiver · UxPlay v1.73.7 (external dependency)",
            ForeColor = SecondaryColor, AutoSize = false, Dock = DockStyle.Fill
        }, 0, 1);
        root.Controls.Add(MakeLabel("Path to uxplay.exe"), 0, 2);
        _executable.Text = @"C:\msys64\ucrt64\bin\uxplay.exe";
        StyleTextBox(_executable);
        root.Controls.Add(_executable, 0, 3);

        root.Controls.Add(MakeLabel("Receiver name shown on iPhone"), 0, 4);
        _name.Text = "iPhone Mirror";
        StyleTextBox(_name);
        root.Controls.Add(_name, 0, 5);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _start.Text = "Start receiver";
        _start.BackColor = AccentColor; _start.ForeColor = Color.Black; _start.FlatStyle = FlatStyle.Flat;
        _start.Size = new Size(165, 42);
        _start.Click += async (_, _) => await StartAsync();
        _stop.Text = "Stop receiver";
        _stop.BackColor = PanelColor; _stop.ForeColor = Color.White; _stop.FlatStyle = FlatStyle.Flat;
        _stop.Size = new Size(165, 42); _stop.Enabled = false;
        _stop.Click += (_, _) => StopReceiver();
        actions.Controls.Add(_start);
        actions.Controls.Add(_stop);
        root.Controls.Add(actions, 0, 6);

        _status.Text = "Status: Not running";
        _status.ForeColor = SecondaryColor;
        _status.Dock = DockStyle.Fill;
        root.Controls.Add(_status, 0, 7);

        _output.ReadOnly = true;
        _output.BackColor = PanelColor;
        _output.ForeColor = Color.White;
        _output.Font = new Font("Consolas", 9);
        _output.BorderStyle = BorderStyle.None;
        _output.Dock = DockStyle.Fill;
        _output.DetectUrls = false;
        root.Controls.Add(_output, 0, 8);

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = SecondaryColor,
            Text = "Read each connection PIN in the UxPlay console window. The controller writes no logs to disk. " +
                   "Allow Windows Firewall access on Private networks only."
        }, 0, 9);
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
        if (_starting || _receiver?.IsRunning == true) return;

        string executable = _executable.Text.Trim().Trim('"');
        string name = _name.Text.Trim();
        if (!Regex.IsMatch(name, @"^[A-Za-z0-9][A-Za-z0-9 _.()-]{0,39}$"))
        {
            SetStatus("Receiver name must be 1–40 basic Latin characters.");
            return;
        }
        if (!File.Exists(executable))
        {
            SetStatus("UxPlay not found. Complete docs/WINDOWS_SETUP.md first.");
            return;
        }

        _starting = true;
        _start.Enabled = false;
        SetStatus("Checking Windows network profiles...");
        try
        {
            var (allowed, message) = await NetworkGuard.CheckAsync();
            Append("Network: " + message);
            if (!allowed)
            {
                SetStatus("Blocked by network safety preflight.");
                return;
            }

            _receiver?.Dispose();
            _receiver = new ReceiverProcess();
            _receiver.Output += line => Append(line);
            _receiver.Exited += () => SafeUi(() =>
            {
                _start.Enabled = true;
                _stop.Enabled = false;
                SetStatus("Receiver stopped.");
            });
            _receiver.Start(executable, name);
            _stop.Enabled = true;
            SetStatus("Receiver running. Read PIN in console; select '" + name + "' on iPhone.");
        }
        catch (Exception ex)
        {
            Append("ERROR: " + ex.Message);
            SetStatus("Could not start receiver.");
        }
        finally
        {
            _starting = false;
            _start.Enabled = _receiver?.IsRunning != true;
        }
    }

    private void StopReceiver()
    {
        _receiver?.Stop();
        _stop.Enabled = false;
        _start.Enabled = true;
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
