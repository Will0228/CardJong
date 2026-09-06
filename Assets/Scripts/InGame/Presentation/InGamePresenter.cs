using System;
using System.Collections.Generic;
using System.Threading;
using CardJong.InGame.Actions;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation.Hud;
using CardJong.InGame.Presentation.Table;
using CardJong.InGame.Rules;
using Cysharp.Threading.Tasks;
using R3;
using VContainer;

namespace CardJong.InGame.Presentation
{
    /// <summary>
    /// インゲームの画面まわりの取りまとめ。State から呼ばれ、Model の変化を View へ流し、
    /// View で起きた操作を <see cref="IPlayerInputPort"/> へ返す。
    /// </summary>
    /// <remarks>
    /// View は何を出しているのかを知らず、渡された値を並べるだけにしてある。Model を読むのも、
    /// 文字列へ整形するのも、リーチ予約のように画面の側だけが持つ状態を覚えておくのもここ。
    ///
    /// 席（seat）と卓の位置（slot）の対応を持つのもこの層。「自分が手前」に見えるよう
    /// 並べ替えるのは表示の都合であって、Model にも View にも関係が無いため。
    /// </remarks>
    public sealed class InGamePresenter : IInGamePresentation, IDisposable
    {
        private readonly InGameModel _model;
        private readonly InGameSettings _settings;
        private readonly IPlayerInputPort _inputPort;
        private readonly InGameHudView _hudView;
        private readonly MahjongTableView _tableView;

        private readonly CompositeDisposable _subscriptions = new();

        // 表示を更新するたびに List を作らずに済むよう、View へ渡す入れ物は使い回す。
        private readonly List<TableTile> _tiles = new();
        private readonly List<int> _meldSizes = new();
        private readonly List<HandTile> _handTiles = new();
        private readonly List<SeatPlateState> _seatPlates = new();
        private readonly List<ActionButtonSpec> _actionButtons = new();

        /// <summary>リーチを予約しているか。宣言と打牌が一体なので、切る牌を選ぶまで覚えておく。</summary>
        private bool _riichiArmed;

        private int HumanSeat => _settings.HumanSeat;

        [Inject]
        public InGamePresenter(
            InGameModel model,
            InGameSettings settings,
            IPlayerInputPort inputPort,
            InGameHudView hudView,
            MahjongTableView tableView)
        {
            _model = model;
            _settings = settings;
            _inputPort = inputPort;
            _hudView = hudView;
            _tableView = tableView;

            _hudView.TileClicked += OnHandTileClicked;
            _subscriptions.Add(_inputPort.OnTurnDecisionRequested.Subscribe(OnTurnDecisionRequested));
            _subscriptions.Add(_inputPort.OnClaimDecisionRequested.Subscribe(OnClaimDecisionRequested));
            _subscriptions.Add(_inputPort.OnDecisionClosed.Subscribe(_ => CloseDecision()));
        }

        public UniTask ShowGameStartAsync(CancellationToken cancellationToken)
        {
            // ここまでにモデルの初期化が済んでいるので、席の数が決まったこの時点で表示を組む。
            _tableView.Initialize(_model.PlayerCount);
            _hudView.BuildSeatPlates(_model.PlayerCount);

            SubscribeToModel();
            RefreshAll();

            return ShowOverlayAsync(InGameMessages.GameStart, _settings.NoticeSeconds, cancellationToken);
        }

        public UniTask ShowDealerDecisionAsync(int dealerSeat, CancellationToken cancellationToken)
            => ShowOverlayAsync(InGameMessages.DealerDecision(dealerSeat), _settings.NoticeSeconds, cancellationToken);

        public UniTask ShowRoundStartAsync(int roundNumber, int honba, CancellationToken cancellationToken)
            => ShowOverlayAsync(
                InGameMessages.Round(roundNumber, honba, _model.PlayerCount),
                _settings.NoticeSeconds,
                cancellationToken);

        public UniTask ShowWinAsync(WinResult win, CancellationToken cancellationToken)
            => ShowOverlayAsync(InGameMessages.Win(win), _settings.ResultSeconds, cancellationToken);

        public UniTask ShowRoundResultAsync(RoundResult result, CancellationToken cancellationToken)
            => ShowOverlayAsync(InGameMessages.RoundResult(result), _settings.ResultSeconds, cancellationToken);

