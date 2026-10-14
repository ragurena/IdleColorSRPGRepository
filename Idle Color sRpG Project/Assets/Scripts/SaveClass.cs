using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using System.IO;

public class SaveClass// : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool Save(CharacterClass[] CharactersAll, int CharactersAllIndexNum,
        ulong CurR, ulong CurG, ulong CurB,
        ulong MaxR, ulong MaxG, ulong MaxB,
        ulong CostMaxRUp, ulong CostMaxGUp, ulong CostMaxBUp,
        ulong IncreaseValueR, ulong IncreaseValueG, ulong IncreaseValueB,
        ulong CostIncreaseValueRUp, ulong CostIncreaseValueGUp, ulong CostIncreaseValueBUp,
        uint[] CharactersIDHelpProductionR, uint[] CharactersIDHelpProductionG, uint[] CharactersIDHelpProductionB,

        uint[] CharactersIDProductionPixel,
        Color[] ColorProductionPixel,
        ushort[,] ProgressProductionPixel,
        ulong[,] SpentProductionPixel,
        uint[] CharactersIDProductionCharacter,
        uint[] CharactersIDProducedCharacter,
        List<bool[,]> ProgressTextureProductionCharacter,
        List<ConsumePixelClass>[] ConsumePixelsProductionCharacter,
        ulong[,,] CurPixels,
        uint[,,] BattlePartyCharacterIds,
        int ActiveBattlePartySet,
        int ActiveBattleStage,
        int ActiveBattleFloorFrom,
        int ActiveBattleFloorTo,
        int ClearedBattleStage,
        int[] ClearedBattleFloor,
        Dictionary<int, int> ItemCounts,
        PersonSaveData people
        )
    {
        Debug.Log("セーブ : " + Application.persistentDataPath + "/ICS.csv");

        try
        {
            int generation = SaveFiles.NextGeneration();
            if (generation < 1)
                return false;

            byte[] body;
            using (MemoryStream memory = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(memory, new UTF8Encoding(false), 1024, true))
                {
                    sw.NewLine = "\n";
                    sw.WriteLine("DataVersion," + GameConfig.DataVersion);
                    sw.WriteLine("SaveFormat," + GameConfig.SaveFormat.ToString());
                    sw.WriteLine("SaveGeneration," + generation.ToString());

        //TODO:CharactersAllのセーブ・ロード
        for (int i = 1; i < CharactersAllIndexNum; i++)
        {
            sw.WriteLine("CharactersAll[" + i.ToString() + "].KnownPixels," + CharactersAll[i].KnownPixels);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].OwnedNumMax," + CharactersAll[i].OwnedNumMax);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].OwnedNumCur," + CharactersAll[i].OwnedNumCur);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].ReincarnationTimes," + CharactersAll[i].ReincarnationTimes);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].Level," + CharactersAll[i].Level);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].Exp," + CharactersAll[i].Exp);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].FusionCount," + CharactersAll[i].FusionCount);

            if (CharactersAll[i].FlagFNT)
            {
                sw.WriteLine("CharactersAll[" + i.ToString() + "].FlagFNT," + "true");
            }
            else 
            {
                sw.WriteLine("CharactersAll[" + i.ToString() + "].FlagFNT," + "false");
            }

            sw.WriteLine("CharactersAll[" + i.ToString() + "].Whereabouts," + CharactersAll[i].Whereabouts);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].CatalogOpened," + (CharactersAll[i].CatalogOpened ? "true" : "false"));
            CharactersAll[i].EnsureStock();
            sw.WriteLine("CharactersAll[" + i.ToString() + "].AuthorPersonId," + CharactersAll[i].AuthorPersonId.ToString());
            sw.WriteLine("CharactersAll[" + i.ToString() + "].OwnedFull," + CharactersAll[i].Stock.Full.ToString());
            sw.WriteLine("CharactersAll[" + i.ToString() + "].OwnedEight," + CharactersAll[i].Stock.Eight.ToString());
            sw.WriteLine("CharactersAll[" + i.ToString() + "].OwnedHalf," + CharactersAll[i].Stock.Half.ToString());

        }

        sw.WriteLine("CurR," + CurR);
        sw.WriteLine("CurG," + CurG);
        sw.WriteLine("CurB," + CurB);

        sw.WriteLine("MaxR," + MaxR);
        sw.WriteLine("MaxG," + MaxG);
        sw.WriteLine("MaxB," + MaxB);

        sw.WriteLine("CostMaxRUp," + CostMaxRUp);
        sw.WriteLine("CostMaxGUp," + CostMaxGUp);
        sw.WriteLine("CostMaxBUp," + CostMaxBUp);

        sw.WriteLine("IncreaseValueR," + IncreaseValueR);
        sw.WriteLine("IncreaseValueG," + IncreaseValueG);
        sw.WriteLine("IncreaseValueB," + IncreaseValueB);

        sw.WriteLine("CostIncreaseValueRUp," + CostIncreaseValueRUp);
        sw.WriteLine("CostIncreaseValueGUp," + CostIncreaseValueGUp);
        sw.WriteLine("CostIncreaseValueBUp," + CostIncreaseValueBUp);
        

        for (int i = 1; i <= 3; i++)
        {
            sw.WriteLine("CharactersIDHelpProductionR[" + i.ToString() + "]," + CharactersIDHelpProductionR[i].ToString());
        }
        for (int i = 1; i <= 3; i++)
        {
            sw.WriteLine("CharactersIDHelpProductionG[" + i.ToString() + "]," + CharactersIDHelpProductionG[i].ToString());
        }
        for (int i = 1; i <= 3; i++)
        {
            sw.WriteLine("CharactersIDHelpProductionB[" + i.ToString() + "]," + CharactersIDHelpProductionB[i].ToString());
        }

        // ピクセル生産枠の保存
        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_PIXEL_NUM; i++)
        {
            sw.WriteLine("CharactersIDProductionPixel[" + i.ToString() + "]," +
                CharactersIDProductionPixel[i].ToString());

            sw.WriteLine("ColorProductionPixel[" + i.ToString() + "].r," +
                CharacterClass.ColorUnit(CharacterClass.ColorByte(ColorProductionPixel[i].r)).ToString());

            sw.WriteLine("ColorProductionPixel[" + i.ToString() + "].g," +
                CharacterClass.ColorUnit(CharacterClass.ColorByte(ColorProductionPixel[i].g)).ToString());

            sw.WriteLine("ColorProductionPixel[" + i.ToString() + "].b," +
                CharacterClass.ColorUnit(CharacterClass.ColorByte(ColorProductionPixel[i].b)).ToString());

            sw.WriteLine("ProgressProductionPixel[" + i.ToString() + "].r," +
                ProgressProductionPixel[i, 1].ToString());
            sw.WriteLine("ProgressProductionPixel[" + i.ToString() + "].g," +
                ProgressProductionPixel[i, 2].ToString());
            sw.WriteLine("ProgressProductionPixel[" + i.ToString() + "].b," +
                ProgressProductionPixel[i, 3].ToString());

            sw.WriteLine("SpentProductionPixel[" + i.ToString() + "].r," +
                SpentProductionPixel[i, 1].ToString());
            sw.WriteLine("SpentProductionPixel[" + i.ToString() + "].g," +
                SpentProductionPixel[i, 2].ToString());
            sw.WriteLine("SpentProductionPixel[" + i.ToString() + "].b," +
                SpentProductionPixel[i, 3].ToString());
        }

        // CurPixels（非ゼロのみ）
        int curPixelsCount = 0;
        for (int r = 0; r < 256; r++)
        {
            for (int g = 0; g < 256; g++)
            {
                for (int b = 0; b < 256; b++)
                {
                    if (CurPixels[r, g, b] > 0)
                        curPixelsCount++;
                }
            }
        }
        sw.WriteLine("CurPixels.Count," + curPixelsCount.ToString());
        int curPixelsIndex = 0;
        for (int r = 0; r < 256; r++)
        {
            for (int g = 0; g < 256; g++)
            {
                for (int b = 0; b < 256; b++)
                {
                    if (CurPixels[r, g, b] > 0)
                    {
                        sw.WriteLine("CurPixels[" + curPixelsIndex.ToString() + "].r," + r.ToString());
                        sw.WriteLine("CurPixels[" + curPixelsIndex.ToString() + "].g," + g.ToString());
                        sw.WriteLine("CurPixels[" + curPixelsIndex.ToString() + "].b," + b.ToString());
                        sw.WriteLine("CurPixels[" + curPixelsIndex.ToString() + "].num," + CurPixels[r, g, b].ToString());
                        curPixelsIndex++;
                    }
                }
            }
        }

        // キャラクター生産枠の保存
        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM; i++)
        {
            sw.WriteLine("CharactersIDProductionCharacter[" + i.ToString() + "]," +
                CharactersIDProductionCharacter[i].ToString());

            sw.WriteLine("CharactersIDProducedCharacter[" + i.ToString() + "]," +
                CharactersIDProducedCharacter[i].ToString());

            //生産者がいなくても、作る対象がいれば塗っている途中を残す
            if (CharactersIDProducedCharacter[i] == 0)
                continue;

            sw.WriteLine("ConsumePixelsProductionCharacter[" + i.ToString() + "].Count," +
                ConsumePixelsProductionCharacter[i].Count.ToString());
            for (int j = 0; j < ConsumePixelsProductionCharacter[i].Count; j++)
            {
                ConsumePixelClass consumePixel = ConsumePixelsProductionCharacter[i][j];
                sw.WriteLine("ConsumePixelsProductionCharacter[" + i.ToString() + "][" + j.ToString() + "].r," +
                    CharacterClass.ColorUnit(CharacterClass.ColorByte(consumePixel.PixelColor.r)).ToString());
                sw.WriteLine("ConsumePixelsProductionCharacter[" + i.ToString() + "][" + j.ToString() + "].g," +
                    CharacterClass.ColorUnit(CharacterClass.ColorByte(consumePixel.PixelColor.g)).ToString());
                sw.WriteLine("ConsumePixelsProductionCharacter[" + i.ToString() + "][" + j.ToString() + "].b," +
                    CharacterClass.ColorUnit(CharacterClass.ColorByte(consumePixel.PixelColor.b)).ToString());
                sw.WriteLine("ConsumePixelsProductionCharacter[" + i.ToString() + "][" + j.ToString() + "].ToBe," +
                    consumePixel.ToBeCurConsumePixelsNum.ToString());
                sw.WriteLine("ConsumePixelsProductionCharacter[" + i.ToString() + "][" + j.ToString() + "].Cur," +
                    consumePixel.CurConsumePixelsNum.ToString());
            }

            if (ProgressTextureProductionCharacter[i] != null)
            {
                bool[,] progressTexture = ProgressTextureProductionCharacter[i];
                int width = progressTexture.GetLength(0);
                int height = progressTexture.GetLength(1);
                sw.WriteLine("ProgressTextureProductionCharacter[" + i.ToString() + "].width," + width.ToString());
                sw.WriteLine("ProgressTextureProductionCharacter[" + i.ToString() + "].height," + height.ToString());

                StringBuilder progressData = new StringBuilder();
                bool first = true;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!first)
                            progressData.Append(',');
                        progressData.Append(progressTexture[x, y].ToString().ToLower());
                        first = false;
                    }
                }
                sw.WriteLine("ProgressTextureProductionCharacter[" + i.ToString() + "].data," + progressData.ToString());
            }
        }

        for (int setIndex = 1; setIndex <= Constants.BATTLE_PARTY_SET_NUM; setIndex++)
        {
            for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
            {
                for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
                {
                    sw.WriteLine("BattlePartyCharacterIds[" + setIndex.ToString() + "][" + x.ToString() + "][" + y.ToString() + "]," +
                        BattlePartyCharacterIds[setIndex, x, y].ToString());
                }
            }
        }


        sw.WriteLine("ActiveBattlePartySet," + ActiveBattlePartySet.ToString());
        sw.WriteLine("ActiveBattleStage," + ActiveBattleStage.ToString());
        sw.WriteLine("ActiveBattleFloorFrom," + ActiveBattleFloorFrom.ToString());
        sw.WriteLine("ActiveBattleFloorTo," + ActiveBattleFloorTo.ToString());
        sw.WriteLine("ClearedBattleStage," + ClearedBattleStage.ToString());
        if (ClearedBattleFloor != null)
        {
            for (int stage = 1; stage < ClearedBattleFloor.Length; stage++)
                sw.WriteLine("ClearedBattleFloor[" + stage.ToString() + "]," + ClearedBattleFloor[stage].ToString());
        }

        if (ItemCounts != null)
        {
            foreach (KeyValuePair<int, int> item in ItemCounts)
                sw.WriteLine("ItemCount[" + item.Key.ToString() + "]," + item.Value.ToString());
        }

        ImportedCharacters.WriteSave(sw);
        if (people != null)
            people.Write(sw);

                    sw.Flush();
                }

                body = memory.ToArray();
            }

            return SaveFiles.Commit(body, generation);
        }
        catch (Exception exception)
        {
            Debug.LogError("セーブに失敗しました。 " + exception.Message);
            return false;
        }
    }

    // セーブが無い、または版の行が無いときは空文字。空は 0.0.4 より古い、とは扱わない
    public static string ReadDataVersion()
    {
        return ReadDataVersion(Application.persistentDataPath + "/ICS.csv");
    }

    public static string ReadDataVersion(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return "";

        try
        {
            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        continue;
                    string[] values = line.Split(',');
                    if (values.Length < 2 || !values[0].Equals("DataVersion"))
                        continue;
                    return values[1];
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("セーブの版を読めませんでした。 " + exception.Message);
        }
        return "";
    }

    public static bool IsOlderDataVersion(string savedVersion)
    {
        return CompareVersions(savedVersion, GameConfig.DataVersion) < 0;
    }

    public static bool IsOlderThan(string savedVersion, string version)
    {
        return CompareVersions(savedVersion, version) < 0;
    }

    static int CompareVersions(string left, string right)
    {
        int[] a = ParseVersion(left);
        int[] b = ParseVersion(right);
        int n = a.Length > b.Length ? a.Length : b.Length;
        for (int i = 0; i < n; i++)
        {
            int av = i < a.Length ? a[i] : 0;
            int bv = i < b.Length ? b[i] : 0;
            if (av < bv)
                return -1;
            if (av > bv)
                return 1;
        }
        return 0;
    }

    static int[] ParseVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return new int[0];
        string[] parts = version.Split('.');
        int[] nums = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            int n;
            if (!int.TryParse(parts[i], out n) || n < 0)
                return new int[0];
            nums[i] = n;
        }
        return nums;
    }

    public bool Load(ref CharacterClass[] CharactersAll, int CharactersAllIndexNum,
        ref ulong CurR, ref ulong CurG, ref ulong CurB,
        ref ulong MaxR, ref ulong MaxG, ref ulong MaxB,
        ref ulong CostMaxRUp, ref ulong CostMaxGUp, ref ulong CostMaxBUp,
        ref ulong IncreaseValueR, ref ulong IncreaseValueG, ref ulong IncreaseValueB,
        ref ulong CostIncreaseValueRUp, ref ulong CostIncreaseValueGUp, ref ulong CostIncreaseValueBUp,
        ref uint[] CharactersIDHelpProductionR, ref uint[] CharactersIDHelpProductionG, ref uint[] CharactersIDHelpProductionB,

        // ★追加
        ref uint[] CharactersIDProductionPixel,
        ref Color[] ColorProductionPixel,
        ref ushort[,] ProgressProductionPixel,
        ref ulong[,] SpentProductionPixel,
        ref uint[] CharactersIDProductionCharacter,
        ref uint[] CharactersIDProducedCharacter,
        List<bool[,]> ProgressTextureProductionCharacter,
        List<ConsumePixelClass>[] ConsumePixelsProductionCharacter,
        ref ulong[,,] CurPixels,
        ref uint[,,] BattlePartyCharacterIds,
        ref int ActiveBattlePartySet,
        ref int ActiveBattleStage,
        ref int ActiveBattleFloorFrom,
        ref int ActiveBattleFloorTo,
        ref int ClearedBattleStage,
        ref int[] ClearedBattleFloor,
        ref Dictionary<int, int> ItemCounts,
        PersonSaveData people,
        string sourcePath
        )
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return false;

        try
        {
        ImportedCharacters.BeginLoad();
        Debug.Log("ロード : " + sourcePath);

        // キャラを途中に入れた版より古いセーブは、入れた位置以降の ID をずらす
        string savedVersion = ReadDataVersion(sourcePath);

        using (StreamReader sr = new StreamReader(sourcePath))
        {

        for (int r = 0; r < 256; r++)
        {
            for (int g = 0; g < 256; g++)
            {
                for (int b = 0; b < 256; b++)
                {
                    CurPixels[r, g, b] = 0;
                }
            }
        }

        int[] progressTextureWidth = new int[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
        int[] progressTextureHeight = new int[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
        int curPixelsLoadIndex = -1;
        int curPixelsLoadR = 0;
        int curPixelsLoadG = 0;
        int curPixelsLoadB = 0;

        while (!sr.EndOfStream)
        {

            string line = sr.ReadLine();
            if (ImportedCharacters.TryReadSaveLine(line))
                continue;
            if (people != null && line.StartsWith("BattlePartyPerson["))
            {
                int split = line.LastIndexOf(',');
                if (split > 0 && people.TryRead(line.Substring(0, split), line.Substring(split + 1)))
                    continue;
            }
            string[] values = line.Split(',');
            if (values.Length >= 2 && people != null && people.TryRead(values[0], values[1]))
                continue;

            if (values[0].Equals("DataVersion"))
            {
            }
            else
            if (values[0].Equals("CurR"))
            {
                CurR = ReadUlong(values);
            }
            else
            if (values[0].Equals("CurG"))
            {
                CurG = ReadUlong(values);
            }
            else
            if (values[0].Equals("CurB"))
            {
                CurB = ReadUlong(values);
            }

            else
            if (values[0].Equals("MaxR"))
            {
                MaxR = ReadUlong(values);
            }
            else
            if (values[0].Equals("MaxG"))
            {
                MaxG = ReadUlong(values);
            }
            else
            if (values[0].Equals("MaxB"))
            {
                MaxB = ReadUlong(values);
            }

            else
            if (values[0].Equals("CostMaxRUp"))
            {
                CostMaxRUp = ReadUlong(values);
            }
            else
            if (values[0].Equals("CostMaxGUp"))
            {
                CostMaxGUp = ReadUlong(values);
            }
            else
            if (values[0].Equals("CostMaxBUp"))
            {
                CostMaxBUp = ReadUlong(values);
            }

            else
            if (values[0].Equals("IncreaseValueR"))
            {
                IncreaseValueR = ReadUlong(values);
            }
            else
            if (values[0].Equals("IncreaseValueG"))
            {
                IncreaseValueG = ReadUlong(values);
            }
            else
            if (values[0].Equals("IncreaseValueB"))
            {
                IncreaseValueB = ReadUlong(values);
            }

            else
            if (values[0].Equals("CostIncreaseValueRUp"))
            {
                CostIncreaseValueRUp = ReadUlong(values);
            }
            else
            if (values[0].Equals("CostIncreaseValueGUp"))
            {
                CostIncreaseValueGUp = ReadUlong(values);
            }
            else
            if (values[0].Equals("CostIncreaseValueBUp"))
            {
                CostIncreaseValueBUp = ReadUlong(values);
            }


            else
            if (values[0].StartsWith("CharactersIDHelpProduction"))
            {
                for (int i = 1; i <= 3; i++)
                {
                    if (values[0].Equals("CharactersIDHelpProductionR[" + i.ToString() + "]"))
                    {
                        CharactersIDHelpProductionR[i] = ShiftCharacterId((uint)int.Parse(values[1]), savedVersion);
                        break;
                    }

                    if (values[0].Equals("CharactersIDHelpProductionG[" + i.ToString() + "]"))
                    {
                        CharactersIDHelpProductionG[i] = ShiftCharacterId((uint)int.Parse(values[1]), savedVersion);
                        break;
                    }

                    if (values[0].Equals("CharactersIDHelpProductionB[" + i.ToString() + "]"))
                    {
                        CharactersIDHelpProductionB[i] = ShiftCharacterId((uint)int.Parse(values[1]), savedVersion);
                        break;
                    }
                }
            }

            else
            if (values[0].StartsWith("CharactersIDProductionPixel"))
            {
                for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_PIXEL_NUM; i++)
                {
                    if (values[0].Equals("CharactersIDProductionPixel[" + i.ToString() + "]"))
                    {
                        CharactersIDProductionPixel[i] =
                            ShiftCharacterId((uint)int.Parse(values[1]), savedVersion);
                        break;
                    }
                }
            }

            else
            if (values[0].StartsWith("ColorProductionPixel"))
            {
                for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_PIXEL_NUM; i++)
                {
                    if (values[0].Equals("ColorProductionPixel[" + i.ToString() + "].r"))
                    {
                        ColorProductionPixel[i].r =
                            CharacterClass.ColorUnit(CharacterClass.ColorByte(float.Parse(values[1])));
                        break;
                    }

                    if (values[0].Equals("ColorProductionPixel[" + i.ToString() + "].g"))
                    {
                        ColorProductionPixel[i].g =
                            CharacterClass.ColorUnit(CharacterClass.ColorByte(float.Parse(values[1])));
                        break;
                    }

                    if (values[0].Equals("ColorProductionPixel[" + i.ToString() + "].b"))
                    {
                        ColorProductionPixel[i].b =
                            CharacterClass.ColorUnit(CharacterClass.ColorByte(float.Parse(values[1])));
                        break;
                    }
                }
            }

            else
            if (values[0].StartsWith("ProgressProductionPixel"))
            {
                for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_PIXEL_NUM; i++)
                {
                    if (values[0].Equals("ProgressProductionPixel[" + i.ToString() + "].r"))
                    {
                        ProgressProductionPixel[i, 1] = (ushort)(int.Parse(values[1]));
                        break;
                    }

                    if (values[0].Equals("ProgressProductionPixel[" + i.ToString() + "].g"))
                    {
                        ProgressProductionPixel[i, 2] = (ushort)(int.Parse(values[1]));
                        break;
                    }

                    if (values[0].Equals("ProgressProductionPixel[" + i.ToString() + "].b"))
                    {
                        ProgressProductionPixel[i, 3] = (ushort)(int.Parse(values[1]));
                        break;
                    }
                }
            }

            else
            if (values[0].StartsWith("SpentProductionPixel"))
            {
                for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_PIXEL_NUM; i++)
                {
                    if (values[0].Equals("SpentProductionPixel[" + i.ToString() + "].r"))
                    {
                        SpentProductionPixel[i, 1] = ReadUlong(values);
                        break;
                    }

                    if (values[0].Equals("SpentProductionPixel[" + i.ToString() + "].g"))
                    {
                        SpentProductionPixel[i, 2] = ReadUlong(values);
                        break;
                    }

                    if (values[0].Equals("SpentProductionPixel[" + i.ToString() + "].b"))
                    {
                        SpentProductionPixel[i, 3] = ReadUlong(values);
                        break;
                    }
                }
            }

            else
            if (values[0].Equals("CurPixels.Count"))
            {
                curPixelsLoadIndex = -1;
            }

            else
            if (values[0].StartsWith("CurPixels["))
            {
                int entryStart = values[0].IndexOf('[') + 1;
                int entryEnd = values[0].IndexOf(']');
                int entryIndex = int.Parse(values[0].Substring(entryStart, entryEnd - entryStart));
                string field = values[0].Substring(entryEnd + 2);

                if (field.Equals("r"))
                {
                    curPixelsLoadIndex = entryIndex;
                    curPixelsLoadR = int.Parse(values[1]);
                }
                else
                if (field.Equals("g"))
                {
                    curPixelsLoadG = int.Parse(values[1]);
                }
                else
                if (field.Equals("b"))
                {
                    curPixelsLoadB = int.Parse(values[1]);
                }
                else
                if (field.Equals("num") && curPixelsLoadIndex == entryIndex)
                {
                    CurPixels[curPixelsLoadR, curPixelsLoadG, curPixelsLoadB] = ReadUlong(values);
                }
            }

            else
            if (values[0].StartsWith("ConsumePixelsProductionCharacter"))
            {
                int slotStart = values[0].IndexOf('[') + 1;
                int slotEnd = values[0].IndexOf(']');
                int slotIndex = int.Parse(values[0].Substring(slotStart, slotEnd - slotStart));

                if (values[0].Equals("ConsumePixelsProductionCharacter[" + slotIndex.ToString() + "].Count"))
                {
                    int count = int.Parse(values[1]);
                    ConsumePixelsProductionCharacter[slotIndex].Clear();
                    for (int k = 0; k < count; k++)
                    {
                        ConsumePixelsProductionCharacter[slotIndex].Add(new ConsumePixelClass(Color.black, 0, 0));
                    }
                }
                else
                {
                    int entryStart = values[0].IndexOf('[', slotEnd) + 1;
                    int entryEnd = values[0].IndexOf(']', entryStart);
                    int entryIndex = int.Parse(values[0].Substring(entryStart, entryEnd - entryStart));
                    string field = values[0].Substring(entryEnd + 2);

                    if (entryIndex < ConsumePixelsProductionCharacter[slotIndex].Count)
                    {
                        ConsumePixelClass consumePixel = ConsumePixelsProductionCharacter[slotIndex][entryIndex];
                        Color pixelColor = consumePixel.PixelColor;

                        if (field.Equals("r"))
                            pixelColor.r = CharacterClass.ColorUnit(CharacterClass.ColorByte(float.Parse(values[1])));
                        else
                        if (field.Equals("g"))
                            pixelColor.g = CharacterClass.ColorUnit(CharacterClass.ColorByte(float.Parse(values[1])));
                        else
                        if (field.Equals("b"))
                            pixelColor.b = CharacterClass.ColorUnit(CharacterClass.ColorByte(float.Parse(values[1])));
                        else
                        if (field.Equals("ToBe"))
                            consumePixel.ToBeCurConsumePixelsNum = uint.Parse(values[1]);
                        else
                        if (field.Equals("Cur"))
                            consumePixel.CurConsumePixelsNum = uint.Parse(values[1]);

                        consumePixel.PixelColor = pixelColor;
                    }
                }
            }

            else
            if (values[0].StartsWith("ProgressTextureProductionCharacter"))
            {
                int slotStart = values[0].IndexOf('[') + 1;
                int slotEnd = values[0].IndexOf(']');
                int slotIndex = int.Parse(values[0].Substring(slotStart, slotEnd - slotStart));
                string field = values[0].Substring(slotEnd + 2);

                if (field.Equals("width"))
                {
                    progressTextureWidth[slotIndex] = int.Parse(values[1]);
                }
                else
                if (field.Equals("height"))
                {
                    progressTextureHeight[slotIndex] = int.Parse(values[1]);
                }
                else
                if (field.Equals("data"))
                {
                    int width = progressTextureWidth[slotIndex];
                    int height = progressTextureHeight[slotIndex];
                    if (width > 0 && height > 0)
                    {
                        bool[,] progressTexture = new bool[width, height];
                        int dataIndex = 1;
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                if (dataIndex < values.Length)
                                {
                                    progressTexture[x, y] = values[dataIndex].Equals("true");
                                    dataIndex++;
                                }
                            }
                        }
                        ProgressTextureProductionCharacter[slotIndex] = progressTexture;
                    }
                }
            }

            else
            if (values[0].Equals("ActiveBattlePartySet"))
            {
                ActiveBattlePartySet = int.Parse(values[1]);
            }

            else
            if (values[0].Equals("ActiveBattleStage"))
            {
                ActiveBattleStage = int.Parse(values[1]);
            }

            else
            if (values[0].Equals("ActiveBattleFloorFrom"))
            {
                ActiveBattleFloorFrom = int.Parse(values[1]);
            }

            else
            if (values[0].Equals("ActiveBattleFloorTo"))
            {
                ActiveBattleFloorTo = int.Parse(values[1]);
            }

            else
            if (values[0].Equals("ClearedBattleStage"))
            {
                ClearedBattleStage = int.Parse(values[1]);
            }

            else
            if (values[0].StartsWith("ClearedBattleFloor["))
            {
                int stageStart = values[0].IndexOf('[') + 1;
                int stageEnd = values[0].IndexOf(']', stageStart);
                int stageIndex = int.Parse(values[0].Substring(stageStart, stageEnd - stageStart));
                if (ClearedBattleFloor != null && stageIndex >= 1 && stageIndex < ClearedBattleFloor.Length)
                    ClearedBattleFloor[stageIndex] = int.Parse(values[1]);
            }

            else
            if (values[0].StartsWith("ItemCount["))
            {
                int idStart = values[0].IndexOf('[') + 1;
                int idEnd = values[0].IndexOf(']', idStart);
                int itemId = int.Parse(values[0].Substring(idStart, idEnd - idStart));
                if (ItemCounts != null)
                    ItemCounts[itemId] = int.Parse(values[1]);
            }

            else
            if (values[0].StartsWith("BattlePartyCharacterIds["))
            {
                int setStart = values[0].IndexOf('[') + 1;
                int setEnd = values[0].IndexOf(']', setStart);
                int xStart = values[0].IndexOf('[', setEnd) + 1;
                int xEnd = values[0].IndexOf(']', xStart);
                int yStart = values[0].IndexOf('[', xEnd) + 1;
                int yEnd = values[0].IndexOf(']', yStart);
                int setIndex = int.Parse(values[0].Substring(setStart, setEnd - setStart));
                int x = int.Parse(values[0].Substring(xStart, xEnd - xStart));
                int y = int.Parse(values[0].Substring(yStart, yEnd - yStart));
                if (setIndex >= 1 && setIndex <= Constants.BATTLE_PARTY_SET_NUM
                    && x >= 1 && x <= Constants.BATTLE_FORMATION_SIZE
                    && y >= 1 && y <= Constants.BATTLE_FORMATION_SIZE)
                {
                    BattlePartyCharacterIds[setIndex, x, y] = ShiftCharacterId((uint)int.Parse(values[1]), savedVersion);
                }
            }

            else
            if (values[0].StartsWith("CharactersIDProductionCharacter"))
            {
                for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM; i++)
                {
                    if (values[0].Equals("CharactersIDProductionCharacter[" + i.ToString() + "]"))
                    {
                        CharactersIDProductionCharacter[i] =
                            ShiftCharacterId((uint)int.Parse(values[1]), savedVersion);
                        break;
                    }
                }
            }

            else
            if (values[0].StartsWith("CharactersIDProducedCharacter"))
            {
                for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM; i++)
                {
                    if (values[0].Equals("CharactersIDProducedCharacter[" + i.ToString() + "]"))
                    {
                        CharactersIDProducedCharacter[i] =
                            ShiftCharacterId((uint)int.Parse(values[1]), savedVersion);
                        break;
                    }
                }
            }


            else
            if (values[0].StartsWith("CharactersAll"))
            {

                for (int i = 1; i < CharactersAllIndexNum; i++)
                {
                    int slot = ShiftedCharacterSlot(i, savedVersion);
                    if (slot < 1 || slot >= CharactersAllIndexNum || CharactersAll[slot] == null)
                        continue;
                    if (IsInsertedCharacterSlot(slot, savedVersion))
                        continue;

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].KnownPixels"))
                    {
                        CharactersAll[slot].KnownPixels = ReadUint(values);
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].OwnedNumMax"))
                    {
                        CharactersAll[slot].OwnedNumMax = ReadUlong(values);
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].OwnedNumCur"))
                    {
                        CharactersAll[slot].OwnedNumCur = ReadUlong(values);
                        if (CharactersAll[slot].OwnedNumCur > BattleBalanceConfig.MaxOwnedCount)
                        {
                            if (CharactersAll[slot].OwnedNumMax < CharactersAll[slot].OwnedNumCur)
                                CharactersAll[slot].OwnedNumMax = CharactersAll[slot].OwnedNumCur;
                            CharactersAll[slot].OwnedNumCur = BattleBalanceConfig.MaxOwnedCount;
                        }
                        CharactersAll[slot].RaiseOwnedMax();
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].ReincarnationTimes"))
                    {
                        CharactersAll[slot].ReincarnationTimes = ReadUlong(values);
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].Level"))
                    {
                        CharactersAll[slot].Level = ReadUlong(values);
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].Exp"))
                    {
                        CharactersAll[slot].Exp = ReadUlong(values);
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].FusionCount"))
                    {
                        CharactersAll[slot].FusionCount = ReadUlong(values);
                        break;
                    }

                    //TODO:FlagFNTテスト
                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].FlagFNT"))
                    {
                        CharactersAll[slot].FlagFNT = values[1].Equals("true");
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].Whereabouts"))
                    {
                        if (values[1].Equals("None"))
                        {
                            CharactersAll[slot].Whereabouts = Place.None;
                        }
                        else
                        if (values[1].Equals("CreateR"))
                        {
                            CharactersAll[slot].Whereabouts = Place.CreateR;
                        }
                        else
                        if (values[1].Equals("CreateG"))
                        {
                            CharactersAll[slot].Whereabouts = Place.CreateG;
                        }
                        else
                        if (values[1].Equals("CreateB"))
                        {
                            CharactersAll[slot].Whereabouts = Place.CreateB;
                        }
                        else
                        if (values[1].Equals("CreatePixel"))
                        {
                            CharactersAll[slot].Whereabouts = Place.CreatePixel;
                        }
                        else
                        if (values[1].Equals("CreateCharacter"))
                        {
                            CharactersAll[slot].Whereabouts = Place.CreateCharacter;
                        }
                        else
                        if (values[1].Equals("Hospital"))
                        {
                            CharactersAll[slot].Whereabouts = Place.Hospital;
                        }
                        else
                        if (values[1].Equals("Battle"))
                        {
                            CharactersAll[slot].Whereabouts = Place.Battle;
                        }

                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].CatalogOpened"))
                    {
                        CharactersAll[slot].CatalogOpened = values[1].Equals("true");
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].AuthorPersonId"))
                    {
                        int author;
                        if (int.TryParse(values[1], out author) && author > 0)
                            CharactersAll[slot].AuthorPersonId = author;
                        else
                            CharactersAll[slot].AuthorPersonId = 0;
                        CharactersAll[slot].AuthorSpecified = true;
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].OwnedFull"))
                    {
                        long number;
                        if (long.TryParse(values[1], out number) && number > 0)
                            CharactersAll[slot].Stock.Full = number;
                        CharactersAll[slot].Stock.Specified = true;
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].OwnedEight"))
                    {
                        long number;
                        if (long.TryParse(values[1], out number) && number > 0)
                            CharactersAll[slot].Stock.Eight = number;
                        CharactersAll[slot].Stock.Specified = true;
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].OwnedHalf"))
                    {
                        long number;
                        if (long.TryParse(values[1], out number) && number > 0)
                            CharactersAll[slot].Stock.Half = number;
                        CharactersAll[slot].Stock.Specified = true;
                        break;
                    }

                }
            }

        }

        }
        return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("セーブの適用に失敗しました。 " + exception.Message);
            return false;
        }
    }

    static ulong ReadUlong(string[] values)
    {
        if (values == null || values.Length < 2)
            throw new FormatException("値がありません");
        ulong number;
        if (!ulong.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            throw new FormatException("数値ではありません");
        return number;
    }

    static uint ReadUint(string[] values)
    {
        ulong number = ReadUlong(values);
        if (number > uint.MaxValue)
            throw new OverflowException("uint の範囲を超えています");
        return (uint)number;
    }

    // 途中挿入より古いセーブの並びを、今の ID に合わせる
    static int ShiftedCharacterSlot(int index, string savedVersion)
    {
        if (index < 1)
            return index;
        if (CompareVersions(savedVersion, "0.0.2") < 0 && index >= 4)
            index++;
        if (CompareVersions(savedVersion, "0.0.3") < 0 && index >= 9)
            index++;
        return index;
    }

    static uint ShiftCharacterId(uint id, string savedVersion)
    {
        if (id == 0)
            return 0;
        if (id >= ImportedCharacters.FirstId)
            return id;
        int shifted = ShiftedCharacterSlot((int)id, savedVersion);
        if (shifted < 1 || shifted > Constants.CHARACTERS_ALL_NUM)
            return 0;
        return (uint)shifted;
    }

    // この版で新しく入った枠には、古い空きデータの上書きをしない
    static bool IsInsertedCharacterSlot(int slot, string savedVersion)
    {
        if (CompareVersions(savedVersion, "0.0.2") < 0 && slot == 4)
            return true;
        if (CompareVersions(savedVersion, "0.0.3") < 0 && (slot == 7 || slot == 8 || slot == 9))
            return true;
        return false;
    }
}
