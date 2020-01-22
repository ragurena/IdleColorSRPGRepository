using System.Collections;
using System.Collections.Generic;
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
        uint[] CharactersIDHelpProductionR, uint[] CharactersIDHelpProductionG, uint[] CharactersIDHelpProductionB)
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

        //TODO:CurPixelsのセーブ・ロード

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

        sw.Flush();
        sw.Close();
    }

    public void Load(ref CharacterClass[] CharactersAll, int CharactersAllIndexNum,
        ref ulong CurR, ref ulong CurG, ref ulong CurB,
        ref ulong MaxR, ref ulong MaxG, ref ulong MaxB,
        ref ulong CostMaxRUp, ref ulong CostMaxGUp, ref ulong CostMaxBUp,
        ref ulong IncreaseValueR, ref ulong IncreaseValueG, ref ulong IncreaseValueB,
        ref ulong CostIncreaseValueRUp, ref ulong CostIncreaseValueGUp, ref ulong CostIncreaseValueBUp,
        ref uint[] CharactersIDHelpProductionR, ref uint[] CharactersIDHelpProductionG, ref uint[] CharactersIDHelpProductionB)
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
    }
}
