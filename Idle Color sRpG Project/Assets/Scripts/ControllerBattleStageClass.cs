using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//バトルステージ選択パネルのコントロール
public class ControllerBattleStageClass : MonoBehaviour
{
    //マスの名前は「ImageBattleStageCell」+ x + y (例: ImageBattleStageCell13 は x=1, y=3)
    //編成パネルの ButtonBattlePartyCell とは別名。こちらは画像だけで、押しても編成できない
    public const string CELL_IMAGE_PREFIX = "ImageBattleStageCell";

    //ステージ番号は 1～BATTLE_STAGE_NUM。0 は未出撃
    static readonly string[] StageNames = { "", "チュートリアル", "スライムの森", "チグサとバトル１", "？？？", "？？？", "ギャラリー" };

    public static string GetStageName(int stageIndex)
    {
        if (stageIndex < 1 || stageIndex >= StageNames.Length)
            return "ステージ" + stageIndex.ToString();
        return StageNames[stageIndex];
    }

    CharacterClass[] CharactersAll;
    ControllerProduction ControllerProduction;

    int CurrentStageIndex = 1;
    int CurrentSetIndex = 1;

    //[0]がステージ1、セット1
    [SerializeField] Button[] ButtonBattleStages = new Button[Constants.BATTLE_STAGE_NUM];
    [SerializeField] Button[] ButtonBattleStageSets = new Button[Constants.BATTLE_PARTY_SET_NUM];
    [SerializeField] Text TextBattleStagePartyTotal;
    [SerializeField] Text TextBattleFloorRange;
    [SerializeField] Slider SliderBattleFloorFrom;
    [SerializeField] Slider SliderBattleFloorTo;
    [SerializeField] Button ButtonSortieBattleStage;
    [SerializeField] Button ButtonBattleRepeat;
    Button ButtonGalleryStage;
    Text TextBattleStageDetail;

    struct StageEnemyPortrait
    {
        public int StageIndex;
        public uint CharacterId;
        public Image Image;
    }

    readonly List<StageEnemyPortrait> StageEnemyPortraits = new List<StageEnemyPortrait>();
    readonly List<uint> StageEnemyIds = new List<uint>();
    readonly Dictionary<uint, Sprite> StageEnemySprites = new Dictionary<uint, Sprite>();
    readonly Dictionary<uint, Sprite> StageEnemySilhouettes = new Dictionary<uint, Sprite>();
    bool StageEnemyPortraitsReady;


