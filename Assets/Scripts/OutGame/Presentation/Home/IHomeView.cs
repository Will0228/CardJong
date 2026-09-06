using R3;

namespace CardJong.OutGame.Presentation.Home
{
    /// <summary>
    /// ホーム画面の見た目の窓口。Presenter はここを介して見た目を操作し、
    /// ユーザー操作をここから受け取る。進行の判断は持たない。
    /// </summary>
    public interface IHomeView
    {
        /// <summary>ゲームスタートボタンが押されたときに発火する。</summary>
        Observable<Unit> OnStartClicked { get; }

        /// <summary>ゲームスタートボタンを押せるかどうか。</summary>
        bool CanStart { set; }
    }
}
