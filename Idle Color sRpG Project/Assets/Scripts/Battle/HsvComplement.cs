using System;

// 補色。HSVの色相を180度回し、彩度と明度は変えない。
public static class HsvComplement
{
    public static void Complementary(int r, int g, int b, out int outR, out int outG, out int outB)
    {
        double hue;
        double saturation;
        double value;
        RgbToHsv(Channel01(r), Channel01(g), Channel01(b), out hue, out saturation, out value);
        hue += 0.5;
        if (hue >= 1.0)
            hue -= 1.0;
        double red;
        double green;
        double blue;
        HsvToRgb(hue, saturation, value, out red, out green, out blue);
        outR = Channel255(red);
        outG = Channel255(green);
        outB = Channel255(blue);
    }

    static double Channel01(int channel)
    {
        if (channel < 0)
            channel = 0;
        if (channel > 255)
            channel = 255;
        return channel / 255.0;
    }

    static int Channel255(double channel)
    {
        if (double.IsNaN(channel) || channel <= 0.0)
            return 0;
        int value = (int)Math.Round(channel * 255.0);
        if (value > 255)
            return 255;
        return value;
    }

    static void RgbToHsv(double r, double g, double b, out double hue, out double saturation, out double value)
    {
        double max = r;
        if (g > max)
            max = g;
        if (b > max)
            max = b;
        double min = r;
        if (g < min)
            min = g;
        if (b < min)
            min = b;
        double delta = max - min;

        value = max;
        saturation = max <= 0.0 ? 0.0 : delta / max;
        hue = 0.0;
        if (delta <= 0.0)
            return;

        if (max == r)
            hue = (g - b) / delta;
        else if (max == g)
            hue = (b - r) / delta + 2.0;
        else
            hue = (r - g) / delta + 4.0;
        hue /= 6.0;
        if (hue < 0.0)
            hue += 1.0;
    }

    static void HsvToRgb(double hue, double saturation, double value, out double r, out double g, out double b)
    {
        if (saturation <= 0.0)
        {
            r = value;
            g = value;
            b = value;
            return;
        }

        double sector = hue * 6.0;
        if (sector >= 6.0)
            sector = 0.0;
        int index = (int)Math.Floor(sector);
        double fraction = sector - index;
        double p = value * (1.0 - saturation);
        double q = value * (1.0 - saturation * fraction);
        double t = value * (1.0 - saturation * (1.0 - fraction));
        switch (index)
        {
            case 0:
                r = value;
                g = t;
                b = p;
                break;
            case 1:
                r = q;
                g = value;
                b = p;
                break;
            case 2:
                r = p;
                g = value;
                b = t;
                break;
            case 3:
                r = p;
                g = q;
                b = value;
                break;
            case 4:
                r = t;
                g = p;
                b = value;
                break;
            default:
                r = value;
                g = p;
                b = q;
                break;
        }
    }
}
