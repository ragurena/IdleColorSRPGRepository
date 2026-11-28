using System;
using System.Collections.Generic;
using System.Globalization;

// 共有ステージの判定。Unity に依存しない。戦闘の敵レベル、読み込み回数、ファイルの形をここへ集める。
public static class ShareStageRules
{
    public const int FormatVersion = 2;
    public const int MinFormatVersion = 1;
    public const string GameVersion = "0.0.9";
    public const int FloorCount = 300;
    public const int MinCharacters = 1;
    public const int MaxCharacters = 9;
    public const int MaxStoredStages = 30;
    public const int MaxMineStages = 20;
    public const int MaxCompanionCount = 500;
    public const int MaxZipBytes = 1024 * 1024;
    public const int MaxUncompressedBytes = 2 * 1024 * 1024;
    public const int MaxPngBytes = 256 * 1024;
    public const int MaxEntries = 16;
    public const int MaxCompressionRatio = 100;
    public const int MaxNameChars = 20;
    public const int MaxAuthorChars = 16;
    public const int MaxCommentChars = 80;
    public const int LevelMin = 1;
    public const int LevelMax = 9999;
    public const long StatMax = 1000000000000L;

    public static readonly int[] Sizes = { 8, 16, 32, 64 };

    public static readonly string[] Backgrounds =
    {
        "",
        "kougisitu",
        "nekohatakouen",
        "chigusa_room",
        "moa_room",
        "souga_room"
    };

    public static string BackgroundLabel(string background)
    {
        if (background == "kougisitu")
            return "講義室";
        if (background == "nekohatakouen")
            return "ネコハタ公園";
        if (background == "chigusa_room")
            return "チグサの部屋";
        if (background == "moa_room")
            return "モアの部屋";
        if (background == "souga_room")
            return "ソウガの部屋";
        return "背景なし";
    }

    public static bool IsBackground(string background)
    {
        if (background == null)
            return false;
        for (int i = 0; i < Backgrounds.Length; i++)
        {
            if (Backgrounds[i] == background)
                return true;
        }
        return false;
    }

    public static bool IsSize(int size)
    {
        for (int i = 0; i < Sizes.Length; i++)
        {
            if (Sizes[i] == size)
                return true;
        }
        return false;
    }

    public static bool IsAttribute(string attribute)
    {
        return attribute == "None" || attribute == "Fire" || attribute == "Grass"
            || attribute == "Water" || attribute == "Light" || attribute == "Dark";
    }

    // 1〜10階は5。10階ごとに1上がり、101階と201階で一度下がる。300階だけ30。
    public static int BandBaseLevel(int floor)
    {
        if (floor >= FloorCount)
            return 30;
        if (floor < 1)
            floor = 1;
        int band = (floor - 1) / 10;
        if (band > 29)
            band = 29;
        int chapter = band / 10;
        int index = band % 10;
        int start = chapter <= 0 ? 5 : chapter == 1 ? 10 : 15;
        return start + index;
    }

    public static int LevelCap(int minCharacterLevel)
    {
        if (minCharacterLevel < 1)
            minCharacterLevel = 1;
        return minCharacterLevel > 30 ? minCharacterLevel : 30;
    }

    public static int LoadBonus(int totalLoads)
    {
        if (totalLoads < 1)
            return 0;
        return totalLoads - 1;
    }

    // 今の敵レベルで星を取れるのは、得た星と反映済みの回数が同じとき。
    public static bool CanEarnStar(int stars, int levelBonus)
    {
        return stars >= 0 && stars == levelBonus;
    }

    // 星を得たあとの読み込みで、敵レベルを1段上げる。星が無ければ上げない。
    public static bool RaiseBonusAfterStar(int stars, int levelBonus, out int nextBonus)
    {
        nextBonus = levelBonus < 0 ? 0 : levelBonus;
        if (stars <= nextBonus)
            return false;
        nextBonus++;
        return true;
    }

