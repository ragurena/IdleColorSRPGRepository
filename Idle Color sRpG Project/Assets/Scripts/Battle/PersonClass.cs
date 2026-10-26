using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public static class PersonIds
{
    public const int Protagonist = 1;
    public const int TeacherBlack = 2;
    public const int ClassmateGreen = 3;
    public const int ClassmateRed = 4;
    public const int ClassmateBlue = 5;
    public const int Count = 5;
}

public sealed class PersonClass
{
    public int Id;
    public string Name;
    public readonly List<CharacterAttribute> Specialties = new List<CharacterAttribute>();
    public readonly int[] RgbLevel = { 1, 1, 1 };
    public readonly long[] RgbExp = new long[3];
    public readonly long[] RgbRemainder = new long[3];
    public int CharacterLevel = 1;
    public long CharacterExp;
    public int PixelLevel = 1;
    public long PixelExp;
    public int BattleLevel = 1;
    public long BattleExp;
    public int Affection;
    int _loadedPixelLevel;

    public CharacterAttribute[] SpecialtyArray()
    {
        return Specialties.ToArray();
    }

    public long RgbAmount(int channel, PersonBalanceConfig config)
    {
        return PersonFormulas.AmountAtLevel(PersonFormulas.RgbInitial(SpecialtyArray(), channel, config), RgbLevelValue(channel), config);
    }

    public long PixelAmount(int r, int g, int b, PersonBalanceConfig config)
    {
        int level = PixelLevel < 1 ? 1 : PixelLevel;
        return PersonFormulas.AmountAtLevel(PersonFormulas.PixelInitial(SpecialtyArray(), r, g, b, config), level, config);
    }

    public long CharacterAmount(bool isAuthor, PersonBalanceConfig config)
    {
        return PersonFormulas.AmountAtLevel(PersonFormulas.CharacterInitial(isAuthor, config), CharacterLevel, config);
    }

    public void AddRgbProduced(int channel, long produced, PersonBalanceConfig config)
    {
        if (channel < 0 || channel > 2)
            return;
        bool specialty = PersonFormulas.RgbSpecialty(SpecialtyArray(), channel);
        long remainder = RgbRemainder[channel];
        long exp = PersonFormulas.TakeRgbExp(ref remainder, produced, specialty, config);
        RgbRemainder[channel] = remainder;
        int level = RgbLevel[channel];
        long skillExp = RgbExp[channel];
        double exponent = config != null ? config.RgbExpExponent : 1.7;
        PersonFormulas.AddSkillExp(ref level, ref skillExp, exp, exponent, config);
        RgbLevel[channel] = level;
        RgbExp[channel] = skillExp;
    }

    public void AddPixelCompletion(int r, int g, int b, PersonBalanceConfig config)
    {
        if (config != null && !PersonFormulas.IsPosterizedColor(r, g, b, config.PosterizeLevels))
            return;
        r = PersonFormulas.ClampChannel(r);
        g = PersonFormulas.ClampChannel(g);
        b = PersonFormulas.ClampChannel(b);
        long gain = PersonFormulas.PixelCompletionExp(r, g, b);
        bool specialty = PersonFormulas.HasSpecialty(SpecialtyArray(), PersonFormulas.ColorAttribute(r, g, b));
        int specialtyMultiplier = config != null ? config.PixelExpSpecialtyMultiplier : 2;
        if (specialty && specialtyMultiplier > 1)
        {
            if (gain > long.MaxValue / specialtyMultiplier)
                gain = long.MaxValue;
            else
                gain *= specialtyMultiplier;
        }
        int level = PixelLevel < 1 ? 1 : PixelLevel;
        long exp = PixelExp < 0 ? 0 : PixelExp;
        double exponent = config != null ? config.PixelExpExponent : 1.6;
        int expBaseMultiplier = config != null ? config.PixelExpBaseMultiplier : 3;
        PersonFormulas.AddSkillExp(ref level, ref exp, gain, exponent, config, expBaseMultiplier);
        PixelLevel = level;
        PixelExp = exp;
    }

