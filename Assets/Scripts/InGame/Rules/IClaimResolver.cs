using System.Collections.Generic;
using CardJong.InGame.Model;

namespace CardJong.InGame.Rules
{
    /// <summary>捨て札に対して各プレイヤーが宣言できる選択肢を列挙する。</summary>
    public interface IClaimResolver
    {
        /// <summary>
        /// 指定席のプレイヤーが、捨て札に対して実行できる宣言を列挙する。
        /// 何もできない場合は空を返す。
        /// </summary>
        IReadOnlyList<ClaimOption> GetOptions(InGameModel model, int seat, DiscardInfo discard);
    }
}
