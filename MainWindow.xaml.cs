using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ThinkTime;

public partial class MainWindow : Window
{
    private bool _loadingSettings;
    private readonly AppSettings _settings;
    private readonly WallpaperService _wallpaperService = new();
    private readonly DispatcherTimer _headerTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _settingsOpen;
    public event Action? LanguageChanged;
    public string CurrentLanguage => _settings.Language;

    public MainWindow()
    {
        InitializeComponent();
        _settings = SettingsStore.Load();
        _settings.Language = Localization.NormalizeLanguage(_settings.Language);
        LoadSettingsUi();
        ApplyLanguage();
        WorldView.CityHovered += text => StatusText.Text = text ?? Localization.Text(_settings.Language, "StatusDefault");
        _headerTimer.Tick += (_, _) => HeaderTime.Text = DateTime.Now.ToString("yyyy-MM-dd  /  HH:mm:ss");
        _headerTimer.Start();
        HeaderTime.Text = DateTime.Now.ToString("yyyy-MM-dd  /  HH:mm:ss");
    }

    public void AttachToDesktop()
    {
        if (_wallpaperService.IsRunning)
        {
            if (_wallpaperService.Refresh()) StatusText.Text = Localization.Text(_settings.Language, "StatusRefreshed");
            return;
        }
        if (_wallpaperService.Start(_settings))
        {
            DesktopButton.Content = Localization.Text(_settings.Language, "RefreshWallpaper");
            StatusText.Text = Localization.Text(_settings.Language, "StatusActive");
            Hide();
        }
        else StatusText.Text = $"{Localization.Text(_settings.Language, "StatusFailed")}  /  {_wallpaperService.LastError}";
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }

    private void DesktopButton_Click(object sender, RoutedEventArgs e) => AttachToDesktop();
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsOpen = !_settingsOpen;
        var slide = (TranslateTransform)SettingsPanel.RenderTransform;
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        if (_settingsOpen)
        {
            SettingsPanel.Visibility = Visibility.Visible;
            SettingsPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170)) { EasingFunction = easing });
            slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(190)) { EasingFunction = easing });
        }
        else
        {
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));
            fade.Completed += (_, _) => SettingsPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.BeginAnimation(OpacityProperty, fade);
            slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, 14, TimeSpan.FromMilliseconds(120)));
        }
    }
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left &&
            FindAncestor<System.Windows.Controls.Button>(e.OriginalSource as DependencyObject) is null) DragMove();
    }
    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match) return match;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }
    private void Window_SourceInitialized(object? sender, EventArgs e) => WindowEffects.Apply(this);
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    private void LoadSettingsUi()
    {
        _loadingSettings = true;
        LanguageCombo.ItemsSource = Localization.Languages;
        LanguageCombo.DisplayMemberPath = nameof(LocalizedOption.Label);
        LanguageCombo.SelectedValuePath = nameof(LocalizedOption.Value);
        LanguageCombo.SelectedValue = _settings.Language;
        Clock24Check.IsChecked = _settings.Use24HourClock;
        TimeZonesCheck.IsChecked = _settings.ShowTimeZones;
        LabelsCheck.IsChecked = _settings.ShowCityLabels;
        LowPowerCheck.IsChecked = _settings.LowPowerMode;
        _loadingSettings = false;
    }

    private void ApplyLanguage()
    {
        var language = _settings.Language;
        HeaderSubtitle.Text = Localization.Text(language, "HeaderSubtitle");
        OnlineText.Text = Localization.Text(language, "Online");
        SettingsButton.ToolTip = Localization.Text(language, "SettingsTooltip");
        DesktopButton.Content = Localization.Text(language,
            _wallpaperService.IsRunning ? "RefreshWallpaper" : "StartWallpaper");
        StatusText.Text = _wallpaperService.IsRunning
            ? Localization.Text(language, "StatusActive")
            : Localization.Text(language, "StatusDefault");
        DataSourceText.Text = Localization.Text(language, "DataSource");
        SettingsTitleText.Text = Localization.Text(language, "SettingsTitle");
        SettingsSubtitleText.Text = Localization.Text(language, "SettingsSubtitle");
        LanguageSectionText.Text = Localization.Text(language, "LanguageSection");
        AboutSectionText.Text = Localization.Text(language, "AboutSection");
        AboutTitleText.Text = Localization.Text(language, "AboutTitle");
        AboutBodyText.Text = Localization.Text(language, "AboutBody");
        TutorialSectionText.Text = Localization.Text(language, "TutorialSection");
        Step1TitleText.Text = Localization.Text(language, "Step1Title");
        Step1BodyText.Text = Localization.Text(language, "Step1Body");
        Step2TitleText.Text = Localization.Text(language, "Step2Title");
        Step2BodyText.Text = Localization.Text(language, "Step2Body");
        Step3TitleText.Text = Localization.Text(language, "Step3Title");
        Step3BodyText.Text = Localization.Text(language, "Step3Body");
        DisplaySectionText.Text = Localization.Text(language, "DisplaySection");
        Clock24Check.Content = Localization.Text(language, "Clock24");
        TimeZonesCheck.Content = Localization.Text(language, "TimeZones");
        LabelsCheck.Content = Localization.Text(language, "CityLabels");
        LowPowerCheck.Content = Localization.Text(language, "LowPower");
        AccentCitySectionText.Text = Localization.Text(language, "AccentCity");
        RefreshIntervalText.Text = Localization.Text(language, "RefreshInterval");
        RefreshIntervalValueText.Text = Localization.Text(language, "SixtySeconds");

        var wasLoading = _loadingSettings;
        _loadingSettings = true;
        AccentCityCombo.ItemsSource = Localization.CityOptions(WorldClockView.CityNames, language);
        AccentCityCombo.DisplayMemberPath = nameof(LocalizedOption.Label);
        AccentCityCombo.SelectedValuePath = nameof(LocalizedOption.Value);
        AccentCityCombo.SelectedValue = _settings.AccentCity;
        _loadingSettings = wasLoading;
        WorldView.ApplySettings(_settings);
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettings || LanguageCombo.SelectedValue is not string language) return;
        _settings.Language = Localization.NormalizeLanguage(language);
        ApplyLanguage();
        SettingsStore.Save(_settings);
        _wallpaperService.UpdateSettings(_settings);
        LanguageChanged?.Invoke();
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings || AccentCityCombo is null) return;
        _settings.Use24HourClock = Clock24Check.IsChecked == true;
        _settings.ShowTimeZones = TimeZonesCheck.IsChecked == true;
        _settings.ShowCityLabels = LabelsCheck.IsChecked == true;
        _settings.LowPowerMode = LowPowerCheck.IsChecked == true;
        _settings.AccentCity = AccentCityCombo.SelectedValue as string ?? "NEW YORK";
        WorldView.ApplySettings(_settings);
        SettingsStore.Save(_settings);
        _wallpaperService.UpdateSettings(_settings);
    }

    public void StopServices()
    {
        _headerTimer.Stop();
        _wallpaperService.Dispose();
    }
}
