namespace CardJong.Network.Matching
{
    /// <summary>カジュアルマッチの進み具合。UI はこれを見て出すものを決める。</summary>
    public enum CasualMatchPhase : byte
    {
        /// <summary>未設定。</summary>
        None = 0,

        /// <summary>何もしていない。マッチングを始めれば <see cref="Connecting"/> へ進む。</summary>
        Idle = 1,

        /// <summary>サーバーへ接続中。</summary>
        Connecting = 2,

        /// <summary>部屋を探している。</summary>
        Matching = 3,

        /// <summary>部屋で相手を待っている。ホストなら開始ボタンを押せる。</summary>
        WaitingInRoom = 4,

        /// <summary>開始処理中。部屋を閉じて席割りを配っている。</summary>
        Starting = 5,

        /// <summary>対局へ進んだ。</summary>
        Started = 6,
    }
}
