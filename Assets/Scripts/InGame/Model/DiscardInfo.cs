using CardJong.InGame.Cards;

namespace CardJong.InGame.Model
{
    /// <summary>直前の捨て札の情報。ロン・ポン・チーの判定対象になる。</summary>
    public readonly struct DiscardInfo
    {
        public Card Card { get; }

        /// <summary>捨てたプレイヤーの席。</summary>
        public int Seat { get; }

        public DiscardInfo(Card card, int seat)
        {
            Card = card;
            Seat = seat;
        }

        public override string ToString() => $"seat{Seat} discards {Card}";
    }
}
