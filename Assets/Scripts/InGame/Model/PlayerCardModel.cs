using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;
using R3;

namespace CardJong.InGame.Model
{
    /// <summary>
    /// プレイヤー 1 人分のカード。手札・鳴き・捨て札をまとめて持つ。
    /// 状態を書き換えるのは Command 層だけにする。
    /// </summary>
    public sealed class PlayerCardModel : IDisposable
    {
        private readonly List<Card> _concealedCards = new();
        private readonly List<Meld> _melds = new();
        private readonly List<Card> _discards = new();
        private readonly Subject<Unit> _onChanged = new();

        /// <summary>手札（伏せている札）。ツモした札もここに含まれる。</summary>
        public IReadOnlyList<Card> ConcealedCards => _concealedCards;

        /// <summary>鳴いて公開した組。</summary>
        public IReadOnlyList<Meld> Melds => _melds;

        /// <summary>捨て札（河）。</summary>
        public IReadOnlyList<Card> Discards => _discards;

        /// <summary>手札・鳴き・捨て札のいずれかが変化したときに発火する。</summary>
        public Observable<Unit> OnChanged => _onChanged;

        /// <summary>直近にツモした札。ツモ切りの既定操作に使う。無い場合は null。</summary>
        public Card LastDrawnCard { get; private set; }

        /// <summary>門前（1 度も鳴いていない）かどうか。</summary>
        public bool IsMenzen => _melds.Count == 0;

        /// <summary>局の開始時にリセットする。</summary>
        public void ResetForNewRound()
        {
            _concealedCards.Clear();
            _melds.Clear();
            _discards.Clear();
            LastDrawnCard = null;
            _onChanged.OnNext(Unit.Default);
        }

        public void DealCards(IEnumerable<Card> cards)
        {
            _concealedCards.AddRange(cards);
            SortHand();
            _onChanged.OnNext(Unit.Default);
        }

        public void Draw(Card card)
        {
            _concealedCards.Add(card);
            LastDrawnCard = card;
            _onChanged.OnNext(Unit.Default);
        }

        /// <summary>手札から 1 枚取り除く。見つからなければ false。</summary>
        public bool RemoveFromHand(Card card)
        {
            var index = _concealedCards.IndexOf(card);
            if (index < 0) return false;

            _concealedCards.RemoveAt(index);
            if (LastDrawnCard == card) LastDrawnCard = null;
            _onChanged.OnNext(Unit.Default);
            return true;
        }

        public void AddDiscard(Card card)
        {
            _discards.Add(card);
            _onChanged.OnNext(Unit.Default);
        }

        /// <summary>直前の捨て札を河から取り除く。鳴かれたときに呼ぶ。</summary>
        public bool RemoveLastDiscard()
        {
            if (_discards.Count == 0) return false;

            _discards.RemoveAt(_discards.Count - 1);
            _onChanged.OnNext(Unit.Default);
            return true;
        }

        public void AddMeld(Meld meld)
        {
            _melds.Add(meld);
            LastDrawnCard = null;
            _onChanged.OnNext(Unit.Default);
        }

        /// <summary>手札に含まれる、指定カードと同じ札の枚数。ポンの成立判定に使う。</summary>
        public int CountSameCardsInHand(Card card)
        {
            var count = 0;
            for (var i = 0; i < _concealedCards.Count; i++)
            {
                if (_concealedCards[i] == card) count++;
            }

            return count;
        }

        /// <summary>マーク順・ランク順に手札を並べ替える。</summary>
        public void SortHand()
        {
            _concealedCards.Sort(static (a, b) =>
            {
                var suit = a.Suit.CompareTo(b.Suit);
                return suit != 0 ? suit : a.Rank.CompareTo(b.Rank);
            });
        }

        public void Dispose()
        {
            _onChanged.Dispose();
        }
    }
}