    public void AddCharacterCompletion(int opaquePixels, PersonBalanceConfig config)
    {
        long gain = opaquePixels;
        if (gain < 0)
            gain = 0;
        int divisor = config != null ? config.CharacterExpOpaqueDivisor : 8;
        gain = BattleMath.CeilDivPositive(gain, divisor);
        int level = CharacterLevel;
        long exp = CharacterExp;
        double exponent = config != null ? config.CharacterExpExponent : 1.5;
        int expBaseMultiplier = config != null ? config.CharacterExpBaseMultiplier : 8;
        PersonFormulas.AddSkillExp(ref level, ref exp, gain, exponent, config, expBaseMultiplier);
        CharacterLevel = level;
        CharacterExp = exp;
    }

    public void AddBattleFloor(PersonBalanceConfig config)
    {
        int level = BattleLevel;
        long exp = BattleExp;
        double exponent = config != null ? config.BattleExpExponent : 1.7;
        PersonFormulas.AddSkillExp(ref level, ref exp, 1, exponent, config);
        BattleLevel = level;
        BattleExp = exp;
    }

    public PersonBattleInput ToBattleInput()
    {
        PersonBattleInput input = new PersonBattleInput();
        input.PersonId = Id;
        input.BattleLevel = BattleLevel;
        input.SpecialtyCount = Specialties.Count;
        if (input.SpecialtyCount > 5)
            input.SpecialtyCount = 5;
        if (input.SpecialtyCount >= 1)
            input.Specialty0 = Specialties[0];
        if (input.SpecialtyCount >= 2)
            input.Specialty1 = Specialties[1];
        if (input.SpecialtyCount >= 3)
            input.Specialty2 = Specialties[2];
        if (input.SpecialtyCount >= 4)
            input.Specialty3 = Specialties[3];
        if (input.SpecialtyCount >= 5)
            input.Specialty4 = Specialties[4];
        return input;
    }

    public string SpecialtyText()
    {
        if (Specialties.Count == 0)
            return "なし";
        string text = "";
        for (int i = 0; i < Specialties.Count; i++)
        {
            if (i > 0)
                text += "・";
            text += SpecialtyName(Specialties[i]);
        }
        return text;
    }

