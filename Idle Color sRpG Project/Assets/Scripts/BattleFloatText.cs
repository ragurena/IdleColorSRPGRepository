using UnityEngine;
using UnityEngine.UI;

// バトルの数値。ダメージはキャラの真ん中の高さから浮かんで消え、経験値は右下に出てその場で消える。
// 数値はバトル画面の中に置く。別の Canvas にすると、クエストなどの画面より手前に出てしまう。
public class BattleFloatText : MonoBehaviour
{
    const float Duration = 0.58f;
    const float Rise = 36f;
    const float StackGap = 16f;
    const float ExpRight = 22f;
    const float ExpBelow = 6f;
    const string LayerName = "BattleFloatLayer";

    static Font _font;

    RectTransform _rect;
    Text _label;
    Outline _outline;
    Vector2 _origin;
    Color _color;
    float _elapsed;
    int _cellId;
    bool _damage;

    public static void Show(RectTransform cell, bool damage, long amount)
    {
        if (cell == null || amount <= 0)
            return;
        Font font = LabelFont();
        if (font == null)
            return;
        RectTransform layer = FloatLayer(cell);
        if (layer == null)
            return;

        int cellId = cell.GetInstanceID();
        int stack = 0;
        BattleFloatText[] live = layer.GetComponentsInChildren<BattleFloatText>(true);
        for (int i = 0; i < live.Length; i++)
        {
            if (live[i] != null && live[i]._cellId == cellId && live[i]._damage == damage)
                stack++;
        }
        if (stack > 3)
            stack = stack % 4;

        GameObject obj = new GameObject(damage ? "DamageFloat" : "ExpFloat", typeof(RectTransform));
        obj.transform.SetParent(layer, false);
        BattleFloatText fx = obj.AddComponent<BattleFloatText>();
        fx.Begin(cell, layer, damage, amount, stack, font, cellId);
    }

    // マスより手前、ステージ切替の幕より奥。クエスト画面はバトルの外なので、この数値より手前に残る。
    static RectTransform FloatLayer(RectTransform cell)
    {
        Transform battle = cell.parent;
        if (battle == null)
            return null;
        Transform existing = battle.Find(LayerName);
        RectTransform layer;
        if (existing == null)
        {
            GameObject obj = new GameObject(LayerName, typeof(RectTransform));
            obj.transform.SetParent(battle, false);
            layer = obj.GetComponent<RectTransform>();
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            layer.pivot = new Vector2(0.5f, 0.5f);
            layer.localScale = Vector3.one;
        }
        else
            layer = existing as RectTransform;
        if (layer == null)
            return null;

        int firstOverlay = battle.childCount;
        Transform wipe = battle.Find("StageWipe");
        Transform dim = battle.Find("StageDim");
        if (wipe != null)
            firstOverlay = Mathf.Min(firstOverlay, wipe.GetSiblingIndex());
        if (dim != null)
            firstOverlay = Mathf.Min(firstOverlay, dim.GetSiblingIndex());
        int target = firstOverlay;
        if (layer.GetSiblingIndex() < firstOverlay)
            target = firstOverlay - 1;
        if (target < 0)
            target = 0;
        layer.SetSiblingIndex(target);
        return layer;
    }

    public static void Clear(Transform layer)
    {
        if (layer == null)
            return;
        BattleFloatText[] live = layer.GetComponentsInChildren<BattleFloatText>(true);
        for (int i = 0; i < live.Length; i++)
        {
            if (live[i] != null)
                Destroy(live[i].gameObject);
        }
    }

    void Begin(RectTransform cell, RectTransform layer, bool damage, long amount, int stack, Font font, int cellId)
    {
        _damage = damage;
        _cellId = cellId;
        _rect = GetComponent<RectTransform>();
        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(1f, damage ? 0.5f : 0f);
        _rect.sizeDelta = new Vector2(220f, 48f);
        Rect area = cell.rect;
        Vector2 onCell = damage
            ? new Vector2(area.xMax, (area.yMin + area.yMax) * 0.5f)
            : new Vector2(area.xMax + ExpRight, area.yMin - ExpBelow);
        onCell.y += stack * StackGap;
        Vector3 world = cell.TransformPoint(onCell);
        _origin = layer.InverseTransformPoint(world);
        _rect.anchoredPosition = _origin;

        _color = RgbProduceFx.ChannelColor(damage ? 0 : 1);
        _color = Color.Lerp(_color, Color.white, 0.22f);
        _color.a = 1f;

        _label = gameObject.AddComponent<Text>();
        _label.font = font;
        _label.fontSize = 28;
        _label.alignment = damage ? TextAnchor.MiddleRight : TextAnchor.LowerRight;
        _label.color = _color;
        _label.raycastTarget = false;
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        _label.verticalOverflow = VerticalWrapMode.Overflow;
        _label.supportRichText = false;
        _label.text = (damage ? "-" : "+") + UiNumber.Format(amount);
        _outline = gameObject.AddComponent<Outline>();
        _outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        _outline.effectDistance = new Vector2(1f, -1f);
        transform.SetAsLastSibling();
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / Duration);
        float eased = 1f - (1f - t) * (1f - t);
        if (_rect != null)
        {
            float y = _origin.y;
            if (_damage)
                y += Rise * eased;
            _rect.anchoredPosition = new Vector2(_origin.x, y);
        }
        float fade = t < 0.22f ? 1f : 1f - (t - 0.22f) / 0.78f;
        if (fade < 0f)
            fade = 0f;
        if (_label != null)
        {
            Color c = _color;
            c.a = fade;
            _label.color = c;
        }
        if (_outline != null)
            _outline.effectColor = new Color(0f, 0f, 0f, 0.88f * fade);
        if (_elapsed >= Duration)
            Destroy(gameObject);
    }

    static Font LabelFont()
    {
        if (_font != null)
            return _font;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return _font;
    }
}
