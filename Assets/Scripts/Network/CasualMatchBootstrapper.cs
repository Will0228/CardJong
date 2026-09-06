using System;
using System.Threading;
using CardJong.Network.Matching;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CardJong.Network
{
    /// <summary>
    /// マッチングの起点。シーンを再生すると接続して部屋に入り、相手が来るのを待つ。
    /// 参加者の出入りと開始ボタンの可否はログに出す。
    /// </summary>
    /// <remarks>
    /// ロビーの View ができたら、ログの代わりにそちらへ流す。
    /// 開始は <see cref="StartMatch"/> をボタンに繋ぐ。
    /// </remarks>
    public sealed class CasualMatchBootstrapper : IStartable, IDisposable
    {
        private readonly CasualMatchService _service;
        private readonly MatchProfile _profile;
        private readonly MatchCriteria _criteria;
        private readonly CancellationTokenSource _cancellation = new();
        private readonly CompositeDisposable _subscriptions = new();

        [Inject]
        public CasualMatchBootstrapper(CasualMatchService service, MatchProfile profile, MatchCriteria criteria)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
        }

        void IStartable.Start()
        {
            _service.Phase.Subscribe(phase => Debug.Log($"[Match] {phase}")).AddTo(_subscriptions);
            _service.Room.Subscribe(LogRoom).AddTo(_subscriptions);
            _service.CanStart.Subscribe(LogCanStart).AddTo(_subscriptions);
            _service.OnMatchStarted.Subscribe(LogMatchStarted).AddTo(_subscriptions);

            EnterAsync().Forget();
        }

        /// <summary>ホストの開始ボタンから呼ぶ。</summary>
        public void StartMatch() => StartMatchAsync().Forget();

        private async UniTaskVoid EnterAsync()
        {
            try
            {
                await _service.EnterAsync(_profile, _criteria, _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // シーンの破棄による中断は正常系
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private async UniTaskVoid StartMatchAsync()
        {
            try
            {
                await _service.StartAsync(_cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // シーンの破棄による中断は正常系
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void LogRoom(MatchRoom room)
        {
            if (room == null)
            {
                Debug.Log("[Match] 部屋を出ました。");
                return;
            }

            var host = room.IsLocalHost ? "（ホスト）" : string.Empty;
            Debug.Log($"[Match] {room} {host} 参加者: {string.Join(", ", room.Members)}");
        }

        private void LogCanStart(bool canStart)
        {
            if (!canStart) return;

            var room = _service.Room.CurrentValue;
            Debug.Log($"[Match] 開始できます。空席 {room.EmptySeatCount} 分は CPU が埋まります。");
        }

        private void LogMatchStarted(MatchStartNotice notice)
        {
            Debug.Log($"[Match] 開始: {notice}");
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
            _subscriptions.Dispose();
        }
    }
}
