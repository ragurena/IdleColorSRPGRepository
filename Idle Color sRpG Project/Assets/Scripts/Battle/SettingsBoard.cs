using System;
using UnityEngine;
using UnityEngine.UI;

// クエストの下のボタンから開く設定。バトル速度の上限、広告を見る前の確認、画面の自動消灯、バトル後の画面を選ぶ。
// 2倍と4倍は最初から選べる。8倍は広告で1時間。16倍は未開放で、課金解放はまだ無い。
public sealed class SettingsBoard
{
    static readonly int[] Caps = { 2, 4, 8, 16 };
    static readonly Color Selected = new Color(0.25f, 0.45f, 0.75f, 1f);
    static readonly Color Idle = new Color(0.18f, 0.20f, 0.26f, 1f);
    static readonly Color Locked = new Color(0.22f, 0.22f, 0.24f, 1f);
    static readonly Color LockedText = new Color(0.48f, 0.48f, 0.50f, 1f);

    Font _font;
    GameObject _root;
    Action<int> _onCap;
    Action _onWatchSpeed8;
    Action<bool> _onAdConfirm;
    Action<bool> _onKeepAwake;
    Action<bool> _onReturnToStage;
    Action _onSave;
    Action _onDeleteSave;
    int _cap = 4;
    bool _speed8Active;
    int _remainMinute = -1;
    bool _adConfirm = true;
    bool _keepAwake = true;
    bool _returnToStage = true;
    readonly Button[] _choices = new Button[Caps.Length];
    Image _speed8Icon;
    Text _speedNote;
    readonly Button[] _adChoices = new Button[2];
    readonly Button[] _awakeChoices = new Button[2];
    readonly Button[] _returnChoices = new Button[2];
    Text _saveStatus;

    public bool IsOpen
    {
        get { return _root != null && _root.activeSelf; }
    }

    // RELEASE_REMOVE_TEST_GRANTS: リリース用ビルドの前に、このテストボタンと呼び出しを消す。
    public void Build(Transform parent, Font font, Action<int> onCap, Action onWatchSpeed8, Action<bool> onAdConfirm, Action<bool> onKeepAwake, Action<bool> onReturnToStage, Action onSave, Action onDeleteSave, Action onAddRgb, Action onAddPixels, Action onAddOwned)
    {
        _font = font;
        _onCap = onCap;
        _onWatchSpeed8 = onWatchSpeed8;
        _onAdConfirm = onAdConfirm;
        _onKeepAwake = onKeepAwake;
        _onReturnToStage = onReturnToStage;
        _onSave = onSave;
        _onDeleteSave = onDeleteSave;
        _root = new GameObject("PanelSettings", typeof(RectTransform), typeof(Image));
        _root.transform.SetParent(parent, false);
        Image background = _root.GetComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.09f, 0.96f);
        Stretch(_root.GetComponent<RectTransform>());

        Text title = CreateText("TextSettingsTitle", _root.transform, 32, TextAnchor.MiddleCenter);
        title.text = "設定";
        PlaceTop(title.rectTransform, 16f, 0f, 240f, 48f);

        Button close = CreateButton("ButtonSettingsClose", _root.transform, "閉じる", new Color(0.75f, 0.28f, 0.24f, 1f));
        PlaceTop(close.GetComponent<RectTransform>(), 12f, 430f, 140f, 68f);
        close.onClick.AddListener(Close);

        Text label = CreateText("TextSettingsSpeedCap", _root.transform, 26, TextAnchor.MiddleCenter);
        label.text = "バトル速度の上限";
        StretchTop(label.rectTransform, 24f, 24f, 110f, 48f);

        for (int i = 0; i < Caps.Length; i++)
        {
            int cap = Caps[i];
            Button choice = CreateButton("ButtonSettingsSpeed" + cap.ToString(), _root.transform, cap.ToString() + "倍", Idle);
            PlaceRow(choice.GetComponent<RectTransform>(), i, Caps.Length, 180f, 96f);
            Text choiceLabel = choice.GetComponentInChildren<Text>();
            if (choiceLabel != null)
                choiceLabel.fontSize = 28;
            ColorBlock colors = choice.colors;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0f;
            choice.colors = colors;
            choice.onClick.AddListener(() => Select(cap));
            if (cap == 8)
                _speed8Icon = CreateVideoIcon(choice.transform);
            _choices[i] = choice;
        }

