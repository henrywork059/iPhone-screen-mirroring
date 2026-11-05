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

Check(HidProtocol.Move(15,-20) == "M 15 -20\n", "HID movement framing");
Check(HidProtocol.Scroll(-2) == "W -2\n", "HID wheel framing");
Check(HidProtocol.Text("hello world!") == "T hello world!\n", "HID text framing");
Check(HidProtocol.Key("ENTER") == "K ENTER\n", "HID special-key framing");
Check(HidProtocol.ButtonDown == "D\n" && HidProtocol.ButtonUp == "U\n", "HID button framing");

static void MustReject(Action attempt, string label)
{
    try { attempt(); }
    catch (ArgumentException) { Console.WriteLine("PASS: rejects " + label); return; }
    throw new Exception("FAIL: expected to reject " + label);
}
MustReject(() => HidProtocol.Move(128, 0), "out-of-range HID delta");
MustReject(() => HidProtocol.Text("bad\ncommand"), "embedded newlines");
MustReject(() => HidProtocol.Text(""), "empty text");
MustReject(() => HidProtocol.Key("ANYTHING"), "unsupported key");
Console.WriteLine("All offline controller logic checks passed.");
