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

    // 経験値 = 切り上げ(不透明ピクセル * 属性値 * ExpMultiplier)。
    // そのあと一辺8を1として、2倍ごとに2.5倍し、小数第1位未満は切り捨てて割る。経験値の端数は切り上げ。
    public double ExpMultiplier = 1.0;

    // 次のレベルまでに必要な経験値 = 非透過ピクセル数 × 属性値 × 切り上げ(今のレベル ^ 指数)。
    // 指数 = 1.15 - (サイズ段階 - 1) × 0.01。段階は一辺8→1、16→2、32→3、64→4。
    // 属性値は倒したときの経験値と同じ。赤・緑・青は1〜3、光は3〜9、闇は1〜3。無属性は1。
    // レベル1は指数乗が1なので、必要量は非透過ピクセル数 × 属性値そのもの。
    // MaxLevel は今のレベル上限。最初は5。100階をクリアするたびに5上がる。
    public const double ExpLevelExponentBase = 1.15;
    public const double ExpLevelExponentPerSizeStep = 0.01;
    public const int InitialLevelCap = 5;
    public const int LevelCapPerHundredFloors = 5;
    public const int LevelCapCeiling = 9999;
    public long MaxLevel = InitialLevelCap;
    // 敵の合成回数。レベル上限が5上がるごとに1。上限5のときは0。
    public const int EnemyFusionPerLevelCapStep = 1;

    public static int EnemyFusionCount(long levelCap)
    {
        if (LevelCapPerHundredFloors < 1)
            return 0;
        if (levelCap < InitialLevelCap)
            levelCap = InitialLevelCap;
        long steps = (levelCap - InitialLevelCap) / LevelCapPerHundredFloors;
        if (steps < 0)
            steps = 0;
        if (steps > int.MaxValue / EnemyFusionPerLevelCapStep)
            return int.MaxValue;
        return (int)steps * EnemyFusionPerLevelCapStep;
    }
    public int MaxLevelsPerExpGrant = 30;

    // 1レベルごとに、基礎ステータスのこの割合を足す。今の値へ掛けない。
    public double HpGrowthRate = 0.05;
    public double AtkGrowthRate = 0.04;
    public double DefGrowthRate = 0.05;
    // SPD は割合を掛けず、1レベルにつき +1 する。合計は 100 を超えない。
    public double SpdGrowthRate = 0.0;
    public double RgbGrowthRate = 0.01;

    // LUC * ExpOrbRatePerLuc。上限 ExpOrbRateCap
    public double ExpOrbRatePerLuc = 0.05;
    public double ExpOrbRateCap = 0.50;

    // (LUC + OBS) * RecruitRatePerPoint。上限 RecruitRateCap
    // (LUC + OBS) / 5 % は (LUC + OBS) * 0.002
    public double RecruitRatePerPoint = 0.002;
    public double RecruitRateCap = 0.05;

    public const int MaxOwnedCount = 999;
    public int MaxLives = MaxOwnedCount;
    public int EnemyInitialLives = 0;

    // 攻撃側の代表カラーが、受ける側の弱点カラーと一致したときの攻撃力倍率
    public double WeaknessAttackMultiplier = 3.0;
    // 攻撃側のタイプが、受ける側の弱点タイプと一致したときの攻撃力倍率。色と重なると両方を掛ける
    public double WeaknessTypeAttackMultiplier = 2.0;

    //通常速度の1アクション。2倍・4倍・8倍・16倍はこの時間を割る
    public double ActionIntervalSeconds = 0.45;

    // 敵サイズ → 経験値玉のアイテムID。一致が無いときは最も近いサイズ。
    public int[] ExpOrbSizes = { 8, 16, 32, 64, 128 };

    public static BattleBalanceConfig CreateDefault()
    {
        return new BattleBalanceConfig();
    }

    public long ExpToNext(long level, long opaquePixels)
    {
        return ExpToNext(level, opaquePixels, 8);
    }

    public long ExpToNext(long level, long opaquePixels, int size)
    {
        if (level < 1)
            level = 1;
        if (opaquePixels < 1)
            opaquePixels = 1;
        double powered = Math.Pow(level, ExpLevelExponent(size));
        long factor = BattleMath.CeilToLong(powered);
        if (factor < 1)
            factor = 1;
        if (opaquePixels > long.MaxValue / factor)
            return long.MaxValue;
        return opaquePixels * factor;
    }

    // 8→1、16→2、32→3。指数は 1.15 - (段階-1)×0.01。
    public static double ExpLevelExponent(int size)
    {
        int step = ExpSizeStep(size);
        double exponent = ExpLevelExponentBase - (step - 1) * ExpLevelExponentPerSizeStep;
        if (exponent < 0.05)
            exponent = 0.05;
        return exponent;
    }

    public static int ExpSizeStep(int size)
    {
        if (size < 16)
            return 1;
        int step = 1;
        int span = 8;
        while (span <= size / 2 && span <= int.MaxValue / 2)
        {
            span *= 2;
            step++;
        }
        return step;
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
    public const int EnmusubiOmamori = 2001;
    // 手直しチケット。入手・消費は後で足す。今は所持0の枠だけ。
    public const int RetouchTicket = 3001;
    // 属性チェンジチケット。入手・消費は後で足す。今は所持0の枠だけ。
    public const int AttributeChangeTicket = 3002;

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
            case EnmusubiOmamori: return "縁結びのお守り";
            case RetouchTicket: return "手直しチケット";
            case AttributeChangeTicket: return "属性チェンジチケット";
            default: return "アイテム" + itemId.ToString();
        }
    }

    public static bool IsExpOrb(int itemId)
    {
        return itemId == ExpOrb8 || itemId == ExpOrb16 || itemId == ExpOrb32 || itemId == ExpOrb64 || itemId == ExpOrb128;
    }

    // 1個あたり、そのサイズ × 10 の経験値。
    public static long ExpPerOrb(int itemId)
    {
        if (!IsExpOrb(itemId))
            return 0;
        return (long)itemId * 10L;
    }
}
