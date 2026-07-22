using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ThinkTime;

internal static class WindowEffects
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        var enabled = 1;
        DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int));
        var rounded = 2;
        DwmSetWindowAttribute(handle, 33, ref rounded, sizeof(int));
    }
}
