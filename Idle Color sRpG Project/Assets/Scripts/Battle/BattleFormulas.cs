using System;

public static class BattleDamageCalculator
{
    public static long Calculate(long attack, long defense, BattleBalanceConfig config)
    {
        return Calculate(attack, defense, config, AttackType.Normal);
    }

    public static long Calculate(long attack, long defense, BattleBalanceConfig config, AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Normal:
                return CalculateNormal(attack, defense, config);
            default:
                return CalculateNormal(attack, defense, config);
        }
    }

    public static long CalculateNormal(long attack, long defense, BattleBalanceConfig config)
    {
        long minimum = config != null ? config.MinimumDamage : 1;
        if (minimum < 0)
            minimum = 0;
        long raw = attack - defense;
        if (raw < minimum)
            return minimum;
        return raw;
    }
}

public enum AttackType
{
    Normal = 0
}

public static class BattleActionCount
{
    public static int Count(long spd, long baseSpd, BattleBalanceConfig config)
    {
        int min = config.MinActions;
        int max = config.MaxActions;
        if (max < min)
            max = min;
        if (baseSpd < 1)
            baseSpd = 1;
        if (spd < 0)
            spd = 0;
        int count = (int)(spd / baseSpd);
        return BattleMath.Clamp(count, min, max);
    }
}

public static class BattleRewardCalculator
{
    public static long RgbGain(long obs, long opaquePixels, long attributeValue, BattleBalanceConfig config)
    {
        double divisor = config.RgbDivisor;
        if (divisor <= 0.0)
            divisor = 1.0;
        if (obs < 0)
            obs = 0;
        if (opaquePixels < 0)
            opaquePixels = 0;
        if (attributeValue < 0)
            attributeValue = 0;
        double raw = (obs * (double)opaquePixels * attributeValue) / divisor;
        return BattleMath.CeilToLong(raw);
    }

    public static long ExpGain(long opaquePixels, long attributeValue, BattleBalanceConfig config)
    {
        if (opaquePixels < 0)
            opaquePixels = 0;
        if (attributeValue < 0)
            attributeValue = 0;
        double multiplier = config.ExpMultiplier;
        if (multiplier < 0.0)
            multiplier = 0.0;
        return BattleMath.CeilToLong(opaquePixels * (double)attributeValue * multiplier);
    }

    public static double ExpOrbDropProbability(long luc, BattleBalanceConfig config)
    {
        if (luc < 0)
            luc = 0;
        return BattleMath.Clamp01(luc * config.ExpOrbRatePerLuc, config.ExpOrbRateCap);
    }

    public static double RecruitProbability(long luc, long obs, BattleBalanceConfig config)
    {
        if (luc < 0)
            luc = 0;
        if (obs < 0)
            obs = 0;
        return BattleMath.Clamp01((luc + obs) * config.RecruitRatePerPoint, config.RecruitRateCap);
    }

    // 赤緑青はその色へ。光と闇は合計が取得量と一致するよう R, G, B に分ける。
    public static void SplitRgb(CharacterAttribute attribute, long amount, out long r, out long g, out long b)
    {
        r = 0;
        g = 0;
        b = 0;
        if (amount <= 0)
            return;

        switch (attribute)
        {
            case CharacterAttribute.Fire:
                r = amount;
                break;
            case CharacterAttribute.Grass:
                g = amount;
                break;
            case CharacterAttribute.Water:
                b = amount;
                break;
            case CharacterAttribute.Light:
            case CharacterAttribute.Dark:
                r = amount / 3;
                g = amount / 3;
                b = amount / 3;
                long remainder = amount - (r + g + b);
                if (remainder >= 1)
                    r++;
                if (remainder >= 2)
                    g++;
                break;
            default:
                break;
        }
    }
}

public struct LevelGrowth
{
    public long NewHpMax;
    public long HpDelta;
    public long NewAtk;
    public long AtkDelta;
    public long NewDef;
    public long DefDelta;
    public long NewSpd;
    public long SpdDelta;
}

public static class BattleLevelGrowth
{
    public static LevelGrowth Grow(long hpMax, long atk, long def, long spd, BattleBalanceConfig config)
    {
        LevelGrowth growth = new LevelGrowth();
        growth.NewHpMax = GrowStat(hpMax, config.HpGrowthRate);
        growth.NewAtk = GrowStat(atk, config.AtkGrowthRate);
        growth.NewDef = GrowStat(def, config.DefGrowthRate);
        growth.NewSpd = GrowStat(spd, config.SpdGrowthRate);
        if (growth.NewHpMax < 1)
            growth.NewHpMax = 1;
        if (growth.NewSpd < 1)
            growth.NewSpd = 1;
        growth.HpDelta = growth.NewHpMax - (hpMax < 0 ? 0 : hpMax);
        growth.AtkDelta = growth.NewAtk - (atk < 0 ? 0 : atk);
        growth.DefDelta = growth.NewDef - (def < 0 ? 0 : def);
        growth.SpdDelta = growth.NewSpd - (spd < 0 ? 0 : spd);
        if (growth.HpDelta < 0)
            growth.HpDelta = 0;
        if (growth.AtkDelta < 0)
            growth.AtkDelta = 0;
        if (growth.DefDelta < 0)
            growth.DefDelta = 0;
        if (growth.SpdDelta < 0)
            growth.SpdDelta = 0;
        return growth;
    }

    static long GrowStat(long current, double rate)
    {
        if (current < 0)
            current = 0;
        if (rate < 0.0)
            rate = 0.0;
        return BattleMath.CeilToLong(current * (1.0 + rate));
    }
}

public static class BattleExperience
{
    public static int Add(ref long level, ref long exp, ref long expMax, long gain, BattleBalanceConfig config)
    {
        return Add(ref level, ref exp, ref expMax, gain, config, null);
    }

    public static int Add(ref long level, ref long exp, ref long expMax, long gain, BattleBalanceConfig config, System.Action onLevel)
    {
        if (gain < 0)
            gain = 0;
        if (level < 0)
            level = 0;
        if (exp < 0)
            exp = 0;
        if (expMax <= 0)
            expMax = config.ExpToNext(level);

        exp += gain;
        int levelsGained = 0;
        int guard = config.MaxLevelsPerExpGrant;
        if (guard < 1)
            guard = 1;

        while (levelsGained < guard && level < config.MaxLevel && exp >= expMax)
        {
            exp -= expMax;
            level++;
            levelsGained++;
            if (onLevel != null)
                onLevel();
            expMax = config.ExpToNext(level);
        }

        if (level >= config.MaxLevel)
        {
            long cap = expMax - 1;
            if (cap < 0)
                cap = 0;
            if (exp > cap)
                exp = cap;
        }

        return levelsGained;
    }
}
