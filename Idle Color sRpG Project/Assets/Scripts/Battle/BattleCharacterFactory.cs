using UnityEngine;

public static class BattleCharacterFactory
{
    public static CharacterAttribute ToAttribute(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Fire: return CharacterAttribute.Fire;
            case CharacterType.Grass: return CharacterAttribute.Grass;
            case CharacterType.Water: return CharacterAttribute.Water;
            case CharacterType.Light: return CharacterAttribute.Light;
            case CharacterType.Dark: return CharacterAttribute.Dark;
            default: return CharacterAttribute.None;
        }
    }

    public static BattleUnit CreateAlly(CharacterClass character, int x, int y, int unitId, BattleBalanceConfig config)
    {
        BattleUnit unit = Create(character, true, x, y, unitId);
        if (unit == null)
            return null;
        int owned = character.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)character.OwnedNumCur;
        unit.Lives = ClampLives(owned, config);
        unit.Level = character.Level > int.MaxValue ? int.MaxValue : (int)character.Level;
        return unit;
    }

    public static BattleUnit CreateEnemy(CharacterClass character, int x, int y, int unitId, double statMultiplier, int level, BattleBalanceConfig config)
    {
        BattleUnit unit = Create(character, false, x, y, unitId);
        if (unit == null)
            return null;
        ApplyEnemyLevel(character, unit, level, config);
        if (statMultiplier < 0.0)
            statMultiplier = 0.0;
        if (statMultiplier != 1.0)
        {
            unit.HpMax = Scale(unit.HpMax, statMultiplier);
            unit.Atk = Scale(unit.Atk, statMultiplier);
            unit.Def = Scale(unit.Def, statMultiplier);
            unit.Spd = Scale(unit.Spd, statMultiplier);
        }
        if (unit.HpMax < 1)
            unit.HpMax = 1;
        if (unit.Spd < 1)
            unit.Spd = 1;
        unit.Hp = unit.HpMax;
        unit.Lives = ClampLives(config.EnemyInitialLives, config);
        unit.Level = level < 0 ? 0 : level;
        return unit;
    }

    static BattleUnit Create(CharacterClass character, bool ally, int x, int y, int unitId)
    {
        if (character == null || character.ID == 0 || character.Stats == null || character.Stats[0] == null)
            return null;

        var unit = new BattleUnit();
        unit.UnitId = unitId;
        unit.IsAlly = ally;
        unit.X = x;
        unit.Y = y;
        unit.CharacterId = character.ID;
        unit.Name = string.IsNullOrEmpty(character.Name) ? "キャラ" + character.ID.ToString() : character.Name;
        unit.Size = character.Size;
        unit.HpMax = (long)character.Stats[0].HPMax;
        if (unit.HpMax < 1)
            unit.HpMax = 1;
        unit.Hp = unit.HpMax;
        unit.Atk = (long)character.Stats[0].ATK;
        unit.Def = (long)character.Stats[0].DEF;
        unit.Spd = character.Stats[0].SPD;
        if (unit.Spd < 1)
            unit.Spd = 1;
        unit.Luc = (long)character.Stats[0].LUC;
        unit.Obs = (long)character.Stats[0].OBS;
        unit.Attribute = ToAttribute(character.CharacterType);
        unit.InBattle = true;
        ReadPixels(character, unit);
        return unit;
    }

    static void ReadPixels(CharacterClass character, BattleUnit unit)
    {
        int opaque = 0;
        if (character.Size > 0)
        {
            int area = character.Size * character.Size;
            if (character.APixels < (uint)area)
                opaque = area - (int)character.APixels;
        }
        unit.OpaquePixels = opaque;
        unit.RepresentativeR = character.RepresentativeR;
        unit.RepresentativeG = character.RepresentativeG;
        unit.RepresentativeB = character.RepresentativeB;
        unit.WeaknessR = character.WeaknessR;
        unit.WeaknessG = character.WeaknessG;
        unit.WeaknessB = character.WeaknessB;
        unit.WeaknessAttribute = ToAttribute(character.WeaknessType);
    }

    static void ApplyEnemyLevel(CharacterClass character, BattleUnit unit, int level, BattleBalanceConfig config)
    {
        if (character.Stats == null || character.Stats[1] == null)
            return;
        long grownHp;
        long grownAtk;
        long grownDef;
        long grownSpd;
        BattleLevelGrowth.ApplyLevels(
            (long)character.Stats[1].HPMax,
            (long)character.Stats[1].ATK,
            (long)character.Stats[1].DEF,
            character.Stats[1].SPD,
            level,
            config,
            out grownHp,
            out grownAtk,
            out grownDef,
            out grownSpd);
        unit.HpMax = grownHp < 1 ? 1 : grownHp;
        unit.Hp = unit.HpMax;
        unit.Atk = grownAtk < 0 ? 0 : grownAtk;
        unit.Def = grownDef < 0 ? 0 : grownDef;
        unit.Spd = grownSpd < 1 ? 1 : grownSpd;
        unit.Luc = (long)character.Stats[1].LUC;
        unit.Obs = (long)character.Stats[1].OBS;
    }

    static long Scale(long value, double multiplier)
    {
        if (value < 0)
            value = 0;
        return BattleMath.CeilToLong(value * multiplier);
    }

    static int ClampLives(int lives, BattleBalanceConfig config)
    {
        return BattleMath.Clamp(lives, 0, config.MaxLives);
    }
}
