using System.Collections.Generic;

// クエストの定義。一覧は番号の小さい順。増やすときは空いている番号で All に1行足す。一度決めた番号は変えない。
public enum QuestMetric
{
    StageFloor,
    GalleryClears,
    RgbProduced,
    PixelCompletions,
    CharacterCompletions,
    EnemyDefeats,
    ImportedOfSize,
    FusionCount,
    StageCompanions,
    RgbHeld,
    BlackPixels,
    PrimaryPixels,
    MiniSlimes
}

public enum QuestRewardKind
{
    ChooseSlots,
    ImportCap,
    RgbAll,
    CharacterBundle,
    FusionUnlock,
    FusionEfficiency,
    TapChooseSlot,
    UnlockFeature,
    GrantPalette,
    BothSlots,
    LevelCap,
    FusionAndPixelSlot,
    TapAndPalette
}

// サブクエストを受け取ったとき、広告で生産枠を増やせる回数をどちらに足すか。
public enum QuestAdBonus
{
    None,
    Pixel,
    Character,
    Both
}

// メインクエストの報酬で開く機能。番号は報酬の RewardAmount と一致させる。
public static class MainFeature
{
    public const int PixelScreen = 1;
    public const int PixelTargets = 2;
    public const int CharacterScreen = 3;
    public const int Battle = 4;
    public const int BattleSpeed = 5;
    public const int FloorRepeat = 6;
    public const int Recovery = 7;
    public const int Author = 8;
    public const int Import = 9;
}

public sealed class QuestDef
{
    public readonly int Id;
    public readonly bool Shown;
    public readonly string Title;
    public readonly QuestMetric Metric;
    public readonly int Arg;
    public readonly long Target;
    public readonly QuestRewardKind Reward;
    public readonly long RewardAmount;
    public readonly bool Main;
    // 0 はレベル上限を変えない。あるときは、その値まで上限を上げる。他の報酬と一緒に渡せる。
    public readonly int LevelCap;
    // 0 は合成回数の上限を変えない。あるときは、その回数だけ上限を足す。クリアした分は重なる。
    public readonly int FusionCap;
    // 0 はアイテムを渡さない。あるときは、受け取りでその個数を足す。
    public readonly int ItemId;
    public readonly int ItemCount;

    public QuestDef(int id, string title, QuestMetric metric, int arg, long target, QuestRewardKind reward, long rewardAmount, bool shown, bool main = false, int levelCap = 0, int fusionCap = 0, int itemId = 0, int itemCount = 0)
    {
        Id = id;
        Shown = shown;
        Title = title;
        Metric = metric;
        Arg = arg;
        Target = target;
        Reward = reward;
        RewardAmount = rewardAmount;
        Main = main;
        LevelCap = levelCap;
        FusionCap = fusionCap;
        if (itemCount < 1 || itemId == 0)
        {
            ItemId = 0;
            ItemCount = 0;
        }
        else
        {
            ItemId = itemId;
            ItemCount = itemCount;
        }
    }
}

public sealed class QuestRecord
{
    public bool Cleared;
    public bool Claimed;
    public int Choice;
    public bool Locked;
}

public sealed class QuestSave
{
    public ulong TotalR;
    public ulong TotalG;
    public ulong TotalB;
    public ulong PixelCompletions;
    public ulong CharacterCompletions;
    public long GalleryClears;
    public ulong EnemyDefeats;
    public bool FusionUnlocked;
    public int FusionEfficiency;
    public int ImportCap = Constants.IMPORT_CHARACTER_CAP_INITIAL;
    // 0 か 1。1 は、次の受け取りで枠が1つ増える 0.5。
    public int ImportCapHalf;
    public bool DefaultsApplied;
    public bool HasTotalR;
    public bool HasTotalG;
    public bool HasTotalB;
    public bool HasPixel;
    public bool HasCharacter;
    public bool HasGallery;
    public bool HasEnemy;
    public bool HasFusionUnlocked;
    public bool HasFusionEfficiency;
    public bool HasImport;
    public bool HasImportCapHalf;
    public int LevelCap = BattleBalanceConfig.InitialLevelCap;
    public bool HasLevelCap;
    public int FusionCap = FusionBonus.InitialCap;
    public bool HasFusionCap;
    public readonly Dictionary<int, QuestRecord> Records = new Dictionary<int, QuestRecord>();

