using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// 100階区間の攻略チャレンジ。星は達成フラグが正。同じチャレンジは二度取れない。
public enum ChallengeKind
{
    Rush = 0,
    BossTime = 1,
    NoDamage = 2,
    MidBossTime = 3
}

public static class ChallengeRules
{
    public const int KindCount = 4;
    // 通常速度の秒。倍速では実時間をこの秒数まで縮める。個別に変えてよい。
    public const float BossTimeLimitSeconds = 10f;
    public const float MidBossTimeLimitSeconds = 10f;

    public static float TimeLimit(ChallengeKind kind)
    {
        if (kind == ChallengeKind.MidBossTime)
            return MidBossTimeLimitSeconds;
        return BossTimeLimitSeconds;
    }

    public static int RangeCount
    {
        get { return Constants.BATTLE_STAGE_FLOOR_CAP / Constants.BATTLE_STAGE_FLOOR_MAX; }
    }

    public static int RangeStartOf(int floor)
    {
        if (floor < 1)
            floor = 1;
        return ((floor - 1) / Constants.BATTLE_STAGE_FLOOR_MAX) * Constants.BATTLE_STAGE_FLOOR_MAX + 1;
    }

    public static int RangeEnd(int rangeStart)
    {
        return rangeStart + Constants.BATTLE_STAGE_FLOOR_MAX - 1;
    }

    // 無被害は最後の10階。91〜100、191〜200、291〜300。出撃の区切りと同じ。
    public static int NoDamageFloor(int rangeStart)
    {
        return rangeStart + Constants.BATTLE_STAGE_FLOOR_MAX - Constants.BATTLE_STAGE_FLOOR_STEP;
    }

    public static int BossFloor(int rangeStart)
    {
        return RangeEnd(rangeStart);
    }

    public static int MidBossFloor(int rangeStart)
    {
        return rangeStart + Constants.BATTLE_STAGE_FLOOR_MAX / 2 - 1;
    }

    public static bool IsBossFloor(int floor)
    {
        return floor >= Constants.BATTLE_STAGE_FLOOR_MAX && floor % Constants.BATTLE_STAGE_FLOOR_MAX == 0;
    }

    public static bool IsMidBossFloor(int floor)
    {
        return floor >= 50 && floor % Constants.BATTLE_STAGE_FLOOR_MAX == 50;
    }

    public static bool TryRangeIndex(int rangeStart, out int index)
    {
        index = 0;
        if (rangeStart < 1 || (rangeStart - 1) % Constants.BATTLE_STAGE_FLOOR_MAX != 0)
            return false;
        index = (rangeStart - 1) / Constants.BATTLE_STAGE_FLOOR_MAX;
        return index >= 0 && index < RangeCount;
    }

    public static bool HasSpawn(int stageIndex, int floor)
    {
        if (stageIndex < 1 || stageIndex > Constants.BATTLE_STAGE_NUM || floor < 1)
            return false;
        StageBattleContent content = BattleStageCatalog.Get(stageIndex);
        if (content == null)
            return false;
        BossFloorSpawn boss = content.FindBoss(floor);
        return boss != null && boss.CharacterId != 0 && boss.MaxCount >= 1;
    }

    public static bool IsOffered(int stageIndex, int rangeStart, ChallengeKind kind)
    {
        int index;
        if (stageIndex < 1 || stageIndex > Constants.BATTLE_STAGE_NUM || !TryRangeIndex(rangeStart, out index))
            return false;
        if (kind == ChallengeKind.Rush || kind == ChallengeKind.NoDamage)
            return true;
        if (kind == ChallengeKind.BossTime)
            return HasSpawn(stageIndex, BossFloor(rangeStart));
        if (kind == ChallengeKind.MidBossTime)
            return HasSpawn(stageIndex, MidBossFloor(rangeStart));
        return false;
    }

    public static void CollectSlots(int stageIndex, int rangeStart, List<ChallengeKind> into)
    {
        if (into == null)
            return;
        into.Clear();
        if (IsOffered(stageIndex, rangeStart, ChallengeKind.Rush))
            into.Add(ChallengeKind.Rush);
        if (IsOffered(stageIndex, rangeStart, ChallengeKind.NoDamage))
            into.Add(ChallengeKind.NoDamage);
        if (IsOffered(stageIndex, rangeStart, ChallengeKind.MidBossTime))
            into.Add(ChallengeKind.MidBossTime);
        if (IsOffered(stageIndex, rangeStart, ChallengeKind.BossTime))
            into.Add(ChallengeKind.BossTime);
    }
}

public static class ChallengeMarks
{
    static Sprite _star;
    static Sprite _starBlur;
    static Sprite _circle;
    static Sprite _triangle;
    static Sprite _square;

    public static Sprite Star()
    {
        if (_star == null)
            _star = Make(InsideStar);
        return _star;
    }

