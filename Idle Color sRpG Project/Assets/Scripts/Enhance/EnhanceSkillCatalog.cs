using System;

// ピクあ！アカデミースキル。数値と式はここに集約し、あとから調整する。
public enum EnhanceSkillId
{
    RgbTapBonus = 0,
    AutoR = 1,
    AutoG = 2,
    AutoB = 3,
    PixelSpeedTapBonus = 4,
    PixelGenBonus = 5,
    CharTapBonus = 6,
    CharChanceBonus = 7,
    HpBonus = 8,
    AtkBonus = 9,
    DefBonus = 10
}

public enum EnhanceCostKind
{
    Linear = 0,
    Pow11 = 1,
    Pow12 = 2,
    Pow15 = 3,
    Pow2 = 4
}

public enum EnhanceCapKind
{
    LevelCap = 0,
    LevelCapTimes5 = 1,
    LevelCapDiv5Min1 = 2,
    Fixed50 = 3
}

public enum EnhanceIconKind
{
    Rgb = 0,
    ChipR = 1,
    ChipG = 2,
    ChipB = 3,
    Pixel = 4,
    Character = 5,
    Battle = 6
}

public sealed class EnhanceSkillDef
{
    public EnhanceSkillId Id;
    public string Name;
    public string Description;
    public int UnlockStars;
    public long CostBase;
    public EnhanceCostKind CostKind;
    public EnhanceCapKind CapKind;
    public EnhanceIconKind Icon;
}

public static class EnhanceSkillCatalog
{
    public const int Count = 11;
    public const int WhiteR = 255;
    public const int WhiteG = 255;
    public const int WhiteB = 255;