    public string Describe(PersonBalanceConfig config)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        string text = Name + "\n"
            + "得意 " + SpecialtyText() + "\n";
        text += SkillLine("R", 0, RgbAmount(0, config), RgbLevel[0], RgbExp[0], config.RgbExpExponent, config) + "\n";
        text += SkillLine("G", 1, RgbAmount(1, config), RgbLevel[1], RgbExp[1], config.RgbExpExponent, config) + "\n";
        text += SkillLine("B", 2, RgbAmount(2, config), RgbLevel[2], RgbExp[2], config.RgbExpExponent, config) + "\n";
        long plain = CharacterAmount(false, config);
        long authored = CharacterAmount(true, config);
        long characterNeed = PersonFormulas.ExpToNext(CharacterLevel, config.CharacterExpExponent, config, config.CharacterExpBaseMultiplier);
        text += "キャラ作成 Lv" + CharacterLevel.ToString() + "  " + plain.ToString() + "/秒"
            + "（作者 " + authored.ToString() + "/秒）  経験値 " + CharacterExp.ToString() + " / " + characterNeed.ToString() + "\n";
        long battleNeed = PersonFormulas.ExpToNext(BattleLevel, config.BattleExpExponent, config);
        text += "バトル Lv" + BattleLevel.ToString() + "  経験値 " + BattleExp.ToString() + " / " + battleNeed.ToString() + "\n";
        int pixelLevel = PixelLevel < 1 ? 1 : PixelLevel;
        long pixelNeed = PersonFormulas.ExpToNext(pixelLevel, config.PixelExpExponent, config, config.PixelExpBaseMultiplier);
        text += "ピクセル Lv" + pixelLevel.ToString() + "  経験値 " + (PixelExp < 0 ? 0 : PixelExp).ToString() + " / " + pixelNeed.ToString();
        return text;
    }

    public static PersonClass[] CreateRoster()
    {
        PersonClass[] people = new PersonClass[PersonIds.Count + 1];
        people[PersonIds.Protagonist] = Create(PersonIds.Protagonist, "主人公");
        people[PersonIds.TeacherBlack] = Create(PersonIds.TeacherBlack, "教師黒", CharacterAttribute.Dark);
        people[PersonIds.ClassmateGreen] = Create(PersonIds.ClassmateGreen, "クラスメイト緑", CharacterAttribute.Grass);
        people[PersonIds.ClassmateRed] = Create(PersonIds.ClassmateRed, "クラスメイト赤", CharacterAttribute.Fire);
        people[PersonIds.ClassmateBlue] = Create(PersonIds.ClassmateBlue, "クラスメイト青", CharacterAttribute.Water);
        return people;
    }

    public static PersonClass Find(PersonClass[] people, int id)
    {
        if (people == null || id < 1 || id >= people.Length)
            return null;
        return people[id];
    }

    static string SpecialtyName(CharacterAttribute attribute)
    {
        switch (attribute)
        {
            case CharacterAttribute.Fire: return "火";
            case CharacterAttribute.Grass: return "草";
            case CharacterAttribute.Water: return "水";
            case CharacterAttribute.Light: return "光";
            case CharacterAttribute.Dark: return "闇";
            default: return "なし";
        }
    }

    static PersonClass Create(int id, string name, params CharacterAttribute[] specialties)
    {
        PersonClass person = new PersonClass();
        person.Id = id;
        person.Name = name;
        if (specialties != null)
        {
            for (int i = 0; i < specialties.Length; i++)
            {
                if (specialties[i] != CharacterAttribute.None)
                    person.Specialties.Add(specialties[i]);
            }
        }
        return person;
    }

    int RgbLevelValue(int channel)
    {
        if (channel < 0 || channel > 2)
            return 1;
        int level = RgbLevel[channel];
        if (level < 1)
            return 1;
        return level;
    }

    string SkillLine(string label, int channel, long amount, int level, long exp, double exponent, PersonBalanceConfig config)
    {
        if (level < 1)
            level = 1;
        long need = PersonFormulas.ExpToNext(level, exponent, config);
        return label + " Lv" + level.ToString() + "  " + amount.ToString() + "/秒  経験値 " + exp.ToString() + " / " + need.ToString();
    }

    public void FoldLoadedPixelLevel(int level)
    {
        if (level < 1)
            level = 1;
        _loadedPixelLevel = level;
        if (level > PixelLevel)
        {
            PixelLevel = level;
            PixelExp = 0;
        }
    }

    public void FoldLoadedPixelExp(long exp)
    {
        if (exp < 0)
            exp = 0;
        if (_loadedPixelLevel > PixelLevel)
        {
            PixelLevel = _loadedPixelLevel;
            PixelExp = exp;
            return;
        }
        if (_loadedPixelLevel == PixelLevel && exp > PixelExp)
            PixelExp = exp;
    }
}

public sealed class PersonSaveData
{
    public PersonClass[] People;
    public uint[] HelpR;
    public uint[] HelpG;
    public uint[] HelpB;
    public uint[] Pixel;
    public uint[] Character;
    public uint[,] BattlePeople;

