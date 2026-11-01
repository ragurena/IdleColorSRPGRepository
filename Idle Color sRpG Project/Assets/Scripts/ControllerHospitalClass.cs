using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 病院画面。ベッドカードと待機列は、合成画面と同じ白枠と文字で組む。
public class ControllerHospitalClass : MonoBehaviour
{
    const float SpiritAlpha = 0.7f;
    const float WaitListHeight = 248f;
    const float WaitPictureSize = 128f;
    const float WaitCaptionGap = 4f;
    const float WaitCaptionHeight = 44f;
    const float WaitCellWidth = 156f;
    const float WaitCellHeight = WaitPictureSize + WaitCaptionGap + WaitCaptionHeight;
    const float WaitEdge = (WaitListHeight - WaitCellHeight) * 0.5f;
    ControllerProduction _production;
    Font _font;
    RectTransform _cardRoot;
    RectTransform _cardScrollRect;
    ScrollRect _waitList;
    RectTransform _waitScrollRect;
    RectTransform _waitContent;
    Text _waitEmpty;
    int _builtBeds = -1;
    string _waitSignature = "";
    readonly List<CardView> _cards = new List<CardView>();
    readonly List<WaitView> _waits = new List<WaitView>();
    readonly Dictionary<uint, Sprite> _sprites = new Dictionary<uint, Sprite>();
    readonly Dictionary<uint, int> _spriteTex = new Dictionary<uint, int>();
    static Sprite _white;
    static Sprite _aura;

    class CardView
    {
        public int CardIndex;
        public BedView Left;
        public BedView Right;
        public Button CareLeft;
        public Button CareRight;
        public Button Nurse;
        public Text NursePlus;
        public HospitalAura NurseAura;
    }

    class BedView
    {
        public int Bed;
        public GameObject Root;
        public Image Picture;
        public HospitalBreath Breath;
        public Text Empty;
        public GameObject Gauge;
        public Image Fill;
        public Text Time;
    }

    class WaitView
    {
        public ulong PatientId;
        public Image Picture;
        public Text Pixels;
        public Text Time;
    }

    public GameObject Panel { get; private set; }

    public void Initialize(ControllerProduction production, GameObject sizeSource)
    {
        _production = production;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        EnsureSprites();
        Build(sizeSource);
    }

    public void Open()
    {
        PlaceRegions();
        Refresh();
    }

    public void RefreshIfVisible()
    {
        if (Panel == null)
            return;
        CanvasGroup group = Panel.GetComponent<CanvasGroup>();
        if (group == null || group.alpha < 0.5f)
            return;
        Refresh();
    }

    public void Refresh()
    {
        if (_production == null || Panel == null)
            return;
        HospitalState hospital = _production.GetHospital();
        if (hospital == null)
            return;
        EnsureCards(hospital.UnlockedBeds);
        int waiting = hospital.WaitingCount;
        for (int i = 0; i < _cards.Count; i++)
            PaintCard(_cards[i], hospital);
        PaintWaiting(hospital);
        if (_waitEmpty != null)
            _waitEmpty.gameObject.SetActive(waiting == 0);
    }

