// 残機合成。合成回数1回につき、基礎＋レベル成長後の HP/ATK/DEF と RGB生産量を5%増やす。
// 次に必要な残機は 2、3、4…。消費したあとに1体以上残るときだけ合成できる。
public static class FusionBonus
{
    public const long PercentPerFusion = 5;

    public static long NextCost(long fusionCount)
    {
        if (fusionCount < 0)
            fusionCount = 0;
        if (fusionCount > long.MaxValue - 2)
            return long.MaxValue;
        return fusionCount + 2;
    }

    public static bool CanFuse(long lives, long fusionCount)
    {
        if (lives < 1)
            return false;
        long cost = NextCost(fusionCount);
        return lives > cost;
    }

    public static long Percent(long fusionCount)
    {
        if (fusionCount <= 0)
            return 0;
        if (fusionCount > long.MaxValue / PercentPerFusion)
            return long.MaxValue;
        return fusionCount * PercentPerFusion;
    }

    public static long Bonus(long grownStat, long fusionCount)
    {
        if (fusionCount <= 0)
            return 0;
        if (grownStat < 0)
            grownStat = 0;
        if (grownStat >= long.MaxValue)
            return 0;

        long bonus = 0;
        long percent = Percent(fusionCount);
        if (grownStat > 0 && percent > 0)
        {
            if (percent <= (long.MaxValue - 99) / grownStat)
                bonus = (grownStat * percent + 99) / 100;
            else
            {
                double raw = ((double)grownStat * (double)percent) / 100.0;
                if (double.IsNaN(raw) || double.IsInfinity(raw) || raw >= (double)long.MaxValue)
                    bonus = long.MaxValue - grownStat;
                else
                    bonus = BattleMath.CeilToLong(raw);
            }
        }

        if (bonus < fusionCount)
            bonus = fusionCount;
        // 足した最終値が long を超えるときだけ、残り幅で止める。
        if (bonus > long.MaxValue - grownStat)
            return long.MaxValue - grownStat;
        return bonus;
    }
}
