using System.Windows;

namespace ThinkTime;

public partial class App : System.Windows.Application
{
    private MainWindow? _window;
    private System.Windows.Forms.NotifyIcon? _tray;
    private System.Windows.Forms.ToolStripItem? _toggleMenuItem;
    private System.Windows.Forms.ToolStripItem? _wallpaperMenuItem;
    private System.Windows.Forms.ToolStripItem? _exitMenuItem;

    protected override void OnStartup(StartupEventArgs e)
    {
        Trace("OnStartup entered");
        base.OnStartup(e);
        Trace("Arguments: " + string.Join(" | ", Environment.GetCommandLineArgs()));
        _window = new MainWindow();
        _window.Show();
        var launchArguments = Environment.GetCommandLineArgs().Skip(1);
        if (launchArguments.Any(arg => arg.Equals("--wallpaper", StringComparison.OrdinalIgnoreCase)))
            _window.AttachToDesktop();

        _tray = new System.Windows.Forms.NotifyIcon
        {
            Text = "ThinkTime",
            Icon = LoadAppIcon(),
            Visible = true
        };
        var menu = new System.Windows.Forms.ContextMenuStrip();
        _toggleMenuItem = menu.Items.Add(string.Empty, null, (_, _) => Dispatcher.Invoke(ToggleWindow));
        _wallpaperMenuItem = menu.Items.Add(string.Empty, null,
            (_, _) => Dispatcher.Invoke(() => _window?.AttachToDesktop()));
        _exitMenuItem = menu.Items.Add(string.Empty, null, (_, _) => Dispatcher.Invoke(ExitApp));
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ToggleWindow);
        _window.LanguageChanged += UpdateTrayLanguage;
        UpdateTrayLanguage();
    }

    internal static void Trace(string message)
    {
        try
        {
            var directory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ThinkTime");
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.AppendAllText(System.IO.Path.Combine(directory, "diagnostic.log"),
                $"{DateTime.Now:O}  {message}{Environment.NewLine}");
        }
        catch { }
    }

    private static System.Drawing.Icon LoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/ThinkTime.ico", UriKind.Absolute);
            var stream = GetResourceStream(uri)?.Stream;
            return stream is null ? System.Drawing.SystemIcons.Application : new System.Drawing.Icon(stream);
        }
        catch { return System.Drawing.SystemIcons.Application; }
    }

    private void ToggleWindow()
    {
        if (_window is null) return;
        if (_window.IsVisible) _window.Hide();
        else { _window.Show(); _window.Activate(); }
    }

    private void UpdateTrayLanguage()
    {
        var language = _window?.CurrentLanguage ?? Localization.SimplifiedChinese;
        if (_toggleMenuItem is not null) _toggleMenuItem.Text = Localization.Text(language, "TrayToggle");
        if (_wallpaperMenuItem is not null) _wallpaperMenuItem.Text = Localization.Text(language, "TrayWallpaper");
        if (_exitMenuItem is not null) _exitMenuItem.Text = Localization.Text(language, "TrayExit");
    }

    private void ExitApp()
    {
        _window?.StopServices();
        _tray?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}
