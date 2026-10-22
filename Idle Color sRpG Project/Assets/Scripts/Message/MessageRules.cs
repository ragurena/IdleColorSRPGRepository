using System.Collections.Generic;

// 届く条件、支払い、上限の案内。画面は持たない。
public static class MessageRules
{
    public static bool MeetAll(MessageCond[] conds, ControllerProduction game, MessageSave save, MessageQuestState fromNow, bool useFromNow)
    {
        if (conds == null || conds.Length == 0)
            return true;
        for (int i = 0; i < conds.Length; i++)
        {
            if (!Meet(conds[i], game, save, Baseline(fromNow, useFromNow, i), useFromNow))
                return false;
        }
        return true;
    }

    public static bool Meet(MessageCond cond, ControllerProduction game, MessageSave save, long baseline, bool useFromNow)
    {
        if (cond == null || game == null)
            return false;
        if (cond.Kind == MessageCondKind.Always)
            return true;
        long current = Raw(cond, game, save);
        long target = cond.Target;
        if (target < 0)
            target = 0;
        if (useFromNow && IsNumeric(cond.Kind))
        {
            long progress = current - baseline;
            if (progress < 0)
                progress = 0;
            return progress >= target;
        }
        return current >= target;
    }

    public static long Raw(MessageCond cond, ControllerProduction game, MessageSave save)
    {
        if (cond == null || game == null)
            return 0;
        if (cond.Kind == MessageCondKind.MainCleared)
            return game.IsQuestCleared(QuestCatalog.Find(cond.Arg)) ? 1 : 0;
        if (cond.Kind == MessageCondKind.MainClaimed)
            return game.IsQuestClaimed(QuestCatalog.Find(cond.Arg)) ? 1 : 0;
        if (cond.Kind == MessageCondKind.StageFloor)
            return game.GetHighestClearedBattleFloor(cond.Arg);
        if (cond.Kind == MessageCondKind.QuestSent)
        {
            MessageQuestState state = save != null ? save.QuestOf(cond.Key) : null;
            return state != null && state.Sent ? 1 : 0;
        }
        if (cond.Kind == MessageCondKind.OfferAccepted)
            return save != null && save.Has(save.DoneOf(TalkIdOfOffer(cond.Arg)), cond.Arg) ? 1 : 0;
        MessageFace face = MessageRoster.Find(cond.Key);
        int personId = face != null ? face.PersonId : 0;
        if (cond.Kind == MessageCondKind.AffectionAtLeast)
            return game.AffectionOfPerson(personId);
        if (cond.Kind == MessageCondKind.AuthoredLevelAtLeast)
            return game.AuthoredMaxLevel(personId);
        if (cond.Kind == MessageCondKind.CharacterLevelAtLeast)
        {
            if (personId > 0 && game.CharacterAuthor(cond.CharacterId) != personId)
                return 0;
            return game.CharacterLevelOf(cond.CharacterId);
        }
        if (cond.Kind == MessageCondKind.CompanionsAtLeast)
            return game.AuthoredCompanionCount(personId);
        return 0;
    }

    public static bool IsNumeric(MessageCondKind kind)
    {
        return kind == MessageCondKind.StageFloor
            || kind == MessageCondKind.AffectionAtLeast
            || kind == MessageCondKind.AuthoredLevelAtLeast
            || kind == MessageCondKind.CharacterLevelAtLeast
            || kind == MessageCondKind.CompanionsAtLeast;
    }

    public static string ProgressText(MessageCond cond, ControllerProduction game, MessageSave save, MessageQuestState state, bool fromNow, int index)
    {
        if (cond == null)
            return "";
        if (cond.Kind == MessageCondKind.Always)
            return "";
        long current = Raw(cond, game, save);
        long target = cond.Target < 0 ? 0 : cond.Target;
        long baseline = Baseline(state, fromNow, index);
        if (fromNow && IsNumeric(cond.Kind))
        {
            current -= baseline;
            if (current < 0)
                current = 0;
        }
        if (current > target && target > 0)
            current = target;
        string label = Label(cond);
        if (cond.Kind == MessageCondKind.StageFloor)
            return label + " " + current.ToString() + "/" + target.ToString() + "階";
        return label + " " + current.ToString() + "/" + target.ToString();
    }

