using System.Runtime.InteropServices;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// P/Invoke declarations for the Win32 input APIs used by the infrastructure layer.
/// Isolated here so no Windows API surface leaks into other layers.
/// </summary>
internal static class NativeMethods
{
    public const int WH_KEYBOARD_LL = 13;
    public const int WH_MOUSE_LL = 14;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_SYSKEYDOWN = 0x0104;

    // Mouse button-down messages — used to invalidate the as-you-type word buffer when the user
    // clicks (which moves the caret without any keystroke passing through the keyboard hook).
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_MBUTTONDOWN = 0x0207;

    // Virtual-key codes
    public const int VK_CONTROL = 0x11;
    public const int VK_SHIFT = 0x10;
    public const int VK_MENU = 0x12;   // Alt
    public const int VK_LWIN = 0x5B;
    public const int VK_RWIN = 0x5C;
    public const int VK_L = 0x4C;
    public const int VK_V = 0x56;
    public const int VK_C = 0x43;
    public const int VK_E = 0x45;
    public const int VK_LEFT = 0x25;
    public const int VK_SPACE = 0x20;
    public const int VK_N = 0x4E;

    // Keys used by the as-you-type layout-suggestion word buffer.
    public const int VK_BACK = 0x08;
    public const int VK_RETURN = 0x0D;
    public const int VK_TAB = 0x09;
    public const int VK_OEM_1 = 0xBA;      // ;
    public const int VK_OEM_2 = 0xBF;      // /
    public const int VK_OEM_4 = 0xDB;      // [
    public const int VK_OEM_5 = 0xDC;      // \
    public const int VK_OEM_6 = 0xDD;      // ]
    public const int VK_OEM_7 = 0xDE;      // '
    public const int VK_OEM_COMMA = 0xBC;  // ,
    public const int VK_OEM_PERIOD = 0xBE; // .
    public const int VK_OEM_PLUS = 0xBB;   // =
    public const int VK_OEM_MINUS = 0xBD;  // -
    public const int VK_OEM_3 = 0xC0;      // `

    public const int LANG_THAI = 0x1E;

    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_UNICODE = 0x0004;

    // Window style flags for secure-field detection.
    public const int GWL_STYLE = -16;
    public const int ES_PASSWORD = 0x0020;

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    // The union MUST contain the largest member (MOUSEINPUT) so Marshal.SizeOf<INPUT> equals the
    // size the OS expects (40 bytes on x64). If only KEYBDINPUT were present the struct would be too
    // small and SendInput would fail silently (cbSize mismatch), injecting nothing.
    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GUITHREADINFO
    {
        public int cbSize;
        public uint flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT rcCaret;
    }

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    /// <summary>True if the foreground window's active keyboard layout is Thai.</summary>
    public static bool ForegroundLayoutIsThai()
    {
        var fg = GetForegroundWindow();
        var thread = GetWindowThreadProcessId(fg, out _);
        var layout = GetKeyboardLayout(thread).ToInt64();
        return (layout & 0x3FF) == LANG_THAI;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    // 64-bit safe: routed to GetWindowLongPtr on 64-bit, GetWindowLong on 32-bit.
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    public static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    public static IntPtr GetWindowLongStyle(IntPtr hWnd)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, GWL_STYLE)
            : new IntPtr(GetWindowLong32(hWnd, GWL_STYLE));
    }
}
