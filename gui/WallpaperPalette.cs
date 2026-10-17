using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;

namespace PredatorSense;

internal sealed record WallpaperColor(byte Red, byte Green, byte Blue, double Coverage)
{
    public string Hex => $"{Red:X2}{Green:X2}{Blue:X2}";
}

internal static class WallpaperPalette
{
    private sealed class ColorGroup
    {
        public long Red, Green, Blue;
        public int Count;
        public double R => (double)Red / Count;
        public double G => (double)Green / Count;
        public double B => (double)Blue / Count;

        public void Add(ColorGroup other)
        {
            Red += other.Red;
            Green += other.Green;
            Blue += other.Blue;
            Count += other.Count;
        }
    }

    public static string? FindCurrentWallpaper()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var state = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
        return FindOmarchyWallpaper(
            string.IsNullOrWhiteSpace(state) ? Path.Combine(home, ".local", "state") : state,
            string.IsNullOrWhiteSpace(config) ? Path.Combine(home, ".config") : config);
    }

    internal static string? FindOmarchyWallpaper(string stateDirectory, string configDirectory)
    {
        foreach (var root in new[] { stateDirectory, configDirectory })
        {
            var path = Path.Combine(root, "omarchy", "current", "background");
            if (!File.Exists(path))
                continue;
            return File.ResolveLinkTarget(path, true)?.FullName ?? Path.GetFullPath(path);
        }
        return null;
    }

    public static IReadOnlyList<WallpaperColor> Extract(string path, int maximumColors = 6)
    {
        if (maximumColors is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(maximumColors));
        using var stream = File.OpenRead(path);
        if (stream.Length > 64 * 1024 * 1024)
            throw new InvalidDataException("Choose an image smaller than 64 MB.");
        using var codec = SKCodec.Create(stream);
        if (codec == null)
            throw new InvalidDataException("This file is not a supported image.");
        var size = codec.Info;
        if (size.Width <= 0 || size.Height <= 0 || (long)size.Width * size.Height > 40_000_000)
            throw new InvalidDataException("Choose an image with fewer than 40 million pixels.");
        using var original = SKBitmap.Decode(codec,
            new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        if (original == null)
            throw new InvalidDataException("The image could not be read.");

        var scale = Math.Min(1d, 192d / Math.Max(size.Width, size.Height));
        using var sample = new SKBitmap(new SKImageInfo(
            Math.Max(1, (int)Math.Round(size.Width * scale)),
            Math.Max(1, (int)Math.Round(size.Height * scale)),
            SKColorType.Rgba8888, SKAlphaType.Unpremul));
        if (!original.ScalePixels(sample, new SKSamplingOptions(SKFilterMode.Linear)))
            throw new InvalidDataException("The image could not be sampled.");
        return ExtractColors(sample.Pixels, maximumColors);
    }

    internal static IReadOnlyList<WallpaperColor> ExtractColors(IEnumerable<SKColor> pixels, int maximumColors)
    {
        var histogram = new Dictionary<int, ColorGroup>();
        var total = 0;
        foreach (var pixel in pixels)
        {
            if (pixel.Alpha < 192)
                continue;
            var key = (pixel.Red >> 4) << 8 | (pixel.Green >> 4) << 4 | pixel.Blue >> 4;
            if (!histogram.TryGetValue(key, out var bucket))
                histogram[key] = bucket = new ColorGroup();
            bucket.Red += pixel.Red;
            bucket.Green += pixel.Green;
            bucket.Blue += pixel.Blue;
            bucket.Count++;
            total++;
        }
        if (total == 0)
            throw new InvalidDataException("The image contains no visible colors.");

        // Combine neighboring shades before ranking by image area. Sorting the
        // buckets first makes selection repeatable and avoids gradient noise.
        var groups = new List<ColorGroup>();
        foreach (var bucket in histogram.OrderByDescending(pair => pair.Value.Count)
                     .ThenBy(pair => pair.Key).Select(pair => pair.Value))
        {
            var nearest = groups.OrderBy(group => Distance(group.R, group.G, group.B,
                bucket.R, bucket.G, bucket.B)).FirstOrDefault();
            if (nearest != null && Distance(nearest.R, nearest.G, nearest.B,
                    bucket.R, bucket.G, bucket.B) < 48 * 48)
                nearest.Add(bucket);
            else
                groups.Add(bucket);
        }

        var palette = new List<WallpaperColor>();
        foreach (var group in groups.OrderByDescending(group => group.Count))
        {
            if (palette.Count > 0 && (double)group.Count / total < 0.005)
                break; // Ignore tiny image artifacts rather than offering them as prominent colors.
            var color = new WallpaperColor((byte)Math.Round(group.R),
                (byte)Math.Round(group.G), (byte)Math.Round(group.B), (double)group.Count / total);
            if (palette.Any(previous => Distance(previous.Red, previous.Green, previous.Blue,
                    color.Red, color.Green, color.Blue) < 72 * 72))
                continue;
            palette.Add(color);
            if (palette.Count == maximumColors)
                break;
        }
        return palette;
    }

    private static double Distance(double r1, double g1, double b1, double r2, double g2, double b2)
    {
        return (r1 - r2) * (r1 - r2) + (g1 - g2) * (g1 - g2) + (b1 - b2) * (b1 - b2);
    }
}
