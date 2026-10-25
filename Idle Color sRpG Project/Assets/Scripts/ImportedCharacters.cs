using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// 自作キャラの大きさ制限。あとから条件を変えるときは、この表だけを直す。
// ClearedStage は、そのステージをクリアしたあとに開く。0 は最初から開ける。
public struct ImportSizeUnlock
{
    public int Size;
    public int ClearedStage;
    public string StageName;

    public ImportSizeUnlock(int size, int clearedStage, string stageName)
    {
        Size = size;
        ClearedStage = clearedStage;
        StageName = stageName;
    }
}

public static class ImportSizeRules
{
    public const int MaxCount = 500;

    public static readonly ImportSizeUnlock[] Unlocks =
    {
        new ImportSizeUnlock(8, 0, ""),
        new ImportSizeUnlock(16, 0, ""),
        new ImportSizeUnlock(32, 1, "チュートリアル"),
        new ImportSizeUnlock(64, 2, "スライムの森"),
        new ImportSizeUnlock(128, 3, "チグサとバトル１"),
    };

    public static string Describe(int clearedStage)
    {
        return Describe(stage => clearedStage >= stage);
    }

    public static string Describe(System.Func<int, bool> isStageCleared)
    {
        StringBuilder text = new StringBuilder();
        text.Append("読めるのは、1辺が 8、16、32、64、128 の正方形 PNG だけです。");
        for (int i = 0; i < Unlocks.Length; i++)
        {
            ImportSizeUnlock unlock = Unlocks[i];
            text.Append("\n");
            text.Append(unlock.Size.ToString());
            if (SizeOpened(unlock, isStageCleared))
                text.Append(" … 開放");
            else
                text.Append(" … 未開放（" + unlock.StageName + "をクリアすると開く）");
        }
        return text.ToString();
    }

    public static string RejectSize(int width, int height, int clearedStage)
    {
        return RejectSize(width, height, stage => clearedStage >= stage);
    }

    public static string RejectSize(int width, int height, System.Func<int, bool> isStageCleared)
    {
        if (width != height)
            return "正方形ではありません。横 " + width.ToString() + "、縦 " + height.ToString() + " です。";

        for (int i = 0; i < Unlocks.Length; i++)
        {
            if (Unlocks[i].Size != width)
                continue;
            if (!SizeOpened(Unlocks[i], isStageCleared))
                return width.ToString() + " はまだ開けていません。" + Unlocks[i].StageName + "をクリアすると開きます。";
            return null;
        }
        return "一辺は 8、16、32、64、128 のいずれかにしてください。今は " + width.ToString() + " です。";
    }

    static bool SizeOpened(ImportSizeUnlock unlock, System.Func<int, bool> isStageCleared)
    {
        if (unlock.ClearedStage <= 0)
            return true;
        return isStageCleared != null && isStageCleared(unlock.ClearedStage);
    }
}

public class ImportedCharacterRecord
{
    public uint Id;
    public bool HasId;
    public string Name;
    public bool HasName;
    public string SourceHash;
    public bool HasHash;
    public uint KnownPixels;
    public bool HasKnownPixels;
    public ulong OwnedNumMax;
    public bool HasOwnedNumMax;
    public ulong OwnedNumCur;
    public bool HasOwnedNumCur;
    public ulong ReincarnationTimes;
    public bool HasReincarnationTimes;
    public ulong Level;
    public bool HasLevel;
    public ulong Exp;
    public bool HasExp;
    public ulong FusionCount;
    public bool HasFusionCount;
    public bool FlagFNT;
    public bool HasFlagFNT;
    public Place Whereabouts;
    public bool HasWhereabouts;
    public bool CatalogOpened;
    public bool HasCatalogOpened;
    public int AuthorPersonId;
    public bool HasAuthor;
    public long OwnedFull;
    public bool HasOwnedFull;
    public long OwnedEight;
    public bool HasOwnedEight;
    public long OwnedHalf;
    public bool HasOwnedHalf;
}

public class ImportedCharacterEntry
{
    public CharacterClass Character;
    public string SourceHash;
}

