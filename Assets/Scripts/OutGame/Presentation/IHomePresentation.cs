using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardJong.OutGame.Presentation
{
    /// <summary>
    /// ホーム画面の窓口。State はここを await するだけで、実際の見た目には関与しない。
    /// </summary>
    public interface IHomePresentation
    {
        /// <summary>ホーム画面の操作を受け付け、ゲームスタートが押されるまで待つ。</summary>
        UniTask WaitForGameStartAsync(CancellationToken cancellationToken);
    }
}
