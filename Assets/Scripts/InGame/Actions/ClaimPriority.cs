using System.Collections.Generic;
using CardJong.InGame.Rules;

namespace CardJong.InGame.Actions
{
    /// <summary>複数の宣言が競合したときの優先順位を解決する。</summary>
    public static class ClaimPriority
    {
        /// <summary>
        /// 発声の優先順位は ロン &gt; ポン &gt; チー。
        /// 同種が競合した場合は、捨てたプレイヤーから見て手番が近い席が優先される（頭ハネ）。
        /// 誰も宣言しなかった場合は <see cref="ClaimDeclaration.None"/> を返す。
        /// </summary>
        public static ClaimDeclaration Resolve(
            IReadOnlyList<ClaimDeclaration> declarations,
            int discarderSeat,
            int playerCount)
        {
            var best = ClaimDeclaration.None;
            var bestDistance = int.MaxValue;

            for (var i = 0; i < declarations.Count; i++)
            {
                var declaration = declarations[i];
                if (declaration.IsPass) continue;

                var distance = (declaration.Seat - discarderSeat + playerCount) % playerCount;

                if (best.IsPass || declaration.Type > best.Type ||
                    (declaration.Type == best.Type && distance < bestDistance))
                {
                    best = declaration;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
