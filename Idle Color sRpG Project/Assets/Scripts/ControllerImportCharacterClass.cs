using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// PNG から自作キャラを読み込む画面。
public class ControllerImportCharacterClass : MonoBehaviour
{
    ControllerProduction _production;
    Font _font;
    Text _count;
    Text _capNote;
    Text _sizes;
    Text _message;
    InputField _name;
    bool _clampingName;
    GameObject _confirm;
    Image _confirmImage;
    Text _confirmPrompt;
    Text _confirmMessage;
    Text _costIntro;
    Text _costR;
    Text _costG;
    Text _costB;
    Text _costError;
    Button _confirmButton;
    Button _pickButton;
    string _importStatus;
    GameObject _deleteConfirm;
    Text _deletePrompt;
    uint _deleteId;
    Sprite _confirmSprite;
    RectTransform _content;
    bool _picking;
    bool _showingRebirth;
    bool _retouchPayDone;
    uint _retouchPickId;
    Coroutine _readPick;
    ImportRebirthPlayer _rebirth;
    readonly List<GameObject> _cells = new List<GameObject>();

    GameObject _retouchPay;
    Text _retouchPayPrompt;
    Text _retouchPayDetail;
    Button _retouchAdButton;
    Button _retouchTicketButton;
    Text _retouchTicketLabel;
    GameObject _retouchConfirm;
    Image _retouchOldImage;
    Image _retouchNewImage;
    Sprite _retouchOldSprite;
    Sprite _retouchNewSprite;
    Text _retouchCostIntro;
    Text _retouchCostR;
    Text _retouchCostG;
    Text _retouchCostB;
    Text _retouchCostError;
    Text _retouchConfirmMessage;
    Button _retouchConfirmButton;
    string _retouchCostStatus;

    GameObject _attributePanel;
    Text _attributePrompt;
    Text _attributeMessage;
    Button _attributeAdButton;
    Button _attributeTicketButton;
    Text _attributeTicketLabel;
    uint _attributeId;
    CharacterType _attributeSelected;
    readonly Button[] _attributeTypeButtons = new Button[5];
    readonly CharacterType[] _attributeTypes =
    {
        CharacterType.Fire,
        CharacterType.Grass,
        CharacterType.Water,
        CharacterType.Dark,
        CharacterType.Light,
    };
    static Sprite _attributeGradientSprite;

    const float FrameThin = 3f;
    static readonly Color DeleteFill = new Color(0.78f, 0.18f, 0.16f, 1f);
    static readonly Color RetouchFill = new Color(0.14f, 0.68f, 0.28f, 1f);
    static readonly Color ImportOkFill = new Color(0.12f, 0.72f, 0.22f, 1f);
    static readonly Color ImportCancelFill = new Color(0.86f, 0.16f, 0.12f, 1f);

    public GameObject Panel { get; private set; }

    public void Initialize(ControllerProduction production, GameObject sizeSource)
    {
        _production = production;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        PngFilePicker.Ensure();
        Build(sizeSource);
    }

    public void Open()
    {
        Refresh();
        if (Panel != null)
            GameAudio.BindAllButtons(Panel.transform);
    }

    public void Close()
    {
        if (_rebirth != null)
            _rebirth.Halt();
        _showingRebirth = false;
        HideDelete();
        DiscardPending();
        DiscardRetouch();
        HideAttributeChange();
    }

    public void Refresh()
    {
        if (_count != null && _production != null)
        {
            int count = _production.ImportedCharacterCount();
            int cap = ImportedCharacters.ImportCap;
            bool full = count >= cap;
            Color ink = full ? new Color(1f, 0.38f, 0.34f, 1f) : Color.white;
            _count.text = "読み込み " + UiNumber.Format(count) + " / " + ImportedCharacters.CapLabel();
            _count.color = ink;
            if (_capNote != null)
                _capNote.color = ink;
        }
        if (_sizes != null && _production != null)
            _sizes.text = ImportSizeRules.Describe(_production.IsBattleStageCleared);
        PaintPickButton();
        RebuildCells();
        if (Panel != null)
            GameAudio.BindAllButtons(Panel.transform);
    }

    public void PushPick()
    {
        if (_picking || _showingRebirth || _production == null)
            return;
        if (_confirm != null && _confirm.activeSelf)
            return;
        if (_deleteConfirm != null && _deleteConfirm.activeSelf)
            return;
        if (ImportExtraBusy())
            return;
        string capMessage;
        if (ImportedCharacters.RejectImportCount(out capMessage))
        {
            SetMessage(capMessage, false);
            return;
        }

        _retouchPickId = 0;
        _picking = true;
        PngFilePicker.Pick(OnPicked);
    }

    public void PushConfirmName()
    {
        if (_production == null || _showingRebirth)
            return;
        string name = _name != null ? _name.text : "";
        CharacterClass character = _production.PreparedImportCharacter();
        string message;
        bool added = _production.CommitPreparedImport(name, out message);
        if (!added)
        {
            SetConfirmMessage(message, false);
            return;
        }
        HideConfirm();
        if (_rebirth == null || character == null)
        {
            SetMessage(message, true);
            Refresh();
            return;
        }
        _showingRebirth = true;
        _rebirth.Play(character, message);
    }

    void OnRebirthClosed(string message)
    {
        _showingRebirth = false;
        SetMessage(message, true);
        Refresh();
    }

    public void PushCancelImport()
    {
        DiscardPending();
        SetMessage("読み込みをやめました。", false);
    }

    void OnPicked(string path)
    {
        _picking = false;
        if (_readPick != null)
            StopCoroutine(_readPick);
        _readPick = StartCoroutine(ReadPicked(path));
    }

