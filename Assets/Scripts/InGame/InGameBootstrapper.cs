using System;
using System.Threading;
using CardJong.Core;
using CardJong.Core.Scenes;
using CardJong.InGame.States;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CardJong.InGame
{
    /// <summary>
    /// インゲームの起点。ステートマシンを GameStart から回し、
    /// 最終結果まで出し終えたらアウトゲーム（ホーム画面）へ戻る。
    /// スコープが破棄されるとキャンセルされ、実行中のステートも中断される。
    /// </summary>
    /// <remarks>
    /// シーンの切り替えをステートではなくここに置いているのは、
    /// 「このシーンが終わったら次はどこか」がシーンの起点と終点を持つ側の責任だから。
    /// </remarks>
    public sealed class InGameBootstrapper : IStartable, IDisposable
    {
        private readonly StateMachine<InGameStateType> _stateMachine;
        private readonly ISceneLoader _sceneLoader;
        private readonly CancellationTokenSource _cancellation = new();

        [Inject]
        public InGameBootstrapper(StateMachine<InGameStateType> stateMachine, ISceneLoader sceneLoader)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
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
                await _sceneLoader.LoadAsync(GameSceneType.OutGame, _cancellation.Token);
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
