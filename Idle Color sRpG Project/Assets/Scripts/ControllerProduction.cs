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

public enum Trigger {User, Update};

static class Constants
{
    public const int CHARACTERS_ALL_NUM = 32;
    public const int CHARACTERS_HELP_PRODUCTION_NUM = 3;
    public const int PRODUCTION_SLOT_MAX = 255;
    public const int PRODUCTION_SLOT_INITIAL = 3;
    public const int QUEST_PRODUCTION_SLOT_REWARD = 3;
    public const int AD_PRODUCTION_SLOT_BASE = 1;
    public const int IMPORT_CHARACTER_CAP_INITIAL = 3;
    public const int CHARACTERS_PRODUCTION_PIXEL_NUM = PRODUCTION_SLOT_MAX;
    public const int CHARACTERS_PRODUCTION_CHARACTER_NUM = PRODUCTION_SLOT_MAX;
    public const int BATTLE_PARTY_SET_NUM = 5;
    public const int BATTLE_FORMATION_SIZE = 3;
    public const int BATTLE_STAGE_NUM = 5;
    public const int BATTLE_STAGE_GALLERY = 6;
    public const int BATTLE_STAGE_FLOOR_MAX = 100;
    public const int BATTLE_STAGE_FLOOR_STEP = 10;
    public const int BATTLE_PERSON_SLOT_NUM = 3;
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
    ControllerImportCharacterClass ControllerImport;
    GameObject PanelCatalog;
    GameObject PanelFusion;
    GameObject PanelImport;
    readonly List<GameObject> SelectScenePage1Buttons = new List<GameObject>();
    readonly List<GameObject> SelectScenePage2Buttons = new List<GameObject>();
    int SelectScenePage;
    Button ButtonSelectScenePagePrev;
    Button ButtonSelectScenePageNext;
    GameObject ButtonImportCharacter;
    Button ButtonFusion;
    Color FusionButtonImageColor = Color.white;
    Color FusionButtonTextColor = Color.white;
    bool FusionButtonColorsStored;
    readonly Dictionary<string, Color> FeatureButtonImageColors = new Dictionary<string, Color>();
    readonly Dictionary<string, Color> FeatureButtonTextColors = new Dictionary<string, Color>();
    BattleBalanceConfig BattleBalance = BattleBalanceConfig.CreateDefault();
    Dictionary<int, int> ItemCounts = new Dictionary<int, int>();
    bool SaveBlocked;
    bool SaveFailureNotified;
    GameObject SaveNotice;

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
    PersonClass[] People = PersonClass.CreateRoster();
    PersonBalanceConfig PersonBalance = PersonBalanceConfig.CreateDefault();
    uint[] PersonIdHelpProductionR = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] PersonIdHelpProductionG = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] PersonIdHelpProductionB = new uint[Constants.CHARACTERS_HELP_PRODUCTION_NUM + 1];
    uint[] PersonIdProductionPixel = new uint[Constants.CHARACTERS_PRODUCTION_PIXEL_NUM + 1];
    uint[] PersonIdProductionCharacter = new uint[Constants.CHARACTERS_PRODUCTION_CHARACTER_NUM + 1];
    uint[,] BattlePartyPersonIds = new uint[Constants.BATTLE_PARTY_SET_NUM + 1, Constants.BATTLE_PERSON_SLOT_NUM + 1];
    int ActiveBattlePartySet = 0;
    int ActiveBattleStage = 0;
    int ActiveBattleFloorFrom = 0;
    int ActiveBattleFloorTo = 0;
    // いま戦っている階。0 は範囲の最初から。クリア直後の保存は次の階
    int ActiveBattleFloor = 0;
    // 1, 2, 4, 8, 16。出撃が終わっても残す。上限は BattleSpeedCap
    int BattleSpeed = 1;
    int BattleSpeedCap = 16;
    bool AdWatchConfirm = true;
    // ステージ1の50階をクリアすると使える。出撃が終わっても残す
    bool BattleRepeat;
    int ClearedBattleStage = 0;
    //ステージごとの最高クリア階。0は未クリア。開始階はこの直後の10階区切りまで
    int[] ClearedBattleFloor = new int[Constants.BATTLE_STAGE_GALLERY + 1];
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
    readonly ProductionSlotState Slots = new ProductionSlotState();
    readonly QuestSave Quests = new QuestSave();
    bool _adBusy;

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
#if UNITY_EDITOR || UNITY_ANDROID
        AdService.Begin();
