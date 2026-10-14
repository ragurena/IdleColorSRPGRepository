using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// ICS.csv の書き出しと、起動時に読む候補の確認。
// 一時ファイルを検証してから、世代付き bak を残したまま本体と付け替える。
public static class SaveFiles
{
    public struct SaveInfo
    {
        public bool Format2;
        public int Generation;
        public string DataVersion;
    }

    public struct SaveCandidate
    {
        public string Path;
        public int Generation;
        public int Rank;
    }

    public static int NextGeneration()
    {
        string directory = Application.persistentDataPath;
        string[] files;
        try
        {
            files = Directory.GetFiles(directory);
        }
        catch (Exception exception)
        {
            Debug.LogError("セーブフォルダを確認できませんでした。 " + exception.Message);
            return -1;
        }

        int max = 0;
        for (int i = 0; i < files.Length; i++)
        {
            string name = Path.GetFileName(files[i]);
            if (name == null || !name.StartsWith("ICS.csv", StringComparison.Ordinal))
                continue;
            int fromName = BakNumberAny(name);
            if (fromName > max)
                max = fromName;
            int fromBody = ReadGeneration(files[i]);
            if (fromBody > max)
                max = fromBody;
        }
        if (max == int.MaxValue)
            return -1;
        return max + 1;
    }

