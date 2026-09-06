using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;

namespace CardJong.InGame.Rules
{
    /// <summary>
    /// 上がり形（5 / 4 / 3 / 2）の判定。役の判定は <see cref="IScoreCalculator"/> の担当。
    /// </summary>
    public interface IHandAnalyzer
    {
        /// <summary>上がり形が完成しているか。</summary>
        bool IsWinningHand(IReadOnlyList<Card> concealedCards, IReadOnlyList<Meld> melds);

        /// <summary>上がり形の内訳を取り出す。複数通りの解釈がある場合は最初に見つかったもの。</summary>
        bool TryDecompose(IReadOnlyList<Card> concealedCards, IReadOnlyList<Meld> melds, out HandDecomposition decomposition);

        /// <summary>あと 1 枚で上がれる状態か。</summary>
        bool IsTenpai(IReadOnlyList<Card> concealedCards, IReadOnlyList<Meld> melds);

        /// <summary>上がり札の候補。上がり形は色で判定するため「色 + ランク」で返す。</summary>
        IReadOnlyList<CardPattern> EnumerateWaits(IReadOnlyList<Card> concealedCards, IReadOnlyList<Meld> melds);
    }
}