#endif

        ControllerCharacterSelect = GetComponent<ControllerCharacterSelectClass>();

        PersonBalance.PosterizeLevels = GameConfig.GRADATION_LEVELS;
        ControllerCharacterSelect.Initialize(ref CharactersAll,
                                             ref ColorProductionPixel,
                                             ref CharactersIDHelpProductionR, ref CharactersIDHelpProductionG, ref CharactersIDHelpProductionB,
                                             ref CharactersIDProductionPixel,
                                             ref CharactersIDProductionCharacter, ref CharactersIDProducedCharacter,
                                             ref PersonIdHelpProductionR, ref PersonIdHelpProductionG, ref PersonIdHelpProductionB,
                                             ref PersonIdProductionPixel, ref PersonIdProductionCharacter,
                                             People, PersonBalance);
        ControllerCharacterSelect.SetPersonSortieCheck(IsPersonSortieing);

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
        //ImportedCharacters.FindAny(CharactersAll, (uint)(1)).MakeCharacter(Resources.Load("Character/RedSlime8", typeof(Texture2D)) as Texture2D, 1, "LittleRedSlime");
        //ImportedCharacters.FindAny(CharactersAll, (uint)(1)).MakeCharacter(Application.dataPath + "/Resources/Character/RedSlime8", 1, "LittleRedSlime");
        //ImportedCharacters.FindAny(CharactersAll, (uint)(1)).MakeCharacter("Character/RedSlime8", 1, "LittleRedSlime");
        //ImportedCharacters.FindAny(CharactersAll, (uint)(1)).MakeCharacter(Application.dataPath + "/Resources/" + "Character/RedSlime8" + ".png", 1, "LittleRedSlime");
        string acceptedVersion;
        bool acceptedSave = TryRestoreSave(out acceptedVersion);
        bool rebuildImages = acceptedSave
            && !string.IsNullOrEmpty(acceptedVersion)
            && SaveClass.IsOlderThan(acceptedVersion, GameConfig.ImageDataVersion);

        //ImportedCharacters.FindAny(CharactersAll, (uint)(32)).MakeCharacter(Application.persistentDataPath + "/Character/wanwan" + ".png", 32, "wanwan");

        //ImportedCharacters.FindAny(CharactersAll, (uint)(1)).MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/RedSlime8", typeof(Texture2D)) as Texture2D, 1, "LittleRedSlime");
        //ImportedCharacters.FindAny(CharactersAll, (uint)(2)).MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/GreenSlime8", typeof(Texture2D)) as Texture2D, 2, "LittleGreenSlime");
        //ImportedCharacters.FindAny(CharactersAll, (uint)(3)).MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/BlueSlime8", typeof(Texture2D)) as Texture2D, 3, "LittleBlueSlime");
        //ImportedCharacters.FindAny(CharactersAll, (uint)(4)).MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/WhiteSlime8", typeof(Texture2D)) as Texture2D, 4, "LittleWhiteSlime");
        //ImportedCharacters.FindAny(CharactersAll, (uint)(5)).MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/RBlackCat8", typeof(Texture2D)) as Texture2D, 5, "LittleRBlackCat");
        //ImportedCharacters.FindAny(CharactersAll, (uint)(8)).MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/WhiteCat8", typeof(Texture2D)) as Texture2D, 8, "LittleWhiteCat");
        //ImportedCharacters.FindAny(CharactersAll, (uint)(32)).MakeCharacter(Resources.Load(Application.streamingAssetsPath + "/Character/wanwan", typeof(Texture2D)) as Texture2D, 32, "wanwan");

        for (int i = 1; i <= Constants.CHARACTERS_ALL_NUM; i++)
        {
            if (ImportedCharacters.FindAny(CharactersAll, (uint)(i)) != null && ImportedCharacters.FindAny(CharactersAll, (uint)(i)).ID != 0)
                ImportedCharacters.FindAny(CharactersAll, (uint)(i)).RecalculateBaseStats(BattleBalance);
        }
        ImportedCharacters.Restore(BattleBalance);

        if (ActiveBattlePartySet == 0 || !IsKnownBattleStage(ActiveBattleStage))
            ActiveBattleStage = 0;
        if (ActiveBattlePartySet == 0 || !IsSortieFloorRange(ActiveBattleStage, ActiveBattleFloorFrom, ActiveBattleFloorTo))
        {
            ActiveBattleFloorFrom = 0;
            ActiveBattleFloorTo = 0;
        }
        if (ClearedBattleStage < 0 || ClearedBattleStage > Constants.BATTLE_STAGE_NUM)
            ClearedBattleStage = 0;
        NormalizeClearedBattleFloors();
        DropFormerStarterOwnership(acceptedVersion);
        if (acceptedSave && SaveClass.IsOlderThan(acceptedVersion, "0.0.6"))
            SaveGame();
        EnsureQuestDefaults();
        for (int i = 1; i <= Constants.CHARACTERS_ALL_NUM; i++)
        {
            if (CharactersAll[i] != null)
                CharactersAll[i].EnsureStock();
        }
        NormalizePersonAssignments();
        ReleasePeopleFromProduction(ActiveBattlePartySet);
        SeedPixelSpendFromProgress();

        if (rebuildImages)
        {
            Debug.Log("セーブの版が古いので、減色画像を作り直す");
            RebuildNormalizedImages();
        }

        for (int i = 1; i <= Slots.CharacterSlots; i++)
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
        ControllerBattle.ApplySavedSpeed(BattleSpeed);

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

        ControllerImport = GetComponent<ControllerImportCharacterClass>();
        if (ControllerImport == null)
            ControllerImport = gameObject.AddComponent<ControllerImportCharacterClass>();
        if (PanelRGBProduction != null)
        {
            ControllerImport.Initialize(this, PanelRGBProduction);
            PanelImport = ControllerImport.Panel;
            if (PanelImport != null)
                NotShowPanel(PanelImport);
        }
        WireSelectScenePages();
        ApplyMainFeatureLocks();
        if (ControllerBattle != null)
            ControllerBattle.BringQuestForward();

        PushButtonSelectSceneRGBProduction();
        if (ControllerBattle != null)
            ControllerBattle.Hide();
        ResumeSortie();

        Debug.Log("ControllerProduction End");
    }

    // Update is called once per frame
    private float TimeOut = 1;
    private float TimeElapsed = 0;
    struct PendingRgbFx
    {
        public string ButtonName;
        public bool Hop;
        public Color Tint;
        public ulong Amount;
    }
    readonly List<PendingRgbFx> _pendingRgbFx = new List<PendingRgbFx>(9);
    Action<int, ulong, bool> _onRgbSlot;
    string _rgbFxPrefix;
    Color _rgbFxTint;
    void Update()
    {
        TimeElapsed += Time.deltaTime;

        if (TimeElapsed >= TimeOut)
        {
            TimeElapsed = 0;
            if (_onRgbSlot == null)
                _onRgbSlot = NoteRgbSlot;
            _pendingRgbFx.Clear();

            //Rの生産
            _rgbFxPrefix = "ButtonRProductionHelpCharacter";
            _rgbFxTint = RgbProduceFx.ChannelColor(0);
            ulong IncreaseValueRHelp = RgbSlotOutput(PersonIdHelpProductionR, CharactersIDHelpProductionR, 0, _onRgbSlot);
            GainRgb(ref CurR, MaxR, IncreaseValueRHelp, 0);
            UpdateRGBProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);

            //Gの生産
            _rgbFxPrefix = "ButtonGProductionHelpCharacter";
            _rgbFxTint = RgbProduceFx.ChannelColor(1);
            ulong IncreaseValueGHelp = RgbSlotOutput(PersonIdHelpProductionG, CharactersIDHelpProductionG, 1, _onRgbSlot);
            GainRgb(ref CurG, MaxG, IncreaseValueGHelp, 1);
            UpdateRGBProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);

            //Bの生産
            _rgbFxPrefix = "ButtonBProductionHelpCharacter";
            _rgbFxTint = RgbProduceFx.ChannelColor(2);
            ulong IncreaseValueBHelp = RgbSlotOutput(PersonIdHelpProductionB, CharactersIDHelpProductionB, 2, _onRgbSlot);
            GainRgb(ref CurB, MaxB, IncreaseValueBHelp, 2);
            UpdateRGBProductionOneColor(CurB, MaxB, TextB, SliderB, IncreaseValueB, CostIncreaseValueBUp, TextIncreaseValueBLeft, TextIncreaseValueBRight, TextCostIncreaseValueBUp, SliderCostIncreaseValueBUp, ButtonIncreaseValueBUp, CostMaxBUp, TextCostMaxBUp, SliderCostMaxBUp, ButtonMaxBUp);

            //ピクセルの生産
            for (int i = 1; i <= Slots.PixelSlots; i++)
                ProductionPixel(Trigger.Update, i, out _);
            UpdateRGBProductionScene();
            RefreshPixelAttention();

            //キャラクター生産
            bool ProductionCharacterFlag = false;
            bool RepeatComplete = true;
            for (int i = 1; i <= Slots.CharacterSlots; i++)
            {
                uint personId = PersonIdProductionCharacter[i];
                CharacterClass productionWorker = personId == 0 ? CharacterOf(CharactersIDProductionCharacter[i]) : null;
                if (CharactersIDProducedCharacter[i] == 0)
                    continue;
                if (personId == 0 && (CharactersIDProductionCharacter[i] == 0 || productionWorker == null))
                    continue;

                int paintRate;
                if (personId != 0)
                {
                    PersonClass painter = PersonAt((int)personId);
                    CharacterClass producedNow = CharacterOf(CharactersIDProducedCharacter[i]);
                    if (painter == null || producedNow == null)
                        continue;
                    bool author = producedNow.AuthorPersonId == painter.Id;
                    long amount = painter.CharacterAmount(author, PersonBalance);
                    if (amount < 1)
                        amount = 1;
                    if (amount > int.MaxValue)
                        amount = int.MaxValue;
                    paintRate = (int)amount;
                }
                else
                    paintRate = productionWorker.PaintPixels > int.MaxValue ? int.MaxValue : (int)productionWorker.PaintPixels;

                bool PCF = false;
                bool RC = true;
                bool painted = false;
                ProductionCharacter(Trigger.Update, i,
                    (uint)paintRate, out PCF, out RC, out painted);
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
            if (ControllerBattle != null)
                ControllerBattle.RefreshQuestBoard();

            //絵を塗り直したあとで出す。跳ねる絵は今のスプライトを使う
            PlayPendingRgbFx();
        }
    }

    void NoteRgbSlot(int index, ulong amount, bool person)
    {
        if (amount == 0 || string.IsNullOrEmpty(_rgbFxPrefix))
            return;
        _pendingRgbFx.Add(new PendingRgbFx
        {
            ButtonName = _rgbFxPrefix + index.ToString(),
            Hop = !person,
            Tint = _rgbFxTint,
            Amount = amount
        });
    }

    void PlayPendingRgbFx()
    {
        if (_pendingRgbFx.Count == 0)
            return;
        if (!RgbPanelShown())
        {
            _pendingRgbFx.Clear();
            return;
        }
        for (int i = 0; i < _pendingRgbFx.Count; i++)
        {
            PendingRgbFx fx = _pendingRgbFx[i];
            string buttonName = fx.ButtonName;
            bool hop = fx.Hop;
            Color tint = fx.Tint;
            ulong amount = fx.Amount;
            ForEachNamedButton(buttonName, button => RgbProduceFx.Play(button, hop, tint, amount));
        }
        _pendingRgbFx.Clear();
    }

    bool RgbPanelShown()
    {
        if (PanelRGBProduction == null)
            return false;
        CanvasGroup group = PanelRGBProduction.GetComponent<CanvasGroup>();
        return group != null && group.alpha > 0.01f;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //セーブ機能

    //セーブボタンが押されたら
    public void PushButtonSave()
    {
        if (!SaveGame())
            return;
        GameObject button = GameObject.Find("ButtonSave");
        if (button == null)
            return;
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.text = Application.persistentDataPath + "/ICS.csv";
    }

    public bool SaveGame()
    {
        if (SaveBlocked)
            return false;
        EnsureQuestDefaults();
        LatchQuests();
        SaveClass SC = new SaveClass();
        bool saved = SC.Save(CharactersAll, Constants.CHARACTERS_ALL_NUM + 1,
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
            ActiveBattleFloor,
            BattleSpeed,
            BattleSpeedCap,
            AdWatchConfirm,
            BattleRepeat,
            ClearedBattleStage,
            ClearedBattleFloor,
            ItemCounts,
            CapturePeople(),
            Slots,
            Quests
            );
        if (saved)
        {
            SaveFailureNotified = false;
            HideSaveNotice();
            return true;
        }
        if (!SaveFailureNotified)
        {
            SaveFailureNotified = true;
            ShowSaveNotice("保存できませんでした。以前のセーブは残っています。");
        }
        return false;
    }

    PersonSaveData CapturePeople()
    {
        return new PersonSaveData
        {
            People = People,
            HelpR = PersonIdHelpProductionR,
            HelpG = PersonIdHelpProductionG,
            HelpB = PersonIdHelpProductionB,
            Pixel = PersonIdProductionPixel,
            Character = PersonIdProductionCharacter,
            BattlePeople = BattlePartyPersonIds
        };
    }

    static void ClearNormalizedCharacterImages()
    {
        DeleteNormalizedImages(Application.persistentDataPath + "/Character/Nomalization");
        DeleteNormalizedImages(Application.persistentDataPath + "/Character/Custom/Nomalization");
    }

    static void DeleteNormalizedImages(string dir)
    {
        if (!Directory.Exists(dir))
            return;
        string[] files;
        try
        {
            files = Directory.GetFiles(dir, "Nomalization_*.png");
        }
        catch (Exception exception)
        {
            Debug.LogError("減色画像を確認できませんでした。 " + exception.Message);
            return;
        }
        for (int i = 0; i < files.Length; i++)
        {
            try
            {
                File.Delete(files[i]);
            }
            catch (Exception exception)
            {
                Debug.LogError("減色画像を消せませんでした。 " + exception.Message);
            }
        }
    }

    void RebuildNormalizedImages()
    {
        ClearNormalizedCharacterImages();
        CharacterDefinition[] roster = CharacterRoster.All;
        for (int i = 0; i < roster.Length; i++)
        {
            CharacterDefinition definition = roster[i];
            if (definition.Id < 1 || definition.Id >= CharactersAll.Length)
                continue;
            CharacterClass character = CharactersAll[definition.Id];
            if (character == null || character.ID != definition.Id)
                continue;
            string source = Application.persistentDataPath + "/Character/" + definition.FileName;
            if (!File.Exists(source))
                continue;
            try
            {
                if (character.ReloadSourceImage(source))
                    character.RecalculateBaseStats(BattleBalance);
            }
            catch (Exception exception)
            {
                Debug.LogError("減色画像を作り直せませんでした。 " + exception.Message);
            }
        }
        ImportedCharacters.ForEach(character =>
        {
            if (character == null || character.ID < ImportedCharacters.FirstId)
                return;
            string source = ImportedCharacters.OriginalPath(character.ID);
            if (!File.Exists(source))
                return;
            try
            {
                if (character.ReloadSourceImage(source))
                    character.RecalculateBaseStats(BattleBalance);
            }
            catch (Exception exception)
            {
                Debug.LogError("減色画像を作り直せませんでした。 " + exception.Message);
            }
        });
    }

    //デリートセーブボタンが押されたら
    public void PushButtonDeleteSave()
    {
        SaveFiles.DeleteManaged();
        SaveBlocked = true;
        SaveFailureNotified = true;
        HideSaveNotice();
        if (AdConfirm != null)
            AdConfirm.SetActive(false);
        ShowRestartLock();
    }

    public void ConfirmDeleteSave(Action onDeleted)
    {
        ShowConfirm("本当にセーブデータを消しますか？", () =>
        {
            PushButtonDeleteSave();
            if (onDeleted != null)
                onDeleted();
        });
    }

    bool TryRestoreSave(out string version)
    {
        version = "";
        List<SaveFiles.SaveCandidate> candidates;
        if (!SaveFiles.TryListCandidates(out candidates))
        {
            ResetBaseline();
            BlockSaves("セーブフォルダを確認できませんでした。セーブは行いません。");
            return false;
        }
        if (candidates == null || candidates.Count == 0)
        {
            ResetBaseline();
            SaveBlocked = false;
            return false;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            SaveFiles.SaveCandidate candidate = candidates[i];
            SaveFiles.SaveInfo info;
            if (!SaveFiles.TryValidate(candidate.Path, out info))
            {
                Debug.LogWarning("セーブを検証できなかったので退避します。 " + candidate.Path);
                SaveFiles.TryQuarantine(candidate.Path);
                continue;
            }

            ResetBaseline();
            bool applied = false;
            try
            {
                SaveClass save = new SaveClass();
                applied = save.Load(ref CharactersAll, Constants.CHARACTERS_ALL_NUM + 1,
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
                    ref ActiveBattleFloor,
                    ref BattleSpeed,
                    ref BattleSpeedCap,
                    ref AdWatchConfirm,
                    ref BattleRepeat,
                    ref ClearedBattleStage,
                    ref ClearedBattleFloor,
                    ref ItemCounts,
                    CapturePeople(),
                    Slots,
                    Quests,
                    candidate.Path);
            }
            catch (Exception exception)
            {
                Debug.LogError("セーブの適用に失敗しました。 " + exception.Message);
                applied = false;
            }
            if (!applied)
            {
                Debug.LogWarning("セーブを適用できなかったので次の候補を見ます。 " + candidate.Path);
                continue;
            }

            version = info.DataVersion ?? "";
            SaveBlocked = false;
            SetBattleSpeed(BattleSpeed);
            return true;
        }

        ResetBaseline();
        BlockSaves("セーブを読み込めませんでした。以前のファイルは残しています。セーブは、削除ボタンを押すまで行いません。");
        return false;
    }

    void ResetBaseline()
    {
        if (CharactersAll != null)
        {
            for (int i = 0; i < CharactersAll.Length; i++)
            {
                if (CharactersAll[i] == null)
                    CharactersAll[i] = new CharacterClass();
                else
                    CharactersAll[i].ClearProgressionForReload();
            }
        }
        ImportedCharacters.ClearForReload();
        CharacterRoster.RegisterAll(CharactersAll);

        ClearArray(CharactersIDHelpProductionR);
        ClearArray(CharactersIDHelpProductionG);
        ClearArray(CharactersIDHelpProductionB);
        ClearArray(CharactersIDProductionPixel);
        ClearArray(CharactersIDProductionCharacter);
        ClearArray(CharactersIDProducedCharacter);
        ClearArray(PersonIdHelpProductionR);
        ClearArray(PersonIdHelpProductionG);
        ClearArray(PersonIdHelpProductionB);
        ClearArray(PersonIdProductionPixel);
        ClearArray(PersonIdProductionCharacter);
        Slots.Reset();
        Quests.Reset();
        FusionBonus.ExtraPercent = 0;
        ImportedCharacters.SetImportCap(Constants.IMPORT_CHARACTER_CAP_INITIAL);
        if (BattlePartyCharacterIds != null)
            Array.Clear(BattlePartyCharacterIds, 0, BattlePartyCharacterIds.Length);
        if (BattlePartyPersonIds != null)
            Array.Clear(BattlePartyPersonIds, 0, BattlePartyPersonIds.Length);
        if (ProgressProductionPixel != null)
            Array.Clear(ProgressProductionPixel, 0, ProgressProductionPixel.Length);
        if (SpentProductionPixel != null)
            Array.Clear(SpentProductionPixel, 0, SpentProductionPixel.Length);
        if (ClearedBattleFloor != null)
            Array.Clear(ClearedBattleFloor, 0, ClearedBattleFloor.Length);
        if (CurPixels != null)
            Array.Clear(CurPixels, 0, CurPixels.Length);
        if (ColorProductionPixel != null)
        {
            for (int i = 0; i < ColorProductionPixel.Length; i++)
                ColorProductionPixel[i] = new Color(0f, 0f, 0f, 1f);
        }
        if (ItemCounts != null)
            ItemCounts.Clear();

        PersonClass[] freshPeople = PersonClass.CreateRoster();
        if (People != null && freshPeople != null)
        {
            int count = People.Length < freshPeople.Length ? People.Length : freshPeople.Length;
            for (int i = 0; i < count; i++)
                People[i] = freshPeople[i];
            for (int i = count; i < People.Length; i++)
                People[i] = null;
        }

        CurR = 0;
        CurG = 0;
        CurB = 0;
        MaxR = 1024;
        MaxG = 1024;
        MaxB = 1024;
        CostMaxRUp = 512;
        CostMaxGUp = 512;
        CostMaxBUp = 512;
        IncreaseValueR = 1;
        IncreaseValueG = 1;
        IncreaseValueB = 1;
        CostIncreaseValueRUp = 16;
        CostIncreaseValueGUp = 16;
        CostIncreaseValueBUp = 16;
        ActiveBattlePartySet = 0;
        ActiveBattleStage = 0;
        ActiveBattleFloorFrom = 0;
        ActiveBattleFloorTo = 0;
        ActiveBattleFloor = 0;
        BattleSpeed = 1;
        BattleSpeedCap = 16;
        AdWatchConfirm = true;
        BattleRepeat = false;
        ClearedBattleStage = 0;

        if (ConsumePixelsProductionCharacter != null)
        {
            for (int i = 0; i < ConsumePixelsProductionCharacter.Length; i++)
            {
                if (ConsumePixelsProductionCharacter[i] == null)
                    ConsumePixelsProductionCharacter[i] = new List<ConsumePixelClass>();
                else
                    ConsumePixelsProductionCharacter[i].Clear();
            }
        }
        if (ProgressTextureProductionCharacter != null)
        {
            for (int i = 0; i < ProgressTextureProductionCharacter.Count; i++)
                ProgressTextureProductionCharacter[i] = null;
        }
    }

    static void ClearArray(uint[] values)
    {
        if (values != null)
            Array.Clear(values, 0, values.Length);
    }

    void BlockSaves(string message)
    {
        SaveBlocked = true;
        ShowSaveNotice(message);
    }

    GameObject RestartLock;

    void ShowRestartLock()
    {
        if (RestartLock != null)
        {
            RestartLock.SetActive(true);
            return;
        }
        GameObject canvas = GameObject.Find("CanvasProduction");
        if (canvas == null)
            return;

        RestartLock = new GameObject("RestartLock", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
        RestartLock.transform.SetParent(canvas.transform, false);
        RectTransform root = RestartLock.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.localScale = Vector3.one;
        Canvas overlay = RestartLock.GetComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 5000;
        Image dim = RestartLock.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        dim.raycastTarget = true;

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(RestartLock.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(820f, 360f);
        card.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 1f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(card.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(36f, 28f);
        textRect.offsetMax = new Vector2(-36f, -28f);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = "アプリを再起動してください";
    }

    void ShowSaveNotice(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;
        Debug.LogError(message);
        if (SaveNotice != null)
        {
            Text current = SaveNotice.GetComponentInChildren<Text>();
            if (current != null)
                current.text = message;
            SaveNotice.SetActive(true);
            return;
        }

        GameObject canvas = GameObject.Find("CanvasProduction");
        if (canvas == null)
            return;

        SaveNotice = new GameObject("SaveNotice", typeof(RectTransform));
        SaveNotice.transform.SetParent(canvas.transform, false);
        RectTransform rect = SaveNotice.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.06f, 0.72f);
        rect.anchorMax = new Vector2(0.94f, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        Image image = SaveNotice.AddComponent<Image>();
        Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
        image.sprite = Sprite.Create(pixel, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        image.color = new Color(0.08f, 0.05f, 0.05f, 0.92f);
        image.raycastTarget = false;

        GameObject textObject = new GameObject("SaveNoticeText", typeof(RectTransform));
        textObject.transform.SetParent(SaveNotice.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 16f);
        textRect.offsetMax = new Vector2(-24f, -16f);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = message;
        SaveNotice.transform.SetAsLastSibling();
    }

    void HideSaveNotice()
    {
        if (SaveNotice != null)
            SaveNotice.SetActive(false);
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //RGB生産

    // ButtonRが押されたら
    public void PushButtonR()
    {
        Debug.Log("ButtonRが押された");
        GainRgb(ref CurR, MaxR, IncreaseValueR, 0);
        UpdateRGBProductionOneColor(CurR, MaxR, TextR, SliderR, IncreaseValueR, CostIncreaseValueRUp, TextIncreaseValueRLeft, TextIncreaseValueRRight, TextCostIncreaseValueRUp, SliderCostIncreaseValueRUp, ButtonIncreaseValueRUp, CostMaxRUp, TextCostMaxRUp, SliderCostMaxRUp, ButtonMaxRUp);
    }
    // ButtonGが押されたら
    public void PushButtonG()
    {
        Debug.Log("ButtonGが押された");
        GainRgb(ref CurG, MaxG, IncreaseValueG, 1);
        UpdateRGBProductionOneColor(CurG, MaxG, TextG, SliderG, IncreaseValueG, CostIncreaseValueGUp, TextIncreaseValueGLeft, TextIncreaseValueGRight, TextCostIncreaseValueGUp, SliderCostIncreaseValueGUp, ButtonIncreaseValueGUp, CostMaxGUp, TextCostMaxGUp, SliderCostMaxGUp, ButtonMaxGUp);
    }
    // ButtonBが押されたら
    public void PushButtonB()
    {
        Debug.Log("ButtonBが押された");
        GainRgb(ref CurB, MaxB, IncreaseValueB, 2);
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
    static Sprite RgbPanelBackgroundSprite;

    public void PushButtonSelectSceneRGBProduction()
    {
        EnsureRgbPanelBackground();
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
        NotShowImportPanel();
    }

    //ピクセル生産シーンボタンが押されたら
    public void PushButtonSelectScenePixelProduction()
    {
        if (!IsMainFeatureOpen(MainFeature.PixelScreen))
            return;

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
        NotShowImportPanel();
    }

    //キャラクター生産シーンボタンが押されたら
    public void PushButtonSelectSceneCharacterProduction()
    {
        if (!IsMainFeatureOpen(MainFeature.CharacterScreen))
            return;

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
        NotShowImportPanel();
        UpdateProductionCharacterConsumeViews();
        UpdateCharacterProductionStockLabels();
    }

    //バトル編成シーンボタンが押されたら
    public void PushButtonSelectSceneBattleParty()
    {
        if (!IsMainFeatureOpen(MainFeature.Battle))
            return;

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
        NotShowImportPanel();
    }

    //バトルステージ選択シーンボタンが押されたら
    public void PushButtonSelectSceneBattleStage()
    {
        if (!IsMainFeatureOpen(MainFeature.Battle))
            return;
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
        NotShowImportPanel();
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
        NotShowImportPanel();
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
        NotShowImportPanel();
    }

    void NotShowCatalogPanel()
    {
        if (PanelCatalog != null)
            NotShowPanel(PanelCatalog);
    }

    public void PushButtonSelectSceneFusion()
    {
        if (!Quests.FusionUnlocked)
            return;
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
        NotShowImportPanel();
    }

    void NotShowFusionPanel()
    {
        if (PanelFusion != null)
            NotShowPanel(PanelFusion);
    }

    public void PushButtonSelectSceneImport()
    {
        if (PanelImport == null)
            return;

        ShowPanel(PanelImport);
        if (ControllerImport != null)
            ControllerImport.Open();

        NotShowPanel(PanelRGBProduction);
        NotShowPanel(PanelPixelProduction);
        ClearPixelListPixelProduction();
        NotShowPanel(PanelCharacterProduction);
        NotShowPanel(PanelBattleParty);
        NotShowBattleStagePanel();
        NotShowCatalogPanel();
        NotShowFusionPanel();
    }

    void NotShowImportPanel()
    {
        if (ControllerImport != null)
            ControllerImport.Close();
        if (PanelImport != null)
            NotShowPanel(PanelImport);
    }

    void WireSelectScenePages()
    {
        GameObject bar = GameObject.Find("PanelSelectScene");
        if (bar == null)
        {
            Debug.LogWarning("PanelSelectScene が見つかりません");
            return;
        }

        ButtonSelectScenePagePrev = FindSceneBarButton(bar.transform, "ButtonSelectScenePagePrev");
        ButtonSelectScenePageNext = FindSceneBarButton(bar.transform, "ButtonSelectScenePageNext");
        Button importButton = null;
        for (int i = 0; i < bar.transform.childCount; i++)
        {
            Transform child = bar.transform.GetChild(i);
            if (!child.name.StartsWith("ButtonImportCharacter"))
                continue;
            Button found = child.GetComponent<Button>();
            if (found == null)
                continue;
            importButton = found;
            if (child.name.Contains("Page2"))
                break;
        }
        if (importButton == null)
            importButton = EnsureSceneBarButton(bar.transform, "ButtonImportCharacter", "読み込み", Vector2.zero, new Vector2(180f, 128f), new Color(0.42f, 0.32f, 0.68f, 1f));
        ButtonImportCharacter = importButton != null ? importButton.gameObject : null;
        if (ButtonSelectScenePagePrev == null)
            Debug.LogWarning("PanelSelectScene の子に ButtonSelectScenePagePrev がありません");
        if (ButtonSelectScenePageNext == null)
            Debug.LogWarning("PanelSelectScene の子に ButtonSelectScenePageNext がありません");

        if (ButtonSelectScenePagePrev != null)
        {
            ButtonSelectScenePagePrev.onClick.RemoveListener(ShowSelectScenePagePrev);
            ButtonSelectScenePagePrev.onClick.AddListener(ShowSelectScenePagePrev);
        }
        if (ButtonSelectScenePageNext != null)
        {
            ButtonSelectScenePageNext.onClick.RemoveListener(ShowSelectScenePageNext);
            ButtonSelectScenePageNext.onClick.AddListener(ShowSelectScenePageNext);
        }
        if (importButton != null)
        {
            importButton.onClick.RemoveListener(PushButtonSelectSceneImport);
            importButton.onClick.AddListener(PushButtonSelectSceneImport);
        }

        SelectScenePage1Buttons.Clear();
        SelectScenePage2Buttons.Clear();
        for (int i = 0; i < bar.transform.childCount; i++)
        {
            Transform child = bar.transform.GetChild(i);
            if (!child.gameObject.activeSelf || child.GetComponent<Button>() == null)
                continue;
            if (child.name == "ButtonSelectScenePageNext")
                continue;
            if (IsSelectScenePage2(child.name))
                SelectScenePage2Buttons.Add(child.gameObject);
            else
                SelectScenePage1Buttons.Add(child.gameObject);
        }

        ShowSelectScenePage(0);
    }

    static bool IsSelectScenePage2(string name)
    {
        return name == "ButtonSelectScenePagePrev"
            || name == "ButtonImportCharacter"
            || name.Contains("Page2");
    }

    void ShowSelectScenePagePrev()
    {
        ShowSelectScenePage(SelectScenePage - 1);
    }

    void ShowSelectScenePageNext()
    {
        ShowSelectScenePage(SelectScenePage + 1);
    }

    void ShowSelectScenePage(int page)
    {
        if (page < 0)
            page = 0;
        if (page > 1)
            page = 1;
        SelectScenePage = page;
        bool first = page == 0;
        for (int i = 0; i < SelectScenePage1Buttons.Count; i++)
        {
            if (SelectScenePage1Buttons[i] != null)
                SelectScenePage1Buttons[i].SetActive(first);
        }
        for (int i = 0; i < SelectScenePage2Buttons.Count; i++)
        {
            if (SelectScenePage2Buttons[i] != null)
                SelectScenePage2Buttons[i].SetActive(!first);
        }
        if (ButtonSelectScenePageNext != null)
            ButtonSelectScenePageNext.gameObject.SetActive(first);
    }

    Button FindSceneBarButton(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing == null)
            return null;
        return existing.GetComponent<Button>();
    }

    Button EnsureSceneBarButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject buttonObject = existing != null ? existing.gameObject : null;
        if (buttonObject == null)
        {
            buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            buttonObject.transform.SetParent(parent, false);
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
            text.raycastTarget = false;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        if (buttonObject.GetComponent<Button>() == null)
            buttonObject.AddComponent<Button>();
        if (buttonObject.GetComponent<Image>() == null)
            buttonObject.AddComponent<Image>();

        Text labelText = buttonObject.GetComponentInChildren<Text>();
        if (labelText != null)
            labelText.text = label;
        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
            image.color = color;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return buttonObject.GetComponent<Button>();
    }

    public int ImportedCharacterCount()
    {
        return ImportedCharacters.Count;
    }

    public void ForEachImportedCharacter(System.Action<CharacterClass> visit)
    {
        ImportedCharacters.ForEach(visit);
    }

    public bool TryPrepareImport(byte[] pngBytes, out string message)
    {
        return ImportedCharacters.TryPrepare(pngBytes, ClearedBattleStage, BattleBalance, out message);
    }

    public CharacterClass PreparedImportCharacter()
    {
        return ImportedCharacters.PreparedCharacter();
    }

    public bool CommitPreparedImport(string name, out string message)
    {
        bool added = ImportedCharacters.CommitPrepared(name, out message);
        if (!added)
            return false;
        SaveGame();
        ShowCharacterOwnedNum();
        if (ControllerCatalog != null && PanelCatalog != null && PanelCatalog.GetComponent<CanvasGroup>().alpha > 0f)
            ControllerCatalog.Refresh();
        if (ControllerFusion != null && PanelFusion != null && PanelFusion.GetComponent<CanvasGroup>().alpha > 0f)
            ControllerFusion.Refresh();
        return true;
    }

    public void DiscardPreparedImport()
    {
        ImportedCharacters.DiscardPrepared();
    }

    public bool DeleteImportedCharacter(uint id, out string message)
    {
        message = null;
        CharacterClass character = ImportedCharacters.Find(id);
        if (character == null || character.ID != id)
        {
            message = "そのキャラはいません。";
            return false;
        }
        if (IsInRunningBattle(id))
        {
            message = "出撃中なので消せません。バトルを終えてから消してください。";
            return false;
        }

        string name = string.IsNullOrWhiteSpace(character.Name) ? "ななし" : character.Name.Trim();
        ReleaseCharacterFromProduction(id);
        ClearBattlePartyCharacter(id);
        ClearProducedCharacter(id);
        RefreshProductionAssignmentViews();
        RefreshProductionFigures();
        if (!ImportedCharacters.TryRemove(id))
        {
            message = "消せませんでした。";
            return false;
        }
        ShowCharacterOwnedNum();
        if (ControllerCatalog != null && PanelCatalog != null && PanelCatalog.GetComponent<CanvasGroup>().alpha > 0f)
            ControllerCatalog.Refresh();
        if (ControllerFusion != null && PanelFusion != null && PanelFusion.GetComponent<CanvasGroup>().alpha > 0f)
            ControllerFusion.Refresh();
        if (ControllerBattleParty != null && PanelBattleParty != null)
        {
            CanvasGroup party = PanelBattleParty.GetComponent<CanvasGroup>();
            if (party != null && party.alpha > 0f)
                ControllerBattleParty.Refresh();
        }
        SaveGame();
        message = name + " を消しました。";
        return true;
    }

    bool IsInRunningBattle(uint characterId)
    {
        if (!IsBattleRunning() || ActiveBattlePartySet < 1)
            return false;
        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                if (BattlePartyCharacterIds[ActiveBattlePartySet, x, y] == characterId)
                    return true;
            }
        }
        return false;
    }

    void ClearProducedCharacter(uint characterId)
    {
        for (int i = 1; i <= Slots.CharacterSlots; i++)
        {
            if (i >= CharactersIDProducedCharacter.Length || CharactersIDProducedCharacter[i] != characterId)
                continue;
            CharactersIDProducedCharacter[i] = 0;
            ReductionPixelsProductionCharacter(i);
            UpdateProductionCharacterProgressImage(i);
        }
        UpdateProductionCharacterConsumeViews();
    }

    CharacterClass CharacterOf(uint id)
    {
        return ImportedCharacters.FindAny(CharactersAll, id);
    }

    ulong HelpCreates(uint id, int channel)
    {
        CharacterClass character = CharacterOf(id);
        if (character == null || character.Stats == null || character.Stats[0] == null || character.ID == 0)
            return 0;
        if (channel == 1)
            return character.Stats[0].GCreates;
        if (channel == 2)
            return character.Stats[0].BCreates;
        return character.Stats[0].RCreates;
    }

    void WireFusionButton()
    {
        GameObject bar = GameObject.Find("PanelSelectScene");
        if (bar == null)
        {
            Debug.LogWarning("PanelSelectScene が見つかりません");
            return;
        }

        bool arrowsPlaced = bar.transform.Find("ButtonSelectScenePageNext") != null;
        if (!arrowsPlaced)
        {
            GameObject adButton = GameObject.Find("ButtonSelectScene7");
            if (adButton != null)
            {
                RectTransform adRect = adButton.GetComponent<RectTransform>();
                if (adRect != null)
                    adRect.anchoredPosition = new Vector2(0f, 4000f);
            }
        }

        Transform existing = bar.transform.Find("ButtonFusionScene");
        if (existing == null)
            existing = bar.transform.Find("ButtonFusionScenePage2");
        GameObject buttonObject = existing != null ? existing.gameObject : null;
        if (buttonObject == null && arrowsPlaced)
        {
            Debug.LogWarning("合成ボタンがありません。PanelSelectScene の子に ButtonFusionScene を置いてください");
            return;
        }
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

        if (existing == null)
        {
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(450f, 0f);
            rect.sizeDelta = new Vector2(128f, 128f);
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            return;
        button.onClick.RemoveListener(PushButtonSelectSceneFusion);
        button.onClick.AddListener(PushButtonSelectSceneFusion);
        ButtonFusion = button;
        ApplyFusionButtonLock();
    }

    // 敵を50体倒すクエストを受け取ると、合成が押せる。
    void ApplyFusionButtonLock()
    {
        if (ButtonFusion == null)
            return;

        Image image = ButtonFusion.GetComponent<Image>();
        Text label = ButtonFusion.GetComponentInChildren<Text>();
        if (!FusionButtonColorsStored)
        {
            if (image != null)
                FusionButtonImageColor = image.color;
            if (label != null)
                FusionButtonTextColor = label.color;
            FusionButtonColorsStored = true;
        }

        bool open = Quests.FusionUnlocked;
        ButtonFusion.interactable = open;
        if (image != null)
            image.color = open ? FusionButtonImageColor : new Color(0.45f, 0.45f, 0.45f, FusionButtonImageColor.a);
        if (label != null)
            label.color = open ? FusionButtonTextColor : new Color(0.62f, 0.62f, 0.62f, FusionButtonTextColor.a);
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
        if (cost > int.MaxValue)
            cost = int.MaxValue;
        if (!character.ConsumeOwned((int)cost, true, BattleBalance.MaxLives))
            return false;
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
        GameObject buttonObject = null;
        GameObject bar = GameObject.Find("PanelSelectScene");
        if (bar != null)
        {
            for (int i = 0; i < bar.transform.childCount; i++)
            {
                Transform child = bar.transform.GetChild(i);
                if (child.name.Contains("Zukan") || child.name == "ButtonSelectScene6" || child.name == "ButtonSelectScene6Page2")
                {
                    buttonObject = child.gameObject;
                    break;
                }
            }
        }
        if (buttonObject == null)
            buttonObject = GameObject.Find("ButtonSelectSceneZukan");
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

    public void NoteEnemyDefeat()
    {
        if (!Quests.DefaultsApplied || Quests.EnemyDefeats >= ulong.MaxValue)
            return;
        Quests.EnemyDefeats++;
    }

    public bool IsNextEnemyDefeatGuaranteedRecruit()
    {
        if (!Quests.DefaultsApplied || Quests.EnemyDefeats >= ulong.MaxValue)
            return false;
        return BattleRewardCalculator.IsGuaranteedRecruitCount(Quests.EnemyDefeats + 1);
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
        if (ControllerBattlePartyClass.IsPersonButton(ButtonCharacterTmp))
        {
            int slot = ControllerBattlePartyClass.PersonSlot(ButtonCharacterTmp);
            ControllerCharacterSelect.ShowPanelSelectBattlePerson(ButtonCharacterTmp,
                (int)GetBattlePartyPerson(ControllerBattleParty.GetCurrentSetIndex(), slot));
            return;
        }
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
        if (ButtonCharacterTmp != null && ControllerBattlePartyClass.IsPersonButton(ButtonCharacterTmp))
        {
            int slot = ControllerBattlePartyClass.PersonSlot(ButtonCharacterTmp);
            SetBattlePartyPerson(ControllerBattleParty.GetCurrentSetIndex(), slot, (uint)ControllerCharacterSelect.GetSelectedPersonId());
            ControllerBattleParty.Refresh();
            ButtonCharacterTmp = null;
            ControllerCharacterSelect.NotShowPanelSelectCharacter();
            NotShowPanel(PanelSelectCharacter);
            return;
        }
        if (ButtonCharacterTmp != null && ControllerCharacterSelect.GetSelectedPersonId() != 0 && IsProductionWorkerButton(ButtonCharacterTmp))
        {
            AssignProductionPerson(ButtonCharacterTmp, ControllerCharacterSelect.GetSelectedPersonId());
            RefreshProductionAssignmentViews();
            if (ButtonCharacterTmp.name.Contains("ButtonPixelProductionCharacter"))
                UpdatePixelProductionScene();
            ButtonCharacterTmp = null;
            ControllerCharacterSelect.NotShowPanelSelectCharacter();
            NotShowPanel(PanelSelectCharacter);
            return;
        }
        if (ControllerBattlePartyClass.IsCellButton(ButtonCharacterTmp))
        {
            SetBattlePartyCharacterFromSelectCharacter(ControllerCharacterSelect.GetSelectedCharacterID());
            return;
        }

        int producedIndex = -1;
        uint previousProducedId = 0;
        if (ButtonCharacterTmp.name.Contains("ButtonCharacterProducedCharacter"))
        {
            producedIndex = ProductionSlotIndex(ButtonCharacterTmp.name);
            previousProducedId = CharactersIDProducedCharacter[producedIndex];
        }

        if (ButtonCharacterTmp != null && IsProductionWorkerButton(ButtonCharacterTmp))
            ClearProductionPerson(ButtonCharacterTmp);
        ControllerCharacterSelect.ConfirmSelectCharacter(ButtonCharacterTmp);
        if (ButtonCharacterTmp != null && IsProductionWorkerButton(ButtonCharacterTmp))
            RefreshProductionAssignmentViews();

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
        if (ControllerBattlePartyClass.IsPersonButton(ButtonCharacterTmp))
        {
            int slot = ControllerBattlePartyClass.PersonSlot(ButtonCharacterTmp);
            SetBattlePartyPerson(ControllerBattleParty.GetCurrentSetIndex(), slot, 0);
            ControllerBattleParty.Refresh();
            ButtonCharacterTmp = null;
            ControllerCharacterSelect.NotShowPanelSelectCharacter();
            NotShowPanel(PanelSelectCharacter);
            return;
        }
        if (ControllerBattlePartyClass.IsCellButton(ButtonCharacterTmp))
        {
            SetBattlePartyCharacterFromSelectCharacter(0);
            return;
        }

        if (ButtonCharacterTmp != null && IsProductionWorkerButton(ButtonCharacterTmp))
            ClearProductionPerson(ButtonCharacterTmp);
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
            int ProductionIndex = ProductionSlotIndex(ButtonCharacterTmp.name);
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
        if (argButtonName != null
            && argButtonName.Contains("ButtonPixelProductionPixelColor")
            && !IsMainFeatureOpen(MainFeature.PixelTargets))
            return;

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
                    ColorTmp.r = CharacterClass.ColorUnit(Mathf.Clamp(Mathf.RoundToInt(SliderSpecificationNumR.value), 0, 255));
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
                        ColorTmp.r = CharacterClass.ColorUnit(num);
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
                    ColorTmp.g = CharacterClass.ColorUnit(Mathf.Clamp(Mathf.RoundToInt(SliderSpecificationNumG.value), 0, 255));
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
                        ColorTmp.g = CharacterClass.ColorUnit(num);
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
                    ColorTmp.b = CharacterClass.ColorUnit(Mathf.Clamp(Mathf.RoundToInt(SliderSpecificationNumB.value), 0, 255));
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
                        ColorTmp.b = CharacterClass.ColorUnit(num);
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
        int ProductionPixelIndex = ProductionSlotIndex(ButtonColorTmp.name);
        if (ProductionPixelIndex < 1)
            return;

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
                ColorTmp.r = CharacterClass.ColorUnit(ColorByte(clickColor.r));
                ColorTmp.g = CharacterClass.ColorUnit(ColorByte(clickColor.g));
                ColorTmp.b = CharacterClass.ColorUnit(ColorByte(clickColor.b));
                ControlImageSelectColor("SelectColorMethodCharacter");


                //クリックされたピクセルカラーではない部分のマスク作成
                tmpButtonCharacter.GetComponentInChildren<RawImage>().color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
                Texture2D Mask = new Texture2D(buttonImage.width, buttonImage.height, TextureFormat.ARGB32, false);
                for (int y = 0; y < buttonImage.height; y++)
                {
                    for (int x = 0; x < buttonImage.width; x++)
                    {
                        Color pixel = buttonImage.GetPixel(x, y);
                        if (pixel.a > 0.0f && ColorByte(pixel.r) == ColorByte(ColorTmp.r) && ColorByte(pixel.g) == ColorByte(ColorTmp.g) && ColorByte(pixel.b) == ColorByte(ColorTmp.b))
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
        int ProductionPixelIndex = ProductionSlotIndex(ButtonColorTmp.name);
        if (ProductionPixelIndex < 1)
            return;

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
        int ProductionPixelIndex = ProductionSlotIndex(argName);
        if (ProductionPixelIndex < 1)
            return;

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
        CharacterClass producedCharacter = CharacterOf(characterId);
        if (characterId == 0 || producedCharacter == null || producedCharacter.ID != characterId
            || producedCharacter.ListExistsColors == null || producedCharacter.ImageTexture2D == null)
            return;

        ConsumePixelsProductionCharacter[argIndex].Clear();
        foreach (ExistColor curExistColor in producedCharacter.ListExistsColors)
        {
            ConsumePixelsProductionCharacter[argIndex].Add(new ConsumePixelClass(curExistColor.Color, curExistColor.Num, 0));
        }
        ProgressTextureProductionCharacter[argIndex] = ImagegUtility.MakeSilhouetteBoolArray(producedCharacter.ImageTexture2D);
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
        CharacterClass nextCharacter = CharacterOf(nextId);
        if (nextId == 0 || nextCharacter == null || nextCharacter.ID != nextId || nextCharacter.ListExistsColors == null)
        {
            if (argIndex >= 0 && argIndex < ProgressTextureProductionCharacter.Count)
                ProgressTextureProductionCharacter[argIndex] = null;
            return;
        }

        foreach (ExistColor curExistColor in nextCharacter.ListExistsColors)
            ConsumePixelsProductionCharacter[argIndex].Add(new ConsumePixelClass(curExistColor.Color, curExistColor.Num, 0));
        ProgressTextureProductionCharacter[argIndex] = ImagegUtility.MakeSilhouetteBoolArray(nextCharacter.ImageTexture2D);
    }

    void ReturnConsumedPixel(ConsumePixelClass consumePixel)
    {
        if (consumePixel == null || consumePixel.CurConsumePixelsNum == 0 || CurPixels == null)
            return;
        int r = ColorByte(consumePixel.PixelColor.r);
        int g = ColorByte(consumePixel.PixelColor.g);
        int b = ColorByte(consumePixel.PixelColor.b);
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

        if (CharactersIDProducedCharacter[argIndex] == 0)
            return false;
        if (CharactersIDProductionCharacter[argIndex] == 0 && PersonIdProductionCharacter[argIndex] == 0)
            return false;

        if (ProgressTextureProductionCharacter[argIndex] == null)
            return false;


        int curRepeatNum = 0;
        CharacterClass producedNow = ImportedCharacters.FindAny(CharactersAll, CharactersIDProducedCharacter[argIndex]);
        if (producedNow == null)
            return false;
        Texture2D ProductionCharacterTexture2D = producedNow.ImageTexture2D;
        if (ProductionCharacterTexture2D == null)
            return false;

        for (int y = ProductionCharacterTexture2D.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < ProductionCharacterTexture2D.width; x++)
            {
                if(ProgressTextureProductionCharacter[argIndex][x,y] == false)
                {
                    Color color = ProductionCharacterTexture2D.GetPixel(x, y);
                    if (CurPixels[ColorByte(color.r), ColorByte(color.g), ColorByte(color.b)] > 0)
                    {

                        CurPixels[ColorByte(color.r), ColorByte(color.g), ColorByte(color.b)]--;
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
            CharacterClass produced = ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProducedCharacter[argIndex]));
            int ownedBefore = produced.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)produced.OwnedNumCur;
            GrantCompletedCharacter(argIndex, produced);
            int ownedAfter = produced.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)produced.OwnedNumCur;
            if (ownedAfter > ownedBefore && ControllerBattle != null)
                ControllerBattle.AddAllyLives(produced.ID, ownedAfter - ownedBefore);

            //進捗ピクセルの初期化
            ConsumePixelsProductionCharacter[argIndex].Clear();
            foreach (ExistColor curExistColor in ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProducedCharacter[argIndex])).ListExistsColors)
            {
                ConsumePixelsProductionCharacter[argIndex].Add(new ConsumePixelClass(curExistColor.Color, curExistColor.Num, 0));
            }
            //進捗画像の初期化
            ProgressTextureProductionCharacter[argIndex] = ImagegUtility.MakeSilhouetteBoolArray(ImportedCharacters.FindAny(CharactersAll, (uint)(CharactersIDProducedCharacter[argIndex])).ImageTexture2D);

            ProductionCharacterFlag = true;
        }

        return true;
    }


    //キャラクター生産でスピードアップボタンが押されたら
    public void PushButtonSpeedUpProductionCharacter(string argName)
    {
        int ProductionCharacterIndex = ProductionSlotIndex(argName);
        if (ProductionCharacterIndex < 1)
            return;

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
            CharacterClass partyCharacter = CharacterOf(characterId);
            if (partyCharacter == null || partyCharacter.ID != characterId)
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
        for (int slot = 1; slot <= Constants.BATTLE_PERSON_SLOT_NUM; slot++)
            BattlePartyPersonIds[setIndex, slot] = 0;
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
                CharacterClass battleCharacter = CharacterOf(characterId);
                if (battleCharacter != null)
                    battleCharacter.Whereabouts = Place.Battle;
            }
        }

        ReleasePeopleFromProduction(setIndex);
        ActiveBattlePartySet = setIndex;
        ActiveBattleStage = 0;
        RefreshProductionAssignmentViews();
        return true;
    }

    //ステージ選択パネルからの出撃。1人以上いるセットだけ成功する。別セットが出ていれば先に撤退する
    public bool EnterBattleStage(int stageIndex, int setIndex, int floorFrom, int floorTo)
    {
        if (!IsKnownBattleStage(stageIndex))
            return false;
        if (!CanEnterStage(stageIndex))
            return false;
        if (!IsSortieFloorRange(stageIndex, floorFrom, floorTo))
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
        ActiveBattleFloor = floorFrom;
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

    public int GetActiveBattleFloor()
    {
        return ActiveBattleFloor;
    }

    public void SetActiveBattleFloor(int floor)
    {
        ActiveBattleFloor = floor;
    }

    public int GetBattleSpeed()
    {
        return BattleSpeed;
    }

    public int GetBattleSpeedCap()
    {
        return NormalizeBattleSpeedCap(BattleSpeedCap);
    }

    public void SetBattleSpeedCap(int cap)
    {
        BattleSpeedCap = NormalizeBattleSpeedCap(cap);
        if (BattleSpeed > BattleSpeedCap)
            BattleSpeed = BattleSpeedCap;
    }

    public void SetBattleSpeed(int speed)
    {
        int cap = GetBattleSpeedCap();
        if (speed != 2 && speed != 4 && speed != 8 && speed != 16)
        {
            BattleSpeed = 1;
            return;
        }
        if (speed > cap)
            speed = cap;
        BattleSpeed = speed;
    }

    static int NormalizeBattleSpeedCap(int cap)
    {
        if (cap == 2 || cap == 4 || cap == 8 || cap == 16)
            return cap;
        return 16;
    }

    public bool GetAdWatchConfirm()
    {
        return AdWatchConfirm;
    }

    public void SetAdWatchConfirm(bool confirm)
    {
        AdWatchConfirm = confirm;
    }

    public bool GetBattleRepeat()
    {
        return BattleRepeat;
    }

    public void SetBattleRepeat(bool on)
    {
        BattleRepeat = on;
    }

    public int GetClearedBattleStage()
    {
        return ClearedBattleStage;
    }

    public int GetHighestClearedBattleFloor(int stageIndex)
    {
        if (ClearedBattleFloor == null || stageIndex < 1 || stageIndex >= ClearedBattleFloor.Length)
            return 0;
        return ClearedBattleFloor[stageIndex];
    }

    public int GetBattleFloorWindowStart(int stageIndex)
    {
        if (stageIndex != Constants.BATTLE_STAGE_GALLERY)
            return 1;
        int cleared = GetHighestClearedBattleFloor(stageIndex);
        if (cleared < 0)
            cleared = 0;
        return (cleared / Constants.BATTLE_STAGE_FLOOR_MAX) * Constants.BATTLE_STAGE_FLOOR_MAX + 1;
    }

    //クリア階の次の階が含まれる10階区切りの始まり。未クリアなら1階
    public int GetMaxBattleFloorFrom(int stageIndex)
    {
        int cleared = GetHighestClearedBattleFloor(stageIndex);
        if (stageIndex != Constants.BATTLE_STAGE_GALLERY)
            return MaxBattleFloorFrom(cleared);

        int windowStart = GetBattleFloorWindowStart(stageIndex);
        int relative = cleared - (windowStart - 1);
        if (relative < 0)
            relative = 0;
        return windowStart - 1 + MaxBattleFloorFrom(relative);
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
        if (ClearedBattleFloor == null || stageIndex < 1 || stageIndex >= ClearedBattleFloor.Length)
            return;
        if (floor < 1)
            return;
        if (stageIndex != Constants.BATTLE_STAGE_GALLERY && floor > Constants.BATTLE_STAGE_FLOOR_MAX)
            return;
        if (stageIndex == Constants.BATTLE_STAGE_GALLERY)
            NoteGalleryClear();
        if (floor > ClearedBattleFloor[stageIndex])
            ClearedBattleFloor[stageIndex] = floor;
    }

    void NormalizeClearedBattleFloors()
    {
        int need = Constants.BATTLE_STAGE_GALLERY + 1;
        if (ClearedBattleFloor == null || ClearedBattleFloor.Length < need)
        {
            int[] next = new int[need];
            if (ClearedBattleFloor != null)
            {
                int copy = ClearedBattleFloor.Length < next.Length ? ClearedBattleFloor.Length : next.Length;
                for (int i = 0; i < copy; i++)
                    next[i] = ClearedBattleFloor[i];
            }
            ClearedBattleFloor = next;
        }

        for (int stage = 1; stage <= Constants.BATTLE_STAGE_NUM; stage++)
        {
            if (ClearedBattleFloor[stage] < 0 || ClearedBattleFloor[stage] > Constants.BATTLE_STAGE_FLOOR_MAX)
                ClearedBattleFloor[stage] = 0;
            if (stage <= ClearedBattleStage)
                ClearedBattleFloor[stage] = Constants.BATTLE_STAGE_FLOOR_MAX;
        }

        if (ClearedBattleFloor[Constants.BATTLE_STAGE_GALLERY] < 0)
            ClearedBattleFloor[Constants.BATTLE_STAGE_GALLERY] = 0;
    }

    public bool IsKnownBattleStage(int stageIndex)
    {
        return (stageIndex >= 1 && stageIndex <= Constants.BATTLE_STAGE_NUM)
            || stageIndex == Constants.BATTLE_STAGE_GALLERY;
    }

    public bool CanEnterStage(int stageIndex)
    {
        if (stageIndex == Constants.BATTLE_STAGE_GALLERY)
            return HasClearedImportQuest();
        return IsBattleStageUnlocked(stageIndex);
    }

    public bool HasClearedImportQuest()
    {
        EnsureQuestDefaults();
        LatchQuests();
        QuestDef[] all = QuestCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].Metric == QuestMetric.ImportedOfSize && IsQuestCleared(all[i]))
                return true;
        }
        return false;
    }

    public void CollectImportedCharacterIds(List<uint> ids)
    {
        if (ids == null)
            return;
        ids.Clear();
        ImportedCharacters.ForEach(character =>
        {
            if (character != null && character.ID >= ImportedCharacters.FirstId)
                ids.Add(character.ID);
        });
    }

    public bool IsSortieFloorRange(int stageIndex, int floorFrom, int floorTo)
    {
        int step = Constants.BATTLE_STAGE_FLOOR_STEP;
        int start = GetBattleFloorWindowStart(stageIndex);
        int end = start + Constants.BATTLE_STAGE_FLOOR_MAX - 1;
        if (floorFrom < start || floorTo > end || floorTo < floorFrom)
            return false;
        if ((floorFrom - start) % step != 0)
            return false;
        if ((floorTo - start + 1) % step != 0)
            return false;
        return true;
    }

    // 0.0.4 より前は ID 4 以降も初期所持だった。仲間化で持ったネコ（ID 27 以降）はそのまま残す。
    // 版が空のときは所持を外さない。
    void DropFormerStarterOwnership(string savedVersion)
    {
        if (string.IsNullOrEmpty(savedVersion) || !SaveClass.IsOlderThan(savedVersion, "0.0.4"))
            return;

        CharacterDefinition[] roster = CharacterRoster.All;
        for (int i = 0; i < roster.Length; i++)
        {
            CharacterDefinition def = roster[i];
            if (def.StartsOwned || def.Id >= 27)
                continue;
            if (def.Id < 1 || def.Id > Constants.CHARACTERS_ALL_NUM)
                continue;

            CharacterClass character = ImportedCharacters.FindAny(CharactersAll, (uint)(def.Id));
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
        ApplyFusionButtonLock();
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

    public bool IsBattleRunning()
    {
        return ControllerBattle != null && ControllerBattle.IsRunning();
    }

    public void EndRunningBattle()
    {
        if (ControllerBattle != null)
            ControllerBattle.EndRunningBattle();
    }

    public void LeaveBattle()
    {
        for (int i = 1; i <= Constants.CHARACTERS_ALL_NUM; i++)
        {
            if (ImportedCharacters.FindAny(CharactersAll, (uint)(i)) != null && ImportedCharacters.FindAny(CharactersAll, (uint)(i)).Whereabouts == Place.Battle)
                ImportedCharacters.FindAny(CharactersAll, (uint)(i)).Whereabouts = Place.None;
        }
        ImportedCharacters.ForEach(character =>
        {
            if (character != null && character.Whereabouts == Place.Battle)
                character.Whereabouts = Place.None;
        });
        ActiveBattlePartySet = 0;
        ActiveBattleStage = 0;
        ActiveBattleFloorFrom = 0;
        ActiveBattleFloorTo = 0;
        ActiveBattleFloor = 0;
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

    // 読み込んだ出撃が有効なら、保存した階と倍速でバトルを開く
    bool ResumeSortie()
    {
        if (ControllerBattle == null)
            return false;
        if (ActiveBattlePartySet == 0 || !IsKnownBattleStage(ActiveBattleStage))
            return false;
        if (!CanEnterStage(ActiveBattleStage) || !IsSortieFloorRange(ActiveBattleStage, ActiveBattleFloorFrom, ActiveBattleFloorTo))
            return WithdrawSavedSortie();
        if (!HasBattlePartyMember(ActiveBattlePartySet))
            return WithdrawSavedSortie();
        if (ActiveBattleFloor > ActiveBattleFloorTo)
        {
            if (IsBattleRepeatOn())
                ActiveBattleFloor = ActiveBattleFloorFrom;
            else
                return WithdrawSavedSortie();
        }
        if (ActiveBattleFloor < ActiveBattleFloorFrom)
            ActiveBattleFloor = ActiveBattleFloorFrom;

        EnsureSortiePlaces();
        if (!BeginAutoBattle())
            return WithdrawSavedSortie();
        return true;
    }

    bool WithdrawSavedSortie()
    {
        if (ActiveBattlePartySet != 0 || ActiveBattleStage != 0)
            LeaveBattle();
        SaveGame();
        return false;
    }

    void EnsureSortiePlaces()
    {
        int setIndex = ActiveBattlePartySet;
        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                uint characterId = BattlePartyCharacterIds[setIndex, x, y];
                if (characterId == 0)
                    continue;
                ReleaseCharacterFromProduction(characterId);
                CharacterClass battleCharacter = CharacterOf(characterId);
                if (battleCharacter != null)
                    battleCharacter.Whereabouts = Place.Battle;
            }
        }
        ReleasePeopleFromProduction(setIndex);
        RefreshProductionAssignmentViews();
    }

    public BattleBalanceConfig GetBattleBalance()
    {
        return BattleBalance;
    }

    public CharacterClass GetCharacter(uint characterId)
    {
        CharacterClass character = CharacterOf(characterId);
        if (character == null || character.ID != characterId)
            return null;
        return character;
    }

    public string GetRgbText()
    {
        return "R " + UiNumber.Format(CurR) + "   G " + UiNumber.Format(CurG) + "   B " + UiNumber.Format(CurB);
    }

    public void AddBattleRgb(CharacterAttribute attribute, long amount)
    {
        long r;
        long g;
        long b;
        BattleRewardCalculator.SplitRgb(attribute, amount, out r, out g, out b);
        AddRgbChannel(ref CurR, MaxR, r, 0);
        AddRgbChannel(ref CurG, MaxG, g, 1);
        AddRgbChannel(ref CurB, MaxB, b, 2);
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

    void AddRgbChannel(ref ulong current, ulong max, long amount, int channel)
    {
        if (amount <= 0)
            return;
        GainRgb(ref current, max, (ulong)amount, channel);
    }

    void GainRgb(ref ulong current, ulong max, ulong amount, int channel)
    {
        if (amount == 0)
            return;
        ulong before = current;
        if (ModelProduction != null)
            ModelProduction.Increase(ref current, amount, max);
        else if (current >= max)
            current = max;
        else if (max - current < amount)
            current = max;
        else
            current += amount;
        if (current > before && Quests.DefaultsApplied)
            NoteRgbProduced(channel, current - before);
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

        if (level < 1)
            level = 1;
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
            unit.RawHpMax = (long)character.Stats[0].HPMax;
            unit.RawAtk = (long)character.Stats[0].ATK;
            unit.RawDef = (long)character.Stats[0].DEF;
            if (unit.RawHpMax < 1)
                unit.RawHpMax = 1;
            ApplyPersonStats(unit);
            long delta = unit.HpMax - previousHpMax;
            if (delta > 0)
                unit.Hp += delta;
            if (unit.Hp > unit.HpMax)
                unit.Hp = unit.HpMax;
            if (unit.HpMax < 1)
                unit.HpMax = 1;
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
            int ownedBefore = character.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)character.OwnedNumCur;
            bool living = unit.IsLiving();
            character.SetOwnedCur(lives, BattleBalance.MaxLives);
            unit.Lives = (int)character.OwnedNumCur;
            if ((int)character.OwnedNumCur != ownedBefore)
            {
                ownedChanged = true;
                ApplyPersonStats(unit);
                if (living && unit.Lives < ownedBefore)
                    unit.Hp = unit.HpMax;
                else if (unit.Hp > unit.HpMax)
                    unit.Hp = unit.HpMax;
            }
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

        for (int i = 1; i <= Slots.PixelSlots; i++)
        {
            if (CharactersIDProductionPixel[i] == characterId)
                CharactersIDProductionPixel[i] = 0;
        }

        for (int i = 1; i <= Slots.CharacterSlots; i++)
        {
            if (CharactersIDProductionCharacter[i] == characterId)
                CharactersIDProductionCharacter[i] = 0;
        }
    }

    void RefreshProductionAssignmentViews()
    {
        EnsureProductionSlotRows();
        UpdateRGBProductionHelpCharacter();
        ClearEmptyHelpProductionButton("ButtonRProductionHelpCharacter", CharactersIDHelpProductionR, PersonIdHelpProductionR, new Color(50 / 255f, 0.0f, 0.0f, 1.0f));
        ClearEmptyHelpProductionButton("ButtonGProductionHelpCharacter", CharactersIDHelpProductionG, PersonIdHelpProductionG, new Color(0.0f, 50 / 255f, 0.0f, 1.0f));
        ClearEmptyHelpProductionButton("ButtonBProductionHelpCharacter", CharactersIDHelpProductionB, PersonIdHelpProductionB, new Color(0.0f, 0.0f, 50 / 255f, 1.0f));
        for (int i = 1; i <= Slots.PixelSlots; i++)
            UpdateCharacterPixelProduction(i);

        for (int i = 1; i <= Slots.CharacterSlots; i++)
            UpdateProductionCharacterButton("ButtonCharacterProductionCharacter" + i.ToString("00"), CharactersIDProductionCharacter[i], PersonIdProductionCharacter[i]);
    }

    void ClearEmptyHelpProductionButton(string buttonPrefix, uint[] characterIds, uint[] personIds, Color emptyColor)
    {
        for (int i = 1; i <= Constants.CHARACTERS_HELP_PRODUCTION_NUM; i++)
        {
            if (characterIds[i] != 0)
                continue;
            if (personIds != null && i < personIds.Length && personIds[i] != 0)
                continue;
            int slot = i;
            ForEachNamedButton(buttonPrefix + slot.ToString(), button =>
            {
                button.image.sprite = null;
                button.image.color = emptyColor;
                HidePersonPortrait(button);
                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = "+";
                SyncPersonSkillBar(button);
            });
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
        PersonPortraits.CopyFromFolder(Application.streamingAssetsPath + "/human");

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
    PersonPortraits.CopyFromFolder(Application.dataPath + "/Raw" + "/human");

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
    yield return CopyHumanAndroid();
#endif

        Debug.Log("MakeFile End");
        yield break; // ←★これを追加！（「ここでコルーチン終了」という意味です）
    }

    IEnumerator CopyHumanAndroid()
    {
        string destDir = Application.persistentDataPath + "/human";
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        for (int id = 1; id <= PersonIds.Count; id++)
        {
            string fileName = PersonPortraits.FileName(id);
            if (string.IsNullOrEmpty(fileName))
                continue;

            string path = Application.streamingAssetsPath + "/human/" + fileName;
            using (UnityWebRequest www = UnityWebRequest.Get(path))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                    File.WriteAllBytes(Path.Combine(destDir, fileName), www.downloadHandler.data);
                else
                    Debug.LogError("人の画像コピー失敗: " + fileName + " エラー: " + www.error);
            }
        }
    }


    //////////////////////////////////////////////////////////////////////////
    //ピクセル生産
    bool ProductionPixel(Trigger argTrigger, int argIndex, out bool ProductionPixelFlag)
    {
        ProductionPixelFlag = false;

        uint pixelPersonId = PersonIdProductionPixel[argIndex];
        PersonClass pixelPerson = pixelPersonId == 0 ? null : PersonAt((int)pixelPersonId);
        CharacterClass pixelWorker = pixelPerson != null ? null : CharacterOf(CharactersIDProductionPixel[argIndex]);
        if (pixelPerson == null && (CharactersIDProductionPixel[argIndex] == 0 || pixelWorker == null || pixelWorker.ID == 0))
            return pixelPerson == null && CharactersIDProductionPixel[argIndex] == 0;

        ushort Progress = PixelTickProgress(argIndex, argTrigger, pixelPerson, pixelWorker);
        if (argTrigger != Trigger.Update && argTrigger != Trigger.User)
        {
            Debug.Log("NG !!!!!!!!!!!!!!!!!!!!!!!");
            return false;
        }

        int pixelR = ColorByte(ColorProductionPixel[argIndex].r);
        int pixelG = ColorByte(ColorProductionPixel[argIndex].g);
        int pixelB = ColorByte(ColorProductionPixel[argIndex].b);
        uint createPixels = pixelPerson != null
            ? ToUint(pixelPerson.PixelAmount(pixelR, pixelG, pixelB, PersonBalance))
            : pixelWorker.GetCreatePixels((ushort)pixelR, (ushort)pixelG, (ushort)pixelB);

        if (pixelR == 0 && ProgressProductionPixel[argIndex, 1] == 0)
        {
            ProgressProductionPixel[argIndex, 1] += 1;
        }
        else
        if (ProgressProductionPixel[argIndex, 1] < pixelR)
        {
            ulong tmpR = CalcProductionPixelRGB(Progress, (uint)pixelR, ProgressProductionPixel[argIndex, 1], createPixels);
            if (CurR >= tmpR)
            {
                CurR -= tmpR;
                RememberPixelSpend(argIndex, 1, tmpR);
                ProgressProductionPixel[argIndex, 1] += Progress;
                if (ProgressProductionPixel[argIndex, 1] > (ushort)pixelR)
                    ProgressProductionPixel[argIndex, 1] = (ushort)pixelR;
            }
        }
        else
        if (pixelG == 0 && ProgressProductionPixel[argIndex, 2] == 0)
        {
            ProgressProductionPixel[argIndex, 2] += 1;
        }
        else
        if (ProgressProductionPixel[argIndex, 2] < pixelG)
        {
            ulong tmpG = CalcProductionPixelRGB(Progress, (uint)pixelG, ProgressProductionPixel[argIndex, 2], createPixels);
            if (CurG >= tmpG)
            {
                CurG -= tmpG;
                RememberPixelSpend(argIndex, 2, tmpG);
                ProgressProductionPixel[argIndex, 2] += Progress;
                if (ProgressProductionPixel[argIndex, 2] > (ushort)pixelG)
                    ProgressProductionPixel[argIndex, 2] = (ushort)pixelG;
            }
        }
        else
        if (pixelB == 0 && ProgressProductionPixel[argIndex, 3] == 0)
        {
            ProgressProductionPixel[argIndex, 3] += 1;
        }
        else
        if (ProgressProductionPixel[argIndex, 3] < pixelB)
        {
            ulong tmpB = CalcProductionPixelRGB(Progress, (uint)pixelB, ProgressProductionPixel[argIndex, 3], createPixels);
            if (CurB >= tmpB)
            {
                CurB -= tmpB;
                RememberPixelSpend(argIndex, 3, tmpB);
                ProgressProductionPixel[argIndex, 3] += Progress;
                if (ProgressProductionPixel[argIndex, 3] > (ushort)pixelB)
                    ProgressProductionPixel[argIndex, 3] = (ushort)pixelB;
            }
        }

        if (ProgressProductionPixel[argIndex, 3] >= pixelB && ProgressProductionPixel[argIndex, 3] != 0)
        {
            ProgressProductionPixel[argIndex, 1] = 0;
            ProgressProductionPixel[argIndex, 2] = 0;
            ProgressProductionPixel[argIndex, 3] = 0;
            ClearPixelSpend(argIndex);

            CurPixels[pixelR, pixelG, pixelB] += createPixels;
            if (pixelPerson != null)
                pixelPerson.AddPixelCompletion(pixelR, pixelG, pixelB, PersonBalance);
            ProductionPixelFlag = true;
            NotePixelCompletion();
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

    // 自動生産が、今進んでいる色の支払いができなければ、その色の警告を出す。
    void RefreshPixelAttention()
    {
        bool shortR = false;
        bool shortG = false;
        bool shortB = false;
        int last = Slots.PixelSlots;
        if (PersonIdProductionPixel != null && last >= PersonIdProductionPixel.Length)
            last = PersonIdProductionPixel.Length - 1;
        for (int i = 1; i <= last; i++)
        {
            uint pixelPersonId = PersonIdProductionPixel[i];
            PersonClass pixelPerson = pixelPersonId == 0 ? null : PersonAt((int)pixelPersonId);
            CharacterClass pixelWorker = pixelPerson != null ? null : CharacterOf(CharactersIDProductionPixel[i]);
            if (pixelPerson == null && (CharactersIDProductionPixel[i] == 0 || pixelWorker == null || pixelWorker.ID == 0))
                continue;

            int channel = PixelChannelInProgress(i);
            if (channel == 0)
                continue;
            ushort progress = PixelTickProgress(i, Trigger.Update, pixelPerson, pixelWorker);
            uint createPixels = PixelYield(i);
            int target = ColorByte(channel == 1 ? ColorProductionPixel[i].r : channel == 2 ? ColorProductionPixel[i].g : ColorProductionPixel[i].b);
            ulong cost = CalcProductionPixelRGB(progress, (uint)target, ProgressProductionPixel[i, channel], createPixels);
            if (cost == 0)
                continue;
            ulong held = channel == 1 ? CurR : channel == 2 ? CurG : CurB;
            if (held >= cost)
                continue;
            if (channel == 1)
                shortR = true;
            else if (channel == 2)
                shortG = true;
            else
                shortB = true;
        }
        if (ImageAttentionR != null)
            ImageAttentionR.enabled = shortR;
        if (ImageAttentionG != null)
            ImageAttentionG.enabled = shortG;
        if (ImageAttentionB != null)
            ImageAttentionB.enabled = shortB;
    }

    ushort PixelTickProgress(int argIndex, Trigger argTrigger, PersonClass pixelPerson, CharacterClass pixelWorker)
    {
        if (argTrigger == Trigger.User)
            return UserProductionPixelNum;
        if (argTrigger != Trigger.Update)
            return 0;
        if (pixelPerson != null)
        {
            int personSpd = PersonBalance != null ? PersonBalance.PixelSpd : 50;
            if (personSpd < 1)
                personSpd = 1;
            if (personSpd > ushort.MaxValue)
                personSpd = ushort.MaxValue;
            return (ushort)personSpd;
        }
        if (pixelWorker == null || pixelWorker.Stats == null || pixelWorker.Stats[0] == null)
            return 0;
        return pixelWorker.Stats[0].SPD;
    }

    // 1=R, 2=G, 3=B。その拍で自動的に進む色や、完成直前は 0。
    int PixelChannelInProgress(int argIndex)
    {
        int pixelR = ColorByte(ColorProductionPixel[argIndex].r);
        int pixelG = ColorByte(ColorProductionPixel[argIndex].g);
        int pixelB = ColorByte(ColorProductionPixel[argIndex].b);
        if (pixelR == 0 && ProgressProductionPixel[argIndex, 1] == 0)
            return 0;
        if (ProgressProductionPixel[argIndex, 1] < pixelR)
            return 1;
        if (pixelG == 0 && ProgressProductionPixel[argIndex, 2] == 0)
            return 0;
        if (ProgressProductionPixel[argIndex, 2] < pixelG)
            return 2;
        if (pixelB == 0 && ProgressProductionPixel[argIndex, 3] == 0)
            return 0;
        if (ProgressProductionPixel[argIndex, 3] < pixelB)
            return 3;
        return 0;
    }
    //色が変わったときだけ、途中の消費RGBを戻して進捗を捨てる。同じ色なら何もしない
    void ApplyPixelProductionColor(int argIndex, Color nextColor)
    {
        if (!SamePixelColor(ColorProductionPixel[argIndex], nextColor))
            RefundPixelProductionProgress(argIndex);

        ColorProductionPixel[argIndex].r = CharacterClass.ColorUnit(ColorByte(nextColor.r));
        ColorProductionPixel[argIndex].g = CharacterClass.ColorUnit(ColorByte(nextColor.g));
        ColorProductionPixel[argIndex].b = CharacterClass.ColorUnit(ColorByte(nextColor.b));
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
        return CharacterClass.ColorByte(value);
    }

    static int ColorByte(float value)
    {
        return CharacterClass.ColorByte(value);
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
        for (int i = 1; i <= Slots.PixelSlots; i++)
        {
            if (SpentProductionPixel[i, 1] != 0 || SpentProductionPixel[i, 2] != 0 || SpentProductionPixel[i, 3] != 0)
                continue;

            uint characterId = CharactersIDProductionPixel[i];
            CharacterClass pixelCharacter = CharacterOf(characterId);
            if (characterId == 0 || pixelCharacter == null || pixelCharacter.ID != characterId)
                continue;

            int r = PixelChannel(ColorProductionPixel[i].r);
            int g = PixelChannel(ColorProductionPixel[i].g);
            int b = PixelChannel(ColorProductionPixel[i].b);
            uint create = pixelCharacter.GetCreatePixels((ushort)r, (ushort)g, (ushort)b);
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
    void EnsureRgbPanelBackground()
    {
        if (PanelRGBProduction == null)
            return;

        Transform existing = PanelRGBProduction.transform.Find("RgbPanelBackground");
        Image image;
        if (existing == null)
        {
            GameObject obj = new GameObject("RgbPanelBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(PanelRGBProduction.transform, false);
            image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
            image = existing.GetComponent<Image>();

        Rect panelRect = PanelRGBProduction.GetComponent<RectTransform>().rect;
        if (panelRect.width < 8f || panelRect.height < 8f)
        {
            Canvas.ForceUpdateCanvases();
            panelRect = PanelRGBProduction.GetComponent<RectTransform>().rect;
        }
        int meshWidth = Mathf.Max(16, Mathf.RoundToInt(panelRect.width));
        int meshHeight = Mathf.Max(16, Mathf.RoundToInt(panelRect.height));
        image.sprite = RgbBackgroundMesh(meshWidth, meshHeight);
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = false;
        image.transform.SetAsFirstSibling();
        StyleChromeButtons(PanelRGBProduction);
        StyleChemSliders(PanelRGBProduction);

        Image panelImage = PanelRGBProduction.GetComponent<Image>();
        if (panelImage != null)
            panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 0f);

        for (int i = 0; i < PanelRGBProduction.transform.childCount; i++)
        {
            Transform child = PanelRGBProduction.transform.GetChild(i);
            if (child.name == "RgbPanelBackground")
                continue;
            if (child.GetComponent<Button>() != null)
                continue;
            Image childImage = child.GetComponent<Image>();
            RectTransform childRect = child as RectTransform;
            if (childImage == null || childRect == null)
                continue;
            if (childRect.anchorMin != Vector2.zero || childRect.anchorMax != Vector2.one)
                continue;
            if (childImage.color.g < childImage.color.r + 0.08f || childImage.color.g < childImage.color.b + 0.08f)
                continue;
            childImage.enabled = false;
        }
    }

    static int RgbBackgroundWidth;
    static int RgbBackgroundHeight;
    static readonly int[] MeshBayer4 =
    {
        0, 8, 2, 10,
        12, 4, 14, 6,
        3, 11, 1, 9,
        15, 7, 13, 5
    };

    static Sprite RgbBackgroundMesh(int width, int height)
    {
        const int cell = 4;
        int cols = Mathf.Max(4, width / cell);
        int rows = Mathf.Max(4, height / cell);
        if (RgbPanelBackgroundSprite != null && RgbBackgroundWidth == cols && RgbBackgroundHeight == rows)
            return RgbPanelBackgroundSprite;
        if (RgbPanelBackgroundSprite != null)
        {
            if (RgbPanelBackgroundSprite.texture != null)
                UnityEngine.Object.Destroy(RgbPanelBackgroundSprite.texture);
            UnityEngine.Object.Destroy(RgbPanelBackgroundSprite);
            RgbPanelBackgroundSprite = null;
        }

        Texture2D texture = new Texture2D(cols, rows, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        texture.hideFlags = HideFlags.HideAndDontSave;
        Color top = new Color(0.11f, 0.028f, 0.026f, 1f);
        Color middle = new Color(0.022f, 0.09f, 0.028f, 1f);
        Color bottom = new Color(0.022f, 0.036f, 0.12f, 1f);
        Color[] pixels = new Color[cols * rows];
        for (int y = 0; y < rows; y++)
        {
            float t = (y + 0.5f) / rows;
            Color from;
            Color to;
            float mix;
            if (t < 0.5f)
            {
                from = bottom;
                to = middle;
                mix = t / 0.5f;
            }
            else
            {
                from = middle;
                to = top;
                mix = (t - 0.5f) / 0.5f;
            }
            for (int x = 0; x < cols; x++)
            {
                int threshold = MeshBayer4[(x & 3) + ((y & 3) * 4)];
                pixels[y * cols + x] = mix * 16f > threshold ? to : from;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        RgbPanelBackgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, cols, rows), new Vector2(0.5f, 0.5f), 100f);
        RgbPanelBackgroundSprite.hideFlags = HideFlags.HideAndDontSave;
        RgbBackgroundWidth = cols;
        RgbBackgroundHeight = rows;
        return RgbPanelBackgroundSprite;
    }

    static readonly Sprite[] RgbButtonFillSprites = new Sprite[3];
    static readonly Sprite[] RgbUpgradeFillSprites = new Sprite[3];
    static readonly Sprite[] RgbButtonGlowSprites = new Sprite[4];
    const float RgbButtonGlow = 3f;
    const float RgbButtonCore = 1.5f;

    void StyleChromeButtons(GameObject root)
    {
        if (root == null)
            return;
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            StyleChromeButton(buttons[i]);
    }

    void StyleChromeButton(Button button)
    {
        if (button == null || IsWorkerButton(button.name) || IsSelectUi(button.transform))
            return;
        Image image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();
        if (image == null)
            return;

        int channel = ButtonChannel(button.name);
        if (channel < 0)
            channel = ChannelFromColor(image.color);
        if (channel < 0)
            return;

        bool upgrade = button.name.Contains("Max") || button.name.Contains("Increase");
        image.sprite = RgbButtonFill(channel, upgrade);
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = false;
        if (FeatureButtonImageColors.ContainsKey(button.name))
            FeatureButtonImageColors[button.name] = Color.white;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.02f, 0.02f, 0.02f, 1f);
        colors.fadeDuration = 0.05f;
        button.colors = colors;
        EnsureButtonNeon(button.transform, channel, button.interactable, upgrade);
        if (!upgrade)
            StyleProductionButtonText(button, channel);
        RgbButtonPressGlow press = button.GetComponent<RgbButtonPressGlow>();
        if (press == null)
            press = button.gameObject.AddComponent<RgbButtonPressGlow>();
        press.Production = this;
    }

    static void StyleProductionButtonText(Button button, int channel)
    {
        Text[] labels = button.GetComponentsInChildren<Text>(true);
        Color ink = new Color(0.93f, 0.94f, 0.96f, 1f);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].color = ink;
            StyleGaugeTextGlow(labels[i], channel);
        }
    }

    static int ButtonChannel(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Contains("RGB"))
            return -1;
        if (name.Contains("ValueG") || name.Contains("MaxG") || name.Contains("ButtonG"))
            return 1;
        if (name.Contains("ValueB") || name.Contains("MaxB") || name.Contains("ButtonB"))
            return 2;
        if (name.Contains("ValueR") || name.Contains("MaxR") || name.Contains("ButtonR"))
            return 0;
        return -1;
    }

    static int ChannelFromColor(Color color)
    {
        if (color.a < 0.2f)
            return -1;
        if (color.r > color.g + 0.15f && color.r > color.b + 0.15f)
            return 0;
        if (color.g > color.r + 0.15f && color.g > color.b + 0.15f)
            return 1;
        if (color.b > color.r + 0.15f && color.b > color.g + 0.15f)
            return 2;
        return -1;
    }

    static Color ButtonBottomColor(int channel)
    {
        if (channel == 1)
            return new Color(0.08f, 0.62f, 0.14f, 1f);
        if (channel == 2)
            return new Color(0.1f, 0.24f, 0.78f, 1f);
        return new Color(0.78f, 0.1f, 0.08f, 1f);
    }

    static Color ButtonGlowColor(int channel)
    {
        if (channel == 1)
            return new Color(0.45f, 1f, 0.5f, 0.9f);
        if (channel == 2)
            return new Color(0.45f, 0.72f, 1f, 0.9f);
        return new Color(1f, 0.4f, 0.32f, 0.9f);
    }

    static bool IsWorkerButton(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return name.Contains("ProductionHelpCharacter")
            || name.Contains("ButtonPixelProduction")
            || name.Contains("ButtonCharacterProduction")
            || name.Contains("ProducedCharacter");
    }

    static bool IsSelectUi(Transform transform)
    {
        while (transform != null)
        {
            string name = transform.name;
            if (name.Contains("SelectCharacter") || name.Contains("SelectColor") || name.Contains("Quest"))
                return true;
            transform = transform.parent;
        }
        return false;
    }

    static Sprite RgbButtonFill(int channel, bool upgrade)
    {
        if (channel < 0 || channel > 2)
            channel = 0;
        Sprite[] cache = upgrade ? RgbUpgradeFillSprites : RgbButtonFillSprites;
        if (cache[channel] != null)
            return cache[channel];

        const int height = 32;
        Texture2D texture = new Texture2D(1, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        Color top = new Color(0.02f, 0.02f, 0.02f, 1f);
        Color bottom = ButtonBottomColor(channel);
        float blackFrom = upgrade ? 2f / 3f : 1f / 3f;
        for (int y = 0; y < height; y++)
        {
            float t = (y + 0.5f) / height;
            if (t >= blackFrom)
                texture.SetPixel(0, y, top);
            else
                texture.SetPixel(0, y, Color.Lerp(bottom, top, t / blackFrom));
        }
        texture.Apply();
        cache[channel] = Sprite.Create(texture, new Rect(0f, 0f, 1f, height), new Vector2(0.5f, 0.5f), 100f);
        cache[channel].hideFlags = HideFlags.HideAndDontSave;
        return cache[channel];
    }

    readonly Dictionary<int, Coroutine> ButtonPressGlows = new Dictionary<int, Coroutine>();

    public void PlayButtonPressGlow(Button button)
    {
        if (button == null || !button.interactable)
            return;
        int id = button.GetInstanceID();
        Coroutine running;
        if (ButtonPressGlows.TryGetValue(id, out running) && running != null)
            StopCoroutine(running);
        ButtonPressGlows[id] = StartCoroutine(ButtonPressGlow(button));
    }

    IEnumerator ButtonPressGlow(Button button)
    {
        Image wash = EnsurePressWash(button.transform);
        int channel = ButtonChannel(button.name);
        float duration = 0.34f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float u = time / duration;
            float wave = u < 0.2f ? u / 0.2f : Mathf.Pow(1f - (u - 0.2f) / 0.8f, 1.35f);
            wash.color = new Color(1f, 1f, 1f, 0.2f * wave);
            if (channel >= 0)
                PaintButtonNeonStrength(button.transform, channel, 1f + 0.55f * wave);
            yield return null;
        }
        wash.color = new Color(1f, 1f, 1f, 0f);
        if (channel >= 0 && button != null)
            EnsureButtonNeon(button.transform, channel, button.interactable, button.name.Contains("Max") || button.name.Contains("Increase"));
    }

    static Image EnsurePressWash(Transform button)
    {
        Transform existing = button.Find("PressWash");
        Image image;
        if (existing == null)
        {
            GameObject obj = new GameObject("PressWash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(button, false);
            image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
            image = existing.GetComponent<Image>();

        int textIndex = button.childCount;
        for (int i = 0; i < button.childCount; i++)
        {
            if (button.GetChild(i).GetComponent<Text>() != null)
            {
                textIndex = i;
                break;
            }
        }
        image.transform.SetSiblingIndex(textIndex);
        return image;
    }

    static void PaintButtonNeonStrength(Transform button, int channel, float strength)
    {
        Transform frame = button.Find("NeonFrame");
        if (frame == null)
            return;
        Color glow = ButtonGlowColor(channel);
        glow = Color.Lerp(glow, Color.white, Mathf.Clamp01(strength - 1f));
        glow.a = Mathf.Clamp01(ButtonGlowColor(channel).a * strength);
        Color core = Color.Lerp(glow, Color.white, 0.7f);
        core.a = 0.95f;
        PaintButtonNeon(frame, "GlowTop", RgbButtonGlowSprite(0), glow);
        PaintButtonNeon(frame, "GlowRight", RgbButtonGlowSprite(1), glow);
        PaintButtonNeon(frame, "GlowBottom", RgbButtonGlowSprite(2), glow);
        PaintButtonNeon(frame, "GlowLeft", RgbButtonGlowSprite(3), glow);
        PaintButtonNeon(frame, "CoreTop", null, core);
        PaintButtonNeon(frame, "CoreRight", null, core);
        PaintButtonNeon(frame, "CoreBottom", null, core);
        PaintButtonNeon(frame, "CoreLeft", null, core);
    }

    static void EnsureButtonNeon(Transform button, int channel, bool bright, bool upgrade)
    {
        float glowSize = upgrade && bright ? 5f : RgbButtonGlow;
        float coreSize = upgrade && bright ? 4f : RgbButtonCore;
        Transform frame = button.Find("NeonFrame");
        if (frame == null)
        {
            GameObject root = new GameObject("NeonFrame", typeof(RectTransform));
            root.transform.SetParent(button, false);
            AddButtonNeonEdge(root.transform, "GlowTop", true, true, glowSize, 0f);
            AddButtonNeonEdge(root.transform, "GlowBottom", true, false, glowSize, 0f);
            AddButtonNeonEdge(root.transform, "GlowLeft", false, false, glowSize, 0f);
            AddButtonNeonEdge(root.transform, "GlowRight", false, true, glowSize, 0f);
            AddButtonNeonEdge(root.transform, "CoreTop", true, true, coreSize, glowSize);
            AddButtonNeonEdge(root.transform, "CoreBottom", true, false, coreSize, glowSize);
            AddButtonNeonEdge(root.transform, "CoreLeft", false, false, coreSize, glowSize);
            AddButtonNeonEdge(root.transform, "CoreRight", false, true, coreSize, glowSize);
            frame = root.transform;
        }

        RectTransform rootRect = frame.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = new Vector2(-glowSize, -glowSize);
        rootRect.offsetMax = new Vector2(glowSize, glowSize);
        LayoutButtonNeonEdge(frame, "GlowTop", true, true, glowSize, 0f);
        LayoutButtonNeonEdge(frame, "GlowBottom", true, false, glowSize, 0f);
        LayoutButtonNeonEdge(frame, "GlowLeft", false, false, glowSize, 0f);
        LayoutButtonNeonEdge(frame, "GlowRight", false, true, glowSize, 0f);
        LayoutButtonNeonEdge(frame, "CoreTop", true, true, coreSize, glowSize);
        LayoutButtonNeonEdge(frame, "CoreBottom", true, false, coreSize, glowSize);
        LayoutButtonNeonEdge(frame, "CoreLeft", false, false, coreSize, glowSize);
        LayoutButtonNeonEdge(frame, "CoreRight", false, true, coreSize, glowSize);

        Color glow = ButtonGlowColor(channel);
        Color core = Color.Lerp(glow, Color.white, bright ? (upgrade ? 0.88f : 0.55f) : 0.2f);
        if (bright)
            core.a = 0.95f;
        else
        {
            glow.a *= 0.28f;
            core.a = 0.38f;
        }
        frame.gameObject.SetActive(true);
        PaintButtonNeon(frame, "GlowTop", RgbButtonGlowSprite(0), glow);
        PaintButtonNeon(frame, "GlowRight", RgbButtonGlowSprite(1), glow);
        PaintButtonNeon(frame, "GlowBottom", RgbButtonGlowSprite(2), glow);
        PaintButtonNeon(frame, "GlowLeft", RgbButtonGlowSprite(3), glow);
        PaintButtonNeon(frame, "CoreTop", null, core);
        PaintButtonNeon(frame, "CoreRight", null, core);
        PaintButtonNeon(frame, "CoreBottom", null, core);
        PaintButtonNeon(frame, "CoreLeft", null, core);
        frame.SetAsFirstSibling();
    }

    static void AddButtonNeonEdge(Transform parent, string name, bool horizontal, bool end, float thickness, float inset)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.raycastTarget = false;
        LayoutButtonNeonEdge(obj.transform.parent, name, horizontal, end, thickness, inset);
    }

    static void LayoutButtonNeonEdge(Transform frame, string name, bool horizontal, bool end, float thickness, float inset)
    {
        Transform edge = frame.Find(name);
        if (edge == null)
            return;
        RectTransform rect = edge.GetComponent<RectTransform>();
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, end ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, end ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, end ? 1f : 0f);
            rect.sizeDelta = new Vector2(-inset * 2f, thickness);
            rect.anchoredPosition = new Vector2(0f, end ? -inset : inset);
        }
        else
        {
            rect.anchorMin = new Vector2(end ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(end ? 1f : 0f, 1f);
            rect.pivot = new Vector2(end ? 1f : 0f, 0.5f);
            rect.sizeDelta = new Vector2(thickness, -inset * 2f);
            rect.anchoredPosition = new Vector2(end ? -inset : inset, 0f);
        }
    }

    static void PaintButtonNeon(Transform frame, string name, Sprite sprite, Color color)
    {
        Transform edge = frame.Find(name);
        if (edge == null)
            return;
        Image image = edge.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
    }

    // 0上 1右 2下 3左。白のぼかしにして、色はボタンの属性で乗せる。
    static Sprite RgbButtonGlowSprite(int edge)
    {
        if (edge < 0 || edge > 3)
            edge = 0;
        if (RgbButtonGlowSprites[edge] != null)
            return RgbButtonGlowSprites[edge];

        const int along = 24;
        const int across = 12;
        bool vertical = edge == 1 || edge == 3;
        int width = vertical ? across : along;
        int height = vertical ? along : across;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float outer;
                if (edge == 0)
                    outer = (y + 0.5f) / height;
                else if (edge == 1)
                    outer = (x + 0.5f) / width;
                else if (edge == 2)
                    outer = 1f - (y + 0.5f) / height;
                else
                    outer = 1f - (x + 0.5f) / width;
                float fade = 1f - outer;
                fade = fade * fade * (3f - 2f * fade);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, fade));
            }
        }
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        RgbButtonGlowSprites[edge] = sprite;
        return sprite;
    }

    void StyleChemSliders(GameObject root)
    {
        if (root == null)
            return;
        Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++)
            StyleChemSlider(sliders[i]);
    }

    void StyleChemSlider(Slider slider)
    {
        if (slider == null)
            return;
        int channel = SliderChannel(slider.name);
        if (channel < 0)
            return;

        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        if (slider.handleRect != null)
            slider.handleRect.gameObject.SetActive(false);

        Transform background = slider.transform.Find("Background");
        Image backImage = background != null ? background.GetComponent<Image>() : null;
        if (backImage != null)
        {
            backImage.sprite = null;
            backImage.color = new Color(0.012f, 0.012f, 0.016f, 1f);
            backImage.type = Image.Type.Simple;
            Transform tube = background.Find("ChemTube");
            if (tube != null)
                Destroy(tube.gameObject);
        }

        if (slider.fillRect == null)
            return;
        Image fill = slider.fillRect.GetComponent<Image>();
        if (fill == null)
            return;
        fill.sprite = null;
        fill.type = Image.Type.Simple;
        fill.color = ChemLiquidColor(channel);
        fill.preserveAspect = false;
        Transform tip = slider.fillRect.Find("ChemTip");
        if (tip != null)
            Destroy(tip.gameObject);
        EnsureChemGlow(slider.fillRect, channel);
    }

    static int SliderChannel(string name)
    {
        if (string.IsNullOrEmpty(name))
            return -1;
        if (name.Contains("G"))
            return 1;
        if (name.Contains("B"))
            return 2;
        if (name.Contains("R"))
            return 0;
        return -1;
    }

    static readonly Sprite[] ChemGlowCornerSprites = new Sprite[4];

    static Color ChemGlowColor(int channel)
    {
        if (channel == 1)
            return new Color(0.2f, 0.9f, 0.26f, 1f);
        if (channel == 2)
            return new Color(0.2f, 0.46f, 1f, 1f);
        return new Color(1f, 0.2f, 0.14f, 1f);
    }

    static Color ChemLiquidColor(int channel)
    {
        Color color = Color.Lerp(ChemGlowColor(channel), Color.white, 0.62f);
        if (channel == 1)
            color = Color.Lerp(color, Color.black, 0.08f);
        color.a = 1f;
        return color;
    }

    static void EnsureChemGlow(RectTransform fill, int channel)
    {
        const float blur = 22f;
        Transform frame = fill.Find("ChemGlow");
        if (frame == null)
        {
            GameObject root = new GameObject("ChemGlow", typeof(RectTransform));
            root.transform.SetParent(fill, false);
            frame = root.transform;
        }
        EnsureChemGlowPiece(frame, "GlowTop");
        EnsureChemGlowPiece(frame, "GlowBottom");
        EnsureChemGlowPiece(frame, "GlowLeft");
        EnsureChemGlowPiece(frame, "GlowRight");
        EnsureChemGlowPiece(frame, "CornerTL");
        EnsureChemGlowPiece(frame, "CornerTR");
        EnsureChemGlowPiece(frame, "CornerBR");
        EnsureChemGlowPiece(frame, "CornerBL");

        RectTransform rootRect = frame.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = new Vector2(-blur, -blur);
        rootRect.offsetMax = new Vector2(blur, blur);
        LayoutChemGlowEdge(frame, "GlowTop", true, true, blur);
        LayoutChemGlowEdge(frame, "GlowBottom", true, false, blur);
        LayoutChemGlowEdge(frame, "GlowLeft", false, false, blur);
        LayoutChemGlowEdge(frame, "GlowRight", false, true, blur);
        LayoutChemGlowCorner(frame, "CornerTL", 0, blur);
        LayoutChemGlowCorner(frame, "CornerTR", 1, blur);
        LayoutChemGlowCorner(frame, "CornerBR", 2, blur);
        LayoutChemGlowCorner(frame, "CornerBL", 3, blur);

        Color glow = ChemGlowColor(channel);
        glow.a = 0.85f;
        PaintButtonNeon(frame, "GlowTop", RgbButtonGlowSprite(0), glow);
        PaintButtonNeon(frame, "GlowRight", RgbButtonGlowSprite(1), glow);
        PaintButtonNeon(frame, "GlowBottom", RgbButtonGlowSprite(2), glow);
        PaintButtonNeon(frame, "GlowLeft", RgbButtonGlowSprite(3), glow);
        PaintButtonNeon(frame, "CornerTL", ChemGlowCornerSprite(0), glow);
        PaintButtonNeon(frame, "CornerTR", ChemGlowCornerSprite(1), glow);
        PaintButtonNeon(frame, "CornerBR", ChemGlowCornerSprite(2), glow);
        PaintButtonNeon(frame, "CornerBL", ChemGlowCornerSprite(3), glow);
    }

    static void EnsureChemGlowPiece(Transform frame, string name)
    {
        if (frame.Find(name) != null)
            return;
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(frame, false);
        obj.GetComponent<Image>().raycastTarget = false;
    }

    static void LayoutChemGlowEdge(Transform frame, string name, bool horizontal, bool end, float thickness)
    {
        Transform edge = frame.Find(name);
        if (edge == null)
            return;
        RectTransform rect = edge.GetComponent<RectTransform>();
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, end ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, end ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, end ? 1f : 0f);
            rect.sizeDelta = new Vector2(-thickness * 2f, thickness);
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(end ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(end ? 1f : 0f, 1f);
            rect.pivot = new Vector2(end ? 1f : 0f, 0.5f);
            rect.sizeDelta = new Vector2(thickness, -thickness * 2f);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    static void LayoutChemGlowCorner(Transform frame, string name, int corner, float size)
    {
        Transform edge = frame.Find(name);
        if (edge == null)
            return;
        bool right = corner == 1 || corner == 2;
        bool top = corner == 0 || corner == 1;
        RectTransform rect = edge.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = Vector2.zero;
    }

    // 0左上 1右上 2右下 3左下。ゲージの角からの距離でぼかすので、辺と足しても明るくならない。
    static Sprite ChemGlowCornerSprite(int corner)
    {
        if (corner < 0 || corner > 3)
            corner = 0;
        if (ChemGlowCornerSprites[corner] != null)
            return ChemGlowCornerSprites[corner];

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size;
                float ny = (y + 0.5f) / size;
                float dx;
                float dy;
                if (corner == 0)
                {
                    dx = 1f - nx;
                    dy = ny;
                }
                else if (corner == 1)
                {
                    dx = nx;
                    dy = ny;
                }
                else if (corner == 2)
                {
                    dx = nx;
                    dy = 1f - ny;
                }
                else
                {
                    dx = 1f - nx;
                    dy = 1f - ny;
                }
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float t = 1f - Mathf.Clamp01(dist);
                float fade = t * t * (3f - 2f * t);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, fade));
            }
        }
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        ChemGlowCornerSprites[corner] = sprite;
        return sprite;
    }

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

        TextIncreaseValueLeft.text = "+" + UiNumber.Format(IncreaseValue);
        TextIncreaseValueRight.text = "+" + UiNumber.Format(IncreaseValue);
        TextCostIncreaseValueUp.text = "コスト : " + UiNumber.Format(CostIncreaseUp);
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

        TextCostMaxValueUp.text = "コスト : " + UiNumber.Format(CostMaxValueUp);
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
        StyleChromeButton(ButtonIncreaseValueUp);
        StyleChromeButton(ButtonMaxValueUp);
    }
    public void UpdateRGBProductionSliderOneColor(Text TextColorValue, ulong ColorValue, ulong MaxValue, Slider SliderColorValue, Slider SliderCostIncrease, Slider SliderCostMaxValueUp)
    {
        TextColorValue.text = UiNumber.Format(ColorValue) + " / " + UiNumber.Format(MaxValue);
        TextColorValue.color = new Color(0.93f, 0.94f, 0.96f, 1f);
        StyleGaugeTextGlow(TextColorValue, SliderColorValue != null ? SliderChannel(SliderColorValue.name) : -1);
        SliderColorValue.maxValue = MaxValue;
        SliderColorValue.minValue = 0;
        SliderColorValue.value = ColorValue;

        SliderCostIncrease.value = ColorValue;
        SliderCostMaxValueUp.value = ColorValue;
    }

    static void StyleGaugeTextGlow(Text text, int channel)
    {
        if (text == null || channel < 0)
            return;
        GaugeTextGlow glow = text.GetComponent<GaugeTextGlow>();
        if (glow == null)
            glow = text.gameObject.AddComponent<GaugeTextGlow>();
        Color color = ChemGlowColor(channel);
        color.a = 0.9f;
        if (glow.Glow == color && Mathf.Approximately(glow.Spread, 1.8f))
            return;
        glow.Glow = color;
        glow.Spread = 1.8f;
        text.SetVerticesDirty();
    }

    public void UpdateRGBProductionHelpCharacter()
    {
        PaintHelpWorker("ButtonRProductionHelpCharacter1", PersonIdHelpProductionR[1], CharactersIDHelpProductionR[1]);
        PaintHelpWorker("ButtonRProductionHelpCharacter2", PersonIdHelpProductionR[2], CharactersIDHelpProductionR[2]);
        PaintHelpWorker("ButtonRProductionHelpCharacter3", PersonIdHelpProductionR[3], CharactersIDHelpProductionR[3]);
        PaintHelpWorker("ButtonGProductionHelpCharacter1", PersonIdHelpProductionG[1], CharactersIDHelpProductionG[1]);
        PaintHelpWorker("ButtonGProductionHelpCharacter2", PersonIdHelpProductionG[2], CharactersIDHelpProductionG[2]);
        PaintHelpWorker("ButtonGProductionHelpCharacter3", PersonIdHelpProductionG[3], CharactersIDHelpProductionG[3]);
        PaintHelpWorker("ButtonBProductionHelpCharacter1", PersonIdHelpProductionB[1], CharactersIDHelpProductionB[1]);
        PaintHelpWorker("ButtonBProductionHelpCharacter2", PersonIdHelpProductionB[2], CharactersIDHelpProductionB[2]);
        PaintHelpWorker("ButtonBProductionHelpCharacter3", PersonIdHelpProductionB[3], CharactersIDHelpProductionB[3]);
    }

    void PaintHelpCharacter(Button button, uint id)
    {
        if (button == null || button.image == null || id == 0)
            return;
        HidePersonPortrait(button);
        CharacterClass character = CharacterOf(id);
        if (character == null || character.ID != id || character.ImageTexture2D == null)
            return;
        button.image.sprite = Sprite.Create(character.ImageTexture2D, new UnityEngine.Rect(0, 0, character.Size, character.Size), new Vector2(0.5f, 0.5f));
        button.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.text = "";
    }

    //ピクセル生産
    public void UpdatePixelProductionScene()
    {
        EnsureProductionSlotRows();
        CreatePixelListPixelProduction();

        for (int i = 1; i <= Slots.PixelSlots; i++)
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
        for (int i = 1; i <= Slots.PixelSlots; i++)
            UpdateSliderPixelProduction(i);
        RefreshPixelListCounts();
        UpdateProductionCharacterConsumeViews();
        RefreshPersonSkillBars();
    }

    void RefreshPersonSkillBars()
    {
        SyncNamedPersonSkillBar("ButtonRProductionHelpCharacter1");
        SyncNamedPersonSkillBar("ButtonRProductionHelpCharacter2");
        SyncNamedPersonSkillBar("ButtonRProductionHelpCharacter3");
        SyncNamedPersonSkillBar("ButtonGProductionHelpCharacter1");
        SyncNamedPersonSkillBar("ButtonGProductionHelpCharacter2");
        SyncNamedPersonSkillBar("ButtonGProductionHelpCharacter3");
        SyncNamedPersonSkillBar("ButtonBProductionHelpCharacter1");
        SyncNamedPersonSkillBar("ButtonBProductionHelpCharacter2");
        SyncNamedPersonSkillBar("ButtonBProductionHelpCharacter3");
        GameObject pixelList = GameObject.Find("ContentPixelProductionList");
        if (pixelList != null)
        {
            for (int i = 1; i <= Slots.PixelSlots; i++)
            {
                Transform row = pixelList.transform.Find("PrefabOnePixelProduction" + i.ToString("00"));
                if (row == null)
                    continue;
                Button[] buttons = row.GetComponentsInChildren<Button>();
                if (buttons.Length > 0)
                    SyncPersonSkillBar(buttons[0]);
            }
        }
        for (int i = 1; i <= Slots.CharacterSlots; i++)
            SyncNamedPersonSkillBar("ButtonCharacterProductionCharacter" + i.ToString("00"));
    }

    void SyncNamedPersonSkillBar(string buttonName)
    {
        ForEachNamedButton(buttonName, SyncPersonSkillBar);
    }

    void SyncPersonSkillBar(Button button)
    {
        if (button == null)
            return;
        string name = button.name;
        PersonClass person = null;
        int level = 1;
        long exp = 0;
        double exponent = PersonBalance != null ? PersonBalance.RgbExpExponent : 1.7;

        if (name.Contains("RProductionHelpCharacter") || name.Contains("GProductionHelpCharacter") || name.Contains("BProductionHelpCharacter"))
        {
            int index;
            if (!int.TryParse(name.Substring(name.Length - 1, 1), out index))
                return;
            int channel = name.Contains("GProductionHelpCharacter") ? 1 : name.Contains("BProductionHelpCharacter") ? 2 : 0;
            uint[] people = channel == 0 ? PersonIdHelpProductionR : channel == 1 ? PersonIdHelpProductionG : PersonIdHelpProductionB;
            if (index < 1 || index >= people.Length)
                return;
            person = PersonAt((int)people[index]);
            if (person != null)
            {
                level = person.RgbLevel[channel];
                exp = person.RgbExp[channel];
                exponent = PersonBalance != null ? PersonBalance.RgbExpExponent : 1.7;
            }
        }
        else if (name.Contains("ButtonPixelProductionCharacter"))
        {
            int index = PixelProductionIndex(button);
            if (index < 1 || index >= PersonIdProductionPixel.Length)
                return;
            person = PersonAt((int)PersonIdProductionPixel[index]);
            if (person != null)
            {
                level = person.PixelLevel;
                exp = person.PixelExp;
                exponent = PersonBalance != null ? PersonBalance.PixelExpExponent : 1.6;
            }
        }
        else if (name.Contains("ButtonCharacterProductionCharacter"))
        {
            int index;
            index = ProductionSlotIndex(name);
            if (index < 1)
                return;
            if (index < 1 || index >= PersonIdProductionCharacter.Length)
                return;
            person = PersonAt((int)PersonIdProductionCharacter[index]);
            if (person != null)
            {
                level = person.CharacterLevel;
                exp = person.CharacterExp;
                exponent = PersonBalance != null ? PersonBalance.CharacterExpExponent : 1.5;
            }
        }
        else
            return;

        if (person == null)
        {
            PersonSkillBar.Hide(button);
            return;
        }
        if (level < 1)
            level = 1;
        long need = PersonFormulas.ExpToNext(level, exponent, PersonBalance);
        PersonSkillBar.Show(button, level, exp, need);
    }

    public static int ProductionSlotIndex(string name)
    {
        if (string.IsNullOrEmpty(name))
            return -1;
        int start = name.Length;
        while (start > 0 && name[start - 1] >= '0' && name[start - 1] <= '9')
            start--;
        if (start == name.Length)
            return -1;
        int index;
        if (!int.TryParse(name.Substring(start), out index))
            return -1;
        return index;
    }

    static int PixelProductionIndex(Button button)
    {
        if (button == null)
            return 0;
        int index = ProductionSlotIndex(button.name);
        if (index >= 1)
            return index;
        Transform parent = button.transform.parent;
        if (parent == null)
            return 0;
        index = ProductionSlotIndex(parent.name);
        return index >= 1 ? index : 0;
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
    GameObject PixelProductionRow(int argIndex)
    {
        GameObject list = GameObject.Find("ContentPixelProductionList");
        if (list == null)
            return null;
        Transform row = list.transform.Find("PrefabOnePixelProduction" + argIndex.ToString("00"));
        return row != null ? row.gameObject : null;
    }

    public void UpdateCharacterPixelProduction(int argIndex)
    {
        string rowName = "PrefabOnePixelProduction" + argIndex.ToString("00");
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject content = objects[i];
            if (content == null || !content.scene.IsValid())
                continue;
            if (content.name != rowName && !content.name.StartsWith(rowName + " ", System.StringComparison.Ordinal))
                continue;
            PaintPixelProductionCharacter(content, argIndex);
        }
    }

    void PaintPixelProductionCharacter(GameObject content, int argIndex)
    {
        Button[] buttons = content.GetComponentsInChildren<Button>();
        if (buttons.Length == 0)
            return;
        Button pixelButton = buttons[0];
        CharacterClass pixelCharacter = PersonIdProductionPixel[argIndex] != 0 ? null : ImportedCharacters.FindAny(CharactersAll, CharactersIDProductionPixel[argIndex]);
        if (PersonIdProductionPixel[argIndex] != 0)
            PaintPerson(pixelButton, PersonAt((int)PersonIdProductionPixel[argIndex]));
        else
        if (pixelCharacter == null || pixelCharacter.ImagePath == null)
        {
            pixelButton.image.sprite = null;
            HidePersonPortrait(pixelButton);
            Text label = pixelButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = "+";
        }
        else
        {
            pixelButton.image.sprite = Sprite.Create(pixelCharacter.ImageTexture2D, new UnityEngine.Rect(0, 0, pixelCharacter.Size, pixelCharacter.Size), new Vector2(0.5f, 0.5f));
            HidePersonPortrait(pixelButton);
            Text label = pixelButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = "";
        }
        SyncPersonSkillBar(pixelButton);
    }
    public void UpdatePixelColorPixelProduction(int argIndex)
    {
        GameObject Content = PixelProductionRow(argIndex);
        if (Content == null)
            return;

        //ピクセルカラー
        Content.GetComponentsInChildren<Button>()[1].image.color
            = new Color(ColorProductionPixel[argIndex].r, ColorProductionPixel[argIndex].g, ColorProductionPixel[argIndex].b);
        Content.GetComponentsInChildren<Button>()[1].image.GetComponentInChildren<Text>().text
            = "#" + (ColorByte(ColorProductionPixel[argIndex].r)).ToString("X2") + (ColorByte(ColorProductionPixel[argIndex].g)).ToString("X2") + (ColorByte(ColorProductionPixel[argIndex].b)).ToString("X2");

        Button colorButton = Content.GetComponentsInChildren<Button>()[1];
        bool targetsOpen = IsMainFeatureOpen(MainFeature.PixelTargets);
        ColorBlock colors = colorButton.colors;
        colors.disabledColor = Color.white;
        colorButton.colors = colors;
        colorButton.interactable = targetsOpen;

        if (((ColorProductionPixel[argIndex].r + ColorProductionPixel[argIndex].g + ColorProductionPixel[argIndex].b) / 3.0f) < 0.5f)
        {
            colorButton.image.GetComponentInChildren<Text>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        }
        else
        {
            colorButton.image.GetComponentInChildren<Text>().color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        }

        //RGB値の表示
        uint pixelCount = PixelYield(argIndex);

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

        int r = ColorByte(ColorProductionPixel[argIndex].r);
        int g = ColorByte(ColorProductionPixel[argIndex].g);
        int b = ColorByte(ColorProductionPixel[argIndex].b);
        if (r < 0) r = 0;
        if (g < 0) g = 0;
        if (b < 0) b = 0;
        if (r > 255) r = 255;
        if (g > 255) g = 255;
        if (b > 255) b = 255;

        ulong stock = CurPixels[r, g, b];
        uint add = PixelYield(argIndex);
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

        int value = ColorByte(color01);
        long total = (long)value * pixelCount;
        text.text = channel + " " + value.ToString() + "\n"
            + "× " + UiNumber.Format(pixelCount) + " px\n"
            + "= " + UiNumber.Format(total);
    }
    public void UpdateSliderPixelProduction(int argIndex)
    {
        GameObject Content = PixelProductionRow(argIndex);
        if (Content == null)
            return;

        //進捗スライダー
        Slider SliderPixelColorR = Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("SliderPixelColorR").gameObject.GetComponent<Slider>();
        SliderPixelColorR.maxValue = ColorByte(ColorProductionPixel[argIndex].r);
        if (SliderPixelColorR.maxValue == 0)
        {
            SliderPixelColorR.maxValue = 1;
        }
        SliderPixelColorR.value = ProgressProductionPixel[argIndex, 1];

        Slider SliderPixelColorG = Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("SliderPixelColorG").gameObject.GetComponent<Slider>();
        SliderPixelColorG.maxValue = ColorByte(ColorProductionPixel[argIndex].g);
        if (SliderPixelColorG.maxValue == 0)
        {
            SliderPixelColorG.maxValue = 1;
        }
        SliderPixelColorG.value = ProgressProductionPixel[argIndex, 2];

        Slider SliderPixelColorB = Content.transform.Find("PanelImagePixelColor").gameObject.transform.Find("SliderPixelColorB").gameObject.GetComponent<Slider>();
        SliderPixelColorB.maxValue = ColorByte(ColorProductionPixel[argIndex].b);
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
            SliderSpecificationNumR.value = ColorByte(ColorTmp.r);

            InputFieldSpecificationNumR.placeholder.GetComponent<Text>().text = ColorByte(ColorTmp.r).ToString();
            InputFieldSpecificationNumR.text = ColorByte(ColorTmp.r).ToString();
        }
        else
        if (argRGB.Equals("G"))
        {
            ImageSpecificationColorG.color = new Color(0.0f, ColorTmp.g, 0.0f, 1.0f);

            Slider SliderSpecificationNumG = GameObject.Find("SliderSpecificationNumG").GetComponent<Slider>();
            SliderSpecificationNumG.value = ColorByte(ColorTmp.g);

            InputFieldSpecificationNumG.placeholder.GetComponent<Text>().text = ColorByte(ColorTmp.g).ToString();
            InputFieldSpecificationNumG.text = ColorByte(ColorTmp.g).ToString();
        }
        else
        if (argRGB.Equals("B"))
        {
            ImageSpecificationColorB.color = new Color(0.0f, 0.0f, ColorTmp.b, 1.0f);

            Slider SliderSpecificationNumB = GameObject.Find("SliderSpecificationNumB").GetComponent<Slider>();
            SliderSpecificationNumB.value = ColorByte(ColorTmp.b);

            InputFieldSpecificationNumB.placeholder.GetComponent<Text>().text = ColorByte(ColorTmp.b).ToString();
            InputFieldSpecificationNumB.text = ColorByte(ColorTmp.b).ToString();
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
                gameObject.GetComponent<Text>().text = "#" + ColorByte(ColorTmp.r).ToString("X2") + ColorByte(ColorTmp.g).ToString("X2") + ColorByte(ColorTmp.b).ToString("X2");

            if(gameObject.name.Equals("TextSelectColorR"))
                gameObject.GetComponent<Text>().text = "R:" + ColorByte(ColorTmp.r).ToString();
            if(gameObject.name.Equals("TextSelectColorG"))
                gameObject.GetComponent<Text>().text = "G:" + ColorByte(ColorTmp.g).ToString();
            if(gameObject.name.Equals("TextSelectColorB"))
                gameObject.GetComponent<Text>().text = "B:" + ColorByte(ColorTmp.b).ToString();

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
        EnsureProductionSlotRows();
        UpdateProductionCharacterButtons();
        UpdateAllProductionCharacterProgressImages();
        UpdateProductionCharacterConsumeViews();
        ShowCharacterOwnedNum();
    }

    //作成担当キャラと作成するキャラのボタンを、保存されている割り当てに合わせる
    void UpdateProductionCharacterButtons()
    {
        for (int i = 1; i <= Slots.CharacterSlots; i++)
        {
            UpdateProductionCharacterButton("ButtonCharacterProductionCharacter" + i.ToString("00"), CharactersIDProductionCharacter[i], PersonIdProductionCharacter[i]);
            UpdateProductionCharacterButton("ButtonCharacterProducedCharacter" + i.ToString("00"), CharactersIDProducedCharacter[i], 0);
            SetCharacterStockLabel(i);
        }
    }

    void UpdateCharacterProductionStockLabels()
    {
        for (int i = 1; i <= Slots.CharacterSlots; i++)
            SetCharacterStockLabel(i);
    }

    //作る対象ボタンの下に、今の所持数と、1体できたあとの所持数を出す
    void SetCharacterStockLabel(int argIndex)
    {
        if (argIndex < 1 || argIndex > Slots.CharacterSlots)
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
        CharacterClass stockCharacter = CharacterOf(characterId);
        if (characterId == 0 || stockCharacter == null || stockCharacter.ID != characterId)
        {
            label.text = "";
            label.gameObject.SetActive(false);
            return;
        }

        label.gameObject.SetActive(true);
        label.text = "在庫 " + stockCharacter.StockText();
        label.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform labelRect = label.rectTransform;
        if (labelRect.sizeDelta.y < 52f)
            labelRect.sizeDelta = new Vector2(labelRect.sizeDelta.x, 52f);
    }

    void UpdateProductionCharacterButton(string argButtonName, uint argCharacterID, uint argPersonId)
    {
        ForEachNamedButton(argButtonName, button =>
        {
            if (argPersonId != 0)
            {
                PaintPerson(button, PersonAt((int)argPersonId));
                SyncPersonSkillBar(button);
                return;
            }

            CharacterClass shownCharacter = argCharacterID == 0 ? null : ImportedCharacters.FindAny(CharactersAll, argCharacterID);
            Text label = button.GetComponentInChildren<Text>();
            if (shownCharacter == null || shownCharacter.ImageTexture2D == null)
            {
                button.image.sprite = null;
                HidePersonPortrait(button);
                if (label != null)
                    label.text = "+";
                SyncPersonSkillBar(button);
                return;
            }

            button.image.sprite = Sprite.Create(shownCharacter.ImageTexture2D, new UnityEngine.Rect(0, 0, shownCharacter.Size, shownCharacter.Size), new Vector2(0.5f, 0.5f));
            button.image.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            HidePersonPortrait(button);
            if (label != null)
                label.text = "";
            SyncPersonSkillBar(button);
        });
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
                    productionIndex = ProductionSlotIndex(child.name);
                    progressImage = child.GetComponent<RawImage>();
                }
                if (child.name.Contains("ScrollViewConsumePixel"))
                {
                    if (productionIndex < 0)
                        productionIndex = ProductionSlotIndex(child.name);
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

        CharacterClass sourceCharacter = ImportedCharacters.FindAny(CharactersAll, characterId);
        if (sourceCharacter == null)
            return;
        Texture2D sourceTexture = sourceCharacter.ImageTexture2D;
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
        texts[0].text = ColorByte(consumePixel.PixelColor.r).ToString();
        texts[2].text = ColorByte(consumePixel.PixelColor.g).ToString();
        texts[4].text = ColorByte(consumePixel.PixelColor.b).ToString();
        int r = ColorByte(consumePixel.PixelColor.r);
        int g = ColorByte(consumePixel.PixelColor.g);
        int b = ColorByte(consumePixel.PixelColor.b);
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
        System.Action<CharacterClass> addOwned = null;
        addOwned = character =>
        {
            if (character == null || character.ID == 0 || character.OwnedNumMax == 0 || character.ImageTexture2D == null)
                return;

            string buttonName = "OwnedCharacter" + character.ID.ToString();
            Transform existing = list.Find(buttonName);
            if (existing != null)
            {
                Text label = OwnedCountLabel(existing);
                if (label != null)
                    FitOwnedStockLabel(label, "所持 " + character.StockText(true));
                existing.SetSiblingIndex(shown);
                shown++;
                return;
            }

            GameObject characterButton = Instantiate((GameObject)Resources.Load("PrefabButtonCharacterImage"), list) as GameObject;
            characterButton.name = buttonName;
            characterButton.transform.SetSiblingIndex(shown);
            characterButton.GetComponentInChildren<Image>().sprite = Sprite.Create(character.ImageTexture2D, new UnityEngine.Rect(0, 0, character.Size, character.Size), new Vector2(0.5f, 0.5f));
            Text createdLabel = characterButton.GetComponentInChildren<Text>();
            FitOwnedStockLabel(createdLabel, "所持 " + character.StockText(true));
            createdLabel.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            if (character.Whereabouts == Place.CreateR ||
                character.Whereabouts == Place.CreateG ||
                character.Whereabouts == Place.CreateB ||
                character.Whereabouts == Place.CreatePixel ||
                character.Whereabouts == Place.CreateCharacter)
                ControllerCharacterSelectClass.AddFaintWarningMark(characterButton);
            shown++;
            added = true;
        };
        for (int i = 1; i <= Constants.CHARACTERS_ALL_NUM; i++)
            addOwned(CharactersAll[i]);
        ImportedCharacters.ForEach(addOwned);

        if (added)
            LayoutRebuilder.ForceRebuildLayoutImmediate(list as RectTransform);
    }

    static void FitOwnedStockLabel(Text label, string text)
    {
        if (label == null)
            return;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -64f);
        rect.sizeDelta = new Vector2(120f, 40f);
        label.alignment = TextAnchor.UpperCenter;
        label.alignByGeometry = false;
        label.lineSpacing = 0.8f;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 8;
        label.resizeTextMaxSize = 13;
        label.text = text;
        label.color = new Color(0f, 0f, 0f, 1f);
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

    PersonClass PersonAt(int id)
    {
        return PersonClass.Find(People, id);
    }

    public PersonClass GetPerson(int id)
    {
        return PersonAt(id);
    }

    public PersonBalanceConfig GetPersonBalance()
    {
        return PersonBalance;
    }

    public int PersonCount()
    {
        return PersonIds.Count;
    }

    ulong RgbSlotOutput(uint[] people, uint[] characters, int channel, Action<int, ulong, bool> onSlot = null)
    {
        ulong sum = 0;
        int length = people != null ? people.Length : 0;
        for (int i = 1; i < length; i++)
        {
            if (people[i] != 0)
            {
                PersonClass person = PersonAt((int)people[i]);
                if (person == null)
                    continue;
                long amount = person.RgbAmount(channel, PersonBalance);
                if (amount < 0)
                    amount = 0;
                person.AddRgbProduced(channel, amount, PersonBalance);
                if (sum > ulong.MaxValue - (ulong)amount)
                    sum = ulong.MaxValue;
                else
                    sum += (ulong)amount;
                if (onSlot != null && amount > 0)
                    onSlot(i, (ulong)amount, true);
                continue;
            }
            if (characters != null && i < characters.Length)
            {
                ulong produced = HelpCreates(characters[i], channel);
                sum += produced;
                if (onSlot != null && produced > 0)
                    onSlot(i, produced, false);
            }
        }
        return sum;
    }

    uint PixelYield(int index)
    {
        if (index < 1 || index >= PersonIdProductionPixel.Length)
            return 0;
        int r = ColorByte(ColorProductionPixel[index].r);
        int g = ColorByte(ColorProductionPixel[index].g);
        int b = ColorByte(ColorProductionPixel[index].b);
        if (PersonIdProductionPixel[index] != 0)
        {
            PersonClass person = PersonAt((int)PersonIdProductionPixel[index]);
            if (person == null)
                return 0;
            return ToUint(person.PixelAmount(r, g, b, PersonBalance));
        }
        CharacterClass character = CharacterOf(CharactersIDProductionPixel[index]);
        if (character == null || character.ID == 0)
            return 0;
        return character.GetCreatePixels((ushort)r, (ushort)g, (ushort)b);
    }

    static uint ToUint(long value)
    {
        if (value <= 0)
            return 0;
        if (value > uint.MaxValue)
            return uint.MaxValue;
        return (uint)value;
    }

    void GrantCompletedCharacter(int index, CharacterClass produced)
    {
        if (produced == null)
            return;
        uint personId = index >= 0 && index < PersonIdProductionCharacter.Length ? PersonIdProductionCharacter[index] : 0;
        uint workerId = index >= 0 && index < CharactersIDProductionCharacter.Length ? CharactersIDProductionCharacter[index] : 0;
        PersonClass person = personId == 0 ? null : PersonAt((int)personId);
        bool workerIsPerson = person != null;
        bool self = !workerIsPerson && workerId == produced.ID;
        bool author = workerIsPerson && produced.AuthorPersonId == person.Id;
        int before = produced.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)produced.OwnedNumCur;
        produced.GainOwnedTier(PersonFormulas.CompletionTier(workerIsPerson, self, author), 1, BattleBalance.MaxLives);
        int after = produced.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)produced.OwnedNumCur;
        if (after > before)
            NoteCharacterCompletions(after - before);
        if (workerIsPerson && after > before)
            person.AddCharacterCompletion(author, PersonBalance);
    }

    public void GrantPersonBattleFloor()
    {
        if (ActiveBattlePartySet < 1 || ActiveBattlePartySet >= BattlePartyPersonIds.GetLength(0))
            return;
        for (int slot = 1; slot <= Constants.BATTLE_PERSON_SLOT_NUM; slot++)
        {
            PersonClass person = PersonAt((int)BattlePartyPersonIds[ActiveBattlePartySet, slot]);
            if (person != null)
                person.AddBattleFloor(PersonBalance);
        }
    }

    public uint GetBattlePartyPerson(int setIndex, int slot)
    {
        if (!IsBattlePersonSlot(setIndex, slot))
            return 0;
        return BattlePartyPersonIds[setIndex, slot];
    }

    public bool SetBattlePartyPerson(int setIndex, int slot, uint personId)
    {
        if (!IsBattlePersonSlot(setIndex, slot))
            return false;
        if (setIndex == ActiveBattlePartySet)
            return false;
        if (personId != 0 && PersonAt((int)personId) == null)
            return false;
        if (personId != 0)
        {
            for (int other = 1; other <= Constants.BATTLE_PERSON_SLOT_NUM; other++)
            {
                if (other != slot && BattlePartyPersonIds[setIndex, other] == personId)
                    BattlePartyPersonIds[setIndex, other] = 0;
            }
        }
        BattlePartyPersonIds[setIndex, slot] = personId;
        return true;
    }

    bool IsBattlePersonSlot(int setIndex, int slot)
    {
        return setIndex >= 1 && setIndex <= Constants.BATTLE_PARTY_SET_NUM
            && slot >= 1 && slot <= Constants.BATTLE_PERSON_SLOT_NUM;
    }

    public bool IsPersonSortieing(int personId)
    {
        if (personId <= 0 || ActiveBattlePartySet < 1 || ActiveBattlePartySet > Constants.BATTLE_PARTY_SET_NUM)
            return false;
        for (int slot = 1; slot <= Constants.BATTLE_PERSON_SLOT_NUM; slot++)
        {
            if (BattlePartyPersonIds[ActiveBattlePartySet, slot] == (uint)personId)
                return true;
        }
        return false;
    }

    void ReleasePeopleFromProduction(int setIndex)
    {
        if (setIndex < 1 || setIndex > Constants.BATTLE_PARTY_SET_NUM)
            return;
        for (int slot = 1; slot <= Constants.BATTLE_PERSON_SLOT_NUM; slot++)
        {
            uint personId = BattlePartyPersonIds[setIndex, slot];
            if (personId != 0)
                ClearPersonFromProduction(personId);
        }
    }

    public void AssignProductionPerson(Button button, int personId)
    {
        if (button == null || PersonAt(personId) == null || IsPersonSortieing(personId))
            return;
        ClearPersonFromProduction((uint)personId);
        if (button.name.StartsWith("ButtonRProductionHelpCharacter"))
            AssignRgbPerson(PersonIdHelpProductionR, CharactersIDHelpProductionR, button, personId);
        else if (button.name.StartsWith("ButtonGProductionHelpCharacter"))
            AssignRgbPerson(PersonIdHelpProductionG, CharactersIDHelpProductionG, button, personId);
        else if (button.name.StartsWith("ButtonBProductionHelpCharacter"))
            AssignRgbPerson(PersonIdHelpProductionB, CharactersIDHelpProductionB, button, personId);
        else if (button.name.Contains("ButtonPixelProductionCharacter"))
        {
            int index = ProductionSlotIndex(button.name);
            if (index < 1 || index >= CharactersIDProductionPixel.Length)
                return;
            ReleaseWorkerCharacter(CharactersIDProductionPixel, index);
            CharactersIDProductionPixel[index] = 0;
            PersonIdProductionPixel[index] = (uint)personId;
        }
        else if (button.name.Contains("ButtonCharacterProductionCharacter"))
        {
            int index = ProductionSlotIndex(button.name);
            if (index < 1 || index >= CharactersIDProductionCharacter.Length)
                return;
            ReleaseWorkerCharacter(CharactersIDProductionCharacter, index);
            CharactersIDProductionCharacter[index] = 0;
            PersonIdProductionCharacter[index] = (uint)personId;
        }
        PaintPerson(button, PersonAt(personId));
        SyncPersonSkillBar(button);
    }

    void AssignRgbPerson(uint[] people, uint[] characters, Button button, int personId)
    {
        int index = int.Parse(button.name.Substring(button.name.Length - 1, 1));
        ReleaseWorkerCharacter(characters, index);
        characters[index] = 0;
        people[index] = (uint)personId;
    }

    void ReleaseWorkerCharacter(uint[] characters, int index)
    {
        if (characters == null || index < 1 || index >= characters.Length || characters[index] == 0)
            return;
        CharacterClass previous = CharacterOf(characters[index]);
        if (previous != null)
            previous.Whereabouts = Place.None;
    }

    void ClearPersonFromProduction(uint personId)
    {
        ClearPerson(PersonIdHelpProductionR, personId);
        ClearPerson(PersonIdHelpProductionG, personId);
        ClearPerson(PersonIdHelpProductionB, personId);
        ClearPerson(PersonIdProductionPixel, personId);
        ClearPerson(PersonIdProductionCharacter, personId);
    }

    static void ClearPerson(uint[] slots, uint personId)
    {
        if (slots == null)
            return;
        for (int i = 1; i < slots.Length; i++)
        {
            if (slots[i] == personId)
                slots[i] = 0;
        }
    }

    public void ClearProductionPerson(Button button)
    {
        if (button == null)
            return;
        if (button.name.StartsWith("ButtonRProductionHelpCharacter"))
            ClearRgbPerson(PersonIdHelpProductionR, button);
        else if (button.name.StartsWith("ButtonGProductionHelpCharacter"))
            ClearRgbPerson(PersonIdHelpProductionG, button);
        else if (button.name.StartsWith("ButtonBProductionHelpCharacter"))
            ClearRgbPerson(PersonIdHelpProductionB, button);
        else if (button.name.Contains("ButtonPixelProductionCharacter"))
        {
            int index = ProductionSlotIndex(button.name);
            if (index >= 1 && index < PersonIdProductionPixel.Length)
            PersonIdProductionPixel[index] = 0;
        }
        else if (button.name.Contains("ButtonCharacterProductionCharacter"))
        {
            int index = ProductionSlotIndex(button.name);
            if (index >= 1 && index < PersonIdProductionCharacter.Length)
            PersonIdProductionCharacter[index] = 0;
        }
        HidePersonPortrait(button);
        if (button.image != null)
        {
            button.image.sprite = null;
            if (button.name.StartsWith("ButtonRProductionHelpCharacter"))
                button.image.color = new Color(50f / 255f, 0f, 0f, 1f);
            else if (button.name.StartsWith("ButtonGProductionHelpCharacter"))
                button.image.color = new Color(0f, 50f / 255f, 0f, 1f);
            else if (button.name.StartsWith("ButtonBProductionHelpCharacter"))
                button.image.color = new Color(0f, 0f, 50f / 255f, 1f);
            else
                button.image.color = Color.white;
        }
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.text = "+";
        SyncPersonSkillBar(button);
    }

    static void ClearRgbPerson(uint[] people, Button button)
    {
        int index = int.Parse(button.name.Substring(button.name.Length - 1, 1));
        if (index >= 1 && index < people.Length)
            people[index] = 0;
    }

    void NormalizePersonAssignments()
    {
        bool[] used = new bool[PersonIds.Count + 1];
        NormalizeSlot(PersonIdHelpProductionR, CharactersIDHelpProductionR, used);
        NormalizeSlot(PersonIdHelpProductionG, CharactersIDHelpProductionG, used);
        NormalizeSlot(PersonIdHelpProductionB, CharactersIDHelpProductionB, used);
        NormalizeSlot(PersonIdProductionPixel, CharactersIDProductionPixel, used);
        NormalizeSlot(PersonIdProductionCharacter, CharactersIDProductionCharacter, used);
        for (int set = 1; set <= Constants.BATTLE_PARTY_SET_NUM; set++)
        {
            bool[] inSet = new bool[PersonIds.Count + 1];
            for (int slot = 1; slot <= Constants.BATTLE_PERSON_SLOT_NUM; slot++)
            {
                int id = (int)BattlePartyPersonIds[set, slot];
                if (PersonAt(id) == null || inSet[id])
                {
                    BattlePartyPersonIds[set, slot] = 0;
                    continue;
                }
                inSet[id] = true;
            }
        }
    }

    void NormalizeSlot(uint[] people, uint[] characters, bool[] used)
    {
        if (people == null)
            return;
        for (int i = 1; i < people.Length; i++)
        {
            int id = (int)people[i];
            if (id == 0)
                continue;
            if (PersonAt(id) == null || used[id])
            {
                people[i] = 0;
                continue;
            }
            used[id] = true;
            if (characters != null && i < characters.Length && characters[i] != 0)
            {
                ReleaseWorkerCharacter(characters, i);
                characters[i] = 0;
            }
        }
    }

    void PaintHelpWorker(string buttonName, uint personId, uint characterId)
    {
        ForEachNamedButton(buttonName, button =>
        {
            if (personId != 0)
            {
                PaintPerson(button, PersonAt((int)personId));
                SyncPersonSkillBar(button);
                return;
            }
            SyncPersonSkillBar(button);
            if (characterId == 0)
                return;
            PaintHelpCharacter(button, characterId);
        });
    }

    public static void ForEachNamedButton(string buttonName, System.Action<Button> action)
    {
        if (string.IsNullOrEmpty(buttonName) || action == null)
            return;
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || !button.gameObject.scene.IsValid())
                continue;
            string name = button.name;
            if (name != buttonName && !name.StartsWith(buttonName + " ", System.StringComparison.Ordinal))
                continue;
            action(button);
        }
    }

    public static void PaintPerson(Button button, PersonClass person)
    {
        if (button == null || button.image == null || person == null)
            return;
        Sprite portrait = PersonPortraits.SpriteOf(person.Id);
        button.image.type = Image.Type.Simple;
        button.image.preserveAspect = false;
        Text label = button.GetComponentInChildren<Text>();
        if (portrait != null)
        {
            if (button.name.Contains("Production"))
            {
                ShowPersonPortrait(button, portrait, PersonFrameColors(person));
                button.image.sprite = null;
                button.image.color = new Color(1f, 1f, 1f, 0f);
            }
            else
            {
                HidePersonPortrait(button);
                button.image.sprite = portrait;
                button.image.color = Color.white;
            }
            if (label != null)
                label.text = "";
            return;
        }

        HidePersonPortrait(button);
        button.image.sprite = null;
        button.image.color = PersonColor(person.Id);
        if (label != null)
        {
            label.text = person.Name;
            label.color = person.Id == PersonIds.TeacherBlack ? Color.white : new Color(0.1f, 0.1f, 0.1f, 1f);
        }
    }

    public static void HidePersonPortrait(Button button)
    {
        if (button == null)
            return;
        Transform portrait = button.transform.Find("PersonPortrait");
        if (portrait != null)
            portrait.gameObject.SetActive(false);
    }

    static readonly Dictionary<int, Sprite> PersonFrameSprites = new Dictionary<int, Sprite>();
    const float PersonFrameSolid = 4f;
    const float PersonFrameBlur = 3f;
    const float PersonFrameThickness = PersonFrameSolid + PersonFrameBlur;

    static void ShowPersonPortrait(Button button, Sprite sprite, Color[] frameColors)
    {
        const float lift = 18f;
        Transform existing = button.transform.Find("PersonPortrait");
        Image image;
        if (existing == null)
        {
            GameObject obj = new GameObject("PersonPortrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(button.transform, false);
            image = obj.GetComponent<Image>();
            image.raycastTarget = false;
        }
        else
            image = existing.GetComponent<Image>();

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, lift);
        rect.sizeDelta = new Vector2(0f, 0f);
        AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
        if (fitter == null)
            fitter = image.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        fitter.aspectRatio = 1f;
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.gameObject.SetActive(true);
        image.transform.SetAsFirstSibling();
        EnsurePersonFrame(image.transform, frameColors);
    }

    // 属性なしは灰色。複数は赤・緑・青・闇・光の順に、枠を一周するグラデーション。
    static Color[] PersonFrameColors(PersonClass person)
    {
        CharacterAttribute[] found = new CharacterAttribute[5];
        int count = 0;
        if (person != null && person.Specialties != null)
        {
            for (int i = 0; i < person.Specialties.Count; i++)
            {
                CharacterAttribute attribute = person.Specialties[i];
                int order = PersonFrameOrder(attribute);
                if (order > 4)
                    continue;
                bool exists = false;
                for (int j = 0; j < count; j++)
                {
                    if (found[j] == attribute)
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists)
                    continue;
                int at = count;
                for (int j = 0; j < count; j++)
                {
                    if (PersonFrameOrder(found[j]) > order)
                    {
                        at = j;
                        break;
                    }
                }
                for (int j = count; j > at; j--)
                    found[j] = found[j - 1];
                found[at] = attribute;
                count++;
            }
        }

        if (count == 0)
            return new Color[] { PersonFrameGray() };
        Color[] colors = new Color[count];
        for (int i = 0; i < count; i++)
            colors[i] = PersonFrameColor(found[i]);
        return colors;
    }

    static int PersonFrameOrder(CharacterAttribute attribute)
    {
        switch (attribute)
        {
            case CharacterAttribute.Fire: return 0;
            case CharacterAttribute.Grass: return 1;
            case CharacterAttribute.Water: return 2;
            case CharacterAttribute.Dark: return 3;
            case CharacterAttribute.Light: return 4;
            default: return 9;
        }
    }

    static Color PersonFrameGray()
    {
        return new Color(0.24f, 0.24f, 0.26f, 0.92f);
    }

    static Color PersonFrameColor(CharacterAttribute attribute)
    {
        switch (attribute)
        {
            case CharacterAttribute.Fire:
                return new Color(0.34f, 0.09f, 0.06f, 0.92f);
            case CharacterAttribute.Grass:
                return new Color(0.06f, 0.24f, 0.1f, 0.92f);
            case CharacterAttribute.Water:
                return new Color(0.06f, 0.16f, 0.32f, 0.92f);
            case CharacterAttribute.Light:
                return new Color(0.72f, 0.72f, 0.74f, 0.92f);
            case CharacterAttribute.Dark:
                return new Color(0.03f, 0.03f, 0.035f, 0.95f);
            default:
                return PersonFrameGray();
        }
    }

    static void EnsurePersonFrame(Transform portrait, Color[] colors)
    {
        if (colors == null || colors.Length == 0)
            colors = new Color[] { PersonFrameGray() };

        Transform frame = portrait.Find("PersonFrame");
        if (frame == null)
        {
            GameObject root = new GameObject("PersonFrame", typeof(RectTransform));
            root.transform.SetParent(portrait, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            AddFrameEdge(root.transform, "Top", true, true, PersonFrameThickness, 0f);
            AddFrameEdge(root.transform, "Bottom", true, false, PersonFrameThickness, 0f);
            AddFrameEdge(root.transform, "Left", false, false, PersonFrameThickness, 0f);
            AddFrameEdge(root.transform, "Right", false, true, PersonFrameThickness, 0f);
            frame = root.transform;
        }

        RectTransform frameRect = frame as RectTransform;
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.offsetMin = new Vector2(-PersonFrameBlur, -PersonFrameBlur);
        frameRect.offsetMax = new Vector2(PersonFrameBlur, PersonFrameBlur);

        for (int i = frame.childCount - 1; i >= 0; i--)
        {
            Transform child = frame.GetChild(i);
            if (child.name.EndsWith("In"))
                Destroy(child.gameObject);
        }

        LayoutFrameEdge(frame, "Top", true, true, 0f);
        LayoutFrameEdge(frame, "Bottom", true, false, 0f);
        LayoutFrameEdge(frame, "Left", false, false, 0f);
        LayoutFrameEdge(frame, "Right", false, true, 0f);
        PaintFrameEdge(frame, "Top", FrameEdgeSprite(colors, 0), Color.white);
        PaintFrameEdge(frame, "Right", FrameEdgeSprite(colors, 1), Color.white);
        PaintFrameEdge(frame, "Bottom", FrameEdgeSprite(colors, 2), Color.white);
        PaintFrameEdge(frame, "Left", FrameEdgeSprite(colors, 3), Color.white);
        frame.SetAsLastSibling();
    }

    static void LayoutFrameEdge(Transform frame, string name, bool horizontal, bool end, float inset)
    {
        Transform edge = frame.Find(name);
        if (edge == null)
            return;
        RectTransform rect = edge.GetComponent<RectTransform>();
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, end ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, end ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, end ? 1f : 0f);
            rect.sizeDelta = new Vector2(-inset * 2f, PersonFrameThickness);
            rect.anchoredPosition = new Vector2(0f, end ? -inset : inset);
        }
        else
        {
            rect.anchorMin = new Vector2(end ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(end ? 1f : 0f, 1f);
            rect.pivot = new Vector2(end ? 1f : 0f, 0.5f);
            rect.sizeDelta = new Vector2(PersonFrameThickness, -inset * 2f);
            rect.anchoredPosition = new Vector2(end ? -inset : inset, 0f);
        }
    }

    static void PaintFrameEdge(Transform frame, string name, Sprite sprite, Color color)
    {
        Transform edge = frame.Find(name);
        if (edge == null)
            return;
        Image image = edge.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = Image.Type.Simple;
    }

    // 上辺の左から時計回りに色を置き、外側の広げた分だけ透明度を落としてぼかす。
    static Sprite FrameEdgeSprite(Color[] colors, int edge)
    {
        int key = edge + 101;
        for (int i = 0; i < colors.Length; i++)
        {
            Color color = colors[i];
            key = key * 31 + Mathf.RoundToInt(color.r * 255f);
            key = key * 31 + Mathf.RoundToInt(color.g * 255f);
            key = key * 31 + Mathf.RoundToInt(color.b * 255f);
            key = key * 31 + Mathf.RoundToInt(color.a * 255f);
        }

        Sprite cached;
        if (PersonFrameSprites.TryGetValue(key, out cached) && cached != null)
            return cached;

        const int along = 32;
        const int across = 16;
        bool vertical = edge == 1 || edge == 3;
        int width = vertical ? across : along;
        int height = vertical ? along : across;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float alongU;
                float outer;
                if (edge == 0)
                {
                    alongU = (x + 0.5f) / width;
                    outer = (y + 0.5f) / height;
                }
                else if (edge == 1)
                {
                    alongU = (y + 0.5f) / height;
                    outer = (x + 0.5f) / width;
                }
                else if (edge == 2)
                {
                    alongU = (x + 0.5f) / width;
                    outer = 1f - (y + 0.5f) / height;
                }
                else
                {
                    alongU = (y + 0.5f) / height;
                    outer = 1f - (x + 0.5f) / width;
                }

                float t;
                if (edge == 0)
                    t = alongU * 0.25f;
                else if (edge == 1)
                    t = 0.5f - alongU * 0.25f;
                else if (edge == 2)
                    t = 0.75f - alongU * 0.25f;
                else
                    t = 0.75f + alongU * 0.25f;
                Color sample = SampleFrameColor(colors, t);
                sample.a *= FrameOuterAlpha(outer);
                texture.SetPixel(x, y, sample);
            }
        }
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        PersonFrameSprites[key] = sprite;
        return sprite;
    }

    static float FrameOuterAlpha(float outer)
    {
        float blurPortion = PersonFrameBlur / PersonFrameThickness;
        float solidEnd = 1f - blurPortion;
        if (outer <= solidEnd)
            return 1f;
        float fade = 1f - (outer - solidEnd) / blurPortion;
        return fade * fade * (3f - 2f * fade);
    }

    static Color SampleFrameColor(Color[] colors, float t)
    {
        int count = colors.Length;
        float scaled = t * count;
        int index = Mathf.FloorToInt(scaled);
        if (index >= count)
            index = count - 1;
        if (index < 0)
            index = 0;
        float blend = scaled - index;
        return Color.Lerp(colors[index], colors[(index + 1) % count], blend);
    }

    static void AddFrameEdge(Transform parent, string name, bool horizontal, bool end, float thickness, float inset)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, end ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, end ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, end ? 1f : 0f);
            rect.sizeDelta = new Vector2(-inset * 2f, thickness);
            rect.anchoredPosition = new Vector2(0f, end ? -inset : inset);
        }
        else
        {
            rect.anchorMin = new Vector2(end ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(end ? 1f : 0f, 1f);
            rect.pivot = new Vector2(end ? 1f : 0f, 0.5f);
            rect.sizeDelta = new Vector2(thickness, -inset * 2f);
            rect.anchoredPosition = new Vector2(end ? -inset : inset, 0f);
        }
    }

    public static Color PersonColor(int id)
    {
        if (id == PersonIds.TeacherBlack)
            return new Color(0.15f, 0.15f, 0.18f, 1f);
        if (id == PersonIds.ClassmateGreen)
            return new Color(0.45f, 0.8f, 0.4f, 1f);
        if (id == PersonIds.ClassmateRed)
            return new Color(0.9f, 0.35f, 0.3f, 1f);
        if (id == PersonIds.ClassmateBlue)
            return new Color(0.35f, 0.55f, 0.9f, 1f);
        return new Color(0.92f, 0.9f, 0.82f, 1f);
    }

    public string AuthorName(CharacterClass character)
    {
        if (character == null || character.AuthorPersonId <= 0)
            return "なし";
        PersonClass person = PersonAt(character.AuthorPersonId);
        if (person == null)
            return "なし";
        return person.Name;
    }

    public void ApplyPersonStatsToAllies(List<BattleUnit> units)
    {
        if (units == null)
            return;
        AllyAuthorship[] allies = AllyList(units);
        for (int i = 0; i < units.Count; i++)
        {
            if (!units[i].IsAlly)
                continue;
            if (units[i].RawHpMax <= 0)
            {
                units[i].RawHpMax = units[i].HpMax;
                units[i].RawAtk = units[i].Atk;
                units[i].RawDef = units[i].Def;
            }
            ApplyPersonStats(units[i], allies);
            units[i].Hp = units[i].HpMax;
        }
    }

    public void ApplyPersonStats(BattleUnit unit)
    {
        ApplyPersonStats(unit, ControllerBattle != null ? ControllerBattle.AllyAuthorships() : null);
    }

    void ApplyPersonStats(BattleUnit unit, AllyAuthorship[] allies)
    {
        if (unit == null || !unit.IsAlly)
            return;
        CharacterClass character = GetCharacter(unit.CharacterId);
        if (character == null)
            return;
        if (unit.RawHpMax <= 0)
            unit.RawHpMax = unit.HpMax > 0 ? unit.HpMax : 1;
        double applied = PersonFormulas.MultiplierForAllies(
            character.NextLifeMultiplier(PersonBalance),
            BattlePeopleInputs(),
            allies,
            unit.Attribute,
            PersonBalance);
        unit.HpMax = PersonFormulas.ScaleStat(unit.RawHpMax, applied);
        if (unit.HpMax < 1)
            unit.HpMax = 1;
        unit.Atk = PersonFormulas.ScaleStat(unit.RawAtk, applied);
        unit.Def = PersonFormulas.ScaleStat(unit.RawDef, applied);
    }

    public long PreviewStat(CharacterClass character, int setIndex, long raw, CharacterAttribute attribute)
    {
        if (character == null)
            return raw < 0 ? 0 : raw;
        AllyAuthorship[] allies = PartyAllies(setIndex);
        double applied = PersonFormulas.MultiplierForAllies(
            character.NextLifeMultiplier(PersonBalance),
            BattlePeopleInputs(setIndex),
            allies,
            attribute,
            PersonBalance);
        long scaled = PersonFormulas.ScaleStat(raw, applied);
        return scaled < 0 ? 0 : scaled;
    }

    PersonBattleInput[] BattlePeopleInputs()
    {
        return BattlePeopleInputs(ActiveBattlePartySet);
    }

    PersonBattleInput[] BattlePeopleInputs(int setIndex)
    {
        if (!IsBattlePersonSlot(setIndex, 1) && setIndex != ActiveBattlePartySet)
            return new PersonBattleInput[0];
        if (setIndex < 1 || setIndex >= BattlePartyPersonIds.GetLength(0))
            return new PersonBattleInput[0];
        var list = new List<PersonBattleInput>();
        for (int slot = 1; slot <= Constants.BATTLE_PERSON_SLOT_NUM; slot++)
        {
            PersonClass person = PersonAt((int)BattlePartyPersonIds[setIndex, slot]);
            if (person != null)
                list.Add(person.ToBattleInput());
        }
        return list.ToArray();
    }

    AllyAuthorship[] AllyList(List<BattleUnit> units)
    {
        var list = new List<AllyAuthorship>();
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.IsAlly)
                continue;
            CharacterClass character = GetCharacter(unit.CharacterId);
            list.Add(new AllyAuthorship
            {
                AuthorPersonId = character != null ? character.AuthorPersonId : 0,
                Type = unit.Attribute
            });
        }
        return list.ToArray();
    }

    AllyAuthorship[] PartyAllies(int setIndex)
    {
        var list = new List<AllyAuthorship>();
        if (setIndex < 1 || setIndex > Constants.BATTLE_PARTY_SET_NUM)
            return list.ToArray();
        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                CharacterClass character = GetCharacter(BattlePartyCharacterIds[setIndex, x, y]);
                if (character == null || character.OwnedNumCur == 0)
                    continue;
                list.Add(new AllyAuthorship
                {
                    AuthorPersonId = character.AuthorPersonId,
                    Type = BattleCharacterFactory.ToAttribute(character.CharacterType)
                });
            }
        }
        return list.ToArray();
    }

    public void CollectVisibleQuests(List<QuestDef> dest)
    {
        if (dest == null)
            return;
        EnsureQuestDefaults();
        LatchQuests();
        dest.Clear();
        QuestDef[] all = QuestCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            if (IsQuestListed(all[i]))
                dest.Add(all[i]);
        }
        dest.Sort(CompareVisibleQuests);
    }

    public bool IsQuestCleared(QuestDef def)
    {
        QuestRecord record;
        return def != null && Quests.TryGet(def.Id, out record) && record.Cleared;
    }

    public bool IsQuestClaimed(QuestDef def)
    {
        QuestRecord record;
        return def != null && Quests.TryGet(def.Id, out record) && record.Claimed;
    }

    public bool IsQuestClaimable(QuestDef def)
    {
        return IsQuestCleared(def) && !IsQuestClaimed(def);
    }

    public bool TryQuestRecord(int id, out QuestRecord record)
    {
        return Quests.TryGet(id, out record);
    }

    public bool IsQuestLocked(QuestDef def)
    {
        QuestRecord record;
        return def != null && Quests.TryGet(def.Id, out record) && record.Locked;
    }

    public bool IsQuestChainLocked(QuestDef def)
    {
        if (def == null)
            return false;
        QuestDef[] all = QuestCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            if (!IsQuestLocked(all[i]))
                continue;
            if (QuestCatalog.SameDisplayChain(all[i], def))
                return true;
        }
        return false;
    }

    public void ToggleQuestLock(int id)
    {
        EnsureQuestDefaults();
        QuestDef def = QuestCatalog.Find(id);
        if (def == null)
            return;
        if (IsQuestChainLocked(def))
            ClearChainLocks(def);
        else
            Quests.GetOrCreate(def.Id).Locked = true;
        SaveGame();
        if (ControllerBattle != null)
            ControllerBattle.RefreshQuestBoard();
    }

    void ClearChainLocks(QuestDef def)
    {
        QuestDef[] all = QuestCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            QuestRecord record;
            if (!Quests.TryGet(all[i].Id, out record) || !record.Locked)
                continue;
            if (QuestCatalog.SameDisplayChain(all[i], def))
                record.Locked = false;
        }
    }

    public QuestDef DisplayQuest(List<QuestDef> visible)
    {
        QuestDef fallback = null;
        QuestDef locked = null;
        QuestDef lockedShown = null;
        if (visible != null)
        {
            for (int i = 0; i < visible.Count; i++)
            {
                QuestDef def = visible[i];
                if (def == null || IsQuestClaimed(def))
                    continue;
                if (fallback == null || def.Id < fallback.Id)
                    fallback = def;
            }
        }
        QuestDef[] all = QuestCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            if (!IsQuestLocked(all[i]))
                continue;
            QuestDef shown = YoungestUnclaimed(visible, all[i]);
            if (shown == null)
                continue;
            if (locked == null || all[i].Id < locked.Id)
            {
                locked = all[i];
                lockedShown = shown;
            }
        }
        if (lockedShown != null)
            return lockedShown;
        return fallback;
    }

    QuestDef YoungestUnclaimed(List<QuestDef> visible, QuestDef locked)
    {
        if (visible == null || locked == null)
            return null;
        QuestDef shown = null;
        for (int i = 0; i < visible.Count; i++)
        {
            QuestDef def = visible[i];
            if (def == null || IsQuestClaimed(def))
                continue;
            if (!QuestCatalog.SameDisplayChain(locked, def))
                continue;
            if (shown == null || def.Id < shown.Id)
                shown = def;
        }
        return shown;
    }

    public string QuestFraction(QuestDef def)
    {
        ulong current = QuestRaw(def);
        ulong target = QuestTarget(def);
        if (target > 0 && current > target)
            current = target;
        string text = UiNumber.Format(current) + "/" + UiNumber.Format(target);
        if (def != null && def.Metric == QuestMetric.StageFloor)
            text += "階";
        return text;
    }

    public float QuestBar(QuestDef def)
    {
        ulong target = QuestTarget(def);
        if (target == 0)
            return 1f;
        ulong current = QuestRaw(def);
        if (current >= target)
            return 1f;
        return (float)((double)current / target);
    }

    public string QuestWarning(QuestDef def, int choice)
    {
        if (def == null || !IsQuestClaimable(def))
            return "";
        if (def.Reward == QuestRewardKind.RgbAll)
            return RgbRewardClips(def.RewardAmount) ? "上限を超える分は破棄されます" : "";
        if (def.Reward == QuestRewardKind.ChooseSlots || def.Reward == QuestRewardKind.TapChooseSlot)
        {
            bool pixelFull = Slots.PixelSlots >= Constants.PRODUCTION_SLOT_MAX;
            bool characterFull = Slots.CharacterSlots >= Constants.PRODUCTION_SLOT_MAX;
            if (choice == 1 && pixelFull)
                return "生産枠は255までです";
            if (choice == 2 && characterFull)
                return "生産枠は255までです";
            if (choice == 0 && pixelFull && characterFull)
                return "生産枠は255までです";
            return "";
        }
        if (def.Reward == QuestRewardKind.ImportCap && Quests.ImportCap >= ImportSizeRules.MaxCount)
            return "読み込み上限は500までです";
        if (def.Reward == QuestRewardKind.CharacterBundle)
        {
            bool slotsFull = Slots.PixelSlots >= Constants.PRODUCTION_SLOT_MAX
                || Slots.CharacterSlots >= Constants.PRODUCTION_SLOT_MAX;
            bool importFull = Quests.ImportCap >= ImportSizeRules.MaxCount;
            if (slotsFull && importFull)
                return "生産枠は255までです\n読み込み上限は500までです";
            if (slotsFull)
                return "生産枠は255までです";
            if (importFull)
                return "読み込み上限は500までです";
        }
        if (def.Reward == QuestRewardKind.BothSlots
            && (Slots.PixelSlots >= Constants.PRODUCTION_SLOT_MAX
                || Slots.CharacterSlots >= Constants.PRODUCTION_SLOT_MAX))
            return "生産枠は255までです";
        return "";
    }

    public bool ClaimQuest(int id, int choice)
    {
        EnsureQuestDefaults();
        LatchQuests();
        QuestDef def = QuestCatalog.Find(id);
        if (def == null || !IsQuestClaimable(def))
            return false;
        if (QuestCatalog.ChoosesProductionSlot(def))
        {
            if (choice != 1 && choice != 2)
                return false;
        }
        else
            choice = 0;

        ApplyQuestReward(def, choice);
        QuestRecord record = Quests.GetOrCreate(def.Id);
        record.Cleared = true;
        record.Claimed = true;
        record.Choice = choice;
        NoteQuestAdBonus(def);
        ImportedCharacters.SetImportCap(Quests.ImportCap);
        RefreshUnlockedProductionSlots();
        ApplyMainFeatureLocks();
        if (ControllerBattleStage != null)
            ControllerBattleStage.Refresh();
        if (ControllerBattle != null)
            ControllerBattle.RefreshQuestBoard();
        UpdateRGBProductionScene();
        RefreshProductionFigures();
        SaveGame();
        return true;
    }

    void NoteQuestAdBonus(QuestDef def)
    {
        QuestAdBonus bonus = QuestCatalog.AdBonus(def);
        if (bonus == QuestAdBonus.Pixel)
        {
            if (Slots.ClaimedPixelAdQuests < int.MaxValue)
                Slots.ClaimedPixelAdQuests++;
        }
        else if (bonus == QuestAdBonus.Character)
        {
            if (Slots.ClaimedCharacterAdQuests < int.MaxValue)
                Slots.ClaimedCharacterAdQuests++;
        }
        else if (bonus == QuestAdBonus.Both)
        {
            if (Slots.ClaimedQuests < int.MaxValue)
                Slots.ClaimedQuests++;
        }
    }

    void ApplyQuestReward(QuestDef def, int choice)
    {
        if (def.Reward == QuestRewardKind.ChooseSlots || def.Reward == QuestRewardKind.TapChooseSlot)
        {
            int amount = def.RewardAmount > int.MaxValue ? int.MaxValue : (int)def.RewardAmount;
            if (amount < 1)
                amount = 1;
            AddProductionSlots(amount, choice == 1, choice == 2);
            if (def.Reward == QuestRewardKind.TapChooseSlot)
                BoostButtonTapAmounts();
        }
        else if (def.Reward == QuestRewardKind.ImportCap)
            RaiseImportCap(1);
        else if (def.Reward == QuestRewardKind.RgbAll)
        {
            ulong add = def.RewardAmount > 0 ? (ulong)def.RewardAmount : 0;
            GainRgb(ref CurR, MaxR, add, 0);
            GainRgb(ref CurG, MaxG, add, 1);
            GainRgb(ref CurB, MaxB, add, 2);
        }
        else if (def.Reward == QuestRewardKind.CharacterBundle)
        {
            RaiseImportCap(1);
            AddProductionSlots(1, true, true);
        }
        else if (def.Reward == QuestRewardKind.FusionUnlock)
        {
            Quests.FusionUnlocked = true;
            ApplyFusionButtonLock();
        }
        else if (def.Reward == QuestRewardKind.FusionEfficiency)
        {
            int amount = def.RewardAmount > int.MaxValue ? int.MaxValue : (int)def.RewardAmount;
            if (amount < 1)
                amount = 1;
            long next = (long)Quests.FusionEfficiency + amount;
            if (next > int.MaxValue)
                next = int.MaxValue;
            Quests.FusionEfficiency = (int)next;
            FusionBonus.ExtraPercent = Quests.FusionEfficiency;
            RebuildAllCharacterStats();
        }
        else if (def.Reward == QuestRewardKind.GrantPalette)
            GrantProduciblePixels(def.RewardAmount > 0 ? (ulong)def.RewardAmount : 0);
        else if (def.Reward == QuestRewardKind.BothSlots)
        {
            int amount = def.RewardAmount > int.MaxValue ? int.MaxValue : (int)def.RewardAmount;
            if (amount < 1)
                amount = 1;
            AddProductionSlots(amount, true, true);
        }
    }

    void RaiseImportCap(int amount)
    {
        if (amount < 1)
            return;
        long next = (long)Quests.ImportCap + amount;
        if (next > ImportSizeRules.MaxCount)
            next = ImportSizeRules.MaxCount;
        if (next < Constants.IMPORT_CHARACTER_CAP_INITIAL)
            next = Constants.IMPORT_CHARACTER_CAP_INITIAL;
        Quests.ImportCap = (int)next;
    }

    void EnsureQuestDefaults()
    {
        if (Quests.DefaultsApplied)
        {
            ImportedCharacters.SetImportCap(Quests.ImportCap);
            return;
        }
        if (!Quests.HasTotalR)
            Quests.TotalR = CurR;
        if (!Quests.HasTotalG)
            Quests.TotalG = CurG;
        if (!Quests.HasTotalB)
            Quests.TotalB = CurB;
        if (!Quests.HasPixel)
            Quests.PixelCompletions = 0;
        if (!Quests.HasCharacter)
            Quests.CharacterCompletions = SumOwnedCharacters();
        if (!Quests.HasGallery)
            Quests.GalleryClears = 0;
        if (!Quests.HasEnemy)
            Quests.EnemyDefeats = 0;
        if (!Quests.HasFusionUnlocked)
            Quests.FusionUnlocked = ClearedBattleStage >= 1;
        if (!Quests.HasFusionEfficiency || Quests.FusionEfficiency < 0)
            Quests.FusionEfficiency = 0;
        FusionBonus.ExtraPercent = Quests.FusionEfficiency;
        if (!Quests.HasImport || Quests.ImportCap < Constants.IMPORT_CHARACTER_CAP_INITIAL)
            Quests.ImportCap = Constants.IMPORT_CHARACTER_CAP_INITIAL;
        if (Quests.ImportCap > ImportSizeRules.MaxCount)
            Quests.ImportCap = ImportSizeRules.MaxCount;
        Quests.DefaultsApplied = true;
        ImportedCharacters.SetImportCap(Quests.ImportCap);
        MigrateLegacyInitialSlots();
        LatchQuests();
        RebuildAllCharacterStats();
        RefreshUnlockedProductionSlots();
        ApplyFusionButtonLock();
        ApplyMainFeatureLocks();
    }

    void MigrateLegacyInitialSlots()
    {
        if (Slots.PixelSlots == 5 && Slots.AdPixelSlots == 0)
            Slots.PixelSlots = Constants.PRODUCTION_SLOT_INITIAL;
        if (Slots.CharacterSlots == 5 && Slots.AdCharacterSlots == 0)
            Slots.CharacterSlots = Constants.PRODUCTION_SLOT_INITIAL;
    }

    void RebuildAllCharacterStats()
    {
        if (BattleBalance == null)
            return;
        if (CharactersAll != null)
        {
            int last = CharactersAll.Length - 1;
            if (last > Constants.CHARACTERS_ALL_NUM)
                last = Constants.CHARACTERS_ALL_NUM;
            for (int i = 1; i <= last; i++)
            {
                if (CharactersAll[i] != null && CharactersAll[i].ID != 0)
                    CharactersAll[i].RebuildLevelStats(BattleBalance);
            }
        }
        ImportedCharacters.ForEach(character =>
        {
            if (character != null && character.ID != 0)
                character.RebuildLevelStats(BattleBalance);
        });
    }

    void LatchQuests()
    {
        QuestDef[] all = QuestCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            if (!QuestReached(all[i]))
                continue;
            QuestRecord record = Quests.GetOrCreate(all[i].Id);
            record.Cleared = true;
        }
    }

    bool IsQuestListed(QuestDef def)
    {
        if (def == null)
            return false;
        if (def.Main)
        {
            if (IsQuestClaimed(def))
                return false;
            QuestDef previousMain = QuestCatalog.PreviousMain(def);
            if (previousMain != null && !IsQuestCleared(previousMain))
                return false;
            return true;
        }
        if (def.Metric == QuestMetric.StageFloor && def.Arg == Constants.BATTLE_STAGE_NUM)
        {
            if (!IsBattleStageUnlocked(def.Arg - 1) && !IsQuestCleared(def) && !IsQuestClaimed(def))
                return false;
        }
        if (QuestCatalog.ChainCount(def) <= 1)
        {
            if (def.Shown)
                return true;
            QuestRecord record;
            return Quests.TryGet(def.Id, out record) && (record.Cleared || record.Claimed);
        }
        if (!def.Shown && !IsQuestCleared(def) && !IsQuestClaimed(def))
            return false;
        if (IsQuestClaimed(def))
            return false;
        QuestDef previous = QuestCatalog.PreviousInChain(def);
        if (previous != null && !IsQuestCleared(previous))
            return false;
        return true;
    }

    int CompareVisibleQuests(QuestDef a, QuestDef b)
    {
        int rank = QuestRank(a) - QuestRank(b);
        if (rank != 0)
            return rank;
        return a.Id - b.Id;
    }

    int QuestRank(QuestDef def)
    {
        if (IsQuestClaimed(def))
            return 2;
        if (IsQuestCleared(def))
            return 0;
        return 1;
    }

    bool QuestReached(QuestDef def)
    {
        return QuestRaw(def) >= QuestTarget(def);
    }

    ulong QuestTarget(QuestDef def)
    {
        if (def == null || def.Target <= 0)
            return 0;
        return (ulong)def.Target;
    }

    ulong QuestRaw(QuestDef def)
    {
        if (def == null)
            return 0;
        if (def.Metric == QuestMetric.StageFloor)
        {
            int floor = GetHighestClearedBattleFloor(def.Arg);
            return floor > 0 ? (ulong)floor : 0;
        }
        if (def.Metric == QuestMetric.GalleryClears)
            return def.Arg == Constants.BATTLE_STAGE_GALLERY && Quests.GalleryClears > 0
                ? (ulong)Quests.GalleryClears : 0;
        if (def.Metric == QuestMetric.RgbProduced)
        {
            if (def.Arg == 0)
                return Quests.TotalR;
            if (def.Arg == 1)
                return Quests.TotalG;
            return Quests.TotalB;
        }
        if (def.Metric == QuestMetric.PixelCompletions)
            return Quests.PixelCompletions;
        if (def.Metric == QuestMetric.CharacterCompletions)
            return Quests.CharacterCompletions;
        if (def.Metric == QuestMetric.EnemyDefeats)
            return Quests.EnemyDefeats;
        if (def.Metric == QuestMetric.ImportedOfSize)
        {
            int count = ImportedCharacters.CountOfSize(def.Arg);
            return count > 0 ? (ulong)count : 0;
        }
        if (def.Metric == QuestMetric.FusionCount)
            return SumFusionCount();
        if (def.Metric == QuestMetric.StageCompanions)
            return CountStageCompanions();
        if (def.Metric == QuestMetric.RgbHeld)
            return LowestRgbHeld();
        if (def.Metric == QuestMetric.BlackPixels)
            return PixelStock(0, 0, 0);
        if (def.Metric == QuestMetric.PrimaryPixels)
            return LowestPrimaryPixel();
        if (def.Metric == QuestMetric.MiniSlimes)
            return LowestMiniSlimeOwned();
        return 0;
    }

    public bool IsMainFeatureOpen(int feature)
    {
        QuestDef[] all = QuestCatalog.All;
        for (int i = 0; i < all.Length; i++)
        {
            QuestDef def = all[i];
            if (def == null || !def.Main || def.Reward != QuestRewardKind.UnlockFeature || def.RewardAmount != feature)
                continue;
            return IsQuestClaimed(def);
        }
        return false;
    }

    void ApplyMainFeatureLocks()
    {
        SetFeatureButton("ButtonSelectScenePixelProduction", IsMainFeatureOpen(MainFeature.PixelScreen));
        SetFeatureButton("ButtonSelectSceneCharacterProduction", IsMainFeatureOpen(MainFeature.CharacterScreen));
        SetFeatureButton("ButtonSelectSceneBattleParty", IsMainFeatureOpen(MainFeature.Battle));
        SetFeatureButton("ButtonSelectSceneBattleStage", IsMainFeatureOpen(MainFeature.Battle));
        if (ControllerBattle != null)
            ControllerBattle.RefreshFeatureLocks();
    }

    void SetFeatureButton(string name, bool open)
    {
        GameObject bar = GameObject.Find("PanelSelectScene");
        if (bar == null)
            return;
        Transform child = bar.transform.Find(name);
        if (child == null)
            return;
        Button button = child.GetComponent<Button>();
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        Text label = button.GetComponentInChildren<Text>();
        if (!FeatureButtonImageColors.ContainsKey(name))
        {
            if (image != null)
                FeatureButtonImageColors[name] = image.color;
            if (label != null)
                FeatureButtonTextColors[name] = label.color;
        }

        button.interactable = open;
        Color imageColor;
        if (image != null && FeatureButtonImageColors.TryGetValue(name, out imageColor))
            image.color = open ? imageColor : new Color(0.45f, 0.45f, 0.45f, imageColor.a);
        Color textColor;
        if (label != null && FeatureButtonTextColors.TryGetValue(name, out textColor))
            label.color = open ? textColor : new Color(0.62f, 0.62f, 0.62f, textColor.a);
    }

    static readonly int[] ProducibleChannels = { 0, 32, 64, 96, 128, 160, 192, 224, 255 };

    void GrantProduciblePixels(ulong amount)
    {
        if (amount == 0 || CurPixels == null)
            return;
        for (int r = 0; r < ProducibleChannels.Length; r++)
        {
            for (int g = 0; g < ProducibleChannels.Length; g++)
            {
                for (int b = 0; b < ProducibleChannels.Length; b++)
                {
                    int red = ProducibleChannels[r];
                    int green = ProducibleChannels[g];
                    int blue = ProducibleChannels[b];
                    ulong stock = CurPixels[red, green, blue];
                    CurPixels[red, green, blue] = stock > ulong.MaxValue - amount ? ulong.MaxValue : stock + amount;
                }
            }
        }
    }

    ulong LowestRgbHeld()
    {
        ulong lowest = CurR;
        if (CurG < lowest)
            lowest = CurG;
        if (CurB < lowest)
            lowest = CurB;
        return lowest;
    }

    ulong PixelStock(int r, int g, int b)
    {
        if (CurPixels == null || r < 0 || g < 0 || b < 0 || r > 255 || g > 255 || b > 255)
            return 0;
        return CurPixels[r, g, b];
    }

    ulong LowestPrimaryPixel()
    {
        ulong lowest = PixelStock(255, 0, 0);
        ulong green = PixelStock(0, 255, 0);
        ulong blue = PixelStock(0, 0, 255);
        if (green < lowest)
            lowest = green;
        if (blue < lowest)
            lowest = blue;
        return lowest;
    }

    ulong LowestMiniSlimeOwned()
    {
        ulong lowest = OwnedCount(1);
        ulong green = OwnedCount(2);
        ulong blue = OwnedCount(3);
        if (green < lowest)
            lowest = green;
        if (blue < lowest)
            lowest = blue;
        return lowest;
    }

    ulong OwnedCount(int id)
    {
        if (CharactersAll == null || id < 0 || id >= CharactersAll.Length || CharactersAll[id] == null)
            return 0;
        return CharactersAll[id].OwnedNumCur;
    }

    bool RgbRewardClips(long amount)
    {
        if (amount <= 0)
            return false;
        ulong add = (ulong)amount;
        return RgbChannelClips(CurR, MaxR, add)
            || RgbChannelClips(CurG, MaxG, add)
            || RgbChannelClips(CurB, MaxB, add);
    }

    static bool RgbChannelClips(ulong current, ulong max, ulong add)
    {
        if (current >= max)
            return true;
        return max - current < add;
    }

    void NoteRgbProduced(int channel, ulong added)
    {
        if (added == 0)
            return;
        if (channel == 0)
            Quests.TotalR = AddUlong(Quests.TotalR, added);
        else if (channel == 1)
            Quests.TotalG = AddUlong(Quests.TotalG, added);
        else
            Quests.TotalB = AddUlong(Quests.TotalB, added);
    }

    void NotePixelCompletion()
    {
        if (!Quests.DefaultsApplied || Quests.PixelCompletions >= ulong.MaxValue)
            return;
        Quests.PixelCompletions++;
    }

    void NoteCharacterCompletions(int count)
    {
        if (!Quests.DefaultsApplied || count < 1)
            return;
        Quests.CharacterCompletions = AddUlong(Quests.CharacterCompletions, (ulong)count);
    }

    void NoteGalleryClear()
    {
        if (!Quests.DefaultsApplied || Quests.GalleryClears >= long.MaxValue)
            return;
        Quests.GalleryClears++;
    }

    void BoostButtonTapAmounts()
    {
        IncreaseValueR = BoostTapAmount(IncreaseValueR);
        IncreaseValueG = BoostTapAmount(IncreaseValueG);
        IncreaseValueB = BoostTapAmount(IncreaseValueB);
    }

    static ulong BoostTapAmount(ulong value)
    {
        if (value == 0)
            return 0;
        if (value > (ulong.MaxValue - 99) / 110)
            return ulong.MaxValue;
        ulong boosted = (value * 110 + 99) / 100;
        if (boosted <= value)
            boosted = value + 1;
        return boosted;
    }

    readonly HashSet<uint> StageCharacterIds = new HashSet<uint>();
    bool StageCharacterIdsReady;

    ulong CountStageCompanions()
    {
        EnsureStageCharacterIds();
        ulong count = 0;
        foreach (uint id in StageCharacterIds)
        {
            if (id == 0 || CharactersAll == null || id >= CharactersAll.Length)
                continue;
            CharacterClass character = CharactersAll[id];
            if (character == null || character.ID != id || character.OwnedNumMax == 0)
                continue;
            count++;
        }
        return count;
    }

    void EnsureStageCharacterIds()
    {
        if (StageCharacterIdsReady)
            return;
        StageCharacterIds.Clear();
        for (int stage = 1; stage <= Constants.BATTLE_STAGE_NUM; stage++)
        {
            StageBattleContent content = BattleStageCatalog.Get(stage);
            if (content == null)
                continue;
            if (content.Bands != null)
            {
                for (int bandIndex = 0; bandIndex < content.Bands.Length; bandIndex++)
                {
                    FloorBandSpawn band = content.Bands[bandIndex];
                    if (band == null || band.Entries == null)
                        continue;
                    for (int entryIndex = 0; entryIndex < band.Entries.Count; entryIndex++)
                    {
                        EnemySpawnEntry entry = band.Entries[entryIndex];
                        if (entry != null && entry.CharacterId != 0)
                            StageCharacterIds.Add(entry.CharacterId);
                    }
                }
            }
            if (content.Bosses == null)
                continue;
            for (int bossIndex = 0; bossIndex < content.Bosses.Count; bossIndex++)
            {
                BossFloorSpawn boss = content.Bosses[bossIndex];
                if (boss != null && boss.CharacterId != 0)
                    StageCharacterIds.Add(boss.CharacterId);
            }
        }
        StageCharacterIdsReady = true;
    }

    ulong SumFusionCount()
    {
        ulong sum = 0;
        if (CharactersAll != null)
        {
            int last = CharactersAll.Length - 1;
            if (last > Constants.CHARACTERS_ALL_NUM)
                last = Constants.CHARACTERS_ALL_NUM;
            for (int i = 1; i <= last; i++)
            {
                CharacterClass character = CharactersAll[i];
                if (character == null || character.ID != (uint)i)
                    continue;
                sum = AddUlong(sum, character.FusionCount);
            }
        }
        ImportedCharacters.ForEach(character =>
        {
            if (character != null)
                sum = AddUlong(sum, character.FusionCount);
        });
        return sum;
    }

    ulong SumOwnedCharacters()
    {
        ulong sum = 0;
        if (CharactersAll != null)
        {
            int last = CharactersAll.Length - 1;
            if (last > Constants.CHARACTERS_ALL_NUM)
                last = Constants.CHARACTERS_ALL_NUM;
            for (int i = 1; i <= last; i++)
            {
                CharacterClass character = CharactersAll[i];
                if (character == null || character.ID != (uint)i)
                    continue;
                sum = AddUlong(sum, character.OwnedNumCur);
            }
        }
        ImportedCharacters.ForEach(character =>
        {
            if (character != null)
                sum = AddUlong(sum, character.OwnedNumCur);
        });
        return sum;
    }

    static ulong AddUlong(ulong current, ulong add)
    {
        if (ulong.MaxValue - current < add)
            return ulong.MaxValue;
        return current + add;
    }

    public void ClaimQuestProductionReward()
    {
        if (Slots.ClaimedQuests < int.MaxValue)
            Slots.ClaimedQuests++;
        AddProductionSlots(Constants.QUEST_PRODUCTION_SLOT_REWARD, true, true);
        RefreshUnlockedProductionSlots();
        SaveGame();
    }

    public bool GrantPurchasedProductionSlots(int amount)
    {
        bool already;
        if (amount == 1)
            already = Slots.IapPlus1;
        else if (amount == 3)
            already = Slots.IapPlus3;
        else if (amount == 5)
            already = Slots.IapPlus5;
        else if (amount == 10)
            already = Slots.IapPlus10;
        else
            return false;
        if (already)
            return false;
        if (amount == 1)
            Slots.IapPlus1 = true;
        else if (amount == 3)
            Slots.IapPlus3 = true;
        else if (amount == 5)
            Slots.IapPlus5 = true;
        else
            Slots.IapPlus10 = true;
        AddProductionSlots(amount, true, true);
        RefreshUnlockedProductionSlots();
        SaveGame();
        return true;
    }

    void PushButtonAdPixelSlot()
    {
        ShowRewardedForSlot(true);
    }

    void PushButtonAdCharacterSlot()
    {
        ShowRewardedForSlot(false);
    }

    bool CanWatchAd(bool pixel)
    {
        return RemainingAdTenths(pixel) >= 10;
    }

    // 1回を10とする。ピクセルは1件で5、キャラは1件で3。初期の1回は10。
    static int AdCreditTenths(int quests, bool pixel)
    {
        if (quests < 0)
            quests = 0;
        long bonus = pixel ? (long)quests * 5L : (long)quests * 3L;
        long total = 10L + bonus;
        if (total > int.MaxValue)
            total = int.MaxValue;
        return (int)total;
    }

    static string FormatAdTenths(int tenths)
    {
        if (tenths < 0)
            tenths = 0;
        return (tenths / 10).ToString() + "." + (tenths % 10).ToString();
    }

    int RemainingAdTenths(bool pixel)
    {
        int slots = pixel ? Slots.PixelSlots : Slots.CharacterSlots;
        int used = pixel ? Slots.AdPixelSlots : Slots.AdCharacterSlots;
        int fromQuests = pixel ? Slots.ClaimedPixelAdQuests : Slots.ClaimedCharacterAdQuests;
        int quests = Slots.ClaimedQuests + fromQuests;
        int credit = AdCreditTenths(quests, pixel) - used * 10;
        int slotLeft = (Constants.PRODUCTION_SLOT_MAX - slots) * 10;
        if (credit < 0)
            credit = 0;
        if (slotLeft < 0)
            slotLeft = 0;
        return credit < slotLeft ? credit : slotLeft;
    }

    void ShowRewardedForSlot(bool pixel)
    {
        if (_adBusy || !CanWatchAd(pixel))
            return;
        if (AdWatchConfirm)
        {
            ShowConfirm("広告が流れますよろしいですか？", () => PlayRewardedForSlot(pixel));
            return;
        }
        PlayRewardedForSlot(pixel);
    }

    void PlayRewardedForSlot(bool pixel)
    {
        if (_adBusy || !CanWatchAd(pixel))
            return;
#if UNITY_ANDROID || UNITY_EDITOR
        _adBusy = true;
        if (!AdService.TryShowRewarded(
            () =>
            {
                _adBusy = false;
                GrantAdSlot(pixel);
            },
            () => { _adBusy = false; }))
        {
            _adBusy = false;
        }
#else
        Debug.LogWarning("広告はAndroidで表示します。");
#endif
    }

    GameObject AdConfirm;
    Text _adConfirmText;
    Action _adConfirmYes;

    void ShowConfirm(string message, Action onYes)
    {
        _adConfirmYes = onYes;
        EnsureAdConfirm();
        if (AdConfirm == null)
        {
            _adConfirmYes = null;
            return;
        }
        if (_adConfirmText != null)
            _adConfirmText.text = message;
        AdConfirm.SetActive(true);
        AdConfirm.transform.SetAsLastSibling();
    }

    void EnsureAdConfirm()
    {
        if (AdConfirm != null)
            return;
        GameObject canvas = GameObject.Find("CanvasProduction");
        if (canvas == null)
            return;

        AdConfirm = new GameObject("AdConfirm", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
        AdConfirm.transform.SetParent(canvas.transform, false);
        RectTransform root = AdConfirm.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.localScale = Vector3.one;
        Canvas overlay = AdConfirm.GetComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 4000;
        Image dim = AdConfirm.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.62f);
        dim.raycastTarget = true;

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(AdConfirm.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(820f, 420f);
        Image cardImage = card.GetComponent<Image>();
        cardImage.color = new Color(0.08f, 0.09f, 0.12f, 1f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(card.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.42f);
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(36f, 12f);
        textRect.offsetMax = new Vector2(-36f, -28f);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = "";
        _adConfirmText = text;

        Button yes = CreateAdConfirmButton(card.transform, "ButtonAdConfirmYes", "はい", new Color(0.25f, 0.45f, 0.75f, 1f), font, true);
        Button no = CreateAdConfirmButton(card.transform, "ButtonAdConfirmNo", "いいえ", new Color(0.75f, 0.25f, 0.25f, 1f), font, false);
        yes.onClick.AddListener(AcceptAdConfirm);
        no.onClick.AddListener(DismissAdConfirm);
        AdConfirm.SetActive(false);
    }

    static Button CreateAdConfirmButton(Transform parent, string name, string label, Color color, Font font, bool yes)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(yes ? 0.08f : 0.52f, 0.1f);
        rect.anchorMax = new Vector2(yes ? 0.48f : 0.92f, 0.38f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        obj.GetComponent<Image>().color = color;
        Button button = obj.GetComponent<Button>();
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(obj.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = label;
        return button;
    }

    void AcceptAdConfirm()
    {
        Action yes = _adConfirmYes;
        _adConfirmYes = null;
        if (AdConfirm != null)
            AdConfirm.SetActive(false);
        if (yes != null)
            yes();
    }

    void DismissAdConfirm()
    {
        _adConfirmYes = null;
        if (AdConfirm != null)
            AdConfirm.SetActive(false);
    }

    void GrantAdSlot(bool pixel)
    {
        if (!CanWatchAd(pixel))
            return;
        if (pixel)
            Slots.AdPixelSlots++;
        else
            Slots.AdCharacterSlots++;
        AddProductionSlots(1, pixel, !pixel);
        RefreshUnlockedProductionSlots();
        SaveGame();
    }

    void AddProductionSlots(int amount, bool pixel, bool character)
    {
        if (amount < 1)
            return;
        if (pixel)
            Slots.PixelSlots = ProductionSlotState.ClampCount(Slots.PixelSlots + amount);
        if (character)
            Slots.CharacterSlots = ProductionSlotState.ClampCount(Slots.CharacterSlots + amount);
    }

    void RefreshUnlockedProductionSlots()
    {
        EnsureProductionSlotRows();
        if (GameObject.Find("ContentPixelProductionList") != null)
        {
            for (int i = 1; i <= Slots.PixelSlots; i++)
            {
                UpdateCharacterPixelProduction(i);
                UpdatePixelColorPixelProduction(i);
                UpdateSliderPixelProduction(i);
            }
        }
        if (GameObject.Find("ContentCharacterProductionList") != null)
            UpdateProductionCharacterScene();
    }

    void EnsureProductionSlotRows()
    {
        Transform pixelList = GameObject.Find("ContentPixelProductionList") != null
            ? GameObject.Find("ContentPixelProductionList").transform : null;
        Transform characterList = GameObject.Find("ContentCharacterProductionList") != null
            ? GameObject.Find("ContentCharacterProductionList").transform : null;
        if (pixelList != null)
        {
            EnsureClonedRows(pixelList, "PrefabOnePixelProduction", Slots.PixelSlots);
            ApplySlotRowVisibility(pixelList, "PrefabOnePixelProduction", Slots.PixelSlots);
            EnsureAdSlotButton(pixelList, "ButtonAdUnlockPixelSlot", true);
        }
        if (characterList != null)
        {
            EnsureClonedRows(characterList, "PrefabOneCharacterProduction", Slots.CharacterSlots);
            ApplySlotRowVisibility(characterList, "PrefabOneCharacterProduction", Slots.CharacterSlots);
            EnsureAdSlotButton(characterList, "ButtonAdUnlockCharacterSlot", false);
        }
    }

    static void ApplySlotRowVisibility(Transform list, string rowPrefix, int count)
    {
        for (int i = 0; i < list.childCount; i++)
        {
            Transform child = list.GetChild(i);
            if (!child.name.StartsWith(rowPrefix, StringComparison.Ordinal))
                continue;
            int index = ProductionSlotIndex(child.name);
            if (index < 1)
                continue;
            child.gameObject.SetActive(index <= count);
        }
    }

    void EnsureClonedRows(Transform list, string rowPrefix, int count)
    {
        Transform template = list.Find(rowPrefix + "01");
        if (template == null || count <= Constants.PRODUCTION_SLOT_INITIAL)
            return;
        if (count > Constants.PRODUCTION_SLOT_MAX)
            count = Constants.PRODUCTION_SLOT_MAX;
        for (int i = Constants.PRODUCTION_SLOT_INITIAL + 1; i <= count; i++)
        {
            string suffix = i.ToString("00");
            if (list.Find(rowPrefix + suffix) != null)
                continue;
            GameObject clone = Instantiate(template.gameObject, list);
            clone.name = rowPrefix + suffix;
            RenameSlotTree(clone.transform, "01", suffix);
            RetargetSlotButtons(clone.transform);
        }
    }

    static void RenameSlotTree(Transform root, string fromSuffix, string toSuffix)
    {
        if (root.name.EndsWith(fromSuffix, StringComparison.Ordinal))
            root.name = root.name.Substring(0, root.name.Length - fromSuffix.Length) + toSuffix;
        for (int i = 0; i < root.childCount; i++)
            RenameSlotTree(root.GetChild(i), fromSuffix, toSuffix);
    }

    static void RetargetSlotButtons(Transform root)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            int count = button.onClick.GetPersistentEventCount();
            if (count == 0)
                continue;
            var calls = new List<KeyValuePair<UnityEngine.Object, string>>();
            for (int c = 0; c < count; c++)
                calls.Add(new KeyValuePair<UnityEngine.Object, string>(button.onClick.GetPersistentTarget(c), button.onClick.GetPersistentMethodName(c)));
            button.onClick = new Button.ButtonClickedEvent();
            for (int c = 0; c < calls.Count; c++)
            {
                UnityEngine.Object target = calls[c].Key;
                string methodName = calls[c].Value;
                Button captured = button;
                button.onClick.AddListener(() => InvokeNamed(target, methodName, captured.gameObject.name));
            }
        }
    }

    static void InvokeNamed(UnityEngine.Object target, string methodName, string argument)
    {
        if (target == null || string.IsNullOrEmpty(methodName))
            return;
        System.Reflection.MethodInfo method = target.GetType().GetMethod(methodName, new Type[] { typeof(string) });
        if (method != null)
        {
            method.Invoke(target, new object[] { argument });
            return;
        }
        method = target.GetType().GetMethod(methodName, Type.EmptyTypes);
        if (method != null)
            method.Invoke(target, null);
    }

    void EnsureAdSlotButton(Transform list, string buttonName, bool pixel)
    {
        int slots = pixel ? Slots.PixelSlots : Slots.CharacterSlots;
        Transform existing = list.Find(buttonName);
        if (slots >= Constants.PRODUCTION_SLOT_MAX)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            Transform hidden = list.Find("TextAdSlotRemain");
            if (hidden != null)
                hidden.gameObject.SetActive(false);
            return;
        }
        if (existing == null)
        {
            GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform));
            buttonObject.transform.SetParent(list, false);
            Image image = buttonObject.AddComponent<Image>();
            Texture2D pixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixelTexture.SetPixel(0, 0, Color.white);
            pixelTexture.Apply();
            image.sprite = Sprite.Create(pixelTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            image.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (pixel)
                button.onClick.AddListener(PushButtonAdPixelSlot);
            else
                button.onClick.AddListener(PushButtonAdCharacterSlot);

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 8f);
            textRect.offsetMax = new Vector2(-12f, -8f);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 96;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            existing = buttonObject.transform;
        }
        existing.gameObject.SetActive(true);
        LayoutElement buttonLayout = existing.GetComponent<LayoutElement>();
        if (buttonLayout == null)
            buttonLayout = existing.gameObject.AddComponent<LayoutElement>();
        buttonLayout.minHeight = 150f;
        buttonLayout.preferredHeight = 150f;
        buttonLayout.flexibleHeight = 0f;
        buttonLayout.flexibleWidth = 1f;

        Transform oldCount = existing.Find("TextAdCount");
        if (oldCount != null)
            Destroy(oldCount.gameObject);

        Text label = existing.Find("Text") != null ? existing.Find("Text").GetComponent<Text>() : null;
        int leftTenths = RemainingAdTenths(pixel);
        bool canWatch = leftTenths >= 10;
        if (label != null)
        {
            label.text = "+";
            label.fontSize = 96;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }
        Button slotButton = existing.GetComponent<Button>();
        if (slotButton != null)
            slotButton.interactable = canWatch;
        existing.SetAsLastSibling();

        Text count = AdRemainLabel(list, label);
        if (count != null)
        {
            count.gameObject.SetActive(true);
            count.text = "広告視聴可能回数：" + FormatAdTenths(leftTenths) + "回　クエスト達成により増加します";
            count.transform.SetAsLastSibling();
        }
    }

    Text AdRemainLabel(Transform list, Text sample)
    {
        Transform existing = list.Find("TextAdSlotRemain");
        Text text;
        if (existing != null)
        {
            text = existing.GetComponent<Text>();
        }
        else
        {
            GameObject textObject = new GameObject("TextAdSlotRemain", typeof(RectTransform));
            textObject.transform.SetParent(list, false);
            text = textObject.AddComponent<Text>();
            text.font = sample != null && sample.font != null
                ? sample.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.raycastTarget = false;
            existing = textObject.transform;
        }
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.05f;
        LayoutElement layout = existing.GetComponent<LayoutElement>();
        if (layout == null)
            layout = existing.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 84f;
        layout.preferredHeight = 84f;
        layout.flexibleHeight = 0f;
        layout.flexibleWidth = 1f;
        return text;
    }

    static bool IsProductionWorkerButton(Button button)
    {
        if (button == null)
            return false;
        string name = button.name;
        return name.Contains("RProductionHelpCharacter")
            || name.Contains("GProductionHelpCharacter")
            || name.Contains("BProductionHelpCharacter")
            || name.Contains("ButtonPixelProductionCharacter")
            || name.Contains("ButtonCharacterProductionCharacter");
    }

}