    void EnsureCards(int beds)
    {
        if (beds == _builtBeds)
            return;
        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i].Left != null && _cards[i].Left.Root != null)
                Destroy(_cards[i].Left.Root.transform.parent.gameObject);
        }
        _cards.Clear();
        _builtBeds = beds;
        int cards = (beds + 1) / 2;
        if (cards < 1)
            cards = 1;
        float cardHeight = 300f;
        float gap = 16f;
        for (int card = 0; card < cards; card++)
        {
            GameObject root = new GameObject("HospitalCard" + card.ToString("00"), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(_cardRoot, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, cardHeight);
            rect.anchoredPosition = new Vector2(0f, -card * (cardHeight + gap));
            root.GetComponent<Image>().color = Color.black;

            CardView view = new CardView();
            view.CardIndex = card;
            int leftBed = card * 2 + 1;
            int rightBed = leftBed + 1;
            view.CareLeft = CreateCareButton(root.transform, leftBed, -415f);
            view.Left = CreateBed(root.transform, leftBed, -192f);
            view.NurseAura = CreateNurseAura(root.transform);
            view.Nurse = CreateNurseSlot(root.transform, card + 1);
            view.NursePlus = view.Nurse.GetComponentInChildren<Text>();
            if (rightBed <= beds)
            {
                view.Right = CreateBed(root.transform, rightBed, 192f);
                view.CareRight = CreateCareButton(root.transform, rightBed, 415f);
            }
            _cards.Add(view);
        }
        float contentHeight = cards * cardHeight + (cards - 1) * gap + 8f;
        _cardRoot.sizeDelta = new Vector2(0f, contentHeight);
    }

    BedView CreateBed(Transform parent, int bed, float x)
    {
        GameObject root = new GameObject("HospitalBed" + bed.ToString("00"), typeof(RectTransform));
        root.transform.SetParent(parent, false);
        PlaceRect(root.GetComponent<RectTransform>(), x, 6f, 200f, 270f);

        BedView view = new BedView();
        view.Bed = bed;
        view.Root = root;

        GameObject pictureObject = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pictureObject.transform.SetParent(root.transform, false);
        PlaceRect(pictureObject.GetComponent<RectTransform>(), 0f, 42f, 130f, 130f);
        view.Picture = pictureObject.GetComponent<Image>();
        view.Picture.preserveAspect = true;
        view.Picture.raycastTarget = false;
        view.Breath = pictureObject.AddComponent<HospitalBreath>();
        view.Breath.Phase = bed * 0.7f;

        view.Empty = CreateText(root.transform, "TextHospitalEmpty" + bed.ToString("00"), "空床", 32, TextAnchor.MiddleCenter);
        PlaceRect(view.Empty.rectTransform, 0f, 42f, 200f, 64f);

        GameObject gauge = new GameObject("Gauge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gauge.transform.SetParent(root.transform, false);
        PlaceRect(gauge.GetComponent<RectTransform>(), 0f, -62f, 180f, 34f);
        gauge.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 1f);
        gauge.GetComponent<Image>().raycastTarget = false;
        view.Gauge = gauge;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(gauge.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);
        view.Fill = fillObject.GetComponent<Image>();
        view.Fill.sprite = _white;
        view.Fill.type = Image.Type.Filled;
        view.Fill.fillMethod = Image.FillMethod.Horizontal;
        view.Fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        view.Fill.color = new Color(0.35f, 0.75f, 0.45f, 1f);
        view.Fill.raycastTarget = false;

        view.Time = CreateText(gauge.transform, "TextHospitalTime" + bed.ToString("00"), "0:00:00", 28, TextAnchor.MiddleCenter);
        Stretch(view.Time.rectTransform);
        view.Time.resizeTextForBestFit = true;
        view.Time.resizeTextMinSize = 12;
        view.Time.resizeTextMaxSize = 22;
        Outline outline = view.Time.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        Shadow shadow = view.Time.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return view;
    }

    Button CreateNurseSlot(Transform parent, int card)
    {
        Button button = CreateButton(parent, "ButtonHospitalNurseCard" + card.ToString("00"), "+", new Vector2(0f, 0f), new Vector2(130f, 130f), false);
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.fontSize = 64;
            label.color = new Color(0.93f, 0.94f, 0.96f, 1f);
        }
        Image image = button.GetComponent<Image>();
        image.preserveAspect = true;
        image.color = Color.white;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            if (_production != null)
                _production.PushButtonSelectCharacter(button.name);
        });
        return button;
    }

    HospitalAura CreateNurseAura(Transform parent)
    {
        GameObject auraObject = new GameObject("NurseAura", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        auraObject.transform.SetParent(parent, false);
        PlaceRect(auraObject.GetComponent<RectTransform>(), 0f, 0f, 200f, 200f);
        Image image = auraObject.GetComponent<Image>();
        image.sprite = _aura;
        image.raycastTarget = false;
        image.enabled = false;
        HospitalAura aura = auraObject.AddComponent<HospitalAura>();
        aura.Bind(image);
        return aura;
    }

    void PaintCard(CardView card, HospitalState hospital)
    {
        PaintBed(card.Left, hospital);
        PaintBed(card.Right, hospital);
        PaintCare(card.CareLeft, card.Left, hospital);
        PaintCare(card.CareRight, card.Right, hospital);
        PaintNurse(card, hospital);
    }

    void PaintNurse(CardView card, HospitalState hospital)
    {
        if (card.Nurse == null)
            return;
        uint nurseId = card.Left != null ? hospital.NurseOnBed(card.Left.Bed) : 0;
        uint personId = card.Left != null ? hospital.PersonOnBed(card.Left.Bed) : 0;
        if (personId != 0)
        {
            ControllerProduction.ShowAssignedPerson(card.Nurse, _production.GetPerson((int)personId));
            int level;
            int into;
            int need;
            HospitalMath.NurseLevelProgress(hospital.Experience(true, personId), out level, out into, out need);
            PersonSkillBar.Show(card.Nurse, level, into, need);
            CenterNursePerson(card.Nurse);
            if (card.NurseAura != null)
                card.NurseAura.Show(ControllerProduction.PersonColor((int)personId));
            return;
        }
        PersonSkillBar.Hide(card.Nurse);
        CharacterClass nurse = nurseId == 0 ? null : _production.GetCharacter(nurseId);
        ControllerProduction.ShowAssignedCharacter(card.Nurse, nurse);
        if (card.NurseAura == null)
            return;
        if (nurse != null)
            card.NurseAura.Show(new Color(nurse.RepresentativeR / 255f, nurse.RepresentativeG / 255f, nurse.RepresentativeB / 255f, 1f));
        else
            card.NurseAura.Hide();
    }

    static void CenterNursePerson(Button button)
    {
        if (button == null)
            return;
        RectTransform portrait = button.transform.Find("PersonPortrait") as RectTransform;
        if (portrait != null)
        {
            portrait.anchorMin = new Vector2(0.5f, 0.5f);
            portrait.anchorMax = new Vector2(0.5f, 0.5f);
            portrait.pivot = new Vector2(0.5f, 0.5f);
            portrait.anchoredPosition = Vector2.zero;
            portrait.sizeDelta = new Vector2(130f, 130f);
            AspectRatioFitter fitter = portrait.GetComponent<AspectRatioFitter>();
            if (fitter != null)
                fitter.enabled = false;
        }
        RectTransform bar = button.transform.Find("PersonSkillBar") as RectTransform;
        if (bar == null)
            return;
        bar.anchorMin = new Vector2(0.5f, 0.5f);
        bar.anchorMax = new Vector2(0.5f, 0.5f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0f, -65f);
        bar.sizeDelta = new Vector2(130f, 28f);
    }

    void PaintCare(Button button, BedView bed, HospitalState hospital)
    {
        if (button == null || _production == null)
            return;
        HospitalPatient patient = bed == null ? null : hospital.PatientOnBed(bed.Bed);
        if (patient == null)
        {
            if (button.interactable)
            {
                button.interactable = false;
                _production.StyleRepresentativeButton(button, Color.black, false);
            }
            return;
        }
        CharacterClass character = _production.GetCharacter(patient.CharacterId);
        _production.StyleRepresentativeButton(button, ControllerProduction.RepresentativeColor(character), true);
    }

    void PaintBed(BedView bed, HospitalState hospital)
    {
        if (bed == null)
            return;
        HospitalPatient patient = hospital.PatientOnBed(bed.Bed);
        if (patient == null)
        {
            bed.Picture.gameObject.SetActive(false);
            bed.Gauge.SetActive(false);
            bed.Empty.gameObject.SetActive(true);
        }
        else
        {
            bed.Empty.gameObject.SetActive(false);
            bed.Picture.gameObject.SetActive(true);
            bed.Gauge.SetActive(true);
            CharacterClass character = _production.GetCharacter(patient.CharacterId);
            bed.Picture.sprite = SpriteFor(character);
            bed.Picture.color = bed.Picture.sprite == null ? new Color(1f, 1f, 1f, 0f) : new Color(1f, 1f, 1f, SpiritAlpha);
            bed.Fill.fillAmount = HospitalMath.Gauge01(patient.HealedPixels, patient.OpaquePixels);
            bed.Time.text = HospitalMath.FormatSeconds(hospital.RemainingSeconds(patient));
        }
    }

    void PaintWaiting(HospitalState hospital)
    {
        List<HospitalPatient> waiting = new List<HospitalPatient>();
        hospital.CopyWaiting(waiting);
        string signature = waiting.Count.ToString();
        for (int i = 0; i < waiting.Count; i++)
            signature += "," + waiting[i].Id.ToString() + ":" + waiting[i].CharacterId.ToString() + ":" + waiting[i].OpaquePixels.ToString();
        if (signature == _waitSignature)
            return;
        _waitSignature = signature;
        for (int i = 0; i < _waits.Count; i++)
        {
            if (_waits[i].Picture == null)
                continue;
            GameObject row = _waits[i].Picture.transform.parent.gameObject;
            row.SetActive(false);
            row.transform.SetParent(null, false);
            Destroy(row);
        }
        _waits.Clear();
        for (int i = waiting.Count - 1; i >= 0; i--)
            _waits.Add(CreateWaitRow(waiting[i]));
        LayoutRebuilder.ForceRebuildLayoutImmediate(_waitContent);
        if (_waitList != null)
            _waitList.horizontalNormalizedPosition = 0f;
    }

    WaitView CreateWaitRow(HospitalPatient patient)
    {
        GameObject cell = new GameObject("HospitalWait" + patient.Id.ToString(), typeof(RectTransform));
        cell.transform.SetParent(_waitContent, false);
        LayoutElement element = cell.AddComponent<LayoutElement>();
        element.preferredWidth = WaitCellWidth;
        element.preferredHeight = WaitCellHeight;
        RectTransform cellRect = cell.GetComponent<RectTransform>();
        cellRect.sizeDelta = new Vector2(WaitCellWidth, WaitCellHeight);

        GameObject pictureObject = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pictureObject.transform.SetParent(cell.transform, false);
        RectTransform pictureRect = pictureObject.GetComponent<RectTransform>();
        pictureRect.anchorMin = new Vector2(0.5f, 1f);
        pictureRect.anchorMax = new Vector2(0.5f, 1f);
        pictureRect.pivot = new Vector2(0.5f, 1f);
        pictureRect.anchoredPosition = Vector2.zero;
        pictureRect.sizeDelta = new Vector2(WaitPictureSize, WaitPictureSize);
        Image picture = pictureObject.GetComponent<Image>();
        picture.preserveAspect = true;
        picture.raycastTarget = false;
        CharacterClass character = _production.GetCharacter(patient.CharacterId);
        picture.sprite = SpriteFor(character);
        picture.color = new Color(1f, 1f, 1f, SpiritAlpha);
        ControllerProduction.EnsureOwnedAttributeGlow(pictureObject.transform, character);

        Text caption = CreateText(cell.transform, "TextHospitalWait", UiNumber.Format(patient.OpaquePixels) + " px\n" + HospitalMath.FormatSeconds(patient.OpaquePixels * 2.0), 18, TextAnchor.UpperCenter);
        RectTransform captionRect = caption.rectTransform;
        captionRect.anchorMin = new Vector2(0.5f, 1f);
        captionRect.anchorMax = new Vector2(0.5f, 1f);
        captionRect.pivot = new Vector2(0.5f, 1f);
        captionRect.anchoredPosition = new Vector2(0f, -(WaitPictureSize + WaitCaptionGap));
        captionRect.sizeDelta = new Vector2(150f, WaitCaptionHeight);
        caption.lineSpacing = 0.9f;
        caption.horizontalOverflow = HorizontalWrapMode.Overflow;
        caption.verticalOverflow = VerticalWrapMode.Overflow;

        WaitView view = new WaitView();
        view.PatientId = patient.Id;
        view.Picture = picture;
        view.Pixels = caption;
        view.Time = caption;
        return view;
    }

    void PushCare(int bed)
    {
        if (_production == null || bed < 1)
            return;
        _production.CareHospitalBed(bed);
        Refresh();
    }

    Button CreateCareButton(Transform parent, int bed, float x)
    {
        Button button = CreateButton(parent, "ButtonHospitalCare" + bed.ToString("00"), "SpeedUp", new Vector2(x, 8f), new Vector2(194f, 194f), false);
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.fontSize = 32;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 32;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }
        RgbHoldRepeat hold = button.gameObject.AddComponent<RgbHoldRepeat>();
        hold.Target = button;
        int captured = bed;
        button.onClick.AddListener(() => PushCare(captured));
        return button;
    }

    void Build(GameObject sizeSource)
    {
        if (Panel != null || sizeSource == null)
            return;
        Panel = new GameObject("PanelHospital", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        Panel.transform.SetParent(sizeSource.transform.parent, false);
        RectTransform panelRect = Panel.GetComponent<RectTransform>();
        RectTransform sourceRect = sizeSource.GetComponent<RectTransform>();
        panelRect.anchorMin = sourceRect.anchorMin;
        panelRect.anchorMax = sourceRect.anchorMax;
        panelRect.pivot = sourceRect.pivot;
        panelRect.anchoredPosition = sourceRect.anchoredPosition;
        panelRect.sizeDelta = sourceRect.sizeDelta;
        panelRect.localScale = Vector3.one;
        Panel.GetComponent<Image>().color = Color.black;
        Panel.GetComponent<Image>().raycastTarget = true;
        CanvasGroup group = Panel.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject cardScroll = new GameObject("HospitalCardScroll", typeof(RectTransform), typeof(ScrollRect));
        cardScroll.transform.SetParent(Panel.transform, false);
        _cardScrollRect = cardScroll.GetComponent<RectTransform>();
        PlaceRect(_cardScrollRect, 0f, -156f, 1040f, 1016f);
        RectTransform cardViewport = CreateViewport(cardScroll.transform);
        GameObject cardContent = new GameObject("Content", typeof(RectTransform));
        cardContent.transform.SetParent(cardViewport, false);
        _cardRoot = cardContent.GetComponent<RectTransform>();
        _cardRoot.anchorMin = new Vector2(0f, 1f);
        _cardRoot.anchorMax = new Vector2(1f, 1f);
        _cardRoot.pivot = new Vector2(0.5f, 1f);
        _cardRoot.anchoredPosition = Vector2.zero;
        WireScroll(cardScroll.GetComponent<ScrollRect>(), cardViewport, _cardRoot);

        GameObject waitScroll = new GameObject("HospitalWaitScroll", typeof(RectTransform), typeof(ScrollRect));
        waitScroll.transform.SetParent(Panel.transform, false);
        _waitScrollRect = waitScroll.GetComponent<RectTransform>();
        PlaceRect(_waitScrollRect, 0f, 492f, 1040f, WaitListHeight);
        RectTransform waitViewport = CreateViewport(waitScroll.transform);
        GameObject waitContent = new GameObject("Content", typeof(RectTransform));
        waitContent.transform.SetParent(waitViewport, false);
        _waitContent = waitContent.GetComponent<RectTransform>();
        _waitContent.anchorMin = new Vector2(0f, 0f);
        _waitContent.anchorMax = new Vector2(0f, 1f);
        _waitContent.pivot = new Vector2(0f, 0.5f);
        _waitContent.anchoredPosition = Vector2.zero;
        _waitContent.sizeDelta = new Vector2(0f, 0f);
        HorizontalLayoutGroup layout = waitContent.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        int edge = Mathf.RoundToInt(WaitEdge);
        int side = Mathf.RoundToInt(WaitEdge - (WaitCellWidth - WaitPictureSize) * 0.5f);
        layout.padding = new RectOffset(side, side, edge, edge);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.reverseArrangement = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = waitContent.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        ScrollRect waitList = waitScroll.GetComponent<ScrollRect>();
        _waitList = waitList;
        WireScroll(waitList, waitViewport, _waitContent);
        waitList.horizontal = true;
        waitList.vertical = false;
        EnsureWhiteFrame(waitScroll.transform, 3f);
        _waitEmpty = CreateText(waitScroll.transform, "TextHospitalWaitEmpty", "待機していません", 32, TextAnchor.MiddleCenter);
        Stretch(_waitEmpty.rectTransform);
        PlaceRegions();
    }

    // キャラリストはパネル上端へ寄せ、カードリストはその下からメニューの直前まで伸ばす。
    void PlaceRegions()
    {
        if (Panel == null || _waitScrollRect == null || _cardScrollRect == null)
            return;
        const float margin = 16f;
        const float gap = 16f;
        RectTransform panelRect = Panel.GetComponent<RectTransform>();
        Canvas.ForceUpdateCanvases();
        float panelHeight = panelRect.rect.height;
        if (panelHeight < WaitListHeight + 320f)
            return;

        float top = panelHeight * 0.5f - margin;
        float bottom = -panelHeight * 0.5f + margin;
        GameObject bar = GameObject.Find("PanelSelectScene");
        RectTransform barRect = bar != null ? bar.GetComponent<RectTransform>() : null;
        if (barRect != null && barRect.rect.height > 8f)
        {
            Vector3[] corners = new Vector3[4];
            barRect.GetWorldCorners(corners);
            float aboveBar = panelRect.InverseTransformPoint(corners[1]).y - margin;
            if (aboveBar > bottom && aboveBar < top - WaitListHeight - gap - 300f)
                bottom = aboveBar;
        }

        float waitTop = top;
        float waitBottom = waitTop - WaitListHeight;
        float cardTop = waitBottom - gap;
        float cardHeight = cardTop - bottom;
        PlaceRect(_waitScrollRect, 0f, (waitTop + waitBottom) * 0.5f, 1040f, WaitListHeight);
        PlaceRect(_cardScrollRect, 0f, (cardTop + bottom) * 0.5f, 1040f, cardHeight);
    }

    RectTransform CreateViewport(Transform parent)
    {
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(parent, false);
        Stretch(viewport.GetComponent<RectTransform>());
        Image image = viewport.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        return viewport.GetComponent<RectTransform>();
    }

    static void WireScroll(ScrollRect scroll, RectTransform viewport, RectTransform content)
    {
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
    }

    Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        return CreateButton(parent, name, label, position, size, true);
    }

    Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, bool frame)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = Color.black;
        image.sprite = _white;
        image.type = Image.Type.Simple;
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.black;
        colors.highlightedColor = new Color(0.16f, 0.16f, 0.18f, 1f);
        colors.pressedColor = new Color(0.28f, 0.28f, 0.3f, 1f);
        colors.selectedColor = Color.black;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        if (size.x > 0f)
            PlaceRect(obj.GetComponent<RectTransform>(), position.x, position.y, size.x, size.y);
        Text text = CreateText(obj.transform, "Text", label, 32, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        if (frame)
            EnsureWhiteFrame(obj.transform, 3f);
        return button;
    }

    Text CreateText(Transform parent, string name, string body, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = body;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    Sprite SpriteFor(CharacterClass character)
    {
        if (character == null || character.ImageTexture2D == null)
            return null;
        character.ImageTexture2D.filterMode = FilterMode.Point;
        int texId = character.ImageTexture2D.GetInstanceID();
        Sprite existing;
        int cached;
        if (_spriteTex.TryGetValue(character.ID, out cached) && cached == texId && _sprites.TryGetValue(character.ID, out existing) && existing != null)
            return existing;
        if (_sprites.TryGetValue(character.ID, out existing) && existing != null)
        {
            ControllerProduction.DestroyRuntime(existing);
            _sprites.Remove(character.ID);
        }
        Sprite sprite = Sprite.Create(
            character.ImageTexture2D,
            new Rect(0f, 0f, character.ImageTexture2D.width, character.ImageTexture2D.height),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        ControllerProduction.MarkRuntime(sprite);
        _sprites[character.ID] = sprite;
        _spriteTex[character.ID] = texId;
        return sprite;
    }

    static void EnsureSprites()
    {
        if (_white == null)
        {
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Point;
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            _white = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
            _white.hideFlags = HideFlags.HideAndDontSave;
        }
        if (_aura != null)
            return;
        const int size = 64;
        Texture2D aura = new Texture2D(size, size, TextureFormat.RGBA32, false);
        aura.hideFlags = HideFlags.HideAndDontSave;
        aura.filterMode = FilterMode.Bilinear;
        aura.wrapMode = TextureWrapMode.Clamp;
        Color[] auraPixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f;
                float dy = (y + 0.5f) / size - 0.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float alpha = 1f - distance;
                if (alpha < 0f)
                    alpha = 0f;
                alpha = alpha * alpha;
                auraPixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        aura.SetPixels(auraPixels);
        aura.Apply();
        _aura = Sprite.Create(aura, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        _aura.hideFlags = HideFlags.HideAndDontSave;
    }

    static void EnsureWhiteFrame(Transform target, float thickness)
    {
        if (target == null || target.Find("WhiteFrame") != null)
            return;
        GameObject root = new GameObject("WhiteFrame", typeof(RectTransform));
        root.transform.SetParent(target, false);
        Stretch(root.GetComponent<RectTransform>());
        AddEdge(root.transform, true, true, thickness);
        AddEdge(root.transform, true, false, thickness);
        AddEdge(root.transform, false, false, thickness);
        AddEdge(root.transform, false, true, thickness);
        root.transform.SetAsLastSibling();
    }

    static void AddEdge(Transform parent, bool horizontal, bool end, float thickness)
    {
        GameObject edgeObject = new GameObject(horizontal ? (end ? "Top" : "Bottom") : (end ? "Right" : "Left"), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        edgeObject.transform.SetParent(parent, false);
        Image image = edgeObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        if (_white != null)
            image.sprite = _white;
        RectTransform rect = edgeObject.GetComponent<RectTransform>();
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, end ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, end ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, end ? 1f : 0f);
            rect.sizeDelta = new Vector2(0f, thickness);
        }
        else
        {
            rect.anchorMin = new Vector2(end ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(end ? 1f : 0f, 1f);
            rect.pivot = new Vector2(end ? 1f : 0f, 0.5f);
            rect.sizeDelta = new Vector2(thickness, 0f);
        }
        rect.anchoredPosition = Vector2.zero;
    }

    static void PlaceRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

public class HospitalBreath : MonoBehaviour
{
    public float Phase;

    void Update()
    {
        float wave = Mathf.Sin((Time.unscaledTime + Phase) * (Mathf.PI * 2f / 3.2f));
        float scale = 1f + 0.06f * wave;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}

public class HospitalAura : MonoBehaviour
{
    Image _image;
    bool _visible;
    Color _color = Color.white;

    public void Bind(Image image)
    {
        _image = image;
    }

    public void Show(Color color)
    {
        _visible = true;
        _color = color;
        if (_image != null)
            _image.enabled = true;
    }

    public void Hide()
    {
        _visible = false;
        if (_image != null)
            _image.enabled = false;
    }

    void Update()
    {
        if (_image == null || !_visible)
            return;
        float wave = Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / 2.4f)) * 0.5f + 0.5f;
        Color color = _color;
        color.a = Mathf.Lerp(0.18f, 0.5f, wave);
        _image.color = color;
        float scale = Mathf.Lerp(1.08f, 1.2f, wave);
        _image.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }
}
