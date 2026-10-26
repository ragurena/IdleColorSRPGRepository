using System;

static class PersonTests
{
    public static int Run()
    {
        int failures = 0;
        PersonBalanceConfig config = PersonBalanceConfig.CreateDefault();
        failures += Check(PersonFormulas.AmountAtLevel(10, 1, config) == 10, "rgb lv1");
        failures += Check(PersonFormulas.AmountAtLevel(10, 2, config) == 12, "rgb lv2");
        failures += Check(PersonFormulas.AmountAtLevel(10, 3, config) == 15, "rgb lv3");
        failures += Check(PersonFormulas.AmountAtLevel(50, 3, config) == 75, "fire lv3");
        failures += Check(PersonFormulas.ExpToNext(1, 1.7, config) == 100, "exp lv1");
        failures += Check(PersonFormulas.ExpToNext(2, 1.7, config) == 325, "exp rgb 2");
        failures += Check(PersonFormulas.ExpToNext(1, 1.6, config, 3) == 300, "pixel exp base lv1");
        failures += Check(PersonFormulas.ExpToNext(2, 1.6, config, 3) == 910, "pixel exp base lv2");
        failures += Check(PersonFormulas.ExpToNext(1, 1.5, config, 8) == 800, "character exp base lv1");
        failures += Check(PersonFormulas.ExpToNext(2, 1.5, config, 8) == 2263, "character exp base lv2");
        failures += Check(PersonFormulas.PixelCompletionExp(0, 0, 0) == 3, "pixel exp black");
        failures += Check(PersonFormulas.PixelCompletionExp(32, 0, 0) == 3, "pixel exp 32");
        failures += Check(PersonFormulas.PixelCompletionExp(64, 64, 0) == 3, "pixel exp 64");
        failures += Check(PersonFormulas.PixelCompletionExp(96, 0, 0) == 4, "pixel exp 96");
        failures += Check(PersonFormulas.PixelCompletionExp(255, 255, 255) == 9, "pixel exp 255");
        failures += Check(PersonFormulas.CharacterInitial(false, config) == 1, "char base");
        failures += Check(PersonFormulas.CharacterInitial(true, config) == 2, "char author");
        failures += Check(PersonFormulas.AmountAtLevel(2, 3, config) == 4, "author lv3");

        CharacterAttribute[] fire = { CharacterAttribute.Fire };
        CharacterAttribute[] light = { CharacterAttribute.Light };
        CharacterAttribute[] dark = { CharacterAttribute.Dark };
        CharacterAttribute[] fireLight = { CharacterAttribute.Fire, CharacterAttribute.Light };
        failures += Check(PersonFormulas.RgbInitial(fire, 0, config) == 50, "fire R");
        failures += Check(PersonFormulas.RgbInitial(fire, 1, config) == 10, "fire G");
        failures += Check(PersonFormulas.RgbInitial(dark, 0, config) == 10, "dark R");
        failures += Check(PersonFormulas.RgbInitial(light, 2, config) == 50, "light B");
        failures += Check(PersonFormulas.RgbInitial(fireLight, 0, config) == 50, "stack R");

        failures += Check(PersonFormulas.ColorAttribute(200, 100, 100) == CharacterAttribute.Fire, "color fire");
        failures += Check(PersonFormulas.ColorAttribute(100, 200, 100) == CharacterAttribute.Grass, "color grass");
        failures += Check(PersonFormulas.ColorAttribute(100, 100, 200) == CharacterAttribute.Water, "color water");
        failures += Check(PersonFormulas.ColorAttribute(150, 150, 100) == CharacterAttribute.Light, "color light");
        failures += Check(PersonFormulas.ColorAttribute(128, 128, 128) == CharacterAttribute.Dark, "color dark");
        failures += Check(PersonFormulas.PixelInitial(fire, 200, 100, 100, config) == 25, "pixel fire");
        failures += Check(PersonFormulas.PixelInitial(fire, 128, 128, 128, config) == 10, "pixel plain");
        failures += Check(PersonFormulas.PixelInitial(dark, 128, 128, 128, config) == 25, "pixel dark");
        failures += Check(PersonFormulas.IsPosterizedColor(224, 32, 255, 8), "posterized");
        failures += Check(!PersonFormulas.IsPosterizedColor(1, 0, 0, 8), "not posterized");

        long remainder = 0;
        failures += Check(PersonFormulas.TakeRgbExp(ref remainder, 100, true, config) == 0 && remainder == 100, "rgb carry");
        failures += Check(PersonFormulas.TakeRgbExp(ref remainder, 200, true, config) == 3 && remainder == 45, "rgb batch");
        remainder = 0;
        failures += Check(PersonFormulas.TakeRgbExp(ref remainder, 255, false, config) == 1 && remainder == 0, "rgb plain");

        failures += Check(PersonFormulas.CompletionTier(true, false, true) == OwnedTier.Full, "tier author");
        failures += Check(PersonFormulas.CompletionTier(true, false, false) == OwnedTier.Eight, "tier person");
        failures += Check(PersonFormulas.CompletionTier(false, true, false) == OwnedTier.Eight, "tier self");
        failures += Check(PersonFormulas.CompletionTier(false, false, false) == OwnedTier.Half, "tier other");

        OwnedStock stock = new OwnedStock();
        stock.Add(OwnedTier.Full, 2, 255);
        stock.Add(OwnedTier.Eight, 1, 255);
        stock.Add(OwnedTier.Half, 3, 255);
        stock.Consume(2, true);
        failures += Check(stock.Half == 1 && stock.Eight == 1 && stock.Full == 2, "fuse consume");
        stock.Consume(2, false);
        failures += Check(stock.Full == 0 && stock.Eight == 1 && stock.Half == 1, "battle consume");
        failures += Check(Math.Abs(stock.NextLifeMultiplier(config) - 0.8) < 0.0001, "next life");

        PersonBattleInput person = new PersonBattleInput();
        person.PersonId = 1;
        person.BattleLevel = 3;
        person.SpecialtyCount = 1;
        person.Specialty0 = CharacterAttribute.Fire;
        AllyAuthorship[] one = { new AllyAuthorship { AuthorPersonId = 9, Type = CharacterAttribute.Fire } };
        double applied = PersonFormulas.MultiplierForAllies(1.0, new[] { person }, one, CharacterAttribute.Fire, config);
        failures += Check(Math.Abs(applied - 1.53) < 0.0000001, "buff 1.53");
        applied = PersonFormulas.MultiplierForAllies(1.0, new[] { person, person }, one, CharacterAttribute.Fire, config);
        failures += Check(Math.Abs(applied - 2.06) < 0.0000001, "buff 2.06");
        applied = PersonFormulas.MultiplierForAllies(0.8, new[] { person }, one, CharacterAttribute.Fire, config);
        failures += Check(Math.Abs(applied - 1.33) < 0.0000001, "buff 1.33");
        person.SpecialtyCount = 0;
        AllyAuthorship[] works = { new AllyAuthorship { AuthorPersonId = 1, Type = CharacterAttribute.Water } };
        applied = PersonFormulas.MultiplierForAllies(1.0, new[] { person }, works, CharacterAttribute.Water, config);
        failures += Check(Math.Abs(applied - 2.03) < 0.0000001, "buff 2.03");
        failures += Check(PersonFormulas.ScaleStat(10, 1.53) == 16, "ceil stat");

        PersonClass[] roster = PersonClass.CreateRoster();
        failures += Check(roster[PersonIds.Protagonist].Specialties.Count == 0, "hero none");
        failures += Check(roster[PersonIds.TeacherBlack].RgbAmount(0, config) == 10, "teacher rgb");
        roster[PersonIds.ClassmateRed].AddRgbProduced(0, 255, config);
        failures += Check(roster[PersonIds.ClassmateRed].RgbExp[0] == 3, "red exp");
        roster[PersonIds.ClassmateRed].AddPixelCompletion(32, 0, 0, config);
        failures += Check(roster[PersonIds.ClassmateRed].PixelLevel == 1 && roster[PersonIds.ClassmateRed].PixelExp == 6, "red pixel exp");
        roster[PersonIds.ClassmateRed].AddPixelCompletion(0, 32, 0, config);
        failures += Check(roster[PersonIds.ClassmateRed].PixelExp == 9, "shared pixel exp");
        PersonClass pixelGrow = roster[PersonIds.ClassmateRed];
        int pixelGuards = 0;
        while (pixelGrow.PixelLevel < 2 && pixelGuards < 200)
        {
            pixelGrow.AddPixelCompletion(32, 0, 0, config);
            pixelGuards++;
        }
        failures += Check(pixelGrow.PixelLevel == 2 && pixelGrow.PixelExp == 3 && pixelGuards == 49, "pixel lv2 costs 300");
        roster[PersonIds.Protagonist].AddPixelCompletion(1, 2, 3, config);
        failures += Check(roster[PersonIds.Protagonist].PixelExp == 0, "skip raw color");
        roster[PersonIds.Protagonist].AddCharacterCompletion(43, config);
        failures += Check(roster[PersonIds.Protagonist].CharacterLevel == 1 && roster[PersonIds.Protagonist].CharacterExp == 6, "character opaque exp");
        roster[PersonIds.ClassmateGreen].AddCharacterCompletion(1, config);
        failures += Check(roster[PersonIds.ClassmateGreen].CharacterExp == 1, "character exp ceil one");
        roster[PersonIds.ClassmateGreen].CharacterExp = 0;
        roster[PersonIds.ClassmateGreen].AddCharacterCompletion(9, config);
        failures += Check(roster[PersonIds.ClassmateGreen].CharacterExp == 2, "character exp ceil nine");

        PersonSaveData data = new PersonSaveData();
        data.People = PersonClass.CreateRoster();
        data.People[PersonIds.Protagonist].Specialties.Add(CharacterAttribute.Light);
        data.People[PersonIds.Protagonist].RgbLevel[0] = 3;
        data.TryRead("Person[1].Specialties", "");
        failures += Check(data.People[1].Specialties.Count == 0 && data.People[1].RgbLevel[0] == 3, "clear specialty keeps level");
        data.People[1].RgbLevel[0] = 3;
        failures += Check(data.People[1].RgbAmount(0, config) == 15, "recalc after clear");
        data.TryRead("Person[1].Specialties", "1");
        failures += Check(data.People[1].RgbAmount(0, config) == 75, "recalc after fire");

        PersonSaveData round = new PersonSaveData();
        round.People = PersonClass.CreateRoster();
        round.HelpR = new uint[4];
        round.Pixel = new uint[6];
        round.Character = new uint[6];
        round.BattlePeople = new uint[6, 4];
        round.TryRead("PersonIDHelpProductionR[2]", "1");
        round.TryRead("PersonIDProductionPixel[3]", "5");
        round.TryRead("PersonIDProductionCharacter[1]", "4");
        round.TryRead("Person[4].RgbLevelR", "3");
        round.TryRead("Person[4].RgbExpR", "12");
        round.TryRead("Person[4].CharacterLevel", "2");
        round.TryRead("Person[4].CharacterExp", "40");
        round.TryRead("Person[4].BattleLevel", "4");
        round.TryRead("Person[4].BattleExp", "7");
        round.TryRead("Person[1].PixelLevel", "2");
        round.TryRead("Person[1].PixelExp", "9");
        round.TryRead("Person[1].Pixel.32.0.0.Level", "1");
        round.TryRead("Person[1].Pixel.32.0.0.Exp", "3");
        round.TryRead("Person[5].Pixel.32.0.0.Level", "1");
        round.TryRead("Person[5].Pixel.32.0.0.Exp", "3");
        round.TryRead("Person[5].Pixel.224.32.32.Level", "4");
        round.TryRead("Person[5].Pixel.224.32.32.Exp", "7");
        round.TryRead("BattlePartyPerson[2,1]", "4");
        failures += Check(round.HelpR[2] == 1 && round.Pixel[3] == 5 && round.Character[1] == 4, "production slots");
        failures += Check(round.People[4].RgbLevel[0] == 3 && round.People[4].RgbExp[0] == 12, "rgb progress");
        failures += Check(round.People[4].CharacterLevel == 2 && round.People[4].CharacterExp == 40, "character progress");
        failures += Check(round.People[4].BattleLevel == 4 && round.People[4].BattleExp == 7, "battle progress");
        failures += Check(round.People[1].PixelLevel == 2 && round.People[1].PixelExp == 9, "pixel progress");
        failures += Check(round.People[5].PixelLevel == 4 && round.People[5].PixelExp == 7, "old pixel colors fold");
        failures += Check(round.BattlePeople[2, 1] == 4, "battle person");

        if (failures > 0)
            Console.WriteLine("person failures " + failures.ToString());
        return failures;
    }

    static int Check(bool condition, string name)
    {
        if (condition)
            return 0;
        Console.WriteLine("FAIL " + name);
        return 1;
    }
}