    static readonly EnhanceSkillDef[] Skills =
    {
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.RgbTapBonus,
            Name = "R・G・Bタップ生産ボーナス",
            Description = "各RGBのタップ生産量をタップごとに＋レベル",
            UnlockStars = 2,
            CostBase = 10,
            CostKind = EnhanceCostKind.Linear,
            CapKind = EnhanceCapKind.LevelCapTimes5,
            Icon = EnhanceIconKind.Rgb
        },
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.AutoR,
            Name = "R自動生産",
            Description = "赤ピクセルを毎秒レベル個、自動生産する",
            UnlockStars = 3,
            CostBase = 100,
            CostKind = EnhanceCostKind.Pow12,
            CapKind = EnhanceCapKind.LevelCap,
            Icon = EnhanceIconKind.ChipR
        },
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.AutoG,
            Name = "G自動生産",
            Description = "緑ピクセルを毎秒レベル個、自動生産する",
            UnlockStars = 3,
            CostBase = 100,
            CostKind = EnhanceCostKind.Pow12,
            CapKind = EnhanceCapKind.LevelCap,
            Icon = EnhanceIconKind.ChipG
        },
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.AutoB,
            Name = "B自動生産",
            Description = "青ピクセルを毎秒レベル個、自動生産する",
            UnlockStars = 3,
            CostBase = 100,
            CostKind = EnhanceCostKind.Pow12,
            CapKind = EnhanceCapKind.LevelCap,
            Icon = EnhanceIconKind.ChipB
        },
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.PixelSpeedTapBonus,
            Name = "ピクセル生成タップボーナス",
            Description = "スピードアップの累計タップ×レベルを、生成完了時に追加する",
            UnlockStars = 3,
            CostBase = 500,
            CostKind = EnhanceCostKind.Pow11,
            CapKind = EnhanceCapKind.LevelCapDiv5Min1,
            Icon = EnhanceIconKind.Pixel
        },
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.PixelGenBonus,
            Name = "ピクセル生成ボーナス",
            Description = "ピクセル生成完了ごとにレベル個を追加する",
            UnlockStars = 4,
            CostBase = 1000,
            CostKind = EnhanceCostKind.Pow12,
            CapKind = EnhanceCapKind.LevelCap,
            Icon = EnhanceIconKind.Pixel
        },
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.CharTapBonus,
            Name = "キャラ生成タップボーナス",
            Description = "キャラ生成のタップ進行を、レベル分だけ追加で進める",
            UnlockStars = 4,
            CostBase = 1000,
            CostKind = EnhanceCostKind.Pow2,
            CapKind = EnhanceCapKind.LevelCapDiv5Min1,
            Icon = EnhanceIconKind.Character
        },
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.CharChanceBonus,
            Name = "キャラ生成確率ボーナス",
            Description = "レベル%の確率で、×0.5の所持を追加生成する",
            UnlockStars = 5,
            CostBase = 10000,
            CostKind = EnhanceCostKind.Pow2,
            CapKind = EnhanceCapKind.Fixed50,
            Icon = EnhanceIconKind.Character
        },
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.HpBonus,
            Name = "キャラHP強化",
            Description = "味方全体のHPを＋(レベル×10)",
            UnlockStars = 5,
            CostBase = 2000,
            CostKind = EnhanceCostKind.Pow15,
            CapKind = EnhanceCapKind.LevelCap,
            Icon = EnhanceIconKind.Battle
        },
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.AtkBonus,
            Name = "キャラATK強化",
            Description = "味方全体のATKを＋レベル",
            UnlockStars = 6,
            CostBase = 3000,
            CostKind = EnhanceCostKind.Pow2,
            CapKind = EnhanceCapKind.LevelCap,
            Icon = EnhanceIconKind.Battle
        },
        new EnhanceSkillDef
        {
            Id = EnhanceSkillId.DefBonus,
            Name = "キャラDEF強化",
            Description = "味方全体のDEFを＋レベル",
            UnlockStars = 7,
            CostBase = 5000,
            CostKind = EnhanceCostKind.Pow2,
            CapKind = EnhanceCapKind.LevelCap,
            Icon = EnhanceIconKind.Battle
        }
    };

    public static EnhanceSkillDef Get(EnhanceSkillId id)
    {
        int index = (int)id;
        if (index < 0 || index >= Skills.Length)
            return null;
        return Skills[index];
    }

    public static EnhanceSkillDef GetAt(int index)
    {
        if (index < 0 || index >= Skills.Length)
            return null;
        return Skills[index];
    }

    public static int LevelCap(EnhanceCapKind kind, int characterLevelCap)
    {
        if (characterLevelCap < 1)
            characterLevelCap = 1;
        switch (kind)
        {
            case EnhanceCapKind.LevelCapTimes5:
                return characterLevelCap * 5;
            case EnhanceCapKind.LevelCapDiv5Min1:
                {
                    int cap = characterLevelCap / 5;
                    return cap < 1 ? 1 : cap;
                }
            case EnhanceCapKind.Fixed50:
                return 50;
            default:
                return characterLevelCap;
        }
    }

    // n は次のレベル。端数は切り上げ。範囲外は long.MaxValue。
    public static long UpgradeCost(EnhanceSkillDef def, int nextLevel)
    {
        if (def == null || nextLevel < 1)
            return 0;
        return CeilCost(def.CostBase, nextLevel, def.CostKind);
    }

    public static long EffectAmount(EnhanceSkillId id, int level)
    {
        if (level < 1)
            return 0;
        if (id == EnhanceSkillId.HpBonus)
            return (long)level * 10L;
        return level;
    }

    public static string EffectText(EnhanceSkillId id, int level)
    {
        long amount = EffectAmount(id, level);
        switch (id)
        {
            case EnhanceSkillId.RgbTapBonus:
                return "タップ +" + amount.ToString();
            case EnhanceSkillId.AutoR:
            case EnhanceSkillId.AutoG:
            case EnhanceSkillId.AutoB:
                return "毎秒 +" + amount.ToString();
            case EnhanceSkillId.PixelSpeedTapBonus:
                return "完了時 タップ数×" + amount.ToString();
            case EnhanceSkillId.PixelGenBonus:
                return "完了時 +" + amount.ToString();
            case EnhanceSkillId.CharTapBonus:
                return "タップ +" + amount.ToString() + "px";
            case EnhanceSkillId.CharChanceBonus:
                return amount.ToString() + "% で×0.5追加";
            case EnhanceSkillId.HpBonus:
                return "HP +" + amount.ToString();
            case EnhanceSkillId.AtkBonus:
                return "ATK +" + amount.ToString();
            case EnhanceSkillId.DefBonus:
                return "DEF +" + amount.ToString();
            default:
                return "+" + amount.ToString();
        }
    }

    static long CeilCost(long costBase, int n, EnhanceCostKind kind)
    {
        if (costBase < 1 || n < 1)
            return 0;
        double factor;
        switch (kind)
        {
            case EnhanceCostKind.Linear:
                factor = n;
                break;
            case EnhanceCostKind.Pow11:
                factor = Math.Pow(n, 1.1);
                break;
            case EnhanceCostKind.Pow12:
                factor = Math.Pow(n, 1.2);
                break;
            case EnhanceCostKind.Pow15:
                factor = Math.Pow(n, 1.5);
                break;
            case EnhanceCostKind.Pow2:
                factor = (double)n * n;
                break;
            default:
                factor = n;
                break;
        }
        if (double.IsNaN(factor) || double.IsInfinity(factor) || factor <= 0d)
            return long.MaxValue;
        double raw = costBase * factor;
        if (double.IsNaN(raw) || double.IsInfinity(raw) || raw >= (double)long.MaxValue)
            return long.MaxValue;
        long floored = (long)raw;
        if (raw > floored)
        {
            if (floored == long.MaxValue)
                return long.MaxValue;
            return floored + 1;
        }
        return floored < 1 ? 1 : floored;
    }
}
