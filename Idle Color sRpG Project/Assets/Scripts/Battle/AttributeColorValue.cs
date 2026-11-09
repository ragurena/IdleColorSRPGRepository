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

// 代表カラーの属性値。減色と同じ階調番号を、1〜3 の3段階にまとめる。
// 番号 1〜3 は 1、4〜6 は 2、7〜9 は 3。光はその合計、闇は切り上げ平均。
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
        return AttributeBand(r);
    }

    public static long GetGreen(int r, int g, int b)
    {
        return AttributeBand(g);
    }

    public static long GetBlue(int r, int g, int b)
    {
        return AttributeBand(b);
    }

    public static long GetLight(int r, int g, int b)
    {
        return AttributeBand(r) + AttributeBand(g) + AttributeBand(b);
    }

    // 3色の段階の平均。端数は切り上げ。
    public static long GetDark(int r, int g, int b)
    {
        long sum = AttributeBand(r) + AttributeBand(g) + AttributeBand(b);
        return BattleMath.CeilDivPositive(sum, 3);
    }

    // 階調番号 1〜3 は 1、4〜6 は 2、7〜9 は 3。10 以降も 3。
    public static int AttributeBand(int channel)
    {
        return BandOfStep(ChannelStep(channel));
    }

    public static int BandOfStep(int step)
    {
        if (step < 1)
            step = 1;
        int band = (step - 1) / 3 + 1;
        if (band > 3)
            band = 3;
        return band;
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

public static class BattleLogFormat
{
    public static string Name(BattleUnit unit)
    {
        if (unit == null)
            return "";
        return Colored(unit.Name, unit.Attribute);
    }

    public static string Name(CharacterClass character)
    {
        if (character == null)
            return "";
        return Colored(character.Name, BattleCharacterFactory.ToAttribute(character.CharacterType));
    }

    // 味方は左、敵は右。矢印は攻撃した側から受ける側へ向ける。
    public static string Attack(BattleUnit actor, BattleUnit target, string tail)
    {
        if (actor == null)
            return tail ?? "";
        if (target == null)
            return Name(actor) + (tail ?? "");
        BattleUnit ally = actor.IsAlly ? actor : target;
        BattleUnit enemy = actor.IsAlly ? target : actor;
        string arrow = actor.IsAlly ? " → " : " ← ";
        return Name(ally) + arrow + Name(enemy) + (tail ?? "");
    }

    public static string Drop(string text)
    {
        return "<color=#5CFF78>" + text + "</color>";
    }

    static string Colored(string name, CharacterAttribute attribute)
    {
        if (string.IsNullOrEmpty(name))
            name = AttributeColorValue.DisplayName(attribute);
        return "<color=" + AttributeColor(attribute) + ">" + name + "</color>";
    }

    static string AttributeColor(CharacterAttribute attribute)
    {
        switch (attribute)
        {
            case CharacterAttribute.Fire: return "#FF4A3A";
            case CharacterAttribute.Grass: return "#3DDC5A";
            case CharacterAttribute.Water: return "#4C8CFF";
            case CharacterAttribute.Light: return "#FFE14A";
            case CharacterAttribute.Dark: return "#C06BFF";
            default: return "#E0E0E6";
        }
    }
}
