namespace CardJong.Network.Matching
{
    /// <summary>マッチングサーバーとの接続状態。</summary>
    public enum MatchConnectionState : byte
    {
        /// <summary>未設定。まだ一度も接続を試みていない。</summary>
        None = 0,

        /// <summary>切断されている。接続し直せば使える。</summary>
        Disconnected = 1,

        /// <summary>接続処理中。</summary>
        Connecting = 2,

        /// <summary>接続済み。部屋にはまだ入っていない。</summary>
        Connected = 3,

        /// <summary>部屋を探している、または入室処理中。</summary>
        JoiningRoom = 4,

        /// <summary>部屋に入っている。</summary>
        InRoom = 5,
    }
}
