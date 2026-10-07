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
    bool _alliesVisible = true;
    bool _alliesEntered;
    bool _cuePixels;
    bool _cueWalk;
    int _cueR;
    int _cueG;
    int _cueB;
    long _cuePixelCount;
    int _nextUnitId = 1;
    readonly List<string> _logs = new List<string>();
    readonly List<BattleUnit> _units = new List<BattleUnit>();
    readonly Dictionary<uint, Sprite> _sprites = new Dictionary<uint, Sprite>();
    CellView[,] _allies;
    CellView[,] _enemies;

    class CellView
    {
        public Image Image;
        public Image InfoBack;
        public Text Lives;
        public Slider Hp;
        public Slider Exp;
        public Vector2 Home;
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

    // 戦闘中に生産で増えた所持数を、出撃中の残機にも足す。足さないと次の攻撃で戦闘側の残機に戻される
    public void AddAllyLives(uint characterId, int amount)
    {
        if (!_running || amount <= 0 || characterId == 0)
            return;
        for (int i = 0; i < _units.Count; i++)
        {
            BattleUnit unit = _units[i];
            if (!unit.IsAlly || unit.CharacterId != characterId)
                continue;
            long next = (long)unit.Lives + amount;
            if (next > _config.MaxLives)
                next = _config.MaxLives;
            if (next < 0)
                next = 0;
            unit.Lives = (int)next;
        }
    }

    public void ApplyAllyFusion(CharacterClass character)
    {
        if (!_running || character == null)
            return;
        for (int i = 0; i < _units.Count; i++)
        {
            BattleUnit unit = _units[i];
            if (!unit.IsAlly || unit.CharacterId != character.ID)
                continue;
            long previousHp = unit.HpMax;
            long hpMax = (long)character.Stats[0].HPMax;
            if (hpMax < 1)
                hpMax = 1;
            unit.HpMax = hpMax;
            long delta = hpMax - previousHp;
            if (delta > 0)
                unit.Hp += delta;
            if (unit.Hp > unit.HpMax)
                unit.Hp = unit.HpMax;
            if (unit.Hp < 1 && unit.IsLiving())
                unit.Hp = 1;
            unit.Atk = (long)character.Stats[0].ATK;
            unit.Def = (long)character.Stats[0].DEF;
            int lives = character.OwnedNumCur > int.MaxValue ? int.MaxValue : (int)character.OwnedNumCur;
            if (lives < 0)
                lives = 0;
            if (_config != null && lives > _config.MaxLives)
                lives = _config.MaxLives;
            unit.Lives = lives;
        }
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
        _alliesVisible = true;
        _alliesEntered = false;
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
            if (!_alliesEntered)
            {
                ParkAlliesOffscreen();
                Refresh(stageName, floor, floorFrom, floorTo);
                yield return MarchAllies(true);
                _alliesEntered = true;
                if (_stop)
                    break;
            }
            else
            {
                Refresh(stageName, floor, floorFrom, floorTo);
                yield return new WaitForSeconds(Interval());
            }

            while (fight.Outcome == BattleOutcome.InProgress && !_stop)
            {
                ClearDropCue();
                BattleAttackResult step = fight.Step();
                if (!string.IsNullOrEmpty(step.Log))
                    AddLog(step.Log);
                _production.SyncAllyLivesFromBattle(_units);
                if (step.Acted && step.Actor != null)
                    yield return PlayAttackMotion(step.Actor, step.Target);
                if (step.Target != null && !step.Target.IsAlly && !step.Target.IsLiving() && (_cuePixels || _cueWalk))
                    yield return PlayDefeatDrops(step.Target);
                Refresh(stageName, floor, floorFrom, floorTo);
                if (_stop)
                    break;
                if (fight.Outcome != BattleOutcome.InProgress)
                    break;
                if (!(step.Acted && step.Actor != null))
                    yield return new WaitForSeconds(Interval());
            }

            if (_stop)
                break;

            if (fight.Outcome != BattleOutcome.FloorCleared)
            {
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

            _production.MarkBattleFloorCleared(stageIndex, floor);
            if (floor >= Constants.BATTLE_STAGE_FLOOR_MAX)
                clearedStage = true;
            if (!HasLivingAlly())
                partyGone = true;
            Refresh(stageName, floor, floorFrom, floorTo);
            _production.SaveGame();
            yield return new WaitForSeconds(Interval());
            if (partyGone)
                break;
        }

        if (_stop)
        {
            yield return MarchAllies(false);
            HideAllyCells();
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
            announcedClear = true;
        }

        if (_production.IsBattleRepeatOn())
        {
            Refresh(stageName, floorFrom, floorFrom, floorTo);
            yield return new WaitForSeconds(Interval());
            continue;
        }

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
        Hide();
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
                if (character == null || character.OwnedNumCur == 0)
                    continue;
                BattleUnit unit = BattleCharacterFactory.CreateAlly(character, x, y, _nextUnitId++, _config);
                if (unit != null)
                    _units.Add(unit);
            }
        }
    }

    void SpawnEnemies(StageBattleContent content, int floor)
    {
        BossFloorSpawn boss = content.FindBoss(floor);
        var picks = new List<EnemySpawnEntry>();
        double multiplier = 1.0;
        var rng = new SystemBattleRandom();
        if (boss != null)
        {
            int count = EnemySpawner.RollBossCount(boss, _config.MaxUnits, rng);
            for (int i = 0; i < count; i++)
                picks.Add(new EnemySpawnEntry(boss.CharacterId, 1, 1, 1, boss.Level));
        }
        else
        {
            FloorBandSpawn band = content.GetBand(floor);
            multiplier = band.StatMultiplier;
            picks = EnemySpawner.Roll(band.Entries, _config.MaxUnits, rng);
        }

        if (picks.Count > _config.MaxUnits)
            picks.RemoveRange(_config.MaxUnits, picks.Count - _config.MaxUnits);

        List<CellPosition> cells = BattlePlacement.ShuffleCells(_config.FormationSize, picks.Count, rng);
        for (int i = 0; i < picks.Count && i < cells.Count; i++)
        {
            CharacterClass character = _production.GetCharacter(picks[i].CharacterId);
            BattleUnit enemy = BattleCharacterFactory.CreateEnemy(character, cells[i].X, cells[i].Y, _nextUnitId++, multiplier, picks[i].Level, _config);
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

    const float AttackHop = 14f;
    const float HitShake = 8f;

    // 攻撃側は上下に1回跳び、受けた側は前後に揺れて戻る。
    IEnumerator PlayAttackMotion(BattleUnit actor, BattleUnit target)
    {
        CellView attacker = CellOf(actor);
        CellView defender = CellOf(target);
        float duration = Interval();
        if (duration <= 0f)
        {
            RestoreCell(attacker);
            RestoreCell(defender);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = elapsed / duration;
            if (u > 1f)
                u = 1f;
            if (attacker != null && attacker.Image != null)
            {
                float hop = Mathf.Sin(u * Mathf.PI) * AttackHop;
                attacker.Image.rectTransform.anchoredPosition = attacker.Home + new Vector2(0f, hop);
            }
            if (defender != null && defender != attacker && defender.Image != null)
            {
                float shake = Mathf.Sin(u * Mathf.PI * 4f) * (1f - u) * HitShake;
                defender.Image.rectTransform.anchoredPosition = defender.Home + new Vector2(shake, 0f);
            }
            yield return null;
        }

        RestoreCell(attacker);
        RestoreCell(defender);
    }

    void ClearDropCue()
    {
        _cuePixels = false;
        _cueWalk = false;
        _cueR = 0;
        _cueG = 0;
        _cueB = 0;
        _cuePixelCount = 0;
    }

    const float PixelSize = 16f;
    const int PixelVisualMax = 5;

    // 仲間になった敵は左へてくてく歩き、落ちたピクセルはその色の四角が3回跳ねて消える。
    IEnumerator PlayDefeatDrops(BattleUnit enemy)
    {
        CellView cell = CellOf(enemy);
        if (cell == null || cell.Image == null)
            yield break;

        Vector2 origin = cell.Home;
        Image walker = null;
        if (_cueWalk && cell.Image.sprite != null)
        {
            walker = CreateImage("DropWalker", _root.transform, Color.white);
            walker.sprite = cell.Image.sprite;
            walker.preserveAspect = true;
            walker.raycastTarget = false;
            PlaceRect(walker.rectTransform, origin.x, origin.y, 70f, 70f);
            walker.transform.SetAsLastSibling();
            cell.Image.sprite = null;
            cell.Image.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            SetMetersVisible(cell, false, false);
            RestoreCell(cell);
        }

        int pixelCount = 0;
        Image[] pixels = null;
        float[] drift = null;
        if (_cuePixels && _cuePixelCount > 0)
        {
            pixelCount = _cuePixelCount > PixelVisualMax ? PixelVisualMax : (int)_cuePixelCount;
            if (pixelCount < 1)
                pixelCount = 1;
            pixels = new Image[pixelCount];
            drift = new float[pixelCount];
            Color color = new Color(_cueR / 255f, _cueG / 255f, _cueB / 255f, 1f);
            for (int i = 0; i < pixelCount; i++)
            {
                Image frame = CreateImage("DropPixel", _root.transform, Color.white);
                frame.raycastTarget = false;
                PlaceRect(frame.rectTransform, origin.x, origin.y, PixelSize, PixelSize);
                Image fill = CreateImage("Fill", frame.transform, color);
                fill.raycastTarget = false;
                Stretch(fill.rectTransform);
                fill.rectTransform.offsetMin = new Vector2(2f, 2f);
                fill.rectTransform.offsetMax = new Vector2(-2f, -2f);
                frame.transform.SetAsLastSibling();
                pixels[i] = frame;
                drift[i] = (i - (pixelCount - 1) * 0.5f) * 18f;
            }
        }

        float walkTime = _fast ? 0.42f : 0.95f;
        float bounceTime = _fast ? 0.32f : 0.7f;
        float total = 0f;
        if (walker != null)
            total = walkTime;
        if (pixels != null && bounceTime > total)
            total = bounceTime;

        float elapsed = 0f;
        while (elapsed < total && !_stop)
        {
            elapsed += Time.deltaTime;
            if (walker != null)
            {
                float u = elapsed / walkTime;
                if (u > 1f)
                    u = 1f;
                float x = Mathf.Lerp(origin.x, OffscreenLeft(), u);
                float step = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 8f)) * 6f;
                walker.rectTransform.anchoredPosition = new Vector2(x, origin.y + step);
            }
            if (pixels != null)
            {
                float u = elapsed / bounceTime;
                if (u > 1f)
                    u = 1f;
                float height = PixelBounce(u);
                for (int i = 0; i < pixelCount; i++)
                {
                    if (pixels[i] == null)
                        continue;
                    float x = origin.x + drift[i] * u;
                    pixels[i].rectTransform.anchoredPosition = new Vector2(x, origin.y + height);
                }
            }
            yield return null;
        }

        if (walker != null)
            Destroy(walker.gameObject);
        if (pixels != null)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i] != null)
                    Destroy(pixels[i].gameObject);
            }
        }
    }

    // 1回目が高く、3回目で着地して高さが0になる。
    static float PixelBounce(float u)
    {
        float[] share = { 0.42f, 0.33f, 0.25f };
        float[] height = { 46f, 24f, 10f };
        float start = 0f;
        for (int i = 0; i < 3; i++)
        {
            float end = start + share[i];
            if (u <= end || i == 2)
            {
                float local = (u - start) / share[i];
                if (local < 0f)
                    local = 0f;
                if (local > 1f)
                    local = 1f;
                return Mathf.Sin(local * Mathf.PI) * height[i];
            }
            start = end;
        }
        return 0f;
    }

    const float MarchHop = 12f;
    const int MarchBounces = 4;

    float MarchDuration()
    {
        return _fast ? 0.45f : 0.8f;
    }

    float OffscreenLeft()
    {
        float half = 540f;
        if (_root != null)
        {
            RectTransform rect = _root.GetComponent<RectTransform>();
            if (rect != null && rect.rect.width > 10f)
                half = rect.rect.width * 0.5f;
        }
        return -half - 90f;
    }

    List<CellView> LivingAllyCells()
    {
        var list = new List<CellView>();
        if (_allies == null || _config == null)
            return list;
        int size = _config.FormationSize;
        for (int x = 1; x <= size; x++)
        {
            for (int y = 1; y <= size; y++)
            {
                if (FindLiving(true, x, y) == null || _allies[x, y] == null || _allies[x, y].Image == null)
                    continue;
                list.Add(_allies[x, y]);
            }
        }
        return list;
    }

    float AllyMarchShift(List<CellView> cells)
    {
        float minX = 0f;
        bool found = false;
        for (int i = 0; i < cells.Count; i++)
        {
            if (!found || cells[i].Home.x < minX)
            {
                minX = cells[i].Home.x;
                found = true;
            }
        }
        if (!found)
            return 0f;
        return OffscreenLeft() - minX;
    }

    void ParkAlliesOffscreen()
    {
        List<CellView> cells = LivingAllyCells();
        float shift = AllyMarchShift(cells);
        for (int i = 0; i < cells.Count; i++)
            cells[i].Image.rectTransform.anchoredPosition = cells[i].Home + new Vector2(shift, 0f);
    }

    // 味方だけ、隊列を保ったまま左端から跳ねて入る。出るときは同じ動きで左へ消える。
    IEnumerator MarchAllies(bool enter)
    {
        List<CellView> cells = LivingAllyCells();
        if (cells.Count == 0)
            yield break;

        float shift = AllyMarchShift(cells);
        var from = new Vector2[cells.Count];
        var to = new Vector2[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2 outside = cells[i].Home + new Vector2(shift, 0f);
            if (enter)
            {
                from[i] = outside;
                to[i] = cells[i].Home;
                cells[i].Image.rectTransform.anchoredPosition = outside;
            }
            else
            {
                from[i] = cells[i].Image.rectTransform.anchoredPosition;
                to[i] = outside;
            }
        }

        float duration = MarchDuration();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (enter && _stop)
                yield break;
            elapsed += Time.deltaTime;
            float u = elapsed / duration;
            if (u > 1f)
                u = 1f;
            float move = Mathf.SmoothStep(0f, 1f, u);
            float hop = Mathf.Abs(Mathf.Sin(u * Mathf.PI * MarchBounces)) * MarchHop;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Image == null)
                    continue;
                Vector2 pos = Vector2.Lerp(from[i], to[i], move);
                cells[i].Image.rectTransform.anchoredPosition = pos + new Vector2(0f, hop);
            }
            yield return null;
        }

        if (enter && !_stop)
        {
            for (int i = 0; i < cells.Count; i++)
                RestoreCell(cells[i]);
        }
    }

    void HideAllyCells()
    {
        _alliesVisible = false;
        if (_allies == null || _config == null)
            return;
        int size = _config.FormationSize;
        for (int x = 1; x <= size; x++)
        {
            for (int y = 1; y <= size; y++)
            {
                CellView view = _allies[x, y];
                if (view == null || view.Image == null)
                    continue;
                view.Image.sprite = null;
                view.Image.color = new Color(0.15f, 0.15f, 0.18f, 1f);
                SetMetersVisible(view, false, false);
                RestoreCell(view);
            }
        }
    }

    CellView CellOf(BattleUnit unit)
    {
        if (unit == null || _config == null)
            return null;
        CellView[,] cells = unit.IsAlly ? _allies : _enemies;
        if (cells == null)
            return null;
        if (unit.X < 1 || unit.Y < 1 || unit.X > _config.FormationSize || unit.Y > _config.FormationSize)
            return null;
        return cells[unit.X, unit.Y];
    }

    static void RestoreCell(CellView view)
    {
        if (view == null || view.Image == null)
            return;
        view.Image.rectTransform.anchoredPosition = view.Home;
    }

    void PushFast()
    {
        _fast = !_fast;
        if (_fastButton != null)
            _fastButton.GetComponentInChildren<Text>().text = _fast ? "通常速度" : "高速";
    }

    public bool RequestWithdraw()
    {
        if (!_running)
            return false;
        _stop = true;
        return true;
    }

    void PushWithdraw()
    {
        RequestWithdraw();
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
                if (ally && !_alliesVisible)
                    unit = null;
                if (unit == null)
                {
                    view.Image.sprite = null;
                    view.Image.color = new Color(0.15f, 0.15f, 0.18f, 1f);
                    SetMetersVisible(view, false, false);
                    continue;
                }

                view.Image.sprite = SpriteOf(unit.CharacterId);
                view.Image.color = Color.white;
                bool showExp = unit.IsAlly;
                SetMetersVisible(view, true, showExp);
                LayoutMeters(view, showExp);
                view.Lives.text = BattleCellLabel(unit);
                SetGauge(view.Hp, unit.Hp, unit.HpMax);
                if (showExp)
                {
                    long exp = 0;
                    long expMax = 0;
                    CharacterClass character = _production.GetCharacter(unit.CharacterId);
                    if (character != null)
                    {
                        exp = (long)character.Exp;
                        expMax = (long)character.ExpMax;
                    }
                    SetGauge(view.Exp, exp, expMax);
                }
            }
        }
    }

    string BattleCellLabel(BattleUnit unit)
    {
        int level = unit.Level;
        if (unit.IsAlly && _production != null)
        {
            CharacterClass character = _production.GetCharacter(unit.CharacterId);
            if (character != null && character.ID == unit.CharacterId)
                level = character.Level > int.MaxValue ? int.MaxValue : (int)character.Level;
        }
        if (level < 0)
            level = 0;
        if (!unit.IsAlly)
            return "Lv " + level.ToString("D3");

        int lives = unit.Lives;
        if (lives < 0)
            lives = 0;
        return "Lv " + level.ToString("D3") + " / " + lives.ToString("D2");
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

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
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
            if (_logText != null)
                _logText.text = "";
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
                //編成パネルは y=1 が下、y=3 が上。バトルも同じ向きにする
                float py = 70f - (size - y) * pitch;
                string side = cells == _allies ? "A" : "E";
                string suffix = side + x.ToString() + y.ToString();
                Image image = CreateImage("ImageBattleCell" + suffix, _root.transform, new Color(0.12f, 0.05f, 0.05f, 0.85f));
                PlaceRect(image.rectTransform, px, py, cell, cell);
                image.preserveAspect = true;
                image.raycastTarget = false;

                Image infoBack = CreateImage("ImageBattleInfo" + suffix, image.transform, new Color(0f, 0f, 0f, 0.62f));
                infoBack.raycastTarget = false;

                Slider hp = CreateGauge("SliderBattleHp" + suffix, image.transform, new Color(0.28f, 0.05f, 0.05f, 1f), new Color(0.92f, 0.16f, 0.14f, 1f));
                Slider exp = CreateGauge("SliderBattleExp" + suffix, image.transform, new Color(0.05f, 0.22f, 0.08f, 1f), new Color(0.25f, 0.82f, 0.28f, 1f));

                Text lives = CreateText("TextBattleLives" + suffix, image.transform, 14, TextAnchor.MiddleCenter);
                lives.color = Color.white;
                lives.raycastTarget = false;
                lives.resizeTextForBestFit = true;
                lives.resizeTextMinSize = 8;
                lives.resizeTextMaxSize = 12;
                lives.horizontalOverflow = HorizontalWrapMode.Overflow;
                lives.verticalOverflow = VerticalWrapMode.Truncate;
                Outline outline = lives.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 1f);
                outline.effectDistance = new Vector2(1f, -1f);

                cells[x, y] = new CellView
                {
                    Image = image,
                    InfoBack = infoBack,
                    Lives = lives,
                    Hp = hp,
                    Exp = exp,
                    Home = image.rectTransform.anchoredPosition
                };
                SetMetersVisible(cells[x, y], false, false);
            }
        }
    }

    const float MeterHeight = 7f;
    const float MeterGap = 1f;
    const float LivesHeight = 16f;

    static void SetMetersVisible(CellView view, bool show, bool showExp)
    {
        if (view.InfoBack != null)
            view.InfoBack.gameObject.SetActive(show);
        if (view.Lives != null)
            view.Lives.gameObject.SetActive(show);
        if (view.Hp != null)
            view.Hp.gameObject.SetActive(show);
        if (view.Exp != null)
            view.Exp.gameObject.SetActive(show && showExp);
    }

    static void LayoutMeters(CellView view, bool showExp)
    {
        float y = 1f;
        if (showExp && view.Exp != null)
        {
            PlaceMeter(view.Exp.GetComponent<RectTransform>(), y, MeterHeight);
            y += MeterHeight + MeterGap;
        }
        if (view.Hp != null)
        {
            PlaceMeter(view.Hp.GetComponent<RectTransform>(), y, MeterHeight);
            y += MeterHeight + MeterGap;
        }
        if (view.Lives != null)
        {
            RectTransform lives = view.Lives.rectTransform;
            lives.anchorMin = new Vector2(0f, 0f);
            lives.anchorMax = new Vector2(1f, 0f);
            lives.pivot = new Vector2(0.5f, 0f);
            lives.offsetMin = new Vector2(0f, y);
            lives.offsetMax = new Vector2(0f, y + LivesHeight);
        }
        if (view.InfoBack != null)
        {
            RectTransform back = view.InfoBack.rectTransform;
            back.anchorMin = new Vector2(0f, 0f);
            back.anchorMax = new Vector2(1f, 0f);
            back.pivot = new Vector2(0.5f, 0f);
            back.offsetMin = Vector2.zero;
            back.offsetMax = new Vector2(0f, y + LivesHeight);
        }
    }

    static void PlaceMeter(RectTransform rect, float bottom, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(3f, bottom);
        rect.offsetMax = new Vector2(-3f, bottom + height);
    }

    static void SetGauge(Slider gauge, long current, long max)
    {
        if (gauge == null)
            return;
        float amount = 0f;
        if (max > 0 && current > 0)
        {
            if (current >= max)
                amount = 1f;
            else
                amount = (float)((double)current / max);
        }
        gauge.SetValueWithoutNotify(amount);
    }

    Slider CreateGauge(string name, Transform parent, Color trackColor, Color fillColor)
    {
        Image track = CreateImage(name, parent, trackColor);
        track.raycastTarget = false;
        Slider slider = track.gameObject.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(track.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        Image fill = CreateImage("Fill", fillArea.transform, fillColor);
        fill.raycastTarget = false;
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        slider.fillRect = fillRect;
        slider.targetGraphic = track;
        ColorBlock colors = slider.colors;
        colors.disabledColor = Color.white;
        slider.colors = colors;
        slider.value = 1f;
        slider.SetValueWithoutNotify(0f);
        return slider;
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
        }

        public void GrantPixels(int r, int g, int b, long count)
        {
            _view._production.AddBattlePixels(r, g, b, count);
            if (count <= 0)
                return;
            _view._cuePixels = true;
            _view._cueR = r;
            _view._cueG = g;
            _view._cueB = b;
            _view._cuePixelCount = count;
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
            _view._production.RecruitFromBattle(characterId, _view._units);
            _view._cueWalk = true;
        }

        public void NoteDefeated(uint characterId)
        {
            _view._production.MarkCatalogDefeated(characterId);
        }
    }
}
