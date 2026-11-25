using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public sealed class SharedStageCharacterData
{
    public int Slot;
    public string Name = "";
    public int Size;
    public int Level = 1;
    public int FusionCount;
    public string Attribute = "None";
    public string PngHash = "";
    public uint CharacterId;
}

public sealed class SharedStageRecord
{
    public string Key = "";
    public string StageId = "";
    public string AuthorId = "";
    public string AuthorName = "";
    public string Name = "";
    public string Comment = "";
    public int MainR;
    public int MainG;
    public int MainB;
    public string Background = "";
    public int Revision = 1;
    public string CreatedAt = "";
    public string UpdatedAt = "";
    public string ContentHash = "";
    public string FileId = "";
    public bool Mine;
    public int ClearedFloor;
    public int RunBonus;
    public int RunCap = 30;
    public int Stars;
    public int LevelBonus;
    public readonly List<uint> SourceIds = new List<uint>();
    public readonly List<SharedStageCharacterData> Characters = new List<SharedStageCharacterData>();
}

public sealed class SharedCompanionRecord
{
    public uint Id;
    public string Name = "";
    public string JoinKey = "";
    public string PngHash = "";
    public ulong OwnedNumMax;
    public ulong OwnedNumCur;
    public ulong Level = 1;
    public ulong Exp;
    public ulong FusionCount;
    public CharacterType CharacterType;
    public bool FlagFNT;
    public Place Whereabouts;
    public bool CatalogOpened = true;
    public bool CatalogNew = true;
    public long OwnedFull;
    public long OwnedEight;
    public long OwnedHalf;
    public bool HasStock;
}

// 共有ステージと、そこから仲間になったキャラ。読み込み枠とは別。
public static class ShareStageLibrary
{
    public const uint CompanionFirstId = SharedCharacterIds.FirstId;

    static readonly SharedCharacterIds CharacterIds = new SharedCharacterIds();

    static readonly List<SharedStageRecord> Stages = new List<SharedStageRecord>();
    static readonly List<SharedStageRecord> PendingStages = new List<SharedStageRecord>();
    static readonly List<SharedCompanionRecord> PendingCompanions = new List<SharedCompanionRecord>();
    static readonly List<AuthorSave> PendingAuthors = new List<AuthorSave>();
    static readonly List<SharedCompanion> Companions = new List<SharedCompanion>();
    static readonly Dictionary<string, string> SeenDates = new Dictionary<string, string>();
    static uint HighestCompanionId;
    static string _userId = "";
    static string _authorName = "";
    static string _rewardDate = "";
    static string _activeKey = "";
    static int _loadCount;

    sealed class SharedCompanion
    {
        public CharacterClass Character;
        public string JoinKey;
        public string PngHash;
    }

    sealed class AuthorSave
    {
        public string AuthorId = "";
        public readonly List<string> Hashes = new List<string>();
    }

    public static int LoadCount
    {
        get { return _loadCount; }
    }

    public static string UserId
    {
        get { return _userId; }
    }

    public static string AuthorName
    {
        get { return _authorName; }
    }

    public static string RewardDate
    {
        get { return _rewardDate; }
    }

    public static string ActiveKey
    {
        get { return _activeKey; }
    }

    public static void EnsureUser()
    {
        if (ShareStageRules.IsUuid(_userId))
            return;
        _userId = ShareStageRules.NewId();
    }

    public static void RememberAuthorName(string name)
    {
        _authorName = ShareStageRules.CleanText(name, ShareStageRules.MaxAuthorChars);
    }

    public static void SetActive(string key)
    {
        _activeKey = key ?? "";
    }

    public static void ClearActive()
    {
        _activeKey = "";
        ShareBattleRoster.Clear();
    }

    public static SharedStageRecord Find(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;
        for (int i = 0; i < Stages.Count; i++)
        {
            if (Stages[i].Key == key)
                return Stages[i];
        }
        return null;
    }

    public static SharedStageRecord ActiveStage()
    {
        SharedStageRecord stage = Find(_activeKey);
        if (stage == null || stage.Mine)
            return null;
        return stage;
    }

    public static int ActiveClearedFloor()
    {
        SharedStageRecord stage = ActiveStage();
        return stage == null ? 0 : stage.ClearedFloor;
    }

    public static void ForEachPlayable(Action<SharedStageRecord> visit)
    {
        if (visit == null)
            return;
        for (int i = 0; i < Stages.Count; i++)
        {
            if (Stages[i] != null && !Stages[i].Mine)
                visit(Stages[i]);
        }
    }

    public static void ForEachMine(Action<SharedStageRecord> visit)
    {
        if (visit == null)
            return;
        for (int i = 0; i < Stages.Count; i++)
        {
            if (Stages[i] != null && Stages[i].Mine)
                visit(Stages[i]);
        }
    }

    public static int PlayableCount()
    {
        int count = 0;
        for (int i = 0; i < Stages.Count; i++)
        {
            if (Stages[i] != null && !Stages[i].Mine)
                count++;
        }
        return count;
    }

    public static void ForEachCompanion(Action<CharacterClass> visit)
    {
        if (visit == null)
            return;
        for (int i = 0; i < Companions.Count; i++)
        {
            if (Companions[i].Character != null)
                visit(Companions[i].Character);
        }
    }

    public static CharacterClass FindCompanion(uint id)
    {
        if (id < CompanionFirstId)
            return null;
        for (int i = 0; i < Companions.Count; i++)
        {
            CharacterClass character = Companions[i].Character;
            if (character != null && character.ID == id)
                return character;
        }
        return null;
    }

    public static string CompanionPath(uint id)
    {
        return Application.persistentDataPath + "/Character/Shared/" + id.ToString(CultureInfo.InvariantCulture) + ".png";
    }

    public static string PngPath(string hash)
    {
        return Application.persistentDataPath + "/ShareStages/png/" + hash + ".png";
    }

    public static void BeginLoad()
    {
        PendingStages.Clear();
        PendingCompanions.Clear();
        PendingAuthors.Clear();
        SeenDates.Clear();
        _loadCount = 0;
        _rewardDate = "";
        _activeKey = "";
        _authorName = "";
    }

    public static void ClearForReload()
    {
        BeginLoad();
        for (int i = 0; i < Companions.Count; i++)
        {
            CharacterClass character = Companions[i].Character;
            if (character != null && character.ImageTexture2D != null)
                UnityEngine.Object.Destroy(character.ImageTexture2D);
        }
        Companions.Clear();
        Stages.Clear();
        CharacterIds.Clear();
        HighestCompanionId = CompanionFirstId - 1;
        _userId = "";
        ShareBattleRoster.Clear();
    }

    public static void DeleteStoredFiles()
    {
        DeleteDirectory(Application.persistentDataPath + "/ShareStages");
        DeleteDirectory(Application.persistentDataPath + "/Character/Shared");
        ClearForReload();
    }

