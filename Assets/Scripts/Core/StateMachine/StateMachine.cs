using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CardJong.Core
{
    /// <summary>
    /// ステートを 1 つずつ直列に実行するステートマシン。
    /// インゲーム / アウトゲームなど、回したい単位ごとにキーの enum を変えて使う。
    /// </summary>
    /// <remarks>
    /// <para>1 ステートの流れは次の通り。</para>
    /// <list type="number">
    /// <item>ステートを生成し、そのステート専用の CancellationToken を用意する</item>
    /// <item>EnterAsync を await する</item>
    /// <item><see cref="IStateSwitcher{TKey}.RequestTransition"/> が呼ばれるまで待つ</item>
    /// <item>ExitAsync を呼び、ステートを破棄して次へ進む</item>
    /// </list>
    /// <para>
    /// 遷移要求は実行中でも待機中でも受け付ける。要求が来るとステート専用の
    /// CancellationToken がキャンセルされ、EnterAsync が走っていればそこで打ち切られる。
    /// この仕組みのおかげで、ステート内で開始したタイマーや入力待ちが次のステートまで
    /// 生き残ることがない。
    /// </para>
    /// </remarks>
    public sealed class StateMachine<TKey> : IStateSwitcher<TKey>, IDisposable where TKey : struct, Enum
    {
        private readonly IStateFactory<TKey> _stateFactory;
        private readonly ReactiveProperty<TKey> _currentState = new(default);
        private readonly Subject<TKey> _onStateEntered = new();

        private CancellationTokenSource _stateCancellation;

        /// <summary>遷移要求を受け取ったら完了する。要求済みなら await しても即座に返る。</summary>
        private UniTaskCompletionSource _transitionSignal;

        /// <summary>遷移先。RequestExit で終了する場合は null。</summary>
        private TKey? _requestedState;

        /// <summary>遷移要求を受け取ったか。null 遷移(終了)と未要求を区別するために持つ。</summary>
        private bool _hasTransitionRequest;

        private bool _isRunning;
        private bool _isDisposed;

        public ReadOnlyReactiveProperty<TKey> CurrentState => _currentState;

        public Observable<TKey> OnStateEntered => _onStateEntered;

        public StateMachine(IStateFactory<TKey> stateFactory)
        {
            _stateFactory = stateFactory ?? throw new ArgumentNullException(nameof(stateFactory));
        }

        /// <summary>
        /// entryState から開始し、RequestExit が呼ばれるまでステートを回し続ける。
        /// </summary>
        public async UniTask RunAsync(TKey entryState, CancellationToken cancellationToken)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(StateMachine<TKey>));
            if (_isRunning) throw new InvalidOperationException("StateMachine is already running.");

            _isRunning = true;
            try
            {
                var current = entryState;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var next = await RunStateAsync(current, cancellationToken);
                    if (!next.HasValue) return;

                    current = next.Value;
                }
            }
            finally
            {
                _isRunning = false;
            }
        }

        void IStateSwitcher<TKey>.RequestTransition(TKey nextState) => Request(nextState);

        void IStateSwitcher<TKey>.RequestExit() => Request(null);

        /// <summary>
        /// 1 ステート分を実行し、次のステートを返す。null なら RequestExit による終了。
        /// </summary>
        private async UniTask<TKey?> RunStateAsync(TKey key, CancellationToken cancellationToken)
        {
            // ステートは Transient で解決される想定なので、1 ステート 1 インスタンス。
            using var state = _stateFactory.Create(key);
            using var stateCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var signal = new UniTaskCompletionSource();

            // 外側のキャンセルでも待機を解けるようにしておく。
            using var registration = cancellationToken.Register(() => signal.TrySetCanceled(cancellationToken));

            _stateCancellation = stateCancellation;
            _transitionSignal = signal;
            _requestedState = null;
            _hasTransitionRequest = false;

            _currentState.Value = key;
            _onStateEntered.OnNext(key);

            try
            {
                await state.EnterAsync(stateCancellation.Token);

                // 遷移要求が来るまでこのステートに留まる。EnterAsync の中で要求済みなら即座に返る。
                // ボタン入力のように処理の外から遷移させたい場合はここで待つことになる。
                await signal.Task;
            }
            catch (OperationCanceledException) when (_hasTransitionRequest)
            {
                // EnterAsync の実行中に遷移要求が来て打ち切られた。中断は正常系として扱う。
            }
            finally
            {
                _stateCancellation = null;
                _transitionSignal = null;

                // ステート内で走っている非同期処理を止めてから後始末する。
                if (!stateCancellation.IsCancellationRequested)
                {
                    stateCancellation.Cancel();
                }

                // 後始末そのものは中断させたくないので、外側のトークンを渡す。
                await state.ExitAsync(cancellationToken);
            }

            // 遷移要求ではなく外側のキャンセルで抜けた場合は、そのまま伝播させる。
            cancellationToken.ThrowIfCancellationRequested();

            return _requestedState;
        }

        /// <summary>
        /// 遷移要求を記録し、待機を解いて実行中のステートを打ち切る。
        /// ステートの切り替え中に来た要求は次のステートの開始時に破棄される。
        /// </summary>
        private void Request(TKey? nextState)
        {
            if (!_isRunning) return;

            // 1 ステートにつき最初の要求だけを通す。
            // 例えば複数の入力が同時に遷移を要求しても、遷移先が上書きされない。
            if (_hasTransitionRequest) return;

            _requestedState = nextState;
            _hasTransitionRequest = true;

            _transitionSignal?.TrySetResult();

            // EnterAsync がまだ動いていれば打ち切る。
            _stateCancellation?.Cancel();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _stateCancellation?.Cancel();
            _stateCancellation = null;

            // 遷移要求を待っているステートは stateCancellation では解けないので、
            // 待機そのものを打ち切る。これをしないと RunAsync が戻ってこない。
            _transitionSignal?.TrySetCanceled();
            _transitionSignal = null;

            _onStateEntered.Dispose();
            _currentState.Dispose();
        }
    }
}