    public void Write(StreamWriter sw)
    {
        if (sw == null || People == null)
            return;
        for (int i = 1; i < People.Length; i++)
        {
            PersonClass person = People[i];
            if (person == null)
                continue;
            string prefix = "Person[" + i.ToString() + "].";
            sw.WriteLine(prefix + "Specialties," + JoinSpecialties(person));
            sw.WriteLine(prefix + "RgbLevelR," + person.RgbLevel[0].ToString());
            sw.WriteLine(prefix + "RgbLevelG," + person.RgbLevel[1].ToString());
            sw.WriteLine(prefix + "RgbLevelB," + person.RgbLevel[2].ToString());
            sw.WriteLine(prefix + "RgbExpR," + person.RgbExp[0].ToString());
            sw.WriteLine(prefix + "RgbExpG," + person.RgbExp[1].ToString());
            sw.WriteLine(prefix + "RgbExpB," + person.RgbExp[2].ToString());
            sw.WriteLine(prefix + "RgbRemainderR," + person.RgbRemainder[0].ToString());
            sw.WriteLine(prefix + "RgbRemainderG," + person.RgbRemainder[1].ToString());
            sw.WriteLine(prefix + "RgbRemainderB," + person.RgbRemainder[2].ToString());
            sw.WriteLine(prefix + "CharacterLevel," + person.CharacterLevel.ToString());
            sw.WriteLine(prefix + "CharacterExp," + person.CharacterExp.ToString());
            sw.WriteLine(prefix + "PixelLevel," + person.PixelLevel.ToString());
            sw.WriteLine(prefix + "PixelExp," + person.PixelExp.ToString());
            sw.WriteLine(prefix + "BattleLevel," + person.BattleLevel.ToString());
            sw.WriteLine(prefix + "BattleExp," + person.BattleExp.ToString());
            if (person.Affection > 0)
                sw.WriteLine(prefix + "Affection," + person.Affection.ToString());
        }
        WriteSlots(sw, "PersonIDHelpProductionR", HelpR);
        WriteSlots(sw, "PersonIDHelpProductionG", HelpG);
        WriteSlots(sw, "PersonIDHelpProductionB", HelpB);
        WriteSlots(sw, "PersonIDProductionPixel", Pixel);
        WriteSlots(sw, "PersonIDProductionCharacter", Character);
        if (BattlePeople == null)
            return;
        for (int set = 1; set < BattlePeople.GetLength(0); set++)
        {
            for (int slot = 1; slot < BattlePeople.GetLength(1); slot++)
                sw.WriteLine("BattlePartyPerson[" + set.ToString() + "," + slot.ToString() + "]," + BattlePeople[set, slot].ToString());
        }
    }

    public bool TryRead(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            return false;
        if (key.StartsWith("Person[", StringComparison.Ordinal))
            return ReadPerson(key, value);
        if (TrySlot(key, "PersonIDHelpProductionR", HelpR, value))
            return true;
        if (TrySlot(key, "PersonIDHelpProductionG", HelpG, value))
            return true;
        if (TrySlot(key, "PersonIDHelpProductionB", HelpB, value))
            return true;
        if (TrySlot(key, "PersonIDProductionPixel", Pixel, value))
            return true;
        if (TrySlot(key, "PersonIDProductionCharacter", Character, value))
            return true;
        if (key.StartsWith("BattlePartyPerson[", StringComparison.Ordinal))
            return ReadBattlePerson(key, value);
        return false;
    }

