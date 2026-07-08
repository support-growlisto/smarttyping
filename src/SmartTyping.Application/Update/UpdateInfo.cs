namespace SmartTyping.Application.Update;

/// <summary>Details of an available newer release.</summary>
public sealed record UpdateInfo(string Version, string DownloadUrl, string Notes);

/// <summary>Pure SemVer-ish comparison for the update check (testable without any I/O).</summary>
public static class UpdateComparer
{
    /// <summary>
    /// Returns true if <paramref name="latestTag"/> (e.g. <c>v0.2.0</c>) is a newer version than
    /// <paramref name="current"/> (e.g. <c>0.1.0</c>). A leading <c>v</c> and any pre-release/build
    /// suffix are ignored; unparsable input yields false (treat as "no update").
    /// </summary>
    public static bool IsNewer(string? current, string? latestTag)
    {
        return TryParse(current, out var cur) && TryParse(latestTag, out var latest) && latest > cur;
    }

    private static bool TryParse(string? text, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim().TrimStart('v', 'V');

        // Keep only the leading numeric-dotted portion (drops pre-release/build suffixes).
        var end = 0;
        while (end < trimmed.Length && (char.IsDigit(trimmed[end]) || trimmed[end] == '.'))
        {
            end++;
        }

        var core = trimmed[..end];
        if (!core.Contains('.'))
        {
            core += ".0";
        }

        if (!Version.TryParse(core, out var parsed))
        {
            return false;
        }

        // Treat unspecified components as 0 so "0.2" and "0.2.0.0" compare equal.
        version = new Version(parsed.Major, parsed.Minor, Math.Max(0, parsed.Build), Math.Max(0, parsed.Revision));
        return true;
    }
}
