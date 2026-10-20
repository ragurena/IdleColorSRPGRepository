using System;
using UnityEngine;
using UnityEngine.UI;

// クエストの下のボタンから開く設定。バトル速度の上限と、広告を見る前の確認を選ぶ。
public sealed class SettingsBoard
{
    static readonly int[] Caps = { 2, 4, 8, 16 };
    static readonly Color Selected = new Color(0.25f, 0.45f, 0.75f, 1f);
    static readonly Color Idle = new Color(0.18f, 0.20f, 0.26f, 1f);

    Font _font;
    GameObject _root;
    Action<int> _onCap;
    Action<bool> _onAdConfirm;
    Action _onSave;
    Action _onDeleteSave;
    int _cap = 16;
    bool _adConfirm = true;
    readonly Button[] _choices = new Button[Caps.Length];
    readonly Button[] _adChoices = new Button[2];
    Text _saveStatus;

    public bool IsOpen
    {
        get { return _root != null && _root.activeSelf; }
    }

    public void Build(Transform parent, Font font, Action<int> onCap, Action<bool> onAdConfirm, Action onSave, Action onDeleteSave)
    {
        _font = font;
        _onCap = onCap;
        _onAdConfirm = onAdConfirm;
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
            choice.onClick.AddListener(() => Select(cap));
            _choices[i] = choice;
        }

        Text note = CreateText("TextSettingsSpeedNote", _root.transform, 22, TextAnchor.MiddleCenter);
        note.text = "バトルの速度ボタンは、選んだ倍率まで切り替わります。";
        note.color = new Color(0.78f, 0.80f, 0.84f, 1f);
        StretchTop(note.rectTransform, 24f, 24f, 292f, 72f);

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

        Button save = CreateButton("ButtonSettingsSave", _root.transform, "セーブ", new Color(0.25f, 0.45f, 0.75f, 1f));
        PlaceRow(save.GetComponent<RectTransform>(), 0, 2, 700f, 96f);
        Text saveLabel = save.GetComponentInChildren<Text>();
        if (saveLabel != null)
            saveLabel.fontSize = 28;
        save.onClick.AddListener(() =>
        {
            if (_onSave != null)
                _onSave();
        });

        Button deleteSave = CreateButton("ButtonSettingsDeleteSave", _root.transform, "セーブ削除", new Color(0.75f, 0.28f, 0.24f, 1f));
        PlaceRow(deleteSave.GetComponent<RectTransform>(), 1, 2, 700f, 96f);
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
        StretchTop(_saveStatus.rectTransform, 24f, 24f, 812f, 48f);

        Paint();
        PaintAdConfirm();
        _root.SetActive(false);
    }

    public void Open(int cap, bool adConfirm)
    {
        if (_root == null)
            return;
        _cap = Normalize(cap);
        _adConfirm = adConfirm;
        _root.SetActive(true);
        Transform overlay = _root.transform.parent;
        if (overlay != null && overlay.parent != null)
            overlay.SetAsLastSibling();
        _root.transform.SetAsLastSibling();
        Paint();
        PaintAdConfirm();
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

    void Select(int cap)
    {
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
            Image image = choice.GetComponent<Image>();
            if (image != null)
                image.color = Caps[i] == _cap ? Selected : Idle;
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

    static int Normalize(int cap)
    {
        if (cap == 2 || cap == 4 || cap == 8 || cap == 16)
            return cap;
        return 16;
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
