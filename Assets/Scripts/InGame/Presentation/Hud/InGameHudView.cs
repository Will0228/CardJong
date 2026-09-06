using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>行動ボタンの種類。実際の色はこれを見て View が決める。</summary>
    public enum ActionButtonKind : byte
    {
        /// <summary>未設定。</summary>
        None = 0,

        /// <summary>ツモ・ロン。</summary>
        Win = 1,

        /// <summary>ポン・チー。</summary>
        Meld = 2,

        /// <summary>リーチ。まだ予約していない状態。</summary>
        Riichi = 3,

        /// <summary>リーチを予約済み。あとは切る牌を選ぶだけの状態。</summary>
        RiichiArmed = 4,

        /// <summary>パス。</summary>
        Pass = 5,
    }

    /// <summary>行動ボタン 1 つぶんの指定。押されたときに何をするかは Presenter が持つ。</summary>
    public record ActionButtonSpec(string Label, ActionButtonKind Kind, Action OnClicked);

    /// <summary>
    /// 卓の上に重ねる画面。渡された文字と牌を並べ、押されたことを外へ流すだけで、
    /// 何を出すか・押されたら何が起きるかは <see cref="InGamePresenter"/> が決める。
    /// </summary>
    /// <remarks>
    /// 他家の手牌・河・鳴き・ドラは <see cref="Table.MahjongTableView"/> が 3D の卓で見せる。
    /// ここが持つのは、点数や局数といった文字情報と、宣言のボタンと、
    /// 画面下に並べる自分の手牌（<see cref="HandUiView"/>）。
    ///
    /// レイアウトを手で組んだシーンにすると、席数やカード枚数が変わるたびにシーン側を
    /// 触ることになる。表示物の数がモデル次第で決まる HUD なので、階層ごとコードに寄せている。
    /// </remarks>
    [AddComponentMenu("CardJong/InGame HUD View")]
    public sealed class InGameHudView : MonoBehaviour
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
        private const int ButtonFontSize = 24;

        /// <summary>手牌の帯を画面下端からどれだけ浮かせるか。</summary>
        private const float HandBottomMargin = 24f;

        /// <summary>画面の縁から情報パネル・名札を離す幅。</summary>
        private const float ScreenMargin = 24f;

        /// <summary>ドラ表示を情報パネルの下に置くときの間隔。</summary>
        private const float DoraPanelGap = 12f;

        [SerializeField]
        [Tooltip("画面下に並べる手牌のプレハブ。")]
        private MahjongTileUiView _tileUiPrefab;

        private readonly List<SeatPlateView> _seatPlates = new();
        private readonly List<GameObject> _actionButtons = new();

        private HudUiFactory _factory;
        private HandUiView _handUi;
        private DoraIndicatorView _doraUi;

        private RectTransform _root;
        private Text _roundText;
        private Text _wallText;
        private RectTransform _buttonRoot;
        private Text _promptText;
        private RectTransform _timerRoot;
        private Image _timerFill;
        private RectTransform _overlayRoot;
        private Text _overlayText;

        private float _timerDuration;
        private float _timerRemaining;

        /// <summary>手牌の牌が押された。</summary>
        public event Action<Card> TileClicked;

        private void Awake()
        {
            _factory = new HudUiFactory();
            BuildHierarchy();

            _handUi.TileClicked += RaiseTileClicked;
        }

        private void OnDestroy()
        {
            if (_handUi != null) _handUi.TileClicked -= RaiseTileClicked;
        }

        private void Update()
        {
            if (_timerDuration <= 0f) return;

            _timerRemaining = Mathf.Max(0f, _timerRemaining - Time.deltaTime);
            _timerFill.fillAmount = _timerRemaining / _timerDuration;
        }

        // ---- 表示の更新 ----

        /// <summary>
        /// 名札を卓の位置ぶん作る。席数が決まったあとに呼ぶ。
        /// 位置（slot）順に並べるので、どの席が座っているかはここでは扱わない。
        /// </summary>
        public void BuildSeatPlates(int slotCount)
        {
            for (var i = 0; i < _seatPlates.Count; i++)
            {
                Destroy(_seatPlates[i].gameObject);
            }

            _seatPlates.Clear();

            for (var slot = 0; slot < slotCount; slot++)
            {
                var rect = _factory.CreateRect($"Seat{slot}", _root);
                var plate = rect.gameObject.AddComponent<SeatPlateView>();
                plate.Build(_factory, PlateAnchor(slot), PlatePosition(slot));
                _seatPlates.Add(plate);
            }
        }

        /// <summary>名札の中身を差し替える。<paramref name="states"/> は卓の位置の順に渡す。</summary>
        public void SetSeatPlates(IReadOnlyList<SeatPlateState> states)
        {
            var count = Mathf.Min(_seatPlates.Count, states.Count);
            for (var slot = 0; slot < count; slot++)
            {
                _seatPlates[slot].Refresh(states[slot]);
            }
        }

        /// <summary>局数と山の残り枚数。</summary>
        public void SetRoundInfo(string round, string wall)
        {
            _roundText.text = round;
            _wallText.text = wall;
        }

        /// <summary>ドラ表示札を並べ直す。</summary>
        public void SetDoraIndicators(IReadOnlyList<Card> indicators) => _doraUi.Refresh(indicators);

        /// <summary>自分の手牌を並べ直す。</summary>
        public void SetHand(IReadOnlyList<HandTile> tiles, bool hasDrawnTile) => _handUi.Refresh(tiles, hasDrawnTile);

        /// <summary>手牌を押せる状態にするか。</summary>
        public void SetHandInteractable(bool value) => _handUi.SetInteractable(value);

        /// <summary>行動を促す一言。空文字なら何も出ない。</summary>
        public void SetPrompt(string text) => _promptText.text = text;

        /// <summary>行動ボタンを並べ直す。</summary>
        public void ShowActions(IReadOnlyList<ActionButtonSpec> actions)
        {
            ClearActions();

            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                var button = _factory.CreateButton(
                    action.Label,
                    _buttonRoot,
                    action.Label,
                    ButtonColorOf(action.Kind),
                    ButtonFontSize,
                    ButtonHeight);

                button.onClick.AddListener(() => action.OnClicked());
                _actionButtons.Add(button.gameObject);
            }
        }

        public void ClearActions()
        {
            for (var i = 0; i < _actionButtons.Count; i++)
            {
                Destroy(_actionButtons[i]);
            }

            _actionButtons.Clear();
        }

        /// <summary>残り時間のバーを出す。0 以下なら出さない。</summary>
        public void ShowTimer(float seconds)
        {
            _timerDuration = seconds;
            _timerRemaining = seconds;
            _timerFill.fillAmount = 1f;
            _timerRoot.gameObject.SetActive(seconds > 0f);
        }

        public void HideTimer()
        {
            _timerDuration = 0f;
            _timerRoot.gameObject.SetActive(false);
        }

        /// <summary>画面全体を覆う案内を出す。消すのは <see cref="HideOverlay"/>。</summary>
        public void ShowOverlay(string message)
        {
            _overlayText.text = message;
            _overlayRoot.gameObject.SetActive(true);
        }

        public void HideOverlay() => _overlayRoot.gameObject.SetActive(false);

        private void RaiseTileClicked(Card card) => TileClicked?.Invoke(card);

        private static Color ButtonColorOf(ActionButtonKind kind) => kind switch
        {
            ActionButtonKind.Win => WinButtonColor,
            ActionButtonKind.Meld => MeldButtonColor,
            ActionButtonKind.Riichi => RiichiButtonColor,
            ActionButtonKind.RiichiArmed => RiichiArmedColor,
            _ => PassButtonColor,
        };

        // ---- 画面の組み立て ----

        private void BuildHierarchy()
        {
            var canvas = _factory.CreateCanvas("HudCanvas", transform);

            // 卓が透けて見えるよう、背景は敷かない。
            _root = _factory.CreateRect("Root", canvas.transform);
            HudUiFactory.Stretch(_root);

            BuildInfoPanel();
            BuildDoraPanel();
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
                new Vector2(ScreenMargin + InfoPanelSize.x * 0.5f, -(ScreenMargin + InfoPanelSize.y * 0.5f)));

            panel.gameObject.AddComponent<Image>().color = PanelColor;
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.childForceExpandWidth = true;

            _roundText = _factory.CreateText("Round", panel, 30, TextAnchor.MiddleLeft, Color.white);
            HudUiFactory.SetFixedSize(_roundText.rectTransform, -1f, 38f);

            _wallText = _factory.CreateText("Wall", panel, 22, TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.82f));
            HudUiFactory.SetFixedSize(_wallText.rectTransform, -1f, 28f);
        }

        /// <summary>ドラ表示札を局数の下に置く。雀魂と同じく画面の左上で常に見えるようにする。</summary>
        private void BuildDoraPanel()
        {
            var rect = _factory.CreateRect("Dora", _root);
            _doraUi = rect.gameObject.AddComponent<DoraIndicatorView>();
            _doraUi.Build(
                _factory,
                _tileUiPrefab,
                new Vector2(ScreenMargin, -(ScreenMargin + InfoPanelSize.y + DoraPanelGap)));
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
                1 => new Vector2(-(ScreenMargin + halfWidth), -20f),
                2 => new Vector2(0f, -(ScreenMargin + halfHeight)),
                3 => new Vector2(ScreenMargin + halfWidth, -20f),
                _ => new Vector2(ScreenMargin + halfWidth, handTop + 16f + halfHeight),
            };
        }
    }
}
