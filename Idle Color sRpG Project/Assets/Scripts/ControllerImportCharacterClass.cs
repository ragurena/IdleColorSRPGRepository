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
    ImportRebirthPlayer _rebirth;
    readonly List<GameObject> _cells = new List<GameObject>();

    const float FrameThin = 3f;
    static readonly Color DeleteFill = new Color(0.78f, 0.18f, 0.16f, 1f);
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
    }

    public void Close()
    {
        if (_rebirth != null)
            _rebirth.Halt();
        _showingRebirth = false;
        HideDelete();
        DiscardPending();
    }

    public void Refresh()
    {
        if (_count != null && _production != null)
        {
            int count = _production.ImportedCharacterCount();
            int cap = ImportedCharacters.ImportCap;
            bool full = count >= cap;
            Color ink = full ? new Color(1f, 0.38f, 0.34f, 1f) : Color.white;
            _count.text = "読み込み " + UiNumber.Format(count) + " / " + UiNumber.Format(cap);
            _count.color = ink;
            if (_capNote != null)
                _capNote.color = ink;
        }
        if (_sizes != null && _production != null)
            _sizes.text = ImportSizeRules.Describe(_production.IsBattleStageCleared);
        PaintPickButton();
        RebuildCells();
    }

    public void PushPick()
    {
        if (_picking || _showingRebirth || _production == null)
            return;
        if (_confirm != null && _confirm.activeSelf)
            return;
        if (_deleteConfirm != null && _deleteConfirm.activeSelf)
            return;
        string capMessage;
        if (ImportedCharacters.RejectImportCount(out capMessage))
        {
            SetMessage(capMessage, false);
            return;
        }

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
        if (_production == null)
            return;
        if (path == null)
        {
            SetMessage("この環境ではファイルを選べません。", false);
            return;
        }
        if (string.IsNullOrEmpty(path))
        {
            SetMessage("ファイルを選びませんでした。", false);
            return;
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (IOException)
        {
            SetMessage("ファイルを読めませんでした。", false);
            return;
        }

        string message;
        bool prepared = _production.TryPrepareImport(bytes, out message);
        if (!prepared)
        {
            SetMessage(message, false);
            return;
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
        if (_confirm == null || !_confirm.activeSelf || _showingRebirth)
            return;
        PaintImportCost();
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
        if (name.Length > 24)
            name = name.Substring(0, 24);
        return name;
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
        pictureRect.anchorMin = new Vector2(0.15f, 0.46f);
        pictureRect.anchorMax = new Vector2(0.85f, 0.96f);
        pictureRect.offsetMin = Vector2.zero;
        pictureRect.offsetMax = Vector2.zero;
        Image image = picture.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.sprite = SpriteOf(character);

        Text label = CreateText("Label", cell.transform, 18, TextAnchor.UpperCenter);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.22f);
        labelRect.anchorMax = new Vector2(1f, 0.46f);
        labelRect.offsetMin = new Vector2(4f, 0f);
        labelRect.offsetMax = new Vector2(-4f, 0f);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.text = character.Name + "\n" + character.Size.ToString();

        uint id = character.ID;
        string name = string.IsNullOrWhiteSpace(character.Name) ? "ななし" : character.Name.Trim();
        GameObject deleteObject = new GameObject("ButtonDelete", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        deleteObject.transform.SetParent(cell.transform, false);
        RectTransform deleteRect = deleteObject.GetComponent<RectTransform>();
        deleteRect.anchorMin = new Vector2(0.08f, 0.03f);
        deleteRect.anchorMax = new Vector2(0.92f, 0.2f);
        deleteRect.offsetMin = Vector2.zero;
        deleteRect.offsetMax = Vector2.zero;
        deleteObject.GetComponent<Image>().color = Color.white;
        Button delete = deleteObject.GetComponent<Button>();
        delete.targetGraphic = deleteObject.GetComponent<Image>();
        delete.onClick.AddListener(() => AskDelete(id, name));
        Text deleteLabel = CreateText("Text", deleteObject.transform, 20, TextAnchor.MiddleCenter);
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
        prompt.text = "名前を決めて読み込むと、この絵がキャラになります。\n違う名前も設定できます。";
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
        _name.characterLimit = 24;
        _name.targetGraphic = background;
        _name.customCaretColor = true;
        _name.caretColor = Color.white;
        EnsureWhiteFrame(fieldObject.transform, FrameThin);
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
        grid.cellSize = new Vector2(188f, 280f);
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
