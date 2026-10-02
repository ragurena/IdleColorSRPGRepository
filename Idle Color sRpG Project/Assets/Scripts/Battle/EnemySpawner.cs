using System.Collections.Generic;

public class EnemySpawnEntry
{
    public uint CharacterId;
    public int Weight;
    public int MinCount;
    public int MaxCount;
    public int Level;

    public EnemySpawnEntry(uint characterId, int weight, int minCount, int maxCount)
        : this(characterId, weight, minCount, maxCount, 0)
    {
    }

    public EnemySpawnEntry(uint characterId, int weight, int minCount, int maxCount, int level)
    {
        CharacterId = characterId;
        Weight = weight;
        MinCount = minCount;
        MaxCount = maxCount < minCount ? minCount : maxCount;
        Level = level < 0 ? 0 : level;
    }
}

public class FloorBandSpawn
{
    public List<EnemySpawnEntry> Entries = new List<EnemySpawnEntry>();
    public double StatMultiplier = 1.0;
}

public class BossFloorSpawn
{
    public int Floor;
    public uint CharacterId;
    public int MinCount;
    public int MaxCount;
    public int Level;

    public BossFloorSpawn(int floor, uint characterId, int minCount, int maxCount)
    {
        Floor = floor;
        CharacterId = characterId;
        MinCount = minCount;
        MaxCount = maxCount < minCount ? minCount : maxCount;
    }
}

public class FloorItemDrop
{
    public int Floor;
    public int ItemId;
    public double Probability;

    public FloorItemDrop(int floor, int itemId, double probability)
    {
        Floor = floor;
        ItemId = itemId;
        Probability = probability;
    }
}

public static class EnemySpawner
{
    // 最少は必ず出す。最少を超える分は、1体ごとに重みを100分率として判定し、外れた体は出さない。
    // 重み1は1%、33は33%。空きマスがあっても、外れた分で最多までは埋めない。
    public static List<EnemySpawnEntry> Roll(IReadOnlyList<EnemySpawnEntry> entries, int maxUnits, IBattleRandom rng)
    {
        var result = new List<EnemySpawnEntry>();
        if (entries == null || rng == null || maxUnits < 1)
            return result;

        for (int i = 0; i < entries.Count; i++)
        {
            int min = entries[i].MinCount;
            if (min < 0)
                min = 0;
            for (int n = 0; n < min; n++)
            {
                if (result.Count >= maxUnits)
                    return result;
                result.Add(entries[i]);
            }
        }

        for (int i = 0; i < entries.Count; i++)
        {
            EnemySpawnEntry entry = entries[i];
            int min = entry.MinCount;
            if (min < 0)
                min = 0;
            int max = entry.MaxCount < min ? min : entry.MaxCount;
            double chance = entry.Weight / 100.0;
            if (chance < 0.0)
                chance = 0.0;
            if (chance > 1.0)
                chance = 1.0;
            for (int n = min; n < max; n++)
            {
                if (result.Count >= maxUnits)
                    return result;
                if (rng.NextDouble() < chance)
                    result.Add(entry);
            }
        }
        return result;
    }

    public static int RollTotalCount(IReadOnlyList<EnemySpawnEntry> entries, int maxUnits, IBattleRandom rng)
    {
        int min = 0;
        int max = 0;
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                int entryMin = entries[i].MinCount;
                int entryMax = entries[i].MaxCount;
                if (entryMin < 0)
                    entryMin = 0;
                if (entryMax < entryMin)
                    entryMax = entryMin;
                min += entryMin;
                max += entryMax;
            }
        }

        if (maxUnits < 1)
            maxUnits = 1;
        if (min > maxUnits)
            min = maxUnits;
        if (max > maxUnits)
            max = maxUnits;
        if (max < min)
            max = min;
        if (max <= 0)
            return 0;
        return rng.NextInt(min, max + 1);
    }

    public static List<EnemySpawnEntry> RollTypes(IReadOnlyList<EnemySpawnEntry> entries, int total, IBattleRandom rng)
    {
        var result = new List<EnemySpawnEntry>();
        if (entries == null || total <= 0)
            return result;

        var placed = new Dictionary<uint, int>();
        for (int n = 0; n < total; n++)
        {
            EnemySpawnEntry picked = PickWeighted(entries, placed, rng);
            if (picked == null)
                break;
            result.Add(picked);
            int count;
            placed.TryGetValue(picked.CharacterId, out count);
            placed[picked.CharacterId] = count + 1;
        }
        return result;
    }

    public static EnemySpawnEntry PickWeighted(IReadOnlyList<EnemySpawnEntry> entries, Dictionary<uint, int> placed, IBattleRandom rng)
    {
        int sum = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (!CanPlace(entries[i], placed))
                continue;
            sum += entries[i].Weight;
        }
        if (sum <= 0)
            return null;

        int roll = rng.NextInt(0, sum);
        int cursor = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (!CanPlace(entries[i], placed))
                continue;
            cursor += entries[i].Weight;
            if (roll < cursor)
                return entries[i];
        }
        return null;
    }

    static bool CanPlace(EnemySpawnEntry entry, Dictionary<uint, int> placed)
    {
        if (entry.Weight <= 0)
            return false;
        if (placed == null)
            return true;
        int count;
        placed.TryGetValue(entry.CharacterId, out count);
        return count < entry.MaxCount;
    }

    public static int RollBossCount(BossFloorSpawn boss, int maxUnits, IBattleRandom rng)
    {
        if (boss == null)
            return 0;
        int min = boss.MinCount;
        int max = boss.MaxCount;
        if (min < 0)
            min = 0;
        if (max < min)
            max = min;
        if (max > maxUnits)
            max = maxUnits;
        if (min > max)
            min = max;
        if (max <= 0)
            return 0;
        return rng.NextInt(min, max + 1);
    }
}

public static class FloorDropRoller
{
    public static List<int> Roll(IReadOnlyList<FloorItemDrop> drops, int floor, IBattleRandom rng)
    {
        var got = new List<int>();
        if (drops == null)
            return got;
        for (int i = 0; i < drops.Count; i++)
        {
            FloorItemDrop drop = drops[i];
            if (drop.Floor != floor)
                continue;
            if (drop.Probability <= 0.0)
                continue;
            if (rng.NextDouble() < drop.Probability)
                got.Add(drop.ItemId);
        }
        return got;
    }
}
