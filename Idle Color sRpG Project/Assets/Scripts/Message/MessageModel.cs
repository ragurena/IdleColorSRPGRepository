using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// メッセージの定義と、セーブする届いた順。本文はこの中に書かない。
public enum MessageLineKind
{
    Text,
    Stamp,
    Quest
}

public enum MessageCondKind
{
    Always,
    MainCleared,
    MainClaimed,
    StageFloor,
    QuestSent,
    OfferAccepted,
    AffectionAtLeast,
    AuthoredLevelAtLeast,
    CharacterLevelAtLeast,
    CompanionsAtLeast
}

public enum MessagePayKind
{
    Rgb,
    Pixel,
    Owned
}

public enum MessageGiftKind
{
    Rgb,
    Pixel,
    Owned,
    Affection,
    QuestReward
}

public sealed class MessageFace
{
    public readonly string Id;
    public readonly string Name;
    public readonly string IconFile;
    public readonly bool Listed;
    public readonly int PersonId;
    public readonly bool HasIconColor;
    public readonly Color IconColor;

    public MessageFace(string id, string name, string iconFile, bool listed, int personId)
        : this(id, name, iconFile, listed, personId, false, default(Color))
    {
    }

    public MessageFace(string id, string name, string iconFile, bool listed, int personId, Color iconColor)
        : this(id, name, iconFile, listed, personId, true, iconColor)
    {
    }

    MessageFace(string id, string name, string iconFile, bool listed, int personId, bool hasIconColor, Color iconColor)
    {
        Id = id;
        Name = name;
        IconFile = iconFile;
        Listed = listed;
        PersonId = personId;
        HasIconColor = hasIconColor;
        IconColor = iconColor;
    }
}

public sealed class MessageCond
{
    public readonly MessageCondKind Kind;
    public readonly int Arg;
    public readonly int Arg2;
    public readonly long Target;
    public readonly string Key;
    public readonly uint CharacterId;

    public MessageCond(MessageCondKind kind, int arg, int arg2, long target, string key, uint characterId)
    {
        Kind = kind;
        Arg = arg;
        Arg2 = arg2;
        Target = target;
        Key = key ?? "";
        CharacterId = characterId;
    }
}

public sealed class MessageCost
{
    public readonly MessagePayKind Kind;
    public readonly long R;
    public readonly long G;
    public readonly long B;
    public readonly long Count;
    public readonly uint CharacterId;

    public MessageCost(MessagePayKind kind, long r, long g, long b, long count, uint characterId)
    {
        Kind = kind;
        R = r;
        G = g;
        B = b;
        Count = count;
        CharacterId = characterId;
    }
}

public sealed class MessageGift
{
    public readonly MessageGiftKind Kind;
    public readonly long R;
    public readonly long G;
    public readonly long B;
    public readonly long Count;
    public readonly uint CharacterId;
    public readonly string FaceId;
    public readonly QuestRewardKind Reward;
    public readonly long RewardAmount;
    public readonly int Choice;

    public MessageGift(MessageGiftKind kind, long r, long g, long b, long count, uint characterId, string faceId, QuestRewardKind reward, long rewardAmount, int choice)
    {
        Kind = kind;
        R = r;
        G = g;
        B = b;
        Count = count;
        CharacterId = characterId;
        FaceId = faceId ?? "";
        Reward = reward;
        RewardAmount = rewardAmount;
        Choice = choice;
    }
}

public sealed class MessageLine
{
    public readonly int Id;
    public readonly string SpeakerId;
    public readonly bool FromPlayer;
    public readonly MessageLineKind Kind;
    public readonly string Body;
    public readonly string QuestId;

    public MessageLine(int id, string speakerId, bool fromPlayer, MessageLineKind kind, string body, string questId)
    {
        Id = id;
        SpeakerId = speakerId ?? "";
        FromPlayer = fromPlayer;
        Kind = kind;
        Body = body ?? "";
        QuestId = questId ?? "";
    }
}

