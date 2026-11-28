using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

public sealed class PiquaCharacter
{
    public int Slot;
    public string Name = "";
    public string FileName = "";
    public int Size;
    public int Level = 1;
    public int FusionCount;
    public string Attribute = "None";
    public long Hp;
    public long Atk;
    public long Def;
    public int Spd;
    public long Luc;
    public long Obs;
    public uint SourceId;
    public byte[] Png;
}

public sealed class PiquaDocument
{
    public int FormatVersion = ShareStageRules.FormatVersion;
    public string GameVersion = ShareStageRules.GameVersion;
    public string FileId = "";
    public string StageId = "";
    public string AuthorId = "";
    public string AuthorName = "";
    public string CreatedAt = "";
    public string UpdatedAt = "";
    public int Revision = 1;
    public string ContentHash = "";
    public string StageName = "";
    public string Comment = "";
    public int MainR;
    public int MainG;
    public int MainB;
    public string Background = "";
    public int FloorCount = ShareStageRules.FloorCount;
    public readonly List<PiquaCharacter> Characters = new List<PiquaCharacter>();
}

// .piqua は ZIP。中身は manifest.json、stage.json、characters/character_00N.png。
public static class PiquaCodec
{
    public static byte[] Build(PiquaDocument document)
    {
        if (document == null)
            throw new ArgumentNullException("document");
        document.ContentHash = HashContent(document);
        byte[] manifest = Encoding.UTF8.GetBytes(ManifestJson(document));
        byte[] stage = Encoding.UTF8.GetBytes(StageJson(document));
        using (MemoryStream memory = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
            {
                WriteEntry(archive, "manifest.json", manifest);
                WriteEntry(archive, "stage.json", stage);
                for (int i = 0; i < document.Characters.Count; i++)
                {
                    PiquaCharacter character = document.Characters[i];
                    WriteEntry(archive, character.FileName, character.Png);
                }
            }
            return memory.ToArray();
        }
    }

