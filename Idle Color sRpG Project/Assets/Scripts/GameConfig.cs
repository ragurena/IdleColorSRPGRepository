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
    public const string DataVersion = "0.0.5";
    public const string ImageDataVersion = "0.0.5";

    // もし今後、他の設定（最大レベル、制限時間など）が増えたらここに並べていきます
    // public const int MAX_LEVEL = 99;
}
