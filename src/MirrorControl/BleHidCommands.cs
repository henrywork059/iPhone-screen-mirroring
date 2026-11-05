using System;

namespace MirrorControl;

/// <summary>
/// Narrow input protocol supported by the pinned BleHid v0.6.0 CLI.
/// Every command is a single fixed-grammar line, never arbitrary user input.
/// </summary>
internal static class BleHidCommands
{
    internal static string SelectHost(int index)
    {
        if (index is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(index));
        return $"host {index}";
    }

    internal static string Move(int dx, int dy)
    {
        if (dx is < -127 or > 127 || dy is < -127 or > 127)
            throw new ArgumentOutOfRangeException(nameof(dx));
        return $"move {dx} {dy}";
    }

    internal static string Click => "click l";

    internal static string Scroll(int steps)
    {
        if (steps is < -10 or > 10)
            throw new ArgumentOutOfRangeException(nameof(steps));
        return $"scroll {steps}";
    }

    internal static string Key(string name) => name switch
    {
        "enter" or "esc" or "backspace" or "up" or "down" or "left" or "right"
            => $"key {name}",
        _ => throw new ArgumentException("Unsupported key.", nameof(name))
    };

    internal static string Type(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 64)
            throw new ArgumentException("Text must be 1–64 characters.", nameof(value));
        foreach (char ch in value)
            if (ch < 32 || ch > 126)
                throw new ArgumentException("Only printable ASCII is supported.", nameof(value));
        return "type " + value;
    }

    internal static bool IsSafe(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 80 &&
        value.All(c => c is >= ' ' and <= '~');
}
