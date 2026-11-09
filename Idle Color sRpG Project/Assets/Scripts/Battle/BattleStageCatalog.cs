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
        if (stageIndex < 1 || stageIndex > 5)
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
        stage.Bands[0] = Band(E(6,1,100,1,1),E(7,1,1,0,1),E(8,1,1,0,1));
        stage.Bands[1] = Band(E(6,1,50,0,1),E(7,1,10,1,1),E(8,1,1,0,1));
        stage.Bands[2] = Band(E(6,1,50,0,1),E(7,1,10,0,1),E(8,1,1,1,1));
        stage.Bands[3] = Band(E(6,2,33,1,1),E(7,2,100,1,1),E(8,2,1,0,1));
        stage.Bands[4] = Band(E(6,2,33,1,1),E(7,2,5,0,2),E(8,2,1,1,1));
        stage.Bands[5] = Band(E(6,2,33,0,1),E(7,2,50,1,2),E(8,2,1,1,1));
        stage.Bands[6] = Band(E(6,3,33,0,1),E(7,3,33,0,1),E(8,3,50,1,1));
        stage.Bands[7] = Band(E(6,3,33,0,1),E(7,3,33,0,1),E(8,3,50,1,2));
        stage.Bands[8] = Band(E(6,3,33,0,1),E(7,3,33,1,1),E(8,3,50,1,3));
        stage.Bands[9] = Band(E(6,5,33,1,3),E(7,5,33,1,3),E(8,5,33,1,3));
        // 101〜200階。0〜9のコピー。
        stage.Bands[10] = Band(E(6,5,50,1,1),E(7,5,10,0,1),E(8,5,10,0,1),E(9,1,10,0,1));
        stage.Bands[11] = Band(E(6,5,10,0,1),E(7,5,50,1,1),E(8,5,10,0,1),E(9,2,20,0,1));
        stage.Bands[12] = Band(E(6,5,33,0,1),E(7,5,10,0,1),E(8,5,50,1,1),E(9,3,30,0,1));
        stage.Bands[13] = Band(E(6,6,33,1,2),E(7,6,33,1,2),E(8,6,33,0,1),E(9,4,40,0,1));
        stage.Bands[14] = Band(E(6,6,33,1,2),E(7,6,33,1,2),E(8,6,33,0,1),E(9,5,50,0,1));
        stage.Bands[15] = Band(E(6,6,33,1,2),E(7,6,33,1,2),E(8,6,33,0,1),E(9,6,60,0,1));
        stage.Bands[16] = Band(E(6,7,33,1,2),E(7,7,33,1,2),E(8,7,33,1,2),E(9,7,70,0,1));
        stage.Bands[17] = Band(E(6,7,33,1,2),E(7,7,33,0,1),E(8,7,33,1,2),E(9,8,90,0,1));
        stage.Bands[18] = Band(E(6,7,33,1,3),E(7,7,33,1,3),E(8,7,33,1,3),E(9,9,90,0,1));
        stage.Bands[19] = Band(E(6,10,33,1,3),E(7,10,33,1,3),E(8,10,33,1,3),E(9,10,60,1,3));
        // 201〜300階。0〜9のコピー。
        stage.Bands[20] = Band(E(6,11,50,1,1),E(7,11,10,1,1),E(8,11,10,1,1),E(9,11,30,0,1));
        stage.Bands[21] = Band(E(6,12,10,1,1),E(7,12,50,1,1),E(8,12,10,1,1),E(9,12,30,0,1),E(10,6,5,0,1));
        stage.Bands[22] = Band(E(6,13,33,1,1),E(7,13,10,1,1),E(8,13,50,1,1),E(9,13,30,0,1),E(10,7,10,0,1));
        stage.Bands[23] = Band(E(6,14,33,1,2),E(7,14,33,1,2),E(8,14,33,1,1),E(9,14,30,0,1),E(10,8,20,0,1));
        stage.Bands[24] = Band(E(6,15,33,1,2),E(7,15,33,1,2),E(8,15,33,1,1),E(9,15,30,1,1),E(10,9,20,0,1));
        stage.Bands[25] = Band(E(6,16,33,1,2),E(7,16,33,1,2),E(8,16,33,1,1),E(9,16,30,1,1),E(10,10,20,0,1));
        stage.Bands[26] = Band(E(6,17,33,1,2),E(7,17,33,1,2),E(8,17,33,1,2),E(9,17,50,1,1),E(10,11,30,0,1));
        stage.Bands[27] = Band(E(6,18,33,1,2),E(7,18,33,1,1),E(8,18,33,1,2),E(9,18,50,1,1),E(10,12,30,0,1));
        stage.Bands[28] = Band(E(6,19,33,1,2),E(7,19,33,1,2),E(8,19,33,1,2),E(9,19,50,1,2),E(10,13,30,0,1));
        stage.Bands[29] = Band(E(6,20,33,1,3),E(7,20,33,1,3),E(8,20,33,1,3),E(9,20,1,1,3),E(10,14,50,1,3));

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
        Boss(stage, 200, 9, 20, 1, 1);
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
        stage.Bands[0] = Band(E(1,5,33,1,2),E(2,5,33,0,1),E(3,5,33,0,1));
        stage.Bands[1] = Band(E(1,6,33,1,2),E(2,6,33,0,1),E(3,6,33,0,1));
        stage.Bands[2] = Band(E(1,7,33,1,2),E(2,7,33,0,1),E(3,7,33,0,1));
        stage.Bands[3] = Band(E(1,8,33,0,1),E(2,8,33,1,2),E(3,8,33,0,1));
        stage.Bands[4] = Band(E(1,9,33,0,1),E(2,9,33,1,2),E(3,9,33,0,1));
        stage.Bands[5] = Band(E(1,10,33,0,1),E(2,10,33,1,2),E(3,10,33,0,1));
        stage.Bands[6] = Band(E(1,11,33,0,2),E(2,11,33,0,2),E(3,11,33,1,2));
        stage.Bands[7] = Band(E(1,12,33,0,2),E(2,12,33,0,2),E(3,12,33,1,2));
        stage.Bands[8] = Band(E(1,13,33,0,2),E(2,13,33,0,2),E(3,13,33,1,2));
        stage.Bands[9] = Band(E(1,14,33,1,3),E(2,14,33,1,3),E(3,14,33,1,3));
        // 101〜200階。0〜9のコピー。
        stage.Bands[10] = Band(E(1,15,33,1,2),E(2,15,33,0,1),E(3,15,33,0,1),E(4,10,25,0,1));
        stage.Bands[11] = Band(E(1,16,33,1,2),E(2,16,33,0,1),E(3,16,33,0,1),E(4,11,25,0,1));
        stage.Bands[12] = Band(E(1,17,33,1,2),E(2,17,33,0,1),E(3,17,33,0,1),E(4,12,30,0,1));
        stage.Bands[13] = Band(E(1,18,33,1,2),E(2,18,33,1,2),E(3,18,33,0,1),E(4,13,40,0,1));
        stage.Bands[14] = Band(E(1,19,33,1,2),E(2,19,33,1,2),E(3,19,33,0,1),E(4,14,50,0,1));
        stage.Bands[15] = Band(E(1,20,33,1,2),E(2,20,33,1,2),E(3,20,33,0,1),E(4,15,60,0,1));
        stage.Bands[16] = Band(E(1,21,33,1,2),E(2,21,33,1,2),E(3,21,33,1,2),E(4,16,70,0,1));
        stage.Bands[17] = Band(E(1,22,33,1,2),E(2,22,33,0,1),E(3,22,33,1,2),E(4,17,80,0,1));
        stage.Bands[18] = Band(E(1,23,33,1,2),E(2,23,33,1,2),E(3,23,33,1,2),E(4,18,90,0,1));
        stage.Bands[19] = Band(E(1,24,33,1,2),E(2,24,33,1,2),E(3,24,33,1,2),E(4,19,60,1,3));
        // 201〜300階。0〜9のコピー。
        stage.Bands[20] = Band(E(1,25,33,1,2),E(2,25,33,0,1),E(3,25,33,0,1),E(4,20,10,0,1));
        stage.Bands[21] = Band(E(1,26,33,1,2),E(2,26,33,0,1),E(3,26,33,0,1),E(4,21,20,0,1));
        stage.Bands[22] = Band(E(1,27,33,1,2),E(2,27,33,0,1),E(3,27,33,0,1),E(4,22,30,0,1));
        stage.Bands[23] = Band(E(1,28,33,1,2),E(2,28,33,1,2),E(3,28,33,0,1),E(4,23,40,0,1),E(5,15,10,0,1));
        stage.Bands[24] = Band(E(1,29,33,1,2),E(2,29,33,1,2),E(3,29,33,0,1),E(4,24,50,0,1),E(5,16,20,0,1));
        stage.Bands[25] = Band(E(1,30,33,1,2),E(2,30,33,1,2),E(3,30,33,0,1),E(4,25,50,0,1),E(5,17,30,0,1));
        stage.Bands[26] = Band(E(1,30,33,1,2),E(2,30,33,1,2),E(3,30,33,1,2),E(4,26,50,0,1),E(5,18,40,0,1));
        stage.Bands[27] = Band(E(1,30,33,1,2),E(2,30,33,0,1),E(3,30,33,1,2),E(4,27,50,0,1),E(5,19,50,0,1));
        stage.Bands[28] = Band(E(1,30,33,1,2),E(2,30,33,1,2),E(3,30,33,1,2),E(4,28,50,0,1),E(5,20,50,0,1));
        stage.Bands[29] = Band(E(1,30,33,1,2),E(2,30,33,1,2),E(3,30,33,1,2),E(4,29,50,1,3),E(5,20,50,1,3));

        //この並びは 階、キャラID、レベル、最少、最多 です。
        Boss(stage, 200, 4, 25, 1, 1);
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
        stage.Bands[0] = Band(E(16, 5, 63, 1, 1), E(17, 5, 13, 0, 1), E(18, 5, 13, 0, 1));
        stage.Bands[1] = Band(E(16, 6, 33, 0, 1), E(17, 6, 63, 1, 1), E(18, 6, 23, 0, 1));
        stage.Bands[2] = Band(E(16, 7, 33, 0, 1), E(17, 7, 33, 0, 1), E(18, 7, 63, 1, 1));
        stage.Bands[3] = Band(E(16, 8, 63, 1, 2), E(17, 8, 33, 1, 2), E(18, 8, 33, 0, 1));
        stage.Bands[4] = Band(E(16, 9, 63, 1, 2), E(17, 9, 33, 1, 2), E(18, 9, 33, 0, 1));
        stage.Bands[5] = Band(E(16, 10, 63, 1, 2), E(17, 10, 33, 1, 2), E(18, 10, 33, 0, 1));
        stage.Bands[6] = Band(E(16, 11, 63, 1, 2), E(17, 11, 33, 1, 2), E(18, 11, 33, 1, 2));
        stage.Bands[7] = Band(E(16, 12, 63, 1, 2), E(17, 12, 33, 0, 1), E(18, 12, 33, 1, 2));
        stage.Bands[8] = Band(E(16, 13, 63, 1, 2), E(17, 13, 33, 1, 2), E(18, 13, 33, 1, 2));
        stage.Bands[9] = Band(E(16, 14, 63, 1, 3), E(17, 14, 33, 1, 3), E(18, 14, 33, 1, 3));
        // 101〜200階。0〜9のコピー。
        stage.Bands[10] = Band(E(16, 15, 63, 1, 1), E(17, 15, 13, 0, 1), E(18, 15, 13, 0, 1), E(19, 5, 1, 0, 1));
        stage.Bands[11] = Band(E(16, 16, 63, 1, 2), E(17, 16, 23, 0, 1), E(18, 16, 23, 0, 1), E(20, 6, 1, 0, 1));
        stage.Bands[12] = Band(E(16, 17, 63, 1, 2), E(17, 17, 33, 0, 1), E(18, 17, 33, 0, 1), E(19, 7, 2, 0, 1));
        stage.Bands[13] = Band(E(16, 18, 63, 1, 2), E(17, 18, 33, 1, 2), E(18, 18, 33, 0, 1), E(20, 8, 20, 0, 1));
        stage.Bands[14] = Band(E(16, 19, 63, 1, 2), E(17, 19, 33, 1, 2), E(18, 19, 33, 0, 1), E(19, 9, 30, 0, 1));
        stage.Bands[15] = Band(E(16, 20, 63, 1, 2), E(17, 20, 33, 1, 2), E(18, 20, 33, 0, 1), E(20, 10, 30, 0, 1));
        stage.Bands[16] = Band(E(16, 21, 63, 1, 2), E(17, 21, 33, 1, 2), E(18, 21, 33, 1, 2), E(19, 10, 40, 0, 1));
        stage.Bands[17] = Band(E(16, 22, 63, 1, 2), E(17, 22, 33, 0, 1), E(18, 22, 33, 1, 2), E(20, 10, 40, 0, 1));
        stage.Bands[18] = Band(E(16, 23, 63, 1, 2), E(17, 23, 33, 1, 2), E(18, 23, 33, 1, 2), E(19, 10, 50, 0, 1));
        stage.Bands[19] = Band(E(16, 24, 63, 1, 2), E(17, 24, 33, 1, 2), E(18, 24, 33, 1, 2), E(20, 10, 50, 1, 3));
        // 201〜300階。0〜9のコピー。
        stage.Bands[20] = Band(E(16, 22, 63, 1, 1), E(17, 22, 33, 0, 1), E(18, 22, 43, 0, 1), E(19, 10, 20, 0, 1));
        stage.Bands[21] = Band(E(16, 24, 63, 1, 2), E(17, 24, 23, 0, 1), E(18, 24, 23, 0, 1), E(19, 11, 20, 0, 1));
        stage.Bands[22] = Band(E(16, 26, 63, 1, 2), E(17, 26, 33, 0, 1), E(18, 26, 33, 0, 1), E(19, 12, 30, 0, 1));
        stage.Bands[23] = Band(E(16, 28, 63, 1, 2), E(17, 28, 33, 1, 2), E(18, 28, 33, 0, 1), E(19, 13, 30, 0, 1), E(20, 15, 10, 0, 1));
        stage.Bands[24] = Band(E(16, 30, 63, 1, 2), E(17, 30, 33, 1, 2), E(18, 30, 33, 0, 1), E(19, 14, 40, 0, 1), E(20, 16, 20, 0, 1));
        stage.Bands[25] = Band(E(16, 32, 63, 1, 2), E(17, 32, 33, 1, 2), E(18, 32, 33, 0, 1), E(19, 15, 40, 0, 1), E(20, 17, 20, 0, 1));
        stage.Bands[26] = Band(E(16, 34, 63, 1, 2), E(17, 34, 33, 1, 2), E(18, 34, 33, 1, 2), E(19, 16, 50, 0, 1), E(20, 18, 30, 0, 1));
        stage.Bands[27] = Band(E(16, 36, 63, 1, 2), E(17, 36, 33, 0, 1), E(18, 36, 33, 1, 2), E(19, 17, 50, 0, 1), E(20, 19, 30, 0, 1));
        stage.Bands[28] = Band(E(16, 38, 63, 1, 2), E(17, 38, 33, 1, 2), E(18, 38, 33, 1, 2), E(19, 18, 50, 0, 1), E(20, 20, 50, 0, 1));
        stage.Bands[29] = Band(E(16, 40, 63, 1, 2), E(17, 40, 33, 1, 2), E(18, 40, 33, 1, 2), E(19, 19, 50, 1, 3), E(20, 20, 50, 1, 1));

        //この並びは 階、キャラID、レベル、最少、最多 です。
        Boss(stage, 200, 19, 15, 1, 2);
        Boss(stage, 300, 20, 25, 1, 1);
        Shards(stage);
        return stage;
    }


    // モアとバトル１
    static StageBattleContent Ruins()
    {
        //E の並びは キャラID、レベル、重み、最少、最多
        var stage = Stage(4);
        stage.Background = "moa_room";
        stage.Bands[0] = Band(E(21, 10, 50, 1, 3), E(22, 10, 30, 0, 2), E(23, 10, 20, 0, 2), E(24, 15, 1, 0, 1));
        stage.Bands[1] = Band(E(21, 13, 30, 1, 3), E(22, 13, 50, 0, 2), E(23, 13, 20, 0, 2), E(25, 15, 1, 0, 1));
        stage.Bands[2] = Band(E(21, 16, 20, 1, 3), E(22, 16, 20, 0, 2), E(23, 16, 50, 0, 2), E(24, 15, 1, 0, 1));
        stage.Bands[3] = Band(E(21, 19, 50, 1, 4), E(22, 19, 30, 0, 3), E(23, 19, 20, 0, 3), E(25, 15, 1, 0, 1));
        stage.Bands[4] = Band(E(21, 22, 30, 1, 4), E(22, 22, 50, 0, 3), E(23, 22, 20, 0, 3), E(24, 15, 1, 0, 1));
        stage.Bands[5] = Band(E(21, 25, 20, 1, 4), E(22, 25, 20, 0, 3), E(23, 25, 50, 0, 3), E(25, 25, 1, 0, 1));
        stage.Bands[6] = Band(E(21, 28, 50, 1, 4), E(22, 28, 30, 0, 4), E(23, 28, 20, 0, 4), E(24, 25, 1, 0, 1));
        stage.Bands[7] = Band(E(21, 31, 50, 1, 4), E(22, 31, 50, 0, 4), E(23, 31, 20, 0, 4), E(25, 25, 1, 0, 1));
        stage.Bands[8] = Band(E(21, 34, 50, 1, 4), E(22, 34, 50, 0, 4), E(23, 34, 50, 0, 4), E(24, 25, 1, 0, 1));
        stage.Bands[9] = Band(E(21, 37, 50, 1, 4), E(22, 37, 50, 0, 4), E(23, 37, 50, 0, 4), E(25, 25, 1, 0, 1));
        // 101〜200階。0〜9のコピー。
        stage.Bands[10] = Band(E(21, 10, 50, 1, 3), E(22, 10, 30, 0, 2), E(23, 10, 20, 0, 2), E(24, 15, 1, 0, 1));
        stage.Bands[11] = Band(E(21, 13, 30, 1, 3), E(22, 13, 50, 0, 2), E(23, 13, 20, 0, 2), E(25, 15, 1, 0, 1));
        stage.Bands[12] = Band(E(21, 16, 20, 1, 3), E(22, 16, 20, 0, 2), E(23, 16, 50, 0, 2), E(24, 15, 1, 0, 1));
        stage.Bands[13] = Band(E(21, 19, 50, 1, 4), E(22, 19, 30, 0, 3), E(23, 19, 20, 0, 3), E(25, 15, 1, 0, 1));
        stage.Bands[14] = Band(E(21, 22, 30, 1, 4), E(22, 22, 50, 0, 3), E(23, 22, 20, 0, 3), E(24, 15, 1, 0, 1));
        stage.Bands[15] = Band(E(21, 25, 20, 1, 4), E(22, 25, 20, 0, 3), E(23, 25, 50, 0, 3), E(25, 25, 1, 0, 1));
        stage.Bands[16] = Band(E(21, 28, 50, 1, 4), E(22, 28, 30, 0, 4), E(23, 28, 20, 0, 4), E(24, 25, 1, 0, 1));
        stage.Bands[17] = Band(E(21, 31, 50, 1, 4), E(22, 31, 50, 0, 4), E(23, 31, 20, 0, 4), E(25, 25, 1, 0, 1));
        stage.Bands[18] = Band(E(21, 34, 50, 1, 4), E(22, 34, 50, 0, 4), E(23, 34, 50, 0, 4), E(24, 25, 1, 0, 1));
        stage.Bands[19] = Band(E(21, 37, 50, 1, 4), E(22, 37, 50, 0, 4), E(23, 37, 50, 0, 4), E(25, 25, 1, 0, 1));
        // 201〜300階。0〜9のコピー。
        stage.Bands[20] = Band(E(21, 10, 50, 1, 3), E(22, 10, 30, 0, 2), E(23, 10, 20, 0, 2), E(24, 15, 1, 0, 1));
        stage.Bands[21] = Band(E(21, 13, 30, 1, 3), E(22, 13, 50, 0, 2), E(23, 13, 20, 0, 2), E(25, 15, 1, 0, 1));
        stage.Bands[22] = Band(E(21, 16, 20, 1, 3), E(22, 16, 20, 0, 2), E(23, 16, 50, 0, 2), E(24, 15, 1, 0, 1));
        stage.Bands[23] = Band(E(21, 19, 50, 1, 4), E(22, 19, 30, 0, 3), E(23, 19, 20, 0, 3), E(25, 15, 1, 0, 1));
        stage.Bands[24] = Band(E(21, 22, 30, 1, 4), E(22, 22, 50, 0, 3), E(23, 22, 20, 0, 3), E(24, 15, 1, 0, 1));
        stage.Bands[25] = Band(E(21, 25, 20, 1, 4), E(22, 25, 20, 0, 3), E(23, 25, 50, 0, 3), E(25, 25, 1, 0, 1));
        stage.Bands[26] = Band(E(21, 28, 50, 1, 4), E(22, 28, 30, 0, 4), E(23, 28, 20, 0, 4), E(24, 25, 1, 0, 1));
        stage.Bands[27] = Band(E(21, 31, 50, 1, 4), E(22, 31, 50, 0, 4), E(23, 31, 20, 0, 4), E(25, 25, 1, 0, 1));
        stage.Bands[28] = Band(E(21, 34, 50, 1, 4), E(22, 34, 50, 0, 4), E(23, 34, 50, 0, 4), E(24, 25, 1, 0, 1));
        stage.Bands[29] = Band(E(21, 37, 50, 1, 4), E(22, 37, 50, 0, 4), E(23, 37, 50, 0, 4), E(25, 25, 1, 0, 1));

        //この並びは 階、キャラID、レベル、最少、最多 です。
        Boss(stage, 50, 24, 25, 1, 2);
        Boss(stage, 100, 25, 50, 1, 1);
        Shards(stage);
        return stage;
    }



    // ？？？
    static StageBattleContent Castle()
    {
        var stage = Stage(5);
        stage.Background = "";
        stage.Bands[0] = Band(E(21, 160, 50, 1, 3), E(22, 160, 30, 0, 2), E(23, 160, 20, 0, 2));
        stage.Bands[1] = Band(E(22, 180, 50, 1, 3), E(23, 180, 30, 0, 2), E(25, 180, 20, 0, 2));
        stage.Bands[2] = Band(E(23, 200, 50, 1, 3), E(25, 200, 30, 0, 2), E(21, 200, 20, 0, 2));
        stage.Bands[3] = Band(E(25, 220, 50, 1, 4), E(21, 220, 30, 0, 3), E(22, 220, 20, 0, 3));
        stage.Bands[4] = Band(E(21, 240, 50, 1, 4), E(22, 240, 30, 0, 3), E(23, 240, 20, 0, 3));
        stage.Bands[5] = Band(E(22, 260, 50, 1, 4), E(23, 260, 30, 0, 3), E(25, 260, 20, 0, 3));
        stage.Bands[6] = Band(E(23, 280, 50, 1, 4), E(25, 280, 30, 0, 4), E(21, 280, 20, 0, 4));
        stage.Bands[7] = Band(E(25, 300, 50, 1, 4), E(21, 300, 30, 0, 4), E(22, 300, 20, 0, 4));
        stage.Bands[8] = Band(E(21, 340, 50, 1, 4), E(22, 340, 30, 0, 4), E(23, 340, 20, 0, 4));
        stage.Bands[9] = Band(E(22, 400, 50, 1, 4), E(23, 400, 30, 0, 4), E(25, 400, 20, 0, 4));
        // 101〜200階。0〜9のコピー。
        stage.Bands[10] = Band(E(21, 160, 50, 1, 3), E(22, 160, 30, 0, 2), E(23, 160, 20, 0, 2));
        stage.Bands[11] = Band(E(22, 180, 50, 1, 3), E(23, 180, 30, 0, 2), E(25, 180, 20, 0, 2));
        stage.Bands[12] = Band(E(23, 200, 50, 1, 3), E(25, 200, 30, 0, 2), E(21, 200, 20, 0, 2));
        stage.Bands[13] = Band(E(25, 220, 50, 1, 4), E(21, 220, 30, 0, 3), E(22, 220, 20, 0, 3));
        stage.Bands[14] = Band(E(21, 240, 50, 1, 4), E(22, 240, 30, 0, 3), E(23, 240, 20, 0, 3));
        stage.Bands[15] = Band(E(22, 260, 50, 1, 4), E(23, 260, 30, 0, 3), E(25, 260, 20, 0, 3));
        stage.Bands[16] = Band(E(23, 280, 50, 1, 4), E(25, 280, 30, 0, 4), E(21, 280, 20, 0, 4));
        stage.Bands[17] = Band(E(25, 300, 50, 1, 4), E(21, 300, 30, 0, 4), E(22, 300, 20, 0, 4));
        stage.Bands[18] = Band(E(21, 340, 50, 1, 4), E(22, 340, 30, 0, 4), E(23, 340, 20, 0, 4));
        stage.Bands[19] = Band(E(22, 400, 50, 1, 4), E(23, 400, 30, 0, 4), E(25, 400, 20, 0, 4));
        // 201〜300階。0〜9のコピー。
        stage.Bands[20] = Band(E(21, 160, 50, 1, 3), E(22, 160, 30, 0, 2), E(23, 160, 20, 0, 2));
        stage.Bands[21] = Band(E(22, 180, 50, 1, 3), E(23, 180, 30, 0, 2), E(25, 180, 20, 0, 2));
        stage.Bands[22] = Band(E(23, 200, 50, 1, 3), E(25, 200, 30, 0, 2), E(21, 200, 20, 0, 2));
        stage.Bands[23] = Band(E(25, 220, 50, 1, 4), E(21, 220, 30, 0, 3), E(22, 220, 20, 0, 3));
        stage.Bands[24] = Band(E(21, 240, 50, 1, 4), E(22, 240, 30, 0, 3), E(23, 240, 20, 0, 3));
        stage.Bands[25] = Band(E(22, 260, 50, 1, 4), E(23, 260, 30, 0, 3), E(25, 260, 20, 0, 3));
        stage.Bands[26] = Band(E(23, 280, 50, 1, 4), E(25, 280, 30, 0, 4), E(21, 280, 20, 0, 4));
        stage.Bands[27] = Band(E(25, 300, 50, 1, 4), E(21, 300, 30, 0, 4), E(22, 300, 20, 0, 4));
        stage.Bands[28] = Band(E(21, 340, 50, 1, 4), E(22, 340, 30, 0, 4), E(23, 340, 20, 0, 4));
        stage.Bands[29] = Band(E(22, 400, 50, 1, 4), E(23, 400, 30, 0, 4), E(25, 400, 20, 0, 4));
        Boss(stage, 10, 25, 160, 1, 1);
        Boss(stage, 20, 26, 180, 1, 1);
        Boss(stage, 30, 27, 200, 1, 1);
        Boss(stage, 40, 25, 220, 1, 1);
        Boss(stage, 50, 26, 240, 1, 2);
        Boss(stage, 60, 27, 260, 1, 2);
        Boss(stage, 70, 25, 280, 1, 2);
        Boss(stage, 80, 26, 300, 1, 2);
        Boss(stage, 90, 27, 340, 1, 3);
        Boss(stage, 100, 25, 40, 1, 3);
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