    public static int EnemyLevel(int floor, int loadBonus, int capLevel)
    {
        if (loadBonus < 0)
            loadBonus = 0;
        int cap = capLevel < 30 ? 30 : capLevel;
        int level = BandBaseLevel(floor) + loadBonus;
        if (level > cap)
            level = cap;
        if (level < 1)
            level = 1;
        return level;
    }

    public static int MinLevel(IList<int> levels)
    {
        if (levels == null || levels.Count == 0)
            return 1;
        int min = levels[0];
        for (int i = 1; i < levels.Count; i++)
        {
            if (levels[i] < min)
                min = levels[i];
        }
        if (min < 1)
            min = 1;
        return min;
    }

    // クリア済みの階から、今出せる100階の始まり。300階のあとは201階。
    public static int WindowStart(int clearedFloor)
    {
        if (clearedFloor < 100)
            return 1;
        if (clearedFloor < 200)
            return 101;
        return 201;
    }

    public static int WindowEnd(int clearedFloor)
    {
        return WindowStart(clearedFloor) + 99;
    }

    // 今の100階のうち、クリア階の次が含まれる10階区切りの始まり。未クリアなら先頭。
    public static int MaxStart(int clearedFloor)
    {
        int window = WindowStart(clearedFloor);
        int relative = clearedFloor - (window - 1);
        if (relative < 0)
            relative = 0;
        int next = relative + 1;
        if (next > 100)
            next = 100;
        int from = window + ((next - 1) / 10) * 10;
        int last = window + 90;
        if (from > last)
            from = last;
        if (from < window)
            from = window;
        return from;
    }

    public static bool IsPlayableWindow(int clearedFloor, int floorFrom, int floorTo)
    {
        int window = WindowStart(clearedFloor);
        int windowEnd = WindowEnd(clearedFloor);
        if (floorFrom < window || floorTo > windowEnd || floorTo < floorFrom)
            return false;
        if ((floorFrom - window) % 10 != 0)
            return false;
        if ((floorTo - window + 1) % 10 != 0)
            return false;
        return floorFrom <= MaxStart(clearedFloor);
    }

    // ギャラリーと同じ出現数。1〜10階は1体、91〜100階は9体。100階ごとに繰り返す。
    public static int EnemyCount(int floor)
    {
        if (floor < 1)
            floor = 1;
        int band = ((floor - 1) % 100) / 10;
        if (band < 1)
            return 1;
        return band;
    }

    // 投稿者の自作キャラID。手直しでも変わらない。共有先の仲間ID（20000番台）とは別。
    public static bool IsAuthorCharacterId(uint id)
    {
        return id >= ImportedCharacters.FirstId && id < SharedCharacterIds.FirstId;
    }