        _speedNote = CreateText("TextSettingsSpeedNote", _root.transform, 22, TextAnchor.MiddleCenter);
        _speedNote.color = new Color(0.78f, 0.80f, 0.84f, 1f);
        StretchTop(_speedNote.rectTransform, 24f, 24f, 292f, 80f);

        Text adLabel = CreateText("TextSettingsAdConfirm", _root.transform, 26, TextAnchor.MiddleCenter);
        adLabel.text = "広告視聴の確認";
        StretchTop(adLabel.rectTransform, 24f, 24f, 380f, 48f);

        Button confirmOn = CreateButton("ButtonSettingsAdConfirmOn", _root.transform, "確認する", Idle);
        PlaceRow(confirmOn.GetComponent<RectTransform>(), 0, 2, 444f, 96f);
        Text confirmOnLabel = confirmOn.GetComponentInChildren<Text>();
        if (confirmOnLabel != null)
            confirmOnLabel.fontSize = 28;
        confirmOn.onClick.AddListener(() => SelectAdConfirm(true));
        _adChoices[0] = confirmOn;

        Button confirmOff = CreateButton("ButtonSettingsAdConfirmOff", _root.transform, "確認しない", Idle);
        PlaceRow(confirmOff.GetComponent<RectTransform>(), 1, 2, 444f, 96f);
        Text confirmOffLabel = confirmOff.GetComponentInChildren<Text>();
        if (confirmOffLabel != null)
            confirmOffLabel.fontSize = 28;
        confirmOff.onClick.AddListener(() => SelectAdConfirm(false));
        _adChoices[1] = confirmOff;

        Text adNote = CreateText("TextSettingsAdBonus", _root.transform, 22, TextAnchor.MiddleCenter);
        adNote.text = ControllerProduction.AdWatchBonusNote;
        adNote.color = new Color(0.78f, 0.80f, 0.84f, 1f);
        StretchTop(adNote.rectTransform, 24f, 24f, 552f, 96f);

        Text awakeLabel = CreateText("TextSettingsKeepAwake", _root.transform, 26, TextAnchor.MiddleCenter);
        awakeLabel.text = "スリープしない";
        StretchTop(awakeLabel.rectTransform, 24f, 24f, 660f, 48f);

        Text awakeNote = CreateText("TextSettingsKeepAwakeNote", _root.transform, 22, TextAnchor.MiddleCenter);
        awakeNote.text = "ONのあいだは、操作していなくても画面が消えません。OFFは端末の設定どおりです。";
        awakeNote.color = new Color(0.78f, 0.80f, 0.84f, 1f);
        StretchTop(awakeNote.rectTransform, 24f, 24f, 712f, 72f);

        Button awakeOn = CreateButton("ButtonSettingsKeepAwakeOn", _root.transform, "ON", Idle);
        PlaceRow(awakeOn.GetComponent<RectTransform>(), 0, 2, 796f, 96f);
        Text awakeOnLabel = awakeOn.GetComponentInChildren<Text>();
        if (awakeOnLabel != null)
            awakeOnLabel.fontSize = 28;
        awakeOn.onClick.AddListener(() => SelectKeepAwake(true));
        _awakeChoices[0] = awakeOn;

        Button awakeOff = CreateButton("ButtonSettingsKeepAwakeOff", _root.transform, "OFF", Idle);
        PlaceRow(awakeOff.GetComponent<RectTransform>(), 1, 2, 796f, 96f);
        Text awakeOffLabel = awakeOff.GetComponentInChildren<Text>();
        if (awakeOffLabel != null)
            awakeOffLabel.fontSize = 28;
        awakeOff.onClick.AddListener(() => SelectKeepAwake(false));
        _awakeChoices[1] = awakeOff;

        Text returnLabel = CreateText("TextSettingsReturnStage", _root.transform, 26, TextAnchor.MiddleCenter);
        returnLabel.text = "バトル終了時バトル設定画面にする";
        StretchTop(returnLabel.rectTransform, 24f, 24f, 916f, 56f);

        Button returnOn = CreateButton("ButtonSettingsReturnStageOn", _root.transform, "ON", Idle);
        PlaceRow(returnOn.GetComponent<RectTransform>(), 0, 2, 980f, 96f);
        Text returnOnLabel = returnOn.GetComponentInChildren<Text>();
        if (returnOnLabel != null)
            returnOnLabel.fontSize = 28;
        returnOn.onClick.AddListener(() => SelectReturnToStage(true));
        _returnChoices[0] = returnOn;

