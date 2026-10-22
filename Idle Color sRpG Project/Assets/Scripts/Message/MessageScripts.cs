// 各トークの中身。一覧の id と Talk の id を揃える。
// 発言番号と差し出し番号は、そのトークの中で重ならないようにする。一度付けた番号は変えない。
// まとまりは上から順。前のまとまりが届いてから、条件がそろったときに末尾へ足す。
// 差し出しは前を待たない。条件がそろったら一覧の直下で「はい」を待つ。
// ピクセルの色は 0, 32, 64, 96, 128, 160, 192, 224, 255。
// スタンプ画像は Assets/StreamingAssets/message/stamp/ 。
public static class MessageScripts
{
    public static readonly MessageTalk[] All =
    {
        Talk("chigusa", new MessageQuest[]
        {
            Quest("chigusa_q1", 3, 4, "あいさつを返す", new MessageCond[] { Cond.Always() }, false,
                "おはよう",
                new MessageCost[0],
                new MessageGift[] { Gift.Rgb(100, 100, 100), Gift.Affection("chigusa", 1) }),
        },
            Chapter(new MessageCond[] { Cond.Always() },
                Line.Say(1, "chigusa", "おはよう。メッセージ、届いてる？"),
                Line.Say(2, "chigusa", "届いてたら、あいさつを返して。")),
            Chapter(new MessageCond[] { Cond.Always() },
                Line.Quest(3, "chigusa_q1")),
            Chapter(new MessageCond[] { Cond.QuestSent("chigusa_q1") },
                Line.Say(5, "chigusa", "報告ありがとう。")),
            Offer(80, new MessageCond[] { Cond.Affection("chigusa", 1) }, "受け取りますか？",
                new MessageGift[] { Gift.Rgb(50, 0, 0) },
                Line.Mine(81, "ありがとう"),
                Line.Say(82, "chigusa", "どういたしまして。"))),

        Talk("class", new MessageQuest[0],
            Chapter(new MessageCond[] { Cond.Always() },
                Line.Say(1, "chigusa", "みんな、集合しよ。"),
                Line.Say(2, "moa", "いま行く。"),
                Line.Stamp(3, "moa", "stamp_hi.png"))),
    };

    public static MessageTalk Find(string id)
    {
        if (string.IsNullOrEmpty(id) || All == null)
            return null;
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i] != null && All[i].Id == id)
                return All[i];
        }
        return null;
    }

    static MessageTalk Talk(string id, MessageQuest[] quests, params MessageBlock[] blocks)
    {
        return new MessageTalk(id, quests, blocks);
    }

    static MessageBlock Chapter(MessageCond[] when, params MessageLine[] lines)
    {
        return new MessageChapterBlock(new MessageChapter(when, lines));
    }

    static MessageBlock Offer(int id, MessageCond[] when, string prompt, MessageGift[] gifts, params MessageLine[] hook)
    {
        return new MessageOfferBlock(new MessageOffer(id, when, prompt, gifts, hook));
    }

    static MessageQuest Quest(string id, int plateId, int sendId, string title, MessageCond[] when, bool fromNow, string sendText, MessageCost[] costs, MessageGift[] reward)
    {
        return new MessageQuest(id, plateId, sendId, title, when, fromNow, sendText, costs, reward);
    }
}

public static class Cond
{
    public static MessageCond Always()
    {
        return new MessageCond(MessageCondKind.Always, 0, 0, 0, "", 0);
    }

    public static MessageCond MainCleared(int questId)
    {
        return new MessageCond(MessageCondKind.MainCleared, questId, 0, 1, "", 0);
    }

    public static MessageCond MainClaimed(int questId)
    {
        return new MessageCond(MessageCondKind.MainClaimed, questId, 0, 1, "", 0);
    }

    public static MessageCond Stage(int stage, int floor)
    {
        return new MessageCond(MessageCondKind.StageFloor, stage, 0, floor, "", 0);
    }

    public static MessageCond QuestSent(string questId)
    {
        return new MessageCond(MessageCondKind.QuestSent, 0, 0, 1, questId, 0);
    }

    public static MessageCond OfferAccepted(int offerId)
    {
        return new MessageCond(MessageCondKind.OfferAccepted, offerId, 0, 1, "", 0);
    }

    public static MessageCond Affection(string faceId, int amount)
    {
        return new MessageCond(MessageCondKind.AffectionAtLeast, 0, 0, amount, faceId, 0);
    }

    public static MessageCond AuthoredLevel(string faceId, int level)
    {
        return new MessageCond(MessageCondKind.AuthoredLevelAtLeast, 0, 0, level, faceId, 0);
    }

    public static MessageCond CharacterLevel(string faceId, uint characterId, int level)
    {
        return new MessageCond(MessageCondKind.CharacterLevelAtLeast, 0, 0, level, faceId, characterId);
    }

    public static MessageCond Companions(string faceId, int count)
    {
        return new MessageCond(MessageCondKind.CompanionsAtLeast, 0, 0, count, faceId, 0);
    }
}

public static class Line
{
    public static MessageLine Say(int id, string speakerId, string text)
    {
        return new MessageLine(id, speakerId, false, MessageLineKind.Text, text, "");
    }

    public static MessageLine Mine(int id, string text)
    {
        return new MessageLine(id, "", true, MessageLineKind.Text, text, "");
    }

    public static MessageLine Stamp(int id, string speakerId, string fileName)
    {
        return new MessageLine(id, speakerId, false, MessageLineKind.Stamp, fileName, "");
    }

    public static MessageLine Quest(int id, string questId)
    {
        return new MessageLine(id, "", false, MessageLineKind.Quest, "", questId);
    }
}

public static class Gift
{
    public static MessageGift Rgb(long r, long g, long b)
    {
        return new MessageGift(MessageGiftKind.Rgb, r, g, b, 0, 0, "", QuestRewardKind.RgbAll, 0, 0);
    }

    public static MessageGift Pixel(int r, int g, int b, long count)
    {
        return new MessageGift(MessageGiftKind.Pixel, r, g, b, count, 0, "", QuestRewardKind.RgbAll, 0, 0);
    }

    public static MessageGift Owned(uint characterId, long count)
    {
        return new MessageGift(MessageGiftKind.Owned, 0, 0, 0, count, characterId, "", QuestRewardKind.RgbAll, 0, 0);
    }

    public static MessageGift Affection(string faceId, long amount)
    {
        return new MessageGift(MessageGiftKind.Affection, 0, 0, 0, amount, 0, faceId, QuestRewardKind.RgbAll, 0, 0);
    }

    public static MessageGift Reward(QuestRewardKind kind, long amount, int choice)
    {
        return new MessageGift(MessageGiftKind.QuestReward, 0, 0, 0, 0, 0, "", kind, amount, choice);
    }
}

public static class Cost
{
    public static MessageCost Rgb(long r, long g, long b)
    {
        return new MessageCost(MessagePayKind.Rgb, r, g, b, 0, 0);
    }

    public static MessageCost Pixel(int r, int g, int b, long count)
    {
        return new MessageCost(MessagePayKind.Pixel, r, g, b, count, 0);
    }

    public static MessageCost Owned(uint characterId, long count)
    {
        return new MessageCost(MessagePayKind.Owned, 0, 0, 0, count, characterId);
    }
}
