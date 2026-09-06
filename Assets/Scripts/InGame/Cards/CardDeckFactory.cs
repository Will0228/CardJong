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
        /// マーク x ランクの 52 通りぶんの実体。Card は不変な参照型なので、4 デッキぶんに
        /// 同じ実体を使い回せる。208 個の Card を毎回 new せずに済むように、ここでまとめて作る。
        /// </summary>
        private static readonly Card[] UniqueCards = CreateUniqueCards();

        /// <summary>
        /// 208 枚ぶんのテンプレート。UniqueCards の参照を 4 デッキぶん並べたものなので、
        /// 実際に確保される Card は 52 個だけで済む。中身は局をまたいでも変わらないので、
        /// 局のたびに作り直さず、ここから複製する。
        /// </summary>
        private static readonly Card[] FullDeckTemplate = CreateFullDeckTemplate();

        /// <summary>
        /// 208 枚の山札を生成する（未シャッフル）。
        /// Card は不変なので、テンプレートの参照をそのまま写すだけでよい。
        /// </summary>
        public static List<Card> CreateFullDeck() => new(FullDeckTemplate);

        private static Card[] CreateUniqueCards()
        {
            var cards = new Card[CardsPerDeck];
            var index = 0;
            foreach (var suit in AllSuits)
            {
                for (var rank = (int)Rank.Ace; rank <= (int)Rank.King; rank++)
                {
                    cards[index++] = new Card(suit, (Rank)rank);
                }
            }

            return cards;
        }

        private static Card[] CreateFullDeckTemplate()
        {
            var cards = new Card[TotalCardCount];
            for (var deck = 0; deck < DeckCount; deck++)
            {
                Array.Copy(UniqueCards, 0, cards, deck * CardsPerDeck, CardsPerDeck);
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