        Button returnOff = CreateButton("ButtonSettingsReturnStageOff", _root.transform, "OFF", Idle);
        PlaceRow(returnOff.GetComponent<RectTransform>(), 1, 2, 980f, 96f);
        Text returnOffLabel = returnOff.GetComponentInChildren<Text>();
        if (returnOffLabel != null)
            returnOffLabel.fontSize = 28;
        returnOff.onClick.AddListener(() => SelectReturnToStage(false));
        _returnChoices[1] = returnOff;

        Button save = CreateButton("ButtonSettingsSave", _root.transform, "セーブ", new Color(0.25f, 0.45f, 0.75f, 1f));
        PlaceRow(save.GetComponent<RectTransform>(), 0, 2, 1100f, 96f);
        Text saveLabel = save.GetComponentInChildren<Text>();
        if (saveLabel != null)
            saveLabel.fontSize = 28;
        save.onClick.AddListener(() =>
        {
            if (_onSave != null)
                _onSave();
        });

        Button deleteSave = CreateButton("ButtonSettingsDeleteSave", _root.transform, "セーブ削除", new Color(0.75f, 0.28f, 0.24f, 1f));
        PlaceRow(deleteSave.GetComponent<RectTransform>(), 1, 2, 1100f, 96f);
        Text deleteLabel = deleteSave.GetComponentInChildren<Text>();
        if (deleteLabel != null)
            deleteLabel.fontSize = 28;
        deleteSave.onClick.AddListener(() =>
        {
            if (_onDeleteSave != null)
                _onDeleteSave();
        });

        _saveStatus = CreateText("TextSettingsSaveStatus", _root.transform, 22, TextAnchor.MiddleCenter);
        _saveStatus.text = "";
        _saveStatus.color = new Color(0.78f, 0.80f, 0.84f, 1f);
        StretchTop(_saveStatus.rectTransform, 24f, 24f, 1212f, 48f);

        // RELEASE_REMOVE_TEST_GRANTS: 設定画面下部のテストボタン。リリース用ビルドの前に消す。
        Text testLabel = CreateText("TextSettingsTest", _root.transform, 22, TextAnchor.MiddleCenter);
        testLabel.text = "テスト用";
        testLabel.color = new Color(0.95f, 0.72f, 0.45f, 1f);
        PlaceBottomLabel(testLabel.rectTransform, 132f);
        PlaceTestButton("ButtonSettingsTestRgb", "RGB+10000", 0, onAddRgb);
        PlaceTestButton("ButtonSettingsTestPixels", "全ピクセル+100", 1, onAddPixels);
        PlaceTestButton("ButtonSettingsTestOwned", "所持数+10", 2, onAddOwned);

