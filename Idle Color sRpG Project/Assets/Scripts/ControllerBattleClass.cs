using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// バトル画面。戦闘の勝敗は BattleFloorFight が決め、ここは表示と進行だけを持つ。
public class ControllerBattleClass : MonoBehaviour
{
    ControllerProduction _production;
    BattleBalanceConfig _config;
    Font _font;
    GameObject _root;
    Text _header;
    Text _resource;
    Text _logText;
    Button _fastButton;
    bool _fast;
    bool _running;
    bool _stop;
    int _nextUnitId = 1;
    readonly List<string> _logs = new List<string>();
    readonly List<BattleUnit> _units = new List<BattleUnit>();
    readonly Dictionary<uint, Sprite> _sprites = new Dictionary<uint, Sprite>();
    CellView[,] _allies;
    CellView[,] _enemies;

    class CellView
    {
        public Image Image;
        public Text Label;
    }

    public void Initialize(ControllerProduction production)
    {
        _production = production;
        _config = production.GetBattleBalance();
        _config.FormationSize = Constants.BATTLE_FORMATION_SIZE;
        _config.MaxUnits = Constants.BATTLE_FORMATION_SIZE * Constants.BATTLE_FORMATION_SIZE;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        BuildUi();
        ShowIdle();
    }

    public bool IsRunning()
    {
        return _running;
    }

    public void Begin(int stageIndex, int setIndex, int floorFrom, int floorTo)
    {
        if (_running)
            return;
        if (_production == null)
            return;

        _units.Clear();
        _logs.Clear();
        _stop = false;
        _nextUnitId = 1;
        PlaceAllies(setIndex);
        if (!HasLivingAlly())
        {
            Debug.LogWarning("出撃できる味方がいません");
            _production.LeaveBattle();
            return;
        }

        Show();
        StartCoroutine(Run(stageIndex, floorFrom, floorTo));
    }

    IEnumerator Run(int stageIndex, int floorFrom, int floorTo)
    {
        _running = true;
        StageBattleContent content = BattleStageCatalog.Get(stageIndex);
        string stageName = ControllerBattleStageClass.GetStageName(stageIndex);
        var sink = new RewardSink(this);
        bool clearedStage = false;
        bool partyGone = false;
        bool announcedClear = false;

        while (true)
        {
        for (int floor = floorFrom; floor <= floorTo; floor++)
        {
            if (_stop)
                break;
            if (!HasLivingAlly())
                break;

            RemoveEnemies();
            SpawnEnemies(content, floor);
            var fight = new BattleFloorFight(_config, new SystemBattleRandom(), sink);
            fight.Begin(_units);
            AddLog(floor.ToString() + "階");
            Refresh(stageName, floor, floorFrom, floorTo);
            yield return new WaitForSeconds(Interval());

            while (fight.Outcome == BattleOutcome.InProgress && !_stop)
            {
                BattleAttackResult step = fight.Step();
                if (!string.IsNullOrEmpty(step.Log))
                    AddLog(step.Log);
                _production.SyncAllyLivesFromBattle(_units);
                Refresh(stageName, floor, floorFrom, floorTo);
                if (fight.Outcome != BattleOutcome.InProgress)
                    break;
                yield return new WaitForSeconds(Interval());
            }

            if (fight.Outcome != BattleOutcome.FloorCleared)
            {
                AddLog("撤退した");
                Refresh(stageName, floor, floorFrom, floorTo);
                yield return new WaitForSeconds(Interval());
                Finish();
                yield break;
            }

            List<int> drops = FloorDropRoller.Roll(content.Drops, floor, new SystemBattleRandom());
            for (int i = 0; i < drops.Count; i++)
            {
                _production.AddBattleItem(drops[i], 1);
                AddLog(ItemIds.DisplayName(drops[i]) + " を入手");
            }

            AddLog(floor.ToString() + "階をクリア");
            if (floor >= Constants.BATTLE_STAGE_FLOOR_MAX)
                clearedStage = true;
            if (!HasLivingAlly())
            {
                partyGone = true;
                AddLog("味方が尽きたため、次の階へは進めない");
            }
            Refresh(stageName, floor, floorFrom, floorTo);
            _production.SaveGame();
            yield return new WaitForSeconds(Interval());
            if (partyGone)
                break;
        }

        if (_stop)
        {
            AddLog("撤退した");
            Refresh(stageName, floorFrom, floorFrom, floorTo);
            yield return new WaitForSeconds(Interval());
            Finish();
            yield break;
        }

        if (partyGone || !HasLivingAlly())
        {
            Finish();
            yield break;
        }

        if (clearedStage && !announcedClear)
        {
            _production.MarkBattleStageCleared();
            AddLog(stageName + " をクリア");
            announcedClear = true;
        }

        if (_production.IsBattleRepeatOn())
        {
            AddLog(floorFrom.ToString() + "階から繰り返す");
            Refresh(stageName, floorFrom, floorFrom, floorTo);
            yield return new WaitForSeconds(Interval());
            continue;
        }

        AddLog(floorFrom.ToString() + "〜" + floorTo.ToString() + "階を踏破");
        Refresh(stageName, floorTo, floorFrom, floorTo);
        yield return new WaitForSeconds(Interval());
        Finish();
        yield break;
        }
    }

