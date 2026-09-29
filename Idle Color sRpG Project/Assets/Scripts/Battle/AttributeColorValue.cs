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

// 代表カラーの属性値。属性ごとに関数を分ける。
public static class AttributeColorValue
{
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
        return r < 0 ? 0 : r;
    }

    public static long GetGreen(int r, int g, int b)
    {
        return g < 0 ? 0 : g;
    }

    public static long GetBlue(int r, int g, int b)
    {
        return b < 0 ? 0 : b;
    }

    public static long GetLight(int r, int g, int b)
    {
        long sum = (long)r + g + b;
        if (sum < 0)
            return 0;
        return sum;
    }

    // (R + G + B) / 3。端数は切り上げ。
    public static long GetDark(int r, int g, int b)
    {
        long sum = (long)r + g + b;
        if (sum < 0)
            return 0;
        return BattleMath.CeilDivPositive(sum, 3);
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
