using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Size = System.Windows.Size;
using Rect = System.Windows.Rect;

namespace ThinkTime;

internal sealed class WallpaperService : IDisposable
{
    private const uint SpiSetDesktopWallpaper = 0x0014;
    private const uint SpifUpdateIniFile = 0x0001;
    private const uint SpifSendChange = 0x0002;
    private readonly DispatcherTimer _refreshTimer;
    private AppSettings? _settings;
    private bool _alternateFile;
    public bool IsRunning { get; private set; }
    public string? LastError { get; private set; }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint parameter, string value, uint flags);

    public WallpaperService()
    {
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _refreshTimer.Tick += (_, _) => Refresh();
    }

    public bool Start(AppSettings settings)
    {
        _settings = settings;
        if (!Refresh()) return false;
        IsRunning = true;
        _refreshTimer.Start();
        return true;
    }

    public bool Refresh()
    {
        try
        {
            App.Trace("Wallpaper refresh started");
            if (_settings is null) return false;
            var bounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds
                ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
            var width = Math.Max(1280, bounds.Width);
            var height = Math.Max(720, bounds.Height);
            App.Trace($"Render size {width}x{height}");
            var view = new WorldClockView();
            view.ApplySettings(_settings);
            view.Measure(new Size(width, height));
            view.Arrange(new Rect(0, 0, width, height));
            view.UpdateLayout();

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(view);
            bitmap.Freeze();
            App.Trace("World view rendered");

            var directory = GetWritableRuntimeDirectory();
            var temporary = Path.Combine(directory, "wallpaper-next.png");
            _alternateFile = !_alternateFile;
            var wallpaper = Path.Combine(directory, _alternateFile ? "wallpaper-a.png" : "wallpaper-b.png");
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(temporary)) encoder.Save(stream);
            App.Trace("Temporary PNG saved");
            File.Copy(temporary, wallpaper, true);
            File.Delete(temporary);

            ApplyWallpaper(wallpaper);
            LastError = null;
            App.Trace("Windows wallpaper updated");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            App.Trace("Wallpaper refresh failed: " + ex);
            try
            {
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ThinkTime");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "wallpaper-error.log"), ex.ToString());
            }
            catch { }
            return false;
        }
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        if (IsRunning) Refresh();
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        IsRunning = false;
    }

    private static string GetWritableRuntimeDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ThinkTime"),
            Path.Combine(AppContext.BaseDirectory, "Runtime")
        };
        foreach (var candidate in candidates)
        {
            try
            {
                Directory.CreateDirectory(candidate);
                var probe = Path.Combine(candidate, ".write-test");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return candidate;
            }
            catch { }
        }
        throw new UnauthorizedAccessException("ThinkTime cannot find a writable wallpaper output directory.");
    }

    private static void ApplyWallpaper(string wallpaper)
    {
        object? instance = null;
        try
        {
            var type = Type.GetTypeFromCLSID(new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD"), true)!;
            instance = Activator.CreateInstance(type);
            var desktop = (IDesktopWallpaper)instance!;
            desktop.SetPosition(DesktopWallpaperPosition.Fill);
            var count = desktop.GetMonitorDevicePathCount();
            if (count == 0) desktop.SetWallpaper(null, wallpaper);
            else
                for (uint i = 0; i < count; i++)
                    desktop.SetWallpaper(desktop.GetMonitorDevicePathAt(i), wallpaper);
            return;
        }
        catch
        {
            if (!SystemParametersInfo(SpiSetDesktopWallpaper, 0, wallpaper,
                    SpifUpdateIniFile | SpifSendChange))
                throw new InvalidOperationException($"Windows wallpaper error {Marshal.GetLastWin32Error()}");
        }
        finally
        {
            if (instance is not null && Marshal.IsComObject(instance)) Marshal.FinalReleaseComObject(instance);
        }
    }

    private enum DesktopWallpaperPosition
    {
        Center, Tile, Stretch, Fit, Fill, Span
    }

    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId,
            [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)] string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId);
        [return: MarshalAs(UnmanagedType.LPWStr)] string GetMonitorDevicePathAt(uint monitorIndex);
        uint GetMonitorDevicePathCount();
        void GetMonitorRect([MarshalAs(UnmanagedType.LPWStr)] string monitorId, out NativeRect displayRect);
        void SetBackgroundColor(uint color);
        uint GetBackgroundColor();
        void SetPosition(DesktopWallpaperPosition position);
        DesktopWallpaperPosition GetPosition();
        void SetSlideshow(IntPtr items);
        IntPtr GetSlideshow();
        void SetSlideshowOptions(uint options, uint slideshowTick);
        void GetSlideshowOptions(out uint options, out uint slideshowTick);
        void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorId, uint direction);
        uint GetStatus();
        void Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
}
