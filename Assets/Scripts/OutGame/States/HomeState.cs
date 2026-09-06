using System.Threading;
using CardJong.Core;
using CardJong.OutGame.Presentation;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.OutGame.States
{
    /// <summary>ホーム画面。ゲームスタートが押されるまで待つ。</summary>
    public sealed class HomeState : AsyncStateBase<OutGameStateType>
    {
        private readonly IHomePresentation _presentation;

        [Inject]
        public HomeState(
            IStateSwitcher<OutGameStateType> stateSwitcher,
            IHomePresentation presentation) : base(stateSwitcher)
        {
            _presentation = presentation;
        }

        protected override async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            await _presentation.WaitForGameStartAsync(cancellationToken);

            // 遷移先のステートは無い。ここを抜けた後のシーン切り替えは
            // OutGameBootstrapper が受け持つ。
            RequestExit();
        }
    }
}
