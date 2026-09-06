namespace CardJong.Network.Matching
{
    /// <summary>
    /// 部屋に居る 1 人。席が決まるのは対局開始の瞬間なので、ここには席番号を持たせない。
    /// </summary>
    /// <param name="ActorId">サーバーが振る一意な番号。Photon の ActorNumber に対応する。</param>
    /// <param name="NickName">表示名。</param>
    public sealed record MatchMember(int ActorId, string NickName)
    {
        public override string ToString() => $"#{ActorId} {NickName}";
    }
}
