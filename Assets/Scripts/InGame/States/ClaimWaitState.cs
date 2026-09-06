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
    /// ロン &gt; ポン &gt; チー の優先順位のうち、上位の宣言が確定した時点で下位の返答を
    /// 待たずに結果を確定する。同じ優先度どうしは頭ハネ（捨てたプレイヤーに近い席が優先）で
    /// 決まるため、同じ階層の返答は全員ぶん待ってから確定する。
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

            var declarations = new List<ClaimDeclaration>(_model.PlayerCount - 1);
            var ronTasks = new List<UniTask<ClaimDeclaration>>();
            var ponTasks = new List<UniTask<ClaimDeclaration>>();
            var chiTasks = new List<UniTask<ClaimDeclaration>>();

            // 上位の階層が確定した時点で下位の返答を打ち切れるように、専用のトークンを挟む。
            using var tierCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                if (seat == discard.Seat) continue;

                var options = _claimResolver.GetOptions(seat, discard);
                if (options.Count == 0)
                {
                    declarations.Add(ClaimDeclaration.Pass(seat));
                    continue;
                }

                var context = new ClaimDecisionContext(
                    _model,
                    seat,
                    discard,
                    options,
                    _settings.ClaimWaitSeconds);

                var task = DecideClaimAsync(
                    _agentRegistry.Get(seat),
                    context,
                    ClaimDeclaration.Pass(seat),
                    tierCancellation.Token);

                // 席の階層は、その席が選べる最も優先度の高い選択肢で決まる。
                if (ContainsOptionType(options, ClaimType.Ron))
                {
                    _ronAvailableSeats.Add(seat);
                    ronTasks.Add(task);
                }
                else if (ContainsOptionType(options, ClaimType.Pon))
                {
                    ponTasks.Add(task);
                }
                else
                {
                    chiTasks.Add(task);
                }
            }

            // ロンが出せる席が全員決め終わるまでは待つ必要がある（複数ロン時の頭ハネのため）。
            // 決め終わった時点でロンが 1 つでもあれば、ポン・チーの結果は覆らないので打ち切る。
            if (ronTasks.Count > 0)
            {
                declarations.AddRange(await UniTask.WhenAll(ronTasks));
                if (ContainsDeclarationType(declarations, ClaimType.Ron))
                {
                    tierCancellation.Cancel();
                    return declarations.ToArray();
                }
            }

            // ポンも同様に、出た時点でチーの結果は覆らない。
            if (ponTasks.Count > 0)
            {
                declarations.AddRange(await UniTask.WhenAll(ponTasks));
                if (ContainsDeclarationType(declarations, ClaimType.Pon))
                {
                    tierCancellation.Cancel();
                    return declarations.ToArray();
                }
            }

            if (chiTasks.Count > 0)
            {
                declarations.AddRange(await UniTask.WhenAll(chiTasks));
            }

            return declarations.ToArray();
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

        private bool ContainsOptionType(IReadOnlyList<ClaimOption> options, ClaimType type)
        {
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i].Type == type) return true;
            }

            return false;
        }

        private bool ContainsDeclarationType(List<ClaimDeclaration> declarations, ClaimType type)
        {
            for (var i = 0; i < declarations.Count; i++)
            {
                if (declarations[i].Type == type) return true;
            }

            return false;
        }
    }
}