    static void WriteSlots(StreamWriter sw, string name, uint[] values)
    {
        if (values == null)
            return;
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] == 0)
                continue;
            sw.WriteLine(name + "[" + i.ToString() + "]," + values[i].ToString());
        }
    }

    static bool TrySlot(string key, string name, uint[] values, string value)
    {
        if (values == null || !key.StartsWith(name + "[", StringComparison.Ordinal))
            return false;
        int open = key.IndexOf('[');
        int close = key.IndexOf(']');
        int index;
        if (open < 0 || close <= open || !int.TryParse(key.Substring(open + 1, close - open - 1), out index))
            return true;
        if (index < 1 || index >= values.Length)
            return true;
        uint id;
        if (uint.TryParse(value, out id))
            values[index] = id;
        return true;
    }

    bool ReadPerson(string key, string value)
    {
        int open = key.IndexOf('[');
        int close = key.IndexOf(']');
        int id;
        if (open < 0 || close <= open || !int.TryParse(key.Substring(open + 1, close - open - 1), out id))
            return true;
        PersonClass person = PersonClass.Find(People, id);
        if (person == null || close + 1 >= key.Length || key[close + 1] != '.')
            return true;
        string field = key.Substring(close + 2);
        if (field == "Specialties")
        {
            person.Specialties.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                string[] parts = value.Split('|');
                for (int i = 0; i < parts.Length; i++)
                {
                    int number;
                    if (!int.TryParse(parts[i], out number))
                        continue;
                    if (number <= (int)CharacterAttribute.None || number > (int)CharacterAttribute.Dark)
                        continue;
                    person.Specialties.Add((CharacterAttribute)number);
                }
            }
            return true;
        }
        if (field.StartsWith("Pixel.", StringComparison.Ordinal))
            return ReadPixel(person, field, value);
        int numberValue;
        long longValue;
        if (field == "RgbLevelR")
            person.RgbLevel[0] = ParseLevel(value);
        else if (field == "RgbLevelG")
            person.RgbLevel[1] = ParseLevel(value);
        else if (field == "RgbLevelB")
            person.RgbLevel[2] = ParseLevel(value);
        else if (field == "RgbExpR" && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            person.RgbExp[0] = longValue < 0 ? 0 : longValue;
        else if (field == "RgbExpG" && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            person.RgbExp[1] = longValue < 0 ? 0 : longValue;
        else if (field == "RgbExpB" && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            person.RgbExp[2] = longValue < 0 ? 0 : longValue;
        else if (field == "RgbRemainderR" && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            person.RgbRemainder[0] = longValue < 0 ? 0 : longValue;
        else if (field == "RgbRemainderG" && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            person.RgbRemainder[1] = longValue < 0 ? 0 : longValue;
        else if (field == "RgbRemainderB" && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            person.RgbRemainder[2] = longValue < 0 ? 0 : longValue;
        else if (field == "CharacterLevel")
            person.CharacterLevel = ParseLevel(value);
        else if (field == "CharacterExp" && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            person.CharacterExp = longValue < 0 ? 0 : longValue;
        else if (field == "PixelLevel")
            person.PixelLevel = ParseLevel(value);
        else if (field == "PixelExp" && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            person.PixelExp = longValue < 0 ? 0 : longValue;
        else if (field == "BattleLevel")
            person.BattleLevel = ParseLevel(value);
        else if (field == "BattleExp" && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            person.BattleExp = longValue < 0 ? 0 : longValue;
        else if (field == "Affection" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out numberValue))
            person.Affection = numberValue < 0 ? 0 : numberValue;
        else if (int.TryParse(value, out numberValue))
            return true;
        return true;
    }

    static bool ReadPixel(PersonClass person, string field, string value)
    {
        string[] parts = field.Split('.');
        if (parts.Length != 5)
            return true;
        int r;
        int g;
        int b;
        if (!int.TryParse(parts[1], out r) || !int.TryParse(parts[2], out g) || !int.TryParse(parts[3], out b))
            return true;
        if (r < 0 || g < 0 || b < 0 || r > 255 || g > 255 || b > 255)
            return true;
        long number;
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) || number < 0)
            number = 0;
        if (parts[4] == "Level")
            person.FoldLoadedPixelLevel(number > int.MaxValue ? int.MaxValue : (int)number);
        else if (parts[4] == "Exp")
            person.FoldLoadedPixelExp(number);
        return true;
    }

    bool ReadBattlePerson(string key, string value)
    {
        if (BattlePeople == null)
            return true;
        int open = key.IndexOf('[');
        int comma = key.IndexOf(',');
        int close = key.IndexOf(']');
        int set;
        int slot;
        if (open < 0 || comma <= open || close <= comma)
            return true;
        if (!int.TryParse(key.Substring(open + 1, comma - open - 1), out set))
            return true;
        if (!int.TryParse(key.Substring(comma + 1, close - comma - 1), out slot))
            return true;
        if (set < 1 || set >= BattlePeople.GetLength(0) || slot < 1 || slot >= BattlePeople.GetLength(1))
            return true;
        uint id;
        if (uint.TryParse(value, out id))
            BattlePeople[set, slot] = id;
        return true;
    }

    static int ParseLevel(string value)
    {
        int level;
        if (!int.TryParse(value, out level) || level < 1)
            return 1;
        return level;
    }

    static string JoinSpecialties(PersonClass person)
    {
        if (person.Specialties.Count == 0)
            return "";
        string text = "";
        for (int i = 0; i < person.Specialties.Count; i++)
        {
            if (i > 0)
                text += "|";
            text += ((int)person.Specialties[i]).ToString();
        }
        return text;
    }
}
