using System;
using System.Collections.Generic;
using System.Globalization;

// アプリ内放置とオフライン報酬の計算。広告・課金の回数はここへ足すだけで、付与そのものは既存の処理を使う。
public sealed class IdleRewardState
{
    public const int ActiveBaseMinutes = 10;
    public const int ActivePerAdMinutes = 1;
    public const int ActivePerPurchaseMinutes = 10;
    public const int WarnAfterMinutes = 3;
    public const int OfflineBaseMinutes = 180;
    public const int OfflinePerAdMinutes = 10;
    public const int OfflinePerPurchaseMinutes = 60;
    public const int OfflineChunkMinutes = 10;
    public const int Percent = 10;

    public const int BankR = 0;
    public const int BankG = 1;
    public const int BankB = 2;
    public const int BankPixel = 3;
    public const int BankPaint = 4;
    public const int BankBattleExp = 5;
    public const int BankBattleRgb = 6;
    public const int BankBattlePixel = 7;
    const int BankCount = 8;

    public long AdSuccessCount;
    public long PurchaseSuccessCount;
    public bool HasPurchaseCount;
    public long LastInputUnixMs;
    public bool Limited;
    public long BackgroundUnixMs;
    public long ClaimedBackgroundUnixMs;
    public long OfferElapsedMs;
    public bool ClockWentBack;
    public bool ClockJumped;
    public bool HasSeenOrbs;
    public readonly List<int> SeenOrbs = new List<int>();
    readonly long[] _bankMilli = new long[BankCount];

    public int ActiveCapMinutes()
    {
        return CapMinutes(ActiveBaseMinutes, ActivePerAdMinutes, ActivePerPurchaseMinutes);
    }

    public int OfflineCapMinutes()
    {
        return CapMinutes(OfflineBaseMinutes, OfflinePerAdMinutes, OfflinePerPurchaseMinutes);
    }

    int CapMinutes(int baseMinutes, int perAd, int perPurchase)
    {
        long ads = AdSuccessCount < 0 ? 0 : AdSuccessCount;
        long buys = PurchaseSuccessCount < 0 ? 0 : PurchaseSuccessCount;
        long total = baseMinutes;
        total = AddCapped(total, Mul(ads, perAd));
        total = AddCapped(total, Mul(buys, perPurchase));
        if (total > int.MaxValue)
            return int.MaxValue;
        return (int)total;
    }

    public static int RewardCycles(long elapsedMs, int capMinutes)
    {
        long minutes = RewardMinutes(elapsedMs, capMinutes);
        if (minutes < OfflineChunkMinutes)
            return 0;
        long cycles = minutes / OfflineChunkMinutes;
        if (cycles > int.MaxValue)
            return int.MaxValue;
        return (int)cycles;
    }

    public static long RewardMinutes(long elapsedMs, int capMinutes)
    {
        if (elapsedMs < 0)
            elapsedMs = 0;
        if (capMinutes < 0)
            capMinutes = 0;
        long minutes = elapsedMs / 60000L;
        if (minutes > capMinutes)
            minutes = capMinutes;
        return minutes;
    }

    public static long ElapsedMs(long fromUnixMs, long nowUnixMs, out bool wentBack, out bool jumped)
    {
        wentBack = false;
        jumped = false;
        if (fromUnixMs <= 0 || nowUnixMs <= 0)
            return 0;
        if (nowUnixMs < fromUnixMs)
        {
            wentBack = true;
            return 0;
        }
        long elapsed = nowUnixMs - fromUnixMs;
        const long sane = 10L * 365L * 24L * 60L * 60L * 1000L;
        if (elapsed > sane)
        {
            jumped = true;
            return sane;
        }
        return elapsed;
    }

    public static bool ShowOfflineAd(long elapsedMs, int capMinutes)
    {
        if (elapsedMs < 0)
            elapsedMs = 0;
        if (capMinutes < 1)
            return false;
        long capMs = (long)capMinutes * 60000L;
        if (capMs < 0)
            return true;
        return elapsedMs >= capMs / 2L;
    }

    public static IdlePayout BuildPayout(int cycles, int multiplier, IReadOnlyList<int> seenOrbs)
    {
        IdlePayout payout = new IdlePayout();
        if (cycles < 0)
            cycles = 0;
        if (multiplier < 1)
            multiplier = 1;
        long times = Mul(cycles, multiplier);
        payout.Cycles = cycles;
        payout.Multiplier = multiplier;
        payout.Red = Mul(times, 500L);
        payout.Green = payout.Red;
        payout.Blue = payout.Red;
        payout.AllPixels = Mul(times, 10L);
        payout.OrbEach = times > int.MaxValue ? int.MaxValue : (int)times;
        if (seenOrbs != null)
        {
            for (int i = 0; i < seenOrbs.Count; i++)
            {
                int size = seenOrbs[i];
                if (!ItemIds.IsExpOrb(size))
                    continue;
                if (payout.OrbSizes == null)
                    payout.OrbSizes = new List<int>();
                if (!payout.OrbSizes.Contains(size))
                    payout.OrbSizes.Add(size);
            }
            if (payout.OrbSizes != null)
                payout.OrbSizes.Sort();
        }
        return payout;
    }

