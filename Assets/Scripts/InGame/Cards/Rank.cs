namespace CardJong.InGame.Cards
{
    /// <summary>
    /// ランク。A=1 〜 K=13。
    /// A は順子の最小（A-2-3）としても最大（Q-K-A）としても使えるが、K-A-2 のような循環はしない。
    /// </summary>
    /// <remarks>
    /// 数字がそのままランクの値になっている方が順子の判定を書きやすいので、
    /// この enum だけは 0 番目に None を置かず Ace = 1 から始める。
    /// 既定値の 0 はどのランクでもない値として、そのまま「未設定」を表す。
    /// </remarks>
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
