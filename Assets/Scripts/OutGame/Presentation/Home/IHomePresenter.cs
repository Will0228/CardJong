using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardJong.OutGame.Presentation.Home
{
    /// <summary>
    /// ホーム画面の進行役。State はここを購読・呼び出しするだけで、
    /// View や DI の詳細には関与しない。
    /// </summary>
    public interface IHomePresenter
    {
        /// <summary>ホーム画面の操作を受け付け、ゲームスタートが押されるまで待つ。</summary>
        UniTask WaitForGameStartAsync(CancellationToken cancellationToken);
    }
}