// 自作キャラ。CharactersAll の添字にはしない。ID は 10001 から。
public static class ImportedCharacters
{
    public const uint FirstId = 10001;
    const int MaxPngBytes = 4 * 1024 * 1024;

    static readonly List<ImportedCharacterEntry> Entries = new List<ImportedCharacterEntry>();
    static readonly Dictionary<string, string> RosterHashes = new Dictionary<string, string>();
    static readonly List<ImportedCharacterRecord> Pending = new List<ImportedCharacterRecord>();
    static uint HighestId;

    public static int Count
    {
        get { return Entries.Count; }
    }

    public static int CountOfSize(int size)
    {
        if (size <= 0)
            return 0;
        int count = 0;
        for (int i = 0; i < Entries.Count; i++)
        {
            CharacterClass character = Entries[i].Character;
            if (character != null && character.Size == size)
                count++;
        }
        return count;
    }

    public static int ImportCap = Constants.IMPORT_CHARACTER_CAP_INITIAL;

    public static void SetImportCap(int cap)
    {
        if (cap < Constants.IMPORT_CHARACTER_CAP_INITIAL)
            cap = Constants.IMPORT_CHARACTER_CAP_INITIAL;
        if (cap > ImportSizeRules.MaxCount)
            cap = ImportSizeRules.MaxCount;
        ImportCap = cap;
    }

    public static bool RejectImportCount(out string message)
    {
        int cap = ImportCap;
        if (cap > ImportSizeRules.MaxCount)
            cap = ImportSizeRules.MaxCount;
        if (Entries.Count < cap)
        {
            message = null;
            return false;
        }
        if (cap >= ImportSizeRules.MaxCount)
            message = "自作キャラは " + ImportSizeRules.MaxCount.ToString() + " 体までです。";
        else
            message = "読み込めるキャラは " + cap.ToString() + " 体までです。クエストで上限が上がります。";
        return true;
    }

