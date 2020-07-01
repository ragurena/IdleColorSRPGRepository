using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using OpenCvSharp;



public enum CharacterType { None, Fire, Grass, Water };
public enum Place { None, CreateR, CreateG, CreateB, CreatePixel, CreateCharacter, Hospital, Battle };

public class ExistColor
{
    public Color Color = new Color(0,0,0);
    public uint Num = 0;

    public ExistColor(Color argColor, uint argNum)
    {
        Color = argColor;
        Num = argNum;
    }
}


public class CharacterClass //: MonoBehaviour
{
    //キャラクターのID
    public uint ID;
    //キャラクターのTexture2D型の画像
    //public Texture2D ImageTexture2D;
    public string ImagePath;
    //キャラクターの名前
    public string Name;
    //サイズ
    public ushort Size;
    //TODO:観察ピクセル数
    public uint KnownPixels;
    //属性
    public CharacterType CharacterType;
    //TODO:所有数
    public ulong OwnedNumMax;
    public ulong OwnedNumCur;
    //TODO:転生回数
    public ulong ReincarnationTimes;
    //TODO:レベル
    public ulong Level;
    //TODO:経験値
    public ulong Exp;
    public ulong ExpMax;

    //ステータス　0:トータル　1:基本ステータス　2:レベルステータス　3:武器ステータス　4:装飾品ステータス
    public StatisticsClass[] Stats = new StatisticsClass[5];

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
    //描画数
    public ulong PaintPixels;

    //TODO:瀕死フラグ
    public bool FlagFNT;
    //TODO:居場所
    public Place Whereabouts;
    //TODO:装備武器
    //TODO:装備装飾品

    //TODO:ピクセル作成数
    //ulong GetCreatePixels(int r,int g,int b);

    //諧調数
    uint GradationNum = 0;
    //uint[,,] ExistsColors = new uint[256, 256, 256];
    //public uint GetExistsColors(Color argColor);
    public List<ExistColor> ListExistsColors = new List<ExistColor>();

    //透過ピクセル数
    public uint APixels = 0;


    public CharacterClass()
    {
        //Debug.Log("new StatisticsClass");
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

    //public bool MakeCharacter(Texture2D argImage, uint argID, string argName)
    public bool MakeCharacter(string argImagePath, uint argID, string argName)
    {
        Debug.Log("argImagePath : " + argImagePath);
        //Texture2D Image = Resources.Load(argImagePath, typeof(Texture2D)) as Texture2D;
        Texture2D Image = ImagegUtility.ReadPng(argImagePath);

        if (Image == null)
        {
            Debug.Log("Error!!!!!!!!!");
            return false;
        }

        //画像が正方形でない場合
        if (Image.width != Image.height)
        {
            Debug.Log("Error!!!!!!!!!");
            return false;
        }

        Size = (ushort)Image.height;

        //ImageTexture2D = NomalizationImage(argImage);
        Debug.Log("ImagePath : " + ImagePath);
        ImagePath = NomalizationImage(Image, argImagePath);
        Debug.Log("ImagePath : " + ImagePath);

        ID = argID;

        Name = argName;

        //TODO:OwnedNumCur仮置き
        OwnedNumCur = 1;

        Whereabouts = Place.None;

        //TODO:ステータスの設定
        if (CalcCharacterStats() == false)
        {
            Debug.Log("Error!!!!!!!!!");
            return false;
        }
        CalcTotalStats();

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

        Object.DestroyImmediate(Image);

        return true;
    }

    //Texture2D NomalizationImage(Texture2D argImage)
    string NomalizationImage(Texture2D argImage, string argImagePath)
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

        if(!(argImage.width == 8 ||
           argImage.width == 16 ||
           argImage.width == 32 ||
           argImage.width == 64 ||
           argImage.width == 128 ||
           argImage.width == 256 ||
           argImage.width == 512 ||
           argImage.width == 1024 ||
           argImage.width == 2048))
        {
            Debug.Log("Error!!!!!!!!!");
            return null;
        }

        //ポスタリゼーション
        argImage = Posterization(argImage, 16);

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

        //resultTexture2Dの画像出力
        string FileName = Path.GetFileName(argImagePath);
        string resultImagePath = argImagePath.Substring(0, argImagePath.Length - FileName.Length) + "Nomalization/Nomalization_" + FileName;
        Debug.Log("resultImagePath : " + resultImagePath);
        File.WriteAllBytes(resultImagePath, resultTexture2D.EncodeToPNG());
        //File.WriteAllBytes("Assets/Resources/" + resultImagePath + ".png", new Texture2D(resultTexture2D.width, resultTexture2D.height, TextureFormat.RGBA32, false).EncodeToPNG());

        ////テクスチャの設定
        ////Texture2D tex = Resources.Load(resultImagePath, typeof(Texture2D)) as Texture2D;
        //Texture2D tex = ReadPng(Application.dataPath + "/Resources/" + resultImagePath + ".png");
        //if (tex == null)
        //{
        //    Debug.Log("tex == null Error!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        //}
        //string assetPath = UnityEditor.AssetDatabase.GetAssetPath(tex);
        //UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
        //importer.isReadable = true;
        //importer.spritePixelsPerUnit = argImage.width;
        //importer.filterMode = FilterMode.Point;
        //importer.maxTextureSize = 64;

        Object.DestroyImmediate(resultTexture2D);

        return resultImagePath;
    }

