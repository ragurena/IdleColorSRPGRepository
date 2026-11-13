using System;
using System.Collections.Generic;
using System.Globalization;

// 解放、既読、報酬の受け取り、章ごとの閲覧位置。四つは別々に持つ。
public sealed class StoryState
{
    public const string PresentTalkId = "present";
    public const int RewardCount = 10;

    public const int PageMailBase = 100000;
    public const int ClaimMailBase = 200000;

    readonly Dictionary<int, int> _lastPage = new Dictionary<int, int>();
    readonly HashSet<int> _open = new HashSet<int>();
    readonly HashSet<int> _read = new HashSet<int>();
    readonly HashSet<int> _claimed = new HashSet<int>();
    readonly List<StoryMail> _mails = new List<StoryMail>();
    int _claimSerial;

    public void Clear()
    {
        _lastPage.Clear();
        _open.Clear();
        _read.Clear();
        _claimed.Clear();
        _mails.Clear();
        _claimSerial = 0;
    }

    public StoryState Copy()
    {
        StoryState copy = new StoryState();
        foreach (KeyValuePair<int, int> pair in _lastPage)
            copy._lastPage[pair.Key] = pair.Value;
        foreach (int id in _open)
            copy._open.Add(id);
        foreach (int id in _read)
            copy._read.Add(id);
        foreach (int id in _claimed)
            copy._claimed.Add(id);
        for (int i = 0; i < _mails.Count; i++)
            copy._mails.Add(new StoryMail(_mails[i].LineId, _mails[i].Text));
        copy._claimSerial = _claimSerial;
        return copy;
    }

    public void Replace(StoryState source)
    {
        Clear();
        if (source == null)
            return;
        foreach (KeyValuePair<int, int> pair in source._lastPage)
            _lastPage[pair.Key] = pair.Value;
        foreach (int id in source._open)
            _open.Add(id);
        foreach (int id in source._read)
            _read.Add(id);
        foreach (int id in source._claimed)
            _claimed.Add(id);
        for (int i = 0; i < source._mails.Count; i++)
            _mails.Add(new StoryMail(source._mails[i].LineId, source._mails[i].Text));
        _claimSerial = source._claimSerial;
    }

    public bool IsOpen(int pageId)
    {
        return pageId > 0 && _open.Contains(pageId);
    }

    public bool IsRead(int pageId)
    {
        return pageId > 0 && _read.Contains(pageId);
    }

    public int LastNumber(int chapterId)
    {
        int number;
        if (_lastPage.TryGetValue(chapterId, out number) && number > 0)
            return number;
        return 0;
    }

    public bool ChapterUnlocked(ControllerProduction game, StoryChapterDef chapter)
    {
        if (chapter == null)
            return false;
        return ConditionMet(game, chapter.UnlockStage, chapter.UnlockFloor);
    }

    public bool CatchUp(ControllerProduction game)
    {
        StoryChapterDef[] chapters = StoryCatalog.All;
        if (chapters == null || game == null)
            return false;
        bool changed = false;
        changed |= PullInto(_claimed, _read);
        changed |= PullInto(_read, _open);
        changed |= PullInto(_claimed, _open);
        for (int c = 0; c < chapters.Length; c++)
        {
            StoryChapterDef chapter = chapters[c];
            if (chapter == null || chapter.Pages == null)
                continue;
            for (int i = 0; i < chapter.Pages.Length; i++)
            {
                StoryPageDef page = chapter.Pages[i];
                if (page == null || page.Id <= 0 || _open.Contains(page.Id))
                    continue;
                if (!ConditionMet(game, page.UnlockStage, page.UnlockFloor))
                    continue;
                _open.Add(page.Id);
                changed = true;
            }
        }
        return changed;
    }

    public bool HasUnreadViewable(ControllerProduction game)
    {
        StoryChapterDef[] chapters = StoryCatalog.All;
        if (chapters == null)
            return false;
        for (int c = 0; c < chapters.Length; c++)
        {
            if (ChapterHasNew(game, chapters[c]))
                return true;
        }
        return false;
    }

    public bool ChapterHasNew(ControllerProduction game, StoryChapterDef chapter)
    {
        if (!ChapterUnlocked(game, chapter) || chapter.Pages == null)
            return false;
        for (int i = 0; i < chapter.Pages.Length; i++)
        {
            StoryPageDef page = chapter.Pages[i];
            if (page != null && _open.Contains(page.Id) && !_read.Contains(page.Id))
                return true;
        }
        return false;
    }