    public static bool TryParse(byte[] zip, out PiquaDocument document, out string error)
    {
        document = null;
        error = null;
        if (zip == null || zip.Length < 22)
        {
            error = "ファイルが破損しています。";
            return false;
        }
        if (zip.Length > ShareStageRules.MaxZipBytes)
        {
            error = "ファイル容量が上限を超えています。";
            return false;
        }

        Dictionary<string, byte[]> files;
        if (!TryUnzip(zip, out files, out error))
            return false;
        byte[] manifestBytes;
        byte[] stageBytes;
        if (!files.TryGetValue("manifest.json", out manifestBytes) || !files.TryGetValue("stage.json", out stageBytes))
        {
            error = "対応していないファイル形式です。";
            return false;
        }

        PiquaDocument parsed = new PiquaDocument();
        if (!ReadManifest(manifestBytes, parsed, out error))
            return false;
        if (!ReadStage(stageBytes, files, parsed, out error))
            return false;
        string actual = HashContent(parsed);
        if (!string.Equals(actual, parsed.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            error = "ファイルが破損しています。";
            return false;
        }
        document = parsed;
        return true;
    }

    public static string HashContent(PiquaDocument document)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] stage = Encoding.UTF8.GetBytes(StageJson(document));
            sha.TransformBlock(stage, 0, stage.Length, null, 0);
            List<PiquaCharacter> ordered = Ordered(document);
            for (int i = 0; i < ordered.Count; i++)
            {
                byte[] name = Encoding.UTF8.GetBytes(ordered[i].FileName + "\n");
                sha.TransformBlock(name, 0, name.Length, null, 0);
                byte[] png = ordered[i].Png ?? new byte[0];
                sha.TransformBlock(png, 0, png.Length, null, 0);
            }
            sha.TransformFinalBlock(new byte[0], 0, 0);
            return ToHex(sha.Hash);
        }
    }

    static bool TryUnzip(byte[] zip, out Dictionary<string, byte[]> files, out string error)
    {
        files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        error = null;
        int total = 0;
        try
        {
            using (MemoryStream input = new MemoryStream(zip, false))
            using (ZipArchive archive = new ZipArchive(input, ZipArchiveMode.Read, false))
            {
                if (archive.Entries.Count > ShareStageRules.MaxEntries)
                {
                    error = "対応していないファイル形式です。";
                    return false;
                }
                for (int i = 0; i < archive.Entries.Count; i++)
                {
                    ZipArchiveEntry entry = archive.Entries[i];
                    string name = entry.FullName.Replace('\\', '/');
                    if (name.EndsWith("/", StringComparison.Ordinal))
                        continue;
                    if (!ShareStageRules.IsSafeEntryName(name) || files.ContainsKey(name))
                    {
                        error = "対応していないファイル形式です。";
                        return false;
                    }
                    if (entry.Length > ShareStageRules.MaxPngBytes && name != "manifest.json" && name != "stage.json")
                    {
                        error = "ファイル容量が上限を超えています。";
                        return false;
                    }
                    if (entry.CompressedLength > 0 && entry.Length / entry.CompressedLength > ShareStageRules.MaxCompressionRatio)
                    {
                        error = "ファイルが破損しています。";
                        return false;
                    }
                    int cap = name.EndsWith(".png", StringComparison.Ordinal) ? ShareStageRules.MaxPngBytes : 256 * 1024;
                    byte[] body = ReadCapped(entry, ref total, cap);
                    if (body == null)
                    {
                        error = "ファイル容量が上限を超えています。";
                        return false;
                    }
                    files[name] = body;
                }
            }
        }
        catch (Exception)
        {
            error = "ファイルが破損しています。";
            return false;
        }
        return true;
    }

    static byte[] ReadCapped(ZipArchiveEntry entry, ref int total, int cap)
    {
        using (Stream input = entry.Open())
        using (MemoryStream memory = new MemoryStream())
        {
            byte[] buffer = new byte[8192];
            int got = 0;
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                got += read;
                if (got > cap || total + got > ShareStageRules.MaxUncompressedBytes)
                    return null;
                memory.Write(buffer, 0, read);
            }
            total += got;
            return memory.ToArray();
        }
    }

    static bool ReadManifest(byte[] json, PiquaDocument document, out string error)
    {
        error = null;
        Dictionary<string, JsonValue> root;
        if (!JsonValue.TryObject(json, out root))
        {
            error = "ファイルが破損しています。";
            return false;
        }
        document.FormatVersion = root.ContainsKey("formatVersion") ? root["formatVersion"].Int() : 0;
        if (document.FormatVersion > ShareStageRules.FormatVersion)
        {
            error = "アプリのバージョンが古いため、このステージを読めません。";
            return false;
        }
        if (document.FormatVersion < ShareStageRules.MinFormatVersion)
        {
            error = "対応していないファイル形式です。";
            return false;
        }
        document.GameVersion = root.ContainsKey("gameVersion") ? root["gameVersion"].Text() : "";
        document.FileId = root.ContainsKey("fileId") ? root["fileId"].Text() : "";
        document.StageId = root.ContainsKey("stageId") ? root["stageId"].Text() : "";
        document.AuthorId = root.ContainsKey("authorId") ? root["authorId"].Text() : "";
        document.AuthorName = root.ContainsKey("authorName") ? root["authorName"].Text() : "";
        document.CreatedAt = root.ContainsKey("createdAt") ? root["createdAt"].Text() : "";
        document.UpdatedAt = root.ContainsKey("updatedAt") ? root["updatedAt"].Text() : "";
        document.Revision = root.ContainsKey("revision") ? root["revision"].Int() : 0;
        document.ContentHash = root.ContainsKey("contentHash") ? root["contentHash"].Text() : "";
        return ValidateIdentity(document, out error);
    }

    static bool ValidateIdentity(PiquaDocument document, out string error)
    {
        error = null;
        if (string.IsNullOrEmpty(document.GameVersion) || ShareStageRules.CompareGameVersion(document.GameVersion, "0.0.0") == 0)
        {
            error = "対応していないファイル形式です。";
            return false;
        }
        if (ShareStageRules.CompareGameVersion(document.GameVersion, ShareStageRules.GameVersion) > 0)
        {
            error = "アプリのバージョンが古いため、このステージを読めません。";
            return false;
        }
        if (!ShareStageRules.IsUuid(document.FileId) || !ShareStageRules.IsUuid(document.StageId) || !ShareStageRules.IsUuid(document.AuthorId))
        {
            error = "対応していないファイル形式です。";
            return false;
        }
        if (document.Revision < 1 || document.Revision > 1000000)
        {
            error = "対応していないファイル形式です。";
            return false;
        }
        DateTime created;
        DateTime updated;
        if (!ShareStageRules.TryParseInstant(document.CreatedAt, out created) || !ShareStageRules.TryParseInstant(document.UpdatedAt, out updated))
        {
            error = "ファイルが破損しています。";
            return false;
        }
        if (updated < created)
        {
            error = "ファイルが破損しています。";
            return false;
        }
        if (string.IsNullOrEmpty(document.ContentHash) || document.ContentHash.Length != 64)
        {
            error = "ファイルが破損しています。";
            return false;
        }
        if (ShareStageRules.HasControl(document.AuthorName) || document.AuthorName == null || document.AuthorName.Length > ShareStageRules.MaxAuthorChars)
        {
            error = "対応していないファイル形式です。";
            return false;
        }
        return true;
    }

    static bool ReadStage(byte[] json, Dictionary<string, byte[]> files, PiquaDocument document, out string error)
    {
        error = null;
        Dictionary<string, JsonValue> root;
        if (!JsonValue.TryObject(json, out root))
        {
            error = "ファイルが破損しています。";
            return false;
        }
        document.StageName = root.ContainsKey("name") ? root["name"].Text() : "";
        document.Comment = root.ContainsKey("comment") ? root["comment"].Text() : "";
        document.Background = root.ContainsKey("background") ? root["background"].Text() : "";
        document.FloorCount = root.ContainsKey("floorCount") ? root["floorCount"].Int() : 0;
        if (document.StageName == null || ShareStageRules.HasControl(document.StageName) || document.StageName.Length > ShareStageRules.MaxNameChars)
        {
            error = "対応していないファイル形式です。";
            return false;
        }
        if (ShareStageRules.HasControl(document.Comment) || document.Comment.Length > ShareStageRules.MaxCommentChars)
        {
            error = "対応していないファイル形式です。";
            return false;
        }
        if (!ShareStageRules.IsBackground(document.Background) || document.FloorCount != ShareStageRules.FloorCount)
        {
            error = "対応していないファイル形式です。";
            return false;
        }
        Dictionary<string, JsonValue> color;
        if (!root.ContainsKey("mainColor") || !root["mainColor"].TryObject(out color))
        {
            error = "対応していないファイル形式です。";
            return false;
        }
        document.MainR = color.ContainsKey("r") ? color["r"].Int() : -1;
        document.MainG = color.ContainsKey("g") ? color["g"].Int() : -1;
        document.MainB = color.ContainsKey("b") ? color["b"].Int() : -1;
        if (!InByte(document.MainR) || !InByte(document.MainG) || !InByte(document.MainB))
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        List<JsonValue> list;
        if (!root.ContainsKey("characters") || !root["characters"].TryArray(out list))
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        if (list.Count < ShareStageRules.MinCharacters || list.Count > ShareStageRules.MaxCharacters)
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        for (int i = 0; i < list.Count; i++)
        {
            Dictionary<string, JsonValue> item;
            if (!list[i].TryObject(out item))
            {
                error = "不正なキャラクターデータです。";
                return false;
            }
            PiquaCharacter character = new PiquaCharacter();
            character.Slot = item.ContainsKey("slot") ? item["slot"].Int() : 0;
            character.Name = item.ContainsKey("name") ? item["name"].Text() : "";
            character.FileName = item.ContainsKey("file") ? item["file"].Text() : "";
            character.Size = item.ContainsKey("size") ? item["size"].Int() : 0;
            character.Level = item.ContainsKey("level") ? item["level"].Int() : 0;
            character.FusionCount = item.ContainsKey("fusionCount") ? item["fusionCount"].Int() : -1;
            character.Attribute = item.ContainsKey("attribute") ? item["attribute"].Text() : "";
            character.Hp = item.ContainsKey("hp") ? item["hp"].Long() : -1;
            character.Atk = item.ContainsKey("atk") ? item["atk"].Long() : -1;
            character.Def = item.ContainsKey("def") ? item["def"].Long() : -1;
            character.Spd = item.ContainsKey("spd") ? item["spd"].Int() : -1;
            character.Luc = item.ContainsKey("luc") ? item["luc"].Long() : -1;
            character.Obs = item.ContainsKey("obs") ? item["obs"].Long() : -1;
            if (item.ContainsKey("sourceId"))
            {
                int raw = item["sourceId"].Int();
                if (raw < 0)
                {
                    error = "不正なキャラクターデータです。";
                    return false;
                }
                character.SourceId = (uint)raw;
            }
            if (document.FormatVersion >= 2)
            {
                if (!ShareStageRules.IsAuthorCharacterId(character.SourceId))
                {
                    error = "不正なキャラクターデータです。";
                    return false;
                }
            }
            else
                character.SourceId = 0;
            if (!ValidCharacter(character, out error))
                return false;
            byte[] png;
            if (!files.TryGetValue(character.FileName, out png) || !LooksLikePng(png, character.Size))
            {
                error = "不正なキャラクターデータです。";
                return false;
            }
            character.Png = png;
            document.Characters.Add(character);
        }
        document.Characters.Sort((a, b) => a.Slot.CompareTo(b.Slot));
        for (int i = 0; i < document.Characters.Count; i++)
        {
            if (document.Characters[i].Slot != i + 1)
            {
                error = "不正なキャラクターデータです。";
                return false;
            }
        }
        if (document.FormatVersion >= 2)
        {
            for (int i = 0; i < document.Characters.Count; i++)
            {
                uint sourceId = document.Characters[i].SourceId;
                for (int j = 0; j < i; j++)
                {
                    if (document.Characters[j].SourceId != sourceId)
                        continue;
                    error = "不正なキャラクターデータです。";
                    return false;
                }
            }
        }
        return true;
    }

    static bool ValidCharacter(PiquaCharacter character, out string error)
    {
        error = null;
        if (character.Slot < 1 || character.Slot > ShareStageRules.MaxCharacters)
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        if (character.FileName != ShareStageRules.CharacterFileName(character.Slot))
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        if (string.IsNullOrEmpty(character.Name) || character.Name.Length > ShareStageRules.MaxNameChars || ShareStageRules.HasControl(character.Name))
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        if (!ShareStageRules.IsSize(character.Size) || !ShareStageRules.IsAttribute(character.Attribute))
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        if (character.Level < ShareStageRules.LevelMin || character.Level > ShareStageRules.LevelMax)
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        if (character.FusionCount < 0 || character.FusionCount > ShareStageRules.LevelMax)
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        if (character.Spd < 0 || character.Spd > 255)
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        if (!InStat(character.Hp) || !InStat(character.Atk) || !InStat(character.Def) || !InStat(character.Luc) || !InStat(character.Obs))
        {
            error = "不正なキャラクターデータです。";
            return false;
        }
        return true;
    }

    public static bool LooksLikePng(byte[] png, int size)
    {
        if (png == null || png.Length < 24 || png.Length > ShareStageRules.MaxPngBytes)
            return false;
        if (png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71)
            return false;
        int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        return width == size && height == size && ShareStageRules.IsSize(size);
    }

    static bool InByte(int value)
    {
        return value >= 0 && value <= 255;
    }

    static bool InStat(long value)
    {
        return value >= 0 && value <= ShareStageRules.StatMax;
    }

    static List<PiquaCharacter> Ordered(PiquaDocument document)
    {
        List<PiquaCharacter> copy = new List<PiquaCharacter>(document.Characters);
        copy.Sort((a, b) => a.Slot.CompareTo(b.Slot));
        return copy;
    }

    static void WriteEntry(ZipArchive archive, string name, byte[] body)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using (Stream stream = entry.Open())
            stream.Write(body, 0, body.Length);
    }

    public static string ManifestJson(PiquaDocument document)
    {
        StringBuilder json = new StringBuilder();
        json.Append("{\"authorId\":").Append(Quote(document.AuthorId));
        json.Append(",\"authorName\":").Append(Quote(document.AuthorName));
        json.Append(",\"contentHash\":").Append(Quote(document.ContentHash));
        json.Append(",\"createdAt\":").Append(Quote(document.CreatedAt));
        json.Append(",\"fileId\":").Append(Quote(document.FileId));
        json.Append(",\"formatVersion\":").Append(document.FormatVersion.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"gameVersion\":").Append(Quote(document.GameVersion));
        json.Append(",\"revision\":").Append(document.Revision.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"stageId\":").Append(Quote(document.StageId));
        json.Append(",\"updatedAt\":").Append(Quote(document.UpdatedAt));
        json.Append('}');
        return json.ToString();
    }

    public static string StageJson(PiquaDocument document)
    {
        StringBuilder json = new StringBuilder();
        json.Append("{\"background\":").Append(Quote(document.Background));
        json.Append(",\"characters\":[");
        List<PiquaCharacter> ordered = Ordered(document);
        for (int i = 0; i < ordered.Count; i++)
        {
            if (i > 0)
                json.Append(',');
            AppendCharacter(json, ordered[i], document.FormatVersion >= 2);
        }
        json.Append("],\"comment\":").Append(Quote(document.Comment));
        json.Append(",\"floorCount\":").Append(document.FloorCount.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"mainColor\":{\"b\":").Append(document.MainB.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"g\":").Append(document.MainG.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"r\":").Append(document.MainR.ToString(CultureInfo.InvariantCulture));
        json.Append("},\"name\":").Append(Quote(document.StageName));
        json.Append('}');
        return json.ToString();
    }

    static void AppendCharacter(StringBuilder json, PiquaCharacter character, bool includeSourceId)
    {
        json.Append("{\"atk\":").Append(character.Atk.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"attribute\":").Append(Quote(character.Attribute));
        json.Append(",\"def\":").Append(character.Def.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"file\":").Append(Quote(character.FileName));
        json.Append(",\"fusionCount\":").Append(character.FusionCount.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"hp\":").Append(character.Hp.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"level\":").Append(character.Level.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"luc\":").Append(character.Luc.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"name\":").Append(Quote(character.Name));
        json.Append(",\"obs\":").Append(character.Obs.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"size\":").Append(character.Size.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"slot\":").Append(character.Slot.ToString(CultureInfo.InvariantCulture));
        if (includeSourceId)
            json.Append(",\"sourceId\":").Append(character.SourceId.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"spd\":").Append(character.Spd.ToString(CultureInfo.InvariantCulture));
        json.Append('}');
    }

    public static string Quote(string text)
    {
        if (text == null)
            text = "";
        StringBuilder json = new StringBuilder();
        json.Append('"');
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"' || c == '\\')
            {
                json.Append('\\').Append(c);
            }
            else if (c < ' ')
            {
                json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
            }
            else
            {
                json.Append(c);
            }
        }
        json.Append('"');
        return json.ToString();
    }

    static string ToHex(byte[] hash)
    {
        StringBuilder text = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
            text.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        return text.ToString();
    }
}

