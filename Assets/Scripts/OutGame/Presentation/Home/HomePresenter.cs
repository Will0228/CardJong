using R3;
using VContainer;

namespace CardJong.OutGame.Presentation.Home
{
    public interface IHomePresenter
    {
        Observable<Unit> OnStartClicked();
    }
    
    /// <summary>
    /// ホーム画面の Presenter。View のユーザー操作を購読し、State から要求された
    /// 「ゲームスタートまで待つ」を仲介する。View や State はお互いを直接知らない。
    /// </summary>
    public sealed class HomePresenter : IHomePresenter
    {
        private readonly HomeView _view;
        
        Observable<Unit> IHomePresenter.OnStartClicked() => _view.OnStartClicked();

        [Inject]
        public HomePresenter(HomeView view)
        {
            _view = view;
        }
    }
}
