using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CardJong.Core
{
    /// <summary>
    /// <see cref="IAsyncState{TKey}"/> の基底実装。
    /// 継承側は <see cref="EnterAsync"/> を実装し、遷移したくなった時点で
    /// <see cref="RequestTransition"/> を呼ぶ。
    /// </summary>
    public abstract class AsyncStateBase<TKey> : IAsyncState<TKey> where TKey : struct, Enum
    {
        private readonly IStateSwitcher<TKey> _stateSwitcher;

        /// <summary>ステート破棄時にまとめて破棄される購読。</summary>
        protected readonly CompositeDisposable disposables = new();

        protected AsyncStateBase(IStateSwitcher<TKey> stateSwitcher)
        {
            _stateSwitcher = stateSwitcher ?? throw new ArgumentNullException(nameof(stateSwitcher));
        }

        UniTask IAsyncState<TKey>.EnterAsync(CancellationToken cancellationToken) => EnterAsync(cancellationToken);

        protected abstract UniTask EnterAsync(CancellationToken cancellationToken);

        UniTask IAsyncState<TKey>.ExitAsync(CancellationToken cancellationToken) => ExitAsync(cancellationToken);

        protected virtual UniTask ExitAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>
        /// 次のステートへ遷移する。EnterAsync の中からでも、購読やコールバックの中からでも呼べる。
        /// </summary>
        protected void RequestTransition(TKey nextState) => _stateSwitcher.RequestTransition(nextState);

        /// <summary>ステートマシンのループを終了する。</summary>
        protected void RequestExit() => _stateSwitcher.RequestExit();

        public virtual void Dispose()
        {
            disposables.Dispose();
        }
    }
}
