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
    ImportedOfSize
}

public enum QuestRewardKind
{
    ChooseSlots,
    ImportCap,
    RgbAll,
    CharacterBundle,
    FusionUnlock,
    FusionEfficiency
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

    public QuestDef(int id, string title, QuestMetric metric, int arg, long target, QuestRewardKind reward, long rewardAmount, bool shown)
    {
        Id = id;
        Shown = shown;
        Title = title;
        Metric = metric;
        Arg = arg;
        Target = target;
        Reward = reward;
        RewardAmount = rewardAmount;
    }
}

public sealed class QuestRecord
{
    public bool Cleared;
    public bool Claimed;
    public int Choice;
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
        Stage(100, 1, 100),
        Stage(200, 2, 100),
        Stage(300, 3, 100),
        Stage(400, 4, 100),
        Stage(500, 5, 100),

        Gallery(110, 100),
        Gallery(120, 200),
        Gallery(130, 300),
        Gallery(140, 400),
        Gallery(150, 500),

        Rgb(10, 0, "R", 10000),
        Rgb(20, 0, "R", 100000),
        Rgb(30, 0, "R", 1000000),
        Rgb(40, 0, "R", 10000000),
        Rgb(50, 0, "R", 100000000),
        Rgb(11, 1, "G", 10000),
        Rgb(21, 1, "G", 100000),
        Rgb(31, 1, "G", 1000000),
        Rgb(41, 1, "G", 10000000),
        Rgb(51, 1, "G", 100000000),
        Rgb(12, 2, "B", 10000),
        Rgb(22, 2, "B", 100000),
        Rgb(32, 2, "B", 1000000),
        Rgb(42, 2, "B", 10000000),
        Rgb(52, 2, "B", 100000000),

        Pixel(13, 50, 1000),
        Pixel(23, 100, 2000),
        Pixel(33, 500, 4000),
        Pixel(43, 1000, 8000),
        Pixel(53, 5000, 16000),
        Pixel(63, 10000, 32000),
        Pixel(73, 50000, 64000),
        Pixel(83, 100000, 128000),

        Character(14, 10),
        Character(24, 50),
        Character(34, 100),
        Character(44, 200),
        Character(54, 300),
        Character(64, 400),
        Character(74, 500),
        Character(84, 600),
        Character(94, 700),
        Character(104, 800),
        Character(114, 900),
        Character(124, 1000),

        Enemy(15, 50, QuestRewardKind.FusionUnlock, 0),
        Enemy(25, 100, QuestRewardKind.FusionEfficiency, 1),
        Enemy(35, 250, QuestRewardKind.FusionEfficiency, 1),
        Enemy(45, 500, QuestRewardKind.FusionEfficiency, 1),
        Enemy(55, 1000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(65, 1500, QuestRewardKind.FusionEfficiency, 1),
        Enemy(75, 2000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(85, 3000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(95, 4000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(105, 5000, QuestRewardKind.FusionEfficiency, 1),

        Import(16, 8, 1),
        Import(26, 8, 3),
        Import(36, 8, 9),
        Import(46, 16, 1),
        Import(56, 16, 3),
        Import(66, 16, 9),
        Import(116, 32, 1),
        Import(126, 32, 3),
        Import(136, 32, 9),
        Import(216, 64, 1),
        Import(226, 64, 3),
        Import(236, 64, 9),
        Import(316, 128, 1),
        Import(326, 128, 3),
        Import(336, 128, 9),
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
        return a != null && b != null && a.Metric == b.Metric && a.Arg == b.Arg;
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
                body = "キャラ生成枠+" + def.RewardAmount.ToString();
            else if (claimed)
                body = "ピクセル生成枠+" + def.RewardAmount.ToString();
            else
                body = "ピクセル生成枠+" + def.RewardAmount.ToString() + " または キャラ生成枠+" + def.RewardAmount.ToString();
        }
        else if (def.Reward == QuestRewardKind.ImportCap)
            body = "キャラ読込上限 +1";
        else if (def.Reward == QuestRewardKind.RgbAll)
            body = "R・G・B すべて +" + def.RewardAmount.ToString();
        else if (def.Reward == QuestRewardKind.CharacterBundle)
            body = "キャラ読込上限 +1、ピクセル生成枠 +1、キャラ生成枠 +1";
        else if (def.Reward == QuestRewardKind.FusionUnlock)
            body = "合成機能開放";
        else if (def.Reward == QuestRewardKind.FusionEfficiency)
            body = "合成効率 +" + def.RewardAmount.ToString() + "%";
        else
            body = "";
        string unlock = StageUnlockText(def);
        if (unlock.Length > 0)
            body = unlock + "\n" + body;
        return body + "\n広告でピクセル生成とキャラ生成を増やせる回数 +1";
    }

    public static string ChoiceLabel(QuestDef def, int choice, string warning)
    {
        string label = choice == 2
            ? "キャラ生成枠+" + def.RewardAmount.ToString()
            : "ピクセル生成枠+" + def.RewardAmount.ToString();
        if (!string.IsNullOrEmpty(warning))
            label += "\n" + warning;
        return label;
    }

    static QuestDef Stage(int id, int stage, long floor)
    {
        string name = ControllerBattleStageClass.GetStageName(stage);
        return new QuestDef(id, name + "を" + floor.ToString() + "階クリア", QuestMetric.StageFloor, stage, floor, QuestRewardKind.ChooseSlots, Constants.QUEST_PRODUCTION_SLOT_REWARD, true);
    }

    static string StageUnlockText(QuestDef def)
    {
        if (def == null || def.Metric != QuestMetric.StageFloor)
            return "";
        int next = def.Arg + 1;
        if (next < 1 || next > Constants.BATTLE_STAGE_NUM)
            return "";
        return "次のステージ「" + ControllerBattleStageClass.GetStageName(next) + "」の開放";
    }

    static QuestDef Gallery(int id, long count)
    {
        return new QuestDef(id, "ギャラリーを" + count.ToString() + "回クリア", QuestMetric.GalleryClears, Constants.BATTLE_STAGE_GALLERY, count, QuestRewardKind.ChooseSlots, Constants.QUEST_PRODUCTION_SLOT_REWARD, true);
    }

    static QuestDef Rgb(int id, int channel, string name, long target)
    {
        return new QuestDef(id, name + "を" + target.ToString() + "生産", QuestMetric.RgbProduced, channel, target, QuestRewardKind.ImportCap, 1, true);
    }

    static QuestDef Pixel(int id, long count, long rgb)
    {
        return new QuestDef(id, "ピクセル生成を" + count.ToString() + "回完了", QuestMetric.PixelCompletions, 0, count, QuestRewardKind.RgbAll, rgb, true);
    }

    static QuestDef Character(int id, long count)
    {
        return new QuestDef(id, "キャラを" + count.ToString() + "体生成", QuestMetric.CharacterCompletions, 0, count, QuestRewardKind.CharacterBundle, 1, true);
    }

    static QuestDef Enemy(int id, long count, QuestRewardKind reward, long amount)
    {
        return new QuestDef(id, "敵を" + count.ToString() + "体倒す", QuestMetric.EnemyDefeats, 0, count, reward, amount, true);
    }

    static QuestDef Import(int id, int size, long count)
    {
        return new QuestDef(id, "1辺" + size.ToString() + "pxのキャラを" + count.ToString() + "体読込", QuestMetric.ImportedOfSize, size, count, QuestRewardKind.FusionEfficiency, 1, true);
    }
}
