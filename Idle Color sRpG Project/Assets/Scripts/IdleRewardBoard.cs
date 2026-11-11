using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 放置の案内と、オフライン報酬のポップアップ。計算は IdleRewardState、付与は ControllerProduction。
public class IdleRewardBoard : MonoBehaviour
{
    ControllerProduction _game;
    IdleTouchLog _touches = new IdleTouchLog();
    Font _font;
    Text _banner;
    GameObject _idlePopup;
    Text _idleBody;
    Image _idleOrbIcon;
    Text _idleOrbName;
    GameObject _offlinePopup;
    Text _offlineBody;
    RectTransform _offlineList;
    Button _offlineAd;
    Button _offlineClose;
    bool _idleAdBusy;
    bool _offlineAdBusy;
    bool _offlineClaimed;
    long _shownBackgroundMs;
    readonly List<RaycastResult> _hits = new List<RaycastResult>();

    public void Initialize(ControllerProduction game)
    {
        _game = game;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        BuildBanner();
    }

    public void OfferSavedOffline()
    {
        if (_game == null || _game.Idle == null)
            return;
        long now = HospitalState.NowUnixMs();
        long from = _game.Idle.BackgroundUnixMs;
        if (from <= 0 || from == _game.Idle.ClaimedBackgroundUnixMs)
            return;
        if (_shownBackgroundMs == from && _offlinePopup != null && _offlinePopup.activeSelf)
            return;
        long elapsed = _game.Idle.OfferElapsedMs;
        if (elapsed <= 0)
        {
            bool back;
            bool jump;
            elapsed = IdleRewardState.ElapsedMs(from, now, out back, out jump);
            if (back)
                _game.Idle.ClockWentBack = true;
            if (jump)
                _game.Idle.ClockJumped = true;
            _game.Idle.OfferElapsedMs = elapsed;
        }
        int cycles = IdleRewardState.RewardCycles(elapsed, _game.Idle.OfflineCapMinutes());
        if (cycles < 1)
        {
            _game.Idle.BackgroundUnixMs = 0;
            _game.Idle.OfferElapsedMs = 0;
            _game.SaveGame();
            return;
        }
        _game.SaveGame();
        _shownBackgroundMs = from;
        _offlineClaimed = false;
        ShowOffline(elapsed, cycles);
    }

    void Update()
    {
        if (_game == null || _game.Idle == null)
            return;
        NotePointer();
        if (_offlinePopup != null && _offlinePopup.activeSelf)
        {
            if (_idlePopup != null)
                _idlePopup.SetActive(false);
            if (_banner != null)
                _banner.gameObject.SetActive(false);
            return;
        }
        long now = HospitalState.NowUnixMs();
        long elapsed = now - _game.Idle.LastInputUnixMs;
        if (elapsed < 0)
            elapsed = 0;
        long capMs = (long)_game.Idle.ActiveCapMinutes() * 60000L;
        bool popupOpen = _idlePopup != null && _idlePopup.activeSelf;
        if (!popupOpen && elapsed >= capMs)
        {
            _game.Idle.Limited = true;
            ShowIdlePopup();
        }
        bool warn = !popupOpen && elapsed >= (long)IdleRewardState.WarnAfterMinutes * 60000L && elapsed < capMs;
        if (_banner != null)
        {
            _banner.gameObject.SetActive(warn);
            if (warn)
            {
                long leftMs = capMs - elapsed;
                long leftMin = (leftMs + 59999L) / 60000L;
                if (leftMin < 1)
                    leftMin = 1;
                _banner.text = "放置中…あと" + leftMin.ToString() + "分で放置効率が10％に減少します";
            }
        }
    }