    Color StageButtonLabelColor = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);
    bool StageButtonLabelColorStored;
    Color RepeatLabelColor = Color.black;
    bool RepeatLabelColorStored;
    bool FloorFromSliderMeasured;
    float FloorFromSliderFullWidth;
    float FloorFromSliderLeft;

    Image[,] CellImages = new Image[Constants.BATTLE_FORMATION_SIZE + 1, Constants.BATTLE_FORMATION_SIZE + 1];

    Color ColorButtonNormal = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    Color ColorButtonSelected = new Color(1.0f, 0.85f, 0.4f, 1.0f);
    Color ColorStageLocked = new Color(0.55f, 0.55f, 0.55f, 1.0f);
    Color ColorCellEmpty = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    Color ColorSortie = new Color(0.12f, 0.72f, 0.22f, 1f);
    Color ColorWithdraw = new Color(0.86f, 0.16f, 0.12f, 1f);
    static readonly Color[] SetColors =
    {
        new Color(0.86f, 0.16f, 0.12f, 1f),
        new Color(0.12f, 0.72f, 0.22f, 1f),
        new Color(0.16f, 0.38f, 0.92f, 1f),
        new Color(0.62f, 0.28f, 0.92f, 1f),
        new Color(0.95f, 0.78f, 0.16f, 1f),
    };
    Button _partyButton;
    bool _partyBlink;
    CanvasGroup _partyBlinkGroup;

    public void Initialize(ref CharacterClass[] argCharactersAll, ControllerProduction argControllerProduction)
    {
        CharactersAll = argCharactersAll;
        ControllerProduction = argControllerProduction;

        for (int i = 0; i < ButtonBattleStages.Length; i++)
        {
            if (ButtonBattleStages[i] == null)
            {
                Debug.LogWarning("ButtonBattleStages の要素 " + i + " が割り当てられていません");
                continue;
            }
            int stageIndex = i + 1;
            ButtonBattleStages[i].onClick.AddListener(() => PushButtonSelectBattleStage(stageIndex));
        }

        for (int i = 0; i < ButtonBattleStageSets.Length; i++)
        {
            if (ButtonBattleStageSets[i] == null)
            {
                Debug.LogWarning("ButtonBattleStageSets の要素 " + i + " が割り当てられていません");
                continue;
            }
            int setIndex = i + 1;
            ButtonBattleStageSets[i].onClick.AddListener(() => PushButtonSelectBattleStageSet(setIndex));
        }

        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                string cellName = CELL_IMAGE_PREFIX + x.ToString() + y.ToString();
                GameObject cellObject = GameObject.Find(cellName);
                if (cellObject == null)
                {
                    Debug.LogWarning(cellName + " が見つかりません");
                    continue;
                }
                CellImages[x, y] = cellObject.GetComponent<Image>();
                CellImages[x, y].raycastTarget = false;
                CellImages[x, y].type = Image.Type.Simple;
                CellImages[x, y].preserveAspect = true;
            }
        }

        if (ButtonSortieBattleStage != null)
            ButtonSortieBattleStage.onClick.AddListener(PushButtonSortieOrWithdraw);

        if (ButtonBattleRepeat == null)
        {
            GameObject repeatObject = GameObject.Find("ButtonBattleRepeat");
            if (repeatObject != null)
                ButtonBattleRepeat = repeatObject.GetComponent<Button>();
        }
        if (ButtonBattleRepeat != null)
            ButtonBattleRepeat.onClick.AddListener(PushButtonBattleRepeat);
        else
            Debug.LogWarning("ButtonBattleRepeat が見つかりません");

        EnsureGalleryButton();

        SetupFloorSlider(SliderBattleFloorFrom, true);
        SetupFloorSlider(SliderBattleFloorTo, false);
        ApplyStageChrome();
    }

    const float FrameThin = 3f;

    // 下地は黒。白い枠はステージ一覧だけ。
    void ApplyStageChrome()
    {
        GameObject panelObject = GameObject.Find("PanelBattleStage");
        if (panelObject == null)
            return;
        Transform panel = panelObject.transform;

        Image panelImage = panelObject.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = Color.black;
            panelImage.raycastTarget = true;
        }

        Transform scroll = panel.Find("StageScroll");
        if (scroll != null)
        {
            Image scrollImage = scroll.GetComponent<Image>();
            if (scrollImage != null)
                scrollImage.color = Color.black;
            Transform viewport = scroll.Find("Viewport");
            if (viewport != null)
            {
                Image viewImage = viewport.GetComponent<Image>();
                if (viewImage != null)
                    viewImage.color = Color.black;
            }
            // 一覧の大きさはそのまま。下のカードを縮めた分だけ下へずらす。
            RectTransform scrollRect = scroll as RectTransform;
            scrollRect.anchoredPosition = new Vector2(scrollRect.anchoredPosition.x, -253f);
            EnsureWhiteFrame(scroll);
        }

        LayoutStageFormation(panel);
        ThickenFloorSliders(panel);
        ArrangeFloorHeader(panel);
        PlaceFooterControls(panel, 88f);
        PaintPartyButton(panel);

        PaintLooseLabel(TextBattleStagePartyTotal);
        PaintLooseLabel(TextBattleFloorRange);
        PaintLooseLabel(FindLabel(panel, "TextSliderBattleFloorFrom"));
        PaintLooseLabel(FindLabel(panel, "TextSliderBattleFloorTo"));
        TextBattleStageDetail = FindLabel(panel, "TextBattleStageDetail");
        PaintLooseLabel(TextBattleStageDetail);
    }

    // バー本体を太くし、階数表示・開始・終了のあいだを同じだけ空ける。
    void ThickenFloorSliders(Transform panel)
    {
        const float height = 90f;
        PlaceFloorSlider(SliderBattleFloorFrom, 174f, height);
        PlaceFloorSlider(SliderBattleFloorTo, 62f, height);
        PlaceFloorLabel(panel, "TextSliderBattleFloorFrom", 174f, height, "Start");
        PlaceFloorLabel(panel, "TextSliderBattleFloorTo", 62f, height, "End");
    }

    void ArrangeFloorHeader(Transform panel)
    {
        const float rowY = 312f;
        const float repeatHeight = 128f;
        const float gap = 16f;
        float textRight = 250f;
        if (ButtonBattleRepeat != null)
        {
            RectTransform rect = ButtonBattleRepeat.GetComponent<RectTransform>();
            float width = rect.sizeDelta.x - 30f;
            float right = rect.anchoredPosition.x + rect.sizeDelta.x * (1f - rect.pivot.x);
            float x = right - width * (1f - rect.pivot.x);
            rect.sizeDelta = new Vector2(width, repeatHeight);
            rect.anchoredPosition = new Vector2(x, rowY);
            textRight = rect.anchoredPosition.x - rect.sizeDelta.x * rect.pivot.x - gap;
        }
        if (TextBattleFloorRange != null)
        {
            RectTransform rect = TextBattleFloorRange.rectTransform;
            float half = Mathf.Min(Mathf.Abs(textRight), 516f);
            rect.anchoredPosition = new Vector2(0f, rowY);
            rect.sizeDelta = new Vector2(half * 2f, 110f);
            TextBattleFloorRange.alignment = TextAnchor.MiddleCenter;
            TextBattleFloorRange.fontSize = 48;
            TextBattleFloorRange.resizeTextForBestFit = true;
            TextBattleFloorRange.resizeTextMinSize = 18;
            TextBattleFloorRange.resizeTextMaxSize = 48;
            TextBattleFloorRange.horizontalOverflow = HorizontalWrapMode.Wrap;
            TextBattleFloorRange.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    static void PlaceFloorSlider(Slider slider, float y, float height)
    {
        if (slider == null)
            return;
        RectTransform rect = slider.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        StretchSliderTrack(slider.transform.Find("Background"));
        StretchSliderTrack(slider.transform.Find("Fill Area"));
    }

    static void StretchSliderTrack(Transform track)
    {
        if (track == null)
            return;
        RectTransform rect = track.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(rect.anchorMin.x, 0.08f);
        rect.anchorMax = new Vector2(rect.anchorMax.x, 0.92f);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0f);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 0f);
    }

    static void PlaceFloorLabel(Transform panel, string name, float y, float height, string word)
    {
        Transform label = panel.Find(name);
        if (label == null)
            return;
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        Text text = label.GetComponent<Text>();
        if (text == null)
            return;
        text.text = word;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 40;
        text.resizeTextForBestFit = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // 人数とステータスは出さない。キャラは3人ずつ、組のあいだを空けて横に並べる。
    void LayoutStageFormation(Transform panel)
    {
        if (TextBattleStagePartyTotal != null)
            TextBattleStagePartyTotal.gameObject.SetActive(false);

        Transform formation = panel.Find("StageFormation");
        if (formation == null)
            return;
        GridLayoutGroup grid = formation.GetComponent<GridLayoutGroup>();
        if (grid != null)
            grid.enabled = false;

        const float cell = 85f;
        const float innerGap = 8f;
        const float groupGap = 40f;
        int columns = Constants.BATTLE_FORMATION_SIZE;
        float width = 0f;
        for (int y = columns; y >= 1; y--)
        {
            for (int x = 1; x <= columns; x++)
            {
                Image image = CellImages[x, y];
                if (image == null)
                    continue;
                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(cell, cell);
                rect.anchoredPosition = new Vector2(width, 0f);
                width += cell;
                bool last = y == 1 && x == columns;
                if (last)
                    continue;
                width += x == columns ? groupGap : innerGap;
            }
        }

        RectTransform formationRect = formation as RectTransform;
        formationRect.anchorMin = new Vector2(0.5f, 1f);
        formationRect.anchorMax = new Vector2(0.5f, 1f);
        formationRect.pivot = new Vector2(0.5f, 0.5f);
        formationRect.sizeDelta = new Vector2(width, cell);
        formationRect.anchoredPosition = new Vector2(0f, -210f);
    }

    static void PlaceFooterControls(Transform panel, float yFromBottom)
    {
        PlaceAnchoredY(panel.Find("ButtonSortieBattleStage"), yFromBottom);
        PlaceAnchoredY(panel.Find("ButtonSelectSceneBattleParty"), yFromBottom);
        PlaceAnchoredY(panel.Find("TextBattleStageDetail"), yFromBottom);
    }

    static void PlaceAnchoredY(Transform target, float y)
    {
        if (target == null)
            return;
        RectTransform rect = target as RectTransform;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }

    static Text FindLabel(Transform panel, string name)
    {
        Transform found = panel.Find(name);
        return found != null ? found.GetComponent<Text>() : null;
    }

    static void PaintLooseLabel(Text text)
    {
        if (text != null && text.color != Color.white)
            text.color = Color.white;
    }

    void PaintPartyButton(Transform panel)
    {
        if (_partyButton == null)
        {
            Transform found = panel != null ? panel.Find("ButtonSelectSceneBattleParty") : null;
            if (found == null)
            {
                GameObject bar = GameObject.Find("PanelSelectScene");
                if (bar != null)
                    found = bar.transform.Find("ButtonSelectSceneBattleParty");
            }
            if (found != null)
                _partyButton = found.GetComponent<Button>();
        }
        PaintUsualButton(_partyButton, Color.white, true);
    }

    // 選んでいるセットに誰もいないあいだ、編成ボタンを点滅させて誘導する。
    void SetPartyBlink(bool on)
    {
        if (_partyButton == null)
        {
            GameObject stagePanel = GameObject.Find("PanelBattleStage");
            PaintPartyButton(stagePanel != null ? stagePanel.transform : null);
        }
        _partyBlink = on && _partyButton != null;
        if (_partyButton == null)
            return;
        _partyBlinkGroup = _partyButton.GetComponent<CanvasGroup>();
        if (_partyBlinkGroup == null)
            _partyBlinkGroup = _partyButton.gameObject.AddComponent<CanvasGroup>();
        if (!_partyBlink)
            _partyBlinkGroup.alpha = 1f;
    }

    void Update()
    {
        if (!_partyBlink || _partyBlinkGroup == null)
            return;
        float wave = Mathf.Sin(Time.unscaledTime * Mathf.PI * 1.5f) * 0.5f + 0.5f;
        _partyBlinkGroup.alpha = Mathf.Lerp(0.15f, 1f, wave);
    }

    void LateUpdate()
    {
        if (ButtonSortieBattleStage == null)
            return;
        bool withdrawMode = false;
        if (ControllerProduction != null)
        {
            int activeSet = ControllerProduction.GetActiveBattlePartySet();
            int activeStage = ControllerProduction.GetActiveBattleStage();
            withdrawMode = activeSet != 0 && (activeStage == 0 || activeStage == CurrentStageIndex);
        }
        PaintSortieFrame(withdrawMode ? ColorWithdraw : ColorSortie);
    }

    void PaintSortieFrame(Color color)
    {
        Transform frame = ButtonSortieBattleStage.transform.Find("NeonFrame");
        if (frame == null)
            return;
        color.a = 1f;
        Image[] images = frame.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
                images[i].color = color;
        }
    }

    void PaintUsualButton(Button button, Color color, bool enabled)
    {
        if (button == null || ControllerProduction == null)
            return;
        ControllerProduction.StyleRepresentativeButton(button, color, enabled);
        if (enabled)
            return;
        button.interactable = false;
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.color = new Color(0.45f, 0.45f, 0.45f, 1f);
    }

    // 選んでいるセットは色を置き換えず、白いスクリーンを上に乗せて明るくする。
    static void SetSelectScreen(Button button, bool on)
    {
        if (button == null)
            return;
        Transform existing = button.transform.Find("SelectScreen");
        if (!on)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        Image image;
        if (existing == null)
        {
            GameObject obj = new GameObject("SelectScreen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(button.transform, false);
            image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            image.sprite = FloorSliderSquare();
            image.type = Image.Type.Simple;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            existing = obj.transform;
        }
        else
            image = existing.GetComponent<Image>();

        image.color = new Color(1f, 1f, 1f, 0.5f);
        existing.SetSiblingIndex(0);
        existing.gameObject.SetActive(true);
    }

    static void EnsureWhiteFrame(Transform target)
    {
        if (target == null)
            return;
        Transform frame = target.Find("WhiteFrame");
        if (frame == null)
        {
            GameObject root = new GameObject("WhiteFrame", typeof(RectTransform));
            root.transform.SetParent(target, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            LayoutElement element = root.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            AddWhiteEdge(root.transform, "Top", true, true);
            AddWhiteEdge(root.transform, "Bottom", true, false);
            AddWhiteEdge(root.transform, "Left", false, false);
            AddWhiteEdge(root.transform, "Right", false, true);
            frame = root.transform;
        }
        frame.SetAsLastSibling();
    }

    static void AddWhiteEdge(Transform parent, string name, bool horizontal, bool end)
    {
        GameObject edgeObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        edgeObject.transform.SetParent(parent, false);
        Image image = edgeObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, end ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, end ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, end ? 1f : 0f);
            rect.sizeDelta = new Vector2(0f, FrameThin);
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(end ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(end ? 1f : 0f, 1f);
            rect.pivot = new Vector2(end ? 1f : 0f, 0.5f);
            rect.sizeDelta = new Vector2(FrameThin, 0f);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    void SetupFloorSlider(Slider slider, bool fromSlider)
    {
        if (slider == null)
        {
            Debug.LogWarning(fromSlider ? "SliderBattleFloorFrom が割り当てられていません" : "SliderBattleFloorTo が割り当てられていません");
            return;
        }

        slider.wholeNumbers = true;
        slider.minValue = 1;
        slider.maxValue = Constants.BATTLE_STAGE_FLOOR_MAX / Constants.BATTLE_STAGE_FLOOR_STEP;
        slider.onValueChanged.AddListener(_ => OnFloorSliderChanged(fromSlider));
        StyleFloorSlider(slider);
        if (fromSlider)
            RememberFloorFromSliderSize();
    }

    static Sprite _floorSliderSquare;

    static Sprite FloorSliderSquare()
    {
        if (_floorSliderSquare != null)
            return _floorSliderSquare;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _floorSliderSquare = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        _floorSliderSquare.hideFlags = HideFlags.HideAndDontSave;
        return _floorSliderSquare;
    }

    static void StyleFloorSlider(Slider slider)
    {
        Sprite square = FloorSliderSquare();
        PaintSliderPart(slider.transform.Find("Background"), square, new Color(0.16f, 0.16f, 0.18f, 1f));
        Transform fill = slider.fillRect;
        if (fill == null)
            fill = slider.transform.Find("Fill Area/Fill");
        PaintSliderPart(fill, square, Color.white);
        Transform handle = slider.handleRect;
        if (handle == null)
            handle = slider.transform.Find("Handle Slide Area/Handle");
        PaintSliderPart(handle, square, Color.white);
        RectTransform rect = handle as RectTransform;
        if (rect == null)
            return;
        float xMin = rect.anchorMin.x;
        float xMax = rect.anchorMax.x;
        rect.anchorMin = new Vector2(xMin, 0.5f);
        rect.anchorMax = new Vector2(xMax, 0.5f);
        rect.sizeDelta = new Vector2(40f, 40f);
    }

    static void PaintSliderPart(Transform part, Sprite sprite, Color color)
    {
        if (part == null)
            return;
        Image image = part.GetComponent<Image>();
        if (image == null)
            return;
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = color;
        image.preserveAspect = false;
    }

    void RememberFloorFromSliderSize()
    {
        if (FloorFromSliderMeasured || SliderBattleFloorFrom == null)
            return;
        RectTransform rect = SliderBattleFloorFrom.GetComponent<RectTransform>();
        float width = rect.rect.width;
        if (width <= 1f)
            width = rect.sizeDelta.x;
        FloorFromSliderFullWidth = width;
        FloorFromSliderLeft = rect.anchoredPosition.x - width * rect.pivot.x;
        FloorFromSliderMeasured = true;
    }

    void OnFloorSliderChanged(bool fromMoved)
    {
        if (SliderBattleFloorFrom != null && SliderBattleFloorTo != null)
        {
            int fromBand = Mathf.RoundToInt(SliderBattleFloorFrom.value);
            int toBand = Mathf.RoundToInt(SliderBattleFloorTo.value);
            if (fromBand > toBand)
            {
                if (fromMoved)
                    SliderBattleFloorTo.SetValueWithoutNotify(fromBand);
                else
                    SliderBattleFloorFrom.SetValueWithoutNotify(toBand);
            }
        }

        Refresh();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //ボタン

    public void PushButtonSelectBattleStage(int argStageIndex)
    {
        if (!CanSelectStage(argStageIndex))
            return;
        CurrentStageIndex = argStageIndex;
        Refresh();
    }

    public void PushButtonSelectBattleStageSet(int argSetIndex)
    {
        if (argSetIndex < 1 || argSetIndex > Constants.BATTLE_PARTY_SET_NUM)
            return;
        CurrentSetIndex = argSetIndex;
        Refresh();
    }

    // 見ているステージが出撃中なら撤退する。別のステージなら、戦闘を止めてから選んでいるセットを出撃させる。
    public void PushButtonSortieOrWithdraw()
    {
        int activeSet = ControllerProduction.GetActiveBattlePartySet();
        int activeStage = ControllerProduction.GetActiveBattleStage();
        if (activeSet != 0 && (activeStage == 0 || activeStage == CurrentStageIndex))
        {
            if (!ControllerProduction.RequestBattleWithdraw())
            {
                ControllerProduction.LeaveBattle();
                ControllerProduction.SaveGame();
            }
        }
        else
        {
            bool resume = ControllerProduction.IsBattleRunning();
            if (resume)
                ControllerProduction.EndRunningBattle();
            GetSelectedFloorRange(out int floorFrom, out int floorTo);
            if (ControllerProduction.EnterBattleStage(CurrentStageIndex, CurrentSetIndex, floorFrom, floorTo))
            {
                if (!ControllerProduction.BeginAutoBattle())
                    ControllerProduction.LeaveBattle();
            }
            else if (resume)
                ControllerProduction.BeginAutoBattle();
        }

        Refresh();
    }

    public void PushButtonBattleRepeat()
    {
        if (!IsRepeatUnlocked() || ControllerProduction == null)
            return;
        ControllerProduction.SetBattleRepeat(!ControllerProduction.GetBattleRepeat());
        ControllerProduction.SaveGame();
        Refresh();
    }

    public bool IsRepeatOn()
    {
        return ControllerProduction != null && ControllerProduction.GetBattleRepeat() && IsRepeatUnlocked();
    }

    bool IsRepeatUnlocked()
    {
        return ControllerProduction != null && ControllerProduction.IsMainFeatureOpen(MainFeature.FloorRepeat);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //表示

    public int GetSelectedSetIndex()
    {
        return CurrentSetIndex;
    }

    public void Open(int keepSetIndex = 0)
    {
        int activeStage = ControllerProduction.GetActiveBattleStage();
        if (activeStage != 0)
            CurrentStageIndex = activeStage;

        int activeSet = ControllerProduction.GetActiveBattlePartySet();
        if (keepSetIndex >= 1 && keepSetIndex <= Constants.BATTLE_PARTY_SET_NUM)
            CurrentSetIndex = keepSetIndex;
        else if (activeSet != 0)
            CurrentSetIndex = activeSet;

        int activeFloorFrom = ControllerProduction.GetActiveBattleFloorFrom();
        int activeFloorTo = ControllerProduction.GetActiveBattleFloorTo();
        int windowStart = ControllerProduction.GetBattleFloorWindowStart(CurrentStageIndex);
        if (activeStage == CurrentStageIndex && ControllerProduction.IsSortieFloorRange(activeStage, activeFloorFrom, activeFloorTo))
            SetFloorSliders(activeFloorFrom, activeFloorTo);
        else
            SetFloorSliders(windowStart, windowStart + Constants.BATTLE_STAGE_FLOOR_STEP - 1);

        Refresh();
    }

    public void Refresh()
    {
        int activeStage = ControllerProduction.GetActiveBattleStage();
        int activeSet = ControllerProduction.GetActiveBattlePartySet();
        int clearedStage = ControllerProduction.GetClearedBattleStage();

        if (!CanSelectStage(CurrentStageIndex))
        {
            int openedStage = clearedStage + 1;
            if (openedStage > Constants.BATTLE_STAGE_NUM)
                openedStage = Constants.BATTLE_STAGE_NUM;
            CurrentStageIndex = openedStage;
        }

        if (TextBattleStageDetail != null)
            TextBattleStageDetail.text = GetStageName(CurrentStageIndex);

        ApplyFloorFromLimit();

        for (int i = 0; i < ButtonBattleStages.Length; i++)
        {
            if (ButtonBattleStages[i] == null)
                continue;

            int stageIndex = i + 1;
            if (stageIndex > Constants.BATTLE_STAGE_NUM)
                break;

            bool revealed = stageIndex < Constants.BATTLE_STAGE_NUM
                || ControllerProduction.IsBattleStageUnlocked(stageIndex - 1)
                || stageIndex == activeStage;
            ButtonBattleStages[i].gameObject.SetActive(revealed);
            if (!revealed)
                continue;

            bool unlocked = ControllerProduction.IsBattleStageUnlocked(stageIndex);
            ButtonBattleStages[i].interactable = unlocked || stageIndex == activeStage;
            Text label = ButtonBattleStages[i].GetComponentInChildren<Text>();

            bool lockedLook = !unlocked && stageIndex != activeStage;
            if (lockedLook)
                ButtonBattleStages[i].image.color = ColorStageLocked;
            else
                ButtonBattleStages[i].image.color = (stageIndex == CurrentStageIndex) ? ColorButtonSelected : ColorButtonNormal;
            ApplyStageButtonBackground(ButtonBattleStages[i], stageIndex, label);
            if (label == null)
                continue;
            if (lockedLook)
            {
                label.text = "？？？";
                label.color = Color.white;
                MatchStageTitleSize(label);
                label.transform.SetAsLastSibling();
            }
            else
            {
                label.text = StageNames[stageIndex];
                if (stageIndex == activeStage)
                    label.text += "\n出撃中";
                else if (ControllerProduction.IsBattleStageCleared(stageIndex))
                    label.text += "\nクリア";
            }
        }

        if (ControllerProduction.IsBattleRunning() && activeStage != 0)
            ControllerProduction.ShowBattleStageBackground(activeStage);

        RefreshStageEnemyPortraits();
        PaintGalleryButton(activeStage);

        for (int i = 0; i < ButtonBattleStageSets.Length; i++)
        {
            if (ButtonBattleStageSets[i] == null)
                continue;

            int setIndex = i + 1;
            Text label = ButtonBattleStageSets[i].GetComponentInChildren<Text>();
            if (label != null)
                label.text = "セット" + setIndex.ToString() + (setIndex == activeSet ? "\n出撃中" : "");
            Color setColor = SetColors[Mathf.Clamp(i, 0, SetColors.Length - 1)];
            PaintUsualButton(ButtonBattleStageSets[i], setColor, true);
            SetSelectScreen(ButtonBattleStageSets[i], setIndex == CurrentSetIndex);
        }

        GameObject stagePanel = GameObject.Find("PanelBattleStage");
        PaintPartyButton(stagePanel != null ? stagePanel.transform : null);

        int memberNum = 0;
        bool setEmpty = true;
        ulong totalHP = 0;
        ulong totalATK = 0;
        ulong totalDEF = 0;

        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                uint characterId = ControllerProduction.GetBattlePartyCharacter(CurrentSetIndex, x, y);
                if (characterId != 0)
                    setEmpty = false;
                if (CellImages[x, y] != null)
                {
                    if (characterId == 0)
                    {
                        CellImages[x, y].sprite = null;
                        CellImages[x, y].color = ColorCellEmpty;
                        CatalogNewMark.Apply(CellImages[x, y].transform, null);
                        ControllerProduction.RemoveAttributeGlow(CellImages[x, y].transform);
                    }
                    else
                    {
                        CellImages[x, y].sprite = CreateCharacterSprite(characterId);
                        CellImages[x, y].color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                        CharacterClass shown = ImportedCharacters.FindAny(CharactersAll, characterId);
                        CatalogNewMark.Apply(CellImages[x, y].transform, shown);
                        if (shown != null)
                            ControllerProduction.EnsureOwnedAttributeGlow(CellImages[x, y].transform, shown);
                        else
                            ControllerProduction.RemoveAttributeGlow(CellImages[x, y].transform);
                    }
                }

                if (characterId == 0)
                    continue;

                CharacterClass member = ImportedCharacters.FindAny(CharactersAll, characterId);
                if (member == null || member.Stats == null || member.Stats[0] == null)
                    continue;
                memberNum++;
                totalHP += member.Stats[0].HPMax;
                totalATK += member.Stats[0].ATK;
                totalDEF += member.Stats[0].DEF;
            }
        }

        if (TextBattleStagePartyTotal != null)
        {
            TextBattleStagePartyTotal.text = memberNum.ToString() + "/" + (Constants.BATTLE_FORMATION_SIZE * Constants.BATTLE_FORMATION_SIZE).ToString() + "人\n"
                + "HP " + UiNumber.Format(totalHP) + "\n"
                + "ATK " + UiNumber.Format(totalATK) + "\n"
                + "DEF " + UiNumber.Format(totalDEF);
            TextBattleStagePartyTotal.horizontalOverflow = HorizontalWrapMode.Overflow;
            TextBattleStagePartyTotal.verticalOverflow = VerticalWrapMode.Overflow;
        }

        SetPartyBlink(setEmpty);

        bool withdrawMode = activeSet != 0 && (activeStage == 0 || activeStage == CurrentStageIndex);
        if (ButtonSortieBattleStage != null)
        {
            bool canPress = withdrawMode || (ControllerProduction.CanEnterStage(CurrentStageIndex) && memberNum > 0);
            Text label = ButtonSortieBattleStage.GetComponentInChildren<Text>();
            if (label != null)
                label.text = withdrawMode ? "撤退" : "出撃";
            PaintUsualButton(ButtonSortieBattleStage, withdrawMode ? ColorWithdraw : ColorSortie, canPress);
            ButtonSortieBattleStage.interactable = canPress;
        }

        if (ButtonBattleRepeat != null)
        {
            bool repeatOpen = IsRepeatUnlocked();
            ButtonBattleRepeat.interactable = repeatOpen;
            Text repeatLabel = ButtonBattleRepeat.GetComponentInChildren<Text>();
            if (repeatLabel != null && !RepeatLabelColorStored)
            {
                RepeatLabelColor = repeatLabel.color;
                RepeatLabelColorStored = true;
            }

            if (!repeatOpen)
            {
                ButtonBattleRepeat.image.color = ColorStageLocked;
                if (repeatLabel != null)
                {
                    repeatLabel.color = new Color(0.28f, 0.28f, 0.28f, 1f);
                    repeatLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    repeatLabel.verticalOverflow = VerticalWrapMode.Overflow;
                    repeatLabel.text = "繰り返し\nクエストを受け取ると開く";
                }
            }
            else
            {
                ButtonBattleRepeat.image.color = ControllerProduction.GetBattleRepeat() ? ColorButtonSelected : ColorButtonNormal;
                if (repeatLabel != null)
                {
                    repeatLabel.color = RepeatLabelColor;
                    repeatLabel.text = ControllerProduction.GetBattleRepeat() ? "繰り返し\nON" : "繰り返し\nOFF";
                }
            }
        }

        if (TextBattleFloorRange != null)
        {
            GetSelectedFloorRange(out int floorFrom, out int floorTo);
            TextBattleFloorRange.text = floorFrom.ToString() + "階 〜 " + floorTo.ToString() + "階";
            int maxFrom = ControllerProduction.GetMaxBattleFloorFrom(CurrentStageIndex);
            int windowStart = ControllerProduction.GetBattleFloorWindowStart(CurrentStageIndex);
            int lastBandStart = windowStart + Constants.BATTLE_STAGE_FLOOR_MAX - Constants.BATTLE_STAGE_FLOOR_STEP;
            if (maxFrom < lastBandStart)
            {
                int highest = ControllerProduction.GetHighestClearedBattleFloor(CurrentStageIndex);
                if (highest <= 0)
                    TextBattleFloorRange.text += "\n（開始は1階まで）";
                else
                    TextBattleFloorRange.text += "\n（" + highest.ToString() + "階クリア、開始は" + maxFrom.ToString() + "階まで）";
            }

            int activeFloorFrom = ControllerProduction.GetActiveBattleFloorFrom();
            int activeFloorTo = ControllerProduction.GetActiveBattleFloorTo();
            if (activeStage == CurrentStageIndex && activeFloorFrom > 0
                && (activeFloorFrom != floorFrom || activeFloorTo != floorTo))
            {
                TextBattleFloorRange.text += "\n出撃中 " + activeFloorFrom.ToString() + "階 〜 " + activeFloorTo.ToString() + "階";
            }
        }
    }

    void ApplyFloorFromLimit()
    {
        if (SliderBattleFloorFrom == null || ControllerProduction == null)
            return;

        int maxFrom = ControllerProduction.GetMaxBattleFloorFrom(CurrentStageIndex);
        int windowStart = ControllerProduction.GetBattleFloorWindowStart(CurrentStageIndex);
        int maxBand = (maxFrom - windowStart) / Constants.BATTLE_STAGE_FLOOR_STEP + 1;
        if (maxBand < 1)
            maxBand = 1;

        int fromBand = Mathf.RoundToInt(SliderBattleFloorFrom.value);
        if (fromBand > maxBand)
            fromBand = maxBand;
        SliderBattleFloorFrom.maxValue = maxBand;
        SliderBattleFloorFrom.SetValueWithoutNotify(fromBand);
        ResizeFloorFromSlider(maxBand);

        if (SliderBattleFloorTo == null)
            return;
        int toBand = Mathf.RoundToInt(SliderBattleFloorTo.value);
        fromBand = Mathf.RoundToInt(SliderBattleFloorFrom.value);
        if (toBand < fromBand)
            SliderBattleFloorTo.SetValueWithoutNotify(fromBand);
    }

    //選べる開始階が浅いときは、バーの長さもその分だけ短くする。左端は動かさない
    void ResizeFloorFromSlider(int maxBand)
    {
        RememberFloorFromSliderSize();
        if (!FloorFromSliderMeasured || SliderBattleFloorFrom == null)
            return;

        int totalBands = Constants.BATTLE_STAGE_FLOOR_MAX / Constants.BATTLE_STAGE_FLOOR_STEP;
        if (totalBands < 1)
            totalBands = 1;
        if (maxBand < 1)
            maxBand = 1;
        if (maxBand > totalBands)
            maxBand = totalBands;

        RectTransform rect = SliderBattleFloorFrom.GetComponent<RectTransform>();
        float width = FloorFromSliderFullWidth * maxBand / totalBands;
        if (width < 1f)
            width = 1f;
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(FloorFromSliderLeft + width * rect.pivot.x, rect.anchoredPosition.y);
    }

    void SetFloorSliders(int floorFrom, int floorTo)
    {
        int windowStart = ControllerProduction != null ? ControllerProduction.GetBattleFloorWindowStart(CurrentStageIndex) : 1;
        int fromBand = (floorFrom - windowStart) / Constants.BATTLE_STAGE_FLOOR_STEP + 1;
        int toBand = (floorTo - windowStart) / Constants.BATTLE_STAGE_FLOOR_STEP + 1;
        if (SliderBattleFloorFrom != null)
            SliderBattleFloorFrom.SetValueWithoutNotify(fromBand);
        if (SliderBattleFloorTo != null)
            SliderBattleFloorTo.SetValueWithoutNotify(toBand);
    }

    void GetSelectedFloorRange(out int floorFrom, out int floorTo)
    {
        int fromBand = SliderBattleFloorFrom != null ? Mathf.RoundToInt(SliderBattleFloorFrom.value) : 1;
        int toBand = SliderBattleFloorTo != null ? Mathf.RoundToInt(SliderBattleFloorTo.value) : 1;
        if (fromBand < 1)
            fromBand = 1;
        int windowStart = 1;
        if (ControllerProduction != null)
        {
            windowStart = ControllerProduction.GetBattleFloorWindowStart(CurrentStageIndex);
            int maxFrom = ControllerProduction.GetMaxBattleFloorFrom(CurrentStageIndex);
            int maxBand = (maxFrom - windowStart) / Constants.BATTLE_STAGE_FLOOR_STEP + 1;
            if (fromBand > maxBand)
                fromBand = maxBand;
        }
        if (toBand < fromBand)
            toBand = fromBand;

        floorFrom = windowStart + (fromBand - 1) * Constants.BATTLE_STAGE_FLOOR_STEP;
        floorTo = windowStart + toBand * Constants.BATTLE_STAGE_FLOOR_STEP - 1;
    }

    void EnsureGalleryButton()
    {
        Button source = null;
        for (int i = 0; i < ButtonBattleStages.Length; i++)
        {
            if (ButtonBattleStages[i] != null)
                source = ButtonBattleStages[i];
        }
        if (source == null)
        {
            Debug.LogWarning("ギャラリーのボタンを置く元になるステージボタンがありません");
            return;
        }

        Transform parent = source.transform.parent;
        Transform existing = parent != null ? parent.Find("ButtonBattleStageGallery") : null;
        if (existing != null)
            ButtonGalleryStage = existing.GetComponent<Button>();

        if (ButtonGalleryStage == null)
        {
            GameObject clone = Instantiate(source.gameObject, parent);
            clone.name = "ButtonBattleStageGallery";
            RectTransform sourceRect = source.GetComponent<RectTransform>();
            RectTransform rect = clone.GetComponent<RectTransform>();
            float gap = sourceRect.sizeDelta.x;
            if (gap < 1f)
                gap = 160f;
            rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(gap + 12f, 0f);
            ButtonGalleryStage = clone.GetComponent<Button>();
        }

        if (ButtonGalleryStage == null)
            return;
        ButtonGalleryStage.onClick = new Button.ButtonClickedEvent();
        ButtonGalleryStage.onClick.AddListener(() => PushButtonSelectBattleStage(Constants.BATTLE_STAGE_GALLERY));
    }

    void PaintGalleryButton(int activeStage)
    {
        if (ButtonGalleryStage == null || ControllerProduction == null)
            return;

        bool open = ControllerProduction.CanEnterStage(Constants.BATTLE_STAGE_GALLERY);
        bool sortieing = activeStage == Constants.BATTLE_STAGE_GALLERY;
        ButtonGalleryStage.interactable = open || sortieing;

        Text label = ButtonGalleryStage.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        if (!open && !sortieing)
        {
            ButtonGalleryStage.image.color = ColorStageLocked;
            if (label == null)
                return;
            label.text = "？？？";
            label.color = Color.white;
            MatchStageTitleSize(label);
            return;
        }

        ButtonGalleryStage.image.color = (CurrentStageIndex == Constants.BATTLE_STAGE_GALLERY) ? ColorButtonSelected : ColorButtonNormal;
        if (label == null)
            return;

        int start = ControllerProduction.GetBattleFloorWindowStart(Constants.BATTLE_STAGE_GALLERY);
        int end = start + Constants.BATTLE_STAGE_FLOOR_MAX - 1;
        label.text = "ギャラリー\n" + start.ToString() + "〜" + end.ToString() + "階";
        if (sortieing)
            label.text += "\n出撃中";
    }

    bool CanSelectStage(int stageIndex)
    {
        return ControllerProduction.CanEnterStage(stageIndex)
            || stageIndex == ControllerProduction.GetActiveBattleStage();
    }

    void MatchStageTitleSize(Text label)
    {
        if (label == null)
            return;
        Text sample = null;
        if (ButtonBattleStages.Length > 0 && ButtonBattleStages[0] != null)
            sample = ButtonBattleStages[0].GetComponentInChildren<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = sample != null && sample.resizeTextMinSize > 0 ? sample.resizeTextMinSize : 10;
        int max = sample != null && sample.resizeTextMaxSize > 0 ? sample.resizeTextMaxSize : 48;
        label.resizeTextMaxSize = max;
        label.fontSize = max;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
    }

    // 各ステージカードの右下に、出る敵を並べる。大きさはカードの高さの3分の2から、さらに3割小さい。
    void ApplyStageButtonBackground(Button button, int stageIndex, Text label)
    {
        if (button == null)
            return;
        Sprite sprite = BattleStageCatalog.BackgroundSprite(stageIndex);
        Image face = EnsureStageFace(button);
        bool show = sprite != null;
        face.gameObject.SetActive(show);
        face.sprite = sprite;
        face.color = new Color(0.38f, 0.38f, 0.38f, 1f);
        if (label == null)
            return;
        if (!StageButtonLabelColorStored)
        {
            StageButtonLabelColor = label.color;
            StageButtonLabelColorStored = true;
        }
        Outline outline = label.GetComponent<Outline>();
        if (!show)
        {
            label.color = StageButtonLabelColor;
            if (outline != null)
                outline.enabled = false;
            return;
        }
        label.color = Color.white;
        if (outline == null)
            outline = label.gameObject.AddComponent<Outline>();
        outline.enabled = true;
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    static Image EnsureStageFace(Button button)
    {
        Transform existing = button.transform.Find("StageBackground");
        Image image;
        if (existing != null)
            image = existing.GetComponent<Image>();
        else
        {
            GameObject obj = new GameObject("StageBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(button.transform, false);
            image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 8f);
            rect.offsetMax = new Vector2(-8f, -8f);
        }
        image.transform.SetAsFirstSibling();
        return image;
    }

    void RefreshStageEnemyPortraits()
    {
        EnsureStageEnemyPortraits();
        for (int i = 0; i < StageEnemyPortraits.Count; i++)
        {
            StageEnemyPortrait portrait = StageEnemyPortraits[i];
            if (portrait.Image == null)
                continue;

            CharacterClass character = ImportedCharacters.FindAny(CharactersAll, portrait.CharacterId);
            bool opened = ControllerProduction.IsBattleStageUnlocked(portrait.StageIndex);
            bool companion = opened && character != null && character.OwnedNumMax != 0;
            portrait.Image.sprite = companion ? StageEnemySprite(character) : StageEnemySilhouette(character);
            portrait.Image.color = companion ? Color.white : Color.black;
            portrait.Image.enabled = portrait.Image.sprite != null;
        }
    }

    void EnsureStageEnemyPortraits()
    {
        if (StageEnemyPortraitsReady)
            return;
        StageEnemyPortraitsReady = true;

        const float pad = 8f;
        const float gap = 4f;
        for (int i = 0; i < ButtonBattleStages.Length; i++)
        {
            Button button = ButtonBattleStages[i];
            if (button == null)
                continue;

            int stageIndex = i + 1;
            if (stageIndex > Constants.BATTLE_STAGE_NUM)
                break;

            BattleStageCatalog.CollectEnemyIds(stageIndex, StageEnemyIds);
            float size = StageCardHeight(button) * 2f / 3f * 0.7f;
            float rowWidth = StageEnemyIds.Count * size + Mathf.Max(0, StageEnemyIds.Count - 1) * gap;
            float x = -(pad + rowWidth);
            for (int n = 0; n < StageEnemyIds.Count; n++)
            {
                uint id = StageEnemyIds[n];
                GameObject obj = new GameObject("StageEnemy" + id.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                obj.transform.SetParent(button.transform, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = Vector2.zero;
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = new Vector2(x, pad);
                Image image = obj.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                StageEnemyPortraits.Add(new StageEnemyPortrait { StageIndex = stageIndex, CharacterId = id, Image = image });
                x += size + gap;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
                continue;
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.offsetMin = new Vector2(0f, label.rectTransform.offsetMin.y);
            label.rectTransform.offsetMax = new Vector2(0f, label.rectTransform.offsetMax.y);
        }
    }

    static float StageCardHeight(Button button)
    {
        LayoutElement element = button.GetComponent<LayoutElement>();
        if (element != null && element.preferredHeight > 1f)
            return element.preferredHeight;
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null && rect.rect.height > 1f)
            return rect.rect.height;
        return 140f;
    }

    Sprite StageEnemySprite(CharacterClass character)
    {
        if (character == null)
            return null;

        Sprite cached;
        if (StageEnemySprites.TryGetValue(character.ID, out cached) && cached != null)
            return cached;

        Texture2D tex = character.ImageTexture2D;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");

        tex.filterMode = FilterMode.Point;
        int size = CharacterImageSize(character, tex);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        StageEnemySprites[character.ID] = sprite;
        return sprite;
    }

    Sprite StageEnemySilhouette(CharacterClass character)
    {
        if (character == null)
            return null;

        Sprite cached;
        if (StageEnemySilhouettes.TryGetValue(character.ID, out cached) && cached != null)
            return cached;

        Texture2D tex = character.ImageTexture2D;
        if (tex == null)
            return null;

        int size = CharacterImageSize(character, tex);
        Color[] pixels = tex.GetPixels(0, 0, size, size);
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(1f, 1f, 1f, pixels[i].a);
        Texture2D white = new Texture2D(size, size, TextureFormat.RGBA32, false);
        white.filterMode = FilterMode.Point;
        white.SetPixels(pixels);
        white.Apply();
        Sprite sprite = Sprite.Create(white, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        StageEnemySilhouettes[character.ID] = sprite;
        return sprite;
    }

    static int CharacterImageSize(CharacterClass character, Texture2D tex)
    {
        int size = character.Size > 0 ? character.Size : tex.width;
        if (size > tex.width)
            size = tex.width;
        if (size > tex.height)
            size = tex.height;
        if (size < 1)
            size = 1;
        return size;
    }

    void OnDestroy()
    {
        foreach (KeyValuePair<uint, Sprite> pair in StageEnemySprites)
        {
            if (pair.Value != null)
                Destroy(pair.Value);
        }
        StageEnemySprites.Clear();

        foreach (KeyValuePair<uint, Sprite> pair in StageEnemySilhouettes)
        {
            if (pair.Value == null)
                continue;
            if (pair.Value.texture != null)
                Destroy(pair.Value.texture);
            Destroy(pair.Value);
        }
        StageEnemySilhouettes.Clear();
    }

    Sprite CreateCharacterSprite(uint argCharacterId)
    {
        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, argCharacterId);
        Texture2D tex = character != null ? character.ImageTexture2D : null;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");

        return Sprite.Create(tex, new UnityEngine.Rect(0, 0, character.Size, character.Size), new Vector2(0.5f, 0.5f));
    }
}