sealed class JsonValue
{
    enum Kind { Null, Text, Number, Bool, Object, Array }
    Kind _kind;
    string _text;
    long _number;
    Dictionary<string, JsonValue> _object;
    List<JsonValue> _array;

    public static bool TryObject(byte[] utf8, out Dictionary<string, JsonValue> root)
    {
        root = null;
        if (utf8 == null)
            return false;
        JsonValue value;
        int index = 0;
        string text = Encoding.UTF8.GetString(utf8);
        if (!TryRead(text, ref index, out value))
            return false;
        while (index < text.Length && (text[index] == ' ' || text[index] == '\n' || text[index] == '\r' || text[index] == '\t'))
            index++;
        if (index != text.Length)
            return false;
        return value.TryObject(out root);
    }

    public bool TryObject(out Dictionary<string, JsonValue> value)
    {
        value = _object;
        return _kind == Kind.Object && value != null;
    }

    public bool TryArray(out List<JsonValue> value)
    {
        value = _array;
        return _kind == Kind.Array && value != null;
    }

    public string Text()
    {
        return _kind == Kind.Text ? _text : "";
    }

    public int Int()
    {
        if (_kind != Kind.Number || _number > int.MaxValue || _number < int.MinValue)
            return 0;
        return (int)_number;
    }