public sealed class MessageChapter
{
    public readonly MessageCond[] When;
    public readonly MessageLine[] Lines;

    public MessageChapter(MessageCond[] when, MessageLine[] lines)
    {
        When = when ?? new MessageCond[0];
        Lines = lines ?? new MessageLine[0];
    }
}

public sealed class MessageOffer
{
    public readonly int Id;
    public readonly MessageCond[] When;
    public readonly string Prompt;
    public readonly MessageGift[] Gifts;
    public readonly MessageLine[] Hook;

    public MessageOffer(int id, MessageCond[] when, string prompt, MessageGift[] gifts, MessageLine[] hook)
    {
        Id = id;
        When = when ?? new MessageCond[0];
        Prompt = string.IsNullOrEmpty(prompt) ? "受け取りますか？" : prompt;
        Gifts = gifts ?? new MessageGift[0];
        Hook = hook ?? new MessageLine[0];
    }
}

public sealed class MessageQuest
{
    public readonly string Id;
    public readonly int PlateId;
    public readonly int SendId;
    public readonly string Title;
    public readonly MessageCond[] When;
    public readonly bool FromNow;
    public readonly string SendText;
    public readonly MessageCost[] Costs;
    public readonly MessageGift[] Reward;

    public MessageQuest(string id, int plateId, int sendId, string title, MessageCond[] when, bool fromNow, string sendText, MessageCost[] costs, MessageGift[] reward)
    {
        Id = id ?? "";
        PlateId = plateId;
        SendId = sendId;
        Title = title ?? "";
        When = when ?? new MessageCond[0];
        FromNow = fromNow;
        SendText = sendText ?? "";
        Costs = costs ?? new MessageCost[0];
        Reward = reward ?? new MessageGift[0];
    }
}

public abstract class MessageBlock
{
}

public sealed class MessageChapterBlock : MessageBlock
{
    public readonly MessageChapter Chapter;

    public MessageChapterBlock(MessageChapter chapter)
    {
        Chapter = chapter;
    }
}

public sealed class MessageOfferBlock : MessageBlock
{
    public readonly MessageOffer Offer;

    public MessageOfferBlock(MessageOffer offer)
    {
        Offer = offer;
    }
}

public sealed class MessageTalk
{
    public readonly string Id;
    public readonly MessageQuest[] Quests;
    public readonly MessageBlock[] Blocks;

    public MessageTalk(string id, MessageQuest[] quests, MessageBlock[] blocks)
    {
        Id = id ?? "";
        Quests = quests ?? new MessageQuest[0];
        Blocks = blocks ?? new MessageBlock[0];
    }

    public MessageQuest FindQuest(string questId)
    {
        for (int i = 0; i < Quests.Length; i++)
        {
            if (Quests[i] != null && Quests[i].Id == questId)
                return Quests[i];
        }
        return null;
    }

    public MessageQuest FindQuestByPlate(int plateId)
    {
        for (int i = 0; i < Quests.Length; i++)
        {
            if (Quests[i] != null && Quests[i].PlateId == plateId)
                return Quests[i];
        }
        return null;
    }

    public MessageQuest FindQuestBySend(int sendId)
    {
        for (int i = 0; i < Quests.Length; i++)
        {
            if (Quests[i] != null && Quests[i].SendId == sendId)
                return Quests[i];
        }
        return null;
    }
}

public sealed class MessageQuestState
{
    public bool Sent;
    public bool HasBaseline;
    public readonly List<long> Baselines = new List<long>();
}

public sealed class MessageSave
{
    public readonly Dictionary<string, List<int>> Order = new Dictionary<string, List<int>>();
    public readonly Dictionary<string, int> ReadId = new Dictionary<string, int>();
    public readonly Dictionary<string, List<int>> Pending = new Dictionary<string, List<int>>();
    public readonly Dictionary<string, List<int>> Done = new Dictionary<string, List<int>>();
    public readonly Dictionary<string, MessageQuestState> Quests = new Dictionary<string, MessageQuestState>();

