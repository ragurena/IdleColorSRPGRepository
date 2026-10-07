using System;
using System.Collections.Generic;

sealed class FixedRandom : IBattleRandom
{
    public int IntValue;
    public double DoubleValue;

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;
        if (IntValue < minInclusive || IntValue >= maxExclusive)
            return minInclusive;
        return IntValue;
    }

    public double NextDouble()
    {
        return DoubleValue;
    }
}

sealed class RecordingSink : IBattleRewardSink
{
    public readonly List<string> Calls = new List<string>();

    public void GrantRgb(CharacterAttribute attribute, long amount)
    {
        Calls.Add("rgb");
    }

    public void GrantPixels(int r, int g, int b, long count)
    {
        Calls.Add("pixel");
    }

    public void GrantExp(BattleUnit ally, long exp)
    {
        Calls.Add("exp");
    }

    public void GrantItem(int itemId, int count)
    {
        Calls.Add("item");
    }

    public void Recruit(uint characterId)
    {
        Calls.Add("recruit");
    }

    public void NoteDefeated(uint characterId)
    {
        Calls.Add("catalog");
    }
}

static class Program
{
    static int _failures;

    static void Main()
    {
        BattleBalanceConfig config = BattleBalanceConfig.CreateDefault();
        TestCeil();
        TestProbabilities(config);
        TestDamage(config);
        TestActions(config);
        TestGrowth(config);
        TestAttribute();
        TestComplement();
        TestRewards(config);
        TestTurnOrder(config);
        TestTargeting();
        TestKnockout(config);
        TestWeightedAndBoss();
        TestFusion();
        TestDefeatOrder(config);
        TestReviveCancelsAction(config);
        TestAllyPriorityWhenBothGone(config);

        if (_failures > 0)
        {
            Console.WriteLine("FAILED " + _failures.ToString());
            Environment.Exit(1);
        }
        Console.WriteLine("OK");
    }

    static void TestCeil()
    {
        Check(BattleMath.CeilToLong(127.01) == 128, "127.01");
        Check(BattleMath.CeilToLong(127.50) == 128, "127.50");
        Check(BattleMath.CeilToLong(127.99) == 128, "127.99");
        Check(BattleMath.CeilToLong(127.0) == 127, "127");
        Check(BattleMath.CeilToLong(0) == 0, "0");
    }

    static void TestProbabilities(BattleBalanceConfig config)
    {
        CheckClose(BattleRewardCalculator.ExpOrbDropProbability(1, config), 0.05, "orb luc 1");
        CheckClose(BattleRewardCalculator.ExpOrbDropProbability(10, config), 0.50, "orb luc 10");
        CheckClose(BattleRewardCalculator.ExpOrbDropProbability(20, config), 0.50, "orb luc 20");
        CheckClose(BattleRewardCalculator.RecruitProbability(5, 0, config), 0.01, "recruit 5");
        CheckClose(BattleRewardCalculator.RecruitProbability(3, 2, config), 0.01, "recruit 3+2");
        CheckClose(BattleRewardCalculator.RecruitProbability(10, 0, config), 0.02, "recruit 10");
        CheckClose(BattleRewardCalculator.RecruitProbability(6, 4, config), 0.02, "recruit 6+4");
        CheckClose(BattleRewardCalculator.RecruitProbability(25, 0, config), 0.05, "recruit 25");
        CheckClose(BattleRewardCalculator.RecruitProbability(20, 5, config), 0.05, "recruit 20+5");
        CheckClose(BattleRewardCalculator.RecruitProbability(50, 0, config), 0.05, "recruit 50");
        CheckClose(BattleRewardCalculator.RecruitProbability(40, 10, config), 0.05, "recruit 40+10");
    }

