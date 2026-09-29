using System;

public static class BattleMath
{
    public static long CeilToLong(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
            return 0;

        // 0.1 や 1.1 の浮動小数の誤差で、整数ちょうどの値が繰り上がらないようにする。
        double nearest = Math.Round(value);
        double scale = Math.Abs(value);
        if (scale < 1.0)
            scale = 1.0;
        if (Math.Abs(value - nearest) <= scale * 1e-9)
            value = nearest;

        double ceiling = Math.Ceiling(value);
        if (ceiling >= long.MaxValue)
            return long.MaxValue;
        return (long)ceiling;
    }

    public static long CeilDivPositive(long numerator, long denominator)
    {
        if (numerator <= 0 || denominator <= 0)
            return 0;
        return (numerator + denominator - 1) / denominator;
    }

    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    public static double Clamp01(double value, double max)
    {
        if (value < 0.0)
            return 0.0;
        if (value > max)
            return max;
        return value;
    }
}
