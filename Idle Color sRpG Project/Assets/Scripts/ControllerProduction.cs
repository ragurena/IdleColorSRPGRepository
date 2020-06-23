using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using OpenCvSharp;

using System.IO;

using UnityEngine.Networking;

using UnityEngine.Advertisements;


public enum Trigger {User, Update};

static class Constants
{
    public const int CHARACTERS_ALL_NUM = 32;
    public const int CHARACTERS_HELP_PRODUCTION_NUM = 3;
    public const int CHARACTERS_PRODUCTION_PIXEL_NUM = 5;
}

//生産画面のコントロール
public class ControllerProduction : MonoBehaviour
{
    [SerializeField] EventSystem eventSystem;

    ModelProduction ModelProduction;
    ControllerCharacterSelectClass ControllerCharacterSelect;

    CharacterClass[] CharactersAll = new CharacterClass[Constants.CHARACTERS_ALL_NUM + 1];

    uint[] CharactersIDHelpProductionR = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] CharactersIDHelpProductionG = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] CharactersIDHelpProductionB = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];

    uint[] CharactersIDProductionPixel = new uint[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1];
    Color[] ColorProductionPixel = new Color[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1];
    ushort[,] ProgressProductionPixel = new ushort[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1, 3 + 1];
    //bool[,] WarningLackRGB = new bool[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1, 3 + 1];

    //現在の全カラーの個数(CurColors[0,0,0]が黒、CurColors[255,0,0]が赤)
    ulong[,,] CurPixels = new ulong[256, 256, 256];

    //現在のRGB値
    ulong CurR = 0;
    ulong CurG = 0;
    ulong CurB = 0;
    [SerializeField] Text TextR;
    [SerializeField] Text TextG;
    [SerializeField] Text TextB;
    [SerializeField] Slider SliderR;
    [SerializeField] Slider SliderG;
    [SerializeField] Slider SliderB;
    [SerializeField] Image ImageAttentionR;
    [SerializeField] Image ImageAttentionG;
    [SerializeField] Image ImageAttentionB;

    //最大値
    ulong MaxR = 1024;
    ulong MaxG = 1024;
    ulong MaxB = 1024;

    //最大値を上げるコスト
    ulong CostMaxRUp = 512;
    ulong CostMaxGUp = 512;
    ulong CostMaxBUp = 512;
    [SerializeField] Text TextCostMaxRUp;
    [SerializeField] Text TextCostMaxGUp;
    [SerializeField] Text TextCostMaxBUp;
    [SerializeField] Slider SliderCostMaxRUp;
    [SerializeField] Slider SliderCostMaxGUp;
    [SerializeField] Slider SliderCostMaxBUp;

    [SerializeField] Button ButtonMaxRUp;
    [SerializeField] Button ButtonMaxGUp;
    [SerializeField] Button ButtonMaxBUp;


    //増加値
    ulong IncreaseValueR = 1;
    ulong IncreaseValueG = 1;
    ulong IncreaseValueB = 1;
    [SerializeField] Text TextIncreaseValueRLeft;
    [SerializeField] Text TextIncreaseValueRRight;
    [SerializeField] Text TextIncreaseValueGLeft;
    [SerializeField] Text TextIncreaseValueGRight;
    [SerializeField] Text TextIncreaseValueBLeft;
    [SerializeField] Text TextIncreaseValueBRight;

    //増加値を上げるコスト
    ulong CostIncreaseValueRUp = 16;
    ulong CostIncreaseValueGUp = 16;
    ulong CostIncreaseValueBUp = 16;
    [SerializeField] Text TextCostIncreaseValueRUp;
    [SerializeField] Text TextCostIncreaseValueGUp;
    [SerializeField] Text TextCostIncreaseValueBUp;
    [SerializeField] Slider SliderCostIncreaseValueRUp;
    [SerializeField] Slider SliderCostIncreaseValueGUp;
    [SerializeField] Slider SliderCostIncreaseValueBUp;

    [SerializeField] Button ButtonIncreaseValueRUp;
    [SerializeField] Button ButtonIncreaseValueGUp;
    [SerializeField] Button ButtonIncreaseValueBUp;


    //ピクセル生産のクリック時の進捗数
    ushort UserProductionPixelNum = 10;

    //シーンパネル
    [SerializeField] GameObject PanelRGBProduction;

    [SerializeField] GameObject PanelPixelProduction;
    byte[] PixelListPage = new byte[3 + 1];

    [SerializeField] GameObject PanelCharacterProduction;

    //キャラクターセレクトパネル
    [SerializeField] GameObject PanelSelectCharacter;

    //カラーセレクトパネル
    [SerializeField] GameObject PanelSelectColor;

    [SerializeField] GameObject PanelSelectColorMethod;

    [SerializeField] GameObject PanelSelectColorMethodRGBNum;
    [SerializeField] InputField InputFieldSpecificationNumR;
    [SerializeField] InputField InputFieldSpecificationNumG;
    [SerializeField] InputField InputFieldSpecificationNumB;

    [SerializeField] GameObject PanelSelectColorMethodCharacter;

    [SerializeField] Image ImageSpecificationColorR;
    [SerializeField] Image ImageSpecificationColorG;
    [SerializeField] Image ImageSpecificationColorB;



    //TODO:グローバルなtmp変数はバグの温床だと分かってるけどどうしたものか...
    Button ButtonCharacterTmp = null;
    Button ButtonColorTmp = null;
    Color ColorTmp = new Color(0.0f, 0.0f, 0.0f, 1.0f);



    //テスト　ポスタリゼーション
    Mat Pos(Mat argSrcImg,int argCullNum)
    {
        Mat resultMat = new Mat(argSrcImg.Height, argSrcImg.Width, MatType.CV_8U);
        //Mat LUT = new Mat(256, 1, MatType.CV_8U);
        byte[] LUT = new byte[256];
        for (int x = 0; x < 256; x++)
        {
            //var px = LUT.Get<Vec3b>(0, x);
            //px[0] = (byte)(x / 16);
            //px[1] = (byte)(x / 16);
            //px[2] = (byte)(x / 16);
            //LUT.Set(0, x, px);
            int num = ((x + (argCullNum / 2)) / argCullNum) * argCullNum;
            if (num > 255)
                num = 255;
            LUT[x] = (byte)num;
        }

        Cv2.LUT(argSrcImg, LUT, resultMat);

        //Cv2.ImWrite(Application.streamingAssetsPath + "/Character/TestImageResult2.bmp", resultMat);

        return resultMat;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        {
            //Mat tes = new Mat(5,5,MatType.CV_8U);
            //tes.SaveImage("./TestImage.bmp");
            //Cv2.ImWrite("TestImage.png", tes);

            //Mat PosImg = Cv2.ImRead(Application.streamingAssetsPath + "/Character/TestImage.png");
            //Mat PosImg = Cv2.ImRead(@"TestImage.bmp");
            Texture2D image = ImagegUtility.ReadPng("TestImage.png");
            Mat PosImg = OpenCvSharp.Unity.TextureToMat(image);
            //Mat PosImg = new Mat(5, 5, MatType.CV_8U); 
            //Mat PosImg = Cv2.ImRead(Application.streamingAssetsPath + "/Character/RedSlime8.png");
            //Mat PosImg = new Mat(5, 5, MatType.CV_8U);
            Debug.Log("PosImg.Height = " + PosImg.Height);
            //Cv2.ImWrite(Application.streamingAssetsPath + "/Character/TestImageResult1.png", PosImg);
            PosImg.SaveImage("./TestImage1.bmp");
            //Cv2.ImWrite("TestImageResult1.bmp", PosImg);
            Cv2.ImWrite("./TestImageResult1.bmp", PosImg);
            Mat PosImgRes = new Mat(PosImg.Height, PosImg.Width, MatType.CV_8U);
            PosImgRes = Pos(PosImg, 32);
            Cv2.ImWrite("./TestImageResult2.bmp", PosImgRes);


            {
                Mat testImageMat = new Mat(3, 3, MatType.CV_8U);
                Vec3b pix = testImageMat.At<Vec3b>(0, 0);
                pix[0] = 0; //B
                pix[1] = 0; //G
                pix[2] = 0; //R
                testImageMat.Set<Vec3b>(0, 0, pix);
                pix = testImageMat.At<Vec3b>(1, 0);
                pix[0] = 128; //B
                pix[1] = 0; //G
                pix[2] = 0; //R
                testImageMat.Set<Vec3b>(1, 0, pix);
                pix = testImageMat.At<Vec3b>(2, 0);
                pix[0] = 255; //B
                pix[1] = 0; //G
                pix[2] = 0; //R
                testImageMat.Set<Vec3b>(2, 0, pix);
                pix = testImageMat.At<Vec3b>(0, 1);
                pix[0] = 0; //B
                pix[1] = 0; //G
                pix[2] = 0; //R
                testImageMat.Set<Vec3b>(0, 1, pix);
                pix = testImageMat.At<Vec3b>(1, 1);
                pix[0] = 0; //B
                pix[1] = 128; //G
                pix[2] = 0; //R
                testImageMat.Set<Vec3b>(1, 1, pix);
                pix = testImageMat.At<Vec3b>(2, 1);
                pix[0] = 0; //B
                pix[1] = 255; //G
                pix[2] = 0; //R
                testImageMat.Set<Vec3b>(2, 1, pix);
                pix = testImageMat.At<Vec3b>(0, 2);
                pix[0] = 0; //B
                pix[1] = 0; //G
                pix[2] = 0; //R
                testImageMat.Set<Vec3b>(0, 2, pix);
                pix = testImageMat.At<Vec3b>(1, 2);
                pix[0] = 0; //B
                pix[1] = 0; //G
                pix[2] = 128; //R
                testImageMat.Set<Vec3b>(1, 2, pix);
                pix = testImageMat.At<Vec3b>(2, 2);
                pix[0] = 0; //B
                pix[1] = 0; //G
                pix[2] = 255; //R
                testImageMat.Set<Vec3b>(2, 2, pix);
                //Cv2.ImWrite("./TestImage9.bmp", testImageMat);
                //Debug.Log("testImageMat.Get<Vec3b>(2,2).Item0 = " + testImageMat.Get<Vec3b>(2,2).Item0);
                //Debug.Log("testImageMat.Get<Vec3b>(2,2).Item1 = " + testImageMat.Get<Vec3b>(2,2).Item1);
                //Debug.Log("testImageMat.Get<Vec3b>(2,2).Item2 = " + testImageMat.Get<Vec3b>(2,2).Item2);
                Texture2D testImageTex = OpenCvSharp.Unity.MatToTexture(testImageMat);
                byte[] pngData = testImageTex.EncodeToPNG();
                File.WriteAllBytes("./TestImage9.png", pngData);
                testImageTex.SetPixel(0, 0, new Color(0, 0, 0));
                testImageTex.SetPixel(1, 0, new Color((float)0.5, 0, 0));
                testImageTex.SetPixel(2, 0, new Color((float)1.0, 0, 0));
                testImageTex.SetPixel(0, 1, new Color(0, 0, 0));
                testImageTex.SetPixel(1, 1, new Color(0, (float)0.5, 0));
                testImageTex.SetPixel(2, 1, new Color(0, (float)1.0, 0));
                testImageTex.SetPixel(0, 2, new Color(0, 0, 0));
                testImageTex.SetPixel(1, 2, new Color(0, 0, (float)0.5));
                testImageTex.SetPixel(2, 2, new Color(0, 0, (float)1.0));
                pngData = testImageTex.EncodeToPNG();
                File.WriteAllBytes("./TestImage99.png", pngData);

            }
        }
#endif

        Debug.Log("ControllerProduction Begin");
#if UNITY_ANDROID && !UNITY_EDITOR
        Advertisement.Initialize("3635910");
#endif

        ControllerCharacterSelect = GetComponent<ControllerCharacterSelectClass>();

        ControllerCharacterSelect.Initialize(ref CharactersAll,
                                             ref ColorProductionPixel,
                                             ref CharactersIDHelpProductionR, ref CharactersIDHelpProductionG, ref CharactersIDHelpProductionB,
                                             ref CharactersIDProductionPixel);
        ModelProduction = GetComponent<ModelProduction>();


        MakeFile();

        //画面回転固定
        //縦
        Screen.autorotateToPortrait = true;
        //上下反転
        Screen.autorotateToPortraitUpsideDown = true;
        //左
        Screen.autorotateToLandscapeLeft = false;
        //右
        Screen.autorotateToLandscapeRight = false;

        //キャラ生成
        Debug.Log("キャラ生成");
        for (int i = 0; i < Constants.CHARACTERS_ALL_NUM + 1; i++)
        {
            CharactersAll[i] = new CharacterClass();
        }

        //IDと配列番号を一致させる、0は初期値のままで
        //CharactersAll[1].MakeCharacter(Resources.Load("Character/RedSlime8", typeof(Texture2D)) as Texture2D, 1, "LittleRedSlime");
        //CharactersAll[1].MakeCharacter(Application.dataPath + "/Resources/Character/RedSlime8", 1, "LittleRedSlime");
        //CharactersAll[1].MakeCharacter("Character/RedSlime8", 1, "LittleRedSlime");
        //CharactersAll[1].MakeCharacter(Application.dataPath + "/Resources/" + "Character/RedSlime8" + ".png", 1, "LittleRedSlime");
        CharactersAll[1].MakeCharacter(Application.persistentDataPath + "/Character/RedSlime8" + ".png", 1, "LittleRedSlime");
        CharactersAll[2].MakeCharacter(Application.persistentDataPath + "/Character/GreenSlime8" + ".png", 2, "LittleGreenSlime");
        CharactersAll[3].MakeCharacter(Application.persistentDataPath + "/Character/BlueSlime8" + ".png", 3, "LittleBlueSlime");
        CharactersAll[4].MakeCharacter(Application.persistentDataPath + "/Character/WhiteSlime8" + ".png", 4, "LittleWhiteSlime");
        CharactersAll[5].MakeCharacter(Application.persistentDataPath + "/Character/RBlackCat8" + ".png", 5, "LittleRBlackCat");
        CharactersAll[8].MakeCharacter(Application.persistentDataPath + "/Character/WhiteCat8" + ".png", 8, "LittleWhiteCat");

        CharactersAll[16 + 1].MakeCharacter(Application.persistentDataPath + "/Character/0032_slime_R" + ".png", 16 + 1, "RedSlime");
        CharactersAll[16 + 2].MakeCharacter(Application.persistentDataPath + "/Character/0032_slime_G" + ".png", 16 + 2, "GreenSlime");
        CharactersAll[16 + 3].MakeCharacter(Application.persistentDataPath + "/Character/0032_slime_B" + ".png", 16 + 3, "BlueSlime");
        CharactersAll[16 + 5].MakeCharacter(Application.persistentDataPath + "/Character/0032_rabbit" + ".png", 16 + 5, "WhiteRabbit");
        CharactersAll[16 + 6].MakeCharacter(Application.persistentDataPath + "/Character/0064_slimeking_R" + ".png", 16 + 6, "RedSlimeKing");
        CharactersAll[16 + 7].MakeCharacter(Application.persistentDataPath + "/Character/0064_slimeking_G" + ".png", 16 + 7, "GreenSlimeKing");
        CharactersAll[16 + 8].MakeCharacter(Application.persistentDataPath + "/Character/0064_slimeking_B" + ".png", 16 + 8, "BlueSlimeKing");

        CharactersAll[32].MakeCharacter(Application.persistentDataPath + "/Character/wanwan" + ".png", 32, "wanwan");

        //CharactersAll[1].MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/RedSlime8", typeof(Texture2D)) as Texture2D, 1, "LittleRedSlime");
        //CharactersAll[2].MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/GreenSlime8", typeof(Texture2D)) as Texture2D, 2, "LittleGreenSlime");
        //CharactersAll[3].MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/BlueSlime8", typeof(Texture2D)) as Texture2D, 3, "LittleBlueSlime");
        //CharactersAll[4].MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/WhiteSlime8", typeof(Texture2D)) as Texture2D, 4, "LittleWhiteSlime");
        //CharactersAll[5].MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/RBlackCat8", typeof(Texture2D)) as Texture2D, 5, "LittleRBlackCat");
        //CharactersAll[8].MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/WhiteCat8", typeof(Texture2D)) as Texture2D, 8, "LittleWhiteCat");
        //CharactersAll[32].MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/wanwan", typeof(Texture2D)) as Texture2D, 32, "wanwan");

        //ColorProductionPixelの初期化
        for (int i = 0; i < Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1; i++)
        {
            ColorProductionPixel[i].a = 1.0f;
        }

        //ロード
        SaveClass SC = new SaveClass();
        SC.Load(ref CharactersAll, Constants.CHARACTERS_ALL_NUM + 1, 
            ref CurR, ref CurG, ref CurB,
            ref MaxR, ref MaxG, ref MaxB,
            ref CostMaxRUp, ref CostMaxGUp, ref CostMaxBUp,
            ref IncreaseValueR, ref IncreaseValueG, ref IncreaseValueB,
            ref CostIncreaseValueRUp, ref CostIncreaseValueGUp, ref CostIncreaseValueBUp,
            ref CharactersIDHelpProductionR, ref CharactersIDHelpProductionG, ref CharactersIDHelpProductionB);


        //UIの更新
        UpdateRGBProductionScene();


        Debug.Log("ControllerProduction End");
    }

    // Update is called once per frame
    private float TimeOut = 1;
    private float TimeElapsed = 0;
    void Update()
    {
        //TODO:test
        GameObject testLogText;
        testLogText = GameObject.FindGameObjectWithTag("test");
        testLogText.GetComponentInChildren<Text>().text = "ButtonCharacterTmp = " + ButtonCharacterTmp + "\n" +
                                                          "ButtonColorTmp = " + ButtonColorTmp + "\n" +
                                                          "ColorTmp = " + ColorTmp;



        TimeElapsed += Time.deltaTime;

        if (TimeElapsed >= TimeOut)
        {
            TimeElapsed = 0;

            //Rの生産
            ulong IncreaseValueRHelp = CharactersAll[CharactersIDHelpProductionR[1]].Stats[0].RCreates +
                                       CharactersAll[CharactersIDHelpProductionR[2]].Stats[0].RCreates +
                                       CharactersAll[CharactersIDHelpProductionR[3]].Stats[0].RCreates;
            ModelProduction.Increase(ref CurR, IncreaseValueRHelp, MaxR);
            UpdateRGBProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);

            //Gの生産
            ulong IncreaseValueGHelp = CharactersAll[CharactersIDHelpProductionG[1]].Stats[0].GCreates +
                                       CharactersAll[CharactersIDHelpProductionG[2]].Stats[0].GCreates +
                                       CharactersAll[CharactersIDHelpProductionG[3]].Stats[0].GCreates;
            ModelProduction.Increase(ref CurG, IncreaseValueGHelp, MaxG);
            UpdateRGBProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);

            //Bの生産
            ulong IncreaseValueBHelp = CharactersAll[CharactersIDHelpProductionB[1]].Stats[0].BCreates +
                                       CharactersAll[CharactersIDHelpProductionB[2]].Stats[0].BCreates +
                                       CharactersAll[CharactersIDHelpProductionB[3]].Stats[0].BCreates;
            ModelProduction.Increase(ref CurB, IncreaseValueBHelp, MaxB);
            UpdateRGBProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);

            //ピクセルの生産
            for (int i = 1; i < Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1; i++)
            {
                bool ProductionPixelFlag;
                ProductionPixel(Trigger.Update, i, out ProductionPixelFlag);

                UpdateSliderPixelProduction(i);

                if (ProductionPixelFlag)
                {
                    CreatePixelListPixelProduction();
                }
            }
            UpdateRGBProductionScene();

            //RGB不足フラグの表示
            {
                ulong tmpR = 0;
                ulong tmpG = 0;
                ulong tmpB = 0;
                for (int i = 1; i < Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1; i++)
                {
                    if (CharactersIDProductionPixel[i] != 0)
                    {
                        tmpR += CalcProductionPixelRGB(UserProductionPixelNum, (uint)(ColorProductionPixel[i].r * 255), ProgressProductionPixel[i, 1], CharactersAll[CharactersIDProductionPixel[i]].GetCreatePixels((ushort)(ColorProductionPixel[i].r * 255), (ushort)(ColorProductionPixel[i].g * 255), (ushort)(ColorProductionPixel[i].b * 255)));
                        tmpG += CalcProductionPixelRGB(UserProductionPixelNum, (uint)(ColorProductionPixel[i].g * 255), ProgressProductionPixel[i, 2], CharactersAll[CharactersIDProductionPixel[i]].GetCreatePixels((ushort)(ColorProductionPixel[i].r * 255), (ushort)(ColorProductionPixel[i].g * 255), (ushort)(ColorProductionPixel[i].b * 255)));
                        tmpB += CalcProductionPixelRGB(UserProductionPixelNum, (uint)(ColorProductionPixel[i].b * 255), ProgressProductionPixel[i, 3], CharactersAll[CharactersIDProductionPixel[i]].GetCreatePixels((ushort)(ColorProductionPixel[i].r * 255), (ushort)(ColorProductionPixel[i].g * 255), (ushort)(ColorProductionPixel[i].b * 255)));
                    }
                }

                //Debug.Log("temR = " + tmpR + "\n"
                //        + "(uint)(ColorProductionPixel[1].r * 255) = " + (uint)(ColorProductionPixel[1].r * 255) + "\n"
                //        + "ProgressProductionPixel[1, 1] = " + ProgressProductionPixel[1, 1]);
                //Debug.Log("temG = " + tmpG);
                //Debug.Log("temB = " + tmpB);

                if (CurR >= tmpR)
                    ImageAttentionR.enabled = false;
                else
                    ImageAttentionR.enabled = true;

                if (CurG >= tmpG)
                    ImageAttentionG.enabled = false;
                else
                    ImageAttentionG.enabled = true;

                if (CurB >= tmpB)
                    ImageAttentionB.enabled = false;
                else
                    ImageAttentionB.enabled = true;
            }

        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //セーブ機能

    //セーブボタンが押されたら
    public void PushButtonSave()
    {
        GameObject.Find("ButtonSave").GetComponent<Button>().GetComponentInChildren<Text>().text = Application.persistentDataPath + "/ICS.csv";

        SaveClass SC = new SaveClass();
        SC.Save(CharactersAll, Constants.CHARACTERS_ALL_NUM + 1,
            CurR, CurG, CurB,
            MaxR, MaxG, MaxB,
            CostMaxRUp, CostMaxGUp, CostMaxBUp,
            IncreaseValueR, IncreaseValueG, IncreaseValueB,
            CostIncreaseValueRUp, CostIncreaseValueGUp, CostIncreaseValueBUp,
            CharactersIDHelpProductionR, CharactersIDHelpProductionG, CharactersIDHelpProductionB);
    }

    //デリートセーブボタンが押されたら
    public void PushButtonDeleteSave()
    {
        File.Delete(Application.persistentDataPath + "/ICS.csv");
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //RGB生産

    // ButtonRが押されたら
    public void PushButtonR()
    {
        Debug.Log("ButtonRが押された");
        ModelProduction.Increase(ref CurR, IncreaseValueR, MaxR);
        UpdateRGBProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
    }
    // ButtonGが押されたら
    public void PushButtonG()
    {
        Debug.Log("ButtonGが押された");
        ModelProduction.Increase(ref CurG, IncreaseValueG, MaxG);
        UpdateRGBProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
    }
    // ButtonBが押されたら
    public void PushButtonB()
    {
        Debug.Log("ButtonBが押された");
        ModelProduction.Increase(ref CurB, IncreaseValueB, MaxB);
        UpdateRGBProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }

    //ButtonIncreaseValueRUpが押されたら
    public void PushButtonIncreaseValueRUp()
    {
        Debug.Log("ButtonIncreaseValueRUpが押された");
        ModelProduction.UpgradeValue(ref CurR, ref CostIncreaseValueRUp, ref IncreaseValueR, ModelProduction.TypeUpgrade.INCREASE);
        UpdateRGBProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
    }
    //ButtonIncreaseValueGUpが押されたら
    public void PushButtonIncreaseValueGUp()
    {
        Debug.Log("ButtonIncreaseValueGUpが押された");
        ModelProduction.UpgradeValue(ref CurG, ref CostIncreaseValueGUp, ref IncreaseValueG, ModelProduction.TypeUpgrade.INCREASE);
        UpdateRGBProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
    }
    //ButtonIncreaseValueBUpが押されたら
    public void PushButtonIncreaseValueBUp()
    {
        Debug.Log("ButtonIncreaseValueBUpが押された");
        ModelProduction.UpgradeValue(ref CurB, ref CostIncreaseValueBUp, ref IncreaseValueB, ModelProduction.TypeUpgrade.INCREASE);
        UpdateRGBProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }

    //ButtonMaxRUpが押されたら
    public void PushButtonMaxRUp()
    {
        Debug.Log("ButtonMaxRUpが押された");
        ModelProduction.UpgradeValue(ref CurR, ref CostMaxRUp, ref MaxR, ModelProduction.TypeUpgrade.MAX);
        UpdateRGBProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
    }
    //ButtonMaxGUpが押されたら
    public void PushButtonMaxGUp()
    {
        Debug.Log("ButtonMaxRUpが押された");
        ModelProduction.UpgradeValue(ref CurG, ref CostMaxGUp, ref MaxG, ModelProduction.TypeUpgrade.MAX);
        UpdateRGBProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
    }
    //ButtonMaxBUpが押されたら
    public void PushButtonMaxBUp()
    {
        Debug.Log("ButtonMaxRUpが押された");
        ModelProduction.UpgradeValue(ref CurB, ref CostMaxBUp, ref MaxB, ModelProduction.TypeUpgrade.MAX);
        UpdateRGBProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //シーンセレクト

    //RGB生産シーンボタンが押されたら
    public void PushButtonSelectSceneRGBProduction()
    {
        //PanelRGBProductionの表示
        ShowPanel(PanelRGBProduction);
        //UIの更新
        UpdateRGBProductionScene();

        //他のシーンを非表示
        //PanelPixelProductionの非表示
        NotShowPanel(PanelPixelProduction);
        ClearPixelListPixelProduction();
        //PanelCharacterProductionの非表示
        NotShowPanel(PanelCharacterProduction);
    }

    //ピクセル生産シーンボタンが押されたら
    public void PushButtonSelectScenePixelProduction()
    {
        //PanelPixelProductionの表示
        ShowPanel(PanelPixelProduction);
        //UIの更新
        UpdatePixelProductionScene();

        //他のシーンを非表示
        //PanelRGBProductionの非表示
        NotShowPanel(PanelRGBProduction);
        //PanelCharacterProductionの非表示
        NotShowPanel(PanelCharacterProduction);
    }

    //キャラクター生産シーンボタンが押されたら
    public void PushButtonSelectSceneCharacterProduction()
    {
        //PanelCharacterProductionの表示
        ShowPanel(PanelCharacterProduction);

        //他のシーンを非表示
        //PanelRGBProductionの非表示
        NotShowPanel(PanelRGBProduction);
        //PanelPixelProductionの非表示
        NotShowPanel(PanelPixelProduction);
        ClearPixelListPixelProduction();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //キャラクターセレクト
    //キャラクター選択ボタンが押されたら
    public void PushButtonSelectCharacter(string argButtonName)
    {
        //どのボタンで呼び出されたか保存
        ButtonCharacterTmp = GameObject.Find(argButtonName).GetComponent<Button>();

        //PanelSelectCharacterの表示
        ShowPanel(PanelSelectCharacter);
        ControllerCharacterSelect.ShowPanelSelectCharacter(ButtonCharacterTmp);

    }

    //キャラクターセレクトの戻るボタンが押されたら
    public void PushButtonBackSelectCharacter()
    {
        //どのボタンで呼び出されたか削除
        ButtonCharacterTmp = null;

        //PanelSelectCharacterの非表示
        ControllerCharacterSelect.NotShowPanelSelectCharacter();
        NotShowPanel(PanelSelectCharacter);
    }

    ////キャラクターセレクトのキャラクターボタンが押されたら
    //public void PushButtonSelectCharacterCharacter(uint argCharacterID)
    //{
    //    ControllerCharacterSelect.SelectCharacterCharacter(argCharacterID);
    //}

    //キャラクターセレクトの決定ボタンが押されたら
    public void PushButtonConfirmSelectCharacter()
    {
        ControllerCharacterSelect.ConfirmSelectCharacter(ButtonCharacterTmp);

        if (ButtonCharacterTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            //進捗の初期化
            int ProductionPixelIndex = int.Parse(ButtonCharacterTmp.name.Substring(ButtonCharacterTmp.name.Length - 2, 2));
            InitializeProgressProductionPixel(ProductionPixelIndex);

            //TODO:view更新、RGBのテキストだけでいい
            UpdatePixelProductionScene();
        }

        //どのボタンで呼び出されたか削除
        ButtonCharacterTmp = null;

        ControllerCharacterSelect.NotShowPanelSelectCharacter();
        NotShowPanel(PanelSelectCharacter);
    }

    //キャラクターセレクトの外すボタンが押されたら
    public void PushButtonRemoveCharacterSelect()
    {
        ControllerCharacterSelect.RemoveCharacterSelect(ButtonCharacterTmp);

        if (ButtonCharacterTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            int ProductionPixelIndex = int.Parse(ButtonCharacterTmp.name.Substring(ButtonCharacterTmp.name.Length - 2, 2));

            //進捗の初期化
            InitializeProgressProductionPixel(ProductionPixelIndex);

            //TODO:view更新、RGBのテキストと、スライダーだけでいい
            UpdatePixelProductionScene();
        }

        //どのボタンで呼び出されたか削除
        ButtonCharacterTmp = null;

        ControllerCharacterSelect.NotShowPanelSelectCharacter();
        NotShowPanel(PanelSelectCharacter);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //カラーセレクト
    //ページDownかUpボタンが押されたら
    public void PushButtonPixelListPageDownOrUp(string argButtonName)
    {
        if (argButtonName.Substring(argButtonName.Length - 1, 1) == "R")
        {
            if (argButtonName.Substring(argButtonName.Length - 5, 4) == "Down")
            {
                if (PixelListPage[1] > 0)
                    PixelListPage[1]--;
            }
            else
            if (argButtonName.Substring(argButtonName.Length - 3, 2) == "Up")
                if (PixelListPage[1] < 5)
                    PixelListPage[1]++;
        }
        else
        if (argButtonName.Substring(argButtonName.Length - 1, 1) == "G")
        {
            if (argButtonName.Substring(argButtonName.Length - 5, 4) == "Down")
            {
                if (PixelListPage[2] > 0)
                    PixelListPage[2]--;
            }
            else
            if (argButtonName.Substring(argButtonName.Length - 3, 2) == "Up")
                if (PixelListPage[2] < 5)
                    PixelListPage[2]++;
        }
        else
        if (argButtonName.Substring(argButtonName.Length - 1, 1) == "B")
        {
            if (argButtonName.Substring(argButtonName.Length - 5, 4) == "Down")
            {
                if (PixelListPage[3] > 0)
                    PixelListPage[3]--;
            }
            else
            if (argButtonName.Substring(argButtonName.Length - 3, 2) == "Up")
                if (PixelListPage[3] < 16)
                    PixelListPage[3]++;
        }

        CreatePixelListPixelProduction();
    }
    //ページのスライダーの入力があったら
    public void ValueChangeSliderPixelListPage(string argSliderName)
    {
        Slider Slider = GameObject.Find(argSliderName).GetComponent<Slider>();
        string strRGB = argSliderName.Substring(argSliderName.Length - 1, 1);

        if (strRGB.Equals("R"))
        {
            if (Slider.value >= 0 && Slider.value <= 5)
                PixelListPage[1] = (byte)Slider.value;
        }
        else
        if (strRGB.Equals("G"))
        {
            if (Slider.value >= 0 && Slider.value <= 5)
                PixelListPage[2] = (byte)Slider.value;
        }
        else
        if (strRGB.Equals("B"))
        {
            if (Slider.value >= 0 && Slider.value <= 16)
                PixelListPage[3] = (byte)Slider.value;
        }

        {
            // 計測開始
            System.Diagnostics.Stopwatch swSlider = new System.Diagnostics.Stopwatch();
            swSlider.Start();
            CreatePixelListPixelProduction();
            swSlider.Stop();
            Debug.Log("swSlider : " + swSlider.Elapsed);
        }
    }


    //色選択ボタンが押されたら
    public void PushButtonSelectColor(string argButtonName)
    {
        //どのボタンで呼び出されたか保存
        ButtonColorTmp = GameObject.Find(argButtonName).GetComponent<Button>();

        ShowPanelSelectColorMethod();
    }
    //カラーセレクトの色指定方法の戻るボタンが押されたら
    public void PushButtonBackSelectColorMethod()
    {
        //どのボタンで呼び出されたか削除
        ButtonColorTmp = null;

        NotShowPanelSelectColorMethod();
    }

    //カラーセレクトの色指定方法のRGB値で指定ボタンが押されたら
    public void PushButtonSelectColorMethodRGBNum()
    {
        ShowPanel(PanelSelectColorMethodRGBNum);

        //パネルの初期化
        UpdateSlectColorMethodRGBNum("R");
        UpdateSlectColorMethodRGBNum("G");
        UpdateSlectColorMethodRGBNum("B");
    }

    //カラーセレクトの色指定方法パネルの表示
    public void ShowPanelSelectColorMethod()
    {
        ShowPanel(PanelSelectColor);
        ShowPanel(PanelSelectColorMethod);
    }
    //カラーセレクトの色指定方法パネルの非表示
    public void NotShowPanelSelectColorMethod()
    {
        NotShowPanel(PanelSelectColorMethod);
        NotShowPanel(PanelSelectColor);
    }

    //カラーセレクトの色指定方法のRGB値で指定で戻るボタンが押されたら
    public void PushButtonBackSelectColorMethodRGBNum()
    {
        NotShowPanel(PanelSelectColorMethodRGBNum);
        ColorTmp.r = 0.0f;
        ColorTmp.g = 0.0f;
        ColorTmp.b = 0.0f;
    }

    //RGB値で色指定のスライダーまたはインプットフィールドの入力があったら
    public void ValueChangeSliderOrInputFieldSpecificationRGBNum(string argName)
    {
        if(argName.StartsWith("SliderSpecificationNum") || argName.StartsWith("InputFieldSpecificationNum"))
        {
            string strRGB = argName.Substring(argName.Length - 1, 1);

            if (strRGB.Equals("R"))
            {
                if (argName.StartsWith("SliderSpecificationNum"))
                {
                    Slider SliderSpecificationNumR = GameObject.Find("SliderSpecificationNumR").GetComponent<Slider>();
                    ColorTmp.r = (float)(SliderSpecificationNumR.value / 255.0);
                }
                else
                if (argName.StartsWith("InputFieldSpecificationNum"))
                {
                    int num;
                    if (int.TryParse(InputFieldSpecificationNumR.text, out num))
                    {
                        if(num > 255)
                        {
                            num = 255;
                        }
                        ColorTmp.r = num / 255.0f;
                    }
                    else
                    if (InputFieldSpecificationNumR.text.Equals(""))
                    {
                        ColorTmp.r = 0.0f;
                    }
                }

                UpdateSlectColorMethodRGBNum("R");
            }
            else
            if (strRGB.Equals("G"))
            {
                if (argName.StartsWith("SliderSpecificationNum"))
                {
                    Slider SliderSpecificationNumG = GameObject.Find("SliderSpecificationNumG").GetComponent<Slider>();
                    ColorTmp.g = (float)(SliderSpecificationNumG.value / 255.0);
                }
                else
                if (argName.StartsWith("InputFieldSpecificationNum"))
                {
                    int num;
                    if (int.TryParse(InputFieldSpecificationNumG.text, out num))
                    {
                        if (num > 255)
                        {
                            num = 255;
                        }
                        ColorTmp.g = num / 255.0f;
                    }
                    else
                    if (InputFieldSpecificationNumG.text.Equals(""))
                    {
                        ColorTmp.g = 0.0f;
                    }
                }

                UpdateSlectColorMethodRGBNum("G");
            }
            else
            if (strRGB.Equals("B"))
            {
                if (argName.StartsWith("SliderSpecificationNum"))
                {
                    Slider SliderSpecificationNumB = GameObject.Find("SliderSpecificationNumB").GetComponent<Slider>();
                    ColorTmp.b = (float)(SliderSpecificationNumB.value / 255.0);
                }
                else
                if (argName.StartsWith("InputFieldSpecificationNum"))
                {
                    int num;
                    if (int.TryParse(InputFieldSpecificationNumB.text, out num))
                    {
                        if (num > 255)
                        {
                            num = 255;
                        }
                        ColorTmp.b = num / 255.0f;
                    }
                    else
                    if (InputFieldSpecificationNumB.text.Equals(""))
                    {
                        ColorTmp.b = 0.0f;
                    }
                }

                UpdateSlectColorMethodRGBNum("B");
            }
        }
    }
    //RGB値で色指定のDownまたはUpボタンが押されたら
    public void PushButtonSpecificationNumRGBDownOrUP(string argName)
    {
        if (argName.Substring(argName.Length - 5, 1).Equals("R") || argName.Substring(argName.Length - 3, 1).Equals("R"))
        {
            if (argName.Contains("Down") && ColorTmp.r > 0.0f)
            {
                ColorTmp.r -= 1 / 255.0f;
            }
            else
            if (argName.Contains("Up") && ColorTmp.r < 1.0f)
            {
                ColorTmp.r += 1 / 255.0f;
            }

            UpdateSlectColorMethodRGBNum("R");
        }
        else
        if (argName.Substring(argName.Length - 5, 1).Equals("G") || argName.Substring(argName.Length - 3, 1).Equals("G"))
        {
            if (argName.Contains("Down") && ColorTmp.g > 0.0f)
            {
                ColorTmp.g -= 1 / 255.0f;
            }
            else
            if (argName.Contains("Up") && ColorTmp.g < 1.0f)
            {
                ColorTmp.g += 1 / 255.0f;
            }

            UpdateSlectColorMethodRGBNum("G");
        }
        else
        if (argName.Substring(argName.Length - 5, 1).Equals("B") || argName.Substring(argName.Length - 3, 1).Equals("B"))
        {
            if (argName.Contains("Down") && ColorTmp.b > 0.0f)
            {
                ColorTmp.b -= 1 / 255.0f;
            }
            else
            if (argName.Contains("Up") && ColorTmp.b < 1.0f)
            {
                ColorTmp.b += 1 / 255.0f;
            }

            UpdateSlectColorMethodRGBNum("B");
        }
    }
    //RGB値で色指定の決定ボタンが押されたら
    public void PushButtonConfirmSelectColorRGBNum()
    {
        int ProductionPixelIndex = int.Parse(ButtonColorTmp.name.Substring(ButtonColorTmp.name.Length - 2, 2));

        //進捗の初期化
        InitializeProgressProductionPixel(ProductionPixelIndex);

        ColorProductionPixel[ProductionPixelIndex].r = ColorTmp.r;
        ColorProductionPixel[ProductionPixelIndex].g = ColorTmp.g;
        ColorProductionPixel[ProductionPixelIndex].b = ColorTmp.b;

        NotShowPanel(PanelSelectColorMethodRGBNum);
        ColorTmp.r = 0;
        ColorTmp.g = 0;
        ColorTmp.b = 0;

        //どのボタンで呼び出されたか削除
        ButtonColorTmp = null;

        NotShowPanelSelectColorMethod();

        UpdatePixelColorPixelProduction(ProductionPixelIndex);
        UpdateSliderPixelProduction(ProductionPixelIndex);
    }


    //カラーセレクトの色指定方法のキャラクターで指定ボタンが押されたら
    public void PushButtonSelectColorMethodCharacter()
    {
        ShowPanel(PanelSelectColorMethodCharacter);

        ColorTmp.r = 0.0f;
        ColorTmp.g = 0.0f;
        ColorTmp.b = 0.0f;

        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag("SelectColorMethodCharacter");
        foreach (GameObject gameObject in tag1_Objects)
        {
            if (gameObject.name.Equals("ButtonCharacterColor"))
            {
                gameObject.GetComponent<Image>().sprite = null;
                gameObject.GetComponent<Image>().color = new Color(0.75f, 0.75f, 0.75f);
                gameObject.GetComponentInChildren<Text>().text = "+";
                gameObject.GetComponentInChildren<RawImage>().texture = new Texture2D(0, 0);
                gameObject.GetComponentInChildren<RawImage>().color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            }
        }

        ControlImageSelectColor("SelectColorMethodCharacter");
    }

    //カラーセレクトの色指定方法のキャラクターで指定で戻るボタンが押されたら
    public void PushButtonBackSelectColorMethodCharacter()
    {
        NotShowPanel(PanelSelectColorMethodCharacter);
        ColorTmp.r = 0.0f;
        ColorTmp.g = 0.0f;
        ColorTmp.b = 0.0f;

        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag("SelectColorMethodCharacter");
        foreach (GameObject gameObject in tag1_Objects)
        {
            if (gameObject.name.Equals("ButtonCharacterColor"))
            {
                gameObject.GetComponent<Image>().sprite = null;
                gameObject.GetComponent<Image>().color = new Color(0.75f, 0.75f, 0.75f);
                gameObject.GetComponentInChildren<Text>().text = "+";
                gameObject.GetComponentInChildren<RawImage>().texture = new Texture2D(0, 0);
                gameObject.GetComponentInChildren<RawImage>().color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            }
        }

        //どのボタンで呼び出されたか削除
        ButtonCharacterTmp = null;

        ControlImageSelectColor("SelectColorMethodCharacter");
    }
    //カラーセレクトの色指定方法のキャラクターで指定でキャラクターボタンが押されたら
    public void PushButtonSelectColorMethodCharacterCharacter()
    {
        GameObject tmpButtonCharacter = eventSystem.currentSelectedGameObject.gameObject;

        if (tmpButtonCharacter.GetComponent<Image>().sprite == null)
        {
            Debug.Log("tmpButton.GetComponent<Image>().sprite == null");

            ButtonCharacterTmp = tmpButtonCharacter.GetComponent<Button>();
            //PanelSelectCharacterの表示
            ShowPanel(PanelSelectCharacter);
            ControllerCharacterSelect.ShowPanelSelectCharacter(ButtonCharacterTmp); 
        }
        else
        {
            //ボタンの位置取得
            Vector2 ButtonPosLeftUnder = GetButtonPosLeftUnder();
            Debug.Log("ButtonPosLeftUnder = " + ButtonPosLeftUnder);
            //クリック位置のピクセルカラーを取得
            Texture2D buttonImage = tmpButtonCharacter.GetComponent<Image>().sprite.texture;
            Vector2 buttonSize = tmpButtonCharacter.GetComponent<RectTransform>().sizeDelta;
            Vector2 pixelSize = new Vector2(buttonSize.x / buttonImage.width, buttonSize.y / buttonImage.height);
            Vector2 clickPosImage = new Vector2(Input.mousePosition.x - ButtonPosLeftUnder.x, Input.mousePosition.y - ButtonPosLeftUnder.y);
            Color clickColor = buttonImage.GetPixel((int)(clickPosImage.x / pixelSize.x), (int)(clickPosImage.y / pixelSize.y));
            Debug.Log("clickColor = " + clickColor);
            if (clickColor.a > 0.0f)
            {
                ColorTmp.r = clickColor.r;
                ColorTmp.g = clickColor.g;
                ColorTmp.b = clickColor.b;
                ControlImageSelectColor("SelectColorMethodCharacter");


                //クリックされたピクセルカラーではない部分のマスク作成
                tmpButtonCharacter.GetComponentInChildren<RawImage>().color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
                Texture2D Mask = new Texture2D(buttonImage.width, buttonImage.height, TextureFormat.ARGB32, false);
                for (int y = 0; y < buttonImage.height; y++)
                {
                    for (int x = 0; x < buttonImage.width; x++)
                    {
                        if (buttonImage.GetPixel(x, y).Equals(clickColor))
                            Mask.SetPixel(x, y, new Color(0, 0, 0, 0));
                        else
                        {
                            Mask.SetPixel(x, y, new Color(0.0f, 0.0f, 0.0f, 0.75f));
                        }
                    }
                }
                Mask.Apply();
                tmpButtonCharacter.GetComponentInChildren<RawImage>().texture = Mask;
            }
        }
    }
    //押したボタンの左下の座標を取得
    public Vector2 GetButtonPosLeftUnder()
    {
        GameObject tmpButton = eventSystem.currentSelectedGameObject.gameObject;
        //Debug.Log("gameObject.transform.position = " + tmpButton.transform.position);
        //Debug.Log("Input.mousePosition = " + Input.mousePosition);
        //Debug.Log("sizeDelta = " + tmpButton.GetComponent<RectTransform>().sizeDelta);
        Vector2 ButtonPosLeftUnder = new Vector2(tmpButton.transform.position.x - (tmpButton.GetComponent<RectTransform>().sizeDelta.x / 2),
                                                 tmpButton.transform.position.y - (tmpButton.GetComponent<RectTransform>().sizeDelta.y / 2));
        //Debug.Log("ButtonPosLeftUnder = " + ButtonPosLeftUnder);

        return ButtonPosLeftUnder;
    }
    //カラーセレクトの色指定方法のキャラクターで色指定で決定ボタンが押されたら
    public void PushButtonConfirmSelectColorCharacter()
    {
        int ProductionPixelIndex = int.Parse(ButtonColorTmp.name.Substring(ButtonColorTmp.name.Length - 2, 2));

        //進捗の初期化
        InitializeProgressProductionPixel(ProductionPixelIndex);

        ColorProductionPixel[ProductionPixelIndex].r = ColorTmp.r;
        ColorProductionPixel[ProductionPixelIndex].g = ColorTmp.g;
        ColorProductionPixel[ProductionPixelIndex].b = ColorTmp.b;

        NotShowPanel(PanelSelectColorMethodCharacter);
        ColorTmp.r = 0;
        ColorTmp.g = 0;
        ColorTmp.b = 0;
        ControlImageSelectColor("SelectColorMethodCharacter");

        //どのボタンで呼び出されたか削除
        ButtonColorTmp = null;
        ButtonCharacterTmp = null;

        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag("SelectColorMethodCharacter");
        foreach (GameObject gameObject in tag1_Objects)
        {
            if (gameObject.name.Equals("ButtonCharacterColor"))
            {
                gameObject.GetComponent<Image>().sprite = null;
                gameObject.GetComponent<Image>().color = new Color(0.75f, 0.75f, 0.75f);
                gameObject.GetComponentInChildren<Text>().text = "+";
            }
        }

        NotShowPanelSelectColorMethod();

        UpdatePixelColorPixelProduction(ProductionPixelIndex);
        UpdateSliderPixelProduction(ProductionPixelIndex);
    }



    //ピクセル生産でスピードアップボタンが押されたら
    public void PushButtonSpeedUpProductionPixel(string argName)
    {
        int ProductionPixelIndex = int.Parse(argName.Substring(argName.Length - 2, 2));

        bool ProductionPixelFlag;
        ProductionPixel(Trigger.User, ProductionPixelIndex, out ProductionPixelFlag);
        UpdateSliderPixelProduction(ProductionPixelIndex);

        UpdateRGBProductionSliderOneColor(TextR, CurR, MaxR, SliderR, SliderCostIncreaseValueRUp, SliderCostMaxRUp);
        UpdateRGBProductionSliderOneColor(TextG, CurG, MaxG, SliderG, SliderCostIncreaseValueGUp, SliderCostMaxGUp);
        UpdateRGBProductionSliderOneColor(TextB, CurB, MaxB, SliderB, SliderCostIncreaseValueBUp, SliderCostMaxBUp);

        if (ProductionPixelFlag)
        {
            CreatePixelListPixelProduction();
        }
    }


    ////////////////////////////////////
    //Model?

    void MakeFile()
    {
        Debug.Log("MakeFile Begin");

        //キャラクターフォルダ、正規化キャラクターフォルダの生成
        if (!(Directory.Exists(Application.persistentDataPath + "/Character")))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/Character");
        }
        if (!(Directory.Exists(Application.persistentDataPath + "/Character/Nomalization")))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/Character/Nomalization");
        }

#if UNITY_EDITOR
        Debug.Log("UNITY_EDITOR");
        //元のキャラクターのパス取得
        string[] files = System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/Character", "*.png", System.IO.SearchOption.AllDirectories);
#elif UNITY_IPHONE //TODO:iosテスト
        Debug.Log("UNITY_IPHONE");
        string[] files = System.IO.Directory.GetFiles(Application.dataPath + "/Raw" + "/Character", "*.png", System.IO.SearchOption.AllDirectories);
//#else
//        Debug.Log("ELSE");
//        string[] files = System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/Character", "*.png", System.IO.SearchOption.AllDirectories);
#endif

//読み書き可能なフォルダへコピー
#if UNITY_EDITOR || UNITY_IPHONE
        foreach (string file in files)
        {
            if (!(File.Exists(Application.persistentDataPath + "/Character/" + Path.GetFileName(file))))
            {
                Debug.Log("コピー　" + file + "->" + Application.persistentDataPath + "/Character/" + Path.GetFileName(file));
                File.Copy(file, Application.persistentDataPath + "/Character/" + Path.GetFileName(file));
            }
        }
#elif UNITY_ANDROID
        Debug.Log("UNITY_ANDROID");

        Debug.Log("ANDROID + 0.41 + " + Application.streamingAssetsPath + "/Character/RedSlime8.png");
        string path = Application.streamingAssetsPath + "/Character/RedSlime8.png";
        //string path = Application.streamingAssetsPath + "/Character";
        Debug.Log("ANDROID + 0.42 + " + path);
        WWW www = new WWW(path);
        while (!www.isDone)
        {
        }
        Debug.Log("ANDROID + 0.51 + " + Application.persistentDataPath + "/Character/RedSlime8.png");
        string toPath = Application.persistentDataPath + "/Character/RedSlime8.png";
        //string toPath = Application.persistentDataPath + "/Character";
        Debug.Log("ANDROID + 0.52 + " + toPath);
        File.WriteAllBytes(toPath, www.bytes);
        Debug.Log("ANDROID + 0.6 + " + Application.streamingAssetsPath + "/Character/RedSlime8.png" + " -> " + Application.persistentDataPath + "/Character/RedSlime8.png");

        string[] CharacterNames = { "GreenSlime8.png" ,
                                    "BlueSlime8.png" ,
                                    "WhiteSlime8.png" ,
                                    "RBlackCat8.png" ,
                                    "WhiteCat8.png" ,
                                    "wanwan.png",

                                    "0032_slime_R.png",
                                    "0032_slime_G.png",
                                    "0032_slime_B.png",
                                    "0032_rabbit.png",
                                    "0064_slimeking_R.png",
                                    "0064_slimeking_G.png",
                                    "0064_slimeking_B.png"
                                    };
        foreach (string curCharacterName in CharacterNames)
        {
            path = Application.streamingAssetsPath + "/Character/" + curCharacterName;
            www = new WWW(path);
            while (!www.isDone){}
            toPath = Application.persistentDataPath + "/Character/" + curCharacterName;
            File.WriteAllBytes(toPath, www.bytes);
        }

        //string[] files = System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/Character", "*.png", System.IO.SearchOption.AllDirectories);
        //foreach (string file in files)
        //{
        //    WWW www = new WWW(file);
        //    while (!www.isDone)
        //    {
        //    }

        //    if (!(File.Exists(Application.persistentDataPath + "/Character/" + Path.GetFileName(file))))
        //    {
        //        File.WriteAllBytes(toPath, www.bytes);

        //        Debug.Log("コピー　" + file + "->" + Application.persistentDataPath + "/Character/" + Path.GetFileName(file));
        //        File.Copy(file, Application.persistentDataPath + "/Character/" + Path.GetFileName(file));
        //    }
        //}



        ////後で試したい
        ////WWW www = new WWW(Application.streamingAssetsPath + "/Character");
        ////while (!www.isDone) { }
        ////Debug.Log("0.211 + " + www.bytes);
        //////string[] files = System.IO.Directory.GetFiles(www.bytes, "*.png", System.IO.SearchOption.AllDirectories);
        ////File.WriteAllBytes(Application.persistentDataPath + "/Character", www.bytes);

        ////WWW www = new WWW("jar:file://" + Application.dataPath + "!/assets");// + "/Character");
        //Debug.Log("0.2101 + " + Application.streamingAssetsPath + "/Character");
        //WWW www = new WWW(Application.streamingAssetsPath + "/Character");
        ////yield return www;
        //while (!www.isDone) { }
        //string TxtTmp = string.Empty;
        //TextReader TR = new StringReader(www.text);
        //string filesTmp = string.Empty;
        //for (int i = 0; (TxtTmp = TR.ReadLine()) != null; i++)
        //{
        //    Debug.Log("0.211 + " + i + " + " + TxtTmp);
        //    filesTmp = filesTmp + TxtTmp + ' ';
        //}
        //string[] files = filesTmp.Split(' ');
        //Debug.Log("0.212 + " + files);

        ////string[] files = System.IO.Directory.GetFiles("jar:file://" + Application.dataPath + "!/assets" + "/Character", "*.png", System.IO.SearchOption.AllDirectories);
#endif

        Debug.Log("MakeFile End");
    }




    //ピクセル生産
    bool ProductionPixel(Trigger argTrigger, int argIndex, out bool ProductionPixelFlag)
    {
        ProductionPixelFlag = false;

        if (CharactersIDProductionPixel[argIndex] != 0)
        {
            ushort Progress;
            if (argTrigger == Trigger.Update)
            {
                Progress = CharactersAll[CharactersIDProductionPixel[argIndex]].Stats[0].SPD;
            }
            else
            if (argTrigger == Trigger.User)
            {
                Progress = UserProductionPixelNum;
            }
            else
            {
                Debug.Log("NG !!!!!!!!!!!!!!!!!!!!!!!");

                return false;
            }

            if ((int)(ColorProductionPixel[argIndex].r * 255) == 0 && ProgressProductionPixel[argIndex, 1] == 0)
            {
                ProgressProductionPixel[argIndex, 1] += 1;
            }
            else
            if (ProgressProductionPixel[argIndex, 1] < (int)(ColorProductionPixel[argIndex].r * 255))
            {
                ulong tmpR = CalcProductionPixelRGB(Progress, (uint)(ColorProductionPixel[argIndex].r * 255), ProgressProductionPixel[argIndex, 1], CharactersAll[CharactersIDProductionPixel[argIndex]].GetCreatePixels((ushort)(ColorProductionPixel[argIndex].r * 255), (ushort)(ColorProductionPixel[argIndex].g * 255), (ushort)(ColorProductionPixel[argIndex].b * 255)));
                if (CurR >= tmpR)
                {
                    CurR -= tmpR;
                    ProgressProductionPixel[argIndex, 1] += Progress;
                    if (ProgressProductionPixel[argIndex, 1] > (ushort)(ColorProductionPixel[argIndex].r * 255))
                        ProgressProductionPixel[argIndex, 1] = (ushort)(ColorProductionPixel[argIndex].r * 255);
                }
            }
            else
            if ((int)(ColorProductionPixel[argIndex].g * 255) == 0 && ProgressProductionPixel[argIndex, 2] == 0)
            {
                ProgressProductionPixel[argIndex, 2] += 1;
            }
            else
            if (ProgressProductionPixel[argIndex, 2] < (int)(ColorProductionPixel[argIndex].g * 255))
            {
                ulong tmpG = CalcProductionPixelRGB(Progress, (uint)(ColorProductionPixel[argIndex].g * 255), ProgressProductionPixel[argIndex, 2], CharactersAll[CharactersIDProductionPixel[argIndex]].GetCreatePixels((ushort)(ColorProductionPixel[argIndex].r * 255), (ushort)(ColorProductionPixel[argIndex].g * 255), (ushort)(ColorProductionPixel[argIndex].b * 255)));
                if (CurG >= tmpG)
                {
                    CurG -= tmpG;
                    ProgressProductionPixel[argIndex, 2] += Progress;
                    if (ProgressProductionPixel[argIndex, 2] > (ushort)(ColorProductionPixel[argIndex].g * 255))
                        ProgressProductionPixel[argIndex, 2] = (ushort)(ColorProductionPixel[argIndex].g * 255);
                }
            }
            else
            if ((int)(ColorProductionPixel[argIndex].b * 255) == 0 && ProgressProductionPixel[argIndex, 3] == 0)
            {
                ProgressProductionPixel[argIndex, 3] += 1;
            }
            else
            if (ProgressProductionPixel[argIndex, 3] < (int)(ColorProductionPixel[argIndex].b * 255))
            {
                ulong tmpB = CalcProductionPixelRGB(Progress, (uint)(ColorProductionPixel[argIndex].b * 255), ProgressProductionPixel[argIndex, 3], CharactersAll[CharactersIDProductionPixel[argIndex]].GetCreatePixels((ushort)(ColorProductionPixel[argIndex].r * 255), (ushort)(ColorProductionPixel[argIndex].g * 255), (ushort)(ColorProductionPixel[argIndex].b * 255)));
                if (CurB >= tmpB)
                {
                    CurB -= tmpB;
                    ProgressProductionPixel[argIndex, 3] += Progress;
                    if (ProgressProductionPixel[argIndex, 3] > (ushort)(ColorProductionPixel[argIndex].b * 255))
                        ProgressProductionPixel[argIndex, 3] = (ushort)(ColorProductionPixel[argIndex].b * 255);
                }
            }

            //ピクセル生産の進捗が満たされたら、進捗を初期化し、ピクセル生産
            if (ProgressProductionPixel[argIndex, 3] >= (int)(ColorProductionPixel[argIndex].b * 255) && ProgressProductionPixel[argIndex, 3] != 0)
            {
                ProgressProductionPixel[argIndex, 1] = 0;
                ProgressProductionPixel[argIndex, 2] = 0;
                ProgressProductionPixel[argIndex, 3] = 0;

                CurPixels[(int)(ColorProductionPixel[argIndex].r * 255), (int)(ColorProductionPixel[argIndex].g * 255), (int)(ColorProductionPixel[argIndex].b * 255)]
                    += CharactersAll[CharactersIDProductionPixel[argIndex]].GetCreatePixels((ushort)(ColorProductionPixel[argIndex].r * 255), (ushort)(ColorProductionPixel[argIndex].g * 255), (ushort)(ColorProductionPixel[argIndex].b * 255));

                ProductionPixelFlag = true;
            }

        }

        return true;
    }
    //ピクセル生産のためのRGB消費値の算出
    ulong CalcProductionPixelRGB(ushort argAddProgress, uint argColorProductionPixelRGB, ushort argCurProgress, uint argCreatePixels)
    {
        short Progress = (short)argAddProgress;

        //残りの進捗が少なかったら
        if (Progress > (short)(argColorProductionPixelRGB - argCurProgress))
        {
            Progress = (short)(argColorProductionPixelRGB - argCurProgress);
        }
        if (Progress < 0)
            Progress = 0;

        return (ulong)(Progress * argCreatePixels);
    }
    //ピクセル生産の進捗の初期化(進捗が途中のときに消費されてしまったRGBを戻す) //TODO:この関数が呼ばれるより前にキャラクターとカラーが先に変更されていないか確認
    bool InitializeProgressProductionPixel(int argIndex)
    {
        CurR += (uint)(ColorProductionPixel[argIndex].r * 255)
            * ProgressProductionPixel[argIndex, 1]
            * CharactersAll[CharactersIDProductionPixel[argIndex]].GetCreatePixels((ushort)(ColorProductionPixel[argIndex].r * 255), (ushort)(ColorProductionPixel[argIndex].g * 255), (ushort)(ColorProductionPixel[argIndex].b * 255));

        CurG += (uint)(ColorProductionPixel[argIndex].g * 255)
            * ProgressProductionPixel[argIndex, 2]
            * CharactersAll[CharactersIDProductionPixel[argIndex]].GetCreatePixels((ushort)(ColorProductionPixel[argIndex].r * 255), (ushort)(ColorProductionPixel[argIndex].g * 255), (ushort)(ColorProductionPixel[argIndex].b * 255));

        CurB += (uint)(ColorProductionPixel[argIndex].b * 255)
            * ProgressProductionPixel[argIndex, 3]
            * CharactersAll[CharactersIDProductionPixel[argIndex]].GetCreatePixels((ushort)(ColorProductionPixel[argIndex].r * 255), (ushort)(ColorProductionPixel[argIndex].g * 255), (ushort)(ColorProductionPixel[argIndex].b * 255));

        ProgressProductionPixel[argIndex, 1] = 0;
        ProgressProductionPixel[argIndex, 2] = 0;
        ProgressProductionPixel[argIndex, 3] = 0;

        return true;
    }



    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //View

    //パネルの表示
    public void ShowPanel(GameObject argPanel)
    {
        CanvasGroup PanelCanvasGroup = argPanel.GetComponent<CanvasGroup>();
        PanelCanvasGroup.alpha = 1;
        PanelCanvasGroup.interactable = true;
        PanelCanvasGroup.blocksRaycasts = true;
    }
    //パネルの非表示
    public void NotShowPanel(GameObject argPanel)
    {
        CanvasGroup PanelCanvasGroup = argPanel.GetComponent<CanvasGroup>();
        PanelCanvasGroup.alpha = 0;
        PanelCanvasGroup.interactable = false;
        PanelCanvasGroup.blocksRaycasts = false;
    }


    //グラフィックをデータと同期させる
    //RGB生産
    public void UpdateRGBProductionScene()
    {
        UpdateRGBProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
        UpdateRGBProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
        UpdateRGBProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);
        UpdateRGBProductionHelpCharacter();
    }
    public void UpdateRGBProductionOneColor(ulong ColorValue, ulong MaxValue, Text TextColorValue, Slider SliderColorValue, ulong IncreaseValue, ulong CostIncreaseUp, Text TextIncreaseValueLeft, Text TextIncreaseValueRight, Text TextCostIncreaseValueUp, Slider SliderCostIncrease, Button ButtonIncreaseValueUp, ulong CostMaxValueUp, Text TextCostMaxValueUp, Slider SliderCostMaxValueUp, Button ButtonMaxValueUp)
    {
        UpdateRGBProductionSliderOneColor(TextColorValue, ColorValue, MaxValue, SliderColorValue, SliderCostIncrease, SliderCostMaxValueUp);

        TextIncreaseValueLeft.text = string.Format("+{0}", IncreaseValue);
        TextIncreaseValueRight.text = string.Format("+{0}", IncreaseValue);
        TextCostIncreaseValueUp.text = string.Format("コスト : {0}", CostIncreaseUp);
        SliderCostIncrease.maxValue = CostIncreaseUp;
        SliderCostIncrease.minValue = 0;
        SliderCostIncrease.value = ColorValue;
        if (ColorValue < CostIncreaseUp)
        {
            ButtonIncreaseValueUp.interactable = false;
        }
        else
        {
            ButtonIncreaseValueUp.interactable = true;
        }

        TextCostMaxValueUp.text = string.Format("コスト : {0}", CostMaxValueUp);
        SliderCostMaxValueUp.maxValue = CostMaxValueUp;
        SliderCostMaxValueUp.minValue = 0;
        SliderCostMaxValueUp.value = ColorValue;
        if (ColorValue < CostMaxValueUp)
        {
            ButtonMaxValueUp.interactable = false;
        }
        else
        {
            ButtonMaxValueUp.interactable = true;
        }
    }
    public void UpdateRGBProductionSliderOneColor(Text TextColorValue, ulong ColorValue, ulong MaxValue, Slider SliderColorValue, Slider SliderCostIncrease, Slider SliderCostMaxValueUp)
    {
        TextColorValue.text = string.Format("{0} / {1}", ColorValue, MaxValue);
        SliderColorValue.maxValue = MaxValue;
        SliderColorValue.minValue = 0;
        SliderColorValue.value = ColorValue;

        SliderCostIncrease.value = ColorValue;
        SliderCostMaxValueUp.value = ColorValue;
    }

    public void UpdateRGBProductionHelpCharacter()//TODO:リファクタリング
    {
        if(CharactersIDHelpProductionR[1] != 0)
        {
            Button B = GameObject.Find("ButtonRProductionHelpCharacter1").GetComponent<Button>();
            B.image.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionR[1]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionR[1]].Size, CharactersAll[CharactersIDHelpProductionR[1]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionR[2] != 0)
        {
            Button B = GameObject.Find("ButtonRProductionHelpCharacter2").GetComponent<Button>();
            B.image.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionR[2]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionR[2]].Size, CharactersAll[CharactersIDHelpProductionR[2]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionR[3] != 0)
        {
            Button B = GameObject.Find("ButtonRProductionHelpCharacter3").GetComponent<Button>();
            B.image.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionR[3]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionR[3]].Size, CharactersAll[CharactersIDHelpProductionR[3]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }

        if (CharactersIDHelpProductionG[1] != 0)
        {
            Button B = GameObject.Find("ButtonGProductionHelpCharacter1").GetComponent<Button>();
            B.image.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionG[1]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionG[1]].Size, CharactersAll[CharactersIDHelpProductionG[1]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionG[2] != 0)
        {
            Button B = GameObject.Find("ButtonGProductionHelpCharacter2").GetComponent<Button>();
            B.image.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionG[2]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionG[2]].Size, CharactersAll[CharactersIDHelpProductionG[2]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionG[3] != 0)
        {
            Button B = GameObject.Find("ButtonGProductionHelpCharacter3").GetComponent<Button>();
            B.image.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionG[3]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionG[3]].Size, CharactersAll[CharactersIDHelpProductionG[3]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }

        if (CharactersIDHelpProductionB[1] != 0)
        {
            Button B = GameObject.Find("ButtonBProductionHelpCharacter1").GetComponent<Button>();
            B.image.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionB[1]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionB[1]].Size, CharactersAll[CharactersIDHelpProductionB[1]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionB[2] != 0)
        {
            Button B = GameObject.Find("ButtonBProductionHelpCharacter2").GetComponent<Button>();
            B.image.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionB[2]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionB[2]].Size, CharactersAll[CharactersIDHelpProductionB[2]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionB[3] != 0)
        {
            Button B = GameObject.Find("ButtonBProductionHelpCharacter3").GetComponent<Button>();
            B.image.sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDHelpProductionB[3]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionB[3]].Size, CharactersAll[CharactersIDHelpProductionB[3]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
    }

    //ピクセル生産
    public void UpdatePixelProductionScene()
    {
        CreatePixelListPixelProduction();

        for (int i = 1; i < Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1; i++)
        {
            UpdateCharacterPixelProduction(i);

            UpdatePixelColorPixelProduction(i);

            UpdateSliderPixelProduction(i);
        }
    }
    public void CreatePixelListPixelProduction()
    {
        //ページ指定部分
        ShowPagePixelListPixelProduction(GameObject.Find("PrefabPixelListPageR"));
        ShowPagePixelListPixelProduction(GameObject.Find("PrefabPixelListPageG"));
        ShowPagePixelListPixelProduction(GameObject.Find("PrefabPixelListPageB"));

        GameObject GameObjectContentPixelList = GameObject.Find("ContentPixelList");

        //PixelListの生成
        GameObject[,] ArrayShowPixelColorAndNum = new GameObject[16, 16];

        // 計測開始
        System.Diagnostics.Stopwatch swI = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch swU = new System.Diagnostics.Stopwatch();

        //int B = PixelListPage[3];
        //for (int G = (PixelListPage[2] * 16); G < (PixelListPage[2] * 16) + 16; G++)
        //{
        //    for (int R = (PixelListPage[1] * 16); R < (PixelListPage[1] * 16) + 16; R++)
        //    {
        //        //プレハブのインスタンス化
        //        swI.Start();
        //        ArrayShowPixelColorAndNum[R % 16, G % 16] = Instantiate((GameObject)Resources.Load("PrefabShowPixelColorAndNum"), GameObjectContentPixelList.transform) as GameObject;
        //        swI.Stop();

        //        swU.Start();
        //        ArrayShowPixelColorAndNum[R % 16, G % 16].GetComponentsInChildren<Image>()[1].color = new Color(R / 255f, G / 255f, B / 255f, 1.0f);
        //        ArrayShowPixelColorAndNum[R % 16, G % 16].GetComponentsInChildren<Text>()[0].text = R.ToString();
        //        ArrayShowPixelColorAndNum[R % 16, G % 16].GetComponentsInChildren<Text>()[2].text = G.ToString();
        //        ArrayShowPixelColorAndNum[R % 16, G % 16].GetComponentsInChildren<Text>()[4].text = B.ToString();
        //        ArrayShowPixelColorAndNum[R % 16, G % 16].GetComponentsInChildren<Text>()[5].text = CurPixels[R, G, B].ToString();
        //        swU.Stop();
        //    }
        //}

        int rowNum = 3;
        int colNum = 3;
        int cullNum = 16;
        int B = PixelListPage[3];

        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag("PixelList");
        foreach (GameObject gameObject in tag1_Objects)
        {
            if (gameObject.name.Equals("ContentPixelList"))
            {
                Debug.Log("((PixelListPage[1] * colNum) + colNum - 1) * cullNum = " + ((PixelListPage[1] * colNum) + colNum - 1) * cullNum);
                if (((PixelListPage[1] * colNum) + colNum - 1) * cullNum < 256)
                    gameObject.GetComponent<GridLayoutGroup>().constraintCount = 3;
                else
                    gameObject.GetComponent<GridLayoutGroup>().constraintCount = 2;
            }
        }

        ClearPixelListPixelProduction();

        for (int G = (PixelListPage[2] * rowNum); G < (PixelListPage[2] * rowNum) + rowNum; G++)
        {
            for (int R = (PixelListPage[1] * colNum); R < (PixelListPage[1] * colNum) + colNum; R++)
            {
                int RColor = R * cullNum;
                int GColor = G * cullNum;
                int BColor = B * cullNum;

                if(cullNum != 1)
                {
                    if (RColor == 256)
                        RColor = 255;
                    if (GColor == 256)
                        GColor = 255;
                    if (BColor == 256)
                        BColor = 255;
                }
                if (RColor > 255 || GColor > 255)
                    continue;


                //プレハブのインスタンス化
                swI.Start();
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum] = Instantiate((GameObject)Resources.Load("PrefabShowPixelColorAndNum"), GameObjectContentPixelList.transform) as GameObject;
                swI.Stop();

                swU.Start();
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum].GetComponentsInChildren<Image>()[1].color = new Color(RColor / 255f, GColor / 255f, BColor / 255f, 1.0f);
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum].GetComponentsInChildren<Text>()[0].text = RColor.ToString();
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum].GetComponentsInChildren<Text>()[2].text = GColor.ToString();
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum].GetComponentsInChildren<Text>()[4].text = BColor.ToString();
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum].GetComponentsInChildren<Text>()[5].text = CurPixels[RColor, GColor, BColor].ToString();
                swU.Stop();
            }
        }

        Debug.Log("swI : " + swI.Elapsed);
        Debug.Log("swU : " + swU.Elapsed);

    }
    public void ShowPagePixelListPixelProduction(GameObject argPrefabPixelListPageRGB)//TODO:ポスカゼーションを考える
    {
        if (argPrefabPixelListPageRGB.name == "PrefabPixelListPageR")
        {
            argPrefabPixelListPageRGB.GetComponentsInChildren<Text>()[2].text = (PixelListPage[1] * 16 * 3).ToString() + " ～ " + ((PixelListPage[1] * 16 * 3) + (16 * 2)).ToString();
            if((PixelListPage[1] * 16 * 3) + (16 * 2) > 255)
                argPrefabPixelListPageRGB.GetComponentsInChildren<Text>()[2].text = (PixelListPage[1] * 16 * 3).ToString() + " ～ " + 255.ToString();

            if (PixelListPage[1] == 0)
            {
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = false;
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = true;
            }
            else
            if (PixelListPage[1] == 5)
            {
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = true;
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = false;
            }
            else
            {
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = true;
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = true;
            }

            argPrefabPixelListPageRGB.GetComponentsInChildren<Slider>()[0].value = PixelListPage[1];
        }
        else
        if (argPrefabPixelListPageRGB.name == "PrefabPixelListPageG")
        {
            argPrefabPixelListPageRGB.GetComponentsInChildren<Text>()[2].text = (PixelListPage[2] * 16 * 3).ToString() + " ～ " + ((PixelListPage[2] * 16 * 3) + (16 * 2)).ToString();
            if((PixelListPage[2] * 16 * 3) + (16 * 2) > 255)
                argPrefabPixelListPageRGB.GetComponentsInChildren<Text>()[2].text = (PixelListPage[2] * 16 * 3).ToString() + " ～ " + 255.ToString();

            if (PixelListPage[2] == 0)
            {
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = false;
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = true;
            }
            else
            if (PixelListPage[2] == 5)
            {
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = true;
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = false;
            }
            else
            {
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = true;
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = true;
            }

            argPrefabPixelListPageRGB.GetComponentsInChildren<Slider>()[0].value = PixelListPage[2];
        }
        else
        if (argPrefabPixelListPageRGB.name == "PrefabPixelListPageB")
        {
            argPrefabPixelListPageRGB.GetComponentsInChildren<Text>()[2].text = (PixelListPage[3] * 16).ToString();
            if (PixelListPage[3] * 16 > 255)
                argPrefabPixelListPageRGB.GetComponentsInChildren<Text>()[2].text = 255.ToString();

            if (PixelListPage[3] == 0)
            {
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = false;
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = true;
            }
            else
            if (PixelListPage[3] == 16)
            {
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = true;
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = false;
            }
            else
            {
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = true;
                argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = true;
            }

            argPrefabPixelListPageRGB.GetComponentsInChildren<Slider>()[0].value = PixelListPage[3];
        }
    }
    public void ClearPixelListPixelProduction()
    {
        GameObject GameObjectContentPixelList = GameObject.Find("ContentPixelList");

        //PixelListの削除
        foreach (Transform child in GameObjectContentPixelList.transform)
        {
            Destroy(child.gameObject);
        }
    }
    public void UpdateCharacterPixelProduction(int argIndex)
    {
        GameObject Content = GameObject.Find("ContentPixelProductionList").transform.Find("PrefabOnePixelProduction" + (argIndex).ToString("00")).gameObject;

        if (CharactersAll[CharactersIDProductionPixel[argIndex]].ImagePath == null)
        {
            Content.GetComponentsInChildren<Button>()[0].image.sprite = null;
        }
        else
        {
            //キャラクター
            Content.GetComponentsInChildren<Button>()[0].image.sprite
                = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[CharactersIDProductionPixel[argIndex]].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDProductionPixel[argIndex]].Size, CharactersAll[CharactersIDProductionPixel[argIndex]].Size), new Vector2(0.5f, 0.5f));
        }
    }
    public void UpdatePixelColorPixelProduction(int argIndex)
    {
        GameObject Content = GameObject.Find("ContentPixelProductionList").transform.Find("PrefabOnePixelProduction" + (argIndex).ToString("00")).gameObject;

        //ピクセルカラー
        Content.GetComponentsInChildren<Button>()[1].image.color
            = new Color(ColorProductionPixel[argIndex].r, ColorProductionPixel[argIndex].g, ColorProductionPixel[argIndex].b);
        Content.GetComponentsInChildren<Button>()[1].image.GetComponentInChildren<Text>().text
            = "#" + ((int)(ColorProductionPixel[argIndex].r * 255)).ToString("X2") + ((int)(ColorProductionPixel[argIndex].g * 255)).ToString("X2") + ((int)(ColorProductionPixel[argIndex].b * 255)).ToString("X2");

        if (((ColorProductionPixel[argIndex].r + ColorProductionPixel[argIndex].g + ColorProductionPixel[argIndex].b) / 3.0f) < 0.5f)
        {
            Content.GetComponentsInChildren<Button>()[1].image.GetComponentInChildren<Text>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        }
        else
        {
            Content.GetComponentsInChildren<Button>()[1].image.GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }

        //RGB値の表示
        if (CharactersIDProductionPixel[argIndex] == 0)
        {
            Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("ImagePixelColorR").gameObject.GetComponent<Image>().GetComponentInChildren<Text>().text
                = (ColorProductionPixel[argIndex].r * 255).ToString();
            Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("ImagePixelColorG").gameObject.GetComponent<Image>().GetComponentInChildren<Text>().text
                = (ColorProductionPixel[argIndex].g * 255).ToString();
            Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("ImagePixelColorB").gameObject.GetComponent<Image>().GetComponentInChildren<Text>().text
                = (ColorProductionPixel[argIndex].b * 255).ToString();
        }
        else
        {
            uint PixelProductionNum = CharactersAll[CharactersIDProductionPixel[argIndex]].GetCreatePixels((ushort)(ColorProductionPixel[argIndex].r * 255), (ushort)(ColorProductionPixel[argIndex].g * 255), (ushort)(ColorProductionPixel[argIndex].b * 255));
            Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("ImagePixelColorR").gameObject.GetComponent<Image>().GetComponentInChildren<Text>().text
                = (ColorProductionPixel[argIndex].r * 255).ToString() + "\n" +
                  "× " + PixelProductionNum.ToString() + "\n" +
                  "= " + (ColorProductionPixel[argIndex].r * 255 * PixelProductionNum).ToString();
            Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("ImagePixelColorG").gameObject.GetComponent<Image>().GetComponentInChildren<Text>().text
                = (ColorProductionPixel[argIndex].g * 255).ToString() + "\n" +
                  "× " + PixelProductionNum.ToString() + "\n" +
                  "= " + (ColorProductionPixel[argIndex].g * 255 * PixelProductionNum).ToString();
            Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("ImagePixelColorB").gameObject.GetComponent<Image>().GetComponentInChildren<Text>().text
                = (ColorProductionPixel[argIndex].b * 255).ToString() + "\n" +
                  "× " + PixelProductionNum.ToString() + "\n" +
                  "= " + (ColorProductionPixel[argIndex].b * 255 * PixelProductionNum).ToString();
        }
    }
    public void UpdateSliderPixelProduction(int argIndex)
    {
        GameObject Content = GameObject.Find("ContentPixelProductionList").transform.Find("PrefabOnePixelProduction" + (argIndex).ToString("00")).gameObject;

        //進捗スライダー
        Slider SliderPixelColorR = Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("SliderPixelColorR").gameObject.GetComponent<Slider>();
        SliderPixelColorR.maxValue = (int)(ColorProductionPixel[argIndex].r * 255);
        if (SliderPixelColorR.maxValue == 0)
        {
            SliderPixelColorR.maxValue = 1;
        }
        SliderPixelColorR.value = ProgressProductionPixel[argIndex, 1];

        Slider SliderPixelColorG = Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("SliderPixelColorG").gameObject.GetComponent<Slider>();
        SliderPixelColorG.maxValue = (int)(ColorProductionPixel[argIndex].g * 255);
        if (SliderPixelColorG.maxValue == 0)
        {
            SliderPixelColorG.maxValue = 1;
        }
        SliderPixelColorG.value = ProgressProductionPixel[argIndex, 2];

        Slider SliderPixelColorB = Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("SliderPixelColorB").gameObject.GetComponent<Slider>();
        SliderPixelColorB.maxValue = (int)(ColorProductionPixel[argIndex].b * 255);
        if (SliderPixelColorB.maxValue == 0)
        {
            SliderPixelColorB.maxValue = 1;
        }
        SliderPixelColorB.value = ProgressProductionPixel[argIndex, 3];
    }


    public void UpdateSlectColorMethodRGBNum(string argRGB)
    {
        if (argRGB.Equals("R"))
        {
            ImageSpecificationColorR.color = new Color(ColorTmp.r, 0.0f, 0.0f, 1.0f);

            Slider SliderSpecificationNumR = GameObject.Find("SliderSpecificationNumR").GetComponent<Slider>();
            SliderSpecificationNumR.value = ColorTmp.r * 255;

            InputFieldSpecificationNumR.placeholder.GetComponent<Text>().text = (ColorTmp.r * 255).ToString();
            InputFieldSpecificationNumR.text = (ColorTmp.r * 255).ToString();
        }
        else
        if (argRGB.Equals("G"))
        {
            ImageSpecificationColorG.color = new Color(0.0f, ColorTmp.g, 0.0f, 1.0f);

            Slider SliderSpecificationNumG = GameObject.Find("SliderSpecificationNumG").GetComponent<Slider>();
            SliderSpecificationNumG.value = ColorTmp.g * 255;

            InputFieldSpecificationNumG.placeholder.GetComponent<Text>().text = (ColorTmp.g * 255).ToString();
            InputFieldSpecificationNumG.text = (ColorTmp.g * 255).ToString();
        }
        else
        if (argRGB.Equals("B"))
        {
            ImageSpecificationColorB.color = new Color(0.0f, 0.0f, ColorTmp.b, 1.0f);

            Slider SliderSpecificationNumB = GameObject.Find("SliderSpecificationNumB").GetComponent<Slider>();
            SliderSpecificationNumB.value = ColorTmp.b * 255;

            InputFieldSpecificationNumB.placeholder.GetComponent<Text>().text = (ColorTmp.b * 255).ToString();
            InputFieldSpecificationNumB.text = (ColorTmp.b * 255).ToString();
        }

        ControlImageSelectColor("SelectColorMethodRGBNum");
    }

    void ControlImageSelectColor(string argTag)
    {
        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag(argTag);

        foreach (GameObject gameObject in tag1_Objects)
        {
            if(gameObject.name.Equals("ImageSelectColor"))
                gameObject.GetComponent<Image>().color = new Color(ColorTmp.r, ColorTmp.g, ColorTmp.b);
            if(gameObject.name.Equals("TextSelectColorCode"))
                gameObject.GetComponent<Text>().text = "#" + ((int)(ColorTmp.r * 255)).ToString("X2") + ((int)(ColorTmp.g * 255)).ToString("X2") + ((int)(ColorTmp.b * 255)).ToString("X2");

            if(gameObject.name.Equals("TextSelectColorR"))
                gameObject.GetComponent<Text>().text = "R:" + (ColorTmp.r * 255).ToString();
            if(gameObject.name.Equals("TextSelectColorG"))
                gameObject.GetComponent<Text>().text = "G:" + (ColorTmp.g * 255).ToString();
            if(gameObject.name.Equals("TextSelectColorB"))
                gameObject.GetComponent<Text>().text = "B:" + (ColorTmp.b * 255).ToString();

            Color strColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            if (((ColorTmp.r + ColorTmp.g + ColorTmp.b) / 3.0f) < 0.5f)
            {
                strColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }
            if (gameObject.name.Equals("TextSelectColorCode"))
                gameObject.GetComponent<Text>().color = strColor;
            if (gameObject.name.Equals("TextSelectColorR"))
                gameObject.GetComponent<Text>().color = strColor;
            if (gameObject.name.Equals("TextSelectColorG"))
                gameObject.GetComponent<Text>().color = strColor;
            if (gameObject.name.Equals("TextSelectColorB"))
                gameObject.GetComponent<Text>().color = strColor;


            //GameObject.Find("ImageSelectColor").GetComponent<Image>().color = new Color(ColorTmp.r, ColorTmp.g, ColorTmp.b);
            //GameObject.Find("TextSelectColorCode").GetComponent<Text>().text = "#" + ((int)(ColorTmp.r * 255)).ToString("X2") + ((int)(ColorTmp.g * 255)).ToString("X2") + ((int)(ColorTmp.b * 255)).ToString("X2");

            //GameObject.Find("TextSelectColorR").GetComponent<Text>().text = "R:" + (ColorTmp.r * 255).ToString();
            //GameObject.Find("TextSelectColorG").GetComponent<Text>().text = "G:" + (ColorTmp.g * 255).ToString();
            //GameObject.Find("TextSelectColorB").GetComponent<Text>().text = "B:" + (ColorTmp.b * 255).ToString();

            //if (((ColorTmp.r + ColorTmp.g + ColorTmp.b) / 3.0f) < 0.5f)
            //{
            //    GameObject.Find("TextSelectColorCode").GetComponent<Text>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            //    GameObject.Find("TextSelectColorR").GetComponent<Text>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            //    GameObject.Find("TextSelectColorG").GetComponent<Text>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            //    GameObject.Find("TextSelectColorB").GetComponent<Text>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            //}
            //else
            //{
            //    GameObject.Find("TextSelectColorCode").GetComponent<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            //    GameObject.Find("TextSelectColorR").GetComponent<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            //    GameObject.Find("TextSelectColorG").GetComponent<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            //    GameObject.Find("TextSelectColorB").GetComponent<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            //}
        }

    }

    public void ShowAd()
    {
        if(Advertisement.IsReady())
        {
            //Advertisement.GetPlacementState();
            Advertisement.Show();
            //return true;
        }
        //return false;
    }

}