    static void TestDamage(BattleBalanceConfig config)
    {
        Check(BattleDamageCalculator.Calculate(10, 3, config) == 7, "damage 7");
        Check(BattleDamageCalculator.Calculate(3, 10, config) == 1, "minimum damage");
        Check(BattleDamageCalculator.Calculate(5, 5, config) == 1, "equal atk def");
        Check(BattleDamageCalculator.AttackForWeakness(10, true, false, config) == 30, "weakness color attack");
        Check(BattleDamageCalculator.AttackForWeakness(10, false, true, config) == 20, "weakness type attack");
        Check(BattleDamageCalculator.AttackForWeakness(10, true, true, config) == 60, "weakness color and type");
        Check(BattleDamageCalculator.Calculate(BattleDamageCalculator.AttackForWeakness(10, true, false, config), 3, config) == 27, "weakness damage");
        Check(BattleDamageCalculator.IsWeaknessHit(0, 255, 255, 0, 255, 255), "weakness match");
        Check(AttributeWeakness.Of(CharacterAttribute.Fire) == CharacterAttribute.Water, "fire weak to water");
        Check(AttributeWeakness.Of(CharacterAttribute.Grass) == CharacterAttribute.Fire, "grass weak to fire");
        Check(AttributeWeakness.Of(CharacterAttribute.Water) == CharacterAttribute.Grass, "water weak to grass");
        Check(AttributeWeakness.Of(CharacterAttribute.Light) == CharacterAttribute.Dark, "light weak to dark");
        Check(AttributeWeakness.Of(CharacterAttribute.Dark) == CharacterAttribute.Light, "dark weak to light");
        Check(AttributeWeakness.IsHit(CharacterAttribute.Water, CharacterAttribute.Water), "type hit");
        Check(!AttributeWeakness.IsHit(CharacterAttribute.Fire, CharacterAttribute.None), "type none");
    }

    static void TestActions(BattleBalanceConfig config)
    {
        Check(BattleActionCount.Count(10, 10, config) == 1, "action 1");
        Check(BattleActionCount.Count(19, 10, config) == 1, "action floor");
        Check(BattleActionCount.Count(20, 10, config) == 2, "action 2");
        Check(BattleActionCount.Count(30, 10, config) == 3, "action 3");
        Check(BattleActionCount.Count(100, 10, config) == 3, "action cap");
        Check(BattleActionCount.Count(0, 10, config) == 1, "action min");
    }

    static void TestGrowth(BattleBalanceConfig config)
    {
        LevelGrowth growth = BattleLevelGrowth.Grow(100, 20, 10, 50, config);
        long hp = 40 + growth.HpDelta;
        Check(growth.NewHpMax == 110 && growth.HpDelta == 10 && hp == 50, "hp growth");
        Check(growth.NewAtk == 21 && growth.AtkDelta == 1, "atk growth");
        Check(growth.NewDef == 11 && growth.DefDelta == 1, "def growth");
        Check(growth.NewSpd == 50 && growth.SpdDelta == 0, "spd stays on level");
        long hp2;
        long atk2;
        long def2;
        long spd2;
        BattleLevelGrowth.ApplyLevels(100, 20, 10, 50, 2, config, out hp2, out atk2, out def2, out spd2);
        Check(hp2 == 120 && atk2 == 22 && def2 == 12 && spd2 == 50, "two levels from base");
        BattleLevelGrowth.ApplyLevels(1000, 10, 10, 10, 327, config, out hp2, out atk2, out def2, out spd2);
        Check(hp2 == 33700 && atk2 == 337 && def2 == 337, "level 327 stays additive");
        Check(BattleLevelGrowth.AddGrowth(100, config.RgbGrowthRate, 327) == 427, "rgb grows 1 percent of base");
        Check(BattleLevelGrowth.AddGrowth(1, 0.01, 50) == 51, "small stat gains at least 1 per level");
    }

    static void TestComplement()
    {
        int r;
        int g;
        int b;
        HsvComplement.Complementary(255, 0, 0, out r, out g, out b);
        Check(r == 0 && g == 255 && b == 255, "complement red");
        HsvComplement.Complementary(0, 255, 0, out r, out g, out b);
        Check(r == 255 && g == 0 && b == 255, "complement green");
        HsvComplement.Complementary(0, 0, 255, out r, out g, out b);
        Check(r == 255 && g == 255 && b == 0, "complement blue");
        HsvComplement.Complementary(128, 128, 128, out r, out g, out b);
        Check(r == 128 && g == 128 && b == 128, "complement gray");
    }

