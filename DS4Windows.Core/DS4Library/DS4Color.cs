using System.Drawing;

namespace DS4Windows;

// ReSharper disable once InconsistentNaming
public struct DS4Color : IEquatable<DS4Color>
{
    public byte Red;
    public byte Green;
    public byte Blue;

    public DS4Color(Color c)
    {
        Red = c.R;
        Green = c.G;
        Blue = c.B;
    }

    public DS4Color(byte r, byte g, byte b)
    {
        Red = r;
        Green = g;
        Blue = b;
    }

    public bool Equals(DS4Color other)
    {
        return Red == other.Red && Green == other.Green && Blue == other.Blue;
    }

    public Color ToColor => Color.FromArgb(Red, Green, Blue);
    
    public Color ToColorA
    {
        get
        {
            var alphaColor = Math.Max(Red, Math.Max(Green, Blue));
            var reg = Color.FromArgb(Red, Green, Blue);
            var full = HueToRgb(reg.GetHue(), reg.GetBrightness(), ref reg);
            return Color.FromArgb((alphaColor > 205 ? 255 : (alphaColor + 50)), full);
        }
    }

    private Color HueToRgb(float hue, float light, ref Color rgb)
    {
        var l = (float)Math.Max(.5, light);
        var c = (1 - Math.Abs(2 * l - 1));
        var x = (c * (1 - Math.Abs((hue / 60) % 2 - 1)));
        var m = l - c / 2;
        float r = 0, g = 0, b = 0;
        if (light == 1) return Color.White;
        else if (rgb.R == rgb.G && rgb.G == rgb.B) return Color.White;
        else if (0 <= hue && hue < 60) { r = c; g = x; }
        else if (60 <= hue && hue < 120) { r = x; g = c; }
        else if (120 <= hue && hue < 180) { g = c; b = x; }
        else if (180 <= hue && hue < 240) { g = x; b = c; }
        else if (240 <= hue && hue < 300) { r = x; b = c; }
        else if (300 <= hue && hue < 360) { r = c; b = x; }
        return Color.FromArgb((int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255));
    }

    public static bool TryParse(string value, ref DS4Color ds4Color)
    {
        try
        {
            var ss = value.Split(',');
            return byte.TryParse(ss[0], out ds4Color.Red) && byte.TryParse(ss[1], out ds4Color.Green) && byte.TryParse(ss[2], out ds4Color.Blue);
        }
        catch { return false; }
    }

    public override string ToString() => $"Red: {Red} Green: {Green} Blue: {Blue}";
}