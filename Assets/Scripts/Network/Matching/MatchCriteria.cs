using System;
using CardJong.InGame;

namespace CardJong.Network.Matching
{
    /// <summary>
    /// カジュアルマッチで相手を探す条件。条件が一致する部屋だけがマッチ対象になる。
    /// </summary>
    /// <param name="PlayerCount">対局人数。3 または 4。</param>
    /// <param name="RoundMode">東風戦か半荘戦か。</param>
    public sealed record MatchCriteria(int PlayerCount, RoundMode RoundMode)
    {
        public static MatchCriteria Default { get; } = new(4, RoundMode.East);

        /// <summary>対局として成立する値か確かめる。部屋を建てる前に呼ぶ。</summary>
        public void Validate()
        {
            if (PlayerCount is not (3 or 4))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(PlayerCount), PlayerCount, "3 人または 4 人のみ対応しています。");
            }

            if (RoundMode == RoundMode.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(RoundMode), RoundMode, "局数が未設定です。");
            }
        }

        public override string ToString() => $"{PlayerCount}人 {RoundMode}";
    }
}