    // 光る星の外側。真ん中は星、周りは白くにじむ。
    public static Sprite StarBlur()
    {
        if (_starBlur == null)
            _starBlur = MakeStarBlur();
        return _starBlur;
    }

    public static Sprite Circle()
    {
        if (_circle == null)
            _circle = Make(InsideCircle);
        return _circle;
    }

    public static Sprite Triangle()
    {
        if (_triangle == null)
            _triangle = Make(InsideTriangle);
        return _triangle;
    }

    public static Sprite Square()
    {
        if (_square != null)
            return _square;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _square = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _square.hideFlags = HideFlags.HideAndDontSave;
        return _square;
    }

    static bool InsideCircle(float x, float y)
    {
        return x * x + y * y <= 0.78f;
    }

    static bool InsideTriangle(float x, float y)
    {
        Vector2 a = new Vector2(0.82f, 0f);
        Vector2 b = new Vector2(-0.72f, 0.78f);
        Vector2 c = new Vector2(-0.72f, -0.78f);
        float ab = Cross(b - a, new Vector2(x, y) - a);
        float bc = Cross(c - b, new Vector2(x, y) - b);
        float ca = Cross(a - c, new Vector2(x, y) - c);
        return (ab >= 0f && bc >= 0f && ca >= 0f) || (ab <= 0f && bc <= 0f && ca <= 0f);
    }

    static float Cross(Vector2 p, Vector2 q)
    {
        return p.x * q.y - p.y * q.x;
    }

    static bool InsideStar(float x, float y)
    {
        Vector2[] poly = StarPoly();
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            float yi = poly[i].y;
            float yj = poly[j].y;
            float xi = poly[i].x;
            float xj = poly[j].x;
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi + 0.00001f) + xi)
                inside = !inside;
        }
        return inside;
    }

    static Vector2[] StarPoly()
    {
        Vector2[] poly = new Vector2[10];
        for (int i = 0; i < poly.Length; i++)
        {
            float angle = (90f - i * 36f) * Mathf.Deg2Rad;
            float radius = i % 2 == 0 ? 0.92f : 0.38f;
            poly[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }
        return poly;
    }

    static Sprite Make(System.Func<float, float, bool> inside)
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = ((x + 0.5f) / size) * 2f - 1f;
                float py = ((y + 0.5f) / size) * 2f - 1f;
                pixels[y * size + x] = inside(px, py)
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    static Sprite MakeStarBlur()
    {
        const int size = 64;
        const float fit = 0.5f;
        float[] alpha = new float[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = (((x + 0.5f) / size) * 2f - 1f) / fit;
                float py = (((y + 0.5f) / size) * 2f - 1f) / fit;
                alpha[y * size + x] = InsideStar(px, py) ? 1f : 0f;
            }
        }
        alpha = BoxBlur(alpha, size, 4);
        alpha = BoxBlur(alpha, size, 4);
        alpha = BoxBlur(alpha, size, 3);
        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            float a = Mathf.Clamp01(alpha[i] * 3.4f);
            a = Mathf.Pow(a, 0.55f);
            pixels[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
        }
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixels32(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    static float[] BoxBlur(float[] src, int size, int radius)
    {
        float[] horizontal = new float[src.Length];
        float[] dst = new float[src.Length];
        int window = radius * 2 + 1;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float sum = 0f;
                for (int k = -radius; k <= radius; k++)
                    sum += src[y * size + Mathf.Clamp(x + k, 0, size - 1)];
                horizontal[y * size + x] = sum / window;
            }
        }
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float sum = 0f;
                for (int k = -radius; k <= radius; k++)
                    sum += horizontal[Mathf.Clamp(y + k, 0, size - 1) * size + x];
                dst[y * size + x] = sum / window;
            }
        }
        return dst;
    }
}

// ステージ1〜5、各100階区間。0は未使用。ギャラリーは入れない。
public sealed class ChallengeProgress
{
    public const int StageSlots = 6;
    readonly bool[,,] _done = new bool[StageSlots, 3, ChallengeRules.KindCount];
    readonly float[,] _bossSeconds = new float[StageSlots, 3];
    readonly float[,] _midSeconds = new float[StageSlots, 3];
    readonly int[] _viewed = new int[StageSlots];
    int _spent;

    public void Clear()
    {
        System.Array.Clear(_done, 0, _done.Length);
        System.Array.Clear(_bossSeconds, 0, _bossSeconds.Length);
        System.Array.Clear(_midSeconds, 0, _midSeconds.Length);
        System.Array.Clear(_viewed, 0, _viewed.Length);
        _spent = 0;
    }

    public int ViewedWindow(int stageIndex)
    {
        if (stageIndex < 1 || stageIndex >= _viewed.Length)
            return 0;
        return _viewed[stageIndex];
    }

