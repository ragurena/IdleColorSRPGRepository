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
        stage.Bands[0] = Band(E(6,1,50,0,1),E(7,1,50,0,1),E(8,1,50,0,1),E(9,1,1,0,1));
        stage.Bands[1] = Band(E(6,2,33,1,2),E(7,2,33,0,1),E(8,2,33,0,1),E(9,3,1,0,1));
        stage.Bands[2] = Band(E(6,3,33,1,2),E(7,3,33,0,1),E(8,3,33,0,1),E(9,5,1,0,1));
        stage.Bands[3] = Band(E(6,4,33,1,2),E(7,4,33,1,2),E(8,4,33,0,1),E(9,7,1,0,1));
        stage.Bands[4] = Band(E(6,5,33,1,2),E(7,5,33,1,2),E(8,5,33,0,1),E(9,9,1,0,1));
        stage.Bands[5] = Band(E(6,7,33,1,2),E(7,7,33,1,2),E(8,7,33,0,1),E(9,11,1,0,1));
        stage.Bands[6] = Band(E(6,9,33,1,2),E(7,9,33,1,2),E(8,9,33,1,2),E(9,13,1,0,1));
        stage.Bands[7] = Band(E(6,11,33,1,2),E(7,11,33,0,1),E(8,11,33,1,2),E(9,15,1,0,1));
        stage.Bands[8] = Band(E(6,15,33,1,2),E(7,15,33,1,2),E(8,15,33,1,2),E(9,17,1,0,1));
        stage.Bands[9] = Band(E(6,20,33,1,3),E(7,20,33,1,3),E(8,20,33,1,3),E(9,20,1,1,3));

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
        stage.Bands[0] = Band(E(1,1,33,1,2),E(2,1,33,0,1),E(3,1,33,0,1),E(4,1,1,0,1));
        stage.Bands[1] = Band(E(1,2,33,1,2),E(2,2,33,0,1),E(3,2,33,0,1),E(4,3,1,0,1));
        stage.Bands[2] = Band(E(1,3,33,1,2),E(2,3,33,0,1),E(3,3,33,0,1),E(4,5,1,0,1));
        stage.Bands[3] = Band(E(1,5,33,1,2),E(2,5,33,1,2),E(3,5,33,0,1),E(4,10,1,0,1));
        stage.Bands[4] = Band(E(1,8,33,1,2),E(2,8,33,1,2),E(3,8,33,0,1),E(4,10,1,0,1));
        stage.Bands[5] = Band(E(1,13,33,1,2),E(2,13,33,1,2),E(3,13,33,0,1),E(4,15,1,0,1));
        stage.Bands[6] = Band(E(1,21,33,1,2),E(2,21,33,1,2),E(3,21,33,1,2),E(4,15,1,0,1));
        stage.Bands[7] = Band(E(1,34,33,1,2),E(2,34,33,0,1),E(3,34,33,1,2),E(4,20,1,0,1));
        stage.Bands[8] = Band(E(1,55,33,1,2),E(2,55,33,1,2),E(3,55,33,1,2),E(4,20,1,0,1));
        stage.Bands[9] = Band(E(1,89,33,1,2),E(2,89,33,1,2),E(3,89,33,1,2),E(4,25,1,1,3));


        Boss(stage, 100, 5, 25, 1, 1);
        Shards(stage);
        return stage;
    }

    // 洞窟
    static StageBattleContent Cave()
    {
        var stage = Stage(3);
        stage.Bands[0] = Band(E(27, 10, 33, 0, 1), E(28, 10, 33, 0, 1), E(29, 10, 33, 0, 1), E(30, 5, 1, 0, 1));
        stage.Bands[1] = Band(E(27, 20, 33, 1, 2), E(28, 20, 33, 0, 1), E(29, 20, 33, 0, 1), E(30, 6, 1, 0, 1));
        stage.Bands[2] = Band(E(27, 30, 33, 1, 2), E(28, 30, 33, 0, 1), E(29, 30, 33, 0, 1), E(30, 11, 1, 0, 1));
        stage.Bands[3] = Band(E(27, 40, 33, 1, 2), E(28, 40, 33, 1, 2), E(29, 40, 33, 0, 1), E(30, 17, 1, 0, 1));
        stage.Bands[4] = Band(E(27, 50, 33, 1, 2), E(28, 50, 33, 1, 2), E(29, 50, 33, 0, 1), E(30, 28, 1, 0, 1));
        stage.Bands[5] = Band(E(27, 70, 33, 1, 2), E(28, 70, 33, 1, 2), E(29, 70, 33, 0, 1), E(30, 45, 1, 0, 1));
        stage.Bands[6] = Band(E(27, 90, 33, 1, 2), E(28, 90, 33, 1, 2), E(29, 90, 33, 1, 2), E(30, 73, 1, 0, 1));
        stage.Bands[7] = Band(E(27, 110, 33, 1, 2), E(28, 110, 33, 0, 1), E(29, 110, 33, 1, 2), E(30, 100, 1, 0, 1));
        stage.Bands[8] = Band(E(27, 150, 33, 1, 2), E(28, 150, 33, 1, 2), E(29, 150, 33, 1, 2), E(30, 100, 1, 0, 1));
        stage.Bands[9] = Band(E(27, 200, 33, 1, 2), E(28, 200, 33, 1, 2), E(29, 200, 33, 1, 2), E(30, 100, 1, 1, 3));        Boss(stage, 10, 13, 6, 1, 1);

        Boss(stage, 10, 31, 10, 1, 1);
        Boss(stage, 20, 31, 20, 1, 1);
        Boss(stage, 30, 31, 30, 1, 1);
        Boss(stage, 40, 31, 40, 1, 1);
        Boss(stage, 50, 31, 50, 1, 2);
        Boss(stage, 60, 31, 60, 1, 2);
        Boss(stage, 70, 31, 70, 1, 2);
        Boss(stage, 80, 31, 80, 1, 2);
        Boss(stage, 90, 31, 90, 1, 3);
        Boss(stage, 100, 31, 100, 1, 3);
        Shards(stage);
        return stage;
    }

    // 遺跡
    static StageBattleContent Ruins()
    {
        var stage = Stage(4);
        stage.Bands[0] = Band(E(11, 100, 50, 1, 3), E(19, 100, 30, 0, 2), E(23, 100, 20, 0, 2));
        stage.Bands[1] = Band(E(19, 120, 50, 1, 3), E(23, 120, 30, 0, 2), E(14, 120, 20, 0, 2));
        stage.Bands[2] = Band(E(23, 140, 50, 1, 3), E(14, 140, 30, 0, 2), E(11, 140, 20, 0, 2));
        stage.Bands[3] = Band(E(14, 160, 50, 1, 4), E(11, 160, 30, 0, 3), E(19, 160, 20, 0, 3));
        stage.Bands[4] = Band(E(11, 180, 50, 1, 4), E(19, 180, 30, 0, 3), E(23, 180, 20, 0, 3));
        stage.Bands[5] = Band(E(19, 200, 50, 1, 4), E(23, 200, 30, 0, 3), E(14, 200, 20, 0, 3));
        stage.Bands[6] = Band(E(23, 220, 50, 1, 4), E(14, 220, 30, 0, 4), E(11, 220, 20, 0, 4));
        stage.Bands[7] = Band(E(14, 240, 50, 1, 4), E(11, 240, 30, 0, 4), E(19, 240, 20, 0, 4));
        stage.Bands[8] = Band(E(11, 260, 50, 1, 4), E(19, 260, 30, 0, 4), E(23, 260, 20, 0, 4));
        stage.Bands[9] = Band(E(19, 280, 50, 1, 4), E(23, 280, 30, 0, 4), E(14, 280, 20, 0, 4));
        Boss(stage, 10, 19, 100, 1, 1);
        Boss(stage, 20, 23, 120, 1, 1);
        Boss(stage, 30, 24, 140, 1, 1);
        Boss(stage, 40, 19, 160, 1, 1);
        Boss(stage, 50, 23, 180, 1, 2);
        Boss(stage, 60, 24, 200, 1, 2);
        Boss(stage, 70, 19, 220, 1, 2);
        Boss(stage, 80, 23, 240, 1, 2);
        Boss(stage, 90, 24, 260, 1, 3);
        Boss(stage, 100, 19, 280, 1, 3);
        Shards(stage);
        return stage;
    }

    // 城
    static StageBattleContent Castle()
    {
        var stage = Stage(5);
        stage.Bands[0] = Band(E(19, 160, 50, 1, 3), E(20, 160, 30, 0, 2), E(21, 160, 20, 0, 2));
        stage.Bands[1] = Band(E(20, 180, 50, 1, 3), E(21, 180, 30, 0, 2), E(24, 180, 20, 0, 2));
        stage.Bands[2] = Band(E(21, 200, 50, 1, 3), E(24, 200, 30, 0, 2), E(19, 200, 20, 0, 2));
        stage.Bands[3] = Band(E(24, 220, 50, 1, 4), E(19, 220, 30, 0, 3), E(20, 220, 20, 0, 3));
        stage.Bands[4] = Band(E(19, 240, 50, 1, 4), E(20, 240, 30, 0, 3), E(21, 240, 20, 0, 3));
        stage.Bands[5] = Band(E(20, 260, 50, 1, 4), E(21, 260, 30, 0, 3), E(24, 260, 20, 0, 3));
        stage.Bands[6] = Band(E(21, 280, 50, 1, 4), E(24, 280, 30, 0, 4), E(19, 280, 20, 0, 4));
        stage.Bands[7] = Band(E(24, 300, 50, 1, 4), E(19, 300, 30, 0, 4), E(20, 300, 20, 0, 4));
        stage.Bands[8] = Band(E(19, 340, 50, 1, 4), E(20, 340, 30, 0, 4), E(21, 340, 20, 0, 4));
        stage.Bands[9] = Band(E(20, 400, 50, 1, 4), E(21, 400, 30, 0, 4), E(24, 400, 20, 0, 4));
        Boss(stage, 10, 24, 160, 1, 1);
        Boss(stage, 20, 25, 180, 1, 1);
        Boss(stage, 30, 26, 200, 1, 1);
        Boss(stage, 40, 24, 220, 1, 1);
        Boss(stage, 50, 25, 240, 1, 2);
        Boss(stage, 60, 26, 260, 1, 2);
        Boss(stage, 70, 24, 280, 1, 2);
        Boss(stage, 80, 25, 300, 1, 2);
        Boss(stage, 90, 26, 340, 1, 3);
        Boss(stage, 100, 24, 400, 1, 3);
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
