using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;

namespace ThinkTime;

internal sealed class GeoJsonMap
{
    private readonly IReadOnlyList<IReadOnlyList<Point>> _land;
    private readonly IReadOnlyList<IReadOnlyList<Point>> _countries;
    private readonly IReadOnlyList<IReadOnlyList<Point>> _china;
    private Geometry? _landGeometry;
    private Geometry? _borderGeometry;
    private Geometry? _chinaGeometry;
    private Size _cachedSize;

    public GeoJsonMap()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Assets", "Geodata");
        _land = ReadOuterRings(Path.Combine(root, "ne_110m_land.geojson"));
        _countries = ReadOuterRings(Path.Combine(root, "ne_110m_admin_0_countries.geojson"));
        _china = ReadOuterRings(Path.Combine(root, "china_full.geojson"));
    }

    public void Draw(DrawingContext dc, double width, double height)
    {
        if (_landGeometry is null || _cachedSize.Width != width || _cachedSize.Height != height)
            BuildGeometry(width, height);

        dc.DrawGeometry(
            new SolidColorBrush(Color.FromRgb(52, 64, 79)),
            new Pen(new SolidColorBrush(Color.FromArgb(160, 113, 130, 150)), .7),
            _landGeometry);
        dc.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(Color.FromArgb(115, 116, 132, 151)), .55),
            _borderGeometry);

        // China-specific national/provincial geometry is drawn last. Its opaque land fill
        // masks conflicting generalized borders in Natural Earth, including southern Tibet.
        dc.DrawGeometry(
            new SolidColorBrush(Color.FromRgb(52, 64, 79)),
            new Pen(new SolidColorBrush(Color.FromArgb(175, 132, 149, 169)), .62),
            _chinaGeometry);
    }

    private void BuildGeometry(double width, double height)
    {
        _cachedSize = new Size(width, height);
        _landGeometry = BuildPath(_land, width, height, true);
        _borderGeometry = BuildPath(_countries, width, height, false);
        _chinaGeometry = BuildPath(_china, width, height, true);
    }

    private static Geometry BuildPath(IReadOnlyList<IReadOnlyList<Point>> rings, double width, double height, bool filled)
    {
        var geometry = new StreamGeometry { FillRule = FillRule.EvenOdd };
        using var context = geometry.Open();
        foreach (var ring in rings)
        {
            if (ring.Count < 2) continue;
            context.BeginFigure(Project(ring[0], width, height), filled, filled);
            for (var i = 1; i < ring.Count; i++)
                context.LineTo(Project(ring[i], width, height), true, false);
        }
        geometry.Freeze();
        return geometry;
    }

    private static Point Project(Point lonLat, double width, double height) =>
        new((lonLat.X + 180) / 360 * width, (90 - lonLat.Y) / 180 * height);

    private static IReadOnlyList<IReadOnlyList<Point>> ReadOuterRings(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var result = new List<IReadOnlyList<Point>>();
        foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
        {
            var geometry = feature.GetProperty("geometry");
            if (geometry.ValueKind == JsonValueKind.Null) continue;
            var type = geometry.GetProperty("type").GetString();
            var coordinates = geometry.GetProperty("coordinates");
            if (type == "Polygon") AddPolygon(coordinates, result);
            else if (type == "MultiPolygon")
                foreach (var polygon in coordinates.EnumerateArray()) AddPolygon(polygon, result);
        }
        return result;
    }

    private static void AddPolygon(JsonElement polygon, ICollection<IReadOnlyList<Point>> output)
    {
        var outer = polygon.EnumerateArray().FirstOrDefault();
        if (outer.ValueKind != JsonValueKind.Array) return;
        var points = new List<Point>();
        foreach (var coordinate in outer.EnumerateArray())
        {
            var values = coordinate.EnumerateArray().ToArray();
            if (values.Length >= 2) points.Add(new Point(values[0].GetDouble(), values[1].GetDouble()));
        }
        if (points.Count > 2) output.Add(points);
    }
}
