using System;
using System.Text.RegularExpressions;

namespace MirrorControl;

/// <summary>
/// Parses the interactive BleHid CLI stdout. Console.Write("> ") leaves a
/// prompt on the SAME line as the following response when output is redirected.
/// Normalizing that prefix is essential for subscriber counts, host discovery,
/// and confirmation that the intended iPhone was selected.
/// </summary>
internal static class BleHidCliOutput
{
    internal static string Normalize(string? raw)
    {
        string value = (raw ?? string.Empty).TrimStart();
        while (value.StartsWith('>'))
            value = value[1..].TrimStart();
        return value.TrimEnd();
    }

    internal static bool TryReadSubscribers(string raw, string kind, out int count)
    {
        count = 0;
        if (kind is not ("keyboard" or "mouse")) return false;
        var match = Regex.Match(Normalize(raw),
            @"^(keyboard|mouse) subs\s*:\s*(\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success &&
            string.Equals(match.Groups[1].Value, kind, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(match.Groups[2].Value, out count);
    }

    internal static bool TryReadPeerCount(string raw, string transport, out int count)
    {
        count = 0;
        if (transport is not ("LE" or "classic")) return false;
        var match = Regex.Match(Normalize(raw),
            @"^connected (LE|classic)\s*\(\s*(\d+)\s*\):$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success &&
            string.Equals(match.Groups[1].Value, transport, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(match.Groups[2].Value, out count);
    }

    internal static bool TryReadHost(string raw, out int index, out string display)
    {
        index = 0;
        display = string.Empty;
        var match = Regex.Match(Normalize(raw),
            @"^\*?\s*(\d{1,2})\.\s+(.+)$", RegexOptions.CultureInvariant);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out index)
            || index is < 1 or > 32)
            return false;

        display = match.Groups[2].Value.Trim();
        // Upstream may append a non-identity lock-key warning after [address].
        int addressEnd = display.LastIndexOf(']');
        if (addressEnd >= 0) display = display[..(addressEnd + 1)];
        return display.Length > 0;
    }

    internal static bool SelectedHostConfirmed(string raw, string expectedDisplay) =>
        !string.IsNullOrEmpty(expectedDisplay) &&
        string.Equals(Normalize(raw), "sending to: " + expectedDisplay, StringComparison.Ordinal);

    internal static bool NoSubscribedHosts(string raw) =>
        Normalize(raw).Equals("(no subscribed hosts)", StringComparison.OrdinalIgnoreCase);
}
