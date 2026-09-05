using System.Collections.Generic;
using System.Threading;
using CardJong.Core;
using CardJong.Core.Commands;
using CardJong.InGame.Actions;
using CardJong.InGame.Commands;
using CardJong.InGame.Model;
using CardJong.InGame.Rules;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.InGame.States
{
    /// <summary>
    /// 待機（待機パターン）。捨て札に対して他家がロン・ポン・チーを宣言できる。
    /// 全員の宣言を並行して集め、ロン &gt; ポン &gt; チー の優先順位で 1 つだけ通す。
    /// </summary>
    public sealed class ClaimWaitState : InGameStateBase
    {
        private readonly InGameModel _model;
        private readonly InGameSettings _settings;
        private readonly IPlayerAgentRegistry _agentRegistry;
        private readonly IClaimResolver _claimResolver;
        private readonly GameCommandFactory _commandFactory;
        private readonly IGameCommandInvoker _commandInvoker;

        /// <summary>ロンできた席。見逃した場合はフリテンとして記録する。</summary>
        private readonly List<int> _ronAvailableSeats = new();

        [Inject]
        public ClaimWaitState(
            IStateSwitcher<InGameStateType> stateSwitcher,
            InGameModel model,
            InGameSettings settings,
            IPlayerAgentRegistry agentRegistry,
            IClaimResolver claimResolver,
            GameCommandFactory commandFactory,
            IGameCommandInvoker commandInvoker) : base(stateSwitcher)
        {
            _model = model;
            _settings = settings;
            _agentRegistry = agentRegistry;
            _claimResolver = claimResolver;
            _commandFactory = commandFactory;
            _commandInvoker = commandInvoker;
        }

        protected override async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            if (_model.LastDiscard == null)
            {
                // 捨て札が無い状態でここに来ることは無いが、来た場合は次のツモへ進める。
                RequestTransition(InGameStateType.Draw);
                return;
            }

            var discard = _model.LastDiscard;
            var declarations = await CollectDeclarationsAsync(discard, cancellationToken);
            var accepted = ClaimPriority.Resolve(declarations, discard.Seat, _model.PlayerCount);

            await RecordMissedRonsAsync(accepted, cancellationToken);

            if (accepted.IsPass)
            {
                // 誰も鳴かなかったので次のプレイヤーのツモへ
                _model.SetCurrentSeat(_model.GetNextSeat(discard.Seat));
                RequestTransition(InGameStateType.Draw);
                return;
            }

            var executed = await _commandInvoker.ExecuteAsync(
                _commandFactory.CreateFromClaim(accepted, discard, wasRonAvailable: false),
                cancellationToken);

            if (!executed)
            {
                // 成立しない宣言が返ってきた場合はパス扱いにして進行を止めない。
                _model.SetCurrentSeat(_model.GetNextSeat(discard.Seat));
                RequestTransition(InGameStateType.Draw);
                return;
            }

            if (accepted.Type == ClaimType.Ron)
            {
                RequestTransition(InGameStateType.Win);
                return;
            }

            // 鳴いたプレイヤーの手番になる。ツモは無く、そのまま打牌する。
            _model.SetCurrentSeat(accepted.Seat);
            _model.SetCanDeclareTsumo(false);
            RequestTransition(InGameStateType.PlayerAction);
        }

        private async UniTask<ClaimDeclaration[]> CollectDeclarationsAsync(
            DiscardInfo discard,
            CancellationToken cancellationToken)
        {
            _ronAvailableSeats.Clear();

            var tasks = new List<UniTask<ClaimDeclaration>>(_model.PlayerCount - 1);

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                if (seat == discard.Seat) continue;

                var options = _claimResolver.GetOptions(_model, seat, discard);
                if (options.Count == 0)
                {
                    tasks.Add(UniTask.FromResult(ClaimDeclaration.Pass(seat)));
                    continue;
                }

                if (ContainsRon(options))
                {
                    _ronAvailableSeats.Add(seat);
                }

                var context = new ClaimDecisionContext(
                    _model,
                    seat,
                    discard,
                    options,
                    _settings.ClaimWaitSeconds);

                tasks.Add(DecideClaimAsync(
                    _agentRegistry.Get(seat),
                    context,
                    ClaimDeclaration.Pass(seat),
                    cancellationToken));
            }

            return await UniTask.WhenAll(tasks);
        }

        /// <summary>ロンできたのに上がらなかった席を一時フリテンにする。</summary>
        private async UniTask RecordMissedRonsAsync(ClaimDeclaration accepted, CancellationToken cancellationToken)
        {
            var ronWinnerSeat = accepted.Type == ClaimType.Ron ? accepted.Seat : -1;

            for (var i = 0; i < _ronAvailableSeats.Count; i++)
            {
                var seat = _ronAvailableSeats[i];
                if (seat == ronWinnerSeat) continue;

                await _commandInvoker.ExecuteAsync(
                    _commandFactory.CreatePass(seat, wasRonAvailable: true),
                    cancellationToken);
            }
        }

        private bool ContainsRon(IReadOnlyList<ClaimOption> options)
        {
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i].Type == ClaimType.Ron) return true;
            }

            return false;
        }
    }
}
