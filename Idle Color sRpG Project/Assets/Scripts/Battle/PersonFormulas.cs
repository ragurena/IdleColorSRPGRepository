using System;

public enum OwnedTier
{
    Full = 0,
    Eight = 1,
    Half = 2
}

public struct AllyAuthorship
{
    public int AuthorPersonId;
    public CharacterAttribute Type;
}

public struct PersonBattleInput
{
    public int PersonId;
    public int BattleLevel;
    public CharacterAttribute Specialty0;
    public CharacterAttribute Specialty1;
    public CharacterAttribute Specialty2;
    public CharacterAttribute Specialty3;
    public CharacterAttribute Specialty4;
    public int SpecialtyCount;
}

// 人の生産量、経験値、所持区分、バトル倍率。Unity には依存しない。
public static class PersonFormulas
{
    public static int ClampChannel(int value)
    {
        if (value < 0)
            return 0;
        if (value > 255)
            return 255;
        return value;
    }

    public static CharacterAttribute ColorAttribute(int r, int g, int b)
    {
        r = ClampChannel(r);
        g = ClampChannel(g);
        b = ClampChannel(b);
        if (r > g && r > b)
            return CharacterAttribute.Fire;
        if (g > r && g > b)
            return CharacterAttribute.Grass;
        if (b > r && b > g)
            return CharacterAttribute.Water;
        int sum = r + g + b;
        if (sum > 384)
            return CharacterAttribute.Light;
        return CharacterAttribute.Dark;
    }

    public static bool IsPosterizedChannel(int value, int levels)
    {
        value = ClampChannel(value);
        if (value == 255)
            return true;
        if (levels < 2)
            levels = 2;
        int step = 256 / levels;
        if (step <= 0 || value % step != 0)
            return false;
        int index = value / step;
        return index >= 0 && index < levels;
    }

    public static bool IsPosterizedColor(int r, int g, int b, int levels)
    {
        return IsPosterizedChannel(r, levels)
            && IsPosterizedChannel(g, levels)
            && IsPosterizedChannel(b, levels);
    }

    public static bool HasSpecialty(CharacterAttribute[] specialties, CharacterAttribute attribute)
    {
        if (specialties == null || attribute == CharacterAttribute.None)
            return false;
        for (int i = 0; i < specialties.Length; i++)
        {
            if (specialties[i] == attribute)
                return true;
        }
        return false;
    }

    public static bool RgbSpecialty(CharacterAttribute[] specialties, int channel)
    {
        if (HasSpecialty(specialties, CharacterAttribute.Light))
            return true;
        if (channel == 0)
            return HasSpecialty(specialties, CharacterAttribute.Fire);
        if (channel == 1)
            return HasSpecialty(specialties, CharacterAttribute.Grass);
        if (channel == 2)
            return HasSpecialty(specialties, CharacterAttribute.Water);
        return false;
    }

