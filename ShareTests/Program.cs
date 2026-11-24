using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

static class Program
{
    static int _failures;

    static void Main()
    {
        TestLevels();
        TestLoadCount();
        TestCharacterIds();
        TestRoundTrip();
        TestRejects();
        if (_failures > 0)
        {
            Console.WriteLine("FAILED " + _failures);
            Environment.Exit(1);
        }
        Console.WriteLine("OK");
    }

    static void TestLevels()
    {
        Eq(ShareStageRules.BandBaseLevel(1), 5);
        Eq(ShareStageRules.BandBaseLevel(10), 5);
        Eq(ShareStageRules.BandBaseLevel(11), 6);
        Eq(ShareStageRules.BandBaseLevel(100), 14);
        Eq(ShareStageRules.BandBaseLevel(101), 10);
        Eq(ShareStageRules.BandBaseLevel(200), 19);
        Eq(ShareStageRules.BandBaseLevel(201), 15);
        Eq(ShareStageRules.BandBaseLevel(291), 24);
        Eq(ShareStageRules.BandBaseLevel(299), 24);
        Eq(ShareStageRules.BandBaseLevel(300), 30);
        Eq(ShareStageRules.LevelCap(1), 30);
        Eq(ShareStageRules.LevelCap(50), 50);
        Eq(ShareStageRules.EnemyLevel(300, 0, 30), 30);
        Eq(ShareStageRules.EnemyLevel(300, 10, 30), 30);
        Eq(ShareStageRules.EnemyLevel(1, 10, 50), 15);
        Eq(ShareStageRules.EnemyLevel(300, 25, 50), 50);
        Eq(ShareStageRules.EnemyCount(1), 1);
        Eq(ShareStageRules.EnemyCount(11), 1);
        Eq(ShareStageRules.EnemyCount(21), 2);
        Eq(ShareStageRules.EnemyCount(91), 9);
        Eq(ShareStageRules.EnemyCount(191), 9);
        Eq(ShareStageRules.WindowStart(0), 1);
        Eq(ShareStageRules.WindowStart(100), 101);
        Eq(ShareStageRules.WindowStart(300), 201);
        True(ShareStageRules.IsPlayableWindow(300, 201, 300));
        True(ShareStageRules.IsPlayableWindow(0, 1, 100));
        True(ShareStageRules.IsPlayableWindow(300, 201, 300));
    }

    static void TestLoadCount()
    {
        int total = 0;
        Dictionary<string, string> seen = new Dictionary<string, string>();
        string author = "11111111-1111-1111-1111-111111111111";
        string stage = "22222222-2222-2222-2222-222222222222";
        int before = total;
        True(ShareStageRules.NoteLoad(ref total, seen, author, stage, "2026-11-24T01:00:00Z"));
        True(ShareStageRules.Increased(before, total));
        Eq(total, 1);
        before = total;
        True(ShareStageRules.NoteLoad(ref total, seen, author, stage, "2026-11-24T20:00:00Z"));
        True(!ShareStageRules.Increased(before, total));
        before = total;
        True(ShareStageRules.NoteLoad(ref total, seen, author, stage, "2026-11-23T20:00:00Z"));
        True(!ShareStageRules.Increased(before, total));
        before = total;
        True(ShareStageRules.NoteLoad(ref total, seen, author, stage, "2026-11-25T00:00:00Z"));
        True(ShareStageRules.Increased(before, total));
        Eq(total, 2);
        Eq(ShareStageRules.LoadBonus(1), 0);
        Eq(ShareStageRules.LoadBonus(2), 1);
        True(ShareStageRules.CanEarnStar(0, 0));
        True(!ShareStageRules.CanEarnStar(1, 0));
        int bonus;
        True(!ShareStageRules.RaiseBonusAfterStar(0, 0, out bonus));
        Eq(bonus, 0);
        True(ShareStageRules.RaiseBonusAfterStar(1, 0, out bonus));
        Eq(bonus, 1);
        True(ShareStageRules.CanEarnStar(1, bonus));
        True(!ShareStageRules.RaiseBonusAfterStar(1, 1, out bonus));
    }

