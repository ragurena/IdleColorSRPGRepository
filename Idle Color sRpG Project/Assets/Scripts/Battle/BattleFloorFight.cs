using System.Collections.Generic;

public enum BattleOutcome
{
    InProgress,
    FloorCleared,
    AlliesDefeated
}

public interface IBattleRewardSink
{
    void GrantRgb(CharacterAttribute attribute, long amount);
    void GrantPixels(int r, int g, int b, long count);
    void GrantExp(BattleUnit ally, long exp);
    void GrantItem(int itemId, int count);
    void Recruit(uint characterId);
    void NoteDefeated(uint characterId);
}

public struct BattleAttackResult
{
    public bool Acted;
    public string Log;
    public BattleOutcome Outcome;
    public BattleUnit Actor;
    public BattleUnit Target;
}

public sealed class BattleFloorFight
{
    public List<BattleUnit> Units = new List<BattleUnit>();
    public BattleOutcome Outcome = BattleOutcome.InProgress;

    readonly BattleBalanceConfig _config;
    readonly IBattleRandom _rng;
    readonly IBattleRewardSink _sink;
    List<int> _queue = new List<int>();
    int _queueIndex;
    bool _turnActive;

    public BattleFloorFight(BattleBalanceConfig config, IBattleRandom rng, IBattleRewardSink sink)
    {
        _config = config;
        _rng = rng;
        _sink = sink;
    }

    public void Begin(List<BattleUnit> units)
    {
        Units = units ?? new List<BattleUnit>();
        Outcome = BattleOutcome.InProgress;
        _queue = new List<int>();
        _queueIndex = 0;
        _turnActive = false;
        ResolveIfSideMissing();
    }

    public BattleAttackResult Step()
    {
        var result = new BattleAttackResult();
        result.Outcome = Outcome;
        if (Outcome != BattleOutcome.InProgress)
            return result;

        for (int guard = 0; guard < 256; guard++)
        {
            if (ResolveIfSideMissing())
            {
                result.Outcome = Outcome;
                return result;
            }

            if (!_turnActive)
            {
                BeginTurn();
                if (ResolveIfSideMissing())
                {
                    result.Outcome = Outcome;
                    return result;
                }
            }

            if (_queueIndex >= _queue.Count)
            {
                _turnActive = false;
                continue;
            }

            int unitId = _queue[_queueIndex];
            _queueIndex++;
            BattleUnit actor = Find(unitId);
            if (actor == null || !actor.IsLiving())
                continue;

            result = Attack(actor);
            result.Outcome = Outcome;
            return result;
        }

        result.Outcome = Outcome;
        return result;
    }

    void BeginTurn()
    {
        var living = new List<BattleUnit>();
        for (int i = 0; i < Units.Count; i++)
        {
            if (Units[i].IsLiving())
                living.Add(Units[i]);
        }
        _queue = BattleTurnPlanner.BuildActionOrder(living, _config, _rng);
        _queueIndex = 0;
        _turnActive = true;
    }

    BattleAttackResult Attack(BattleUnit actor)
    {
        var result = new BattleAttackResult();
        result.Acted = true;
        result.Actor = actor;
        BattleUnit target = BattleTargeting.Select(Units, actor.IsAlly, _rng);
        result.Target = target;
        if (target == null)
        {
            ResolveIfSideMissing();
            result.Log = actor.Name + " は対象がいない";
            result.Outcome = Outcome;
            return result;
        }

        bool colorWeakness = BattleDamageCalculator.IsWeaknessHit(
            actor.RepresentativeR, actor.RepresentativeG, actor.RepresentativeB,
            target.WeaknessR, target.WeaknessG, target.WeaknessB);
        bool typeWeakness = AttributeWeakness.IsHit(actor.Attribute, target.WeaknessAttribute);
        long attack = BattleDamageCalculator.AttackForWeakness(actor.Atk, colorWeakness, typeWeakness, _config);
        long damage = BattleDamageCalculator.Calculate(attack, target.Def, _config);
        KnockoutResult knockout = BattleKnockout.Apply(target, damage, _config.MaxLives);
        if (knockout != KnockoutResult.Alive)
            CancelRemainingActions(target.UnitId);

        if (knockout == KnockoutResult.Defeated && actor.IsAlly && !target.IsAlly)
            BattleDefeatProcessor.Process(actor, target, Units, _config, _rng, _sink);

        ResolveIfSideMissing();

        string weaknessMark = "";
        if (colorWeakness && typeWeakness)
            weaknessMark = " 弱点";
        else if (colorWeakness)
            weaknessMark = " 弱点";
        else if (typeWeakness)
            weaknessMark = " タイプ";
        if (knockout == KnockoutResult.Revived)
            result.Log = actor.Name + " → " + target.Name + " に " + damage + weaknessMark + " 。残機で復活";
        else if (knockout == KnockoutResult.Defeated)
            result.Log = actor.Name + " → " + target.Name + " に " + damage + weaknessMark + " 。戦闘不能";
        else
            result.Log = actor.Name + " → " + target.Name + " に " + damage + weaknessMark + " （HP " + target.Hp + "）";
        result.Outcome = Outcome;
        return result;
    }