        public UniTask ShowGameResultAsync(GameResult result, CancellationToken cancellationToken)
            => ShowOverlayAsync(InGameMessages.GameResult(result), _settings.ResultSeconds * 2f, cancellationToken);

        public void Dispose()
        {
            if (_hudView != null) _hudView.TileClicked -= OnHandTileClicked;

            _subscriptions.Dispose();
        }

        // ---- モデルの購読 ----

        private void SubscribeToModel()
        {
            _subscriptions.Add(_model.RoundNumber.Subscribe(_ => RefreshInfo()));
            _subscriptions.Add(_model.Honba.Subscribe(_ => RefreshInfo()));

            // ドラ表示札は生き山を確保する直前にめくられるので、残り枚数が動いた時点で
            // めくられたことも拾える。
            _subscriptions.Add(_model.Wall.LiveWallRemaining.Subscribe(_ =>
            {
                RefreshInfo();
                RefreshDora();
            }));

            _subscriptions.Add(_model.CurrentSeat.Subscribe(_ => RefreshSeatPlates()));
            _subscriptions.Add(_model.DealerSeat.Subscribe(_ => RefreshSeatPlates()));

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                var player = _model.GetPlayer(seat);
                _subscriptions.Add(player.Score.Points.Subscribe(_ => RefreshSeatPlates()));
                _subscriptions.Add(player.Cards.OnChanged.Subscribe(_ => RefreshSeat(player.Seat)));
            }
        }

        private void RefreshAll()
        {
            RefreshInfo();
            RefreshDora();
            RefreshSeatPlates();

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                RefreshTable(seat);
            }

