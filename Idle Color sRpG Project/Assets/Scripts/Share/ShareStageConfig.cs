// アップロード先。空のあいだは、端末への保存だけできる。
// Cloudflare の手順は Docs/共有ステージ.md。デプロイした Worker の URL を、末尾の / なしで入れる。
public static class ShareStageConfig
{
    public const string WorkerBaseUrl = "";

    public static bool HasWorker
    {
        get { return !string.IsNullOrEmpty(WorkerBaseUrl); }
    }

    public static string FileUrl(string fileId)
    {
        return WorkerBaseUrl + "/v1/files/" + fileId;
    }
}
