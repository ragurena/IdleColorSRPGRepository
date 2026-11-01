using System;
using System.Globalization;

// 治療時間。非透過1pxにつき、担当なしは2秒。担当ありは画像の一辺で短縮する。
// 人の担当は8サイズと同じ。患者が戻るたびに人だけ経験が1入り、64、128、256、512と倍の区切りで次のサイズと同じ速度になる。
// 32サイズの0.5秒より先は、区切りごとに半分になる。
public static class HospitalMath
{
    public const int MillisecondsWithoutNurse = 2000;
    public const int PersonNurseSize = 8;
    public const int SpeedUpMilliseconds = 5000;

    public static int MillisecondsPerPixel(int nurseSize, bool nurse)
    {
        if (!nurse)
            return MillisecondsWithoutNurse;
        return MillisecondsForNurse(nurseSize, 0);
    }

    public static int LevelFromExperience(int experience)
    {
        if (experience < 64)
            return 0;
        int level = 0;
        int need = 64;
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
        int threshold = 64;
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

    public static int MillisecondsForNurse(int baseSize, int experience)
    {
        int tier = SizeTier(baseSize) + LevelFromExperience(experience);
        if (tier <= 0)
            return 1500;
        if (tier == 1)
            return 1000;
        if (tier == 2)
            return 500;
        int ms = 500;
        for (int extra = 2; extra < tier && ms > 1; extra++)
            ms /= 2;
        return ms < 1 ? 1 : ms;
    }

    static int SizeTier(int size)
    {
        if (size <= 8)
            return 0;
        if (size <= 16)
            return 1;
        return 2;
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
