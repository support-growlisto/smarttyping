namespace SmartTyping.Application.Snippets;

/// <summary>
/// Subsequence fuzzy matcher (fzf/Sublime-style). A query matches if its characters appear in
/// order within the candidate (not necessarily contiguously). The score rewards contiguous runs,
/// matches at the start, matches right after a separator (<c>/ _ - space</c>) or a camelCase
/// boundary, and exact-case hits; earlier overall matches score higher.
/// </summary>
public static class FuzzyMatcher
{
    /// <summary>
    /// Tries to match <paramref name="query"/> against <paramref name="text"/>. Returns true with a
    /// score (higher is better) when every query character is found in order.
    /// </summary>
    public static bool TryMatch(string text, string query, out int score)
    {
        score = 0;
        if (string.IsNullOrEmpty(query))
        {
            return true;
        }

        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var qi = 0;
        var streak = 0;
        var firstMatch = -1;

        for (var ti = 0; ti < text.Length && qi < query.Length; ti++)
        {
            if (char.ToLowerInvariant(text[ti]) != char.ToLowerInvariant(query[qi]))
            {
                streak = 0;
                continue;
            }

            if (firstMatch < 0)
            {
                firstMatch = ti;
            }

            var bonus = 1;
            streak++;
            bonus += streak * 2; // reward contiguous runs

            if (ti == 0)
            {
                bonus += 10; // match at the very start
            }
            else
            {
                var prev = text[ti - 1];
                if (prev is '/' or ' ' or '_' or '-' or '.')
                {
                    bonus += 7; // match right after a separator (start of a "word")
                }
                else if (char.IsUpper(text[ti]) && char.IsLower(prev))
                {
                    bonus += 5; // camelCase boundary
                }
            }

            if (text[ti] == query[qi])
            {
                bonus += 1; // exact-case hit
            }

            score += bonus;
            qi++;
        }

        if (qi < query.Length)
        {
            score = 0;
            return false; // not all query characters matched
        }

        score -= firstMatch; // earlier first match is better
        return true;
    }
}
