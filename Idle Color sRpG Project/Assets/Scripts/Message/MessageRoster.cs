using UnityEngine;

// トーク一覧のアイコンと表示名。ここを直接編集する。
// Listed が false の人は、グループの発言者にだけ使う。
// PersonId は既存の人。チグサはクラスメイト緑。グループは 0。
// 末尾の3つはアイコン背景の色。アイコンは丸く切り抜いて、この色で背景を塗る。
// アイコンは Assets/StreamingAssets/message/icon/ 。無ければ StreamingAssets/human/ も見る。
public static class MessageRoster
{
    public static readonly MessageFace[] All =
    {
        Face("chigusa", "チグサ", "chigusa_kaoaikon.png", true, PersonIds.ClassmateGreen, 128, 192, 128),
        Face("moa", "モア", "moa_kaoaikon.png", true, PersonIds.ClassmateRed, 255, 160, 192),
        Face("souga", "ソウガ", "souga_kaoaikon.png", false, PersonIds.ClassmateBlue, 64, 64, 192),
        Face("gensirou", "ゲンシロウ", "gensirou_kaoaikon.png", false, PersonIds.TeacherBlack, 96, 96, 96),
        Face("class", "クラス", "group_class.png", true, 0),
    };

    public static MessageFace Find(string id)
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

    public static MessageFace FindByPerson(int personId)
    {
        if (personId <= 0 || All == null)
            return null;
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i] != null && All[i].PersonId == personId)
                return All[i];
        }
        return null;
    }

    static MessageFace Face(string id, string name, string iconFile, bool listed, int personId)
    {
        return new MessageFace(id, name, iconFile, listed, personId);
    }

    static MessageFace Face(string id, string name, string iconFile, bool listed, int personId, int r, int g, int b)
    {
        return new MessageFace(id, name, iconFile, listed, personId, new Color(r / 255f, g / 255f, b / 255f, 1f));
    }
}
