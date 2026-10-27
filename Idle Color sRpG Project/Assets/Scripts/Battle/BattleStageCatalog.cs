using System.Collections.Generic;
using UnityEngine;

// ステージごとの敵。10階ごとに1帯。
// 帯0が1〜10階、帯1が11〜20階、…、帯9が91〜100階。
// E(キャラID, レベル, 重み, 最少, 最多)。同じ帯でもキャラごとにレベルを書ける。
// 最少は必ず出す。それを超える分は、1体ごとに重み%で判定し、外れた体は出さない。
// 重み1は1%、33は33%。100以上なら、その追加の体は必ず出る。
// レベルは 1 始まり。1 は基礎のまま。成長はレベル-1 回。
// ボスは Boss(階, キャラID, レベル, 最少, 最多)。書いた階だけ出る。
public class StageBattleContent
{
    public int StageIndex;
    // Assets/Resources の画像名。拡張子は書かない。空なら背景なし。
    public string Background;
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

    // そのステージに一度でも出る敵。帯の並び順で、同じキャラは1回だけ。
    public static void CollectEnemyIds(int stageIndex, List<uint> ids)
    {
        if (ids == null)
            return;
        ids.Clear();
        if (stageIndex < 1 || stageIndex > 5)
            return;

        StageBattleContent content = Get(stageIndex);
        if (content == null || content.StageIndex != stageIndex)
            return;

        for (int band = 0; band < content.Bands.Length; band++)
        {
            FloorBandSpawn spawn = content.Bands[band];
            if (spawn == null || spawn.Entries == null)
                continue;
            for (int i = 0; i < spawn.Entries.Count; i++)
                AddEnemyId(ids, spawn.Entries[i]);
        }

        for (int i = 0; i < content.Bosses.Count; i++)
        {
            BossFloorSpawn boss = content.Bosses[i];
            if (boss == null || boss.CharacterId == 0 || boss.MaxCount < 1)
                continue;
            if (!ids.Contains(boss.CharacterId))
                ids.Add(boss.CharacterId);
        }
    }

    static void AddEnemyId(List<uint> ids, EnemySpawnEntry entry)
    {
        if (entry == null || entry.CharacterId == 0)
            return;
        bool appears = entry.MinCount > 0 || (entry.Weight > 0 && entry.MaxCount > 0);
        if (!appears || ids.Contains(entry.CharacterId))
            return;
        ids.Add(entry.CharacterId);
    }

    static readonly Dictionary<string, Sprite> BackgroundSprites = new Dictionary<string, Sprite>();
    static readonly HashSet<string> MissingBackgrounds = new HashSet<string>();

    public static Sprite BackgroundSprite(int stageIndex)
    {
        if (stageIndex < 1 || stageIndex > 5)
            return null;
        StageBattleContent content = Get(stageIndex);
        if (content == null || content.StageIndex != stageIndex)
            return null;
        return LoadBackground(content.Background);
    }

    static Sprite LoadBackground(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
            return null;
        Sprite cached;
        if (BackgroundSprites.TryGetValue(resourceName, out cached))
            return cached;
        if (MissingBackgrounds.Contains(resourceName))
            return null;

        Sprite sprite = Resources.Load<Sprite>(resourceName);
        if (sprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourceName);
            if (tex != null)
                sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        if (sprite == null)
        {
            MissingBackgrounds.Add(resourceName);
            Debug.LogWarning("ステージ背景が見つかりません: Resources/" + resourceName);
            return null;
        }
        BackgroundSprites[resourceName] = sprite;
        return sprite;
    }

    // チュートリアル
    static StageBattleContent Prairie()
    {   
        //E の並びは キャラID、レベル、重み、最少、最多
        var stage = Stage(1);
        stage.Background = "kougisitu";
        stage.Bands[0] = Band(E(6,1,50,1,1),E(7,1,10,0,1),E(8,1,10,0,1),E(9,1,0,0,1));
        stage.Bands[1] = Band(E(6,2,10,0,1),E(7,2,50,1,1),E(8,2,10,0,1),E(9,2,0,0,1));
        stage.Bands[2] = Band(E(6,3,33,0,1),E(7,3,10,0,1),E(8,3,50,1,1),E(9,3,0,0,1));
        stage.Bands[3] = Band(E(6,4,33,1,2),E(7,4,33,1,2),E(8,4,33,0,1),E(9,4,0,0,1));
        stage.Bands[4] = Band(E(6,5,33,1,2),E(7,5,33,1,2),E(8,5,33,0,1),E(9,5,0,0,1));
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
        Boss(stage, 100, 10, 10, 1, 1);
        Shards(stage);
        return stage;
    }

