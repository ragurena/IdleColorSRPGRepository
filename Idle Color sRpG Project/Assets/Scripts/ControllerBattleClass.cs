using System;
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
    Sprite _fieldSprite;
    Image.Type _fieldType;
    bool _fieldStored;
    int _shownStageIndex;
    bool _stageWiping;
    float _stageWipeTime;
    Image _stageWipeImage;
    Image _stageDimImage;
    Material _stageWipeMaterial;
    Sprite _stageWipeSprite;
    const float StageWipeSeconds = 0.55f;
    Text _header;
    RawImage _repeatMark;
    Image _headerFill;
    Image _headerRecord;
    float _headerRate;
    float _headerRecordRate;
    Text _resource;
    Text _logText;
    RectTransform _logClip;
    bool _showFrontStatus;
    RectTransform _frontHost;
    GameObject _frontBody;
    RectTransform _frontPlate;
    Vector2 _frontPlatePos;
    Vector2 _frontPlateSize;
    bool _frontPlateSaved;
    // マス下の経験値バーは24pxはみ出す。ログ上端はそこからさらに下。カード上端はその8px下。
    const float FrontHostHeight = 100f;
    const float FrontRaise = 14f;
    Text _frontEmpty;
    Text _frontToggleLabel;
    readonly FrontSlot[] _frontSlots = new FrontSlot[3];
    readonly List<BattleUnit> _frontUnits = new List<BattleUnit>();
    Button _fastButton;
    Button _withdrawButton;
    QuestBoard _questBoard;
        SettingsBoard _settings;
        MessageBoard _messages;
        ItemBoard _items;
        GameObject _messageButton;
        GameObject _charmIcon;
        GameObject _messageBadge;
        Text _messageBadgeText;
    Transform _questOverlay;
    GameObject _questBanner;
    Image _questBannerFill;
    Text _questBannerText;
    GameObject _questBadge;
    Text _questBadgeText;
    readonly List<QuestDef> _questCandidates = new List<QuestDef>();
    static readonly Color EmptyCellColor = new Color(0.15f, 0.15f, 0.18f, 0.3f);
    static readonly Color HeaderBackColor = new Color(0f, 0f, 0f, 1f);
    static readonly Color HeaderRecordColor = new Color(0.40f, 0.41f, 0.44f, 1f);
    static readonly Color HeaderFillColor = new Color(0.22f, 0.50f, 0.26f, 1f);
    int _battleSpeed = 1;
    bool _running;
    bool _stop;
    bool _alliesVisible = true;
    bool _alliesEntered;
    bool _cuePixels;
    bool _cueWalk;
    bool _cueItem;
    int _cueItemId;
    int _cueR;
    int _cueG;
    int _cueB;
    long _cuePixelCount;
    Vector2 _itemDropHome;
    bool _itemDropHomeSet;
    int _nextUnitId = 1;
    int _battleStageIndex;
    StageBattleContent _stageContent;
    readonly List<string> _logs = new List<string>();
    readonly List<BattleUnit> _units = new List<BattleUnit>();
    readonly List<BattleUnit> _expPopUnits = new List<BattleUnit>();
    readonly List<long> _expPopAmounts = new List<long>();
    readonly Dictionary<uint, Sprite> _sprites = new Dictionary<uint, Sprite>();
    CellView[,] _allies;
    CellView[,] _enemies;
    Image[] _battlePeople;

    class FrontSlot
    {
        public GameObject Root;
        public Image Portrait;
        public Text Info;
        public Slider Hp;
        public Text HpText;
        public Slider Exp;
        public Text ExpText;
    }

    class CellView
    {
        public Image Image;
        public Image Portrait;
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
            unit.RawHpMax = hpMax;
            unit.RawAtk = (long)character.Stats[0].ATK;
            unit.RawDef = (long)character.Stats[0].DEF;
            int level = character.Level > int.MaxValue ? int.MaxValue : (int)character.Level;
            if (level < 1)
                level = 1;
            unit.Level = level;
            _production.ApplyPersonStats(unit);
            long delta = unit.HpMax - previousHp;
            if (delta > 0)
                unit.Hp += delta;
            if (unit.Hp > unit.HpMax)
                unit.Hp = unit.HpMax;
            if (unit.Hp < 1 && unit.IsLiving())
                unit.Hp = 1;
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
        _expPopUnits.Clear();
        _expPopAmounts.Clear();
        _stop = false;
        _alliesVisible = true;
        _alliesEntered = false;
        _nextUnitId = 1;
        PlaceAllies(setIndex);
        ShowBattlePeople(setIndex);
        if (!HasLivingAlly())
        {
            Debug.LogWarning("出撃できる味方がいません");
            _production.LeaveBattle();
            return;
        }

        Show();
        ApplyStageBackground(stageIndex);
        StartCoroutine(Run(stageIndex, floorFrom, floorTo));
    }

    IEnumerator Run(int stageIndex, int floorFrom, int floorTo)
    {
        _running = true;
        _battleStageIndex = stageIndex;
        StageBattleContent content = stageIndex == Constants.BATTLE_STAGE_GALLERY
            ? new StageBattleContent { StageIndex = stageIndex }
            : BattleStageCatalog.Get(stageIndex);
        _stageContent = content;
        string stageName = ControllerBattleStageClass.GetStageName(stageIndex);
        var sink = new RewardSink(this);
        bool clearedStage = false;
        bool partyGone = false;
        bool announcedClear = false;

        while (true)
        {
        int start = _production.GetActiveBattleFloor();
        if (start < floorFrom || start > floorTo)
            start = floorFrom;
        for (int floor = start; floor <= floorTo; floor++)
        {
            _production.SetActiveBattleFloor(floor);
            _itemDropHomeSet = false;
            ClearBattleFloats();
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
                ClearDropVisuals();
                ClearDropCue();
                BattleAttackResult step = fight.Step();
                if (!string.IsNullOrEmpty(step.Log))
                    AddLog(step.Log);
                _production.SyncAllyLivesFromBattle(_units);
                ShowBattleFloats(step);
                if (step.Acted && step.Actor != null)
                    yield return PlayAttackMotion(step.Actor, step.Target);
                if (step.Target != null && !step.Target.IsAlly && !step.Target.IsLiving())
                {
                    CellView fallen = CellOf(step.Target);
                    if (fallen != null)
                    {
                        _itemDropHome = fallen.Home;
                        _itemDropHomeSet = true;
                    }
                    if (_cuePixels || _cueWalk || _cueItem)
                        yield return PlayDefeatDrops(step.Target);
                }
                Refresh(stageName, floor, floorFrom, floorTo);
                if (_stop)
                    break;
                if (fight.Outcome != BattleOutcome.InProgress)
                    break;
                if (!(step.Acted && step.Actor != null))
                    yield return new WaitForSeconds(Interval());
            }

            ClearDropVisuals();

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
                AddLog(BattleLogFormat.Drop(ItemIds.DisplayName(drops[i]) + " を入手"));
            }
            if (drops.Count > 0)
                yield return PlayFloorItemDrops(drops);

            bool advanced = _production.MarkBattleFloorCleared(stageIndex, floor);
            _production.GrantPersonBattleFloor();
            _production.SetActiveBattleFloor(floor + 1);
            if (stageIndex != Constants.BATTLE_STAGE_GALLERY && floor >= Constants.BATTLE_STAGE_FLOOR_MAX)
            {
                clearedStage = true;
                if (!announcedClear)
                {
                    _production.MarkBattleStageCleared();
                    announcedClear = true;
                }
            }
            if (!HasLivingAlly())
                partyGone = true;
            Refresh(stageName, floor, floorFrom, floorTo);
            if (advanced)
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
            _production.SetActiveBattleFloor(floorFrom);
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
        _battleStageIndex = 0;
        _stageContent = null;
        _production.SyncAllyLivesFromBattle(_units);
        if (_production.GetActiveBattleStage() != 0)
            _production.LeaveBattle();
        _production.SaveGame();
        _production.UpdateRGBProductionScene();
        _production.RefreshProductionFigures();
        _production.ShowCharacterOwnedNum();
        ShowIdle();
        Hide();
        _production.ReturnFromBattle();
        ClearBattleFloats();
    }

    public AllyAuthorship[] AllyAuthorships()
    {
        var list = new List<AllyAuthorship>();
        for (int i = 0; i < _units.Count; i++)
        {
            BattleUnit unit = _units[i];
            if (unit == null || !unit.IsAlly)
                continue;
            CharacterClass character = _production != null ? _production.GetCharacter(unit.CharacterId) : null;
            list.Add(new AllyAuthorship
            {
                AuthorPersonId = character != null ? character.AuthorPersonId : 0,
                Type = unit.Attribute
            });
        }
        return list.ToArray();
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
                if (character == null || character.OwnedNumCur == 0 || _production.IsHospitalNurse(id))
                    continue;
                BattleUnit unit = BattleCharacterFactory.CreateAlly(character, x, y, _nextUnitId++, _config);
                if (unit == null)
                    continue;
                unit.RawHpMax = unit.HpMax;
                unit.RawAtk = unit.Atk;
                unit.RawDef = unit.Def;
                _units.Add(unit);
            }
        }
        _production.ApplyPersonStatsToAllies(_units);
    }

    void SpawnEnemies(StageBattleContent content, int floor)
    {
        BossFloorSpawn boss = content.FindBoss(floor);
        var picks = new List<EnemySpawnEntry>();
        double multiplier = 1.0;
        var rng = new SystemBattleRandom();
        if (content.StageIndex == Constants.BATTLE_STAGE_GALLERY)
        {
            picks = RollGalleryEnemies(floor, rng);
        }
        else if (boss != null)
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

    List<EnemySpawnEntry> RollGalleryEnemies(int floor, IBattleRandom rng)
    {
        var picks = new List<EnemySpawnEntry>();
        var ids = new List<uint>();
        _production.CollectImportedCharacterIds(ids);
        if (ids.Count == 0 || rng == null)
            return picks;

        int level = EnemySpawner.GalleryLevel(floor);
        int count = EnemySpawner.GalleryCount(floor);
        if (_config != null && count > _config.MaxUnits)
            count = _config.MaxUnits;
        if (count < 1)
            count = 1;
        for (int i = 0; i < count; i++)
        {
            uint id = ids[rng.NextInt(0, ids.Count)];
            picks.Add(new EnemySpawnEntry(id, 1, 1, 1, level));
        }
        return picks;
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
        double seconds = _config.ActionIntervalSeconds / BattleSpeed();
        if (seconds < 0.0)
            seconds = 0.0;
        return (float)seconds;
    }

    int BattleSpeed()
    {
        int cap = SpeedCap();
        if (_battleSpeed == 2 || _battleSpeed == 4 || _battleSpeed == 8 || _battleSpeed == 16)
            return _battleSpeed <= cap ? _battleSpeed : cap;
        return 1;
    }

    int SpeedCap()
    {
        if (_production == null)
            return 16;
        return _production.GetBattleSpeedCap();
    }

    float ScaledSeconds(float normalSeconds)
    {
        return normalSeconds / BattleSpeed();
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
            if (attacker != null && attacker.Portrait != null)
            {
                float hop = Mathf.Sin(u * Mathf.PI) * AttackHop;
                NudgePortrait(attacker, new Vector2(0f, hop));
            }
            if (defender != null && defender != attacker && defender.Portrait != null)
            {
                float shake = Mathf.Sin(u * Mathf.PI * 4f) * (1f - u) * HitShake;
                NudgePortrait(defender, new Vector2(shake, 0f));
            }
            yield return null;
        }

        RestoreCell(attacker);
        RestoreCell(defender);
    }

    void QueueExpFloat(BattleUnit ally, long exp)
    {
        if (ally == null || exp <= 0)
            return;
        _expPopUnits.Add(ally);
        _expPopAmounts.Add(exp);
    }

    // ダメージは受けたキャラの右、真ん中の高さから浮かぶ。経験値は味方の右下に出て、その場で消える。
    void ShowBattleFloats(BattleAttackResult step)
    {
        if (step.Acted && step.Target != null && step.Damage > 0)
        {
            CellView cell = CellOf(step.Target);
            if (cell != null && cell.Image != null)
                BattleFloatText.Show(cell.Image.rectTransform, true, step.Damage);
        }
        for (int i = 0; i < _expPopUnits.Count; i++)
        {
            CellView cell = CellOf(_expPopUnits[i]);
            if (cell != null && cell.Image != null)
                BattleFloatText.Show(cell.Image.rectTransform, false, _expPopAmounts[i]);
        }
        _expPopUnits.Clear();
        _expPopAmounts.Clear();
    }

    void ClearBattleFloats()
    {
        _expPopUnits.Clear();
        _expPopAmounts.Clear();
        if (_root != null)
            BattleFloatText.Clear(_root.transform);
    }

    void ClearDropCue()
    {
        _cuePixels = false;
        _cueWalk = false;
        _cueR = 0;
        _cueG = 0;
        _cueB = 0;
        _cuePixelCount = 0;
        _cueItem = false;
        _cueItemId = 0;
    }

    const float PixelSize = 16f;
    const int PixelVisualMax = 5;

    // 仲間になった敵は左へてくてく歩き、落ちたピクセルはその色の四角が3回跳ねて消える。
    void GrantCharacterDrops(uint characterId)
    {
        if (_stageContent == null || _production == null || characterId == 0)
            return;
        var got = new List<int>();
        _stageContent.RollCharacterDrops(_production.GetActiveBattleFloor(), characterId, new SystemBattleRandom(), got);
        for (int i = 0; i < got.Count; i++)
        {
            _production.AddBattleItem(got[i], 1);
            AddLog(BattleLogFormat.Drop(ItemIds.DisplayName(got[i]) + " を入手"));
            _cueItem = true;
            _cueItemId = got[i];
        }
    }

    IEnumerator PlayDefeatDrops(BattleUnit enemy)
    {
        CellView cell = CellOf(enemy);
        if (cell == null || cell.Image == null)
            yield break;

        Vector2 origin = cell.Home;
        float walkTime = ScaledSeconds(0.95f);
        float bounceTime = ScaledSeconds(0.7f);
        Image walker = null;
        if (_cueWalk && cell.Portrait != null && cell.Portrait.sprite != null)
        {
            walker = CreateImage("DropWalker", _root.transform, Color.white);
            walker.sprite = cell.Portrait.sprite;
            walker.preserveAspect = true;
            walker.raycastTarget = false;
            PlaceRect(walker.rectTransform, origin.x, origin.y, 70f, 70f);
            walker.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            walker.transform.SetAsLastSibling();
            RememberDrop(walker.gameObject, walkTime);
            SetPortrait(cell, null);
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
                RememberDrop(frame.gameObject, bounceTime);
            }
        }

        Image item = null;
        int itemHops = 3;
        Vector2 feet = DropAnchor(new Vector2(origin.x, origin.y - 35f));
        float itemTime = 0f;
        if (_cueItem)
        {
            item = CreateDropItem(_cueItemId, feet);
            if (item != null)
            {
                itemHops = ItemHopCount(_cueItemId);
                itemTime = ItemHopDuration() + ItemSuckDuration();
                RememberDrop(item.gameObject, itemTime + 0.4f);
            }
        }

        float total = 0f;
        if (walker != null)
            total = walkTime;
        if (pixels != null && bounceTime > total)
            total = bounceTime;
        if (item != null && itemTime > total)
            total = itemTime;

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
            if (item != null)
                PlaceItemFlight(item, feet, elapsed, ItemHopDuration(), ItemSuckDuration(), itemHops);
            yield return null;
        }

        ClearDropVisuals();
    }

    IEnumerator PlayFloorItemDrops(List<int> itemIds)
    {
        if (itemIds == null || itemIds.Count == 0 || _root == null)
            yield break;
        Vector2 home = _itemDropHomeSet ? _itemDropHome : EnemySideHome();
        Vector2 feet = DropAnchor(new Vector2(home.x, home.y - 35f));
        var icons = new List<Image>();
        var hops = new List<int>();
        for (int i = 0; i < itemIds.Count; i++)
        {
            Image icon = CreateDropItem(itemIds[i], feet);
            if (icon == null)
                continue;
            icons.Add(icon);
            hops.Add(ItemHopCount(itemIds[i]));
        }
        if (icons.Count == 0)
            yield break;

        float hop = ItemHopDuration();
        float suck = ItemSuckDuration();
        float flight = hop + suck;
        float stagger = hop * 0.16f;
        float total = flight + stagger * (icons.Count - 1);
        float elapsed = 0f;
        while (elapsed < total && !_stop)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < icons.Count; i++)
            {
                if (icons[i] == null)
                    continue;
                float local = elapsed - stagger * i;
                PlaceItemFlight(icons[i], feet, local, hop, suck, hops[i]);
            }
            yield return null;
        }
        for (int i = 0; i < icons.Count; i++)
        {
            if (icons[i] != null)
                Destroy(icons[i].gameObject);
        }
    }

    Image CreateDropItem(int itemId, Vector2 feet)
    {
        Sprite sprite = ItemImages.SpriteOf(itemId);
        if (sprite == null || _root == null)
            return null;
        Transform parent = _questOverlay != null ? _questOverlay : _root.transform;
        Image icon = CreateImage("DropItem", parent, Color.white);
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        PlaceRect(icon.rectTransform, feet.x, feet.y, 48f, 48f);
        icon.rectTransform.localScale = new Vector3(0.2f, 0.2f, 1f);
        icon.transform.SetAsLastSibling();
        return icon;
    }

    Vector2 EnemySideHome()
    {
        if (_enemies != null && _config != null)
        {
            int mid = (_config.FormationSize + 1) / 2;
            if (mid >= 1 && mid <= _config.FormationSize)
            {
                CellView view = _enemies[mid, mid];
                if (view != null)
                    return view.Home;
            }
        }
        return new Vector2(170f, -14f);
    }

    float ItemHopDuration()
    {
        float duration = ScaledSeconds(0.92f);
        if (duration < 0.24f)
            duration = 0.24f;
        return duration;
    }

    float ItemSuckDuration()
    {
        float duration = ScaledSeconds(0.42f);
        if (duration < 0.18f)
            duration = 0.18f;
        return duration;
    }

    const float ItemHopTravel = 112f;
    Coroutine _itemPulse;

    // 跳ねたあと、アイテム一覧ボタンへ縮みながら入って消える。
    void PlaceItemFlight(Image icon, Vector2 feet, float elapsed, float hopTime, float suckTime, int hops)
    {
        if (icon == null)
            return;
        if (elapsed < hopTime)
        {
            PlaceItemHop(icon, feet, hopTime <= 0f ? 1f : elapsed / hopTime, hops);
            return;
        }
        float u = suckTime <= 0f ? 1f : (elapsed - hopTime) / suckTime;
        bool arrived = u >= 1f;
        if (u > 1f)
            u = 1f;
        PlaceItemSuck(icon, feet + new Vector2(ItemHopTravel, 0f), ItemPocket(), u);
        if (arrived && icon.gameObject.activeSelf)
        {
            icon.gameObject.SetActive(false);
            NudgeItemButton();
        }
    }

    static void PlaceItemSuck(Image icon, Vector2 from, Vector2 pocket, float u)
    {
        if (icon == null)
            return;
        if (u < 0f)
            u = 0f;
        if (u > 1f)
            u = 1f;
        float move = u * u;
        Vector2 pos = Vector2.Lerp(from, pocket, move);
        pos.y += Mathf.Sin(u * Mathf.PI) * 28f;
        float scale = Mathf.Lerp(0.84f, 0.16f, move);
        icon.rectTransform.anchoredPosition = pos;
        icon.rectTransform.localScale = new Vector3(scale, scale, 1f);
        icon.color = Color.white;
    }

    Vector2 DropAnchor(Vector2 panelPoint)
    {
        if (_root == null)
            return panelPoint;
        RectTransform panel = _root.GetComponent<RectTransform>();
        Transform parent = _questOverlay != null ? _questOverlay : _root.transform;
        RectTransform space = parent as RectTransform;
        if (panel == null || space == null || space == panel)
            return panelPoint;
        Vector2 anchor = new Vector2(
            (0.5f - panel.pivot.x) * panel.rect.width,
            (0.5f - panel.pivot.y) * panel.rect.height);
        Vector3 world = panel.TransformPoint(new Vector3(anchor.x + panelPoint.x, anchor.y + panelPoint.y, 0f));
        return AnchoredFromWorld(space, world);
    }

    Vector2 ItemPocket()
    {
        RectTransform space = (_questOverlay != null ? _questOverlay : _root.transform) as RectTransform;
        RectTransform button = _questOverlay != null ? _questOverlay.Find("ButtonItemList") as RectTransform : null;
        if (space == null || button == null)
            return new Vector2(430f, -220f);
        return AnchoredFromWorld(space, button.TransformPoint(button.rect.center));
    }

    static Vector2 AnchoredFromWorld(RectTransform space, Vector3 world)
    {
        Vector3 local = space.InverseTransformPoint(world);
        Vector2 anchor = new Vector2(
            (0.5f - space.pivot.x) * space.rect.width,
            (0.5f - space.pivot.y) * space.rect.height);
        return new Vector2(local.x - anchor.x, local.y - anchor.y);
    }

    void NudgeItemButton()
    {
        if (_itemPulse != null)
            StopCoroutine(_itemPulse);
        _itemPulse = StartCoroutine(PulseItemButton());
    }

    IEnumerator PulseItemButton()
    {
        RectTransform button = _questOverlay != null ? _questOverlay.Find("ButtonItemList") as RectTransform : null;
        if (button == null)
            yield break;
        float duration = 0.18f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = elapsed / duration;
            if (u > 1f)
                u = 1f;
            float scale = 1f + Mathf.Sin(u * Mathf.PI) * 0.14f;
            if (button != null)
                button.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        if (button != null)
            button.localScale = Vector3.one;
        _itemPulse = null;
    }

    static int ItemHopCount(int itemId)
    {
        return (itemId / 8) % 2 == 0 ? 3 : 2;
    }

    static void PlaceItemHop(Image icon, Vector2 feet, float u, int hops)
    {
        if (icon == null)
            return;
        if (u < 0f)
        {
            icon.rectTransform.localScale = new Vector3(0.2f, 0.2f, 1f);
            icon.rectTransform.anchoredPosition = feet;
            return;
        }
        if (u > 1f)
            u = 1f;
        float along;
        float rise;
        float scale;
        float alpha;
        SampleItemHop(u, hops, out along, out rise, out scale, out alpha);
        icon.rectTransform.anchoredPosition = new Vector2(feet.x + along, feet.y + rise);
        icon.rectTransform.localScale = new Vector3(scale, scale, 1f);
        Color color = Color.white;
        color.a = alpha;
        icon.color = color;
    }

    // 足元から右へ、2回か3回跳ねて着地する。収納はそのあと。
    static void SampleItemHop(float u, int hops, out float along, out float rise, out float scale, out float alpha)
    {
        if (hops < 2)
            hops = 2;
        if (hops > 3)
            hops = 3;
        float[] share = hops == 2 ? new float[] { 0.56f, 0.44f } : new float[] { 0.40f, 0.33f, 0.27f };
        float[] height = hops == 2 ? new float[] { 72f, 30f } : new float[] { 70f, 36f, 16f };
        float[] endX = hops == 2 ? new float[] { 0.64f, 1f } : new float[] { 0.46f, 0.78f, 1f };
        const float travel = ItemHopTravel;
        float start = 0f;
        float prevX = 0f;
        along = 0f;
        rise = 0f;
        scale = 1f;
        alpha = 1f;
        for (int i = 0; i < hops; i++)
        {
            float end = start + share[i];
            if (u > end && i < hops - 1)
            {
                prevX = endX[i];
                start = end;
                continue;
            }
            float local = share[i] <= 0f ? 1f : (u - start) / share[i];
            if (local < 0f)
                local = 0f;
            if (local > 1f)
                local = 1f;
            float move = local * local * (3f - 2f * local);
            along = Mathf.Lerp(prevX, endX[i], move) * travel;
            rise = Mathf.Sin(local * Mathf.PI) * height[i];
            scale = 1f;
            if (i == 0 && local < 0.22f)
                scale = Mathf.Lerp(0.2f, 1.18f, local / 0.22f);
            else if (i == 0 && local < 0.36f)
                scale = Mathf.Lerp(1.18f, 1f, (local - 0.22f) / 0.14f);
            if (local > 0.84f)
            {
                float land = (local - 0.84f) / 0.16f;
                float squash = Mathf.Lerp(1f, 0.84f, land);
                if (scale > squash)
                    scale = squash;
            }
            return;
        }
    }

    sealed class BattleDrop
    {
        public GameObject Object;
        public float DieAt;
    }

    readonly List<BattleDrop> _battleDrops = new List<BattleDrop>();

    void RememberDrop(GameObject obj, float life)
    {
        if (obj == null)
            return;
        if (life < 0.05f)
            life = 0.05f;
        _battleDrops.Add(new BattleDrop { Object = obj, DieAt = Time.time + life });
    }

    void TickDrops()
    {
        float now = Time.time;
        for (int i = _battleDrops.Count - 1; i >= 0; i--)
        {
            BattleDrop drop = _battleDrops[i];
            if (drop.Object == null || now >= drop.DieAt)
            {
                if (drop.Object != null)
                    Destroy(drop.Object);
                _battleDrops.RemoveAt(i);
            }
        }
    }

    void ClearDropVisuals()
    {
        for (int i = _battleDrops.Count - 1; i >= 0; i--)
        {
            if (_battleDrops[i].Object != null)
                Destroy(_battleDrops[i].Object);
        }
        _battleDrops.Clear();
        if (_root == null)
            return;
        for (int i = _root.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = _root.transform.GetChild(i);
            if (child.name == "DropPixel" || child.name == "DropWalker" || child.name == "DropItem")
                Destroy(child.gameObject);
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
        return ScaledSeconds(0.8f);
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
            {
                for (int i = 0; i < cells.Count; i++)
                    NudgePortrait(cells[i], Vector2.zero);
                yield break;
            }
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
                cells[i].Image.rectTransform.anchoredPosition = pos;
                NudgePortrait(cells[i], new Vector2(0f, hop));
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
                SetPortrait(view, null);
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
        NudgePortrait(view, Vector2.zero);
    }

    static void NudgePortrait(CellView view, Vector2 offset)
    {
        if (view == null || view.Portrait == null)
            return;
        view.Portrait.rectTransform.anchoredPosition = offset;
    }

    static void SetPortrait(CellView view, Sprite sprite)
    {
        if (view == null || view.Image == null)
            return;
        view.Image.sprite = null;
        bool show = sprite != null;
        view.Image.color = show ? new Color(0f, 0f, 0f, 0f) : EmptyCellColor;
        if (view.Portrait == null)
            return;
        view.Portrait.sprite = sprite;
        view.Portrait.color = Color.white;
        view.Portrait.gameObject.SetActive(show);
    }

    void PushFast()
    {
        if (_production == null || !_production.IsMainFeatureOpen(MainFeature.BattleSpeed))
            return;
        int cap = SpeedCap();
        int next = _battleSpeed < 2 ? 2 : _battleSpeed * 2;
        if (next > cap || (next != 2 && next != 4 && next != 8 && next != 16))
            next = 1;
        _battleSpeed = next;
        ApplySpeedButton();
        if (_production != null)
        {
            _production.SetBattleSpeed(_battleSpeed);
            _production.SaveGame();
        }
    }

    public void ApplySavedSpeed(int speed)
    {
        int cap = SpeedCap();
        if ((speed == 2 || speed == 4 || speed == 8 || speed == 16) && speed <= cap)
            _battleSpeed = speed;
        else if (speed > cap && cap >= 2)
            _battleSpeed = cap;
        else
            _battleSpeed = 1;
        ApplySpeedButton();
    }

    public void RefreshFeatureLocks()
    {
        ApplySpeedButton();
    }

    void ApplySpeedButton()
    {
        if (_fastButton == null)
            return;
        bool open = _production != null && _production.IsMainFeatureOpen(MainFeature.BattleSpeed);
        _fastButton.interactable = open;
        Image image = _fastButton.GetComponent<Image>();
        if (image != null)
            image.color = open ? new Color(0.25f, 0.45f, 0.75f, 1f) : new Color(0.45f, 0.45f, 0.45f, 1f);
        Text label = _fastButton.GetComponentInChildren<Text>();
        if (label == null)
            return;
        label.color = open ? Color.white : new Color(0.62f, 0.62f, 0.62f, 1f);
        if (_battleSpeed == 2)
            label.text = "2倍";
        else if (_battleSpeed == 4)
            label.text = "4倍";
        else if (_battleSpeed == 8)
            label.text = "8倍";
        else if (_battleSpeed == 16)
            label.text = "16倍";
        else
            label.text = "通常";
    }

    public bool RequestWithdraw()
    {
        if (!_running)
            return false;
        _stop = true;
        return true;
    }

    // 別ステージへ出撃し直すとき、撤退の完了を待たずに戦闘だけ止める。編成の解除は呼び出し元が行う。
    public void EndRunningBattle()
    {
        if (!_running && !_stop)
            return;
        StopAllCoroutines();
        ClearDropVisuals();
        _running = false;
        _stop = false;
        _battleStageIndex = 0;
        _stageContent = null;
        if (_production != null)
            _production.SyncAllyLivesFromBattle(_units);
        _units.Clear();
        DrawSide(_allies, true);
        DrawSide(_enemies, false);
        ShowIdle();
        Hide();
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

    public void RefreshStageTitle()
    {
        if (_header == null || _production == null || !_running)
            return;
        int stageIndex = _battleStageIndex != 0 ? _battleStageIndex : _production.GetActiveBattleStage();
        if (stageIndex == 0)
            return;
        string stageName = ControllerBattleStageClass.GetStageName(stageIndex);
        SetStageTitle(stageName, _production.GetActiveBattleFloor(), _production.GetActiveBattleFloorFrom(), _production.GetActiveBattleFloorTo());
    }

    string FormatStageTitle(string stageName, int floor, int floorFrom, int floorTo)
    {
        return stageName + "  " + floor.ToString() + "階  （" + floorFrom.ToString() + "〜" + floorTo.ToString() + "）";
    }

    void SetStageTitle(string stageName, int floor, int floorFrom, int floorTo)
    {
        if (_header == null)
            return;
        _header.text = FormatStageTitle(stageName, floor, floorFrom, floorTo);
        PlaceRepeatMark();
    }

    // 見出しのフォントは絵文字を描けない。文字に混ぜると空白のまま幅を取り、ステージ名がずれる。
    void PlaceRepeatMark()
    {
        bool on = _running && _production != null && _production.IsBattleRepeatOn();
        if (!on)
        {
            if (_repeatMark != null)
                _repeatMark.gameObject.SetActive(false);
            return;
        }
        if (_header == null)
            return;
        if (_repeatMark == null)
        {
            Texture2D texture = Resources.Load<Texture2D>("RepeatMark");
            if (texture == null)
                return;
            GameObject obj = new GameObject("ImageBattleRepeatMark");
            obj.transform.SetParent(_header.transform, false);
            RawImage image = obj.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.color = Color.white;
            // 余白を除いた矢印の範囲。画像の上からの位置を、下原点のUVにしている。
            const float tex = 1024f;
            image.uvRect = new Rect(50f / tex, (tex - 1000f) / tex, 924f / tex, 950f / tex);
            _repeatMark = image;
        }
        _repeatMark.gameObject.SetActive(true);
        float textWidth = _header.preferredWidth;
        if (textWidth < 8f)
            textWidth = _header.fontSize * Mathf.Max(1, _header.text.Length) * 0.55f;
        float size = _header.fontSize * 0.75f;
        RectTransform mark = _repeatMark.rectTransform;
        mark.anchorMin = new Vector2(0.5f, 0.5f);
        mark.anchorMax = new Vector2(0.5f, 0.5f);
        mark.pivot = new Vector2(1f, 0.5f);
        mark.sizeDelta = new Vector2(size * (924f / 950f), size);
        mark.anchoredPosition = new Vector2(-textWidth * 0.5f - 4f, 0f);
    }

    void Refresh(string stageName, int floor, int floorFrom, int floorTo)
    {
        SetStageTitle(stageName, floor, floorFrom, floorTo);
        SetHeaderProgress(floor);
        if (_resource != null)
            _resource.text = _production.GetRgbText();
        if (_questBoard != null)
            _questBoard.Refresh();
        RefreshQuestBanner();
        DrawSide(_allies, true);
        DrawSide(_enemies, false);
        if (_production != null)
            ShowBattlePeople(_production.GetActiveBattlePartySet());
        PaintFrontStatus();
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
                    SetPortrait(view, null);
                    SetMetersVisible(view, false, false);
                    continue;
                }

                SetPortrait(view, SpriteOf(unit.CharacterId));
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
        if (level < 1)
            level = 1;
        string body = "Lv " + UiNumber.Format(level);
        if (!unit.IsAlly)
            return body;

        long cap = _config != null ? _config.MaxLevel : 0;
        string levelText = UiNumber.LevelText(body, level, cap);
        int lives = unit.Lives;
        if (lives < 0)
            lives = 0;
        return levelText + " / " + UiNumber.Format(lives);
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
        RefreshCharmIcon();
    }

    public void ApplyStageBackground(int stageIndex)
    {
        if (_root == null)
            _root = GameObject.Find("PanelBattle");
        if (_root == null)
            return;
        Image image = _root.GetComponent<Image>();
        if (image == null)
            return;
        if (!_fieldStored)
        {
            _fieldSprite = image.sprite;
            _fieldType = image.type;
            _fieldStored = true;
        }

        if (stageIndex == _shownStageIndex)
            return;

        Sprite sprite = BattleStageCatalog.BackgroundSprite(stageIndex);
        int previous = _shownStageIndex;
        bool wipe = previous > 0 && stageIndex > 0 && sprite != null;
        if (_stageWiping)
            FinishStageWipe();
        _shownStageIndex = stageIndex;
        if (!wipe)
        {
            HideStageWipe();
            PaintStageImage(image, sprite);
            return;
        }

        PlayStageWipe(image, sprite, stageIndex > previous);
    }

    void Update()
    {
        TickDrops();
        if (!_stageWiping || _stageWipeMaterial == null)
            return;
        _stageWipeTime += Time.deltaTime;
        PlaceStageTransition();
        float t = _stageWipeTime / StageWipeSeconds;
        if (t >= 1f)
        {
            FinishStageWipe();
            return;
        }
        float remain = 1f - t;
        SetStageDim(1f - remain * remain);
        _stageWipeMaterial.SetFloat("_Progress", Mathf.SmoothStep(0f, 1f, t));
    }

    void PlayStageWipe(Image baseImage, Sprite sprite, bool forward)
    {
        if (!EnsureStageWipe())
        {
            PaintStageImage(baseImage, sprite);
            return;
        }

        _stageWipeSprite = sprite;
        _stageWipeImage.sprite = sprite;
        _stageWipeImage.color = Color.white;
        _stageWipeImage.gameObject.SetActive(true);
        if (_stageDimImage != null)
        {
            _stageDimImage.gameObject.SetActive(true);
            SetStageDim(0f);
        }
        PlaceStageTransition();
        Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
        _stageWipeMaterial.SetVector("_UvRect", uv);
        _stageWipeMaterial.SetFloat("_Direction", forward ? 1f : -1f);
        _stageWipeMaterial.SetFloat("_Softness", 0.34f);
        _stageWipeMaterial.SetFloat("_Progress", 0f);
        _stageWipeTime = 0f;
        _stageWiping = true;
    }

    void FinishStageWipe()
    {
        _stageWiping = false;
        if (_root != null && _stageWipeSprite != null)
        {
            Image image = _root.GetComponent<Image>();
            if (image != null)
                PaintStageImage(image, _stageWipeSprite);
        }
        _stageWipeSprite = null;
        HideStageWipe();
    }

    void PlaceStageTransition()
    {
        if (_stageDimImage != null)
            _stageDimImage.transform.SetAsFirstSibling();
        if (_stageWipeImage == null)
            return;
        int index = _stageDimImage != null ? _stageDimImage.transform.GetSiblingIndex() + 1 : 0;
        _stageWipeImage.transform.SetSiblingIndex(index);
    }

    void SetStageDim(float alpha)
    {
        if (_stageDimImage == null)
            return;
        if (alpha < 0f)
            alpha = 0f;
        if (alpha > 1f)
            alpha = 1f;
        _stageDimImage.color = new Color(0f, 0f, 0f, alpha);
    }

    void HideStageWipe()
    {
        if (_stageWipeImage != null)
            _stageWipeImage.gameObject.SetActive(false);
        if (_stageDimImage != null)
            _stageDimImage.gameObject.SetActive(false);
    }

    bool EnsureStageWipe()
    {
        if (_stageWipeImage != null && _stageWipeMaterial != null)
            return true;
        Shader shader = Resources.Load<Shader>("StageWipe");
        if (shader == null)
            shader = Shader.Find("UI/StageWipe");
        if (shader == null)
            return false;
        if (_stageWipeMaterial == null)
        {
            _stageWipeMaterial = new Material(shader);
            _stageWipeMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        if (_root == null)
            return false;
        if (_stageWipeImage == null)
        {
            GameObject obj = new GameObject("StageWipe", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(_root.transform, false);
            _stageWipeImage = obj.GetComponent<Image>();
            _stageWipeImage.raycastTarget = false;
            _stageWipeImage.type = Image.Type.Simple;
            _stageWipeImage.preserveAspect = false;
            _stageWipeImage.material = _stageWipeMaterial;
            StretchStageOverlay(_stageWipeImage.rectTransform);
        }
        if (_stageDimImage == null && _root != null)
        {
            GameObject dim = new GameObject("StageDim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dim.transform.SetParent(_root.transform, false);
            _stageDimImage = dim.GetComponent<Image>();
            _stageDimImage.sprite = StageDimSprite();
            _stageDimImage.raycastTarget = false;
            _stageDimImage.type = Image.Type.Simple;
            _stageDimImage.color = new Color(0f, 0f, 0f, 0f);
            StretchStageOverlay(_stageDimImage.rectTransform);
            dim.SetActive(false);
        }
        return _stageWipeImage != null;
    }

    static void StretchStageOverlay(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static Sprite _stageDimSprite;

    static Sprite StageDimSprite()
    {
        if (_stageDimSprite != null)
            return _stageDimSprite;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _stageDimSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        _stageDimSprite.hideFlags = HideFlags.HideAndDontSave;
        return _stageDimSprite;
    }

    void PaintStageImage(Image image, Sprite sprite)
    {
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            return;
        }

        image.sprite = _fieldSprite;
        image.type = _fieldType;
        image.color = Color.white;
    }

    void OnDestroy()
    {
        if (_stageWipeMaterial != null)
            Destroy(_stageWipeMaterial);
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    public void RefreshQuestBoard()
    {
        BringQuestForward();
        if (_questBoard != null)
            _questBoard.Refresh();
        RefreshQuestBanner();
    }

    public void BringQuestForward()
    {
        if (_questOverlay == null || _questOverlay.parent == null)
            return;
        int last = _questOverlay.parent.childCount - 1;
        if (_questOverlay.GetSiblingIndex() != last)
            _questOverlay.SetAsLastSibling();
    }

    void ShowIdle()
    {
        Show();
        if (_header != null)
            _header.text = "バトル";
        if (_repeatMark != null)
            _repeatMark.gameObject.SetActive(false);
        SetHeaderProgress(0);
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
        RefreshQuestBanner();
        if (_production != null)
            ShowBattlePeople(_production.GetActiveBattlePartySet());
        PaintFrontStatus();
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
            _logText.supportRichText = true;
            _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _logText.verticalOverflow = VerticalWrapMode.Overflow;
            _logText.raycastTarget = false;
            _logText.resizeTextForBestFit = false;
        }

        _header = CreateText("TextBattleHeader", _root.transform, 28, TextAnchor.MiddleLeft);
        PlaceRect(_header.rectTransform, 110f, 175f, 620f, 44f);

        _allies = new CellView[_config.FormationSize + 1, _config.FormationSize + 1];
        _enemies = new CellView[_config.FormationSize + 1, _config.FormationSize + 1];
        BuildGrid(_allies, -170f);
        BuildGrid(_enemies, 170f);
        BuildBattlePeople();

        if (_logText == null)
        {
            _logText = CreateText("TextBattleLog", _root.transform, 24, TextAnchor.UpperLeft);
            RectTransform logRect = _logText.rectTransform;
            logRect.anchorMin = new Vector2(0.5f, 0f);
            logRect.anchorMax = new Vector2(0.5f, 0f);
            logRect.pivot = new Vector2(0.5f, 0f);
            logRect.anchoredPosition = new Vector2(0f, 24f);
            logRect.sizeDelta = new Vector2(1040f, 110f);
            _logText.supportRichText = true;
            _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _logText.verticalOverflow = VerticalWrapMode.Overflow;
            _logText.raycastTarget = false;
        }

        _withdrawButton = CreateButton("ButtonBattleWithdraw", "撤退", new Color(0.75f, 0.28f, 0.24f, 1f));
        PlaceRect(_withdrawButton.GetComponent<RectTransform>(), -430f, 175f, 120f, 44f);
        _withdrawButton.onClick.AddListener(PushWithdraw);
        BuildCharmIcon();

        _fastButton = CreateButton("ButtonBattleFast", "通常", new Color(0.25f, 0.45f, 0.75f, 1f));
        PlaceRect(_fastButton.GetComponent<RectTransform>(), -290f, 175f, 120f, 44f);
        _fastButton.onClick.AddListener(PushFast);
        ApplySpeedButton();

        PlaceTextPlate(_header);
        PlaceTextPlate(_logText);
        BuildFrontStatus();
        BuildQuestAccess();
        StartCoroutine(FitTextPlates());
    }

    IEnumerator FitTextPlates()
    {
        yield return null;
        SpaceBattleText();
    }

    // 文字の板を、ボタンとキャラのマスから離して置く
    void SpaceBattleText()
    {
        if (_root == null || _header == null)
            return;
        RectTransform panel = _root.GetComponent<RectTransform>();
        float panelH = panel != null ? panel.rect.height : 0f;
        float panelW = panel != null ? panel.rect.width : 0f;
        if (panelH < 8f || panelW < 8f)
            return;

        float gridTop;
        float gridBottom;
        if (CellSpan(out gridTop, out gridBottom) == false)
            return;

        const float gap = 14f;
        float questLeft = 430f - 75f;
        float right = questLeft - gap;
        float panelLeft = -panelW * 0.5f + 12f;

        RectTransform header = _header.rectTransform;
        float headerH = header.sizeDelta.y;
        float resourceH = _resource != null ? _resource.rectTransform.sizeDelta.y : 32f;
        const float headerDrop = 24f;
        float headerY = gridTop + gap + resourceH + gap + headerH * 0.5f - headerDrop;
        float resourceY = gridTop + gap + resourceH * 0.5f;
        float headerBottom = headerY - headerH * 0.5f;
        float resourceTop = resourceY + resourceH * 0.5f;
        if (headerBottom < resourceTop + 6f)
        {
            float resourceDrop = resourceTop + 6f - headerBottom;
            float maxDrop = resourceY - resourceH * 0.5f - (gridTop + 4f);
            if (maxDrop < 0f)
                maxDrop = 0f;
            if (resourceDrop > maxDrop)
                resourceDrop = maxDrop;
            resourceY -= resourceDrop;
            if (headerY - headerH * 0.5f < resourceY + resourceH * 0.5f + 6f)
                headerY = resourceY + resourceH * 0.5f + 6f + headerH * 0.5f;
        }
        float shift = headerY - header.anchoredPosition.y;

        RectTransform fast = _fastButton != null ? _fastButton.GetComponent<RectTransform>() : null;
        float headerLeft = panelLeft;
        if (fast != null)
        {
            fast.anchoredPosition += new Vector2(0f, shift);
            headerLeft = fast.anchoredPosition.x + fast.sizeDelta.x * 0.5f + gap;
        }
        if (_withdrawButton != null)
        {
            RectTransform withdrawRect = _withdrawButton.GetComponent<RectTransform>();
            withdrawRect.anchoredPosition += new Vector2(0f, shift);
        }

        float headerWidth = right - headerLeft;
        if (headerWidth < 80f)
            headerWidth = 80f;
        header.anchoredPosition = new Vector2(headerLeft + headerWidth * 0.5f, headerY);
        header.sizeDelta = new Vector2(headerWidth, headerH);
        _header.alignment = TextAnchor.MiddleCenter;

        PlaceQuestBannerInPanel(gap);

        if (_logText != null)
        {
            RectTransform log = _logText.rectTransform;
            float logH = log.sizeDelta.y;
            if (logH < 128f)
                logH = 128f;
            const float logDrop = 32f;
            float logTop = gridBottom - gap - logDrop;
            float logBottom = logTop - logH;
            float panelBottom = -panelH * 0.5f + 8f;
            if (logBottom < panelBottom)
            {
                logBottom = panelBottom;
                logH = logTop - logBottom;
                if (logH < 64f)
                    logH = 64f;
            }
            log.anchoredPosition = new Vector2(0f, logBottom + panelH * 0.5f);
            log.sizeDelta = new Vector2(panelW - 24f, logH);
        }

        PlaceTextPlate(_header);
        PlaceTextPlate(_logText);
        InsetLogForToggle();
        FitFrontStatus();
    }

    // クエストのバーは、消したセーブボタンの空きへ上げる。撤退・速度・ステージ名は、バーがあった高さへ上げる
    void PlaceQuestBannerInPanel(float gap)
    {
        if (_questBanner == null || _root == null || _header == null)
            return;
        RectTransform banner = _questBanner.GetComponent<RectTransform>();
        RectTransform parent = banner.parent as RectTransform;
        RectTransform panelRect = _root.GetComponent<RectTransform>();
        if (parent == null || panelRect == null)
            return;

        Vector2 panelMin;
        Vector2 panelMax;
        Vector2 headerMin;
        Vector2 headerMax;
        if (TryBoundsIn(panelRect, parent, out panelMin, out panelMax) == false)
            return;
        if (TryBoundsIn(_header.rectTransform, parent, out headerMin, out headerMax) == false)
            return;
        if (headerMax.y < headerMin.y)
            return;

        const float margin = 10f;
        const float bannerH = 42f;
        float left = panelMin.x + margin;
        float right = panelMax.x - margin;
        float limitTop = panelMax.y - margin;

        Transform quest = parent.Find("ButtonBattleQuest");
        Vector2 questMin;
        Vector2 questMax;
        if (quest is RectTransform && TryBoundsIn((RectTransform)quest, parent, out questMin, out questMax))
        {
            float bannerBottom = limitTop - bannerH;
            bool overlaps = questMax.y > bannerBottom && questMin.y < limitTop;
            if (overlaps && questMin.x - gap < right)
                right = questMin.x - gap;
        }
        if (right > panelMax.x - margin)
            right = panelMax.x - margin;
        if (right < left + 80f)
            right = left + 80f;

        float bottom = headerMax.y + gap;
        float top = bottom + bannerH;
        if (top > limitTop)
        {
            top = limitTop;
            bottom = top - bannerH;
        }
        if (bottom < panelMin.y + margin)
            bottom = panelMin.y + margin;
        LiftHeaderRow((bottom + top) * 0.5f);

        const float bannerDrawH = 50f;
        float bannerCenter = panelMax.y + 38f;
        float canvasTop = parent.rect.yMax;
        if (bannerCenter + bannerDrawH * 0.5f > canvasTop - 8f)
            bannerCenter = canvasTop - 8f - bannerDrawH * 0.5f;
        left = panelMin.x + margin;
        right = panelMax.x - margin;
        InsetBannerEdge(parent.Find("ButtonSettings"), parent, gap, ref left, true);
        InsetBannerEdge(parent.Find("ButtonBattleQuest"), parent, gap, ref right, false);
        if (right < left + 80f)
            right = left + 80f;

        banner.anchorMin = new Vector2(0.5f, 0.5f);
        banner.anchorMax = new Vector2(0.5f, 0.5f);
        banner.pivot = new Vector2(0.5f, 0.5f);
        Vector2 anchorFromPivot = new Vector2(
            (0.5f - parent.pivot.x) * parent.rect.width,
            (0.5f - parent.pivot.y) * parent.rect.height);
        banner.sizeDelta = new Vector2(right - left, bannerDrawH);
        banner.anchoredPosition = new Vector2((left + right) * 0.5f, bannerCenter) - anchorFromPivot;
        AlignTopButtons(parent, banner.anchoredPosition.y, bannerDrawH);
    }

    static void AlignTopButtons(RectTransform parent, float bannerCenterY, float bannerHeight)
    {
        float bannerTop = bannerCenterY + bannerHeight * 0.5f;
        RectTransform settings = parent.Find("ButtonSettings") as RectTransform;
        if (settings != null)
        {
            settings.sizeDelta = new Vector2(settings.sizeDelta.x, bannerHeight);
            settings.anchoredPosition = new Vector2(settings.anchoredPosition.x, bannerCenterY);
        }
        RectTransform quest = parent.Find("ButtonBattleQuest") as RectTransform;
        if (quest != null)
        {
            float questY = bannerTop - quest.sizeDelta.y * 0.5f;
            quest.anchoredPosition = new Vector2(quest.anchoredPosition.x, questY);
        }
        RectTransform message = parent.Find("ButtonMessage") as RectTransform;
        if (quest != null && message != null)
        {
            const float gap = 12f;
            float messageY = quest.anchoredPosition.y - quest.sizeDelta.y * 0.5f - gap - message.sizeDelta.y * 0.5f;
            message.anchoredPosition = new Vector2(quest.anchoredPosition.x, messageY);
        }
        RectTransform items = parent.Find("ButtonItemList") as RectTransform;
        if (items != null)
        {
            const float gap = 12f;
            RectTransform above = message != null && message.gameObject.activeSelf ? message : quest;
            if (above != null)
            {
                float itemY = above.anchoredPosition.y - above.sizeDelta.y * 0.5f - gap - items.sizeDelta.y * 0.5f;
                items.anchoredPosition = new Vector2(above.anchoredPosition.x, itemY);
            }
        }
    }

    static void InsetBannerEdge(Transform button, RectTransform parent, float gap, ref float edge, bool leftSide)
    {
        RectTransform rect = button as RectTransform;
        Vector2 min;
        Vector2 max;
        if (rect == null || TryBoundsIn(rect, parent, out min, out max) == false)
            return;
        if (leftSide)
        {
            if (max.x + gap > edge)
                edge = max.x + gap;
        }
        else if (min.x - gap < edge)
            edge = min.x - gap;
    }

    void LiftHeaderRow(float canvasY)
    {
        if (_header == null || _root == null)
            return;
        RectTransform panel = _root.GetComponent<RectTransform>();
        RectTransform header = _header.rectTransform;
        if (panel == null)
            return;
        float delta = canvasY - (panel.anchoredPosition.y + header.anchoredPosition.y);
        header.anchoredPosition += new Vector2(0f, delta);
        if (_fastButton != null)
        {
            RectTransform fast = _fastButton.GetComponent<RectTransform>();
            fast.anchoredPosition += new Vector2(0f, delta);
        }
        if (_withdrawButton != null)
        {
            RectTransform withdrawRect = _withdrawButton.GetComponent<RectTransform>();
            withdrawRect.anchoredPosition += new Vector2(0f, delta);
        }
    }

    static bool TryBoundsIn(RectTransform rect, RectTransform parent, out Vector2 min, out Vector2 max)
    {
        min = Vector2.zero;
        max = Vector2.zero;
        if (rect == null || parent == null)
            return false;
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Camera camera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        bool has = false;
        for (int i = 0; i < 4; i++)
        {
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                RectTransformUtility.WorldToScreenPoint(camera, corners[i]),
                camera,
                out local) == false)
                return false;
            if (has == false)
            {
                min = max = local;
                has = true;
            }
            else
            {
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
        }
        return has;
    }

    // 上の行を下げ、3行の間隔を均等に広げる。y=1 が下、y=3 が上。
    const float UnitPitchY = 104f;
    const float UnitTopY = 90f;

    static float UnitRowY(int y, int size)
    {
        return UnitTopY - (size - y) * UnitPitchY;
    }

    bool CellSpan(out float top, out float bottom)
    {
        top = float.NegativeInfinity;
        bottom = float.PositiveInfinity;
        bool any = false;
        any |= SpanOf(_allies, ref top, ref bottom);
        any |= SpanOf(_enemies, ref top, ref bottom);
        return any && top > bottom;
    }

    static bool SpanOf(CellView[,] cells, ref float top, ref float bottom)
    {
        if (cells == null)
            return false;
        bool any = false;
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellView view = cells[x, y];
                if (view == null || view.Image == null)
                    continue;
                RectTransform rect = view.Image.rectTransform;
                float half = rect.sizeDelta.y * 0.5f;
                float cellTop = rect.anchoredPosition.y + half;
                float cellBottom = rect.anchoredPosition.y - half;
                if (cellTop > top)
                    top = cellTop;
                if (cellBottom < bottom)
                    bottom = cellBottom;
                any = true;
            }
        }
        return any;
    }

    // ログの左上。押すと、攻撃を受ける味方の列のステータスとログを入れ替える
    void BuildFrontStatus()
    {
        if (_frontHost != null || _logText == null)
            return;

        GameObject hostObject = new GameObject("BattleFrontStatus");
        hostObject.transform.SetParent(_logText.transform.parent, false);
        _frontHost = hostObject.AddComponent<RectTransform>();

        Button button = CreateButton("ButtonBattleLogMode", "前列", new Color(0.30f, 0.34f, 0.42f, 1f), _frontHost);
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 0.5f);
        buttonRect.offsetMin = new Vector2(4f, 2f);
        buttonRect.offsetMax = new Vector2(92f, -2f);
        _frontToggleLabel = button.GetComponentInChildren<Text>();
        if (_frontToggleLabel != null)
            _frontToggleLabel.fontSize = 20;
        button.onClick.AddListener(PushFrontToggle);

        GameObject body = new GameObject("FrontStatusBody");
        body.transform.SetParent(_frontHost, false);
        RectTransform bodyRect = body.AddComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(96f, 2f);
        bodyRect.offsetMax = new Vector2(-4f, -2f);
        body.AddComponent<RectMask2D>();
        _frontBody = body;

        _frontEmpty = CreateText("TextFrontEmpty", body.transform, 20, TextAnchor.MiddleCenter);
        Stretch(_frontEmpty.rectTransform);
        _frontEmpty.text = "攻撃を受ける列はいません";
        _frontEmpty.raycastTarget = false;
        _frontEmpty.gameObject.SetActive(false);

        for (int i = 0; i < _frontSlots.Length; i++)
            _frontSlots[i] = CreateFrontSlot(body.transform, i);

        _showFrontStatus = _production != null && _production.GetShowBattleFrontStatus();
        ApplyFrontMode();
    }

    FrontSlot CreateFrontSlot(Transform parent, int index)
    {
        FrontSlot slot = new FrontSlot();
        GameObject root = new GameObject("FrontSlot" + index.ToString());
        root.transform.SetParent(parent, false);
        RectTransform rect = root.AddComponent<RectTransform>();
        float width = 1f / _frontSlots.Length;
        rect.anchorMin = new Vector2(index * width, 0f);
        rect.anchorMax = new Vector2((index + 1) * width, 1f);
        rect.offsetMin = new Vector2(2f, 0f);
        rect.offsetMax = new Vector2(-2f, 0f);
        slot.Root = root;

        Image portrait = CreateImage("Portrait", root.transform, Color.white);
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        RectTransform portraitRect = portrait.rectTransform;
        portraitRect.anchorMin = new Vector2(0f, 0f);
        portraitRect.anchorMax = new Vector2(0f, 0f);
        portraitRect.pivot = new Vector2(0f, 0f);
        portraitRect.anchoredPosition = Vector2.zero;
        portraitRect.sizeDelta = new Vector2(48f, 48f);
        slot.Portrait = portrait;

        Text info = CreateText("Info", root.transform, 20, TextAnchor.MiddleLeft);
        info.supportRichText = true;
        info.horizontalOverflow = HorizontalWrapMode.Overflow;
        info.verticalOverflow = VerticalWrapMode.Overflow;
        info.resizeTextForBestFit = false;
        info.raycastTarget = false;
        RectTransform infoRect = info.rectTransform;
        infoRect.anchorMin = new Vector2(0f, 1f);
        infoRect.anchorMax = new Vector2(1f, 1f);
        infoRect.pivot = new Vector2(0f, 1f);
        infoRect.offsetMin = new Vector2(2f, -30f);
        infoRect.offsetMax = new Vector2(-2f, 0f);
        slot.Info = info;

        slot.Hp = CreateGauge("Hp", root.transform, new Color(0.28f, 0.05f, 0.05f, 1f), new Color(0.92f, 0.16f, 0.14f, 1f));
        PlaceFrontBarFromBottom(slot.Hp.GetComponent<RectTransform>(), 18f, 24f);
        slot.HpText = CreateFrontValue(slot.Hp.transform, 17);

        slot.Exp = CreateGauge("Exp", root.transform, new Color(0.05f, 0.22f, 0.08f, 1f), new Color(0.25f, 0.82f, 0.28f, 1f));
        PlaceFrontBarFromBottom(slot.Exp.GetComponent<RectTransform>(), 0f, 18f);
        slot.ExpText = CreateFrontValue(slot.Exp.transform, 14);

        root.SetActive(false);
        return slot;
    }

    static void PlaceFrontBarFromBottom(RectTransform rect, float bottom, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.offsetMin = new Vector2(54f, bottom);
        rect.offsetMax = new Vector2(-2f, bottom + height);
    }

    Text CreateFrontValue(Transform bar, int size)
    {
        Text text = CreateText("Value", bar, size, TextAnchor.MiddleCenter);
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(text.rectTransform);
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        text.transform.SetAsLastSibling();
        return text;
    }

    void PushFrontToggle()
    {
        _showFrontStatus = !_showFrontStatus;
        ApplyFrontMode();
        if (_production == null)
            return;
        _production.SetShowBattleFrontStatus(_showFrontStatus);
        _production.SaveGame();
    }

    void ApplyFrontMode()
    {
        if (_logText != null)
            _logText.enabled = !_showFrontStatus;
        if (_frontBody != null)
            _frontBody.SetActive(_showFrontStatus);
        if (_frontToggleLabel != null)
            _frontToggleLabel.text = _showFrontStatus ? "ログ" : "前列";
        if (_showFrontStatus)
            PaintFrontStatus();
        ApplyFrontHeight();
    }

    void InsetLogForToggle()
    {
        if (_logText == null)
            return;
        RectTransform log = _logText.rectTransform;
        const float inset = 96f;
        log.anchoredPosition = new Vector2(log.anchoredPosition.x + inset * 0.5f, log.anchoredPosition.y);
        float width = log.sizeDelta.x - inset;
        if (width < 80f)
            width = 80f;
        log.sizeDelta = new Vector2(width, log.sizeDelta.y);
    }

    void FitFrontStatus()
    {
        if (_frontHost == null || _logText == null)
            return;
        RectTransform log = _logText.rectTransform;
        Transform plate = log.parent.Find("Image" + log.gameObject.name + "Back");
        RectTransform source = plate as RectTransform;
        if (source == null)
            source = log;
        if (_frontHost.parent != source.parent)
            _frontHost.SetParent(source.parent, false);
        _frontHost.anchorMin = source.anchorMin;
        _frontHost.anchorMax = source.anchorMax;
        _frontHost.pivot = source.pivot;
        _frontHost.anchoredPosition = source.anchoredPosition;
        _frontHost.sizeDelta = source.sizeDelta;
        _frontHost.SetAsLastSibling();
        RememberFrontPlate(source);
        ApplyFrontHeight();
    }

    void RememberFrontPlate(RectTransform source)
    {
        if (_frontPlateSaved || source == null)
            return;
        _frontPlate = source;
        _frontPlatePos = source.anchoredPosition;
        _frontPlateSize = source.sizeDelta;
        _frontPlateSaved = true;
    }

    // ログも前列も、同じ高さと上端にする。
    void ApplyFrontHeight()
    {
        if (!_frontPlateSaved || _frontPlate == null || _frontHost == null)
            return;
        float pivotY = _frontPlate.pivot.y;
        float logTop = _frontPlatePos.y + _frontPlateSize.y * (1f - pivotY);
        float height = FrontHostHeight;
        float top = logTop + FrontRaise;
        if (height > _frontPlateSize.y + FrontRaise)
            height = _frontPlateSize.y + FrontRaise;
        float posY = top - height * (1f - pivotY);
        Vector2 pos = new Vector2(_frontPlatePos.x, posY);
        Vector2 size = new Vector2(_frontPlateSize.x, height);
        _frontPlate.anchoredPosition = pos;
        _frontPlate.sizeDelta = size;
        _frontHost.anchoredPosition = pos;
        _frontHost.sizeDelta = size;
        FitLogTextHeight(top, height);
    }

    void FitLogTextHeight(float top, float height)
    {
        if (_logText == null)
            return;
        EnsureLogClip();
        RectTransform log = _logClip != null ? _logClip : _logText.rectTransform;
        float pivotY = log.pivot.y;
        float posY = top - height * (1f - pivotY);
        log.anchoredPosition = new Vector2(log.anchoredPosition.x, posY);
        log.sizeDelta = new Vector2(log.sizeDelta.x, height);
        _logText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // 3行目は枠からはみ出ても、見えているところだけ出す。
    void EnsureLogClip()
    {
        if (_logText == null || _logClip != null)
            return;
        RectTransform text = _logText.rectTransform;
        Transform parent = text.parent;
        GameObject clipObject = new GameObject("LogClip", typeof(RectTransform));
        RectTransform clip = clipObject.GetComponent<RectTransform>();
        clip.SetParent(parent, false);
        clip.anchorMin = text.anchorMin;
        clip.anchorMax = text.anchorMax;
        clip.pivot = text.pivot;
        clip.anchoredPosition = text.anchoredPosition;
        clip.sizeDelta = text.sizeDelta;
        clip.SetSiblingIndex(text.GetSiblingIndex());
        clipObject.AddComponent<RectMask2D>();
        text.SetParent(clip, false);
        text.anchorMin = Vector2.zero;
        text.anchorMax = Vector2.one;
        text.offsetMin = Vector2.zero;
        text.offsetMax = Vector2.zero;
        _logClip = clip;
    }

    void PaintFrontStatus()
    {
        if (!_showFrontStatus || _frontBody == null)
            return;
        CollectFrontAllies();
        bool any = _frontUnits.Count > 0;
        if (_frontEmpty != null)
            _frontEmpty.gameObject.SetActive(!any);
        for (int i = 0; i < _frontSlots.Length; i++)
        {
            FrontSlot slot = _frontSlots[i];
            if (slot == null || slot.Root == null)
                continue;
            if (i >= _frontUnits.Count)
            {
                slot.Root.SetActive(false);
                continue;
            }
            slot.Root.SetActive(true);
            PaintFrontSlot(slot, _frontUnits[i]);
        }
    }

    void CollectFrontAllies()
    {
        _frontUnits.Clear();
        int frontX = 0;
        bool found = false;
        for (int i = 0; i < _units.Count; i++)
        {
            BattleUnit unit = _units[i];
            if (unit == null || !unit.IsAlly || !unit.IsLiving())
                continue;
            if (!found || unit.X > frontX)
            {
                frontX = unit.X;
                found = true;
            }
        }
        if (!found)
            return;
        for (int i = 0; i < _units.Count; i++)
        {
            BattleUnit unit = _units[i];
            if (unit != null && unit.IsAlly && unit.IsLiving() && unit.X == frontX)
                _frontUnits.Add(unit);
        }
        _frontUnits.Sort(CompareFrontRow);
    }

    static int CompareFrontRow(BattleUnit a, BattleUnit b)
    {
        if (a == null && b == null)
            return 0;
        if (a == null)
            return 1;
        if (b == null)
            return -1;
        return b.Y.CompareTo(a.Y);
    }

    static string FrontEmphasis(string number)
    {
        return "<b><size=26>" + number + "</size></b>";
    }

    void PaintFrontSlot(FrontSlot slot, BattleUnit unit)
    {
        Sprite sprite = SpriteOf(unit.CharacterId);
        if (slot.Portrait != null)
        {
            slot.Portrait.sprite = sprite;
            slot.Portrait.enabled = sprite != null;
        }

        CharacterClass character = _production != null ? _production.GetCharacter(unit.CharacterId) : null;
        long level = character != null ? (long)character.Level : unit.Level;
        long full = 0;
        long eight = 0;
        long half = 0;
        if (character != null)
        {
            character.EnsureStock();
            full = character.Stock.Full;
            eight = character.Stock.Eight;
            half = character.Stock.Half;
        }
        if (slot.Info != null)
        {
            slot.Info.text = "Lv" + FrontEmphasis(UiNumber.Format(level))
                + "    ×1: " + FrontEmphasis(UiNumber.Format(full))
                + "  ×0.8: " + FrontEmphasis(UiNumber.Format(eight))
                + "  ×0.5: " + FrontEmphasis(UiNumber.Format(half));
        }

        SetGauge(slot.Hp, unit.Hp, unit.HpMax);
        if (slot.HpText != null)
            slot.HpText.text = UiNumber.Format(unit.Hp) + "/" + UiNumber.Format(unit.HpMax);

        long exp = 0;
        long need = 1;
        if (character != null)
        {
            exp = (long)character.Exp;
            need = (long)character.ExpMax;
            if (need < 1 && _config != null)
                need = _config.ExpToNext((long)character.Level, character.OpaquePixelCount());
        }
        else if (_config != null)
            need = _config.ExpToNext(unit.Level, unit.OpaquePixels);
        if (need < 1)
            need = 1;
        SetGauge(slot.Exp, exp, need);
        if (slot.ExpText != null)
            slot.ExpText.text = UiNumber.Format(exp) + "/" + UiNumber.Format(need);
    }

    // 背景画像と文字の間。板は文字と同じ大きさにして、隣のボタンやマスには被せない
    void PlaceTextPlate(Text text)
    {
        if (text == null)
            return;
        Transform parent = text.transform.parent;
        string name = "Image" + text.gameObject.name + "Back";
        Transform existing = parent.Find(name);
        RectTransform back;
        if (existing == null)
        {
            Image image = CreateImage(name, parent, new Color(0f, 0f, 0f, 0.7f));
            image.raycastTarget = false;
            back = image.rectTransform;
        }
        else
            back = existing as RectTransform;

        RectTransform textRect = text.rectTransform;
        back.anchorMin = textRect.anchorMin;
        back.anchorMax = textRect.anchorMax;
        back.pivot = textRect.pivot;
        back.anchoredPosition = textRect.anchoredPosition;
        back.sizeDelta = textRect.sizeDelta;
        if (text == _header)
        {
            Image plate = back.GetComponent<Image>();
            if (plate != null)
                plate.color = HeaderBackColor;
            EnsureHeaderFill(back);
        }

        int textIndex = textRect.GetSiblingIndex();
        int backIndex = back.GetSiblingIndex();
        int desired = backIndex < textIndex ? textIndex - 1 : textIndex;
        if (backIndex != desired)
            back.SetSiblingIndex(desired);
    }

    // 今の100階を満タンにする。緑がいまの階、その後ろの灰色が最高クリア階
    void SetHeaderProgress(int floor)
    {
        int current = floor > 0 ? floor : 0;
        int reached = current;
        int windowStart = 1;
        if (_production != null && _battleStageIndex > 0)
        {
            windowStart = _production.GetBattleFloorWindowStart(_battleStageIndex);
            int cleared = _production.GetHighestClearedBattleFloor(_battleStageIndex);
            if (cleared > reached)
                reached = cleared;
        }
        _headerRate = HeaderRate(current, windowStart);
        _headerRecordRate = HeaderRate(reached, windowStart);
        ApplyHeaderFill();
    }

    static float HeaderRate(int floor, int windowStart)
    {
        if (windowStart < 1)
            windowStart = 1;
        int local = floor - (windowStart - 1);
        float rate = 0f;
        if (local > 0)
            rate = local / (float)Constants.BATTLE_STAGE_FLOOR_MAX;
        if (rate < 0f)
            rate = 0f;
        if (rate > 1f)
            rate = 1f;
        return rate;
    }

    void EnsureHeaderFill(RectTransform back)
    {
        _headerRecord = EnsureHeaderBar(back, "Record", HeaderRecordColor);
        _headerFill = EnsureHeaderBar(back, "Fill", HeaderFillColor);
        if (_headerFill != null)
            _headerFill.transform.SetAsLastSibling();
        ApplyHeaderFill();
    }

    Image EnsureHeaderBar(RectTransform back, string name, Color color)
    {
        Transform existing = back.Find(name);
        Image image;
        if (existing == null)
        {
            image = CreateImage(name, back, color);
            image.raycastTarget = false;
        }
        else
            image = existing.GetComponent<Image>();
        if (image != null)
            image.color = color;
        return image;
    }

    void ApplyHeaderFill()
    {
        PlaceHeaderBar(_headerRecord, _headerRecordRate);
        PlaceHeaderBar(_headerFill, _headerRate);
    }

    static void PlaceHeaderBar(Image image, float rate)
    {
        if (image == null)
            return;
        if (rate < 0f)
            rate = 0f;
        if (rate > 1f)
            rate = 1f;
        RectTransform fill = image.rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(rate, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.pivot = new Vector2(0f, 0.5f);
    }

    float BattleTextWidth()
    {
        RectTransform rect = _root != null ? _root.GetComponent<RectTransform>() : null;
        float width = rect != null ? rect.rect.width : 0f;
        if (width < 8f && rect != null)
            width = rect.sizeDelta.x;
        if (width < 8f)
            width = 1080f;
        return width - 24f;
    }

    // クエストボタン、達成率、一覧はバトル画面の外に置く。バトルを閉じていても見える。
    void BuildQuestAccess()
    {
        Transform canvas = _root.transform.parent != null ? _root.transform.parent : _root.transform;
        GameObject overlay = new GameObject("PanelQuestAccess", typeof(RectTransform));
        overlay.transform.SetParent(canvas, false);
        Stretch(overlay.GetComponent<RectTransform>());
        _questOverlay = overlay.transform;

        RectTransform battleRect = _root.GetComponent<RectTransform>();
        Vector2 battlePos = battleRect != null ? battleRect.anchoredPosition : new Vector2(0f, 745f);
        BuildQuestBanner(battlePos);

        Button quest = CreateButton("ButtonBattleQuest", "クエスト", new Color(0.62f, 0.46f, 0.16f, 1f), _questOverlay);
        PlaceRect(quest.GetComponent<RectTransform>(), battlePos.x + 430f, battlePos.y + 177f, 150f, 150f);
        quest.onClick.AddListener(ToggleQuest);
        BuildQuestBadge(quest.transform);

        Button settings = CreateButton("ButtonSettings", "設定", new Color(0.30f, 0.34f, 0.42f, 1f), _questOverlay);
        PlaceRect(settings.GetComponent<RectTransform>(), battlePos.x - 430f, battlePos.y + 207f, 150f, 50f);
        Text settingsLabel = settings.GetComponentInChildren<Text>();
        if (settingsLabel != null)
            settingsLabel.fontSize = 28;
        settings.onClick.AddListener(ToggleSettings);

        Button message = CreateButton("ButtonMessage", "メッセージ", new Color(0.22f, 0.55f, 0.38f, 1f), _questOverlay);
        PlaceRect(message.GetComponent<RectTransform>(), battlePos.x + 430f, battlePos.y - 65f, 150f, 90f);
        Text messageLabel = message.GetComponentInChildren<Text>();
        if (messageLabel != null)
            messageLabel.fontSize = 31;
        message.onClick.AddListener(ToggleMessage);
        _messageButton = message.gameObject;
        BuildMessageBadge(message.transform);

        Button items = CreateButton("ButtonItemList", "アイテム一覧", new Color(0.55f, 0.36f, 0.22f, 1f), _questOverlay);
        PlaceRect(items.GetComponent<RectTransform>(), battlePos.x + 430f, battlePos.y - 172f, 150f, 96f);
        Text itemLabel = items.GetComponentInChildren<Text>();
        if (itemLabel != null)
        {
            itemLabel.fontSize = 22;
            itemLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemLabel.verticalOverflow = VerticalWrapMode.Truncate;
        }
        items.onClick.AddListener(ToggleItems);

        _questBoard = new QuestBoard();
        _questBoard.Build(_questOverlay, _font, _production);
        _settings = new SettingsBoard();
        _settings.Build(_questOverlay, _font, ApplySpeedCap, ApplyAdWatchConfirm, ApplyKeepAwake, ApplyReturnToBattleStage, PushSettingsSave, PushSettingsDeleteSave, PushSettingsTestRgb, PushSettingsTestPixels, PushSettingsTestOwned);
        _messages = new MessageBoard();
        _messages.Build(_questOverlay, _font, _production);
        _items = new ItemBoard();
        _items.Build(_questOverlay, _font, _production);
        RefreshMessageButton();
        BringQuestForward();
    }

    void BuildQuestBanner(Vector2 battlePos)
    {
        Image track = CreateImage("ImageBattleQuestBanner", _questOverlay, new Color(0.10f, 0.12f, 0.16f, 0.95f));
        PlaceRect(track.rectTransform, battlePos.x, battlePos.y + 250f, BattleTextWidth(), 50f);
        Button banner = track.gameObject.AddComponent<Button>();
        banner.targetGraphic = track;
        banner.onClick.AddListener(ToggleQuest);
        _questBanner = track.gameObject;

        _questBannerFill = CreateImage("Fill", track.transform, new Color(0.32f, 0.72f, 0.4f, 1f));
        RectTransform fillRect = _questBannerFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.pivot = new Vector2(0f, 0.5f);
        _questBannerFill.raycastTarget = false;

        _questBannerText = CreateText("Text", track.transform, 24, TextAnchor.MiddleCenter);
        Stretch(_questBannerText.rectTransform);
        _questBannerText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _questBannerText.verticalOverflow = VerticalWrapMode.Truncate;
        _questBannerText.raycastTarget = false;
        Outline outline = _questBannerText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);
        _questBanner.SetActive(false);
    }

    void BuildQuestBadge(Transform quest)
    {
        Image badge = CreateImage("ImageQuestBadge", quest, new Color(0.86f, 0.16f, 0.16f, 1f));
        badge.sprite = CircleSprite();
        badge.raycastTarget = false;
        RectTransform rect = badge.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-26f, -26f);
        rect.sizeDelta = new Vector2(40f, 40f);
        _questBadge = badge.gameObject;

        _questBadgeText = CreateText("Text", badge.transform, 22, TextAnchor.MiddleCenter);
        Stretch(_questBadgeText.rectTransform);
        _questBadgeText.raycastTarget = false;
        _questBadgeText.resizeTextForBestFit = true;
        _questBadgeText.resizeTextMinSize = 14;
        _questBadgeText.resizeTextMaxSize = 22;
        _questBadge.SetActive(false);
    }

    static Sprite CircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        float radius = (size - 1) * 0.5f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius;
                float dy = y - radius;
                float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.75f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    void RefreshQuestBanner()
    {
        if (_questBanner == null || _production == null)
            return;
        _production.CollectVisibleQuests(_questCandidates);
        int claimable = 0;
        for (int i = 0; i < _questCandidates.Count; i++)
        {
            QuestDef def = _questCandidates[i];
            if (def != null && _production.IsQuestClaimable(def))
                claimable++;
        }
        QuestDef shown = _production.DisplayQuest(_questCandidates);
        RefreshQuestBadge(claimable);
        // ロックした系統を優先し、なければ出ている未受取の最小番号。
        if (shown == null)
        {
            _questBanner.SetActive(false);
            return;
        }
        _questBanner.SetActive(true);
        _questBannerText.text = shown.Title;
        bool cleared = _production.IsQuestCleared(shown);
        _questBannerFill.color = cleared
            ? new Color(0.95f, 0.78f, 0.18f, 1f)
            : new Color(0.32f, 0.72f, 0.4f, 1f);
        float rate = _production.QuestBar(shown);
        if (rate < 0f)
            rate = 0f;
        if (rate > 1f)
            rate = 1f;
        _questBannerFill.rectTransform.anchorMax = new Vector2(rate, 1f);
    }

    void RefreshQuestBadge(int claimable)
    {
        if (_questBadge == null)
            return;
        if (claimable <= 0)
        {
            _questBadge.SetActive(false);
            return;
        }
        _questBadge.SetActive(true);
        if (_questBadgeText != null)
            _questBadgeText.text = claimable.ToString();
    }

    void ToggleQuest()
    {
        if (_questBoard == null)
            return;
        if (_questBoard.IsOpen)
            _questBoard.Close();
        else
        {
            if (_settings != null && _settings.IsOpen)
                _settings.Close();
            if (_messages != null && _messages.IsOpen)
                _messages.Close();
            if (_items != null && _items.IsOpen)
                _items.Close();
            _questBoard.Open();
        }
    }

    void ToggleSettings()
    {
        if (_settings == null)
            return;
        if (_settings.IsOpen)
            _settings.Close();
        else
        {
            if (_questBoard != null && _questBoard.IsOpen)
                _questBoard.Close();
            if (_messages != null && _messages.IsOpen)
                _messages.Close();
            if (_items != null && _items.IsOpen)
                _items.Close();
            _settings.Open(SpeedCap(), AdWatchConfirm(), ScreenAwake.Enabled, ReturnToBattleStage());
        }
    }

    public void OnMessagesChanged(bool added)
    {
        RefreshMessageButton();
        if (added && _messages != null && _messages.IsOpen && (_production == null || !_production.IsSaving))
            _messages.Refresh();
    }

    public void RefreshMessageButton()
    {
        bool show = _production != null && _production.Messages != null && _production.Messages.AnyVisible();
        if (_messageButton != null)
            _messageButton.SetActive(show);
        PlaceItemButton();
        int badge = show && _production.Messages != null ? _production.Messages.BadgeCount() : 0;
        if (_messageBadge == null)
            return;
        if (badge <= 0)
        {
            _messageBadge.SetActive(false);
            return;
        }
        _messageBadge.SetActive(true);
        if (_messageBadgeText != null)
            _messageBadgeText.text = badge.ToString();
    }

    void ToggleMessage()
    {
        if (_messages == null)
            return;
        if (_messages.IsOpen)
            _messages.Close();
        else
        {
            if (_questBoard != null && _questBoard.IsOpen)
                _questBoard.Close();
            if (_settings != null && _settings.IsOpen)
                _settings.Close();
            if (_items != null && _items.IsOpen)
                _items.Close();
            _messages.Open();
        }
    }

    void PlaceItemButton()
    {
        if (_questOverlay == null)
            return;
        RectTransform items = _questOverlay.Find("ButtonItemList") as RectTransform;
        RectTransform message = _questOverlay.Find("ButtonMessage") as RectTransform;
        RectTransform quest = _questOverlay.Find("ButtonBattleQuest") as RectTransform;
        if (items == null)
            return;
        const float gap = 12f;
        RectTransform above = message != null && message.gameObject.activeSelf ? message : quest;
        if (above == null)
            return;
        float itemY = above.anchoredPosition.y - above.sizeDelta.y * 0.5f - gap - items.sizeDelta.y * 0.5f;
        items.anchoredPosition = new Vector2(above.anchoredPosition.x, itemY);
    }

    void ToggleItems()
    {
        if (_items == null)
            return;
        if (_items.IsOpen)
            _items.Close();
        else
        {
            if (_questBoard != null && _questBoard.IsOpen)
                _questBoard.Close();
            if (_settings != null && _settings.IsOpen)
                _settings.Close();
            if (_messages != null && _messages.IsOpen)
                _messages.Close();
            _items.Open();
        }
    }

    public void RefreshItemBoard()
    {
        if (_items != null)
            _items.Refresh();
    }

    void BuildCharmIcon()
    {
        if (_withdrawButton == null || _charmIcon != null)
            return;
        Image icon = CreateImage("ImageEnmusubiCharm", _withdrawButton.transform, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Sprite sprite = ItemImages.Enmusubi();
        if (sprite != null)
            icon.sprite = sprite;
        RectTransform rect = icon.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.82f, 0.82f);
        rect.anchoredPosition = new Vector2(22f, -24f);
        rect.sizeDelta = new Vector2(68f, 68f);
        _charmIcon = icon.gameObject;
        _charmIcon.SetActive(false);
    }

    public void RefreshCharmIcon()
    {
        if (_charmIcon == null)
            return;
        bool on = _production != null && _production.IsEnmusubiArmed();
        Image icon = _charmIcon.GetComponent<Image>();
        if (on && icon != null && icon.sprite == null)
        {
            Sprite sprite = ItemImages.Enmusubi();
            if (sprite != null)
                icon.sprite = sprite;
        }
        _charmIcon.SetActive(on && icon != null && icon.sprite != null);
    }

    void BuildMessageBadge(Transform message)
    {
        Image badge = CreateImage("ImageMessageBadge", message, new Color(0.86f, 0.16f, 0.16f, 1f));
        badge.sprite = CircleSprite();
        badge.raycastTarget = false;
        RectTransform rect = badge.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-18f, -18f);
        rect.sizeDelta = new Vector2(36f, 36f);
        _messageBadge = badge.gameObject;
        _messageBadgeText = CreateText("Text", badge.transform, 23, TextAnchor.MiddleCenter);
        Stretch(_messageBadgeText.rectTransform);
        _messageBadgeText.raycastTarget = false;
        _messageBadge.SetActive(false);
    }

    void ApplySpeedCap(int cap)
    {
        if (_production == null)
            return;
        _production.SetBattleSpeedCap(cap);
        ApplySavedSpeed(_production.GetBattleSpeed());
        _production.SaveGame();
    }

    bool AdWatchConfirm()
    {
        return _production != null && _production.GetAdWatchConfirm();
    }

    void ApplyAdWatchConfirm(bool confirm)
    {
        if (_production == null)
            return;
        _production.SetAdWatchConfirm(confirm);
        _production.SaveGame();
    }

    void ApplyKeepAwake(bool keep)
    {
        ScreenAwake.Enabled = keep;
    }

    bool ReturnToBattleStage()
    {
        return _production == null || _production.GetReturnToBattleStage();
    }

    void ApplyReturnToBattleStage(bool enabled)
    {
        if (_production == null)
            return;
        _production.SetReturnToBattleStage(enabled);
        _production.SaveGame();
    }

    void PushSettingsSave()
    {
        if (_production == null || _settings == null)
            return;
        bool saved = _production.SaveGame();
        _settings.SetSaveStatus(saved ? "保存しました" : "保存できませんでした");
    }

    // RELEASE_REMOVE_TEST_GRANTS: 設定画面のテストボタン。リリース用ビルドの前に消す。
    void PushSettingsTestRgb()
    {
        GrantFromSettings(() => _production.DebugGrantRgb(10000), "RGBを+10000しました");
    }

    void PushSettingsTestPixels()
    {
        GrantFromSettings(() => _production.DebugGrantAllPixels(100), "全ピクセルを+100しました");
    }

    void PushSettingsTestOwned()
    {
        GrantFromSettings(() => _production.DebugGrantOwned(10), "所持数を+10しました");
    }

    void GrantFromSettings(Action grant, string message)
    {
        if (_production == null || _settings == null || grant == null)
            return;
        grant();
        bool saved = _production.SaveGame();
        _settings.SetSaveStatus(saved ? message : "追加しましたが保存できませんでした");
    }

    void PushSettingsDeleteSave()
    {
        if (_production == null || _settings == null)
            return;
        _production.ConfirmDeleteSave(() =>
        {
            if (_settings != null)
                _settings.SetSaveStatus("セーブを削除しました");
        });
    }

    void BuildBattlePeople()
    {
        int count = Constants.BATTLE_PERSON_SLOT_NUM;
        _battlePeople = new Image[count + 1];
        int size = _config.FormationSize;
        float pitch = 78f;
        float cell = 70f;
        float allyLeft = -170f + (1 - 2) * pitch;
        float px = allyLeft - pitch;
        for (int slot = 1; slot <= count; slot++)
        {
            int y = slot <= size ? slot : size;
            float py = UnitRowY(y, size);
            Image image = CreateImage("ImageBattlePerson" + slot.ToString(), _root.transform, new Color(0.15f, 0.15f, 0.18f, 0.35f));
            PlaceRect(image.rectTransform, px, py, cell, cell);
            image.preserveAspect = true;
            image.raycastTarget = false;
            _battlePeople[slot] = image;
        }
    }

    void ShowBattlePeople(int setIndex)
    {
        if (_battlePeople == null || _production == null)
            return;
        for (int slot = 1; slot < _battlePeople.Length; slot++)
        {
            Image image = _battlePeople[slot];
            if (image == null)
                continue;
            PersonClass person = _production.GetPerson((int)_production.GetBattlePartyPerson(setIndex, slot));
            Sprite portrait = person != null ? PersonPortraits.SpriteOf(person.Id) : null;
            if (portrait != null)
            {
                image.sprite = portrait;
                image.color = Color.white;
            }
            else if (person != null)
            {
                image.sprite = null;
                image.color = ControllerProduction.PersonColor(person.Id);
            }
            else
            {
                image.sprite = null;
                image.color = new Color(0.15f, 0.15f, 0.18f, 0.35f);
            }
            if (person == null)
            {
                PersonSkillBar.Hide(image.transform);
                continue;
            }
            int level = person.BattleLevel < 1 ? 1 : person.BattleLevel;
            PersonBalanceConfig balance = _production.GetPersonBalance();
            double exponent = balance != null ? balance.BattleExpExponent : 1.7;
            long need = PersonFormulas.ExpToNext(level, exponent, balance);
            PersonSkillBar.ShowInside(image.transform, level, person.BattleExp, need);
        }
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
                float py = UnitRowY(y, size);
                string side = cells == _allies ? "A" : "E";
                string suffix = side + x.ToString() + y.ToString();
                Image image = CreateImage("ImageBattleCell" + suffix, _root.transform, EmptyCellColor);
                PlaceRect(image.rectTransform, px, py, cell, cell);
                image.preserveAspect = true;
                image.raycastTarget = false;

                Image portrait = CreateImage("Portrait", image.transform, Color.white);
                PlaceRect(portrait.rectTransform, 0f, 0f, cell, cell);
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                portrait.gameObject.SetActive(false);
                if (cells != _allies)
                    portrait.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

                Image infoBack = CreateImage("ImageBattleInfo" + suffix, image.transform, new Color(0f, 0f, 0f, 0.62f));
                infoBack.raycastTarget = false;

                Slider hp = CreateGauge("SliderBattleHp" + suffix, image.transform, new Color(0.28f, 0.05f, 0.05f, 1f), new Color(0.92f, 0.16f, 0.14f, 1f));
                Slider exp = CreateGauge("SliderBattleExp" + suffix, image.transform, new Color(0.05f, 0.22f, 0.08f, 1f), new Color(0.25f, 0.82f, 0.28f, 1f));

                Text lives = CreateText("TextBattleLives" + suffix, image.transform, 16, TextAnchor.MiddleCenter);
                lives.color = Color.white;
                lives.supportRichText = true;
                lives.raycastTarget = false;
                lives.resizeTextForBestFit = true;
                lives.resizeTextMinSize = 8;
                lives.resizeTextMaxSize = 14;
                lives.horizontalOverflow = HorizontalWrapMode.Overflow;
                lives.verticalOverflow = VerticalWrapMode.Truncate;
                Outline outline = lives.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 1f);
                outline.effectDistance = new Vector2(1f, -1f);

                cells[x, y] = new CellView
                {
                    Image = image,
                    Portrait = portrait,
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
    const float LivesHeight = 18f;

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
        int bars = 1;
        if (showExp && view.Exp != null)
            bars = 2;
        const float livesOverlap = 10f;
        float y = -((MeterHeight + MeterGap) * bars + (LivesHeight - livesOverlap));
        float stackBottom = y;
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
        float livesTop = y + LivesHeight;
        if (view.Lives != null)
        {
            RectTransform lives = view.Lives.rectTransform;
            lives.anchorMin = new Vector2(0f, 0f);
            lives.anchorMax = new Vector2(1f, 0f);
            lives.pivot = new Vector2(0.5f, 0f);
            lives.offsetMin = new Vector2(0f, y);
            lives.offsetMax = new Vector2(0f, livesTop);
        }
        if (view.InfoBack != null)
        {
            RectTransform back = view.InfoBack.rectTransform;
            back.anchorMin = new Vector2(0f, 0f);
            back.anchorMax = new Vector2(1f, 0f);
            back.pivot = new Vector2(0.5f, 0f);
            back.offsetMin = new Vector2(0f, stackBottom);
            back.offsetMax = new Vector2(0f, livesTop);
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
        return CreateButton(name, label, color, _root.transform);
    }

    Button CreateButton(string name, string label, Color color, Transform parent)
    {
        Image image = CreateImage(name, parent, color);
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

    public void NoteHospitalReturn(uint characterId)
    {
        if (characterId == 0)
            return;
        int cap = _config != null ? _config.MaxLives : BattleBalanceConfig.MaxOwnedCount;
        for (int i = 0; i < _units.Count; i++)
        {
            BattleUnit unit = _units[i];
            if (unit == null || !unit.IsAlly || unit.CharacterId != characterId)
                continue;
            if (unit.Lives < cap)
                unit.Lives++;
            return;
        }
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
            if (_view._battleStageIndex == Constants.BATTLE_STAGE_GALLERY && amount > 0)
                amount = (amount + 1) / 2;
            _view._production.AddBattleRgb(attribute, amount);
        }

        public void GrantPixels(int r, int g, int b, long count)
        {
            if (_view._battleStageIndex == Constants.BATTLE_STAGE_GALLERY && count > 0)
                count = (count + 1) / 2;
            _view._production.AddBattlePixels(r, g, b, count);
            if (count <= 0)
                return;
            _view._cuePixels = true;
            _view._cueR = r;
            _view._cueG = g;
            _view._cueB = b;
            _view._cuePixelCount = count;
        }

        public bool CanReceiveExp(BattleUnit ally)
        {
            return _view._production != null && _view._production.CanGainBattleExp(ally, _view._config);
        }

        public void GrantExp(BattleUnit ally, long exp)
        {
            if (_view._battleStageIndex == Constants.BATTLE_STAGE_GALLERY && exp > 0)
                exp = (exp + 1) / 2;
            int levels = _view._production.GrantBattleExp(ally, exp, _view._config);
            if (exp > 0)
            {
                _view.AddLog(BattleLogFormat.Name(ally) + " 経験値 +" + UiNumber.Format(exp));
                _view.QueueExpFloat(ally, exp);
            }
            if (levels > 0)
                _view.AddLog(BattleLogFormat.Name(ally) + " レベル " + levels.ToString() + " 上昇");
        }

        public void GrantItem(int itemId, int count)
        {
            _view._production.AddBattleItem(itemId, count);
            _view.AddLog(BattleLogFormat.Drop(ItemIds.DisplayName(itemId) + " を入手"));
            if (count > 0)
            {
                _view._cueItem = true;
                _view._cueItemId = itemId;
            }
        }

        public void Recruit(uint characterId)
        {
            _view._production.RecruitFromBattle(characterId, _view._units);
            _view._cueWalk = true;
        }

        public void NoteDefeated(uint characterId)
        {
            _view._production.NoteEnemyDefeat();
            _view._production.MarkCatalogDefeated(characterId);
            _view.GrantCharacterDrops(characterId);
        }

        public void NoteAllyFainted(uint characterId)
        {
            if (_view._production == null)
                return;
            _view._production.AdmitHospitalSpirit(characterId);
            CharacterClass character = _view._production.GetCharacter(characterId);
            if (character != null)
                _view.AddLog(BattleLogFormat.Name(character) + " を回復へ");
        }

        public bool IsGuaranteedRecruit()
        {
            return _view._production != null && _view._production.IsNextEnemyDefeatGuaranteedRecruit();
        }

        public bool ForcesUnownedRecruit(uint characterId)
        {
            return _view._production != null && _view._production.CharmForcesUnownedRecruit(characterId);
        }

        public void EndForcedRecruit()
        {
            if (_view._production != null)
                _view._production.EndEnmusubiCharm();
        }
    }
}
