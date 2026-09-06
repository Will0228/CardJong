using System.Text;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation.Hud;
using CardJong.InGame.Rules;

namespace CardJong.InGame.Presentation
{
    /// <summary>
    /// 画面に出す文言。モデルの値を文字列にする変換をここへ集める。
    /// </summary>
    /// <remarks>
    /// 引数だけで答えが決まる純粋な変換なので static で置く。View には組み立て済みの
    /// 文字列だけを渡し、書式を持たせない。文言を直したいときはこのファイルだけを見ればよい。
    /// </remarks>
    public static class InGameMessages
    {
        public const string GameStart = "対局開始";

        public const string DiscardPrompt = "捨てる牌を選んでください";

        public const string RiichiPrompt = "リーチ宣言牌を選んでください";

        public const string PassButton = "パス";

        public const string RiichiButton = "リーチ";

        public const string TsumoButton = "ツモ";

        /// <summary>「東1局  0本場」。</summary>
        public static string Round(int roundNumber, int honba, int playerCount)
        {
            var windIndex = (roundNumber - 1) / playerCount;
            var number = (roundNumber - 1) % playerCount + 1;
            return $"{(windIndex == 0 ? "東" : "南")}{number}局  {honba}本場";
        }

        public static string WallRemaining(int count) => $"残り {count} 枚";

        public static string DealerDecision(int dealerSeat) => $"親は seat{dealerSeat}";

        /// <summary>名札の 1 行目。「自分  seat0  【親】  リーチ」。</summary>
        public static string SeatName(string relation, int seat, bool isDealer, bool isRiichi)
        {
            var dealerMark = isDealer ? "  【親】" : string.Empty;
            var riichiMark = isRiichi ? "  リーチ" : string.Empty;
            return $"{relation}  seat{seat}{dealerMark}{riichiMark}";
        }

        public static string SeatScore(int points) => $"{points:N0} 点";

        public static string DiscardAnnounce(DiscardInfo discard)
            => $"seat{discard.Seat} が {CardLabel.Of(discard.Card)} を捨てました";

        /// <summary>鳴きのボタン。どの札を使うのかまで出す。</summary>
        public static string ClaimButton(ClaimOption option)
        {
            if (option.Type == ClaimType.Ron) return "ロン";

            var name = option.Type == ClaimType.Pon ? "ポン" : "チー";
            return $"{name} [{CardLabel.Join(option.UsedCards)}]";
        }

        public static string Win(WinResult win)
        {
            var builder = new StringBuilder();
            builder.Append(win.IsTsumo
                ? $"seat{win.WinnerSeat}  ツモ"
                : $"seat{win.WinnerSeat}  ロン  (seat{win.LoserSeat} から)");
            builder.Append('\n').Append(CardLabel.Of(win.WinningCard)).Append('\n');

            for (var i = 0; i < win.Yaku.Count; i++)
            {
                builder.Append('\n').Append(win.Yaku[i].Name).Append("  ").Append(win.Yaku[i].Han).Append('翻');
            }

            if (win.DoraCount > 0)
            {
                builder.Append("\nドラ  ").Append(win.DoraCount);
            }

            builder.Append(win.IsYakuman ? "\n\n役満" : $"\n\n合計 {win.Han}翻");
            return builder.ToString();
        }

        public static string RoundResult(RoundResult result)
        {
            var builder = new StringBuilder(result.IsDrawGame ? "流局" : "局終了");
            builder.Append('\n');

            for (var seat = 0; seat < result.ScoreDeltas.Count; seat++)
            {
                builder.Append($"\nseat{seat}  {result.ScoreDeltas[seat]:+#,0;-#,0;±0}");
            }

            if (result.IsDealerRepeat)
            {
                builder.Append("\n\n連荘");
            }

            return builder.ToString();
        }

        public static string GameResult(GameResult result)
        {
            var builder = new StringBuilder("対局終了\n");
            for (var i = 0; i < result.Rankings.Count; i++)
            {
                var ranking = result.Rankings[i];
                builder.Append($"\n{ranking.Rank}位  seat{ranking.Seat}  {ranking.Score:N0}点");
            }

            return builder.ToString();
        }
    }
}
