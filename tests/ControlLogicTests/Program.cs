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

Console.WriteLine("All mirror quality profile checks passed.");