    public void Reset()
    {
        TotalR = 0;
        TotalG = 0;
        TotalB = 0;
        PixelCompletions = 0;
        CharacterCompletions = 0;
        GalleryClears = 0;
        EnemyDefeats = 0;
        FusionUnlocked = false;
        FusionEfficiency = 0;
        ImportCap = Constants.IMPORT_CHARACTER_CAP_INITIAL;
        ImportCapHalf = 0;
        DefaultsApplied = false;
        HasTotalR = false;
        HasTotalG = false;
        HasTotalB = false;
        HasPixel = false;
        HasCharacter = false;
        HasGallery = false;
        HasEnemy = false;
        HasFusionUnlocked = false;
        HasFusionEfficiency = false;
        HasImport = false;
        HasImportCapHalf = false;
        LevelCap = BattleBalanceConfig.InitialLevelCap;
        HasLevelCap = false;
        FusionCap = FusionBonus.InitialCap;
        HasFusionCap = false;
        Records.Clear();
    }

    public bool TryGet(int id, out QuestRecord record)
    {
        return Records.TryGetValue(id, out record);
    }

    public QuestRecord GetOrCreate(int id)
    {
        QuestRecord record;
        if (!Records.TryGetValue(id, out record))
        {
            record = new QuestRecord();
            Records[id] = record;
        }
        return record;
    }
}

