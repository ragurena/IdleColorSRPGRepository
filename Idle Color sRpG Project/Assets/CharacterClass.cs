using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



enum CharacterType { Fire, Grass, Water, None };
enum Place { CreateRGB, CreatePixel, CreateCharacter, Hospital, Battle, None };


public class CharacterClass //: MonoBehaviour
{
    //キャラクターのID
    uint ID;
    //キャラクターのTexture2D型の画像
    Texture2D ImageTexture2D;
    //キャラクターの名前
    string Name;
    //サイズ
    ushort Size;
    //TODO:観察ピクセル数
    uint KnownPixels;
    //属性
    CharacterType CharacterType;
    //TODO:所有数
    ulong OwnedNumMax;
    ulong OwnedNumCur;
    //TODO:転生回数
    ulong ReincarnationTimes;
    //TODO:レベル
    ulong Level;
    //TODO:経験値
    ulong Exp;
    ulong ExpMax;

    //ステータス　0:トータル　1:基本ステータス　2:レベルステータス　3:武器ステータス　4:装飾品ステータス
    StatisticsClass[] Stats = new StatisticsClass[5];

    ////体力
    //ulong HPMax;
    //ulong HPCur;
    ////攻撃力
    //ulong ATK;
    ////防御力
    //ulong DEF;
    ////素早さ
    //ulong SPD;
    ////運
    //ulong LUC;
    ////観察力
    //ulong OBS;
    ////TODO:治癒力
    //ulong HealPower;
    ////RGB作成数
    //ulong RCreates;
    //ulong GCreates;
    //ulong BCreates;
    ////TODO:ピクセル作成数
    ////ulong GetCreatePixels(int r,int g,int b);
    ////TODO:描画数
    //ulong PaintPixels;

    //TODO:瀕死フラグ
    bool FlagFNT;
    //TODO:居場所
    Place Whereabouts;
    //TODO:装備武器
    //TODO:装備装飾品


