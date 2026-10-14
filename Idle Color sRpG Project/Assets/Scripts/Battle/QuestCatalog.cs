using System.Collections.Generic;

// クエストの定義。増やすときは All に1行足す。番号は10飛ばしで、一度決めた番号は変えない。
public enum QuestMetric
{
    StageFloor,
    GalleryClears,
    RgbProduced,
    PixelCompletions,
    CharacterCompletions,
    EnemyDefeats
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
        Stage(10, 1, "草原", 100),
        Stage(20, 2, "森", 100),
        Stage(30, 3, "洞窟", 100),
        Stage(40, 4, "遺跡", 100),
        Stage(50, 5, "城", 100),

        Gallery(110, 100),
        Gallery(120, 200),
        Gallery(130, 300),
        Gallery(140, 400),
        Gallery(150, 500),

        Rgb(210, 0, "R", 10000),
        Rgb(220, 0, "R", 100000),
        Rgb(230, 0, "R", 1000000),
        Rgb(310, 1, "G", 10000),
        Rgb(320, 1, "G", 100000),
        Rgb(330, 1, "G", 1000000),
        Rgb(410, 2, "B", 10000),
        Rgb(420, 2, "B", 100000),
        Rgb(430, 2, "B", 1000000),

        Pixel(510, 10, 1000),
        Pixel(520, 100, 2000),
        Pixel(530, 1000, 4000),

        Character(610, 10),
        Character(620, 20),
        Character(630, 30),

        Enemy(710, 50, QuestRewardKind.FusionUnlock, 0),
        Enemy(720, 100, QuestRewardKind.FusionEfficiency, 1),
        Enemy(730, 500, QuestRewardKind.FusionEfficiency, 1),
        Enemy(740, 1000, QuestRewardKind.FusionEfficiency, 1),
        Enemy(750, 5000, QuestRewardKind.FusionEfficiency, 1),
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

    public static int CurrentId(int savedId)
    {
        switch (savedId)
        {
            case 1: return 10;
            case 2: return 20;
            case 3: return 30;
            case 4: return 40;
            case 5: return 50;
            case 11: return 110;
            case 12: return 120;
            case 13: return 130;
            case 14: return 140;
            case 15: return 150;
            case 21: return 210;
            case 22: return 220;
            case 23: return 230;
            case 24: return 310;
            case 25: return 320;
            case 26: return 330;
            case 27: return 410;
            case 28: return 420;
            case 29: return 430;
            case 31: return 510;
            case 32: return 520;
            case 33: return 530;
            case 41: return 610;
            case 42: return 620;
            case 43: return 630;
            default: return savedId;
        }
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
                body = "キャラ枠+" + def.RewardAmount.ToString();
            else if (claimed)
                body = "ピクセル枠+" + def.RewardAmount.ToString();
            else
                body = "ピクセル枠+" + def.RewardAmount.ToString() + " または キャラ枠+" + def.RewardAmount.ToString();
        }
        else if (def.Reward == QuestRewardKind.ImportCap)
            body = "読み込み上限 +1";
        else if (def.Reward == QuestRewardKind.RgbAll)
            body = "R・G・B すべて +" + def.RewardAmount.ToString();
        else if (def.Reward == QuestRewardKind.CharacterBundle)
            body = "読み込み上限 +1、ピクセル枠 +1、キャラ枠 +1";
        else if (def.Reward == QuestRewardKind.FusionUnlock)
            body = "合成機能開放";
        else if (def.Reward == QuestRewardKind.FusionEfficiency)
            body = "合成効率 +" + def.RewardAmount.ToString() + "%";
        else
            body = "";
        return body + "\n広告で増やせる回数 +1";
    }

    public static string ChoiceLabel(QuestDef def, int choice, string warning)
    {
        string label = choice == 2
            ? "キャラ枠+" + def.RewardAmount.ToString()
            : "ピクセル枠+" + def.RewardAmount.ToString();
        if (!string.IsNullOrEmpty(warning))
            label += "\n" + warning;
        return label;
    }

    static QuestDef Stage(int id, int stage, string name, long floor)
    {
        return new QuestDef(id, name + "を" + floor.ToString() + "階クリア", QuestMetric.StageFloor, stage, floor, QuestRewardKind.ChooseSlots, Constants.QUEST_PRODUCTION_SLOT_REWARD, true);
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
}
