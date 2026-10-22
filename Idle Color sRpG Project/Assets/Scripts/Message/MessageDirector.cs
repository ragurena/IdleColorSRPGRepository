using System;
using System.Collections.Generic;

// 届いた順を足す。同じ番号は何度呼んでも一度しか入らない。所持は変えない。
public sealed class MessageDirector
{
    readonly ControllerProduction _game;
    readonly MessageSave _save = new MessageSave();
    bool _busy;

    public MessageDirector(ControllerProduction game)
    {
        _game = game;
    }

    public MessageSave Save
    {
        get { return _save; }
    }

    public bool Busy
    {
        get { return _busy; }
    }

    public bool CatchUp()
    {
        bool added = false;
        MessageTalk[] talks = MessageScripts.All;
        if (talks == null)
            return false;
        for (int i = 0; i < talks.Length; i++)
            added |= CatchUpTalk(talks[i]);
        return added;
    }

    public bool AnyVisible()
    {
        MessageFace[] faces = MessageRoster.All;
        if (faces == null)
            return false;
        for (int i = 0; i < faces.Length; i++)
        {
            MessageFace face = faces[i];
            if (face == null || !face.Listed)
                continue;
            if (Save.OrderOf(face.Id).Count > 0 || Save.PendingOf(face.Id).Count > 0)
                return true;
        }
        return false;
    }

    public int BadgeCount()
    {
        int count = 0;
        MessageFace[] faces = MessageRoster.All;
        if (faces == null)
            return 0;
        for (int i = 0; i < faces.Length; i++)
        {
            MessageFace face = faces[i];
            if (face == null || !face.Listed)
                continue;
            if (IsUnread(face.Id))
                count++;
            count += Save.PendingOf(face.Id).Count;
        }
        return count;
    }

    public List<TalkRow> Rows()
    {
        List<TalkRow> rows = new List<TalkRow>();
        MessageFace[] faces = MessageRoster.All;
        if (faces == null)
            return rows;
        for (int i = 0; i < faces.Length; i++)
        {
            MessageFace face = faces[i];
            if (face == null || !face.Listed)
                continue;
            List<int> order = Save.OrderOf(face.Id);
            List<int> pending = Save.PendingOf(face.Id);
            if (order.Count == 0 && pending.Count == 0)
                continue;
            MessageTalk talk = MessageScripts.Find(face.Id);
            TalkRow row = new TalkRow();
            row.TalkId = face.Id;
            row.Name = face.Name;
            row.IconFile = face.IconFile;
            row.Unread = IsUnread(face.Id);
            if (order.Count > 0)
                row.Preview = PreviewOf(talk, order[order.Count - 1]);
            for (int p = 0; p < pending.Count; p++)
            {
                MessageOffer offer = FindOffer(talk, pending[p]);
                if (offer == null)
                    continue;
                OfferRow item = new OfferRow();
                item.TalkId = face.Id;
                item.OfferId = offer.Id;
                item.Prompt = offer.Prompt;
                item.Detail = _game.MessageGiftDetail(offer.Gifts);
                item.Warning = _game.MessageGiftWarning(offer.Gifts);
                row.Offers.Add(item);
            }
            rows.Add(row);
        }
        return rows;
    }

    public List<ThreadLine> Thread(string talkId)
    {
        List<ThreadLine> lines = new List<ThreadLine>();
        MessageTalk talk = MessageScripts.Find(talkId);
        List<int> order = Save.OrderOf(talkId);
        for (int i = 0; i < order.Count; i++)
            lines.Add(BuildLine(talk, talkId, order[i]));
        return lines;
    }

    public SendPrompt Prompt(string talkId)
    {
        SendPrompt prompt = new SendPrompt();
        MessageQuest quest = ReadyQuest(talkId);
        if (quest == null)
            return prompt;
        prompt.Text = string.IsNullOrEmpty(quest.SendText) ? "メッセージ" : quest.SendText;
        prompt.Enabled = true;
        prompt.Warning = _game.MessageGiftWarning(quest.Reward);
        prompt.QuestId = quest.Id;
        return prompt;
    }

    public bool TryAccept(string talkId, int offerId)
    {
        if (_busy || _game == null)
            return false;
        MessageTalk talk = MessageScripts.Find(talkId);
        MessageOffer offer = FindOffer(talk, offerId);
        if (offer == null || !Save.Has(Save.PendingOf(talkId), offerId) || Save.Has(Save.DoneOf(talkId), offerId))
            return false;
        _busy = true;
        MessageMoment moment = _game.CaptureMessageMoment(offer.Gifts, null);
        try
        {
            _game.MessageApply(offer.Gifts);
            AppendHook(talkId, offer);
            Save.PendingOf(talkId).Remove(offerId);
            if (!Save.Has(Save.DoneOf(talkId), offerId))
                Save.DoneOf(talkId).Add(offerId);
            CatchUp();
            if (!_game.SaveGame())
            {
                _game.RestoreMessageMoment(moment);
                return false;
            }
            _game.RefreshAfterMessage();
            return true;
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("メッセージの受け取りに失敗しました。 " + exception.Message);
            _game.RestoreMessageMoment(moment);
            return false;
        }
        finally
        {
            _busy = false;
        }
    }

