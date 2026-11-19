using System.Globalization;
using System.IO;

// スキルの解放とレベル。星の消費自体は ChallengeProgress 側。
public sealed class EnhanceProgress
{
    readonly bool[] _unlocked = new bool[EnhanceSkillCatalog.Count];
    readonly int[] _level = new int[EnhanceSkillCatalog.Count];
    bool _busy;

    public void Clear()
    {
        for (int i = 0; i < EnhanceSkillCatalog.Count; i++)
        {
            _unlocked[i] = false;
            _level[i] = 0;
        }
        _busy = false;
    }

    public bool IsUnlocked(EnhanceSkillId id)
    {
        int index = (int)id;
        if (index < 0 || index >= _unlocked.Length)
            return false;
        return _unlocked[index];
    }

    public int Level(EnhanceSkillId id)
    {
        int index = (int)id;
        if (index < 0 || index >= _level.Length)
            return 0;
        int level = _level[index];
        return level < 0 ? 0 : level;
    }

    public bool TryBeginAction()
    {
        if (_busy)
            return false;
        _busy = true;
        return true;
    }

    public void EndAction()
    {
        _busy = false;
    }

    public bool Unlock(EnhanceSkillId id)
    {
        int index = (int)id;
        if (index < 0 || index >= _unlocked.Length)
            return false;
        if (_unlocked[index])
            return false;
        _unlocked[index] = true;
        if (_level[index] < 0)
            _level[index] = 0;
        return true;
    }

    public bool LevelUp(EnhanceSkillId id, int cap)
    {
        int index = (int)id;
        if (index < 0 || index >= _level.Length)
            return false;
        if (!_unlocked[index])
            return false;
        if (cap < 1)
            return false;
        if (_level[index] >= cap)
            return false;
        _level[index]++;
        return true;
    }

    public void ClampToCaps(int characterLevelCap)
    {
        for (int i = 0; i < EnhanceSkillCatalog.Count; i++)
        {
            EnhanceSkillDef def = EnhanceSkillCatalog.GetAt(i);
            if (def == null)
                continue;
            int cap = EnhanceSkillCatalog.LevelCap(def.CapKind, characterLevelCap);
            if (_level[i] > cap)
                _level[i] = cap;
            if (_level[i] < 0)
                _level[i] = 0;
        }
    }

    public void Write(StreamWriter sw)
    {
        if (sw == null)
            return;
        for (int i = 0; i < EnhanceSkillCatalog.Count; i++)
        {
            if (!_unlocked[i] && _level[i] <= 0)
                continue;
            sw.WriteLine("EnhanceSkill[" + i.ToString(CultureInfo.InvariantCulture) + "],"
                + (_unlocked[i] ? "1" : "0") + ","
                + _level[i].ToString(CultureInfo.InvariantCulture));
        }
    }

    public bool TryRead(string[] values)
    {
        if (values == null || values.Length < 2 || string.IsNullOrEmpty(values[0]))
            return false;
        string key = values[0];
        if (!key.StartsWith("EnhanceSkill[") || !key.EndsWith("]"))
            return false;
        string inside = key.Substring("EnhanceSkill[".Length, key.Length - "EnhanceSkill[".Length - 1);
        int index;
        if (!int.TryParse(inside, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
            return true;
        if (index < 0 || index >= EnhanceSkillCatalog.Count)
            return true;
        int unlocked = 0;
        int level = 0;
        if (values.Length >= 2)
            int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out unlocked);
        if (values.Length >= 3)
            int.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out level);
        _unlocked[index] = unlocked != 0;
        if (level < 0)
            level = 0;
        _level[index] = level;
        return true;
    }
}
