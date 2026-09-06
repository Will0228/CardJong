using System.Threading;
using CardJong.Core;
using CardJong.OutGame.Presentation.Home;
using Cysharp.Threading.Tasks;
using VContainer;

namespace CardJong.OutGame.States
{
    /// <summary>ホーム画面。ゲームスタートが押されるまで待つ。</summary>
    public sealed class HomeState : AsyncStateBase<OutGameStateType>
    {
        private readonly IHomePresenter _presenter;

        [Inject]
        public HomeState(
            IStateSwitcher<OutGameStateType> stateSwitcher,
            IHomePresenter presenter) : base(stateSwitcher)
        {
            _presenter = presenter;
        }

        protected override async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            await _presenter.WaitForGameStartAsync(cancellationToken);

            // 遷移先のステートは無い。ここを抜けた後のシーン切り替えは
            // OutGameBootstrapper が受け持つ。
            RequestExit();
        }
    }
}
