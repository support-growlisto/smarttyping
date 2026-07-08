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
                dwFlags = isUp ? NativeMethods.KEYEVENTF_KEYUP : 0
            }
        }
    };
}
