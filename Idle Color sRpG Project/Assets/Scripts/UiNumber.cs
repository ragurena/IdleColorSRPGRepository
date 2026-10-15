using System.Globalization;

// 画面に出す数量。色の0〜255やセーブの数値には使わない。
public static class UiNumber
{
    public static string Format(ulong value)
    {
        return value.ToString("#,0", CultureInfo.InvariantCulture);
    }

    public static string Format(long value)
    {
        return value.ToString("#,0", CultureInfo.InvariantCulture);
    }

    public static string Format(int value)
    {
        return Format((long)value);
    }

    public static string Format(uint value)
    {
        return Format((ulong)value);
    }
}
