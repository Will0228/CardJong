using System;
using System.Collections.Generic;
using System.Threading;
using CardJong.InGame;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Realtime;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace CardJong.Network.Matching
{
    /// <summary>
    /// Photon Realtime を使う <see cref="IMatchClient"/>。
    /// </summary>
    /// <remarks>
    /// PUN の層（PhotonNetwork・PhotonView）は使わず、LoadBalancingClient を直接動かす。
    /// このゲームは GameObject の同期を必要とせず、送るのは席割りと行動だけなので、
    /// 部屋と RaiseEvent だけあれば足りる。
    /// </remarks>
    public sealed class PhotonMatchClient : IMatchClient, ITickable,
        IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, IOnEventCallback
    {
        /// <summary>席割りを配るイベントの番号。Photon が予約している 200 以上は避ける。</summary>
        private const byte MatchStartEventCode = 1;

        /// <summary>人数を部屋の検索条件に載せるためのキー。</summary>
        private const string PlayerCountKey = "pc";

        /// <summary>局数を部屋の検索条件に載せるためのキー。</summary>
        private const string RoundModeKey = "rm";

        /// <summary>ロビーの検索対象にする部屋プロパティ。</summary>
        private static readonly string[] LobbyKeys = { PlayerCountKey, RoundModeKey };

        private readonly LoadBalancingClient _client = new();
        private readonly PhotonMatchSettings _settings;

        private readonly ReactiveProperty<MatchConnectionState> _connectionState =
            new(MatchConnectionState.Disconnected);

        private readonly ReactiveProperty<MatchRoom> _currentRoom = new(null);
        private readonly Subject<MatchDisconnectReason> _onDisconnected = new();
        private readonly Subject<MatchSeating> _onMatchStartReceived = new();

        private UniTaskCompletionSource _connectCompletion;
        private UniTaskCompletionSource<MatchRoom> _joinCompletion;
        private UniTaskCompletionSource _leaveCompletion;
        private UniTaskCompletionSource _disconnectCompletion;
        private MatchCriteria _criteria = MatchCriteria.Default;

        public ReadOnlyReactiveProperty<MatchConnectionState> ConnectionState => _connectionState;

        public ReadOnlyReactiveProperty<MatchRoom> CurrentRoom => _currentRoom;

        public Observable<MatchDisconnectReason> OnDisconnected => _onDisconnected;

        public Observable<MatchSeating> OnMatchStartReceived => _onMatchStartReceived;

        public int LocalActorId => _client.LocalPlayer?.ActorNumber ?? 0;

        [Inject]
        public PhotonMatchClient(PhotonMatchSettings settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _client.AddCallbackTarget(this);
        }

        void ITickable.Tick()
        {
            // LoadBalancingClient は自前のループを持たないので、毎フレーム回して送受信を進める。
            _client.Service();
        }

        public async UniTask ConnectAsync(string nickName, CancellationToken cancellationToken)
        {
            if (!_settings.HasAppId)
            {
                throw new InvalidOperationException(
                    "App ID が空です。Photon のダッシュボードで発行した Realtime の App ID を "
                    + "PhotonMatchSettings に入れてください。");
            }

            if (_client.IsConnected) return;

            _client.NickName = string.IsNullOrWhiteSpace(nickName) ? MatchProfile.Default.NickName : nickName;
            _connectionState.Value = MatchConnectionState.Connecting;
            _connectCompletion = new UniTaskCompletionSource();

            if (!_client.ConnectUsingSettings(_settings.AppSettings))
            {
                _connectCompletion = null;
                _connectionState.Value = MatchConnectionState.Disconnected;
                throw new InvalidOperationException("Photon への接続を開始できませんでした。");
            }

            await AwaitAsync(_connectCompletion, cancellationToken);
        }

        public async UniTask<MatchRoom> JoinRandomOrCreateRoomAsync(
            MatchCriteria criteria,
            CancellationToken cancellationToken)
        {
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));
            criteria.Validate();

            _criteria = criteria;
            _connectionState.Value = MatchConnectionState.JoiningRoom;
            _joinCompletion = new UniTaskCompletionSource<MatchRoom>();

            var joinParams = new OpJoinRandomRoomParams
            {
                ExpectedMaxPlayers = criteria.PlayerCount,
                ExpectedCustomRoomProperties = CreateRoomProperties(criteria),
            };

            var createParams = new EnterRoomParams
            {
                RoomOptions = new RoomOptions
                {
                    MaxPlayers = criteria.PlayerCount,
                    CustomRoomProperties = CreateRoomProperties(criteria),
                    CustomRoomPropertiesForLobby = LobbyKeys,

                    // 抜けた席はすぐ空ける。復帰を待つ仕組みは対局の同期を作ってから考える。
                    PlayerTtl = 0,
                    CleanupCacheOnLeave = true,
                },
            };

            if (!_client.OpJoinRandomOrCreateRoom(joinParams, createParams))
            {
                _joinCompletion = null;
                _connectionState.Value = MatchConnectionState.Connected;
                throw new InvalidOperationException("部屋の検索を開始できませんでした。");
            }

            return await AwaitAsync(_joinCompletion, cancellationToken);
        }

        public async UniTask LeaveRoomAsync(CancellationToken cancellationToken)
        {
            if (_client.CurrentRoom == null) return;

            _leaveCompletion = new UniTaskCompletionSource();

            if (!_client.OpLeaveRoom(false))
            {
                _leaveCompletion = null;
                throw new InvalidOperationException("部屋を出られませんでした。");
            }

            await AwaitAsync(_leaveCompletion, cancellationToken);
        }

        public async UniTask DisconnectAsync(CancellationToken cancellationToken)
        {
            if (!_client.IsConnected) return;

            _disconnectCompletion = new UniTaskCompletionSource();
            _client.Disconnect();

            await AwaitAsync(_disconnectCompletion, cancellationToken);
        }

        public UniTask CloseRoomAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var room = _client.CurrentRoom;
            if (room == null) throw new InvalidOperationException("部屋に入っていません。");

            // 変更できるのはホストだけ。閉じた部屋にはマッチングで人が入ってこない。
            room.IsOpen = false;
            room.IsVisible = false;
            return UniTask.CompletedTask;
        }

        public UniTask BroadcastMatchStartAsync(MatchSeating seating, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (seating == null) throw new ArgumentNullException(nameof(seating));

            // ReceiverGroup.All は送った本人にも届く。ホストとクライアントで受け口を分けずに済む。
            var options = new RaiseEventOptions { Receivers = ReceiverGroup.All };

            if (!_client.OpRaiseEvent(MatchStartEventCode, EncodeSeating(seating), options, SendOptions.SendReliable))
            {
                throw new InvalidOperationException("席割りを配れませんでした。");
            }

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _client.RemoveCallbackTarget(this);

            if (_client.IsConnected)
            {
                _client.Disconnect();
            }

            _connectionState.Dispose();
            _currentRoom.Dispose();
            _onDisconnected.Dispose();
            _onMatchStartReceived.Dispose();
        }

        // --- Photon からの通知 ---

        void IConnectionCallbacks.OnConnectedToMaster()
        {
            _connectionState.Value = MatchConnectionState.Connected;
            _connectCompletion?.TrySetResult();
            _connectCompletion = null;
        }

        void IConnectionCallbacks.OnDisconnected(DisconnectCause cause)
        {
            _connectionState.Value = MatchConnectionState.Disconnected;
            _currentRoom.Value = null;

            var reason = ToDisconnectReason(cause);

            // 待っている操作を放置すると呼び出し側が固まるので、まとめて終わらせる。
            FailPending(new InvalidOperationException($"Photon から切断されました: {cause}"));
            _disconnectCompletion?.TrySetResult();
            _disconnectCompletion = null;

            _onDisconnected.OnNext(reason);
        }

        void IMatchmakingCallbacks.OnJoinedRoom()
        {
            _connectionState.Value = MatchConnectionState.InRoom;

            var room = PublishRoom();
            _joinCompletion?.TrySetResult(room);
            _joinCompletion = null;
        }

        void IMatchmakingCallbacks.OnJoinRandomFailed(short returnCode, string message)
            => FailJoin("部屋が見つかりませんでした", returnCode, message);

        void IMatchmakingCallbacks.OnCreateRoomFailed(short returnCode, string message)
            => FailJoin("部屋を建てられませんでした", returnCode, message);

        void IMatchmakingCallbacks.OnJoinRoomFailed(short returnCode, string message)
            => FailJoin("部屋に入れませんでした", returnCode, message);

        void IMatchmakingCallbacks.OnLeftRoom()
        {
            _currentRoom.Value = null;
            _connectionState.Value = _client.IsConnected
                ? MatchConnectionState.Connected
                : MatchConnectionState.Disconnected;

            _leaveCompletion?.TrySetResult();
            _leaveCompletion = null;
        }

        void IInRoomCallbacks.OnPlayerEnteredRoom(Player newPlayer) => PublishRoom();

        void IInRoomCallbacks.OnPlayerLeftRoom(Player otherPlayer) => PublishRoom();

        void IInRoomCallbacks.OnMasterClientSwitched(Player newMasterClient) => PublishRoom();

        void IInRoomCallbacks.OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) => PublishRoom();

        void IInRoomCallbacks.OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) => PublishRoom();

        void IOnEventCallback.OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != MatchStartEventCode) return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 2)
            {
                Debug.LogError("[Match] 席割りの中身を読めませんでした。");
                return;
            }

            _onMatchStartReceived.OnNext(DecodeSeating(payload));
        }

        // 使わない通知。Photon 側がインターフェースの実装を要求するので空で置く。
        void IConnectionCallbacks.OnConnected()
        {
        }

        void IConnectionCallbacks.OnRegionListReceived(RegionHandler regionHandler)
        {
        }

        void IConnectionCallbacks.OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
        }

        void IConnectionCallbacks.OnCustomAuthenticationFailed(string debugMessage)
        {
            FailPending(new InvalidOperationException($"Photon の認証に失敗しました: {debugMessage}"));
        }

        void IMatchmakingCallbacks.OnFriendListUpdate(List<FriendInfo> friendList)
        {
        }

        void IMatchmakingCallbacks.OnCreatedRoom()
        {
        }

        // --- 変換 ---

        private Hashtable CreateRoomProperties(MatchCriteria criteria) => new()
        {
            { PlayerCountKey, criteria.PlayerCount },
            { RoundModeKey, (byte)criteria.RoundMode },
        };

        /// <summary>Photon の部屋の状態を読み取って <see cref="MatchRoom"/> に写す。</summary>
        private MatchRoom PublishRoom()
        {
            var room = _client.CurrentRoom;
            if (room == null)
            {
                _currentRoom.Value = null;
                return null;
            }

            // Players は Dictionary なので順序が決まらない。ActorNumber 順に並べて入室順に揃える。
            var actorNumbers = new List<int>(room.Players.Keys);
            actorNumbers.Sort();

            var members = new List<MatchMember>(actorNumbers.Count);
            for (var i = 0; i < actorNumbers.Count; i++)
            {
                var player = room.Players[actorNumbers[i]];
                members.Add(new MatchMember(player.ActorNumber, GetDisplayName(player)));
            }

            var current = new MatchRoom(
                room.Name,
                ReadCriteria(room),
                members,
                room.MasterClientId,
                LocalActorId);

            _currentRoom.Value = current;
            return current;
        }

        /// <summary>部屋のプロパティから条件を読む。後から入った人も同じ条件を見られる。</summary>
        private MatchCriteria ReadCriteria(Room room)
        {
            var properties = room.CustomProperties;

            var playerCount = properties.TryGetValue(PlayerCountKey, out var rawCount) && rawCount is int count
                ? count
                : _criteria.PlayerCount;

            var roundMode = properties.TryGetValue(RoundModeKey, out var rawMode) && rawMode is byte mode
                ? (RoundMode)mode
                : _criteria.RoundMode;

            return new MatchCriteria(playerCount, roundMode);
        }

        private string GetDisplayName(Player player)
            => string.IsNullOrWhiteSpace(player.NickName) ? $"Player {player.ActorNumber}" : player.NickName;

        /// <summary>
        /// 席割りを Photon が送れる形にする。席番号は配列の添字で表し、
        /// ActorId が 0 の席が CPU。
        /// </summary>
        private object[] EncodeSeating(MatchSeating seating)
        {
            var actorIds = new int[seating.PlayerCount];
            var displayNames = new string[seating.PlayerCount];

            for (var i = 0; i < seating.Seats.Count; i++)
            {
                var assignment = seating.Seats[i];
                actorIds[assignment.Seat] = assignment.Kind == SeatOccupantKind.Human ? assignment.ActorId : 0;
                displayNames[assignment.Seat] = assignment.DisplayName;
            }

            return new object[] { actorIds, displayNames };
        }

        private MatchSeating DecodeSeating(object[] payload)
        {
            var actorIds = (int[])payload[0];
            var displayNames = (string[])payload[1];

            var assignments = new List<SeatAssignment>(actorIds.Length);
            for (var seat = 0; seat < actorIds.Length; seat++)
            {
                assignments.Add(actorIds[seat] == 0
                    ? SeatAssignment.ForCpu(seat, displayNames[seat])
                    : new SeatAssignment(seat, SeatOccupantKind.Human, actorIds[seat], displayNames[seat]));
            }

            return new MatchSeating(assignments);
        }

        private MatchDisconnectReason ToDisconnectReason(DisconnectCause cause) => cause switch
        {
            DisconnectCause.DisconnectByClientLogic => MatchDisconnectReason.ByRequest,
            DisconnectCause.DisconnectByServerLogic => MatchDisconnectReason.ServerError,
            DisconnectCause.DisconnectByServerReasonUnknown => MatchDisconnectReason.ServerError,
            DisconnectCause.InvalidAuthentication => MatchDisconnectReason.ServerError,
            DisconnectCause.CustomAuthenticationFailed => MatchDisconnectReason.ServerError,
            DisconnectCause.AuthenticationTicketExpired => MatchDisconnectReason.ServerError,
            DisconnectCause.MaxCcuReached => MatchDisconnectReason.ServerError,
            DisconnectCause.InvalidRegion => MatchDisconnectReason.ServerError,
            DisconnectCause.None => MatchDisconnectReason.None,
            _ => MatchDisconnectReason.ConnectionLost,
        };

        // --- 待ち合わせ ---

        private void FailJoin(string summary, short returnCode, string message)
        {
            _connectionState.Value = MatchConnectionState.Connected;
            _joinCompletion?.TrySetException(new InvalidOperationException($"{summary} ({returnCode}): {message}"));
            _joinCompletion = null;
        }

        private void FailPending(Exception exception)
        {
            _connectCompletion?.TrySetException(exception);
            _connectCompletion = null;

            _joinCompletion?.TrySetException(exception);
            _joinCompletion = null;

            _leaveCompletion?.TrySetResult();
            _leaveCompletion = null;
        }

        private async UniTask AwaitAsync(UniTaskCompletionSource completion, CancellationToken cancellationToken)
        {
            using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            await completion.Task;
        }

        private async UniTask<T> AwaitAsync<T>(
            UniTaskCompletionSource<T> completion,
            CancellationToken cancellationToken)
        {
            using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            return await completion.Task;
        }
    }
}
