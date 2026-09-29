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
        TestRewards(config);
        TestTurnOrder(config);
        TestTargeting();
        TestKnockout(config);
        TestWeightedAndBoss();
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
        Check(growth.NewSpd == 51 && growth.SpdDelta == 1, "spd growth");
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
        unit.Lives = 1;
        KnockoutResult revived = BattleKnockout.Apply(unit, 10, config.MaxLives);
        Check(revived == KnockoutResult.Revived && unit.Hp == 40 && unit.Lives == 0 && unit.InBattle, "revive");

        KnockoutResult dead = BattleKnockout.Apply(unit, 40, config.MaxLives);
        Check(dead == KnockoutResult.Defeated && !unit.InBattle && unit.Hp == 0, "incapacitated");
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

        StageBattleContent stage = BattleStageCatalog.Get(1);
        Check(stage.FindBoss(10) != null && stage.FindBoss(11) == null, "boss floor");
        Check(stage.FindBoss(100) != null, "floor 100 boss");
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
        Check(sink.Calls.Count == 4, "reward count");
        Check(sink.Calls[0] == "rgb" && sink.Calls[1] == "exp" && sink.Calls[2] == "item" && sink.Calls[3] == "recruit", "reward order");
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
        enemy.Lives = 1;
        enemy.Def = 0;

        var fight = new BattleFloorFight(config, new FixedRandom(), new RecordingSink());
        fight.Begin(new List<BattleUnit> { ally, enemy });
        fight.Step();
        Check(enemy.Lives == 0 && enemy.Hp == 100 && enemy.InBattle, "revived before next action");
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
            InBattle = true
        };
    }

    static BattleUnit Dead(int id, bool ally, int x)
    {
        BattleUnit unit = Unit(id, ally, 10, x, 1);
        unit.Hp = 0;
        unit.InBattle = false;
        return unit;
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
