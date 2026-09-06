namespace CardJong.Network.Matching
{
    /// <summary>
    /// 自分がマッチングに持ち込む情報。いまは表示名だけだが、
    /// アカウント連携やレート戦を入れるならここが差し替え先になる。
    /// </summary>
    /// <param name="NickName">部屋の参加者一覧に出る名前。</param>
    public sealed record MatchProfile(string NickName)
    {
        public static MatchProfile Default { get; } = new("Player");
    }
}