    public bool ResolveIfSideMissing()
    {
        if (Outcome != BattleOutcome.InProgress)
            return true;

        bool anyAlly = false;
        bool anyEnemy = false;
        for (int i = 0; i < Units.Count; i++)
        {
            if (!Units[i].IsLiving())
                continue;
            if (Units[i].IsAlly)
                anyAlly = true;
            else
                anyEnemy = true;
        }

        if (!anyEnemy)
        {
            Outcome = BattleOutcome.FloorCleared;
            _queue.Clear();
            _queueIndex = 0;
            return true;
        }
        if (!anyAlly)
        {
            Outcome = BattleOutcome.AlliesDefeated;
            _queue.Clear();
            _queueIndex = 0;
            return true;
        }
        return false;
    }

    void CancelRemainingActions(int unitId)
    {
        for (int i = _queue.Count - 1; i >= _queueIndex; i--)
        {
            if (_queue[i] == unitId)
                _queue.RemoveAt(i);
        }
    }

    BattleUnit Find(int unitId)
    {
        for (int i = 0; i < Units.Count; i++)
        {
            if (Units[i].UnitId == unitId)
                return Units[i];
        }
        return null;
    }
}

public static class BattleDefeatProcessor
{
    public static void Process(BattleUnit killer, BattleUnit enemy, IReadOnlyList<BattleUnit> units, BattleBalanceConfig config, IBattleRandom rng, IBattleRewardSink sink)
    {
        if (killer == null || enemy == null || sink == null)
            return;

        long attributeValue = AttributeColorValue.GetAttributeColorValue(
            enemy.Attribute, enemy.RepresentativeR, enemy.RepresentativeG, enemy.RepresentativeB);
        long rgb = BattleRewardCalculator.RgbGain(killer.Obs, enemy.OpaquePixels, attributeValue, config);
        sink.GrantRgb(enemy.Attribute, rgb);

        long pixels = killer.Luc;
        if (pixels < 0)
            pixels = 0;
        if (pixels > 0)
            sink.GrantPixels(enemy.RepresentativeR, enemy.RepresentativeG, enemy.RepresentativeB, pixels);

        long exp = BattleRewardCalculator.ExpGain(enemy.OpaquePixels, attributeValue, config);
        int survivors = 0;
        if (units != null)
        {
            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit ally = units[i];
                if (ally.IsAlly && ally.IsLiving())
                    survivors++;
            }
        }
        long share = BattleRewardCalculator.ExpShare(exp, survivors);
        if (units != null && share > 0)
        {
            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit ally = units[i];
                if (!ally.IsAlly || !ally.IsLiving())
                    continue;
                sink.GrantExp(ally, share);
            }
        }

        double orbProbability = BattleRewardCalculator.ExpOrbDropProbability(killer.Luc, config);
        if (rng.NextDouble() < orbProbability)
            sink.GrantItem(config.ResolveExpOrbItemId(enemy.Size), 1);

        double recruitProbability = BattleRewardCalculator.RecruitProbability(killer.Luc, killer.Obs, config);
        if (rng.NextDouble() < recruitProbability)
            sink.Recruit(enemy.CharacterId);

        sink.NoteDefeated(enemy.CharacterId);
    }
}
