using System;
using System.Collections.Generic;
using System.IO;

static class HospitalTests
{
    static void Main()
    {
        int failures = Run();
        if (failures > 0)
        {
            Console.WriteLine("FAILED " + failures.ToString());
            Environment.Exit(1);
        }
        Console.WriteLine("OK");
    }

    public static int Run()
    {
        int failures = 0;
        failures += Formats();
        failures += Rates();
        failures += QueueAndOrder();
        failures += NurseKeepsProgress();
        failures += TapAndTime();
        failures += OfflineChain();
        failures += Rollback();
        failures += SameCharacterTwice();
        failures += WaitingDoesNotHeal();
        failures += SaveRoundTrip();
        failures += OldSave();
        failures += GrantOnce();
        failures += PendingUntilRoom();
        failures += ZeroPixelsReturn();
        failures += CareDoesNotHitNewcomer();
        failures += NurseLearns();
        return failures;
    }

    static int Formats()
    {
        int failures = 0;
        failures += Check(HospitalMath.FormatSeconds(64 * 2) == "0:02:08", "8x8 time");
        failures += Check(HospitalMath.FormatSeconds(256 * 2) == "0:08:32", "16x16 time");
        failures += Check(HospitalMath.FormatSeconds(1024 * 2) == "0:34:08", "32x32 time");
        failures += Check(HospitalMath.FormatSeconds(4096 * 2) == "2:16:32", "64x64 time");
        failures += Check(HospitalMath.FormatSeconds(16384 * 2) == "9:06:08", "128x128 time");
        failures += Check(HospitalMath.FormatSeconds(65536 * 2) == "36:24:32", "256x256 time");
        failures += Check(HospitalMath.FormatSeconds(262144 * 2) == "145:38:08", "512x512 time");
        failures += Check(HospitalMath.Gauge01(100, 4096) == 100f / 4096f, "gauge");
        failures += Check(HospitalMath.Gauge01(4096, 4096) == 1f, "gauge full");
        return failures;
    }

    static int Rates()
    {
        int failures = 0;
        failures += Check(HospitalMath.MillisecondsPerPixel(8, false) == 2000, "base 8");
        failures += Check(HospitalMath.MillisecondsPerPixel(64, false) == 2000, "base 64");
        failures += Check(HospitalMath.MillisecondsPerPixel(8, true) == 1500, "nurse 8");
        failures += Check(HospitalMath.MillisecondsPerPixel(16, true) == 1000, "nurse 16");
        failures += Check(HospitalMath.MillisecondsPerPixel(32, true) == 500, "nurse 32");
        failures += Check(HospitalMath.MillisecondsPerPixel(512, true) == 500, "nurse 512");
        failures += Check(HospitalMath.MillisecondsForNurse(8, 63) == 1500, "xp 63 stays size 8");
        failures += Check(HospitalMath.MillisecondsForNurse(8, 64) == 1000, "xp 64 is size 16");
        failures += Check(HospitalMath.MillisecondsForNurse(8, 128) == 500, "xp 128 is size 32");
        failures += Check(HospitalMath.MillisecondsForNurse(8, 256) == 250, "xp 256 is faster");
        failures += Check(HospitalMath.MillisecondsForNurse(8, 512) == 125, "xp 512 is faster");
        failures += Check(HospitalMath.MillisecondsForNurse(32, 64) == 250, "size 32 grows at 64");
        int level;
        int into;
        int need;
        HospitalMath.NurseLevelProgress(0, out level, out into, out need);
        failures += Check(level == 1 && into == 0 && need == 64, "nurse level 1");
        HospitalMath.NurseLevelProgress(63, out level, out into, out need);
        failures += Check(level == 1 && into == 63 && need == 64, "nurse level 1 almost");
        HospitalMath.NurseLevelProgress(64, out level, out into, out need);
        failures += Check(level == 2 && into == 0 && need == 64, "nurse level 2");
        HospitalMath.NurseLevelProgress(128, out level, out into, out need);
        failures += Check(level == 3 && into == 0 && need == 128, "nurse level 3");
        return failures;
    }