    public void SetViewedWindow(int stageIndex, int rangeStart)
    {
        if (stageIndex < 1 || stageIndex >= _viewed.Length)
            return;
        _viewed[stageIndex] = rangeStart;
    }

    public bool IsDone(int stageIndex, int rangeStart, ChallengeKind kind)
    {
        int index;
        int kindIndex = (int)kind;
        if (!ChallengeRules.TryRangeIndex(rangeStart, out index) || stageIndex < 1 || stageIndex >= StageSlots)
            return false;
        if (kindIndex < 0 || kindIndex >= ChallengeRules.KindCount)
            return false;
        return _done[stageIndex, index, kindIndex];
    }

    public bool MarkDone(int stageIndex, int rangeStart, ChallengeKind kind)
    {
        int index;
        int kindIndex = (int)kind;
        if (!ChallengeRules.TryRangeIndex(rangeStart, out index) || stageIndex < 1 || stageIndex >= StageSlots)
            return false;
        if (kindIndex < 0 || kindIndex >= ChallengeRules.KindCount)
            return false;
        if (_done[stageIndex, index, kindIndex])
            return false;
        _done[stageIndex, index, kindIndex] = true;
        return true;
    }

    public bool NoteTime(int stageIndex, int rangeStart, bool mid, float seconds)
    {
        int index;
        if (!ChallengeRules.TryRangeIndex(rangeStart, out index) || stageIndex < 1 || stageIndex >= StageSlots)
            return false;
        if (seconds < 0f)
            seconds = 0f;
        if (mid)
        {
            if (_midSeconds[stageIndex, index] > 0f && seconds >= _midSeconds[stageIndex, index])
                return false;
            _midSeconds[stageIndex, index] = seconds;
            return true;
        }
        if (_bossSeconds[stageIndex, index] > 0f && seconds >= _bossSeconds[stageIndex, index])
            return false;
        _bossSeconds[stageIndex, index] = seconds;
        return true;
    }

    public int EarnedStars()
    {
        int count = 0;
        int ranges = ChallengeRules.RangeCount;
        for (int stage = 1; stage <= Constants.BATTLE_STAGE_NUM; stage++)
        {
            for (int range = 0; range < ranges; range++)
            {
                int start = range * Constants.BATTLE_STAGE_FLOOR_MAX + 1;
                for (int kind = 0; kind < ChallengeRules.KindCount; kind++)
                {
                    if (!_done[stage, range, kind])
                        continue;
                    if (!ChallengeRules.IsOffered(stage, start, (ChallengeKind)kind))
                        continue;
                    count++;
                }
            }
        }
        return count;
    }

    public int SpentStars()
    {
        int spent = _spent;
        if (spent < 0)
            spent = 0;
        int earned = EarnedStars();
        if (spent > earned)
            spent = earned;
        return spent;
    }

    public bool TrySpend(int count)
    {
        if (count < 1)
            return false;
        int held = EarnedStars() - SpentStars();
        if (count > held)
            return false;
        _spent = SpentStars() + count;
        return true;
    }

    public void ClampViewed(System.Func<int, int> progressStart)
    {
        if (_spent < 0)
            _spent = 0;
        int earned = EarnedStars();
        if (_spent > earned)
            _spent = earned;
        if (progressStart == null)
            return;
        for (int stage = 1; stage <= Constants.BATTLE_STAGE_NUM; stage++)
        {
            int viewed = _viewed[stage];
            if (viewed == 0)
                continue;
            int progress = progressStart(stage);
            int index;
            if (!ChallengeRules.TryRangeIndex(viewed, out index) || viewed > progress)
                _viewed[stage] = 0;
        }
    }

    public void Write(StreamWriter sw)
    {
        if (sw == null)
            return;
        int spent = SpentStars();
        if (spent > 0)
            sw.WriteLine("ChallengeStarsSpent," + spent.ToString(CultureInfo.InvariantCulture));
        int ranges = ChallengeRules.RangeCount;
        for (int stage = 1; stage <= Constants.BATTLE_STAGE_NUM; stage++)
        {
            if (_viewed[stage] > 0)
                sw.WriteLine("ChallengeView[" + stage.ToString(CultureInfo.InvariantCulture) + "]," + _viewed[stage].ToString(CultureInfo.InvariantCulture));
            for (int range = 0; range < ranges; range++)
            {
                int start = range * Constants.BATTLE_STAGE_FLOOR_MAX + 1;
                bool any = false;
                for (int kind = 0; kind < ChallengeRules.KindCount; kind++)
                {
                    if (_done[stage, range, kind])
                        any = true;
                }
                if (_bossSeconds[stage, range] > 0f || _midSeconds[stage, range] > 0f)
                    any = true;
                if (!any)
                    continue;
                sw.WriteLine("Challenge[" + stage.ToString(CultureInfo.InvariantCulture) + "," + start.ToString(CultureInfo.InvariantCulture) + "],"
                    + Bit(stage, range, ChallengeKind.Rush) + ","
                    + Bit(stage, range, ChallengeKind.BossTime) + ","
                    + Bit(stage, range, ChallengeKind.NoDamage) + ","
                    + Bit(stage, range, ChallengeKind.MidBossTime));
                if (_bossSeconds[stage, range] > 0f || _midSeconds[stage, range] > 0f)
                {
                    sw.WriteLine("ChallengeTime[" + stage.ToString(CultureInfo.InvariantCulture) + "," + start.ToString(CultureInfo.InvariantCulture) + "],"
                        + _bossSeconds[stage, range].ToString("0.###", CultureInfo.InvariantCulture) + ","
                        + _midSeconds[stage, range].ToString("0.###", CultureInfo.InvariantCulture));
                }
            }
        }
    }