    public bool TrySend(string talkId)
    {
        if (_busy || _game == null)
            return false;
        MessageQuest quest = ReadyQuest(talkId);
        if (quest == null)
            return false;
        _busy = true;
        MessageMoment moment = _game.CaptureMessageMoment(quest.Reward, quest.Costs);
        try
        {
            _game.MessagePay(quest.Costs);
            _game.MessageApply(quest.Reward);
            AppendId(Save.OrderOf(talkId), quest.SendId);
            MessageQuestState state = Save.QuestOf(quest.Id);
            state.Sent = true;
            CatchUp();
            if (!_game.SaveGame())
            {
                _game.RestoreMessageMoment(moment);
                return false;
            }
            _game.RefreshAfterMessage();
            return true;
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("メッセージの送信に失敗しました。 " + exception.Message);
            _game.RestoreMessageMoment(moment);
            return false;
        }
        finally
        {
            _busy = false;
        }
    }

    public void MarkRead(string talkId)
    {
        List<int> order = Save.OrderOf(talkId);
        if (order.Count == 0)
            return;
        Save.ReadId[talkId] = order[order.Count - 1];
    }

    public bool IsUnread(string talkId)
    {
        List<int> order = Save.OrderOf(talkId);
        int read = Save.ReadOf(talkId);
        int readIndex = order.IndexOf(read);
        if (read == 0 || readIndex < 0)
            return order.Count > 0;
        return readIndex < order.Count - 1;
    }

    bool CatchUpTalk(MessageTalk talk)
    {
        if (talk == null)
            return false;
        bool added = false;
        for (int i = 0; i < talk.Blocks.Length; i++)
        {
            MessageChapterBlock chapterBlock = talk.Blocks[i] as MessageChapterBlock;
            if (chapterBlock != null && chapterBlock.Chapter != null)
            {
                added |= CatchUpChapter(talk, chapterBlock.Chapter);
                continue;
            }
            MessageOfferBlock offerBlock = talk.Blocks[i] as MessageOfferBlock;
            if (offerBlock != null && offerBlock.Offer != null)
                added |= CatchUpOffer(talk, offerBlock.Offer);
        }
        return added;
    }

    bool CatchUpChapter(MessageTalk talk, MessageChapter chapter)
    {
        if (chapter.Lines == null || chapter.Lines.Length == 0)
            return false;
        if (ChapterPresent(talk.Id, chapter))
            return false;
        if (!EarlierChaptersPresent(talk, chapter))
            return false;
        if (!MessageRules.MeetAll(chapter.When, _game, _save, null, false))
            return false;
        List<int> order = Save.OrderOf(talk.Id);
        for (int i = 0; i < chapter.Lines.Length; i++)
        {
            MessageLine line = chapter.Lines[i];
            if (line == null)
                continue;
            AppendId(order, line.Id);
            if (line.Kind == MessageLineKind.Quest)
            {
                MessageQuest quest = talk.FindQuest(line.QuestId);
                if (quest != null && quest.FromNow)
                    MessageRules.CaptureBaselines(quest, Save.QuestOf(quest.Id), _game, _save);
            }
        }
        return true;
    }

    bool CatchUpOffer(MessageTalk talk, MessageOffer offer)
    {
        if (Save.Has(Save.PendingOf(talk.Id), offer.Id) || Save.Has(Save.DoneOf(talk.Id), offer.Id))
            return false;
        if (offer.When == null || offer.When.Length == 0)
            return false;
        if (!MessageRules.MeetAll(offer.When, _game, _save, null, false))
            return false;
        Save.PendingOf(talk.Id).Add(offer.Id);
        return true;
    }

    bool EarlierChaptersPresent(MessageTalk talk, MessageChapter chapter)
    {
        for (int i = 0; i < talk.Blocks.Length; i++)
        {
            MessageChapterBlock block = talk.Blocks[i] as MessageChapterBlock;
            if (block == null || block.Chapter == null)
                continue;
            if (block.Chapter == chapter)
                return true;
            if (!ChapterPresent(talk.Id, block.Chapter))
                return false;
        }
        return true;
    }

    bool ChapterPresent(string talkId, MessageChapter chapter)
    {
        if (chapter == null || chapter.Lines == null)
            return false;
        List<int> order = Save.OrderOf(talkId);
        for (int i = 0; i < chapter.Lines.Length; i++)
        {
            if (chapter.Lines[i] != null && order.Contains(chapter.Lines[i].Id))
                return true;
        }
        return false;
    }

