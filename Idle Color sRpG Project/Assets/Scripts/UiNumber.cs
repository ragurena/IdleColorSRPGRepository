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

    // レベル上限に達した表記だけ赤くする。前後の文字色はそのまま。
    public static string LevelText(string body, long level, long cap)
    {
        if (cap > 0 && level >= cap)
            return "<color=#ff2a2a>" + body + "</color>";
        return body;
    }
}
