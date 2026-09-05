namespace CardJong.InGame.Cards
{
    /// <summary>
    /// ランク。A=1 〜 K=13。
    /// A は順子の最小（A-2-3）としても最大（Q-K-A）としても使えるが、K-A-2 のような循環はしない。
    /// </summary>
    public enum Rank : byte
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
    }
}
