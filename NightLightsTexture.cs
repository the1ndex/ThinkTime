using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThinkTime;

internal sealed class NightLightsTexture
{
    public ImageSource? Image { get; }

    public NightLightsTexture()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Textures",
                "BlackMarble_2016_01deg_gray.jpg");
            Image = BuildTransparentTexture(path);
        }
        catch
        {
            Image = null;
        }
    }

    private static BitmapSource BuildTransparentTexture(string path)
    {
        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.DecodePixelWidth = 1800;
        source.UriSource = new Uri(path, UriKind.Absolute);
        source.EndInit();
        source.Freeze();

        var gray = new FormatConvertedBitmap(source, PixelFormats.Gray8, null, 0);
        var width = gray.PixelWidth;
        var height = gray.PixelHeight;
        var luminance = new byte[width * height];
        gray.CopyPixels(luminance, width, 0);
        var bgra = new byte[width * height * 4];

        for (var i = 0; i < luminance.Length; i++)
        {
            var normalized = Math.Max(0, (luminance[i] - 11) / 244d);
            var intensity = Math.Pow(normalized, .72);
            var alpha = (byte)Math.Clamp(intensity * 238, 0, 238);
            var j = i * 4;
            bgra[j] = (byte)(92 + 34 * intensity);
            bgra[j + 1] = (byte)(178 + 52 * intensity);
            bgra[j + 2] = 255;
            bgra[j + 3] = alpha;
        }

        var texture = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32,
            null, bgra, width * 4);
        texture.Freeze();
        return texture;
    }
}
