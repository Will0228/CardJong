using System;
using System.Collections.Generic;
using CardJong.Core;

namespace CardJong.InGame.Cards
{
    /// <summary>山札の生成とシャッフル。</summary>
    public static class CardDeckFactory
    {
        /// <summary>1 デッキあたりの枚数（ジョーカーは使用しない）。</summary>
        public const int CardsPerDeck = 52;

        /// <summary>使用するデッキ数。</summary>
        public const int DeckCount = 4;

        /// <summary>山札の総枚数（52 x 4 = 208）。</summary>
        public const int TotalCardCount = CardsPerDeck * DeckCount;

        /// <summary>
        /// 全マーク。Enum.GetValues は非ジェネリックな Array を返してボックス化を挟むので、
        /// 配列を持っておいて素直に列挙する。
        /// </summary>
        private static readonly Suit[] AllSuits = { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };

        /// <summary>
        /// 208 枚ぶんのテンプレート。中身は局をまたいでも変わらないので、
        /// 局のたびに 208 枚を作り直さず、ここから複製する。
        /// </summary>
        private static readonly Card[] FullDeckTemplate = CreateFullDeckTemplate();

        /// <summary>
        /// 208 枚の山札を生成する（未シャッフル）。
        /// Card は不変なので、テンプレートの参照をそのまま写すだけでよい。
        /// </summary>
        public static List<Card> CreateFullDeck() => new(FullDeckTemplate);

        private static Card[] CreateFullDeckTemplate()
        {
            var cards = new Card[TotalCardCount];
            var index = 0;
            for (var deck = 0; deck < DeckCount; deck++)
            {
                foreach (var suit in AllSuits)
                {
                    for (var rank = (int)Rank.Ace; rank <= (int)Rank.King; rank++)
                    {
                        cards[index++] = new Card(suit, (Rank)rank);
                    }
                }
            }

            return cards;
        }

        /// <summary>Fisher-Yates でその場シャッフルする。</summary>
        public static void Shuffle(IList<Card> cards, IRandomService random)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (random == null) throw new ArgumentNullException(nameof(random));

            for (var i = cards.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }
    }
}
