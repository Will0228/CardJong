using CardJong.InGame.Cards;

namespace CardJong.InGame.Actions
{
    /// <summary>自分の手番でできる行動。</summary>
    public enum TurnActionType : byte
    {
        /// <summary>カードを 1 枚捨てる。</summary>
        Discard = 0,

        /// <summary>ツモ上がり。</summary>
        Tsumo = 1,

        /// <summary>リーチを宣言して 1 枚捨てる。</summary>
        Riichi = 2,
    }

    /// <summary>
    /// 思考時間の結果としてプレイヤーが選んだ行動。
    /// これを State が Command に変換して実行する。
    /// </summary>
    public readonly struct TurnAction
    {
        public TurnActionType Type { get; }

        /// <summary>捨てるカード。<see cref="TurnActionType.Tsumo"/> のときは未使用。</summary>
        public Card Card { get; }

        private TurnAction(TurnActionType type, Card card)
        {
            Type = type;
            Card = card;
        }

        public static TurnAction Discard(Card card) => new(TurnActionType.Discard, card);

        public static TurnAction Tsumo() => new(TurnActionType.Tsumo, default);

        public static TurnAction Riichi(Card card) => new(TurnActionType.Riichi, card);

        public override string ToString()
            => Type == TurnActionType.Tsumo ? "Tsumo" : $"{Type}({Card})";
    }
}
