namespace CardJong.Network.Matching
{
    /// <summary>切断の理由。再接続を試みるかどうかの判断に使う。</summary>
    public enum MatchDisconnectReason : byte
    {
        /// <summary>未設定。</summary>
        None = 0,

        /// <summary>自分から切断した。再接続しない。</summary>
        ByRequest = 1,

        /// <summary>通信が途切れた。再接続を試みてよい。</summary>
        ConnectionLost = 2,

        /// <summary>サーバー側の都合で切られた。時間を置いてから試す。</summary>
        ServerError = 3,
    }
}