    public static bool IsUuid(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != 36)
            return false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (i == 8 || i == 13 || i == 18 || i == 23)
            {
                if (c != '-')
                    return false;
                continue;
            }
            bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
            if (!hex)
                return false;
        }
        return true;
    }

    public static string NewId()
    {
        return Guid.NewGuid().ToString("D");
    }

    public static bool TryUpdateDate(string updatedAt, out string date)
    {
        date = null;
        DateTime parsed;
        if (!TryParseInstant(updatedAt, out parsed))
            return false;
        date = parsed.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    public static bool TryParseInstant(string text, out DateTime instant)
    {
        instant = default(DateTime);
        if (string.IsNullOrEmpty(text))
            return false;
        DateTimeOffset parsed;
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
            return false;
        instant = parsed.UtcDateTime;
        return true;
    }

    public static string FormatInstant(DateTime instant)
    {
        return instant.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    // 作成日が、前回読み込んだファイルより新しければ true。読めない日付は新しくしない。
    public static bool IsNewerInstant(string next, string previous)
    {
        DateTime after;
        if (!TryParseInstant(next, out after))
            return false;
        DateTime before;
        if (!TryParseInstant(previous, out before))
            return true;
        return after > before;
    }

    // 初回と、記録より新しい更新日だけ回数を増やす。同じ日と古い日は増やさない。
    public static bool NoteLoad(ref int totalLoads, Dictionary<string, string> seen, string authorId, string stageId, string updatedAt)
    {
        if (seen == null || !IsUuid(authorId) || !IsUuid(stageId))
            return false;
        string date;
        if (!TryUpdateDate(updatedAt, out date))
            return false;
        if (totalLoads < 0)
            totalLoads = 0;
        string key = authorId + "|" + stageId;
        string previous;
        if (!seen.TryGetValue(key, out previous) || string.IsNullOrEmpty(previous))
        {
            seen[key] = date;
            if (totalLoads < int.MaxValue)
                totalLoads++;
            return true;
        }
        if (string.CompareOrdinal(date, previous) > 0)
        {
            seen[key] = date;
            if (totalLoads < int.MaxValue)
                totalLoads++;
            return true;
        }
        return true;
    }

    public static bool Increased(int before, int after)
    {
        return after > before;
    }

    public static int CompareGameVersion(string left, string right)
    {
        int[] a = ParseVersion(left);
        int[] b = ParseVersion(right);
        for (int i = 0; i < 3; i++)
        {
            if (a[i] != b[i])
                return a[i] < b[i] ? -1 : 1;
        }
        return 0;
    }

    static int[] ParseVersion(string text)
    {
        int[] parts = { 0, 0, 0 };
        if (string.IsNullOrEmpty(text))
            return parts;
        string[] split = text.Split('.');
        for (int i = 0; i < split.Length && i < 3; i++)
        {
            int value;
            if (int.TryParse(split[i], NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0)
                parts[i] = value;
        }
        return parts;
    }

    public static string CharacterFileName(int slot)
    {
        return "characters/character_" + slot.ToString("000", CultureInfo.InvariantCulture) + ".png";
    }

    public static bool IsSafeEntryName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > 80)
            return false;
        if (name == "manifest.json" || name == "stage.json")
            return true;
        if (!name.StartsWith("characters/character_", StringComparison.Ordinal) || !name.EndsWith(".png", StringComparison.Ordinal))
            return false;
        if (name.IndexOf('\\') >= 0 || name.IndexOf("..", StringComparison.Ordinal) >= 0)
            return false;
        string number = name.Substring("characters/character_".Length, name.Length - "characters/character_".Length - ".png".Length);
        if (number.Length != 3)
            return false;
        for (int i = 0; i < number.Length; i++)
        {
            if (number[i] < '0' || number[i] > '9')
                return false;
        }
        int slot;
        if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out slot))
            return false;
        return slot >= 1 && slot <= MaxCharacters;
    }

    public static string CleanText(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        text = text.Replace("\r", "").Replace("\n", "").Trim();
        if (maxChars < 1)
            return "";
        if (text.Length > maxChars)
            text = text.Substring(0, maxChars);
        return text;
    }

    public static bool HasControl(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c < ' ' || c == '\u007f')
                return true;
        }
        return false;
    }
}

// 共有ファイルから入ったキャラの ID。50 個ごとの枠を 20000 から順に置く。
// 同じ投稿者の枠は続く。50 体を超えたら、末尾に次の 50 枠を足す。
// 版2は投稿者のキャラIDを枠に結び付ける。絵が変わっても、同じ枠のままにする。
public sealed class SharedCharacterIds
{
    public const uint FirstId = 20000;
    public const int PerAuthor = 50;
    public const int MaxAuthors = 200;

    readonly List<string> _authors = new List<string>();
    readonly List<List<string>> _hashes = new List<List<string>>();
    readonly List<List<uint>> _sources = new List<List<uint>>();

    public int AuthorCount
    {
        get { return _authors.Count; }
    }

    public void Clear()
    {
        _authors.Clear();
        _hashes.Clear();
        _sources.Clear();
    }

    public int HashCount(string authorId)
    {
        int count = 0;
        for (int i = 0; i < _authors.Count; i++)
        {
            if (SameAuthor(_authors[i], authorId))
                count += _hashes[i].Count;
        }
        return count;
    }

