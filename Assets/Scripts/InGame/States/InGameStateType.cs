namespace CardJong.InGame.States
{
    /// <summary>
    /// インゲームのステート。
    /// </summary>
    /// <remarks>
    /// 遷移は次の通り。
    /// <code>
    /// GameStart -> DecideDealer -> RoundStart -> Draw
    ///
    /// [行動パターン]
    ///   Draw -> PlayerAction --(打牌)--> ClaimWait
    ///                        --(ツモ)--> Win
    ///        -- 生き山が空 --> RoundEnd（流局）
    ///
    /// [待機パターン]
    ///   ClaimWait --(パス)------> Draw（次のプレイヤーへ）
    ///             --(ポン/チー)-> PlayerAction（鳴いた人がツモ無しで打牌）
    ///             --(ロン)------> Win
    ///
    /// Win -> RoundEnd -> RoundStart（次局 / 連荘） または GameEnd
    /// </code>
    /// </remarks>
    public enum InGameStateType : byte
    {
        /// <summary>未設定。ステートマシンを回し始める前の値。</summary>
        None = 0,

        /// <summary>ゲーム開始。</summary>
        GameStart = 1,

        /// <summary>親決定。</summary>
        DecideDealer = 2,

        /// <summary>局の開始。配牌とドラめくり。</summary>
        RoundStart = 3,

        /// <summary>カードを引く（行動パターン）。</summary>
        Draw = 4,

        /// <summary>思考時間。打牌かツモ上がりを選ぶ（行動パターン）。</summary>
        PlayerAction = 5,

        /// <summary>待機。他家の捨て札にロン・ポン・チーできる（待機パターン）。</summary>
        ClaimWait = 6,

        /// <summary>誰かが上がったときの演出画面。</summary>
        Win = 7,

        /// <summary>局の終了。点数移動と連荘判定。</summary>
        RoundEnd = 8,

        /// <summary>ゲーム終了画面。</summary>
        GameEnd = 9,
    }
}