    System.Collections.IEnumerator ReadPicked(string path)
    {
        if (_production == null)
            yield break;
        if (path == null)
        {
            _retouchPickId = 0;
            SetMessage("この環境ではファイルを選べません。", false);
            yield break;
        }
        if (string.IsNullOrEmpty(path))
        {
            _retouchPickId = 0;
            SetMessage("ファイルを選びませんでした。", false);
            yield break;
        }

        byte[] bytes = null;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                if (File.Exists(path))
                    bytes = File.ReadAllBytes(path);
            }
            catch (System.Exception)
            {
                bytes = null;
            }
            if (bytes != null && bytes.Length >= 8)
                break;
            bytes = null;
            yield return new WaitForSecondsRealtime(0.08f);
        }

        if (bytes == null)
        {
            _retouchPickId = 0;
            SetMessage("ファイルを読めませんでした。", false);
            yield break;
        }

        if (_retouchPickId != 0)
        {
            uint targetId = _retouchPickId;
            _retouchPickId = 0;
            string retouchMessage;
            bool retouchPrepared = _production.TryPrepareRetouch(targetId, bytes, out retouchMessage);
            if (!retouchPrepared)
            {
                SetMessage(retouchMessage, false);
                yield break;
            }
            _retouchPayDone = !_production.PreparedRetouchRequiresPay();
            if (_retouchPayDone)
                ShowRetouchConfirm();
            else
                ShowRetouchPay();
            yield break;
        }

        string message;
        bool prepared = _production.TryPrepareImport(bytes, out message);
        if (!prepared)
        {
            SetMessage(message, false);
            yield break;
        }

        ShowConfirm(NameFromFile(path));
    }

    void ShowConfirm(string fileName)
    {
        CharacterClass character = _production != null ? _production.PreparedImportCharacter() : null;
        if (character == null || _confirm == null)
        {
            DiscardPending();
            SetMessage("画像をキャラにできませんでした。", false);
            return;
        }

        if (_name != null)
            _name.text = fileName ?? "";
        if (_confirmImage != null)
        {
            if (_confirmSprite != null)
                Destroy(_confirmSprite);
            _confirmSprite = SpriteOf(character);
            _confirmImage.sprite = _confirmSprite;
            _confirmImage.color = Color.white;
        }
        _importStatus = null;
        _confirm.SetActive(true);
        PaintImportCost();
    }

    void Update()
    {
        if (_showingRebirth)
            return;
        if (_confirm != null && _confirm.activeSelf)
            PaintImportCost();
        if (_retouchConfirm != null && _retouchConfirm.activeSelf)
            PaintRetouchCost();
    }

    void PaintImportCost()
    {
        CharacterClass character = _production != null ? _production.PreparedImportCharacter() : null;
        bool affordable = false;
        ulong red = 0;
        ulong green = 0;
        ulong blue = 0;
        bool priced = _production != null && _production.TryImportRgbCost(character, out red, out green, out blue, out affordable);
        if (!priced)
            affordable = false;
        string shortage = priced ? _production.ImportRgbShortageText() : "このキャラのRGBを計算できません。";
        string status = (affordable ? "1" : "0") + "\n" + red.ToString() + "\n" + green.ToString() + "\n" + blue.ToString() + "\n" + shortage;
        if (status == _importStatus)
            return;
        _importStatus = status;
        ShowImportCost(priced, red, green, blue);
        PaintConfirmAfford(affordable);
        string shown = _confirmMessage != null ? _confirmMessage.text : "";
        bool statusMessage = shown.Length == 0
            || shown.StartsWith("RGBが足りません")
            || shown.StartsWith("このキャラのRGB");
        if (!statusMessage)
            return;
        if (affordable)
            SetConfirmMessage("", true);
        else
            SetConfirmMessage(shortage, false);
    }

    void BuildImportCostRow(Transform parent)
    {
        GameObject row = new GameObject("ImportCostRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        Place(row.GetComponent<RectTransform>(), 0f, -104f, 960f, 72f);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 18f;
        layout.padding = new RectOffset(12, 12, 10, 10);

        _costIntro = CreateCostText(row.transform, "消費", 36);
        _costR = CreateCostText(row.transform, "R", 40);
        _costG = CreateCostText(row.transform, "G", 40);
        _costB = CreateCostText(row.transform, "B", 40);
        ControllerProduction.PaintChannelText(_costR, 0);
        ControllerProduction.PaintChannelText(_costG, 1);
        ControllerProduction.PaintChannelText(_costB, 2);
        _costError = CreateCostText(row.transform, "", 32);
        _costError.gameObject.SetActive(false);
    }

    Text CreateCostText(Transform parent, string text, int fontSize)
    {
        Text label = CreateText("Text", parent, fontSize, TextAnchor.MiddleCenter);
        label.text = text;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    void ShowImportCost(bool priced, ulong red, ulong green, ulong blue)
    {
        if (_costIntro != null)
            _costIntro.gameObject.SetActive(priced);
        if (_costR != null)
        {
            _costR.gameObject.SetActive(priced);
            if (priced)
                _costR.text = "R " + UiNumber.Format(red);
        }
        if (_costG != null)
        {
            _costG.gameObject.SetActive(priced);
            if (priced)
                _costG.text = "G " + UiNumber.Format(green);
        }
        if (_costB != null)
        {
            _costB.gameObject.SetActive(priced);
            if (priced)
                _costB.text = "B " + UiNumber.Format(blue);
        }
        if (_costError != null)
        {
            _costError.gameObject.SetActive(!priced);
            if (!priced)
                _costError.text = "このキャラのRGBを計算できません。";
        }
    }

    void HideConfirm()
    {
        _importStatus = null;
        if (_confirm != null)
            _confirm.SetActive(false);
        if (_confirmImage != null)
            _confirmImage.sprite = null;
        if (_confirmSprite != null)
        {
            Destroy(_confirmSprite);
            _confirmSprite = null;
        }
    }

    void DiscardPending()
    {
        HideConfirm();
        if (_production != null)
            _production.DiscardPreparedImport();
    }

    static string NameFromFile(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path ?? "");
        if (string.IsNullOrWhiteSpace(name))
            return "";
        name = name.Trim().Replace("\r", "").Replace("\n", "");
        return ImportNameRules.Clamp(name);
    }

    void SetMessage(string message, bool ok)
    {
        PaintMessage(_message, message, ok);
    }

    void SetConfirmMessage(string message, bool ok)
    {
        PaintMessage(_confirmMessage, message, ok);
    }

    static void PaintMessage(Text label, string message, bool ok)
    {
        if (label == null)
            return;
        label.text = message ?? "";
        label.color = ok ? new Color(0.45f, 0.95f, 0.55f, 1f) : new Color(1f, 0.38f, 0.34f, 1f);
    }

    void RebuildCells()
    {
        if (_content == null || _production == null)
            return;
        for (int i = 0; i < _cells.Count; i++)
        {
            if (_cells[i] != null)
                Destroy(_cells[i]);
        }
        _cells.Clear();
        if (_deleteConfirm != null && _deleteConfirm.activeSelf && _production != null && ImportedCharacters.Find(_deleteId) == null)
            HideDelete();
        _production.ForEachImportedCharacter(character =>
        {
            if (character == null)
                return;
            _cells.Add(CreateCell(character));
        });
    }

    GameObject CreateCell(CharacterClass character)
    {
        GameObject cell = new GameObject("ImportCell" + character.ID, typeof(RectTransform), typeof(Image));
        cell.transform.SetParent(_content, false);
        cell.GetComponent<Image>().color = Color.black;

        GameObject picture = new GameObject("Picture", typeof(RectTransform));
        picture.transform.SetParent(cell.transform, false);
        RectTransform pictureRect = picture.GetComponent<RectTransform>();
        pictureRect.anchorMin = new Vector2(0.15f, 0.58f);
        pictureRect.anchorMax = new Vector2(0.85f, 0.97f);
        pictureRect.offsetMin = Vector2.zero;
        pictureRect.offsetMax = Vector2.zero;
        Image image = picture.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.sprite = SpriteOf(character);

        Text label = CreateText("Label", cell.transform, 17, TextAnchor.UpperCenter);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.48f);
        labelRect.anchorMax = new Vector2(1f, 0.58f);
        labelRect.offsetMin = new Vector2(4f, 0f);
        labelRect.offsetMax = new Vector2(-4f, 0f);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.text = character.Name + "\n" + character.Size.ToString();

        uint id = character.ID;
        string name = string.IsNullOrWhiteSpace(character.Name) ? "ななし" : character.Name.Trim();

        GameObject retouchObject = new GameObject("ButtonRetouch", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        retouchObject.transform.SetParent(cell.transform, false);
        RectTransform retouchRect = retouchObject.GetComponent<RectTransform>();
        retouchRect.anchorMin = new Vector2(0.08f, 0.33f);
        retouchRect.anchorMax = new Vector2(0.92f, 0.46f);
        retouchRect.offsetMin = Vector2.zero;
        retouchRect.offsetMax = Vector2.zero;
        retouchObject.GetComponent<Image>().color = Color.white;
        Button retouch = retouchObject.GetComponent<Button>();
        retouch.targetGraphic = retouchObject.GetComponent<Image>();
        retouch.onClick.AddListener(() => PushRetouch(id));
        GameAudio.BindUi(retouch);
        Text retouchLabel = CreateText("Text", retouchObject.transform, 18, TextAnchor.MiddleCenter);
        Stretch(retouchLabel.rectTransform);
        retouchLabel.text = "手直し";
        retouchLabel.color = Color.white;
        StyleChromeButton(retouch);
        PaintRetouchFill(retouch);

        GameObject attributeObject = new GameObject("ButtonAttributeChange", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        attributeObject.transform.SetParent(cell.transform, false);
        RectTransform attributeRect = attributeObject.GetComponent<RectTransform>();
        attributeRect.anchorMin = new Vector2(0.08f, 0.18f);
        attributeRect.anchorMax = new Vector2(0.92f, 0.31f);
        attributeRect.offsetMin = Vector2.zero;
        attributeRect.offsetMax = Vector2.zero;
        Image attributeImage = attributeObject.GetComponent<Image>();
        attributeImage.sprite = AttributeGradientSprite();
        attributeImage.type = Image.Type.Simple;
        attributeImage.color = Color.white;
        attributeImage.preserveAspect = false;
        Button attribute = attributeObject.GetComponent<Button>();
        attribute.targetGraphic = attributeImage;
        attribute.onClick.AddListener(() => PushAttributeChange(id));
        GameAudio.BindUi(attribute);
        Text attributeLabel = CreateText("Text", attributeObject.transform, 16, TextAnchor.MiddleCenter);
        Stretch(attributeLabel.rectTransform);
        bool attributeUsed = character.AttributeChangeUsed;
        attributeLabel.text = attributeUsed ? "属性済" : "属性チェンジ";
        attributeLabel.color = attributeUsed ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
        PaintAttributeGradientButton(attribute);
        if (attributeUsed)
        {
            attribute.interactable = false;
            attributeImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            ColorBlock usedColors = attribute.colors;
            usedColors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            attribute.colors = usedColors;
        }
        EnsureWhiteFrame(attributeObject.transform, FrameThin);

        GameObject deleteObject = new GameObject("ButtonDelete", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        deleteObject.transform.SetParent(cell.transform, false);
        RectTransform deleteRect = deleteObject.GetComponent<RectTransform>();
        deleteRect.anchorMin = new Vector2(0.08f, 0.03f);
        deleteRect.anchorMax = new Vector2(0.92f, 0.16f);
        deleteRect.offsetMin = Vector2.zero;
        deleteRect.offsetMax = Vector2.zero;
        deleteObject.GetComponent<Image>().color = Color.white;
        Button delete = deleteObject.GetComponent<Button>();
        delete.targetGraphic = deleteObject.GetComponent<Image>();
        delete.onClick.AddListener(() => AskDelete(id, name));
        GameAudio.BindUi(delete);
        Text deleteLabel = CreateText("Text", deleteObject.transform, 18, TextAnchor.MiddleCenter);
        Stretch(deleteLabel.rectTransform);
        deleteLabel.text = "消す";
        deleteLabel.color = Color.white;
        StyleChromeButton(delete);
        PaintDeleteFill(delete);
        EnsureWhiteFrame(cell.transform, FrameThin);
        return cell;
    }

    void AskDelete(uint id, string name)
    {
        if (_showingRebirth || _picking || _production == null)
            return;
        if (_confirm != null && _confirm.activeSelf)
            return;
        if (ImportExtraBusy())
            return;
        if (ImportedCharacters.Find(id) == null)
            return;
        _deleteId = id;
        if (_deletePrompt != null)
            _deletePrompt.text = name + " を消します。\n所持数や合成も消え、読み込み枠が1つ戻ります。\n生産と編成からも外れます。";
        if (_deleteConfirm != null)
            _deleteConfirm.SetActive(true);
    }

    public void PushDelete()
    {
        if (_production == null || _deleteId == 0)
            return;
        uint id = _deleteId;
        HideDelete();
        string message;
        bool removed = _production.DeleteImportedCharacter(id, out message);
        SetMessage(message, removed);
        Refresh();
    }

    public void PushCancelDelete()
    {
        HideDelete();
    }

    void HideDelete()
    {
        _deleteId = 0;
        if (_deleteConfirm != null)
            _deleteConfirm.SetActive(false);
    }

    static Sprite SpriteOf(CharacterClass character)
    {
        Texture2D tex = character.ImageTexture2D;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");
        tex.filterMode = FilterMode.Point;
        int size = character.Size > 0 ? character.Size : tex.width;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void Build(GameObject sizeSource)
    {
        if (Panel != null || sizeSource == null)
            return;

        Panel = new GameObject("PanelImportCharacter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
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

        Text title = CreateText("TextImportTitle", Panel.transform, 40, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 0f, 640f, 1000f, 56f);
        title.text = "キャラを読み込む";

        _count = CreateText("TextImportCount", Panel.transform, 48, TextAnchor.MiddleCenter);
        Place(_count.rectTransform, 0f, 586f, 1000f, 56f);
        _count.horizontalOverflow = HorizontalWrapMode.Overflow;
        _count.verticalOverflow = VerticalWrapMode.Overflow;

        _capNote = CreateText("TextImportCapNote", Panel.transform, 24, TextAnchor.MiddleCenter);
        Place(_capNote.rectTransform, 0f, 534f, 1000f, 32f);
        _capNote.text = "クエストクリアなどで上限が増えます";
        _capNote.horizontalOverflow = HorizontalWrapMode.Overflow;
        _capNote.verticalOverflow = VerticalWrapMode.Overflow;

        _sizes = CreateText("TextImportSizes", Panel.transform, 22, TextAnchor.UpperLeft);
        Place(_sizes.rectTransform, 0f, 384f, 980f, 160f);
        _sizes.horizontalOverflow = HorizontalWrapMode.Wrap;
        _sizes.verticalOverflow = VerticalWrapMode.Overflow;

        GameObject pickObject = new GameObject("ButtonPickPng", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        pickObject.transform.SetParent(Panel.transform, false);
        Place(pickObject.GetComponent<RectTransform>(), 0f, 238f, 520f, 90f);
        pickObject.GetComponent<Image>().color = Color.white;
        Button pick = pickObject.GetComponent<Button>();
        pick.targetGraphic = pickObject.GetComponent<Image>();
        pick.onClick.AddListener(PushPick);
        GameAudio.BindUi(pick);
        Text pickLabel = CreateText("Text", pickObject.transform, 32, TextAnchor.MiddleCenter);
        Stretch(pickLabel.rectTransform);
        pickLabel.text = "ドット絵を読み込む";
        pickLabel.color = Color.white;
        _pickButton = pick;
        PaintPickButton();

        _message = CreateText("TextImportMessage", Panel.transform, 26, TextAnchor.MiddleCenter);
        Place(_message.rectTransform, 0f, 117f, 980f, 80f);
        _message.horizontalOverflow = HorizontalWrapMode.Wrap;
        _message.verticalOverflow = VerticalWrapMode.Overflow;

        BuildScroll();
        BuildConfirm();
        BuildRebirth();
        BuildDeleteConfirm();
        BuildRetouchPay();
        BuildRetouchConfirm();
        BuildAttributeChange();
    }

    public void PushRetouch(uint id)
    {
        if (_picking || _showingRebirth || _production == null || id == 0)
            return;
        if (_confirm != null && _confirm.activeSelf)
            return;
        if (_deleteConfirm != null && _deleteConfirm.activeSelf)
            return;
        if (ImportExtraBusy())
            return;
        if (ImportedCharacters.Find(id) == null)
            return;
        _retouchPickId = id;
        _picking = true;
        PngFilePicker.Pick(OnPicked);
    }

    public void PushAttributeChange(uint id)
    {
        if (_picking || _showingRebirth || _production == null || id == 0)
            return;
        if (_confirm != null && _confirm.activeSelf)
            return;
        if (_deleteConfirm != null && _deleteConfirm.activeSelf)
            return;
        if (ImportExtraBusy())
            return;
        CharacterClass character = ImportedCharacters.Find(id);
        if (character == null)
            return;
        if (character.AttributeChangeUsed)
        {
            SetMessage("このキャラの属性チェンジはもう使えません。1体につき1回だけです。", false);
            return;
        }
        _attributeId = id;
        _attributeSelected = CharacterType.None;
        string name = string.IsNullOrWhiteSpace(character.Name) ? "ななし" : character.Name.Trim();
        if (_attributePrompt != null)
        {
            _attributePrompt.text = name + " の属性を変えます。いまは "
                + ImportedCharacters.AttributeLabel(character.CharacterType)
                + " です。\n変えたい属性を選んでから、広告視聴か属性チェンジチケットが必要です。";
        }
        SetAttributeMessage("", true);
        PaintAttributeTypeButtons(character.CharacterType);
        PaintAttributePayButtons();
        if (_attributePanel != null)
            _attributePanel.SetActive(true);
    }

    public void PushAttributeType(CharacterType type)
    {
        if (_attributePanel == null || !_attributePanel.activeSelf || _production == null)
            return;
        CharacterClass character = ImportedCharacters.Find(_attributeId);
        if (character == null)
            return;
        if (type == CharacterType.None || type == character.CharacterType)
            return;
        _attributeSelected = type;
        PaintAttributeTypeButtons(character.CharacterType);
        PaintAttributePayButtons();
        SetAttributeMessage(ImportedCharacters.AttributeLabel(type) + " を選びました。", true);
    }

    public void PushAttributeAd()
    {
        if (_production == null || _attributePanel == null || !_attributePanel.activeSelf)
            return;
        if (_attributeSelected == CharacterType.None)
            return;
        _production.WatchAttributeChangeAd(ApplySelectedAttribute);
    }

    public void PushAttributeTicket()
    {
        // チケット消費は後で実装。今は所持0の枠だけ。
    }

    public void PushCancelAttributeChange()
    {
        HideAttributeChange();
        SetMessage("属性チェンジをやめました。", false);
    }

    void ApplySelectedAttribute()
    {
        if (_production == null || _attributeSelected == CharacterType.None || _attributeId == 0)
            return;
        uint id = _attributeId;
        CharacterType type = _attributeSelected;
        string message;
        bool changed = _production.ChangeImportedAttribute(id, type, out message);
        HideAttributeChange();
        SetMessage(message, changed);
        Refresh();
    }

    void HideAttributeChange()
    {
        _attributeId = 0;
        _attributeSelected = CharacterType.None;
        if (_attributePanel != null)
            _attributePanel.SetActive(false);
    }

    bool AttributeBusy()
    {
        return _attributePanel != null && _attributePanel.activeSelf;
    }

    bool ImportExtraBusy()
    {
        return RetouchBusy() || AttributeBusy();
    }

    void PaintAttributeTypeButtons(CharacterType current)
    {
        for (int i = 0; i < _attributeTypeButtons.Length; i++)
        {
            Button button = _attributeTypeButtons[i];
            if (button == null)
                continue;
            CharacterType type = _attributeTypes[i];
            bool selected = type == _attributeSelected;
            bool same = type == current;
            button.interactable = !same;
            Color fill = AttributeButtonColor(type);
            if (same)
                fill = new Color(fill.r * 0.35f, fill.g * 0.35f, fill.b * 0.35f, 1f);
            if (_production != null)
                _production.StyleRepresentativeButton(button, fill, !same);
            if (selected && !same)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = fill;
                colors.highlightedColor = fill;
                colors.selectedColor = fill;
                button.colors = colors;
                EnsureWhiteFrame(button.transform, 5f);
                SetFrameColor(button.transform, Color.white);
            }
            else
            {
                Transform frame = button.transform.Find("WhiteFrame");
                if (frame != null)
                    frame.gameObject.SetActive(false);
            }
            Text label = button.GetComponentInChildren<Text>();
            if (label == null)
                continue;
            label.color = same ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
        }
    }

    void PaintAttributePayButtons()
    {
        bool ready = _attributeSelected != CharacterType.None;
        if (_attributeAdButton != null && _production != null)
        {
            _production.StyleRepresentativeButton(_attributeAdButton, RetouchFill, ready);
            _attributeAdButton.interactable = ready;
            Text adLabel = _attributeAdButton.GetComponentInChildren<Text>();
            if (!ready && adLabel != null)
                adLabel.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        }
        if (_attributeTicketButton == null || _production == null)
            return;
        int count = _production.AttributeChangeTicketCount();
        if (_attributeTicketLabel != null)
            _attributeTicketLabel.text = "チケット (" + UiNumber.Format(count) + ")";
        bool open = ready && count > 0;
        _production.StyleRepresentativeButton(_attributeTicketButton, new Color(0.75f, 0.55f, 0.18f, 1f), open);
        _attributeTicketButton.interactable = open;
        if (_attributeTicketLabel != null && !open)
            _attributeTicketLabel.color = new Color(0.45f, 0.45f, 0.45f, 1f);
    }

    void SetAttributeMessage(string message, bool ok)
    {
        PaintMessage(_attributeMessage, message, ok);
    }

    static Color AttributeButtonColor(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Fire: return new Color(0.9f, 0.22f, 0.16f, 1f);
            case CharacterType.Grass: return new Color(0.18f, 0.7f, 0.28f, 1f);
            case CharacterType.Water: return new Color(0.2f, 0.42f, 0.92f, 1f);
            case CharacterType.Dark: return new Color(0.55f, 0.22f, 0.85f, 1f);
            case CharacterType.Light: return new Color(0.92f, 0.78f, 0.16f, 1f);
            default: return Color.gray;
        }
    }

    void BuildAttributeChange()
    {
        _attributePanel = new GameObject("PanelImportAttributeChange", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _attributePanel.transform.SetParent(Panel.transform, false);
        Stretch(_attributePanel.GetComponent<RectTransform>());
        _attributePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        _attributePanel.GetComponent<Image>().raycastTarget = true;

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(_attributePanel.transform, false);
        Place(card.GetComponent<RectTransform>(), 0f, 40f, 960f, 740f);
        card.GetComponent<Image>().color = Color.black;
        EnsureWhiteFrame(card.transform, FrameThin);

        Text title = CreateText("TextAttributeTitle", card.transform, 34, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 0f, 310f, 900f, 50f);
        title.text = "属性チェンジ";

        _attributePrompt = CreateText("TextAttributePrompt", card.transform, 24, TextAnchor.MiddleCenter);
        Place(_attributePrompt.rectTransform, 0f, 190f, 900f, 150f);
        _attributePrompt.horizontalOverflow = HorizontalWrapMode.Wrap;
        _attributePrompt.verticalOverflow = VerticalWrapMode.Overflow;

        Text notice = CreateText("TextAttributeNotice", card.transform, 22, TextAnchor.MiddleCenter);
        Place(notice.rectTransform, 0f, 95f, 900f, 50f);
        notice.color = new Color(1f, 0.22f, 0.22f, 1f);
        notice.fontStyle = FontStyle.Bold;
        notice.horizontalOverflow = HorizontalWrapMode.Wrap;
        notice.text = "※ 読み込みキャラ1体につき、属性チェンジは1回だけです。";

        GameObject row = new GameObject("AttributeTypeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(card.transform, false);
        Place(row.GetComponent<RectTransform>(), 0f, 20f, 900f, 100f);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.spacing = 12f;
        layout.padding = new RectOffset(8, 8, 8, 8);

        string[] labels = { "火", "草", "水", "闇", "光" };
        for (int i = 0; i < _attributeTypes.Length; i++)
        {
            CharacterType type = _attributeTypes[i];
            GameObject buttonObject = new GameObject("ButtonAttribute" + type, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(row.transform, false);
            buttonObject.GetComponent<LayoutElement>().preferredWidth = 150f;
            buttonObject.GetComponent<LayoutElement>().preferredHeight = 84f;
            buttonObject.GetComponent<Image>().color = Color.white;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            CharacterType captured = type;
            button.onClick.AddListener(() => PushAttributeType(captured));
            Text label = CreateText("Text", buttonObject.transform, 30, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.text = labels[i];
            label.color = Color.white;
            _attributeTypeButtons[i] = button;
        }

        GameObject adObject = new GameObject("ButtonAttributeAd", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        adObject.transform.SetParent(card.transform, false);
        Place(adObject.GetComponent<RectTransform>(), -200f, -140f, 340f, 84f);
        adObject.GetComponent<Image>().color = Color.white;
        _attributeAdButton = adObject.GetComponent<Button>();
        _attributeAdButton.targetGraphic = adObject.GetComponent<Image>();
        _attributeAdButton.onClick.AddListener(PushAttributeAd);
        Text adLabel = CreateText("Text", adObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(adLabel.rectTransform);
        adLabel.text = "広告を見る";
        adLabel.color = Color.white;

        GameObject ticketObject = new GameObject("ButtonAttributeTicket", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        ticketObject.transform.SetParent(card.transform, false);
        Place(ticketObject.GetComponent<RectTransform>(), 200f, -140f, 340f, 84f);
        ticketObject.GetComponent<Image>().color = Color.white;
        _attributeTicketButton = ticketObject.GetComponent<Button>();
        _attributeTicketButton.targetGraphic = ticketObject.GetComponent<Image>();
        _attributeTicketButton.onClick.AddListener(PushAttributeTicket);
        _attributeTicketLabel = CreateText("Text", ticketObject.transform, 24, TextAnchor.MiddleCenter);
        Stretch(_attributeTicketLabel.rectTransform);
        _attributeTicketLabel.text = "チケット (0)";
        _attributeTicketLabel.color = Color.white;

        GameObject cancelObject = new GameObject("ButtonAttributeCancel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cancelObject.transform.SetParent(card.transform, false);
        Place(cancelObject.GetComponent<RectTransform>(), 0f, -250f, 280f, 76f);
        cancelObject.GetComponent<Image>().color = Color.white;
        Button cancel = cancelObject.GetComponent<Button>();
        cancel.targetGraphic = cancelObject.GetComponent<Image>();
        cancel.onClick.AddListener(PushCancelAttributeChange);
        Text cancelLabel = CreateText("Text", cancelObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(cancelLabel.rectTransform);
        cancelLabel.text = "やめる";
        cancelLabel.color = Color.white;
        StyleChromeButton(cancel);

        _attributeMessage = CreateText("TextAttributeMessage", card.transform, 24, TextAnchor.MiddleCenter);
        Place(_attributeMessage.rectTransform, 0f, -320f, 900f, 50f);
        _attributeMessage.horizontalOverflow = HorizontalWrapMode.Wrap;

        _attributePanel.SetActive(false);
    }

    public void PushRetouchAd()
    {
        if (_production == null || !_retouchPay || !_retouchPay.activeSelf)
            return;
        _production.WatchRetouchUnlockAd(OnRetouchPayUnlocked);
    }

    public void PushRetouchTicket()
    {
        // チケット消費は後で実装。今は所持0の枠だけ。
    }

    public void PushCancelRetouchPay()
    {
        DiscardRetouch();
        SetMessage("手直しをやめました。", false);
    }

    public void PushConfirmRetouch()
    {
        if (_production == null || !_retouchPayDone)
            return;
        string message;
        bool replaced = _production.CommitPreparedRetouch(out message);
        if (!replaced)
        {
            if (_production.PreparedRetouchCharacter() == null)
            {
                DiscardRetouch();
                SetMessage(message, false);
                return;
            }
            SetRetouchConfirmMessage(message, false);
            return;
        }
        HideRetouchConfirm();
        DiscardRetouch();
        SetMessage(message, true);
        Refresh();
    }

    public void PushCancelRetouchConfirm()
    {
        DiscardRetouch();
        SetMessage("手直しをやめました。", false);
    }

    void OnRetouchPayUnlocked()
    {
        _retouchPayDone = true;
        HideRetouchPay();
        ShowRetouchConfirm();
    }

    bool RetouchBusy()
    {
        if (_retouchPay != null && _retouchPay.activeSelf)
            return true;
        if (_retouchConfirm != null && _retouchConfirm.activeSelf)
            return true;
        return _production != null && _production.PreparedRetouchCharacter() != null;
    }

    void DiscardRetouch()
    {
        _retouchPayDone = false;
        _retouchPickId = 0;
        _retouchCostStatus = null;
        HideRetouchPay();
        HideRetouchConfirm();
        if (_production != null)
            _production.DiscardPreparedRetouch();
    }

    void ShowRetouchPay()
    {
        if (_retouchPay == null || _production == null)
            return;
        CharacterClass target = _production.RetouchTargetCharacter();
        ImportRetouchDiff diff = _production.PreparedRetouchDiff();
        string name = target != null && !string.IsNullOrWhiteSpace(target.Name) ? target.Name.Trim() : "ななし";
        if (_retouchPayPrompt != null)
            _retouchPayPrompt.text = name + " の手直しには、\n広告視聴か手直しチケットが必要です。";
        if (_retouchPayDetail != null)
            _retouchPayDetail.text = RetouchDiffText(diff);
        PaintRetouchTicketButton();
        _retouchPay.SetActive(true);
    }

    void HideRetouchPay()
    {
        if (_retouchPay != null)
            _retouchPay.SetActive(false);
    }

    void ShowRetouchConfirm()
    {
        if (_retouchConfirm == null || _production == null)
            return;
        CharacterClass target = _production.RetouchTargetCharacter();
        CharacterClass preview = _production.PreparedRetouchCharacter();
        if (target == null || preview == null)
        {
            DiscardRetouch();
            SetMessage("画像をキャラにできませんでした。", false);
            return;
        }
        if (_retouchOldSprite != null)
            Destroy(_retouchOldSprite);
        if (_retouchNewSprite != null)
            Destroy(_retouchNewSprite);
        _retouchOldSprite = SpriteOf(target);
        _retouchNewSprite = SpriteOf(preview);
        if (_retouchOldImage != null)
        {
            _retouchOldImage.sprite = _retouchOldSprite;
            _retouchOldImage.color = Color.white;
        }
        if (_retouchNewImage != null)
        {
            _retouchNewImage.sprite = _retouchNewSprite;
            _retouchNewImage.color = Color.white;
        }
        _retouchCostStatus = null;
        SetRetouchConfirmMessage("", true);
        _retouchConfirm.SetActive(true);
        PaintRetouchCost();
    }

    void HideRetouchConfirm()
    {
        _retouchCostStatus = null;
        if (_retouchConfirm != null)
            _retouchConfirm.SetActive(false);
        if (_retouchOldImage != null)
            _retouchOldImage.sprite = null;
        if (_retouchNewImage != null)
            _retouchNewImage.sprite = null;
        if (_retouchOldSprite != null)
        {
            Destroy(_retouchOldSprite);
            _retouchOldSprite = null;
        }
        if (_retouchNewSprite != null)
        {
            Destroy(_retouchNewSprite);
            _retouchNewSprite = null;
        }
    }

    void PaintRetouchCost()
    {
        CharacterClass preview = _production != null ? _production.PreparedRetouchCharacter() : null;
        bool affordable = false;
        ulong red = 0;
        ulong green = 0;
        ulong blue = 0;
        bool priced = _production != null && _production.TryImportRgbCost(preview, out red, out green, out blue, out affordable);
        if (!priced)
            affordable = false;
        string shortage = priced ? _production.ImportRgbShortageText() : "このキャラのRGBを計算できません。";
        string status = (affordable ? "1" : "0") + "\n" + red.ToString() + "\n" + green.ToString() + "\n" + blue.ToString() + "\n" + shortage;
        if (status == _retouchCostStatus)
            return;
        _retouchCostStatus = status;
        ShowRetouchCost(priced, red, green, blue);
        PaintRetouchConfirmAfford(affordable);
        string shown = _retouchConfirmMessage != null ? _retouchConfirmMessage.text : "";
        bool statusMessage = shown.Length == 0
            || shown.StartsWith("RGBが足りません")
            || shown.StartsWith("このキャラのRGB");
        if (!statusMessage)
            return;
        if (affordable)
            SetRetouchConfirmMessage("", true);
        else
            SetRetouchConfirmMessage(shortage, false);
    }

    void ShowRetouchCost(bool priced, ulong red, ulong green, ulong blue)
    {
        if (_retouchCostIntro != null)
            _retouchCostIntro.gameObject.SetActive(priced);
        if (_retouchCostR != null)
        {
            _retouchCostR.gameObject.SetActive(priced);
            if (priced)
                _retouchCostR.text = "R " + UiNumber.Format(red);
        }
        if (_retouchCostG != null)
        {
            _retouchCostG.gameObject.SetActive(priced);
            if (priced)
                _retouchCostG.text = "G " + UiNumber.Format(green);
        }
        if (_retouchCostB != null)
        {
            _retouchCostB.gameObject.SetActive(priced);
            if (priced)
                _retouchCostB.text = "B " + UiNumber.Format(blue);
        }
        if (_retouchCostError != null)
        {
            _retouchCostError.gameObject.SetActive(!priced);
            if (!priced)
                _retouchCostError.text = "このキャラのRGBを計算できません。";
        }
    }

    void PaintRetouchConfirmAfford(bool affordable)
    {
        if (_retouchConfirmButton == null || _production == null)
            return;
        bool open = affordable && _retouchPayDone;
        _production.StyleRepresentativeButton(_retouchConfirmButton, ImportOkFill, open);
        _retouchConfirmButton.interactable = open;
        if (open)
            return;
        Text label = _retouchConfirmButton.GetComponentInChildren<Text>();
        if (label != null)
            label.color = new Color(0.45f, 0.45f, 0.45f, 1f);
    }

    void PaintRetouchTicketButton()
    {
        if (_retouchTicketButton == null || _production == null)
            return;
        int count = _production.RetouchTicketCount();
        if (_retouchTicketLabel != null)
            _retouchTicketLabel.text = "チケット (" + UiNumber.Format(count) + ")";
        bool open = count > 0;
        _production.StyleRepresentativeButton(_retouchTicketButton, new Color(0.75f, 0.55f, 0.18f, 1f), open);
        _retouchTicketButton.interactable = open;
        if (open || _retouchTicketLabel == null)
            return;
        _retouchTicketLabel.color = new Color(0.45f, 0.45f, 0.45f, 1f);
    }

    void SetRetouchConfirmMessage(string message, bool ok)
    {
        PaintMessage(_retouchConfirmMessage, message, ok);
    }

    static string RetouchDiffText(ImportRetouchDiff diff)
    {
        int percent = Mathf.RoundToInt(diff.ChangedRatio * 100f);
        return "修正 " + UiNumber.Format(diff.ChangedPixels) + " ドット (" + percent.ToString() + "%)\n"
            + "RGB合計 " + UiNumber.Format(diff.OldRgbSum) + " → " + UiNumber.Format(diff.NewRgbSum) + "\n"
            + "不透明 " + UiNumber.Format(diff.OldOpaque) + " → " + UiNumber.Format(diff.NewOpaque) + "\n"
            + "LUC " + UiNumber.Format(diff.OldLuc) + " → " + UiNumber.Format(diff.NewLuc);
    }

    void BuildRetouchPay()
    {
        _retouchPay = new GameObject("PanelImportRetouchPay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _retouchPay.transform.SetParent(Panel.transform, false);
        Stretch(_retouchPay.GetComponent<RectTransform>());
        _retouchPay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        _retouchPay.GetComponent<Image>().raycastTarget = true;

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(_retouchPay.transform, false);
        Place(card.GetComponent<RectTransform>(), 0f, 40f, 900f, 620f);
        card.GetComponent<Image>().color = Color.black;
        EnsureWhiteFrame(card.transform, FrameThin);

        _retouchPayPrompt = CreateText("TextRetouchPayPrompt", card.transform, 28, TextAnchor.MiddleCenter);
        Place(_retouchPayPrompt.rectTransform, 0f, 200f, 820f, 120f);
        _retouchPayPrompt.horizontalOverflow = HorizontalWrapMode.Wrap;
        _retouchPayPrompt.verticalOverflow = VerticalWrapMode.Overflow;

        _retouchPayDetail = CreateText("TextRetouchPayDetail", card.transform, 24, TextAnchor.MiddleCenter);
        Place(_retouchPayDetail.rectTransform, 0f, 40f, 820f, 160f);
        _retouchPayDetail.horizontalOverflow = HorizontalWrapMode.Wrap;
        _retouchPayDetail.verticalOverflow = VerticalWrapMode.Overflow;

        GameObject adObject = new GameObject("ButtonRetouchAd", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        adObject.transform.SetParent(card.transform, false);
        Place(adObject.GetComponent<RectTransform>(), -200f, -140f, 340f, 84f);
        adObject.GetComponent<Image>().color = Color.white;
        _retouchAdButton = adObject.GetComponent<Button>();
        _retouchAdButton.targetGraphic = adObject.GetComponent<Image>();
        _retouchAdButton.onClick.AddListener(PushRetouchAd);
        Text adLabel = CreateText("Text", adObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(adLabel.rectTransform);
        adLabel.text = "広告を見る";
        adLabel.color = Color.white;
        _production.StyleRepresentativeButton(_retouchAdButton, RetouchFill, true);

        GameObject ticketObject = new GameObject("ButtonRetouchTicket", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        ticketObject.transform.SetParent(card.transform, false);
        Place(ticketObject.GetComponent<RectTransform>(), 200f, -140f, 340f, 84f);
        ticketObject.GetComponent<Image>().color = Color.white;
        _retouchTicketButton = ticketObject.GetComponent<Button>();
        _retouchTicketButton.targetGraphic = ticketObject.GetComponent<Image>();
        _retouchTicketButton.onClick.AddListener(PushRetouchTicket);
        _retouchTicketLabel = CreateText("Text", ticketObject.transform, 26, TextAnchor.MiddleCenter);
        Stretch(_retouchTicketLabel.rectTransform);
        _retouchTicketLabel.text = "チケット (0)";
        _retouchTicketLabel.color = Color.white;

        GameObject cancelObject = new GameObject("ButtonRetouchPayCancel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cancelObject.transform.SetParent(card.transform, false);
        Place(cancelObject.GetComponent<RectTransform>(), 0f, -250f, 280f, 76f);
        cancelObject.GetComponent<Image>().color = Color.white;
        Button cancel = cancelObject.GetComponent<Button>();
        cancel.targetGraphic = cancelObject.GetComponent<Image>();
        cancel.onClick.AddListener(PushCancelRetouchPay);
        Text cancelLabel = CreateText("Text", cancelObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(cancelLabel.rectTransform);
        cancelLabel.text = "やめる";
        cancelLabel.color = Color.white;
        StyleChromeButton(cancel);

        _retouchPay.SetActive(false);
    }

    void BuildRetouchConfirm()
    {
        _retouchConfirm = new GameObject("PanelImportRetouchConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _retouchConfirm.transform.SetParent(Panel.transform, false);
        Stretch(_retouchConfirm.GetComponent<RectTransform>());
        _retouchConfirm.GetComponent<Image>().color = Color.black;
        _retouchConfirm.GetComponent<Image>().raycastTarget = true;

        Text title = CreateText("TextRetouchConfirmTitle", _retouchConfirm.transform, 36, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 0f, 620f, 1000f, 56f);
        title.text = "この絵に手直しする";

        GameObject oldPicture = new GameObject("ImageRetouchOld", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        oldPicture.transform.SetParent(_retouchConfirm.transform, false);
        Place(oldPicture.GetComponent<RectTransform>(), -220f, 250f, 300f, 300f);
        _retouchOldImage = oldPicture.GetComponent<Image>();
        _retouchOldImage.preserveAspect = true;
        _retouchOldImage.raycastTarget = false;
        EnsureWhiteFrame(oldPicture.transform, FrameThin);
        Text oldLabel = CreateText("TextRetouchOldLabel", _retouchConfirm.transform, 24, TextAnchor.MiddleCenter);
        Place(oldLabel.rectTransform, -220f, 70f, 300f, 40f);
        oldLabel.text = "いまの絵";

        GameObject newPicture = new GameObject("ImageRetouchNew", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        newPicture.transform.SetParent(_retouchConfirm.transform, false);
        Place(newPicture.GetComponent<RectTransform>(), 220f, 250f, 300f, 300f);
        _retouchNewImage = newPicture.GetComponent<Image>();
        _retouchNewImage.preserveAspect = true;
        _retouchNewImage.raycastTarget = false;
        EnsureWhiteFrame(newPicture.transform, FrameThin);
        Text newLabel = CreateText("TextRetouchNewLabel", _retouchConfirm.transform, 24, TextAnchor.MiddleCenter);
        Place(newLabel.rectTransform, 220f, 70f, 300f, 40f);
        newLabel.text = "新しい絵";

        Text prompt = CreateText("TextRetouchConfirmPrompt", _retouchConfirm.transform, 24, TextAnchor.MiddleCenter);
        Place(prompt.rectTransform, 0f, -10f, 960f, 70f);
        prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
        prompt.text = "属性・レベル・合成回数はそのまま残します。\n新しい絵のRGBを消費して差し替えます。";

        GameObject costRow = new GameObject("RetouchCostRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        costRow.transform.SetParent(_retouchConfirm.transform, false);
        Place(costRow.GetComponent<RectTransform>(), 0f, -100f, 960f, 72f);
        HorizontalLayoutGroup layout = costRow.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 18f;
        layout.padding = new RectOffset(12, 12, 10, 10);
        _retouchCostIntro = CreateCostText(costRow.transform, "消費", 36);
        _retouchCostR = CreateCostText(costRow.transform, "R", 40);
        _retouchCostG = CreateCostText(costRow.transform, "G", 40);
        _retouchCostB = CreateCostText(costRow.transform, "B", 40);
        ControllerProduction.PaintChannelText(_retouchCostR, 0);
        ControllerProduction.PaintChannelText(_retouchCostG, 1);
        ControllerProduction.PaintChannelText(_retouchCostB, 2);
        _retouchCostError = CreateCostText(costRow.transform, "", 32);
        _retouchCostError.gameObject.SetActive(false);

        GameObject okObject = new GameObject("ButtonRetouchConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        okObject.transform.SetParent(_retouchConfirm.transform, false);
        Place(okObject.GetComponent<RectTransform>(), -200f, -260f, 360f, 84f);
        okObject.GetComponent<Image>().color = Color.white;
        _retouchConfirmButton = okObject.GetComponent<Button>();
        _retouchConfirmButton.targetGraphic = okObject.GetComponent<Image>();
        _retouchConfirmButton.onClick.AddListener(PushConfirmRetouch);
        Text okLabel = CreateText("Text", okObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(okLabel.rectTransform);
        okLabel.text = "この絵に差し替える";
        _production.StyleRepresentativeButton(_retouchConfirmButton, ImportOkFill, true);

        GameObject cancelObject = new GameObject("ButtonRetouchConfirmCancel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cancelObject.transform.SetParent(_retouchConfirm.transform, false);
        Place(cancelObject.GetComponent<RectTransform>(), 200f, -260f, 280f, 84f);
        cancelObject.GetComponent<Image>().color = Color.white;
        Button cancel = cancelObject.GetComponent<Button>();
        cancel.targetGraphic = cancelObject.GetComponent<Image>();
        cancel.onClick.AddListener(PushCancelRetouchConfirm);
        Text cancelLabel = CreateText("Text", cancelObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(cancelLabel.rectTransform);
        cancelLabel.text = "やめる";
        _production.StyleRepresentativeButton(cancel, ImportCancelFill, true);

        _retouchConfirmMessage = CreateText("TextRetouchConfirmMessage", _retouchConfirm.transform, 24, TextAnchor.MiddleCenter);
        Place(_retouchConfirmMessage.rectTransform, 0f, -370f, 960f, 70f);
        _retouchConfirmMessage.horizontalOverflow = HorizontalWrapMode.Wrap;

        _retouchConfirm.SetActive(false);
    }

    void BuildDeleteConfirm()
    {
        _deleteConfirm = new GameObject("PanelImportDelete", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _deleteConfirm.transform.SetParent(Panel.transform, false);
        Stretch(_deleteConfirm.GetComponent<RectTransform>());
        _deleteConfirm.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        _deleteConfirm.GetComponent<Image>().raycastTarget = true;

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(_deleteConfirm.transform, false);
        Place(card.GetComponent<RectTransform>(), 0f, 40f, 820f, 460f);
        card.GetComponent<Image>().color = Color.black;
        EnsureWhiteFrame(card.transform, FrameThin);

        _deletePrompt = CreateText("TextImportDeletePrompt", card.transform, 28, TextAnchor.MiddleCenter);
        Place(_deletePrompt.rectTransform, 0f, 70f, 740f, 220f);
        _deletePrompt.horizontalOverflow = HorizontalWrapMode.Wrap;
        _deletePrompt.verticalOverflow = VerticalWrapMode.Overflow;

        GameObject okObject = new GameObject("ButtonImportDelete", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        okObject.transform.SetParent(card.transform, false);
        Place(okObject.GetComponent<RectTransform>(), -170f, -140f, 280f, 80f);
        okObject.GetComponent<Image>().color = Color.white;
        Button ok = okObject.GetComponent<Button>();
        ok.targetGraphic = okObject.GetComponent<Image>();
        ok.onClick.AddListener(PushDelete);
        Text okLabel = CreateText("Text", okObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(okLabel.rectTransform);
        okLabel.text = "消す";
        okLabel.color = Color.white;
        StyleChromeButton(ok);
        PaintDeleteFill(ok);

        GameObject cancelObject = new GameObject("ButtonImportDeleteCancel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cancelObject.transform.SetParent(card.transform, false);
        Place(cancelObject.GetComponent<RectTransform>(), 170f, -140f, 280f, 80f);
        cancelObject.GetComponent<Image>().color = Color.white;
        Button cancel = cancelObject.GetComponent<Button>();
        cancel.targetGraphic = cancelObject.GetComponent<Image>();
        cancel.onClick.AddListener(PushCancelDelete);
        Text cancelLabel = CreateText("Text", cancelObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(cancelLabel.rectTransform);
        cancelLabel.text = "やめる";
        cancelLabel.color = Color.white;
        StyleChromeButton(cancel);
        EnsureWhiteFrame(card.transform, FrameThin);

        _deleteConfirm.SetActive(false);
    }

    void BuildRebirth()
    {
        GameObject birth = new GameObject("PanelImportRebirth", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        birth.transform.SetParent(Panel.transform, false);
        Stretch(birth.GetComponent<RectTransform>());
        _rebirth = birth.AddComponent<ImportRebirthPlayer>();
        _rebirth.Initialize(_production, _font, OnRebirthClosed);
    }

    void BuildConfirm()
    {
        _confirm = new GameObject("PanelImportConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _confirm.transform.SetParent(Panel.transform, false);
        Stretch(_confirm.GetComponent<RectTransform>());
        _confirm.GetComponent<Image>().color = Color.black;
        _confirm.GetComponent<Image>().raycastTarget = true;

        Text title = CreateText("TextImportConfirmTitle", _confirm.transform, 36, TextAnchor.MiddleCenter);
        Place(title.rectTransform, 0f, 620f, 1000f, 56f);
        title.text = "この絵を読み込む";

        GameObject picture = new GameObject("ImageImportConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        picture.transform.SetParent(_confirm.transform, false);
        Place(picture.GetComponent<RectTransform>(), 0f, 230f, 380f, 380f);
        _confirmImage = picture.GetComponent<Image>();
        _confirmImage.preserveAspect = true;
        _confirmImage.raycastTarget = false;
        EnsureWhiteFrame(picture.transform, FrameThin);

        Text prompt = CreateText("TextImportConfirmPrompt", _confirm.transform, 26, TextAnchor.MiddleCenter);
        Place(prompt.rectTransform, 0f, -8f, 960f, 80f);
        prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
        prompt.verticalOverflow = VerticalWrapMode.Overflow;
        prompt.text = "名前を決めて読み込むと、この絵がキャラになります。\n10文字以内です。アルファベットは0.5文字です。";
        _confirmPrompt = prompt;

        BuildImportCostRow(_confirm.transform);
        BuildNameField(_confirm.transform);

        GameObject okObject = new GameObject("ButtonImportConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        okObject.transform.SetParent(_confirm.transform, false);
        Place(okObject.GetComponent<RectTransform>(), -200f, -300f, 360f, 84f);
        okObject.GetComponent<Image>().color = Color.white;
        Button ok = okObject.GetComponent<Button>();
        ok.targetGraphic = okObject.GetComponent<Image>();
        ok.onClick.AddListener(PushConfirmName);
        _confirmButton = ok;
        Text okLabel = CreateText("Text", okObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(okLabel.rectTransform);
        okLabel.text = "この名前で読み込む";
        _production.StyleRepresentativeButton(ok, ImportOkFill, true);

        GameObject cancelObject = new GameObject("ButtonImportCancel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cancelObject.transform.SetParent(_confirm.transform, false);
        Place(cancelObject.GetComponent<RectTransform>(), 200f, -300f, 280f, 84f);
        cancelObject.GetComponent<Image>().color = Color.white;
        Button cancel = cancelObject.GetComponent<Button>();
        cancel.targetGraphic = cancelObject.GetComponent<Image>();
        cancel.onClick.AddListener(PushCancelImport);
        Text cancelLabel = CreateText("Text", cancelObject.transform, 28, TextAnchor.MiddleCenter);
        Stretch(cancelLabel.rectTransform);
        cancelLabel.text = "やめる";
        _production.StyleRepresentativeButton(cancel, ImportCancelFill, true);

        _confirmMessage = CreateText("TextImportConfirmMessage", _confirm.transform, 24, TextAnchor.MiddleCenter);
        Place(_confirmMessage.rectTransform, 0f, -410f, 960f, 70f);
        _confirmMessage.horizontalOverflow = HorizontalWrapMode.Wrap;

        _confirm.SetActive(false);
    }

    void BuildNameField(Transform parent)
    {
        GameObject fieldObject = new GameObject("InputFieldImportCharacterName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
        fieldObject.transform.SetParent(parent, false);
        Place(fieldObject.GetComponent<RectTransform>(), 0f, -200f, 760f, 80f);
        Image background = fieldObject.GetComponent<Image>();
        background.color = Color.black;

        Text text = CreateText("Text", fieldObject.transform, 32, TextAnchor.MiddleLeft);
        Place(text.rectTransform, 8f, 0f, 720f, 70f);
        text.color = Color.white;
        text.supportRichText = false;

        Text placeholder = CreateText("Placeholder", fieldObject.transform, 28, TextAnchor.MiddleLeft);
        Place(placeholder.rectTransform, 8f, 0f, 720f, 70f);
        placeholder.text = "名前";
        placeholder.color = new Color(0.55f, 0.55f, 0.58f, 1f);
        placeholder.fontStyle = FontStyle.Italic;

        _name = fieldObject.GetComponent<InputField>();
        _name.textComponent = text;
        _name.placeholder = placeholder;
        _name.lineType = InputField.LineType.SingleLine;
        _name.characterLimit = 0;
        _name.onValidateInput = ValidateImportName;
        _name.onValueChanged.AddListener(ClampImportName);
        _name.targetGraphic = background;
        _name.customCaretColor = true;
        _name.caretColor = Color.white;
        EnsureWhiteFrame(fieldObject.transform, FrameThin);
    }

    char ValidateImportName(string text, int index, char added)
    {
        if (added < ' ')
            return '\0';
        if (_name == null)
            return added;
        string current = text ?? "";
        int start = Mathf.Min(_name.selectionAnchorPosition, _name.selectionFocusPosition);
        int end = Mathf.Max(_name.selectionAnchorPosition, _name.selectionFocusPosition);
        int insertAt = index;
        if (start >= 0 && end > start && end <= current.Length)
        {
            current = current.Remove(start, end - start);
            insertAt = start;
        }
        if (insertAt < 0)
            insertAt = 0;
        if (insertAt > current.Length)
            insertAt = current.Length;
        string next = current.Insert(insertAt, added.ToString());
        if (!ImportNameRules.Fits(next))
            return '\0';
        return added;
    }

    void ClampImportName(string value)
    {
        if (_clampingName || _name == null)
            return;
        string clamped = ImportNameRules.Clamp(value ?? "");
        if (clamped == value)
            return;
        _clampingName = true;
        int caret = _name.caretPosition;
        _name.text = clamped;
        if (caret > clamped.Length)
            caret = clamped.Length;
        _name.caretPosition = caret;
        _name.selectionAnchorPosition = caret;
        _name.selectionFocusPosition = caret;
        _clampingName = false;
    }

    void BuildScroll()
    {
        GameObject scrollObject = new GameObject("ImportScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(Panel.transform, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        PlaceCharacterList(scrollRect);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        _content = content.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(0f, 0f);

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(188f, 390f);
        grid.spacing = new Vector2(16f, 16f);
        grid.padding = new RectOffset(16, 16, 16, 16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = _content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        EnsureWhiteFrame(scrollObject.transform, FrameThin);
    }

    // 上端は説明の下のまま。下端はパネルの下、メニューに重なるときはその直前まで伸ばす。
    void PlaceCharacterList(RectTransform scrollRect)
    {
        const float top = 72f;
        const float gap = 8f;
        const float fallbackHeight = 680f;
        RectTransform panelRect = Panel.GetComponent<RectTransform>();
        float panelHeight = panelRect.rect.height;
        if (panelHeight < 8f)
        {
            Place(scrollRect, 0f, -220f, 1040f, fallbackHeight);
            return;
        }

        float bottom = -panelHeight * 0.5f + gap;
        GameObject bar = GameObject.Find("PanelSelectScene");
        RectTransform barRect = bar != null ? bar.GetComponent<RectTransform>() : null;
        if (barRect != null && barRect.rect.height > 8f)
        {
            Vector3[] corners = new Vector3[4];
            barRect.GetWorldCorners(corners);
            float aboveBar = panelRect.InverseTransformPoint(corners[1]).y - gap;
            if (aboveBar > bottom && aboveBar < top - 240f)
                bottom = aboveBar;
        }

        float height = top - bottom;
        Place(scrollRect, 0f, (top + bottom) * 0.5f, 1040f, height);
    }

    Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    void PaintPickButton()
    {
        if (_pickButton == null || _production == null)
            return;
        string unused;
        bool open = !ImportedCharacters.RejectImportCount(out unused);
        _production.StyleRepresentativeButton(_pickButton, Color.white, open);
        _pickButton.interactable = open;
        Text label = _pickButton.GetComponentInChildren<Text>();
        if (!open && label != null)
            label.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        Transform frame = _pickButton.transform.Find("WhiteFrame");
        if (open)
        {
            if (frame != null)
                frame.gameObject.SetActive(false);
            return;
        }
        EnsureWhiteFrame(_pickButton.transform, FrameThin);
        frame = _pickButton.transform.Find("WhiteFrame");
        if (frame != null)
            frame.gameObject.SetActive(true);
        SetFrameColor(_pickButton.transform, new Color(0.45f, 0.45f, 0.45f, 1f));
    }

    void PaintConfirmAfford(bool affordable)
    {
        if (_confirmButton == null || _production == null)
            return;
        _production.StyleRepresentativeButton(_confirmButton, ImportOkFill, affordable);
        _confirmButton.interactable = affordable;
        if (affordable)
            return;
        Text label = _confirmButton.GetComponentInChildren<Text>();
        if (label != null)
            label.color = new Color(0.45f, 0.45f, 0.45f, 1f);
    }

    static void PaintDeleteFill(Button button)
    {
        if (button == null)
            return;
        ColorBlock colors = button.colors;
        colors.normalColor = DeleteFill;
        colors.highlightedColor = new Color(0.9f, 0.28f, 0.24f, 1f);
        colors.pressedColor = new Color(0.55f, 0.1f, 0.1f, 1f);
        colors.selectedColor = DeleteFill;
        colors.disabledColor = DeleteFill;
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    static void PaintRetouchFill(Button button)
    {
        if (button == null)
            return;
        ColorBlock colors = button.colors;
        colors.normalColor = RetouchFill;
        colors.highlightedColor = new Color(0.22f, 0.8f, 0.36f, 1f);
        colors.pressedColor = new Color(0.08f, 0.45f, 0.18f, 1f);
        colors.selectedColor = RetouchFill;
        colors.disabledColor = RetouchFill;
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    static void PaintAttributeGradientButton(Button button)
    {
        if (button == null)
            return;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    // 赤・緑・青・紫・黄の横グラデ。属性チェンジボタン用。
    static Sprite AttributeGradientSprite()
    {
        if (_attributeGradientSprite != null)
            return _attributeGradientSprite;
        const int width = 160;
        const int height = 8;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] stops =
        {
            new Color(0.95f, 0.18f, 0.14f, 1f),
            new Color(0.16f, 0.78f, 0.28f, 1f),
            new Color(0.18f, 0.42f, 0.95f, 1f),
            new Color(0.62f, 0.22f, 0.9f, 1f),
            new Color(0.95f, 0.82f, 0.14f, 1f),
        };
        for (int x = 0; x < width; x++)
        {
            float t = x / (float)(width - 1);
            float scaled = t * (stops.Length - 1);
            int index = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, stops.Length - 2);
            float local = scaled - index;
            Color color = Color.Lerp(stops[index], stops[index + 1], local);
            for (int y = 0; y < height; y++)
                texture.SetPixel(x, y, color);
        }
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        _attributeGradientSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        _attributeGradientSprite.hideFlags = HideFlags.HideAndDontSave;
        return _attributeGradientSprite;
    }

    static void StyleChromeButton(Button button)
    {
        if (button == null)
            return;
        Image image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();
        if (image != null)
            image.color = Color.white;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.black;
        colors.highlightedColor = new Color(0.16f, 0.16f, 0.18f, 1f);
        colors.pressedColor = new Color(0.28f, 0.28f, 0.3f, 1f);
        colors.selectedColor = Color.black;
        colors.disabledColor = Color.black;
        colors.colorMultiplier = 1f;
        button.colors = colors;
        EnsureWhiteFrame(button.transform, FrameThin);
    }

    static void EnsureWhiteFrame(Transform target, float thickness)
    {
        if (target == null)
            return;
        Transform frame = target.Find("WhiteFrame");
        if (frame == null)
        {
            GameObject root = new GameObject("WhiteFrame", typeof(RectTransform));
            root.transform.SetParent(target, false);
            Stretch(root.GetComponent<RectTransform>());
            AddWhiteEdge(root.transform, "Top", true, true);
            AddWhiteEdge(root.transform, "Bottom", true, false);
            AddWhiteEdge(root.transform, "Left", false, false);
            AddWhiteEdge(root.transform, "Right", false, true);
            frame = root.transform;
        }
        LayoutWhiteEdge(frame, "Top", true, true, thickness);
        LayoutWhiteEdge(frame, "Bottom", true, false, thickness);
        LayoutWhiteEdge(frame, "Left", false, false, thickness);
        LayoutWhiteEdge(frame, "Right", false, true, thickness);
        frame.SetAsLastSibling();
    }

    static void AddWhiteEdge(Transform parent, string name, bool horizontal, bool end)
    {
        GameObject edgeObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        edgeObject.transform.SetParent(parent, false);
        Image image = edgeObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        LayoutWhiteEdge(parent, name, horizontal, end, FrameThin);
    }

    static void LayoutWhiteEdge(Transform frame, string name, bool horizontal, bool end, float thickness)
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
            rect.sizeDelta = new Vector2(0f, thickness);
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(end ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(end ? 1f : 0f, 1f);
            rect.pivot = new Vector2(end ? 1f : 0f, 0.5f);
            rect.sizeDelta = new Vector2(thickness, 0f);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    static void SetFrameColor(Transform target, Color color)
    {
        if (target == null)
            return;
        Transform frame = target.Find("WhiteFrame");
        if (frame == null)
            return;
        Image[] images = frame.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
            images[i].color = color;
    }

    static void Place(RectTransform rect, float x, float y, float width, float height)
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
