using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CardJong.InGame.Actions
{
    /// <summary>
    /// 人間プレイヤーの入力を仲介する。
    /// Agent 側が要求を出し、UI 側が Submit で応答する形で待ち合わせる。
    /// </summary>
    public sealed class PlayerInputPort : IPlayerInputPort, IPlayerInputRequester, IDisposable
    {
        private readonly Subject<TurnDecisionContext> _onTurnDecisionRequested = new();
        private readonly Subject<ClaimDecisionContext> _onClaimDecisionRequested = new();
        private readonly Subject<Unit> _onDecisionClosed = new();
        private readonly Subject<TurnAction> _onTurnActionSubmitted = new();
        private readonly Subject<ClaimDeclaration> _onClaimSubmitted = new();

        public Observable<TurnDecisionContext> OnTurnDecisionRequested => _onTurnDecisionRequested;

        public Observable<ClaimDecisionContext> OnClaimDecisionRequested => _onClaimDecisionRequested;

        public Observable<Unit> OnDecisionClosed => _onDecisionClosed;

        public void SubmitTurnAction(TurnAction action) => _onTurnActionSubmitted.OnNext(action);

        public void SubmitClaim(ClaimDeclaration declaration) => _onClaimSubmitted.OnNext(declaration);

        public async UniTask<TurnAction> RequestTurnActionAsync(
            TurnDecisionContext context,
            CancellationToken cancellationToken)
        {
            var completionSource = new UniTaskCompletionSource<TurnAction>();

            // UI が OnNext の中で同期的に Submit する場合に備え、購読してから要求を流す。
            using var subscription = _onTurnActionSubmitted.Subscribe(
                action => completionSource.TrySetResult(action));
            using var registration = cancellationToken.Register(
                () => completionSource.TrySetCanceled(cancellationToken));

            try
            {
                _onTurnDecisionRequested.OnNext(context);
                return await completionSource.Task;
            }
            finally
            {
                _onDecisionClosed.OnNext(Unit.Default);
            }
        }

        public async UniTask<ClaimDeclaration> RequestClaimAsync(
            ClaimDecisionContext context,
            CancellationToken cancellationToken)
        {
            var completionSource = new UniTaskCompletionSource<ClaimDeclaration>();

            using var subscription = _onClaimSubmitted.Subscribe(
                declaration => completionSource.TrySetResult(declaration));
            using var registration = cancellationToken.Register(
                () => completionSource.TrySetCanceled(cancellationToken));

            try
            {
                _onClaimDecisionRequested.OnNext(context);
                return await completionSource.Task;
            }
            finally
            {
                _onDecisionClosed.OnNext(Unit.Default);
            }
        }

        public void Dispose()
        {
            _onTurnDecisionRequested.Dispose();
            _onClaimDecisionRequested.Dispose();
            _onDecisionClosed.Dispose();
            _onTurnActionSubmitted.Dispose();
            _onClaimSubmitted.Dispose();
        }
    }
}
