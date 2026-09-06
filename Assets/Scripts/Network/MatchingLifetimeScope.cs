using CardJong.Core;
using CardJong.InGame;
using CardJong.Network.Matching;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CardJong.Network
{
    /// <summary>
    /// カジュアルマッチの DI 構成。このコンポーネントを置いたシーンを再生すると
    /// 部屋に入って相手を待つところまで進む。
    /// </summary>
    public sealed class MatchingLifetimeScope : LifetimeScope
    {
        [Header("マッチング条件")]
        [SerializeField, Range(3, 4)] private int _playerCount = 4;
        [SerializeField] private RoundMode _roundMode = RoundMode.East;

        [Header("プロフィール")]
        [SerializeField] private string _nickName = "Player";

        [Header("接続先")]
        [Tooltip("未設定の場合は通信せず、1 台で流れだけ確かめる LoopbackMatchClient になる。")]
        [SerializeField] private PhotonMatchSettings _photonSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(new MatchCriteria(_playerCount, _roundMode));
            builder.RegisterInstance(new MatchProfile(
                string.IsNullOrWhiteSpace(_nickName) ? MatchProfile.Default.NickName : _nickName));

            // SystemRandomService は引数ありのコンストラクタも持つので、生成方法を明示する。
            builder.Register<IRandomService>(_ => new SystemRandomService(), Lifetime.Singleton);

            RegisterMatchClient(builder);

            builder.Register<ISeatingArranger, SeatingArranger>(Lifetime.Singleton);
            builder.Register<CasualMatchService>(Lifetime.Singleton);

            builder.RegisterEntryPoint<CasualMatchBootstrapper>().AsSelf();
        }

        private void RegisterMatchClient(IContainerBuilder builder)
        {
            if (_photonSettings == null)
            {
                builder.Register<IMatchClient, LoopbackMatchClient>(Lifetime.Singleton);
                return;
            }

            builder.RegisterInstance(_photonSettings);

            // LoadBalancingClient は毎フレーム Service() を回す必要があるので、
            // ITickable として登録される EntryPoint にする。
            builder.RegisterEntryPoint<PhotonMatchClient>(Lifetime.Singleton).As<IMatchClient>();
        }

        // --- ここから下はロビーの UI ができるまでの動作確認用 ---

        [ContextMenu("動作確認/参加者を 1 人増やす")]
        private void AddDummyMember()
        {
            if (!TryGetLoopbackClient(out var client)) return;

            var member = client.AddDummyMember($"Guest {client.CurrentRoom.CurrentValue.HumanCount}");
            Debug.Log($"[Match] {member} が入室しました。");
        }

        [ContextMenu("動作確認/対局を開始する")]
        private void StartMatch()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Match] 再生中に実行してください。");
                return;
            }

            Container.Resolve<CasualMatchBootstrapper>().StartMatch();
        }

        private bool TryGetLoopbackClient(out LoopbackMatchClient client)
        {
            client = null;

            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Match] 再生中に実行してください。");
                return false;
            }

            var loopback = Container.Resolve<IMatchClient>() as LoopbackMatchClient;
            if (loopback == null)
            {
                Debug.LogWarning("[Match] LoopbackMatchClient のときだけ使えます。");
                return false;
            }

            if (loopback.CurrentRoom.CurrentValue == null)
            {
                Debug.LogWarning("[Match] まだ部屋に入っていません。");
                return false;
            }

            client = loopback;
            return true;
        }
    }
}
