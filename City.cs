namespace ThinkTime;

public sealed record City(
    string Name,
    double Longitude,
    double Latitude,
    string TimeZoneId,
    bool Accent = false,
    double LabelOffsetX = 7,
    double LabelOffsetY = 0);
