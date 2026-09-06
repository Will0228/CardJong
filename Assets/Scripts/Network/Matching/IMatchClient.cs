using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace CardJong.Network.Matching
{
    /// <summary>
    /// マッチングサーバーとのやり取り。Photon Realtime の LoadBalancingClient をこの裏に隠す。
    /// </summary>
    /// <remarks>
    /// ロビーで必要な操作だけを置いている。対局が始まったあとの通信は別の口を用意する。
    /// </remarks>
    public interface IMatchClient : IDisposable
    {
        /// <summary>いまの接続状態。</summary>
        ReadOnlyReactiveProperty<MatchConnectionState> ConnectionState { get; }

        /// <summary>入っている部屋。入っていなければ null。参加者が変わるたびに更新される。</summary>
        ReadOnlyReactiveProperty<MatchRoom> CurrentRoom { get; }

        /// <summary>切断されたときに発火する。</summary>
        Observable<MatchDisconnectReason> OnDisconnected { get; }

        /// <summary>ホストが配った席割りが届いたときに発火する。ホスト自身にも届く。</summary>
        Observable<MatchSeating> OnMatchStartReceived { get; }

        /// <summary>自分の <see cref="MatchMember.ActorId"/>。未接続なら 0。</summary>
        int LocalActorId { get; }

        /// <summary>サーバーへ接続する。Photon の ConnectUsingSettings に対応する。</summary>
        UniTask ConnectAsync(string nickName, CancellationToken cancellationToken);

        /// <summary>
        /// 条件の合う部屋へ入る。空いている部屋が無ければ自分で建ててホストになる。
        /// Photon の OpJoinRandomOrCreateRoom に対応する。
        /// </summary>
        UniTask<MatchRoom> JoinRandomOrCreateRoomAsync(MatchCriteria criteria, CancellationToken cancellationToken);

        /// <summary>部屋を出る。接続は保ったまま。</summary>
        UniTask LeaveRoomAsync(CancellationToken cancellationToken);

        /// <summary>サーバーから切断する。</summary>
        UniTask DisconnectAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 部屋を新規参加者から隠す。席割りを配る直前にホストが呼ぶ。
        /// Photon の Room.IsOpen / IsVisible に対応する。
        /// </summary>
        UniTask CloseRoomAsync(CancellationToken cancellationToken);

        /// <summary>席割りを全員に配る。ホストのみ。Photon の RaiseEvent に対応する。</summary>
        UniTask BroadcastMatchStartAsync(MatchSeating seating, CancellationToken cancellationToken);
    }
}