    public int OpenCount(string authorId)
    {
        int index = LastIndexOf(authorId);
        if (index < 0)
            return 0;
        return _hashes[index].Count;
    }

    public string AuthorAt(int index)
    {
        if (index < 0 || index >= _authors.Count)
            return "";
        return _authors[index];
    }

    public string HashAt(int authorIndex, int hashIndex)
    {
        if (authorIndex < 0 || authorIndex >= _hashes.Count)
            return "";
        if (hashIndex < 0 || hashIndex >= _hashes[authorIndex].Count)
            return "";
        return _hashes[authorIndex][hashIndex];
    }

    public uint SourceAt(int authorIndex, int hashIndex)
    {
        if (authorIndex < 0 || authorIndex >= _sources.Count)
            return 0;
        if (hashIndex < 0 || hashIndex >= _sources[authorIndex].Count)
            return 0;
        return _sources[authorIndex][hashIndex];
    }

    public uint SourceOf(uint localId)
    {
        if (localId < FirstId)
            return 0;
        uint offset = localId - FirstId;
        int block = (int)(offset / (uint)PerAuthor);
        int index = (int)(offset % (uint)PerAuthor);
        return SourceAt(block, index);
    }

    public bool LoadAuthor(string authorId, IList<string> hashes, IList<uint> sources)
    {
        if (_authors.Count >= MaxAuthors)
            return false;
        List<string> copy = new List<string>();
        List<uint> sourceCopy = new List<uint>();
        if (ShareStageRules.IsUuid(authorId) && hashes != null)
        {
            for (int i = 0; i < hashes.Count && copy.Count < PerAuthor; i++)
            {
                if (string.IsNullOrEmpty(hashes[i]) || copy.Contains(hashes[i]))
                    continue;
                uint source = 0;
                if (sources != null && i < sources.Count)
                    source = sources[i];
                if (source != 0 && sourceCopy.Contains(source))
                    source = 0;
                copy.Add(hashes[i]);
                sourceCopy.Add(source);
            }
        }
        else
            authorId = "";
        _authors.Add(authorId);
        _hashes.Add(copy);
        _sources.Add(sourceCopy);
        return authorId.Length > 0;
    }

    public bool TryAdd(string authorId, IList<string> hashes, IList<uint> sources, out string error)
    {
        error = null;
        if (!ShareStageRules.IsUuid(authorId) || hashes == null || sources == null || sources.Count != hashes.Count)
        {
            error = "ファイルが破損しています。";
            return false;
        }
        List<string> fresh = new List<string>();
        List<uint> freshSources = new List<uint>();
        for (int i = 0; i < hashes.Count; i++)
        {
            string hash = hashes[i];
            uint source = sources[i];
            if (string.IsNullOrEmpty(hash) || fresh.Contains(hash) || Known(authorId, hash))
                continue;
            if (source != 0 && (IdOfSource(authorId, source) != 0 || freshSources.Contains(source)))
                continue;
            fresh.Add(hash);
            freshSources.Add(source);
        }
        int room = PerAuthor - OpenCount(authorId);
        if (LastIndexOf(authorId) < 0)
            room = 0;
        int overflow = fresh.Count - room;
        if (overflow < 0)
            overflow = 0;
        int extraBlocks = overflow == 0 ? 0 : (overflow + PerAuthor - 1) / PerAuthor;
        if (_authors.Count + extraBlocks > MaxAuthors)
        {
            error = "読み込める投稿者の数を超えています。";
            return false;
        }
        int cursor = 0;
        while (cursor < fresh.Count)
        {
            int index = LastIndexOf(authorId);
            if (index < 0 || _hashes[index].Count >= PerAuthor)
            {
                _authors.Add(authorId);
                _hashes.Add(new List<string>());
                _sources.Add(new List<uint>());
                index = _authors.Count - 1;
            }
            List<string> block = _hashes[index];
            List<uint> bound = _sources[index];
            while (cursor < fresh.Count && block.Count < PerAuthor)
            {
                block.Add(fresh[cursor]);
                bound.Add(freshSources[cursor]);
                cursor++;
            }
        }
        return true;
    }

