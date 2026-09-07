using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using R3;
using VContainer;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// 自分の手牌の受け持ち。モデルの手牌が変わるたびに <see cref="HandUiView"/> を並べ直し、
    /// 押された牌を外へ流す。
    /// </summary>
    /// <remarks>
    /// 手牌だけで完結する話（ドラかどうか、ツモ牌を離して見せるか）はここに閉じてある。
    /// <see cref="InGamePresenter"/> は手牌そのものを知らず、この窓口ごしに
    /// 「選べるようにするか」と「どの牌が選ばれたか」だけを扱う。
    ///
    /// 表示に必要な値はすべて <see cref="InGameModel"/> から引けるので、
    /// この画面のためだけの Model は置いていない。
    /// </remarks>
    public sealed class HandPresenter : IHandPresenter, IDisposable
    {
        private readonly InGameModel _model;
        private readonly InGameSettings _settings;
        private readonly HandUiView _view;

        private readonly Subject<Card> _tileSelected = new();
        private readonly CompositeDisposable _subscriptions = new();

        // 並べ直すたびに List を作らずに済むよう、View へ渡す入れ物は使い回す。
        private readonly List<HandTile> _tiles = new();

        private int HumanSeat => _settings.HumanSeat;

        public Observable<Card> TileSelected => _tileSelected;

        [Inject]
        public HandPresenter(InGameModel model, InGameSettings settings, HandUiView view)
        {
            _model = model;
            _settings = settings;
            _view = view;
        }

        public void Initialize()
        {
            // 人間が座っていない構成では画面下に出すものが無いので、購読ごと省く。
            if (HumanSeat < 0) return;

            Bind();
            Refresh();
        }

        public void SetSelectable(bool value) => _view.SetInteractable(value);

        public void Dispose()
        {
            if (_view != null) _view.TileClicked -= OnTileClicked;

            _subscriptions.Dispose();
            _tileSelected.Dispose();
        }

        private void Bind()
        {
            _view.TileClicked += OnTileClicked;
            _model.GetPlayer(HumanSeat).Cards.OnChanged.Subscribe(_ => Refresh()).AddTo(_subscriptions);
        }

        private void Refresh()
        {
            var cards = _model.GetPlayer(HumanSeat).Cards;
            var concealed = cards.ConcealedCards;

            _tiles.Clear();
            for (var i = 0; i < concealed.Count; i++)
            {
                _tiles.Add(new HandTile(concealed[i], _model.Wall.IsDora(concealed[i])));
            }

            // ツモ牌は並べ替えずに末尾へ足されるので、最後の 1 枚だけ離して見せる。
            var hasDrawnTile = cards.LastDrawnCard != null && concealed.Count > 1;
            _view.Refresh(_tiles, hasDrawnTile);
        }

        private void OnTileClicked(Card card) => _tileSelected.OnNext(card);
    }
}
