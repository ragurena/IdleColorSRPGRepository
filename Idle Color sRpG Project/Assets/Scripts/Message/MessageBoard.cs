using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// メッセージの一覧とトーク。判定は MessageDirector に任せる。
public sealed class MessageBoard
{
    const float Header = 96f;
    const float InputBar = 120f;

    ControllerProduction _game;
    Font _font;
    GameObject _root;
    GameObject _listPage;
    GameObject _talkPage;
    RectTransform _listContent;
    RectTransform _talkContent;
    ScrollRect _talkScroll;
    Text _talkTitle;
    Image _talkIcon;
    Text _inputText;
    Text _inputWarning;
    Button _send;
    Button _jumpTop;
    Button _jumpBottom;
    string _talkId = "";
    readonly List<PlateView> _plates = new List<PlateView>();

    public bool IsOpen
    {
        get { return _root != null && _root.activeSelf; }
    }

    public bool TalkOpen
    {
        get { return IsOpen && _talkPage != null && _talkPage.activeSelf; }
    }

    public string OpenTalkId
    {
        get { return TalkOpen ? _talkId : ""; }
    }

    public void Build(Transform parent, Font font, ControllerProduction game)
    {
        _font = font;
        _game = game;
        _root = new GameObject("PanelMessage", typeof(RectTransform), typeof(Image));
        _root.transform.SetParent(parent, false);
        _root.GetComponent<Image>().color = new Color(0.93f, 0.94f, 0.96f, 1f);
        RectTransform rootRect = _root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.1f, 0.1f);
        rootRect.anchorMax = new Vector2(0.9f, 0.9f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        BuildList();
        BuildTalk();
        _root.SetActive(false);
    }

