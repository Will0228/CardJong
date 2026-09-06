using System;

namespace CardJong.InGame.Rules
{
    /// <summary>
    /// 翻数から点数を引く表。符は使用せず、4 翻を満貫とする圧縮スケール。
    /// </summary>
    /// <remarks>
    /// ツモの支払額は 4 人打ちを前提にした値。3 人打ちの配分は仕様上まだ未確定のため、
    /// 決まり次第ここを分岐させる。
    /// </remarks>
    public static class ScoreTable
    {
        /// <summary>本場 1 つあたりの加算点（和了点の合計に加算する）。</summary>
        public const int HonbaBonus = 300;

        /// <summary>ノーテン罰符の合計。</summary>
        public const int NotenPenaltyTotal = 3000;

        // 添字は点数帯: 0=1翻 1=2翻 2=3翻 3=満貫 4=跳満 5=倍満 6=三倍満 7=役満
        private static readonly int[] NonDealerRon = { 1000, 2000, 4000, 8000, 12000, 16000, 24000, 32000 };
        private static readonly int[] NonDealerTsumoFromNonDealer = { 300, 500, 1000, 2000, 3000, 4000, 6000, 8000 };
        private static readonly int[] NonDealerTsumoFromDealer = { 500, 1000, 2000, 4000, 6000, 8000, 12000, 16000 };
        private static readonly int[] DealerRon = { 1500, 3000, 6000, 12000, 18000, 24000, 36000, 48000 };
        private static readonly int[] DealerTsumoAll = { 500, 1000, 2000, 4000, 6000, 8000, 12000, 16000 };

        /// <summary>ロン和了時に放銃者が支払う点数。</summary>
        public static int GetRonPayment(int han, bool isDealer, bool isYakuman)
        {
            var tier = GetTier(han, isYakuman);
            return isDealer ? DealerRon[tier] : NonDealerRon[tier];
        }

        /// <summary>親のツモ和了時に、子 1 人が支払う点数（All）。</summary>
        public static int GetDealerTsumoPayment(int han, bool isYakuman) => DealerTsumoAll[GetTier(han, isYakuman)];

        /// <summary>子のツモ和了時に、他の子 1 人が支払う点数。</summary>
        public static int GetNonDealerTsumoPaymentFromNonDealer(int han, bool isYakuman)
            => NonDealerTsumoFromNonDealer[GetTier(han, isYakuman)];

        /// <summary>子のツモ和了時に、親が支払う点数。</summary>
        public static int GetNonDealerTsumoPaymentFromDealer(int han, bool isYakuman)
            => NonDealerTsumoFromDealer[GetTier(han, isYakuman)];

        /// <summary>翻数を点数帯の添字に変換する。</summary>
        private static int GetTier(int han, bool isYakuman)
        {
            if (isYakuman || han >= 11) return 7;
            if (han >= 9) return 6;
            if (han >= 7) return 5;
            if (han >= 5) return 4;
            if (han >= 4) return 3;
            return Math.Max(han, 1) - 1;
        }
    }
}
