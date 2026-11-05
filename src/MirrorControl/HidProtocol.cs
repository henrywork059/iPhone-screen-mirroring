using System;

namespace MirrorControl;

/// <summary>
/// Strict newline-framed, ASCII-only serial HID commands. Nothing is
/// transmitted unless the user explicitly connects an external USB bridge.
/// </summary>
internal static class HidProtocol
{
    internal static string Move(int dx, int dy)
    {
        if (dx is < -127 or > 127 || dy is < -127 or > 127)
            throw new ArgumentOutOfRangeException(nameof(dx), "HID movement must fit signed 8-bit deltas.");
        return $"M {dx} {dy}\n";
    }

    internal static string Scroll(int steps)
    {
        if (steps is < -10 or > 10)
            throw new ArgumentOutOfRangeException(nameof(steps));
        return $"W {steps}\n";
    }

    internal static string ButtonDown => "D\n";
    internal static string ButtonUp => "U\n";

    internal static string Key(string key) => key switch
    {
        "ENTER" or "BACKSPACE" or "ESC" or "UP" or "DOWN" or "LEFT" or "RIGHT"
            => $"K {key}\n",
        _ => throw new ArgumentException("Unsupported HID keyboard key.", nameof(key))
    };

    internal static string Text(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 48)
            throw new ArgumentException("Text must have 1–48 characters.", nameof(value));
        foreach (char ch in value)
            if (ch < 32 || ch > 126)
                throw new ArgumentException("Only printable ASCII is supported in the prototype.", nameof(value));
        return $"T {value}\n";
    }
}
