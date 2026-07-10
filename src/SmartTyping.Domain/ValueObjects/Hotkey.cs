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
    /// <summary>
    /// True when the combination cannot hijack plain typing: either it carries a non-Shift modifier,
    /// or it is Shift plus a key that produces no character (Backspace, Delete, F-keys, arrows…).
    /// The latter is what allows the RightLang-style <c>Shift+Backspace</c> undo.
    /// </summary>
    public bool IsValid =>
        VirtualKey != 0 &&
        ((Modifiers & (HotkeyModifiers.Ctrl | HotkeyModifiers.Alt | HotkeyModifiers.Win)) != 0 ||
         (Modifiers.HasFlag(HotkeyModifiers.Shift) && ProducesNoCharacter(VirtualKey)));

    /// <summary>Keys that never insert text, so Shift alone is a safe modifier for them.</summary>
    private static bool ProducesNoCharacter(int virtualKey) => virtualKey switch
    {
        0x08 => true,                    // Backspace
        0x1B => true,                    // Escape
        0x2D or 0x2E => true,            // Insert, Delete
        >= 0x21 and <= 0x28 => true,     // PageUp/PageDown/End/Home/arrows
        >= 0x70 and <= 0x87 => true,     // F1–F24
        _ => false
    };

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

    /// <summary>Named non-character keys, so they round-trip through <see cref="ToStorageString"/>.</summary>
    private static readonly (int Vk, string Name)[] NamedKeys =
    {
        (0x08, "Backspace"), (0x1B, "Escape"), (0x2D, "Insert"), (0x2E, "Delete"),
        (0x21, "PageUp"), (0x22, "PageDown"), (0x23, "End"), (0x24, "Home"),
        (0x25, "Left"), (0x26, "Up"), (0x27, "Right"), (0x28, "Down")
    };

    private static string KeyName(int vk)
    {
        foreach (var (named, name) in NamedKeys)
        {
            if (named == vk)
            {
                return name;
            }
        }

        return vk switch
        {
            >= 0x41 and <= 0x5A => ((char)vk).ToString(),          // A-Z
            >= 0x30 and <= 0x39 => ((char)vk).ToString(),          // 0-9
            0x20 => "Space",
            >= 0x70 and <= 0x7B => "F" + (vk - 0x6F),              // F1-F12
            _ => "0x" + vk.ToString("X2", CultureInfo.InvariantCulture)
        };
    }

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

        foreach (var (named, keyName) in NamedKeys)
        {
            if (string.Equals(name, keyName, StringComparison.OrdinalIgnoreCase))
            {
                vk = named;
                return true;
            }
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
