// 残機合成。合成回数1回につき、基礎＋レベル成長後の HP/ATK/DEF と RGB生産量を5%増やす。
// 次に必要な残機は 2、3、4…。消費したあとに1体以上残るときだけ合成できる。
public static class FusionBonus
{
    public const long PercentPerFusion = 5;
    // 1体あたりの合成回数。最初は5。100階をクリアするたびに5上がる。
    public const int InitialCap = 5;
    public const int CapPerHundredFloors = 5;
    public const int CapCeiling = 9999;
    public static int MaxCount = InitialCap;

    // クエストで足す合成効率。合成1回あたりの5%とは別に、全体へ足す。
    public static int ExtraPercent;

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
        long cap = MaxCount;
        if (cap < InitialCap)
            cap = InitialCap;
        if (cap > CapCeiling)
            cap = CapCeiling;
        if (fusionCount >= cap)
            return false;
        long cost = NextCost(fusionCount);
        return lives > cost;
    }

    public static long CountPercent(long fusionCount)
    {
        if (fusionCount < 0)
            fusionCount = 0;
        if (fusionCount > long.MaxValue / PercentPerFusion)
            return long.MaxValue;
        return fusionCount * PercentPerFusion;
    }

    public static long Percent(long fusionCount)
    {
        long fromCount = CountPercent(fusionCount);
        long extra = ExtraPercent;
        if (extra < 0)
            extra = 0;
        if (fromCount > long.MaxValue - extra)
            return long.MaxValue;
        return fromCount + extra;
    }

    public static long Bonus(long grownStat, long fusionCount)
    {
        return Bonus(grownStat, fusionCount, true);
    }

    // includeExtra が false のときは、クエストの合成効率を足さない。敵はこちら。
    public static long Bonus(long grownStat, long fusionCount, bool includeExtra)
    {
        if (fusionCount < 0)
            fusionCount = 0;
        if (fusionCount <= 0 && (!includeExtra || ExtraPercent <= 0))
            return 0;
        if (grownStat < 0)
            grownStat = 0;
        if (grownStat >= long.MaxValue)
            return 0;

        long bonus = 0;
        long percent = includeExtra ? Percent(fusionCount) : CountPercent(fusionCount);
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