public static class QuestCatalog
{
    public static readonly QuestDef[] All =
    {
        // メインクエスト。IDは5刻み。間の番号は、あとから挟むために空けてある。
        // 広告報酬はなし。
        // ピクセル生成の開放
        MainQuest(5, "スライムをRGB生産に配置し、R・G・Bをそれぞれ10,000以上所持する", QuestMetric.RgbHeld, 0, 10000, QuestRewardKind.UnlockFeature, MainFeature.PixelScreen),
        // ピクセル生成の対象枠の開放 
        MainQuest(10, "黒(0,0,0)を1,000個以上調色する", QuestMetric.BlackPixels, 0, 1000, QuestRewardKind.UnlockFeature, MainFeature.PixelTargets), 
        // キャラ生成の開放
        MainQuest(15, "赤・緑・青を各30個以上調色する", QuestMetric.PrimaryPixels, 0, 30, QuestRewardKind.UnlockFeature, MainFeature.CharacterScreen),
        // バトルの開放
        MainQuest(20, "R・G・Bミニスライムをそれぞれ1体以上作成する", QuestMetric.MiniSlimes, 0, 2, QuestRewardKind.UnlockFeature, MainFeature.Battle),
        // バトル倍速の開放
        MainQuest(25, "「チュートリアル」ステージの3階をクリアする", QuestMetric.StageFloor, 1, 3, QuestRewardKind.UnlockFeature, MainFeature.BattleSpeed),
        // 階層繰り返しの開放
        MainQuest(30, "黒猫を仲間にしながら、チュートリアルを10階クリアする", QuestMetric.StageFloor, 1, 10, QuestRewardKind.UnlockFeature, MainFeature.FloorRepeat),
        // 作者の開放
        MainQuest(35, "敵を100体倒す", QuestMetric.EnemyDefeats, 0, 100, QuestRewardKind.UnlockFeature, MainFeature.Author),
        // 合成機能開放
        MainQuest(40, "キャラを10体以上生成する", QuestMetric.CharacterCompletions, 0, 10, QuestRewardKind.FusionUnlock, 0),
        // スライムの森の開放、ピクセル生成枠+1、キャラ生成枠+1
        MainQuest(45, "「チュートリアル」ステージの100階をクリアする", QuestMetric.StageFloor, 1, 100, QuestRewardKind.BothSlots, 1),
        // チグサとバトル１の開放、キャラ読み込みの開放
        MainQuest(55, "「チュートリアル」ステージの200階をクリアする", QuestMetric.StageFloor, 1, 200, QuestRewardKind.UnlockFeature, MainFeature.Import),
        // 回復の開放
        MainQuest(65, "「チュートリアル」ステージの300階をクリアする", QuestMetric.StageFloor, 1, 300, QuestRewardKind.UnlockFeature, MainFeature.Recovery),

        // ステージクリア。100階、200階、300階。同じステージは100階をクリアすると次が出る。
        // ピクセル生成枠+1、キャラ生成枠+1。広告でピクセル生成+0.5、キャラ生成+0.3。
        // レベル上限と合成上限は、100階クリアごとに5上がる。この引数では付けない。
        // 次のステージ開放は100階だけ。スライムの森は出さない。
        Stage(5200, 2, 100),
        Stage(5220, 2, 200),
        Stage(5230, 2, 300),
        Stage(5300, 3, 100), // モアとバトル１の開放
        Stage(5320, 3, 200),
        Stage(5330, 3, 300),
        Stage(5400, 4, 100), // 次のステージの開放
        Stage(5420, 4, 200),
        Stage(5430, 4, 300),
        Stage(5500, 5, 100),
        Stage(5520, 5, 200),
        Stage(5530, 5, 300),

        // ギャラリークリア。
        // ピクセル生成枠+1、キャラ生成枠+1。広告でピクセル生成+0.5、キャラ生成+0.3。
        Gallery( 5219, 100),
        Gallery( 5429, 200),
        Gallery( 5639, 300),
        Gallery( 5849, 400),
        Gallery(6059, 500),

        // RGB生産。広告でピクセル生成+0.5。
        // Rはキャラ読込上限+0.5。
        Rgb( 5010, 0, "R",   250000),
        Rgb( 5020, 0, "R",   500000),
        Rgb( 5030, 0, "R",   750000),
        Rgb( 5040, 0, "R",  1000000),
        Rgb( 5050, 0, "R",  2500000),
        Rgb( 5060, 0, "R",  5000000),
        Rgb( 5070, 0, "R",  7500000),
        Rgb( 5080, 0, "R", 10000000),
        Rgb( 5090, 0, "R", 25000000),
        Rgb(5100, 0, "R", 50000000),

        // Gは生産できるピクセルをすべて、行の最後の数だけ。
        Rgb( 5011, 1, "G",   250000, QuestRewardKind.GrantPalette,  50),
        Rgb( 5021, 1, "G",   500000, QuestRewardKind.GrantPalette, 100),
        Rgb( 5031, 1, "G",   750000, QuestRewardKind.GrantPalette, 150),
        Rgb( 5041, 1, "G",  1000000, QuestRewardKind.GrantPalette, 200),
        Rgb( 5051, 1, "G",  2500000, QuestRewardKind.GrantPalette, 250),
        Rgb( 5061, 1, "G",  5000000, QuestRewardKind.GrantPalette, 300),
        Rgb( 5071, 1, "G",  7500000, QuestRewardKind.GrantPalette, 350),
        Rgb( 5081, 1, "G", 10000000, QuestRewardKind.GrantPalette, 400),
        Rgb( 5091, 1, "G", 25000000, QuestRewardKind.GrantPalette, 450),
        Rgb(5101, 1, "G", 50000000, QuestRewardKind.GrantPalette, 500),

        // BはR・G・Bをすべて、行の最後の数だけ。
        Rgb( 5012, 2, "B",   250000, QuestRewardKind.RgbAll,   5000),
        Rgb( 5022, 2, "B",   500000, QuestRewardKind.RgbAll,  10000),
        Rgb( 5032, 2, "B",   750000, QuestRewardKind.RgbAll,  20000),
        Rgb( 5042, 2, "B",  1000000, QuestRewardKind.RgbAll,  30000),
        Rgb( 5052, 2, "B",  2500000, QuestRewardKind.RgbAll,  40000),
        Rgb( 5062, 2, "B",  5000000, QuestRewardKind.RgbAll,  50000),
        Rgb( 5072, 2, "B",  7500000, QuestRewardKind.RgbAll,  60000),
        Rgb( 5082, 2, "B", 10000000, QuestRewardKind.RgbAll,  70000),
        Rgb( 5092, 2, "B", 25000000, QuestRewardKind.RgbAll,  80000),
        Rgb(5102, 2, "B", 50000000, QuestRewardKind.RgbAll,  90000),

        // ピクセル生成。R・G・Bをすべて、行の最後の数だけ。広告でピクセル生成+0.5。
        Pixel( 5013,   250,    5000),
        Pixel( 5023,   500,    5000),
        Pixel( 5033,   750,    5000),
        Pixel( 5043,  1000,    5000),
        Pixel( 5053,  1500,    5000),
        Pixel( 5063,  2000,    6000),
        Pixel( 5073,  2500,    7000),
        Pixel( 5083,  3000,    8000),
        Pixel( 5093,  3500,    9000),
        Pixel(5103,  4000,   10000),
        Pixel(5113,  4500,   10000),
        Pixel(5123,  4500,   10000),
        Pixel(5133,  5000,   10000),
        Pixel(5143,  6000,   10000),
        Pixel(5153,  7000,   10000),
        Pixel(5163,  8000,   10000),
        Pixel(5173,  9000,   10000),
        Pixel(5183, 10000,   10000),
        Pixel(5193, 12000,   10000),
        Pixel(5203, 14000,   10000),
        Pixel(5213, 16000,   10000),
        Pixel(5223, 18000,   10000),
        Pixel(5233, 20000,   10000),

        // キャラ生成。キャラ読込上限+0.5。第3引数がある行は縁結びのお守りをその数。広告でキャラ生成+0.3。
        Character(  5054,  100, 1),
        Character(  5074,  250),
        Character(  5094,  500),
        Character( 5114,  750),
        Character( 5134, 1000, 1),
        Character( 5154, 1250),
        Character( 5174, 1500),
        Character( 5194, 1750),
        Character( 5214, 2000),
        Character( 5234, 2250),
        Character( 5254, 2500),
        Character( 5274, 2750),
        Character( 5294, 3000),
        Character( 5314, 3500),
        Character( 5334, 4000),
        Character( 5354, 4500),
        Character( 5374, 5000, 1),
        Character( 5394, 6000),
        Character( 5414, 7000),
        Character( 5434, 8000),
        Character( 5454, 9000),
        Character( 5474, 10000, 1),
        Character( 5494, 100000, 1),

        // 敵を倒す。
        // 100体はメインクエスト。
        // 合成効率+1%。広告でキャラ生成+0.3。
        Enemy( 5025,   250, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 5035,   500, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 5045,   750, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 5055,  1000, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 5065,  1500, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 5075,  2000, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 5085,  3000, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 5095,  4000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5105,  5000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5115,  7500, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5125, 10000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5135, 12500, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5145, 15000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5155, 17500, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5165, 20000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5175, 25000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5185, 30000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5195, 35000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5205, 40000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5215, 45000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(5225, 50000, QuestRewardKind.FusionEfficiency, 1),

        // 合成。
        // ボタンタップ生成+10%、生産できるピクセルをすべて行の最後の数だけ。
        // 枠は増やさない。広告でキャラ生成+0.3。
        Fusion(  5016,  10,  50),
        Fusion(  5056,  25, 100),
        Fusion(  5086,  50, 150),
        Fusion( 5116,  75, 200),
        Fusion( 5146, 100, 250),
        Fusion( 5176, 150, 300),
        Fusion( 5206, 200, 350),
        Fusion( 5236, 250, 400),
        Fusion( 5266, 300, 450),
        Fusion( 5296, 350, 500),
        Fusion( 5326, 400, 550),
        Fusion( 5356, 450, 600),
        Fusion( 5386, 500, 650),
        // 500回の次は100回ごと。1000回の次は250回ごと。ピクセルは1件ずつ50増やす。
        // 番号は30刻み。5000回は6076。読込は10016と10046で、ここには重ならない。
        Fusion( 5416,  600,  700),
        Fusion( 5446,  700,  750),
        Fusion( 5476,  800,  800),
        Fusion( 5506,  900,  850),
        Fusion( 5536, 1000,  900),
        Fusion( 5566, 1250,  950),
        Fusion( 5596, 1500, 1000),
        Fusion( 5626, 1750, 1050),
        Fusion( 5656, 2000, 1100),
        Fusion( 5686, 2250, 1150),
        Fusion( 5716, 2500, 1200),
        Fusion( 5746, 2750, 1250),
        Fusion( 5776, 3000, 1300),
        Fusion( 5806, 3250, 1350),
        Fusion( 5836, 3500, 1400),
        Fusion( 5866, 3750, 1450),
        Fusion( 5896, 4000, 1500),
        Fusion( 5926, 4250, 1550),
        Fusion( 5956, 4500, 1600),
        Fusion( 5986, 4750, 1650),
        Fusion(6076, 5000, 1700),

        // ステージのキャラを仲間にする。
        // ピクセル生成枠+1かキャラ生成枠+1を選ぶ。広告でピクセル生成+0.5、キャラ生成+0.3。
        Companion( 5037,  6),
        Companion(5157,  8),
        Companion(5257, 10),
        Companion(5357, 15),
        Companion(5457, 20),
        Companion(5557, 25),

        // キャラを読込。
        // 合成効率+1%、ピクセル生成枠+1。広告でピクセル生成+0.5、キャラ生成+0.3。
        Import(10016,   8, 1),
        Import(10026,   8, 3),
        Import(10036,   8, 9),
        Import(10046,  16, 1),
        Import(10056,  16, 3),
        Import(10066,  16, 9),
        Import(10116,  32, 1),
        Import(10126,  32, 3),
        Import(10136,  32, 9),
        Import(10216,  64, 1),
        Import(10226,  64, 3),
        Import(10236,  64, 9),
        Import(10316, 128, 1),
        Import(10326, 128, 3),
        Import(10336, 128, 9),
    };

    public static QuestDef Find(int id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
                return All[i];
        }
        return null;
    }

