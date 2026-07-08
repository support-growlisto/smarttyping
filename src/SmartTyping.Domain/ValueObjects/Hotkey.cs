using System.Globalization;

namespace SmartTyping.Domain.ValueObjects;

/// <summary>Modifier keys for a <see cref="Hotkey"/>.</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4,
    Win = 8
}

/// <summary>
/// A global hotkey: a set of modifiers plus a Windows virtual-key code. Serializes to a stable,
/// human-readable string such as <c>Ctrl+Shift+L</c>.
/// </summary>
public readonly record struct Hotkey(HotkeyModifiers Modifiers, int VirtualKey)
{
    /// <summary>True if at least one non-Shift modifier is present (so it won't hijack plain typing).</summary>
    public bool IsValid =>
        VirtualKey != 0 && (Modifiers & (HotkeyModifiers.Ctrl | HotkeyModifiers.Alt | HotkeyModifiers.Win)) != 0;

    public string ToStorageString()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Ctrl)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(KeyName(VirtualKey));
        return string.Join("+", parts);
    }

    public override string ToString() => ToStorageString();

    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var mods = HotkeyModifiers.None;
        var vk = 0;
        foreach (var raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl" or "control": mods |= HotkeyModifiers.Ctrl; break;
                case "shift": mods |= HotkeyModifiers.Shift; break;
                case "alt": mods |= HotkeyModifiers.Alt; break;
                case "win": mods |= HotkeyModifiers.Win; break;
                default:
                    if (!TryParseKey(raw, out vk))
                    {
                        return false;
                    }
                    break;
            }
        }

        if (vk == 0)
        {
            return false;
        }

        hotkey = new Hotkey(mods, vk);
        return true;
    }

    private static string KeyName(int vk) => vk switch
    {
        >= 0x41 and <= 0x5A => ((char)vk).ToString(),          // A-Z
        >= 0x30 and <= 0x39 => ((char)vk).ToString(),          // 0-9
        0x20 => "Space",
        >= 0x70 and <= 0x7B => "F" + (vk - 0x6F),              // F1-F12
        _ => "0x" + vk.ToString("X2", CultureInfo.InvariantCulture)
    };

    private static bool TryParseKey(string name, out int vk)
    {
        vk = 0;
        if (name.Length == 1)
        {
            var c = char.ToUpperInvariant(name[0]);
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                vk = c;
                return true;
            }
        }

        if (string.Equals(name, "Space", StringComparison.OrdinalIgnoreCase))
        {
            vk = 0x20;
            return true;
        }

        if (name.Length is 2 or 3 && (name[0] is 'F' or 'f')
            && int.TryParse(name.AsSpan(1), out var fn) && fn is >= 1 and <= 12)
        {
            vk = 0x6F + fn;
            return true;
        }

        if (name.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(name.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
        {
            vk = hex;
            return true;
        }

        return false;
    }
}
