using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OoWinConfig.Services;

public static class WindowChromeService
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    public static void TryEnableDarkTitleBar(Window window)
    {
        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            var enabled = 1;
            _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref enabled, Marshal.SizeOf<int>());
        }
        catch
        {
            // Older Windows builds may not support dark title bars.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);
}