    public static void ForEach(Action<CharacterClass> visit)
    {
        if (visit == null)
            return;
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].Character != null)
                visit(Entries[i].Character);
        }
    }

    public static CharacterClass Find(uint id)
    {
        if (id < FirstId)
            return null;
        for (int i = 0; i < Entries.Count; i++)
        {
            CharacterClass character = Entries[i].Character;
            if (character != null && character.ID == id)
                return character;
        }
        return null;
    }

    public static CharacterClass FindAny(CharacterClass[] builtIn, uint id)
    {
        if (id >= FirstId)
            return Find(id);
        if (builtIn == null || id >= builtIn.Length)
            return null;
        return builtIn[id];
    }

    public static string OriginalPath(uint id)
    {
        return Application.persistentDataPath + "/Character/Custom/" + id.ToString() + ".png";
    }

    public static bool TryAdd(byte[] pngBytes, string name, int clearedStage, out string message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(name))
        {
            message = "名前を入れてください。";
            return false;
        }
        name = NormalizeName(name);
        if (name.Length == 0)
        {
            message = "名前を入れてください。";
            return false;
        }
        if (NameTaken(name))
        {
            message = "同じ名前は使えません。";
            return false;
        }
        if (RejectImportCount(out message))
            return false;
        if (pngBytes == null || pngBytes.Length < 8)
        {
            message = "ファイルを読めませんでした。";
            return false;
        }
        if (pngBytes.Length > MaxPngBytes)
        {
            message = "ファイルが大きすぎます。";
            return false;
        }
        if (!IsPng(pngBytes))
        {
            message = "PNGではありません。";
            return false;
        }

        Texture2D probe = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!probe.LoadImage(pngBytes))
        {
            UnityEngine.Object.Destroy(probe);
            message = "PNGを読めませんでした。";
            return false;
        }
        string sizeReason = ImportSizeRules.RejectSize(probe.width, probe.height, clearedStage);
        UnityEngine.Object.Destroy(probe);
        if (sizeReason != null)
        {
            message = sizeReason;
            return false;
        }

        string hash = HashPosterizedPng(pngBytes);
        if (RejectKnownImage(hash, out message))
            return false;

        uint id = NextId();
        string directory = Application.persistentDataPath + "/Character/Custom";
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        string path = OriginalPath(id);
        File.WriteAllBytes(path, pngBytes);

        CharacterClass character = new CharacterClass();
        if (!character.MakeCharacter(path, id, name) || character.ID != id)
        {
            DeleteImportFiles(id);
            message = "画像をキャラにできませんでした。";
            return false;
        }
        AssignImportedAuthor(character);

        if (id > HighestId)
            HighestId = id;
        Entries.Add(new ImportedCharacterEntry { Character = character, SourceHash = hash });
        message = name + " を読み込みました。";
        return true;
    }

    static void AssignImportedAuthor(CharacterClass character)
    {
        if (character == null)
            return;
        character.AuthorPersonId = PersonIds.Protagonist;
        character.AuthorSpecified = true;
        character.EnsureStock();
    }

    class PreparedImport
    {
        public uint Id;
        public string SourceHash;
        public CharacterClass Character;
    }

    static PreparedImport Prepared;

    public static CharacterClass PreparedCharacter()
    {
        return Prepared != null ? Prepared.Character : null;
    }

    // 大きさ・重複を見て画像を作り、ステータスまで計算する。所持にはまだ加えない。
    public static bool TryPrepare(byte[] pngBytes, int clearedStage, BattleBalanceConfig balance, out string message)
    {
        return TryPrepare(pngBytes, stage => clearedStage >= stage, balance, out message);
    }

    public static bool TryPrepare(byte[] pngBytes, System.Func<int, bool> isStageCleared, BattleBalanceConfig balance, out string message)
    {
        DiscardPrepared();
        message = null;
        if (RejectImportCount(out message))
            return false;
        if (pngBytes == null || pngBytes.Length < 8)
        {
            message = "ファイルを読めませんでした。";
            return false;
        }
        if (pngBytes.Length > MaxPngBytes)
        {
            message = "ファイルが大きすぎます。";
            return false;
        }
        if (!IsPng(pngBytes))
        {
            message = "PNGではありません。";
            return false;
        }

        Texture2D probe = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!probe.LoadImage(pngBytes))
        {
            UnityEngine.Object.Destroy(probe);
            message = "PNGを読めませんでした。";
            return false;
        }
        string sizeReason = ImportSizeRules.RejectSize(probe.width, probe.height, isStageCleared);
        UnityEngine.Object.Destroy(probe);
        if (sizeReason != null)
        {
            message = sizeReason;
            return false;
        }

        string hash = HashPosterizedPng(pngBytes);
        if (RejectKnownImage(hash, out message))
            return false;

        uint id = NextId();
        string directory = Application.persistentDataPath + "/Character/Custom";
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(OriginalPath(id), pngBytes);

        CharacterClass character = new CharacterClass();
        if (!character.MakeCharacter(OriginalPath(id), id, "ななし") || character.ID != id)
        {
            DeleteImportFiles(id);
            message = "画像をキャラにできませんでした。";
            return false;
        }
        character.RecalculateBaseStats(balance);
        AssignImportedAuthor(character);
        Prepared = new PreparedImport { Id = id, SourceHash = hash, Character = character };
        message = null;
        return true;
    }

    public static bool CommitPrepared(string name, out string message)
    {
        message = null;
        if (Prepared == null || Prepared.Character == null)
        {
            message = "画像をキャラにできませんでした。";
            return false;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            message = "名前を入れてください。";
            return false;
        }
        name = NormalizeName(name);
        if (name.Length == 0)
        {
            message = "名前を入れてください。";
            return false;
        }
        if (NameTaken(name))
        {
            message = "同じ名前は使えません。";
            return false;
        }
        if (RejectImportCount(out message))
            return false;

        CharacterClass character = Prepared.Character;
        character.Name = name;
        if (Prepared.Id > HighestId)
            HighestId = Prepared.Id;
        Entries.Add(new ImportedCharacterEntry { Character = character, SourceHash = Prepared.SourceHash });
        Prepared = null;
        message = name + " を読み込みました。";
        return true;
    }

    public static void DiscardPrepared()
    {
        if (Prepared == null)
            return;
        uint id = Prepared.Id;
        CharacterClass character = Prepared.Character;
        Prepared = null;
        if (character != null && character.ImageTexture2D != null)
            UnityEngine.Object.Destroy(character.ImageTexture2D);
        DeleteImportFiles(id);
    }

    // 所持から外す。ID は再利用しない。枚数は Entries の数なので、消すと読み込み枠が戻る。
    public static bool TryRemove(uint id)
    {
        if (id < FirstId)
            return false;
        for (int i = 0; i < Entries.Count; i++)
        {
            CharacterClass character = Entries[i].Character;
            if (character == null || character.ID != id)
                continue;
            if (character.ImageTexture2D != null)
                UnityEngine.Object.Destroy(character.ImageTexture2D);
            Entries.RemoveAt(i);
            DeleteImportFiles(id);
            return true;
        }
        return false;
    }

    public static void BeginLoad()
    {
        Pending.Clear();
        HighestId = FirstId - 1;
    }

    // 起動時の読み直し。画像ファイルは消さない。
    public static void ClearForReload()
    {
        Entries.Clear();
        Pending.Clear();
        HighestId = FirstId - 1;
        Prepared = null;
    }

    public static bool TryReadSaveLine(string line)
    {
        if (string.IsNullOrEmpty(line) || !line.StartsWith("ImportedCharacter["))
            return false;

        int comma = line.IndexOf(',');
        if (comma <= 0)
            return true;

        string key = line.Substring(0, comma);
        string value = Unescape(line.Substring(comma + 1));
        int bracket = key.IndexOf('[');
        int bracketEnd = key.IndexOf(']');
        if (bracket < 0 || bracketEnd <= bracket)
            return true;

        int index;
        if (!int.TryParse(key.Substring(bracket + 1, bracketEnd - bracket - 1), out index) || index < 0)
            return true;
        if (bracketEnd + 1 >= key.Length || key[bracketEnd + 1] != '.')
            return true;

        while (Pending.Count <= index)
            Pending.Add(new ImportedCharacterRecord());
        ImportedCharacterRecord record = Pending[index];
        string field = key.Substring(bracketEnd + 2);
        ApplyField(record, field, value);
        return true;
    }

    public static void WriteSave(StreamWriter sw)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            CharacterClass character = Entries[i].Character;
            if (character == null || character.ID < FirstId)
                continue;
            string prefix = "ImportedCharacter[" + i.ToString() + "].";
            sw.WriteLine(prefix + "ID," + character.ID.ToString());
            sw.WriteLine(prefix + "Name," + Escape(character.Name));
            sw.WriteLine(prefix + "SourceHash," + (Entries[i].SourceHash ?? ""));
            sw.WriteLine(prefix + "KnownPixels," + character.KnownPixels.ToString());
            sw.WriteLine(prefix + "OwnedNumMax," + character.OwnedNumMax.ToString());
            sw.WriteLine(prefix + "OwnedNumCur," + character.OwnedNumCur.ToString());
            sw.WriteLine(prefix + "ReincarnationTimes," + character.ReincarnationTimes.ToString());
            sw.WriteLine(prefix + "Level," + character.Level.ToString());
            sw.WriteLine(prefix + "Exp," + character.Exp.ToString());
            sw.WriteLine(prefix + "FusionCount," + character.FusionCount.ToString());
            sw.WriteLine(prefix + "FlagFNT," + (character.FlagFNT ? "true" : "false"));
            sw.WriteLine(prefix + "Whereabouts," + character.Whereabouts.ToString());
            sw.WriteLine(prefix + "CatalogOpened," + (character.CatalogOpened ? "true" : "false"));
            character.EnsureStock();
            sw.WriteLine(prefix + "AuthorPersonId," + character.AuthorPersonId.ToString());
            sw.WriteLine(prefix + "OwnedFull," + character.Stock.Full.ToString());
            sw.WriteLine(prefix + "OwnedEight," + character.Stock.Eight.ToString());
            sw.WriteLine(prefix + "OwnedHalf," + character.Stock.Half.ToString());
        }
    }

    // レベルを 1 始まりにする前のセーブは、0 が基礎だった。1 足して強さを保つ。
    public static void RaisePendingLevels()
    {
        for (int i = 0; i < Pending.Count; i++)
        {
            ImportedCharacterRecord record = Pending[i];
            if (record == null || !record.HasLevel || record.Level >= ulong.MaxValue)
                continue;
            record.Level++;
        }
    }

    public static void Restore(BattleBalanceConfig balance)
    {
        Entries.Clear();
        for (int i = 0; i < Pending.Count; i++)
        {
            ImportedCharacterRecord record = Pending[i];
            if (record == null || !record.HasId || record.Id < FirstId)
                continue;
            if (record.Id > HighestId)
                HighestId = record.Id;
            if (Find(record.Id) != null)
                continue;

            string path = OriginalPath(record.Id);
            if (!File.Exists(path))
            {
                Debug.LogWarning("自作キャラの画像が無いので読み飛ばします。ID " + record.Id.ToString());
                continue;
            }

            string name = record.HasName ? record.Name : "";
            if (string.IsNullOrWhiteSpace(name))
                name = "ななし";

            CharacterClass character = new CharacterClass();
            if (!character.MakeCharacter(path, record.Id, name) || character.ID != record.Id)
            {
                Debug.LogWarning("自作キャラの画像をキャラにできませんでした。ID " + record.Id.ToString());
                continue;
            }

            if (record.HasKnownPixels)
                character.KnownPixels = record.KnownPixels;
            if (record.HasOwnedNumMax)
                character.OwnedNumMax = record.OwnedNumMax;
            if (record.HasOwnedNumCur)
            {
                character.OwnedNumCur = record.OwnedNumCur;
                if (character.OwnedNumCur > BattleBalanceConfig.MaxOwnedCount)
                {
                    if (character.OwnedNumMax < character.OwnedNumCur)
                        character.OwnedNumMax = character.OwnedNumCur;
                    character.OwnedNumCur = BattleBalanceConfig.MaxOwnedCount;
                }
            }
            character.RaiseOwnedMax();
            if (record.HasReincarnationTimes)
                character.ReincarnationTimes = record.ReincarnationTimes;
            if (record.HasLevel)
                character.Level = record.Level;
            if (record.HasExp)
                character.Exp = record.Exp;
            if (record.HasFusionCount)
                character.FusionCount = record.FusionCount;
            if (record.HasFlagFNT)
                character.FlagFNT = record.FlagFNT;
            if (record.HasWhereabouts)
                character.Whereabouts = record.Whereabouts;
            if (record.HasCatalogOpened)
                character.CatalogOpened = record.CatalogOpened;
            if (record.HasAuthor)
                character.AuthorPersonId = record.AuthorPersonId;
            else if (character.ID >= FirstId)
                character.AuthorPersonId = PersonIds.Protagonist;
            character.AuthorSpecified = true;
            if (record.HasOwnedFull || record.HasOwnedEight || record.HasOwnedHalf)
            {
                character.Stock.Full = record.HasOwnedFull && record.OwnedFull > 0 ? record.OwnedFull : 0;
                character.Stock.Eight = record.HasOwnedEight && record.OwnedEight > 0 ? record.OwnedEight : 0;
                character.Stock.Half = record.HasOwnedHalf && record.OwnedHalf > 0 ? record.OwnedHalf : 0;
                character.Stock.Specified = true;
            }
            character.EnsureStock();
            if (record.HasName && !string.IsNullOrWhiteSpace(record.Name))
                character.Name = record.Name.Trim();

            character.RecalculateBaseStats(balance);

            string hash = HashPosterizedFile(path);
            if (string.IsNullOrEmpty(hash) && record.HasHash)
                hash = record.SourceHash;
            Entries.Add(new ImportedCharacterEntry { Character = character, SourceHash = hash });
        }
        Pending.Clear();
    }

    static void ApplyField(ImportedCharacterRecord record, string field, string value)
    {
        if (field == "ID")
        {
            uint id;
            if (uint.TryParse(value, out id))
            {
                record.Id = id;
                record.HasId = true;
                if (id > HighestId)
                    HighestId = id;
            }
            return;
        }
        if (field == "Name")
        {
            record.Name = value;
            record.HasName = true;
            return;
        }
        if (field == "SourceHash")
        {
            record.SourceHash = value;
            record.HasHash = true;
            return;
        }
        if (field == "KnownPixels")
        {
            uint number;
            if (uint.TryParse(value, out number))
            {
                record.KnownPixels = number;
                record.HasKnownPixels = true;
            }
            return;
        }
        if (field == "OwnedNumMax")
        {
            ulong number;
            if (ulong.TryParse(value, out number))
            {
                record.OwnedNumMax = number;
                record.HasOwnedNumMax = true;
            }
            return;
        }
        if (field == "OwnedNumCur")
        {
            ulong number;
            if (ulong.TryParse(value, out number))
            {
                record.OwnedNumCur = number;
                record.HasOwnedNumCur = true;
            }
            return;
        }
        if (field == "ReincarnationTimes")
        {
            ulong number;
            if (ulong.TryParse(value, out number))
            {
                record.ReincarnationTimes = number;
                record.HasReincarnationTimes = true;
            }
            return;
        }
        if (field == "Level")
        {
            ulong number;
            if (ulong.TryParse(value, out number))
            {
                record.Level = number;
                record.HasLevel = true;
            }
            return;
        }
        if (field == "Exp")
        {
            ulong number;
            if (ulong.TryParse(value, out number))
            {
                record.Exp = number;
                record.HasExp = true;
            }
            return;
        }
        if (field == "FusionCount")
        {
            ulong number;
            if (ulong.TryParse(value, out number))
            {
                record.FusionCount = number;
                record.HasFusionCount = true;
            }
            return;
        }
        if (field == "FlagFNT")
        {
            record.FlagFNT = value == "true";
            record.HasFlagFNT = true;
            return;
        }
        if (field == "Whereabouts")
        {
            Place place;
            if (TryParsePlace(value, out place))
            {
                record.Whereabouts = place;
                record.HasWhereabouts = true;
            }
            return;
        }
        if (field == "CatalogOpened")
        {
            record.CatalogOpened = value == "true";
            record.HasCatalogOpened = true;
        }
        if (field == "AuthorPersonId")
        {
            int author;
            if (int.TryParse(value, out author) && author > 0)
                record.AuthorPersonId = author;
            else
                record.AuthorPersonId = 0;
            record.HasAuthor = true;
        }
        if (field == "OwnedFull")
        {
            long number;
            if (long.TryParse(value, out number) && number > 0)
                record.OwnedFull = number;
            record.HasOwnedFull = true;
        }
        if (field == "OwnedEight")
        {
            long number;
            if (long.TryParse(value, out number) && number > 0)
                record.OwnedEight = number;
            record.HasOwnedEight = true;
        }
        if (field == "OwnedHalf")
        {
            long number;
            if (long.TryParse(value, out number) && number > 0)
                record.OwnedHalf = number;
            record.HasOwnedHalf = true;
        }
    }

    static bool TryParsePlace(string value, out Place place)
    {
        switch (value)
        {
            case "None": place = Place.None; return true;
            case "CreateR": place = Place.CreateR; return true;
            case "CreateG": place = Place.CreateG; return true;
            case "CreateB": place = Place.CreateB; return true;
            case "CreatePixel": place = Place.CreatePixel; return true;
            case "CreateCharacter": place = Place.CreateCharacter; return true;
            case "Hospital": place = Place.Hospital; return true;
            case "Battle": place = Place.Battle; return true;
            default:
                place = Place.None;
                return false;
        }
    }

    static uint NextId()
    {
        uint next = HighestId >= FirstId ? HighestId + 1 : FirstId;
        for (int i = 0; i < Entries.Count; i++)
        {
            CharacterClass character = Entries[i].Character;
            if (character != null && character.ID >= next)
                next = character.ID + 1;
        }
        return next;
    }

    static bool ContainsHash(string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].SourceHash == hash)
                return true;
        }
        return false;
    }

    static bool RejectKnownImage(string hash, out string message)
    {
        message = null;
        if (string.IsNullOrEmpty(hash))
        {
            message = "画像をキャラにできませんでした。";
            return true;
        }
        if (ContainsHash(hash))
        {
            message = "同じ画像をすでに読み込んでいます。";
            return true;
        }
        if (MatchesRosterImage(hash))
        {
            message = "この絵はモンスターと同じです。";
            return true;
        }
        return false;
    }

    public static bool NameTaken(string name)
    {
        string key = NormalizeName(name);
        if (key.Length == 0)
            return false;
        for (int i = 0; i < Entries.Count; i++)
        {
            CharacterClass character = Entries[i].Character;
            if (character != null && SameName(character.Name, key))
                return true;
        }
        CharacterDefinition[] roster = CharacterRoster.All;
        for (int i = 0; i < roster.Length; i++)
        {
            if (roster[i] != null && SameName(roster[i].DisplayName, key))
                return true;
        }
        return false;
    }

    static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";
        return name.Trim().Replace("\r", "").Replace("\n", "");
    }

    static bool SameName(string a, string b)
    {
        return string.Equals(NormalizeName(a), NormalizeName(b), StringComparison.OrdinalIgnoreCase);
    }

    static bool MatchesRosterImage(string hash)
    {
        CharacterDefinition[] roster = CharacterRoster.All;
        for (int i = 0; i < roster.Length; i++)
        {
            CharacterDefinition definition = roster[i];
            if (definition == null || string.IsNullOrEmpty(definition.FileName))
                continue;
            string known;
            if (!RosterHashes.TryGetValue(definition.FileName, out known))
            {
                known = HashPosterizedFile(RosterImagePath(definition.FileName));
                if (string.IsNullOrEmpty(known))
                    continue;
                RosterHashes[definition.FileName] = known;
            }
            if (known == hash)
                return true;
        }
        return false;
    }

    static string RosterImagePath(string fileName)
    {
        string persistent = Application.persistentDataPath + "/Character/" + fileName;
        if (File.Exists(persistent))
            return persistent;
        string streaming = Application.streamingAssetsPath + "/Character/" + fileName;
        if (File.Exists(streaming))
            return streaming;
        return "";
    }

    static void DeleteImportFiles(uint id)
    {
        string path = OriginalPath(id);
        if (File.Exists(path))
            File.Delete(path);
        string normalized = Application.persistentDataPath + "/Character/Custom/Nomalization/Nomalization_" + id.ToString() + ".png";
        if (File.Exists(normalized))
            File.Delete(normalized);
    }

    static string HashPosterizedFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return "";
            return HashPosterizedPng(File.ReadAllBytes(path));
        }
        catch (Exception)
        {
            return "";
        }
    }

    // 同じ画像かは、元のバイト列ではなく、保存する減色後の画素で見る。
    static string HashPosterizedPng(byte[] pngBytes)
    {
        if (pngBytes == null || pngBytes.Length < 8)
            return "";

        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!source.LoadImage(pngBytes))
        {
            UnityEngine.Object.Destroy(source);
            return "";
        }

        Texture2D posterized = CharacterClass.MakePosterizedTexture(source);
        UnityEngine.Object.Destroy(source);
        if (posterized == null)
            return "";

        Color32[] pixels = posterized.GetPixels32();
        UnityEngine.Object.Destroy(posterized);
        if (pixels == null || pixels.Length == 0)
            return "";

        byte[] raw = new byte[pixels.Length * 4];
        for (int i = 0; i < pixels.Length; i++)
        {
            raw[i * 4] = pixels[i].r;
            raw[i * 4 + 1] = pixels[i].g;
            raw[i * 4 + 2] = pixels[i].b;
            raw[i * 4 + 3] = pixels[i].a;
        }
        return Sha256(raw);
    }

    public static bool IsPng(byte[] bytes)
    {
        return bytes != null
            && bytes.Length >= 8
            && bytes[0] == 137
            && bytes[1] == 80
            && bytes[2] == 78
            && bytes[3] == 71
            && bytes[4] == 13
            && bytes[5] == 10
            && bytes[6] == 26
            && bytes[7] == 10;
    }

    public static string Sha256(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(bytes);
            StringBuilder text = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                text.Append(hash[i].ToString("x2"));
            return text.ToString();
        }
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
}
