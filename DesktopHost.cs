using System.Runtime.InteropServices;

namespace ThinkTime;

internal static class DesktopHost
{
    private const uint SpawnWorker = 0x052C;
    private const uint SmtoNormal = 0x0000;

    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr FindWindow(string? cls, string? title);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? cls, string? title);
    [DllImport("user32.dll")] private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll", SetLastError = true)] private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    private const int GwlStyle = -16;
    private const int WsChild = 0x40000000;
    private const int WsPopup = unchecked((int)0x80000000);
    private const uint SwpShowWindow = 0x0040;

    public static bool TryAttach(IntPtr window)
    {
        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return false;
        SendMessageTimeout(progman, SpawnWorker, IntPtr.Zero, IntPtr.Zero, SmtoNormal, 1000, out _);

        IntPtr worker = IntPtr.Zero;
        EnumWindows((top, _) =>
        {
            var shellView = FindWindowEx(top, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                worker = FindWindowEx(IntPtr.Zero, top, "WorkerW", null);
                return false;
            }
            return true;
        }, IntPtr.Zero);

        if (worker == IntPtr.Zero) worker = progman;
        var style = GetWindowLong(window, GwlStyle);
        SetWindowLong(window, GwlStyle, (style | WsChild) & ~WsPopup);
        SetParent(window, worker);
        if (GetParent(window) != worker) return false;
        if (!GetClientRect(worker, out var bounds)) return false;
        return SetWindowPos(window, IntPtr.Zero, 0, 0,
            bounds.Right - bounds.Left, bounds.Bottom - bounds.Top, SwpShowWindow);
    }
}
