using System.Collections.Generic;

// ステージごとの敵。10階ごとに1帯。
// 帯0が1〜10階、帯1が11〜20階、…、帯9が91〜100階。
// E(キャラID, レベル, 重み, 最少, 最多)。同じ帯でもキャラごとにレベルを書ける。
// 重みは比率。合計が100でなくてもよい。
// レベルは、画像の基礎ステータスに味方と同じ成長を掛ける回数。0は基礎のまま。
// ボスは Boss(階, キャラID, レベル, 最少, 最多)。書いた階だけ出る。
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
            case 2: return Forest();
            case 3: return Cave();
            case 4: return Ruins();
            case 5: return Castle();
            default: return Prairie();
        }
    }

    // 草原
    static StageBattleContent Prairie()
    {   
        //E の並びは キャラID、レベル、重み、最少、最多
        var stage = Stage(1);
        stage.Bands[0] = Band(E(25, 1, 33, 1, 2), E(26, 1, 33, 0, 1), E(27, 1, 33, 0, 1), E(28, 1, 1, 0, 1));
        stage.Bands[1] = Band(E(25, 2, 33, 1, 2), E(26, 2, 33, 0, 1), E(27, 2, 33, 0, 1), E(28, 3, 1, 0, 1));
        stage.Bands[2] = Band(E(25, 3, 33, 1, 2), E(26, 3, 33, 0, 1), E(27, 3, 33, 0, 1), E(28, 5, 1, 0, 1));
        stage.Bands[3] = Band(E(25, 4, 33, 1, 2), E(26, 4, 33, 1, 2), E(27, 4, 33, 0, 1), E(28, 10, 1, 0, 1));
        stage.Bands[4] = Band(E(25, 5, 33, 1, 2), E(26, 5, 33, 1, 2), E(27, 5, 33, 0, 1), E(28, 15, 1, 0, 1));
        stage.Bands[5] = Band(E(25, 7, 33, 1, 2), E(26, 7, 33, 1, 2), E(27, 7, 33, 0, 1), E(28, 20, 1, 0, 1));
        stage.Bands[6] = Band(E(25, 9, 33, 1, 2), E(26, 9, 33, 1, 2), E(27, 9, 33, 1, 2), E(28, 25, 1, 0, 1));
        stage.Bands[7] = Band(E(25, 11, 33, 1, 2), E(26, 11, 33, 0, 1), E(27, 11, 33, 1, 2), E(28, 30, 1, 0, 1));
        stage.Bands[8] = Band(E(25, 15, 33, 1, 2), E(26, 15, 33, 1, 2), E(27, 15, 33, 1, 2), E(28, 35, 1, 0, 1));
        stage.Bands[9] = Band(E(25, 20, 33, 1, 2), E(26, 20, 33, 1, 2), E(27, 20, 33, 1, 2), E(28, 40, 1, 1, 3));

        //この並びは 階、キャラID、レベル、最少、最多 です。
        //Boss(stage, 10, 9, 1, 1, 1);
        //Boss(stage, 20, 17, 2, 1, 1);
        //Boss(stage, 30, 22, 3, 1, 1);
        //Boss(stage, 40, 9, 4, 1, 1);
        //Boss(stage, 50, 17, 5, 1, 2);
        //Boss(stage, 60, 22, 6, 1, 2);
        //Boss(stage, 70, 9, 7, 1, 2);
        //Boss(stage, 80, 17, 8, 1, 2);
        //Boss(stage, 90, 22, 9, 1, 3);
        Boss(stage, 100, 29, 25, 1, 1);
        Shards(stage);
        return stage;
    }

    // 森
    static StageBattleContent Forest()
    {
        var stage = Stage(2);
        stage.Bands[0] = Band(E(2, 3, 50, 1, 2), E(10, 3, 30, 0, 1), E(8, 3, 20, 0, 1));
        stage.Bands[1] = Band(E(10, 4, 50, 1, 2), E(8, 4, 30, 0, 1), E(4, 4, 20, 0, 1));
        stage.Bands[2] = Band(E(8, 5, 50, 1, 2), E(4, 5, 30, 0, 1), E(25, 5, 20, 0, 1));
        stage.Bands[3] = Band(E(4, 6, 50, 1, 3), E(25, 6, 30, 0, 2), E(26, 6, 20, 0, 2));
        stage.Bands[4] = Band(E(25, 7, 50, 1, 3), E(26, 7, 30, 0, 2), E(27, 7, 20, 0, 2));
        stage.Bands[5] = Band(E(26, 8, 50, 1, 3), E(27, 8, 30, 0, 2), E(2, 8, 20, 0, 2));
        stage.Bands[6] = Band(E(27, 9, 50, 1, 4), E(2, 9, 30, 0, 3), E(10, 9, 20, 0, 3));
        stage.Bands[7] = Band(E(2, 10, 50, 1, 4), E(10, 10, 30, 0, 3), E(8, 10, 20, 0, 3));
        stage.Bands[8] = Band(E(10, 11, 50, 1, 4), E(8, 11, 30, 0, 3), E(4, 11, 20, 0, 3));
        stage.Bands[9] = Band(E(8, 12, 50, 1, 4), E(4, 12, 30, 0, 3), E(25, 12, 20, 0, 3));
        Boss(stage, 10, 10, 3, 1, 1);
        Boss(stage, 20, 18, 4, 1, 1);
        Boss(stage, 30, 23, 5, 1, 1);
        Boss(stage, 40, 10, 6, 1, 1);
        Boss(stage, 50, 18, 7, 1, 2);
        Boss(stage, 60, 23, 8, 1, 2);
        Boss(stage, 70, 10, 9, 1, 2);
        Boss(stage, 80, 18, 10, 1, 2);
        Boss(stage, 90, 23, 11, 1, 3);
        Boss(stage, 100, 10, 12, 1, 3);
        Shards(stage);
        return stage;
    }

    // 洞窟
    static StageBattleContent Cave()
    {
        var stage = Stage(3);
        stage.Bands[0] = Band(E(5, 6, 50, 1, 2), E(3, 6, 30, 0, 1), E(11, 6, 20, 0, 1));
        stage.Bands[1] = Band(E(3, 7, 50, 1, 2), E(11, 7, 30, 0, 1), E(19, 7, 20, 0, 1));
        stage.Bands[2] = Band(E(11, 8, 50, 1, 2), E(19, 8, 30, 0, 1), E(27, 8, 20, 0, 1));
        stage.Bands[3] = Band(E(19, 9, 50, 1, 3), E(27, 9, 30, 0, 2), E(28, 9, 20, 0, 2));
        stage.Bands[4] = Band(E(27, 10, 50, 1, 3), E(28, 10, 30, 0, 2), E(29, 10, 20, 0, 2));
        stage.Bands[5] = Band(E(28, 11, 50, 1, 3), E(29, 11, 30, 0, 2), E(5, 11, 20, 0, 2));
        stage.Bands[6] = Band(E(29, 12, 50, 1, 4), E(5, 12, 30, 0, 3), E(3, 12, 20, 0, 3));
        stage.Bands[7] = Band(E(5, 13, 50, 1, 4), E(3, 13, 30, 0, 3), E(11, 13, 20, 0, 3));
        stage.Bands[8] = Band(E(3, 14, 50, 1, 4), E(11, 14, 30, 0, 3), E(19, 14, 20, 0, 3));
        stage.Bands[9] = Band(E(11, 15, 50, 1, 4), E(19, 15, 30, 0, 3), E(27, 15, 20, 0, 3));
        Boss(stage, 10, 11, 6, 1, 1);
        Boss(stage, 20, 19, 7, 1, 1);
        Boss(stage, 30, 24, 8, 1, 1);
        Boss(stage, 40, 11, 9, 1, 1);
        Boss(stage, 50, 19, 10, 1, 2);
        Boss(stage, 60, 24, 11, 1, 2);
        Boss(stage, 70, 11, 12, 1, 2);
        Boss(stage, 80, 19, 13, 1, 2);
        Boss(stage, 90, 24, 14, 1, 3);
        Boss(stage, 100, 11, 15, 1, 3);
        Shards(stage);
        return stage;
    }

    // 遺跡
    static StageBattleContent Ruins()
    {
        var stage = Stage(4);
        stage.Bands[0] = Band(E(9, 10, 50, 1, 3), E(17, 10, 30, 0, 2), E(21, 10, 20, 0, 2));
        stage.Bands[1] = Band(E(17, 12, 50, 1, 3), E(21, 12, 30, 0, 2), E(12, 12, 20, 0, 2));
        stage.Bands[2] = Band(E(21, 14, 50, 1, 3), E(12, 14, 30, 0, 2), E(9, 14, 20, 0, 2));
        stage.Bands[3] = Band(E(12, 16, 50, 1, 4), E(9, 16, 30, 0, 3), E(17, 16, 20, 0, 3));
        stage.Bands[4] = Band(E(9, 18, 50, 1, 4), E(17, 18, 30, 0, 3), E(21, 18, 20, 0, 3));
        stage.Bands[5] = Band(E(17, 20, 50, 1, 4), E(21, 20, 30, 0, 3), E(12, 20, 20, 0, 3));
        stage.Bands[6] = Band(E(21, 22, 50, 1, 4), E(12, 22, 30, 0, 4), E(9, 22, 20, 0, 4));
        stage.Bands[7] = Band(E(12, 24, 50, 1, 4), E(9, 24, 30, 0, 4), E(17, 24, 20, 0, 4));
        stage.Bands[8] = Band(E(9, 26, 50, 1, 4), E(17, 26, 30, 0, 4), E(21, 26, 20, 0, 4));
        stage.Bands[9] = Band(E(17, 28, 50, 1, 4), E(21, 28, 30, 0, 4), E(12, 28, 20, 0, 4));
        Boss(stage, 10, 17, 10, 1, 1);
        Boss(stage, 20, 21, 12, 1, 1);
        Boss(stage, 30, 22, 14, 1, 1);
        Boss(stage, 40, 17, 16, 1, 1);
        Boss(stage, 50, 21, 18, 1, 2);
        Boss(stage, 60, 22, 20, 1, 2);
        Boss(stage, 70, 17, 22, 1, 2);
        Boss(stage, 80, 21, 24, 1, 2);
        Boss(stage, 90, 22, 26, 1, 3);
        Boss(stage, 100, 17, 28, 1, 3);
        Shards(stage);
        return stage;
    }

    // 城
    static StageBattleContent Castle()
    {
        var stage = Stage(5);
        stage.Bands[0] = Band(E(17, 16, 50, 1, 3), E(18, 16, 30, 0, 2), E(19, 16, 20, 0, 2));
        stage.Bands[1] = Band(E(18, 18, 50, 1, 3), E(19, 18, 30, 0, 2), E(22, 18, 20, 0, 2));
        stage.Bands[2] = Band(E(19, 20, 50, 1, 3), E(22, 20, 30, 0, 2), E(17, 20, 20, 0, 2));
        stage.Bands[3] = Band(E(22, 22, 50, 1, 4), E(17, 22, 30, 0, 3), E(18, 22, 20, 0, 3));
        stage.Bands[4] = Band(E(17, 24, 50, 1, 4), E(18, 24, 30, 0, 3), E(19, 24, 20, 0, 3));
        stage.Bands[5] = Band(E(18, 26, 50, 1, 4), E(19, 26, 30, 0, 3), E(22, 26, 20, 0, 3));
        stage.Bands[6] = Band(E(19, 28, 50, 1, 4), E(22, 28, 30, 0, 4), E(17, 28, 20, 0, 4));
        stage.Bands[7] = Band(E(22, 30, 50, 1, 4), E(17, 30, 30, 0, 4), E(18, 30, 20, 0, 4));
        stage.Bands[8] = Band(E(17, 34, 50, 1, 4), E(18, 34, 30, 0, 4), E(19, 34, 20, 0, 4));
        stage.Bands[9] = Band(E(18, 40, 50, 1, 4), E(19, 40, 30, 0, 4), E(22, 40, 20, 0, 4));
        Boss(stage, 10, 22, 16, 1, 1);
        Boss(stage, 20, 23, 18, 1, 1);
        Boss(stage, 30, 24, 20, 1, 1);
        Boss(stage, 40, 22, 22, 1, 1);
        Boss(stage, 50, 23, 24, 1, 2);
        Boss(stage, 60, 24, 26, 1, 2);
        Boss(stage, 70, 22, 28, 1, 2);
        Boss(stage, 80, 23, 30, 1, 2);
        Boss(stage, 90, 24, 34, 1, 3);
        Boss(stage, 100, 22, 40, 1, 3);
        Shards(stage);
        return stage;
    }

    static StageBattleContent Stage(int stageIndex)
    {
        var stage = new StageBattleContent();
        stage.StageIndex = stageIndex;
        return stage;
    }

    static FloorBandSpawn Band(params EnemySpawnEntry[] entries)
    {
        var band = new FloorBandSpawn();
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
                band.Entries.Add(entries[i]);
        }
        return band;
    }

    static EnemySpawnEntry E(uint id, int level, int weight, int min, int max)
    {
        return new EnemySpawnEntry(id, weight, min, max, level);
    }

    static void Boss(StageBattleContent stage, int floor, uint id, int level, int min, int max)
    {
        var boss = new BossFloorSpawn(floor, id, min, max);
        boss.Level = level;
        stage.Bosses.Add(boss);
    }

    static void Shards(StageBattleContent stage)
    {
        for (int floor = 10; floor <= 100; floor += 10)
            stage.Drops.Add(new FloorItemDrop(floor, ItemIds.StageShard, 0.25));
    }
}