    void Finish()
    {
        _running = false;
        _stop = false;
        _production.SyncAllyLivesFromBattle(_units);
        if (_production.GetActiveBattleStage() != 0)
            _production.LeaveBattle();
        _production.SaveGame();
        _production.UpdateRGBProductionScene();
        _production.ShowCharacterOwnedNum();
        ShowIdle();
        _production.ReturnFromBattle();
    }

    void PlaceAllies(int setIndex)
    {
        int size = _config.FormationSize;
        for (int x = 1; x <= size; x++)
        {
            for (int y = 1; y <= size; y++)
            {
                uint id = _production.GetBattlePartyCharacter(setIndex, x, y);
                if (id == 0)
                    continue;
                CharacterClass character = _production.GetCharacter(id);
                BattleUnit unit = BattleCharacterFactory.CreateAlly(character, x, y, _nextUnitId++, _config);
                if (unit != null)
                    _units.Add(unit);
            }
        }
    }

    void SpawnEnemies(StageBattleContent content, int floor)
    {
        BossFloorSpawn boss = content.FindBoss(floor);
        var ids = new List<uint>();
        double multiplier = 1.0;
        var rng = new SystemBattleRandom();
        if (boss != null)
        {
            int count = EnemySpawner.RollBossCount(boss, _config.MaxUnits, rng);
            for (int i = 0; i < count; i++)
                ids.Add(boss.CharacterId);
        }
        else
        {
            FloorBandSpawn band = content.GetBand(floor);
            multiplier = band.StatMultiplier;
            int total = EnemySpawner.RollTotalCount(band.Entries, _config.MaxUnits, rng);
            ids = EnemySpawner.RollTypes(band.Entries, total, rng);
        }

        if (ids.Count > _config.MaxUnits)
            ids.RemoveRange(_config.MaxUnits, ids.Count - _config.MaxUnits);

        List<CellPosition> cells = BattlePlacement.ShuffleCells(_config.FormationSize, ids.Count, rng);
        for (int i = 0; i < ids.Count && i < cells.Count; i++)
        {
            CharacterClass character = _production.GetCharacter(ids[i]);
            BattleUnit enemy = BattleCharacterFactory.CreateEnemy(character, cells[i].X, cells[i].Y, _nextUnitId++, multiplier, _config);
            if (enemy != null)
                _units.Add(enemy);
        }
    }

    void RemoveEnemies()
    {
        for (int i = _units.Count - 1; i >= 0; i--)
        {
            if (!_units[i].IsAlly)
                _units.RemoveAt(i);
        }
    }