    public long Long()
    {
        return _kind == Kind.Number ? _number : 0;
    }

    static bool TryRead(string text, ref int index, out JsonValue value)
    {
        value = null;
        if (!Skip(text, ref index))
            return false;
        char c = text[index];
        if (c == '"')
            return ReadString(text, ref index, out value);
        if (c == '{')
            return ReadObject(text, ref index, out value);
        if (c == '[')
            return ReadArray(text, ref index, out value);
        if (c == 't' || c == 'f')
            return ReadBool(text, ref index, out value);
        if (c == 'n')
            return ReadNull(text, ref index, out value);
        if (c == '-' || (c >= '0' && c <= '9'))
            return ReadNumber(text, ref index, out value);
        return false;
    }

    static bool ReadObject(string text, ref int index, out JsonValue value)
    {
        value = null;
        index++;
        Dictionary<string, JsonValue> map = new Dictionary<string, JsonValue>();
        if (!Skip(text, ref index))
            return false;
        if (text[index] == '}')
        {
            index++;
            value = new JsonValue { _kind = Kind.Object, _object = map };
            return true;
        }
        while (true)
        {
            JsonValue key;
            if (!ReadString(text, ref index, out key))
                return false;
            if (!Skip(text, ref index) || text[index] != ':')
                return false;
            index++;
            JsonValue item;
            if (!TryRead(text, ref index, out item))
                return false;
            map[key._text] = item;
            if (!Skip(text, ref index))
                return false;
            if (text[index] == '}')
            {
                index++;
                value = new JsonValue { _kind = Kind.Object, _object = map };
                return true;
            }
            if (text[index] != ',')
                return false;
            index++;
        }
    }