    public void Clear()
    {
        Order.Clear();
        ReadId.Clear();
        Pending.Clear();
        Done.Clear();
        Quests.Clear();
    }

    public MessageSave Copy()
    {
        MessageSave copy = new MessageSave();
        CopyLists(Order, copy.Order);
        foreach (KeyValuePair<string, int> pair in ReadId)
            copy.ReadId[pair.Key] = pair.Value;
        CopyLists(Pending, copy.Pending);
        CopyLists(Done, copy.Done);
        foreach (KeyValuePair<string, MessageQuestState> pair in Quests)
        {
            MessageQuestState state = new MessageQuestState();
            state.Sent = pair.Value.Sent;
            state.HasBaseline = pair.Value.HasBaseline;
            for (int i = 0; i < pair.Value.Baselines.Count; i++)
                state.Baselines.Add(pair.Value.Baselines[i]);
            copy.Quests[pair.Key] = state;
        }
        return copy;
    }

    public void Replace(MessageSave source)
    {
        Clear();
        if (source == null)
            return;
        CopyLists(source.Order, Order);
        foreach (KeyValuePair<string, int> pair in source.ReadId)
            ReadId[pair.Key] = pair.Value;
        CopyLists(source.Pending, Pending);
        CopyLists(source.Done, Done);
        foreach (KeyValuePair<string, MessageQuestState> pair in source.Quests)
        {
            MessageQuestState state = new MessageQuestState();
            state.Sent = pair.Value.Sent;
            state.HasBaseline = pair.Value.HasBaseline;
            for (int i = 0; i < pair.Value.Baselines.Count; i++)
                state.Baselines.Add(pair.Value.Baselines[i]);
            Quests[pair.Key] = state;
        }
    }

    public List<int> OrderOf(string talkId)
    {
        return ListOf(Order, talkId);
    }

    public List<int> PendingOf(string talkId)
    {
        return ListOf(Pending, talkId);
    }

    public List<int> DoneOf(string talkId)
    {
        return ListOf(Done, talkId);
    }

    public bool Has(List<int> list, int id)
    {
        return list != null && list.Contains(id);
    }

    public int ReadOf(string talkId)
    {
        int id;
        if (ReadId.TryGetValue(talkId, out id))
            return id;
        return 0;
    }

    public MessageQuestState QuestOf(string questId)
    {
        MessageQuestState state;
        if (!Quests.TryGetValue(questId, out state))
        {
            state = new MessageQuestState();
            Quests[questId] = state;
        }
        return state;
    }

    public void Write(System.IO.StreamWriter sw)
    {
        if (sw == null)
            return;
        WriteIds(sw, "MessageOrder", Order);
        foreach (KeyValuePair<string, int> pair in ReadId)
        {
            if (pair.Value == 0)
                continue;
            sw.WriteLine("MessageRead[" + pair.Key + "]," + pair.Value.ToString(CultureInfo.InvariantCulture));
        }
        WriteIds(sw, "MessageOffer", Pending);
        WriteIds(sw, "MessageOfferDone", Done);
        foreach (KeyValuePair<string, MessageQuestState> pair in Quests)
        {
            MessageQuestState state = pair.Value;
            if (state == null || (!state.Sent && !state.HasBaseline))
                continue;
            string line = "MessageQuest[" + pair.Key + "]," + (state.Sent ? "1" : "0");
            if (state.HasBaseline)
            {
                for (int i = 0; i < state.Baselines.Count; i++)
                    line += "," + state.Baselines[i].ToString(CultureInfo.InvariantCulture);
            }
            sw.WriteLine(line);
        }
    }

