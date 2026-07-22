namespace ThinkTime;

internal static class SolarMath
{
    public static (double Latitude, double Longitude) GetSubsolarPoint(DateTime utc)
    {
        utc = utc.ToUniversalTime();
        var fractionalHour = utc.Hour + utc.Minute / 60d + utc.Second / 3600d;
        var yearAngle = 2 * Math.PI / 365d * (utc.DayOfYear - 1 + (fractionalHour - 12) / 24d);

        var declination = 0.006918
            - 0.399912 * Math.Cos(yearAngle) + 0.070257 * Math.Sin(yearAngle)
            - 0.006758 * Math.Cos(2 * yearAngle) + 0.000907 * Math.Sin(2 * yearAngle)
            - 0.002697 * Math.Cos(3 * yearAngle) + 0.001480 * Math.Sin(3 * yearAngle);
        var equationOfTime = 229.18 * (0.000075
            + 0.001868 * Math.Cos(yearAngle) - 0.032077 * Math.Sin(yearAngle)
            - 0.014615 * Math.Cos(2 * yearAngle) - 0.040849 * Math.Sin(2 * yearAngle));

        var minutesUtc = utc.Hour * 60 + utc.Minute + utc.Second / 60d;
        var longitude = (720 - minutesUtc - equationOfTime) / 4d;
        longitude = (longitude + 180) % 360 - 180;
        return (declination * 180 / Math.PI, longitude);
    }

    public static double BoundaryLatitude(double longitude, double solarLatitude, double solarLongitude, double elevation)
    {
        var declination = solarLatitude * Math.PI / 180;
        var hourAngle = (longitude - solarLongitude) * Math.PI / 180;
        var targetElevation = elevation * Math.PI / 180;
        var a = Math.Sin(declination);
        var b = Math.Cos(declination) * Math.Cos(hourAngle);
        var magnitude = Math.Sqrt(a * a + b * b);
        if (magnitude < 1e-9) return 0;
        var argument = Math.Clamp(Math.Sin(targetElevation) / magnitude, -1, 1);
        var latitude = (Math.Asin(argument) - Math.Atan2(b, a)) * 180 / Math.PI;
        return Math.Clamp(latitude, -90, 90);
    }

    public static double SolarElevation(double latitude, double longitude, double solarLatitude, double solarLongitude)
    {
        var lat = latitude * Math.PI / 180;
        var dec = solarLatitude * Math.PI / 180;
        var hour = (longitude - solarLongitude) * Math.PI / 180;
        var sine = Math.Sin(lat) * Math.Sin(dec) + Math.Cos(lat) * Math.Cos(dec) * Math.Cos(hour);
        return Math.Asin(Math.Clamp(sine, -1, 1)) * 180 / Math.PI;
    }
}