    void NotePointer()
    {
        if (PopupOpen())
            return;
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame)
            return;
        string name = HitName(pointer.position.ReadValue());
        long now = HospitalState.NowUnixMs();
        _game.Idle.LastInputUnixMs = now;
        _touches.Record(name, now);
    }

    bool PopupOpen()
    {
        return (_idlePopup != null && _idlePopup.activeSelf)
            || (_offlinePopup != null && _offlinePopup.activeSelf);
    }

    string HitName(Vector2 screen)
    {
        EventSystem events = EventSystem.current;
        if (events == null)
            return "pointer";
        PointerEventData data = new PointerEventData(events);
        data.position = screen;
        _hits.Clear();
        events.RaycastAll(data, _hits);
        if (_hits.Count == 0 || _hits[0].gameObject == null)
            return "pointer";
        return _hits[0].gameObject.name;
    }

    void ShowIdlePopup()
    {
        EnsureIdlePopup();
        if (_idlePopup == null)
            return;
        if (_idleBody != null)
        {
            _idleBody.text = "長時間放置のため、生産や成長を10％にしています。\n"
                + "広告を見たり、課金すると、放置できる時間が伸びます。";
        }
        int orb = _game != null && _game.Idle != null ? _game.Idle.MaxSeenOrb() : ItemIds.ExpOrb8;
        if (_idleOrbIcon != null)
        {
            _idleOrbIcon.sprite = ItemImages.SpriteOf(orb);
            _idleOrbIcon.preserveAspect = true;
            _idleOrbIcon.color = Color.white;
        }
        if (_idleOrbName != null)
            _idleOrbName.text = ItemIds.DisplayName(orb);
        _idlePopup.SetActive(true);
        _idlePopup.transform.SetAsLastSibling();
    }

    void WatchIdleAd()
    {
        if (_idleAdBusy || _game == null)
            return;
        _idleAdBusy = true;
        if (!AdService.TryShowRewarded(FinishIdleAd, FailIdleAd))
            _idleAdBusy = false;
    }

    void FinishIdleAd()
    {
        if (!_idleAdBusy)
            return;
        _idleAdBusy = false;
        _game.GrantIdleWatchReward();
        HideIdle();
    }

    void FailIdleAd()
    {
        _idleAdBusy = false;
    }

    void CloseIdle()
    {
        if (_idleAdBusy)
            return;
        if (_game != null && _game.Idle != null)
        {
            _game.Idle.Limited = false;
            _game.Idle.LastInputUnixMs = HospitalState.NowUnixMs();
            _game.SaveGame();
        }
        HideIdle();
    }

    void HideIdle()
    {
        if (_idlePopup != null)
            _idlePopup.SetActive(false);
        if (_banner != null)
            _banner.gameObject.SetActive(false);
    }

    void ShowOffline(long elapsedMs, int cycles)
    {
        EnsureOfflinePopup();
        if (_offlinePopup == null || _game == null)
            return;
        int cap = _game.Idle.OfflineCapMinutes();
        bool ad = IdleRewardState.ShowOfflineAd(elapsedMs, cap);
        if (_offlineBody != null)
        {
            _offlineBody.text = "おかえりなさい！\n"
                + "アプリを閉じていた時間：" + IdleRewardState.FormatSpan(elapsedMs) + "\n"
                + "報酬計算上限：" + IdleRewardState.FormatSpan((long)cap * 60000L) + "\n\n"
                + "以下のオフライン報酬を獲得しました！";
        }
        IdlePayout payout = _game.BuildOfflinePayout(cycles, 1);
        FillOfflineList(payout);
        if (_offlineAd != null)
            _offlineAd.gameObject.SetActive(ad);
        if (_offlineClose != null)
        {
            Text label = _offlineClose.GetComponentInChildren<Text>();
            if (label != null)
                label.text = ad ? "広告を見ずに閉じる" : "閉じる";
        }
        _offlinePopup.SetActive(true);
        _offlinePopup.transform.SetAsLastSibling();
    }

    void WatchOfflineAd()
    {
        if (_offlineAdBusy || _offlineClaimed || _game == null)
            return;
        _offlineAdBusy = true;
        if (!AdService.TryShowRewarded(FinishOfflineAd, FailOfflineAd))
            _offlineAdBusy = false;
    }

    void FinishOfflineAd()
    {
        if (!_offlineAdBusy || _offlineClaimed)
            return;
        _offlineAdBusy = false;
        ClaimOffline(3, true);
    }

    void FailOfflineAd()
    {
        _offlineAdBusy = false;
    }

    void CloseOffline()
    {
        if (_offlineAdBusy)
            return;
        ClaimOffline(1, false);
    }

    void ClaimOffline(int multiplier, bool countAd)
    {
        if (_offlineClaimed || _game == null || _game.Idle == null)
            return;
        long stamp = _game.Idle.BackgroundUnixMs;
        if (stamp <= 0 || stamp == _game.Idle.ClaimedBackgroundUnixMs)
        {
            HideOffline();
            return;
        }
        _offlineClaimed = true;
        long elapsed = _game.Idle.OfferElapsedMs;
        int cycles = IdleRewardState.RewardCycles(elapsed, _game.Idle.OfflineCapMinutes());
        IdlePayout payout = _game.BuildOfflinePayout(cycles, multiplier);
        _game.Idle.ClaimedBackgroundUnixMs = stamp;
        _game.Idle.BackgroundUnixMs = 0;
        _game.Idle.OfferElapsedMs = 0;
        if (countAd)
            _game.Idle.NoteAdSuccess();
        _game.Idle.Limited = false;
        _game.Idle.LastInputUnixMs = HospitalState.NowUnixMs();
        _game.GrantOfflineReward(payout);
        _game.SaveGame();
        HideOffline();
    }

    void HideOffline()
    {
        if (_offlinePopup != null)
            _offlinePopup.SetActive(false);
    }

    void FillOfflineList(IdlePayout payout)
    {
        if (_offlineList == null)
            return;
        for (int i = _offlineList.childCount - 1; i >= 0; i--)
            Destroy(_offlineList.GetChild(i).gameObject);
        List<RectTransform> tiles = new List<RectTransform>();
        if (payout.OrbSizes != null)
        {
            for (int i = 0; i < payout.OrbSizes.Count; i++)
                tiles.Add(CreateRewardTile(ItemIds.DisplayName(payout.OrbSizes[i]), payout.OrbEach, ItemImages.SpriteOf(payout.OrbSizes[i]), Color.white));
        }
        tiles.Add(CreateRewardTile("R", payout.Red, null, new Color(0.95f, 0.28f, 0.24f, 1f)));
        tiles.Add(CreateRewardTile("G", payout.Green, null, new Color(0.28f, 0.78f, 0.32f, 1f)));
        tiles.Add(CreateRewardTile("B", payout.Blue, null, new Color(0.28f, 0.48f, 0.95f, 1f)));
        tiles.Add(CreateRewardTile("全ピクセル", payout.AllPixels, null, Color.white));
        LayoutRewardTiles(tiles);
    }

    RectTransform CreateRewardTile(string label, long count, Sprite sprite, Color tint)
    {
        GameObject row = new GameObject("Reward", typeof(RectTransform));
        row.transform.SetParent(_offlineList, false);
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(820f, 84f);

        GameObject cluster = new GameObject("Cluster", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        cluster.transform.SetParent(row.transform, false);
        RectTransform clusterRect = cluster.GetComponent<RectTransform>();
        clusterRect.anchorMin = new Vector2(0.5f, 0.5f);
        clusterRect.anchorMax = new Vector2(0.5f, 0.5f);
        clusterRect.pivot = new Vector2(0.5f, 0.5f);
        HorizontalLayoutGroup layout = cluster.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 16f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fit = cluster.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image icon = CreateImage("Icon", cluster.transform, tint);
        icon.sprite = sprite;
        icon.preserveAspect = true;
        Prefer(icon.rectTransform, 72f, 72f);
        RewardLine(cluster.transform, label, 36);
        RewardLine(cluster.transform, "×" + UiNumber.Format(count), 36);
        return rect;
    }

    void RewardLine(Transform parent, string value, int size)
    {
        Text text = CreateLabel("Text", parent, size, TextAnchor.MiddleCenter);
        text.text = value;
        text.fontStyle = FontStyle.Bold;
        ContentSizeFitter fit = text.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void LayoutRewardTiles(List<RectTransform> tiles)
    {
        int count = tiles.Count;
        if (count < 1)
            return;
        float rowH = 84f;
        float gap = 10f;
        float blockH = count * rowH + (count - 1) * gap;
        float areaH = _offlineList.rect.height;
        if (areaH < 100f)
            areaH = 520f;
        if (blockH > areaH)
        {
            float scale = areaH / blockH;
            rowH *= scale;
            gap *= scale;
            blockH = areaH;
            float icon = rowH - 12f;
            if (icon < 36f)
                icon = 36f;
            int font = rowH >= 72f ? 36 : rowH >= 56f ? 28 : 22;
            for (int i = 0; i < count; i++)
                FitRewardLine(tiles[i], icon, font);
        }
        float top = blockH * 0.5f;
        for (int i = 0; i < count; i++)
        {
            tiles[i].sizeDelta = new Vector2(820f, rowH);
            tiles[i].anchoredPosition = new Vector2(0f, top - rowH * 0.5f - i * (rowH + gap));
        }
    }

    static void FitRewardLine(RectTransform row, float icon, int font)
    {
        Transform cluster = row.Find("Cluster");
        if (cluster == null)
            return;
        for (int i = 0; i < cluster.childCount; i++)
        {
            Transform child = cluster.GetChild(i);
            if (child.name == "Icon")
            {
                RectTransform iconRect = child as RectTransform;
                if (iconRect != null)
                {
                    iconRect.sizeDelta = new Vector2(icon, icon);
                    LayoutElement element = iconRect.GetComponent<LayoutElement>();
                    if (element != null)
                    {
                        element.preferredWidth = icon;
                        element.preferredHeight = icon;
                    }
                }
            }
            Text text = child.GetComponent<Text>();
            if (text != null)
                text.fontSize = font;
        }
    }

    void BuildBanner()
    {
        Transform canvas = Canvas();
        if (canvas == null || _banner != null)
            return;
        _banner = CreateLabel("TextIdleWarning", canvas, 26, TextAnchor.MiddleCenter);
        _banner.color = Color.white;
        RectTransform rect = _banner.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(-24f, 56f);
        _banner.gameObject.SetActive(false);
    }

    void EnsureIdlePopup()
    {
        if (_idlePopup != null)
            return;
        Transform canvas = Canvas();
        if (canvas == null)
            return;
        _idlePopup = Dim("IdleLimitPopup", canvas);
        Transform card = Card(_idlePopup.transform, 860f, 760f);
        _idleBody = CreateLabel("Body", card, 28, TextAnchor.UpperCenter);
        PlaceTop(_idleBody.rectTransform, 28f, 120f);
        Text lead = CreateLabel("Lead", card, 28, TextAnchor.MiddleCenter);
        lead.text = "広告を最後まで見ると";
        PlaceTop(lead.rectTransform, 156f, 48f);
        BuildIdleRewardRow(card, 214f, true);
        BuildIdleRewardRow(card, 330f, false);
        Text gain = CreateLabel("Gain", card, 28, TextAnchor.MiddleCenter);
        gain.text = "を取得します";
        PlaceTop(gain.rectTransform, 442f, 48f);
        Button watch = WideButton("ButtonIdleWatchAd", card, "広告を見る", new Color(0.25f, 0.45f, 0.75f, 1f), 150f);
        Button close = WideButton("ButtonIdleClose", card, "広告を見ずに再開する", new Color(0.45f, 0.45f, 0.48f, 1f), 48f);
        watch.onClick.AddListener(WatchIdleAd);
        close.onClick.AddListener(CloseIdle);
        _idlePopup.SetActive(false);
    }

    void BuildIdleRewardRow(Transform card, float top, bool orb)
    {
        GameObject row = new GameObject(orb ? "OrbReward" : "PixelReward", typeof(RectTransform));
        row.transform.SetParent(card, false);
        PlaceTop(row.GetComponent<RectTransform>(), top, 104f);

        GameObject cluster = new GameObject("Cluster", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        cluster.transform.SetParent(row.transform, false);
        RectTransform clusterRect = cluster.GetComponent<RectTransform>();
        clusterRect.anchorMin = new Vector2(0.5f, 0.5f);
        clusterRect.anchorMax = new Vector2(0.5f, 0.5f);
        clusterRect.pivot = new Vector2(0.5f, 0.5f);
        HorizontalLayoutGroup layout = cluster.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fit = cluster.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (orb)
        {
            _idleOrbIcon = CreateImage("Icon", cluster.transform, Color.white);
            _idleOrbIcon.preserveAspect = true;
            Prefer(_idleOrbIcon.rectTransform, 84f, 84f);
            _idleOrbName = RewardLabel(cluster.transform, "");
        }
        else
        {
            Image swatch = CreateImage("Icon", cluster.transform, new Color(0f, 0f, 0f, 0f));
            Prefer(swatch.rectTransform, 84f, 84f);
            AddSwatch(swatch.transform, new Color(0.95f, 0.28f, 0.24f, 1f), 0f, 42f);
            AddSwatch(swatch.transform, new Color(0.28f, 0.78f, 0.32f, 1f), 42f, 42f);
            AddSwatch(swatch.transform, new Color(0.28f, 0.48f, 0.95f, 1f), 0f, 0f);
            AddSwatch(swatch.transform, Color.white, 42f, 0f);
            RewardLabel(cluster.transform, "全ピクセル");
        }
        RewardLabel(cluster.transform, orb ? "×10" : "×100");
    }

    Text RewardLabel(Transform parent, string value)
    {
        Text text = CreateLabel("Text", parent, 42, TextAnchor.MiddleLeft);
        text.text = value;
        text.fontStyle = FontStyle.Bold;
        ContentSizeFitter fit = text.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        if (string.IsNullOrEmpty(value))
            _idleOrbName = text;
        return text;
    }

    static void AddSwatch(Transform parent, Color color, float x, float y)
    {
        Image image = CreateImage("Swatch", parent, color);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(38f, 38f);
    }

    static void Prefer(RectTransform rect, float width, float height)
    {
        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;
        rect.sizeDelta = new Vector2(width, height);
    }

    static void PlaceTop(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(-48f, height);
    }

    void EnsureOfflinePopup()
    {
        if (_offlinePopup != null)
            return;
        Transform canvas = Canvas();
        if (canvas == null)
            return;
        _offlinePopup = Dim("OfflineRewardPopup", canvas);
        Transform card = Card(_offlinePopup.transform, 880f, 980f);
        _offlineBody = CreateLabel("Body", card, 26, TextAnchor.UpperCenter);
        RectTransform body = _offlineBody.rectTransform;
        body.anchorMin = new Vector2(0f, 1f);
        body.anchorMax = new Vector2(1f, 1f);
        body.pivot = new Vector2(0.5f, 1f);
        body.anchoredPosition = new Vector2(0f, -24f);
        body.sizeDelta = new Vector2(-48f, 220f);
        GameObject list = new GameObject("List", typeof(RectTransform));
        list.transform.SetParent(card, false);
        _offlineList = list.GetComponent<RectTransform>();
        _offlineList.anchorMin = new Vector2(0f, 0f);
        _offlineList.anchorMax = new Vector2(1f, 1f);
        // 下端132・高さ84の広告ボタンより上に余白を残す。一覧はその分だけ上に寄る。
        _offlineList.offsetMin = new Vector2(16f, 248f);
        _offlineList.offsetMax = new Vector2(-16f, -250f);
        _offlineAd = WideButton("ButtonOfflineWatchAd", card, "広告を見て放置報酬を3倍受け取る", new Color(0.25f, 0.45f, 0.75f, 1f), 132f);
        _offlineClose = WideButton("ButtonOfflineClose", card, "広告を見ずに閉じる", new Color(0.45f, 0.45f, 0.48f, 1f), 36f);
        _offlineAd.onClick.AddListener(WatchOfflineAd);
        _offlineClose.onClick.AddListener(CloseOffline);
        _offlinePopup.SetActive(false);
    }

    static Transform Canvas()
    {
        GameObject canvas = GameObject.Find("CanvasProduction");
        if (canvas == null)
            return null;
        return canvas.transform;
    }

    GameObject Dim(string name, Transform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 4200;
        Image image = root.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.62f);
        image.raycastTarget = true;
        return root;
    }

    Transform Card(Transform parent, float width, float height)
    {
        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        card.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 1f);
        return card.transform;
    }

    Button WideButton(string name, Transform parent, string label, Color color, float bottom)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0f);
        rect.anchorMax = new Vector2(0.92f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottom);
        rect.sizeDelta = new Vector2(0f, 84f);
        obj.GetComponent<Image>().color = color;
        Text text = CreateLabel("Text", obj.transform, 26, TextAnchor.MiddleCenter);
        text.text = label;
        Stretch(text.rectTransform, 8f, 8f, 8f, 8f);
        return obj.GetComponent<Button>();
    }

    Text CreateLabel(string name, Transform parent, int size, TextAnchor anchor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