    bool HasLivingAlly()
    {
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i].IsAlly && _units[i].IsLiving())
                return true;
        }
        return false;
    }

    float Interval()
    {
        double seconds = _fast ? _config.FastActionIntervalSeconds : _config.ActionIntervalSeconds;
        if (seconds < 0.0)
            seconds = 0.0;
        return (float)seconds;
    }

    void PushFast()
    {
        _fast = !_fast;
        if (_fastButton != null)
            _fastButton.GetComponentInChildren<Text>().text = _fast ? "通常速度" : "高速";
    }

    void PushWithdraw()
    {
        if (!_running)
            return;
        _stop = true;
    }

    void AddLog(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;
        _logs.Add(line);
        while (_logs.Count > 8)
            _logs.RemoveAt(0);
        if (_logText != null)
            _logText.text = string.Join("\n", _logs.ToArray());
    }

    void Refresh(string stageName, int floor, int floorFrom, int floorTo)
    {
        if (_header != null)
            _header.text = stageName + "  " + floor.ToString() + "階  （" + floorFrom.ToString() + "〜" + floorTo.ToString() + "）";
        if (_resource != null)
            _resource.text = _production.GetRgbText();
        DrawSide(_allies, true);
        DrawSide(_enemies, false);
    }

    void DrawSide(CellView[,] cells, bool ally)
    {
        if (cells == null)
            return;
        int size = _config.FormationSize;
        for (int x = 1; x <= size; x++)
        {
            for (int y = 1; y <= size; y++)
            {
                CellView view = cells[x, y];
                if (view == null)
                    continue;
                BattleUnit unit = FindLiving(ally, x, y);
                if (unit == null)
                {
                    view.Image.sprite = null;
                    view.Image.color = new Color(0.15f, 0.15f, 0.18f, 1f);
                    view.Label.text = "";
                    continue;
                }

                view.Image.sprite = SpriteOf(unit.CharacterId);
                view.Image.color = Color.white;
                view.Label.text = "HP " + unit.Hp.ToString() + "\n残機 " + unit.Lives.ToString();
            }
        }
    }

    BattleUnit FindLiving(bool ally, int x, int y)
    {
        for (int i = 0; i < _units.Count; i++)
        {
            BattleUnit unit = _units[i];
            if (unit.IsAlly == ally && unit.X == x && unit.Y == y && unit.IsLiving())
                return unit;
        }
        return null;
    }

    Sprite SpriteOf(uint characterId)
    {
        Sprite cached;
        if (_sprites.TryGetValue(characterId, out cached) && cached != null)
            return cached;

        CharacterClass character = _production.GetCharacter(characterId);
        Texture2D tex = character != null ? character.ImageTexture2D : null;
        if (tex == null)
            return Resources.Load<Sprite>("NoImageSprite");

        tex.filterMode = FilterMode.Point;
        int size = character.Size > 0 ? character.Size : tex.width;
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        _sprites[characterId] = sprite;
        return sprite;
    }

    public void Show()
    {
        if (_root != null)
            _root.SetActive(true);
    }

    void ShowIdle()
    {
        Show();
        if (_header != null)
            _header.text = "バトル";
        if (_resource != null && _production != null)
            _resource.text = _production.GetRgbText();
        if (_units.Count == 0)
        {
            _logs.Clear();
            AddLog("出撃すると、ここに戦闘が出ます");
        }
        DrawSide(_allies, true);
        DrawSide(_enemies, false);
    }

    void BuildUi()
    {
        if (_root != null)
            return;

        _root = GameObject.Find("PanelBattle");
        if (_root == null)
        {
            Debug.LogWarning("PanelBattle が見つかりません");
            return;
        }

        GameObject logObject = GameObject.Find("TextLog");
        if (logObject != null)
        {
            _logText = logObject.GetComponent<Text>();
            if (_logText != null && _logText.font != null)
                _font = _logText.font;
            RectTransform logRect = logObject.GetComponent<RectTransform>();
            logRect.SetParent(_root.transform, false);
            logRect.anchorMin = new Vector2(0.5f, 0f);
            logRect.anchorMax = new Vector2(0.5f, 0f);
            logRect.pivot = new Vector2(0.5f, 0f);
            logRect.anchoredPosition = new Vector2(0f, 24f);
            logRect.sizeDelta = new Vector2(1040f, 110f);
            _logText.font = _font;
            _logText.fontSize = 24;
            _logText.alignment = TextAnchor.UpperLeft;
            _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _logText.verticalOverflow = VerticalWrapMode.Truncate;
            _logText.raycastTarget = false;
            _logText.resizeTextForBestFit = false;
        }

        _header = CreateText("TextBattleHeader", _root.transform, 28, TextAnchor.MiddleLeft);
        PlaceRect(_header.rectTransform, 110f, 175f, 620f, 44f);

        _resource = CreateText("TextBattleRgb", _root.transform, 24, TextAnchor.MiddleCenter);
        PlaceRect(_resource.rectTransform, 0f, 128f, 1040f, 32f);

        _allies = new CellView[_config.FormationSize + 1, _config.FormationSize + 1];
        _enemies = new CellView[_config.FormationSize + 1, _config.FormationSize + 1];
        BuildGrid(_allies, -170f);
        BuildGrid(_enemies, 170f);

        if (_logText == null)
        {
            _logText = CreateText("TextBattleLog", _root.transform, 24, TextAnchor.UpperLeft);
            RectTransform logRect = _logText.rectTransform;
            logRect.anchorMin = new Vector2(0.5f, 0f);
            logRect.anchorMax = new Vector2(0.5f, 0f);
            logRect.pivot = new Vector2(0.5f, 0f);
            logRect.anchoredPosition = new Vector2(0f, 24f);
            logRect.sizeDelta = new Vector2(1040f, 110f);
            _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _logText.verticalOverflow = VerticalWrapMode.Truncate;
            _logText.raycastTarget = false;
        }

        Button withdraw = CreateButton("ButtonBattleWithdraw", "撤退", new Color(0.75f, 0.28f, 0.24f, 1f));
        PlaceRect(withdraw.GetComponent<RectTransform>(), -430f, 175f, 120f, 44f);
        withdraw.onClick.AddListener(PushWithdraw);

        _fastButton = CreateButton("ButtonBattleFast", "高速", new Color(0.25f, 0.45f, 0.75f, 1f));
        PlaceRect(_fastButton.GetComponent<RectTransform>(), -290f, 175f, 120f, 44f);
        _fastButton.onClick.AddListener(PushFast);
    }

    void BuildGrid(CellView[,] cells, float originX)
    {
        int size = _config.FormationSize;
        float pitch = 78f;
        float cell = 70f;
        for (int x = 1; x <= size; x++)
        {
            for (int y = 1; y <= size; y++)
            {
                float px = originX + (x - 2) * pitch;
                float py = 70f - (y - 1) * pitch;
                Image image = CreateImage("ImageBattleCell" + (cells == _allies ? "A" : "E") + x.ToString() + y.ToString(), _root.transform, new Color(0.12f, 0.05f, 0.05f, 0.85f));
                PlaceRect(image.rectTransform, px, py, cell, cell);
                image.preserveAspect = true;
                image.raycastTarget = false;
                Text label = CreateText("TextBattleCell" + (cells == _allies ? "A" : "E") + x.ToString() + y.ToString(), image.transform, 16, TextAnchor.LowerCenter);
                Stretch(label.rectTransform);
                label.color = Color.white;
                label.raycastTarget = false;
                Outline outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 1f);
                outline.effectDistance = new Vector2(1f, -1f);
                cells[x, y] = new CellView { Image = image, Label = label };
            }
        }
    }

    Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    Text CreateText(string name, Transform parent, int size, TextAnchor anchor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    Button CreateButton(string name, string label, Color color)
    {
        Image image = CreateImage(name, _root.transform, color);
        Button button = image.gameObject.AddComponent<Button>();
        Text text = CreateText("Text", image.transform, 22, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        text.text = label;
        text.color = Color.white;
        return button;
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

    sealed class RewardSink : IBattleRewardSink
    {
        readonly ControllerBattleClass _view;

        public RewardSink(ControllerBattleClass view)
        {
            _view = view;
        }

        public void GrantRgb(CharacterAttribute attribute, long amount)
        {
            _view._production.AddBattleRgb(attribute, amount);
            if (amount > 0)
                _view.AddLog(AttributeColorValue.DisplayName(attribute) + " +" + amount.ToString());
        }

        public void GrantExp(BattleUnit ally, long exp)
        {
            int levels = _view._production.GrantBattleExp(ally, exp, _view._config);
            if (exp > 0)
                _view.AddLog(ally.Name + " 経験値 +" + exp.ToString());
            if (levels > 0)
                _view.AddLog(ally.Name + " レベル " + levels.ToString() + " 上昇");
        }

        public void GrantItem(int itemId, int count)
        {
            _view._production.AddBattleItem(itemId, count);
            _view.AddLog(ItemIds.DisplayName(itemId) + " を入手");
        }

        public void Recruit(uint characterId)
        {
            string message = _view._production.RecruitFromBattle(characterId, _view._units);
            if (!string.IsNullOrEmpty(message))
                _view.AddLog(message);
        }
    }
}
