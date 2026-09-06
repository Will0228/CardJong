namespace CardJong.Network.Matching
{
    /// <summary>
    /// 対局開始の通知。ホストが席割りを確定して全員に配る。
    /// インゲーム側はこれを受け取って自分の席と CPU 席を決める。
    /// </summary>
    /// <param name="Criteria">この対局の条件。</param>
    /// <param name="Seating">確定した席割り。</param>
    /// <param name="LocalActorId">受け取った側の <see cref="MatchMember.ActorId"/>。</param>
    public sealed record MatchStartNotice(MatchCriteria Criteria, MatchSeating Seating, int LocalActorId)
    {
        /// <summary>自分の席。席を持たない場合は -1。</summary>
        public int LocalSeat => Seating.TryGetSeatOf(LocalActorId, out var seat) ? seat : -1;

        public override string ToString() => $"seat{LocalSeat} で開始 [{Seating}]";
    }
}