    public static bool TryReadSaveLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return false;
        int comma = line.IndexOf(',');
        if (comma <= 0)
            return false;
        string key = line.Substring(0, comma);
        string value = Unescape(line.Substring(comma + 1));
        if (key == "ShareUserId")
        {
            _userId = value;
            return true;
        }
        if (key == "ShareAuthorName")
        {
            _authorName = value;
            return true;
        }
        if (key == "ShareLoadCount")
        {
            int count;
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count) && count > 0)
                _loadCount = count;
            return true;
        }
        if (key == "ShareRewardDate")
        {
            _rewardDate = value;
            return true;
        }
        if (key == "ShareActiveKey")
        {
            _activeKey = value;
            return true;
        }
        if (key.StartsWith("ShareSeen[", StringComparison.Ordinal) && key.EndsWith("]", StringComparison.Ordinal))
        {
            string seen = key.Substring("ShareSeen[".Length, key.Length - "ShareSeen[".Length - 1);
            if (!string.IsNullOrEmpty(seen) && !string.IsNullOrEmpty(value))
                SeenDates[seen] = value;
            return true;
        }
        if (key.StartsWith("ShareAuthor[", StringComparison.Ordinal))
            return ReadAuthorField(key, value);
        if (key.StartsWith("SharedStage[", StringComparison.Ordinal))
            return ReadStageField(key, value);
        if (key.StartsWith("SharedCompanion[", StringComparison.Ordinal))
            return ReadCompanionField(key, value);
        return false;
    }

    public static void WriteSave(StreamWriter sw)
    {
        if (sw == null)
            return;
        EnsureUser();
        sw.WriteLine("ShareUserId," + _userId);
        sw.WriteLine("ShareAuthorName," + Escape(_authorName));
        sw.WriteLine("ShareLoadCount," + _loadCount.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine("ShareRewardDate," + _rewardDate);
        sw.WriteLine("ShareActiveKey," + Escape(_activeKey));
        foreach (KeyValuePair<string, string> pair in SeenDates)
            sw.WriteLine("ShareSeen[" + pair.Key + "]," + pair.Value);
        for (int i = 0; i < CharacterIds.AuthorCount; i++)
            WriteAuthor(sw, i);
        for (int i = 0; i < Stages.Count; i++)
            WriteStage(sw, i, Stages[i]);
        for (int i = 0; i < Companions.Count; i++)
            WriteCompanion(sw, i, Companions[i]);
    }

    public static void Restore(BattleBalanceConfig balance)
    {
        Stages.Clear();
        for (int i = 0; i < PendingStages.Count; i++)
        {
            SharedStageRecord stage = PendingStages[i];
            if (stage == null || string.IsNullOrEmpty(stage.Key) || !ShareStageRules.IsUuid(stage.StageId))
                continue;
            if (!stage.Mine && stage.Characters.Count < 1)
                continue;
            Stages.Add(stage);
        }
        PendingStages.Clear();
        CharacterIds.Clear();
        for (int i = 0; i < PendingAuthors.Count; i++)
        {
            AuthorSave saved = PendingAuthors[i];
            if (saved == null)
                continue;
            CharacterIds.LoadAuthor(saved.AuthorId, saved.Hashes);
        }
        PendingAuthors.Clear();
        for (int i = 0; i < Stages.Count; i++)
            FillCharacterIds(Stages[i]);
        FoldSameAuthors();
        Companions.Clear();
        HighestCompanionId = CompanionFirstId - 1;
        for (int i = 0; i < PendingCompanions.Count; i++)
        {
            SharedCompanionRecord record = PendingCompanions[i];
            if (record == null || record.Id < CompanionFirstId)
                continue;
            RestoreCompanion(record, balance);
        }
        PendingCompanions.Clear();
        EnsureCatalogCharacters(balance);
        if (ActiveStage() == null)
            _activeKey = "";
    }

    public static bool TryBuild(string stageName, string authorName, string comment, int mainR, int mainG, int mainB, string background, string existingStageId, IList<CharacterClass> selected, out PiquaDocument document, out string error)
    {
        document = null;
        error = null;
        EnsureUser();
        authorName = ShareStageRules.CleanText(authorName, ShareStageRules.MaxAuthorChars);
        comment = ShareStageRules.CleanText(comment, ShareStageRules.MaxCommentChars);
        if (string.IsNullOrEmpty(authorName))
        {
            error = "ユーザー名を入れてください。";
            return false;
        }
        stageName = ShareStageRules.CleanText(authorName + "とバトル", ShareStageRules.MaxNameChars);
        if (!ShareStageRules.IsBackground(background))
        {
            error = "背景を選び直してください。";
            return false;
        }
        if (selected == null || selected.Count < ShareStageRules.MinCharacters || selected.Count > ShareStageRules.MaxCharacters)
        {
            error = "キャラクターは1体から9体までです。";
            return false;
        }
        PiquaDocument built = new PiquaDocument();
        built.FileId = ShareStageRules.NewId();
        built.AuthorId = _userId;
        built.AuthorName = authorName;
        built.StageName = stageName;
        built.Comment = comment;
        built.MainR = Mathf.Clamp(mainR, 0, 255);
        built.MainG = Mathf.Clamp(mainG, 0, 255);
        built.MainB = Mathf.Clamp(mainB, 0, 255);
        built.Background = background ?? "";
        built.FloorCount = ShareStageRules.FloorCount;
        SharedStageRecord previous = FindMine(existingStageId);
        if (previous == null)
        {
            built.StageId = ShareStageRules.NewId();
            built.Revision = 1;
            string now = ShareStageRules.FormatInstant(DateTime.UtcNow);
            built.CreatedAt = now;
            built.UpdatedAt = now;
        }
        else
        {
            built.StageId = previous.StageId;
            built.Revision = previous.Revision < int.MaxValue ? previous.Revision + 1 : previous.Revision;
            built.CreatedAt = previous.CreatedAt;
            built.UpdatedAt = ShareStageRules.FormatInstant(DateTime.UtcNow);
        }
        for (int i = 0; i < selected.Count; i++)
        {
            CharacterClass character = selected[i];
            if (character == null || character.ID < ImportedCharacters.FirstId || character.ID >= CompanionFirstId)
            {
                error = "読み込んだキャラクターだけ選べます。";
                return false;
            }
            string path = ImportedCharacters.OriginalPath(character.ID);
            if (!File.Exists(path))
            {
                error = "キャラクター画像がありません。";
                return false;
            }
            byte[] png = File.ReadAllBytes(path);
            if (character.Stats == null || character.Stats[1] == null)
            {
                error = "不正なキャラクターデータです。";
                return false;
            }
            PiquaCharacter entry = new PiquaCharacter();
            entry.Slot = i + 1;
            entry.Name = ShareStageRules.CleanText(character.Name, ShareStageRules.MaxNameChars);
            if (string.IsNullOrEmpty(entry.Name))
                entry.Name = "ななし";
            entry.FileName = ShareStageRules.CharacterFileName(entry.Slot);
            entry.Size = character.Size;
            long level = character.Level > (ulong)ShareStageRules.LevelMax ? ShareStageRules.LevelMax : (long)character.Level;
            entry.Level = level < 1 ? 1 : (int)level;
            long fusion = character.FusionCount > (ulong)ShareStageRules.LevelMax ? ShareStageRules.LevelMax : (long)character.FusionCount;
            entry.FusionCount = fusion < 0 ? 0 : (int)fusion;
            entry.Attribute = character.CharacterType.ToString();
            entry.Hp = (long)character.Stats[1].HPMax;
            entry.Atk = (long)character.Stats[1].ATK;
            entry.Def = (long)character.Stats[1].DEF;
            entry.Spd = character.Stats[1].SPD;
            entry.Luc = (long)character.Stats[1].LUC;
            entry.Obs = (long)character.Stats[1].OBS;
            entry.Png = png;
            built.Characters.Add(entry);
        }
        string check;
        PiquaDocument parsed;
        byte[] zip = PiquaCodec.Build(built);
        if (!PiquaCodec.TryParse(zip, out parsed, out check))
        {
            error = check ?? "ファイルを作れませんでした。";
            return false;
        }
        document = built;
        RememberAuthorName(authorName);
        return true;
    }

    public static void RememberMine(PiquaDocument document, IList<CharacterClass> selected)
    {
        if (document == null)
            return;
        SharedStageRecord stage = FindMine(document.StageId);
        if (stage == null)
        {
            int mines = 0;
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].Mine)
                    mines++;
            }
            if (mines >= ShareStageRules.MaxMineStages)
                return;
            stage = new SharedStageRecord();
            stage.Key = "mine_" + document.StageId;
            stage.Mine = true;
            Stages.Add(stage);
        }
        CopyMeta(stage, document);
        stage.SourceIds.Clear();
        if (selected != null)
        {
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] != null)
                    stage.SourceIds.Add(selected[i].ID);
            }
        }
    }

    public static bool TryAccept(PiquaDocument document, BattleBalanceConfig balance, out string error)
    {
        error = null;
        EnsureUser();
        if (document == null)
        {
            error = "ファイルが破損しています。";
            return false;
        }
        if (string.Equals(document.AuthorId, _userId, StringComparison.OrdinalIgnoreCase))
        {
            error = "自分のステージは読み込めません。";
            return false;
        }
        SharedStageRecord existing = FindPlayableByAuthor(document.AuthorId);
        string key = existing != null && !string.IsNullOrEmpty(existing.Key) ? existing.Key : document.AuthorId;
        if (existing == null && PlayableCount() >= ShareStageRules.MaxStoredStages)
        {
            error = "共有ステージは" + ShareStageRules.MaxStoredStages.ToString(CultureInfo.InvariantCulture) + "件までです。";
            return false;
        }
        for (int i = 0; i < document.Characters.Count; i++)
        {
            string statError;
            if (!StatsMatch(document.Characters[i], balance, out statError))
            {
                error = statError;
                return false;
            }
        }
        List<string> hashes = new List<string>();
        for (int i = 0; i < document.Characters.Count; i++)
            hashes.Add(Sha256(document.Characters[i].Png));
        int authorsBefore = CharacterIds.AuthorCount;
        int hashesBefore = CharacterIds.OpenCount(document.AuthorId);
        if (!CharacterIds.TryAdd(document.AuthorId, hashes, out error))
            return false;
        int before = _loadCount;
        if (!ShareStageRules.NoteLoad(ref _loadCount, SeenDates, document.AuthorId, document.StageId, document.UpdatedAt))
        {
            CharacterIds.Revert(document.AuthorId, authorsBefore, hashesBefore);
            error = "ファイルが破損しています。";
            return false;
        }
        for (int i = 0; i < document.Characters.Count; i++)
        {
            PiquaCharacter character = document.Characters[i];
            string hash = Sha256(character.Png);
            string path = PngPath(hash);
            if (!File.Exists(path))
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllBytes(path, character.Png);
            }
        }
        SharedStageRecord stage = existing ?? new SharedStageRecord();
        int cleared = existing != null ? existing.ClearedFloor : 0;
        int runBonus = existing != null ? existing.RunBonus : 0;
        int runCap = existing != null ? existing.RunCap : 30;
        int stars = existing != null ? existing.Stars : 0;
        int levelBonus = existing != null ? existing.LevelBonus : 0;
        bool levelsUpdated = existing != null && CharacterLevelsDiffer(existing, document);
        bool createdNewer = existing != null && ShareStageRules.IsNewerInstant(document.CreatedAt, existing.CreatedAt);
        bool refreshLevel = existing == null || (levelsUpdated && createdNewer);
        CopyMeta(stage, document);
        stage.Key = key;
        stage.Mine = false;
        stage.ClearedFloor = cleared;
        stage.RunBonus = runBonus;
        stage.RunCap = runCap;
        stage.Stars = stars;
        stage.LevelBonus = levelBonus;
        stage.Characters.Clear();
        for (int i = 0; i < document.Characters.Count; i++)
        {
            PiquaCharacter character = document.Characters[i];
            string hash = Sha256(character.Png);
            stage.Characters.Add(new SharedStageCharacterData
            {
                Slot = character.Slot,
                Name = character.Name,
                Size = character.Size,
                Level = character.Level,
                FusionCount = character.FusionCount,
                Attribute = character.Attribute,
                PngHash = hash,
                CharacterId = CharacterIds.IdOf(document.AuthorId, hash)
            });
        }
        bool stronger = false;
        if (refreshLevel)
        {
            stage.RunCap = CapFromCharacters(stage);
            int nextBonus;
            if (existing != null && ShareStageRules.RaiseBonusAfterStar(stage.Stars, stage.LevelBonus, out nextBonus))
            {
                stronger = true;
                stage.LevelBonus = nextBonus;
                stage.RunBonus = nextBonus;
            }
            else
                stage.RunBonus = stage.LevelBonus;
        }
        if (existing == null)
            Stages.Add(stage);
        EnsureCatalogCharacters(balance);
        error = stronger
            ? "読み込みました。敵が少し強くなります。"
            : "読み込みました。";
        return true;
    }

    static bool CharacterLevelsDiffer(SharedStageRecord stage, PiquaDocument document)
    {
        if (stage == null || document == null)
            return false;
        if (stage.Characters.Count != document.Characters.Count)
            return true;
        int count = stage.Characters.Count;
        int[] before = new int[count];
        int[] after = new int[count];
        for (int i = 0; i < count; i++)
        {
            before[i] = stage.Characters[i] != null ? stage.Characters[i].Level : 0;
            after[i] = document.Characters[i] != null ? document.Characters[i].Level : 0;
        }
        Array.Sort(before);
        Array.Sort(after);
        for (int i = 0; i < count; i++)
        {
            if (before[i] != after[i])
                return true;
        }
        return false;
    }

    static int CapFromCharacters(SharedStageRecord stage)
    {
        int min = 1;
        bool any = false;
        if (stage != null)
        {
            for (int i = 0; i < stage.Characters.Count; i++)
            {
                int level = stage.Characters[i].Level;
                if (!any || level < min)
                    min = level;
                any = true;
            }
        }
        return ShareStageRules.LevelCap(min);
    }

    public static void Freeze(SharedStageRecord stage)
    {
        if (stage == null)
            return;
        if (stage.RunCap < 30)
            stage.RunCap = 30;
        stage.RunBonus = stage.LevelBonus;
    }

    public static int TotalStars()
    {
        int total = 0;
        for (int i = 0; i < Stages.Count; i++)
        {
            if (Stages[i] != null && !Stages[i].Mine && Stages[i].Stars > 0)
                total += Stages[i].Stars;
        }
        return total;
    }

    public static int ActiveStars()
    {
        SharedStageRecord stage = ActiveStage();
        return stage != null ? stage.Stars : 0;
    }

    public static bool StarAvailable()
    {
        SharedStageRecord stage = ActiveStage();
        return stage != null && ShareStageRules.CanEarnStar(stage.Stars, stage.LevelBonus);
    }

    public static bool TryAwardStar()
    {
        SharedStageRecord stage = ActiveStage();
        if (stage == null || !ShareStageRules.CanEarnStar(stage.Stars, stage.LevelBonus))
            return false;
        stage.Stars++;
        return true;
    }

    public static bool MarkFloorCleared(int floor)
    {
        SharedStageRecord stage = ActiveStage();
        if (stage == null || floor < 1)
            return false;
        if (floor > ShareStageRules.FloorCount)
            floor = ShareStageRules.FloorCount;
        if (floor <= stage.ClearedFloor)
            return false;
        stage.ClearedFloor = floor;
        return true;
    }

    public static bool RecruitAllowed()
    {
        SharedStageRecord stage = ActiveStage();
        return stage != null && stage.ClearedFloor >= ShareStageRules.FloorCount;
    }

    public static void FillSpawns(int floor, Func<int, int> nextIndex, List<EnemySpawnEntry> into)
    {
        if (into == null)
            return;
        into.Clear();
        SharedStageRecord stage = ActiveStage();
        if (stage == null || stage.Characters.Count == 0)
            return;
        if (!ShareBattleRoster.IsReady())
            ShareBattleRoster.Prepare(stage);
        int cap = stage.RunCap < 30 ? 30 : stage.RunCap;
        int level = ShareStageRules.EnemyLevel(floor, stage.RunBonus, cap);
        if (floor >= ShareStageRules.FloorCount)
        {
            for (int i = 0; i < stage.Characters.Count && i < 9; i++)
                into.Add(new EnemySpawnEntry(ShareBattleRoster.TempId(stage.Characters[i].Slot), 1, 1, 1, level));
            return;
        }
        int count = ShareStageRules.EnemyCount(floor);
        if (count > 9)
            count = 9;
        for (int i = 0; i < count; i++)
        {
            int index = nextIndex != null ? nextIndex(stage.Characters.Count) : 0;
            if (index < 0 || index >= stage.Characters.Count)
                index = 0;
            into.Add(new EnemySpawnEntry(ShareBattleRoster.TempId(stage.Characters[index].Slot), 1, 1, 1, level));
        }
    }

    public static bool CharmForces(uint tempId)
    {
        string joinKey = ShareBattleRoster.JoinKey(tempId);
        if (string.IsNullOrEmpty(joinKey))
            return false;
        SharedCompanion companion = FindByJoin(joinKey);
        if (companion == null || companion.Character == null)
            return true;
        return companion.Character.OwnedNumCur < 1;
    }

    public static string RecruitTemp(uint tempId, BattleBalanceConfig balance, int lifeCap)
    {
        string joinKey = ShareBattleRoster.JoinKey(tempId);
        byte[] png = ShareBattleRoster.Png(tempId);
        string name = ShareBattleRoster.NameOf(tempId);
        if (string.IsNullOrEmpty(joinKey) || png == null)
            return null;
        string authorId;
        string hash;
        SplitJoin(joinKey, out authorId, out hash);
        uint id = CharacterIds.IdOf(authorId, hash);
        if (id == 0)
            return "仲間にできませんでした。";
        SharedCompanion companion = FindByJoin(joinKey);
        if (companion == null)
            companion = FindByCharacterId(id);
        if (companion != null && companion.Character != null)
        {
            bool first = companion.Character.OwnedNumMax == 0;
            int lives = companion.Character.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)companion.Character.OwnedNumCur;
            if (lives < lifeCap)
                lives++;
            companion.Character.SetOwnedCur(lives, lifeCap);
            if (first)
            {
                companion.Character.CatalogOpened = true;
                companion.Character.CatalogNew = true;
                return companion.Character.Name + " が仲間になった";
            }
            return companion.Character.Name + " の所持が1増えた";
        }
        if (Companions.Count >= ShareStageRules.MaxCompanionCount)
            return "共有の仲間は" + ShareStageRules.MaxCompanionCount.ToString(CultureInfo.InvariantCulture) + "体までです。";
        string path = CompanionPath(id);
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(path, png);
        CharacterClass character = new CharacterClass();
        string unique = UniqueName(name);
        if (!character.MakeCharacter(path, id, unique) || character.ID != id)
        {
            if (File.Exists(path))
                File.Delete(path);
            return "仲間にできませんでした。";
        }
        character.Level = 1;
        character.FusionCount = 0;
        character.Exp = 0;
        character.SetOwnedCur(1, lifeCap);
        character.AuthorPersonId = 0;
        character.AuthorSpecified = true;
        character.CatalogOpened = true;
        character.CatalogNew = true;
        CharacterClass source = ShareBattleRoster.Find(tempId);
        CharacterType fileType = source != null ? source.CharacterType : CharacterType.None;
        if (source != null)
            character.RecalculateBaseStatsKeepingType(balance, fileType);
        else if (balance != null)
            character.RecalculateBaseStats(balance);
        if (id > HighestCompanionId)
            HighestCompanionId = id;
        Companions.Add(new SharedCompanion { Character = character, JoinKey = joinKey, PngHash = string.IsNullOrEmpty(hash) ? Sha256(png) : hash });
        return character.Name + " が仲間になった";
    }

    public static bool TryClaimReward(string today)
    {
        if (string.IsNullOrEmpty(today))
            return false;
        if (_rewardDate == today)
            return false;
        _rewardDate = today;
        return true;
    }

    public static string Today()
    {
        return DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static bool TryReadAttribute(string value, out CharacterType type)
    {
        switch (value)
        {
            case "None": type = CharacterType.None; return true;
            case "Fire": type = CharacterType.Fire; return true;
            case "Grass": type = CharacterType.Grass; return true;
            case "Water": type = CharacterType.Water; return true;
            case "Light": type = CharacterType.Light; return true;
            case "Dark": type = CharacterType.Dark; return true;
            default:
                type = CharacterType.None;
                return false;
        }
    }

    public static string Sha256(byte[] body)
    {
        if (body == null)
            body = new byte[0];
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(body);
            StringBuilder text = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                text.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return text.ToString();
        }
    }

    static bool StatsMatch(PiquaCharacter character, BattleBalanceConfig balance, out string error)
    {
        error = null;
        string directory = Application.persistentDataPath + "/ShareStages/check";
        string path = directory + "/character_" + character.Slot.ToString("000", CultureInfo.InvariantCulture) + ".png";
        CharacterClass made = null;
        try
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, character.Png);
            made = new CharacterClass();
            if (!made.MakeCharacter(path, 39999, character.Name) || made.Size != character.Size)
            {
                error = "不正なキャラクターデータです。";
                return false;
            }
            CharacterType fileType;
            bool keepType = TryReadAttribute(character.Attribute, out fileType);
            if (keepType)
                made.RecalculateBaseStatsKeepingType(balance, fileType);
            else if (balance != null)
                made.RecalculateBaseStats(balance);
            if (made.Stats == null || made.Stats[1] == null)
            {
                error = "不正なキャラクターデータです。";
                return false;
            }
            // 数値は絵から取り直す。属性は、チェンジ後のものをファイルから読む。
            character.Hp = (long)made.Stats[1].HPMax;
            character.Atk = (long)made.Stats[1].ATK;
            character.Def = (long)made.Stats[1].DEF;
            character.Spd = made.Stats[1].SPD;
            character.Luc = (long)made.Stats[1].LUC;
            character.Obs = (long)made.Stats[1].OBS;
            return true;
        }
        finally
        {
            if (made != null && made.ImageTexture2D != null)
                UnityEngine.Object.Destroy(made.ImageTexture2D);
            DeleteDirectory(directory);
        }
    }

    static void CopyMeta(SharedStageRecord stage, PiquaDocument document)
    {
        stage.StageId = document.StageId;
        stage.AuthorId = document.AuthorId;
        stage.AuthorName = document.AuthorName;
        stage.Name = document.StageName;
        stage.Comment = document.Comment;
        stage.MainR = document.MainR;
        stage.MainG = document.MainG;
        stage.MainB = document.MainB;
        stage.Background = document.Background;
        stage.Revision = document.Revision;
        stage.CreatedAt = document.CreatedAt;
        stage.UpdatedAt = document.UpdatedAt;
        stage.ContentHash = document.ContentHash;
        stage.FileId = document.FileId;
    }

    static SharedStageRecord FindMine(string stageId)
    {
        if (!ShareStageRules.IsUuid(stageId))
            return null;
        return Find("mine_" + stageId);
    }

    static SharedStageRecord FindPlayableByAuthor(string authorId)
    {
        if (!ShareStageRules.IsUuid(authorId))
            return null;
        for (int i = 0; i < Stages.Count; i++)
        {
            SharedStageRecord stage = Stages[i];
            if (stage == null || stage.Mine)
                continue;
            if (string.Equals(stage.AuthorId, authorId, StringComparison.OrdinalIgnoreCase))
                return stage;
        }
        return null;
    }

    // 同じユーザーIDの読み込みステージは1枚にまとめる。進行は残し、キャラは新しいファイルのほう。
    static void FoldSameAuthors()
    {
        for (int i = 0; i < Stages.Count; i++)
        {
            SharedStageRecord keep = Stages[i];
            if (keep == null || keep.Mine || !ShareStageRules.IsUuid(keep.AuthorId))
                continue;
            for (int j = Stages.Count - 1; j > i; j--)
            {
                SharedStageRecord other = Stages[j];
                if (other == null || other.Mine)
                    continue;
                if (!string.Equals(other.AuthorId, keep.AuthorId, StringComparison.OrdinalIgnoreCase))
                    continue;
                AbsorbAuthor(keep, other);
                if (!string.IsNullOrEmpty(other.Key) && _activeKey == other.Key)
                    _activeKey = keep.AuthorId;
                Stages.RemoveAt(j);
            }
            string previous = keep.Key;
            keep.Key = keep.AuthorId;
            if (!string.IsNullOrEmpty(previous) && _activeKey == previous)
                _activeKey = keep.Key;
        }
    }

    static void AbsorbAuthor(SharedStageRecord keep, SharedStageRecord other)
    {
        bool newer = string.CompareOrdinal(other.UpdatedAt ?? "", keep.UpdatedAt ?? "") > 0;
        int stars = keep.Stars > other.Stars ? keep.Stars : other.Stars;
        int bonus = keep.LevelBonus > other.LevelBonus ? keep.LevelBonus : other.LevelBonus;
        int cleared = keep.ClearedFloor > other.ClearedFloor ? keep.ClearedFloor : other.ClearedFloor;
        if (newer)
            CopyFace(keep, other);
        keep.Stars = stars;
        keep.LevelBonus = bonus > stars ? stars : bonus;
        keep.ClearedFloor = cleared;
        keep.RunBonus = keep.LevelBonus;
    }

    static void CopyFace(SharedStageRecord into, SharedStageRecord from)
    {
        into.StageId = from.StageId;
        into.AuthorName = from.AuthorName;
        into.Name = from.Name;
        into.Comment = from.Comment;
        into.MainR = from.MainR;
        into.MainG = from.MainG;
        into.MainB = from.MainB;
        into.Background = from.Background;
        into.Revision = from.Revision;
        into.CreatedAt = from.CreatedAt;
        into.UpdatedAt = from.UpdatedAt;
        into.ContentHash = from.ContentHash;
        into.FileId = from.FileId;
        into.Characters.Clear();
        for (int i = 0; i < from.Characters.Count; i++)
        {
            SharedStageCharacterData source = from.Characters[i];
            if (source == null)
                continue;
            into.Characters.Add(new SharedStageCharacterData
            {
                Slot = source.Slot,
                Name = source.Name,
                Size = source.Size,
                Level = source.Level,
                FusionCount = source.FusionCount,
                Attribute = source.Attribute,
                PngHash = source.PngHash,
                CharacterId = source.CharacterId
            });
        }
    }

    static SharedCompanion FindByJoin(string joinKey)
    {
        for (int i = 0; i < Companions.Count; i++)
        {
            if (Companions[i].JoinKey == joinKey)
                return Companions[i];
        }
        return null;
    }

    static SharedCompanion FindByCharacterId(uint id)
    {
        for (int i = 0; i < Companions.Count; i++)
        {
            CharacterClass character = Companions[i].Character;
            if (character != null && character.ID == id)
                return Companions[i];
        }
        return null;
    }

    static void SplitJoin(string joinKey, out string authorId, out string hash)
    {
        authorId = "";
        hash = "";
        if (string.IsNullOrEmpty(joinKey))
            return;
        int bar = joinKey.IndexOf('|');
        if (bar <= 0)
            return;
        authorId = joinKey.Substring(0, bar);
        string rest = joinKey.Substring(bar + 1);
        int bar2 = rest.IndexOf('|');
        hash = bar2 >= 0 ? rest.Substring(bar2 + 1) : rest;
    }

    static void EnsureCatalogCharacters(BattleBalanceConfig balance)
    {
        for (int s = 0; s < Stages.Count; s++)
        {
            SharedStageRecord stage = Stages[s];
            if (stage == null || stage.Mine)
                continue;
            for (int i = 0; i < stage.Characters.Count; i++)
                EnsureCatalogCharacter(stage, stage.Characters[i], balance);
        }
        ClearUnownedCatalogNew();
    }

    static void ClearUnownedCatalogNew()
    {
        for (int i = 0; i < Companions.Count; i++)
        {
            CharacterClass character = Companions[i].Character;
            if (character == null || character.OwnedNumMax != 0 || !character.CatalogNew)
                continue;
            character.CatalogNew = false;
            character.CatalogOpened = false;
        }
    }

    public static bool NoteTempDefeated(uint tempId)
    {
        string joinKey = ShareBattleRoster.JoinKey(tempId);
        if (string.IsNullOrEmpty(joinKey))
            return false;
        SharedCompanion companion = FindByJoin(joinKey);
        if (companion == null || companion.Character == null || companion.Character.CatalogOpened)
            return false;
        companion.Character.CatalogOpened = true;
        return true;
    }

    // 読み込んだ他ユーザーのキャラを図鑑に出す。所持は 0 のままなので、編成や合成には入らない。
    static void EnsureCatalogCharacter(SharedStageRecord stage, SharedStageCharacterData data, BattleBalanceConfig balance)
    {
        if (stage == null || data == null || data.CharacterId < CompanionFirstId || string.IsNullOrEmpty(data.PngHash))
            return;
        string joinKey = stage.AuthorId + "|" + data.PngHash;
        if (FindByJoin(joinKey) != null || FindByCharacterId(data.CharacterId) != null)
            return;
        if (Companions.Count >= ShareStageRules.MaxCompanionCount)
            return;
        string source = PngPath(data.PngHash);
        if (!File.Exists(source))
            return;
        string path = CompanionPath(data.CharacterId);
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        if (!File.Exists(path))
            File.Copy(source, path);
        CharacterClass character = new CharacterClass();
        string unique = UniqueName(string.IsNullOrWhiteSpace(data.Name) ? "ななし" : data.Name);
        if (!character.MakeCharacter(path, data.CharacterId, unique) || character.ID != data.CharacterId)
            return;
        character.Level = 1;
        character.FusionCount = 0;
        character.Exp = 0;
        character.OwnedNumCur = 0;
        character.OwnedNumMax = 0;
        character.EnsureStock();
        character.AuthorPersonId = 0;
        character.AuthorSpecified = true;
        character.CatalogOpened = false;
        character.CatalogNew = false;
        CharacterType fileType;
        if (TryReadAttribute(data.Attribute, out fileType))
            character.RecalculateBaseStatsKeepingType(balance, fileType);
        else if (balance != null)
            character.RecalculateBaseStats(balance);
        if (data.CharacterId > HighestCompanionId)
            HighestCompanionId = data.CharacterId;
        Companions.Add(new SharedCompanion { Character = character, JoinKey = joinKey, PngHash = data.PngHash });
    }

    static void FillCharacterIds(SharedStageRecord stage)
    {
        if (stage == null || stage.Mine)
            return;
        for (int i = 0; i < stage.Characters.Count; i++)
        {
            SharedStageCharacterData character = stage.Characters[i];
            uint id = CharacterIds.IdOf(stage.AuthorId, character.PngHash);
            if (id != 0)
                character.CharacterId = id;
        }
    }

    static string UniqueName(string name)
    {
        name = ImportNameRules.Clamp(string.IsNullOrWhiteSpace(name) ? "ななし" : name.Trim());
        if (string.IsNullOrEmpty(name))
            name = "ななし";
        if (!NameTaken(name))
            return name;
        for (int n = 2; n < 100; n++)
        {
            string suffix = n.ToString(CultureInfo.InvariantCulture);
            string stem = name;
            while (stem.Length > 0 && ImportNameRules.Units(stem + suffix) > ImportNameRules.LimitUnits)
                stem = stem.Substring(0, stem.Length - 1);
            string candidate = stem + suffix;
            if (!string.IsNullOrEmpty(candidate) && !NameTaken(candidate))
                return candidate;
        }
        return "共有";
    }

    static bool NameTaken(string name)
    {
        if (ImportedCharacters.NameTaken(name))
            return true;
        for (int i = 0; i < Companions.Count; i++)
        {
            CharacterClass character = Companions[i].Character;
            if (character != null && character.Name == name)
                return true;
        }
        return false;
    }

    static void RestoreCompanion(SharedCompanionRecord record, BattleBalanceConfig balance)
    {
        string path = CompanionPath(record.Id);
        if (!File.Exists(path))
        {
            Debug.LogWarning("共有の仲間の画像が無いので読み飛ばします。ID " + record.Id.ToString(CultureInfo.InvariantCulture));
            return;
        }
        CharacterClass character = new CharacterClass();
        string name = string.IsNullOrWhiteSpace(record.Name) ? "ななし" : record.Name;
        if (!character.MakeCharacter(path, record.Id, name))
            return;
        character.Name = name;
        if (record.OwnedNumCur > BattleBalanceConfig.MaxOwnedCount)
            record.OwnedNumCur = BattleBalanceConfig.MaxOwnedCount;
        character.OwnedNumCur = record.OwnedNumCur;
        character.OwnedNumMax = record.OwnedNumMax < record.OwnedNumCur ? record.OwnedNumCur : record.OwnedNumMax;
        character.Level = record.Level < 1 ? 1 : record.Level;
        character.Exp = record.Exp;
        character.FusionCount = record.FusionCount;
        character.FlagFNT = record.FlagFNT;
        character.Whereabouts = record.Whereabouts == Place.Battle ? Place.None : record.Whereabouts;
        character.CatalogOpened = record.CatalogOpened;
        character.CatalogNew = record.CatalogNew;
        character.AuthorPersonId = 0;
        character.AuthorSpecified = true;
        if (record.HasStock)
        {
            character.Stock.Full = record.OwnedFull > 0 ? record.OwnedFull : 0;
            character.Stock.Eight = record.OwnedEight > 0 ? record.OwnedEight : 0;
            character.Stock.Half = record.OwnedHalf > 0 ? record.OwnedHalf : 0;
            character.Stock.Specified = true;
            character.EnsureStock();
        }
        if (balance != null)
            character.RecalculateBaseStatsKeepingType(balance, record.CharacterType);
        if (record.Id > HighestCompanionId)
            HighestCompanionId = record.Id;
        Companions.Add(new SharedCompanion { Character = character, JoinKey = record.JoinKey, PngHash = record.PngHash });
    }

    static void WriteStage(StreamWriter sw, int index, SharedStageRecord stage)
    {
        if (stage == null)
            return;
        string prefix = "SharedStage[" + index.ToString(CultureInfo.InvariantCulture) + "].";
        sw.WriteLine(prefix + "Key," + Escape(stage.Key));
        sw.WriteLine(prefix + "StageId," + stage.StageId);
        sw.WriteLine(prefix + "AuthorId," + stage.AuthorId);
        sw.WriteLine(prefix + "AuthorName," + Escape(stage.AuthorName));
        sw.WriteLine(prefix + "Name," + Escape(stage.Name));
        sw.WriteLine(prefix + "Comment," + Escape(stage.Comment));
        sw.WriteLine(prefix + "MainR," + stage.MainR.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "MainG," + stage.MainG.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "MainB," + stage.MainB.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "Background," + Escape(stage.Background));
        sw.WriteLine(prefix + "Revision," + stage.Revision.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "CreatedAt," + stage.CreatedAt);
        sw.WriteLine(prefix + "UpdatedAt," + stage.UpdatedAt);
        sw.WriteLine(prefix + "ContentHash," + stage.ContentHash);
        sw.WriteLine(prefix + "FileId," + stage.FileId);
        sw.WriteLine(prefix + "Mine," + (stage.Mine ? "true" : "false"));
        sw.WriteLine(prefix + "ClearedFloor," + stage.ClearedFloor.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "RunBonus," + stage.RunBonus.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "RunCap," + stage.RunCap.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "Stars," + stage.Stars.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "LevelBonus," + stage.LevelBonus.ToString(CultureInfo.InvariantCulture));
        StringBuilder sources = new StringBuilder();
        for (int i = 0; i < stage.SourceIds.Count; i++)
        {
            if (i > 0)
                sources.Append('|');
            sources.Append(stage.SourceIds[i].ToString(CultureInfo.InvariantCulture));
        }
        sw.WriteLine(prefix + "Sources," + sources.ToString());
        for (int i = 0; i < stage.Characters.Count; i++)
        {
            SharedStageCharacterData character = stage.Characters[i];
            string mark = prefix + "Character[" + i.ToString(CultureInfo.InvariantCulture) + "].";
            sw.WriteLine(mark + "Slot," + character.Slot.ToString(CultureInfo.InvariantCulture));
            sw.WriteLine(mark + "Name," + Escape(character.Name));
            sw.WriteLine(mark + "Size," + character.Size.ToString(CultureInfo.InvariantCulture));
            sw.WriteLine(mark + "Level," + character.Level.ToString(CultureInfo.InvariantCulture));
            sw.WriteLine(mark + "Fusion," + character.FusionCount.ToString(CultureInfo.InvariantCulture));
            sw.WriteLine(mark + "Attribute," + character.Attribute);
            sw.WriteLine(mark + "PngHash," + character.PngHash);
            sw.WriteLine(mark + "CharacterId," + character.CharacterId.ToString(CultureInfo.InvariantCulture));
        }
    }

    static void WriteAuthor(StreamWriter sw, int index)
    {
        string prefix = "ShareAuthor[" + index.ToString(CultureInfo.InvariantCulture) + "].";
        sw.WriteLine(prefix + "Id," + CharacterIds.AuthorAt(index));
        for (int i = 0; i < SharedCharacterIds.PerAuthor; i++)
        {
            string hash = CharacterIds.HashAt(index, i);
            if (string.IsNullOrEmpty(hash))
                break;
            sw.WriteLine(prefix + "Hash[" + i.ToString(CultureInfo.InvariantCulture) + "]," + hash);
        }
    }

    static void WriteCompanion(StreamWriter sw, int index, SharedCompanion companion)
    {
        CharacterClass character = companion.Character;
        if (character == null)
            return;
        string prefix = "SharedCompanion[" + index.ToString(CultureInfo.InvariantCulture) + "].";
        character.EnsureStock();
        sw.WriteLine(prefix + "ID," + character.ID.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "Name," + Escape(character.Name));
        sw.WriteLine(prefix + "JoinKey," + Escape(companion.JoinKey));
        sw.WriteLine(prefix + "PngHash," + companion.PngHash);
        sw.WriteLine(prefix + "OwnedNumMax," + character.OwnedNumMax.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "OwnedNumCur," + character.OwnedNumCur.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "Level," + character.Level.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "Exp," + character.Exp.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "FusionCount," + character.FusionCount.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "CharacterType," + character.CharacterType.ToString());
        sw.WriteLine(prefix + "FlagFNT," + (character.FlagFNT ? "true" : "false"));
        sw.WriteLine(prefix + "Whereabouts," + character.Whereabouts.ToString());
        sw.WriteLine(prefix + "CatalogOpened," + (character.CatalogOpened ? "true" : "false"));
        sw.WriteLine(prefix + "CatalogNew," + (character.CatalogNew ? "true" : "false"));
        sw.WriteLine(prefix + "OwnedFull," + character.Stock.Full.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "OwnedEight," + character.Stock.Eight.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine(prefix + "OwnedHalf," + character.Stock.Half.ToString(CultureInfo.InvariantCulture));
    }

    static bool ReadStageField(string key, string value)
    {
        int bracket = key.IndexOf('[');
        int bracketEnd = key.IndexOf(']');
        if (bracket < 0 || bracketEnd <= bracket)
            return true;
        int index;
        if (!int.TryParse(key.Substring(bracket + 1, bracketEnd - bracket - 1), NumberStyles.None, CultureInfo.InvariantCulture, out index) || index < 0 || index > 100)
            return true;
        while (PendingStages.Count <= index)
            PendingStages.Add(new SharedStageRecord());
        SharedStageRecord stage = PendingStages[index];
        string rest = key.Substring(bracketEnd + 1);
        if (rest.StartsWith(".Character[", StringComparison.Ordinal))
        {
            int inner = rest.IndexOf('[', 1);
            int innerEnd = rest.IndexOf(']');
            int characterIndex;
            if (inner < 0 || innerEnd <= inner)
                return true;
            if (!int.TryParse(rest.Substring(inner + 1, innerEnd - inner - 1), NumberStyles.None, CultureInfo.InvariantCulture, out characterIndex) || characterIndex < 0 || characterIndex > 8)
                return true;
            while (stage.Characters.Count <= characterIndex)
                stage.Characters.Add(new SharedStageCharacterData());
            string field = rest.Substring(innerEnd + 2);
            ApplyCharacterField(stage.Characters[characterIndex], field, value);
            return true;
        }
        if (rest.Length < 2 || rest[0] != '.')
            return true;
        ApplyStageField(stage, rest.Substring(1), value);
        return true;
    }

    static void ApplyStageField(SharedStageRecord stage, string field, string value)
    {
        if (field == "Key")
            stage.Key = value;
        else if (field == "StageId")
            stage.StageId = value;
        else if (field == "AuthorId")
            stage.AuthorId = value;
        else if (field == "AuthorName")
            stage.AuthorName = value;
        else if (field == "Name")
            stage.Name = value;
        else if (field == "Comment")
            stage.Comment = value;
        else if (field == "Background")
            stage.Background = value;
        else if (field == "CreatedAt")
            stage.CreatedAt = value;
        else if (field == "UpdatedAt")
            stage.UpdatedAt = value;
        else if (field == "ContentHash")
            stage.ContentHash = value;
        else if (field == "FileId")
            stage.FileId = value;
        else if (field == "Mine")
            stage.Mine = value == "true";
        else if (field == "MainR")
            stage.MainR = ReadInt(value);
        else if (field == "MainG")
            stage.MainG = ReadInt(value);
        else if (field == "MainB")
            stage.MainB = ReadInt(value);
        else if (field == "Revision")
            stage.Revision = ReadInt(value);
        else if (field == "ClearedFloor")
            stage.ClearedFloor = ReadInt(value);
        else if (field == "RunBonus")
            stage.RunBonus = ReadInt(value);
        else if (field == "Stars")
            stage.Stars = ReadInt(value);
        else if (field == "LevelBonus")
            stage.LevelBonus = ReadInt(value);
        else if (field == "RunCap")
            stage.RunCap = ReadInt(value);
        else if (field == "Sources" && !string.IsNullOrEmpty(value))
        {
            string[] parts = value.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                uint id;
                if (uint.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out id))
                    stage.SourceIds.Add(id);
            }
        }
    }

    static void ApplyCharacterField(SharedStageCharacterData character, string field, string value)
    {
        if (field == "Name")
            character.Name = value;
        else if (field == "Attribute")
            character.Attribute = value;
        else if (field == "PngHash")
            character.PngHash = value;
        else if (field == "Slot")
            character.Slot = ReadInt(value);
        else if (field == "Size")
            character.Size = ReadInt(value);
        else if (field == "Level")
            character.Level = ReadInt(value);
        else if (field == "Fusion")
            character.FusionCount = ReadInt(value);
        else if (field == "CharacterId")
        {
            uint id;
            if (uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                character.CharacterId = id;
        }
    }

    static bool ReadAuthorField(string key, string value)
    {
        int bracket = key.IndexOf('[');
        int bracketEnd = key.IndexOf(']');
        if (bracket < 0 || bracketEnd <= bracket)
            return true;
        int index;
        if (!int.TryParse(key.Substring(bracket + 1, bracketEnd - bracket - 1), NumberStyles.None, CultureInfo.InvariantCulture, out index) || index < 0 || index >= SharedCharacterIds.MaxAuthors)
            return true;
        while (PendingAuthors.Count <= index)
            PendingAuthors.Add(new AuthorSave());
        AuthorSave author = PendingAuthors[index];
        string rest = key.Substring(bracketEnd + 1);
        if (rest == ".Id")
        {
            author.AuthorId = value;
            return true;
        }
        if (rest.StartsWith(".Hash[", StringComparison.Ordinal) && rest.EndsWith("]", StringComparison.Ordinal))
        {
            int hashIndex;
            string number = rest.Substring(".Hash[".Length, rest.Length - ".Hash[".Length - 1);
            if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out hashIndex) || hashIndex < 0 || hashIndex >= SharedCharacterIds.PerAuthor)
                return true;
            while (author.Hashes.Count <= hashIndex)
                author.Hashes.Add("");
            author.Hashes[hashIndex] = value;
            return true;
        }
        return true;
    }

    static bool ReadCompanionField(string key, string value)
    {
        int bracket = key.IndexOf('[');
        int bracketEnd = key.IndexOf(']');
        if (bracket < 0 || bracketEnd <= bracket || bracketEnd + 1 >= key.Length)
            return true;
        int index;
        if (!int.TryParse(key.Substring(bracket + 1, bracketEnd - bracket - 1), NumberStyles.None, CultureInfo.InvariantCulture, out index) || index < 0 || index > 2000)
            return true;
        while (PendingCompanions.Count <= index)
            PendingCompanions.Add(new SharedCompanionRecord());
        SharedCompanionRecord record = PendingCompanions[index];
        string field = key.Substring(bracketEnd + 2);
        if (field == "ID")
        {
            uint id;
            if (uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                record.Id = id;
        }
        else if (field == "Name")
            record.Name = value;
        else if (field == "JoinKey")
            record.JoinKey = value;
        else if (field == "PngHash")
            record.PngHash = value;
        else if (field == "OwnedNumMax")
            record.OwnedNumMax = ReadUlong(value);
        else if (field == "OwnedNumCur")
            record.OwnedNumCur = ReadUlong(value);
        else if (field == "Level")
            record.Level = ReadUlong(value);
        else if (field == "Exp")
            record.Exp = ReadUlong(value);
        else if (field == "FusionCount")
            record.FusionCount = ReadUlong(value);
        else if (field == "CharacterType")
        {
            CharacterType type;
            if (Enum.TryParse(value, out type))
                record.CharacterType = type;
        }
        else if (field == "FlagFNT")
            record.FlagFNT = value == "true";
        else if (field == "Whereabouts")
        {
            Place place;
            if (Enum.TryParse(value, out place))
                record.Whereabouts = place;
        }
        else if (field == "CatalogOpened")
            record.CatalogOpened = value == "true";
        else if (field == "CatalogNew")
            record.CatalogNew = value == "true";
        else if (field == "OwnedFull")
        {
            record.OwnedFull = ReadLong(value);
            record.HasStock = true;
        }
        else if (field == "OwnedEight")
        {
            record.OwnedEight = ReadLong(value);
            record.HasStock = true;
        }
        else if (field == "OwnedHalf")
        {
            record.OwnedHalf = ReadLong(value);
            record.HasStock = true;
        }
        return true;
    }

    static int ReadInt(string value)
    {
        int number;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            return 0;
        return number;
    }

    static long ReadLong(string value)
    {
        long number;
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            return 0;
        return number;
    }

    static ulong ReadUlong(string value)
    {
        ulong number;
        if (!ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            return 0;
        return number;
    }

    static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Replace("\\", "\\\\").Replace(",", "{{,}}").Replace("\r", "").Replace("\n", "");
    }

    static string Unescape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Replace("{{,}}", ",").Replace("\\\\", "\\");
    }

    static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;
        try
        {
            Directory.Delete(path, true);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("共有ステージのファイルを消せませんでした。 " + exception.Message);
        }
    }
}

