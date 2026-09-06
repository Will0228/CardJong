using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CardJong.InGame.Actions;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation.Table;
using CardJong.InGame.Rules;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// 卓の上に重ねる画面。局の進行を <see cref="IInGamePresentation"/> として受け取りつつ、
    /// 人間プレイヤーの選択を <see cref="IPlayerInputPort"/> へ返す。
    /// </summary>
    /// <remarks>
    /// 他家の手牌・河・鳴き・ドラは <see cref="MahjongTableView"/> が 3D の卓で見せる。
    /// ここが持つのは、点数や局数といった文字情報と、宣言のボタンと、
    /// 画面下に並べる自分の手牌（<see cref="HandUiView"/>）。
    /// </remarks>
    [AddComponentMenu("CardJong/InGame HUD View")]
    public sealed class InGameHudView : MonoBehaviour, IInGamePresentation
    {
        private static readonly Color PanelColor = new(0f, 0f, 0f, 0.55f);
        private static readonly Color OverlayColor = new(0f, 0f, 0f, 0.72f);
        private static readonly Color TimerColor = new(0.98f, 0.78f, 0.30f);
        private static readonly Color WinButtonColor = new(0.76f, 0.24f, 0.24f);
        private static readonly Color MeldButtonColor = new(0.20f, 0.40f, 0.66f);
        private static readonly Color RiichiButtonColor = new(0.70f, 0.45f, 0.16f);
        private static readonly Color RiichiArmedColor = new(0.94f, 0.64f, 0.20f);
        private static readonly Color PassButtonColor = new(0.32f, 0.34f, 0.36f);

        private static readonly Vector2 InfoPanelSize = new(360f, 96f);
        private static readonly Vector2 ActionBarSize = new(1240f, 150f);

        private const float ButtonHeight = 52f;

        /// <summary>手牌の帯を画面下端からどれだけ浮かせるか。</summary>
        private const float HandBottomMargin = 24f;

        [SerializeField]
        [Tooltip("画面下に並べる手牌のプレハブ。")]
        private MahjongTileUiView _tileUiPrefab;

        [SerializeField]
        [Min(0f)]
        [Tooltip("局の開始・親決めなど、短い案内を出しておく秒数。")]
        private float _noticeSeconds = 1.6f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("和了・局終了・最終結果を出しておく秒数。")]
        private float _resultSeconds = 3.5f;

        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<SeatPlateView> _seatPlates = new();
        private readonly List<GameObject> _actionButtons = new();

        private InGameModel _model;
        private InGameSettings _settings;
        private IPlayerInputPort _inputPort;
        private MahjongTableView _table;
        private HudUiFactory _factory;
        private HandUiView _handUi;

        private RectTransform _root;
        private Text _roundText;
        private Text _wallText;
        private RectTransform _buttonRoot;
        private Text _promptText;
        private RectTransform _timerRoot;
        private Image _timerFill;
        private RectTransform _overlayRoot;
        private Text _overlayText;

        private Button _riichiButton;
        private bool _riichiArmed;
        private float _timerDuration;
        private float _timerRemaining;

        private int HumanSeat => _settings.HumanSeat;

        [Inject]
        public void Construct(
            InGameModel model,
            InGameSettings settings,
            IPlayerInputPort inputPort,
            MahjongTableView table)
        {
            _model = model;
            _settings = settings;
            _inputPort = inputPort;
            _table = table;

            _factory = new HudUiFactory();
            BuildHierarchy();

            _handUi.TileClicked += OnHandTileClicked;
            _subscriptions.Add(_inputPort.OnTurnDecisionRequested.Subscribe(OnTurnDecisionRequested));
            _subscriptions.Add(_inputPort.OnClaimDecisionRequested.Subscribe(OnClaimDecisionRequested));
            _subscriptions.Add(_inputPort.OnDecisionClosed.Subscribe(_ => CloseDecision()));
        }

        public UniTask ShowGameStartAsync(CancellationToken cancellationToken)
        {
            // ここまでにモデルの初期化が済んでいるので、席の数が決まったこの時点で表示を組む。
            _table.Initialize();
            BuildSeatPlates();
            SubscribeToModel();
            RefreshAll();

            return ShowOverlayAsync("対局開始", _noticeSeconds, cancellationToken);
        }

        public UniTask ShowDealerDecisionAsync(int dealerSeat, CancellationToken cancellationToken)
            => ShowOverlayAsync($"親は seat{dealerSeat}", _noticeSeconds, cancellationToken);

        public UniTask ShowRoundStartAsync(int roundNumber, int honba, CancellationToken cancellationToken)
            => ShowOverlayAsync($"{RoundLabel(roundNumber)}  {honba}本場", _noticeSeconds, cancellationToken);

        public UniTask ShowWinAsync(WinResult win, CancellationToken cancellationToken)
            => ShowOverlayAsync(BuildWinMessage(win), _resultSeconds, cancellationToken);

        public UniTask ShowRoundResultAsync(RoundResult result, CancellationToken cancellationToken)
            => ShowOverlayAsync(BuildRoundResultMessage(result), _resultSeconds, cancellationToken);

        public UniTask ShowGameResultAsync(GameResult result, CancellationToken cancellationToken)
            => ShowOverlayAsync(BuildGameResultMessage(result), _resultSeconds * 2f, cancellationToken);

        private void Update()
        {
            if (_timerDuration <= 0f) return;

            _timerRemaining = Mathf.Max(0f, _timerRemaining - Time.deltaTime);
            _timerFill.fillAmount = _timerRemaining / _timerDuration;
        }

        private void OnDestroy()
        {
            if (_handUi != null) _handUi.TileClicked -= OnHandTileClicked;
            _subscriptions.Dispose();
        }

        // ---- 画面の組み立て ----

        private void BuildHierarchy()
        {
            var canvas = _factory.CreateCanvas("HudCanvas", transform);

            // 卓が透けて見えるよう、背景は敷かない。
            _root = _factory.CreateRect("Root", canvas.transform);
            HudUiFactory.Stretch(_root);

            BuildInfoPanel();
            BuildHandUi();
            BuildActionBar();
            BuildOverlay((RectTransform)canvas.transform);
        }

        private void BuildInfoPanel()
        {
            var panel = _factory.CreateColumn("Info", _root, 2f, TextAnchor.MiddleLeft);
            HudUiFactory.Anchor(
                panel,
                new Vector2(0f, 1f),
                InfoPanelSize,
                new Vector2(24f + InfoPanelSize.x * 0.5f, -(24f + InfoPanelSize.y * 0.5f)));

            panel.gameObject.AddComponent<Image>().color = PanelColor;
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.childForceExpandWidth = true;

            _roundText = _factory.CreateText("Round", panel, 30, TextAnchor.MiddleLeft, Color.white);
            HudUiFactory.SetFixedSize(_roundText.rectTransform, -1f, 38f);

            _wallText = _factory.CreateText("Wall", panel, 22, TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.82f));
            HudUiFactory.SetFixedSize(_wallText.rectTransform, -1f, 28f);
        }

        private void BuildHandUi()
        {
            var rect = _factory.CreateRect("Hand", _root);
            _handUi = rect.gameObject.AddComponent<HandUiView>();
            _handUi.Build(_tileUiPrefab, new Vector2(0f, HandUiView.AreaHeight * 0.5f + HandBottomMargin));
        }

        private void BuildActionBar()
        {
            // 手牌は画面の下端に並ぶので、その上に重ならない高さへ置く。
            var bar = _factory.CreateColumn("ActionBar", _root, 10f, TextAnchor.LowerCenter);
            HudUiFactory.Anchor(bar, new Vector2(0.5f, 0f), ActionBarSize, new Vector2(0f, 400f));

            var barLayout = bar.GetComponent<VerticalLayoutGroup>();
            barLayout.childForceExpandWidth = true;
            barLayout.padding = new RectOffset(20, 20, 12, 12);

            var promptRow = _factory.CreateRow("Prompt", bar, 20f, TextAnchor.MiddleCenter);
            HudUiFactory.SetFlexibleWidth(promptRow, 34f);

            _promptText = _factory.CreateText("Text", promptRow, 26, TextAnchor.MiddleRight, Color.white);
            HudUiFactory.SetFixedSize(_promptText.rectTransform, 640f, 34f);

            // 牌の上に重なっても読めるよう、文字に縁を付ける。
            var outline = _promptText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            _timerRoot = _factory.CreateRect("Timer", promptRow);
            HudUiFactory.SetFixedSize(_timerRoot, 280f, 14f);
            _timerRoot.gameObject.AddComponent<Image>().color = PanelColor;

            var fillRect = _factory.CreateRect("Fill", _timerRoot);
            HudUiFactory.Stretch(fillRect);
            _timerFill = fillRect.gameObject.AddComponent<Image>();
            _timerFill.color = TimerColor;
            _timerFill.type = Image.Type.Filled;
            _timerFill.fillMethod = Image.FillMethod.Horizontal;
            _timerFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _timerRoot.gameObject.SetActive(false);

            _buttonRoot = _factory.CreateRow("Buttons", bar, 12f, TextAnchor.MiddleCenter);
            HudUiFactory.SetFlexibleWidth(_buttonRoot, ButtonHeight);
        }

        private void BuildOverlay(RectTransform parent)
        {
            _overlayRoot = _factory.CreateRect("Overlay", parent);
            HudUiFactory.Stretch(_overlayRoot);
            _overlayRoot.gameObject.AddComponent<Image>().color = OverlayColor;

            _overlayText = _factory.CreateText("Text", _overlayRoot, 40, TextAnchor.MiddleCenter, Color.white);
            HudUiFactory.Stretch(_overlayText.rectTransform);
            _overlayRoot.gameObject.SetActive(false);
        }

        /// <summary>名札を、その席が卓のどこに座っているかに合わせて画面の縁に置く。</summary>
        private void BuildSeatPlates()
        {
            for (var i = 0; i < _seatPlates.Count; i++)
            {
                Destroy(_seatPlates[i].gameObject);
            }

            _seatPlates.Clear();

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                var slot = TableLayout.SlotOf(seat, HumanSeat, _model.PlayerCount);
                var rect = _factory.CreateRect($"Seat{seat}", _root);
                var plate = rect.gameObject.AddComponent<SeatPlateView>();
                plate.Build(_factory, seat, RelationLabelOf(slot), PlateAnchor(slot), PlatePosition(slot));
                _seatPlates.Add(plate);
            }
        }

        private static Vector2 PlateAnchor(int slot) => slot switch
        {
            1 => new Vector2(1f, 0.5f),
            2 => new Vector2(0.5f, 1f),
            3 => new Vector2(0f, 0.5f),
            _ => new Vector2(0f, 0f),
        };

        private static Vector2 PlatePosition(int slot)
        {
            var halfWidth = SeatPlateView.PlateSize.x * 0.5f;
            var halfHeight = SeatPlateView.PlateSize.y * 0.5f;

            // 自分の名札だけは、画面下に並ぶ手牌に重ならないよう手牌の帯の上に置く。
            var handTop = HandBottomMargin + HandUiView.AreaHeight;

            return slot switch
            {
                1 => new Vector2(-(24f + halfWidth), -20f),
                2 => new Vector2(0f, -(24f + halfHeight)),
                3 => new Vector2(24f + halfWidth, -20f),
                _ => new Vector2(24f + halfWidth, handTop + 16f + halfHeight),
            };
        }

        // ---- モデルの購読 ----

        private void SubscribeToModel()
        {
            _subscriptions.Add(_model.RoundNumber.Subscribe(_ => RefreshInfo()));
            _subscriptions.Add(_model.Honba.Subscribe(_ => RefreshInfo()));
            _subscriptions.Add(_model.Wall.LiveWallRemaining.Subscribe(_ => RefreshInfo()));
            _subscriptions.Add(_model.CurrentSeat.Subscribe(_ => RefreshSeatPlates()));
            _subscriptions.Add(_model.DealerSeat.Subscribe(_ => RefreshSeatPlates()));

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                var player = _model.GetPlayer(seat);
                _subscriptions.Add(player.Score.Points.Subscribe(_ => RefreshSeatPlates()));
                _subscriptions.Add(player.Cards.OnChanged.Subscribe(_ =>
                {
                    RefreshSeatPlates();
                    if (player.Seat == HumanSeat) RefreshHand();
                }));
            }
        }

        private void RefreshAll()
        {
            RefreshInfo();
            RefreshSeatPlates();
            RefreshHand();
        }

        private void RefreshHand()
        {
            if (HumanSeat < 0) return;

            _handUi.Refresh(_model.GetPlayer(HumanSeat).Cards, _model.Wall);
        }

        private void RefreshInfo()
        {
            _roundText.text = $"{RoundLabel(_model.RoundNumber.CurrentValue)}  {_model.Honba.CurrentValue}本場";
            _wallText.text = $"残り {_model.Wall.LiveWallRemaining.CurrentValue} 枚";
        }

        private void RefreshSeatPlates()
        {
            var dealerSeat = _model.DealerSeat.CurrentValue;
            var currentSeat = _model.CurrentSeat.CurrentValue;

            for (var i = 0; i < _seatPlates.Count; i++)
            {
                var plate = _seatPlates[i];
                plate.Refresh(_model.GetPlayer(plate.Seat), plate.Seat == dealerSeat, plate.Seat == currentSeat);
            }
        }

        // ---- 人間プレイヤーの入力 ----

        private void OnTurnDecisionRequested(TurnDecisionContext context)
        {
            _riichiArmed = false;
            _riichiButton = null;
            ClearActionButtons();

            _promptText.text = "捨てる牌を選んでください";

            if (context.CanDeclareTsumo)
            {
                AddActionButton("ツモ", WinButtonColor, () => _inputPort.SubmitTurnAction(TurnAction.Tsumo()));
            }

            if (context.CanDeclareRiichi)
            {
                _riichiButton = AddActionButton("リーチ", RiichiButtonColor, ToggleRiichi);
            }

            _handUi.SetInteractable(true);
            StartTimer(context.TimeLimitSeconds);
        }

        private void OnClaimDecisionRequested(ClaimDecisionContext context)
        {
            ClearActionButtons();

            _promptText.text = $"seat{context.Discard.Seat} が {CardLabel.Of(context.Discard.Card)} を捨てました";

            for (var i = 0; i < context.Options.Count; i++)
            {
                var option = context.Options[i];
                AddActionButton(
                    ClaimButtonLabel(option),
                    option.Type == ClaimType.Ron ? WinButtonColor : MeldButtonColor,
                    () => _inputPort.SubmitClaim(ClaimDeclaration.From(context.Seat, option)));
            }

            AddActionButton("パス", PassButtonColor, () => _inputPort.SubmitClaim(ClaimDeclaration.Pass(context.Seat)));
            StartTimer(context.TimeLimitSeconds);
        }

        private void CloseDecision()
        {
            _riichiArmed = false;
            _riichiButton = null;
            ClearActionButtons();
            StopTimer();
            _promptText.text = string.Empty;
            _handUi.SetInteractable(false);
        }

        private void OnHandTileClicked(Card card)
        {
            _inputPort.SubmitTurnAction(_riichiArmed ? TurnAction.Riichi(card) : TurnAction.Discard(card));
        }

        /// <summary>リーチは宣言と打牌が一体なので、ボタンで予約してから捨てる牌を選ばせる。</summary>
        private void ToggleRiichi()
        {
            _riichiArmed = !_riichiArmed;
            _promptText.text = _riichiArmed ? "リーチ宣言牌を選んでください" : "捨てる牌を選んでください";
            _riichiButton.image.color = _riichiArmed ? RiichiArmedColor : RiichiButtonColor;
        }

        private Button AddActionButton(string label, Color color, Action onClicked)
        {
            var button = _factory.CreateButton(label, _buttonRoot, label, color, 24, ButtonHeight);
            button.onClick.AddListener(() => onClicked());
            _actionButtons.Add(button.gameObject);
            return button;
        }

        private void ClearActionButtons()
        {
            for (var i = 0; i < _actionButtons.Count; i++)
            {
                Destroy(_actionButtons[i]);
            }

            _actionButtons.Clear();
        }

        private void StartTimer(float seconds)
        {
            _timerDuration = seconds;
            _timerRemaining = seconds;
            _timerFill.fillAmount = 1f;
            _timerRoot.gameObject.SetActive(seconds > 0f);
        }

        private void StopTimer()
        {
            _timerDuration = 0f;
            _timerRoot.gameObject.SetActive(false);
        }

        // ---- 案内の表示 ----

        private async UniTask ShowOverlayAsync(string message, float seconds, CancellationToken cancellationToken)
        {
            _overlayText.text = message;
            _overlayRoot.gameObject.SetActive(true);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: cancellationToken);
            }
            finally
            {
                _overlayRoot.gameObject.SetActive(false);
            }
        }

        private string RoundLabel(int roundNumber)
        {
            var windIndex = (roundNumber - 1) / _model.PlayerCount;
            var number = (roundNumber - 1) % _model.PlayerCount + 1;
            return $"{(windIndex == 0 ? "東" : "南")}{number}局";
        }

        private string RelationLabelOf(int slot)
        {
            if (HumanSeat < 0) return string.Empty;
            if (slot == 0) return "自分";
            if (slot == 1) return "下家";
            return slot == _model.PlayerCount - 1 ? "上家" : "対面";
        }

        private static string ClaimButtonLabel(ClaimOption option)
        {
            if (option.Type == ClaimType.Ron) return "ロン";

            var name = option.Type == ClaimType.Pon ? "ポン" : "チー";
            return $"{name} [{CardLabel.Join(option.UsedCards)}]";
        }

        private string BuildWinMessage(WinResult win)
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

        private string BuildRoundResultMessage(RoundResult result)
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

        private string BuildGameResultMessage(GameResult result)
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