    public static string Label(MessageCond cond)
    {
        if (cond == null)
            return "";
        if (cond.Kind == MessageCondKind.MainCleared)
            return "クエスト" + cond.Arg.ToString() + "クリア";
        if (cond.Kind == MessageCondKind.MainClaimed)
            return "クエスト" + cond.Arg.ToString() + "受取";
        if (cond.Kind == MessageCondKind.StageFloor)
            return "ステージ" + cond.Arg.ToString();
        if (cond.Kind == MessageCondKind.QuestSent)
            return "送信";
        if (cond.Kind == MessageCondKind.OfferAccepted)
            return "受け取り";
        if (cond.Kind == MessageCondKind.AffectionAtLeast)
            return "好感度";
        if (cond.Kind == MessageCondKind.AuthoredLevelAtLeast)
            return "作者キャラのレベル";
        if (cond.Kind == MessageCondKind.CharacterLevelAtLeast)
            return "キャラ" + cond.CharacterId.ToString() + "のレベル";
        if (cond.Kind == MessageCondKind.CompanionsAtLeast)
            return "仲間にした種類";
        return "";
    }

    public static string Preview(MessageTalk talk, MessageLine line, MessageQuest quest)
    {
        if (line == null && quest == null)
            return "";
        if (quest != null && line != null && line.Kind == MessageLineKind.Quest)
            return Short(quest.Title);
        if (line == null)
            return "";
        if (line.Kind == MessageLineKind.Stamp)
            return "スタンプ";
        if (quest != null && line.Id == quest.SendId)
            return Short(quest.SendText);
        return Short(line.Body);
    }

    public static string Short(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        const int limit = 18;
        if (text.Length <= limit)
            return text;
        return text.Substring(0, limit);
    }

    public static long Baseline(MessageQuestState state, bool useFromNow, int index)
    {
        if (!useFromNow || state == null || !state.HasBaseline)
            return 0;
        if (index < 0 || index >= state.Baselines.Count)
            return 0;
        return state.Baselines[index];
    }

    public static string TalkIdOfOffer(int offerId)
    {
        MessageTalk[] talks = MessageScripts.All;
        if (talks == null)
            return "";
        for (int i = 0; i < talks.Length; i++)
        {
            MessageTalk talk = talks[i];
            if (talk == null)
                continue;
            for (int b = 0; b < talk.Blocks.Length; b++)
            {
                MessageOfferBlock block = talk.Blocks[b] as MessageOfferBlock;
                if (block != null && block.Offer != null && block.Offer.Id == offerId)
                    return talk.Id;
            }
        }
        return "";
    }

    public static void CaptureBaselines(MessageQuest quest, MessageQuestState state, ControllerProduction game, MessageSave save)
    {
        if (quest == null || state == null || state.HasBaseline)
            return;
        state.Baselines.Clear();
        if (quest.When != null)
        {
            for (int i = 0; i < quest.When.Length; i++)
                state.Baselines.Add(Raw(quest.When[i], game, save));
        }
        state.HasBaseline = true;
    }
}

public sealed class TalkRow
{
    public string TalkId;
    public string Name;
    public string IconFile;
    public string Preview;
    public bool Unread;
    public readonly List<OfferRow> Offers = new List<OfferRow>();
}

public sealed class OfferRow
{
    public string TalkId;
    public int OfferId;
    public string Prompt;
    public string Detail;
    public string Warning;
}

public sealed class ThreadLine
{
    public int LineId;
    public MessageLineKind Kind;
    public string SpeakerId;
    public string SpeakerName;
    public string IconFile;
    public string Text;
    public bool FromPlayer;
    public bool QuestOpen;
    public string QuestId;
}

public sealed class SendPrompt
{
    public string Text = "メッセージ";
    public bool Enabled;
    public string Warning = "";
    public string QuestId = "";
}
