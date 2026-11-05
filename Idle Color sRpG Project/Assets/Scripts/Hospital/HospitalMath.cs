using System;
using System.Globalization;

// 治療時間。非透過1pxにつき、担当なしは5秒。担当キャラは画像の一辺で、8は4秒、16は3秒、32は2秒、64は1秒、128以上は0.5秒。
// 人の担当はレベル1が5秒。回復した相手の非透過ピクセル数が経験値になる。
// 最初の区切りは 4096。その後は 8192、16384、32768 と倍になり、次のサイズと同じ速度になる。
public static class HospitalMath
{
    public const int MillisecondsWithoutNurse = 5000;
    public const int PersonNurseSize = 8;
    public const int SpeedUpMilliseconds = 5000;
    public const int PersonLevelStep = 64 * 64;
    static readonly int[] PersonMilliseconds = { 5000, 4000, 3000, 2000, 1000, 500 };

    public static int MillisecondsPerPixel(int nurseSize, bool nurse)
    {
        if (!nurse)
            return MillisecondsWithoutNurse;
        return MillisecondsForSize(nurseSize);
    }

    public static int MillisecondsForSize(int size)
    {
        if (size <= 8)
            return 4000;
        if (size <= 16)
            return 3000;
        if (size <= 32)
            return 2000;
        if (size < 128)
            return 1000;
        return 500;
    }

    public static int MillisecondsForPerson(int experience)
    {
        int step = LevelFromExperience(experience);
        if (step < 0)
            step = 0;
        if (step >= PersonMilliseconds.Length)
            step = PersonMilliseconds.Length - 1;
        return PersonMilliseconds[step];
    }

    public static int PersonEquivalentSize(int experience)
    {
        int step = LevelFromExperience(experience);
        if (step <= 0)
            return 0;
        int size = 8;
        for (int i = 1; i < step && size < 128; i++)
            size *= 2;
        return size;
    }

    public static int LevelFromExperience(int experience)
    {
        if (experience < PersonLevelStep)
            return 0;
        int level = 0;
        int need = PersonLevelStep;
        while (experience >= need && level < 24)
        {
            level++;
            if (need > int.MaxValue / 2)
                break;
            need *= 2;
        }
        return level;
    }

    public static void NurseLevelProgress(int experience, out int level, out int into, out int need)
    {
        if (experience < 0)
            experience = 0;
        int levelIndex = 0;
        int previous = 0;
        int threshold = PersonLevelStep;
        while (experience >= threshold && levelIndex < 24)
        {
            levelIndex++;
            previous = threshold;
            if (threshold > int.MaxValue / 2)
                break;
            threshold *= 2;
        }
        level = levelIndex + 1;
        into = experience - previous;
        need = threshold - previous;
        if (need < 1)
            need = 1;
    }

    public static double RemainingSeconds(double healedPixels, int opaquePixels, int millisecondsPerPixel)
    {
        if (opaquePixels <= 0)
            return 0;
        if (millisecondsPerPixel < 1)
            millisecondsPerPixel = 1;
        double left = opaquePixels - healedPixels;
        if (left < 0)
            left = 0;
        return left * millisecondsPerPixel / 1000.0;
    }

    public static float Gauge01(double healedPixels, int opaquePixels)
    {
        if (opaquePixels <= 0)
            return 1f;
        if (healedPixels <= 0)
            return 0f;
        double gauge = healedPixels / opaquePixels;
        if (gauge < 0)
            return 0f;
        if (gauge > 1)
            return 1f;
        return (float)gauge;
    }

    // 時間は H:MM:SS。時は桁を固定しない。
    public static string FormatSeconds(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            seconds = 0;
        double raised = seconds - 1e-9;
        if (raised < 0)
            raised = 0;
        long total = (long)Math.Ceiling(raised);
        if (total < 0)
            total = 0;
        long hours = total / 3600;
        long minutes = (total % 3600) / 60;
        long secs = total % 60;
        return hours.ToString(CultureInfo.InvariantCulture)
            + ":" + minutes.ToString("00", CultureInfo.InvariantCulture)
            + ":" + secs.ToString("00", CultureInfo.InvariantCulture);
    }
}
