using MirrorControl;

static void Check(bool expected, string label)
{
    if (!expected) throw new Exception("FAIL: " + label);
    Console.WriteLine("PASS: " + label);
}

Check(MirrorQualityPreset.Options.Count == 4, "four quality profiles");
Check(MirrorQualityPreset.Options[2].EnableHevc, "1440p enables HEVC");
Check(!MirrorQualityPreset.Options[1].EnableHevc, "1080p does not require HEVC");
Check(MirrorQualityPreset.Options[2].GetUxPlayOptions().SequenceEqual(
    new[] { "-h265", "-s", "2560x1440@60", "-fps", "60" }), "1440p arguments");
Check(MirrorQualityPreset.Options[3].GetUxPlayOptions().SequenceEqual(
    new[] { "-h265", "-s", "3840x2160@60", "-fps", "30" }), "4K arguments");
Check(MirrorQualityPreset.Options[1].GetUxPlayOptions().SequenceEqual(
    new[] { "-s", "1920x1080@60", "-fps", "60" }), "1080p smooth arguments");

Check(BleHidCommands.SelectHost(1) == "host 1", "BleHid single host selection");
Check(BleHidCommands.Move(12, -20) == "move 12 -20", "BleHid mouse movement");
Check(BleHidCommands.Click == "click l", "BleHid left click");
Check(BleHidCommands.Scroll(-2) == "scroll -2", "BleHid scrolling");
Check(BleHidCommands.Key("enter") == "key enter", "BleHid Enter key");
Check(BleHidCommands.Type("hello") == "type hello", "BleHid ASCII typing");
Check(!BleHidCommands.IsSafe("type hello\nquit"), "newline injection rejected");
Check(!BleHidCommands.IsSafe("capture\rquit"), "carriage return injection rejected");

static void Reject(Action action, string label)
{
    try { action(); }
    catch (ArgumentException) { Console.WriteLine("PASS: " + label); return; }
    throw new Exception("FAIL: " + label);
}

Reject(() => BleHidCommands.SelectHost(0), "reject host zero");
Reject(() => BleHidCommands.Move(128, 0), "reject out of bounds movement");
Reject(() => BleHidCommands.Scroll(11), "reject excessive scroll");
Reject(() => BleHidCommands.Key("all"), "reject unsupported key");
Reject(() => BleHidCommands.Type("bad\ncommand"), "reject multi-line user input");
// BleHid console writes "> " without a newline; command responses include
// that prefix when the app redirects stdout. This caused paired hosts to
// disappear from the GUI despite real HID subscriptions.
Check(BleHidCliOutput.Normalize(">   keyboard subs : 1") == "keyboard subs : 1",
    "strip redirected CLI prompt");
Check(BleHidCliOutput.TryReadSubscribers(">   keyboard subs : 1", "keyboard", out int k) && k == 1,
    "read keyboard subscriptions with prompt");
Check(BleHidCliOutput.TryReadSubscribers("  mouse subs    : 2", "mouse", out int m) && m == 2,
    "read mouse subscriptions");
Check(!BleHidCliOutput.TryReadSubscribers("> keyboard subs : 1", "mouse", out _),
    "avoid treating keyboard subscribers as mouse subscribers");
Check(BleHidCliOutput.TryReadPeerCount(">   connected LE      (1):", "LE", out int le) && le == 1,
    "read BLE peer count with prompt");
Check(BleHidCliOutput.TryReadPeerCount("  connected classic (1):", "classic", out int classic)
    && classic == 1, "read Classic peer count");
Check(BleHidCliOutput.TryReadHost(">     1. Henry iPhone [A1:B2:C3]", out int hostIndex, out string host)
    && hostIndex == 1 && host == "Henry iPhone [A1:B2:C3]",
    "read numbered iPhone despite prompt");
Check(BleHidCliOutput.TryReadHost(">   * 2. iPhone [A1:B2:C3]   CAPS LOCK ON",
    out int starredIndex, out string starredHost)
    && starredIndex == 2 && starredHost == "iPhone [A1:B2:C3]",
    "read selected host and strip lock-key warning");
Check(BleHidCliOutput.SelectedHostConfirmed(">   sending to: iPhone [A1:B2:C3]",
    "iPhone [A1:B2:C3]"), "confirm selected host despite prompt");
Check(!BleHidCliOutput.SelectedHostConfirmed(">   sending to: another host",
    "iPhone [A1:B2:C3]"), "reject non-target selection");
Check(BleHidCliOutput.NoSubscribedHosts(">     (no subscribed hosts)"),
    "detect no subscribers despite prompt");
Check(!BleHidCliOutput.TryReadHost(">   sending to: this PC (input stays local)",
    out _, out _), "do not parse console status as a host");
Console.WriteLine("All mirror quality, BLE HID command and CLI parsing tests passed.");