    public static long RgbInitial(CharacterAttribute[] specialties, int channel, PersonBalanceConfig config)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        if (RgbSpecialty(specialties, channel))
            return config.RgbSpecialtyBase;
        return config.RgbBase;
    }

    public static long PixelInitial(CharacterAttribute[] specialties, int r, int g, int b, PersonBalanceConfig config)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        CharacterAttribute attribute = ColorAttribute(r, g, b);
        if (HasSpecialty(specialties, attribute))
            return config.PixelSpecialtyBase;
        return config.PixelBase;
    }

    public static long CharacterInitial(bool isAuthor, PersonBalanceConfig config)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        long initial = config.CharacterBase;
        if (initial < 1)
            initial = 1;
        if (isAuthor)
        {
            int multiplier = config.AuthorCharacterMultiplier;
            if (multiplier < 1)
                multiplier = 1;
            initial *= multiplier;
        }
        return initial;
    }

    public static long AmountAtLevel(long initial, int level, PersonBalanceConfig config)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        if (initial < 0)
            initial = 0;
        if (level < 1)
            level = 1;
        int divisor = config.LevelGrowthDivisor;
        if (divisor < 1)
            divisor = 1;
        long amount = initial;
        for (int next = 2; next <= level; next++)
        {
            long add = BattleMath.CeilDivPositive(initial * next, divisor);
            if (long.MaxValue - amount < add)
                return long.MaxValue;
            amount += add;
        }
        return amount;
    }

    public static long ExpToNext(int level, double exponent, PersonBalanceConfig config, int expBaseMultiplier = 1)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        if (level < 1)
            level = 1;
        long expBase = config.ExpBase;
        if (expBase < 1)
            expBase = 1;
        if (expBaseMultiplier < 1)
            expBaseMultiplier = 1;
        if (expBase > long.MaxValue / expBaseMultiplier)
            expBase = long.MaxValue;
        else
            expBase *= expBaseMultiplier;
        double need = expBase * Math.Pow(level, exponent);
        long result = BattleMath.CeilToLong(need);
        if (result < 1)
            result = 1;
        return result;
    }

    // 各色をまとめ階調（1〜3 は 1、4〜6 は 2、7〜9 は 3）にしてから足す。
    public static long PixelCompletionExp(int r, int g, int b)
    {
        return AttributeColorValue.AttributeBand(r)
            + AttributeColorValue.AttributeBand(g)
            + AttributeColorValue.AttributeBand(b);
    }

    public static void AddSkillExp(ref int level, ref long exp, long gain, double exponent, PersonBalanceConfig config, int expBaseMultiplier = 1)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        if (gain <= 0)
            return;
        if (level < 1)
            level = 1;
        if (exp < 0)
            exp = 0;
        if (long.MaxValue - exp < gain)
            exp = long.MaxValue;
        else
            exp += gain;

        int maxLevel = config.MaxLevel;
        if (maxLevel < 1)
            maxLevel = 1;
        int guardMax = config.MaxLevelsPerGrant;
        if (guardMax < 1)
            guardMax = 1;
        int guard = 0;
        while (level < maxLevel && guard < guardMax)
        {
            long need = ExpToNext(level, exponent, config, expBaseMultiplier);
            if (exp < need)
                break;
            exp -= need;
            level++;
            guard++;
        }
        if (level >= maxLevel)
            exp = 0;
    }

    public static long TakeRgbExp(ref long remainder, long produced, bool specialty, PersonBalanceConfig config)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        if (produced <= 0)
            return 0;
        if (remainder < 0)
            remainder = 0;
        if (long.MaxValue - remainder < produced)
            remainder = long.MaxValue;
        else
            remainder += produced;
        int batch = config.RgbExpBatch;
        if (batch < 1)
            batch = 1;
        long batches = remainder / batch;
        remainder %= batch;
        long exp = batches;
        if (specialty)
        {
            int multiplier = config.ExpSpecialtyMultiplier;
            if (multiplier < 1)
                multiplier = 1;
            if (exp > long.MaxValue / multiplier)
                exp = long.MaxValue;
            else
                exp *= multiplier;
        }
        return exp;
    }

    public static int ExpMultiplier(bool specialty, PersonBalanceConfig config)
    {
        if (!specialty)
            return 1;
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        if (config.ExpSpecialtyMultiplier < 1)
            return 1;
        return config.ExpSpecialtyMultiplier;
    }

    public static OwnedTier CompletionTier(bool workerIsPerson, bool workerIsProducedCharacter, bool workerIsAuthor)
    {
        if (workerIsPerson && workerIsAuthor)
            return OwnedTier.Full;
        if (workerIsPerson)
            return OwnedTier.Eight;
        if (workerIsProducedCharacter)
            return OwnedTier.Eight;
        return OwnedTier.Half;
    }

    public static bool AllAuthoredBy(AllyAuthorship[] allies, int personId)
    {
        if (allies == null || allies.Length == 0 || personId <= 0)
            return false;
        for (int i = 0; i < allies.Length; i++)
        {
            if (allies[i].AuthorPersonId != personId)
                return false;
        }
        return true;
    }

    public static double SpecialtyBuff(PersonBattleInput person, CharacterAttribute allyType, bool allAlliesAreTheirWorks, PersonBalanceConfig config)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        if (allAlliesAreTheirWorks)
        {
            double author = config.SpecialtyBuffAuthor;
            if (author < 1.0)
                author = 1.0;
            return author;
        }
        if (MatchesSpecialty(person, allyType))
        {
            double match = config.SpecialtyBuffMatch;
            if (match < 1.0)
                match = 1.0;
            return match;
        }
        return 1.0;
    }

    public static double Increase(double specialtyBuff, int battleLevel, PersonBalanceConfig config)
    {
        if (config == null)
            config = PersonBalanceConfig.CreateDefault();
        if (specialtyBuff < 1.0)
            specialtyBuff = 1.0;
        if (battleLevel < 1)
            battleLevel = 1;
        return (specialtyBuff - 1.0) + battleLevel * config.BattleLevelBonus;
    }

    public static double AppliedMultiplier(double creationMultiplier, double increaseSum)
    {
        if (creationMultiplier < 0.0)
            creationMultiplier = 0.0;
        if (increaseSum < 0.0)
            increaseSum = 0.0;
        return creationMultiplier + increaseSum;
    }

    public static long ScaleStat(long stat, double multiplier)
    {
        if (stat < 0)
            stat = 0;
        if (multiplier < 0.0)
            multiplier = 0.0;
        return BattleMath.CeilToLong(stat * multiplier);
    }

    public static double MultiplierForAllies(double creationMultiplier, PersonBattleInput[] people, AllyAuthorship[] allies, CharacterAttribute allyType, PersonBalanceConfig config)
    {
        double increases = 0.0;
        if (people != null)
        {
            for (int i = 0; i < people.Length; i++)
            {
                if (people[i].PersonId <= 0)
                    continue;
                bool authored = AllAuthoredBy(allies, people[i].PersonId);
                double buff = SpecialtyBuff(people[i], allyType, authored, config);
                increases += Increase(buff, people[i].BattleLevel, config);
            }
        }
        return AppliedMultiplier(creationMultiplier, increases);
    }

    static bool MatchesSpecialty(PersonBattleInput person, CharacterAttribute allyType)
    {
        if (allyType == CharacterAttribute.None || person.SpecialtyCount <= 0)
            return false;
        if (person.SpecialtyCount >= 1 && person.Specialty0 == allyType)
            return true;
        if (person.SpecialtyCount >= 2 && person.Specialty1 == allyType)
            return true;
        if (person.SpecialtyCount >= 3 && person.Specialty2 == allyType)
            return true;
        if (person.SpecialtyCount >= 4 && person.Specialty3 == allyType)
            return true;
        if (person.SpecialtyCount >= 5 && person.Specialty4 == allyType)
            return true;
        return false;
    }
}