    static void TestAttribute()
    {
        Check(AttributeColorValue.GetAttributeColorValue(CharacterAttribute.Fire, 12, 3, 4) == 12, "red");
        Check(AttributeColorValue.GetAttributeColorValue(CharacterAttribute.Grass, 12, 3, 4) == 3, "green");
        Check(AttributeColorValue.GetAttributeColorValue(CharacterAttribute.Water, 12, 3, 4) == 4, "blue");
        Check(AttributeColorValue.GetAttributeColorValue(CharacterAttribute.Light, 2, 2, 1) == 5, "light");
        Check(AttributeColorValue.GetAttributeColorValue(CharacterAttribute.Dark, 2, 2, 1) == 2, "dark");
    }

    static void TestRewards(BattleBalanceConfig config)
    {
        Check(BattleRewardCalculator.RgbGain(1, 1, 1, config) == 1, "rgb ceil");
        Check(BattleRewardCalculator.RgbGain(2, 5, 3, config) == 3, "rgb exact");
        Check(BattleRewardCalculator.ExpGain(10, 10, config) == 100, "exp");
        Check(BattleRewardCalculator.ExpGain(43, 0, config) == 43, "exp black");
        Check(BattleRewardCalculator.ExpGain(0, 0, config) == 0, "exp empty");
        Check(config.ExpToNext(0, 43) == 43, "exp table lv0");
        Check(config.ExpToNext(1, 43) == 122, "exp table lv1");
        Check(config.ExpToNext(2, 10) == 52, "exp table lv2");
        Check(BattleRewardCalculator.ExpShare(100, 1) == 100, "exp share 1");
        Check(BattleRewardCalculator.ExpShare(100, 3) == 34, "exp share 3");
        Check(BattleRewardCalculator.ExpShare(100, 0) == 0, "exp share 0");
        long r, g, b;
        BattleRewardCalculator.SplitRgb(CharacterAttribute.Fire, 10, out r, out g, out b);
        Check(r == 10 && g == 0 && b == 0, "split fire");
        BattleRewardCalculator.SplitRgb(CharacterAttribute.Light, 10, out r, out g, out b);
        Check(r + g + b == 10, "split light keeps total");
        Check(config.ResolveExpOrbItemId(8) == 8, "orb 8");
        Check(config.ResolveExpOrbItemId(20) == 16, "orb nearest");
    }

    static void TestTurnOrder(BattleBalanceConfig config)
    {
        var units = new List<BattleUnit>
        {
            Unit(1, true, 30, 3, 1),
            Unit(2, true, 20, 1, 1),
            Unit(3, false, 10, 1, 1)
        };
        List<int> order = BattleTurnPlanner.BuildActionOrder(units, config, new FixedRandom());
        Check(order.Count == 6, "order count");
        Check(order[0] == 1 && order[1] == 2 && order[2] == 3 && order[3] == 1 && order[4] == 2 && order[5] == 1, "A B C A B A");
    }

    static void TestTargeting()
    {
        var units = new List<BattleUnit>
        {
            Unit(1, true, 10, 3, 1),
            Dead(2, false, 1),
            Unit(3, false, 10, 2, 1),
            Unit(4, false, 10, 3, 1)
        };
        BattleUnit target = BattleTargeting.Select(units, true, new FixedRandom());
        Check(target != null && target.UnitId == 3, "ally hits enemy front column 2");

        var allies = new List<BattleUnit>
        {
            Unit(10, false, 10, 1, 1),
            Unit(11, true, 10, 1, 1),
            Unit(12, true, 10, 3, 2)
        };
        BattleUnit allyTarget = BattleTargeting.Select(allies, false, new FixedRandom());
        Check(allyTarget != null && allyTarget.UnitId == 12, "enemy hits ally front column 3");
    }

