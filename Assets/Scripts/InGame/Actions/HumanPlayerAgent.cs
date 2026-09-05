using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Actions
{
    /// <summary>UI からの入力を待つ Agent。</summary>
    public sealed class HumanPlayerAgent : IPlayerAgent
    {
        private readonly IPlayerInputRequester _inputRequester;

        public int Seat { get; }

        public HumanPlayerAgent(int seat, IPlayerInputRequester inputRequester)
        {
            Seat = seat;
            _inputRequester = inputRequester ?? throw new ArgumentNullException(nameof(inputRequester));
        }

        public UniTask<TurnAction> DecideTurnActionAsync(
            TurnDecisionContext context,
            CancellationToken cancellationToken)
            => _inputRequester.RequestTurnActionAsync(context, cancellationToken);

        public UniTask<ClaimDeclaration> DecideClaimAsync(
            ClaimDecisionContext context,
            CancellationToken cancellationToken)
            => _inputRequester.RequestClaimAsync(context, cancellationToken);
    }
}
