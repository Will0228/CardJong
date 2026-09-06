using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CardJong.Network.Matching
{
    /// <summary>
    /// 通信を伴わない <see cref="IMatchClient"/>。SDK を入れる前に、
    /// 入室から開始までの流れを 1 台で確かめるために使う。
    /// </summary>
    /// <remarks>
    /// 自分は必ずホストになる。他の参加者は <see cref="AddDummyMember"/> で足す。
    /// </remarks>
    public sealed class LoopbackMatchClient : IMatchClient
    {
        private const string RoomId = "loopback";
        private const int LocalActor = 1;

        private readonly ReactiveProperty<MatchConnectionState> _connectionState =
            new(MatchConnectionState.Disconnected);

        private readonly ReactiveProperty<MatchRoom> _currentRoom = new(null);
        private readonly Subject<MatchDisconnectReason> _onDisconnected = new();
        private readonly Subject<MatchSeating> _onMatchStartReceived = new();
        private readonly List<MatchMember> _members = new();

        private MatchCriteria _criteria = MatchCriteria.Default;
        private string _nickName = MatchProfile.Default.NickName;
        private int _nextActorId = LocalActor + 1;

        public ReadOnlyReactiveProperty<MatchConnectionState> ConnectionState => _connectionState;

        public ReadOnlyReactiveProperty<MatchRoom> CurrentRoom => _currentRoom;

        public Observable<MatchDisconnectReason> OnDisconnected => _onDisconnected;

        public Observable<MatchSeating> OnMatchStartReceived => _onMatchStartReceived;

        public int LocalActorId
            => _connectionState.CurrentValue == MatchConnectionState.Disconnected ? 0 : LocalActor;

        public UniTask ConnectAsync(string nickName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _nickName = string.IsNullOrEmpty(nickName) ? MatchProfile.Default.NickName : nickName;
            _connectionState.Value = MatchConnectionState.Connected;
            return UniTask.CompletedTask;
        }

        public UniTask<MatchRoom> JoinRandomOrCreateRoomAsync(
            MatchCriteria criteria,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));

            _criteria = criteria;
            _members.Clear();
            _members.Add(new MatchMember(LocalActor, _nickName));
            _connectionState.Value = MatchConnectionState.InRoom;

            return UniTask.FromResult(PublishRoom());
        }

        public UniTask LeaveRoomAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _members.Clear();
            _currentRoom.Value = null;
            _connectionState.Value = MatchConnectionState.Connected;
            return UniTask.CompletedTask;
        }

        public UniTask DisconnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _members.Clear();
            _currentRoom.Value = null;
            _connectionState.Value = MatchConnectionState.Disconnected;
            _onDisconnected.OnNext(MatchDisconnectReason.ByRequest);
            return UniTask.CompletedTask;
        }

        public UniTask CloseRoomAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 新規参加者が来ない実装なので閉じるものが無い。
            return UniTask.CompletedTask;
        }

        public UniTask BroadcastMatchStartAsync(MatchSeating seating, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (seating == null) throw new ArgumentNullException(nameof(seating));

            _onMatchStartReceived.OnNext(seating);
            return UniTask.CompletedTask;
        }

        /// <summary>他の参加者が入ってきたことにする。動作確認用。</summary>
        public MatchMember AddDummyMember(string nickName)
        {
            if (_currentRoom.CurrentValue == null)
            {
                throw new InvalidOperationException("部屋に入っていません。");
            }

            if (_members.Count >= _criteria.PlayerCount)
            {
                throw new InvalidOperationException("席が埋まっています。");
            }

            var member = new MatchMember(_nextActorId++, nickName);
            _members.Add(member);
            PublishRoom();
            return member;
        }

        /// <summary>参加者が抜けたことにする。動作確認用。</summary>
        public bool RemoveDummyMember(int actorId)
        {
            if (_members.RemoveAll(member => member.ActorId == actorId) == 0) return false;

            PublishRoom();
            return true;
        }

        private MatchRoom PublishRoom()
        {
            var room = new MatchRoom(
                RoomId,
                _criteria,
                new List<MatchMember>(_members),
                LocalActor,
                LocalActor);

            _currentRoom.Value = room;
            return room;
        }

        public void Dispose()
        {
            _connectionState.Dispose();
            _currentRoom.Dispose();
            _onDisconnected.Dispose();
            _onMatchStartReceived.Dispose();
        }
    }
}
