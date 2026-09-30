// 弱点タイプ。赤←青←緑←赤。光と闇は互いに弱点。
public static class AttributeWeakness
{
    public static CharacterAttribute Of(CharacterAttribute type)
    {
        switch (type)
        {
            case CharacterAttribute.Fire:
                return CharacterAttribute.Water;
            case CharacterAttribute.Grass:
                return CharacterAttribute.Fire;
            case CharacterAttribute.Water:
                return CharacterAttribute.Grass;
            case CharacterAttribute.Light:
                return CharacterAttribute.Dark;
            case CharacterAttribute.Dark:
                return CharacterAttribute.Light;
            default:
                return CharacterAttribute.None;
        }
    }

    public static bool IsHit(CharacterAttribute attacker, CharacterAttribute defenderWeakness)
    {
        return defenderWeakness != CharacterAttribute.None && attacker == defenderWeakness;
    }
}
