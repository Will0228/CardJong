using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace CardJong.Core.Scenes
{
    /// <summary>
    /// <see cref="SceneManager"/> でシーンを読み替える。
    /// </summary>
    /// <remarks>
    /// シーン名の文字列がここにしか出てこないよう、呼び出し側には
    /// <see cref="GameSceneType"/> だけを見せる。名前は Build Settings の登録と一致させること。
    /// </remarks>
    public sealed class SceneLoader : ISceneLoader
    {
        public UniTask LoadAsync(GameSceneType scene, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var operation = SceneManager.LoadSceneAsync(ToSceneName(scene), LoadSceneMode.Single);
            if (operation == null)
            {
                throw new InvalidOperationException($"{scene} を読み込めませんでした。Build Settings の登録を確認してください。");
            }

            // 読み込みが終わると同時に呼び出し元のシーンが壊れ、そのスコープのトークンが
            // キャンセルされる。待機にトークンを渡すと必ずキャンセル例外になるので渡さない。
            return operation.ToUniTask();
        }

        /// <summary>enum とシーン名の対応。引数だけで答えが決まるので static にしている。</summary>
        private static string ToSceneName(GameSceneType scene) => scene switch
        {
            GameSceneType.OutGame => "OutGame",
            GameSceneType.InGame => "InGame",
            _ => throw new ArgumentOutOfRangeException(nameof(scene), scene, "未登録のシーンです。"),
        };
    }
}