    public long Scale(long amount, int bank)
    {
        if (!Limited || amount <= 0)
            return amount < 0 ? 0 : amount;
        if (bank < 0 || bank >= BankCount)
            return amount;
        long add = Mul(amount, Percent);
        _bankMilli[bank] = AddCapped(_bankMilli[bank], add);
        long whole = _bankMilli[bank] / 100L;
        _bankMilli[bank] -= whole * 100L;
        return whole;
    }

    public void NoteAdSuccess()
    {
        if (AdSuccessCount < long.MaxValue)
            AdSuccessCount++;
    }

    public void NotePurchase()
    {
        HasPurchaseCount = true;
        if (PurchaseSuccessCount < long.MaxValue)
            PurchaseSuccessCount++;
    }

    public void NoteOrb(int itemId)
    {
        if (!ItemIds.IsExpOrb(itemId))
            return;
        HasSeenOrbs = true;
        if (!SeenOrbs.Contains(itemId))
            SeenOrbs.Add(itemId);
    }

    public int MaxSeenOrb()
    {
        int best = 0;
        for (int i = 0; i < SeenOrbs.Count; i++)
        {
            int size = SeenOrbs[i];
            if (ItemIds.IsExpOrb(size) && size > best)
                best = size;
        }
        if (best == 0)
            return ItemIds.ExpOrb8;
        return best;
    }

    public void SeedOrbs(Dictionary<int, int> itemCounts)
    {
        if (HasSeenOrbs || itemCounts == null)
            return;
        foreach (KeyValuePair<int, int> pair in itemCounts)
        {
            if (pair.Value > 0)
                NoteOrb(pair.Key);
        }
        HasSeenOrbs = true;
    }

    public int SeedPurchases(bool plus1, bool plus3, bool plus5, bool plus10)
    {
        if (HasPurchaseCount)
            return PurchaseSuccessCount > int.MaxValue ? int.MaxValue : (int)PurchaseSuccessCount;
        int count = 0;
        if (plus1)
            count++;
        if (plus3)
            count++;
        if (plus5)
            count++;
        if (plus10)
            count++;
        PurchaseSuccessCount = count;
        HasPurchaseCount = true;
        return count;
    }

    public void MarkBackground(long nowUnixMs)
    {
        if (BackgroundUnixMs > 0)
            return;
        if (nowUnixMs < 0)
            nowUnixMs = 0;
        BackgroundUnixMs = nowUnixMs;
    }

