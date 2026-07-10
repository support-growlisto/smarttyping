namespace SmartTyping.Application.Settings;

/// <summary>
/// The applications where SmartTyping must never type on the user's behalf.
///
/// <para>Automatic expansion and layout correction work by synthesizing backspaces and characters
/// into whatever has focus. That is fine in a text field and destructive elsewhere: a game reads raw
/// key state, a terminal may run whatever lands on the command line, and a remote-desktop client
/// forwards the keystrokes to another machine entirely. In those apps we stay passive.</para>
///
/// <para>Matching is on the executable name without its extension, case-insensitively, so
/// <c>C:\Windows\System32\cmd.exe</c> matches the entry <c>cmd</c>.</para>
/// </summary>
public sealed class AppBlocklist
{
    /// <summary>
    /// Sensible defaults: remote sessions and virtual machines (keystrokes go to another computer),
    /// terminals (a stray character can execute something), and games (they read raw key state and
    /// synthetic input is often treated as cheating).
    /// </summary>
    public static readonly IReadOnlyList<string> Defaults = new[]
    {
        // Remote sessions and VMs — our keystrokes are forwarded elsewhere.
        "mstsc", "vmconnect", "vmware", "virtualbox", "anydesk", "teamviewer", "rustdesk",
        // Terminals and shells.
        "cmd", "powershell", "pwsh", "windowsterminal", "wt", "conhost", "putty", "mintty",
        // Password managers — never touch a vault field.
        "keepass", "keepassxc", "1password", "bitwarden",
        // Games / launchers that read raw input.
        "steam", "steamwebhelper"
    };

    private readonly HashSet<string> _blocked;

    public AppBlocklist(IEnumerable<string> processNames)
    {
        _blocked = processNames
            .Select(Normalize)
            .Where(name => name.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The blocked entries, normalized and sorted — what the settings UI shows.</summary>
    public IReadOnlyList<string> Entries => _blocked.OrderBy(n => n, StringComparer.Ordinal).ToList();

    /// <summary>
    /// True when <paramref name="processName"/> (with or without a path and <c>.exe</c>) is blocked.
    /// A null or empty name is never blocked — we must not disable ourselves just because the
    /// foreground process could not be identified.
    /// </summary>
    public bool IsBlocked(string? processName) =>
        !string.IsNullOrWhiteSpace(processName) && _blocked.Contains(Normalize(processName));

    /// <summary>Parses the comma/newline separated list used for persistence.</summary>
    public static AppBlocklist Parse(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? new AppBlocklist(Defaults)
            : new AppBlocklist(raw.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries));

    /// <summary>The persisted form. Round-trips through <see cref="Parse"/>.</summary>
    public override string ToString() => string.Join(", ", Entries);

    private static string Normalize(string name)
    {
        var trimmed = name.Trim();

        // Accept a full path or a bare name, with or without the extension.
        var slash = trimmed.LastIndexOfAny(['\\', '/']);
        if (slash >= 0)
        {
            trimmed = trimmed[(slash + 1)..];
        }

        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^4];
        }

        return trimmed.Trim();
    }
}