    public void Revert(string authorId, int blockCount, int openCount)
    {
        while (_authors.Count > blockCount)
        {
            int last = _authors.Count - 1;
            _authors.RemoveAt(last);
            _hashes.RemoveAt(last);
            if (last < _sources.Count)
                _sources.RemoveAt(last);
        }
        int index = LastIndexOf(authorId);
        if (index < 0)
            return;
        List<string> owned = _hashes[index];
        while (owned.Count > openCount)
            owned.RemoveAt(owned.Count - 1);
        if (index < _sources.Count)
        {
            List<uint> bound = _sources[index];
            while (bound.Count > owned.Count)
                bound.RemoveAt(bound.Count - 1);
        }
        if (owned.Count == 0)
        {
            _authors.RemoveAt(index);
            _hashes.RemoveAt(index);
            if (index < _sources.Count)
                _sources.RemoveAt(index);
        }
    }

    public bool CanBindSource(string authorId, uint sourceId, string hash)
    {
        if (sourceId == 0 || string.IsNullOrEmpty(hash))
            return false;
        uint existing = IdOfSource(authorId, sourceId);
        uint hashId = IdOf(authorId, hash);
        if (hashId == 0)
            return false;
        if (existing != 0)
            return existing == hashId;
        return SlotSource(authorId, hash) == 0;
    }

    public bool TryBindSource(string authorId, uint sourceId, string hash)
    {
        if (!CanBindSource(authorId, sourceId, hash))
            return false;
        if (IdOfSource(authorId, sourceId) != 0)
            return true;
        for (int block = 0; block < _authors.Count; block++)
        {
            if (!SameAuthor(_authors[block], authorId))
                continue;
            int character = _hashes[block].IndexOf(hash);
            if (character < 0)
                continue;
            List<uint> bound = _sources[block];
            while (bound.Count <= character)
                bound.Add(0);
            bound[character] = sourceId;
            return true;
        }
        return false;
    }

    public uint IdOf(string authorId, string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return 0;
        for (int block = 0; block < _authors.Count; block++)
        {
            if (!SameAuthor(_authors[block], authorId))
                continue;
            int character = _hashes[block].IndexOf(hash);
            if (character < 0)
                continue;
            return FirstId + (uint)block * (uint)PerAuthor + (uint)character;
        }
        return 0;
    }

    public uint IdOfSource(string authorId, uint sourceId)
    {
        if (sourceId == 0)
            return 0;
        for (int block = 0; block < _authors.Count; block++)
        {
            if (!SameAuthor(_authors[block], authorId) || block >= _sources.Count)
                continue;
            int character = _sources[block].IndexOf(sourceId);
            if (character < 0)
                continue;
            return FirstId + (uint)block * (uint)PerAuthor + (uint)character;
        }
        return 0;
    }

    uint SlotSource(string authorId, string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return 0;
        for (int block = 0; block < _authors.Count; block++)
        {
            if (!SameAuthor(_authors[block], authorId) || block >= _sources.Count)
                continue;
            int character = _hashes[block].IndexOf(hash);
            if (character < 0 || character >= _sources[block].Count)
                continue;
            return _sources[block][character];
        }
        return 0;
    }

    bool Known(string authorId, string hash)
    {
        for (int i = 0; i < _authors.Count; i++)
        {
            if (SameAuthor(_authors[i], authorId) && _hashes[i].Contains(hash))
                return true;
        }
        return false;
    }

    int LastIndexOf(string authorId)
    {
        for (int i = _authors.Count - 1; i >= 0; i--)
        {
            if (SameAuthor(_authors[i], authorId))
                return i;
        }
        return -1;
    }

    static bool SameAuthor(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