public static class ShareBattleRoster
{
    public const uint FirstTempId = 40001;

    static readonly Dictionary<uint, CharacterClass> Temps = new Dictionary<uint, CharacterClass>();
    static readonly Dictionary<uint, string> Keys = new Dictionary<uint, string>();
    static readonly Dictionary<uint, string> Names = new Dictionary<uint, string>();
    static readonly Dictionary<uint, byte[]> Pngs = new Dictionary<uint, byte[]>();

    public static uint TempId(int slot)
    {
        if (slot < 1)
            slot = 1;
        return FirstTempId + (uint)(slot - 1);
    }

    public static bool IsReady()
    {
        return Temps.Count > 0;
    }

    public static bool IsTemp(uint id)
    {
        return id >= FirstTempId && id < FirstTempId + 9;
    }

    public static CharacterClass Find(uint id)
    {
        CharacterClass character;
        if (!Temps.TryGetValue(id, out character))
            return null;
        return character;
    }

    public static string JoinKey(uint id)
    {
        string key;
        return Keys.TryGetValue(id, out key) ? key : "";
    }

    public static string NameOf(uint id)
    {
        string name;
        return Names.TryGetValue(id, out name) ? name : "";
    }

    public static byte[] Png(uint id)
    {
        byte[] png;
        return Pngs.TryGetValue(id, out png) ? png : null;
    }