    public static bool Commit(byte[] body, int generation)
    {
        if (body == null || generation < 1)
            return false;

        string directory = Application.persistentDataPath;
        string hex = ImportedCharacters.Sha256(body);
        byte[] extra = new UTF8Encoding(false).GetBytes("SaveChecksum," + hex + "\nSaveEnd,1\n");
        byte[] full = new byte[body.Length + extra.Length];
        Buffer.BlockCopy(body, 0, full, 0, body.Length);
        Buffer.BlockCopy(extra, 0, full, body.Length, extra.Length);

        string tmp = UniquePath(directory, "ICS.csv.tmp.");
        if (tmp == null)
            return false;
        try
        {
            using (FileStream stream = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(full, 0, full.Length);
                stream.Flush(true);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("セーブの一時ファイルを書けませんでした。 " + exception.Message);
            TryQuarantine(tmp);
            return false;
        }

        SaveInfo written;
        if (!TryValidate(tmp, out written) || !written.Format2 || written.Generation != generation)
        {
            Debug.LogError("セーブの一時ファイルを検証できませんでした。");
            TryQuarantine(tmp);
            return false;
        }

        string csv = Path.Combine(directory, "ICS.csv");
        string hold = null;
        bool madeBackup = false;
        try
        {
            if (File.Exists(csv))
            {
                madeBackup = TryCopyBackup(csv, directory, generation);
                if (!madeBackup)
                    return false;
                hold = UniquePath(directory, "ICS.csv.hold.");
                if (hold == null)
                    return false;
                File.Move(csv, hold);
            }
            File.Move(tmp, csv);
        }
        catch (Exception exception)
        {
            Debug.LogError("セーブの付け替えに失敗しました。 " + exception.Message);
            return false;
        }

        SaveInfo published;
        if (!TryValidate(csv, out published) || !published.Format2 || published.Generation != generation)
        {
            Debug.LogError("付け替えたセーブを検証できませんでした。");
            bool moved = TryQuarantine(csv);
            if (hold != null && moved && !File.Exists(csv))
            {
                try
                {
                    File.Move(hold, csv);
                }
                catch (Exception exception)
                {
                    Debug.LogError("以前のセーブを戻せませんでした。 " + exception.Message);
                }
            }
            return false;
        }

        if (hold != null)
            TryDelete(hold);
        if (madeBackup)
            DeleteOlderBackups(directory, generation);
        return true;
    }

    public static bool TryListCandidates(out List<SaveCandidate> candidates)
    {
        candidates = null;
        string directory = Application.persistentDataPath;
        string[] files;
        try
        {
            files = Directory.GetFiles(directory);
        }
        catch (Exception exception)
        {
            Debug.LogError("セーブフォルダを確認できませんでした。 " + exception.Message);
            return false;
        }

        List<SaveCandidate> list = new List<SaveCandidate>();
        for (int i = 0; i < files.Length; i++)
        {
            string name = Path.GetFileName(files[i]);
            if (!IsCandidate(name))
                continue;
            SaveCandidate candidate = new SaveCandidate();
            candidate.Path = files[i];
            candidate.Generation = ReadGeneration(files[i]);
            candidate.Rank = RankOf(name);
            list.Add(candidate);
        }
        list.Sort(CompareCandidates);
        candidates = list;
        return true;
    }

    public static bool TryValidate(string path, out SaveInfo info)
    {
        info = new SaveInfo();
        info.DataVersion = "";
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes == null || bytes.Length == 0)
                return false;
            string text = DecodeText(bytes);
            int format = DetectFormat(text);
            if (format < 0)
                return false;
            if (format == 2)
                return ValidateFormat2(bytes, text, out info);
            return ValidateLegacy(text, out info);
        }
        catch (Exception exception)
        {
            Debug.LogError("セーブの検証に失敗しました。 " + exception.Message);
            return false;
        }
    }

    public static bool TryQuarantine(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        if (!File.Exists(path))
            return true;

        string directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
            directory = Application.persistentDataPath;
        for (int i = 0; i < 100; i++)
        {
            string dest = Path.Combine(directory, "ICS.csv.bad." + DateTime.UtcNow.Ticks.ToString() + "-" + i.ToString());
            if (File.Exists(dest))
                continue;
            try
            {
                File.Move(path, dest);
                return !File.Exists(path);
            }
            catch (Exception exception)
            {
                Debug.LogError("セーブの退避に失敗しました。 " + exception.Message);
                return false;
            }
        }
        Debug.LogError("セーブの退避先を作れませんでした。");
        return false;
    }

    public static void DeleteManaged()
    {
        string directory = Application.persistentDataPath;
        TryDelete(Path.Combine(directory, "ICS.csv"));
        string[] files;
        try
        {
            files = Directory.GetFiles(directory);
        }
        catch (Exception exception)
        {
            Debug.LogError("セーブファイルを確認できませんでした。 " + exception.Message);
            return;
        }
        for (int i = 0; i < files.Length; i++)
        {
            string name = Path.GetFileName(files[i]);
            if (IsManagedExtra(name))
                TryDelete(files[i]);
        }
    }

    static bool TryCopyBackup(string source, string directory, int generation)
    {
        string finalPath = Path.Combine(directory, "ICS.csv.bak." + generation.ToString());
        string partial = finalPath + ".partial";
        if (File.Exists(finalPath) || File.Exists(partial))
        {
            Debug.LogError("バックアップの名前が既にあります。");
            return false;
        }
        try
        {
            File.Copy(source, partial, false);
            if (!SameBytes(source, partial))
            {
                Debug.LogError("バックアップの内容が一致しません。");
                return false;
            }
            File.Move(partial, finalPath);
            return File.Exists(finalPath);
        }
        catch (Exception exception)
        {
            Debug.LogError("バックアップを作れませんでした。 " + exception.Message);
            return false;
        }
    }

    static bool SameBytes(string left, string right)
    {
        byte[] a = File.ReadAllBytes(left);
        byte[] b = File.ReadAllBytes(right);
        if (a.Length != b.Length)
            return false;
        return ImportedCharacters.Sha256(a) == ImportedCharacters.Sha256(b);
    }

    static void DeleteOlderBackups(string directory, int generation)
    {
        string[] files;
        try
        {
            files = Directory.GetFiles(directory);
        }
        catch (Exception exception)
        {
            Debug.LogError("古いバックアップを確認できませんでした。 " + exception.Message);
            return;
        }
        for (int i = 0; i < files.Length; i++)
        {
            int number = BakNumberExact(Path.GetFileName(files[i]));
            if (number >= 1 && number < generation)
                TryDelete(files[i]);
        }
    }

    static void TryDelete(string path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception)
        {
            Debug.LogError("ファイルを削除できませんでした。 " + path + " " + exception.Message);
        }
    }

    static string UniquePath(string directory, string prefix)
    {
        for (int i = 0; i < 100; i++)
        {
            string path = Path.Combine(directory, prefix + Guid.NewGuid().ToString("N"));
            if (!File.Exists(path))
                return path;
        }
        Debug.LogError("空きファイル名を作れませんでした。");
        return null;
    }

    static bool ValidateFormat2(byte[] bytes, string text, out SaveInfo info)
    {
        info = new SaveInfo();
        info.DataVersion = "";
        info.Format2 = true;
        byte[] body;
        string hex;
        string after;
        if (!TrySplitChecksum(bytes, out body, out hex, out after))
            return false;
        if (!IsHex64(hex) || !string.Equals(ImportedCharacters.Sha256(body), hex, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!IsSaveEnd(after))
            return false;

        bool sawFormat = false;
        bool sawGeneration = false;
        bool recognized = false;
        int generation = 0;
        if (!ScanLines(text, ref recognized, ref sawFormat, ref sawGeneration, ref generation, ref info.DataVersion))
            return false;
        if (!sawFormat || !sawGeneration || generation < 1 || !recognized)
            return false;
        info.Generation = generation;
        return true;
    }

    static bool ValidateLegacy(string text, out SaveInfo info)
    {
        info = new SaveInfo();
        info.DataVersion = "";
        info.Format2 = false;
        info.Generation = 0;
        bool sawFormat = false;
        bool sawGeneration = false;
        bool recognized = false;
        int generation = 0;
        if (!ScanLines(text, ref recognized, ref sawFormat, ref sawGeneration, ref generation, ref info.DataVersion))
            return false;
        if (sawFormat || !recognized)
            return false;
        return true;
    }

    static bool ScanLines(string text, ref bool recognized, ref bool sawFormat, ref bool sawGeneration, ref int generation, ref string dataVersion)
    {
        if (text == null)
            return false;
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim('\r', ' ', '\t');
            if (line.Length == 0)
                continue;
            int comma = line.IndexOf(',');
            if (comma <= 0)
                continue;
            string key = line.Substring(0, comma);
            string value = line.Substring(comma + 1);
            if (!KnownValueOk(key, value))
                return false;
            if (IsRecognizedKey(key))
                recognized = true;
            if (key == "DataVersion" && string.IsNullOrEmpty(dataVersion))
                dataVersion = FirstToken(value);
            if (key == "SaveFormat")
                sawFormat = true;
            if (key == "SaveGeneration")
            {
                sawGeneration = true;
                int number;
                if (int.TryParse(FirstToken(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    generation = number;
            }
        }
        return true;
    }

    static int DetectFormat(string text)
    {
        if (string.IsNullOrEmpty(text))
            return -1;
        string[] lines = text.Split('\n');
        int format = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim('\r', ' ', '\t');
            if (!line.StartsWith("SaveFormat,", StringComparison.Ordinal))
                continue;
            string value = line.Substring("SaveFormat,".Length).Trim();
            if (value == "2")
                format = 2;
            else
                return -1;
        }
        return format;
    }

    static bool TrySplitChecksum(byte[] bytes, out byte[] body, out string hex, out string after)
    {
        body = null;
        hex = "";
        after = "";
        byte[] marker = Encoding.ASCII.GetBytes("SaveChecksum,");
        int at = LastIndexOf(bytes, marker);
        if (at < 0)
            return false;
        body = new byte[at];
        Buffer.BlockCopy(bytes, 0, body, 0, at);
        int lineEnd = at;
        while (lineEnd < bytes.Length && bytes[lineEnd] != (byte)'\n' && bytes[lineEnd] != (byte)'\r')
            lineEnd++;
        string line = Encoding.ASCII.GetString(bytes, at, lineEnd - at);
        const string prefix = "SaveChecksum,";
        if (!line.StartsWith(prefix, StringComparison.Ordinal))
            return false;
        hex = line.Substring(prefix.Length).Trim();
        int rest = lineEnd;
        while (rest < bytes.Length && (bytes[rest] == (byte)'\n' || bytes[rest] == (byte)'\r'))
            rest++;
        after = Encoding.UTF8.GetString(bytes, rest, bytes.Length - rest);
        return true;
    }

    static bool IsSaveEnd(string after)
    {
        if (after == null)
            return false;
        string text = after.Replace("\r", "");
        if (text.EndsWith("\n", StringComparison.Ordinal))
            text = text.Substring(0, text.Length - 1);
        return text == "SaveEnd,1";
    }

    static int LastIndexOf(byte[] data, byte[] pattern)
    {
        if (data == null || pattern == null || pattern.Length == 0 || data.Length < pattern.Length)
            return -1;
        for (int i = data.Length - pattern.Length; i >= 0; i--)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (data[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return i;
        }
        return -1;
    }

    static string DecodeText(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        return Encoding.UTF8.GetString(bytes);
    }

    static int ReadGeneration(string path)
    {
        try
        {
            using (StreamReader reader = new StreamReader(path))
            {
                bool format2 = false;
                int generation = 0;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        continue;
                    int comma = line.IndexOf(',');
                    if (comma <= 0)
                        continue;
                    string key = line.Substring(0, comma);
                    string value = FirstToken(line.Substring(comma + 1));
                    if (key == "SaveFormat" && value == "2")
                        format2 = true;
                    if (key == "SaveGeneration")
                    {
                        int number;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number > generation)
                            generation = number;
                    }
                }
                if (!format2)
                    return 0;
                return generation;
            }
        }
        catch (Exception)
        {
            return 0;
        }
    }

    static int CompareCandidates(SaveCandidate a, SaveCandidate b)
    {
        if (a.Generation != b.Generation)
            return b.Generation.CompareTo(a.Generation);
        if (a.Rank != b.Rank)
            return a.Rank.CompareTo(b.Rank);
        return string.CompareOrdinal(a.Path, b.Path);
    }

    static int RankOf(string fileName)
    {
        if (fileName == "ICS.csv")
            return 0;
        if (fileName.StartsWith("ICS.csv.hold.", StringComparison.Ordinal))
            return 1;
        if (fileName.StartsWith("ICS.csv.tmp.", StringComparison.Ordinal))
            return 2;
        if (fileName.StartsWith("ICS.csv.bak.", StringComparison.Ordinal))
            return 3;
        return 9;
    }

    static bool IsCandidate(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
        if (fileName.StartsWith("ICS.csv.bad.", StringComparison.Ordinal))
            return false;
        if (fileName.EndsWith(".partial", StringComparison.Ordinal))
            return false;
        if (fileName == "ICS.csv")
            return true;
        if (fileName.StartsWith("ICS.csv.hold.", StringComparison.Ordinal) && fileName.Length > "ICS.csv.hold.".Length)
            return true;
        if (fileName.StartsWith("ICS.csv.tmp.", StringComparison.Ordinal) && fileName.Length > "ICS.csv.tmp.".Length)
            return true;
        return BakNumberExact(fileName) >= 1;
    }

    static bool IsManagedExtra(string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.StartsWith("ICS.csv.bad.", StringComparison.Ordinal))
            return false;
        if (fileName.StartsWith("ICS.csv.tmp.", StringComparison.Ordinal))
            return true;
        if (fileName.StartsWith("ICS.csv.bak.", StringComparison.Ordinal))
            return true;
        if (fileName.StartsWith("ICS.csv.hold.", StringComparison.Ordinal))
            return true;
        return fileName.StartsWith("ICS.csv", StringComparison.Ordinal) && fileName.EndsWith(".partial", StringComparison.Ordinal);
    }

    static int BakNumberExact(string fileName)
    {
        const string prefix = "ICS.csv.bak.";
        if (fileName == null || !fileName.StartsWith(prefix, StringComparison.Ordinal))
            return 0;
        string suffix = fileName.Substring(prefix.Length);
        if (suffix.Length == 0)
            return 0;
        for (int i = 0; i < suffix.Length; i++)
        {
            if (suffix[i] < '0' || suffix[i] > '9')
                return 0;
        }
        int number;
        if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out number))
            return 0;
        return number;
    }

    static int BakNumberAny(string fileName)
    {
        const string prefix = "ICS.csv.bak.";
        if (fileName == null || !fileName.StartsWith(prefix, StringComparison.Ordinal))
            return 0;
        string suffix = fileName.Substring(prefix.Length);
        if (suffix.EndsWith(".partial", StringComparison.Ordinal))
            suffix = suffix.Substring(0, suffix.Length - ".partial".Length);
        int number;
        if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out number) || number < 1)
            return 0;
        return number;
    }

    static bool IsRecognizedKey(string key)
    {
        if (key == "DataVersion" || key == "CurR" || key == "CurG" || key == "CurB")
            return true;
        if (key.StartsWith("CharactersAll[", StringComparison.Ordinal))
            return true;
        if (key.StartsWith("ImportedCharacter[", StringComparison.Ordinal))
            return true;
        if (key.StartsWith("Person[", StringComparison.Ordinal))
            return true;
        if (key.StartsWith("CurPixels", StringComparison.Ordinal))
            return true;
        if (key.StartsWith("CharactersID", StringComparison.Ordinal))
            return true;
        if (key.StartsWith("BattleParty", StringComparison.Ordinal))
            return true;
        return false;
    }

    static bool KnownValueOk(string key, string rawValue)
    {
        if (string.IsNullOrEmpty(key))
            return true;
        string value = FirstToken(rawValue);
        if (key == "SaveFormat")
            return value == "2";
        if (key == "SaveGeneration")
        {
            int number;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number >= 1;
        }
        if (key == "SaveEnd")
            return value == "1";
        if (key == "SaveChecksum")
            return IsHex64(value);
        if (IsCurPixelChannel(key))
            return IsByte(value);
        if (key.EndsWith(".KnownPixels", StringComparison.Ordinal))
            return IsUint(value);
        if (IsUlongKey(key))
            return IsUlong(value);
        if (IsProgressSizeKey(key))
            return IsRange(value, 0, 128);
        if (IsColorChannel(key))
            return IsFloat(value);
        return true;
    }

    static bool IsUlongKey(string key)
    {
        switch (key)
        {
            case "CurR":
            case "CurG":
            case "CurB":
            case "MaxR":
            case "MaxG":
            case "MaxB":
            case "CostMaxRUp":
            case "CostMaxGUp":
            case "CostMaxBUp":
            case "IncreaseValueR":
            case "IncreaseValueG":
            case "IncreaseValueB":
            case "CostIncreaseValueRUp":
            case "CostIncreaseValueGUp":
            case "CostIncreaseValueBUp":
                return true;
        }
        if (key.EndsWith(".OwnedNumMax", StringComparison.Ordinal)
            || key.EndsWith(".OwnedNumCur", StringComparison.Ordinal)
            || key.EndsWith(".ReincarnationTimes", StringComparison.Ordinal)
            || key.EndsWith(".Level", StringComparison.Ordinal)
            || key.EndsWith(".Exp", StringComparison.Ordinal)
            || key.EndsWith(".FusionCount", StringComparison.Ordinal))
            return true;
        if (key.StartsWith("CurPixels[", StringComparison.Ordinal) && key.EndsWith(".num", StringComparison.Ordinal))
            return true;
        return key.StartsWith("SpentProductionPixel[", StringComparison.Ordinal)
            && (key.EndsWith(".r", StringComparison.Ordinal)
                || key.EndsWith(".g", StringComparison.Ordinal)
                || key.EndsWith(".b", StringComparison.Ordinal));
    }

    static bool IsCurPixelChannel(string key)
    {
        return key.StartsWith("CurPixels[", StringComparison.Ordinal)
            && (key.EndsWith(".r", StringComparison.Ordinal)
                || key.EndsWith(".g", StringComparison.Ordinal)
                || key.EndsWith(".b", StringComparison.Ordinal));
    }

    static bool IsProgressSizeKey(string key)
    {
        return key.StartsWith("ProgressTextureProductionCharacter[", StringComparison.Ordinal)
            && (key.EndsWith(".width", StringComparison.Ordinal) || key.EndsWith(".height", StringComparison.Ordinal));
    }

    static bool IsColorChannel(string key)
    {
        bool color = key.StartsWith("ColorProductionPixel[", StringComparison.Ordinal)
            || key.StartsWith("ConsumePixelsProductionCharacter[", StringComparison.Ordinal);
        return color && (key.EndsWith(".r", StringComparison.Ordinal)
            || key.EndsWith(".g", StringComparison.Ordinal)
            || key.EndsWith(".b", StringComparison.Ordinal));
    }

    static string FirstToken(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        int comma = value.IndexOf(',');
        if (comma >= 0)
            value = value.Substring(0, comma);
        return value.Trim();
    }

    static bool IsUlong(string value)
    {
        ulong number;
        return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
    }

    static bool IsUint(string value)
    {
        ulong number;
        return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            && number <= uint.MaxValue;
    }

    static bool IsByte(string value)
    {
        int number;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            && number >= 0 && number <= 255;
    }

    static bool IsRange(string value, int min, int max)
    {
        int number;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            && number >= min && number <= max;
    }

    static bool IsFloat(string value)
    {
        float number;
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            return true;
        return float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out number);
    }

    static bool IsHex64(string text)
    {
        if (text == null || text.Length != 64)
            return false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            bool digit = c >= '0' && c <= '9';
            bool lower = c >= 'a' && c <= 'f';
            bool upper = c >= 'A' && c <= 'F';
            if (!digit && !lower && !upper)
                return false;
        }
        return true;
    }
}
