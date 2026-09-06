using System.Collections.Generic;

namespace CardJong.Network.Matching
{
    /// <summary>
    /// 入っている部屋のいまの状態。参加者が出入りするたびに新しい値へ差し替える。
    /// </summary>
    /// <param name="RoomId">部屋の識別子。Photon の Room.Name に対応する。</param>
    /// <param name="Criteria">この部屋の対局条件。</param>
    /// <param name="Members">参加者。入室順に並ぶ。</param>
    /// <param name="HostActorId">対局を開始できるプレイヤー。Photon の MasterClient に対応する。</param>
    /// <param name="LocalActorId">自分の <see cref="MatchMember.ActorId"/>。</param>
    public sealed record MatchRoom(
        string RoomId,
        MatchCriteria Criteria,
        IReadOnlyList<MatchMember> Members,
        int HostActorId,
        int LocalActorId)
    {
        /// <summary>部屋に居る人間の数。CPU はまだ居ないので含まない。</summary>
        public int HumanCount => Members.Count;

        /// <summary>自分がホストか。開始ボタンを出せるのはホストだけ。</summary>
        public bool IsLocalHost => LocalActorId == HostActorId;

        /// <summary>人間が入っていない席の数。開始した時点で CPU が埋める。</summary>
        public int EmptySeatCount => Criteria.PlayerCount - Members.Count;

        public bool IsFull => EmptySeatCount <= 0;

        public override string ToString()
            => $"{RoomId} {HumanCount}/{Criteria.PlayerCount}人";
    }
}
