using System.Runtime.InteropServices;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Sends synthetic key combinations via <c>SendInput</c>. Infrastructure-internal helper shared
/// by the text injector and selection service.
/// </summary>
internal static class KeyboardSender
{
    /// <summary>Sends Ctrl + the given virtual-key (down/up), e.g. Ctrl+C or Ctrl+V.</summary>
    public static void SendCtrl(int vk)
    {
        var inputs = new[]
        {
            KeyDown(NativeMethods.VK_CONTROL),
            KeyDown(vk),
            KeyUp(vk),
            KeyUp(NativeMethods.VK_CONTROL),
        };

        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    /// <summary>Sends Ctrl + Shift + the given virtual-key (e.g. Ctrl+Shift+Left to select the previous word).</summary>
    public static void SendCtrlShift(int vk)
    {
        var inputs = new[]
        {
            KeyDown(NativeMethods.VK_CONTROL),
            KeyDown(NativeMethods.VK_SHIFT),
            KeyDown(vk),
            KeyUp(vk),
            KeyUp(NativeMethods.VK_SHIFT),
            KeyUp(NativeMethods.VK_CONTROL),
        };

        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    /// <summary>
    /// Types <paramref name="text"/> directly as Unicode keystrokes (<c>KEYEVENTF_UNICODE</c>), without
    /// touching the clipboard. Used for inline auto-replacement, where a clipboard paste races its own
    /// save/restore. Newlines are sent as Enter so multi-line snippets break correctly.
    /// </summary>
    public static void SendUnicode(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var run = new List<NativeMethods.INPUT>(text.Length * 2);

        void Flush()
        {
            if (run.Count == 0)
            {
                return;
            }

            var batch = run.ToArray();
            NativeMethods.SendInput((uint)batch.Length, batch, Marshal.SizeOf<NativeMethods.INPUT>());
            run.Clear();
        }

        foreach (var ch in text)
        {
            if (ch == '\r')
            {
                continue; // the '\n' below emits the line break
            }

            if (ch == '\n')
            {
                Flush();
                TapKey(NativeMethods.VK_RETURN, 1);
                continue;
            }

            run.Add(UnicodeEvent(ch, isUp: false));
            run.Add(UnicodeEvent(ch, isUp: true));
        }

        Flush();
    }

    private static NativeMethods.INPUT UnicodeEvent(char ch, bool isUp) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = 0,
                wScan = ch,
                dwFlags = NativeMethods.KEYEVENTF_UNICODE | (isUp ? NativeMethods.KEYEVENTF_KEYUP : 0),
                dwExtraInfo = NativeMethods.SelfInjectedTag
            }
        }
    };

    /// <summary>
    /// Performs a whole inline replacement — backspaces, the replacement text, and any caret-restoring
    /// Left taps — in a <b>single</b> <c>SendInput</c> call. Doing it atomically matters: the user keeps
    /// typing while we replace, and any gap between calls lets their next keystroke interleave into the
    /// middle of the edit (which corrupts the text at normal typing speeds).
    /// </summary>
    public static void ReplaceInline(int backspaces, string text, int leftTaps)
    {
        var events = new List<NativeMethods.INPUT>((backspaces + text.Length + leftTaps) * 2);

        for (var i = 0; i < backspaces; i++)
        {
            events.Add(KeyDown(NativeMethods.VK_BACK));
            events.Add(KeyUp(NativeMethods.VK_BACK));
        }

        foreach (var ch in text)
        {
            if (ch == '\r')
            {
                continue; // the '\n' below emits the line break
            }

            if (ch == '\n')
            {
                events.Add(KeyDown(NativeMethods.VK_RETURN));
                events.Add(KeyUp(NativeMethods.VK_RETURN));
                continue;
            }

            events.Add(UnicodeEvent(ch, isUp: false));
            events.Add(UnicodeEvent(ch, isUp: true));
        }

        for (var i = 0; i < leftTaps; i++)
        {
            events.Add(KeyDown(NativeMethods.VK_LEFT));
            events.Add(KeyUp(NativeMethods.VK_LEFT));
        }

        if (events.Count == 0)
        {
            return;
        }

        var batch = events.ToArray();
        NativeMethods.SendInput((uint)batch.Length, batch, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    /// <summary>
    /// How many Left taps walk the caret from the end of <paramref name="text"/> back to
    /// <paramref name="cursorOffset"/> (a snippet's <c>{cursor}</c> marker), or 0 when there is no marker.
    ///
    /// <para>A line break costs <b>one</b> tap, not two. It is stored as CRLF — two characters — which is
    /// why this used to count it twice, and why the caret of a multi-line snippet landed one place too far
    /// back for every line break: at the end of the previous line instead of on the empty one. But Left is
    /// a <i>caret</i> movement, and the caret steps over a line break in a single press. Measured against a
    /// real text box, after the arithmetic said otherwise.</para>
    /// </summary>
    public static int LeftTapsFor(string text, int? cursorOffset)
    {
        if (cursorOffset is not int offset || offset < 0 || offset >= text.Length)
        {
            return 0;
        }

        var taps = 0;
        for (var i = offset; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                continue; // never typed: the '\n' is what we send, as Enter
            }

            taps++;
        }

        return taps;
    }

    /// <summary>Taps a virtual-key <paramref name="count"/> times (e.g. VK_LEFT to move the caret back).</summary>
    public static void TapKey(int vk, int count)
    {
        if (count <= 0)
        {
            return;
        }

        var inputs = new NativeMethods.INPUT[count * 2];
        for (var i = 0; i < count; i++)
        {
            inputs[i * 2] = KeyDown(vk);
            inputs[i * 2 + 1] = KeyUp(vk);
        }

        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static NativeMethods.INPUT KeyDown(int vk) => KeyEvent((ushort)vk, isUp: false);

    private static NativeMethods.INPUT KeyUp(int vk) => KeyEvent((ushort)vk, isUp: true);

    private static NativeMethods.INPUT KeyEvent(ushort vk, bool isUp)
    {
        // Arrow and navigation keys must be flagged extended, or Windows delivers them as their numpad
        // twins: Ctrl+Shift+Left then arrives as Ctrl+Shift+Numpad4 and selects nothing.
        var flags = isUp ? NativeMethods.KEYEVENTF_KEYUP : 0;
        if (NativeMethods.IsExtendedKey(vk))
        {
            flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
        }

        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = vk,
                    dwFlags = flags,
                    dwExtraInfo = NativeMethods.SelfInjectedTag
                }
            }
        };
    }
}