    public void Open()
    {
        if (_root == null || _game == null)
            return;
        _game.RefreshMessages();
        _game.SaveMessagesIfNeeded();
        _root.SetActive(true);
        Transform overlay = _root.transform.parent;
        if (overlay != null)
            overlay.SetAsLastSibling();
        _root.transform.SetAsLastSibling();
        ShowList();
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);
        _talkId = "";
    }

    public void Refresh()
    {
        if (_root == null || !_root.activeSelf || _game == null || _game.Messages == null)
            return;
        if (_talkPage != null && _talkPage.activeSelf)
        {
            if (_game.Messages.IsUnread(_talkId))
            {
                _game.Messages.MarkRead(_talkId);
                _game.NoteMessageRead();
            }
            FillTalk();
        }
        FillList();
    }

    void BuildList()
    {
        _listPage = new GameObject("PageMessageList", typeof(RectTransform));
        _listPage.transform.SetParent(_root.transform, false);
        Stretch(_listPage.GetComponent<RectTransform>());

        Text title = CreateText("TextMessageTitle", _listPage.transform, 35, TextAnchor.MiddleCenter);
        title.text = "メッセージ";
        title.color = new Color(0.12f, 0.14f, 0.18f, 1f);
        PlaceTop(title.rectTransform, 16f, 0f, 360f, 56f);

        Button close = CreateButton("ButtonMessageClose", _listPage.transform, "閉じる", new Color(0.75f, 0.28f, 0.24f, 1f));
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-16f, -12f);
        closeRect.sizeDelta = new Vector2(140f, 64f);
        close.onClick.AddListener(Close);

        _listContent = CreateScroll("ScrollMessageList", _listPage.transform, 88f, 16f);
    }

    void BuildTalk()
    {
        _talkPage = new GameObject("PageMessageTalk", typeof(RectTransform), typeof(Image));
        _talkPage.transform.SetParent(_root.transform, false);
        _talkPage.GetComponent<Image>().color = new Color(0.62f, 0.76f, 0.90f, 1f);
        Stretch(_talkPage.GetComponent<RectTransform>());

        Image header = CreateImage("ImageMessageTalkHeader", _talkPage.transform, new Color(0.16f, 0.20f, 0.28f, 1f));
        RectTransform headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, Header);
        headerRect.anchoredPosition = Vector2.zero;

        Button back = CreateButton("ButtonMessageBack", header.transform, "戻る", new Color(0.30f, 0.34f, 0.42f, 1f));
        RectTransform backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0f, 0.5f);
        backRect.anchorMax = new Vector2(0f, 0.5f);
        backRect.pivot = new Vector2(0f, 0.5f);
        backRect.anchoredPosition = new Vector2(16f, 0f);
        backRect.sizeDelta = new Vector2(120f, 64f);
        back.onClick.AddListener(ShowList);

        _talkIcon = CreateImage("ImageMessageTalkIcon", header.transform, Color.white);
        _talkIcon.sprite = MessageImages.Circle();
        RectTransform iconRect = _talkIcon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(152f, 0f);
        iconRect.sizeDelta = new Vector2(83f, 83f);

        _talkTitle = CreateText("TextMessageTalkTitle", header.transform, 31, TextAnchor.MiddleLeft);
        _talkTitle.color = Color.white;
        RectTransform titleRect = _talkTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(251f, 0f);
        titleRect.offsetMax = new Vector2(-16f, 0f);

        _jumpTop = CreateButton("ButtonMessageQuestAbove", _talkPage.transform, "未処理のトーククエストがあります", new Color(0.95f, 0.86f, 0.45f, 1f));
        PlaceBand(_jumpTop.GetComponent<RectTransform>(), true);
        Text topLabel = _jumpTop.GetComponentInChildren<Text>();
        if (topLabel != null)
        {
            topLabel.color = new Color(0.2f, 0.16f, 0.05f, 1f);
            topLabel.fontSize = 25;
        }
        _jumpTop.onClick.AddListener(() => FocusPlate(true));

        _jumpBottom = CreateButton("ButtonMessageQuestBelow", _talkPage.transform, "未処理のトーククエストがあります", new Color(0.95f, 0.86f, 0.45f, 1f));
        PlaceBand(_jumpBottom.GetComponent<RectTransform>(), false);
        Text bottomLabel = _jumpBottom.GetComponentInChildren<Text>();
        if (bottomLabel != null)
        {
            bottomLabel.color = new Color(0.2f, 0.16f, 0.05f, 1f);
            bottomLabel.fontSize = 25;
        }
        _jumpBottom.onClick.AddListener(() => FocusPlate(false));

        _talkContent = CreateScroll("ScrollMessageTalk", _talkPage.transform, Header, InputBar + 36f);
        _talkScroll = _talkContent.parent.parent.GetComponent<ScrollRect>();
        if (_talkScroll != null)
            _talkScroll.onValueChanged.AddListener(value => UpdateQuestBanners());

        Image bar = CreateImage("ImageMessageInput", _talkPage.transform, new Color(0.95f, 0.96f, 0.97f, 1f));
        RectTransform barRect = bar.rectTransform;
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.sizeDelta = new Vector2(0f, InputBar);
        barRect.anchoredPosition = Vector2.zero;

        _inputWarning = CreateText("TextMessageSendWarning", bar.transform, 23, TextAnchor.MiddleLeft);
        _inputWarning.color = new Color(0.7f, 0.35f, 0.1f, 1f);
        RectTransform warningRect = _inputWarning.rectTransform;
        warningRect.anchorMin = new Vector2(0f, 1f);
        warningRect.anchorMax = new Vector2(1f, 1f);
        warningRect.pivot = new Vector2(0.5f, 1f);
        warningRect.anchoredPosition = new Vector2(0f, -4f);
        warningRect.sizeDelta = new Vector2(-32f, 32f);

        Image field = CreateImage("ImageMessageField", bar.transform, new Color(0.86f, 0.88f, 0.9f, 1f));
        RectTransform fieldRect = field.rectTransform;
        fieldRect.anchorMin = new Vector2(0f, 0f);
        fieldRect.anchorMax = new Vector2(1f, 0f);
        fieldRect.pivot = new Vector2(0f, 0f);
        fieldRect.offsetMin = new Vector2(16f, 24f);
        fieldRect.offsetMax = new Vector2(-160f, 84f);
        _inputText = CreateText("Text", field.transform, 27, TextAnchor.MiddleLeft);
        _inputText.color = new Color(0.2f, 0.22f, 0.26f, 1f);
        _inputText.rectTransform.offsetMin = new Vector2(16f, 0f);
        _inputText.rectTransform.offsetMax = new Vector2(-12f, 0f);
        Stretch(_inputText.rectTransform);
        _inputText.rectTransform.offsetMin = new Vector2(16f, 0f);
        _inputText.rectTransform.offsetMax = new Vector2(-12f, 0f);

        _send = CreateButton("ButtonMessageSend", bar.transform, "送信", new Color(0.62f, 0.66f, 0.7f, 1f));
        RectTransform sendRect = _send.GetComponent<RectTransform>();
        sendRect.anchorMin = new Vector2(1f, 0f);
        sendRect.anchorMax = new Vector2(1f, 0f);
        sendRect.pivot = new Vector2(1f, 0f);
        sendRect.anchoredPosition = new Vector2(-16f, 24f);
        sendRect.sizeDelta = new Vector2(128f, 60f);
        _send.onClick.AddListener(PushSend);

        _jumpTop.transform.SetAsLastSibling();
        _jumpBottom.transform.SetAsLastSibling();
        bar.transform.SetAsLastSibling();
        _talkPage.SetActive(false);
    }

    void ShowList()
    {
        _talkId = "";
        if (_talkPage != null)
            _talkPage.SetActive(false);
        if (_listPage != null)
            _listPage.SetActive(true);
        FillList();
        if (_game != null)
            _game.RefreshMessageButton();
    }

    void OpenTalk(string talkId)
    {
        _talkId = talkId;
        if (_listPage != null)
            _listPage.SetActive(false);
        if (_talkPage != null)
            _talkPage.SetActive(true);
        MessageFace face = MessageRoster.Find(talkId);
        if (_talkTitle != null)
            _talkTitle.text = face != null ? face.Name : "";
        if (_talkIcon != null)
            PaintFace(_talkIcon, face, face != null ? face.IconFile : "");
        if (_game != null && _game.Messages != null && _game.Messages.IsUnread(talkId))
        {
            _game.Messages.MarkRead(talkId);
            _game.NoteMessageRead();
        }
        FillTalk();
        if (_game != null)
            _game.RefreshMessageButton();
    }

    void FillList()
    {
        if (_listContent == null || _game == null || _game.Messages == null)
            return;
        Clear(_listContent);
        List<TalkRow> rows = _game.Messages.Rows();
        for (int i = 0; i < rows.Count; i++)
            CreateTalkRow(rows[i]);
    }

    void CreateTalkRow(TalkRow row)
    {
        GameObject box = new GameObject("Row_" + row.TalkId, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        box.transform.SetParent(_listContent, false);
        VerticalLayoutGroup column = box.GetComponent<VerticalLayoutGroup>();
        column.spacing = 4f;
        column.childAlignment = TextAnchor.UpperCenter;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        LayoutElement boxLayout = box.GetComponent<LayoutElement>();
        boxLayout.minHeight = 128f;

        Button button = CreateButton("ButtonMessageTalk_" + row.TalkId, box.transform, "", Color.white);
        LayoutElement buttonLayout = button.gameObject.AddComponent<LayoutElement>();
        buttonLayout.minHeight = 128f;
        buttonLayout.preferredHeight = 128f;
        string talkId = row.TalkId;
        button.onClick.AddListener(() => OpenTalk(talkId));
        Text hidden = button.GetComponentInChildren<Text>();
        if (hidden != null)
            hidden.gameObject.SetActive(false);

        Image icon = CreateImage("Icon", button.transform, Color.white);
        PaintFace(icon, MessageRoster.Find(row.TalkId), row.IconFile);
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(16f, 0f);
        iconRect.sizeDelta = new Vector2(104f, 104f);

        Text name = CreateText("Name", button.transform, 31, TextAnchor.MiddleLeft);
        name.text = row.Name;
        name.color = new Color(0.1f, 0.12f, 0.16f, 1f);
        name.fontStyle = FontStyle.Bold;
        RectTransform nameRect = name.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(136f, 0f);
        nameRect.offsetMax = new Vector2(-48f, -8f);

        Text preview = CreateText("Preview", button.transform, 25, TextAnchor.MiddleLeft);
        preview.text = row.Preview;
        preview.color = new Color(0.4f, 0.44f, 0.5f, 1f);
        RectTransform previewRect = preview.rectTransform;
        previewRect.anchorMin = new Vector2(0f, 0f);
        previewRect.anchorMax = new Vector2(1f, 0.5f);
        previewRect.offsetMin = new Vector2(136f, 8f);
        previewRect.offsetMax = new Vector2(-48f, 0f);

        if (row.Unread)
        {
            Image dot = CreateImage("Unread", button.transform, new Color(0.86f, 0.16f, 0.16f, 1f));
            dot.sprite = MessageImages.Circle();
            RectTransform dotRect = dot.rectTransform;
            dotRect.anchorMin = new Vector2(1f, 0.5f);
            dotRect.anchorMax = new Vector2(1f, 0.5f);
            dotRect.pivot = new Vector2(1f, 0.5f);
            dotRect.anchoredPosition = new Vector2(-20f, 0f);
            dotRect.sizeDelta = new Vector2(18f, 18f);
        }

        for (int i = 0; i < row.Offers.Count; i++)
            CreateOffer(box.transform, row.Offers[i]);
    }

    void CreateOffer(Transform parent, OfferRow offer)
    {
        Image plate = CreateImage("Offer_" + offer.OfferId.ToString(), parent, new Color(0.86f, 0.88f, 0.9f, 1f));
        LayoutElement layout = plate.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 132f;
        layout.preferredHeight = 132f;
        Text body = CreateText("Text", plate.transform, 25, TextAnchor.UpperLeft);
        body.color = new Color(0.25f, 0.28f, 0.32f, 1f);
        string text = offer.Prompt;
        if (!string.IsNullOrEmpty(offer.Detail))
            text += "\n" + offer.Detail;
        if (!string.IsNullOrEmpty(offer.Warning))
            text += "\n" + offer.Warning;
        body.text = text;
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(16f, 12f);
        bodyRect.offsetMax = new Vector2(-150f, -8f);

        Button yes = CreateButton("ButtonMessageAccept_" + offer.TalkId + "_" + offer.OfferId.ToString(), plate.transform, "はい", new Color(0.28f, 0.55f, 0.34f, 1f));
        RectTransform yesRect = yes.GetComponent<RectTransform>();
        yesRect.anchorMin = new Vector2(1f, 0.5f);
        yesRect.anchorMax = new Vector2(1f, 0.5f);
        yesRect.pivot = new Vector2(1f, 0.5f);
        yesRect.anchoredPosition = new Vector2(-16f, 0f);
        yesRect.sizeDelta = new Vector2(120f, 64f);
        string talkId = offer.TalkId;
        int offerId = offer.OfferId;
        yes.onClick.AddListener(() => PushAccept(talkId, offerId));
    }

    void FillTalk()
    {
        if (_talkContent == null || _game == null || _game.Messages == null)
            return;
        Clear(_talkContent);
        _plates.Clear();
        MessageFace talkFace = MessageRoster.Find(_talkId);
        bool group = talkFace == null || talkFace.PersonId == 0;
        List<ThreadLine> lines = _game.Messages.Thread(_talkId);
        for (int i = 0; i < lines.Count; i++)
            CreateBubble(lines[i], group);
        PaintPrompt();
        Canvas.ForceUpdateCanvases();
        if (_talkScroll != null)
            _talkScroll.verticalNormalizedPosition = 0f;
        UpdateQuestBanners();
    }

    void CreateBubble(ThreadLine line, bool group)
    {
        bool player = line.FromPlayer;
        bool showName = group && !player && line.Kind == MessageLineKind.Text && !string.IsNullOrEmpty(line.SpeakerName);
        float height = BubbleHeight(line.Text, line.Kind == MessageLineKind.Stamp);
        if (showName)
            height += 28f;
        if (!player && line.Kind != MessageLineKind.Quest && height < 100f)
            height = 100f;
        GameObject row = new GameObject("Line_" + line.LineId.ToString(), typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(_talkContent, false);
        LayoutElement layout = row.GetComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        RectTransform rowRect = row.GetComponent<RectTransform>();

        if (!player && line.Kind != MessageLineKind.Quest)
        {
            Image icon = CreateImage("Icon", row.transform, Color.white);
            PaintFace(icon, MessageRoster.Find(line.SpeakerId), line.IconFile);
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(8f, -8f);
            iconRect.sizeDelta = new Vector2(83f, 83f);
        }

        if (line.Kind == MessageLineKind.Quest)
        {
            CreateQuestPlate(line, rowRect);
            return;
        }
        if (line.Kind == MessageLineKind.Stamp)
        {
            Image stamp = CreateImage("Stamp", row.transform, Color.white);
            Sprite sprite = MessageImages.Stamp(line.Text);
            RectTransform stampRect = stamp.rectTransform;
            stampRect.anchorMin = new Vector2(0f, 0.5f);
            stampRect.anchorMax = new Vector2(0f, 0.5f);
            stampRect.pivot = new Vector2(0f, 0.5f);
            stampRect.anchoredPosition = new Vector2(103f, 0f);
            stampRect.sizeDelta = new Vector2(256f, 256f);
            if (sprite != null)
                stamp.sprite = sprite;
            else
            {
                stamp.color = new Color(0.9f, 0.92f, 0.95f, 1f);
                Text missing = CreateText("Text", stamp.transform, 21, TextAnchor.MiddleCenter);
                missing.text = line.Text;
                missing.color = new Color(0.3f, 0.32f, 0.36f, 1f);
                Stretch(missing.rectTransform);
            }
            return;
        }

        Image bubble = CreateImage("Bubble", row.transform, player
            ? new Color(0.68f, 0.9f, 0.62f, 1f)
            : Color.white);
        RectTransform bubbleRect = bubble.rectTransform;
        if (player)
        {
            bubbleRect.anchorMin = new Vector2(1f, 0f);
            bubbleRect.anchorMax = new Vector2(1f, 1f);
            bubbleRect.pivot = new Vector2(1f, 0.5f);
            bubbleRect.offsetMin = new Vector2(-620f, 8f);
            bubbleRect.offsetMax = new Vector2(-16f, -8f);
        }
        else
        {
            bubbleRect.anchorMin = new Vector2(0f, 0f);
            bubbleRect.anchorMax = new Vector2(0f, 1f);
            bubbleRect.pivot = new Vector2(0f, 0.5f);
            bubbleRect.anchoredPosition = new Vector2(103f, 0f);
            bubbleRect.sizeDelta = new Vector2(601f, -16f);
            bubbleRect.offsetMin = new Vector2(103f, 8f);
            bubbleRect.offsetMax = new Vector2(704f, -8f);
        }

        const float pad = 16f;
        float textTop = pad;
        if (showName)
        {
            Text name = CreateText("Name", bubble.transform, 23, TextAnchor.UpperLeft);
            name.text = line.SpeakerName;
            name.color = new Color(0.2f, 0.45f, 0.75f, 1f);
            RectTransform nameRect = name.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0f, -pad);
            nameRect.sizeDelta = new Vector2(-24f, 28f);
            textTop = pad + 28f;
        }

        Text body = CreateText("Text", bubble.transform, 27, TextAnchor.MiddleLeft);
        body.text = line.Text;
        body.color = new Color(0.12f, 0.14f, 0.18f, 1f);
        body.alignByGeometry = true;
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(16f, pad);
        bodyRect.offsetMax = new Vector2(-16f, -textTop);
    }

    void CreateQuestPlate(ThreadLine line, RectTransform rowRect)
    {
        Image bubble = CreateImage("Bubble", rowRect, new Color(0.73f, 0.75f, 0.78f, 1f));
        bubble.sprite = MessageImages.Round();
        bubble.type = Image.Type.Sliced;
        RectTransform bubbleRect = bubble.rectTransform;
        float width = QuestWidth(line.Text);
        bubbleRect.anchorMin = new Vector2(0.5f, 0f);
        bubbleRect.anchorMax = new Vector2(0.5f, 1f);
        bubbleRect.pivot = new Vector2(0.5f, 0.5f);
        bubbleRect.offsetMin = new Vector2(-width * 0.5f, 6f);
        bubbleRect.offsetMax = new Vector2(width * 0.5f, -6f);

        Text body = CreateText("Text", bubble.transform, 27, TextAnchor.MiddleCenter);
        body.text = line.Text;
        body.color = new Color(0.18f, 0.2f, 0.24f, 1f);
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(20f, 8f);
        bodyRect.offsetMax = new Vector2(-20f, -8f);

        PlateView plate = new PlateView();
        plate.Rect = rowRect;
        plate.Open = line.QuestOpen;
        _plates.Add(plate);
    }

    static float QuestWidth(string text)
    {
        int longest = 1;
        int current = 0;
        if (!string.IsNullOrEmpty(text))
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    if (current > longest)
                        longest = current;
                    current = 0;
                }
                else
                    current++;
            }
            if (current > longest)
                longest = current;
        }
        float width = 64f + longest * 26f;
        if (width < 240f)
            width = 240f;
        if (width > 640f)
            width = 640f;
        return width;
    }

    void PaintPrompt()
    {
        SendPrompt prompt = _game != null && _game.Messages != null ? _game.Messages.Prompt(_talkId) : new SendPrompt();
        if (_inputText != null)
        {
            _inputText.text = prompt.Text;
            _inputText.color = prompt.Enabled
                ? new Color(0.12f, 0.14f, 0.18f, 1f)
                : new Color(0.45f, 0.48f, 0.52f, 1f);
        }
        if (_inputWarning != null)
            _inputWarning.text = prompt.Warning ?? "";
        if (_send != null)
        {
            _send.interactable = prompt.Enabled;
            Image image = _send.GetComponent<Image>();
            if (image != null)
                image.color = prompt.Enabled
                    ? new Color(0.28f, 0.62f, 0.38f, 1f)
                    : new Color(0.62f, 0.66f, 0.7f, 1f);
        }
    }

    void PushSend()
    {
        if (_game == null || _game.Messages == null)
            return;
        bool sent = _game.Messages.TrySend(_talkId);
        if (_game.Messages.IsUnread(_talkId))
        {
            _game.Messages.MarkRead(_talkId);
            _game.NoteMessageRead();
        }
        FillTalk();
        FillList();
        _game.RefreshMessageButton();
        if (!sent)
            PaintPrompt();
    }

    void PushAccept(string talkId, int offerId)
    {
        if (_game == null || _game.Messages == null)
            return;
        _game.Messages.TryAccept(talkId, offerId);
        FillList();
        _game.RefreshMessageButton();
    }

    void UpdateQuestBanners()
    {
        bool above = false;
        bool below = false;
        if (_talkScroll != null && _talkScroll.viewport != null)
        {
            RectTransform view = _talkScroll.viewport;
            Vector3[] viewCorners = new Vector3[4];
            view.GetWorldCorners(viewCorners);
            float viewBottom = viewCorners[0].y;
            float viewTop = viewCorners[1].y;
            for (int i = 0; i < _plates.Count; i++)
            {
                PlateView plate = _plates[i];
                if (plate == null || plate.Rect == null || !plate.Open)
                    continue;
                Vector3[] corners = new Vector3[4];
                plate.Rect.GetWorldCorners(corners);
                float bottom = corners[0].y;
                float top = corners[1].y;
                if (bottom <= viewTop && top >= viewBottom)
                    continue;
                if (bottom > viewTop)
                    above = true;
                if (top < viewBottom)
                    below = true;
            }
        }
        if (_jumpTop != null)
            _jumpTop.gameObject.SetActive(above);
        if (_jumpBottom != null)
            _jumpBottom.gameObject.SetActive(below);
    }

    void FocusPlate(bool above)
    {
        if (_talkScroll == null || _talkScroll.viewport == null || _talkScroll.content == null)
            return;
        RectTransform view = _talkScroll.viewport;
        Vector3[] viewCorners = new Vector3[4];
        view.GetWorldCorners(viewCorners);
        RectTransform target = null;
        float best = above ? float.MaxValue : float.MinValue;
        for (int i = 0; i < _plates.Count; i++)
        {
            PlateView plate = _plates[i];
            if (plate == null || plate.Rect == null || !plate.Open)
                continue;
            Vector3[] corners = new Vector3[4];
            plate.Rect.GetWorldCorners(corners);
            if (above && corners[0].y > viewCorners[1].y && corners[0].y < best)
            {
                best = corners[0].y;
                target = plate.Rect;
            }
            if (!above && corners[1].y < viewCorners[0].y && corners[1].y > best)
            {
                best = corners[1].y;
                target = plate.Rect;
            }
        }
        if (target == null)
            return;
        Canvas.ForceUpdateCanvases();
        Bounds viewBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(view, view);
        Bounds plateBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(view, target);
        float delta = above ? viewBounds.max.y - plateBounds.max.y : viewBounds.min.y - plateBounds.min.y;
        Vector2 position = _talkScroll.content.anchoredPosition;
        position.y += delta;
        _talkScroll.content.anchoredPosition = position;
        UpdateQuestBanners();
    }

    static float BubbleHeight(string text, bool stamp)
    {
        if (stamp)
            return 276f;
        return 44f + CountLines(text) * 40f;
    }

    static int CountLines(string text)
    {
        const int perLine = 18;
        if (string.IsNullOrEmpty(text))
            return 1;
        int lines = 0;
        int column = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lines += column == 0 ? 1 : (column + perLine - 1) / perLine;
                column = 0;
                continue;
            }
            column++;
        }
        if (column > 0)
            lines += (column + perLine - 1) / perLine;
        if (lines < 1)
            lines = 1;
        return lines;
    }

    RectTransform CreateScroll(string name, Transform parent, float top, float bottom)
    {
        GameObject scrollObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);
        scrollObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(0f, bottom);
        scrollRect.offsetMax = new Vector2(0f, -top);
        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        Stretch(viewport.GetComponent<RectTransform>());
        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;
        return contentRect;
    }

    Button CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        Text text = CreateText("Text", obj.transform, 27, TextAnchor.MiddleCenter);
        text.text = label;
        text.color = Color.white;
        text.raycastTarget = false;
        Stretch(text.rectTransform);
        return obj.GetComponent<Button>();
    }

    Text CreateText(string name, Transform parent, int size, TextAnchor anchor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    static void PaintFace(Image icon, MessageFace face, string iconFile)
    {
        if (icon == null)
            return;
        Sprite sprite = null;
        if (face != null && face.HasIconColor)
            sprite = MessageImages.Portrait(string.IsNullOrEmpty(iconFile) ? face.IconFile : iconFile, face.IconColor);
        if (sprite == null && !string.IsNullOrEmpty(iconFile))
            sprite = MessageImages.Icon(iconFile);
        if (sprite != null)
        {
            icon.sprite = sprite;
            icon.color = Color.white;
            return;
        }
        icon.sprite = MessageImages.Circle();
        icon.color = new Color(0.75f, 0.77f, 0.8f, 1f);
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    static void Clear(RectTransform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(content.GetChild(i).gameObject);
    }

    static void PlaceTop(RectTransform rect, float yFromTop, float x, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -yFromTop);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void PlaceBand(RectTransform rect, bool top)
    {
        rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rect.sizeDelta = new Vector2(-32f, 52f);
        rect.anchoredPosition = new Vector2(0f, top ? -Header : InputBar + 8f);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    sealed class PlateView
    {
        public RectTransform Rect;
        public bool Open;
    }
}