    static void TestKnockout(BattleBalanceConfig config)
    {
        BattleUnit unit = Unit(1, false, 10, 1, 1);
        unit.Hp = 10;
        unit.HpMax = 40;
        unit.Lives = 2;
        KnockoutResult revived = BattleKnockout.Apply(unit, 10, config.MaxLives);
        Check(revived == KnockoutResult.Revived && unit.Hp == 40 && unit.Lives == 1 && unit.InBattle, "revive while a spare remains");

        KnockoutResult dead = BattleKnockout.Apply(unit, 40, config.MaxLives);
        Check(dead == KnockoutResult.Defeated && !unit.InBattle && unit.Hp == 0 && unit.Lives == 0, "last life falls");
    }

    static void TestWeightedAndBoss()
    {
        var entries = new List<EnemySpawnEntry>
        {
            new EnemySpawnEntry(1, 5, 1, 2),
            new EnemySpawnEntry(2, 3, 0, 2),
            new EnemySpawnEntry(3, 2, 0, 2)
        };
        var rng = new FixedRandom();
        rng.IntValue = 0;
        Check(EnemySpawner.PickWeighted(entries, null, rng).CharacterId == 1, "weight A");
        rng.IntValue = 5;
        Check(EnemySpawner.PickWeighted(entries, null, rng).CharacterId == 2, "weight B");
        rng.IntValue = 8;
        Check(EnemySpawner.PickWeighted(entries, null, rng).CharacterId == 3, "weight C");

        rng.IntValue = 1;
        Check(EnemySpawner.RollTotalCount(entries, 9, rng) == 1, "total min");

        var band = new List<EnemySpawnEntry>
        {
            new EnemySpawnEntry(6, 33, 1, 2),
            new EnemySpawnEntry(9, 1, 1, 3)
        };
        rng.DoubleValue = 0.5;
        List<EnemySpawnEntry> missed = EnemySpawner.Roll(band, 9, rng);
        Check(CountId(missed, 6) == 1 && CountId(missed, 9) == 1, "extras miss");
        rng.DoubleValue = 0.0;
        List<EnemySpawnEntry> hit = EnemySpawner.Roll(band, 9, rng);
        Check(CountId(hit, 6) == 2 && CountId(hit, 9) == 3, "extras hit");
        rng.DoubleValue = 0.02;
        List<EnemySpawnEntry> partial = EnemySpawner.Roll(band, 9, rng);
        Check(CountId(partial, 6) == 2 && CountId(partial, 9) == 1, "only weight that hits");

        StageBattleContent stage = BattleStageCatalog.Get(1);
        Check(stage.FindBoss(10) != null && stage.FindBoss(11) == null, "boss floor");
        Check(stage.FindBoss(100) != null, "floor 100 boss");
        Check(stage.GetBand(1).Entries[0].CharacterId == 1 && stage.GetBand(1).Entries[0].Level == 1, "band 1");
        Check(stage.GetBand(1).Entries[1].Level == 1, "band level per character");
        Check(stage.GetBand(1).Entries[0].Weight == 50, "band weight");
        Check(stage.FindBoss(100).Level == 10, "boss level");
    }

    static void TestFusion()
    {
        Check(FusionBonus.NextCost(0) == 2 && FusionBonus.NextCost(3) == 5, "fusion cost");
        Check(!FusionBonus.CanFuse(2, 0) && FusionBonus.CanFuse(3, 0), "fusion keeps one");
        Check(!FusionBonus.CanFuse(5, 3), "fusion later cost");
        Check(FusionBonus.Bonus(50, 1) == 3, "fusion ceil");
        Check(FusionBonus.Bonus(100, 3) == 15, "fusion percent");
        Check(FusionBonus.Bonus(40, 0) == 0, "fusion none");
        Check(FusionBonus.Bonus(1, 2) - FusionBonus.Bonus(1, 1) >= 1, "fusion small stat steps");
        Check(FusionBonus.Bonus(0, 3) == 3, "fusion zero stat still steps");
        long huge = long.MaxValue / 2;
        long hugeBonus = FusionBonus.Bonus(huge, 37);
        Check(hugeBonus > 0 && huge <= long.MaxValue - hugeBonus, "fusion bonus fits in long");
    }

