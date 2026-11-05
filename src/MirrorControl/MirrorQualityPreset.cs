using System.Collections.Generic;

namespace MirrorControl;

/// <summary>
/// AirPlay can only REQUEST a stream size/frame rate. The connected iPhone
/// decides what it actually produces. HEVC requires compatible iPhone/GStreamer.
/// </summary>
internal sealed record MirrorQualityPreset(string Label, string Resolution, int MaximumFps, bool EnableHevc)
{
    internal static IReadOnlyList<MirrorQualityPreset> Options { get; } = new[]
    {
        new MirrorQualityPreset("Compatible — 1080p / 30 fps", "1920x1080@60", 30, false),
        new MirrorQualityPreset("Smooth — 1080p / up to 60 fps", "1920x1080@60", 60, false),
        new MirrorQualityPreset("Sharper — 1440p / up to 60 fps (HEVC)", "2560x1440@60", 60, true),
        new MirrorQualityPreset("Ultra — 4K / up to 30 fps (HEVC)", "3840x2160@60", 30, true)
    };

    public override string ToString() => Label;

    internal IEnumerable<string> GetUxPlayOptions()
    {
        if (EnableHevc)
            yield return "-h265";
        yield return "-s";
        yield return Resolution;
        yield return "-fps";
        yield return MaximumFps.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
