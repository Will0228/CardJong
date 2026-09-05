using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;
using R3;

namespace CardJong.InGame.Model
{
    /// <summary>
    /// 山札。208 枚のうち「配札 → ドラ表示札 → 生き山」の順に切り出し、残りは死に山として使わない。
    /// </summary>
    public sealed class WallModel : IDisposable
    {
        private readonly List<Card> _cards = new();
        private readonly List<Card> _doraIndicators = new();
        private readonly ReactiveProperty<int> _liveWallRemaining = new(0);

        /// <summary>次に配るカードの位置。</summary>
        private int _cursor;

        /// <summary>生き山の終端。ここ以降は死に山。</summary>
        private int _liveWallEnd;

        /// <summary>生き山の残り枚数。0 になると流局。</summary>
        public ReadOnlyReactiveProperty<int> LiveWallRemaining => _liveWallRemaining;

        /// <summary>ドラ表示札。</summary>
        public IReadOnlyList<Card> DoraIndicators => _doraIndicators;

        public bool IsLiveWallEmpty => _cursor >= _liveWallEnd;

        /// <summary>シャッフル済みの山札をセットする。局の開始時に呼ぶ。</summary>
        public void Reset(IReadOnlyList<Card> shuffledDeck)
        {
            if (shuffledDeck == null) throw new ArgumentNullException(nameof(shuffledDeck));

            _cards.Clear();
            _cards.AddRange(shuffledDeck);
            _doraIndicators.Clear();
            _cursor = 0;
            _liveWallEnd = 0;
            _liveWallRemaining.Value = 0;
        }

        /// <summary>配札用に山の上から count 枚を取り出す。</summary>
        public IReadOnlyList<Card> DealCards(int count)
        {
            EnsureAvailable(count);

            var dealt = _cards.GetRange(_cursor, count);
            _cursor += count;
            return dealt;
        }

        /// <summary>ドラ表示札を 1 枚めくる。</summary>
        public Card RevealDoraIndicator()
        {
            EnsureAvailable(1);

            var card = _cards[_cursor++];
            _doraIndicators.Add(card);
            return card;
        }

        /// <summary>
        /// 残りのカードから生き山を count 枚確保する。配札とドラ表示札の後に呼ぶ。
        /// </summary>
        public void SetLiveWall(int count)
        {
            _liveWallEnd = Math.Min(_cursor + count, _cards.Count);
            _liveWallRemaining.Value = _liveWallEnd - _cursor;
        }

        /// <summary>生き山から 1 枚ツモる。</summary>
        public Card Draw()
        {
            if (IsLiveWallEmpty) throw new InvalidOperationException("生き山が空です。");

            var card = _cards[_cursor++];
            _liveWallRemaining.Value = _liveWallEnd - _cursor;
            return card;
        }

        /// <summary>ドラかどうか。表示札の次のランクかつ同じ色の札がドラ。</summary>
        public bool IsDora(Card card)
        {
            for (var i = 0; i < _doraIndicators.Count; i++)
            {
                var indicator = _doraIndicators[i];
                if (indicator.Color == card.Color && NextRank(indicator.Rank) == card.Rank)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>ドラ表示札から見た次のランク。K の次は A に戻る。</summary>
        public static Rank NextRank(Rank rank) => rank == Rank.King ? Rank.Ace : rank + 1;

        private void EnsureAvailable(int count)
        {
            if (_cursor + count > _cards.Count)
            {
                throw new InvalidOperationException($"山札が足りません。要求 {count} 枚 / 残り {_cards.Count - _cursor} 枚");
            }
        }

        public void Dispose()
        {
            _liveWallRemaining.Dispose();
        }
    }
}
