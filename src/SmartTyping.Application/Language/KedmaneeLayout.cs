namespace SmartTyping.Application.Language;

/// <summary>
/// The Thai Kedmanee ↔ US-QWERTY key mapping table. Each entry maps a US-QWERTY character
/// (the physical key, unshifted or shifted) to the Thai character produced on the Kedmanee
/// layout by that same key.
///
/// Pure constant data — the single source of truth for the converter. Both the unshifted and
/// shifted rows are included so symbols and the second Thai character set are covered.
/// </summary>
public static class KedmaneeLayout
{
    /// <summary>
    /// English (QWERTY) → Thai (Kedmanee). Keys are single US characters; values are the Thai
    /// characters. This drives EnglishToThai; the reverse map is derived from it.
    /// </summary>
    public static readonly IReadOnlyDictionary<char, char> EnglishToThai = new Dictionary<char, char>
    {
        // Number row (unshifted): the Thai layout puts letters/tone marks here.
        ['1'] = 'ๅ', ['2'] = '/', ['3'] = '-', ['4'] = 'ภ', ['5'] = 'ถ',
        ['6'] = 'ุ', ['7'] = 'ึ', ['8'] = 'ค', ['9'] = 'ต', ['0'] = 'จ',
        ['-'] = 'ข', ['='] = 'ช',

        // Top letter row (unshifted)
        ['q'] = 'ๆ', ['w'] = 'ไ', ['e'] = 'ำ', ['r'] = 'พ', ['t'] = 'ะ',
        ['y'] = 'ั', ['u'] = 'ี', ['i'] = 'ร', ['o'] = 'น', ['p'] = 'ย',
        ['['] = 'บ', [']'] = 'ล', ['\\'] = 'ฃ',

        // Home row (unshifted)
        ['a'] = 'ฟ', ['s'] = 'ห', ['d'] = 'ก', ['f'] = 'ด', ['g'] = 'เ',
        ['h'] = '้', ['j'] = '่', ['k'] = 'า', ['l'] = 'ส', [';'] = 'ว',
        ['\''] = 'ง',

        // Bottom row (unshifted)
        ['z'] = 'ผ', ['x'] = 'ป', ['c'] = 'แ', ['v'] = 'อ', ['b'] = 'ิ',
        ['n'] = 'ื', ['m'] = 'ท', [','] = 'ม', ['.'] = 'ใ', ['/'] = 'ฝ',

        // Number row (shifted)
        ['!'] = '+', ['@'] = '๑', ['#'] = '๒', ['$'] = '๓', ['%'] = '๔',
        ['^'] = 'ู', ['&'] = '฿', ['*'] = '๕', ['('] = '๖', [')'] = '๗',
        ['_'] = '๘', ['+'] = '๙',

        // Top letter row (shifted)
        ['Q'] = '๐', ['W'] = '"', ['E'] = 'ฎ', ['R'] = 'ฑ', ['T'] = 'ธ',
        ['Y'] = 'ํ', ['U'] = '๊', ['I'] = 'ณ', ['O'] = 'ฯ', ['P'] = 'ญ',
        ['{'] = 'ฐ', ['}'] = ',', ['|'] = 'ฅ',

        // Home row (shifted)
        ['A'] = 'ฤ', ['S'] = 'ฆ', ['D'] = 'ฏ', ['F'] = 'โ', ['G'] = 'ฌ',
        ['H'] = '็', ['J'] = '๋', ['K'] = 'ษ', ['L'] = 'ศ', [':'] = 'ซ',
        ['"'] = '.',

        // Bottom row (shifted)
        ['Z'] = '(', ['X'] = ')', ['C'] = 'ฉ', ['V'] = 'ฮ', ['B'] = 'ฺ',
        ['N'] = '์', ['M'] = '?', ['<'] = 'ฒ', ['>'] = 'ฬ', ['?'] = 'ฦ',
    };

    /// <summary>
    /// Thai (Kedmanee) → English (QWERTY). Derived by inverting <see cref="EnglishToThai"/>.
    /// If two English keys ever mapped to the same Thai character, the first wins (kept stable,
    /// insertion order of the dictionary above).
    /// </summary>
    public static readonly IReadOnlyDictionary<char, char> ThaiToEnglish = BuildReverse();

    private static Dictionary<char, char> BuildReverse()
    {
        var reverse = new Dictionary<char, char>();
        foreach (var (english, thai) in EnglishToThai)
        {
            reverse.TryAdd(thai, english);
        }

        return reverse;
    }
}
