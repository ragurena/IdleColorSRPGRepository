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
    BothSlots
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

    public QuestDef(int id, string title, QuestMetric metric, int arg, long target, QuestRewardKind reward, long rewardAmount, bool shown, bool main = false)
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
        // メインクエスト
        MainQuest(1, "スライムをRGB生産に配置し、R・G・Bをそれぞれ10,000以上所持する", QuestMetric.RgbHeld, 0, 10000, QuestRewardKind.UnlockFeature, MainFeature.PixelScreen),
        MainQuest(2, "黒(0,0,0)を1,000個以上調色する", QuestMetric.BlackPixels, 0, 1000, QuestRewardKind.UnlockFeature, MainFeature.PixelTargets),
        MainQuest(3, "赤・緑・青を各30個以上調色する", QuestMetric.PrimaryPixels, 0, 30, QuestRewardKind.UnlockFeature, MainFeature.CharacterScreen),
        MainQuest(4, "R・G・Bミニスライムをそれぞれ1体以上作成する", QuestMetric.MiniSlimes, 0, 2, QuestRewardKind.UnlockFeature, MainFeature.Battle),
        MainQuest(5, "「チュートリアル」ステージの3階をクリアする", QuestMetric.StageFloor, 1, 3, QuestRewardKind.UnlockFeature, MainFeature.BattleSpeed),
        MainQuest(6, "黒猫を仲間にしながら、チュートリアルを10階クリアする", QuestMetric.StageFloor, 1, 10, QuestRewardKind.UnlockFeature, MainFeature.FloorRepeat),
        MainQuest(7, "キャラを10体以上生成する", QuestMetric.CharacterCompletions, 0, 10, QuestRewardKind.GrantPalette, 50),

        // ステージクリア
        Stage(100, 1, 100),
        Stage(200, 2, 100),
        Stage(300, 3, 100),
        Stage(400, 4, 100),
        Stage(500, 5, 100),

        // ギャラリークリア
        Gallery( 219, 100),
        Gallery( 429, 200),
        Gallery( 639, 300),
        Gallery( 849, 400),
        Gallery(1059, 500),

        // RGB生産
        Rgb( 10, 0, "R",   250000),
        Rgb( 20, 0, "R",   500000),
        Rgb( 30, 0, "R",   750000),
        Rgb( 40, 0, "R",  1000000),
        Rgb( 50, 0, "R",  2500000),
        Rgb( 60, 0, "R",  5000000),
        Rgb( 70, 0, "R",  7500000),
        Rgb( 80, 0, "R", 10000000),
        Rgb( 90, 0, "R", 25000000),
        Rgb(100, 0, "R", 50000000),

        Rgb( 11, 1, "G",   250000, QuestRewardKind.GrantPalette,  50),
        Rgb( 21, 1, "G",   500000, QuestRewardKind.GrantPalette, 100),
        Rgb( 31, 1, "G",   750000, QuestRewardKind.GrantPalette, 150),
        Rgb( 41, 1, "G",  1000000, QuestRewardKind.GrantPalette, 200),
        Rgb( 51, 1, "G",  2500000, QuestRewardKind.GrantPalette, 250),
        Rgb( 61, 1, "G",  5000000, QuestRewardKind.GrantPalette, 300),
        Rgb( 71, 1, "G",  7500000, QuestRewardKind.GrantPalette, 350),
        Rgb( 81, 1, "G", 10000000, QuestRewardKind.GrantPalette, 400),
        Rgb( 91, 1, "G", 25000000, QuestRewardKind.GrantPalette, 450),
        Rgb(101, 1, "G", 50000000, QuestRewardKind.GrantPalette, 500),

        Rgb( 12, 2, "B",   250000, QuestRewardKind.RgbAll,   5000),
        Rgb( 22, 2, "B",   500000, QuestRewardKind.RgbAll,  10000),
        Rgb( 32, 2, "B",   750000, QuestRewardKind.RgbAll,  20000),
        Rgb( 42, 2, "B",  1000000, QuestRewardKind.RgbAll,  30000),
        Rgb( 52, 2, "B",  2500000, QuestRewardKind.RgbAll,  40000),
        Rgb( 62, 2, "B",  5000000, QuestRewardKind.RgbAll,  50000),
        Rgb( 72, 2, "B",  7500000, QuestRewardKind.RgbAll,  60000),
        Rgb( 82, 2, "B", 10000000, QuestRewardKind.RgbAll,  70000),
        Rgb( 92, 2, "B", 25000000, QuestRewardKind.RgbAll,  80000),
        Rgb(102, 2, "B", 50000000, QuestRewardKind.RgbAll,  90000),

        // ピクセル生成
        Pixel( 13,   100,    1000),
        Pixel( 23,   250,    2000),
        Pixel( 33,   500,    4000),
        Pixel( 43,   750,    8000),
        Pixel( 53,  1000,   16000),
        Pixel( 63,  1500,   32000),
        Pixel( 73,  2000,   64000),
        Pixel( 83,  2500,  128000),
        Pixel( 93,  3000,  256000),
        Pixel(103,  3500,  512000),
        Pixel(113,  4000, 1000000),
        Pixel(123,  4500, 1000000),
        Pixel(133,  5000, 1000000),
        Pixel(143,  6000, 1000000),
        Pixel(153,  7000, 1000000),
        Pixel(163,  8000, 1000000),
        Pixel(173,  9000, 1000000),
        Pixel(183, 10000, 1000000),
        Pixel(193, 11000, 1000000),

        // キャラ生成
        Character(  14,  100),
        Character(  34,  250),
        Character(  54,  500),
        Character(  74,  750),
        Character(  94, 1000),
        Character( 114, 1250),
        Character( 134, 1500),
        Character( 154, 1750),
        Character( 174, 2000),
        Character( 194, 2250),
        Character( 214, 2500),
        Character( 234, 2750),
        Character( 254, 3000),

        // 敵を倒す
        Enemy( 15,   100, QuestRewardKind.FusionUnlock, 0),
        Enemy( 25,   250, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 35,   500, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 45,   750, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 55,  1000, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 65,  1500, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 75,  2000, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 85,  3000, QuestRewardKind.FusionEfficiency, 1),
        Enemy( 95,  4000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(105,  5000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(115,  7500, QuestRewardKind.FusionEfficiency, 1),
        Enemy(125, 10000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(135, 12500, QuestRewardKind.FusionEfficiency, 1),
        Enemy(145, 15000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(155, 17500, QuestRewardKind.FusionEfficiency, 1),
        Enemy(165, 20000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(175, 25000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(185, 30000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(195, 35000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(205, 40000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(215, 45000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(225, 50000, QuestRewardKind.FusionEfficiency, 1),

        // 合成
        Fusion(  16,  10),
        Fusion(  56,  25),
        Fusion(  86,  50),
        Fusion( 116,  75),
        Fusion( 146, 100),
        Fusion( 176, 150),
        Fusion( 206, 200),
        Fusion( 236, 250),
        Fusion( 266, 300),
        Fusion( 296, 350),
        Fusion( 326, 400),
        Fusion( 356, 450),
        Fusion( 386, 500),

        // ステージのキャラを仲間にする
        Companion( 37,  6),
        Companion(157,  8),
        Companion(257, 10),
        Companion(357, 15),
        Companion(457, 20),
        Companion(557, 25),

        // キャラを読込
        Import(1016,   8, 1),
        Import(1026,   8, 3),
        Import(1036,   8, 9),
        Import(1046,  16, 1),
        Import(1056,  16, 3),
        Import(1066,  16, 9),
        Import(1116,  32, 1),
        Import(1126,  32, 3),
        Import(1136,  32, 9),
        Import(1216,  64, 1),
        Import(1226,  64, 3),
        Import(1236,  64, 9),
        Import(1316, 128, 1),
        Import(1326, 128, 3),
        Import(1336, 128, 9),
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
            body = "キャラ読込上限 +1";
        else if (def.Reward == QuestRewardKind.RgbAll)
            body = "R・G・B すべて +" + UiNumber.Format(def.RewardAmount);
        else if (def.Reward == QuestRewardKind.CharacterBundle)
            body = "キャラ読込上限 +1、ピクセル生成枠 +1、キャラ生成枠 +1";
        else if (def.Reward == QuestRewardKind.FusionUnlock)
            body = "合成機能開放";
        else if (def.Reward == QuestRewardKind.FusionEfficiency)
            body = "合成効率 +" + UiNumber.Format(def.RewardAmount) + "%";
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
        else if (def.Reward == QuestRewardKind.UnlockFeature)
            body = FeatureUnlockText(def.RewardAmount);
        else if (def.Reward == QuestRewardKind.GrantPalette)
            body = "生産できるピクセルをすべて" + UiNumber.Format(def.RewardAmount) + "個";
        else if (def.Reward == QuestRewardKind.BothSlots)
            body = "ピクセル生成枠 +" + UiNumber.Format(def.RewardAmount) + "、キャラ生成枠 +" + UiNumber.Format(def.RewardAmount);
        else
            body = "";
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
            return "広告でピクセル生成を増やせる回数 +1";
        if (bonus == QuestAdBonus.Character)
            return "広告でキャラ生成を増やせる回数 +1";
        if (bonus == QuestAdBonus.Both)
            return "広告でピクセル生成とキャラ生成を増やせる回数 +1";
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

    static QuestDef Stage(int id, int stage, long floor)
    {
        string name = ControllerBattleStageClass.GetStageName(stage);
        return new QuestDef(id, name + "を" + UiNumber.Format(floor) + "階クリア", QuestMetric.StageFloor, stage, floor, QuestRewardKind.BothSlots, 1, true);
    }

    static string StageUnlockText(QuestDef def)
    {
        if (def == null || def.Main || def.Metric != QuestMetric.StageFloor)
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

    static QuestDef Character(int id, long count)
    {
        return new QuestDef(id, "キャラを" + UiNumber.Format(count) + "体生成", QuestMetric.CharacterCompletions, 0, count, QuestRewardKind.CharacterBundle, 1, true);
    }

    static QuestDef Enemy(int id, long count, QuestRewardKind reward, long amount)
    {
        return new QuestDef(id, "敵を" + UiNumber.Format(count) + "体倒す", QuestMetric.EnemyDefeats, 0, count, reward, amount, true);
    }

    public static bool ChoosesProductionSlot(QuestDef def)
    {
        return def != null && (def.Reward == QuestRewardKind.ChooseSlots || def.Reward == QuestRewardKind.TapChooseSlot);
    }

    static QuestDef Fusion(int id, long count)
    {
        return new QuestDef(id, "合成を" + UiNumber.Format(count) + "回する", QuestMetric.FusionCount, 0, count, QuestRewardKind.TapChooseSlot, 1, true);
    }

    static QuestDef Companion(int id, long count)
    {
        return new QuestDef(id, "ステージのキャラを" + UiNumber.Format(count) + "種仲間にする", QuestMetric.StageCompanions, 0, count, QuestRewardKind.ChooseSlots, 1, true);
    }

    static QuestDef Import(int id, int size, long count)
    {
        return new QuestDef(id, "1辺" + size.ToString() + "pxのキャラを" + UiNumber.Format(count) + "体読込", QuestMetric.ImportedOfSize, size, count, QuestRewardKind.FusionEfficiency, 1, true);
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
        return "";
    }
}
