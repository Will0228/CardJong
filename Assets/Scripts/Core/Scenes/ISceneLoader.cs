using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardJong.Core.Scenes
{
    /// <summary>
    /// シーンの読み替え。
    /// </summary>
    /// <remarks>
    /// 読み込みが終わった時点で呼び出し元のシーンは破棄される。
    /// await が返ってきた後に、呼び出し元のオブジェクトを触らないこと。
    /// </remarks>
    public interface ISceneLoader
    {
        /// <summary>指定のシーンへ切り替える。</summary>
        UniTask LoadAsync(GameSceneType scene, CancellationToken cancellationToken);
    }
}
