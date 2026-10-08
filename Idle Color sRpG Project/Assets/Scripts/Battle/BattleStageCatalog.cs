using System.Collections.Generic;

// ステージごとの敵。10階ごとに1帯。
// 帯0が1〜10階、帯1が11〜20階、…、帯9が91〜100階。
// E(キャラID, レベル, 重み, 最少, 最多)。同じ帯でもキャラごとにレベルを書ける。
// 最少は必ず出す。それを超える分は、1体ごとに重み%で判定し、外れた体は出さない。
// 重み1は1%、33は33%。100以上なら、その追加の体は必ず出る。
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
        stage.Bands[0] = Band(E(6,1,50,0,1),E(7,1,50,0,1),E(8,1,50,0,1),E(9,1,0,0,1));
        stage.Bands[1] = Band(E(6,2,33,1,2),E(7,2,33,0,1),E(8,2,33,0,1),E(9,2,0,0,1));
        stage.Bands[2] = Band(E(6,3,33,1,2),E(7,3,33,0,1),E(8,3,33,0,1),E(9,3,0,0,1));
        stage.Bands[3] = Band(E(6,4,33,1,2),E(7,4,33,1,2),E(8,4,33,0,1),E(9,4,1,0,1));
        stage.Bands[4] = Band(E(6,5,33,1,2),E(7,5,33,1,2),E(8,5,33,0,1),E(9,5,1,0,1));
        stage.Bands[5] = Band(E(6,6,33,1,2),E(7,6,33,1,2),E(8,6,33,0,1),E(9,6,1,0,1));
        stage.Bands[6] = Band(E(6,7,33,1,2),E(7,7,33,1,2),E(8,7,33,1,2),E(9,7,1,0,1));
        stage.Bands[7] = Band(E(6,8,33,1,2),E(7,8,33,0,1),E(8,8,33,1,2),E(9,8,1,0,1));
        stage.Bands[8] = Band(E(6,9,33,1,2),E(7,9,33,1,2),E(8,9,33,1,2),E(9,9,1,0,1));
        stage.Bands[9] = Band(E(6,10,33,1,3),E(7,10,33,1,3),E(8,10,33,1,3),E(9,10,1,1,3));

        //この並びは 階、キャラID、レベル、最少、最多 です。
        //Boss(stage, 10, 11, 1, 1, 1);
        //Boss(stage, 20, 19, 2, 1, 1);
        //Boss(stage, 30, 24, 3, 1, 1);
        //Boss(stage, 40, 11, 4, 1, 1);
        //Boss(stage, 50, 19, 5, 1, 2);
        //Boss(stage, 60, 24, 6, 1, 2);
        //Boss(stage, 70, 11, 7, 1, 2);
        //Boss(stage, 80, 19, 8, 1, 2);
        //Boss(stage, 90, 24, 9, 1, 3);
        Boss(stage, 100, 10, 25, 1, 1);
        Shards(stage);
        return stage;
    }

    // 森
    static StageBattleContent Forest()
    {
        var stage = Stage(2);
        stage.Bands[0] = Band(E(1,20,33,1,2),E(2,20,33,0,1),E(3,20,33,0,1),E(4,20,1,0,1));
        stage.Bands[1] = Band(E(1,30,33,1,2),E(2,30,33,0,1),E(3,30,33,0,1),E(4,30,2,0,1));
        stage.Bands[2] = Band(E(1,40,33,1,2),E(2,40,33,0,1),E(3,40,33,0,1),E(4,40,3,0,1));
        stage.Bands[3] = Band(E(1,50,33,1,2),E(2,50,33,1,2),E(3,50,33,0,1),E(4,50,4,0,1));
        stage.Bands[4] = Band(E(1,60,33,1,2),E(2,60,33,1,2),E(3,60,33,0,1),E(4,60,5,0,1));
        stage.Bands[5] = Band(E(1,70,33,1,2),E(2,70,33,1,2),E(3,70,33,0,1),E(4,70,6,0,1));
        stage.Bands[6] = Band(E(1,80,33,1,2),E(2,80,33,1,2),E(3,80,33,1,2),E(4,80,7,0,1));
        stage.Bands[7] = Band(E(1,90,33,1,2),E(2,90,33,0,1),E(3,90,33,1,2),E(4,90,8,0,1));
        stage.Bands[8] = Band(E(1,100,33,1,2),E(2,100,33,1,2),E(3,100,33,1,2),E(4,100,9,0,1));
        stage.Bands[9] = Band(E(1,110,33,1,2),E(2,110,33,1,2),E(3,110,33,1,2),E(4,110,10,1,3));


        Boss(stage, 100, 5, 200, 1, 1);
        Shards(stage);
        return stage;
    }

    // 洞窟
    static StageBattleContent Cave()
    {
        var stage = Stage(3);
        stage.Bands[0] = Band(E(27, 100, 33, 0, 1), E(28, 100, 33, 0, 1), E(29, 100, 33, 0, 1), E(30, 100, 1, 0, 1));
        stage.Bands[1] = Band(E(27, 200, 33, 1, 2), E(28, 200, 33, 0, 1), E(29, 200, 33, 0, 1), E(30, 200, 2, 0, 1));
        stage.Bands[2] = Band(E(27, 300, 33, 1, 2), E(28, 300, 33, 0, 1), E(29, 300, 33, 0, 1), E(30, 300, 3, 0, 1));
        stage.Bands[3] = Band(E(27, 400, 33, 1, 2), E(28, 400, 33, 1, 2), E(29, 400, 33, 0, 1), E(30, 400, 4, 0, 1));
        stage.Bands[4] = Band(E(27, 500, 33, 1, 2), E(28, 500, 33, 1, 2), E(29, 500, 33, 0, 1), E(30, 500, 5, 0, 1));
        stage.Bands[5] = Band(E(27, 700, 33, 1, 2), E(28, 700, 33, 1, 2), E(29, 700, 33, 0, 1), E(30, 600, 6, 0, 1));
        stage.Bands[6] = Band(E(27, 900, 33, 1, 2), E(28, 900, 33, 1, 2), E(29, 900, 33, 1, 2), E(30, 700, 7, 0, 1));
        stage.Bands[7] = Band(E(27, 1000, 33, 1, 2), E(28, 1000, 33, 0, 1), E(29, 1000, 33, 1, 2), E(30, 800, 8, 0, 1));
        stage.Bands[8] = Band(E(27, 1100, 33, 1, 2), E(28, 1100, 33, 1, 2), E(29, 1100, 33, 1, 2), E(30, 900, 9, 0, 1));
        stage.Bands[9] = Band(E(27, 1200, 33, 1, 2), E(28, 1200, 33, 1, 2), E(29, 1200, 33, 1, 2), E(30, 1000, 10, 1, 3));        

        Boss(stage, 10, 31, 100, 1, 1);
        Boss(stage, 20, 31, 200, 1, 1);
        Boss(stage, 30, 31, 300, 1, 1);
        Boss(stage, 40, 31, 400, 1, 1);
        Boss(stage, 50, 31, 500, 1, 2);
        Boss(stage, 60, 31, 600, 1, 2);
        Boss(stage, 70, 31, 700, 1, 2);
        Boss(stage, 80, 31, 800, 1, 2);
        Boss(stage, 90, 31, 900, 1, 3);
        Boss(stage, 100, 31, 1000, 1, 3);
        Shards(stage);
        return stage;
    }

    // 遺跡
    static StageBattleContent Ruins()
    {
        var stage = Stage(4);
        stage.Bands[0] = Band(E(11, 1000, 50, 1, 3), E(19, 1000, 30, 0, 2), E(23, 1000, 20, 0, 2));
        stage.Bands[1] = Band(E(19, 1200, 50, 1, 3), E(23, 1200, 30, 0, 2), E(14, 1200, 20, 0, 2));
        stage.Bands[2] = Band(E(23, 1400, 50, 1, 3), E(14, 1400, 30, 0, 2), E(11, 1400, 20, 0, 2));
        stage.Bands[3] = Band(E(14, 1600, 50, 1, 4), E(11, 1600, 30, 0, 3), E(19, 1600, 20, 0, 3));
        stage.Bands[4] = Band(E(11, 1800, 50, 1, 4), E(19, 1800, 30, 0, 3), E(23, 1800, 20, 0, 3));
        stage.Bands[5] = Band(E(19, 2000, 50, 1, 4), E(23, 2000, 30, 0, 3), E(14, 2000, 20, 0, 3));
        stage.Bands[6] = Band(E(23, 2200, 50, 1, 4), E(14, 2200, 30, 0, 4), E(11, 2200, 20, 0, 4));
        stage.Bands[7] = Band(E(14, 2400, 50, 1, 4), E(11, 2400, 30, 0, 4), E(19, 2400, 20, 0, 4));
        stage.Bands[8] = Band(E(11, 2600, 50, 1, 4), E(19, 2600, 30, 0, 4), E(23, 2600, 20, 0, 4));
        stage.Bands[9] = Band(E(19, 2800, 50, 1, 4), E(23, 2800, 30, 0, 4), E(14, 2800, 20, 0, 4));
        Boss(stage, 10, 19, 1000, 1, 1);
        Boss(stage, 20, 23, 1200, 1, 1);
        Boss(stage, 30, 24, 1400, 1, 1);
        Boss(stage, 40, 19, 1600, 1, 1);
        Boss(stage, 50, 23, 1800, 1, 2);
        Boss(stage, 60, 24, 2000, 1, 2);
        Boss(stage, 70, 19, 2200, 1, 2);
        Boss(stage, 80, 23, 2400, 1, 2);
        Boss(stage, 90, 24, 2600, 1, 3);
        Boss(stage, 100, 19, 2800, 1, 3);
        Shards(stage);
        return stage;
    }

    // 城
    static StageBattleContent Castle()
    {
        var stage = Stage(5);
        stage.Bands[0] = Band(E(19, 1600, 50, 1, 3), E(20, 1600, 30, 0, 2), E(21, 1600, 20, 0, 2));
        stage.Bands[1] = Band(E(20, 1800, 50, 1, 3), E(21, 1800, 30, 0, 2), E(24, 1800, 20, 0, 2));
        stage.Bands[2] = Band(E(21, 2000, 50, 1, 3), E(24, 2000, 30, 0, 2), E(19, 2000, 20, 0, 2));
        stage.Bands[3] = Band(E(24, 2200, 50, 1, 4), E(19, 2200, 30, 0, 3), E(20, 2200, 20, 0, 3));
        stage.Bands[4] = Band(E(19, 2400, 50, 1, 4), E(20, 2400, 30, 0, 3), E(21, 2400, 20, 0, 3));
        stage.Bands[5] = Band(E(20, 2600, 50, 1, 4), E(21, 2600, 30, 0, 3), E(24, 2600, 20, 0, 3));
        stage.Bands[6] = Band(E(21, 2800, 50, 1, 4), E(24, 2800, 30, 0, 4), E(19, 2800, 20, 0, 4));
        stage.Bands[7] = Band(E(24, 3000, 50, 1, 4), E(19, 3000, 30, 0, 4), E(20, 3000, 20, 0, 4));
        stage.Bands[8] = Band(E(19, 3400, 50, 1, 4), E(20, 3400, 30, 0, 4), E(21, 3400, 20, 0, 4));
        stage.Bands[9] = Band(E(20, 4000, 50, 1, 4), E(21, 4000, 30, 0, 4), E(24, 4000, 20, 0, 4));
        Boss(stage, 10, 24, 1600, 1, 1);
        Boss(stage, 20, 25, 1800, 1, 1);
        Boss(stage, 30, 26, 2000, 1, 1);
        Boss(stage, 40, 24, 2200, 1, 1);
        Boss(stage, 50, 25, 2400, 1, 2);
        Boss(stage, 60, 26, 2600, 1, 2);
        Boss(stage, 70, 24, 2800, 1, 2);
        Boss(stage, 80, 25, 3000, 1, 2);
        Boss(stage, 90, 26, 3400, 1, 3);
        Boss(stage, 100, 24, 4000, 1, 3);
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
