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

    private static NativeMethods.INPUT KeyEvent(ushort vk, bool isUp) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = vk,
                dwFlags = isUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
                dwExtraInfo = NativeMethods.SelfInjectedTag
            }
        }
    };
}