    MessageQuest ReadyQuest(string talkId)
    {
        MessageTalk talk = MessageScripts.Find(talkId);
        if (talk == null)
            return null;
        List<int> order = Save.OrderOf(talkId);
        for (int i = 0; i < order.Count; i++)
        {
            MessageQuest quest = talk.FindQuestByPlate(order[i]);
            if (quest == null || Save.QuestOf(quest.Id).Sent)
                continue;
            MessageQuestState state = Save.QuestOf(quest.Id);
            if (!MessageRules.MeetAll(quest.When, _game, _save, state, quest.FromNow))
                continue;
            if (!_game.MessageCanPay(quest.Costs))
                continue;
            return quest;
        }
        return null;
    }

    ThreadLine BuildLine(MessageTalk talk, string talkId, int lineId)
    {
        ThreadLine view = new ThreadLine();
        view.LineId = lineId;
        MessageQuest sendQuest = talk != null ? talk.FindQuestBySend(lineId) : null;
        if (sendQuest != null)
        {
            view.Kind = MessageLineKind.Text;
            view.FromPlayer = true;
            view.Text = sendQuest.SendText;
            return view;
        }
        MessageLine line = FindLine(talk, lineId);
        if (line == null)
        {
            view.Kind = MessageLineKind.Text;
            view.Text = "";
            return view;
        }
        view.Kind = line.Kind;
        view.FromPlayer = line.FromPlayer;
        view.SpeakerId = line.SpeakerId;
        view.QuestId = line.QuestId;
        MessageFace speaker = MessageRoster.Find(line.SpeakerId);
        if (speaker != null)
        {
            view.SpeakerName = speaker.Name;
            view.IconFile = speaker.IconFile;
        }
        if (line.Kind == MessageLineKind.Quest)
        {
            MessageQuest quest = talk != null ? talk.FindQuest(line.QuestId) : null;
            view.Text = PlateText(quest);
            view.QuestOpen = quest != null && !Save.QuestOf(quest.Id).Sent;
            view.QuestId = line.QuestId;
            return view;
        }
        view.Text = line.Body;
        return view;
    }

    string PlateText(MessageQuest quest)
    {
        if (quest == null)
            return "";
        MessageQuestState state = Save.QuestOf(quest.Id);
        string text = quest.Title;
        if (quest.When != null)
        {
            for (int i = 0; i < quest.When.Length; i++)
            {
                string progress = MessageRules.ProgressText(quest.When[i], _game, _save, state, quest.FromNow, i);
                if (progress.Length > 0)
                    text += "\n" + progress;
            }
        }
        if (quest.Costs != null && quest.Costs.Length > 0)
            text += "\n" + _game.MessageCostDetail(quest.Costs);
        if (state.Sent)
            text += "\n送信済み";
        return text;
    }

    string PreviewOf(MessageTalk talk, int lineId)
    {
        if (talk != null)
        {
            MessageQuest send = talk.FindQuestBySend(lineId);
            if (send != null)
                return MessageRules.Short(send.SendText);
        }
        MessageLine line = FindLine(talk, lineId);
        MessageQuest quest = line != null && line.Kind == MessageLineKind.Quest && talk != null ? talk.FindQuest(line.QuestId) : null;
        return MessageRules.Preview(talk, line, quest);
    }

    static MessageLine FindLine(MessageTalk talk, int lineId)
    {
        if (talk == null)
            return null;
        for (int i = 0; i < talk.Blocks.Length; i++)
        {
            MessageChapterBlock chapter = talk.Blocks[i] as MessageChapterBlock;
            if (chapter != null && chapter.Chapter != null)
            {
                MessageLine found = FindIn(chapter.Chapter.Lines, lineId);
                if (found != null)
                    return found;
            }
            MessageOfferBlock offer = talk.Blocks[i] as MessageOfferBlock;
            if (offer != null && offer.Offer != null)
            {
                MessageLine found = FindIn(offer.Offer.Hook, lineId);
                if (found != null)
                    return found;
            }
        }
        return null;
    }

    static MessageLine FindIn(MessageLine[] lines, int lineId)
    {
        if (lines == null)
            return null;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] != null && lines[i].Id == lineId)
                return lines[i];
        }
        return null;
    }

    static MessageOffer FindOffer(MessageTalk talk, int offerId)
    {
        if (talk == null)
            return null;
        for (int i = 0; i < talk.Blocks.Length; i++)
        {
            MessageOfferBlock block = talk.Blocks[i] as MessageOfferBlock;
            if (block != null && block.Offer != null && block.Offer.Id == offerId)
                return block.Offer;
        }
        return null;
    }

    void AppendHook(string talkId, MessageOffer offer)
    {
        if (offer == null || offer.Hook == null)
            return;
        List<int> order = Save.OrderOf(talkId);
        for (int i = 0; i < offer.Hook.Length; i++)
        {
            if (offer.Hook[i] != null)
                AppendId(order, offer.Hook[i].Id);
        }
    }

    static void AppendId(List<int> order, int id)
    {
        if (order == null || order.Contains(id))
            return;
        order.Add(id);
    }
}
