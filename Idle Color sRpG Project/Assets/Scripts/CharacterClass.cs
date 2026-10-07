using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
//using OpenCvSharp;



public enum CharacterType { None, Fire, Grass, Water, Light, Dark };
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
    //正規化済みのキャラクター画像。生産中はこれを使い、PNGは読み直さない
    public Texture2D ImageTexture2D;
    public string ImagePath;
    //キャラクターの名前
    public string Name;
    //サイズ
    public ushort Size;
    //TODO:観察ピクセル数
    public uint KnownPixels;
    //図鑑で倒したことがある。倒すまではシルエット
    public bool CatalogOpened;
    //属性
    public CharacterType CharacterType;
    //代表カラーと弱点カラー。0〜255。起動時に画像から計算し、セーブしない
    public int RepresentativeR;
    public int RepresentativeG;
    public int RepresentativeB;
    public int WeaknessR;
    public int WeaknessG;
    public int WeaknessB;
    //このキャラが攻撃を受けると不利なタイプ。起動時に属性から決める
    public CharacterType WeaknessType;
    //これまでに到達した所持数の最大。戦闘で減っても下がらない
    public ulong OwnedNumMax;
    //現在の所持数。戦闘の残機と同じ。1のときにHPが0になると0になって戦闘不能。最大は BattleBalanceConfig.MaxOwnedCount
    public ulong OwnedNumCur;
    //TODO:転生回数
    public ulong ReincarnationTimes;
    //残機合成の回数。1回につき、基礎＋レベル成長後の HP/ATK/DEF と RGB生産量が5%増える。
    public ulong FusionCount;
    //TODO:レベル
    public ulong Level;
    //TODO:経験値
    public ulong Exp;
    public ulong ExpMax;

    public void GainOwned(int amount, int cap)
    {
        if (amount <= 0)
            return;
        ulong next = OwnedNumCur + (ulong)amount;
        if (cap >= 0 && next > (ulong)cap)
            next = (ulong)cap;
        OwnedNumCur = next;
        RaiseOwnedMax();
    }

    public void SetOwnedCur(int count, int cap)
    {
        if (count < 0)
            count = 0;
        if (cap >= 0 && count > cap)
            count = cap;
        OwnedNumCur = (ulong)count;
        RaiseOwnedMax();
    }

    public void RaiseOwnedMax()
    {
        if (OwnedNumCur > OwnedNumMax)
            OwnedNumMax = OwnedNumCur;
    }

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
    public uint PaintPixels;

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

    // 画像の一辺の二乗から透過を引いた数。0にはしない。
    public int OpaquePixelCount()
    {
        int area = Size * Size;
        if (area < 1)
            return 1;
        int transparent = APixels > (uint)area ? area : (int)APixels;
        int opaque = area - transparent;
        if (opaque < 1)
            return 1;
        return opaque;
    }


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

        string normalizedPath = NormalizedImagePath(argImagePath);
        if (!string.IsNullOrEmpty(normalizedPath) && File.Exists(normalizedPath))
        {
            ImagePath = LoadNormalizedImage(normalizedPath);
            if (ImagePath == null)
                return false;
            return FinishMakeCharacter(argID, argName);
        }

        Texture2D Image = null;
        bool destroySourceImage = false;
        // 1. パスがちゃんと指定されている場合だけ読み込みを試みる
        if (!string.IsNullOrWhiteSpace(argImagePath))
        {
            Image = ImagegUtility.ReadPng(argImagePath);
            destroySourceImage = Image != null;
        }// 2. パスが空、または画像の読み込みに失敗した場合は、Resourcesの仮画像(NoImageSprite)を使う
        if (Image == null)
        {
            Debug.LogWarning("キャラクター画像の読み込みに失敗したため、仮画像(NoImageSprite)を適用します。");// ResourcesからSpriteとして読み込んで、その中身のテクスチャ(texture)を取得する
            Sprite noImageSprite = Resources.Load<Sprite>("NoImageSprite");
            if (noImageSprite != null)
            {
                Image = noImageSprite.texture;
            }
        }


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
        Debug.Log("MakeCharacter ImagePath : " + ImagePath);
        ImagePath = NomalizationImage(Image, argImagePath);
        Debug.Log("MakeCharacter ImagePath : " + ImagePath);
        if (ImagePath == null)
        {
            if (destroySourceImage)
                Object.DestroyImmediate(Image);
            return false;
        }

        bool made = FinishMakeCharacter(argID, argName);

        if (destroySourceImage)
            Object.DestroyImmediate(Image);

        return made;
    }

    bool FinishMakeCharacter(uint argID, string argName)
    {
        ID = argID;

        Name = argName;

        //TODO:OwnedNumCur仮置き
        OwnedNumCur = 1;
        RaiseOwnedMax();

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

        return true;
    }

    string NormalizedImagePath(string argImagePath)
    {
        if (string.IsNullOrWhiteSpace(argImagePath))
            return null;
        string fileName = Path.GetFileName(argImagePath);
        if (string.IsNullOrEmpty(fileName) || argImagePath.Length < fileName.Length)
            return null;
        return argImagePath.Substring(0, argImagePath.Length - fileName.Length) + "Nomalization/Nomalization_" + fileName;
    }

    string LoadNormalizedImage(string resultImagePath)
    {
        ImageTexture2D = ImagegUtility.ReadPng(resultImagePath);
        if (ImageTexture2D == null)
        {
            Debug.Log("Error!!!!!!!!!");
            return null;
        }
        ImageTexture2D.filterMode = FilterMode.Point;
        ImageTexture2D.Apply();
        Size = (ushort)ImageTexture2D.height;
        return resultImagePath;
    }

    //Texture2D NomalizationImage(Texture2D argImage)
    string NomalizationImage(Texture2D argImage, string argImagePath)
    {
        Debug.Log("argImage : " + argImage);

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
           argImage.width == 128 //||
           //argImage.width == 256 ||
           //argImage.width == 512 ||
           //argImage.width == 1024 ||
           //argImage.width == 2048
           ))
        {
            Debug.Log("Error!!!!!!!!!");
            return null;
        }

        string existingPath = NormalizedImagePath(argImagePath);
        if (!string.IsNullOrEmpty(existingPath) && File.Exists(existingPath))
            return LoadNormalizedImage(existingPath);

        //ポスタリゼーション
        argImage = Posterization(argImage, GameConfig.GRADATION_LEVELS);

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
        resultTexture2D.filterMode = FilterMode.Point;
        resultTexture2D.Apply();

        // オリジナルは書き換えず、ポスタリゼーション済みを別ファイルに保存する
        string FileName = Path.GetFileName(argImagePath);
        string resultDirectory = argImagePath.Substring(0, argImagePath.Length - FileName.Length) + "Nomalization";
        if (!Directory.Exists(resultDirectory))
            Directory.CreateDirectory(resultDirectory);
        string resultImagePath = resultDirectory + "/Nomalization_" + FileName;
        Debug.Log("resultImagePath : " + resultImagePath);
        File.WriteAllBytes(resultImagePath, resultTexture2D.EncodeToPNG());
        Object.DestroyImmediate(resultTexture2D);

        // 表示も数値も、保存したファイルを読み直したものを使う
        return LoadNormalizedImage(resultImagePath);
    }

    /// <summary>
    /// ピクセル生産と同じ階調へ減色する。8階調なら各色 0, 32, 64, 96, 128, 160, 192, 224。255以上は255。
    /// </summary>
    /// <param name="srcTex">読み込んだ元画像</param>
    /// <param name="levels">色の段階数（例: 4〜8）</param>
    public static Texture2D Posterization(Texture2D srcTex, int levels = 4)
    {
        if (srcTex == null) return null;
        if (levels < 2)
            levels = 2;

        Color[] pixels = srcTex.GetPixels();
        Color[] newPixels = new Color[pixels.Length];
        int step = 256 / levels;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];

            if (c.a > 0.01f)
            {
                c.r = SnapChannel(c.r, step, levels);
                c.g = SnapChannel(c.g, step, levels);
                c.b = SnapChannel(c.b, step, levels);
            }

            newPixels[i] = c;
        }

        // 3. 新しい Texture2D を作成してピクセルを適用
        Texture2D resultTex = new Texture2D(srcTex.width, srcTex.height, TextureFormat.RGBA32, false);
        resultTex.SetPixels(newPixels);
        resultTex.Apply();

        return resultTex;
    }

    // 0〜255 を、ピクセル一覧と同じ刻みのいちばん近い値にする
    static float SnapChannel(float channel, int step, int levels)
    {
        int value = Mathf.RoundToInt(channel * 255f);
        if (value < 0)
            value = 0;
        if (value >= 255)
            return 1f;

        int index = (value + step / 2) / step;
        if (index < 0)
            index = 0;
        if (index >= levels)
            index = levels - 1;

        int snapped = index * step;
        if (snapped > 255)
            snapped = 255;
        return snapped / 255f;
    }

    bool CalcCharacterStats()
    {
        if (ImageTexture2D == null)
            ImageTexture2D = ImagegUtility.ReadPng(ImagePath);

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
        uint LightPixels = 0;
        uint DarkPixels = 0;
        ulong opaqueRgbSum = 0;
        APixels = 0;
        uint NoneRGBPixels = 0;
        ListExistsColors.Clear();


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
                {
                    float brightness =
                        (ImageColor[x + y * ImageTexture2D.width].r +
                         ImageColor[x + y * ImageTexture2D.width].g +
                         ImageColor[x + y * ImageTexture2D.width].b) * 255f / 3f;
                    if (brightness > 128f)
                        LightPixels++;
                    else
                        DarkPixels++;

                    int channelR = (int)(ImageColor[x + y * ImageTexture2D.width].r * 255f);
                    int channelG = (int)(ImageColor[x + y * ImageTexture2D.width].g * 255f);
                    int channelB = (int)(ImageColor[x + y * ImageTexture2D.width].b * 255f);
                    opaqueRgbSum += (ulong)channelR + (ulong)channelG + (ulong)channelB;

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
            uint opaquePixels = RPixels + GPixels + BPixels + NoneRGBPixels;
            if (opaquePixels == 0)
            {
                CharacterType = CharacterType.None;
            }
            else
            {
                float averageBrightness =
                    (RPixelValues + GPixelValues + BPixelValues) / (float)(opaquePixels * 3);
                CharacterType = averageBrightness > 128f
                    ? CharacterType.Light
                    : CharacterType.Dark;
            }
        }

        uint opaqueCount = RPixels + GPixels + BPixels + NoneRGBPixels;
        uint matchedPixels = 0;
        switch (CharacterType)
        {
            case CharacterType.Fire:
                matchedPixels = RPixels;
                break;
            case CharacterType.Grass:
                matchedPixels = GPixels;
                break;
            case CharacterType.Water:
                matchedPixels = BPixels;
                break;
            case CharacterType.Light:
                matchedPixels = LightPixels;
                break;
            case CharacterType.Dark:
                matchedPixels = DarkPixels;
                break;
            default:
                matchedPixels = 0;
                break;
        }
        Stats[1].ATK = matchedPixels;
        Stats[1].DEF = opaqueCount - matchedPixels;

        //運の算出に使う、不透明ピクセルの4連結の島
        uint islandCount;
        uint largestIslandSize;
        CountOpaqueIslands(ImageColor, ImageTexture2D.width, ImageTexture2D.height, out islandCount, out largestIslandSize);

        //体力 = 不透明ピクセルの R+G+B の合計 / 画像の一辺。端数は切り上げ
        int side = ImageTexture2D.width;
        if (side < 1)
            side = 1;
        long rgbSum = opaqueRgbSum > long.MaxValue ? long.MaxValue : (long)opaqueRgbSum;
        long hp = BattleMath.CeilDivPositive(rgbSum, side);
        if (hp < 1)
            hp = 1;
        Stats[1].HPMax = (ulong)hp;
        Stats[1].HPCur = Stats[1].HPMax;

        //素早さの算出
        //SPD = APixels;
        Stats[1].SPD = (byte)(((float)APixels / (ImageTexture2D.width * ImageTexture2D.height)) * 100);
        if (Stats[1].SPD == 0)
            Stats[1].SPD = 1;

        //諧調数の算出
        {
            GradationNum = 0;

            GradationNum = (uint)ListExistsColors.Count();
            foreach(ExistColor E in ListExistsColors)
            {
                Debug.Log("諧調 : r" + E.Color.r * 255 + " g" + E.Color.g * 255 + " b" + E.Color.b * 255);
            }

            //運の算出
            Stats[1].LUC = islandCount;
            //観察力の算出
            Stats[1].OBS = GradationNum;
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
        SetRepresentativeColors();

        return true;
    }

    void SetRepresentativeColors()
    {
        WeaknessType = ToCharacterType(AttributeWeakness.Of(ToBattleAttribute(CharacterType)));

        ExistColor best = null;
        if (ListExistsColors != null)
        {
            for (int i = 0; i < ListExistsColors.Count; i++)
            {
                ExistColor color = ListExistsColors[i];
                if (best == null || color.Num > best.Num)
                    best = color;
            }
        }

        if (best == null)
        {
            RepresentativeR = 0;
            RepresentativeG = 0;
            RepresentativeB = 0;
            WeaknessR = 0;
            WeaknessG = 0;
            WeaknessB = 0;
            return;
        }

        RepresentativeR = ColorChannel(best.Color.r);
        RepresentativeG = ColorChannel(best.Color.g);
        RepresentativeB = ColorChannel(best.Color.b);
        HsvComplement.Complementary(RepresentativeR, RepresentativeG, RepresentativeB, out WeaknessR, out WeaknessG, out WeaknessB);
    }

    static CharacterAttribute ToBattleAttribute(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Fire: return CharacterAttribute.Fire;
            case CharacterType.Grass: return CharacterAttribute.Grass;
            case CharacterType.Water: return CharacterAttribute.Water;
            case CharacterType.Light: return CharacterAttribute.Light;
            case CharacterType.Dark: return CharacterAttribute.Dark;
            default: return CharacterAttribute.None;
        }
    }

    static CharacterType ToCharacterType(CharacterAttribute type)
    {
        switch (type)
        {
            case CharacterAttribute.Fire: return CharacterType.Fire;
            case CharacterAttribute.Grass: return CharacterType.Grass;
            case CharacterAttribute.Water: return CharacterType.Water;
            case CharacterAttribute.Light: return CharacterType.Light;
            case CharacterAttribute.Dark: return CharacterType.Dark;
            default: return CharacterType.None;
        }
    }

    static int ColorChannel(float value)
    {
        int channel = (int)(value * 255f);
        if (channel < 0)
            return 0;
        if (channel > 255)
            return 255;
        return channel;
    }

    //画像から基礎ステータス(Stats[1])を計算し、レベル回数ぶん成長を掛け直してトータルを更新する。
    public bool RecalculateBaseStats(BattleBalanceConfig config)
    {
        if (ImageTexture2D == null && string.IsNullOrEmpty(ImagePath))
            return false;
        if (CalcCharacterStats() == false)
            return false;
        RebuildLevelStats(config);
        return true;
    }

    //Stats[2]を、基礎ステータスへ成長率をレベル回数ぶん足した増加分にする。
    public void RebuildLevelStats(BattleBalanceConfig config)
    {
        if (config == null)
            config = BattleBalanceConfig.CreateDefault();

        long level = (long)Level;
        if (level < 0)
            level = 0;
        if (level > config.MaxLevel)
            level = config.MaxLevel;

        long baseHp = ToStatLong(Stats[1].HPMax);
        long baseAtk = ToStatLong(Stats[1].ATK);
        long baseDef = ToStatLong(Stats[1].DEF);
        long grownHp;
        long grownAtk;
        long grownDef;
        long grownSpd;
        BattleLevelGrowth.ApplyLevels(baseHp, baseAtk, baseDef, Stats[1].SPD, level, config, out grownHp, out grownAtk, out grownDef, out grownSpd);

        long hpBonus = grownHp - baseHp;
        long atkBonus = grownAtk - baseAtk;
        long defBonus = grownDef - baseDef;
        long spdBonus = grownSpd - Stats[1].SPD;
        if (hpBonus < 0)
            hpBonus = 0;
        if (atkBonus < 0)
            atkBonus = 0;
        if (defBonus < 0)
            defBonus = 0;
        if (spdBonus < 0)
            spdBonus = 0;
        if (spdBonus > byte.MaxValue)
            spdBonus = byte.MaxValue;

        Stats[2].HPMax = (ulong)hpBonus;
        Stats[2].HPCur = (ulong)hpBonus;
        Stats[2].ATK = (ulong)atkBonus;
        Stats[2].DEF = (ulong)defBonus;
        Stats[2].SPD = (byte)spdBonus;

        long baseR = ToStatLong(Stats[1].RCreates);
        long baseG = ToStatLong(Stats[1].GCreates);
        long baseB = ToStatLong(Stats[1].BCreates);
        long rBonus = BattleLevelGrowth.AddGrowth(baseR, config.RgbGrowthRate, level) - baseR;
        long gBonus = BattleLevelGrowth.AddGrowth(baseG, config.RgbGrowthRate, level) - baseG;
        long bBonus = BattleLevelGrowth.AddGrowth(baseB, config.RgbGrowthRate, level) - baseB;
        if (rBonus < 0)
            rBonus = 0;
        if (gBonus < 0)
            gBonus = 0;
        if (bBonus < 0)
            bBonus = 0;
        Stats[2].RCreates = (ulong)rBonus;
        Stats[2].GCreates = (ulong)gBonus;
        Stats[2].BCreates = (ulong)bBonus;

        long need = config.ExpToNext((long)Level, OpaquePixelCount());
        if (need < 1)
            need = 1;
        ExpMax = (ulong)need;
        CalcTotalStats();
    }

    bool CalcTotalStats()
    {
        ulong fusionHp = FusionStatBonus(Stats[1].HPMax, Stats[2].HPMax);
        ulong fusionAtk = FusionStatBonus(Stats[1].ATK, Stats[2].ATK);
        ulong fusionDef = FusionStatBonus(Stats[1].DEF, Stats[2].DEF);
        Stats[0].HPMax = SumCapped(Stats[1].HPMax, Stats[2].HPMax, Stats[3].HPMax, Stats[4].HPMax, fusionHp);
        Stats[0].HPCur = SumCapped(Stats[1].HPCur, Stats[2].HPCur, Stats[3].HPCur, Stats[4].HPCur, fusionHp);
        Stats[0].ATK = SumCapped(Stats[1].ATK, Stats[2].ATK, Stats[3].ATK, Stats[4].ATK, fusionAtk);
        Stats[0].DEF = SumCapped(Stats[1].DEF, Stats[2].DEF, Stats[3].DEF, Stats[4].DEF, fusionDef);
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
        Stats[0].RCreates = SumCapped(Stats[1].RCreates, Stats[2].RCreates, Stats[3].RCreates, Stats[4].RCreates, FusionStatBonus(Stats[1].RCreates, Stats[2].RCreates));
        Stats[0].GCreates = SumCapped(Stats[1].GCreates, Stats[2].GCreates, Stats[3].GCreates, Stats[4].GCreates, FusionStatBonus(Stats[1].GCreates, Stats[2].GCreates));
        Stats[0].BCreates = SumCapped(Stats[1].BCreates, Stats[2].BCreates, Stats[3].BCreates, Stats[4].BCreates, FusionStatBonus(Stats[1].BCreates, Stats[2].BCreates));
        Stats[0].PaintPixels = Stats[1].PaintPixels + Stats[2].PaintPixels + Stats[3].PaintPixels + Stats[4].PaintPixels;

        return true;
    }

    ulong FusionStatBonus(ulong baseStat, ulong levelStat)
    {
        long baseLong = ToStatLong(baseStat);
        long levelLong = ToStatLong(levelStat);
        long grown = baseLong > long.MaxValue - levelLong ? long.MaxValue : baseLong + levelLong;
        long count = FusionCount > (ulong)long.MaxValue ? long.MaxValue : (long)FusionCount;
        long bonus = FusionBonus.Bonus(grown, count);
        if (bonus < 0)
            bonus = 0;
        return (ulong)bonus;
    }

    static ulong SumCapped(ulong a, ulong b, ulong c, ulong d, ulong e)
    {
        ulong cap = (ulong)long.MaxValue;
        ulong sum = 0;
        ulong[] parts = { a, b, c, d, e };
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] >= cap - sum)
                return cap;
            sum += parts[i];
        }
        return sum;
    }

    static long ToStatLong(ulong value)
    {
        if (value > (ulong)long.MaxValue)
            return long.MaxValue;
        return (long)value;
    }

    //レベルアップ分はレベルステータス(Stats[2])へ足し、トータルを作り直す。
    //現在HPは最大HPの増加分だけ増える。
    public void AddBattleLevelGrowth(long hpDelta, long atkDelta, long defDelta, long spdDelta)
    {
        if (hpDelta < 0)
            hpDelta = 0;
        if (atkDelta < 0)
            atkDelta = 0;
        if (defDelta < 0)
            defDelta = 0;
        if (spdDelta < 0)
            spdDelta = 0;

        Stats[2].HPMax += (ulong)hpDelta;
        Stats[2].HPCur += (ulong)hpDelta;
        Stats[2].ATK += (ulong)atkDelta;
        Stats[2].DEF += (ulong)defDelta;

        long spd = Stats[2].SPD + spdDelta;
        if (spd > byte.MaxValue)
            spd = byte.MaxValue;
        Stats[2].SPD = (byte)spd;
        CalcTotalStats();
    }

    public uint GetExistsColors(Color argColor)
    {

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

        return ListExistsColors.Any(L => L.Color == argColor);
    }

    public uint GetCreatePixels(ushort r, ushort g, ushort b)
    {
        //画像の一辺に、色コードと同じ 0〜255 のピクセル数を足す
        return Size + CountMatchingColors(r, g, b);
    }

    uint CountMatchingColors(ushort r, ushort g, ushort b)
    {
        if (ListExistsColors == null)
            return 0;
        uint count = 0;
        for (int i = 0; i < ListExistsColors.Count; i++)
        {
            ExistColor entry = ListExistsColors[i];
            if (entry == null)
                continue;
            if (ToColorChannel(entry.Color.r) != r || ToColorChannel(entry.Color.g) != g || ToColorChannel(entry.Color.b) != b)
                continue;
            count += entry.Num;
        }
        return count;
    }

    static ushort ToColorChannel(float value)
    {
        int channel = (int)(value * 255f);
        if (channel < 0)
            return 0;
        if (channel > 255)
            return 255;
        return (ushort)channel;
    }

    void CountOpaqueIslands(Color[] imageColor, int width, int height, out uint islandCount, out uint largestIslandSize)
    {
        islandCount = 0;
        largestIslandSize = 0;
        bool[] visited = new bool[width * height];
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int start = x + y * width;
                if (visited[start] || imageColor[start].a == 0.0f)
                    continue;

                islandCount++;
                uint size = 0;
                Stack<int> stack = new Stack<int>();
                stack.Push(start);
                visited[start] = true;

                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    size++;
                    int cx = i % width;
                    int cy = i / width;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + dx[d];
                        int ny = cy + dy[d];
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            continue;
                        int ni = nx + ny * width;
                        if (visited[ni] || imageColor[ni].a == 0.0f)
                            continue;
                        visited[ni] = true;
                        stack.Push(ni);
                    }
                }

                if (size > largestIslandSize)
                    largestIslandSize = size;
            }
        }
    }

}
