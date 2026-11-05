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
Console.WriteLine("All mirror quality and optional BLE HID command checks passed.");
