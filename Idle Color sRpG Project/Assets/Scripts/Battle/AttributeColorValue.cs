// CharacterType と同じ並び。バトル計算は Unity の列挙に依存しない。
public enum CharacterAttribute
{
    None = 0,
    Fire = 1,
    Grass = 2,
    Water = 3,
    Light = 4,
    Dark = 5
}

// 代表カラーの属性値。色そのものではなく、減色と同じ階調の番号を使う。
// 8階調なら 0→1、32→2、64→3、…、224→8、255→9。光はその合計、闇は切り上げ平均。
public static class AttributeColorValue
{
    // GameConfig.GRADATION_LEVELS と同じ。255 はこれより 1 段多い番号になる。
    public const int Levels = 8;

    public static long GetAttributeColorValue(CharacterAttribute attribute, int r, int g, int b)
    {
        switch (attribute)
        {
            case CharacterAttribute.Fire:
                return GetRed(r, g, b);
            case CharacterAttribute.Grass:
                return GetGreen(r, g, b);
            case CharacterAttribute.Water:
                return GetBlue(r, g, b);
            case CharacterAttribute.Light:
                return GetLight(r, g, b);
            case CharacterAttribute.Dark:
                return GetDark(r, g, b);
            default:
                return 0;
        }
    }

    public static long GetRed(int r, int g, int b)
    {
        return ChannelStep(r);
    }

    public static long GetGreen(int r, int g, int b)
    {
        return ChannelStep(g);
    }

    public static long GetBlue(int r, int g, int b)
    {
        return ChannelStep(b);
    }

    public static long GetLight(int r, int g, int b)
    {
        return ChannelStep(r) + ChannelStep(g) + ChannelStep(b);
    }

    // 3色の階調番号の平均。端数は切り上げ。
    public static long GetDark(int r, int g, int b)
    {
        long sum = ChannelStep(r) + ChannelStep(g) + ChannelStep(b);
        return BattleMath.CeilDivPositive(sum, 3);
    }

    // 0〜255 を、減色と同じいちばん近い階調の番号（1 始まり）にする。255 以上は最終段。
    public static int ChannelStep(int channel)
    {
        return ChannelStep(channel, Levels);
    }

    public static int ChannelStep(int channel, int levels)
    {
        if (levels < 2)
            levels = 2;
        if (channel < 0)
            channel = 0;

        int step = 256 / levels;
        if (step < 1)
            step = 1;
        if (channel >= 255)
            return levels + 1;

        int index = (channel + step / 2) / step;
        if (index < 0)
            index = 0;
        if (index >= levels)
            index = levels - 1;
        return index + 1;
    }

    public static string DisplayName(CharacterAttribute attribute)
    {
        switch (attribute)
        {
            case CharacterAttribute.Fire: return "赤";
            case CharacterAttribute.Grass: return "緑";
            case CharacterAttribute.Water: return "青";
            case CharacterAttribute.Light: return "光";
            case CharacterAttribute.Dark: return "闇";
            default: return "無";
        }
    }
}
