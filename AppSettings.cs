namespace ThinkTime;

public sealed class AppSettings
{
    public string Language { get; set; } = Localization.SimplifiedChinese;
    public bool Use24HourClock { get; set; }
    public bool ShowTimeZones { get; set; } = true;
    public bool ShowCityLabels { get; set; } = true;
    public bool LowPowerMode { get; set; }
    public string AccentCity { get; set; } = "NEW YORK";
}