    public bool TryRead(string[] values)
    {
        if (values == null || values.Length < 1 || string.IsNullOrEmpty(values[0]))
            return false;
        string key = values[0];
        if (TryIds("MessageOrder[", key, values, Order))
            return true;
        if (TryIds("MessageOffer[", key, values, Pending))
            return true;
        if (TryIds("MessageOfferDone[", key, values, Done))
            return true;
        if (key.StartsWith("MessageRead[", StringComparison.Ordinal) && key.EndsWith("]"))
        {
            string talkId = Inside(key, "MessageRead[");
            if (values.Length < 2)
                return true;
            int id;
            if (int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out id) && id > 0)
                ReadId[talkId] = id;
            return true;
        }
        if (key.StartsWith("MessageQuest[", StringComparison.Ordinal) && key.EndsWith("]"))
        {
            string questId = Inside(key, "MessageQuest[");
            MessageQuestState state = QuestOf(questId);
            state.Sent = false;
            state.HasBaseline = false;
            state.Baselines.Clear();
            if (values.Length >= 2)
                state.Sent = values[1] == "1";
            if (values.Length > 2)
            {
                state.HasBaseline = true;
                for (int i = 2; i < values.Length; i++)
                {
                    long number;
                    if (!long.TryParse(values[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                        number = 0;
                    state.Baselines.Add(number);
                }
            }
            return true;
        }
        return false;
    }

    static void WriteIds(System.IO.StreamWriter sw, string name, Dictionary<string, List<int>> source)
    {
        foreach (KeyValuePair<string, List<int>> pair in source)
        {
            if (pair.Value == null || pair.Value.Count == 0)
                continue;
            string line = name + "[" + pair.Key + "]";
            for (int i = 0; i < pair.Value.Count; i++)
                line += "," + pair.Value[i].ToString(CultureInfo.InvariantCulture);
            sw.WriteLine(line);
        }
    }

    static bool TryIds(string prefix, string key, string[] values, Dictionary<string, List<int>> dest)
    {
        if (!key.StartsWith(prefix, StringComparison.Ordinal) || !key.EndsWith("]"))
            return false;
        string id = Inside(key, prefix);
        List<int> list = new List<int>();
        for (int i = 1; i < values.Length; i++)
        {
            int number;
            if (!int.TryParse(values[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                continue;
            if (!list.Contains(number))
                list.Add(number);
        }
        dest[id] = list;
        return true;
    }

    static string Inside(string key, string prefix)
    {
        return key.Substring(prefix.Length, key.Length - prefix.Length - 1);
    }

    static List<int> ListOf(Dictionary<string, List<int>> source, string id)
    {
        List<int> list;
        if (string.IsNullOrEmpty(id) || !source.TryGetValue(id, out list) || list == null)
        {
            list = new List<int>();
            if (!string.IsNullOrEmpty(id))
                source[id] = list;
        }
        return list;
    }

    static void CopyLists(Dictionary<string, List<int>> source, Dictionary<string, List<int>> dest)
    {
        foreach (KeyValuePair<string, List<int>> pair in source)
        {
            List<int> copy = new List<int>();
            if (pair.Value != null)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                    copy.Add(pair.Value[i]);
            }
            dest[pair.Key] = copy;
        }
    }
}

public struct MessageStockSnap
{
    public uint Id;
    public OwnedStock Stock;
    public ulong OwnedMax;
}

public struct MessagePixelSnap
{
    public int R;
    public int G;
    public int B;
    public ulong Count;
}

// 送信か受け取りの直前の所持。失敗したときにここへ戻す。
public sealed class MessageMoment
{
    public ulong R;
    public ulong G;
    public ulong B;
    public ulong TapR;
    public ulong TapG;
    public ulong TapB;
    public int PixelSlots;
    public int CharacterSlots;
    public int ImportCap;
    public bool FusionUnlocked;
    public int FusionEfficiency;
    public int[] Affection = new int[0];
    public MessageStockSnap[] Stocks = new MessageStockSnap[0];
    public MessagePixelSnap[] Pixels = new MessagePixelSnap[0];
    public MessageSave Messages;
}
