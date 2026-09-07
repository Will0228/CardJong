using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using R3;
using VContainer;

namespace CardJong.InGame.Presentation.Table
{
    /// <summary>
    /// 卓の 3D 表示の受け持ち。手牌の枚数・河・鳴き・卓上のドラ表示札を、
    /// モデルが変わるたびに <see cref="MahjongTableView"/> へ流す。
    /// </summary>
    /// <remarks>
    /// 席（seat）と卓の位置（slot）の対応を持つのはここ。「自分が手前」に見えるよう
    /// 並べ替えるのは表示の都合であって、Model にも View にも関係が無いため。
    ///
    /// 自分の手牌だけは卓に並べず、画面下の UI で見せる（<see cref="Hud.HandPresenter"/>）。
    /// 表示に要る値はすべて <see cref="InGameModel"/> から引けるので、卓専用の Model は置いていない。
    /// </remarks>
    public sealed class TablePresenter : ITablePresenter, IDisposable
    {
        private readonly InGameModel _model;
        private readonly InGameSettings _settings;
        private readonly MahjongTableView _view;

        private readonly CompositeDisposable _subscriptions = new();

        // 並べ直すたびに List を作らずに済むよう、View へ渡す入れ物は使い回す。
        private readonly List<TableTile> _tiles = new();
        private readonly List<int> _meldSizes = new();

        private int HumanSeat => _settings.HumanSeat;

        [Inject]
        public TablePresenter(InGameModel model, InGameSettings settings, MahjongTableView view)
        {
            _model = model;
            _settings = settings;
            _view = view;
        }

        public void Initialize()
        {
            _view.Initialize(_model.PlayerCount);

            Bind();
            RefreshAll();
        }

        public void Dispose() => _subscriptions.Dispose();

        private void Bind()
        {
            // ドラ表示札は生き山を確保する直前にめくられるので、残り枚数が動いた時点で
            // めくられたことも拾える。
            _model.Wall.LiveWallRemaining.Subscribe(_ => RefreshDora()).AddTo(_subscriptions);

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                var player = _model.GetPlayer(seat);
                player.Cards.OnChanged.Subscribe(_ => RefreshSeat(player.Seat)).AddTo(_subscriptions);
            }
        }

        private void RefreshAll()
        {
            RefreshDora();

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                RefreshSeat(seat);
            }
        }

        private void RefreshDora() => _view.SetDoraIndicators(_model.Wall.DoraIndicators);

        private void RefreshSeat(int seat)
        {
            var slot = SlotOf(seat);
            var cards = _model.GetPlayer(seat).Cards;

            // 自分の手牌は画面下の UI で見せるので、卓には並べない。
            _view.SetHand(slot, seat == HumanSeat ? 0 : cards.ConcealedCards.Count);

            RefreshDiscards(slot, cards.Discards);
            RefreshMelds(slot, cards.Melds);
        }

        private void RefreshDiscards(int slot, IReadOnlyList<Card> discards)
        {
            _tiles.Clear();
            for (var i = 0; i < discards.Count; i++)
            {
                _tiles.Add(new TableTile(discards[i], _model.Wall.IsDora(discards[i])));
            }

            _view.SetDiscards(slot, _tiles);
        }

        private void RefreshMelds(int slot, IReadOnlyList<Meld> melds)
        {
            _tiles.Clear();
            _meldSizes.Clear();

            for (var i = 0; i < melds.Count; i++)
            {
                var cards = melds[i].Cards;
                _meldSizes.Add(cards.Count);

                for (var j = 0; j < cards.Count; j++)
                {
                    _tiles.Add(new TableTile(cards[j], _model.Wall.IsDora(cards[j])));
                }
            }

            _view.SetMelds(slot, _tiles, _meldSizes);
        }

        private int SlotOf(int seat) => TableLayout.SlotOf(seat, HumanSeat, _model.PlayerCount);
    }
}
