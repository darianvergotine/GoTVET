using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace GoTVET;

public static class ThemeService
{
    public const string DefaultHex = "#0F6B4C";

    public static readonly string[] Presets =
    [
        "#0F6B4C",
        "#123A5F",
        "#C47B17",
        "#1F4E79",
        "#8B2942",
        "#333333"
    ];

    public static Color CurrentPrimary { get; private set; } = Parse(DefaultHex);

    public static event EventHandler? Changed;

    private static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GoTVET", "theme.json");

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(SettingsPath));
            if (document.RootElement.TryGetProperty("color", out var color) &&
                TryParse(color.GetString(), out var parsed))
            {
                Apply(parsed, persist: false);
            }
        }
        catch
        {
            // Keep the XAML defaults if the saved colour cannot be read.
        }
    }

    public static void Reset() => Apply(Parse(DefaultHex));

    public static void ApplyHex(string hex)
    {
        if (TryParse(hex, out var color))
        {
            Apply(color);
        }
    }

    public static void Apply(Color primary, bool persist = true)
    {
        CurrentPrimary = Color.FromRgb(primary.R, primary.G, primary.B);
        var palette = CreatePalette(CurrentPrimary);
        var app = Application.Current;
        if (app is not null)
        {
            app.Resources["BrandGreen"] = new SolidColorBrush(palette.Primary);
            app.Resources["BrandGreenDark"] = new SolidColorBrush(palette.PrimaryDark);
            app.Resources["BrandGold"] = new SolidColorBrush(palette.Accent);
            app.Resources["HeaderForeground"] = new SolidColorBrush(palette.OnPrimary);
            app.Resources["HeaderMuted"] = new SolidColorBrush(palette.OnPrimaryMuted);
            app.Resources["BrandOnGold"] = new SolidColorBrush(palette.OnAccent);
            app.Resources["BrandOnPrimary"] = new SolidColorBrush(palette.OnPrimary);
        }

        if (persist)
        {
            Save();
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static bool TryParse(string? hex, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        try
        {
            color = Parse(hex);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Color Parse(string hex) =>
        (Color)ColorConverter.ConvertFromString(hex.StartsWith('#') ? hex : "#" + hex)!;

    private static void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new { color = ToHex(CurrentPrimary) }));
    }

    private static ThemePalette CreatePalette(Color primary)
    {
        ToHsl(primary, out var h, out var s, out var l);
        var dark = FromHsl(h, s, Clamp(l * 0.72, 0, 0.45));
        var accent = FromHsl(h, Clamp(s * 0.85 + 0.15, 0.45, 1), Clamp(l + 0.22, 0.45, 0.72));
        var onPrimary = RelativeLuminance(primary) > 0.45
            ? Color.FromRgb(0x12, 0x26, 0x3A)
            : Colors.White;
        var onAccent = RelativeLuminance(accent) > 0.45
            ? Color.FromRgb(0x12, 0x26, 0x3A)
            : Colors.White;
        var muted = onPrimary == Colors.White
            ? Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0xC0, 0x12, 0x26, 0x3A);
        return new ThemePalette(primary, dark, accent, onPrimary, onAccent, muted);
    }

    private readonly record struct ThemePalette(
        Color Primary,
        Color PrimaryDark,
        Color Accent,
        Color OnPrimary,
        Color OnAccent,
        Color OnPrimaryMuted);

    private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

    private static double RelativeLuminance(Color color)
    {
        static double Linear(byte channel)
        {
            var value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
    }

    private static void ToHsl(Color color, out double h, out double s, out double l)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        l = (max + min) / 2;
        if (Math.Abs(max - min) < 0.00001)
        {
            h = 0;
            s = 0;
            return;
        }

        var delta = max - min;
        s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);
        if (Math.Abs(max - r) < 0.00001)
        {
            h = 60 * (((g - b) / delta) + (g < b ? 6 : 0));
        }
        else if (Math.Abs(max - g) < 0.00001)
        {
            h = 60 * (((b - r) / delta) + 2);
        }
        else
        {
            h = 60 * (((r - g) / delta) + 4);
        }
    }

    private static Color FromHsl(double h, double s, double l)
    {
        static double HueToRgb(double p, double q, double t)
        {
            if (t < 0)
            {
                t += 1;
            }

            if (t > 1)
            {
                t -= 1;
            }

            if (t < 1.0 / 6)
            {
                return p + (q - p) * 6 * t;
            }

            if (t < 0.5)
            {
                return q;
            }

            if (t < 2.0 / 3)
            {
                return p + (q - p) * (2.0 / 3 - t) * 6;
            }

            return p;
        }

        byte Channel(double value) => (byte)Math.Round(Clamp(value, 0, 1) * 255);
        if (s <= 0)
        {
            var grey = Channel(l);
            return Color.FromRgb(grey, grey, grey);
        }

        h = ((h % 360) + 360) % 360 / 360.0;
        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        return Color.FromRgb(
            Channel(HueToRgb(p, q, h + 1.0 / 3)),
            Channel(HueToRgb(p, q, h)),
            Channel(HueToRgb(p, q, h - 1.0 / 3)));
    }
}
