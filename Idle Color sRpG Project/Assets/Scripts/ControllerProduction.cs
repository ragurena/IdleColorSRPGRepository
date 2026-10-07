using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    public const int BATTLE_PARTY_SET_NUM = 5;
    public const int BATTLE_FORMATION_SIZE = 3;
    public const int BATTLE_STAGE_NUM = 5;
    public const int BATTLE_STAGE_FLOOR_MAX = 100;
    public const int BATTLE_STAGE_FLOOR_STEP = 10;
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
    ControllerBattlePartyClass ControllerBattleParty;
    ControllerBattleStageClass ControllerBattleStage;
    ControllerBattleClass ControllerBattle;
    ControllerCatalogClass ControllerCatalog;
    ControllerFusionClass ControllerFusion;
    GameObject PanelCatalog;
    GameObject PanelFusion;
    BattleBalanceConfig BattleBalance = BattleBalanceConfig.CreateDefault();
    Dictionary<int, int> ItemCounts = new Dictionary<int, int>();

    CharacterClass[] CharactersAll = new CharacterClass[Constants.CHARACTERS_ALL_NUM + 1];

    uint[] CharactersIDHelpProductionR = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] CharactersIDHelpProductionG = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] CharactersIDHelpProductionB = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];

    uint[] CharactersIDProductionPixel = new uint[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1];
    Color[] ColorProductionPixel = new Color[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1];
    ushort[,] ProgressProductionPixel = new ushort[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1, 3 + 1];
    //途中の生産で実際に消費したRGB。キャラを替えても進捗は残し、色を変えたときだけここを還元する
    ulong[,] SpentProductionPixel = new ulong[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1, 3 + 1];
    //bool[,] WarningLackRGB = new bool[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1, 3 + 1];

    uint[] CharactersIDProductionCharacter = new uint[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
    uint[] CharactersIDProducedCharacter = new uint[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
    // [セット1..5, x1..3, y1..3]。0は空き。セットをまたいだ同じキャラは許可する
    uint[,,] BattlePartyCharacterIds = new uint[Constants.BATTLE_PARTY_SET_NUM + 1, Constants.BATTLE_FORMATION_SIZE + 1, Constants.BATTLE_FORMATION_SIZE + 1];
    int ActiveBattlePartySet = 0;
    int ActiveBattleStage = 0;
    int ActiveBattleFloorFrom = 0;
    int ActiveBattleFloorTo = 0;
    int ClearedBattleStage = 0;
    //ステージごとの最高クリア階。0は未クリア。開始階はこの直後の10階区切りまで
    int[] ClearedBattleFloor = new int[Constants.BATTLE_STAGE_NUM + 1];
    List<bool[,]> ProgressTextureProductionCharacter = new List<bool[,]>();//[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
    List<ConsumePixelClass>[] ConsumePixelsProductionCharacter = new List<ConsumePixelClass>[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
    Texture[] ProductionCharacterViewTexture = new Texture[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];

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

    [SerializeField] GameObject PanelBattleParty;

    [SerializeField] GameObject PanelBattleStage;

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

        ControllerBattleParty = GetComponent<ControllerBattlePartyClass>();
        ControllerBattleParty.Initialize(ref CharactersAll, this);

        ControllerBattleStage = GetComponent<ControllerBattleStageClass>();
        if (ControllerBattleStage != null)
            ControllerBattleStage.Initialize(ref CharactersAll, this);
                                                 
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
        bool rebuildImages = File.Exists(Application.persistentDataPath + "/ICS.csv")
            && SaveClass.IsOlderThan(SaveClass.ReadDataVersion(), GameConfig.ImageDataVersion);
        if (rebuildImages)
        {
            Debug.Log("セーブの版が古いので、減色画像を作り直す");
            ClearNormalizedCharacterImages();
        }

        CharacterRoster.RegisterAll(CharactersAll);

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
            ref ProgressProductionPixel,
            ref SpentProductionPixel,
            ref CharactersIDProductionCharacter,
            ref CharactersIDProducedCharacter,
            ProgressTextureProductionCharacter,
            ConsumePixelsProductionCharacter,
            ref CurPixels,
            ref BattlePartyCharacterIds,
            ref ActiveBattlePartySet,
            ref ActiveBattleStage,
            ref ActiveBattleFloorFrom,
            ref ActiveBattleFloorTo,
            ref ClearedBattleStage,
            ref ClearedBattleFloor,
            ref ItemCounts
            );

        for (int i = 1; i <= Constants.CHARACTERS_ALL_NUM; i++)
        {
            if (CharactersAll[i] != null && CharactersAll[i].ID != 0)
                CharactersAll[i].RecalculateBaseStats(BattleBalance);
        }

        if (ActiveBattlePartySet == 0 || ActiveBattleStage < 1 || ActiveBattleStage > Constants.BATTLE_STAGE_NUM)
            ActiveBattleStage = 0;
        if (ActiveBattlePartySet == 0 || !IsBattleFloorRange(ActiveBattleFloorFrom, ActiveBattleFloorTo))
        {
            ActiveBattleFloorFrom = 0;
            ActiveBattleFloorTo = 0;
        }
        if (ClearedBattleStage < 0 || ClearedBattleStage > Constants.BATTLE_STAGE_NUM)
            ClearedBattleStage = 0;
        NormalizeClearedBattleFloors();
        DropFormerStarterOwnership();
        SeedPixelSpendFromProgress();

        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM; i++)
        {
            //生産者がいなくても、作る対象がいればシルエットを出す
            if (CharactersIDProducedCharacter[i] == 0)
                continue;

            if (rebuildImages || ProgressTextureProductionCharacter[i] == null)
                InitProgressProductionCharacterWithoutReduction(i);
        }

        if (rebuildImages)
            SaveGame();

        UpdateProductionCharacterScene();

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

        ControllerBattle = GetComponent<ControllerBattleClass>();
        if (ControllerBattle == null)
            ControllerBattle = gameObject.AddComponent<ControllerBattleClass>();
        ControllerBattle.Initialize(this);

        ControllerCatalog = GetComponent<ControllerCatalogClass>();
        if (ControllerCatalog == null)
            ControllerCatalog = gameObject.AddComponent<ControllerCatalogClass>();
        if (PanelRGBProduction != null)
        {
            ControllerCatalog.Initialize(this, PanelRGBProduction);
            PanelCatalog = ControllerCatalog.Panel;
            if (PanelCatalog != null)
                NotShowPanel(PanelCatalog);
        }
        WireCatalogButton();

        ControllerFusion = GetComponent<ControllerFusionClass>();
        if (ControllerFusion == null)
            ControllerFusion = gameObject.AddComponent<ControllerFusionClass>();
        if (PanelRGBProduction != null)
        {
            ControllerFusion.Initialize(this, PanelRGBProduction);
            PanelFusion = ControllerFusion.Panel;
            if (PanelFusion != null)
                NotShowPanel(PanelFusion);
        }
        WireFusionButton();

        PushButtonSelectSceneRGBProduction();
        if (ControllerBattle != null)
            ControllerBattle.Hide();

        Debug.Log("ControllerProduction End");
    }

    // Update is called once per frame
    private float TimeOut = 1;
    private float TimeElapsed = 0;
    void Update()
    {
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
                ProductionPixel(Trigger.Update, i, out _);
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
                    continue;

                bool PCF = false;
                bool RC = true;
                bool painted = false;
                ProductionCharacter(Trigger.Update, i,
                    CharactersAll[CharactersIDProductionCharacter[i]].PaintPixels, out PCF, out RC, out painted);
                if (painted || PCF)
                    UpdateProductionCharacterProgressImage(i);
                if (PCF)
                    ProductionCharacterFlag = true;
                if (RC == false)
                    RepeatComplete = false;
            }
            RefreshProductionFigures();
            if(ProductionCharacterFlag)
                ShowCharacterOwnedNum();
            if(RepeatComplete == false)
            {
                //TODO:キャラクター生産停止中の警告表示
            }

        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //セーブ機能

    //セーブボタンが押されたら
    public void PushButtonSave()
    {
        GameObject.Find("ButtonSave").GetComponent<Button>().GetComponentInChildren<Text>().text = Application.persistentDataPath + "/ICS.csv";
        SaveGame();
    }

    public void SaveGame()
    {
        SaveClass SC = new SaveClass();
        SC.Save(CharactersAll, Constants.CHARACTERS_ALL_NUM + 1,
            CurR, CurG, CurB,
            MaxR, MaxG, MaxB,
            CostMaxRUp, CostMaxGUp, CostMaxBUp,
            IncreaseValueR, IncreaseValueG, IncreaseValueB,
            CostIncreaseValueRUp, CostIncreaseValueGUp, CostIncreaseValueBUp,
            CharactersIDHelpProductionR, CharactersIDHelpProductionG, CharactersIDHelpProductionB,

            CharactersIDProductionPixel,
            ColorProductionPixel,
            ProgressProductionPixel,
            SpentProductionPixel,
            CharactersIDProductionCharacter,
            CharactersIDProducedCharacter,
            ProgressTextureProductionCharacter,
            ConsumePixelsProductionCharacter,
            CurPixels,
            BattlePartyCharacterIds,
            ActiveBattlePartySet,
            ActiveBattleStage,
            ActiveBattleFloorFrom,
            ActiveBattleFloorTo,
            ClearedBattleStage,
            ClearedBattleFloor,
            ItemCounts
            );
    }

    static void ClearNormalizedCharacterImages()
    {
        string dir = Application.persistentDataPath + "/Character/Nomalization";
        if (!Directory.Exists(dir))
            return;
        string[] files = Directory.GetFiles(dir, "Nomalization_*.png");
        for (int i = 0; i < files.Length; i++)
            File.Delete(files[i]);
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
        //PanelBattlePartyの非表示
        NotShowPanel(PanelBattleParty);
        NotShowBattleStagePanel();
        NotShowCatalogPanel();
        NotShowFusionPanel();
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
        //PanelBattlePartyの非表示
        NotShowPanel(PanelBattleParty);
        NotShowBattleStagePanel();
        NotShowCatalogPanel();
        NotShowFusionPanel();
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
        //PanelBattlePartyの非表示
        NotShowPanel(PanelBattleParty);
        NotShowBattleStagePanel();
        NotShowCatalogPanel();
        NotShowFusionPanel();
        UpdateProductionCharacterConsumeViews();
        UpdateCharacterProductionStockLabels();
    }

    //バトル編成シーンボタンが押されたら
    public void PushButtonSelectSceneBattleParty()
    {
        //PanelBattlePartyの表示
        ShowPanel(PanelBattleParty);
        //UIの更新
        ControllerBattleParty.Open();

        //他のシーンを非表示
        //PanelRGBProductionの非表示
        NotShowPanel(PanelRGBProduction);
        //PanelPixelProductionの非表示
        NotShowPanel(PanelPixelProduction);
        ClearPixelListPixelProduction();
        //PanelCharacterProductionの非表示
        NotShowPanel(PanelCharacterProduction);
        NotShowBattleStagePanel();
        NotShowCatalogPanel();
        NotShowFusionPanel();
    }

    //バトルステージ選択シーンボタンが押されたら
    public void PushButtonSelectSceneBattleStage()
    {
        if (PanelBattleStage == null)
            return;

        ShowPanel(PanelBattleStage);
        if (ControllerBattleStage != null)
            ControllerBattleStage.Open();

        NotShowPanel(PanelRGBProduction);
        NotShowPanel(PanelPixelProduction);
        ClearPixelListPixelProduction();
        NotShowPanel(PanelCharacterProduction);
        NotShowPanel(PanelBattleParty);
        NotShowCatalogPanel();
        NotShowFusionPanel();
    }

    public void PushButtonSelectSceneCatalog()
    {
        if (PanelCatalog == null)
            return;

        ShowPanel(PanelCatalog);
        if (ControllerCatalog != null)
            ControllerCatalog.Open();

        NotShowPanel(PanelRGBProduction);
        NotShowPanel(PanelPixelProduction);
        ClearPixelListPixelProduction();
        NotShowPanel(PanelCharacterProduction);
        NotShowPanel(PanelBattleParty);
        NotShowBattleStagePanel();
        NotShowFusionPanel();
    }

    public void ReturnFromBattle()
    {
        if (PanelBattleStage == null)
            return;

        ShowPanel(PanelBattleStage);
        if (ControllerBattleStage != null)
            ControllerBattleStage.Refresh();

        NotShowPanel(PanelRGBProduction);
        NotShowPanel(PanelPixelProduction);
        ClearPixelListPixelProduction();
        NotShowPanel(PanelCharacterProduction);
        NotShowPanel(PanelBattleParty);
        NotShowCatalogPanel();
        NotShowFusionPanel();
    }

    void NotShowCatalogPanel()
    {
        if (PanelCatalog != null)
            NotShowPanel(PanelCatalog);
    }

    public void PushButtonSelectSceneFusion()
    {
        if (PanelFusion == null)
            return;

        ShowPanel(PanelFusion);
        if (ControllerFusion != null)
            ControllerFusion.Open();

        NotShowPanel(PanelRGBProduction);
        NotShowPanel(PanelPixelProduction);
        ClearPixelListPixelProduction();
        NotShowPanel(PanelCharacterProduction);
        NotShowPanel(PanelBattleParty);
        NotShowBattleStagePanel();
        NotShowCatalogPanel();
    }

    void NotShowFusionPanel()
    {
        if (PanelFusion != null)
            NotShowPanel(PanelFusion);
    }

    void WireFusionButton()
    {
        GameObject bar = GameObject.Find("PanelSelectScene");
        if (bar == null)
        {
            Debug.LogWarning("PanelSelectScene が見つかりません");
            return;
        }

        GameObject adButton = GameObject.Find("ButtonSelectScene7");
        if (adButton != null)
        {
            RectTransform adRect = adButton.GetComponent<RectTransform>();
            if (adRect != null)
                adRect.anchoredPosition = new Vector2(0f, 4000f);
        }

        Transform existing = bar.transform.Find("ButtonFusionScene");
        GameObject buttonObject = existing != null ? existing.gameObject : null;
        if (buttonObject == null)
        {
            buttonObject = new GameObject("ButtonFusionScene", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            buttonObject.transform.SetParent(bar.transform, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.25f, 0.48f, 0.75f, 1f);
            buttonObject.AddComponent<Button>();
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "合成";
            text.raycastTarget = false;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(450f, 0f);
        rect.sizeDelta = new Vector2(128f, 128f);

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            return;
        button.onClick.RemoveListener(PushButtonSelectSceneFusion);
        button.onClick.AddListener(PushButtonSelectSceneFusion);
    }

    public bool TryFuse(uint characterId)
    {
        CharacterClass character = GetCharacter(characterId);
        if (character == null || character.ID != characterId)
            return false;

        long lives = character.OwnedNumCur > (ulong)long.MaxValue ? long.MaxValue : (long)character.OwnedNumCur;
        long count = character.FusionCount > (ulong)long.MaxValue ? long.MaxValue : (long)character.FusionCount;
        if (!FusionBonus.CanFuse(lives, count))
            return false;

        long cost = FusionBonus.NextCost(count);
        long nextLives = lives - cost;
        if (nextLives > int.MaxValue)
            nextLives = int.MaxValue;
        character.SetOwnedCur((int)nextLives, BattleBalance.MaxLives);
        if (character.FusionCount < ulong.MaxValue)
            character.FusionCount++;
        character.RebuildLevelStats(BattleBalance);
        if (ControllerBattle != null)
            ControllerBattle.ApplyAllyFusion(character);
        ShowCharacterOwnedNum();
        SaveGame();
        return true;
    }

    void WireCatalogButton()
    {
        GameObject buttonObject = GameObject.Find("ButtonSelectSceneZukan");
        if (buttonObject == null)
            buttonObject = GameObject.Find("ButtonSelectScene6");
        if (buttonObject == null)
        {
            Debug.LogWarning("ButtonSelectSceneZukan が見つかりません");
            return;
        }

        Text label = buttonObject.GetComponentInChildren<Text>();
        if (label != null)
            label.text = "図鑑";

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            return;
        button.onClick.RemoveListener(PushButtonSelectSceneCatalog);
        button.onClick.AddListener(PushButtonSelectSceneCatalog);
    }

    public void MarkCatalogDefeated(uint characterId)
    {
        CharacterClass character = GetCharacter(characterId);
        if (character == null || character.ID != characterId || character.CatalogOpened)
            return;
        character.CatalogOpened = true;
        SaveGame();
        if (ControllerCatalog != null && PanelCatalog != null && PanelCatalog.GetComponent<CanvasGroup>().alpha > 0f)
            ControllerCatalog.Refresh();
    }

    void NotShowBattleStagePanel()
    {
        if (PanelBattleStage != null)
            NotShowPanel(PanelBattleStage);
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
        if (ControllerBattlePartyClass.IsCellButton(ButtonCharacterTmp))
        {
            int x, y;
            ControllerBattlePartyClass.GetCellPosition(ButtonCharacterTmp, out x, out y);
            ControllerCharacterSelect.ShowPanelSelectCharacterBattleParty(ButtonCharacterTmp,
                GetBattlePartyCharacter(ControllerBattleParty.GetCurrentSetIndex(), x, y));
            return;
        }
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
        if (ControllerBattlePartyClass.IsCellButton(ButtonCharacterTmp))
        {
            SetBattlePartyCharacterFromSelectCharacter(ControllerCharacterSelect.GetSelectedCharacterID());
            return;
        }

        int producedIndex = -1;
        uint previousProducedId = 0;
        if (ButtonCharacterTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            producedIndex = int.Parse(ButtonCharacterTmp.name.Substring(ButtonCharacterTmp.name.Length - 2, 2));
            previousProducedId = CharactersIDProducedCharacter[producedIndex];
        }

        ControllerCharacterSelect.ConfirmSelectCharacter(ButtonCharacterTmp);

        if (ButtonCharacterTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            //生産キャラが変わっても、色と進捗はそのまま続ける
            UpdatePixelProductionScene();
        }
        else
        if (producedIndex >= 0 && CharactersIDProducedCharacter[producedIndex] != previousProducedId)
        {
            //作る対象が変わったときだけ、使ったピクセルを戻して最初から。生産担当の変更ではここを通らない
            InitializeProgressProductionCharacter(producedIndex);
        }
        if (producedIndex >= 0)
            SetCharacterStockLabel(producedIndex);

        //どのボタンで呼び出されたか削除
        ButtonCharacterTmp = null;

        ControllerCharacterSelect.NotShowPanelSelectCharacter();
        NotShowPanel(PanelSelectCharacter);
    }

    //キャラクターセレクトの外すボタンが押されたら
    public void PushButtonRemoveCharacterSelect()
    {
        if (ControllerBattlePartyClass.IsCellButton(ButtonCharacterTmp))
        {
            SetBattlePartyCharacterFromSelectCharacter(0);
            return;
        }

        ControllerCharacterSelect.RemoveCharacterSelect(ButtonCharacterTmp);

        if (ButtonCharacterTmp.name.Contains("ButtonPixelProductionCharacter"))
        {
            //外しても進捗は残す。次のキャラが続きから生産する
            UpdatePixelProductionScene();
        }
        else
        if (ButtonCharacterTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            //生産対象を外したら、塗った分のピクセルを戻してシルエットを消す。生産担当を外してもここは通らない
            int ProductionIndex = int.Parse(ButtonCharacterTmp.name.Substring(ButtonCharacterTmp.name.Length - 2, 2));
            InitializeProgressProductionCharacter(ProductionIndex);
            SetCharacterStockLabel(ProductionIndex);
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

        ApplyPixelProductionColor(ProductionPixelIndex, ColorTmp);

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
            Vector2 pointer = PointerScreenPosition();
            Vector2 clickPosImage = new Vector2(pointer.x - ButtonPosLeftUnder.x, pointer.y - ButtonPosLeftUnder.y);
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
    // Input System 有効時は UnityEngine.Input を読むと例外になる
    Vector2 PointerScreenPosition()
    {
        if (Pointer.current == null)
        {
            Debug.LogWarning("ポインタ位置を取得できません");
            return Vector2.zero;
        }
        return Pointer.current.position.ReadValue();
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

        ApplyPixelProductionColor(ProductionPixelIndex, ColorTmp);

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

        ProductionPixel(Trigger.User, ProductionPixelIndex, out _);
        RefreshProductionFigures();
    }

    ///////////////////////////////////////////////////////////////
    //キャラクター生産

    //使用ピクセルの表示
    public void ShowConsumePixel()
    {

    }

    //キャラクター生産の進捗初期化（ロード時用・ピクセル還元なし）
    public void InitProgressProductionCharacterWithoutReduction(int argIndex)
    {
        uint characterId = CharactersIDProducedCharacter[argIndex];
        if (characterId == 0 || characterId > Constants.CHARACTERS_ALL_NUM || CharactersAll[characterId] == null
            || CharactersAll[characterId].ListExistsColors == null || CharactersAll[characterId].ImageTexture2D == null)
            return;

        ConsumePixelsProductionCharacter[argIndex].Clear();
        foreach (ExistColor curExistColor in CharactersAll[characterId].ListExistsColors)
        {
            ConsumePixelsProductionCharacter[argIndex].Add(new ConsumePixelClass(curExistColor.Color, curExistColor.Num, 0));
        }
        ProgressTextureProductionCharacter[argIndex] = ImagegUtility.MakeSilhouetteBoolArray(CharactersAll[characterId].ImageTexture2D);
    }

    //作る対象が変わったとき、使ったピクセルを戻して、新しい対象のシルエットから始める
    public void InitializeProgressProductionCharacter(int argIndex)
    {
        ReductionPixelsProductionCharacter(argIndex);
        UpdateProductionCharacterProgressImage(argIndex);
        RefreshProductionFigures();
    }
    //ピクセルを還元してから、今の生産対象で進捗を作り直す。対象が無いときは空にする
    public void ReductionPixelsProductionCharacter(int argIndex)
    {
        foreach (ConsumePixelClass ConsumePixel in ConsumePixelsProductionCharacter[argIndex])
            ReturnConsumedPixel(ConsumePixel);

        ConsumePixelsProductionCharacter[argIndex].Clear();
        uint nextId = CharactersIDProducedCharacter[argIndex];
        if (nextId == 0 || nextId > Constants.CHARACTERS_ALL_NUM || CharactersAll[nextId] == null || CharactersAll[nextId].ListExistsColors == null)
        {
            if (argIndex >= 0 && argIndex < ProgressTextureProductionCharacter.Count)
                ProgressTextureProductionCharacter[argIndex] = null;
            return;
        }

        foreach (ExistColor curExistColor in CharactersAll[nextId].ListExistsColors)
            ConsumePixelsProductionCharacter[argIndex].Add(new ConsumePixelClass(curExistColor.Color, curExistColor.Num, 0));
        ProgressTextureProductionCharacter[argIndex] = ImagegUtility.MakeSilhouetteBoolArray(CharactersAll[nextId].ImageTexture2D);
    }

    void ReturnConsumedPixel(ConsumePixelClass consumePixel)
    {
        if (consumePixel == null || consumePixel.CurConsumePixelsNum == 0 || CurPixels == null)
            return;
        int r = (int)(consumePixel.PixelColor.r * 255f);
        int g = (int)(consumePixel.PixelColor.g * 255f);
        int b = (int)(consumePixel.PixelColor.b * 255f);
        if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
            return;
        ulong current = CurPixels[r, g, b];
        ulong add = consumePixel.CurConsumePixelsNum;
        if (ulong.MaxValue - current < add)
            CurPixels[r, g, b] = ulong.MaxValue;
        else
            CurPixels[r, g, b] = current + add;
    }
    //キャラクター生産
    public bool ProductionCharacter(Trigger argTrigger, int argIndex, uint argRepeatNum, out bool ProductionCharacterFlag, out bool RepeatComplete, out bool paintedPixel)
    {
        ProductionCharacterFlag = false;
        RepeatComplete = false;
        paintedPixel = false;

        if (CharactersIDProductionCharacter[argIndex] == 0 || CharactersIDProducedCharacter[argIndex] == 0)
            return false;

        if (ProgressTextureProductionCharacter[argIndex] == null)
            return false;


        int curRepeatNum = 0;
        Texture2D ProductionCharacterTexture2D = CharactersAll[CharactersIDProducedCharacter[argIndex]].ImageTexture2D;
        if (ProductionCharacterTexture2D == null)
            return false;

        for (int y = ProductionCharacterTexture2D.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < ProductionCharacterTexture2D.width; x++)
            {
                if(ProgressTextureProductionCharacter[argIndex][x,y] == false)
                {
                    Color color = ProductionCharacterTexture2D.GetPixel(x, y);
                    if (CurPixels[(int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255)] > 0)
                    {

                        CurPixels[(int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255)]--;
                        ProgressTextureProductionCharacter[argIndex][x, y] = true;
                        foreach(ConsumePixelClass ConsumePixel in ConsumePixelsProductionCharacter[argIndex])
                        {
                            if(ConsumePixel.PixelColor.Equals(color))
                            {
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

        paintedPixel = curRepeatNum > 0;

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
            CharacterClass produced = CharactersAll[CharactersIDProducedCharacter[argIndex]];
            int ownedBefore = produced.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)produced.OwnedNumCur;
            produced.GainOwned(1, BattleBalance.MaxLives);
            int ownedAfter = produced.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)produced.OwnedNumCur;
            if (ownedAfter > ownedBefore && ControllerBattle != null)
                ControllerBattle.AddAllyLives(produced.ID, ownedAfter - ownedBefore);

            //進捗ピクセルの初期化
            ConsumePixelsProductionCharacter[argIndex].Clear();
            foreach (ExistColor curExistColor in CharactersAll[CharactersIDProducedCharacter[argIndex]].ListExistsColors)
            {
                ConsumePixelsProductionCharacter[argIndex].Add(new ConsumePixelClass(curExistColor.Color, curExistColor.Num, 0));
            }
            //進捗画像の初期化
            ProgressTextureProductionCharacter[argIndex] = ImagegUtility.MakeSilhouetteBoolArray(CharactersAll[CharactersIDProducedCharacter[argIndex]].ImageTexture2D);

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
        bool paintedPixel;
        ProductionCharacter(Trigger.User, ProductionCharacterIndex, 1, out ProductionCharacterFlag, out RepeatComplete, out paintedPixel);
        if (paintedPixel || ProductionCharacterFlag)
            UpdateProductionCharacterProgressImage(ProductionCharacterIndex);
        RefreshProductionFigures();
        if (ProductionCharacterFlag)
            ShowCharacterOwnedNum();
        if (RepeatComplete == false)
        {
            //TODO:キャラクター生産停止中の警告表示
        }
    }

    //バトル編成。1セットは3x3。同じセット内の同じキャラは移動扱いにする
    public bool SetBattlePartyCharacter(int setIndex, int x, int y, uint characterId)
    {
        if (!IsBattlePartyCell(setIndex, x, y))
            return false;

        if (characterId != 0)
        {
            if (characterId > Constants.CHARACTERS_ALL_NUM || CharactersAll[characterId] == null || CharactersAll[characterId].ID != characterId)
                return false;
            if (setIndex == ActiveBattlePartySet)
                return false;

            for (int cellX = 1; cellX <= Constants.BATTLE_FORMATION_SIZE; cellX++)
            {
                for (int cellY = 1; cellY <= Constants.BATTLE_FORMATION_SIZE; cellY++)
                {
                    if (cellX == x && cellY == y)
                        continue;
                    if (BattlePartyCharacterIds[setIndex, cellX, cellY] == characterId)
                        BattlePartyCharacterIds[setIndex, cellX, cellY] = 0;
                }
            }
        }

        BattlePartyCharacterIds[setIndex, x, y] = characterId;
        return true;
    }

    //キャラクターセレクトパネルから編成のマスへ反映する。0なら外す
    void SetBattlePartyCharacterFromSelectCharacter(uint characterId)
    {
        int x, y;
        ControllerBattlePartyClass.GetCellPosition(ButtonCharacterTmp, out x, out y);
        SetBattlePartyCharacter(ControllerBattleParty.GetCurrentSetIndex(), x, y, characterId);

        //どのボタンで呼び出されたか削除
        ButtonCharacterTmp = null;

        ControllerCharacterSelect.NotShowPanelSelectCharacter();
        NotShowPanel(PanelSelectCharacter);

        ControllerBattleParty.Refresh();
    }

    public int GetActiveBattlePartySet()
    {
        return ActiveBattlePartySet;
    }

    public uint GetBattlePartyCharacter(int setIndex, int x, int y)
    {
        if (!IsBattlePartyCell(setIndex, x, y))
            return 0;
        return BattlePartyCharacterIds[setIndex, x, y];
    }

    public void ClearBattlePartySet(int setIndex)
    {
        if (setIndex < 1 || setIndex > Constants.BATTLE_PARTY_SET_NUM)
            return;
        if (setIndex == ActiveBattlePartySet)
            return;

        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
                BattlePartyCharacterIds[setIndex, x, y] = 0;
        }
    }

    bool IsBattlePartyCell(int setIndex, int x, int y)
    {
        return setIndex >= 1 && setIndex <= Constants.BATTLE_PARTY_SET_NUM
            && x >= 1 && x <= Constants.BATTLE_FORMATION_SIZE
            && y >= 1 && y <= Constants.BATTLE_FORMATION_SIZE;
    }

    //出撃中だけ Whereabouts を Battle にする。別セットを出すときは、今のセットを撤退してから入れ替える
    public bool EnterBattle(int setIndex)
    {
        if (setIndex < 1 || setIndex > Constants.BATTLE_PARTY_SET_NUM)
            return false;

        LeaveBattle();

        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                uint characterId = BattlePartyCharacterIds[setIndex, x, y];
                if (characterId == 0)
                    continue;

                ReleaseCharacterFromProduction(characterId);
                CharactersAll[characterId].Whereabouts = Place.Battle;
            }
        }

        ActiveBattlePartySet = setIndex;
        ActiveBattleStage = 0;
        RefreshProductionAssignmentViews();
        return true;
    }

    //ステージ選択パネルからの出撃。1人以上いるセットだけ成功する。別セットが出ていれば先に撤退する
    public bool EnterBattleStage(int stageIndex, int setIndex, int floorFrom, int floorTo)
    {
        if (stageIndex < 1 || stageIndex > Constants.BATTLE_STAGE_NUM)
            return false;
        if (!IsBattleStageUnlocked(stageIndex))
            return false;
        if (!IsBattleFloorRange(floorFrom, floorTo))
            return false;
        if (floorFrom > GetMaxBattleFloorFrom(stageIndex))
            return false;
        if (!HasBattlePartyMember(setIndex))
            return false;
        if (!EnterBattle(setIndex))
            return false;

        ActiveBattleStage = stageIndex;
        ActiveBattleFloorFrom = floorFrom;
        ActiveBattleFloorTo = floorTo;
        return true;
    }

    //10階区切り。始まりは 1,11,21... 終わりは 10,20,30... で、終わりは始まり以上
    public static bool IsBattleFloorRange(int floorFrom, int floorTo)
    {
        int step = Constants.BATTLE_STAGE_FLOOR_STEP;
        if (floorFrom < 1 || floorTo > Constants.BATTLE_STAGE_FLOOR_MAX || floorTo < floorFrom)
            return false;
        if ((floorFrom - 1) % step != 0)
            return false;
        if (floorTo % step != 0)
            return false;
        return true;
    }

    public int GetActiveBattleStage()
    {
        return ActiveBattleStage;
    }

    public int GetActiveBattleFloorFrom()
    {
        return ActiveBattleFloorFrom;
    }

    public int GetActiveBattleFloorTo()
    {
        return ActiveBattleFloorTo;
    }

    public int GetClearedBattleStage()
    {
        return ClearedBattleStage;
    }

    public int GetHighestClearedBattleFloor(int stageIndex)
    {
        if (stageIndex < 1 || stageIndex > Constants.BATTLE_STAGE_NUM || ClearedBattleFloor == null)
            return 0;
        return ClearedBattleFloor[stageIndex];
    }

    //クリア階の次の階が含まれる10階区切りの始まり。未クリアなら1階
    public int GetMaxBattleFloorFrom(int stageIndex)
    {
        return MaxBattleFloorFrom(GetHighestClearedBattleFloor(stageIndex));
    }

    public static int MaxBattleFloorFrom(int clearedFloor)
    {
        if (clearedFloor < 0)
            clearedFloor = 0;
        int next = clearedFloor + 1;
        if (next > Constants.BATTLE_STAGE_FLOOR_MAX)
            next = Constants.BATTLE_STAGE_FLOOR_MAX;
        int step = Constants.BATTLE_STAGE_FLOOR_STEP;
        return ((next - 1) / step) * step + 1;
    }

    public void MarkBattleFloorCleared(int stageIndex, int floor)
    {
        if (stageIndex < 1 || stageIndex > Constants.BATTLE_STAGE_NUM || ClearedBattleFloor == null)
            return;
        if (floor < 1 || floor > Constants.BATTLE_STAGE_FLOOR_MAX)
            return;
        if (floor > ClearedBattleFloor[stageIndex])
            ClearedBattleFloor[stageIndex] = floor;
    }

    void NormalizeClearedBattleFloors()
    {
        if (ClearedBattleFloor == null || ClearedBattleFloor.Length != Constants.BATTLE_STAGE_NUM + 1)
            ClearedBattleFloor = new int[Constants.BATTLE_STAGE_NUM + 1];

        for (int stage = 1; stage <= Constants.BATTLE_STAGE_NUM; stage++)
        {
            if (ClearedBattleFloor[stage] < 0 || ClearedBattleFloor[stage] > Constants.BATTLE_STAGE_FLOOR_MAX)
                ClearedBattleFloor[stage] = 0;
            if (stage <= ClearedBattleStage)
                ClearedBattleFloor[stage] = Constants.BATTLE_STAGE_FLOOR_MAX;
        }
    }

    // 0.0.4 より前は ID 4 以降も初期所持だった。仲間化で持ったネコ（ID 27 以降）はそのまま残す
    void DropFormerStarterOwnership()
    {
        if (!SaveClass.IsOlderThan(SaveClass.ReadDataVersion(), "0.0.4"))
            return;

        CharacterDefinition[] roster = CharacterRoster.All;
        for (int i = 0; i < roster.Length; i++)
        {
            CharacterDefinition def = roster[i];
            if (def.StartsOwned || def.Id >= 27)
                continue;
            if (def.Id < 1 || def.Id > Constants.CHARACTERS_ALL_NUM)
                continue;

            CharacterClass character = CharactersAll[def.Id];
            if (character == null || character.ID != def.Id)
                continue;

            character.OwnedNumCur = 0;
            character.OwnedNumMax = 0;
            character.Whereabouts = Place.None;
            ReleaseCharacterFromProduction(def.Id);
            ClearBattlePartyCharacter(def.Id);
        }

        SaveGame();
    }

    void ClearBattlePartyCharacter(uint characterId)
    {
        for (int setIndex = 1; setIndex <= Constants.BATTLE_PARTY_SET_NUM; setIndex++)
        {
            for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
            {
                for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
                {
                    if (BattlePartyCharacterIds[setIndex, x, y] == characterId)
                        BattlePartyCharacterIds[setIndex, x, y] = 0;
                }
            }
        }
    }

    //ステージ1は最初から開放。以降は、1つ前をクリアしているときだけ開放
    public bool IsBattleStageUnlocked(int stageIndex)
    {
        return stageIndex >= 1 && stageIndex <= Constants.BATTLE_STAGE_NUM
            && stageIndex <= ClearedBattleStage + 1;
    }

    //出撃中のステージをクリアしたことにする。次のステージが開放される
    public bool MarkBattleStageCleared()
    {
        if (!IsBattleStageUnlocked(ActiveBattleStage))
            return false;
        if (ActiveBattleStage > ClearedBattleStage)
            ClearedBattleStage = ActiveBattleStage;
        MarkBattleFloorCleared(ActiveBattleStage, Constants.BATTLE_STAGE_FLOOR_MAX);
        return true;
    }

    bool HasBattlePartyMember(int setIndex)
    {
        if (setIndex < 1 || setIndex > Constants.BATTLE_PARTY_SET_NUM)
            return false;

        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                if (BattlePartyCharacterIds[setIndex, x, y] != 0)
                    return true;
            }
        }
        return false;
    }

    public bool RequestBattleWithdraw()
    {
        return ControllerBattle != null && ControllerBattle.RequestWithdraw();
    }

    public void LeaveBattle()
    {
        for (int i = 1; i <= Constants.CHARACTERS_ALL_NUM; i++)
        {
            if (CharactersAll[i] != null && CharactersAll[i].Whereabouts == Place.Battle)
                CharactersAll[i].Whereabouts = Place.None;
        }
        ActiveBattlePartySet = 0;
        ActiveBattleStage = 0;
        ActiveBattleFloorFrom = 0;
        ActiveBattleFloorTo = 0;
    }

    public bool IsBattleRepeatOn()
    {
        return ControllerBattleStage != null && ControllerBattleStage.IsRepeatOn();
    }

    public bool BeginAutoBattle()
    {
        if (ControllerBattle == null)
            return false;
        if (ControllerBattle.IsRunning())
            return true;
        if (ActiveBattleStage == 0 || ActiveBattlePartySet == 0)
            return false;
        ControllerBattle.Begin(ActiveBattleStage, ActiveBattlePartySet, ActiveBattleFloorFrom, ActiveBattleFloorTo);
        return ControllerBattle.IsRunning();
    }

    public BattleBalanceConfig GetBattleBalance()
    {
        return BattleBalance;
    }

    public CharacterClass GetCharacter(uint characterId)
    {
        if (characterId == 0 || characterId > Constants.CHARACTERS_ALL_NUM)
            return null;
        return CharactersAll[characterId];
    }

    public string GetRgbText()
    {
        return "R " + CurR.ToString() + "   G " + CurG.ToString() + "   B " + CurB.ToString();
    }

    public void AddBattleRgb(CharacterAttribute attribute, long amount)
    {
        long r;
        long g;
        long b;
        BattleRewardCalculator.SplitRgb(attribute, amount, out r, out g, out b);
        AddRgbChannel(ref CurR, MaxR, r);
        AddRgbChannel(ref CurG, MaxG, g);
        AddRgbChannel(ref CurB, MaxB, b);
    }

    public void AddBattlePixels(int r, int g, int b, long count)
    {
        if (CurPixels == null || count <= 0)
            return;
        if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
            return;
        ulong add = (ulong)count;
        ulong current = CurPixels[r, g, b];
        if (ulong.MaxValue - current < add)
            CurPixels[r, g, b] = ulong.MaxValue;
        else
            CurPixels[r, g, b] = current + add;
    }

    void AddRgbChannel(ref ulong current, ulong max, long amount)
    {
        if (amount <= 0)
            return;
        ulong add = (ulong)amount;
        if (ModelProduction != null)
            ModelProduction.Increase(ref current, add, max);
        else if (current >= max)
            current = max;
        else if (max - current < add)
            current = max;
        else
            current += add;
    }

    public void AddBattleItem(int itemId, int count)
    {
        if (count <= 0)
            return;
        int current = 0;
        ItemCounts.TryGetValue(itemId, out current);
        long sum = (long)current + count;
        if (sum > int.MaxValue)
            sum = int.MaxValue;
        ItemCounts[itemId] = (int)sum;
    }

    public int GrantBattleExp(BattleUnit unit, long gain, BattleBalanceConfig config)
    {
        if (unit == null || !unit.IsAlly || config == null)
            return 0;
        CharacterClass character = GetCharacter(unit.CharacterId);
        if (character == null || character.ID != unit.CharacterId)
            return 0;

        long level = (long)character.Level;
        long exp = (long)character.Exp;
        long expMax = (long)character.ExpMax;
        long previousHpMax = unit.HpMax;
        int levels = BattleExperience.Add(ref level, ref exp, ref expMax, gain, config, character.OpaquePixelCount());

        if (level < 0)
            level = 0;
        if (exp < 0)
            exp = 0;
        if (expMax < 0)
            expMax = 0;
        character.Level = (ulong)level;
        character.Exp = (ulong)exp;
        character.ExpMax = (ulong)expMax;
        unit.Level = (int)level;
        if (levels > 0)
        {
            character.RebuildLevelStats(config);
            long newMax = (long)character.Stats[0].HPMax;
            long delta = newMax - previousHpMax;
            unit.HpMax = newMax;
            if (delta > 0)
                unit.Hp += delta;
            if (unit.Hp > unit.HpMax)
                unit.Hp = unit.HpMax;
            if (unit.HpMax < 1)
                unit.HpMax = 1;
            unit.Atk = (long)character.Stats[0].ATK;
            unit.Def = (long)character.Stats[0].DEF;
            unit.Spd = character.Stats[0].SPD;
            if (unit.Spd < 1)
                unit.Spd = 1;
        }
        return levels;
    }

    public string RecruitFromBattle(uint characterId, List<BattleUnit> units)
    {
        CharacterClass character = GetCharacter(characterId);
        if (character == null || character.ID != characterId)
            return null;

        int lives = character.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)character.OwnedNumCur;
        bool alreadyOwned = lives > 0;
        if (units != null)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].IsAlly && units[i].CharacterId == characterId)
                {
                    lives = units[i].Lives;
                    alreadyOwned = true;
                    break;
                }
            }
        }

        if (lives < BattleBalance.MaxLives)
            lives++;
        int ownedBeforeRecruit = character.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)character.OwnedNumCur;
        character.SetOwnedCur(lives, BattleBalance.MaxLives);
        if (units != null)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].IsAlly && units[i].CharacterId == characterId)
                    units[i].Lives = (int)character.OwnedNumCur;
            }
        }
        int ownedAfterRecruit = character.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)character.OwnedNumCur;
        if (ownedAfterRecruit != ownedBeforeRecruit)
            ShowCharacterOwnedNum();
        if (!alreadyOwned)
            return character.Name + " が仲間になった";
        return character.Name + " の残機が1増えた";
    }

    public void SyncAllyLivesFromBattle(List<BattleUnit> units)
    {
        if (units == null)
            return;
        bool ownedChanged = false;
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (!unit.IsAlly)
                continue;
            CharacterClass character = GetCharacter(unit.CharacterId);
            if (character == null || character.ID != unit.CharacterId)
                continue;
            int lives = unit.Lives;
            if (lives < 0)
                lives = 0;
            if (lives > BattleBalance.MaxLives)
                lives = BattleBalance.MaxLives;
            unit.Lives = lives;
            int ownedBefore = character.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)character.OwnedNumCur;
            character.SetOwnedCur(lives, BattleBalance.MaxLives);
            unit.Lives = (int)character.OwnedNumCur;
            if ((int)character.OwnedNumCur != ownedBefore)
                ownedChanged = true;
        }
        if (ownedChanged)
            ShowCharacterOwnedNum();
    }

    void ReleaseCharacterFromProduction(uint characterId)
    {
        for (int i = 1; i <= Constants.CHARACTERS_HELP_PRODUCTION_NUM; i++)
        {
            if (CharactersIDHelpProductionR[i] == characterId)
                CharactersIDHelpProductionR[i] = 0;
            if (CharactersIDHelpProductionG[i] == characterId)
                CharactersIDHelpProductionG[i] = 0;
            if (CharactersIDHelpProductionB[i] == characterId)
                CharactersIDHelpProductionB[i] = 0;
        }

        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_PIXEL_NUM; i++)
        {
            if (CharactersIDProductionPixel[i] == characterId)
                CharactersIDProductionPixel[i] = 0;
        }

        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM; i++)
        {
            if (CharactersIDProductionCharacter[i] == characterId)
                CharactersIDProductionCharacter[i] = 0;
        }
    }

    void RefreshProductionAssignmentViews()
    {
        UpdateRGBProductionHelpCharacter();
        ClearEmptyHelpProductionButton("ButtonRProductionHelpCharacter", CharactersIDHelpProductionR, new Color(50 / 255f, 0.0f, 0.0f, 1.0f));
        ClearEmptyHelpProductionButton("ButtonGProductionHelpCharacter", CharactersIDHelpProductionG, new Color(0.0f, 50 / 255f, 0.0f, 1.0f));
        ClearEmptyHelpProductionButton("ButtonBProductionHelpCharacter", CharactersIDHelpProductionB, new Color(0.0f, 0.0f, 50 / 255f, 1.0f));
        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_PIXEL_NUM; i++)
            UpdateCharacterPixelProduction(i);

        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM; i++)
        {
            if (CharactersIDProductionCharacter[i] != 0)
                continue;
            GameObject buttonObject = GameObject.Find("ButtonCharacterProductionCharacter" + i.ToString("00"));
            if (buttonObject == null)
                continue;
            Button button = buttonObject.GetComponent<Button>();
            button.image.sprite = null;
            button.GetComponentInChildren<Text>().text = "+";
        }
    }

    void ClearEmptyHelpProductionButton(string buttonPrefix, uint[] characterIds, Color emptyColor)
    {
        for (int i = 1; i <= Constants.CHARACTERS_HELP_PRODUCTION_NUM; i++)
        {
            if (characterIds[i] != 0)
                continue;
            GameObject buttonObject = GameObject.Find(buttonPrefix + i.ToString());
            if (buttonObject == null)
                continue;
            Button button = buttonObject.GetComponent<Button>();
            button.image.sprite = null;
            button.image.color = emptyColor;
            button.GetComponentInChildren<Text>().text = "+";
        }
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

    CharacterDefinition[] roster = CharacterRoster.All;
    for (int rosterIndex = 0; rosterIndex < roster.Length; rosterIndex++)
    {
        string curCharacterName = roster[rosterIndex].FileName;
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
                    RememberPixelSpend(argIndex, 1, tmpR);
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
                    RememberPixelSpend(argIndex, 2, tmpG);
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
                    RememberPixelSpend(argIndex, 3, tmpB);
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
                ClearPixelSpend(argIndex);

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
    //色が変わったときだけ、途中の消費RGBを戻して進捗を捨てる。同じ色なら何もしない
    void ApplyPixelProductionColor(int argIndex, Color nextColor)
    {
        if (!SamePixelColor(ColorProductionPixel[argIndex], nextColor))
            RefundPixelProductionProgress(argIndex);

        ColorProductionPixel[argIndex].r = nextColor.r;
        ColorProductionPixel[argIndex].g = nextColor.g;
        ColorProductionPixel[argIndex].b = nextColor.b;
        ColorProductionPixel[argIndex].a = 1.0f;
        UpdateRGBProductionScene();
    }

    static bool SamePixelColor(Color a, Color b)
    {
        return PixelChannel(a.r) == PixelChannel(b.r)
            && PixelChannel(a.g) == PixelChannel(b.g)
            && PixelChannel(a.b) == PixelChannel(b.b);
    }

    static int PixelChannel(float value)
    {
        int channel = (int)(value * 255f);
        if (channel < 0)
            return 0;
        if (channel > 255)
            return 255;
        return channel;
    }

    void RememberPixelSpend(int argIndex, int channel, ulong amount)
    {
        ulong current = SpentProductionPixel[argIndex, channel];
        if (ulong.MaxValue - current < amount)
            SpentProductionPixel[argIndex, channel] = ulong.MaxValue;
        else
            SpentProductionPixel[argIndex, channel] = current + amount;
    }

    void ClearPixelSpend(int argIndex)
    {
        SpentProductionPixel[argIndex, 1] = 0;
        SpentProductionPixel[argIndex, 2] = 0;
        SpentProductionPixel[argIndex, 3] = 0;
    }

    void RefundPixelProductionProgress(int argIndex)
    {
        AddRgbStock(ref CurR, SpentProductionPixel[argIndex, 1]);
        AddRgbStock(ref CurG, SpentProductionPixel[argIndex, 2]);
        AddRgbStock(ref CurB, SpentProductionPixel[argIndex, 3]);
        ProgressProductionPixel[argIndex, 1] = 0;
        ProgressProductionPixel[argIndex, 2] = 0;
        ProgressProductionPixel[argIndex, 3] = 0;
        ClearPixelSpend(argIndex);
    }

    static void AddRgbStock(ref ulong current, ulong amount)
    {
        if (amount == 0)
            return;
        if (ulong.MaxValue - current < amount)
            current = ulong.MaxValue;
        else
            current += amount;
    }

    //古いセーブは消費量が無い。今の担当キャラの生産数で、色が0でないチャンネルだけ見積もる
    void SeedPixelSpendFromProgress()
    {
        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_PIXEL_NUM; i++)
        {
            if (SpentProductionPixel[i, 1] != 0 || SpentProductionPixel[i, 2] != 0 || SpentProductionPixel[i, 3] != 0)
                continue;

            uint characterId = CharactersIDProductionPixel[i];
            if (characterId == 0 || characterId > Constants.CHARACTERS_ALL_NUM || CharactersAll[characterId] == null)
                continue;

            int r = PixelChannel(ColorProductionPixel[i].r);
            int g = PixelChannel(ColorProductionPixel[i].g);
            int b = PixelChannel(ColorProductionPixel[i].b);
            uint create = CharactersAll[characterId].GetCreatePixels((ushort)r, (ushort)g, (ushort)b);
            if (r > 0)
                SpentProductionPixel[i, 1] = (ulong)ProgressProductionPixel[i, 1] * create;
            if (g > 0)
                SpentProductionPixel[i, 2] = (ulong)ProgressProductionPixel[i, 2] * create;
            if (b > 0)
                SpentProductionPixel[i, 3] = (ulong)ProgressProductionPixel[i, 3] * create;
        }
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
            B.image.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionR[1]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionR[1]].Size, CharactersAll[CharactersIDHelpProductionR[1]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionR[2] != 0)
        {
            Button B = GameObject.Find("ButtonRProductionHelpCharacter2").GetComponent<Button>();
            B.image.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionR[2]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionR[2]].Size, CharactersAll[CharactersIDHelpProductionR[2]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionR[3] != 0)
        {
            Button B = GameObject.Find("ButtonRProductionHelpCharacter3").GetComponent<Button>();
            B.image.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionR[3]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionR[3]].Size, CharactersAll[CharactersIDHelpProductionR[3]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }

        if (CharactersIDHelpProductionG[1] != 0)
        {
            Button B = GameObject.Find("ButtonGProductionHelpCharacter1").GetComponent<Button>();
            B.image.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionG[1]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionG[1]].Size, CharactersAll[CharactersIDHelpProductionG[1]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionG[2] != 0)
        {
            Button B = GameObject.Find("ButtonGProductionHelpCharacter2").GetComponent<Button>();
            B.image.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionG[2]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionG[2]].Size, CharactersAll[CharactersIDHelpProductionG[2]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionG[3] != 0)
        {
            Button B = GameObject.Find("ButtonGProductionHelpCharacter3").GetComponent<Button>();
            B.image.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionG[3]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionG[3]].Size, CharactersAll[CharactersIDHelpProductionG[3]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }

        if (CharactersIDHelpProductionB[1] != 0)
        {
            Button B = GameObject.Find("ButtonBProductionHelpCharacter1").GetComponent<Button>();
            B.image.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionB[1]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionB[1]].Size, CharactersAll[CharactersIDHelpProductionB[1]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionB[2] != 0)
        {
            Button B = GameObject.Find("ButtonBProductionHelpCharacter2").GetComponent<Button>();
            B.image.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionB[2]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionB[2]].Size, CharactersAll[CharactersIDHelpProductionB[2]].Size), new Vector2(0.5f, 0.5f));
            B.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            B.GetComponentInChildren<Text>().text = "";
        }
        if (CharactersIDHelpProductionB[3] != 0)
        {
            Button B = GameObject.Find("ButtonBProductionHelpCharacter3").GetComponent<Button>();
            B.image.sprite = Sprite.Create(CharactersAll[CharactersIDHelpProductionB[3]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDHelpProductionB[3]].Size, CharactersAll[CharactersIDHelpProductionB[3]].Size), new Vector2(0.5f, 0.5f));
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
    //RGB、ピクセル在庫、キャラ生産の消費数など、今出ている数値だけ書き直す。マスの作り直しはしない
    public void RefreshProductionFigures()
    {
        UpdateRGBProductionSliderOneColor(TextR, CurR, MaxR, SliderR, SliderCostIncreaseValueRUp, SliderCostMaxRUp);
        UpdateRGBProductionSliderOneColor(TextG, CurG, MaxG, SliderG, SliderCostIncreaseValueGUp, SliderCostMaxGUp);
        UpdateRGBProductionSliderOneColor(TextB, CurB, MaxB, SliderB, SliderCostIncreaseValueBUp, SliderCostMaxBUp);
        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_PIXEL_NUM; i++)
            UpdateSliderPixelProduction(i);
        RefreshPixelListCounts();
        UpdateProductionCharacterConsumeViews();
    }

    void RefreshPixelListCounts()
    {
        GameObject content = GameObject.Find("ContentPixelList");
        if (content == null || CurPixels == null)
            return;
        for (int i = 0; i < content.transform.childCount; i++)
        {
            Text[] texts = content.transform.GetChild(i).GetComponentsInChildren<Text>();
            if (texts.Length < 6)
                continue;
            int r;
            int g;
            int b;
            if (!int.TryParse(texts[0].text, out r) || !int.TryParse(texts[2].text, out g) || !int.TryParse(texts[4].text, out b))
                continue;
            if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
                continue;
            texts[5].text = CurPixels[r, g, b].ToString();
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
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum] = Instantiate((GameObject)Resources.Load("PrefabShowPixelColorAndNum"), GameObjectContentPixelList.transform) as GameObject;

                ArrayShowPixelColorAndNum[R % colNum, G % rowNum].GetComponentsInChildren<Image>()[1].color = new Color(RColor / 255f, GColor / 255f, BColor / 255f, 1.0f);
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum].GetComponentsInChildren<Text>()[0].text = RColor.ToString();
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum].GetComponentsInChildren<Text>()[2].text = GColor.ToString();
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum].GetComponentsInChildren<Text>()[4].text = BColor.ToString();
                ArrayShowPixelColorAndNum[R % colNum, G % rowNum].GetComponentsInChildren<Text>()[5].text = CurPixels[RColor, GColor, BColor].ToString();
            }

        }

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
            Content.GetComponentsInChildren<Button>()[0].GetComponentInChildren<Text>().text = "+";
        }
        else
        {
            //キャラクター
            Content.GetComponentsInChildren<Button>()[0].image.sprite
                = Sprite.Create(CharactersAll[CharactersIDProductionPixel[argIndex]].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[CharactersIDProductionPixel[argIndex]].Size, CharactersAll[CharactersIDProductionPixel[argIndex]].Size), new Vector2(0.5f, 0.5f));
            Content.GetComponentsInChildren<Button>()[0].GetComponentInChildren<Text>().text = "";
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
        uint pixelCount = 0;
        if (CharactersIDProductionPixel[argIndex] != 0)
            pixelCount = CharactersAll[CharactersIDProductionPixel[argIndex]].GetCreatePixels((ushort)(ColorProductionPixel[argIndex].r * 255), (ushort)(ColorProductionPixel[argIndex].g * 255), (ushort)(ColorProductionPixel[argIndex].b * 255));

        Transform colorPanel = Content.transform.Find("PanelImagePixelColor");
        SetPixelChannelLine(colorPanel, "ImagePixelColorR", "R", ColorProductionPixel[argIndex].r, pixelCount);
        SetPixelChannelLine(colorPanel, "ImagePixelColorG", "G", ColorProductionPixel[argIndex].g, pixelCount);
        SetPixelChannelLine(colorPanel, "ImagePixelColorB", "B", ColorProductionPixel[argIndex].b, pixelCount);
        SetPixelStockLabel(Content, argIndex);
    }

    //色ボタンの下に、今の在庫と、この生産が1回終わったあとの在庫を出す
    void SetPixelStockLabel(GameObject content, int argIndex)
    {
        Transform colorButton = FindPixelColorButton(content.transform, argIndex);
        if (colorButton == null)
            return;

        Transform labelTransform = content.transform.Find("TextPixelStock");
        Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : CreatePixelStockLabel(content.transform, colorButton);
        if (label == null)
            return;

        int r = (int)(ColorProductionPixel[argIndex].r * 255f);
        int g = (int)(ColorProductionPixel[argIndex].g * 255f);
        int b = (int)(ColorProductionPixel[argIndex].b * 255f);
        if (r < 0) r = 0;
        if (g < 0) g = 0;
        if (b < 0) b = 0;
        if (r > 255) r = 255;
        if (g > 255) g = 255;
        if (b > 255) b = 255;

        ulong stock = CurPixels[r, g, b];
        uint add = 0;
        if (CharactersIDProductionPixel[argIndex] != 0)
            add = CharactersAll[CharactersIDProductionPixel[argIndex]].GetCreatePixels((ushort)r, (ushort)g, (ushort)b);
        ulong after = stock > ulong.MaxValue - add ? ulong.MaxValue : stock + add;
        label.text = "在庫 " + stock.ToString() + " → " + after.ToString();
    }

    static Transform FindPixelColorButton(Transform content, int argIndex)
    {
        Transform button = content.Find("ButtonPixelProductionPixelColor" + argIndex.ToString("00"));
        if (button != null)
            return button;
        button = content.Find("ButtonPixelProductionPixelColor");
        if (button != null)
            return button;

        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            if (child.name.StartsWith("ButtonPixelProductionPixelColor"))
                return child;
        }
        return null;
    }

    static Text CreatePixelStockLabel(Transform content, Transform colorButton)
    {
        return CreateUnderButtonLabel(content, colorButton, "TextPixelStock");
    }

    static Text CreateUnderButtonLabel(Transform content, Transform anchor, string objectName)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform));
        labelObject.transform.SetParent(content, false);

        RectTransform buttonRect = anchor as RectTransform;
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = buttonRect.anchorMin;
        rect.anchorMax = buttonRect.anchorMax;
        rect.pivot = new Vector2(0.5f, 1f);
        float height = buttonRect.rect.height;
        if (height <= 0f)
            height = buttonRect.sizeDelta.y;
        float halfHeight = height * buttonRect.pivot.y;
        rect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, buttonRect.anchoredPosition.y - halfHeight - 2f);
        rect.sizeDelta = new Vector2(Mathf.Max(buttonRect.rect.width, 160f), 36f);
        rect.SetAsLastSibling();

        Text source = anchor.GetComponentInChildren<Text>();
        Text label = labelObject.AddComponent<Text>();
        label.font = source != null && source.font != null
            ? source.font
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 16;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 10;
        label.resizeTextMaxSize = 18;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    static void SetPixelChannelLine(Transform colorPanel, string imageName, string channel, float color01, uint pixelCount)
    {
        if (colorPanel == null)
            return;
        Transform image = colorPanel.Find(imageName);
        if (image == null)
            return;
        Text text = image.GetComponent<Image>().GetComponentInChildren<Text>();
        if (text == null)
            return;

        int value = Mathf.RoundToInt(color01 * 255f);
        if (value < 0)
            value = 0;
        if (value > 255)
            value = 255;
        long total = (long)value * pixelCount;
        text.text = channel + " " + value.ToString() + "\n"
            + "× " + pixelCount.ToString() + " px\n"
            + "= " + total.ToString();
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
        SetPixelStockLabel(Content, argIndex);
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

    //起動時など、進捗画像・消費ピクセル・所持数をまとめて描く
    public void UpdateProductionCharacterScene()
    {
        UpdateProductionCharacterButtons();
        UpdateAllProductionCharacterProgressImages();
        UpdateProductionCharacterConsumeViews();
        ShowCharacterOwnedNum();
    }

    //作成担当キャラと作成するキャラのボタンを、保存されている割り当てに合わせる
    void UpdateProductionCharacterButtons()
    {
        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM; i++)
        {
            UpdateProductionCharacterButton("ButtonCharacterProductionCharacter" + i.ToString("00"), CharactersIDProductionCharacter[i]);
            UpdateProductionCharacterButton("ButtonCharacterProducedCharacter" + i.ToString("00"), CharactersIDProducedCharacter[i]);
            SetCharacterStockLabel(i);
        }
    }

    void UpdateCharacterProductionStockLabels()
    {
        for (int i = 1; i <= Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM; i++)
            SetCharacterStockLabel(i);
    }

    //作る対象ボタンの下に、今の所持数と、1体できたあとの所持数を出す
    void SetCharacterStockLabel(int argIndex)
    {
        if (argIndex < 1 || argIndex > Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM)
            return;
        GameObject buttonObject = GameObject.Find("ButtonCharacterProducedCharacter" + argIndex.ToString("00"));
        if (buttonObject == null)
            return;

        Transform row = buttonObject.transform.parent;
        Transform labelTransform = row.Find("TextCharacterStock");
        Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : CreateUnderButtonLabel(row, buttonObject.transform, "TextCharacterStock");
        if (label == null)
            return;

        uint characterId = CharactersIDProducedCharacter[argIndex];
        if (characterId == 0 || characterId > Constants.CHARACTERS_ALL_NUM || CharactersAll[characterId] == null)
        {
            label.text = "";
            label.gameObject.SetActive(false);
            return;
        }

        label.gameObject.SetActive(true);
        ulong stock = CharactersAll[characterId].OwnedNumCur;
        int cap = BattleBalance.MaxLives;
        ulong after = stock;
        if (cap < 0 || stock < (ulong)cap)
        {
            after = stock == ulong.MaxValue ? stock : stock + 1;
            if (cap >= 0 && after > (ulong)cap)
                after = (ulong)cap;
        }
        label.text = "在庫 " + stock.ToString() + " → " + after.ToString();
    }

    void UpdateProductionCharacterButton(string argButtonName, uint argCharacterID)
    {
        GameObject buttonObject = GameObject.Find(argButtonName);
        if (buttonObject == null)
            return;
        Button button = buttonObject.GetComponent<Button>();

        if (argCharacterID == 0 || CharactersAll[argCharacterID].ImageTexture2D == null)
        {
            button.image.sprite = null;
            button.GetComponentInChildren<Text>().text = "+";
            return;
        }

        button.image.sprite = Sprite.Create(CharactersAll[argCharacterID].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[argCharacterID].Size, CharactersAll[argCharacterID].Size), new Vector2(0.5f, 0.5f));
        button.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        button.GetComponentInChildren<Text>().text = "";
    }

    void ForEachProductionCharacterView(Action<int, RawImage, Transform> visit)
    {
        GameObject productionList = null;
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("CharacterProduction");
        foreach (GameObject taggedObject in taggedObjects)
        {
            if (taggedObject.name.Equals("ContentCharacterProductionList"))
            {
                productionList = taggedObject;
                break;
            }
        }
        if (productionList == null)
            return;

        foreach (Transform slot in productionList.transform)
        {
            RawImage progressImage = null;
            Transform consumeContent = null;
            int productionIndex = -1;
            foreach (Transform child in slot)
            {
                if (child.name.Contains("RawImageCharacterProduction"))
                {
                    productionIndex = int.Parse(child.name.Substring(child.name.Length - 1, 1));
                    progressImage = child.GetComponent<RawImage>();
                }
                if (child.name.Contains("ScrollViewConsumePixel"))
                {
                    if (productionIndex < 0)
                        productionIndex = int.Parse(child.name.Substring(child.name.Length - 2, 2));
                    consumeContent = FindConsumePixelContent(child);
                }
            }
            if (productionIndex < 0)
                continue;
            visit(productionIndex, progressImage, consumeContent);
        }
    }

    Transform FindConsumePixelContent(Transform scrollView)
    {
        foreach (Transform viewport in scrollView)
        {
            if (!viewport.name.Contains("Viewport"))
                continue;
            foreach (Transform content in viewport)
            {
                if (content.name.Contains("Content"))
                    return content;
            }
        }
        return null;
    }

    void UpdateAllProductionCharacterProgressImages()
    {
        ForEachProductionCharacterView((productionIndex, progressImage, consumeContent) =>
        {
            if (progressImage != null)
                AssignProductionCharacterProgressImage(productionIndex, progressImage);
        });
    }

    void UpdateProductionCharacterProgressImage(int productionIndex)
    {
        ForEachProductionCharacterView((index, progressImage, consumeContent) =>
        {
            if (index == productionIndex && progressImage != null)
                AssignProductionCharacterProgressImage(productionIndex, progressImage);
        });
    }

    void AssignProductionCharacterProgressImage(int productionIndex, RawImage progressImage)
    {
        if (productionIndex < 0 || productionIndex >= ProgressTextureProductionCharacter.Count)
            return;

        uint characterId = CharactersIDProducedCharacter[productionIndex];
        if (characterId == 0 || ProgressTextureProductionCharacter[productionIndex] == null)
        {
            Texture oldView = ProductionCharacterViewTexture[productionIndex];
            ProductionCharacterViewTexture[productionIndex] = null;
            progressImage.texture = null;
            if (oldView != null)
                Destroy(oldView);
            return;
        }

        Texture2D sourceTexture = CharactersAll[characterId].ImageTexture2D;
        if (sourceTexture == null)
            return;

        Texture view = ImagegUtility.BoolArrayTOTexture(ProgressTextureProductionCharacter[productionIndex],
            sourceTexture, new Color(1f, 1f, 1f, 1f), new Color(0.45f, 0.45f, 0.45f, 1f));
        Texture previous = ProductionCharacterViewTexture[productionIndex];
        ProductionCharacterViewTexture[productionIndex] = view;
        progressImage.texture = view;
        if (previous != null)
            Destroy(previous);
    }

    void UpdateProductionCharacterConsumeViews()
    {
        ForEachProductionCharacterView((productionIndex, progressImage, consumeContent) =>
        {
            if (consumeContent == null)
                return;
            if (productionIndex < 0 || productionIndex >= ConsumePixelsProductionCharacter.Length)
                return;
            ApplyConsumePixelList(consumeContent, productionIndex);
        });
    }

    void ApplyConsumePixelList(Transform content, int productionIndex)
    {
        List<ConsumePixelClass> pixels = ConsumePixelsProductionCharacter[productionIndex];
        if (content.childCount != pixels.Count)
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);

            for (int i = 0; i < pixels.Count; i++)
            {
                GameObject row = Instantiate((GameObject)Resources.Load("PrefabShowPixelColorAndNum"), content) as GameObject;
                ApplyConsumePixelRow(row, pixels[i]);
            }
            return;
        }

        for (int i = 0; i < pixels.Count; i++)
            ApplyConsumePixelRow(content.GetChild(i).gameObject, pixels[i]);
    }

    void ApplyConsumePixelRow(GameObject row, ConsumePixelClass consumePixel)
    {
        row.GetComponentsInChildren<Image>()[1].color = consumePixel.PixelColor;
        Text[] texts = row.GetComponentsInChildren<Text>();
        texts[0].text = (consumePixel.PixelColor.r * 255).ToString();
        texts[2].text = (consumePixel.PixelColor.g * 255).ToString();
        texts[4].text = (consumePixel.PixelColor.b * 255).ToString();
        int r = (int)(consumePixel.PixelColor.r * 255);
        int g = (int)(consumePixel.PixelColor.g * 255);
        int b = (int)(consumePixel.PixelColor.b * 255);
        texts[5].text = consumePixel.CurConsumePixelsNum.ToString() + " / " + consumePixel.ToBeCurConsumePixelsNum.ToString() + " / " + CurPixels[r, g, b].ToString();
        uint remaining = consumePixel.ToBeCurConsumePixelsNum - consumePixel.CurConsumePixelsNum;
        texts[5].color = CurPixels[r, g, b] < remaining ? new Color(1, 0, 0, 1) : new Color(0, 0, 0, 1);
    }

    //TODO:TODO!
    //キャラクター生産のキャラクター所持数の表示
    public void ShowCharacterOwnedNum()
    {
        UpdateCharacterProductionStockLabels();

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

        if (gameObjectCharacterList == null)
            return;

        Transform list = gameObjectCharacterList.transform;
        for (int childIndex = list.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = list.GetChild(childIndex);
            if (!child.name.StartsWith("OwnedCharacter"))
                Destroy(child.gameObject);
        }

        int shown = 0;
        bool added = false;
        for (int i = 1; i <= Constants.CHARACTERS_ALL_NUM; i++)
        {
            if (CharactersAll[i] == null || CharactersAll[i].OwnedNumMax == 0 || CharactersAll[i].ImageTexture2D == null)
                continue;

            string buttonName = "OwnedCharacter" + i.ToString();
            Transform existing = list.Find(buttonName);
            if (existing != null)
            {
                Text label = OwnedCountLabel(existing);
                if (label != null)
                    label.text = "OwnedNum : " + CharactersAll[i].OwnedNumCur;
                existing.SetSiblingIndex(shown);
                shown++;
                continue;
            }

            GameObject characterButton = Instantiate((GameObject)Resources.Load("PrefabButtonCharacterImage"), list) as GameObject;
            characterButton.name = buttonName;
            characterButton.transform.SetSiblingIndex(shown);
            characterButton.GetComponentInChildren<Image>().sprite = Sprite.Create(CharactersAll[i].ImageTexture2D, new UnityEngine.Rect(0, 0, CharactersAll[i].Size, CharactersAll[i].Size), new Vector2(0.5f, 0.5f));
            Text createdLabel = characterButton.GetComponentInChildren<Text>();
            createdLabel.text = "OwnedNum : " + CharactersAll[i].OwnedNumCur;
            createdLabel.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            if (CharactersAll[i].Whereabouts == Place.CreateR ||
                CharactersAll[i].Whereabouts == Place.CreateG ||
                CharactersAll[i].Whereabouts == Place.CreateB ||
                CharactersAll[i].Whereabouts == Place.CreatePixel ||
                CharactersAll[i].Whereabouts == Place.CreateCharacter)
                ControllerCharacterSelectClass.AddFaintWarningMark(characterButton);
            shown++;
            added = true;
        }

        if (added)
            LayoutRebuilder.ForceRebuildLayoutImmediate(list as RectTransform);
    }

    static Text OwnedCountLabel(Transform button)
    {
        Text[] labels = button.GetComponentsInChildren<Text>();
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name != "ProductionWarning")
                return labels[i];
        }
        return null;
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