    public bool TryRead(string key, string value)
    {
        if (string.IsNullOrEmpty(key) || !key.StartsWith("Idle", StringComparison.Ordinal))
            return false;
        long number;
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
        if (key == "IdleAdSuccess")
            AdSuccessCount = number < 0 ? 0 : number;
        else if (key == "IdlePurchaseSuccess")
        {
            PurchaseSuccessCount = number < 0 ? 0 : number;
            HasPurchaseCount = true;
        }
        else if (key == "IdleLastInput")
            LastInputUnixMs = number < 0 ? 0 : number;
        else if (key == "IdleLimited")
            Limited = value == "true";
        else if (key == "IdleBackground")
            BackgroundUnixMs = number < 0 ? 0 : number;
        else if (key == "IdleClaimedBackground")
            ClaimedBackgroundUnixMs = number < 0 ? 0 : number;
        else if (key == "IdleOfferElapsed")
            OfferElapsedMs = number < 0 ? 0 : number;
        else if (key == "IdleClockBack")
            ClockWentBack = value == "true";
        else if (key == "IdleClockJump")
            ClockJumped = value == "true";
        else if (key == "IdleSeenOrbs")
        {
            HasSeenOrbs = true;
            SeenOrbs.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                string[] parts = value.Split('|');
                for (int i = 0; i < parts.Length; i++)
                {
                    int size;
                    if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out size))
                        NoteOrb(size);
                }
            }
        }
        else if (key.StartsWith("IdleBank", StringComparison.Ordinal))
        {
            int index;
            if (int.TryParse(key.Substring(8), NumberStyles.Integer, CultureInfo.InvariantCulture, out index)
                && index >= 0 && index < BankCount)
                _bankMilli[index] = number < 0 ? 0 : number;
        }
        return true;
    }

    public void Write(System.IO.StreamWriter sw)
    {
        if (sw == null)
            return;
        sw.WriteLine("IdleAdSuccess," + AdSuccessCount.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine("IdlePurchaseSuccess," + PurchaseSuccessCount.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine("IdleLastInput," + LastInputUnixMs.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine("IdleLimited," + (Limited ? "true" : "false"));
        sw.WriteLine("IdleBackground," + BackgroundUnixMs.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine("IdleClaimedBackground," + ClaimedBackgroundUnixMs.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine("IdleOfferElapsed," + OfferElapsedMs.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine("IdleClockBack," + (ClockWentBack ? "true" : "false"));
        sw.WriteLine("IdleClockJump," + (ClockJumped ? "true" : "false"));
        string orbs = "";
        for (int i = 0; i < SeenOrbs.Count; i++)
        {
            if (i > 0)
                orbs += "|";
            orbs += SeenOrbs[i].ToString(CultureInfo.InvariantCulture);
        }
        sw.WriteLine("IdleSeenOrbs," + orbs);
        for (int i = 0; i < BankCount; i++)
            sw.WriteLine("IdleBank" + i.ToString(CultureInfo.InvariantCulture) + "," + _bankMilli[i].ToString(CultureInfo.InvariantCulture));
    }

    public static string FormatSpan(long milliseconds)
    {
        if (milliseconds < 0)
            milliseconds = 0;
        long minutes = milliseconds / 60000L;
        long hours = minutes / 60L;
        long rest = minutes % 60L;
        if (hours > 100000L)
            return "100000時間以上";
        return hours.ToString(CultureInfo.InvariantCulture) + "時間" + rest.ToString(CultureInfo.InvariantCulture) + "分";
    }

    static long Mul(long a, long b)
    {
        if (a <= 0 || b <= 0)
            return 0;
        if (a > long.MaxValue / b)
            return long.MaxValue;
        return a * b;
    }

    static long AddCapped(long a, long b)
    {
        if (b <= 0)
            return a < 0 ? 0 : a;
        if (a < 0)
            a = 0;
        if (a > long.MaxValue - b)
            return long.MaxValue;
        return a + b;
    }
}

public struct IdlePayout
{
    public int Cycles;
    public int Multiplier;
    public long Red;
    public long Green;
    public long Blue;
    public long AllPixels;
    public int OrbEach;
    public List<int> OrbSizes;
}

// 操作の記録だけ。同じボタンの連打では不正にしない。ペナルティは付けない。
public sealed class IdleTouchLog
{
    const int Capacity = 64;
    readonly string[] _names = new string[Capacity];
    readonly long[] _times = new long[Capacity];
    int _count;
    int _next;
    long _lastReportMs;

    public void Record(string name, long nowUnixMs)
    {
        if (string.IsNullOrEmpty(name))
            name = "pointer";
        _names[_next] = name;
        _times[_next] = nowUnixMs;
        _next = (_next + 1) % Capacity;
        if (_count < Capacity)
            _count++;
        string reason = Detect();
        if (reason == null)
            return;
        if (nowUnixMs - _lastReportMs < 30000L)
            return;
        _lastReportMs = nowUnixMs;
        UnityEngine.Debug.Log("操作の検知候補: " + reason + " 直近5回 " + RecentText());
    }

    public string RecentText()
    {
        int n = _count < 5 ? _count : 5;
        string text = "";
        for (int i = n - 1; i >= 0; i--)
        {
            if (text.Length > 0)
                text += " / ";
            text += At(i);
        }
        return text;
    }

    string Detect()
    {
        if (_count < 24)
            return null;
        bool same = true;
        string first = At(0);
        for (int i = 1; i < 24; i++)
        {
            if (At(i) != first)
            {
                same = false;
                break;
            }
        }
        long fastest = long.MaxValue;
        long slowest = 0;
        bool inhuman = true;
        long previous = TimeAt(0);
        for (int i = 1; i < 24; i++)
        {
            long gap = previous - TimeAt(i);
            previous = TimeAt(i);
            if (gap < 0)
                gap = 0;
            if (gap < fastest)
                fastest = gap;
            if (gap > slowest)
                slowest = gap;
            if (gap >= 35L)
                inhuman = false;
        }
        if (inhuman)
            return "操作間隔が35ミリ秒未満のまま続いています";
        if (same)
            return null;
        if (slowest - fastest <= 20L)
            return "操作間隔が一定のまま続いています";
        if (RepeatingPattern())
            return "同じ5操作の並びが繰り返されています";
        return null;
    }

    bool RepeatingPattern()
    {
        if (_count < 30)
            return false;
        for (int block = 1; block < 6; block++)
        {
            for (int i = 0; i < 5; i++)
            {
                if (At(i) != At(block * 5 + i))
                    return false;
            }
        }
        bool identical = true;
        for (int i = 1; i < 5; i++)
        {
            if (At(i) != At(0))
                identical = false;
        }
        return !identical;
    }

    string At(int newestIndex)
    {
        int index = _next - 1 - newestIndex;
        while (index < 0)
            index += Capacity;
        return _names[index] ?? "";
    }

    long TimeAt(int newestIndex)
    {
        int index = _next - 1 - newestIndex;
        while (index < 0)
            index += Capacity;
        return _times[index];
    }
}
