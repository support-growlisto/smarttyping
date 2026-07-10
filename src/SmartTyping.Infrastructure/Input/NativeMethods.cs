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

    // Focus and foreground changes. Watching them lets us inspect the caret's surroundings as soon as
    // it moves, rather than when the user's next keystroke arrives — by then it is too late to decide
    // whether that keystroke may attach to the character in front of it.
    public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    public const uint EVENT_OBJECT_FOCUS = 0x8005;
    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

    public delegate void WinEventProc(
        IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(
        uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc,
        uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    // Mouse button-down messages — used to invalidate the as-you-type word buffer when the user
    // clicks (which moves the caret without any keystroke passing through the keyboard hook).
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
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
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_UNICODE = 0x0004;

    /// <summary>
    /// The virtual-keys that live on the "extended" part of the keyboard (the arrow/navigation cluster,
    /// among others). Sent through <c>SendInput</c> without <see cref="KEYEVENTF_EXTENDEDKEY"/> they are
    /// delivered as their numeric-keypad twins — a bare Left still moves the caret, but Ctrl+Shift+Left
    /// arrives as Ctrl+Shift+Numpad4 and selects nothing.
    /// </summary>
    public static bool IsExtendedKey(int vk) => vk is
        0x21 or 0x22 or 0x23 or 0x24 or       // PgUp, PgDn, End, Home
        0x25 or 0x26 or 0x27 or 0x28 or       // Left, Up, Right, Down
        0x2D or 0x2E or                       // Insert, Delete
        0x90 or 0xA3 or 0xA5;                 // NumLock, RControl, RAlt

    /// <summary>
    /// Stamped into <c>dwExtraInfo</c> on every keystroke this app synthesizes. The low-level hook
    /// ignores events carrying it, so text we type back (a snippet expansion, a layout correction)
    /// can never re-enter the word buffer or re-trigger a hotkey. Without this, the app corrects and
    /// expands its own output.
    /// </summary>
    public static readonly IntPtr SelfInjectedTag = new(0x53547970); // 'STyp'

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

    public const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;

    // Just enough rights to read the image name; PROCESS_QUERY_LIMITED_INFORMATION works for
    // elevated processes too, where PROCESS_QUERY_INFORMATION would be denied.
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryFullProcessImageName(IntPtr process, uint flags, System.Text.StringBuilder exeName, ref uint size);

    [DllImport("user32.dll")]
    public static extern uint GetKeyboardLayoutList(int nBuff, [Out] IntPtr[]? lpList);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public const int LANG_ENGLISH = 0x09;

    /// <summary>The installed keyboard layout for <paramref name="primaryLanguage"/>, or zero.</summary>
    public static IntPtr FindLayout(int primaryLanguage)
    {
        var count = GetKeyboardLayoutList(0, null);
        if (count == 0)
        {
            return IntPtr.Zero;
        }

        var list = new IntPtr[count];
        GetKeyboardLayoutList((int)count, list);
        foreach (var hkl in list)
        {
            if ((hkl.ToInt64() & 0x3FF) == primaryLanguage)
            {
                return hkl;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>The installed Thai keyboard layout, or <see cref="IntPtr.Zero"/> if none.</summary>
    public static IntPtr FindThaiLayout() => FindLayout(LANG_THAI);

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