    static void TestCharacterIds()
    {
        SharedCharacterIds ids = new SharedCharacterIds();
        string authorA = "11111111-1111-1111-1111-111111111111";
        string authorB = "22222222-2222-2222-2222-222222222222";
        string error;
        True(ids.TryAdd(authorA, new[] { "a", "b", "c" }, out error));
        Eq((int)ids.IdOf(authorA, "a"), 20000);
        Eq((int)ids.IdOf(authorA, "b"), 20001);
        Eq((int)ids.IdOf(authorA, "c"), 20002);
        True(ids.TryAdd(authorB, new[] { "d", "e" }, out error));
        Eq((int)ids.IdOf(authorB, "d"), 20050);
        Eq((int)ids.IdOf(authorB, "e"), 20051);
        True(ids.TryAdd(authorA, new[] { "a", "c", "f", "g" }, out error));
        Eq((int)ids.IdOf(authorA, "a"), 20000);
        Eq((int)ids.IdOf(authorA, "c"), 20002);
        Eq((int)ids.IdOf(authorA, "f"), 20003);
        Eq((int)ids.IdOf(authorA, "g"), 20004);
        Eq(ids.HashCount(authorA), 5);

        SharedCharacterIds more = new SharedCharacterIds();
        List<string> fifty = new List<string>();
        for (int i = 0; i < 50; i++)
            fifty.Add("n" + i.ToString());
        True(more.TryAdd(authorA, fifty, out error));
        Eq((int)more.IdOf(authorA, "n0"), 20000);
        Eq((int)more.IdOf(authorA, "n49"), 20049);
        True(more.TryAdd(authorB, new[] { "b0" }, out error));
        Eq((int)more.IdOf(authorB, "b0"), 20050);
        True(more.TryAdd(authorA, new[] { "n50", "n51" }, out error));
        Eq((int)more.IdOf(authorA, "n49"), 20049);
        Eq((int)more.IdOf(authorA, "n50"), 20100);
        Eq((int)more.IdOf(authorA, "n51"), 20101);
        Eq((int)more.IdOf(authorB, "b0"), 20050);
        Eq(more.HashCount(authorA), 52);
        int blocks = more.AuthorCount;
        int open = more.OpenCount(authorA);
        True(more.TryAdd(authorA, new[] { "n52" }, out error));
        more.Revert(authorA, blocks, open);
        Eq((int)more.IdOf(authorA, "n52"), 0);
        Eq((int)more.IdOf(authorA, "n51"), 20101);
    }

    static void TestRoundTrip()
    {
        PiquaDocument document = Sample();
        byte[] zip = PiquaCodec.Build(document);
        PiquaDocument parsed;
        string error;
        if (!PiquaCodec.TryParse(zip, out parsed, out error))
        {
            Fail(error ?? "parse");
            return;
        }
        Eq(parsed.Characters.Count, 1);
        Eq(parsed.StageName, "森");
        Eq(parsed.Revision, 3);
        Eq(parsed.Characters[0].Level, 12);
        Eq(parsed.Characters[0].Hp, 40);

        document.Characters.Clear();
        for (int i = 1; i <= 9; i++)
        {
            PiquaCharacter character = SampleCharacter(i);
            character.Slot = i;
            character.FileName = ShareStageRules.CharacterFileName(i);
            document.Characters.Add(character);
        }
        zip = PiquaCodec.Build(document);
        True(PiquaCodec.TryParse(zip, out parsed, out error));
        Eq(parsed.Characters.Count, 9);
        True(LocalHeadersAreSized(zip));
    }

    static void TestRejects()
    {
        PiquaDocument document = Sample();
        byte[] zip = PiquaCodec.Build(document);
        zip[zip.Length / 2] ^= 0x5a;
        PiquaDocument parsed;
        string error;
        True(!PiquaCodec.TryParse(zip, out parsed, out error));

        document = Sample();
        document.Characters[0].Png = new byte[8];
        zip = PiquaCodec.Build(document);
        True(!PiquaCodec.TryParse(zip, out parsed, out error));

        document = Sample();
        document.FormatVersion = 99;
        document.ContentHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        byte[] manifest = Encoding.UTF8.GetBytes(PiquaCodec.ManifestJson(document));
        zip = Swap(PiquaCodec.Build(Sample()), "manifest.json", manifest);
        True(!PiquaCodec.TryParse(zip, out parsed, out error));
        Eq(error, "アプリのバージョンが古いため、このステージを読めません。");

        True(!ShareStageRules.IsSafeEntryName("../stage.json"));
        True(!ShareStageRules.IsSafeEntryName("characters/../../stage.json"));
        True(ShareStageRules.IsSafeEntryName("characters/character_001.png"));

        byte[] bomb = ZipWithEntry("../manifest.json", Encoding.UTF8.GetBytes("{}"));
        True(!PiquaCodec.TryParse(bomb, out parsed, out error));
    }

