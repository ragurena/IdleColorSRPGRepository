// 💡 static をつけることで、ゲーム内に1つだけの「裏方設定クラス」になります
public static class GameConfig
{
    // 💡 階調数の定数（ゲーム全体からいつでも書き換え不可の固定値）
    public const int GRADATION_LEVELS = 8;

    // セーブの版。減色の仕方を変えたら DataVersion と ImageDataVersion を同じ値に上げる。
    // 0.0.2 は GraySlime を ID 4 に挿入した版。これより古いセーブは ID 4 以降を 1 つずらす。
    // 0.0.3 は ID 7, 8 に黒猫、ID 9 に灰猫を入れた版。これより古いセーブは ID 9 以降を 1 つずらす。
    // 0.0.4 は初期所持を ID 1〜3 だけにした版。これより古いセーブは、それ以外の初期所持を外す。
    // 0.0.5 は 255 以上の色を 255 のまま残す減色。これより古いセーブは減色画像を作り直す。
    // 0.0.6 はキャラのレベルを 1 始まりにした。これより古いセーブはレベルを 1 足す。
    public const string DataVersion = "0.0.6";
    public const string ImageDataVersion = "0.0.5";

    // ファイルの手順。DataVersion とは別。2 はチェックサムと SaveEnd がある。
    public const int SaveFormat = 2;

    // 本番の AdMob 報酬型広告ユニット ID。空のままの Release ビルドでは広告を読まない。
    // 値は AdMob の広告ユニット画面のものを入れる。開発用のサンプル ID はここへ書かない。
    public const string AndroidRewardedAdUnitId = "ca-app-pub-6699312150986616/5586223306";
    public const string IosRewardedAdUnitId = "";

    // もし今後、他の設定（最大レベル、制限時間など）が増えたらここに並べていきます
    // public const int MAX_LEVEL = 99;
}

// ピクセル枠とキャラ枠の開放数。広告で増やせる回数は、初期値にクエスト報酬を足す。ピクセルは1件+0.5、キャラは1件+0.3。
public sealed class ProductionSlotState
{
    public int PixelSlots = Constants.PRODUCTION_SLOT_INITIAL;
    public int CharacterSlots = Constants.PRODUCTION_SLOT_INITIAL;
    public int AdPixelSlots;
    public int AdCharacterSlots;
    public int ClaimedQuests;
    public int ClaimedPixelAdQuests;
    public int ClaimedCharacterAdQuests;
    public bool IapPlus1;
    public bool IapPlus3;
    public bool IapPlus5;
    public bool IapPlus10;

    public void Reset()
    {
        PixelSlots = Constants.PRODUCTION_SLOT_INITIAL;
        CharacterSlots = Constants.PRODUCTION_SLOT_INITIAL;
        AdPixelSlots = 0;
        AdCharacterSlots = 0;
        ClaimedQuests = 0;
        ClaimedPixelAdQuests = 0;
        ClaimedCharacterAdQuests = 0;
        IapPlus1 = false;
        IapPlus3 = false;
        IapPlus5 = false;
        IapPlus10 = false;
    }

    public void Clamp()
    {
        PixelSlots = ClampCount(PixelSlots);
        CharacterSlots = ClampCount(CharacterSlots);
        if (AdPixelSlots < 0)
            AdPixelSlots = 0;
        if (AdCharacterSlots < 0)
            AdCharacterSlots = 0;
        if (AdPixelSlots > Constants.PRODUCTION_SLOT_MAX)
            AdPixelSlots = Constants.PRODUCTION_SLOT_MAX;
        if (AdCharacterSlots > Constants.PRODUCTION_SLOT_MAX)
            AdCharacterSlots = Constants.PRODUCTION_SLOT_MAX;
        if (ClaimedQuests < 0)
            ClaimedQuests = 0;
        if (ClaimedPixelAdQuests < 0)
            ClaimedPixelAdQuests = 0;
        if (ClaimedCharacterAdQuests < 0)
            ClaimedCharacterAdQuests = 0;
    }

    public static int ClampCount(int value)
    {
        if (value < Constants.PRODUCTION_SLOT_INITIAL)
            return Constants.PRODUCTION_SLOT_INITIAL;
        if (value > Constants.PRODUCTION_SLOT_MAX)
            return Constants.PRODUCTION_SLOT_MAX;
        return value;
    }
}
