using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using FontFamily = System.Windows.Media.FontFamily;
using FlowDirection = System.Windows.FlowDirection;

namespace ThinkTime;

public sealed class WorldClockView : FrameworkElement
{
    private readonly DispatcherTimer _timer;
    private readonly GeoJsonMap _map;
    private readonly NightLightsTexture _nightLights;
    private City? _hovered;
    private AppSettings _settings = new();
    public event Action<string?>? CityHovered;

    private static readonly City[] Cities =
    {
        new("SAN FRANCISCO", -122.42, 37.77, "Pacific Standard Time"),
        new("NEW YORK", -74.01, 40.71, "Eastern Standard Time", true),
        new("LONDON", -0.13, 51.51, "GMT Standard Time"),
        new("BERLIN", 13.41, 52.52, "W. Europe Standard Time"),
        new("DUBAI", 55.27, 25.20, "Arabian Standard Time"),
        new("MUMBAI", 72.88, 19.08, "India Standard Time"),
        new("SINGAPORE", 103.82, 1.35, "Singapore Standard Time"),
        new("BEIJING", 116.41, 39.90, "China Standard Time", false, 8, -19),
        new("HONG KONG", 114.17, 22.32, "China Standard Time", false, 8, 15),
        new("TOKYO", 139.69, 35.68, "Tokyo Standard Time"),
        new("SYDNEY", 151.21, -33.87, "AUS Eastern Standard Time"),
        new("AUCKLAND", 174.76, -36.85, "New Zealand Standard Time")
    };
    public static IReadOnlyList<string> CityNames { get; } = Cities.Select(c => c.Name).ToArray();
    private static readonly (double Lon, double Lat, double Strength)[] NightLights =
    {
        (-122.4,37.8,1),(-118.2,34.1,.9),(-87.6,41.9,.8),(-74,40.7,1),(-79.4,43.7,.7),
        (-99.1,19.4,.8),(-46.6,-23.6,.9),(-58.4,-34.6,.7),(-0.1,51.5,1),(2.35,48.86,.9),
        (4.9,52.4,.7),(13.4,52.5,.8),(12.5,41.9,.65),(37.6,55.8,.8),(29,41,.9),
        (31.2,30,.8),(28, -26.2,.7),(55.3,25.2,.8),(72.9,19.1,1),(77.2,28.6,.8),
        (77.6,12.97,.7),(90.4,23.8,.8),(103.8,1.35,.9),(106.8,-6.2,1),(100.5,13.75,.8),
        (114.17,22.32,.9),(113.3,23.1,.9),(121.47,31.23,1),(116.41,39.9,.9),(117.2,39.1,.7),
        (126.98,37.57,1),(139.69,35.68,1),(135.5,34.7,.8),(121,14.6,.9),(151.2,-33.9,.9),
        (144.96,-37.8,.75),(174.76,-36.85,.65)
    };

