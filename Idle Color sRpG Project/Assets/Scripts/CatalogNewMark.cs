using UnityEngine;
using UnityEngine.UI;

// 図鑑の赤丸が残っているキャラの絵の左上に出す「New!」
public class CatalogNewMark : MonoBehaviour
{
    public uint CharacterId;

    public static void Apply(Transform host, CharacterClass character)
    {
        if (host == null)
            return;
        Transform existing = host.Find("CatalogNewMark");
        bool show = character != null && character.CatalogNew;
        if (!show)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        if (existing == null)
        {
            GameObject markObject = new GameObject("CatalogNewMark", typeof(RectTransform));
            markObject.transform.SetParent(host, false);
            RectTransform rect = markObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(4f, -2f);
            rect.sizeDelta = new Vector2(125f, 44f);

            Text text = markObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = "New!";
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 31;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(1f, 0.88f, 0.05f, 1f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = markObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2.1f, -2.1f);
            markObject.AddComponent<CatalogNewMark>();
            existing = markObject.transform;
        }
        else
            existing.gameObject.SetActive(true);

        RectTransform existingRect = existing.GetComponent<RectTransform>();
        if (existingRect != null)
            existingRect.sizeDelta = new Vector2(125f, 44f);
        Text label = existing.GetComponent<Text>();
        if (label != null)
        {
            label.text = "New!";
            label.fontSize = 31;
            label.color = new Color(1f, 0.88f, 0.05f, 1f);
        }
        Outline existingOutline = existing.GetComponent<Outline>();
        if (existingOutline != null)
            existingOutline.effectDistance = new Vector2(2.1f, -2.1f);

        CatalogNewMark mark = existing.GetComponent<CatalogNewMark>();
        if (mark == null)
            mark = existing.gameObject.AddComponent<CatalogNewMark>();
        mark.CharacterId = character.ID;
        existing.SetAsLastSibling();
    }

    public static void Hide(uint characterId)
    {
        if (characterId == 0)
            return;
        CatalogNewMark[] marks = Resources.FindObjectsOfTypeAll<CatalogNewMark>();
        for (int i = 0; i < marks.Length; i++)
        {
            CatalogNewMark mark = marks[i];
            if (mark == null || !mark.gameObject.scene.IsValid() || mark.CharacterId != characterId)
                continue;
            mark.gameObject.SetActive(false);
        }
    }
}
