using System.Collections.Generic;
using System.Text;
using CardJong.InGame.Cards;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>カードを画面に出す短い文字列にする。</summary>
    /// <remarks>
    /// <see cref="Card.ToString"/> はログ向けに ASCII だけで書いてあるので、
    /// 画面用のマーク記号はここで持つ。引数だけで決まる純粋な変換なので static で置く。
    /// </remarks>
    public static class CardLabel
    {
        public static string Of(Card card) => $"{SuitSymbol(card.Suit)}{Card.RankLabel(card.Rank)}";

        public static string Join(IReadOnlyList<Card> cards)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < cards.Count; i++)
            {
                if (i > 0) builder.Append(' ');
                builder.Append(Of(cards[i]));
            }

            return builder.ToString();
        }

        private static string SuitSymbol(Suit suit) => suit switch
        {
            Suit.Spade => "♠",
            Suit.Heart => "♥",
            Suit.Diamond => "♦",
            Suit.Club => "♣",
            _ => "?",
        };
    }
}