    static int NurseLearns()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        failures += Check(hospital.AssignPerson(1, 4, 0, null) >= 0, "assign person");
        hospital.Admit(1, 4, 64, 0, null);
        hospital.Admit(2, 4, 64, 0, null);
        hospital.CatchUp(1500, null);
        failures += Check(Nearly(hospital.PatientOnBed(1).HealedPixels, 1) && Nearly(hospital.PatientOnBed(2).HealedPixels, 1), "person heals like size 8");
        List<uint> returned = new List<uint>();
        hospital.CatchUp(6000, id => Grant(returned, id));
        failures += Check(returned.Count == 2 && hospital.Experience(true, 4) == 2, "each return gives 1 xp");
        HospitalState characterNurse = new HospitalState();
        characterNurse.AssignNurse(1, 3, 32, 0, null);
        characterNurse.Admit(1, 1, 8, 0, null);
        characterNurse.CatchUp(500, id => true);
        failures += Check(characterNurse.Experience(false, 3) == 0, "character nurse gains no xp");

        HospitalState grown = new HospitalState();
        grown.AssignPerson(1, 4, 0, null);
        grown.AddExperience(true, 4, 64);
        grown.Admit(1, 4, 8, 0, null);
        grown.CatchUp(1000, null);
        failures += Check(Nearly(grown.PatientOnBed(1).HealedPixels, 1), "64 xp heals at 1 sec");
        grown.AddExperience(true, 4, 64);
        grown.CatchUp(1500, null);
        failures += Check(Nearly(grown.PatientOnBed(1).HealedPixels, 2), "128 xp heals at 0.5 sec");
        StringWriter writer = new StringWriter();
        grown.Write(writer);
        HospitalState loaded = new HospitalState();
        loaded.Reset();
        string[] lines = writer.ToString().Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim('\r', ' ', '\t');
            if (line.Length == 0)
                continue;
            loaded.TryApply(line.Split(','));
        }
        loaded.Repair();
        failures += Check(loaded.PersonOnBed(1) == 4 && loaded.NurseOnBed(1) == 0 && loaded.Experience(true, 4) == 128, "saved person xp");
        failures += Check(Nearly(loaded.PatientOnBed(1).HealedPixels, 2), "saved progress after growth");
        return failures;
    }

    static int QueueAndOrder()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        List<uint> returned = new List<uint>();
        hospital.Admit(1, 4, 8, 0, id => Grant(returned, id));
        hospital.Admit(2, 4, 8, 0, id => Grant(returned, id));
        hospital.Admit(3, 4, 8, 0, id => Grant(returned, id));
        failures += Check(hospital.UnlockedBeds == 2, "initial beds");
        failures += Check(hospital.PatientOnBed(1) != null && hospital.PatientOnBed(1).CharacterId == 1, "bed1 first");
        failures += Check(hospital.PatientOnBed(2) != null && hospital.PatientOnBed(2).CharacterId == 2, "bed2 second");
        failures += Check(hospital.WaitingCount == 1 && WaitingId(hospital) == 3, "third waits");
        hospital.CatchUp(4 * 2000, id => Grant(returned, id));
        failures += Check(returned.Count == 2 && returned[0] == 1 && returned[1] == 2, "finish order");
        failures += Check(hospital.PatientOnBed(1) != null && hospital.PatientOnBed(1).CharacterId == 3, "waiter takes bed");
        failures += Check(hospital.WaitingCount == 0, "queue empty");
        return failures;
    }

    static int NurseKeepsProgress()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        hospital.Admit(7, 4, 8, 0, null);
        hospital.CatchUp(2000, null);
        HospitalPatient patient = hospital.PatientOnBed(1);
        failures += Check(patient != null && Nearly(patient.HealedPixels, 1), "2 sec per px");
        double kept = patient.HealedPixels;
        failures += Check(hospital.AssignNurse(1, 9, 8, 2000, null) >= 0, "assign nurse");
        failures += Check(hospital.IsNurse(9) && hospital.NurseOnBed(1) == 9 && hospital.NurseOnBed(2) == 9, "nurse covers the card");
        hospital.TryGrantBed("nurse-card", 2000, null);
        hospital.TryGrantBed("nurse-card-2", 2000, null);
        failures += Check(hospital.AssignNurse(2, 9, 8, 2000, null) < 0, "nurse unique");
        patient = hospital.PatientOnBed(1);
        failures += Check(patient != null && Nearly(patient.HealedPixels, kept), "progress kept");
        hospital.CatchUp(2000 + 1500, null);
        patient = hospital.PatientOnBed(1);
        failures += Check(patient != null && Nearly(patient.HealedPixels, 2), "8 nurse 1.5 sec");

        HospitalState smallNurse = new HospitalState();
        smallNurse.Admit(4, 4, 64, 0, null);
        smallNurse.AssignNurse(1, 3, 8, 0, null);
        smallNurse.CatchUp(1500, null);
        failures += Check(Nearly(smallNurse.PatientOnBed(1).HealedPixels, 1), "large patient still follows 8 nurse");

        HospitalState wide = new HospitalState();
        wide.Admit(4, 4, 8, 0, null);
        wide.AssignNurse(1, 3, 16, 0, null);
        wide.CatchUp(1000, null);
        failures += Check(Nearly(wide.PatientOnBed(1).HealedPixels, 1), "16 nurse 1 sec");

        HospitalState big = new HospitalState();
        big.Admit(5, 4, 8, 0, null);
        big.AssignNurse(1, 3, 32, 0, null);
        big.CatchUp(500, null);
        failures += Check(Nearly(big.PatientOnBed(1).HealedPixels, 1), "32 nurse 0.5 sec");
        big.CatchUp(500 + 250, null);
        failures += Check(Nearly(big.PatientOnBed(1).HealedPixels, 1.5), "half pixel");

        HospitalState pair = new HospitalState();
        pair.Admit(1, 4, 64, 0, null);
        pair.Admit(2, 4, 64, 0, null);
        pair.AssignNurse(1, 8, 8, 0, null);
        pair.CatchUp(1500, null);
        failures += Check(Nearly(pair.PatientOnBed(1).HealedPixels, 1) && Nearly(pair.PatientOnBed(2).HealedPixels, 1), "card nurse heals both");
        return failures;
    }

    static int TapAndTime()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        hospital.Admit(1, 10, 8, 0, null);
        hospital.CatchUp(2000, null);
        double before = hospital.RemainingSeconds(hospital.PatientOnBed(1));
        HospitalStep step = hospital.Care(0, 2000, null);
        failures += Check(step.Tapped == 1, "one tap");
        failures += Check(Nearly(hospital.PatientOnBed(1).HealedPixels, 3.5), "tap is 5 seconds");
        failures += Check(Nearly(hospital.RemainingSeconds(hospital.PatientOnBed(1)), before - 5), "five seconds shorter");
        hospital.CatchUp(3000, null);
        failures += Check(Nearly(hospital.PatientOnBed(1).HealedPixels, 4), "time after tap");
        HospitalState fast = new HospitalState();
        fast.Admit(1, 40, 8, 0, null);
        fast.AssignNurse(1, 3, 32, 0, null);
        double fastBefore = fast.RemainingSeconds(fast.PatientOnBed(1));
        fast.CareBed(1, 0, null);
        failures += Check(Nearly(fast.PatientOnBed(1).HealedPixels, 10), "32 nurse tap is 10 px");
        failures += Check(Nearly(fast.RemainingSeconds(fast.PatientOnBed(1)), fastBefore - 5), "nurse tap still 5 seconds");
        List<uint> returned = new List<uint>();
        HospitalState finish = new HospitalState();
        finish.Admit(1, 3, 8, 0, null);
        finish.CatchUp(2000, null);
        step = finish.Care(0, 2000, id => Grant(returned, id));
        failures += Check(step.Tapped == 1 && returned.Count == 1 && finish.PatientOnBed(1) == null, "tap under 5 seconds returns");
        step = finish.Care(0, 2000, id => Grant(returned, id));
        failures += Check(step.Tapped == 0 && returned.Count == 1, "tap after complete ignored");
        return failures;
    }

    static int OfflineChain()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        List<uint> returned = new List<uint>();
        hospital.Admit(1, 2, 32, 1000, id => Grant(returned, id));
        hospital.Admit(2, 2, 32, 1000, id => Grant(returned, id));
        hospital.Admit(3, 2, 32, 1000, id => Grant(returned, id));
        long now = 1000 + 2 * 2000 + 500;
        hospital.CatchUp(now, id => Grant(returned, id));
        failures += Check(returned.Count == 2, "offline finished two");
        HospitalPatient third = hospital.PatientOnBed(1);
        if (third == null || third.CharacterId != 3)
            third = hospital.PatientOnBed(2);
        failures += Check(third != null && third.CharacterId == 3, "third entered offline");
        failures += Check(third != null && Nearly(third.HealedPixels, 0.25), "offline partial after turnover");
        failures += Check(hospital.WaitingCount == 0, "no waiter left");
        return failures;
    }

    static int Rollback()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        hospital.Admit(1, 10, 8, 5000, null);
        hospital.CatchUp(1000, null);
        HospitalPatient patient = hospital.PatientOnBed(1);
        failures += Check(patient != null && Nearly(patient.HealedPixels, 0), "rollback adds nothing");
        failures += Check(patient.LastCalcUnixMs == 1000, "clock clamped");
        return failures;
    }

    static int SameCharacterTwice()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        ulong first = hospital.Admit(4, 5, 8, 0, null);
        ulong second = hospital.Admit(4, 5, 8, 1, null);
        failures += Check(first != 0 && second != 0 && first != second, "separate spirits");
        failures += Check(hospital.PatientCount == 2, "both stay");
        failures += Check(hospital.PatientOnBed(1).Id == first && hospital.PatientOnBed(2).Id == second, "both bedded");
        return failures;
    }

    static int WaitingDoesNotHeal()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        hospital.Admit(1, 100, 8, 0, null);
        hospital.Admit(2, 100, 8, 0, null);
        hospital.Admit(3, 100, 8, 0, null);
        hospital.CatchUp(20000, null);
        List<HospitalPatient> waiting = new List<HospitalPatient>();
        hospital.CopyWaiting(waiting);
        failures += Check(waiting.Count == 1 && Nearly(waiting[0].HealedPixels, 0), "waiter stays at zero");
        return failures;
    }

    static int SaveRoundTrip()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        hospital.TryGrantBed("quest-bed", 0, null);
        hospital.Admit(12, 64, 8, 10, null);
        hospital.AssignNurse(1, 3, 32, 10, null);
        hospital.CatchUp(10 + 500, null);
        StringWriter writer = new StringWriter();
        hospital.Write(writer);
        HospitalState loaded = new HospitalState();
        loaded.Reset();
        string[] lines = writer.ToString().Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim('\r', ' ', '\t');
            if (line.Length == 0)
                continue;
            loaded.TryApply(line.Split(','));
        }
        loaded.Repair();
        failures += Check(loaded.UnlockedBeds == 3, "saved beds");
        failures += Check(loaded.NurseOnBed(1) == 3 && loaded.NurseOnBed(2) == 3 && loaded.NurseSize(1) == 32, "saved nurse");
        HospitalPatient patient = loaded.PatientOnBed(1);
        failures += Check(patient != null && patient.CharacterId == 12 && patient.OpaquePixels == 64, "saved patient");
        failures += Check(patient != null && Nearly(patient.HealedPixels, 1), "saved fraction");
        failures += Check(!loaded.TryGrantBed("quest-bed", 0, null), "grant restored once");
        return failures;
    }

    static int OldSave()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        hospital.Admit(1, 4, 8, 0, null);
        hospital.Reset();
        hospital.TryApply(new string[] { "CurR", "1" });
        hospital.Repair();
        failures += Check(hospital.UnlockedBeds == 2 && hospital.PatientCount == 0 && hospital.PendingCount == 0, "missing hospital keys");
        HospitalState legacy = new HospitalState();
        legacy.TryApply(new string[] { "HospitalNurse", "1", "9" });
        legacy.FillMissingNurseSizes(id => id == 9 ? 16 : 0);
        failures += Check(legacy.NurseOnBed(1) == 9 && legacy.NurseSize(1) == 16, "nurse size filled from character");
        HospitalState paired = new HospitalState();
        paired.TryApply(new string[] { "HospitalNurse", "2", "4", "32" });
        failures += Check(paired.NurseOnBed(1) == 4 && paired.NurseOnBed(2) == 4 && paired.NurseSize(2) == 32, "old bed nurse becomes the card nurse");
        return failures;
    }

    static int GrantOnce()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        failures += Check(hospital.TryGrantBed("quest-a", 0, null), "first grant");
        failures += Check(hospital.UnlockedBeds == 3, "bed became 3");
        failures += Check(!hospital.TryGrantBed("quest-a", 0, null), "duplicate grant");
        failures += Check(hospital.UnlockedBeds == 3, "bed stays 3");
        failures += Check(hospital.TryGrantBed("quest-b", 0, null) && hospital.UnlockedBeds == 4, "another source");
        return failures;
    }

    static int PendingUntilRoom()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        bool room = false;
        hospital.Admit(6, 1, 8, 0, id => room);
        hospital.CatchUp(2000, id => room);
        failures += Check(hospital.PendingCount == 1 && hospital.PatientOnBed(1) == null, "waits for room");
        room = true;
        List<uint> got = new List<uint>();
        hospital.CatchUp(2000, id =>
        {
            if (!room)
                return false;
            got.Add(id);
            return true;
        });
        failures += Check(got.Count == 1 && got[0] == 6 && hospital.PendingCount == 0, "returns when room");
        return failures;
    }

    static int ZeroPixelsReturn()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        List<uint> got = new List<uint>();
        hospital.Admit(2, 0, 8, 50, id => Grant(got, id));
        failures += Check(got.Count == 1 && hospital.PatientCount == 0, "transparent finishes now");
        return failures;
    }

    static int CareDoesNotHitNewcomer()
    {
        int failures = 0;
        HospitalState hospital = new HospitalState();
        List<uint> got = new List<uint>();
        hospital.Admit(1, 1, 8, 0, id => Grant(got, id));
        hospital.Admit(2, 10, 8, 0, id => Grant(got, id));
        hospital.Admit(3, 10, 8, 0, id => Grant(got, id));
        HospitalStep step = hospital.Care(0, 0, id => Grant(got, id));
        failures += Check(step.Tapped == 2 && got.Count == 1 && got[0] == 1, "card tap hits both beds");
        HospitalPatient newcomer = hospital.PatientOnBed(1);
        failures += Check(newcomer != null && newcomer.CharacterId == 3 && Nearly(newcomer.HealedPixels, 0), "newcomer not tapped");
        HospitalState oneSide = new HospitalState();
        oneSide.Admit(1, 4, 8, 0, null);
        oneSide.Admit(2, 4, 8, 0, null);
        HospitalStep left = oneSide.CareBed(1, 0, null);
        failures += Check(left.Tapped == 1 && Nearly(oneSide.PatientOnBed(1).HealedPixels, 2.5), "left care hits left bed");
        failures += Check(Nearly(oneSide.PatientOnBed(2).HealedPixels, 0), "left care skips right bed");
        return failures;
    }

    static uint WaitingId(HospitalState hospital)
    {
        List<HospitalPatient> waiting = new List<HospitalPatient>();
        hospital.CopyWaiting(waiting);
        if (waiting.Count == 0)
            return 0;
        return waiting[0].CharacterId;
    }

    static bool Grant(List<uint> returned, uint id)
    {
        returned.Add(id);
        return true;
    }

    static bool Nearly(double value, double expected)
    {
        return Math.Abs(value - expected) < 0.0001;
    }

    static int Check(bool condition, string name)
    {
        if (condition)
            return 0;
        Console.WriteLine("FAIL " + name);
        return 1;
    }
}