public class RgbButtonPressGlow : MonoBehaviour, IPointerDownHandler
{
    public ControllerProduction Production;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Production != null)
            Production.PlayButtonPressGlow(GetComponent<Button>());
    }
}

public class GaugeTextGlow : BaseMeshEffect
{
    public Color Glow = new Color(1f, 1f, 1f, 0.9f);
    public float Spread = 1.8f;

    readonly List<UIVertex> verts = new List<UIVertex>();

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;
        verts.Clear();
        vh.GetUIVertexStream(verts);
        int count = verts.Count;
        if (count == 0)
            return;

        vh.Clear();
        AddRing(vh, count, Spread, 0.32f);
        AddRing(vh, count, Spread * 0.45f, 0.62f);
        int origin = vh.currentVertCount;
        for (int i = 0; i < count; i++)
            vh.AddVert(verts[i]);
        for (int i = 0; i < count; i += 3)
            vh.AddTriangle(origin + i, origin + i + 1, origin + i + 2);
    }

    void AddRing(VertexHelper vh, int count, float radius, float alphaScale)
    {
        const int steps = 8;
        Color32 color = Glow;
        color.a = (byte)Mathf.Clamp(Mathf.RoundToInt(Glow.a * alphaScale * 255f), 0, 255);
        for (int step = 0; step < steps; step++)
        {
            float angle = step * Mathf.PI * 2f / steps;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            int start = vh.currentVertCount;
            for (int i = 0; i < count; i++)
            {
                UIVertex vert = verts[i];
                vert.position += offset;
                vert.color = color;
                vh.AddVert(vert);
            }
            for (int i = 0; i < count; i += 3)
                vh.AddTriangle(start + i, start + i + 1, start + i + 2);
        }
    }
}