using System.Collections.Generic;

public static class BattleTurnPlanner
{
    public static long SlowestSpd(IReadOnlyList<BattleUnit> living)
    {
        long slowest = long.MaxValue;
        for (int i = 0; i < living.Count; i++)
        {
            if (living[i].Spd < slowest)
                slowest = living[i].Spd;
        }
        if (slowest == long.MaxValue)
            return 1;
        return slowest;
    }

    // 同じキャラが連続せず、行動回数ぶんラウンドで回す。
    public static List<int> BuildActionOrder(IReadOnlyList<BattleUnit> living, BattleBalanceConfig config, IBattleRandom rng)
    {
        var order = new List<int>();
        if (living == null || living.Count == 0)
            return order;

        long baseSpd = SlowestSpd(living);
        var ranked = new List<TurnRank>(living.Count);
        for (int i = 0; i < living.Count; i++)
        {
            BattleUnit unit = living[i];
            unit.ActionsThisTurn = BattleActionCount.Count(unit.Spd, baseSpd, config);
            ranked.Add(new TurnRank
            {
                Unit = unit,
                Tie = rng.NextInt(0, 1000000)
            });
        }

        ranked.Sort(Compare);

        int rounds = 0;
        for (int i = 0; i < ranked.Count; i++)
        {
            if (ranked[i].Unit.ActionsThisTurn > rounds)
                rounds = ranked[i].Unit.ActionsThisTurn;
        }

        for (int round = 0; round < rounds; round++)
        {
            for (int i = 0; i < ranked.Count; i++)
            {
                if (ranked[i].Unit.ActionsThisTurn > round)
                    order.Add(ranked[i].Unit.UnitId);
            }
        }

        return order;
    }

    static int Compare(TurnRank a, TurnRank b)
    {
        int spd = b.Unit.Spd.CompareTo(a.Unit.Spd);
        if (spd != 0)
            return spd;
        int ally = (b.Unit.IsAlly ? 1 : 0).CompareTo(a.Unit.IsAlly ? 1 : 0);
        if (ally != 0)
            return ally;
        int front = FrontScore(b.Unit).CompareTo(FrontScore(a.Unit));
        if (front != 0)
            return front;
        return a.Tie.CompareTo(b.Tie);
    }

    // 大きいほど前列。味方は右端(x大)、敵は左端(x小)。
    public static int FrontScore(BattleUnit unit)
    {
        if (unit.IsAlly)
            return unit.X;
        return 1000 - unit.X;
    }

    sealed class TurnRank
    {
        public BattleUnit Unit;
        public int Tie;
    }
}

public static class BattleTargeting
{
    public static BattleUnit Select(IReadOnlyList<BattleUnit> units, bool attackerIsAlly, IBattleRandom rng)
    {
        int frontX = 0;
        bool found = false;
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit.IsAlly == attackerIsAlly || !unit.IsLiving())
                continue;
            if (!found || IsCloserToFront(unit.X, frontX, attackerIsAlly))
            {
                frontX = unit.X;
                found = true;
            }
        }
        if (!found)
            return null;

        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit.IsAlly != attackerIsAlly && unit.IsLiving() && unit.X == frontX)
                count++;
        }
        if (count == 0)
            return null;

        int pick = count == 1 ? 0 : rng.NextInt(0, count);
        int seen = 0;
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit.IsAlly != attackerIsAlly && unit.IsLiving() && unit.X == frontX)
            {
                if (seen == pick)
                    return unit;
                seen++;
            }
        }
        return null;
    }

    // 味方が攻撃するときは敵の小さい x が前列。敵が攻撃するときは味方の大きい x が前列。
    static bool IsCloserToFront(int x, int currentFront, bool attackerIsAlly)
    {
        if (attackerIsAlly)
            return x < currentFront;
        return x > currentFront;
    }
}

public static class BattlePlacement
{
    public static List<CellPosition> ShuffleCells(int formationSize, int count, IBattleRandom rng)
    {
        var cells = new List<CellPosition>();
        for (int x = 1; x <= formationSize; x++)
        {
            for (int y = 1; y <= formationSize; y++)
                cells.Add(new CellPosition(x, y));
        }

        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(0, i + 1);
            CellPosition tmp = cells[i];
            cells[i] = cells[j];
            cells[j] = tmp;
        }

        if (count > cells.Count)
            count = cells.Count;
        if (count < 0)
            count = 0;
        if (count == cells.Count)
            return cells;
        return cells.GetRange(0, count);
    }
}

public struct CellPosition
{
    public int X;
    public int Y;

    public CellPosition(int x, int y)
    {
        X = x;
        Y = y;
    }
}