    // スライムの森
    static StageBattleContent Forest()
    {
        //E の並びは キャラID、レベル、重み、最少、最多
        var stage = Stage(2);
        stage.Background = "nekohatakouen";
        stage.Bands[0] = Band(E(1,2,33,1,2),E(2,2,33,0,1),E(3,2,33,0,1),E(4,2,1,0,1));
        stage.Bands[1] = Band(E(1,4,33,1,2),E(2,4,33,0,1),E(3,4,33,0,1),E(4,4,2,0,1));
        stage.Bands[2] = Band(E(1,6,33,1,2),E(2,6,33,0,1),E(3,6,33,0,1),E(4,6,3,0,1));
        stage.Bands[3] = Band(E(1,8,33,1,2),E(2,8,33,1,2),E(3,8,33,0,1),E(4,8,4,0,1));
        stage.Bands[4] = Band(E(1,10,33,1,2),E(2,10,33,1,2),E(3,10,33,0,1),E(4,10,5,0,1));
        stage.Bands[5] = Band(E(1,12,33,1,2),E(2,12,33,1,2),E(3,12,33,0,1),E(4,12,6,0,1));
        stage.Bands[6] = Band(E(1,14,33,1,2),E(2,14,33,1,2),E(3,14,33,1,2),E(4,14,7,0,1));
        stage.Bands[7] = Band(E(1,16,33,1,2),E(2,16,33,0,1),E(3,16,33,1,2),E(4,16,8,0,1));
        stage.Bands[8] = Band(E(1,18,33,1,2),E(2,18,33,1,2),E(3,18,33,1,2),E(4,18,9,0,1));
        stage.Bands[9] = Band(E(1,20,33,1,2),E(2,20,33,1,2),E(3,20,33,1,2),E(4,20,10,1,3));


        //この並びは 階、キャラID、レベル、最少、最多 です。
        Boss(stage, 100, 5, 20, 1, 1);
        Shards(stage);
        return stage;
    }

    // チグサとバトル１
    static StageBattleContent Cave()
    {
        //E の並びは キャラID、レベル、重み、最少、最多
        var stage = Stage(3);
        stage.Background = "chigusa_room";
        stage.Bands[0] = Band(E(16, 13, 33, 0, 1), E(17, 13, 33, 0, 1), E(18, 13, 33, 0, 1), E(19, 10, 1, 0, 1));
        stage.Bands[1] = Band(E(16, 16, 33, 1, 2), E(17, 16, 33, 0, 1), E(18, 16, 33, 0, 1), E(20, 10, 1, 0, 1));
        stage.Bands[2] = Band(E(16, 19, 33, 1, 2), E(17, 19, 33, 0, 1), E(18, 19, 33, 0, 1), E(19, 10, 2, 0, 1));
        stage.Bands[3] = Band(E(16, 22, 33, 1, 2), E(17, 22, 33, 1, 2), E(18, 22, 33, 0, 1), E(20, 10, 2, 0, 1));
        stage.Bands[4] = Band(E(16, 25, 33, 1, 2), E(17, 25, 33, 1, 2), E(18, 25, 33, 0, 1), E(19, 10, 3, 0, 1));
        stage.Bands[5] = Band(E(16, 28, 33, 1, 2), E(17, 28, 33, 1, 2), E(18, 28, 33, 0, 1), E(20, 10, 3, 0, 1));
        stage.Bands[6] = Band(E(16, 31, 33, 1, 2), E(17, 31, 33, 1, 2), E(18, 31, 33, 1, 2), E(19, 10, 4, 0, 1));
        stage.Bands[7] = Band(E(16, 34, 33, 1, 2), E(17, 34, 33, 0, 1), E(18, 34, 33, 1, 2), E(20, 10, 4, 0, 1));
        stage.Bands[8] = Band(E(16, 37, 33, 1, 2), E(17, 37, 33, 1, 2), E(18, 37, 33, 1, 2), E(19, 10, 5, 0, 1));
        stage.Bands[9] = Band(E(16, 40, 33, 1, 2), E(17, 40, 33, 1, 2), E(18, 40, 33, 1, 2), E(20, 10, 5, 1, 3));        

        //この並びは 階、キャラID、レベル、最少、最多 です。
        Boss(stage, 50, 19, 20, 1, 2);
        Boss(stage, 100, 20, 40, 1, 3);
        Shards(stage);
        return stage;
    }