    static void TestDefeatOrder(BattleBalanceConfig config)
    {
        BattleUnit ally = Unit(1, true, 10, 3, 1);
        ally.Atk = 20;
        ally.Luc = 1;
        ally.Obs = 0;
        ally.Hp = 100;
        ally.HpMax = 100;
        BattleUnit enemy = Unit(2, false, 10, 1, 1);
        enemy.Hp = 5;
        enemy.HpMax = 5;
        enemy.Def = 0;
        enemy.OpaquePixels = 10;
        enemy.Attribute = CharacterAttribute.Fire;
        enemy.RepresentativeR = 10;
        enemy.Size = 8;
        enemy.Lives = 0;

        var sink = new RecordingSink();
        var fight = new BattleFloorFight(config, new FixedRandom(), sink);
        fight.Begin(new List<BattleUnit> { ally, enemy });
        fight.Step();
        Check(fight.Outcome == BattleOutcome.FloorCleared, "floor cleared");
        Check(sink.Calls.Count == 6, "reward count");
        Check(sink.Calls[0] == "rgb" && sink.Calls[1] == "pixel" && sink.Calls[2] == "exp" && sink.Calls[3] == "item" && sink.Calls[4] == "recruit" && sink.Calls[5] == "catalog", "reward order");
    }

    static void TestReviveCancelsAction(BattleBalanceConfig config)
    {
        BattleUnit ally = Unit(1, true, 30, 3, 1);
        ally.Atk = 10;
        ally.Hp = 100;
        ally.HpMax = 100;
        BattleUnit enemy = Unit(2, false, 10, 1, 1);
        enemy.Atk = 80;
        enemy.Hp = 5;
        enemy.HpMax = 100;
        enemy.Lives = 2;
        enemy.Def = 0;

        var fight = new BattleFloorFight(config, new FixedRandom(), new RecordingSink());
        fight.Begin(new List<BattleUnit> { ally, enemy });
        fight.Step();
        Check(enemy.Lives == 1 && enemy.Hp == 100 && enemy.InBattle, "revived before next action");
        fight.Step();
        Check(ally.Hp == 100 && enemy.Hp == 90, "revived enemy lost this turn's action");
    }

    static void TestAllyPriorityWhenBothGone(BattleBalanceConfig config)
    {
        BattleUnit ally = Unit(1, true, 10, 3, 1);
        BattleUnit enemy = Unit(2, false, 10, 1, 1);
        var fight = new BattleFloorFight(config, new FixedRandom(), new RecordingSink());
        fight.Begin(new List<BattleUnit> { ally, enemy });
        ally.InBattle = false;
        ally.Hp = 0;
        enemy.InBattle = false;
        enemy.Hp = 0;
        fight.ResolveIfSideMissing();
        Check(fight.Outcome == BattleOutcome.FloorCleared, "both gone favors allies");
    }

    static BattleUnit Unit(int id, bool ally, long spd, int x, int y)
    {
        return new BattleUnit
        {
            UnitId = id,
            IsAlly = ally,
            Spd = spd,
            X = x,
            Y = y,
            Hp = 10,
            HpMax = 10,
            Name = "U" + id.ToString(),
            InBattle = true,
            WeaknessR = -1,
            WeaknessG = -1,
            WeaknessB = -1
        };
    }

    static BattleUnit Dead(int id, bool ally, int x)
    {
        BattleUnit unit = Unit(id, ally, 10, x, 1);
        unit.Hp = 0;
        unit.InBattle = false;
        return unit;
    }

    static int CountId(List<EnemySpawnEntry> picks, uint id)
    {
        int count = 0;
        for (int i = 0; i < picks.Count; i++)
        {
            if (picks[i].CharacterId == id)
                count++;
        }
        return count;
    }

    static void Check(bool condition, string name)
    {
        if (condition)
            return;
        _failures++;
        Console.WriteLine("FAIL " + name);
    }

    static void CheckClose(double actual, double expected, string name)
    {
        Check(Math.Abs(actual - expected) < 0.0000001, name + " actual " + actual.ToString());
    }
}