    string Bit(int stage, int range, ChallengeKind kind)
    {
        return _done[stage, range, (int)kind] ? "1" : "0";
    }

    public bool TryRead(string[] values)
    {
        if (values == null || values.Length < 2 || string.IsNullOrEmpty(values[0]))
            return false;
        // Challenge[1,1] はキーの中にカンマがある。Split で割れた列を戻す。
        RejoinBracket(ref values, "ChallengeTime[");
        RejoinBracket(ref values, "Challenge[");
        if (values.Length < 2 || string.IsNullOrEmpty(values[0]))
            return false;
        string key = values[0];
        if (key.Equals("ChallengeStarsSpent"))
        {
            int spent;
            if (int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out spent) && spent > 0)
                _spent = spent;
            return true;
        }
        int stage;
        if (key.StartsWith("ChallengeView[") && key.EndsWith("]"))
        {
            if (!TryStage(key, "ChallengeView[", out stage))
                return true;
            int start;
            if (int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out start))
                _viewed[stage] = start;
            return true;
        }
        int rangeStart;
        if (key.StartsWith("ChallengeTime[") && key.EndsWith("]"))
        {
            if (!TryStageRange(key, "ChallengeTime[", out stage, out rangeStart))
                return true;
            int index;
            if (!ChallengeRules.TryRangeIndex(rangeStart, out index))
                return true;
            float boss;
            float mid;
            if (float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out boss) && boss > 0f)
                _bossSeconds[stage, index] = boss;
            if (values.Length >= 3 && float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out mid) && mid > 0f)
                _midSeconds[stage, index] = mid;
            return true;
        }
        if (key.StartsWith("Challenge[") && key.EndsWith("]"))
        {
            if (!TryStageRange(key, "Challenge[", out stage, out rangeStart))
                return true;
            int index;
            if (!ChallengeRules.TryRangeIndex(rangeStart, out index))
                return true;
            SetBit(stage, index, ChallengeKind.Rush, values, 1);
            SetBit(stage, index, ChallengeKind.BossTime, values, 2);
            SetBit(stage, index, ChallengeKind.NoDamage, values, 3);
            SetBit(stage, index, ChallengeKind.MidBossTime, values, 4);
            return true;
        }
        return false;
    }

    void SetBit(int stage, int range, ChallengeKind kind, string[] values, int column)
    {
        if (column >= values.Length)
            return;
        if (values[column] == "1" || values[column] == "true")
            _done[stage, range, (int)kind] = true;
    }

    static void RejoinBracket(ref string[] values, string prefix)
    {
        if (values == null || values.Length < 3)
            return;
        if (!values[0].StartsWith(prefix) || values[0].EndsWith("]"))
            return;
        if (string.IsNullOrEmpty(values[1]) || !values[1].EndsWith("]"))
            return;
        string[] joined = new string[values.Length - 1];
        joined[0] = values[0] + "," + values[1];
        for (int i = 2; i < values.Length; i++)
            joined[i - 1] = values[i];
        values = joined;
    }

    bool TryStage(string key, string prefix, out int stage)
    {
        stage = 0;
        string body = key.Substring(prefix.Length, key.Length - prefix.Length - 1);
        if (!int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out stage))
            return false;
        return stage >= 1 && stage <= Constants.BATTLE_STAGE_NUM;
    }

    static bool TryStageRange(string key, string prefix, out int stage, out int rangeStart)
    {
        stage = 0;
        rangeStart = 0;
        string body = key.Substring(prefix.Length, key.Length - prefix.Length - 1);
        int comma = body.IndexOf(',');
        if (comma <= 0)
            return false;
        if (!int.TryParse(body.Substring(0, comma), NumberStyles.Integer, CultureInfo.InvariantCulture, out stage))
            return false;
        if (!int.TryParse(body.Substring(comma + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeStart))
            return false;
        return stage >= 1 && stage <= Constants.BATTLE_STAGE_NUM;
    }
}
