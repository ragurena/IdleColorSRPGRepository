using System;

//バトルの倍率・上限・成長率・確率。戦闘処理はここの値だけを参照する。
//確率は 0〜1。画面で%にするときだけ 100 倍する。
public class BattleBalanceConfig
{
    public int FormationSize = 3;
    public int MaxUnits = 9;

    public int MinActions = 1;
    public int MaxActions = 3;

    public long MinimumDamage = 1;

    // (OBS * 不透明ピクセル * 属性値) / RgbDivisor を切り上げ
    public double RgbDivisor = 10.0;

    // 経験値 = 切り上げ(不透明ピクセル * 属性値 * ExpMultiplier)
    public double ExpMultiplier = 1.0;

    // 次のレベルまでに必要な経験値。既存データに曲線が無いので、ここが調整点。
    public long ExpBase = 2000;
    public long ExpPerLevel = 1000;
    public long MaxLevel = 9999;
    public int MaxLevelsPerExpGrant = 30;

    public double HpGrowthRate = 0.10;
    public double AtkGrowthRate = 0.05;
    public double DefGrowthRate = 0.05;
    public double SpdGrowthRate = 0.01;

    // LUC * ExpOrbRatePerLuc。上限 ExpOrbRateCap
    public double ExpOrbRatePerLuc = 0.05;
    public double ExpOrbRateCap = 0.50;

    // (LUC + OBS) * RecruitRatePerPoint。上限 RecruitRateCap
    // (LUC + OBS) / 5 % は (LUC + OBS) * 0.002
    public double RecruitRatePerPoint = 0.002;
    public double RecruitRateCap = 0.05;

    public const int MaxOwnedCount = 255;
    public int MaxLives = MaxOwnedCount;
    public int EnemyInitialLives = 0;

    // 攻撃側の代表カラーが、受ける側の弱点カラーと一致したときの攻撃力倍率
    public double WeaknessAttackMultiplier = 3.0;
    // 攻撃側のタイプが、受ける側の弱点タイプと一致したときの攻撃力倍率。色と重なると両方を掛ける
    public double WeaknessTypeAttackMultiplier = 2.0;

    public double ActionIntervalSeconds = 0.28;
    public double FastActionIntervalSeconds = 0.02;

    // 敵サイズ → 経験値玉のアイテムID。一致が無いときは最も近いサイズ。
    public int[] ExpOrbSizes = { 8, 16, 32, 64, 128 };

    public static BattleBalanceConfig CreateDefault()
    {
        return new BattleBalanceConfig();
    }

    public long ExpToNext(long level)
    {
        if (level < 0)
            level = 0;
        long need = ExpBase + ExpPerLevel * level;
        if (need < 1)
            need = 1;
        return need;
    }

    public int ResolveExpOrbItemId(int enemySize)
    {
        if (ExpOrbSizes == null || ExpOrbSizes.Length == 0)
            return enemySize;

        int best = ExpOrbSizes[0];
        int bestDistance = Math.Abs(best - enemySize);
        for (int i = 1; i < ExpOrbSizes.Length; i++)
        {
            int distance = Math.Abs(ExpOrbSizes[i] - enemySize);
            if (distance < bestDistance)
            {
                best = ExpOrbSizes[i];
                bestDistance = distance;
            }
        }
        return best;
    }
}

public static class ItemIds
{
    public const int ExpOrb8 = 8;
    public const int ExpOrb16 = 16;
    public const int ExpOrb32 = 32;
    public const int ExpOrb64 = 64;
    public const int ExpOrb128 = 128;
    public const int StageShard = 1001;

    public static string DisplayName(int itemId)
    {
        switch (itemId)
        {
            case ExpOrb8: return "経験値玉(8)";
            case ExpOrb16: return "経験値玉(16)";
            case ExpOrb32: return "経験値玉(32)";
            case ExpOrb64: return "経験値玉(64)";
            case ExpOrb128: return "経験値玉(128)";
            case StageShard: return "階層の欠片";
            default: return "アイテム" + itemId.ToString();
        }
    }
}
