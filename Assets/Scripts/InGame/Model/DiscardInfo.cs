using CardJong.InGame.Cards;

namespace CardJong.InGame.Model
{
    /// <summary>直前の捨て札の情報。ロン・ポン・チーの判定対象になる。</summary>
    /// <param name="Card">捨てられたカード。</param>
    /// <param name="Seat">捨てたプレイヤーの席。</param>
    public sealed record DiscardInfo(Card Card, int Seat)
    {
        public override string ToString() => $"seat{Seat} discards {Card}";
    }
}
