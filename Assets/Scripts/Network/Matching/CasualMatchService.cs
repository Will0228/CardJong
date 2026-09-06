using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using VContainer;

namespace CardJong.Network.Matching
{
    /// <summary>
    /// カジュアルマッチの手順をまとめる。接続 → 入室 → 相手待ち → 開始まで。
    /// </summary>
    /// <remarks>
    /// 開始は時間で自動的に進めず、ホストのボタン操作だけで進める。
    /// 押した時点で空いている席は CPU が埋める。
    /// </remarks>
    public sealed class CasualMatchService : IDisposable
    {
        /// <summary>開始に必要な人間の数。自分の他に最低 1 人。</summary>
        public const int MinimumHumanCount = 2;

        private readonly IMatchClient _client;
        private readonly ISeatingArranger _seatingArranger;
        private readonly ReactiveProperty<CasualMatchPhase> _phase = new(CasualMatchPhase.Idle);
        private readonly ReactiveProperty<bool> _canStart = new(false);
        private readonly Subject<MatchStartNotice> _onMatchStarted = new();
        private readonly CompositeDisposable _subscriptions = new();

        /// <summary>いまどこまで進んでいるか。</summary>
        public ReadOnlyReactiveProperty<CasualMatchPhase> Phase => _phase;

        /// <summary>入っている部屋。入っていなければ null。</summary>
        public ReadOnlyReactiveProperty<MatchRoom> Room => _client.CurrentRoom;

        /// <summary>開始ボタンを押せるか。ホストで、かつ人間が 2 人以上いること。</summary>
        public ReadOnlyReactiveProperty<bool> CanStart => _canStart;

        /// <summary>対局が始まったときに発火する。ホストにもクライアントにも届く。</summary>
        public Observable<MatchStartNotice> OnMatchStarted => _onMatchStarted;

        [Inject]
        public CasualMatchService(IMatchClient client, ISeatingArranger seatingArranger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _seatingArranger = seatingArranger ?? throw new ArgumentNullException(nameof(seatingArranger));

            // 参加者の出入りとホストの交代でボタンの出しどころが変わる。
            _client.CurrentRoom.Subscribe(_ => RefreshCanStart()).AddTo(_subscriptions);
            _phase.Subscribe(_ => RefreshCanStart()).AddTo(_subscriptions);

            _client.OnMatchStartReceived.Subscribe(ReceiveMatchStart).AddTo(_subscriptions);
            _client.OnDisconnected.Subscribe(_ => _phase.Value = CasualMatchPhase.Idle).AddTo(_subscriptions);
        }

        /// <summary>
        /// 接続して条件の合う部屋に入る。戻ってきた時点では相手待ちで、対局はまだ始まっていない。
        /// </summary>
        public async UniTask EnterAsync(
            MatchProfile profile,
            MatchCriteria criteria,
            CancellationToken cancellationToken)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));
            criteria.Validate();

            try
            {
                if (!IsConnected(_client.ConnectionState.CurrentValue))
                {
                    _phase.Value = CasualMatchPhase.Connecting;
                    await _client.ConnectAsync(profile.NickName, cancellationToken);
                }

                _phase.Value = CasualMatchPhase.Matching;
                await _client.JoinRandomOrCreateRoomAsync(criteria, cancellationToken);

                _phase.Value = CasualMatchPhase.WaitingInRoom;
            }
            catch
            {
                _phase.Value = CasualMatchPhase.Idle;
                throw;
            }
        }

        /// <summary>ホストが開始ボタンを押したときに呼ぶ。空席を CPU で埋めて全員に配る。</summary>
        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            var room = _client.CurrentRoom.CurrentValue;
            if (!CanStartNow(room, _phase.CurrentValue))
            {
                throw new InvalidOperationException("いま対局を開始できる状態ではありません。");
            }

            _phase.Value = CasualMatchPhase.Starting;

            try
            {
                // 席割りを配る前に閉じる。配ったあとに人が入ると席が足りなくなる。
                await _client.CloseRoomAsync(cancellationToken);

                var seating = _seatingArranger.Arrange(room);
                await _client.BroadcastMatchStartAsync(seating, cancellationToken);
            }
            catch
            {
                _phase.Value = CasualMatchPhase.WaitingInRoom;
                throw;
            }
        }

        /// <summary>部屋を出てマッチングをやめる。</summary>
        public async UniTask LeaveAsync(CancellationToken cancellationToken)
        {
            await _client.LeaveRoomAsync(cancellationToken);
            _phase.Value = CasualMatchPhase.Idle;
        }

        private void ReceiveMatchStart(MatchSeating seating)
        {
            var room = _client.CurrentRoom.CurrentValue;
            if (room == null)
            {
                // 部屋を出た直後に届いた通知。自分には関係がないので捨てる。
                return;
            }

            _phase.Value = CasualMatchPhase.Started;
            _onMatchStarted.OnNext(new MatchStartNotice(room.Criteria, seating, _client.LocalActorId));
        }

        private void RefreshCanStart()
            => _canStart.Value = CanStartNow(_client.CurrentRoom.CurrentValue, _phase.CurrentValue);

        private bool CanStartNow(MatchRoom room, CasualMatchPhase phase)
        {
            if (phase != CasualMatchPhase.WaitingInRoom) return false;
            if (room == null || !room.IsLocalHost) return false;

            return room.HumanCount >= MinimumHumanCount && room.HumanCount <= room.Criteria.PlayerCount;
        }

        private bool IsConnected(MatchConnectionState state)
            => state is MatchConnectionState.Connected
                or MatchConnectionState.JoiningRoom
                or MatchConnectionState.InRoom;

        public void Dispose()
        {
            _subscriptions.Dispose();
            _phase.Dispose();
            _canStart.Dispose();
            _onMatchStarted.Dispose();
        }
    }
}
