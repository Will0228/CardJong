using System;
using System.Collections.Generic;
using System.Threading;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation.Table;
using Cysharp.Threading.Tasks;
using R3;
using VContainer;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// 卓に重ねる HUD の受け持ち。局数・山の残り・ドラ表示札・名札はモデルの変化に合わせて
    /// 自分で並べ直し、選択を促す表示と画面いっぱいの案内は言われたとおりに出す。
    /// </summary>
    /// <remarks>
    /// 名札は卓のどこに座って見えるかに合わせて置いてあるので、席（seat）ではなく
    /// 卓の位置（slot）の順で View へ渡す。文字への整形は <see cref="InGameMessages"/> に
    /// 寄せてあるので、ここが持つのはどの値をどの順で並べるかだけ。
    ///
    /// 表示に要る値はすべて <see cref="InGameModel"/> から引けるので、HUD 専用の Model は
    /// 置いていない。リーチ予約のように画面の側だけが覚えておく状態は
    /// <see cref="InGamePresenter"/> にある。
    /// </remarks>
    public sealed class HudPresenter : IHudPresenter, IDisposable
    {
        private readonly InGameModel _model;
        private readonly InGameSettings _settings;
        private readonly InGameHudView _view;

        private readonly CompositeDisposable _subscriptions = new();

        // 並べ直すたびに List を作らずに済むよう、View へ渡す入れ物は使い回す。
        private readonly List<SeatPlateState> _seatPlates = new();

        private int HumanSeat => _settings.HumanSeat;

        [Inject]
        public HudPresenter(InGameModel model, InGameSettings settings, InGameHudView view)
        {
            _model = model;
            _settings = settings;
            _view = view;
        }

        public void Initialize()
        {
            _view.BuildSeatPlates(_model.PlayerCount);

            Bind();
            RefreshAll();
        }

        public void ShowDecision(string prompt, IReadOnlyList<ActionButtonSpec> actions)
        {
            _view.SetPrompt(prompt);
            _view.ShowActions(actions);
        }

        public void StartTimer(float seconds) => _view.ShowTimer(seconds);

        public void CloseDecision()
        {
            _view.ClearActions();
            _view.HideTimer();
            _view.SetPrompt(string.Empty);
        }

        public async UniTask ShowNoticeAsync(string message, float seconds, CancellationToken cancellationToken)
        {
            _view.ShowOverlay(message);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: cancellationToken);
            }
            finally
            {
                _view.HideOverlay();
            }
        }

        public void Dispose() => _subscriptions.Dispose();

        private void Bind()
        {
            _model.RoundNumber.Subscribe(_ => RefreshInfo()).AddTo(_subscriptions);
            _model.Honba.Subscribe(_ => RefreshInfo()).AddTo(_subscriptions);

            // ドラ表示札は生き山を確保する直前にめくられるので、残り枚数が動いた時点で
            // めくられたことも拾える。
            _model.Wall.LiveWallRemaining.Subscribe(_ =>
            {
                RefreshInfo();
                RefreshDora();
            }).AddTo(_subscriptions);

            _model.CurrentSeat.Subscribe(_ => RefreshSeatPlates()).AddTo(_subscriptions);
            _model.DealerSeat.Subscribe(_ => RefreshSeatPlates()).AddTo(_subscriptions);

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                var player = _model.GetPlayer(seat);
                player.Score.Points.Subscribe(_ => RefreshSeatPlates()).AddTo(_subscriptions);

                // リーチ宣言も名札に出るので、持ち物が動いたときも名札を直す。
                player.Cards.OnChanged.Subscribe(_ => RefreshSeatPlates()).AddTo(_subscriptions);
            }
        }

        private void RefreshAll()
        {
            RefreshInfo();
            RefreshDora();
            RefreshSeatPlates();
        }

        private void RefreshInfo()
            => _view.SetRoundInfo(
                InGameMessages.Round(_model.RoundNumber.CurrentValue, _model.Honba.CurrentValue, _model.PlayerCount),
                InGameMessages.WallRemaining(_model.Wall.LiveWallRemaining.CurrentValue));

        private void RefreshDora() => _view.SetDoraIndicators(_model.Wall.DoraIndicators);

        private void RefreshSeatPlates()
        {
            var dealerSeat = _model.DealerSeat.CurrentValue;
            var currentSeat = _model.CurrentSeat.CurrentValue;

            _seatPlates.Clear();

            for (var slot = 0; slot < _model.PlayerCount; slot++)
            {
                var seat = SeatOfSlot(slot);
                var player = _model.GetPlayer(seat);
                var isRiichi = player.Status.IsRiichi;

                _seatPlates.Add(new SeatPlateState(
                    InGameMessages.SeatName(RelationOf(slot), seat, seat == dealerSeat, isRiichi),
                    InGameMessages.SeatScore(player.Score.Points.CurrentValue),
                    isRiichi,
                    seat == currentSeat));
            }

            _view.SetSeatPlates(_seatPlates);
        }

        private int SeatOfSlot(int slot) => TableLayout.SeatOfSlot(slot, HumanSeat, _model.PlayerCount);

        private string RelationOf(int slot)
        {
            if (HumanSeat < 0) return string.Empty;
            if (slot == 0) return "自分";
            if (slot == 1) return "下家";

            return slot == _model.PlayerCount - 1 ? "上家" : "対面";
        }
    }
}
