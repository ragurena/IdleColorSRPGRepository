public enum KnockoutResult
{
    Alive,
    Revived,
    Defeated
}

public sealed class BattleUnit
{
    public int UnitId;
    public bool IsAlly;
    public int X;
    public int Y;
    public uint CharacterId;
    public string Name;
    public int Size;
    public long Hp;
    public long HpMax;
    public long Atk;
    public long Def;
    public long Spd;
    public long Luc;
    public long Obs;
    public int Lives;
    public int OpaquePixels;
    public int RepresentativeR;
    public int RepresentativeG;
    public int RepresentativeB;
    public CharacterAttribute Attribute;
    public bool InBattle = true;
    public int ActionsThisTurn;

    public bool IsLiving()
    {
        return InBattle && Hp > 0;
    }
}

public static class BattleKnockout
{
    public static KnockoutResult Apply(BattleUnit unit, long damage, int maxLives)
    {
        if (unit == null || !unit.IsLiving())
            return KnockoutResult.Defeated;
        if (damage < 0)
            damage = 0;

        if (unit.Hp > damage)
        {
            unit.Hp -= damage;
            return KnockoutResult.Alive;
        }

        if (unit.Lives >= 1)
        {
            unit.Lives--;
            if (unit.Lives > maxLives)
                unit.Lives = maxLives;
            unit.Hp = unit.HpMax;
            if (unit.Hp < 1)
                unit.Hp = 1;
            unit.InBattle = true;
            return KnockoutResult.Revived;
        }

        unit.Hp = 0;
        unit.InBattle = false;
        return KnockoutResult.Defeated;
    }
}
