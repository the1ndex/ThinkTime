using System.Globalization;

namespace ThinkTime;

internal sealed record LocalizedOption(string Value, string Label);

internal static class Localization
{
    public const string SimplifiedChinese = "zh-CN";
    public const string English = "en-US";

    public static IReadOnlyList<LocalizedOption> Languages { get; } =
    [
        new(SimplifiedChinese, "简体中文"),
        new(English, "English")
    ];

    private static readonly IReadOnlyDictionary<string, string> Chinese = new Dictionary<string, string>
    {
        ["HeaderSubtitle"] = "世界时钟系统  /  实时太阳模型",
        ["Online"] = "在线",
        ["SettingsTooltip"] = "设置",
        ["StartWallpaper"] = "应用动态壁纸",
        ["RefreshWallpaper"] = "刷新壁纸",
        ["StatusDefault"] = "实时  /  晨昏线  /  当地时间",
        ["DataSource"] = "NASA 夜间灯光  /  NOAA 太阳模型",
        ["SettingsTitle"] = "设置与使用",
        ["SettingsSubtitle"] = "更改会自动保存并应用",
        ["AboutSection"] = "动态壁纸",
        ["AboutTitle"] = "一张跟随昼夜变化的世界时钟",
        ["AboutBody"] = "ThinkTime 根据太阳位置绘制晨昏线，并只在夜间区域叠加 NASA Black Marble 城市灯光。壁纸每分钟更新一次，不会遮挡桌面图标或右键菜单。",
        ["TutorialSection"] = "快速开始",
        ["Step1Title"] = "应用壁纸",
        ["Step1Body"] = "点击主界面右上角的红色按钮。",
        ["Step2Title"] = "保持运行",
        ["Step2Body"] = "程序会驻留系统托盘并自动刷新。",
        ["Step3Title"] = "恢复窗口",
        ["Step3Body"] = "双击托盘图标，或使用右键菜单。",
        ["LanguageSection"] = "语言",
        ["DisplaySection"] = "显示与地图",
        ["Clock24"] = "24 小时制",
        ["TimeZones"] = "时区网格",
        ["CityLabels"] = "城市标签",
        ["LowPower"] = "低功耗更新",
        ["AccentCity"] = "高亮城市",
        ["RefreshInterval"] = "壁纸刷新间隔",
        ["SixtySeconds"] = "60 秒",
        ["StatusRefreshed"] = "动态壁纸已刷新",
        ["StatusActive"] = "动态壁纸运行中  /  60 秒后自动更新",
        ["StatusFailed"] = "壁纸应用失败",
        ["TrayToggle"] = "显示 / 隐藏",
        ["TrayWallpaper"] = "应用 / 刷新动态壁纸",
        ["TrayExit"] = "退出",
        ["SolarLive"] = "太阳位置 / 实时"
    };

    private static readonly IReadOnlyDictionary<string, string> EnglishTexts = new Dictionary<string, string>
    {
        ["HeaderSubtitle"] = "WORLD TIME SYSTEM  /  LIVE SOLAR MODEL",
        ["Online"] = "ONLINE",
        ["SettingsTooltip"] = "Settings",
        ["StartWallpaper"] = "START LIVE WALLPAPER",
        ["RefreshWallpaper"] = "REFRESH WALLPAPER",
        ["StatusDefault"] = "LIVE  /  SOLAR TERMINATOR  /  LOCAL TIME",
        ["DataSource"] = "NASA BLACK MARBLE  /  NOAA SOLAR MODEL",
        ["SettingsTitle"] = "Settings & guide",
        ["SettingsSubtitle"] = "Changes are saved and applied automatically",
        ["AboutSection"] = "LIVE WALLPAPER",
        ["AboutTitle"] = "A world clock that follows day and night",
        ["AboutBody"] = "ThinkTime draws the solar terminator and reveals NASA Black Marble city lights only on the night side. It refreshes once a minute without covering desktop icons or the context menu.",
        ["TutorialSection"] = "QUICK START",
        ["Step1Title"] = "Apply wallpaper",
        ["Step1Body"] = "Use the red button in the top-right corner.",
        ["Step2Title"] = "Keep it running",
        ["Step2Body"] = "ThinkTime stays in the tray and refreshes itself.",
        ["Step3Title"] = "Restore the window",
        ["Step3Body"] = "Double-click the tray icon or use its menu.",
        ["LanguageSection"] = "LANGUAGE",
        ["DisplaySection"] = "DISPLAY & MAP",
        ["Clock24"] = "24-hour clock",
        ["TimeZones"] = "Time-zone grid",
        ["CityLabels"] = "City labels",
        ["LowPower"] = "Low-power updates",
        ["AccentCity"] = "ACCENT CITY",
        ["RefreshInterval"] = "WALLPAPER REFRESH",
        ["SixtySeconds"] = "60 SEC",
        ["StatusRefreshed"] = "LIVE WALLPAPER REFRESHED",
        ["StatusActive"] = "LIVE WALLPAPER ACTIVE  /  NEXT UPDATE IN 60 SECONDS",
        ["StatusFailed"] = "WALLPAPER FAILED",
        ["TrayToggle"] = "Show / Hide",
        ["TrayWallpaper"] = "Apply / Refresh live wallpaper",
        ["TrayExit"] = "Exit",
        ["SolarLive"] = "SOLAR POSITION / LIVE"
    };

    private static readonly IReadOnlyDictionary<string, string> ChineseCities = new Dictionary<string, string>
    {
        ["SAN FRANCISCO"] = "旧金山",
        ["NEW YORK"] = "纽约",
        ["LONDON"] = "伦敦",
        ["BERLIN"] = "柏林",
        ["DUBAI"] = "迪拜",
        ["MUMBAI"] = "孟买",
        ["SINGAPORE"] = "新加坡",
        ["BEIJING"] = "北京",
        ["HONG KONG"] = "香港",
        ["TOKYO"] = "东京",
        ["SYDNEY"] = "悉尼",
        ["AUCKLAND"] = "奥克兰"
    };

    public static bool IsChinese(string? language) => !string.Equals(language, English, StringComparison.OrdinalIgnoreCase);

    public static string NormalizeLanguage(string? language) =>
        string.Equals(language, English, StringComparison.OrdinalIgnoreCase) ? English : SimplifiedChinese;

    public static CultureInfo Culture(string? language) =>
        CultureInfo.GetCultureInfo(IsChinese(language) ? SimplifiedChinese : English);

    public static string Text(string? language, string key)
    {
        var source = IsChinese(language) ? Chinese : EnglishTexts;
        return source.TryGetValue(key, out var value) ? value : key;
    }

    public static string CityName(string internalName, string? language) =>
        IsChinese(language) && ChineseCities.TryGetValue(internalName, out var value) ? value : internalName;

    public static IReadOnlyList<LocalizedOption> CityOptions(IEnumerable<string> names, string? language) =>
        names.Select(name => new LocalizedOption(name, CityName(name, language))).ToArray();
}
