using UnityEngine;

// ストーリーの中身。章とページを足すときは、この配列へ足す。
// 章IDとページIDは、一度付けたら変えない。ページIDは章をまたいで重ならないようにする。
// ページの並びは配列の順。ページ番号は章の中で重ならないようにする。
// 解放は、対象ステージの到達階が必要階層以上なら開く。ステージか階層が 0 のときは最初から開く。
// 背景とバナーのファイル名は、Assets/StreamingAssets/story に置く PNG。空なら色だけを使う。
// エディタでは Assets/Images/story も見る。
public static class StoryCatalog
{
    public static readonly Color CommonBackground = new Color(0.10f, 0.12f, 0.16f, 1f);

    public static readonly StoryChapterDef[] All =
    {
        new StoryChapterDef(
            1,
            "第1章",
            "",
            true,
            new Color(0.15f, 0.20f, 0.28f, 1f),
            0,
            0,
            new StoryPageDef[]
            {
                Page(101, 1, "朝の光が、机の上の絵を照らしていた。", 1, 10),
                Still(102, 2, "", 1, 10),
                Page(103, 3, "その絵は、まだ色を待っている。", 1, 10),
                Page(104, 4, "森の風が、ページをめくった。", 1, 20),
                Page(105, 5, "木の陰に、小さな影が立っていた。", 1, 20),
                Page(106, 6, "影はこちらを見て、何も言わなかった。", 1, 20),
                Page(107, 7, "道の先で、光が一つ増えていた。", 1, 30),
                Page(108, 8, "足を止めても、道はそのまま残っている。", 1, 30),
                Page(109, 9, "絵は、言葉より先に残ることがある。", 1, 30),
                Page(110, 10, "続きは、次に開く頁の向こうにある。", 1, 30),
            }),
    };

    public static StoryChapterDef Find(int chapterId)
    {
        if (All == null)
            return null;
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i] != null && All[i].Id == chapterId)
                return All[i];
        }
        return null;
    }

    public static bool ContainsPage(int pageId)
    {
        return FindPage(pageId) != null;
    }

    public static StoryPageDef FindPage(int pageId)
    {
        if (All == null || pageId <= 0)
            return null;
        for (int c = 0; c < All.Length; c++)
        {
            StoryChapterDef chapter = All[c];
            if (chapter == null || chapter.Pages == null)
                continue;
            for (int i = 0; i < chapter.Pages.Length; i++)
            {
                StoryPageDef page = chapter.Pages[i];
                if (page != null && page.Id == pageId)
                    return page;
            }
        }
        return null;
    }

    public static string UnlockLabel(int stage, int floor)
    {
        if (stage <= 0 || floor <= 0)
            return "";
        return ControllerBattleStageClass.GetStageName(stage) + " 第" + floor.ToString() + "階クリアで解放";
    }

    public static int IndexOfNumber(StoryChapterDef chapter, int number)
    {
        if (chapter == null || chapter.Pages == null || number <= 0)
            return -1;
        for (int i = 0; i < chapter.Pages.Length; i++)
        {
            StoryPageDef page = chapter.Pages[i];
            if (page != null && page.Number == number)
                return i;
        }
        return -1;
    }

    static StoryPageDef Page(int id, int number, string text, int stage, int floor)
    {
        return new StoryPageDef(id, number, "", text, false, stage, floor);
    }

    static StoryPageDef Still(int id, int number, string fileName, int stage, int floor)
    {
        return new StoryPageDef(id, number, fileName, "", true, stage, floor);
    }
}

public sealed class StoryChapterDef
{
    public readonly int Id;
    public readonly string Title;
    public readonly string BannerFile;
    public readonly bool HasBackground;
    public readonly Color Background;
    public readonly int UnlockStage;
    public readonly int UnlockFloor;
    public readonly StoryPageDef[] Pages;

    public StoryChapterDef(int id, string title, string bannerFile, bool hasBackground, Color background, int unlockStage, int unlockFloor, StoryPageDef[] pages)
    {
        Id = id;
        Title = string.IsNullOrEmpty(title) ? "章" : title;
        BannerFile = bannerFile ?? "";
        HasBackground = hasBackground;
        Background = background;
        UnlockStage = unlockStage;
        UnlockFloor = unlockFloor;
        Pages = pages ?? new StoryPageDef[0];
    }

    public Color BackgroundColor
    {
        get { return HasBackground ? Background : StoryCatalog.CommonBackground; }
    }
}

public sealed class StoryPageDef
{
    public readonly int Id;
    public readonly int Number;
    public readonly string BackgroundFile;
    public readonly string Text;
    public readonly bool Still;
    public readonly int UnlockStage;
    public readonly int UnlockFloor;

    public StoryPageDef(int id, int number, string backgroundFile, string text, bool still, int unlockStage, int unlockFloor)
    {
        Id = id;
        Number = number;
        BackgroundFile = backgroundFile ?? "";
        Text = text ?? "";
        Still = still;
        UnlockStage = unlockStage;
        UnlockFloor = unlockFloor;
    }
}
