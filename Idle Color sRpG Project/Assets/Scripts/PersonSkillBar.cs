using UnityEngine;
using UnityEngine.UI;

// 生産枠のすぐ下に出す、人のレベルと経験値バー。
public static class PersonSkillBar
{
    const string RootName = "PersonSkillBar";
    const int LevelFontSize = 16;
    const float LevelHeight = 18f;

    public static void Hide(Button button)
    {
        if (button == null)
            return;
        Hide(button.transform);
    }

    public static void Hide(Transform host)
    {
        if (host == null)
            return;
        Transform root = host.Find(RootName);
        if (root != null)
            root.gameObject.SetActive(false);
    }

    public static void Show(Button button, int level, long exp, long need)
    {
        Show(button != null ? button.transform : null, level, exp, need, false);
    }

    public static void ShowInside(Transform host, int level, long exp, long need)
    {
        Show(host, level, exp, need, true);
    }

    static void Show(Transform host, int level, long exp, long need, bool inside)
    {
        if (host == null)
            return;
        if (level < 1)
            level = 1;
        if (exp < 0)
            exp = 0;
        if (need < 1)
            need = 1;

        Transform root = Ensure(host);
        RectTransform rect = root.GetComponent<RectTransform>();
        if (NeedsPlace(rect, inside))
            Place(rect, inside);
        root.gameObject.SetActive(true);
        Text label = root.Find("Level").GetComponent<Text>();
        if (label.fontSize != LevelFontSize)
            label.fontSize = LevelFontSize;
        RectTransform levelRect = label.rectTransform;
        if (levelRect.sizeDelta.y != LevelHeight)
            levelRect.sizeDelta = new Vector2(levelRect.sizeDelta.x, LevelHeight);
        string levelText = "Lv" + level.ToString();
        if (label.text != levelText)
            label.text = levelText;

        float ratio = (float)exp / need;
        if (ratio < 0f)
            ratio = 0f;
        if (ratio > 1f)
            ratio = 1f;
        RectTransform fill = root.Find("Track/Fill").GetComponent<RectTransform>();
        Vector2 fillAnchor = new Vector2(ratio, 1f);
        if (fill.anchorMax != fillAnchor)
        {
            fill.anchorMax = fillAnchor;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }
    }

    static bool NeedsPlace(RectTransform root, bool inside)
    {
        if (inside)
            return root.pivot.y > 0.5f || root.anchoredPosition.y != 10f - 28f;
        return root.pivot.y < 0.5f || root.anchoredPosition.y != 18f;
    }

    static void Place(RectTransform root, bool inside)
    {
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        if (inside)
        {
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, 10f - 28f);
        }
        else
        {
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, 18f);
        }
        root.sizeDelta = new Vector2(0f, 28f);
    }

    static Transform Ensure(Transform button)
    {
        Transform existing = button.Find(RootName);
        if (existing != null)
            return existing;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject rootObject = new GameObject(RootName, typeof(RectTransform));
        rootObject.transform.SetParent(button, false);
        RectTransform root = rootObject.GetComponent<RectTransform>();
        Image back = rootObject.AddComponent<Image>();
        back.color = new Color(0f, 0f, 0f, 0.62f);
        back.raycastTarget = false;
        Place(root, false);

        GameObject levelObject = new GameObject("Level", typeof(RectTransform));
        levelObject.transform.SetParent(root, false);
        RectTransform levelRect = levelObject.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0f, 1f);
        levelRect.anchorMax = new Vector2(1f, 1f);
        levelRect.pivot = new Vector2(0.5f, 1f);
        levelRect.anchoredPosition = Vector2.zero;
        levelRect.sizeDelta = new Vector2(0f, LevelHeight);
        Text level = levelObject.AddComponent<Text>();
        level.font = font;
        level.fontSize = LevelFontSize;
        level.alignment = TextAnchor.MiddleCenter;
        level.color = Color.white;
        level.raycastTarget = false;
        level.horizontalOverflow = HorizontalWrapMode.Overflow;
        Outline outline = levelObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject trackObject = new GameObject("Track", typeof(RectTransform));
        trackObject.transform.SetParent(root, false);
        RectTransform track = trackObject.GetComponent<RectTransform>();
        track.anchorMin = new Vector2(0f, 0f);
        track.anchorMax = new Vector2(1f, 0f);
        track.pivot = new Vector2(0.5f, 0f);
        track.anchoredPosition = Vector2.zero;
        track.sizeDelta = new Vector2(0f, 10f);
        Image trackImage = trackObject.AddComponent<Image>();
        trackImage.color = new Color(0.05f, 0.22f, 0.08f, 1f);
        trackImage.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
        fillObject.transform.SetParent(track, false);
        RectTransform fill = fillObject.GetComponent<RectTransform>();
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.25f, 0.82f, 0.28f, 1f);
        fillImage.raycastTarget = false;

        return root;
    }
}