    public int StartIndex(StoryChapterDef chapter)
    {
        if (chapter == null || chapter.Pages == null)
            return -1;
        int last = LastNumber(chapter.Id);
        if (last > 0)
        {
            int saved = StoryCatalog.IndexOfNumber(chapter, last);
            if (saved >= 0 && IsOpen(chapter.Pages[saved].Id))
                return saved;
        }
        int firstNumber = StoryCatalog.IndexOfNumber(chapter, 1);
        if (firstNumber >= 0 && IsOpen(chapter.Pages[firstNumber].Id))
            return firstNumber;
        for (int i = 0; i < chapter.Pages.Length; i++)
        {
            StoryPageDef page = chapter.Pages[i];
            if (page != null && IsOpen(page.Id))
                return i;
        }
        return -1;
    }

    public string BannerNote(ControllerProduction game, StoryChapterDef chapter)
    {
        if (chapter == null)
            return "";
        if (!ChapterUnlocked(game, chapter))
            return StoryCatalog.UnlockLabel(chapter.UnlockStage, chapter.UnlockFloor);
        if (StartIndex(chapter) >= 0 || chapter.Pages == null)
            return "";
        for (int i = 0; i < chapter.Pages.Length; i++)
        {
            StoryPageDef page = chapter.Pages[i];
            if (page != null && !IsOpen(page.Id))
                return StoryCatalog.UnlockLabel(page.UnlockStage, page.UnlockFloor);
        }
        return "";
    }

    public bool View(int chapterId, StoryPageDef page, out bool newlyRead)
    {
        newlyRead = false;
        if (page == null || page.Id <= 0 || !_open.Contains(page.Id))
            return false;
        bool changed = false;
        int last;
        if (!_lastPage.TryGetValue(chapterId, out last) || last != page.Number)
        {
            _lastPage[chapterId] = page.Number;
            changed = true;
        }
        if (!_read.Contains(page.Id))
        {
            _read.Add(page.Id);
            newlyRead = true;
            changed = true;
        }
        return changed;
    }

    public int UnclaimedCount()
    {
        int count = 0;
        foreach (int id in _read)
        {
            if (StoryCatalog.ContainsPage(id) && !_claimed.Contains(id))
                count++;
        }
        return count;
    }

    public int ClaimAll()
    {
        int count = 0;
        foreach (int id in _read)
        {
            if (!StoryCatalog.ContainsPage(id) || _claimed.Contains(id))
                continue;
            _claimed.Add(id);
            count++;
        }
        return count;
    }

    public string MailText(int lineId)
    {
        for (int i = 0; i < _mails.Count; i++)
        {
            if (_mails[i].LineId == lineId)
                return _mails[i].Text;
        }
        return null;
    }

    public bool TryAddPageMail(StoryChapterDef chapter, StoryPageDef page, out int lineId)
    {
        lineId = 0;
        if (chapter == null || page == null || page.Id <= 0)
            return false;
        lineId = PageMailBase + page.Id;
        if (MailText(lineId) != null)
            return false;
        string text = "ストーリーの" + chapter.Title + page.Number.ToString(CultureInfo.InvariantCulture)
            + "ページを読んだ報酬　全ピクセル×" + RewardCount.ToString(CultureInfo.InvariantCulture);
        _mails.Add(new StoryMail(lineId, text));
        return true;
    }

    public List<int> AddMissingPageMails()
    {
        List<int> added = new List<int>();
        StoryChapterDef[] chapters = StoryCatalog.All;
        if (chapters == null)
            return added;
        for (int c = 0; c < chapters.Length; c++)
        {
            StoryChapterDef chapter = chapters[c];
            if (chapter == null || chapter.Pages == null)
                continue;
            for (int i = 0; i < chapter.Pages.Length; i++)
            {
                StoryPageDef page = chapter.Pages[i];
                int lineId;
                if (page == null || !IsRead(page.Id) || !TryAddPageMail(chapter, page, out lineId))
                    continue;
                added.Add(lineId);
            }
        }
        return added;
    }

    public List<int> MailLineIds()
    {
        List<int> ids = new List<int>();
        for (int i = 0; i < _mails.Count; i++)
        {
            if (_mails[i] != null && _mails[i].LineId > 0)
                ids.Add(_mails[i].LineId);
        }
        return ids;
    }

    public int AddClaimMail(long count)
    {
        if (count < 0)
            count = 0;
        _claimSerial++;
        int lineId = ClaimMailBase + _claimSerial;
        string text = "全ピクセル×" + count.ToString(CultureInfo.InvariantCulture) + "を受け取りました";
        _mails.Add(new StoryMail(lineId, text));
        return lineId;
    }