    public CharacterClass()
    {
        Debug.Log("new StatisticsClass");
        for (int i = 0; i < 5; i++)
        {
            Stats[i] = new StatisticsClass();
            //Stats[i] = GameObject.Find("GameObject").GetComponent<StatisticsClass>();
            //Stats[i] = GetComponent<StatisticsClass>();
        }

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool MakeCharacter(Texture2D argImage, uint argID, string argName)
    {
        if (argImage == null)
        {
            Debug.Log("Error!!!!!!!!!");
            return false;
        }

        //画像が正方形でない場合
        if (argImage.width != argImage.height)
        {
            Debug.Log("Error!!!!!!!!!");
            return false;
        }

        Size = (ushort)argImage.height;

        ImageTexture2D = NomalizationImage(argImage);

        ID = argID;

        Name = argName;

        //TODO:ステータスの設定
        if (CalcCharacterStats() == false)
        {
            Debug.Log("Error!!!!!!!!!");
            return false;
        }

        Debug.Log("ID[" + ID + "] " + Name + "\n" +
                "Size : " + Size + "\n" +
                "CharacterType : " + CharacterType + "\n" +
                "HPMax : " + Stats[1].HPMax + "\n" +
                "HPCur : " + Stats[1].HPCur + "\n" +
                "ATK : " + Stats[1].ATK + "\n" +
                "DEF : " + Stats[1].DEF + "\n" +
                "SPD : " + Stats[1].SPD + "\n" +
                "LUC : " + Stats[1].LUC + "\n" +
                "OBS : " + Stats[1].OBS + "\n" +
                "RCreates : " + Stats[1].RCreates + "\n" +
                "GCreates : " + Stats[1].GCreates + "\n" +
                "BCreates : " + Stats[1].BCreates
            );

        return true;
    }

    Texture2D NomalizationImage(Texture2D argImage)
    {
        if (argImage == null)
        {
            Debug.Log("Error!!!!!!!!!");
            return null;
        }

        //画像が正方形でない場合
        if (argImage.width != argImage.height)
        {
            Debug.Log("Error!!!!!!!!!");
            return null;
        }

        //アルファチャンネルを二値化、透過画素の色を0
        Color[] ImageColor = argImage.GetPixels(0, 0, argImage.width, argImage.height);
        for (int x = 0; x < argImage.width; x++)
        {
            for (int y = 0; y < argImage.height; y++)
            {
                if(ImageColor[x + y * argImage.width].a < (float)(0.5))
                {
                    ImageColor[x + y * argImage.width].a = (float)(0.0);
                    ImageColor[x + y * argImage.width].r = (float)(0.0);
                    ImageColor[x + y * argImage.width].g = (float)(0.0);
                    ImageColor[x + y * argImage.width].b = (float)(0.0);
                }
                else
                {
                    ImageColor[x + y * argImage.width].a = (float)(1.0);
                }

            }
        }

        Texture2D resultTexture2D = argImage;
        resultTexture2D.SetPixels(0, 0, argImage.width, argImage.height, ImageColor);

        return resultTexture2D;
    }

    bool CalcCharacterStats()
    {
        if (ImageTexture2D == null)
        {
            Debug.Log("Error!!!!!!!!!");
            return false;
        }

        Color[] ImageColor = ImageTexture2D.GetPixels(0, 0, ImageTexture2D.width, ImageTexture2D.height);

        uint RPixelValues = 0;
        uint GPixelValues = 0;
        uint BPixelValues = 0;
        uint RPixels = 0;
        uint GPixels = 0;
        uint BPixels = 0;
        uint APixels = 0;
        uint NoneRGBPixels = 0;
        bool[,,] ExistsColor = new bool[256, 256, 256];

        for (int x = 0; x < ImageTexture2D.width; x++)
        {
            for (int y = 0; y < ImageTexture2D.height; y++)
            {
                Debug.Log("ImageColor[" + x + "][" + y + "] : " + ImageColor[x + y * ImageTexture2D.width]);

                if (ImageColor[x + y * ImageTexture2D.width].a != 0.0)
                {
                    ExistsColor[(int)(ImageColor[x + y * ImageTexture2D.width].r * 255), (int)(ImageColor[x + y * ImageTexture2D.width].g * 255), (int)(ImageColor[x + y * ImageTexture2D.width].b * 255)]
                    = true;
                }

                //RGBの各合計値を算出
                RPixelValues += (uint)(ImageColor[x + y * ImageTexture2D.width].r * 255);
                GPixelValues += (uint)(ImageColor[x + y * ImageTexture2D.width].g * 255);
                BPixelValues += (uint)(ImageColor[x + y * ImageTexture2D.width].b * 255);

                //透過,R,G,Bのピクセル数を算出
                if (ImageColor[x + y * ImageTexture2D.width].a == 0.0)
                {
                    APixels++;
                }
                else 
                if ((ImageColor[x + y * ImageTexture2D.width].r > ImageColor[x + y * ImageTexture2D.width].g) &&
                    (ImageColor[x + y * ImageTexture2D.width].r > ImageColor[x + y * ImageTexture2D.width].b))
                {
                    RPixels++;
                }
                else
                if ((ImageColor[x + y * ImageTexture2D.width].g > ImageColor[x + y * ImageTexture2D.width].r) &&
                    (ImageColor[x + y * ImageTexture2D.width].g > ImageColor[x + y * ImageTexture2D.width].b))
                {
                    GPixels++;
                }
                else
                if ((ImageColor[x + y * ImageTexture2D.width].b > ImageColor[x + y * ImageTexture2D.width].r) &&
                    (ImageColor[x + y * ImageTexture2D.width].b > ImageColor[x + y * ImageTexture2D.width].g))
                {
                    BPixels++;
                }
                else
                {
                    NoneRGBPixels++;
                }

            }
        }
        Debug.Log("RPixelValues : " + RPixelValues);
        Debug.Log("GPixelValues : " + GPixelValues);
        Debug.Log("BPixelValues : " + BPixelValues);

        //属性決め
        if (RPixelValues > GPixelValues && RPixelValues > BPixelValues)
        {
            CharacterType = CharacterType.Fire;
        }
        else
        if (GPixelValues > RPixelValues && GPixelValues > BPixelValues)
        {
            CharacterType = CharacterType.Grass;
        }
        else
        if (BPixelValues > RPixelValues && BPixelValues > GPixelValues)
        {
            CharacterType = CharacterType.Water;
        }
        else
        {
            CharacterType = CharacterType.None;
        }

        //攻撃力の算出
        Stats[1].ATK = RPixelValues + GPixelValues + BPixelValues;

        //防御力の算出
        {
            uint MaxPixelValues = RPixelValues;
            if (MaxPixelValues < GPixelValues)
                MaxPixelValues = GPixelValues;
            if (MaxPixelValues < BPixelValues)
                MaxPixelValues = BPixelValues;

            Stats[1].DEF = MaxPixelValues;
        }

        //体力の算出
        Stats[1].HPMax = (RPixels + GPixels + BPixels + NoneRGBPixels) * (255 + 255 + 255);
        Stats[1].HPCur = Stats[1].HPMax;
        if (Stats[1].HPMax == 0)
        {
            Stats[1].HPMax = 1;
            Stats[1].HPCur = 1;
        }

        //素早さの算出
        //SPD = APixels;
        Stats[1].SPD = (ulong)(((float)APixels / (ImageTexture2D.width * ImageTexture2D.height)) * 100);
        if (Stats[1].SPD == 0)
            Stats[1].SPD = 1;

        //諧調数の算出
        {
            uint GradationNum = 0;
            for(int r = 0; r < 256; r++)
            {
                for (int g = 0; g < 256; g++)
                {
                    for (int b = 0; b < 256; b++)
                    {
                        if (ExistsColor[r, g, b] == true)
                        {
                            Debug.Log("諧調 : r" + r + " g" + g + " b" + b);
                            GradationNum++;
                        }
                    }
                }
            }

            //運の算出
            Stats[1].LUC = GradationNum;
            //観察力の算出
            Stats[1].OBS = (ulong)(Mathf.Floor(GradationNum / ImageTexture2D.height)) + 1;
        }


        //RGB作成数の算出
        Debug.Log("APixels : " + APixels);
        Debug.Log("RPixels : " + RPixels);
        Debug.Log("GPixels : " + GPixels);
        Debug.Log("BPixels : " + BPixels);
        Debug.Log("NoneRGBPixels : " + NoneRGBPixels);
        Debug.Log("Total : " + (APixels + RPixels + GPixels + BPixels + NoneRGBPixels));

        RPixels += NoneRGBPixels / 3;
        GPixels += NoneRGBPixels / 3;
        BPixels += NoneRGBPixels / 3;
        if (NoneRGBPixels % 3 == 1)
        {
            RPixels++;
        }
        else if (NoneRGBPixels % 3 == 2)
        {
            RPixels++;
            GPixels++;
        }
        Debug.Log("RPixels : " + RPixels);
        Debug.Log("GPixels : " + GPixels);
        Debug.Log("BPixels : " + BPixels);
        Debug.Log("Total : " + (APixels + RPixels + GPixels + BPixels));

        Stats[1].RCreates = RPixels;
        Stats[1].GCreates = GPixels;
        Stats[1].BCreates = BPixels;

        return true;
    }

    ulong GetCreatePixels(int r, int g, int b)
    {
        //TODO:GetCreatePixelsの処理
        return 0;
    }


}