    static bool ReadArray(string text, ref int index, out JsonValue value)
    {
        value = null;
        index++;
        List<JsonValue> list = new List<JsonValue>();
        if (!Skip(text, ref index))
            return false;
        if (text[index] == ']')
        {
            index++;
            value = new JsonValue { _kind = Kind.Array, _array = list };
            return true;
        }
        while (true)
        {
            JsonValue item;
            if (!TryRead(text, ref index, out item))
                return false;
            list.Add(item);
            if (!Skip(text, ref index))
                return false;
            if (text[index] == ']')
            {
                index++;
                value = new JsonValue { _kind = Kind.Array, _array = list };
                return true;
            }
            if (text[index] != ',')
                return false;
            index++;
        }
    }

    static bool ReadString(string text, ref int index, out JsonValue value)
    {
        value = null;
        if (!Skip(text, ref index) || text[index] != '"')
            return false;
        index++;
        StringBuilder body = new StringBuilder();
        while (index < text.Length)
        {
            char c = text[index++];
            if (c == '"')
            {
                value = new JsonValue { _kind = Kind.Text, _text = body.ToString() };
                return true;
            }
            if (c == '\\')
            {
                if (index >= text.Length)
                    return false;
                char esc = text[index++];
                if (esc == '"' || esc == '\\' || esc == '/')
                    body.Append(esc);
                else if (esc == 'n')
                    body.Append('\n');
                else if (esc == 'r')
                    body.Append('\r');
                else if (esc == 't')
                    body.Append('\t');
                else if (esc == 'u')
                {
                    if (index + 4 > text.Length)
                        return false;
                    int code;
                    if (!int.TryParse(text.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                        return false;
                    body.Append((char)code);
                    index += 4;
                }
                else
                    return false;
            }
            else if (c < ' ')
            {
                return false;
            }
            else
            {
                body.Append(c);
            }
        }
        return false;
    }

    static bool ReadNumber(string text, ref int index, out JsonValue value)
    {
        value = null;
        int start = index;
        if (text[index] == '-')
            index++;
        if (index >= text.Length || text[index] < '0' || text[index] > '9')
            return false;
        if (text[index] == '0')
            index++;
        else
        {
            while (index < text.Length && text[index] >= '0' && text[index] <= '9')
                index++;
        }
        if (index < text.Length && text[index] == '.')
            return false;
        long number;
        if (!long.TryParse(text.Substring(start, index - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number))
            return false;
        value = new JsonValue { _kind = Kind.Number, _number = number };
        return true;
    }

    static bool ReadBool(string text, ref int index, out JsonValue value)
    {
        value = null;
        if (text.IndexOf("true", index, StringComparison.Ordinal) == index)
        {
            index += 4;
            value = new JsonValue { _kind = Kind.Bool };
            return true;
        }
        if (text.IndexOf("false", index, StringComparison.Ordinal) == index)
        {
            index += 5;
            value = new JsonValue { _kind = Kind.Bool };
            return true;
        }
        return false;
    }

    static bool ReadNull(string text, ref int index, out JsonValue value)
    {
        value = null;
        if (text.IndexOf("null", index, StringComparison.Ordinal) != index)
            return false;
        index += 4;
        value = new JsonValue { _kind = Kind.Null };
        return true;
    }

    static bool Skip(string text, ref int index)
    {
        while (index < text.Length && (text[index] == ' ' || text[index] == '\n' || text[index] == '\r' || text[index] == '\t'))
            index++;
        return index < text.Length;
    }
}
