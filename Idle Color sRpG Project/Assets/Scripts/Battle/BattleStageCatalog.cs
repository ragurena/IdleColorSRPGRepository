using System;
using System.Collections.Generic;

// ステージごとの敵・ボス・階ドロップ。数値を変えるときはここを編集する。
public class StageBattleContent
{
    public int StageIndex;
    public FloorBandSpawn[] Bands = new FloorBandSpawn[10];
    public List<BossFloorSpawn> Bosses = new List<BossFloorSpawn>();
    public List<FloorItemDrop> Drops = new List<FloorItemDrop>();

    public FloorBandSpawn GetBand(int floor)
    {
        if (floor < 1)
            floor = 1;
        int index = (floor - 1) / 10;
        if (index < 0)
            index = 0;
        if (index >= Bands.Length)
            index = Bands.Length - 1;
        if (Bands[index] == null)
            Bands[index] = new FloorBandSpawn();
        return Bands[index];
    }

    public BossFloorSpawn FindBoss(int floor)
    {
        for (int i = 0; i < Bosses.Count; i++)
        {
            if (Bosses[i].Floor == floor)
                return Bosses[i];
        }
        return null;
    }
}

public static class BattleStageCatalog
{
    public static StageBattleContent Get(int stageIndex)
    {
        switch (stageIndex)
        {
            case 2:
                return Build(2, new uint[] { 2, 10, 8, 4 }, new uint[] { 10, 18, 23 });
            case 3:
                return Build(3, new uint[] { 5, 3, 11, 19 }, new uint[] { 11, 19, 24 });
            case 4:
                return Build(4, new uint[] { 9, 17, 21, 12 }, new uint[] { 17, 21, 22 });
            case 5:
                return Build(5, new uint[] { 17, 18, 19, 22 }, new uint[] { 22, 23, 24 });
            default:
                return Build(1, new uint[] { 1, 2, 3, 4 }, new uint[] { 9, 17, 22 });
        }
    }

    static StageBattleContent Build(int stageIndex, uint[] pool, uint[] bosses)
    {
        var content = new StageBattleContent();
        content.StageIndex = stageIndex;
        if (pool == null || pool.Length == 0)
            pool = new uint[] { 1 };
        if (bosses == null || bosses.Length == 0)
            bosses = pool;

        for (int band = 0; band < 10; band++)
        {
            var spawn = new FloorBandSpawn();
            spawn.StatMultiplier = 1.0;
            int types = pool.Length < 3 ? pool.Length : 3;
            int[] weights = { 50, 30, 20 };
            for (int t = 0; t < types; t++)
            {
                uint id = pool[(band + t) % pool.Length];
                int min = t == 0 ? 1 : 0;
                int max = 1 + (band / 3) + (t == 0 ? 1 : 0);
                if (stageIndex >= 4)
                    max += 1;
                if (max > 4)
                    max = 4;
                spawn.Entries.Add(new EnemySpawnEntry(id, weights[t], min, max));
            }
            content.Bands[band] = spawn;
        }

        for (int i = 1; i <= 10; i++)
        {
            uint id = bosses[(i - 1) % bosses.Length];
            int max = 1 + (i - 1) / 4;
            if (max > 3)
                max = 3;
            content.Bosses.Add(new BossFloorSpawn(i * 10, id, 1, max));
            content.Drops.Add(new FloorItemDrop(i * 10, ItemIds.StageShard, 0.25));
        }

        return content;
    }
}
