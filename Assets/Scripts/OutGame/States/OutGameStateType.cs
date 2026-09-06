namespace CardJong.OutGame.States
{
    /// <summary>
    /// アウトゲームのステート。
    /// </summary>
    /// <remarks>
    /// 遷移は次の通り。
    /// <code>
    /// Home --(ゲームスタート)--> ステートマシンの終了（この後インゲームのシーンへ移る）
    /// </code>
    /// ロビーや設定のような画面が増えたら、ここにステートを足して Home から遷移させる。
    /// </remarks>
    public enum OutGameStateType : byte
    {
        /// <summary>未設定。ステートマシンを回し始める前の値。</summary>
        None = 0,

        /// <summary>ホーム画面。</summary>
        Home = 1,
    }
}
