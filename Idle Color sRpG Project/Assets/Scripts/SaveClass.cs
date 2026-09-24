using System.Collections;
using System.Collections.Generic;
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

    public void Save(CharacterClass[] CharactersAll, int CharactersAllIndexNum,
        ulong CurR, ulong CurG, ulong CurB,
        ulong MaxR, ulong MaxG, ulong MaxB,
        ulong CostMaxRUp, ulong CostMaxGUp, ulong CostMaxBUp,
        ulong IncreaseValueR, ulong IncreaseValueG, ulong IncreaseValueB,
        ulong CostIncreaseValueRUp, ulong CostIncreaseValueGUp, ulong CostIncreaseValueBUp,
        uint[] CharactersIDHelpProductionR, uint[] CharactersIDHelpProductionG, uint[] CharactersIDHelpProductionB,

        uint[] CharactersIDProductionPixel,
        Color[] ColorProductionPixel,
        ushort[,] ProgressProductionPixel,
        uint[] CharactersIDProductionCharacter,
        uint[] CharactersIDProducedCharacter,
        List<bool[,]> ProgressTextureProductionCharacter,
        List<ConsumePixelClass>[] ConsumePixelsProductionCharacter,
        ulong[,,] CurPixels,
        uint[,,] BattlePartyCharacterIds,
        int ActiveBattlePartySet
        )
    {
        Debug.Log("セーブ : " + Application.persistentDataPath + "/ICS.csv");
        //Debug.Log("セーブ : " + Application.streamingAssetsPath + "/ICS.csv");

        //if(File.Exists("Assets/Resources/ICS.csv"))
        if (File.Exists(Application.persistentDataPath + "/ICS.csv"))
        //if (File.Exists(Application.streamingAssetsPath + "/ICS.csv"))
        {
            Debug.Log("セーブファイルが存在します");
        }
        else
        {
            Debug.Log("セーブファイルが存在しません");
            //FileStream fs = File.Create("Assets/Resources/ICS.csv");
            FileStream fs = File.Create(Application.persistentDataPath + "/ICS.csv");
            //FileStream fs = File.Create(Application.streamingAssetsPath + "/ICS.csv");
            fs.Close();
        }

        //StreamWriter sw = new StreamWriter("Assets/Resources/ICS.csv");
        StreamWriter sw = new StreamWriter(Application.persistentDataPath + "/ICS.csv");
        //StreamWriter sw = new StreamWriter(Application.streamingAssetsPath + "/ICS.csv");

        //TODO:CharactersAllのセーブ・ロード
        for (int i = 1; i < CharactersAllIndexNum; i++)
        {
            sw.WriteLine("CharactersAll[" + i.ToString() + "].KnownPixels," + CharactersAll[i].KnownPixels);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].OwnedNumMax," + CharactersAll[i].OwnedNumMax);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].OwnedNumCur," + CharactersAll[i].OwnedNumCur);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].ReincarnationTimes," + CharactersAll[i].ReincarnationTimes);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].Level," + CharactersAll[i].Level);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].Exp," + CharactersAll[i].Exp);
            sw.WriteLine("CharactersAll[" + i.ToString() + "].ExpMax," + CharactersAll[i].ExpMax);

            for (int j = 0; j < 5; j++)
            {
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].HPMax," + CharactersAll[i].Stats[j].HPMax);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].HPCur," + CharactersAll[i].Stats[j].HPCur);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].ATK," + CharactersAll[i].Stats[j].ATK);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].DEF," + CharactersAll[i].Stats[j].DEF);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].SPD," + CharactersAll[i].Stats[j].SPD);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].LUC," + CharactersAll[i].Stats[j].LUC);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].OBS," + CharactersAll[i].Stats[j].OBS);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].HealPower," + CharactersAll[i].Stats[j].HealPower);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].RCreates," + CharactersAll[i].Stats[j].RCreates);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].GCreates," + CharactersAll[i].Stats[j].GCreates);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].BCreates," + CharactersAll[i].Stats[j].BCreates);
                sw.WriteLine("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].PaintPixels," + CharactersAll[i].Stats[j].PaintPixels);
            }

            if (CharactersAll[i].FlagFNT)
            {
                sw.WriteLine("CharactersAll[" + i.ToString() + "].FlagFNT," + "true");
            }
            else 
            {
                sw.WriteLine("CharactersAll[" + i.ToString() + "].FlagFNT," + "false");
            }

            sw.WriteLine("CharactersAll[" + i.ToString() + "].Whereabouts," + CharactersAll[i].Whereabouts);

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
                ColorProductionPixel[i].r.ToString());

            sw.WriteLine("ColorProductionPixel[" + i.ToString() + "].g," +
                ColorProductionPixel[i].g.ToString());

            sw.WriteLine("ColorProductionPixel[" + i.ToString() + "].b," +
                ColorProductionPixel[i].b.ToString());

            sw.WriteLine("ProgressProductionPixel[" + i.ToString() + "].r," +
                ProgressProductionPixel[i, 1].ToString());
            sw.WriteLine("ProgressProductionPixel[" + i.ToString() + "].g," +
                ProgressProductionPixel[i, 2].ToString());
            sw.WriteLine("ProgressProductionPixel[" + i.ToString() + "].b," +
                ProgressProductionPixel[i, 3].ToString());
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

            if (CharactersIDProductionCharacter[i] == 0 || CharactersIDProducedCharacter[i] == 0)
                continue;

            sw.WriteLine("ConsumePixelsProductionCharacter[" + i.ToString() + "].Count," +
                ConsumePixelsProductionCharacter[i].Count.ToString());
            for (int j = 0; j < ConsumePixelsProductionCharacter[i].Count; j++)
            {
                ConsumePixelClass consumePixel = ConsumePixelsProductionCharacter[i][j];
                sw.WriteLine("ConsumePixelsProductionCharacter[" + i.ToString() + "][" + j.ToString() + "].r," +
                    consumePixel.PixelColor.r.ToString());
                sw.WriteLine("ConsumePixelsProductionCharacter[" + i.ToString() + "][" + j.ToString() + "].g," +
                    consumePixel.PixelColor.g.ToString());
                sw.WriteLine("ConsumePixelsProductionCharacter[" + i.ToString() + "][" + j.ToString() + "].b," +
                    consumePixel.PixelColor.b.ToString());
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

        sw.Flush();
        sw.Close();
    }

    public void Load(ref CharacterClass[] CharactersAll, int CharactersAllIndexNum,
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
        ref uint[] CharactersIDProductionCharacter,
        ref uint[] CharactersIDProducedCharacter,
        List<bool[,]> ProgressTextureProductionCharacter,
        List<ConsumePixelClass>[] ConsumePixelsProductionCharacter,
        ref ulong[,,] CurPixels,
        ref uint[,,] BattlePartyCharacterIds,
        ref int ActiveBattlePartySet
        )
    {
        Debug.Log("ロード : " + Application.persistentDataPath + "/ICS.csv");
        //Debug.Log("ロード : " + Application.streamingAssetsPath + "/ICS.csv");

        //if (File.Exists("Assets/Resources/ICS.csv"))
        if (File.Exists(Application.persistentDataPath + "/ICS.csv"))
        //if (File.Exists(Application.streamingAssetsPath + "/ICS.csv"))
        {
            Debug.Log("セーブファイルが存在します");
        }
        else
        {
            Debug.Log("セーブファイルが存在しません");

            return;
        }

        //StreamReader sr = new StreamReader("Assets/Resources/ICS.csv");
        StreamReader sr = new StreamReader(Application.persistentDataPath + "/ICS.csv");
        //StreamReader sr = new StreamReader(Application.streamingAssetsPath + "/ICS.csv");

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
            string[] values = line.Split(',');

            if (values[0].Equals("CurR"))
            {
                CurR = (ulong)(int.Parse(values[1]));
            }
            else
            if (values[0].Equals("CurG"))
            {
                CurG = (ulong)(int.Parse(values[1]));
            }
            else
            if (values[0].Equals("CurB"))
            {
                CurB = (ulong)(int.Parse(values[1]));
            }

            else
            if (values[0].Equals("MaxR"))
            {
                MaxR = (ulong)(int.Parse(values[1]));
            }
            else
            if (values[0].Equals("MaxG"))
            {
                MaxG = (ulong)(int.Parse(values[1]));
            }
            else
            if (values[0].Equals("MaxB"))
            {
                MaxB = (ulong)(int.Parse(values[1]));
            }

            else
            if (values[0].Equals("CostMaxRUp"))
            {
                CostMaxRUp = (ulong)(int.Parse(values[1]));
            }
            else
            if (values[0].Equals("CostMaxGUp"))
            {
                CostMaxGUp = (ulong)(int.Parse(values[1]));
            }
            else
            if (values[0].Equals("CostMaxBUp"))
            {
                CostMaxBUp = (ulong)(int.Parse(values[1]));
            }

            else
            if (values[0].Equals("IncreaseValueR"))
            {
                IncreaseValueR = (ulong)(int.Parse(values[1]));
            }
            else
            if (values[0].Equals("IncreaseValueG"))
            {
                IncreaseValueG = (ulong)(int.Parse(values[1]));
            }
            else
            if (values[0].Equals("IncreaseValueB"))
            {
                IncreaseValueB = (ulong)(int.Parse(values[1]));
            }

            else
            if (values[0].Equals("CostIncreaseValueRUp"))
            {
                CostIncreaseValueRUp = (ulong)(int.Parse(values[1]));
            }
            else
            if (values[0].Equals("CostIncreaseValueGUp"))
            {
                CostIncreaseValueGUp = (ulong)(int.Parse(values[1]));
            }
            else
            if (values[0].Equals("CostIncreaseValueBUp"))
            {
                CostIncreaseValueBUp = (ulong)(int.Parse(values[1]));
            }


            else
            if (values[0].StartsWith("CharactersIDHelpProduction"))
            {
                for (int i = 1; i <= 3; i++)
                {
                    if (values[0].Equals("CharactersIDHelpProductionR[" + i.ToString() + "]"))
                    {
                        CharactersIDHelpProductionR[i] = (uint)(int.Parse(values[1]));
                        break;
                    }

                    if (values[0].Equals("CharactersIDHelpProductionG[" + i.ToString() + "]"))
                    {
                        CharactersIDHelpProductionG[i] = (uint)(int.Parse(values[1]));
                        break;
                    }

                    if (values[0].Equals("CharactersIDHelpProductionB[" + i.ToString() + "]"))
                    {
                        CharactersIDHelpProductionB[i] = (uint)(int.Parse(values[1]));
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
                            (uint)(int.Parse(values[1]));
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
                            float.Parse(values[1]);
                        break;
                    }

                    if (values[0].Equals("ColorProductionPixel[" + i.ToString() + "].g"))
                    {
                        ColorProductionPixel[i].g =
                            float.Parse(values[1]);
                        break;
                    }

                    if (values[0].Equals("ColorProductionPixel[" + i.ToString() + "].b"))
                    {
                        ColorProductionPixel[i].b =
                            float.Parse(values[1]);
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
                    CurPixels[curPixelsLoadR, curPixelsLoadG, curPixelsLoadB] = ulong.Parse(values[1]);
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
                            pixelColor.r = float.Parse(values[1]);
                        else
                        if (field.Equals("g"))
                            pixelColor.g = float.Parse(values[1]);
                        else
                        if (field.Equals("b"))
                            pixelColor.b = float.Parse(values[1]);
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
                    BattlePartyCharacterIds[setIndex, x, y] = (uint)int.Parse(values[1]);
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
                            (uint)(int.Parse(values[1]));
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
                            (uint)(int.Parse(values[1]));
                        break;
                    }
                }
            }


            else
            if (values[0].StartsWith("CharactersAll"))
            {

                for (int i = 1; i < CharactersAllIndexNum; i++)
                {
                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].KnownPixels"))
                    {
                        CharactersAll[i].KnownPixels = (uint)(int.Parse(values[1]));
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].OwnedNumMax"))
                    {
                        CharactersAll[i].OwnedNumMax = (uint)(int.Parse(values[1]));
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].OwnedNumCur"))
                    {
                        CharactersAll[i].OwnedNumCur = (uint)(int.Parse(values[1]));
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].ReincarnationTimes"))
                    {
                        CharactersAll[i].ReincarnationTimes = (uint)(int.Parse(values[1]));
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].Level"))
                    {
                        CharactersAll[i].Level = (uint)(int.Parse(values[1]));
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].Exp"))
                    {
                        CharactersAll[i].Exp = (uint)(int.Parse(values[1]));
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].ExpMax"))
                    {
                        CharactersAll[i].ExpMax = (uint)(int.Parse(values[1]));
                        break;
                    }

                    //TODO:FlagFNTテスト
                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].FlagFNT"))
                    {
                        CharactersAll[i].FlagFNT = values[1].Equals("true");
                        break;
                    }

                    if (values[0].Equals("CharactersAll[" + i.ToString() + "].Whereabouts"))
                    {
                        if (values[1].Equals("None"))
                        {
                            CharactersAll[i].Whereabouts = Place.None;
                        }
                        else
                        if (values[1].Equals("CreateR"))
                        {
                            CharactersAll[i].Whereabouts = Place.CreateR;
                        }
                        else
                        if (values[1].Equals("CreateG"))
                        {
                            CharactersAll[i].Whereabouts = Place.CreateG;
                        }
                        else
                        if (values[1].Equals("CreateB"))
                        {
                            CharactersAll[i].Whereabouts = Place.CreateB;
                        }
                        else
                        if (values[1].Equals("CreatePixel"))
                        {
                            CharactersAll[i].Whereabouts = Place.CreatePixel;
                        }
                        else
                        if (values[1].Equals("CreateCharacter"))
                        {
                            CharactersAll[i].Whereabouts = Place.CreateCharacter;
                        }
                        else
                        if (values[1].Equals("Hospital"))
                        {
                            CharactersAll[i].Whereabouts = Place.Hospital;
                        }
                        else
                        if (values[1].Equals("Battle"))
                        {
                            CharactersAll[i].Whereabouts = Place.Battle;
                        }

                    }

                    bool FlagHit = false;
                    for (int j = 0; j < 5; j++)
                    {
                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].HPMax"))
                        {
                            CharactersAll[i].Stats[j].HPMax = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].HPCur"))
                        {
                            CharactersAll[i].Stats[j].HPCur = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].ATK"))
                        {
                            CharactersAll[i].Stats[j].ATK = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].DEF"))
                        {
                            CharactersAll[i].Stats[j].DEF = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].SPD"))
                        {
                            CharactersAll[i].Stats[j].SPD = (byte)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].LUC"))
                        {
                            CharactersAll[i].Stats[j].LUC = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].OBS"))
                        {
                            CharactersAll[i].Stats[j].OBS = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].HealPower"))
                        {
                            CharactersAll[i].Stats[j].HealPower = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].RCreates"))
                        {
                            CharactersAll[i].Stats[j].RCreates = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].GCreates"))
                        {
                            CharactersAll[i].Stats[j].GCreates = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].BCreates"))
                        {
                            CharactersAll[i].Stats[j].BCreates = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }

                        if (values[0].Equals("CharactersAll[" + i.ToString() + "].Stats[" + j.ToString() + "].PaintPixels"))
                        {
                            CharactersAll[i].Stats[j].PaintPixels = (uint)(int.Parse(values[1]));
                            FlagHit = true;
                            break;
                        }
                    }
                    if (FlagHit == true)
                    {
                        break;
                    }

                }
            }

        }

        sr.Close();
    }
}