    public static bool SameChain(QuestDef a, QuestDef b)
    {
        return a != null && b != null && !a.Main && !b.Main && a.Metric == b.Metric && a.Arg == b.Arg;
    }

    // 表示クエストの系統。メインは1本、それ以外は同じ指標と同じ対象。
    public static bool SameDisplayChain(QuestDef a, QuestDef b)
    {
        if (a == null || b == null)
            return false;
        if (a.Id == b.Id)
            return true;
        if (a.Main || b.Main)
            return a.Main && b.Main;
        return SameChain(a, b);
    }

    public static QuestDef PreviousMain(QuestDef def)
    {
        if (def == null || !def.Main)
            return null;
        QuestDef best = null;
        for (int i = 0; i < All.Length; i++)
        {
            QuestDef other = All[i];
            if (!other.Main || other.Id >= def.Id)
                continue;
            if (best == null || other.Id > best.Id)
                best = other;
        }
        return best;
    }

    public static int ChainCount(QuestDef def)
    {
        if (def == null)
            return 0;
        int count = 0;
        for (int i = 0; i < All.Length; i++)
        {
            if (SameChain(All[i], def))
                count++;
        }
        return count;
    }

    public static QuestDef PreviousInChain(QuestDef def)
    {
        if (def == null)
            return null;
        QuestDef best = null;
        int bestIndex = -1;
        int defIndex = IndexOf(def.Id);
        for (int i = 0; i < All.Length; i++)
        {
            QuestDef other = All[i];
            if (!SameChain(other, def) || other.Id == def.Id)
                continue;
            if (other.Target > def.Target)
                continue;
            if (other.Target == def.Target && i >= defIndex)
                continue;
            if (best == null || other.Target > best.Target || (other.Target == best.Target && i > bestIndex))
            {
                best = other;
                bestIndex = i;
            }
        }
        return best;
    }

