using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//using OpenCvSharp;

using System.IO;

using UnityEngine.Networking;

using UnityEngine.Advertisements;



public enum Trigger {User, Update};

static class Constants
{
    public const int CHARACTERS_ALL_NUM = 32;
    public const int CHARACTERS_HELP_PRODUCTION_NUM = 3;
    public const int CHARACTERS_PRODUCTION_PIXEL_NUM = 5;
    public const int CHARACTERS_PRODUCTION_CHARACTER_NUM = 5;
}

public class ConsumePixelClass
{
    public Color PixelColor;
    public uint ToBeCurConsumePixelsNum;
    public uint CurConsumePixelsNum;

    public ConsumePixelClass(Color argPixelColor, uint argToBeCurConsumePixelsNum, uint argCurConsumePixelsNum)
    {
        PixelColor = argPixelColor;
        ToBeCurConsumePixelsNum = argToBeCurConsumePixelsNum;
        CurConsumePixelsNum = argCurConsumePixelsNum;
    }
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

    uint[] CharactersIDProductionCharacter = new uint[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
    uint[] CharactersIDProducedCharacter = new uint[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
    List<bool[,]> ProgressTextureProductionCharacter = new List<bool[,]>();//[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
    List<ConsumePixelClass>[] ConsumePixelsProductionCharacter = new List<ConsumePixelClass>[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];

    //現在の全ピクセルの個数(CurColors[0,0,0]が黒、CurColors[255,0,0]が赤)
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



    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Start is called before the first frame update
    IEnumerator Start()
    {
        Debug.Log("==================== [LOG 1] Start 始まったよ！ ====================");

#if UNITY_EDITOR
        {


            {
                // 3x3 の Texture2D を作成
                Texture2D testImageTex = new Texture2D(3, 3, TextureFormat.RGBA32, false);

                // ピクセル色のセット (Colorは 0.0f ～ 1.0f で指定)
                testImageTex.SetPixel(0, 0, new Color(0.0f, 0.0f, 0.0f));
                testImageTex.SetPixel(1, 0, new Color(0.5f, 0.0f, 0.0f));
                testImageTex.SetPixel(2, 0, new Color(1.0f, 0.0f, 0.0f));

                testImageTex.SetPixel(0, 1, new Color(0.0f, 0.0f, 0.0f));
                testImageTex.SetPixel(1, 1, new Color(0.0f, 0.5f, 0.0f));
                testImageTex.SetPixel(2, 1, new Color(0.0f, 1.0f, 0.0f));

                testImageTex.SetPixel(0, 2, new Color(0.0f, 0.0f, 0.0f));
                testImageTex.SetPixel(1, 2, new Color(0.0f, 0.0f, 0.5f));
                testImageTex.SetPixel(2, 2, new Color(0.0f, 0.0f, 1.0f));

                // 変更を適用
                testImageTex.Apply();

                // PNGに変換して保存
                byte[] pngData = testImageTex.EncodeToPNG();
                string savePath = System.IO.Path.Combine(Application.persistentDataPath, "TestImage99.png");
                System.IO.File.WriteAllBytes(savePath, pngData);
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
                                             ref CharactersIDProductionPixel,
                                             ref CharactersIDProductionCharacter, ref CharactersIDProducedCharacter);
                                                 
        ModelProduction = GetComponent<ModelProduction>();



        for (int i = 0; i < Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1; i++)
        {
            ProgressTextureProductionCharacter.Add(null);
            ConsumePixelsProductionCharacter[i] = new List<ConsumePixelClass>();
        }

        // ② MakeFile が完全に終わるまで、ここで待機する！
        yield return StartCoroutine(MakeFile());
        Debug.Log("==================== [LOG 3] MakeFile 終わったよ！ ====================");

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

        // 1. スマホ内の保存パスを表示
        string testPath = Application.persistentDataPath + "/Character/RedSlime8.png";
        Debug.Log("【CHECK1】画像のパス: " + testPath);

        // 2. スマホの中に実際に画像ファイルが存在しているかチェック
        bool isExist = System.IO.File.Exists(testPath);
        Debug.Log("【CHECK2】ファイルは存在するか？ : " + isExist);

        // 3. 画像ファイルを読めたかチェック（ImagegUtilityなどを使っている場所の前後に書く）
        Texture2D tex = ImagegUtility.ReadPng("RedSlime8.png"); // ※実際の読み込み処理のコード
        Debug.Log("【CHECK3】テクスチャ読み込み結果 : " + (tex != null ? "成功！" : "失敗（null）"));


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

        CharactersAll[9].MakeCharacter(Application.persistentDataPath + "/Character/RedSlime16" + ".png", 9, "SmallRedSlime");
        CharactersAll[10].MakeCharacter(Application.persistentDataPath + "/Character/GreenSlime16" + ".png", 10, "SmallGreenSlime");
        CharactersAll[11].MakeCharacter(Application.persistentDataPath + "/Character/BlueSlime16" + ".png", 11, "SmallBlueSlime");
        CharactersAll[12].MakeCharacter(Application.persistentDataPath + "/Character/WhiteSlime16" + ".png", 12, "SmallWhiteSlime");

        CharactersAll[16 + 1].MakeCharacter(Application.persistentDataPath + "/Character/0032_slime_R" + ".png", 16 + 1, "RedSlime");
        CharactersAll[16 + 2].MakeCharacter(Application.persistentDataPath + "/Character/0032_slime_G" + ".png", 16 + 2, "GreenSlime");
        CharactersAll[16 + 3].MakeCharacter(Application.persistentDataPath + "/Character/0032_slime_B" + ".png", 16 + 3, "BlueSlime");
        CharactersAll[16 + 5].MakeCharacter(Application.persistentDataPath + "/Character/0032_rabbit" + ".png", 16 + 5, "WhiteRabbit");
        CharactersAll[16 + 6].MakeCharacter(Application.persistentDataPath + "/Character/0064_slimeking_R" + ".png", 16 + 6, "RedSlimeKing");
        CharactersAll[16 + 7].MakeCharacter(Application.persistentDataPath + "/Character/0064_slimeking_G" + ".png", 16 + 7, "GreenSlimeKing");
        CharactersAll[16 + 8].MakeCharacter(Application.persistentDataPath + "/Character/0064_slimeking_B" + ".png", 16 + 8, "BlueSlimeKing");

        //CharactersAll[32].MakeCharacter(Application.persistentDataPath + "/Character/wanwan" + ".png", 32, "wanwan");

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
            ref CharactersIDHelpProductionR, ref CharactersIDHelpProductionG, ref CharactersIDHelpProductionB,

            ref CharactersIDProductionPixel,
            ref ColorProductionPixel,
            ref CharactersIDProductionCharacter,
            ref CharactersIDProducedCharacter
            );


        //UIの更新
        UpdateRGBProductionScene();

        // --- 起動時にスライダーの最大値を階調数（GameConfig.GRADATION_LEVELS）に合わせる処理 ---
        // ヒエラルキーの構造（PrefabPixelListPageR ➔ SliderPixelListPageR）に合わせて安全に取得します
        Slider sliderR = GameObject.Find("PrefabPixelListPageR")?.transform.Find("SliderPixelListPageR")?.GetComponent<Slider>();
        if (sliderR != null) sliderR.maxValue = Mathf.RoundToInt(GameConfig.GRADATION_LEVELS / 3f)-1;

        Slider sliderG = GameObject.Find("PrefabPixelListPageG")?.transform.Find("SliderPixelListPageG")?.GetComponent<Slider>();
        if (sliderG != null) sliderG.maxValue = Mathf.RoundToInt(GameConfig.GRADATION_LEVELS / 3f)-1;

        Slider sliderB = GameObject.Find("PrefabPixelListPageB")?.transform.Find("SliderPixelListPageB")?.GetComponent<Slider>();
        if (sliderB != null) sliderB.maxValue = GameConfig.GRADATION_LEVELS;



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

                //ピクセルが生産されたら
                if (ProductionPixelFlag)
                {
                    CreatePixelListPixelProduction();
                    UpdateProductionCharacterScene();
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

            //キャラクター生産
            bool ProductionCharacterFlag = false;
            bool RepeatComplete = true;
            for (int i = 1; i < Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1; i++)
            {
                if (CharactersIDProductionCharacter[i] == 0 || CharactersIDProducedCharacter[i] == 0)
                    break;

                bool PCF = false;
                bool RC = true;
                ProductionCharacter(Trigger.Update, i,
                    CharactersAll[CharactersIDProducedCharacter[i]].PaintPixels, out PCF, out RC);
                if (PCF)
                    ProductionCharacterFlag = true;
                if (RC == false)
                    RepeatComplete = false;
            }
            if(ProductionCharacterFlag)
            {
                //TODO:キャラクターの所持数表示
            }
            if(RepeatComplete == false)
            {
                //TODO:キャラクター生産停止中の警告表示
            }
            //UI更新
            UpdateProductionCharacterScene();

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
            CharactersIDHelpProductionR, CharactersIDHelpProductionG, CharactersIDHelpProductionB,

            // ★追加
            CharactersIDProductionPixel,
            ColorProductionPixel,
            CharactersIDProductionCharacter,
            CharactersIDProducedCharacter
            );
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
        else//TODO:ButtonCharacterProductionCharacterでの更新
        if (ButtonCharacterTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            int ProductionIndex = int.Parse(ButtonCharacterTmp.name.Substring(ButtonCharacterTmp.name.Length - 2, 2));
            InitializeProgressProductionCharacter(ProductionIndex);
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
                if (PixelListPage[1] < Mathf.CeilToInt(GameConfig.GRADATION_LEVELS / 3))
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
                if (PixelListPage[2] < Mathf.CeilToInt(GameConfig.GRADATION_LEVELS / 3))
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
                if (PixelListPage[3] < GameConfig.GRADATION_LEVELS)
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
            if (Slider.value >= 0 && Slider.value <= Mathf.CeilToInt(GameConfig.GRADATION_LEVELS / 3))
                PixelListPage[1] = (byte)Slider.value;
        }
        else
        if (strRGB.Equals("G"))
        {
            if (Slider.value >= 0 && Slider.value <= Mathf.CeilToInt(GameConfig.GRADATION_LEVELS / 3))
                PixelListPage[2] = (byte)Slider.value;
        }
        else
        if (strRGB.Equals("B"))
        {
            if (Slider.value >= 0 && Slider.value <= GameConfig.GRADATION_LEVELS)
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

    ///////////////////////////////////////////////////////////////
    //キャラクター生産

    //使用ピクセルの表示
    public void ShowConsumePixel()
    {

    }

    //キャラクター生産の進捗初期化
    public void InitializeProgressProductionCharacter(int argIndex)
    {
        if (ConsumePixelsProductionCharacter[argIndex].Count != 0)
        {
            //TODO:ピクセルを還元
            ReductionPixelsProductionCharacter(argIndex);
        }
        else
        {
            //進捗ピクセルの初期化
            ConsumePixelsProductionCharacter[argIndex].Clear();
            foreach (ExistColor curExistColor in CharactersAll[CharactersIDProducedCharacter[argIndex]].ListExistsColors)
            {
                ConsumePixelsProductionCharacter[argIndex].Add(new ConsumePixelClass(curExistColor.Color, curExistColor.Num, 0));
            }
            //進捗画像の初期化
            ProgressTextureProductionCharacter[argIndex] = ImagegUtility.MakeSilhouetteBoolArray(ImagegUtility.ReadPng(CharactersAll[CharactersIDProducedCharacter[argIndex]].ImagePath));
        }

        //UI更新 //TODO:インデックスごとの更新でよい
        UpdateProductionCharacterScene();
    }
    //ピクセルを還元
    public void ReductionPixelsProductionCharacter(int argIndex)
    {
        foreach (ConsumePixelClass ConsumePixel in ConsumePixelsProductionCharacter[argIndex])
        {
            CurPixels[(int)ConsumePixel.PixelColor.r * 255, (int)ConsumePixel.PixelColor.g * 255, (int)ConsumePixel.PixelColor.b * 255]
                += ConsumePixel.CurConsumePixelsNum;
        }

        //進捗ピクセルの初期化
        ConsumePixelsProductionCharacter[argIndex].Clear();
        foreach (ExistColor curExistColor in CharactersAll[CharactersIDProducedCharacter[argIndex]].ListExistsColors)
        {
            ConsumePixelsProductionCharacter[argIndex].Add(new ConsumePixelClass(curExistColor.Color, curExistColor.Num, 0));
        }
        //進捗画像の初期化
        ProgressTextureProductionCharacter[argIndex] = ImagegUtility.MakeSilhouetteBoolArray(ImagegUtility.ReadPng(CharactersAll[CharactersIDProducedCharacter[argIndex]].ImagePath));
    }
    //キャラクター生産
    public bool ProductionCharacter(Trigger argTrigger, int argIndex, uint argRepeatNum, out bool ProductionCharacterFlag, out bool RepeatComplete)
    {
        ProductionCharacterFlag = false;
        RepeatComplete = false;

        if (CharactersIDProductionCharacter[argIndex] == 0 || CharactersIDProducedCharacter[argIndex] == 0)
            return false;

        Debug.Log("ProductionCharacter1");

        int curRepeatNum = 0;
        Texture2D ProductionCharacterTexture2D = ImagegUtility.ReadPng(CharactersAll[CharactersIDProducedCharacter[argIndex]].ImagePath);

        for (int y = ProductionCharacterTexture2D.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < ProductionCharacterTexture2D.width; x++)
            {
                if(ProgressTextureProductionCharacter[argIndex][x,y] == false)
                {
                    Color color = ProductionCharacterTexture2D.GetPixel(x, y);
                    if (CurPixels[(int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255)] > 0)
                    {
                        Debug.Log("ProductionCharacter2");

                        CurPixels[(int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255)]--;
                        ProgressTextureProductionCharacter[argIndex][x, y] = true;
                        foreach(ConsumePixelClass ConsumePixel in ConsumePixelsProductionCharacter[argIndex])
                        {
                            if(ConsumePixel.PixelColor.Equals(color))
                            {
                                Debug.Log("ProductionCharacter3");
                                ConsumePixel.CurConsumePixelsNum++;
                                break;
                            }
                        }
                        curRepeatNum++;
                    }
                }
                if(curRepeatNum == argRepeatNum)
                {
                    RepeatComplete = true;
                    break;
                }
            }
            if (RepeatComplete == true)
            {
                break;
            }
        }

        bool ProductionCharacterComplete = true;
        for (int y = 0; y < ProductionCharacterTexture2D.height; y++)
        {
            for (int x = 0; x < ProductionCharacterTexture2D.width; x++)
            {
                if (ProgressTextureProductionCharacter[argIndex][x, y] == false)
                {
                    ProductionCharacterComplete = false;
                    break;
                }
            }
            if (ProductionCharacterComplete == false)
                break;
        }

        if (ProductionCharacterComplete)
        {
            CharactersAll[CharactersIDProducedCharacter[argIndex]].OwnedNumCur++;

            //進捗ピクセルの初期化
            ConsumePixelsProductionCharacter[argIndex].Clear();
            foreach (ExistColor curExistColor in CharactersAll[CharactersIDProducedCharacter[argIndex]].ListExistsColors)
            {
                ConsumePixelsProductionCharacter[argIndex].Add(new ConsumePixelClass(curExistColor.Color, curExistColor.Num, 0));
            }
            //進捗画像の初期化
            ProgressTextureProductionCharacter[argIndex] = ImagegUtility.MakeSilhouetteBoolArray(ImagegUtility.ReadPng(CharactersAll[CharactersIDProducedCharacter[argIndex]].ImagePath));

            ProductionCharacterFlag = true;
        }

        return true;
    }


    //キャラクター生産でスピードアップボタンが押されたら
    public void PushButtonSpeedUpProductionCharacter(string argName)
    {
        int ProductionCharacterIndex = int.Parse(argName.Substring(argName.Length - 2, 2));

        bool ProductionCharacterFlag;
        bool RepeatComplete;
        ProductionCharacter(Trigger.User, ProductionCharacterIndex, 1, out ProductionCharacterFlag, out RepeatComplete);
        if (ProductionCharacterFlag)
        {
            //TODO:キャラクターの所持数表示
        }
        if (RepeatComplete == false)
        {
            //TODO:キャラクター生産停止中の警告表示
        }
        //UI更新
        UpdateProductionCharacterScene();
    }

    ////////////////////////////////////
    //Model?

    IEnumerator MakeFile()
    {
        Debug.Log("==================== [LOG 2] MakeFile 始まったよ！ ====================");
        Debug.Log("MakeFile Begin");

        // キャラクターフォルダ、正規化キャラクターフォルダの生成
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
        string[] files = System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/Character", "*.png", System.IO.SearchOption.AllDirectories);

        foreach (string file in files)
        {
            if (!(File.Exists(Application.persistentDataPath + "/Character/" + Path.GetFileName(file))))
            {
                Debug.Log("コピー " + file + "->" + Application.persistentDataPath + "/Character/" + Path.GetFileName(file));
                File.Copy(file, Application.persistentDataPath + "/Character/" + Path.GetFileName(file));
            }
        }

#elif UNITY_IPHONE
    Debug.Log("UNITY_IPHONE");
    string[] files = System.IO.Directory.GetFiles(Application.dataPath + "/Raw" + "/Character", "*.png", System.IO.SearchOption.AllDirectories);

    foreach (string file in files)
    {
        if (!(File.Exists(Application.persistentDataPath + "/Character/" + Path.GetFileName(file))))
        {
            Debug.Log("コピー " + file + "->" + Application.persistentDataPath + "/Character/" + Path.GetFileName(file));
            File.Copy(file, Application.persistentDataPath + "/Character/" + Path.GetFileName(file));
        }
    }

#elif UNITY_ANDROID
    Debug.Log("UNITY_ANDROID");

    string[] CharacterNames = { 
        "RedSlime8.png",
        "GreenSlime8.png",
        "BlueSlime8.png",
        "WhiteSlime8.png",
        "RBlackCat8.png",
        "WhiteCat8.png",
        "RedSlime16.png",
        "GreenSlime16.png",
        "BlueSlime16.png",
        "WhiteSlime16.png",
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
        string toPath = Application.persistentDataPath + "/Character/" + curCharacterName;

        // すでにファイルが存在すればスキップ（毎回コピーする無駄を省く）
        if (File.Exists(toPath)) continue;

        string path = Application.streamingAssetsPath + "/Character/" + curCharacterName;

        // 古い「WWW」と「while」を使わず、新しい UnityWebRequest と yield return で安全に待つ
        using (UnityWebRequest www = UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest(); // 読み込み完了までフリーズせずに待つ

            if (www.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllBytes(toPath, www.downloadHandler.data);
                Debug.Log("コピー成功: " + curCharacterName);
            }
            else
            {
                Debug.LogError("コピー失敗: " + curCharacterName + " エラー: " + www.error);
            }
        }
    }
#endif

        Debug.Log("MakeFile End");
        yield break; // ←★これを追加！（「ここでコルーチン終了」という意味です）
    }


    //////////////////////////////////////////////////////////////////////////
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
        // ページ指定部分
        ShowPagePixelListPixelProduction(GameObject.Find("PrefabPixelListPageR"));
        ShowPagePixelListPixelProduction(GameObject.Find("PrefabPixelListPageG"));
        ShowPagePixelListPixelProduction(GameObject.Find("PrefabPixelListPageB"));

        GameObject GameObjectContentPixelList = GameObject.Find("ContentPixelList");

        // PixelListの生成
        GameObject[,] ArrayShowPixelColorAndNum = new GameObject[GameConfig.GRADATION_LEVELS, GameConfig.GRADATION_LEVELS];

        // 計測開始
        System.Diagnostics.Stopwatch swI = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch swU = new System.Diagnostics.Stopwatch();

        int rowNum = 3;
        int colNum = 3;

        // ★修正ポイント1：1階調あたりの「色幅」を計算する
        // GRADATION_LEVELSが 16 の時は 256 / 16 = 16 刻み
        // GRADATION_LEVELSが  8 の時は 256 /  8 = 32 刻み に自動的になります
        int colorStep = 256 / GameConfig.GRADATION_LEVELS;

        int B = PixelListPage[3];

        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag("PixelList");
        foreach (GameObject gameObject in tag1_Objects)
        {
            if (gameObject.name.Equals("ContentPixelList"))
            {
                // 最後のページかどうかに関わらず、横に3個並べたい場合は常に「3」にする
                gameObject.GetComponent<GridLayoutGroup>().constraintCount = 3;
            }
        }

        ClearPixelListPixelProduction();

        for (int G = (PixelListPage[2] * rowNum); G < (PixelListPage[2] * rowNum) + rowNum; G++)
        {
            for (int R = (PixelListPage[1] * colNum); R < (PixelListPage[1] * colNum) + colNum; R++)
            {
                // ★修正ポイント3：cullNum ではなく、計算した色幅（colorStep）を掛ける
                int RColor = R * colorStep;
                int GColor = G * colorStep;
                int BColor = B * colorStep;
                // 256を超えないように255に丸める処理（元のロジックを維持）
                if (GameConfig.GRADATION_LEVELS != 1)
                {
                    if (RColor >= 256) RColor = 255;
                    if (GColor >= 256) GColor = 255;
                    if (BColor >= 256) BColor = 255;
                }

                // 255を超えた色をスキップするのではなく、255のマスとして生成して表示させる
                if (RColor > 255 && RColor - colorStep >= 255) continue;
                if (GColor > 255 && GColor - colorStep >= 255) continue;

                // プレハブのインスタンス化
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


    public void ShowPagePixelListPixelProduction(GameObject argPrefabPixelListPageRGB)
    {
        // 1ステップあたりの「色幅」を計算（8階調なら32刻み）
        int colorStep = 256 / GameConfig.GRADATION_LEVELS;

        // ★修正：-1 を削除し、割り算の切り上げ（CeilToInt）で純粋な最大ページ数を割り出す
        // 8階調のとき：Bは最大「7ページ」(0～7の8段階) になるようにします
        int maxPageR = Mathf.CeilToInt(GameConfig.GRADATION_LEVELS / 3f) - 1;
        int maxPageG = Mathf.CeilToInt(GameConfig.GRADATION_LEVELS / 3f) - 1;
        int maxPageB = GameConfig.GRADATION_LEVELS; // 👈 ここは 0 から数えるので -1 で合っていますが、前後の条件を調整します


        if (argPrefabPixelListPageRGB.name == "PrefabPixelListPageR")
        {
            int currentPage = PixelListPage[1];
            // ラベルテキストの計算（colorStep を基準にする）
            int minVal = currentPage * colorStep * 3;
            int maxVal = (currentPage * colorStep * 3) + (colorStep * 2);
            if (maxVal > 255) maxVal = 255;

            argPrefabPixelListPageRGB.GetComponentsInChildren<Text>()[2].text = minVal.ToString() + " ～ " + maxVal.ToString();

            // ボタンの有効・無効化（動的に計算した maxPageR を使う）
            argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = (currentPage > 0);
            argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = (currentPage < maxPageR);

            argPrefabPixelListPageRGB.GetComponentsInChildren<Slider>()[0].value = currentPage;

        }
        else
        if (argPrefabPixelListPageRGB.name == "PrefabPixelListPageG")
        {
            int currentPage = PixelListPage[2];
            // ラベルテキストの計算
            int minVal = currentPage * colorStep * 3;
            int maxVal = (currentPage * colorStep * 3) + (colorStep * 2);
            if (maxVal > 255) maxVal = 255;

            argPrefabPixelListPageRGB.GetComponentsInChildren<Text>()[2].text = minVal.ToString() + " ～ " + maxVal.ToString();

            // ボタンの有効・無効化（動的に計算した maxPageG を使う）
            argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = (currentPage > 0);
            argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = (currentPage < maxPageG);

            argPrefabPixelListPageRGB.GetComponentsInChildren<Slider>()[0].value = currentPage;

        }
        else
        if (argPrefabPixelListPageRGB.name == "PrefabPixelListPageB")
        {
            int currentPage = PixelListPage[3];
            // Bは範囲ではなく単一の数値を表示（255を超えないように丸める）
            int val = currentPage * colorStep;
            if (val > 255) val = 255;

            argPrefabPixelListPageRGB.GetComponentsInChildren<Text>()[2].text = val.ToString();

            // ボタンの有効・無効化（動的に計算した maxPageB を使う）
            argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[0].interactable = (currentPage > 0);
            argPrefabPixelListPageRGB.GetComponentsInChildren<Button>()[1].interactable = (currentPage < maxPageB);

            argPrefabPixelListPageRGB.GetComponentsInChildren<Slider>()[0].value = currentPage;

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

    //キャラクター生産の表示
    public void UpdateProductionCharacterScene()
    {
        GameObject[] CharacterProduction_Objects;
        CharacterProduction_Objects = GameObject.FindGameObjectsWithTag("CharacterProduction");

        foreach (GameObject CharacterProduction_gameObject in CharacterProduction_Objects)
        {
            if (CharacterProduction_gameObject.name.Equals("ContentCharacterProductionList"))
            {
                foreach (Transform gameObject in CharacterProduction_gameObject.transform)
                {
                    foreach (Transform child_gameObject in gameObject.transform)
                    {
                        if (child_gameObject.name.Contains("RawImageCharacterProduction"))
                        {
                            int ProductionIndex = int.Parse(child_gameObject.name.Substring(child_gameObject.name.Length - 1, 1));
                            if (ProgressTextureProductionCharacter[ProductionIndex] != null)
                            {
                                child_gameObject.GetComponent<RawImage>().texture = ImagegUtility.BoolArrayTOTexture(ProgressTextureProductionCharacter[ProductionIndex],
                                                                                                                    //CharactersAll[CharactersIDProducedCharacter[ProductionIndex]].Size, CharactersAll[CharactersIDProducedCharacter[ProductionIndex]].Size, 
                                                                                                                    ImagegUtility.ReadPng(CharactersAll[CharactersIDProducedCharacter[ProductionIndex]].ImagePath), new Color(0, 0, 0, 1));
                            }
                        }
                        //使用ピクセルリストの表示
                        if (child_gameObject.name.Contains("ScrollViewConsumePixel"))
                        {
                            foreach (Transform child_child_gameObject in child_gameObject)
                            {
                                if (child_child_gameObject.name.Contains("Viewport"))
                                {
                                    foreach (Transform child_child_child_gameObject in child_child_gameObject)
                                    {
                                        if (child_child_child_gameObject.name.Contains("Content"))
                                        {
                                            int ProductionIndex = int.Parse(child_gameObject.name.Substring(child_gameObject.name.Length - 2, 2));

                                            //PixelListの削除
                                            foreach (Transform child in child_child_child_gameObject.transform)
                                            {
                                                Destroy(child.gameObject);
                                            }

                                            if (ConsumePixelsProductionCharacter[ProductionIndex].Count != 0)
                                            {
                                                for (int i = 0; i < ConsumePixelsProductionCharacter[ProductionIndex].Count; i++)
                                                {
                                                    //プレハブのインスタンス化
                                                    GameObject prefab = Instantiate((GameObject)Resources.Load("PrefabShowPixelColorAndNum"), child_child_child_gameObject.transform) as GameObject;
                                                    prefab.GetComponentsInChildren<Image>()[1].color = ConsumePixelsProductionCharacter[ProductionIndex][i].PixelColor;
                                                    prefab.GetComponentsInChildren<Text>()[0].text = (ConsumePixelsProductionCharacter[ProductionIndex][i].PixelColor.r * 255).ToString();
                                                    prefab.GetComponentsInChildren<Text>()[2].text = (ConsumePixelsProductionCharacter[ProductionIndex][i].PixelColor.g * 255).ToString();
                                                    prefab.GetComponentsInChildren<Text>()[4].text = (ConsumePixelsProductionCharacter[ProductionIndex][i].PixelColor.b * 255).ToString();
                                                    prefab.GetComponentsInChildren<Text>()[5].text = ConsumePixelsProductionCharacter[ProductionIndex][i].CurConsumePixelsNum.ToString() + 
                                                                                                     " / " +
                                                                                                     ConsumePixelsProductionCharacter[ProductionIndex][i].ToBeCurConsumePixelsNum.ToString();
                                                    prefab.GetComponentsInChildren<Text>()[5].color = new Color(0, 0, 0, 1);
                                                    if(CurPixels[(int)(ConsumePixelsProductionCharacter[ProductionIndex][i].PixelColor.r * 255), (int)(ConsumePixelsProductionCharacter[ProductionIndex][i].PixelColor.g * 255), (int)(ConsumePixelsProductionCharacter[ProductionIndex][i].PixelColor.b * 255)]
                                                        < ConsumePixelsProductionCharacter[ProductionIndex][i].ToBeCurConsumePixelsNum - ConsumePixelsProductionCharacter[ProductionIndex][i].CurConsumePixelsNum)
                                                    {
                                                        prefab.GetComponentsInChildren<Text>()[5].color = new Color(1, 0, 0, 1);
                                                    }
                                                }
                                            }
                                        }

                                    }
                                }

                            }
                        }


                    }
                }
            }
        }

        //TODO:ためし
        ShowCharacterOwnedNum();
    }

    //TODO:TODO!
    //キャラクター生産のキャラクター所持数の表示
    public void ShowCharacterOwnedNum()
    {
        GameObject gameObjectCharacterList = null;
        GameObject[] tag1_Objects;
        tag1_Objects = GameObject.FindGameObjectsWithTag("CharacterProduction");
        foreach (GameObject gameObject in tag1_Objects)
        {
            if (gameObject.name.Equals("ContentCharacterList"))
            {
                gameObjectCharacterList = gameObject;
            }
        }

        //表示の初期化
        //キャラクターボタンの削除
        foreach (Transform child in gameObjectCharacterList.transform)
        {
            Destroy(child.gameObject);
        }

        //表示プレハブの作成
        for (int i = 0; i < Constants.CHARACTERS_ALL_NUM; i++)
        {
            if (CharactersAll[i].OwnedNumCur != 0)
            {
                //プレハブのインスタンス化
                GameObject GameObjectCharacterButton = Instantiate((GameObject)Resources.Load("PrefabButtonCharacterImage"), gameObjectCharacterList.transform) as GameObject;
                //spriteの指定
                GameObjectCharacterButton.GetComponentInChildren<Image>().sprite = Sprite.Create(ImagegUtility.ReadPng(CharactersAll[i].ImagePath), new UnityEngine.Rect(0, 0, CharactersAll[i].Size, CharactersAll[i].Size), new Vector2(0.5f, 0.5f));
                //textの指定
                GameObjectCharacterButton.GetComponentInChildren<Text>().text = "OwnedNum : " + CharactersAll[i].OwnedNumCur;
                GameObjectCharacterButton.GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            }
        }

    }


    //広告の表示
    public void ShowAd()
    {
        // 最新バージョン移行時のエラー回避のため、一時的にコメントアウト中
        UnityEngine.Debug.LogWarning("【デバッグ】Unity 6移行エラーのため、広告表示処理はコメントアウトされています。");

        /*
        if(Advertisement.IsReady())
        {
            //Advertisement.GetPlacementState();
            Advertisement.Show();
            //return true;
        }
        //return false;
        */
    }


}