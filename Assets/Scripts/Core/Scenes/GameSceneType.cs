namespace CardJong.Core.Scenes
{
    /// <summary>
    /// 読み込む対象のシーン。
    /// </summary>
    /// <remarks>
    /// 遷移は次の通り。
    /// <code>
    /// OutGame --(ゲームスタート)--> InGame --(対局の結果表示まで終わる)--> OutGame
    /// </code>
    /// </remarks>
    public enum GameSceneType : byte
    {
        /// <summary>未設定。</summary>
        None = 0,

        /// <summary>アウトゲーム。ホーム画面。</summary>
        OutGame = 1,

        /// <summary>インゲーム。対局。</summary>
        InGame = 2,
    }
}
