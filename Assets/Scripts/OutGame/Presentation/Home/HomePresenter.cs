using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using VContainer;

namespace CardJong.OutGame.Presentation.Home
{
    /// <summary>
    /// ホーム画面の Presenter。View のユーザー操作を購読し、State から要求された
    /// 「ゲームスタートまで待つ」を仲介する。View や State はお互いを直接知らない。
    /// </summary>
    public sealed class HomePresenter : IHomePresenter
    {
        private readonly IHomeView _view;

        [Inject]
        public HomePresenter(IHomeView view)
        {
            _view = view;
        }

        public async UniTask WaitForGameStartAsync(CancellationToken cancellationToken)
        {
            var request = new UniTaskCompletionSource();

            // View が OnNext の中で同期的に押される場合に備え、購読してから受け付け始める。
            using var subscription = _view.OnStartClicked.Subscribe(_ => request.TrySetResult());
            using var registration = cancellationToken.Register(() => request.TrySetCanceled(cancellationToken));

            _view.CanStart = true;

            try
            {
                await request.Task;
            }
            finally
            {
                _view.CanStart = false;
            }
        }
    }
}