    public WorldClockView()
    {
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.Grayscale);
        _map = new GeoJsonMap();
        _nightLights = new NightLightsTexture();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => InvalidateVisual();
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
        MouseMove += HandleMouseMove;
        MouseLeave += (_, _) => SetHovered(null);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth; var h = ActualHeight;
        var now = DateTime.UtcNow;
        dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(16,22,30), Color.FromRgb(6,10,15), 90), null, new Rect(0,0,w,h));
        _map.Draw(dc, w, h);
        DrawTimeZoneHighlights(dc, w, h);
        DrawGrid(dc, w, h);
        if (_settings.ShowTimeZones) DrawTimeZones(dc, w, h);
        DrawNight(dc, w, h, now);
        DrawSatelliteNightLights(dc, w, h, now);
        DrawVignette(dc, w, h);
        if (_settings.ShowTimeZones) DrawOffsetLabels(dc, w, h);
        DrawCities(dc, w, h, now);
        DrawCornerData(dc);
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _timer.Interval = settings.LowPowerMode ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(1);
        InvalidateVisual();
    }

    private static void DrawGrid(DrawingContext dc, double w, double h)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(24, 143, 158, 176)), .8);
        for (var lon=-150; lon<=150; lon+=30) { var x=(lon+180)/360*w; dc.DrawLine(pen,new Point(x,0),new Point(x,h)); }
        for (var lat=-60; lat<=60; lat+=30) { var y=(90-lat)/180*h; dc.DrawLine(pen,new Point(0,y),new Point(w,y)); }
    }

    private static void DrawTimeZones(DrawingContext dc, double w, double h)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(22, 194, 203, 214)), .65);
        for (var lon=-172.5; lon<180; lon+=15)
        {
            var x=(lon+180)/360*w;
            dc.DrawLine(pen,new Point(x,0),new Point(x,h));
        }
        var red = new Pen(new SolidColorBrush(Color.FromArgb(105, 226, 35, 26)), 1);
        dc.DrawLine(red,new Point(w-2,0),new Point(w-2,h));
    }

    private void DrawTimeZoneHighlights(DrawingContext dc, double w, double h)
    {
        var zoneWidth = w / 24d;
        var gmtX = w / 2d - zoneWidth / 2d;
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(22, 76, 177, 139)), null,
            new Rect(gmtX, 0, zoneWidth, h));

        var accent = Cities.FirstOrDefault(c => c.Name == _settings.AccentCity);
        if (accent is null) return;
        var centerX = (accent.Longitude + 180) / 360 * w;
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(12, 226, 35, 26)), null,
            new Rect(centerX - zoneWidth / 2, 0, zoneWidth, h));
    }

    private void DrawOffsetLabels(DrawingContext dc, double w, double h)
    {
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var face = new Typeface(new FontFamily("Cascadia Mono, Consolas"), FontStyles.Normal,
            FontWeights.Normal, FontStretches.Normal);
        for (var offset = -11; offset <= 12; offset++)
        {
            var x = (offset + 12) / 24d * w;
            var label = offset == 0 ? "UTC" : $"{offset:+#;-#}";
            var color = offset == 0 ? Color.FromArgb(175, 109, 198, 159) : Color.FromArgb(108, 146, 158, 171);
            var text = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                face, 10, new SolidColorBrush(color), dpi);
            dc.DrawText(text, new Point(Math.Round(x - text.Width / 2), Math.Round(h - 18)));
        }
    }

    private static void DrawNight(DrawingContext dc, double w, double h, DateTime utc)
    {
        var sun = SolarMath.GetSubsolarPoint(utc);
        (double Elevation, byte Alpha)[] bands = { (0, 52), (-6, 38), (-12, 28), (-18, 20) };
        foreach (var band in bands)
            DrawNightBand(dc, w, h, sun.Latitude, sun.Longitude, band.Elevation, band.Alpha);

        var boundary = BuildBoundary(w, h, sun.Latitude, sun.Longitude, 0);
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(26, 138, 177, 202)), 3.2), boundary);
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(110, 173, 201, 218)), .8), boundary);
    }

    private static Geometry BuildBoundary(double w, double h, double solarLat, double solarLon, double elevation)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        var first = true;
        for (var lon = -180d; lon <= 180; lon += 1.5)
        {
            var point = Project(lon, SolarMath.BoundaryLatitude(lon, solarLat, solarLon, elevation), w, h);
            if (first) { context.BeginFigure(point, false, false); first = false; }
            else context.LineTo(point, true, false);
        }
        geometry.Freeze();
        return geometry;
    }

    private static void DrawNightLights(DrawingContext dc, double w, double h, DateTime utc)
    {
        var sun = SolarMath.GetSubsolarPoint(utc);
        foreach (var light in NightLights)
        {
            var elevation = SolarMath.SolarElevation(light.Lat, light.Lon, sun.Latitude, sun.Longitude);
            if (elevation > -3) continue;
            var darkness = Math.Clamp((-elevation - 3) / 15, 0, 1) * light.Strength;
            var point = Project(light.Lon, light.Lat, w, h);
            var glow = new RadialGradientBrush
            {
                GradientStops = new GradientStopCollection
                {
                    new(Color.FromArgb((byte)(210 * darkness), 255, 218, 122), 0),
                    new(Color.FromArgb((byte)(80 * darkness), 245, 166, 50), .35),
                    new(Colors.Transparent, 1)
                }
            };
            var radius = 3 + 3.5 * light.Strength;
            dc.DrawEllipse(glow, null, point, radius, radius);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)(225 * darkness), 255, 231, 167)), null, point, 1.1, 1.1);
        }
    }

    private static void DrawNightBand(DrawingContext dc, double w, double h, double solarLat, double solarLon, double elevation, byte alpha)
    {
        var geometry = BuildNightRegion(w, h, solarLat, solarLon, elevation);
        dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(alpha,0,4,9)),null,geometry);
    }

    private static Geometry BuildNightRegion(double w, double h, double solarLat, double solarLon, double elevation)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        var points = new List<Point>();
        for (var lon = -180d; lon <= 180; lon += 2)
            points.Add(Project(lon, SolarMath.BoundaryLatitude(lon, solarLat, solarLon, elevation), w, h));
        var northDark = solarLat < 0;
        context.BeginFigure(points[0], true, true);
        foreach (var point in points.Skip(1)) context.LineTo(point, true, false);
        context.LineTo(new Point(w, northDark ? 0 : h), true, false);
        context.LineTo(new Point(0, northDark ? 0 : h), true, false);
        geometry.Freeze();
        return geometry;
    }

    private void DrawSatelliteNightLights(DrawingContext dc, double w, double h, DateTime utc)
    {
        if (_nightLights.Image is null)
        {
            DrawNightLights(dc, w, h, utc);
            return;
        }

        var sun = SolarMath.GetSubsolarPoint(utc);
        (double Elevation, double Opacity)[] reveal = { (0, .08), (-6, .16), (-12, .22), (-18, .23) };
        foreach (var layer in reveal)
        {
            dc.PushClip(BuildNightRegion(w, h, sun.Latitude, sun.Longitude, layer.Elevation));
            dc.PushOpacity(layer.Opacity);
            dc.DrawImage(_nightLights.Image, new Rect(0, 0, w, h));
            dc.Pop();
            dc.Pop();
        }
    }

    private void DrawCities(DrawingContext dc, double w, double h, DateTime utc)
    {
        var visuals = BuildCityLayout(w, h, utc);
        var plateFill = new SolidColorBrush(Color.FromArgb(218, 8, 13, 19));
        var plateStroke = new Pen(new SolidColorBrush(Color.FromArgb(72, 102, 119, 137)), .65);
        foreach (var visual in visuals)
        {
            if (visual.ShowLabel)
            {
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(55, 173, 187, 201)), .6),
                    visual.Dot, NearestPoint(visual.Plate, visual.Dot));
                dc.DrawRoundedRectangle(plateFill, plateStroke, visual.Plate, 3.5, 3.5);
                dc.DrawText(visual.Text, visual.TextOrigin);
            }
            dc.DrawEllipse(new SolidColorBrush(visual.Color),
                new Pen(new SolidColorBrush(Color.FromArgb(150, 5, 8, 11)), 1),
                visual.Dot, visual.Accent ? 4 : 2.7, visual.Accent ? 4 : 2.7);
        }
    }

    private IReadOnlyList<CityVisual> BuildCityLayout(double w, double h, DateTime utc)
    {
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var bounds = new Rect(14, 76, Math.Max(1, w - 28), Math.Max(1, h - 144));
        var placed = new List<Rect> { new(18, 78, 276, 58) };
        var dots = Cities.Select(c => Project(c.Longitude, c.Latitude, w, h)).ToArray();
        placed.AddRange(dots.Select(p => new Rect(p.X - 5, p.Y - 5, 10, 10)));
        var output = new List<CityVisual>();

        var ordered = Cities.OrderByDescending(c => c.Name == _settings.AccentCity || c == _hovered)
            .ThenBy(c => c.Longitude).ToArray();
        foreach (var city in ordered)
        {
            var dot = Project(city.Longitude, city.Latitude, w, h);
            var accent = city.Name == _settings.AccentCity || city == _hovered;
            var color = accent ? Color.FromRgb(247, 198, 51) : Color.FromRgb(220, 226, 233);
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(city.TimeZoneId));
            var culture = Localization.Culture(_settings.Language);
            var timeFormat = _settings.Use24HourClock ? "HH:mm" : Localization.IsChinese(_settings.Language) ? "tt h:mm" : "h:mm tt";
            var label = $"{Localization.CityName(city.Name, _settings.Language)}\n{local.ToString(timeFormat, culture)}";
            var family = Localization.IsChinese(_settings.Language)
                ? "Microsoft YaHei UI, Segoe UI, Cascadia Mono"
                : "Cascadia Mono, Consolas";
            var face = new Typeface(new FontFamily(family), FontStyles.Normal,
                accent ? FontWeights.Bold : FontWeights.Medium, FontStretches.Normal);
            var text = new FormattedText(label, culture, FlowDirection.LeftToRight,
                face, accent ? 16 : 14, new SolidColorBrush(color), dpi);
            var show = _settings.ShowCityLabels || city == _hovered || accent;
            var candidates = LabelCandidates(dot, text.Width, text.Height, city, w);
            var best = candidates.OrderBy(r => PlacementPenalty(r, bounds, placed)).First();
            if (show) placed.Add(best);
            output.Add(new CityVisual(city, dot, text, best,
                new Point(Math.Round(best.X + 8), Math.Round(best.Y + 5)), color, accent, show));
        }
        return output;
    }

    private static IEnumerable<Rect> LabelCandidates(Point dot, double textW, double textH, City city, double width)
    {
        const double padX = 8, padY = 5, gap = 10;
        var pw = textW + padX * 2; var ph = textH + padY * 2;
        var right = new Rect(dot.X + gap, dot.Y - ph / 2, pw, ph);
        var left = new Rect(dot.X - gap - pw, dot.Y - ph / 2, pw, ph);
        var above = new Rect(dot.X - pw / 2, dot.Y - gap - ph, pw, ph);
        var below = new Rect(dot.X - pw / 2, dot.Y + gap, pw, ph);
        var preferred = new Rect(dot.X + city.LabelOffsetX - padX,
            dot.Y - textH / 2 + city.LabelOffsetY - padY, pw, ph);
        if (dot.X > width * .78) return new[] { left, above, below, right, preferred };
        if (dot.X < width * .22) return new[] { right, above, below, left, preferred };
        return new[] { preferred, right, left, below, above };
    }

    private static double PlacementPenalty(Rect candidate, Rect bounds, IReadOnlyList<Rect> obstacles)
    {
        var outside = Math.Max(0, bounds.Left - candidate.Left) + Math.Max(0, candidate.Right - bounds.Right)
            + Math.Max(0, bounds.Top - candidate.Top) + Math.Max(0, candidate.Bottom - bounds.Bottom);
        var overlap = obstacles.Sum(o => IntersectionArea(candidate, o));
        return outside * 10000 + overlap * 100 + candidate.Y * .001;
    }

    private static double IntersectionArea(Rect a, Rect b)
    {
        var intersection = Rect.Intersect(a, b);
        return intersection.IsEmpty ? 0 : intersection.Width * intersection.Height;
    }

    private static Point NearestPoint(Rect rect, Point point) => new(
        Math.Clamp(point.X, rect.Left, rect.Right), Math.Clamp(point.Y, rect.Top, rect.Bottom));

    private static void DrawVignette(DrawingContext dc, double w, double h)
    {
        var brush = new RadialGradientBrush
        {
            Center = new Point(.5, .47), GradientOrigin = new Point(.5, .47), RadiusX = .72, RadiusY = .72,
            GradientStops = new GradientStopCollection
            {
                new(Colors.Transparent, .42),
                new(Color.FromArgb(26, 0, 3, 7), .78),
                new(Color.FromArgb(82, 0, 2, 5), 1)
            }
        };
        dc.DrawRectangle(brush, null, new Rect(0, 0, w, h));
    }

    private sealed record CityVisual(City City, Point Dot, FormattedText Text, Rect Plate,
        Point TextOrigin, Color Color, bool Accent, bool ShowLabel);

    private void DrawCornerData(DrawingContext dc)
    {
        var family = Localization.IsChinese(_settings.Language)
            ? "Microsoft YaHei UI, Segoe UI, Cascadia Mono"
            : "Cascadia Mono, Consolas";
        var ft=new FormattedText($"UTC  {DateTime.UtcNow:yyyy-MM-dd  HH:mm:ss}\n{Localization.Text(_settings.Language, "SolarLive")}",
            Localization.Culture(_settings.Language),FlowDirection.LeftToRight,new Typeface(family),11.5,
            new SolidColorBrush(Color.FromArgb(176,174,186,198)),VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(ft,new Point(24,82));
    }

    private void HandleMouseMove(object sender, MouseEventArgs e)
    {
        var mouse=e.GetPosition(this); City? nearest=null; var distance=28d;
        foreach(var city in Cities)
        {
            var p=Project(city.Longitude,city.Latitude,ActualWidth,ActualHeight);
            var d=(p-mouse).Length; if(d<distance){distance=d;nearest=city;}
        }
        SetHovered(nearest);
    }

    private void SetHovered(City? city)
    {
        if(city==_hovered)return; _hovered=city; InvalidateVisual();
        if(city is null) CityHovered?.Invoke(null);
        else
        {
            var zone=TimeZoneInfo.FindSystemTimeZoneById(city.TimeZoneId);
            var local=TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,zone);
            var offset=zone.GetUtcOffset(DateTime.UtcNow);
            var sign=offset < TimeSpan.Zero ? "-" : "+";
            var date = Localization.IsChinese(_settings.Language)
                ? local.ToString("yyyy年M月d日 dddd", Localization.Culture(_settings.Language))
                : local.ToString("dddd, MMMM dd", Localization.Culture(_settings.Language));
            CityHovered?.Invoke($"{Localization.CityName(city.Name, _settings.Language)}  /  {date}  /  UTC{sign}{offset.Duration():hh\\:mm}");
        }
    }

    private static Point Project(double lon,double lat,double w,double h)=>new((lon+180)/360*w,(90-lat)/180*h);
}
