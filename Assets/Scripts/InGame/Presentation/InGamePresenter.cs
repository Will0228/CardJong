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
    /// インゲームの画面まわりの取りまとめ。State から呼ばれて案内を出し、
    /// 人間プレイヤーに選ばせた結果を <see cref="IPlayerInputPort"/> へ返す。
    /// </summary>
    /// <remarks>
    /// 卓・HUD・手牌はそれぞれの Presenter が自分でモデルを見て並べ直すので、ここは
    /// View を 1 つも持たない。残るのは「今なにを選ばせるか」という進行の判断と、
    /// リーチ予約のように画面の側だけが覚えておく状態。
    ///
    /// <list type="bullet">
    /// <item><see cref="ITablePresenter"/> … 3D の卓（他家の手牌・河・鳴き・ドラ）</item>
    /// <item><see cref="IHudPresenter"/> … 局数・名札・宣言のボタン・案内</item>
    /// <item><see cref="IHandPresenter"/> … 画面下に並べる自分の手牌</item>
    /// </list>
    /// </remarks>
    public sealed class InGamePresenter : IInGamePresentation, IDisposable
    {
        private readonly InGameModel _model;
        private readonly InGameSettings _settings;
        private readonly IPlayerInputPort _inputPort;
        private readonly ITablePresenter _tablePresenter;
        private readonly IHudPresenter _hudPresenter;
        private readonly IHandPresenter _handPresenter;

        private readonly CompositeDisposable _subscriptions = new();

        // 出し直すたびに List を作らずに済むよう、渡す入れ物は使い回す。
        private readonly List<ActionButtonSpec> _actionButtons = new();

        /// <summary>リーチを予約しているか。宣言と打牌が一体なので、切る牌を選ぶまで覚えておく。</summary>
        private bool _riichiArmed;

        [Inject]
        public InGamePresenter(
            InGameModel model,
            InGameSettings settings,
            IPlayerInputPort inputPort,
            ITablePresenter tablePresenter,
            IHudPresenter hudPresenter,
            IHandPresenter handPresenter)
        {
            _model = model;
            _settings = settings;
            _inputPort = inputPort;
            _tablePresenter = tablePresenter;
            _hudPresenter = hudPresenter;
            _handPresenter = handPresenter;

            Bind();
        }

        public UniTask ShowGameStartAsync(CancellationToken cancellationToken)
        {
            // ここまでにモデルの初期化が済んでいるので、席の数が決まったこの時点で表示を組む。
            _tablePresenter.Initialize();
            _hudPresenter.Initialize();
            _handPresenter.Initialize();

            return _hudPresenter.ShowNoticeAsync(
                InGameMessages.GameStart,
                _settings.NoticeSeconds,
                cancellationToken);
        }

        public UniTask ShowDealerDecisionAsync(int dealerSeat, CancellationToken cancellationToken)
            => _hudPresenter.ShowNoticeAsync(
                InGameMessages.DealerDecision(dealerSeat),
                _settings.NoticeSeconds,
                cancellationToken);

        public UniTask ShowRoundStartAsync(int roundNumber, int honba, CancellationToken cancellationToken)
            => _hudPresenter.ShowNoticeAsync(
                InGameMessages.Round(roundNumber, honba, _model.PlayerCount),
                _settings.NoticeSeconds,
                cancellationToken);

        public UniTask ShowWinAsync(WinResult win, CancellationToken cancellationToken)
            => _hudPresenter.ShowNoticeAsync(InGameMessages.Win(win), _settings.ResultSeconds, cancellationToken);

        public UniTask ShowRoundResultAsync(RoundResult result, CancellationToken cancellationToken)
            => _hudPresenter.ShowNoticeAsync(
                InGameMessages.RoundResult(result),
                _settings.ResultSeconds,
                cancellationToken);

        public UniTask ShowGameResultAsync(GameResult result, CancellationToken cancellationToken)
            => _hudPresenter.ShowNoticeAsync(
                InGameMessages.GameResult(result),
                _settings.ResultSeconds * 2f,
                cancellationToken);

        public void Dispose() => _subscriptions.Dispose();

        private void Bind()
        {
            _handPresenter.TileSelected.Subscribe(OnHandTileSelected).AddTo(_subscriptions);
            _inputPort.OnTurnDecisionRequested.Subscribe(OnTurnDecisionRequested).AddTo(_subscriptions);
            _inputPort.OnClaimDecisionRequested.Subscribe(OnClaimDecisionRequested).AddTo(_subscriptions);
            _inputPort.OnDecisionClosed.Subscribe(_ => CloseDecision()).AddTo(_subscriptions);
        }

        // ---- 人間プレイヤーの入力 ----

        private void OnTurnDecisionRequested(TurnDecisionContext context)
        {
            _riichiArmed = false;
            ShowTurnActions(context);

            _handPresenter.SetSelectable(true);
            _hudPresenter.StartTimer(context.TimeLimitSeconds);
        }

        private void ShowTurnActions(TurnDecisionContext context)
        {
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

            _hudPresenter.ShowDecision(
                _riichiArmed ? InGameMessages.RiichiPrompt : InGameMessages.DiscardPrompt,
                _actionButtons);
        }

        /// <summary>リーチは宣言と打牌が一体なので、ボタンで予約してから捨てる牌を選ばせる。</summary>
        private void ToggleRiichi(TurnDecisionContext context)
        {
            _riichiArmed = !_riichiArmed;
            ShowTurnActions(context);
        }

        private void OnClaimDecisionRequested(ClaimDecisionContext context)
        {
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

            _hudPresenter.ShowDecision(InGameMessages.DiscardAnnounce(context.Discard), _actionButtons);
            _hudPresenter.StartTimer(context.TimeLimitSeconds);
        }

        private void CloseDecision()
        {
            _riichiArmed = false;

            _hudPresenter.CloseDecision();
            _handPresenter.SetSelectable(false);
        }

        private void OnHandTileSelected(Card card)
            => _inputPort.SubmitTurnAction(_riichiArmed ? TurnAction.Riichi(card) : TurnAction.Discard(card));
    }
}