    public static void Prepare(SharedStageRecord stage)
    {
        Clear();
        if (stage == null)
            return;
        for (int i = 0; i < stage.Characters.Count; i++)
        {
            SharedStageCharacterData data = stage.Characters[i];
            string path = ShareStageLibrary.PngPath(data.PngHash);
            if (!File.Exists(path))
                continue;
            uint id = TempId(data.Slot);
            CharacterClass character = new CharacterClass();
            if (!character.MakeCharacter(path, id, data.Name))
                continue;
            CharacterType fileType;
            if (ShareStageLibrary.TryReadAttribute(data.Attribute, out fileType))
                character.RecalculateBaseStatsKeepingType(null, fileType);
            character.SetOwnedCur(0, BattleBalanceConfig.MaxOwnedCount);
            Temps[id] = character;
            Names[id] = data.Name;
            byte[] png = File.ReadAllBytes(path);
            Pngs[id] = png;
            Keys[id] = stage.AuthorId + "|" + data.PngHash;
        }
    }

    public static void Clear()
    {
        foreach (KeyValuePair<uint, CharacterClass> pair in Temps)
        {
            if (pair.Value != null && pair.Value.ImageTexture2D != null)
                UnityEngine.Object.Destroy(pair.Value.ImageTexture2D);
        }
        Temps.Clear();
        Keys.Clear();
        Names.Clear();
        Pngs.Clear();
    }
}
