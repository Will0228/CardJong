using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CardJong.OutGame.Presentation.Home
{
    /// <summary>
    /// ホーム画面の状態。ゲームスタートの要求を持ち、State はここを介して待ち合わせる。
    /// </summary>
    /// <remarks>
    /// 見た目を組み立てるだけの View に、ボタンが押せるかどうかの状態や
    /// 完了待ちの仕組みまで持たせたくないので、ここに切り出している。
    /// InGame 側の <see cref="CardJong.InGame.Actions.PlayerInputPort"/> と役割分担は同じで、
    /// View は購読して見た目を変えるだけ、書き換えはここからだけ行う。
    /// </remarks>
    public sealed class HomeModel : IHomePresentation, IDisposable
    {
        private readonly ReactiveProperty<bool> _canStart = new(false);

        private UniTaskCompletionSource _startRequest;

        /// <summary>ゲームスタートのボタンを押せる状態かどうか。</summary>
        public ReadOnlyReactiveProperty<bool> CanStart => _canStart;

        public async UniTask WaitForGameStartAsync(CancellationToken cancellationToken)
        {
            var request = new UniTaskCompletionSource();

            // 押される前に中断された場合も待機を解けるようにしておく。
            using var registration = cancellationToken.Register(() => request.TrySetCanceled(cancellationToken));

            _startRequest = request;
            _canStart.Value = true;

            try
            {
                await request.Task;
            }
            finally
            {
                _startRequest = null;
                _canStart.Value = false;
            }
        }

        /// <summary>ゲームスタートのボタンから呼ぶ。</summary>
        public void RequestStart() => _startRequest?.TrySetResult();

        public void Dispose()
        {
            _canStart.Dispose();
        }
    }
}