    // ？？？
    static StageBattleContent Ruins()
    {
        var stage = Stage(4);
        stage.Background = "";
        stage.Bands[0] = Band(E(11, 1000, 50, 1, 3), E(21, 1000, 30, 0, 2), E(24, 1000, 20, 0, 2));
        stage.Bands[1] = Band(E(21, 1200, 50, 1, 3), E(24, 1200, 30, 0, 2), E(14, 1200, 20, 0, 2));
        stage.Bands[2] = Band(E(24, 1400, 50, 1, 3), E(14, 1400, 30, 0, 2), E(11, 1400, 20, 0, 2));
        stage.Bands[3] = Band(E(14, 1600, 50, 1, 4), E(11, 1600, 30, 0, 3), E(21, 1600, 20, 0, 3));
        stage.Bands[4] = Band(E(11, 1800, 50, 1, 4), E(21, 1800, 30, 0, 3), E(24, 1800, 20, 0, 3));
        stage.Bands[5] = Band(E(21, 2000, 50, 1, 4), E(24, 2000, 30, 0, 3), E(14, 2000, 20, 0, 3));
        stage.Bands[6] = Band(E(24, 2200, 50, 1, 4), E(14, 2200, 30, 0, 4), E(11, 2200, 20, 0, 4));
        stage.Bands[7] = Band(E(14, 2400, 50, 1, 4), E(11, 2400, 30, 0, 4), E(21, 2400, 20, 0, 4));
        stage.Bands[8] = Band(E(11, 2600, 50, 1, 4), E(21, 2600, 30, 0, 4), E(24, 2600, 20, 0, 4));
        stage.Bands[9] = Band(E(21, 2800, 50, 1, 4), E(24, 2800, 30, 0, 4), E(14, 2800, 20, 0, 4));
        Boss(stage, 10, 21, 1000, 1, 1);
        Boss(stage, 20, 24, 1200, 1, 1);
        Boss(stage, 30, 25, 1400, 1, 1);
        Boss(stage, 40, 21, 1600, 1, 1);
        Boss(stage, 50, 24, 1800, 1, 2);
        Boss(stage, 60, 25, 2000, 1, 2);
        Boss(stage, 70, 21, 2200, 1, 2);
        Boss(stage, 80, 24, 2400, 1, 2);
        Boss(stage, 90, 25, 2600, 1, 3);
        Boss(stage, 100, 21, 2800, 1, 3);
        Shards(stage);
        return stage;
    }

    // ？？？
    static StageBattleContent Castle()
    {
        var stage = Stage(5);
        stage.Background = "";
        stage.Bands[0] = Band(E(21, 1600, 50, 1, 3), E(22, 1600, 30, 0, 2), E(23, 1600, 20, 0, 2));
        stage.Bands[1] = Band(E(22, 1800, 50, 1, 3), E(23, 1800, 30, 0, 2), E(25, 1800, 20, 0, 2));
        stage.Bands[2] = Band(E(23, 2000, 50, 1, 3), E(25, 2000, 30, 0, 2), E(21, 2000, 20, 0, 2));
        stage.Bands[3] = Band(E(25, 2200, 50, 1, 4), E(21, 2200, 30, 0, 3), E(22, 2200, 20, 0, 3));
        stage.Bands[4] = Band(E(21, 2400, 50, 1, 4), E(22, 2400, 30, 0, 3), E(23, 2400, 20, 0, 3));
        stage.Bands[5] = Band(E(22, 2600, 50, 1, 4), E(23, 2600, 30, 0, 3), E(25, 2600, 20, 0, 3));
        stage.Bands[6] = Band(E(23, 2800, 50, 1, 4), E(25, 2800, 30, 0, 4), E(21, 2800, 20, 0, 4));
        stage.Bands[7] = Band(E(25, 3000, 50, 1, 4), E(21, 3000, 30, 0, 4), E(22, 3000, 20, 0, 4));
        stage.Bands[8] = Band(E(21, 3400, 50, 1, 4), E(22, 3400, 30, 0, 4), E(23, 3400, 20, 0, 4));
        stage.Bands[9] = Band(E(22, 4000, 50, 1, 4), E(23, 4000, 30, 0, 4), E(25, 4000, 20, 0, 4));
        Boss(stage, 10, 25, 1600, 1, 1);
        Boss(stage, 20, 26, 1800, 1, 1);
        Boss(stage, 30, 27, 2000, 1, 1);
        Boss(stage, 40, 25, 2200, 1, 1);
        Boss(stage, 50, 26, 2400, 1, 2);
        Boss(stage, 60, 27, 2600, 1, 2);
        Boss(stage, 70, 25, 2800, 1, 2);
        Boss(stage, 80, 26, 3000, 1, 2);
        Boss(stage, 90, 27, 3400, 1, 3);
        Boss(stage, 100, 25, 4000, 1, 3);
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
