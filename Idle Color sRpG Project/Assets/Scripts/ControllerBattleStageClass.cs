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
    static readonly string[] StageNames = { "", "チュートリアル", "スライムの森", "チグサとバトル１", "モアとバトル１", "ソウガとバトル１", "チグサとバトル２", "ギャラリー" };

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
    Button ButtonComingSoon;
    float TrailContentWidth = -1f;
    Text TextBattleStageDetail;
    Text _battleEndedNote;
    bool _battleEndedNoteOn;
    Text _partyErrorNote;
    string _partyErrorText = "";
    bool _detailLayoutStored;
    Vector2 _detailPos;
    Vector2 _detailSize;

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


    Color StageButtonLabelColor = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);
    bool StageButtonLabelColorStored;
    RawImage _repeatButtonMark;
    bool FloorFromSliderMeasured;
    float FloorFromSliderFullWidth;
    float FloorFromSliderLeft;

    Image[,] CellImages = new Image[Constants.BATTLE_FORMATION_SIZE + 1, Constants.BATTLE_FORMATION_SIZE + 1];

    Color ColorButtonNormal = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    Color ColorButtonSelected = new Color(1.0f, 0.85f, 0.4f, 1.0f);
    Color ColorStageLocked = new Color(0.22f, 0.22f, 0.22f, 1.0f);
    Color ColorCellEmpty = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    Color ColorSortie = new Color(0.12f, 0.72f, 0.22f, 1f);
    Color ColorRepeatOn = new Color(0.35f, 0.95f, 0.38f, 1f);
    Color ColorRepeatOff = new Color(0.04f, 0.28f, 0.08f, 1f);
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
    readonly Button[] FloorWindowPrev = new Button[Constants.BATTLE_STAGE_NUM];
    readonly Button[] FloorWindowNext = new Button[Constants.BATTLE_STAGE_NUM];
    readonly Image[] ChallengeStarPlates = new Image[Constants.BATTLE_STAGE_NUM];
    GameObject _starPopup;
    RectTransform _starPopupCard;
    Text _starPopupTitle;
    RectTransform _starPopupRows;
    readonly Image[,] ChallengeStarBlurs = new Image[Constants.BATTLE_STAGE_NUM, ChallengeRules.KindCount];
    readonly Image[,] ChallengeStarGlows = new Image[Constants.BATTLE_STAGE_NUM, ChallengeRules.KindCount];
    readonly Image[,] ChallengeStars = new Image[Constants.BATTLE_STAGE_NUM, ChallengeRules.KindCount];
    readonly List<ChallengeKind> ChallengeSlots = new List<ChallengeKind>();

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

        EnsureTrailingStageButtons();
        EnsureFloorWindowControls();

        SetupFloorSlider(SliderBattleFloorFrom, true);
        SetupFloorSlider(SliderBattleFloorTo, false);
        ApplyStageChrome();
    }

    const float FrameThin = 3f;
    const float StageCardHeightPreferred = 188f;

    void RaiseStageCards()
    {
        for (int i = 0; i < ButtonBattleStages.Length; i++)
            SetStageCardHeight(ButtonBattleStages[i]);
        SetStageCardHeight(ButtonComingSoon);
        SetStageCardHeight(ButtonGalleryStage);
    }

    static void SetStageCardHeight(Button button)
    {
        if (button == null)
            return;
        LayoutElement element = button.GetComponent<LayoutElement>();
        if (element == null)
            element = button.gameObject.AddComponent<LayoutElement>();
        element.minHeight = StageCardHeightPreferred;
        element.preferredHeight = StageCardHeightPreferred;
    }

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
                StretchStageViewport(viewport as RectTransform);
            }
            // 一覧の大きさはそのまま。下のカードを縮めた分だけ下へずらす。
            RectTransform scrollRect = scroll as RectTransform;
            scrollRect.anchoredPosition = new Vector2(scrollRect.anchoredPosition.x, -253f);
            EnsureWhiteFrame(scroll);
        }

        RaiseStageCards();

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
        float wave = Mathf.Sin(Time.unscaledTime * Mathf.PI * 1.5f) * 0.5f + 0.5f;
        float alpha = Mathf.Lerp(0.15f, 1f, wave);
        if (_partyBlink && _partyBlinkGroup != null)
            _partyBlinkGroup.alpha = alpha;
        BlinkZeroOwnedPortraits(alpha);
    }

    void BlinkZeroOwnedPortraits(float alpha)
    {
        if (ControllerProduction == null)
            return;
        for (int x = 1; x <= Constants.BATTLE_FORMATION_SIZE; x++)
        {
            for (int y = 1; y <= Constants.BATTLE_FORMATION_SIZE; y++)
            {
                Image image = CellImages[x, y];
                if (image == null)
                    continue;
                uint id = ControllerProduction.GetBattlePartyCharacter(CurrentSetIndex, x, y);
                bool blink = OwnsNone(id);
                CanvasGroup group = image.GetComponent<CanvasGroup>();
                if (!blink)
                {
                    if (group != null && group.alpha != 1f)
                        group.alpha = 1f;
                    continue;
                }
                if (group == null)
                    group = image.gameObject.AddComponent<CanvasGroup>();
                group.alpha = alpha;
            }
        }
    }

    bool OwnsNone(uint id)
    {
        if (id == 0)
            return false;
        CharacterClass character = ImportedCharacters.FindAny(CharactersAll, id);
        if (character != null && character.OwnedNumCur < 1)
            return true;
        return ControllerProduction != null && ControllerProduction.IsHospitalNurse(id);
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

    // 文字の左に、直した繰り返し矢印を置く。閉じているときは文字だけにする。
    void LayoutRepeatButtonLabel(Text label, bool showMark)
    {
        if (ButtonBattleRepeat == null || label == null)
            return;
        RectTransform textRect = label.rectTransform;
        if (!showMark)
        {
            if (_repeatButtonMark != null)
                _repeatButtonMark.gameObject.SetActive(false);
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);
            label.alignment = TextAnchor.MiddleCenter;
            return;
        }

        if (_repeatButtonMark == null)
        {
            Texture2D texture = Resources.Load<Texture2D>("RepeatMark");
            if (texture == null)
                return;
            GameObject obj = new GameObject("ImageRepeatButtonMark");
            obj.transform.SetParent(ButtonBattleRepeat.transform, false);
            RawImage image = obj.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.color = Color.white;
            const float tex = 1024f;
            image.uvRect = new Rect(50f / tex, (tex - 1000f) / tex, 924f / tex, 950f / tex);
            _repeatButtonMark = image;
        }

        _repeatButtonMark.gameObject.SetActive(true);
        const float size = 48f;
        float markWidth = size * (924f / 950f);
        const float pad = 14f;
        RectTransform mark = _repeatButtonMark.rectTransform;
        mark.anchorMin = new Vector2(0f, 0.5f);
        mark.anchorMax = new Vector2(0f, 0.5f);
        mark.pivot = new Vector2(0f, 0.5f);
        mark.sizeDelta = new Vector2(markWidth, size);
        mark.anchoredPosition = new Vector2(pad, 0f);
        mark.SetAsLastSibling();

        textRect.offsetMin = new Vector2(pad + markWidth + 8f, 4f);
        textRect.offsetMax = new Vector2(-10f, -4f);
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
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
        GameAudio.BindSlider(slider);
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

        // ドラッグ中は一覧全体を作り直さず、選択範囲の文字だけ即時更新する。
        PaintFloorRangeText();
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

    public void ShowBattleEndedNote()
    {
        _battleEndedNoteOn = true;
        ApplyDetailFootnote();
    }

    public void ClearBattleEndedNote()
    {
        if (!_battleEndedNoteOn && (_battleEndedNote == null || !_battleEndedNote.gameObject.activeSelf))
            return;
        _battleEndedNoteOn = false;
        ApplyDetailFootnote();
    }

    void ApplyPartyError(string message)
    {
        _partyErrorText = message ?? "";
        ApplyDetailFootnote();
    }

    // ステージ詳細の下。セットのエラーとバトル終了の注記は同じ場所なので、エラーを優先する
    void ApplyDetailFootnote()
    {
        if (TextBattleStageDetail == null)
            return;
        RectTransform detail = TextBattleStageDetail.rectTransform;
        if (!_detailLayoutStored)
        {
            _detailPos = detail.anchoredPosition;
            _detailSize = detail.sizeDelta;
            _detailLayoutStored = true;
        }
        bool errorOn = !string.IsNullOrEmpty(_partyErrorText);
        bool endedOn = _battleEndedNoteOn && !errorOn;
        const float noteHeight = 64f;
        if (errorOn || endedOn)
        {
            detail.sizeDelta = new Vector2(_detailSize.x, Mathf.Max(48f, _detailSize.y - noteHeight));
            detail.anchoredPosition = new Vector2(_detailPos.x, _detailPos.y + noteHeight * 0.5f);
        }
        else
        {
            detail.sizeDelta = _detailSize;
            detail.anchoredPosition = _detailPos;
        }
        EnsureBattleEndedNote();
        EnsurePartyErrorNote();
        if (_battleEndedNote != null)
            _battleEndedNote.gameObject.SetActive(endedOn);
        if (_partyErrorNote != null)
        {
            _partyErrorNote.text = _partyErrorText;
            _partyErrorNote.gameObject.SetActive(errorOn);
            if (errorOn)
                _partyErrorNote.transform.SetAsLastSibling();
        }
    }

    void EnsureBattleEndedNote()
    {
        if (TextBattleStageDetail == null)
            return;
        if (_battleEndedNote == null)
        {
            GameObject obj = new GameObject("TextBattleEndedNote", typeof(RectTransform));
            obj.transform.SetParent(TextBattleStageDetail.transform.parent, false);
            Text text = obj.AddComponent<Text>();
            text.font = TextBattleStageDetail.font;
            text.fontSize = 22;
            text.alignment = TextAnchor.UpperCenter;
            text.color = new Color(1f, 0.164f, 0.164f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = "（バトルが終了したため遷移しました、設定よりOFFにできます）";
            _battleEndedNote = text;
            obj.SetActive(false);
        }
        PlaceDetailFootnote(_battleEndedNote.rectTransform);
        _battleEndedNote.transform.SetAsLastSibling();
    }

    void EnsurePartyErrorNote()
    {
        if (TextBattleStageDetail == null)
            return;
        if (_partyErrorNote == null)
        {
            GameObject obj = new GameObject("TextBattlePartyError", typeof(RectTransform));
            obj.transform.SetParent(TextBattleStageDetail.transform.parent, false);
            Text text = obj.AddComponent<Text>();
            text.font = TextBattleStageDetail.font;
            text.fontSize = 22;
            text.alignment = TextAnchor.UpperCenter;
            text.color = new Color(1f, 0.164f, 0.164f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            _partyErrorNote = text;
            obj.SetActive(false);
        }
        PlaceDetailFootnote(_partyErrorNote.rectTransform);
    }

    void PlaceDetailFootnote(RectTransform rect)
    {
        rect.anchorMin = TextBattleStageDetail.rectTransform.anchorMin;
        rect.anchorMax = TextBattleStageDetail.rectTransform.anchorMax;
        rect.pivot = new Vector2(0.5f, 0f);
        float bottom = _detailPos.y - _detailSize.y * 0.5f;
        rect.anchoredPosition = new Vector2(_detailPos.x + _detailSize.x * 0.5f, bottom);
        rect.sizeDelta = new Vector2(_detailSize.x, 64f);
    }

    public void Open(int keepSetIndex = 0)
    {
        ClearBattleEndedNote();
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
        int windowStart = ControllerProduction.GetViewedFloorWindowStart(CurrentStageIndex);
        int windowEnd = ControllerProduction.GetViewedFloorWindowEnd(CurrentStageIndex);
        if (activeStage == CurrentStageIndex && ControllerProduction.IsSortieFloorRange(activeStage, activeFloorFrom, activeFloorTo)
            && activeFloorFrom >= windowStart && activeFloorTo <= windowEnd)
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
            KeepLockedColor(ButtonBattleStages[i]);
            if (lockedLook)
                ButtonBattleStages[i].image.color = ColorStageLocked;
            else
                ButtonBattleStages[i].image.color = (stageIndex == CurrentStageIndex) ? ColorButtonSelected : ColorButtonNormal;
            ApplyStageButtonBackground(ButtonBattleStages[i], stageIndex, label, lockedLook);
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
                int start = ControllerProduction.GetViewedFloorWindowStart(stageIndex);
                int end = ControllerProduction.GetViewedFloorWindowEnd(stageIndex);
                label.text = StageNames[stageIndex] + "\n" + start.ToString() + "〜" + end.ToString() + "階";
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                if (stageIndex == activeStage)
                    label.text += "\n出撃中";
                else if (ControllerProduction.GetHighestClearedBattleFloor(stageIndex) >= ControllerProduction.StageFloorCap(stageIndex))
                    label.text += "\nクリア";
            }
        }

        if (ControllerProduction.IsBattleRunning() && activeStage != 0)
            ControllerProduction.ShowBattleStageBackground(activeStage);

        RefreshStageEnemyPortraits();
        PaintChallengeMarks();
        PlaceTrailingStageButtons(activeStage);

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
        bool anyOwned = false;
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
                if (member != null)
                {
                    member.EnsureStock();
                    if (member.Stock.Total > 0)
                        anyOwned = true;
                }
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
        if (setEmpty)
            ApplyPartyError("セット" + CurrentSetIndex.ToString() + "にキャラがいません");
        else if (!anyOwned)
            ApplyPartyError("セット" + CurrentSetIndex.ToString() + "のキャラの所持数がすべて0です");
        else
            ApplyPartyError("");

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
            bool repeatOn = repeatOpen && ControllerProduction.GetBattleRepeat();
            PaintUsualButton(ButtonBattleRepeat, repeatOn ? ColorRepeatOn : ColorRepeatOff, repeatOpen);
            Text repeatLabel = ButtonBattleRepeat.GetComponentInChildren<Text>();
            if (!repeatOpen)
            {
                if (repeatLabel != null)
                {
                    repeatLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    repeatLabel.verticalOverflow = VerticalWrapMode.Overflow;
                    repeatLabel.text = "Repeat\nクエストを受け取ると開く";
                }
                LayoutRepeatButtonLabel(repeatLabel, false);
            }
            else if (repeatLabel != null)
            {
                repeatLabel.color = Color.white;
                repeatLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                repeatLabel.verticalOverflow = VerticalWrapMode.Overflow;
                repeatLabel.text = ControllerProduction.GetBattleRepeat() ? "Repeat\nON" : "Repeat\nOFF";
                LayoutRepeatButtonLabel(repeatLabel, true);
            }
        }

        PaintFloorRangeText();
    }

    void PaintFloorRangeText()
    {
        if (TextBattleFloorRange == null || ControllerProduction == null)
            return;
        GetSelectedFloorRange(out int floorFrom, out int floorTo);
        TextBattleFloorRange.text = floorFrom.ToString() + "階 〜 " + floorTo.ToString() + "階";
        int maxFrom = ControllerProduction.GetMaxBattleFloorFrom(CurrentStageIndex);
        int windowStart = ControllerProduction.GetViewedFloorWindowStart(CurrentStageIndex);
        int lastBandStart = windowStart + Constants.BATTLE_STAGE_FLOOR_MAX - Constants.BATTLE_STAGE_FLOOR_STEP;
        if (maxFrom < lastBandStart)
        {
            int highest = ControllerProduction.GetHighestClearedBattleFloor(CurrentStageIndex);
            if (highest <= 0)
                TextBattleFloorRange.text += "\n（開始は1階まで）";
            else
                TextBattleFloorRange.text += "\n（" + highest.ToString() + "階クリア、開始は" + maxFrom.ToString() + "階まで）";
        }

        int activeStage = ControllerProduction.GetActiveBattleStage();
        int activeFloorFrom = ControllerProduction.GetActiveBattleFloorFrom();
        int activeFloorTo = ControllerProduction.GetActiveBattleFloorTo();
        if (activeStage == CurrentStageIndex && activeFloorFrom > 0
            && (activeFloorFrom != floorFrom || activeFloorTo != floorTo))
        {
            TextBattleFloorRange.text += "\n出撃中 " + activeFloorFrom.ToString() + "階 〜 " + activeFloorTo.ToString() + "階";
        }
    }

    // クリアで開始できる上限が伸びたとき。選択はそのまま、バーの長さと文字だけ合わせる。
    public void RefreshFloorSliderLimit()
    {
        ApplyFloorFromLimit();
        PaintFloorRangeText();
    }

    void ApplyFloorFromLimit()
    {
        if (SliderBattleFloorFrom == null || ControllerProduction == null)
            return;

        int maxFrom = ControllerProduction.GetMaxBattleFloorFrom(CurrentStageIndex);
        int windowStart = ControllerProduction.GetViewedFloorWindowStart(CurrentStageIndex);
        int maxBand = (maxFrom - windowStart) / Constants.BATTLE_STAGE_FLOOR_STEP + 1;
        if (maxBand < 1)
            maxBand = 1;
        int totalBands = Constants.BATTLE_STAGE_FLOOR_MAX / Constants.BATTLE_STAGE_FLOOR_STEP;
        if (totalBands < 1)
            totalBands = 1;
        if (maxBand > totalBands)
            maxBand = totalBands;

        int fromBand = Mathf.RoundToInt(SliderBattleFloorFrom.value);
        if (fromBand > maxBand)
            fromBand = maxBand;
        if (fromBand < 1)
            fromBand = 1;
        SliderBattleFloorFrom.minValue = 1;
        SliderBattleFloorFrom.maxValue = maxBand;
        SliderBattleFloorFrom.SetValueWithoutNotify(fromBand);
        ResizeFloorFromSlider(maxBand);

        if (SliderBattleFloorTo == null)
            return;
        int toBand = Mathf.RoundToInt(SliderBattleFloorTo.value);
        fromBand = Mathf.RoundToInt(SliderBattleFloorFrom.value);
        if (toBand < fromBand)
            toBand = fromBand;
        if (toBand > totalBands)
            toBand = totalBands;
        SliderBattleFloorTo.minValue = 1;
        SliderBattleFloorTo.maxValue = totalBands;
        SliderBattleFloorTo.SetValueWithoutNotify(toBand);
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
        int windowStart = ControllerProduction != null ? ControllerProduction.GetViewedFloorWindowStart(CurrentStageIndex) : 1;
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
            windowStart = ControllerProduction.GetViewedFloorWindowStart(CurrentStageIndex);
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

    void EnsureFloorWindowControls()
    {
        for (int i = 0; i < ButtonBattleStages.Length && i < Constants.BATTLE_STAGE_NUM; i++)
        {
            Button card = ButtonBattleStages[i];
            if (card == null)
                continue;
            int stageIndex = i + 1;
            FloorWindowPrev[i] = EnsureFloorArrow(card, "ButtonBattleStageFloorPrev" + stageIndex.ToString(), true, stageIndex);
            FloorWindowNext[i] = EnsureFloorArrow(card, "ButtonBattleStageFloorNext" + stageIndex.ToString(), false, stageIndex);
            ChallengeStarPlates[i] = EnsureChallengePlate(card, "ImageChallengeStarPlate" + stageIndex.ToString(), stageIndex);
            for (int star = 0; star < ChallengeRules.KindCount; star++)
            {
                ChallengeStarBlurs[i, star] = EnsureChallengeBlur(card, "ImageChallengeStarBlur" + stageIndex.ToString() + "_" + star.ToString());
                ChallengeStarGlows[i, star] = EnsureChallengeStar(card, "ImageChallengeStarGlow" + stageIndex.ToString() + "_" + star.ToString(), true);
                ChallengeStars[i, star] = EnsureChallengeStar(card, "ImageChallengeStar" + stageIndex.ToString() + "_" + star.ToString(), false);
            }
        }
    }

    Button EnsureFloorArrow(Button card, string name, bool previous, int stageIndex)
    {
        Transform existing = card.transform.Find(name);
        if (existing != null)
        {
            Button found = existing.GetComponent<Button>();
            LayoutFloorArrow(found, previous);
            return found;
        }

        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(card.transform, false);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(1f, 0.9f, 0.45f, 1f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
        button.colors = colors;
        int direction = previous ? -1 : 1;
        button.onClick.AddListener(() => PushFloorWindow(stageIndex, direction));
        GameAudio.BindUi(button);
        LayoutFloorArrow(button, previous);
        obj.SetActive(false);
        return button;
    }

    Image EnsureChallengePlate(Button card, string name, int stageIndex)
    {
        Transform existing = card.transform.Find(name);
        GameObject obj;
        if (existing != null)
            obj = existing.gameObject;
        else
        {
            obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(card.transform, false);
            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            obj.SetActive(false);
        }
        Image image = obj.GetComponent<Image>();
        image.sprite = ChallengeMarks.Square();
        image.color = new Color(0f, 0f, 0f, 0.62f);
        image.raycastTarget = true;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        Button button = obj.GetComponent<Button>();
        if (button == null)
            button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => OpenChallengeStars(stageIndex));
        GameAudio.ForceBindUi(button);
        return image;
    }

    void OpenChallengeStars(int stageIndex)
    {
        if (ControllerProduction == null)
            return;
        int start = ControllerProduction.GetViewedFloorWindowStart(stageIndex);
        EnsureStarPopup();
        if (_starPopup == null)
            return;
        FillStarPopup(stageIndex, start);
        _starPopup.SetActive(true);
        _starPopup.transform.SetAsLastSibling();
    }

    void CloseChallengeStars()
    {
        if (_starPopup != null)
            _starPopup.SetActive(false);
    }

    void EnsureStarPopup()
    {
        if (_starPopup != null)
            return;
        GameObject panelObject = GameObject.Find("PanelBattleStage");
        if (panelObject == null)
            return;
        _starPopup = new GameObject("ChallengeStarPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        _starPopup.transform.SetParent(panelObject.transform, false);
        RectTransform dimRect = _starPopup.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        Image dim = _starPopup.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.62f);
        dim.raycastTarget = true;
        Button dimButton = _starPopup.GetComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(CloseChallengeStars);
        GameAudio.BindUi(dimButton);

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        card.transform.SetParent(_starPopup.transform, false);
        _starPopupCard = card.GetComponent<RectTransform>();
        _starPopupCard.anchorMin = new Vector2(0.5f, 0.5f);
        _starPopupCard.anchorMax = new Vector2(0.5f, 0.5f);
        _starPopupCard.pivot = new Vector2(0.5f, 0.5f);
        Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = ChallengeMarks.Square();
        cardImage.color = new Color(0.08f, 0.09f, 0.12f, 1f);
        cardImage.raycastTarget = true;
        Button cardButton = card.GetComponent<Button>();
        cardButton.transition = Selectable.Transition.None;

        _starPopupTitle = CreateStarPopupText("Title", card.transform, 36, TextAnchor.MiddleCenter);
        _starPopupTitle.color = Color.white;

        GameObject rows = new GameObject("Rows", typeof(RectTransform));
        rows.transform.SetParent(card.transform, false);
        _starPopupRows = rows.GetComponent<RectTransform>();
        _starPopupRows.anchorMin = new Vector2(0f, 1f);
        _starPopupRows.anchorMax = new Vector2(1f, 1f);
        _starPopupRows.pivot = new Vector2(0.5f, 1f);

        Button close = CreateStarPopupButton(card.transform, "閉じる", new Color(0.45f, 0.45f, 0.48f, 1f));
        close.onClick.AddListener(CloseChallengeStars);
        GameAudio.BindUi(close);
        _starPopup.SetActive(false);
    }

    void FillStarPopup(int stageIndex, int rangeStart)
    {
        int end = ChallengeRules.RangeEnd(rangeStart);
        _starPopupTitle.text = GetStageName(stageIndex) + "　" + rangeStart.ToString() + "~" + end.ToString() + "階";
        for (int i = _starPopupRows.childCount - 1; i >= 0; i--)
            Destroy(_starPopupRows.GetChild(i).gameObject);
        ChallengeRules.CollectSlots(stageIndex, rangeStart, ChallengeSlots);
        const float rowH = 96f;
        const float titleH = 88f;
        const float closeH = 108f;
        float height = 28f + titleH + ChallengeSlots.Count * rowH + closeH;
        _starPopupCard.sizeDelta = new Vector2(980f, height);
        RectTransform titleRect = _starPopupTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -16f);
        titleRect.sizeDelta = new Vector2(-48f, titleH - 16f);
        _starPopupRows.anchoredPosition = new Vector2(0f, -titleH);
        _starPopupRows.sizeDelta = new Vector2(-36f, ChallengeSlots.Count * rowH);
        for (int i = 0; i < ChallengeSlots.Count; i++)
        {
            bool done = ControllerProduction != null && ControllerProduction.IsChallengeStarLit(stageIndex, rangeStart, i);
            AddChallengeStarRow(i, ChallengeSlots[i], rangeStart, done, rowH);
        }
        Transform close = _starPopupCard.Find("ButtonClose");
        if (close != null)
        {
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.08f, 0f);
            closeRect.anchorMax = new Vector2(0.92f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 24f);
            closeRect.sizeDelta = new Vector2(0f, 84f);
        }
    }

    void AddChallengeStarRow(int index, ChallengeKind kind, int rangeStart, bool done, float rowH)
    {
        GameObject row = new GameObject("Row" + index.ToString(), typeof(RectTransform));
        row.transform.SetParent(_starPopupRows, false);
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * rowH);
        rect.sizeDelta = new Vector2(0f, rowH);

        Image star = new GameObject("Star", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        star.transform.SetParent(row.transform, false);
        star.sprite = ChallengeMarks.Star();
        star.raycastTarget = false;
        star.color = done ? new Color(1f, 0.86f, 0.2f, 1f) : new Color(0.62f, 0.62f, 0.66f, 1f);
        RectTransform starRect = star.rectTransform;
        starRect.anchorMin = new Vector2(0f, 0.5f);
        starRect.anchorMax = new Vector2(0f, 0.5f);
        starRect.pivot = new Vector2(0f, 0.5f);
        starRect.anchoredPosition = new Vector2(8f, 0f);
        starRect.sizeDelta = new Vector2(64f, 64f);

        Text body = CreateStarPopupText("Body", row.transform, 30, TextAnchor.MiddleLeft);
        body.text = ChallengeCaption(kind, rangeStart);
        body.color = Color.white;
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(84f, 0f);
        bodyRect.offsetMax = new Vector2(-150f, 0f);

        Text state = CreateStarPopupText("State", row.transform, 30, TextAnchor.MiddleRight);
        state.text = done ? "達成" : "未達成";
        state.color = done ? new Color(1f, 0.86f, 0.2f, 1f) : new Color(0.62f, 0.62f, 0.66f, 1f);
        RectTransform stateRect = state.rectTransform;
        stateRect.anchorMin = new Vector2(1f, 0f);
        stateRect.anchorMax = new Vector2(1f, 1f);
        stateRect.pivot = new Vector2(1f, 0.5f);
        stateRect.anchoredPosition = new Vector2(-8f, 0f);
        stateRect.sizeDelta = new Vector2(140f, 0f);
    }

    static string ChallengeCaption(ChallengeKind kind, int rangeStart)
    {
        int end = ChallengeRules.RangeEnd(rangeStart);
        int seconds = Mathf.RoundToInt(ChallengeRules.TimeLimit(kind));
        if (kind == ChallengeKind.Rush)
            return rangeStart.ToString() + "~" + end.ToString() + "階を一気にクリア";
        if (kind == ChallengeKind.NoDamage)
            return ChallengeRules.NoDamageFloor(rangeStart).ToString() + "~" + end.ToString() + "階を誰も倒れずにクリア";
        if (kind == ChallengeKind.MidBossTime)
            return ChallengeRules.MidBossFloor(rangeStart).ToString() + "階のボスを" + seconds.ToString() + "秒以内に倒す";
        if (kind == ChallengeKind.BossTime)
            return end.ToString() + "階のボスを" + seconds.ToString() + "秒以内に倒す";
        return "";
    }

    Text CreateStarPopupText(string name, Transform parent, int size, TextAnchor anchor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = StageFont();
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    Button CreateStarPopupButton(Transform parent, string label, Color color)
    {
        GameObject obj = new GameObject("ButtonClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = ChallengeMarks.Square();
        image.color = color;
        Text text = CreateStarPopupText("Text", obj.transform, 30, TextAnchor.MiddleCenter);
        text.text = label;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return obj.GetComponent<Button>();
    }

    Font StageFont()
    {
        if (TextBattleStageDetail != null && TextBattleStageDetail.font != null)
            return TextBattleStageDetail.font;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    Image EnsureChallengeBlur(Button card, string name)
    {
        Transform existing = card.transform.Find(name);
        if (existing != null)
            return existing.GetComponent<Image>();

        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(card.transform, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = ChallengeMarks.StarBlur();
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(108f, 108f);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        obj.SetActive(false);
        return image;
    }

    Image EnsureChallengeStar(Button card, string name, bool glow)
    {
        Transform existing = card.transform.Find(name);
        if (existing != null)
            return existing.GetComponent<Image>();

        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(card.transform, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = ChallengeMarks.Star();
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = glow ? new Vector2(76f, 76f) : new Vector2(60f, 60f);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        obj.SetActive(false);
        return image;
    }

    void PushFloorWindow(int stageIndex, int direction)
    {
        if (ControllerProduction == null || !ControllerProduction.TryShiftViewedFloorWindow(stageIndex, direction))
            return;
        CurrentStageIndex = stageIndex;
        int start = ControllerProduction.GetViewedFloorWindowStart(stageIndex);
        SetFloorSliders(start, start + Constants.BATTLE_STAGE_FLOOR_STEP - 1);
        Refresh();
    }

    void PaintChallengeMarks()
    {
        bool feature = ControllerProduction != null && ControllerProduction.IsFloorRangeSelectUnlocked();
        int activeStage = ControllerProduction != null ? ControllerProduction.GetActiveBattleStage() : 0;
        for (int i = 0; i < ButtonBattleStages.Length && i < Constants.BATTLE_STAGE_NUM; i++)
        {
            Button card = ButtonBattleStages[i];
            if (card == null)
                continue;
            int stageIndex = i + 1;
            bool unlocked = ControllerProduction != null && ControllerProduction.IsBattleStageUnlocked(stageIndex);
            bool lockedLook = !unlocked && stageIndex != activeStage;
            bool show = feature && card.gameObject.activeSelf && !lockedLook;
            if (FloorWindowPrev[i] != null)
            {
                FloorWindowPrev[i].gameObject.SetActive(show && ControllerProduction.CanShiftViewedFloorWindow(stageIndex, -1));
                PlaceFloorArrow(FloorWindowPrev[i], true);
            }
            if (FloorWindowNext[i] != null)
            {
                FloorWindowNext[i].gameObject.SetActive(show && ControllerProduction.CanShiftViewedFloorWindow(stageIndex, 1));
                PlaceFloorArrow(FloorWindowNext[i], false);
            }
            int rangeStart = ControllerProduction != null ? ControllerProduction.GetViewedFloorWindowStart(stageIndex) : 1;
            ChallengeRules.CollectSlots(stageIndex, rangeStart, ChallengeSlots);
            const float size = 60f;
            const float gap = 8f;
            const float left = 24f;
            const float bottom = 20f;
            int slots = ChallengeSlots.Count;
            float row = slots * size + Mathf.Max(0, slots - 1) * gap;
            if (ChallengeStarPlates[i] != null)
            {
                ChallengeStarPlates[i].gameObject.SetActive(show && slots > 0);
                RectTransform plate = ChallengeStarPlates[i].rectTransform;
                plate.anchorMin = new Vector2(0f, 0f);
                plate.anchorMax = new Vector2(0f, 0f);
                plate.pivot = new Vector2(0f, 0f);
                plate.sizeDelta = new Vector2(row + 16f, size + 12f);
                plate.anchoredPosition = new Vector2(left - 8f, bottom - 6f);
                ChallengeStarPlates[i].transform.SetAsLastSibling();
            }
            for (int star = 0; star < ChallengeRules.KindCount; star++)
            {
                bool slot = show && star < slots;
                bool lit = slot && ControllerProduction.IsChallengeStarLit(stageIndex, rangeStart, star);
                float x = left + size * 0.5f + star * (size + gap);
                float y = bottom + size * 0.5f;
                PlaceChallengeBlur(ChallengeStarBlurs[i, star], x, y, size, slot && lit);
                PlaceChallengeStar(ChallengeStarGlows[i, star], x, y, size, slot, lit, true);
                PlaceChallengeStar(ChallengeStars[i, star], x, y + 1f, size, slot, lit, false);
            }
            Text label = card.GetComponentInChildren<Text>();
            if (label != null && show)
            {
                label.rectTransform.offsetMin = new Vector2(64f, 0f);
                label.rectTransform.offsetMax = new Vector2(-64f, 0f);
            }
            if (FloorWindowPrev[i] != null)
                FloorWindowPrev[i].transform.SetAsLastSibling();
            if (FloorWindowNext[i] != null)
                FloorWindowNext[i].transform.SetAsLastSibling();
        }
    }

    static void PlaceFloorArrow(Button button, bool previous)
    {
        LayoutFloorArrow(button, previous);
    }

    // 絵は52×68のまま、回転は絵だけ。当たりは回さない。
    // 横は中央側だけ、絵の2.25倍（1.5倍のさらに1.5倍）。縦は上下に1.2倍。
    static void LayoutFloorArrow(Button button, bool previous)
    {
        if (button == null)
            return;
        const float visualW = 52f;
        const float visualH = 68f;
        const float hitW = visualW * 2.25f;
        const float hitH = visualH * 1.2f;
        float shift = (hitW - visualW) * 0.5f;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(previous ? 0f : 1f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(hitW, hitH);
        rect.anchoredPosition = new Vector2(previous ? 36f + shift : -36f - shift, 46f);
        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;

        Image hit = button.GetComponent<Image>();
        hit.sprite = ChallengeMarks.Square();
        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;
        hit.raycastPadding = Vector4.zero;
        hit.canvasRenderer.cullTransparentMesh = false;

        Image icon = EnsureFloorArrowIcon(button);
        icon.sprite = ChallengeMarks.Triangle();
        icon.raycastTarget = false;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(visualW, visualH);
        iconRect.anchoredPosition = new Vector2(previous ? -shift : shift, 0f);
        iconRect.localEulerAngles = previous ? new Vector3(0f, 0f, 180f) : Vector3.zero;
        iconRect.localScale = Vector3.one;
        button.targetGraphic = icon;
    }

    static Image EnsureFloorArrowIcon(Button button)
    {
        Transform existing = button.transform.Find("ImageFloorArrow");
        if (existing != null)
            return existing.GetComponent<Image>();
        GameObject obj = new GameObject("ImageFloorArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(button.transform, false);
        Image image = obj.GetComponent<Image>();
        image.color = Color.white;
        return image;
    }

    static void PlaceChallengeStar(Image image, float x, float y, float size, bool visible, bool lit, bool glow)
    {
        if (image == null)
            return;
        image.gameObject.SetActive(visible);
        if (!visible)
            return;
        float span = !glow ? size : (lit ? size + 28f : size + 16f);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(span, span);
        rect.anchoredPosition = new Vector2(x, y);
        if (glow && lit)
            image.color = new Color(1f, 1f, 0.82f, 1f);
        else if (glow)
            image.color = new Color(0f, 0f, 0f, 0.9f);
        else if (lit)
            image.color = new Color(1f, 0.86f, 0.2f, 1f);
        else
            image.color = new Color(0.62f, 0.62f, 0.66f, 1f);
        image.transform.SetAsLastSibling();
    }

    static void PlaceChallengeBlur(Image image, float x, float y, float size, bool visible)
    {
        if (image == null)
            return;
        image.gameObject.SetActive(visible);
        if (!visible)
            return;
        float span = size + 48f;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(span, span);
        rect.anchoredPosition = new Vector2(x, y);
        image.color = new Color(1f, 1f, 1f, 0.95f);
        image.transform.SetAsLastSibling();
    }

    void EnsureTrailingStageButtons()
    {
        Button source = LastStageButton();
        if (source == null)
        {
            Debug.LogWarning("ギャラリーのボタンを置く元になるステージボタンがありません");
            return;
        }

        ButtonComingSoon = FindOrCloneStageButton(source, "ButtonBattleStageComingSoon");
        ButtonGalleryStage = FindOrCloneStageButton(source, "ButtonBattleStageGallery");
        if (ButtonComingSoon != null)
        {
            ButtonComingSoon.onClick = new Button.ButtonClickedEvent();
            ButtonComingSoon.gameObject.SetActive(false);
        }
        if (ButtonGalleryStage != null)
        {
            ButtonGalleryStage.onClick = new Button.ButtonClickedEvent();
            ButtonGalleryStage.onClick.AddListener(() => PushButtonSelectBattleStage(Constants.BATTLE_STAGE_GALLERY));
        }
        if (ButtonComingSoon != null)
            ButtonComingSoon.transform.SetAsLastSibling();
        if (ButtonGalleryStage != null)
            ButtonGalleryStage.transform.SetAsLastSibling();
    }

    Button LastStageButton()
    {
        Button source = null;
        for (int i = 0; i < ButtonBattleStages.Length; i++)
        {
            if (ButtonBattleStages[i] != null)
                source = ButtonBattleStages[i];
        }
        return source;
    }

    static Button FindOrCloneStageButton(Button source, string name)
    {
        Transform parent = source.transform.parent;
        Transform existing = parent != null ? parent.Find(name) : null;
        if (existing != null)
            return existing.GetComponent<Button>();
        GameObject clone = Instantiate(source.gameObject, parent);
        clone.name = name;
        return clone.GetComponent<Button>();
    }

    bool AllConfiguredStagesUnlocked()
    {
        if (ControllerProduction == null)
            return false;
        for (int stage = 1; stage <= Constants.BATTLE_STAGE_NUM; stage++)
        {
            if (!ControllerProduction.IsBattleStageUnlocked(stage))
                return false;
        }
        return true;
    }

    // 表示枠の幅も高さも 0 だと、中のカードがマスクで全部隠れる。
    static void StretchStageViewport(RectTransform viewRect)
    {
        if (viewRect == null)
            return;
        if (viewRect.anchorMin != viewRect.anchorMax)
            return;
        if (viewRect.sizeDelta.x > 1f || viewRect.sizeDelta.y > 1f)
            return;
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = new Vector2(-20f, 0f);
    }

    // 設定済みのステージが全部開いたら、ギャラリーの前に空枠を出す。出撃はできない。
    // 一覧は縦並びなので、右へずらすと横スクロールがなく、枠の外に隠れる。
    void PlaceTrailingStageButtons(int activeStage)
    {
        Button anchor = LastStageButton();
        bool showSoon = AllConfiguredStagesUnlocked();
        if (ButtonComingSoon != null)
        {
            ButtonComingSoon.gameObject.SetActive(showSoon);
            if (showSoon)
                PaintComingSoon();
        }
        if (ButtonGalleryStage != null)
            ButtonGalleryStage.gameObject.SetActive(true);

        if (anchor != null)
        {
            Transform parent = anchor.transform.parent;
            if (ButtonComingSoon != null)
                ButtonComingSoon.transform.SetAsLastSibling();
            if (ButtonGalleryStage != null)
                ButtonGalleryStage.transform.SetAsLastSibling();

            bool laidOut = parent != null && parent.GetComponent<LayoutGroup>() != null;
            if (!laidOut)
            {
                RectTransform anchorRect = anchor.GetComponent<RectTransform>();
                float gap = anchorRect.sizeDelta.x;
                if (gap < 1f)
                    gap = 160f;
                float step = gap + 12f;
                if (showSoon && ButtonComingSoon != null)
                {
                    RectTransform soon = ButtonComingSoon.GetComponent<RectTransform>();
                    soon.anchoredPosition = anchorRect.anchoredPosition + new Vector2(step, 0f);
                }
                if (ButtonGalleryStage != null)
                {
                    RectTransform gallery = ButtonGalleryStage.GetComponent<RectTransform>();
                    gallery.anchoredPosition = anchorRect.anchoredPosition + new Vector2(step * (showSoon ? 2f : 1f), 0f);
                }
                int extras = (ButtonGalleryStage != null ? 1 : 0) + (showSoon ? 1 : 0);
                FitTrailContent(anchorRect.parent as RectTransform, step * extras);
            }
            else if (parent is RectTransform content)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }
        }

        PaintGalleryButton(activeStage);
    }

    void FitTrailContent(RectTransform content, float extra)
    {
        if (content == null || content.GetComponent<ContentSizeFitter>() != null)
            return;
        if (TrailContentWidth < 0f)
            TrailContentWidth = content.sizeDelta.x;
        if (TrailContentWidth <= extra)
            return;
        float width = TrailContentWidth + extra;
        if (Mathf.Approximately(content.sizeDelta.x, width))
            return;
        content.sizeDelta = new Vector2(width, content.sizeDelta.y);
    }

    void PaintComingSoon()
    {
        if (ButtonComingSoon == null)
            return;
        ButtonComingSoon.interactable = false;
        KeepLockedColor(ButtonComingSoon);
        ButtonComingSoon.image.color = ColorStageLocked;
        Text label = ButtonComingSoon.GetComponentInChildren<Text>();
        ApplyStageButtonBackground(ButtonComingSoon, 0, label, true);
        if (label == null)
            return;
        label.text = "Coming Soon……";
        label.color = Color.white;
        MatchStageTitleSize(label);
        label.transform.SetAsLastSibling();
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
            KeepLockedColor(ButtonGalleryStage);
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

        int start = ControllerProduction.GetViewedFloorWindowStart(Constants.BATTLE_STAGE_GALLERY);
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

    static void KeepLockedColor(Button button)
    {
        if (button == null)
            return;
        ColorBlock colors = button.colors;
        if (colors.disabledColor == Color.white)
            return;
        colors.disabledColor = Color.white;
        button.colors = colors;
    }

    // 各ステージカードの右下に、出る敵を並べる。大きさはカードの高さの3分の2から、さらに3割小さい。
    void ApplyStageButtonBackground(Button button, int stageIndex, Text label, bool locked = false)
    {
        if (button == null)
            return;
        Sprite sprite = locked ? null : BattleStageCatalog.BackgroundSprite(stageIndex);
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
        if (ControllerProduction == null)
            return;

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

            int floorFrom = ControllerProduction.GetViewedFloorWindowStart(stageIndex);
            int floorTo = ControllerProduction.GetViewedFloorWindowEnd(stageIndex);
            BattleStageCatalog.CollectEnemyIds(stageIndex, floorFrom, floorTo, StageEnemyIds);
            if (StagePortraitsMatch(stageIndex, StageEnemyIds))
                continue;

            RemoveStagePortraits(stageIndex);
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
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = new Vector2(x + size * 0.5f, pad + size * 0.5f);
                rect.localScale = new Vector3(-1f, 1f, 1f);
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

    bool StagePortraitsMatch(int stageIndex, List<uint> ids)
    {
        int seen = 0;
        for (int i = 0; i < StageEnemyPortraits.Count; i++)
        {
            if (StageEnemyPortraits[i].StageIndex != stageIndex)
                continue;
            if (seen >= ids.Count || StageEnemyPortraits[i].CharacterId != ids[seen])
                return false;
            seen++;
        }
        return seen == ids.Count;
    }

    void RemoveStagePortraits(int stageIndex)
    {
        for (int i = StageEnemyPortraits.Count - 1; i >= 0; i--)
        {
            if (StageEnemyPortraits[i].StageIndex != stageIndex)
                continue;
            if (StageEnemyPortraits[i].Image != null)
                Destroy(StageEnemyPortraits[i].Image.gameObject);
            StageEnemyPortraits.RemoveAt(i);
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
