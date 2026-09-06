using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation.Tiles;
using R3;
using UnityEngine;
using VContainer;

namespace CardJong.InGame.Presentation.Table
{
    /// <summary>
    /// 卓の 3D 表示。モデルの手牌・河・鳴き・ドラ表示札を実際の牌として並べる。
    /// </summary>
    /// <remarks>
    /// 牌は席ごとに使い回す。局をまたぐたびに Instantiate し直すと 200 個近い生成が走るため、
    /// 一度作ったものは非表示にして残し、必要な枚数だけ表示する。
    /// </remarks>
    [AddComponentMenu("CardJong/Mahjong Table View")]
    public sealed class MahjongTableView : MonoBehaviour
    {
        /// <summary>1 席ぶんの牌置き場。</summary>
        private sealed class SeatTiles
        {
            public SeatTiles(int seat, int slot, Transform root)
            {
                Seat = seat;
                Slot = slot;
                Root = root;
            }

            public int Seat { get; }

            /// <summary>卓のどの位置に座っているか。0 が手前。</summary>
            public int Slot { get; }

            public Transform Root { get; }

            public List<MahjongTileView> Hand { get; } = new();

            public List<MahjongTileView> Discards { get; } = new();

            public List<MahjongTileView> Melds { get; } = new();
        }

        [SerializeField]
        [Tooltip("並べる牌のプレハブ。")]
        private MahjongTileView _tilePrefab;

        private readonly TableLayout _layout = TableLayout.Standard;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<SeatTiles> _seats = new();
        private readonly List<MahjongTileView> _doraTiles = new();

        private InGameModel _model;
        private InGameSettings _settings;
        private SeatTiles _humanSeat;

        [Inject]
        public void Construct(InGameModel model, InGameSettings settings)
        {
            _model = model;
            _settings = settings;
        }

        /// <summary>席が確定したあとに呼ぶ。ここで席ぶんの置き場を作って購読を始める。</summary>
        public void Initialize()
        {
            BuildSeats();
            SubscribeToModel();
            RefreshAll();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void BuildSeats()
        {
            for (var i = 0; i < _seats.Count; i++)
            {
                Destroy(_seats[i].Root.gameObject);
            }

            _seats.Clear();
            _humanSeat = null;

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                var root = new GameObject($"Seat{seat}").transform;
                root.SetParent(transform, false);

                var slot = TableLayout.SlotOf(seat, _settings.HumanSeat, _model.PlayerCount);
                var seatTiles = new SeatTiles(seat, slot, root);
                _seats.Add(seatTiles);

                if (seat == _settings.HumanSeat) _humanSeat = seatTiles;
            }
        }

        private void SubscribeToModel()
        {
            // ドラ表示札は生き山を確保する直前にめくられるので、残り枚数の変化で拾える。
            _subscriptions.Add(_model.Wall.LiveWallRemaining.Subscribe(_ => RefreshDora()));

            for (var i = 0; i < _seats.Count; i++)
            {
                var seat = _seats[i];
                _subscriptions.Add(_model.GetPlayer(seat.Seat).Cards.OnChanged.Subscribe(_ => RefreshSeat(seat)));
            }
        }

        private void RefreshAll()
        {
            for (var i = 0; i < _seats.Count; i++)
            {
                RefreshSeat(_seats[i]);
            }

            RefreshDora();
        }

        private void RefreshSeat(SeatTiles seat)
        {
            var cards = _model.GetPlayer(seat.Seat).Cards;
            RefreshHand(seat, cards.ConcealedCards);
            RefreshDiscards(seat, cards.Discards);
            RefreshMelds(seat, cards.Melds);
        }

        /// <summary>
        /// 他家の手牌を立てて並べる。牌の面は座っている側を向くので、卓の外からは背しか見えない。
        /// </summary>
        /// <remarks>自分の手牌は画面下の UI で見せるため、ここには並べない。</remarks>
        private void RefreshHand(SeatTiles seat, IReadOnlyList<Card> cards)
        {
            if (seat == _humanSeat) return;

            EnsureTiles(seat.Hand, seat.Root, cards.Count);

            for (var i = 0; i < cards.Count; i++)
            {
                Place(seat.Hand[i], _layout.HandTile(seat.Slot, i, cards.Count));
            }
        }

        private void RefreshDiscards(SeatTiles seat, IReadOnlyList<Card> discards)
        {
            EnsureTiles(seat.Discards, seat.Root, discards.Count);

            for (var i = 0; i < discards.Count; i++)
            {
                var tile = seat.Discards[i];
                tile.SetCard(discards[i]);
                tile.SetDora(_model.Wall.IsDora(discards[i]));
                Place(tile, _layout.DiscardTile(seat.Slot, i));
            }
        }

        private void RefreshMelds(SeatTiles seat, IReadOnlyList<Meld> melds)
        {
            var total = 0;
            for (var i = 0; i < melds.Count; i++)
            {
                total += melds[i].Cards.Count;
            }

            EnsureTiles(seat.Melds, seat.Root, total);

            var placed = 0;
            for (var meldIndex = 0; meldIndex < melds.Count; meldIndex++)
            {
                var cards = melds[meldIndex].Cards;
                for (var i = 0; i < cards.Count; i++)
                {
                    var tile = seat.Melds[placed];
                    tile.SetCard(cards[i]);
                    tile.SetDora(_model.Wall.IsDora(cards[i]));
                    Place(tile, _layout.MeldTile(seat.Slot, meldIndex, placed));
                    placed++;
                }
            }
        }

        private void RefreshDora()
        {
            var indicators = _model.Wall.DoraIndicators;
            EnsureTiles(_doraTiles, transform, indicators.Count);

            for (var i = 0; i < indicators.Count; i++)
            {
                _doraTiles[i].SetCard(indicators[i]);
                Place(_doraTiles[i], _layout.DoraTile(i, indicators.Count));
            }
        }

        private void EnsureTiles(List<MahjongTileView> tiles, Transform parent, int count)
        {
            while (tiles.Count < count)
            {
                tiles.Add(Instantiate(_tilePrefab, parent));
            }

            for (var i = 0; i < tiles.Count; i++)
            {
                tiles[i].gameObject.SetActive(i < count);
            }
        }

        private static void Place(MahjongTileView tile, Pose pose)
        {
            tile.transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
