using System.Collections.Generic;
using UnityEngine;

// ステージごとの敵。10階ごとに1帯。ギャラリー以外は300階まで。
// 帯0が1〜10階、帯1が11〜20階、…、帯29が291〜300階。
// 書いていない帯は、100階前の同じ位置を使う。101〜110階は帯0。
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
    public FloorBandSpawn[] Bands = new FloorBandSpawn[Constants.BATTLE_STAGE_FLOOR_CAP / Constants.BATTLE_STAGE_FLOOR_STEP];
    public List<BossFloorSpawn> Bosses = new List<BossFloorSpawn>();
    public List<FloorItemDrop> Drops = new List<FloorItemDrop>();
    public List<CharacterChapterDrop> CharacterDrops = new List<CharacterChapterDrop>();

    public FloorBandSpawn GetBand(int floor)
    {
        if (floor < 1)
            floor = 1;
        int index = (floor - 1) / Constants.BATTLE_STAGE_FLOOR_STEP;
        if (index < 0)
            index = 0;
        FloorBandSpawn written = WrittenBand(index);
        if (written != null)
            return written;
        int chapterBands = Constants.BATTLE_STAGE_FLOOR_MAX / Constants.BATTLE_STAGE_FLOOR_STEP;
        if (chapterBands < 1)
            chapterBands = 1;
        int wrapped = index % chapterBands;
        written = WrittenBand(wrapped);
        if (written != null)
            return written;
        if (Bands.Length == 0)
            return new FloorBandSpawn();
        if (Bands[0] == null)
            Bands[0] = new FloorBandSpawn();
        return Bands[0];
    }

    FloorBandSpawn WrittenBand(int index)
    {
        if (index < 0 || Bands == null || index >= Bands.Length || Bands[index] == null)
            return null;
        if (Bands[index].Entries == null || Bands[index].Entries.Count == 0)
            return null;
        return Bands[index];
    }

    public void RollCharacterDrops(int floor, uint characterId, IBattleRandom rng, List<int> into)
    {
        if (into == null || rng == null || characterId == 0 || CharacterDrops == null)
            return;
        if (floor < 1)
            floor = 1;
        int chapter = (floor - 1) / Constants.BATTLE_STAGE_FLOOR_MAX + 1;
        for (int i = 0; i < CharacterDrops.Count; i++)
        {
            CharacterChapterDrop drop = CharacterDrops[i];
            if (drop == null || drop.Chapter != chapter || drop.CharacterId != characterId)
                continue;
            if (drop.ItemId == 0 || drop.Probability <= 0.0)
                continue;
            if (rng.NextDouble() < drop.Probability)
                into.Add(drop.ItemId);
        }
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
            case 6: return Cave2();
            default: return Prairie();
        }
    }

    // そのステージに一度でも出る敵。帯の並び順で、同じキャラは1回だけ。
    public static void CollectEnemyIds(int stageIndex, List<uint> ids)
    {
        CollectEnemyIds(stageIndex, 1, Constants.BATTLE_STAGE_FLOOR_CAP, ids);
    }

    // floorFrom〜floorTo に出る敵。書いていない帯は、戦闘と同じく100階前の並びを使う。ボスも含む。
    public static void CollectEnemyIds(int stageIndex, int floorFrom, int floorTo, List<uint> ids)
    {
        if (ids == null)
            return;
        ids.Clear();
        if (stageIndex < 1 || stageIndex > Constants.BATTLE_STAGE_NUM)
            return;

        StageBattleContent content = Get(stageIndex);
        if (content == null || content.StageIndex != stageIndex)
            return;

        if (floorFrom < 1)
            floorFrom = 1;
        if (floorTo < floorFrom)
            floorTo = floorFrom;
        int step = Constants.BATTLE_STAGE_FLOOR_STEP;
        if (step < 1)
            step = 1;
        int firstBand = (floorFrom - 1) / step;
        int lastBand = (floorTo - 1) / step;
        for (int band = firstBand; band <= lastBand; band++)
        {
            int floor = band * step + 1;
            if (floor < floorFrom)
                floor = floorFrom;
            FloorBandSpawn spawn = content.GetBand(floor);
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
            if (boss.Floor < floorFrom || boss.Floor > floorTo)
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

    public static Sprite BackgroundByName(string resourceName)
    {
        return LoadBackground(resourceName);
    }

    public static Sprite BackgroundSprite(int stageIndex)
    {
        if (stageIndex < 1 || stageIndex > Constants.BATTLE_STAGE_NUM)
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
        stage.Bands[0] = Band(E(6,1,100,1,1),E(7,1,1,0,1),E(8,1,1,0,1));
        stage.Bands[1] = Band(E(6,1,50,0,1),E(7,1,10,1,1),E(8,1,1,0,1));
        stage.Bands[2] = Band(E(6,1,50,0,1),E(7,1,10,0,1),E(8,1,1,1,1));
        stage.Bands[3] = Band(E(6,2,33,1,1),E(7,2,100,1,1),E(8,2,1,0,1));
        stage.Bands[4] = Band(E(6,2,33,1,1),E(7,2,5,0,1),E(8,2,1,1,1));
        stage.Bands[5] = Band(E(6,2,33,0,1),E(7,2,50,1,1),E(8,2,1,1,1));
        stage.Bands[6] = Band(E(6,3,33,0,1),E(7,3,33,0,1),E(8,3,50,1,1));
        stage.Bands[7] = Band(E(6,3,33,0,1),E(7,3,33,0,1),E(8,3,50,1,1));
        stage.Bands[8] = Band(E(6,3,33,0,1),E(7,3,33,1,1),E(8,3,50,1,1));
        stage.Bands[9] = Band(E(6,4,33,1,2),E(7,4,33,1,2),E(8,4,33,1,2));
        // 101〜200階。0〜9のコピー。
        stage.Bands[10] = Band(E(6,1,50,1,1),E(7,1,10,0,1),E(8,1,10,0,1),E(9,1,10,0,1));
        stage.Bands[11] = Band(E(6,2,10,0,1),E(7,2,50,1,1),E(8,2,10,0,1),E(9,1,20,0,1));
        stage.Bands[12] = Band(E(6,3,33,0,1),E(7,3,10,0,1),E(8,3,50,1,1),E(9,1,30,0,1));
        stage.Bands[13] = Band(E(6,4,33,1,1),E(7,4,33,0,1),E(8,4,33,0,1),E(9,1,40,0,1));
        stage.Bands[14] = Band(E(6,4,33,0,1),E(7,4,33,1,1),E(8,4,33,0,1),E(9,2,50,0,1));
        stage.Bands[15] = Band(E(6,4,33,0,1),E(7,4,33,0,1),E(8,4,33,1,1),E(9,2,60,0,1));
        stage.Bands[16] = Band(E(6,5,33,1,1),E(7,5,33,1,1),E(8,5,63,0,1),E(9,3,70,0,1));
        stage.Bands[17] = Band(E(6,5,63,0,1),E(7,5,33,1,1),E(8,5,33,1,1),E(9,3,90,0,1));
        stage.Bands[18] = Band(E(6,5,33,1,1),E(7,5,63,0,1),E(8,5,33,1,1),E(9,3,90,0,1));
        stage.Bands[19] = Band(E(6,6,13,1,2),E(7,6,13,1,2),E(8,6,13,1,2),E(9,4,40,1,2));
        // 201〜300階。0〜9のコピー。
        stage.Bands[20] = Band(E(6,5,50,1,1),E(7,5,10,1,1),E(8,5,10,1,1),E(9,3,30,0,1));
        stage.Bands[21] = Band(E(6,7,20,1,1),E(7,7,50,1,1),E(8,7,20,1,1),E(9,5,30,0,1),E(10,3,5,0,1));
        stage.Bands[22] = Band(E(6,10,33,1,1),E(7,10,30,1,1),E(8,10,50,1,1),E(9,7,30,0,1),E(10,4,10,0,1));
        stage.Bands[23] = Band(E(6,13,33,1,2),E(7,13,33,1,2),E(8,13,33,1,2),E(9,10,30,0,2),E(10,5,20,0,1));
        stage.Bands[24] = Band(E(6,15,33,1,2),E(7,15,33,1,2),E(8,15,33,1,2),E(9,13,30,1,2),E(10,6,25,0,1));
        stage.Bands[25] = Band(E(6,17,33,1,2),E(7,17,33,1,2),E(8,17,33,1,2),E(9,15,30,1,2),E(10,7,25,0,1));
        stage.Bands[26] = Band(E(6,19,33,1,2),E(7,19,33,1,2),E(8,19,33,1,2),E(9,17,50,1,2),E(10,7,30,0,1));
        stage.Bands[27] = Band(E(6,21,33,1,2),E(7,21,33,1,2),E(8,21,33,1,2),E(9,19,50,1,2),E(10,8,40,0,1));
        stage.Bands[28] = Band(E(6,23,33,1,2),E(7,23,33,1,2),E(8,23,33,1,2),E(9,21,50,1,2),E(10,8,40,0,1));
        stage.Bands[29] = Band(E(6,25,33,1,3),E(7,25,33,1,3),E(8,25,33,1,3),E(9,23,1,1,3),E(10,10,50,1,3));

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
        Boss(stage, 100, 6, 5, 1, 1);
        Boss(stage, 150, 9, 5, 1, 1);
        Boss(stage, 200, 9, 10, 1, 1);
        Boss(stage, 250, 10, 15, 1, 1);
        Boss(stage, 300, 10, 20, 1, 1);
        Shards(stage);
        // 100階区切りのキャラドロップ。章、キャラID、アイテム、確率。倒したときに判定する。
        // 章1は1〜100階、章2は101〜200階、章3は201〜300階。
        // CharacterDrop(stage, 1, 6, ItemIds.StageShard, 0.25);
        return stage;
    }
    // スライムの森
    static StageBattleContent Forest()
    {
        //E の並びは キャラID、レベル、重み、最少、最多
        var stage = Stage(2);
        stage.Background = "nekohatakouen";
        stage.Bands[0] = Band(E(1,1,33,1,1),E(2,1,1,0,1),E(3,1,1,0,1));
        stage.Bands[1] = Band(E(1,1,1,0,1),E(2,1,33,1,1),E(3,1,1,0,1));
        stage.Bands[2] = Band(E(1,1,1,0,1),E(2,1,1,0,1),E(3,1,33,1,1));
        stage.Bands[3] = Band(E(1,1,33,1,1),E(2,1,33,0,1),E(3,1,1,0,1));
        stage.Bands[4] = Band(E(1,1,33,0,1),E(2,1,1,1,1),E(3,1,33,0,1));
        stage.Bands[5] = Band(E(1,1,1,0,1),E(2,1,33,0,1),E(3,1,33,1,1));
        stage.Bands[6] = Band(E(1,2,33,1,2),E(2,2,33,0,2),E(3,2,33,0,2));
        stage.Bands[7] = Band(E(1,2,33,0,2),E(2,2,33,1,2),E(3,2,33,0,2));
        stage.Bands[8] = Band(E(1,2,33,0,2),E(2,2,33,0,2),E(3,2,33,1,2));
        stage.Bands[9] = Band(E(1,3,33,1,2),E(2,3,33,1,2),E(3,3,33,1,2));
        // 101〜200階。0〜9のコピー。
        stage.Bands[10] = Band(E(1,3,33,1,1),E(2,3,33,0,1),E(3,3,33,0,1),E(4,3,25,0,1));
        stage.Bands[11] = Band(E(1,4,33,1,2),E(2,4,33,0,1),E(3,4,33,0,1),E(4,4,25,0,1));
        stage.Bands[12] = Band(E(1,5,33,1,2),E(2,5,33,0,1),E(3,5,33,0,1),E(4,5,30,0,1));
        stage.Bands[13] = Band(E(1,6,33,1,2),E(2,6,33,1,2),E(3,6,33,0,1),E(4,6,40,0,1));
        stage.Bands[14] = Band(E(1,7,33,1,2),E(2,7,33,1,2),E(3,7,33,0,1),E(4,7,50,0,1));
        stage.Bands[15] = Band(E(1,8,33,1,2),E(2,8,33,1,2),E(3,8,33,0,1),E(4,8,60,0,1));
        stage.Bands[16] = Band(E(1,9,33,1,2),E(2,9,33,1,2),E(3,9,33,1,2),E(4,9,70,0,1));
        stage.Bands[17] = Band(E(1,10,33,1,2),E(2,10,33,0,1),E(3,10,33,1,2),E(4,10,80,0,1));
        stage.Bands[18] = Band(E(1,12,33,1,2),E(2,12,33,1,2),E(3,12,33,1,2),E(4,15,90,0,1));
        stage.Bands[19] = Band(E(1,15,33,1,2),E(2,15,33,1,2),E(3,15,33,1,2),E(4,20,60,1,3));
        // 201〜300階。0〜9のコピー。
        stage.Bands[20] = Band(E(1,10,33,1,2),E(2,10,33,1,1),E(3,10,33,1,1),E(4,10,10,0,1));
        stage.Bands[21] = Band(E(1,12,33,1,1),E(2,12,33,1,2),E(3,12,33,1,1),E(4,12,20,0,1));
        stage.Bands[22] = Band(E(1,14,33,1,1),E(2,14,33,1,1),E(3,14,33,1,2),E(4,14,30,0,1));
        stage.Bands[23] = Band(E(1,16,33,1,2),E(2,16,33,1,2),E(3,16,33,1,2),E(4,16,40,0,1),E(5,11,10,0,1));
        stage.Bands[24] = Band(E(1,18,33,1,2),E(2,18,33,1,2),E(3,18,33,1,2),E(4,18,50,0,1),E(5,12,20,0,1));
        stage.Bands[25] = Band(E(1,20,33,1,2),E(2,20,33,1,2),E(3,20,33,1,2),E(4,20,50,0,1),E(5,13,30,0,1));
        stage.Bands[26] = Band(E(1,22,33,1,2),E(2,22,33,1,2),E(3,22,33,1,2),E(4,23,50,0,1),E(5,14,40,0,1));
        stage.Bands[27] = Band(E(1,24,33,1,2),E(2,24,33,1,2),E(3,24,33,1,2),E(4,26,50,0,1),E(5,15,50,0,1));
        stage.Bands[28] = Band(E(1,26,33,1,2),E(2,26,33,1,2),E(3,26,33,1,2),E(4,29,50,0,1),E(5,15,50,0,1));
        stage.Bands[29] = Band(E(1,28,33,1,2),E(2,28,33,1,2),E(3,28,33,1,2),E(4,30,50,1,3),E(5,15,50,1,3));

        //この並びは 階、キャラID、レベル、最少、最多 です。
        Boss(stage, 100, 2, 5, 1, 1);
        Boss(stage, 150, 4, 15, 1, 1);
        Boss(stage, 200, 4, 25, 1, 1);
        Boss(stage, 250, 5, 15, 1, 1);
        Boss(stage, 300, 5, 25, 1, 1);
        Shards(stage);
        return stage;
    }


    // チグサとバトル１
    static StageBattleContent Cave()
    {
        //E の並びは キャラID、レベル、重み、最少、最多
        var stage = Stage(3);
        stage.Background = "chigusa_room";
        stage.Bands[0] = Band(E(16, 1, 63, 1, 1), E(17, 1, 1, 0, 1), E(18, 1, 1, 0, 1));
        stage.Bands[1] = Band(E(16, 1, 2, 0, 1), E(17, 1, 63, 1, 1), E(18, 1, 2, 0, 1));
        stage.Bands[2] = Band(E(16, 1, 3, 0, 1), E(17, 1, 3, 0, 1), E(18, 1, 3, 1, 1));
        stage.Bands[3] = Band(E(16, 1, 63, 1, 1), E(17, 1, 33, 0, 1), E(18, 1, 3, 0, 1));
        stage.Bands[4] = Band(E(16, 1, 3, 0, 1), E(17, 1, 33, 1, 1), E(18, 1, 33, 0, 1));
        stage.Bands[5] = Band(E(16, 1, 33, 0, 1), E(17, 1, 3, 0, 1), E(18, 1, 33, 1, 1));
        stage.Bands[6] = Band(E(16, 2, 63, 1, 2), E(17, 2, 33, 0, 2), E(18, 2, 33, 0, 2));
        stage.Bands[7] = Band(E(16, 2, 63, 0, 2), E(17, 2, 33, 1, 2), E(18, 2, 33, 0, 2));
        stage.Bands[8] = Band(E(16, 2, 63, 0, 2), E(17, 2, 33, 0, 2), E(18, 2, 33, 1, 2));
        stage.Bands[9] = Band(E(16, 3, 63, 1, 3), E(17, 3, 33, 1, 3), E(18, 3, 33, 1, 3));
        // 101〜200階。レベルは前の値から+5。
        stage.Bands[10] = Band(E(16, 20, 63, 1, 1), E(17, 20, 13, 0, 1), E(18, 20, 13, 0, 1), E(19, 15, 1, 0, 1));
        stage.Bands[11] = Band(E(16, 20, 63, 1, 2), E(17, 20, 23, 0, 1), E(18, 20, 23, 0, 1), E(19, 15, 1, 0, 1));
        stage.Bands[12] = Band(E(16, 21, 63, 1, 2), E(17, 21, 33, 0, 1), E(18, 21, 33, 0, 1), E(19, 16, 2, 0, 1));
        stage.Bands[13] = Band(E(16, 21, 63, 1, 2), E(17, 21, 33, 1, 2), E(18, 21, 33, 0, 1), E(19, 16, 20, 0, 1));
        stage.Bands[14] = Band(E(16, 22, 63, 1, 2), E(17, 22, 33, 1, 2), E(18, 22, 33, 0, 1), E(19, 17, 30, 0, 1));
        stage.Bands[15] = Band(E(16, 22, 63, 1, 2), E(17, 22, 33, 1, 2), E(18, 22, 33, 0, 1), E(19, 17, 30, 0, 1));
        stage.Bands[16] = Band(E(16, 23, 63, 1, 2), E(17, 23, 33, 1, 2), E(18, 23, 33, 1, 2), E(19, 18, 40, 0, 1));
        stage.Bands[17] = Band(E(16, 23, 63, 1, 2), E(17, 23, 33, 0, 1), E(18, 23, 33, 1, 2), E(19, 18, 40, 0, 1));
        stage.Bands[18] = Band(E(16, 24, 63, 1, 2), E(17, 24, 33, 1, 2), E(18, 24, 33, 1, 2), E(19, 19, 50, 0, 1));
        stage.Bands[19] = Band(E(16, 25, 63, 1, 2), E(17, 25, 33, 1, 2), E(18, 25, 33, 1, 2), E(19, 20, 50, 1, 3));
        // 201〜300階。レベルは前の値からさらに+5。
        stage.Bands[20] = Band(E(16, 30, 63, 1, 1), E(17, 30, 33, 0, 1), E(18, 30, 43, 0, 1), E(19, 25, 20, 0, 1));
        stage.Bands[21] = Band(E(16, 31, 63, 1, 2), E(17, 31, 23, 0, 1), E(18, 31, 23, 0, 1), E(19, 26, 20, 0, 1));
        stage.Bands[22] = Band(E(16, 32, 63, 1, 2), E(17, 32, 33, 0, 1), E(18, 32, 33, 0, 1), E(19, 27, 30, 0, 1));
        stage.Bands[23] = Band(E(16, 33, 63, 1, 2), E(17, 33, 33, 1, 2), E(18, 33, 33, 0, 1), E(19, 28, 30, 0, 1), E(20, 28, 10, 0, 1));
        stage.Bands[24] = Band(E(16, 34, 63, 1, 2), E(17, 34, 33, 1, 2), E(18, 34, 33, 0, 1), E(19, 29, 40, 0, 1), E(20, 29, 20, 0, 1));
        stage.Bands[25] = Band(E(16, 35, 63, 1, 2), E(17, 35, 33, 1, 2), E(18, 35, 33, 0, 1), E(19, 30, 40, 0, 1), E(20, 30, 20, 0, 1));
        stage.Bands[26] = Band(E(16, 36, 63, 1, 2), E(17, 36, 33, 1, 2), E(18, 36, 33, 1, 2), E(19, 31, 50, 0, 1), E(20, 31, 30, 0, 1));
        stage.Bands[27] = Band(E(16, 37, 63, 1, 2), E(17, 37, 33, 0, 1), E(18, 37, 33, 1, 2), E(19, 32, 50, 0, 1), E(20, 32, 40, 0, 1));
        stage.Bands[28] = Band(E(16, 38, 63, 1, 2), E(17, 38, 33, 1, 2), E(18, 38, 33, 1, 2), E(19, 33, 50, 0, 1), E(20, 33, 50, 0, 1));
        stage.Bands[29] = Band(E(16, 39, 63, 1, 2), E(17, 39, 33, 1, 2), E(18, 39, 33, 1, 2), E(19, 34, 50, 1, 3), E(20, 34, 50, 1, 1));

        //この並びは 階、キャラID、レベル、最少、最多 です。
        Boss(stage, 100, 18, 5, 1, 1);
        Boss(stage, 150, 19, 25, 1, 1);
        Boss(stage, 200, 19, 35, 1, 1);
        Boss(stage, 250, 20, 40, 1, 1);
        Boss(stage, 300, 20, 50, 1, 1);
        Shards(stage);
        return stage;
    }

    // チグサとバトル２（仮。中身はチグサとバトル１と同じ）
    static StageBattleContent Cave2()
    {
        StageBattleContent stage = Cave();
        stage.StageIndex = 6;
        return stage;
    }


    // モアとバトル１
    static StageBattleContent Ruins()
    {
        //E の並びは キャラID、レベル、重み、最少、最多
        var stage = Stage(4);
        stage.Background = "moa_room";
        stage.Bands[0] = Band(E(21, 2, 20, 1, 2), E(22, 2, 10, 0, 1), E(23, 2, 10, 0, 1));
        stage.Bands[1] = Band(E(21, 2, 20, 0, 2), E(22, 2, 20, 1, 2), E(23, 2, 10, 0, 1));
        stage.Bands[2] = Band(E(21, 3, 20, 0, 2), E(22, 3, 20, 0, 2), E(23, 3, 20, 1, 2));
        stage.Bands[3] = Band(E(21, 3, 20, 1, 2), E(22, 3, 20, 1, 2), E(23, 3, 20, 0, 2));
        stage.Bands[4] = Band(E(21, 3, 20, 0, 2), E(22, 3, 20, 1, 2), E(23, 3, 20, 1, 2));
        stage.Bands[5] = Band(E(21, 4, 20, 1, 2), E(22, 4, 20, 0, 2), E(23, 4, 20, 1, 2));
        stage.Bands[6] = Band(E(21, 4, 20, 1, 3), E(22, 4, 20, 1, 3), E(23, 4, 20, 1, 3));
        stage.Bands[7] = Band(E(21, 4, 50, 1, 3), E(22, 4, 50, 1, 3), E(23, 4, 30, 1, 3));
        stage.Bands[8] = Band(E(21, 4, 40, 1, 3), E(22, 4, 40, 1, 3), E(23, 4, 40, 1, 3));
        stage.Bands[9] = Band(E(21, 5, 50, 1, 3), E(22, 5, 50, 1, 3), E(23, 5, 50, 1, 3));
        // 101〜200階。レベルは前の値から+5。
        stage.Bands[10] = Band(E(21, 25, 50, 1, 2), E(22, 25, 30, 0, 2), E(23, 25, 20, 0, 2));
        stage.Bands[11] = Band(E(21, 26, 30, 1, 2), E(22, 26, 50, 0, 2), E(23, 26, 20, 0, 2), E(24, 20, 5, 0, 1));
        stage.Bands[12] = Band(E(21, 27, 20, 1, 2), E(22, 27, 20, 0, 2), E(23, 27, 50, 0, 2), E(24, 21, 10, 0, 1));
        stage.Bands[13] = Band(E(21, 28, 50, 1, 3), E(22, 28, 30, 1, 3), E(23, 28, 20, 0, 3), E(24, 21, 10, 0, 1));
        stage.Bands[14] = Band(E(21, 29, 30, 1, 3), E(22, 29, 50, 1, 3), E(23, 29, 20, 0, 3), E(24, 22, 20, 0, 1));
        stage.Bands[15] = Band(E(21, 30, 20, 1, 3), E(22, 30, 20, 1, 3), E(23, 30, 50, 0, 3), E(24, 22, 20, 0, 1));
        stage.Bands[16] = Band(E(21, 31, 50, 1, 3), E(22, 31, 30, 1, 3), E(23, 31, 20, 0, 3), E(24, 23, 30, 0, 1));
        stage.Bands[17] = Band(E(21, 32, 50, 1, 3), E(22, 32, 50, 1, 3), E(23, 32, 20, 1, 3), E(24, 23, 40, 0, 1));
        stage.Bands[18] = Band(E(21, 33, 50, 1, 3), E(22, 33, 50, 1, 3), E(23, 33, 50, 1, 3), E(24, 24, 50, 0, 1));
        stage.Bands[19] = Band(E(21, 34, 50, 1, 3), E(22, 34, 50, 1, 3), E(23, 34, 50, 1, 3), E(24, 24, 70, 1, 1));
        // 201〜300階。レベルは前の値からさらに+5。
        stage.Bands[20] = Band(E(21, 30, 50, 1, 3), E(22, 30, 30, 0, 2), E(23, 30, 20, 0, 2), E(24, 30, 10, 0, 1));
        stage.Bands[21] = Band(E(21, 30, 30, 1, 3), E(22, 30, 50, 0, 2), E(23, 30, 20, 0, 2), E(24, 30, 20, 0, 1), E(25, 30, 5, 0, 1));
        stage.Bands[22] = Band(E(21, 31, 20, 1, 3), E(22, 31, 20, 0, 2), E(23, 31, 50, 0, 2), E(24, 31, 30, 0, 1), E(25, 31, 10, 0, 1));
        stage.Bands[23] = Band(E(21, 34, 50, 1, 4), E(22, 34, 30, 0, 3), E(23, 34, 20, 0, 3), E(24, 31, 30, 0, 1), E(25, 31, 10, 0, 1));
        stage.Bands[24] = Band(E(21, 37, 30, 1, 4), E(22, 37, 50, 0, 3), E(23, 37, 20, 0, 3), E(24, 32, 30, 0, 1), E(25, 32, 20, 0, 1));
        stage.Bands[25] = Band(E(21, 40, 20, 1, 4), E(22, 40, 20, 0, 3), E(23, 40, 50, 0, 3), E(24, 32, 40, 0, 1), E(25, 32, 30, 0, 1));
        stage.Bands[26] = Band(E(21, 43, 50, 1, 4), E(22, 43, 30, 0, 4), E(23, 43, 20, 0, 4), E(24, 33, 40, 0, 1), E(25, 33, 30, 0, 1));
        stage.Bands[27] = Band(E(21, 46, 50, 1, 4), E(22, 46, 50, 0, 4), E(23, 46, 20, 0, 4), E(24, 33, 50, 1, 1), E(25, 33, 40, 0, 1));
        stage.Bands[28] = Band(E(21, 49, 50, 1, 4), E(22, 49, 50, 0, 4), E(23, 49, 50, 0, 4), E(24, 34, 60, 1, 1), E(25, 34, 50, 0, 1));
        stage.Bands[29] = Band(E(21, 52, 50, 1, 4), E(22, 52, 50, 0, 4), E(23, 52, 50, 0, 4), E(24, 34, 80, 1, 1), E(25, 34, 60, 1, 1));

        //この並びは 階、キャラID、レベル、最少、最多 です。
        Boss(stage, 100, 23, 15, 1, 1);
        Boss(stage, 150, 24, 30, 1, 1);
        Boss(stage, 200, 24, 40, 1, 1);
        Boss(stage, 250, 25, 50, 1, 1);
        Boss(stage, 300, 25, 70, 1, 1);
        Shards(stage);
        return stage;
    }



    // ソウガとバトル１
    static StageBattleContent Castle()
    {
        var stage = Stage(5);
        stage.Background = "souga_room";
        stage.Bands[0] = Band(E(26, 25, 20, 1, 2), E(27, 25, 20, 0, 2), E(28, 25, 20, 0, 2));
        stage.Bands[1] = Band(E(26, 26, 20, 0, 2), E(27, 26, 20, 1, 2), E(28, 26, 20, 0, 2));
        stage.Bands[2] = Band(E(26, 27, 20, 0, 2), E(27, 27, 20, 0, 2), E(28, 27, 20, 1, 2));
        stage.Bands[3] = Band(E(26, 28, 30, 1, 2), E(27, 28, 30, 1, 2), E(28, 28, 30, 0, 2));
        stage.Bands[4] = Band(E(26, 30 ,30, 0, 2), E(27, 30, 30, 1, 2), E(28, 30, 30, 1, 2));
        stage.Bands[5] = Band(E(26, 32, 30, 1, 2), E(27, 32, 30, 0, 2), E(28, 32, 30, 1, 2));
        stage.Bands[6] = Band(E(26, 34, 40, 1, 2), E(27, 34, 40, 1, 2), E(28, 34, 40, 1, 2));
        stage.Bands[7] = Band(E(26, 36, 40, 1, 3), E(27, 36, 40, 1, 3), E(28, 36, 40, 1, 3));
        stage.Bands[8] = Band(E(26, 38, 40, 1, 3), E(27, 38, 40, 1, 3), E(28, 38, 40, 1, 3));
        stage.Bands[9] = Band(E(26, 40, 50, 1, 3), E(27, 40, 50, 1, 3), E(28, 40, 50, 1, 3));
        // 101〜200階。レベルは前の値から+5。
        stage.Bands[10] = Band(E(26, 60, 50, 1, 2), E(27, 60, 30, 0, 2), E(28, 60, 20, 0, 2));
        stage.Bands[11] = Band(E(26, 61, 30, 1, 2), E(27, 61, 50, 0, 2), E(28, 61, 20, 0, 2), E(29, 55, 5, 0, 1));
        stage.Bands[12] = Band(E(26, 62, 20, 1, 2), E(27, 62, 20, 0, 2), E(28, 62, 50, 0, 2), E(29, 56, 10, 0, 1));
        stage.Bands[13] = Band(E(26, 63, 50, 1, 3), E(27, 63, 30, 1, 3), E(28, 63, 20, 0, 3), E(29, 56, 10, 0, 1));
        stage.Bands[14] = Band(E(26, 64, 30, 1, 3), E(27, 64, 50, 1, 3), E(28, 64, 20, 0, 3), E(29, 57, 20, 0, 1));
        stage.Bands[15] = Band(E(26, 65, 20, 1, 3), E(27, 65, 20, 1, 3), E(28, 65, 50, 0, 3), E(29, 57, 20, 0, 1));
        stage.Bands[16] = Band(E(26, 66, 50, 1, 3), E(27, 66, 30, 1, 3), E(28, 66, 20, 0, 3), E(29, 58, 30, 0, 1));
        stage.Bands[17] = Band(E(26, 67, 50, 1, 3), E(27, 67, 50, 1, 3), E(28, 67, 20, 1, 3), E(29, 58, 40, 0, 1));
        stage.Bands[18] = Band(E(26, 68, 50, 1, 3), E(27, 68, 50, 1, 3), E(28, 68, 50, 1, 3), E(29, 59, 50, 0, 1));
        stage.Bands[19] = Band(E(26, 69, 50, 1, 3), E(27, 69, 50, 1, 3), E(28, 69, 50, 1, 3), E(29, 59, 70, 1, 1));
        // 201〜300階。レベルは前の値からさらに+5。
        stage.Bands[20] = Band(E(26, 75, 50, 1, 3), E(27, 75, 30, 0, 2), E(28, 75, 20, 0, 2), E(29, 75, 10, 0, 1));
        stage.Bands[21] = Band(E(26, 75, 30, 1, 3), E(27, 75, 50, 0, 2), E(28, 75, 20, 0, 2), E(29, 75, 20, 0, 1), E(30, 85, 1, 0, 1));
        stage.Bands[22] = Band(E(26, 76, 20, 1, 3), E(27, 76, 20, 0, 2), E(28, 76, 50, 0, 2), E(29, 76, 30, 0, 1), E(30, 86, 5, 0, 1));
        stage.Bands[23] = Band(E(26, 79, 50, 1, 4), E(27, 79, 30, 0, 3), E(28, 79, 20, 0, 3), E(29, 76, 30, 0, 1), E(30, 86, 5, 0, 1));
        stage.Bands[24] = Band(E(26, 82, 30, 1, 4), E(27, 82, 50, 0, 3), E(28, 82, 20, 0, 3), E(29, 77, 30, 0, 1), E(30, 87, 10, 0, 1));
        stage.Bands[25] = Band(E(26, 85, 20, 1, 4), E(27, 85, 20, 0, 3), E(28, 85, 50, 0, 3), E(29, 77, 40, 0, 1), E(30, 87, 10, 0, 1));
        stage.Bands[26] = Band(E(26, 88, 50, 1, 4), E(27, 88, 30, 0, 4), E(28, 88, 20, 0, 4), E(29, 78, 40, 0, 1), E(30, 88, 20, 0, 1));
        stage.Bands[27] = Band(E(26, 91, 50, 1, 4), E(27, 91, 50, 0, 4), E(28, 91, 20, 0, 4), E(29, 78, 50, 1, 1), E(30, 88, 20, 0, 1));
        stage.Bands[28] = Band(E(26, 94, 50, 1, 4), E(27, 94, 50, 0, 4), E(28, 94, 50, 0, 4), E(29, 79, 60, 1, 1), E(30, 89, 30, 0, 1));
        stage.Bands[29] = Band(E(26, 97, 50, 1, 4), E(27, 97, 50, 0, 4), E(28, 97, 50, 0, 4), E(29, 79, 80, 1, 1), E(30, 89, 30, 1, 1));

        //この並びは 階、キャラID、レベル、最少、最多 です。
        Boss(stage, 100, 28, 50, 1, 1);
        Boss(stage, 150, 29, 75, 1, 1);
        Boss(stage, 200, 29, 85, 1, 1);
        Boss(stage, 250, 30, 120, 1, 1);
        Boss(stage, 300, 30, 130, 1, 1);
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
        for (int floor = 10; floor <= Constants.BATTLE_STAGE_FLOOR_CAP; floor += 10)
            stage.Drops.Add(new FloorItemDrop(floor, ItemIds.StageShard, 0.25));
    }

    // 章、キャラID、アイテム、確率。章1は1〜100階、章2は101〜200階、章3は201〜300階。倒したときに判定する。
    static void CharacterDrop(StageBattleContent stage, int chapter, uint characterId, int itemId, double probability)
    {
        if (stage == null || characterId == 0 || itemId == 0 || probability <= 0.0)
            return;
        if (stage.CharacterDrops == null)
            stage.CharacterDrops = new List<CharacterChapterDrop>();
        stage.CharacterDrops.Add(new CharacterChapterDrop(chapter, characterId, itemId, probability));
    }
}
