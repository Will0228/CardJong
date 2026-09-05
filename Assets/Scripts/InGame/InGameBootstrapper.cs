using System;
using System.Threading;
using CardJong.Core;
using CardJong.InGame.States;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CardJong.InGame
{
    /// <summary>
    /// インゲームの起点。ステートマシンを GameStart から回し始める。
    /// スコープが破棄されるとキャンセルされ、実行中のステートも中断される。
    /// </summary>
    public sealed class InGameBootstrapper : IStartable, IDisposable
    {
        private readonly StateMachine<InGameStateType> _stateMachine;
        private readonly CancellationTokenSource _cancellation = new();

        [Inject]
        public InGameBootstrapper(StateMachine<InGameStateType> stateMachine)
        {
            _stateMachine = stateMachine;
        }

        void IStartable.Start()
        {
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            try
            {
                await _stateMachine.RunAsync(InGameStateType.GameStart, _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // スコープ破棄による中断は正常系
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
        }
    }
}