    public void Write(System.IO.StreamWriter sw)
    {
        if (sw == null)
            return;
        foreach (KeyValuePair<int, int> pair in _lastPage)
        {
            if (pair.Key <= 0 || pair.Value <= 0)
                continue;
            sw.WriteLine("StoryLast[" + pair.Key.ToString(CultureInfo.InvariantCulture) + "]," + pair.Value.ToString(CultureInfo.InvariantCulture));
        }
        WriteIds(sw, "StoryOpen", _open);
        WriteIds(sw, "StoryRead", _read);
        WriteIds(sw, "StoryClaimed", _claimed);
        for (int i = 0; i < _mails.Count; i++)
        {
            StoryMail mail = _mails[i];
            if (mail == null || mail.LineId <= 0 || string.IsNullOrEmpty(mail.Text))
                continue;
            sw.WriteLine("StoryMail[" + mail.LineId.ToString(CultureInfo.InvariantCulture) + "]," + mail.Text);
        }
    }

    public bool TryRead(string[] values)
    {
        if (values == null || values.Length < 1 || string.IsNullOrEmpty(values[0]))
            return false;
        string key = values[0];
        if (!key.StartsWith("Story", StringComparison.Ordinal))
            return false;
        if (values.Length < 2)
            return true;
        if (key.StartsWith("StoryLast[", StringComparison.Ordinal) && key.EndsWith("]", StringComparison.Ordinal))
        {
            string inside = key.Substring("StoryLast[".Length, key.Length - "StoryLast[".Length - 1);
            int chapterId;
            int page;
            if (int.TryParse(inside, NumberStyles.Integer, CultureInfo.InvariantCulture, out chapterId)
                && int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out page)
                && chapterId > 0 && page > 0)
                _lastPage[chapterId] = page;
            return true;
        }
        if (key == "StoryOpen")
        {
            ReadIds(values[1], _open);
            return true;
        }
        if (key == "StoryRead")
        {
            ReadIds(values[1], _read);
            return true;
        }
        if (key == "StoryClaimed")
        {
            ReadIds(values[1], _claimed);
            return true;
        }
        if (key.StartsWith("StoryMail[", StringComparison.Ordinal) && key.EndsWith("]", StringComparison.Ordinal))
        {
            string inside = key.Substring("StoryMail[".Length, key.Length - "StoryMail[".Length - 1);
            int lineId;
            if (!int.TryParse(inside, NumberStyles.Integer, CultureInfo.InvariantCulture, out lineId) || lineId <= 0)
                return true;
            string text = values[1];
            for (int i = 2; i < values.Length; i++)
                text += "," + values[i];
            if (MailText(lineId) == null)
                _mails.Add(new StoryMail(lineId, text));
            if (lineId > ClaimMailBase)
            {
                int serial = lineId - ClaimMailBase;
                if (serial > _claimSerial)
                    _claimSerial = serial;
            }
            return true;
        }
        return true;
    }

    sealed class StoryMail
    {
        public readonly int LineId;
        public readonly string Text;

        public StoryMail(int lineId, string text)
        {
            LineId = lineId;
            Text = text ?? "";
        }
    }

    static bool ConditionMet(ControllerProduction game, int stage, int floor)
    {
        if (stage <= 0 || floor <= 0)
            return true;
        if (game == null)
            return false;
        return game.GetHighestClearedBattleFloor(stage) >= floor;
    }

    static bool PullInto(HashSet<int> source, HashSet<int> dest)
    {
        bool changed = false;
        foreach (int id in source)
        {
            if (!StoryCatalog.ContainsPage(id))
                continue;
            if (dest.Add(id))
                changed = true;
        }
        return changed;
    }

    static void WriteIds(System.IO.StreamWriter sw, string name, HashSet<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return;
        int[] list = new int[ids.Count];
        ids.CopyTo(list);
        Array.Sort(list);
        string line = name;
        bool any = false;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] <= 0)
                continue;
            line += any ? "|" : ",";
            line += list[i].ToString(CultureInfo.InvariantCulture);
            any = true;
        }
        if (any)
            sw.WriteLine(line);
    }

    static void ReadIds(string value, HashSet<int> dest)
    {
        dest.Clear();
        if (string.IsNullOrEmpty(value))
            return;
        string[] parts = value.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            int id;
            if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out id) || id <= 0)
                continue;
            dest.Add(id);
        }
    }
}
