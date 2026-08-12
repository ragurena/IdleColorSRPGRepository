// 💡 static をつけることで、ゲーム内に1つだけの「裏方設定クラス」になります
public static class GameConfig
{
    // 💡 階調数の定数（ゲーム全体からいつでも書き換え不可の固定値）
    public const int GRADATION_LEVELS = 8;

    // もし今後、他の設定（最大レベル、制限時間など）が増えたらここに並べていきます
    // public const int MAX_LEVEL = 99;
}
