using System;

// 所持数の ×1、×0.8、×0.5。合計が今までの所持数。
public struct OwnedStock
{
    public long Full;
    public long Eight;
    public long Half;
    public bool Specified;

    public long Total
    {
        get
        {
            long total = 0;
            total = AddCapped(total, Full);
            total = AddCapped(total, Eight);
            total = AddCapped(total, Half);
            return total;
        }
    }

    public void AdoptLegacy(long ownedCur)
    {
        if (Specified)
            return;
        if (ownedCur < 0)
            ownedCur = 0;
        Full = ownedCur;
        Eight = 0;
        Half = 0;
        Specified = true;
    }

    public void Add(OwnedTier tier, int amount, int cap)
    {
        if (amount <= 0)
            return;
        long room = Room(cap);
        if (room <= 0)
            return;
        if (amount > room)
            amount = (int)room;
        if (tier == OwnedTier.Eight)
            Eight += amount;
        else if (tier == OwnedTier.Half)
            Half += amount;
        else
            Full += amount;
    }

    public void Consume(int amount, bool halfFirst)
    {
        if (amount <= 0)
            return;
        long left = amount;
        if (halfFirst)
        {
            Take(ref Half, ref left);
            Take(ref Eight, ref left);
            Take(ref Full, ref left);
        }
        else
        {
            Take(ref Full, ref left);
            Take(ref Eight, ref left);
            Take(ref Half, ref left);
        }
    }

    public void MatchTotal(int target, int cap)
    {
        if (target < 0)
            target = 0;
        if (cap >= 0 && target > cap)
            target = cap;
        long total = Total;
        if (target < total)
        {
            long drop = total - target;
            if (drop > int.MaxValue)
                drop = int.MaxValue;
            Consume((int)drop, false);
        }
        else if (target > total)
        {
            long add = target - total;
            if (add > int.MaxValue)
                add = int.MaxValue;
            Add(OwnedTier.Full, (int)add, cap);
        }
    }

    public void ClampTotal(int cap)
    {
        if (cap < 0)
            return;
        long extra = Total - cap;
        if (extra <= 0)
            return;
        if (extra > int.MaxValue)
            extra = int.MaxValue;
        Consume((int)extra, true);
    }

    public double NextLifeMultiplier(PersonBalanceConfig config)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        if (Full > 0)
            return config.StockFull;
        if (Eight > 0)
            return config.StockEight;
        if (Half > 0)
            return config.StockHalf;
        return config.StockFull;
    }

    public static string Format(long full, long eight, long half)
    {
        return Format(full, eight, half, false);
    }

    public static string Format(long full, long eight, long half, bool compact)
    {
        long total = 0;
        total = AddCapped(total, full);
        total = AddCapped(total, eight);
        total = AddCapped(total, half);
        string detail = compact
            ? "×1:" + full.ToString() + "\n×0.8:" + eight.ToString() + " ×0.5:" + half.ToString()
            : "×1: " + full.ToString() + " ×0.8: " + eight.ToString() + " ×0.5: " + half.ToString();
        return total.ToString() + "\n" + detail;
    }

    long Room(int cap)
    {
        if (cap < 0)
            return int.MaxValue;
        long room = cap - Total;
        if (room < 0)
            return 0;
        if (room > int.MaxValue)
            return int.MaxValue;
        return room;
    }

    static void Take(ref long bucket, ref long left)
    {
        if (left <= 0 || bucket <= 0)
            return;
        long used = bucket < left ? bucket : left;
        bucket -= used;
        left -= used;
    }

    static long AddCapped(long total, long add)
    {
        if (add <= 0)
            return total;
        if (total > long.MaxValue - add)
            return long.MaxValue;
        return total + add;
    }
}
