using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CardJong.InGame.Actions;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation.Tiles;
using CardJong.InGame.Rules;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// インゲームの画面。局の進行を <see cref="IInGamePresentation"/> として受け取りつつ、
    /// 人間プレイヤーの選択を <see cref="IPlayerInputPort"/> へ返す。
    /// </summary>
    /// <remarks>
    /// シーンに置くのはこのコンポーネント 1 つだけで、Canvas から下の階層は
    /// <see cref="HudUiFactory"/> が実行時に組み立てる。
    /// </remarks>
    [AddComponentMenu("CardJong/InGame HUD View")]
    public sealed class InGameHudView : MonoBehaviour, IInGamePresentation
    {
        private static readonly Color TableColor = new(0.07f, 0.16f, 0.12f);
        private static readonly Color PanelColor = new(0f, 0f, 0f, 0.55f);
        private static readonly Color OverlayColor = new(0f, 0f, 0f, 0.78f);
        private static readonly Color TimerColor = new(0.98f, 0.78f, 0.30f);
        private static readonly Color WinButtonColor = new(0.76f, 0.24f, 0.24f);
        private static readonly Color MeldButtonColor = new(0.20f, 0.40f, 0.66f);
        private static readonly Color RiichiButtonColor = new(0.70f, 0.45f, 0.16f);
        private static readonly Color RiichiArmedColor = new(0.94f, 0.64f, 0.20f);
        private static readonly Color PassButtonColor = new(0.32f, 0.34f, 0.36f);

        private static readonly Vector2 HandCardSize = new(74f, 103f);

        private const float TopBarHeight = 62f;
        private const float HandAreaHeight = 116f;
        private const float ActionBarHeight = 100f;
        private const float ButtonHeight = 52f;

        [SerializeField]
        [Tooltip("カードの絵柄。3D の牌と同じアトラスを使う。")]
        private CardFaceAtlas _faceAtlas;

        [SerializeField]
        [Min(0f)]
        [Tooltip("局の開始・親決めなど、短い案内を出しておく秒数。")]
        private float _noticeSeconds = 1.6f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("和了・局終了・最終結果を出しておく秒数。")]
        private float _resultSeconds = 3.5f;

        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<PlayerAreaView> _playerAreas = new();
        private readonly List<CardView> _handCards = new();
        private readonly List<CardView> _doraCards = new();
        private readonly List<GameObject> _actionButtons = new();

        private InGameModel _model;
        private InGameSettings _settings;
        private IPlayerInputPort _inputPort;
        private HudUiFactory _factory;

        private Text _roundText;
        private Text _wallText;
        private RectTransform _doraRoot;
        private RectTransform _playersRoot;
        private RectTransform _handRoot;
        private RectTransform _buttonRoot;
        private Text _promptText;
        private RectTransform _timerRoot;
        private Image _timerFill;
        private RectTransform _overlayRoot;
        private Text _overlayText;

        private Button _riichiButton;
        private bool _riichiArmed;
        private bool _handInteractable;
        private float _timerDuration;
        private float _timerRemaining;

        private int HumanSeat => _settings.HumanSeat;

        [Inject]
        public void Construct(InGameModel model, InGameSettings settings, IPlayerInputPort inputPort)
        {
            _model = model;
            _settings = settings;
            _inputPort = inputPort;

            _factory = new HudUiFactory();
            BuildHierarchy();

            _subscriptions.Add(_inputPort.OnTurnDecisionRequested.Subscribe(OnTurnDecisionRequested));
            _subscriptions.Add(_inputPort.OnClaimDecisionRequested.Subscribe(OnClaimDecisionRequested));
            _subscriptions.Add(_inputPort.OnDecisionClosed.Subscribe(_ => CloseDecision()));
        }

        public UniTask ShowGameStartAsync(CancellationToken cancellationToken)
        {
            // ここまでにモデルの初期化が済んでいるので、席の数が決まったこの時点で表示を組む。
            BuildPlayerAreas();
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
            _subscriptions.Dispose();
        }

        // ---- 画面の組み立て ----

        private void BuildHierarchy()
        {
            var canvas = _factory.CreateCanvas("HudCanvas", transform);
            var canvasRect = (RectTransform)canvas.transform;

            var background = _factory.CreateImage("Background", canvasRect, TableColor);
            HudUiFactory.Stretch(background.rectTransform);

            var root = _factory.CreateColumn("Root", canvasRect, 8f, TextAnchor.UpperCenter);
            HudUiFactory.Stretch(root);
            var rootLayout = root.GetComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(16, 16, 16, 16);
            rootLayout.childForceExpandWidth = true;

            BuildTopBar(root);
            BuildPlayersRoot(root);
            BuildHandRoot(root);
            BuildActionBar(root);
            BuildOverlay(canvasRect);
        }

        private void BuildTopBar(RectTransform parent)
        {
            var bar = _factory.CreateRow("TopBar", parent, 24f, TextAnchor.MiddleLeft);
            HudUiFactory.SetFlexibleWidth(bar, TopBarHeight);
            bar.gameObject.AddComponent<Image>().color = PanelColor;
            bar.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(16, 16, 4, 4);

            _roundText = _factory.CreateText("Round", bar, 28, TextAnchor.MiddleLeft, Color.white);
            HudUiFactory.SetFixedSize(_roundText.rectTransform, 240f, TopBarHeight - 8f);

            _wallText = _factory.CreateText("Wall", bar, 24, TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.82f));
            HudUiFactory.SetFixedSize(_wallText.rectTransform, 180f, TopBarHeight - 8f);

            var doraLabel = _factory.CreateText("DoraLabel", bar, 24, TextAnchor.MiddleLeft, new Color(0.98f, 0.86f, 0.44f));
            doraLabel.text = "ドラ表示";
            HudUiFactory.SetFixedSize(doraLabel.rectTransform, 110f, TopBarHeight - 8f);

            _doraRoot = _factory.CreateRow("Dora", bar, 4f, TextAnchor.MiddleLeft);
            HudUiFactory.SetFlexibleWidth(_doraRoot, PlayerAreaView.SmallCardSize.y);
        }

        private void BuildPlayersRoot(RectTransform parent)
        {
            _playersRoot = _factory.CreateColumn("Players", parent, 6f, TextAnchor.UpperCenter);
            var layout = _playersRoot.GetComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;

            var element = HudUiFactory.SetFlexibleWidth(_playersRoot, -1f);
            element.minHeight = -1f;
            element.preferredHeight = -1f;
            element.flexibleHeight = 1f;
        }

        private void BuildHandRoot(RectTransform parent)
        {
            _handRoot = _factory.CreateRow("Hand", parent, 5f, TextAnchor.MiddleCenter);
            HudUiFactory.SetFlexibleWidth(_handRoot, HandAreaHeight);
        }

        private void BuildActionBar(RectTransform parent)
        {
            var bar = _factory.CreateColumn("ActionBar", parent, 8f, TextAnchor.UpperCenter);
            HudUiFactory.SetFlexibleWidth(bar, ActionBarHeight);
            bar.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;

            var promptRow = _factory.CreateRow("Prompt", bar, 20f, TextAnchor.MiddleCenter);
            HudUiFactory.SetFlexibleWidth(promptRow, 30f);

            _promptText = _factory.CreateText("Text", promptRow, 24, TextAnchor.MiddleRight, Color.white);
            HudUiFactory.SetFixedSize(_promptText.rectTransform, 620f, 30f);

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

            _buttonRoot = _factory.CreateRow("Buttons", bar, 10f, TextAnchor.MiddleCenter);
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

        /// <summary>席の表示を作る。自分が一番下に来るよう、自分の次の席から順に並べる。</summary>
        private void BuildPlayerAreas()
        {
            for (var i = 0; i < _playerAreas.Count; i++)
            {
                Destroy(_playerAreas[i].gameObject);
            }

            _playerAreas.Clear();

            var start = HumanSeat >= 0 ? _model.GetNextSeat(HumanSeat) : 0;
            for (var i = 0; i < _model.PlayerCount; i++)
            {
                var seat = (start + i) % _model.PlayerCount;
                var rect = _factory.CreateRect($"Player{seat}", _playersRoot);
                var area = rect.gameObject.AddComponent<PlayerAreaView>();
                area.Build(_factory, _faceAtlas, seat, RelationLabelOf(seat));
                _playerAreas.Add(area);
            }
        }

        // ---- モデルの購読 ----

        private void SubscribeToModel()
        {
            _subscriptions.Add(_model.RoundNumber.Subscribe(_ => RefreshTopBar()));
            _subscriptions.Add(_model.Honba.Subscribe(_ => RefreshTopBar()));
            _subscriptions.Add(_model.Wall.LiveWallRemaining.Subscribe(_ => RefreshTopBar()));
            _subscriptions.Add(_model.CurrentSeat.Subscribe(_ => RefreshPlayers()));
            _subscriptions.Add(_model.DealerSeat.Subscribe(_ => RefreshPlayers()));

            for (var seat = 0; seat < _model.PlayerCount; seat++)
            {
                var player = _model.GetPlayer(seat);
                _subscriptions.Add(player.Score.Points.Subscribe(_ => RefreshPlayers()));
                _subscriptions.Add(player.Cards.OnChanged.Subscribe(_ =>
                {
                    RefreshPlayers();
                    if (player.Seat == HumanSeat) RefreshHand();
                }));
            }
        }

        private void RefreshAll()
        {
            RefreshTopBar();
            RefreshPlayers();
            RefreshHand();
        }

        private void RefreshTopBar()
        {
            _roundText.text = $"{RoundLabel(_model.RoundNumber.CurrentValue)}  {_model.Honba.CurrentValue}本場";
            _wallText.text = $"残り {_model.Wall.LiveWallRemaining.CurrentValue} 枚";

            var indicators = _model.Wall.DoraIndicators;
            _factory.EnsureCardViews(_doraCards, _doraRoot, _faceAtlas, indicators.Count, PlayerAreaView.SmallCardSize);
            for (var i = 0; i < indicators.Count; i++)
            {
                _doraCards[i].ShowFace(indicators[i]);
            }
        }

        private void RefreshPlayers()
        {
            var dealerSeat = _model.DealerSeat.CurrentValue;
            var currentSeat = _model.CurrentSeat.CurrentValue;

            for (var i = 0; i < _playerAreas.Count; i++)
            {
                var area = _playerAreas[i];
                area.Refresh(_model.GetPlayer(area.Seat), area.Seat == dealerSeat, area.Seat == currentSeat);
            }
        }

        private void RefreshHand()
        {
            if (HumanSeat < 0) return;

            var cards = _model.GetPlayer(HumanSeat).Cards.ConcealedCards;
            _factory.EnsureCardViews(_handCards, _handRoot, _faceAtlas, cards.Count, HandCardSize);

            var handler = _handInteractable ? new Action<Card>(OnHandCardClicked) : null;
            for (var i = 0; i < cards.Count; i++)
            {
                _handCards[i].ShowFace(cards[i]);
                _handCards[i].SetClickHandler(handler);
            }
        }

        // ---- 人間プレイヤーの入力 ----

        private void OnTurnDecisionRequested(TurnDecisionContext context)
        {
            _riichiArmed = false;
            _riichiButton = null;
            ClearActionButtons();

            _promptText.text = "捨てるカードを選んでください";

            if (context.CanDeclareTsumo)
            {
                AddActionButton("ツモ", WinButtonColor, () => _inputPort.SubmitTurnAction(TurnAction.Tsumo()));
            }

            if (context.CanDeclareRiichi)
            {
                _riichiButton = AddActionButton("リーチ", RiichiButtonColor, ToggleRiichi);
            }

            _handInteractable = true;
            RefreshHand();
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
            _handInteractable = false;
            ClearActionButtons();
            StopTimer();
            _promptText.text = string.Empty;
            RefreshHand();
        }

        private void OnHandCardClicked(Card card)
        {
            _inputPort.SubmitTurnAction(_riichiArmed ? TurnAction.Riichi(card) : TurnAction.Discard(card));
        }

        /// <summary>リーチは宣言と打牌が一体なので、ボタンで予約してから捨てる札を選ばせる。</summary>
        private void ToggleRiichi()
        {
            _riichiArmed = !_riichiArmed;
            _promptText.text = _riichiArmed ? "リーチ宣言牌を選んでください" : "捨てるカードを選んでください";
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

        private string RelationLabelOf(int seat)
        {
            if (HumanSeat < 0) return string.Empty;

            var distance = (seat - HumanSeat + _model.PlayerCount) % _model.PlayerCount;
            if (distance == 0) return "自分";
            if (distance == 1) return "下家";
            return distance == _model.PlayerCount - 1 ? "上家" : "対面";
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
            builder.Append("\n").Append(CardLabel.Of(win.WinningCard)).Append("\n");

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