        Paint();
        PaintAdConfirm();
        PaintKeepAwake();
        PaintReturnToStage();
        _root.SetActive(false);
    }

    public void Open(int cap, bool adConfirm, bool keepAwake, bool returnToStage, bool speed8Active, long speed8RemainMs)
    {
        if (_root == null)
            return;
        _cap = Normalize(cap);
        _speed8Active = speed8Active;
        _remainMinute = RemainMinute(speed8RemainMs);
        _adConfirm = adConfirm;
        _keepAwake = keepAwake;
        _returnToStage = returnToStage;
        _root.SetActive(true);
        Transform overlay = _root.transform.parent;
        if (overlay != null && overlay.parent != null)
            overlay.SetAsLastSibling();
        _root.transform.SetAsLastSibling();
        Paint();
        PaintAdConfirm();
        PaintKeepAwake();
        PaintReturnToStage();
        SetSaveStatus("");
    }

    public void SetSaveStatus(string message)
    {
        if (_saveStatus != null)
            _saveStatus.text = message ?? "";
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    public void SyncSpeed(int cap, bool speed8Active, long speed8RemainMs)
    {
        if (_root == null || !_root.activeSelf)
            return;
        int shown = Normalize(cap);
        int minute = RemainMinute(speed8RemainMs);
        if (shown == _cap && speed8Active == _speed8Active && minute == _remainMinute)
            return;
        _cap = shown;
        _speed8Active = speed8Active;
        _remainMinute = minute;
        Paint();
    }

    void Select(int cap)
    {
        if (cap == 16)
            return;
        if (cap == 8 && !_speed8Active)
        {
            if (_onWatchSpeed8 != null)
                _onWatchSpeed8();
            return;
        }
        _cap = Normalize(cap);
        Paint();
        if (_onCap != null)
            _onCap(_cap);
    }

    void Paint()
    {
        for (int i = 0; i < _choices.Length; i++)
        {
            Button choice = _choices[i];
            if (choice == null)
                continue;
            int cap = Caps[i];
            Image image = choice.GetComponent<Image>();
            Text label = choice.GetComponentInChildren<Text>();
            if (cap == 16)
            {
                choice.interactable = false;
                if (image != null)
                    image.color = Locked;
                if (label != null)
                    label.color = LockedText;
                continue;
            }
            choice.interactable = true;
            bool offerAd = cap == 8 && !_speed8Active;
            if (image != null)
                image.color = offerAd ? Idle : (cap == _cap ? Selected : Idle);
            if (label != null)
                label.color = Color.white;
        }
        if (_speed8Icon != null)
            _speed8Icon.gameObject.SetActive(!_speed8Active);
        if (_speedNote != null)
        {
            if (_speed8Active)
                _speedNote.text = "バトルの速度ボタンは、選んだ倍率まで切り替わります。\n8倍速の時間は、戦闘で8倍にしているあいだだけ減ります。";
            else
                _speedNote.text = "バトルの速度ボタンは、選んだ倍率まで切り替わります。\n8倍速は広告で1時間だけ使えます。16倍速は未開放です。";
        }
    }

    void SelectAdConfirm(bool confirm)
    {
        _adConfirm = confirm;
        PaintAdConfirm();
        if (_onAdConfirm != null)
            _onAdConfirm(_adConfirm);
    }

    void PaintAdConfirm()
    {
        for (int i = 0; i < _adChoices.Length; i++)
        {
            Button choice = _adChoices[i];
            if (choice == null)
                continue;
            Image image = choice.GetComponent<Image>();
            bool selected = i == 0 ? _adConfirm : !_adConfirm;
            if (image != null)
                image.color = selected ? Selected : Idle;
        }
    }

    void SelectKeepAwake(bool keep)
    {
        _keepAwake = keep;
        PaintKeepAwake();
        if (_onKeepAwake != null)
            _onKeepAwake(_keepAwake);
    }

    void PaintKeepAwake()
    {
        for (int i = 0; i < _awakeChoices.Length; i++)
        {
            Button choice = _awakeChoices[i];
            if (choice == null)
                continue;
            Image image = choice.GetComponent<Image>();
            bool selected = i == 0 ? _keepAwake : !_keepAwake;
            if (image != null)
                image.color = selected ? Selected : Idle;
        }
    }

    void SelectReturnToStage(bool enabled)
    {
        _returnToStage = enabled;
        PaintReturnToStage();
        if (_onReturnToStage != null)
            _onReturnToStage(_returnToStage);
    }

    void PaintReturnToStage()
    {
        for (int i = 0; i < _returnChoices.Length; i++)
        {
            Button choice = _returnChoices[i];
            if (choice == null)
                continue;
            Image image = choice.GetComponent<Image>();
            bool selected = i == 0 ? _returnToStage : !_returnToStage;
            if (image != null)
                image.color = selected ? Selected : Idle;
        }
    }

    static int Normalize(int cap)
    {
        if (cap == 2 || cap == 4 || cap == 8)
            return cap;
        return 4;
    }

    static int RemainMinute(long remainMs)
    {
        if (remainMs <= 0)
            return 0;
        long minute = (remainMs + 59999L) / 60000L;
        if (minute < 1)
            return 1;
        if (minute > int.MaxValue)
            return int.MaxValue;
        return (int)minute;
    }

    Image CreateVideoIcon(Transform parent)
    {
        GameObject obj = new GameObject("ImageSettingsSpeedAd", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = VideoIconSprite();
        image.color = new Color(1f, 1f, 1f, 0.55f);
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-8f, -8f);
        rect.sizeDelta = new Vector2(32f, 32f);
        return image;
    }

    static Sprite _videoIcon;

    static Sprite VideoIconSprite()
    {
        if (_videoIcon != null)
            return _videoIcon;
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color ink = new Color(1f, 1f, 1f, 1f);
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;
        StrokeRoundRect(pixels, size, 6, 14, 57, 49, 6, 2, ink);
        FillTriangle(pixels, size, 26f, 24f, 26f, 40f, 42f, 32f, ink);
        texture.SetPixels(pixels);
        texture.Apply();
        _videoIcon = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return _videoIcon;
    }

    static void StrokeRoundRect(Color[] pixels, int size, int left, int bottom, int right, int top, int radius, int width, Color color)
    {
        for (int y = bottom; y <= top; y++)
        {
            for (int x = left; x <= right; x++)
            {
                if (!InRoundRect(x, y, left, bottom, right, top, radius))
                    continue;
                bool inner = InRoundRect(x, y, left + width, bottom + width, right - width, top - width, radius - width);
                if (!inner)
                    pixels[y * size + x] = color;
            }
        }
    }

    static bool InRoundRect(int x, int y, int left, int bottom, int right, int top, int radius)
    {
        if (x < left || x > right || y < bottom || y > top)
            return false;
        if (radius < 1)
            return true;
        int cx = x < left + radius ? left + radius : (x > right - radius ? right - radius : x);
        int cy = y < bottom + radius ? bottom + radius : (y > top - radius ? top - radius : y);
        if (cx == x && cy == y)
            return true;
        int dx = x - cx;
        int dy = y - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    static void FillTriangle(Color[] pixels, int size, float ax, float ay, float bx, float by, float cx, float cy, Color color)
    {
        int left = Mathf.FloorToInt(Mathf.Min(ax, Mathf.Min(bx, cx)));
        int right = Mathf.CeilToInt(Mathf.Max(ax, Mathf.Max(bx, cx)));
        int bottom = Mathf.FloorToInt(Mathf.Min(ay, Mathf.Min(by, cy)));
        int top = Mathf.CeilToInt(Mathf.Max(ay, Mathf.Max(by, cy)));
        for (int y = bottom; y <= top; y++)
        {
            for (int x = left; x <= right; x++)
            {
                if (x < 0 || y < 0 || x >= size || y >= size)
                    continue;
                if (PointInTriangle(x + 0.5f, y + 0.5f, ax, ay, bx, by, cx, cy))
                    pixels[y * size + x] = color;
            }
        }
    }

    static bool PointInTriangle(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
    {
        float d1 = Sign(px, py, ax, ay, bx, by);
        float d2 = Sign(px, py, bx, by, cx, cy);
        float d3 = Sign(px, py, cx, cy, ax, ay);
        bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNeg && hasPos);
    }

    static float Sign(float px, float py, float ax, float ay, float bx, float by)
    {
        return (px - bx) * (ay - by) - (ax - bx) * (py - by);
    }

    Button CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        Button button = obj.GetComponent<Button>();
        Text text = CreateText("Text", obj.transform, 22, TextAnchor.MiddleCenter);
        text.text = label;
        text.color = Color.white;
        text.raycastTarget = false;
        Stretch(text.rectTransform);
        return button;
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

    void PlaceTestButton(string name, string label, int index, Action onClick)
    {
        Button button = CreateButton(name, _root.transform, label, new Color(0.42f, 0.32f, 0.14f, 1f));
        RectTransform rect = button.GetComponent<RectTransform>();
        float slot = 1f / 3f;
        rect.anchorMin = new Vector2(index * slot, 0f);
        rect.anchorMax = new Vector2((index + 1f) * slot, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        float left = index == 0 ? 24f : 8f;
        float right = index == 2 ? 24f : 8f;
        rect.offsetMin = new Vector2(left, 28f);
        rect.offsetMax = new Vector2(-right, 116f);
        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
            text.fontSize = 24;
        button.onClick.AddListener(() =>
        {
            if (onClick != null)
                onClick();
        });
    }

    static void PlaceBottomLabel(RectTransform rect, float yFromBottom)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(24f, yFromBottom);
        rect.offsetMax = new Vector2(-24f, yFromBottom + 36f);
    }

    static void PlaceTop(RectTransform rect, float yFromTop, float x, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -yFromTop);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void PlaceRow(RectTransform rect, int index, int count, float yFromTop, float height)
    {
        float slot = 1f / count;
        rect.anchorMin = new Vector2(index * slot, 1f);
        rect.anchorMax = new Vector2((index + 1) * slot, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        float left = index == 0 ? 24f : 8f;
        float right = index == count - 1 ? 24f : 8f;
        rect.offsetMin = new Vector2(left, -(yFromTop + height));
        rect.offsetMax = new Vector2(-right, -yFromTop);
    }

    static void StretchTop(RectTransform rect, float left, float right, float yFromTop, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -(yFromTop + height));
        rect.offsetMax = new Vector2(-right, -yFromTop);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