    static PiquaDocument Sample()
    {
        PiquaDocument document = new PiquaDocument();
        document.FileId = "33333333-3333-3333-3333-333333333333";
        document.StageId = "22222222-2222-2222-2222-222222222222";
        document.AuthorId = "11111111-1111-1111-1111-111111111111";
        document.AuthorName = "ヒトハ";
        document.CreatedAt = "2026-11-24T00:00:00Z";
        document.UpdatedAt = "2026-11-24T01:00:00Z";
        document.Revision = 3;
        document.StageName = "森";
        document.Comment = "こんにちは";
        document.MainR = 12;
        document.MainG = 34;
        document.MainB = 56;
        document.Background = "nekohatakouen";
        document.Characters.Add(SampleCharacter(1));
        return document;
    }

    static PiquaCharacter SampleCharacter(int slot)
    {
        return new PiquaCharacter
        {
            Slot = slot,
            Name = "スライム",
            FileName = ShareStageRules.CharacterFileName(slot),
            Size = 8,
            Level = 12,
            FusionCount = 2,
            Attribute = "Fire",
            Hp = 40,
            Atk = 5,
            Def = 3,
            Spd = 4,
            Luc = 1,
            Obs = 2,
            Png = TinyPng(8)
        };
    }

    static byte[] TinyPng(int size)
    {
        byte[] png = new byte[24];
        png[0] = 137;
        png[1] = 80;
        png[2] = 78;
        png[3] = 71;
        png[4] = 13;
        png[5] = 10;
        png[6] = 26;
        png[7] = 10;
        png[11] = 13;
        png[12] = (byte)'I';
        png[13] = (byte)'H';
        png[14] = (byte)'D';
        png[15] = (byte)'R';
        png[16] = (byte)((size >> 24) & 255);
        png[17] = (byte)((size >> 16) & 255);
        png[18] = (byte)((size >> 8) & 255);
        png[19] = (byte)(size & 255);
        png[20] = png[16];
        png[21] = png[17];
        png[22] = png[18];
        png[23] = png[19];
        return png;
    }

    static bool LocalHeadersAreSized(byte[] zip)
    {
        int found = 0;
        for (int i = 0; i + 30 < zip.Length; i++)
        {
            if (zip[i] != 0x50 || zip[i + 1] != 0x4b || zip[i + 2] != 3 || zip[i + 3] != 4)
                continue;
            int flags = zip[i + 6] | (zip[i + 7] << 8);
            if ((flags & 1) != 0 || (flags & 8) != 0)
                return false;
            found++;
        }
        return found >= 3;
    }

    static byte[] Swap(byte[] zip, string name, byte[] body)
    {
        using (MemoryStream input = new MemoryStream(zip))
        using (MemoryStream output = new MemoryStream())
        {
            using (ZipArchive source = new ZipArchive(input, ZipArchiveMode.Read, true))
            using (ZipArchive dest = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                for (int i = 0; i < source.Entries.Count; i++)
                {
                    ZipArchiveEntry entry = source.Entries[i];
                    byte[] bytes;
                    if (entry.FullName == name)
                        bytes = body;
                    else
                    {
                        using (Stream read = entry.Open())
                        using (MemoryStream copy = new MemoryStream())
                        {
                            read.CopyTo(copy);
                            bytes = copy.ToArray();
                        }
                    }
                    ZipArchiveEntry created = dest.CreateEntry(entry.FullName);
                    using (Stream write = created.Open())
                        write.Write(bytes, 0, bytes.Length);
                }
            }
            return output.ToArray();
        }
    }

    static byte[] ZipWithEntry(string name, byte[] body)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (ZipArchive dest = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry created = dest.CreateEntry(name);
                using (Stream write = created.Open())
                    write.Write(body, 0, body.Length);
            }
            return output.ToArray();
        }
    }

    static void Eq(int actual, int expected)
    {
        if (actual != expected)
            Fail(actual + " != " + expected);
    }

    static void Eq(long actual, long expected)
    {
        if (actual != expected)
            Fail(actual + " != " + expected);
    }

    static void Eq(string actual, string expected)
    {
        if (actual != expected)
            Fail(actual + " != " + expected);
    }

    static void True(bool value)
    {
        if (!value)
            Fail("expected true");
    }

    static void Fail(string message)
    {
        _failures++;
        Console.WriteLine(message);
    }
}
