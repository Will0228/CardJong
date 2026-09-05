using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;
using R3;

namespace CardJong.InGame.Model
{
    /// <summary>
    /// インゲーム全体の状態。State と Command が共有する唯一の真実。
    /// 書き換えは Command 層からのみ行い、View は購読するだけにする。
    /// </summary>
    public sealed class InGameModel : IDisposable
    {
        /// <summary>手牌の枚数。</summary>
        public const int HandSize = 13;

        /// <summary>上がり形の枚数（5 + 4 + 3 + 2）。</summary>
        public const int WinningHandSize = 14;

        /// <summary>1 人あたりのツモ回数。生き山の枚数を決める基準。</summary>
        public const int DrawCountPerPlayer = 18;

        private readonly List<PlayerModel> _players = new();
        private readonly ReactiveProperty<int> _dealerSeat = new(0);
        private readonly ReactiveProperty<int> _currentSeat = new(0);
        private readonly ReactiveProperty<int> _roundNumber = new(1);
        private readonly ReactiveProperty<int> _honba = new(0);
        private readonly Subject<DiscardInfo> _onDiscarded = new();

        public WallModel Wall { get; } = new();

        public IReadOnlyList<PlayerModel> Players => _players;

        public int PlayerCount => _players.Count;

        /// <summary>親の席。</summary>
        public ReadOnlyReactiveProperty<int> DealerSeat => _dealerSeat;

        /// <summary>現在の手番。</summary>
        public ReadOnlyReactiveProperty<int> CurrentSeat => _currentSeat;

        /// <summary>通し局番号（1 = 東 1 局）。</summary>
        public ReadOnlyReactiveProperty<int> RoundNumber => _roundNumber;

        /// <summary>本場。</summary>
        public ReadOnlyReactiveProperty<int> Honba => _honba;

        /// <summary>カードが捨てられたときに発火する。</summary>
        public Observable<DiscardInfo> OnDiscarded => _onDiscarded;

        /// <summary>直前の捨て札。局の開始直後は null。</summary>
        public DiscardInfo LastDiscard { get; private set; }

        /// <summary>
        /// 直前にツモしたか。鳴いた直後の打牌ではツモ上がりできないので false になる。
        /// </summary>
        public bool CanDeclareTsumo { get; private set; }

        /// <summary>
        /// 確定した上がりの内容。局の途中で上がりが成立した時点で設定され、局の終了処理で消費される。
        /// null のまま局が終わった場合は流局。
        /// </summary>
        public WinResult PendingWin { get; private set; }

        /// <summary>直近の局の結果。</summary>
        public RoundResult LastRoundResult { get; private set; }

        /// <summary>この対局の総局数（東風戦なら人数分、半荘戦なら 2 倍）。</summary>
        public int TotalRoundCount { get; private set; }

        /// <summary>全局を消化したか。</summary>
        public bool IsGameOver => _roundNumber.CurrentValue > TotalRoundCount;

        /// <summary>生き山の枚数。</summary>
        public int LiveWallCount => PlayerCount * DrawCountPerPlayer;

        public void Setup(int playerCount, int initialScore, int totalRoundCount)
        {
            if (playerCount is not (3 or 4))
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount, "3 人または 4 人のみ対応しています。");
            }

            foreach (var player in _players)
            {
                player.Dispose();
            }

            _players.Clear();
            for (var seat = 0; seat < playerCount; seat++)
            {
                _players.Add(new PlayerModel(seat, initialScore));
            }

            TotalRoundCount = totalRoundCount;
            _dealerSeat.Value = 0;
            _currentSeat.Value = 0;
            _roundNumber.Value = 1;
            _honba.Value = 0;
            LastDiscard = null;
            LastRoundResult = null;
            PendingWin = null;
            CanDeclareTsumo = false;
        }

        public PlayerModel GetPlayer(int seat) => _players[seat];

        /// <summary>次の手番の席。</summary>
        public int GetNextSeat(int seat) => (seat + 1) % PlayerCount;

        /// <summary>上家（自分の直前の手番）の席。チーはこの席からのみ可能。</summary>
        public int GetUpperSeat(int seat) => (seat - 1 + PlayerCount) % PlayerCount;

        public void SetDealer(int seat) => _dealerSeat.Value = seat;

        public void SetCurrentSeat(int seat) => _currentSeat.Value = seat;

        public void SetCanDeclareTsumo(bool value) => CanDeclareTsumo = value;

        public void SetLastDiscard(DiscardInfo info)
        {
            LastDiscard = info;
            _onDiscarded.OnNext(info);
        }

        public void ClearLastDiscard() => LastDiscard = null;

        public void SetPendingWin(WinResult win) => PendingWin = win;

        public void ClearPendingWin() => PendingWin = null;

        public void SetRoundResult(RoundResult result) => LastRoundResult = result;

        /// <summary>局を進める。連荘なら本場だけ増やし、親流れなら親と局番号を進める。</summary>
        /// <remarks>
        /// TODO: 連荘では局番号が進まないため、理屈の上では対局が終わらない。
        /// 打ち切り条件（連荘回数の上限、点数によるサドンデスなど）は仕様が固まり次第ここに入れる。
        /// </remarks>
        public void BeginNextRound(bool isDealerRepeat)
        {
            if (isDealerRepeat)
            {
                _honba.Value++;
            }
            else
            {
                _honba.Value = 0;
                _dealerSeat.Value = GetNextSeat(_dealerSeat.CurrentValue);
                _roundNumber.Value++;
            }

            LastDiscard = null;
            PendingWin = null;
            CanDeclareTsumo = false;
        }

        /// <summary>現在の点数から最終順位を作る。</summary>
        public GameResult BuildGameResult()
        {
            var ordered = new List<PlayerModel>(_players);
            ordered.Sort(static (a, b) =>
            {
                var score = b.Score.CurrentValue.CompareTo(a.Score.CurrentValue);
                return score != 0 ? score : a.Seat.CompareTo(b.Seat);
            });

            var rankings = new List<PlayerFinalScore>(ordered.Count);
            for (var i = 0; i < ordered.Count; i++)
            {
                rankings.Add(new PlayerFinalScore(ordered[i].Seat, ordered[i].Score.CurrentValue, i + 1));
            }

            return new GameResult(rankings);
        }

        public void Dispose()
        {
            foreach (var player in _players)
            {
                player.Dispose();
            }

            _players.Clear();
            Wall.Dispose();
            _dealerSeat.Dispose();
            _currentSeat.Dispose();
            _roundNumber.Dispose();
            _honba.Dispose();
            _onDiscarded.Dispose();
        }
    }
}