    Texture2D Posterization(Texture2D argSrcImg, int argCullNum)
    {
        Mat SrcImg = OpenCvSharp.Unity.TextureToMat(argSrcImg);

        Mat resultMat = new Mat(SrcImg.Height, SrcImg.Width, MatType.CV_8U);
        byte[] LUT = new byte[256];
        for (int x = 0; x < 256; x++)
        {
            int num = ((x + (argCullNum / 2)) / argCullNum) * argCullNum;
            if (num > 255)
                num = 255;
            LUT[x] = (byte)num;
        }

        Cv2.LUT(SrcImg, LUT, resultMat);

        Texture2D resultTexture2D = OpenCvSharp.Unity.MatToTexture(resultMat);
        
        Color[] srcBuffer = argSrcImg.GetPixels();
        Color[] resultBuffer = resultTexture2D.GetPixels();
        for (int i = 0; i < SrcImg.Height * SrcImg.Width; i++)
        {
            resultBuffer.SetValue(new Color(resultBuffer[i].r, resultBuffer[i].g, resultBuffer[i].b, srcBuffer[i].a), i);
        }
        resultTexture2D.SetPixels(resultBuffer);
        resultTexture2D.Apply();

        return resultTexture2D;
    }

    bool CalcCharacterStats()
    {
        //Texture2D ImageTexture2D = Resources.Load(ImagePath, typeof(Texture2D)) as Texture2D;
        Texture2D ImageTexture2D = ImagegUtility.ReadPng(ImagePath);

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
        //uint APixels = 0;
        uint NoneRGBPixels = 0;
        ListExistsColors.Clear();

        //for (int b = 0; b < 256; b++)
        //{
        //    for (int g = 0; g < 256; g++)
        //    {
        //        for (int r = 0; r < 256; r++)
        //        {
        //            ExistsColors[r,g,b] = 0;
        //        }
        //    }
        //}

        for (int x = 0; x < ImageTexture2D.width; x++)
        {
            for (int y = 0; y < ImageTexture2D.height; y++)
            {
                //Debug.Log("ImageColor[" + x + "][" + y + "] : " + ImageColor[x + y * ImageTexture2D.width]);

                if (ImageColor[x + y * ImageTexture2D.width].a != 0.0)
                {
                    //ExistsColors[(int)(ImageColor[x + y * ImageTexture2D.width].r * 255), (int)(ImageColor[x + y * ImageTexture2D.width].g * 255), (int)(ImageColor[x + y * ImageTexture2D.width].b * 255)]
                    //+= 1;
                    if(ListExistsColors.Any(item => item.Color == ImageColor[x + y * ImageTexture2D.width]))
                    {
                        ListExistsColors.Find(item => item.Color == ImageColor[x + y * ImageTexture2D.width]).Num++;
                    }
                    else
                    {
                        ListExistsColors.Add(new ExistColor(ImageColor[x + y * ImageTexture2D.width], 1));
                    }
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
        Debug.Log("PixelValues\n" +
                  "RPixelValues : " + RPixelValues + "\n" +
                  "GPixelValues : " + GPixelValues + "\n" +
                  "BPixelValues : " + BPixelValues);

        //ListExistsColorsのソート
        IOrderedEnumerable<ExistColor> sortList =
            ListExistsColors.OrderBy(item => item.Color.r).ThenBy(item => item.Color.g).ThenBy(item => item.Color.b);
        List< ExistColor> tmpList = new List<ExistColor>();
        foreach (ExistColor element in sortList)
        {
            tmpList.Add(element);
        }
        ListExistsColors.Clear();
        ListExistsColors = tmpList;


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
        Stats[1].SPD = (byte)(((float)APixels / (ImageTexture2D.width * ImageTexture2D.height)) * 100);
        if (Stats[1].SPD == 0)
            Stats[1].SPD = 1;

        //諧調数の算出
        {
            GradationNum = 0;
            //Color C = new Color(0,0,0);
            //for (int r = 0; r < 256; r++)
            //{
            //    for (int g = 0; g < 256; g++)
            //    {
            //        for (int b = 0; b < 256; b++)
            //        {
            //            C.r = r / 255;
            //            C.g = g / 255;
            //            C.b = b / 255;
            //            if (IsExistsColors(C))
            //            {
            //                Debug.Log("諧調 : r" + r + " g" + g + " b" + b);
            //                GradationNum++;
            //            }
            //        }
            //    }
            //}
            GradationNum = (uint)ListExistsColors.Count();
            foreach(ExistColor E in ListExistsColors)
            {
                Debug.Log("諧調 : r" + E.Color.r * 255 + " g" + E.Color.g * 255 + " b" + E.Color.b * 255);
            }

            //運の算出
            Stats[1].LUC = GradationNum;
            //観察力の算出
            Stats[1].OBS = (ulong)(Mathf.Floor(GradationNum / ImageTexture2D.height)) + 1;
            PaintPixels = GradationNum;
        }


        //RGB作成数の算出
        Debug.Log("Pixels \n" + 
                  "APixels : " + APixels + "\n" +
                  "RPixels : " + RPixels + "\n" +
                  "GPixels : " + GPixels + "\n" +
                  "BPixels : " + BPixels + "\n" +
                  "NoneRGBPixels : " + NoneRGBPixels + "\n" +
                  "Total : " + (APixels + RPixels + GPixels + BPixels + NoneRGBPixels));

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
        Debug.Log("Pixels \n" +
                  "RPixels : " + RPixels + "\n" +
                  "GPixels : " + GPixels + "\n" +
                  "BPixels : " + BPixels + "\n" +
                  "Total : " + (APixels + RPixels + GPixels + BPixels));

        Stats[1].RCreates = RPixels;
        Stats[1].GCreates = GPixels;
        Stats[1].BCreates = BPixels;

        Object.DestroyImmediate(ImageTexture2D);

        return true;
    }

    bool CalcTotalStats()
    {
        Stats[0].HPMax = Stats[1].HPMax + Stats[2].HPMax + Stats[3].HPMax + Stats[4].HPMax;
        Stats[0].HPCur = Stats[1].HPCur + Stats[2].HPCur + Stats[3].HPCur + Stats[4].HPCur;
        Stats[0].ATK = Stats[1].ATK + Stats[2].ATK + Stats[3].ATK + Stats[4].ATK;
        Stats[0].DEF = Stats[1].DEF + Stats[2].DEF + Stats[3].DEF + Stats[4].DEF;
        if (Stats[1].SPD + Stats[2].SPD + Stats[3].SPD + Stats[4].SPD <= 100)
        {
            Stats[0].SPD = (byte)(Stats[1].SPD + Stats[2].SPD + Stats[3].SPD + Stats[4].SPD);
        }
        else
        {
            Stats[0].SPD = 100;
        }
        Stats[0].LUC = Stats[1].LUC + Stats[2].LUC + Stats[3].LUC + Stats[4].LUC;
        Stats[0].OBS = Stats[1].OBS + Stats[2].OBS + Stats[3].OBS + Stats[4].OBS;
        Stats[0].HealPower = Stats[1].HealPower + Stats[2].HealPower + Stats[3].HealPower + Stats[4].HealPower;
        Stats[0].RCreates = Stats[1].RCreates + Stats[2].RCreates + Stats[3].RCreates + Stats[4].RCreates;
        Stats[0].GCreates = Stats[1].GCreates + Stats[2].GCreates + Stats[3].GCreates + Stats[4].GCreates;
        Stats[0].BCreates = Stats[1].BCreates + Stats[2].BCreates + Stats[3].BCreates + Stats[4].BCreates;
        Stats[0].PaintPixels = Stats[1].PaintPixels + Stats[2].PaintPixels + Stats[3].PaintPixels + Stats[4].PaintPixels;

        return true;
    }

    public uint GetExistsColors(Color argColor)
    {
        //uint resultColors = 0;

        //Texture2D ImageTexture2D = ReadPng(ImagePath);
        //Color[] ImageColor = ImageTexture2D.GetPixels(0, 0, ImageTexture2D.width, ImageTexture2D.height);

        //for (int x = 0; x < ImageTexture2D.width; x++)
        //{
        //    for (int y = 0; y < ImageTexture2D.height; y++)
        //    {

        //        if (ImageColor[x + y * ImageTexture2D.width].a != 0.0)
        //        {
        //            if(ImageColor[x + y * ImageTexture2D.width].r == argColor.r
        //            && ImageColor[x + y * ImageTexture2D.width].g == argColor.g
        //            && ImageColor[x + y * ImageTexture2D.width].b == argColor.b)
        //            {
        //                resultColors++;
        //            }
        //        }

        //    }
        //}

        //return resultColors;

        if (ListExistsColors.Any(L => L.Color == argColor))
        {
            return ListExistsColors.Find(L => L.Color == argColor).Num;
        }
        else
        {
            return 0;
        }

    }
    public bool IsExistsColors(Color argColor)
    {
        //Texture2D ImageTexture2D = ReadPng(ImagePath);
        //Color[] ImageColor = ImageTexture2D.GetPixels(0, 0, ImageTexture2D.width, ImageTexture2D.height);

        //for (int x = 0; x < ImageTexture2D.width; x++)
        //{
        //    for (int y = 0; y < ImageTexture2D.height; y++)
        //    {

        //        if (ImageColor[x + y * ImageTexture2D.width].a != 0.0)
        //        {
        //            if (ImageColor[x + y * ImageTexture2D.width].r == argColor.r
        //            && ImageColor[x + y * ImageTexture2D.width].g == argColor.g
        //            && ImageColor[x + y * ImageTexture2D.width].b == argColor.b)
        //            {
        //                return true;
        //            }
        //        }

        //    }
        //}

        //return false;

        return ListExistsColors.Any(L => L.Color == argColor);
    }

    public uint GetCreatePixels(ushort r, ushort g, ushort b)
    {
        uint result = 0;

        //result = Size + ExistsColors[r,g,b];
        result = Size + GetExistsColors(new Color(r / 255, g / 255, b / 255));

        return result;
    }

}
