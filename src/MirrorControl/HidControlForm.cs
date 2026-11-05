using System;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace MirrorControl;

/// <summary>
/// Experimental local-only mouse/keyboard control pad. It does not intercept
/// input inside UxPlay's separate GStreamer video window. A USB-attached ESP32
/// running the optional firmware is required to relay HID over Bluetooth.
/// </summary>
internal sealed class HidControlForm : Form
{
    private readonly ComboBox _ports = new();
    private readonly Button _connect = new();
    private readonly Button _disconnect = new();
    private readonly Panel _pad = new();
    private readonly TextBox _typing = new();
    private readonly Label _status = new();
    private SerialPort? _serial;
    private Point? _previous;
    private long _lastMoveAt;
    private bool _held;

    internal HidControlForm()
    {
        Text = "iPhone Input Pad — Experimental Bluetooth HID Bridge";
        Size = new Size(610, 630);
        MinimumSize = new Size(520, 570);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(48, 52, 70);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 7
        };
        foreach (int height in new[] { 72, 48, 30, 0, 38, 50, 60 })
            layout.RowStyles.Add(height == 0
                ? new RowStyle(SizeType.Percent, 100)
                : new RowStyle(SizeType.Absolute, height));
        Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Optional USB-to-Bluetooth control bridge. Requires an ESP32 paired with the iPhone.\n" +
                   "Move and click inside this pad; clicks in UxPlay's separate video window do not transfer.",
            ForeColor = Color.FromArgb(210, 219, 237)
        }, 0, 0);

        var connection = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _ports.DropDownStyle = ComboBoxStyle.DropDownList;
        _ports.Width = 115;
        _ports.DropDown += (_, _) => RefreshPorts();
        connection.Controls.Add(_ports);
        Button refresh = MakeButton("Refresh", (_, _) => RefreshPorts(), 75);
        connection.Controls.Add(refresh);
        _connect = MakeButton("Connect USB", (_, _) => Connect(), 125);
        connection.Controls.Add(_connect);
        _disconnect = MakeButton("Disconnect", (_, _) => Disconnect(), 118);
        _disconnect.Enabled = false;
        connection.Controls.Add(_disconnect);
        layout.Controls.Add(connection, 0, 1);

        layout.Controls.Add(new Label
        {
            Text = "Touchpad — mouse movement / left-click / drag / wheel",
            Dock = DockStyle.Fill
        }, 0, 2);
        _pad.Dock = DockStyle.Fill;
        _pad.BackColor = Color.FromArgb(79, 102, 130);
        _pad.BorderStyle = BorderStyle.FixedSingle;
        _pad.Cursor = Cursors.Cross;
        _pad.MouseEnter += (_, _) => _previous = null;
        _pad.MouseLeave += (_, _) => { Release(); _previous = null; };
        _pad.MouseMove += (_, e) => Move(e.Location);
        _pad.MouseDown += (_, e) => { _previous = e.Location; if (e.Button == MouseButtons.Left) Press(); };
        _pad.MouseUp += (_, e) => { if (e.Button == MouseButtons.Left) Release(); };
        _pad.MouseWheel += (_, e) => Transmit(HidProtocol.Scroll(Math.Clamp(Math.Sign(e.Delta), -1, 1)));
        layout.Controls.Add(_pad, 0, 3);

        _typing.Dock = DockStyle.Fill;
        _typing.MaxLength = 48;
        _typing.PlaceholderText = "Type non-sensitive ASCII text here";
        _typing.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            SendText(); e.SuppressKeyPress = true;
        };
        layout.Controls.Add(_typing, 0, 4);

        var keys = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        keys.Controls.Add(MakeButton("Send text", (_, _) => SendText(), 90));
        keys.Controls.Add(MakeButton("Enter", (_, _) => Transmit(HidProtocol.Key("ENTER")), 75));
        keys.Controls.Add(MakeButton("Backspace", (_, _) => Transmit(HidProtocol.Key("BACKSPACE")), 112));
        keys.Controls.Add(MakeButton("Esc", (_, _) => Transmit(HidProtocol.Key("ESC")), 65));
        keys.Controls.Add(MakeButton("Scroll ↑", (_, _) => Transmit(HidProtocol.Scroll(1)), 90));
        keys.Controls.Add(MakeButton("Scroll ↓", (_, _) => Transmit(HidProtocol.Scroll(-1)), 90));
        keys.WrapContents = true;
        layout.Controls.Add(keys, 0, 5);

        _status.Text = "USB bridge disconnected. Pair the ESP32 with the iPhone in Bluetooth settings.";
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = Color.FromArgb(200, 220, 235);
        layout.Controls.Add(_status, 0, 6);
        FormClosing += (_, _) => Disconnect();
        RefreshPorts();
    }

    private static Button MakeButton(string title, EventHandler click, int width)
    {
        var button = new Button { Text = title, Width = width, Height = 31 };
        button.Click += click;
        return button;
    }

    private void RefreshPorts()
    {
        string? selected = _ports.SelectedItem?.ToString();
        _ports.Items.Clear();
        try
        {
            foreach (string port in SerialPort.GetPortNames().OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                _ports.Items.Add(port);
            if (selected is not null && _ports.Items.Contains(selected))
                _ports.SelectedItem = selected;
            else if (_ports.Items.Count > 0)
                _ports.SelectedIndex = 0;
        }
        catch (Exception ex) { _status.Text = "Could not read COM ports: " + ex.Message; }
    }

    private void Connect()
    {
        if (_serial is not null || _ports.SelectedItem is not string port) return;
        try
        {
            var serial = new SerialPort(port, 115200)
            {
                WriteTimeout = 250,
                ReadTimeout = 250,
                NewLine = "\n",
                DtrEnable = false,
                RtsEnable = false
            };
            serial.Open();
            _serial = serial;
            _connect.Enabled = false;
            _disconnect.Enabled = true;
            _status.Text = "USB serial connected to " + port + ". Bluetooth pairing with iPhone is separate.";
        }
        catch (Exception ex)
        {
            _status.Text = "USB connection failed: " + ex.Message;
            Disconnect();
        }
    }

    private void Transmit(string frame)
    {
        if (_serial?.IsOpen != true)
        {
            _status.Text = "Connect a USB ESP32 first. Built-in Windows Bluetooth is not a HID relay.";
            return;
        }
        try { _serial.Write(frame); }
        catch (Exception ex)
        {
            _status.Text = "USB HID write failed: " + ex.Message;
            Disconnect();
        }
    }

    private void Move(Point current)
    {
        if (_previous is not Point last)
        {
            _previous = current;
            return;
        }
        long now = Environment.TickCount64;
        if (now - _lastMoveAt < 18) return;
        int dx = Math.Clamp(current.X - last.X, -127, 127);
        int dy = Math.Clamp(current.Y - last.Y, -127, 127);
        _previous = current;
        _lastMoveAt = now;
        if (dx != 0 || dy != 0) Transmit(HidProtocol.Move(dx, dy));
    }

    private void Press()
    {
        if (_held) return;
        _held = true;
        Transmit(HidProtocol.ButtonDown);
    }

    private void Release()
    {
        if (!_held) return;
        _held = false;
        if (_serial?.IsOpen == true) Transmit(HidProtocol.ButtonUp);
    }

    private void SendText()
    {
        if (_typing.TextLength == 0) return;
        try
        {
            Transmit(HidProtocol.Text(_typing.Text));
            _typing.Clear();
        }
        catch (ArgumentException ex) { _status.Text = ex.Message; }
    }

    private void Disconnect()
    {
        Release();
        _serial?.Dispose();
        _serial = null;
        _connect.Enabled = true;
        _disconnect.Enabled = false;
        _previous = null;
        _status.Text = "Disconnected. The iPhone Bluetooth HID device may remain paired.";
    }
}