    public static int IndexOf(int id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
                return i;
        }
        return All.Length;
    }

    public static string RewardText(QuestDef def, bool claimed, int choice)
    {
        string body;
        if (def.Reward == QuestRewardKind.ChooseSlots)
        {
            if (claimed && choice == 2)
                body = "キャラ生成枠+" + UiNumber.Format(def.RewardAmount);
            else if (claimed)
                body = "ピクセル生成枠+" + UiNumber.Format(def.RewardAmount);
            else
                body = "ピクセル生成枠+" + UiNumber.Format(def.RewardAmount) + " または キャラ生成枠+" + UiNumber.Format(def.RewardAmount);
        }
        else if (def.Reward == QuestRewardKind.ImportCap)
            body = "キャラ読込上限 +0.5";
        else if (def.Reward == QuestRewardKind.RgbAll)
            body = "R・G・B すべて +" + UiNumber.Format(def.RewardAmount);
        else if (def.Reward == QuestRewardKind.CharacterBundle)
            body = "キャラ読込上限 +0.5、ピクセル生成枠 +1、キャラ生成枠 +1";
        else if (def.Reward == QuestRewardKind.FusionUnlock)
            body = "合成機能開放";
        else if (def.Reward == QuestRewardKind.FusionEfficiency)
            body = "合成効率 +" + UiNumber.Format(def.RewardAmount) + "%";
        else if (def.Reward == QuestRewardKind.FusionAndPixelSlot)
            body = "合成効率 +" + UiNumber.Format(def.RewardAmount) + "%、ピクセル生成枠 +1";
        else if (def.Reward == QuestRewardKind.TapChooseSlot)
        {
            string slots;
            if (claimed && choice == 2)
                slots = "キャラ生成枠+" + UiNumber.Format(def.RewardAmount);
            else if (claimed)
                slots = "ピクセル生成枠+" + UiNumber.Format(def.RewardAmount);
            else
                slots = "ピクセル生成枠+" + UiNumber.Format(def.RewardAmount) + " または キャラ生成枠+" + UiNumber.Format(def.RewardAmount);
            body = "ボタンタップ生成 +10%\n" + slots;
        }
        else if (def.Reward == QuestRewardKind.TapAndPalette)
            body = "ボタンタップ生成 +10%\n生産できるピクセルをすべて" + UiNumber.Format(def.RewardAmount) + "個";
        else if (def.Reward == QuestRewardKind.UnlockFeature)
            body = FeatureUnlockText(def.RewardAmount);
        else if (def.Reward == QuestRewardKind.GrantPalette)
            body = "生産できるピクセルをすべて" + UiNumber.Format(def.RewardAmount) + "個";
        else if (def.Reward == QuestRewardKind.BothSlots)
            body = "ピクセル生成枠 +" + UiNumber.Format(def.RewardAmount) + "、キャラ生成枠 +" + UiNumber.Format(def.RewardAmount);
        else if (def.Reward == QuestRewardKind.LevelCap)
            body = "レベル上限 " + UiNumber.Format(def.RewardAmount);
        else
            body = "";
        if (def.LevelCap > 0 && def.Reward != QuestRewardKind.LevelCap)
        {
            string capText = "レベル上限 " + def.LevelCap.ToString();
            body = body.Length == 0 ? capText : body + "\n" + capText;
        }
        if (GrantsLevelCapStep(def))
        {
            string capText = "レベル上限 +" + BattleBalanceConfig.LevelCapPerHundredFloors.ToString();
            body = body.Length == 0 ? capText : body + "\n" + capText;
            string fusionText = "合成上限 +" + FusionBonus.CapPerHundredFloors.ToString() + "回";
            body = body + "\n" + fusionText;
        }
        if (def.FusionCap > 0)
        {
            string capText = "合成上限 +" + def.FusionCap.ToString() + "回";
            body = body.Length == 0 ? capText : body + "\n" + capText;
        }
        if (def.ItemCount > 0)
        {
            string itemText = ItemIds.DisplayName(def.ItemId) + " +" + UiNumber.Format(def.ItemCount);
            body = body.Length == 0 ? itemText : body + "\n" + itemText;
        }
        string unlock = StageUnlockText(def);
        if (unlock.Length > 0)
            body = unlock + "\n" + body;
        string ad = AdRewardText(def);
        if (ad.Length == 0)
            return body;
        if (body.Length == 0)
            return ad;
        return body + "\n" + ad;
    }

    public static QuestAdBonus AdBonus(QuestDef def)
    {
        if (def == null || def.Main)
            return QuestAdBonus.None;
        if (def.Metric == QuestMetric.RgbProduced || def.Metric == QuestMetric.PixelCompletions)
            return QuestAdBonus.Pixel;
        if (def.Metric == QuestMetric.CharacterCompletions
            || def.Metric == QuestMetric.EnemyDefeats
            || def.Metric == QuestMetric.FusionCount)
            return QuestAdBonus.Character;
        return QuestAdBonus.Both;
    }

    public static string AdRewardText(QuestDef def)
    {
        QuestAdBonus bonus = AdBonus(def);
        if (bonus == QuestAdBonus.Pixel)
            return "広告でピクセル生成を増やせる回数 +0.5";
        if (bonus == QuestAdBonus.Character)
            return "広告でキャラ生成を増やせる回数 +0.3";
        if (bonus == QuestAdBonus.Both)
            return "広告でピクセル生成を増やせる回数 +0.5、キャラ生成を増やせる回数 +0.3";
        return "";
    }

    public static string ChoiceLabel(QuestDef def, int choice, string warning)
    {
        string label = choice == 2
            ? "キャラ生成枠+" + UiNumber.Format(def.RewardAmount)
            : "ピクセル生成枠+" + UiNumber.Format(def.RewardAmount);
        if (!string.IsNullOrEmpty(warning))
            label += "\n" + warning;
        return label;
    }

    // 100階、200階、300階…のクリア。レベル上限が5上がる。
    public static bool GrantsLevelCapStep(QuestDef def)
    {
        if (def == null || def.Metric != QuestMetric.StageFloor)
            return false;
        if (def.Target < Constants.BATTLE_STAGE_FLOOR_MAX)
            return false;
        return def.Target % Constants.BATTLE_STAGE_FLOOR_MAX == 0;
    }

    static QuestDef Stage(int id, int stage, long floor, int levelCap = 0, int fusionCap = 0)
    {
        string name = ControllerBattleStageClass.GetStageName(stage);
        return new QuestDef(id, name + "を" + UiNumber.Format(floor) + "階クリア", QuestMetric.StageFloor, stage, floor, QuestRewardKind.BothSlots, 1, true, false, levelCap, fusionCap);
    }

    static string StageUnlockText(QuestDef def)
    {
        if (def == null || def.Metric != QuestMetric.StageFloor)
            return "";
        if (def.Arg == 1)
        {
            if (def.Target == Constants.BATTLE_STAGE_FLOOR_MAX)
                return "次のステージ「" + ControllerBattleStageClass.GetStageName(2) + "」の開放";
            if (def.Target == Constants.BATTLE_STAGE_FLOOR_MAX * 2)
                return "次のステージ「" + ControllerBattleStageClass.GetStageName(3) + "」の開放";
            return "";
        }
        if (def.Main)
            return "";
        // 次のステージは100階クリアで開く。200階と300階では重ねて出さない。
        if (def.Target != Constants.BATTLE_STAGE_FLOOR_MAX)
            return "";
        // チグサとバトル１はチュートリアル200階で開く。スライムの森では重ねて出さない。
        if (def.Arg == 2)
            return "";
        int next = def.Arg + 1;
        if (next < 1 || next > Constants.BATTLE_STAGE_NUM)
            return "";
        return "次のステージ「" + ControllerBattleStageClass.GetStageName(next) + "」の開放";
    }

    static QuestDef Gallery(int id, long floor)
    {
        return new QuestDef(id, "ギャラリーを" + UiNumber.Format(floor) + "階クリア", QuestMetric.StageFloor, Constants.BATTLE_STAGE_GALLERY, floor, QuestRewardKind.BothSlots, 1, true);
    }

    static QuestDef Rgb(int id, int channel, string name, long target)
    {
        return Rgb(id, channel, name, target, QuestRewardKind.ImportCap, 1);
    }

    static QuestDef Rgb(int id, int channel, string name, long target, QuestRewardKind reward, long amount)
    {
        return new QuestDef(id, name + "を" + UiNumber.Format(target) + "生産", QuestMetric.RgbProduced, channel, target, reward, amount, true);
    }

    static QuestDef Pixel(int id, long count, long rgb)
    {
        return new QuestDef(id, "ピクセル生成を" + UiNumber.Format(count) + "回完了", QuestMetric.PixelCompletions, 0, count, QuestRewardKind.RgbAll, rgb, true);
    }

    static QuestDef Character(int id, long count, int charmCount = 0)
    {
        int itemId = charmCount > 0 ? ItemIds.EnmusubiOmamori : 0;
        return new QuestDef(id, "キャラを" + UiNumber.Format(count) + "体生成", QuestMetric.CharacterCompletions, 0, count, QuestRewardKind.ImportCap, 1, true, false, 0, 0, itemId, charmCount);
    }

    static QuestDef Enemy(int id, long count, QuestRewardKind reward, long amount)
    {
        return new QuestDef(id, "敵を" + UiNumber.Format(count) + "体倒す", QuestMetric.EnemyDefeats, 0, count, reward, amount, true);
    }

    public static bool ChoosesProductionSlot(QuestDef def)
    {
        return def != null && (def.Reward == QuestRewardKind.ChooseSlots || def.Reward == QuestRewardKind.TapChooseSlot);
    }

    static QuestDef Fusion(int id, long count, long pixels)
    {
        return new QuestDef(id, "合成を" + UiNumber.Format(count) + "回する", QuestMetric.FusionCount, 0, count, QuestRewardKind.TapAndPalette, pixels, true);
    }

    static QuestDef Companion(int id, long count)
    {
        return new QuestDef(id, "ステージのキャラを" + UiNumber.Format(count) + "種仲間にする", QuestMetric.StageCompanions, 0, count, QuestRewardKind.ChooseSlots, 1, true);
    }

    static QuestDef Import(int id, int size, long count)
    {
        return new QuestDef(id, "1辺" + size.ToString() + "pxのキャラを" + UiNumber.Format(count) + "体読込", QuestMetric.ImportedOfSize, size, count, QuestRewardKind.FusionAndPixelSlot, 1, true);
    }

    static QuestDef MainQuest(int id, string title, QuestMetric metric, int arg, long target, QuestRewardKind reward, long amount)
    {
        return new QuestDef(id, title, metric, arg, target, reward, amount, true, true);
    }

    static string FeatureUnlockText(long feature)
    {
        if (feature == MainFeature.PixelScreen)
            return "ピクセル生成の開放";
        if (feature == MainFeature.PixelTargets)
            return "ピクセル生成の対象枠の開放";
        if (feature == MainFeature.CharacterScreen)
            return "キャラ生成の開放";
        if (feature == MainFeature.Battle)
            return "バトルの開放";
        if (feature == MainFeature.BattleSpeed)
            return "バトル倍速の開放";
        if (feature == MainFeature.FloorRepeat)
            return "階層繰り返しの開放";
        if (feature == MainFeature.Recovery)
            return "回復の開放";
        if (feature == MainFeature.Author)
            return "作者の開放";
        if (feature == MainFeature.Import)
            return "キャラ読み込みの開放";
        return "";
    }
}