            RefreshHand();
        }

        /// <summary>その席の持ち物が変わったときの更新。リーチ宣言も名札に出るので名札ごと直す。</summary>
        private void RefreshSeat(int seat)
        {
            RefreshTable(seat);
            RefreshSeatPlates();

            if (seat == HumanSeat) RefreshHand();
        }

        private void RefreshInfo()
            => _hudView.SetRoundInfo(
                InGameMessages.Round(_model.RoundNumber.CurrentValue, _model.Honba.CurrentValue, _model.PlayerCount),
                InGameMessages.WallRemaining(_model.Wall.LiveWallRemaining.CurrentValue));

        private void RefreshDora()
        {
            _tableView.SetDoraIndicators(_model.Wall.DoraIndicators);
            _hudView.SetDoraIndicators(_model.Wall.DoraIndicators);
        }

        private void RefreshSeatPlates()
        {
            var dealerSeat = _model.DealerSeat.CurrentValue;
            var currentSeat = _model.CurrentSeat.CurrentValue;

            // 名札は卓のどこに座って見えるかに合わせて置いてあるので、位置の順で渡す。
            _seatPlates.Clear();

            for (var slot = 0; slot < _model.PlayerCount; slot++)
            {
                var seat = SeatOfSlot(slot);
                var player = _model.GetPlayer(seat);
                var isRiichi = player.Status.IsRiichi;

                _seatPlates.Add(new SeatPlateState(
                    InGameMessages.SeatName(RelationOf(slot), seat, seat == dealerSeat, isRiichi),
                    InGameMessages.SeatScore(player.Score.Points.CurrentValue),
                    isRiichi,
                    seat == currentSeat));
            }

            _hudView.SetSeatPlates(_seatPlates);
        }

        private void RefreshTable(int seat)
        {
            var slot = SlotOf(seat);
            var cards = _model.GetPlayer(seat).Cards;

            // 自分の手牌は画面下の UI で見せるので、卓には並べない。
            _tableView.SetHand(slot, seat == HumanSeat ? 0 : cards.ConcealedCards.Count);

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

            _tableView.SetDiscards(slot, _tiles);
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

            _tableView.SetMelds(slot, _tiles, _meldSizes);
        }

        private void RefreshHand()
        {
            if (HumanSeat < 0) return;

            var cards = _model.GetPlayer(HumanSeat).Cards;
            var concealed = cards.ConcealedCards;

            _handTiles.Clear();
            for (var i = 0; i < concealed.Count; i++)
            {
                _handTiles.Add(new HandTile(concealed[i], _model.Wall.IsDora(concealed[i])));
            }

            // ツモ牌は並べ替えずに末尾へ足されるので、最後の 1 枚だけ離して見せる。
            var hasDrawnTile = cards.LastDrawnCard != null && concealed.Count > 1;
            _hudView.SetHand(_handTiles, hasDrawnTile);
        }

        // ---- 人間プレイヤーの入力 ----

        private void OnTurnDecisionRequested(TurnDecisionContext context)
        {
            _riichiArmed = false;
            ShowTurnActions(context);

            _hudView.SetHandInteractable(true);
            _hudView.ShowTimer(context.TimeLimitSeconds);
        }

        private void ShowTurnActions(TurnDecisionContext context)
        {
            _hudView.SetPrompt(_riichiArmed ? InGameMessages.RiichiPrompt : InGameMessages.DiscardPrompt);

            _actionButtons.Clear();

            if (context.CanDeclareTsumo)
            {
                _actionButtons.Add(new ActionButtonSpec(
                    InGameMessages.TsumoButton,
                    ActionButtonKind.Win,
                    () => _inputPort.SubmitTurnAction(TurnAction.Tsumo())));
            }

            if (context.CanDeclareRiichi)
            {
                _actionButtons.Add(new ActionButtonSpec(
                    InGameMessages.RiichiButton,
                    _riichiArmed ? ActionButtonKind.RiichiArmed : ActionButtonKind.Riichi,
                    () => ToggleRiichi(context)));
            }

            _hudView.ShowActions(_actionButtons);
        }

        /// <summary>リーチは宣言と打牌が一体なので、ボタンで予約してから捨てる牌を選ばせる。</summary>
        private void ToggleRiichi(TurnDecisionContext context)
        {
            _riichiArmed = !_riichiArmed;
            ShowTurnActions(context);
        }

        private void OnClaimDecisionRequested(ClaimDecisionContext context)
        {
            _hudView.SetPrompt(InGameMessages.DiscardAnnounce(context.Discard));

            _actionButtons.Clear();

            for (var i = 0; i < context.Options.Count; i++)
            {
                var option = context.Options[i];
                _actionButtons.Add(new ActionButtonSpec(
                    InGameMessages.ClaimButton(option),
                    option.Type == ClaimType.Ron ? ActionButtonKind.Win : ActionButtonKind.Meld,
                    () => _inputPort.SubmitClaim(ClaimDeclaration.From(context.Seat, option))));
            }

            _actionButtons.Add(new ActionButtonSpec(
                InGameMessages.PassButton,
                ActionButtonKind.Pass,
                () => _inputPort.SubmitClaim(ClaimDeclaration.Pass(context.Seat))));

            _hudView.ShowActions(_actionButtons);
            _hudView.ShowTimer(context.TimeLimitSeconds);
        }

        private void CloseDecision()
        {
            _riichiArmed = false;

            _hudView.ClearActions();
            _hudView.HideTimer();
            _hudView.SetPrompt(string.Empty);
            _hudView.SetHandInteractable(false);
        }

        private void OnHandTileClicked(Card card)
            => _inputPort.SubmitTurnAction(_riichiArmed ? TurnAction.Riichi(card) : TurnAction.Discard(card));

        // ---- 案内の表示 ----

        private async UniTask ShowOverlayAsync(string message, float seconds, CancellationToken cancellationToken)
        {
            _hudView.ShowOverlay(message);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: cancellationToken);
            }
            finally
            {
                _hudView.HideOverlay();
            }
        }

        // ---- 席と卓の位置の対応 ----

        private int SlotOf(int seat) => TableLayout.SlotOf(seat, HumanSeat, _model.PlayerCount);

        /// <summary>卓のその位置に座っているのは誰か。<see cref="SlotOf"/> の逆。</summary>
        private int SeatOfSlot(int slot) => HumanSeat < 0 ? slot : (HumanSeat + slot) % _model.PlayerCount;

        private string RelationOf(int slot)
        {
            if (HumanSeat < 0) return string.Empty;
            if (slot == 0) return "自分";
            if (slot == 1) return "下家";

            return slot == _model.PlayerCount - 1 ? "上家" : "対面";
        }
    }
}
